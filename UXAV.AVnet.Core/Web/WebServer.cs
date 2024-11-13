using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.EthernetCommunication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using UXAV.AVnet.Core.Models;
using UXAV.AVnet.Core.UI;
using UXAV.AVnet.Core.UI.Ch5;
using UXAV.Logging;

namespace UXAV.AVnet.Core.Web;

/// <summary>
/// Provides functionality to initialize and manage a web server.
/// </summary>
public class WebServer
{
    private WebApplication _app;
    private Dictionary<string, Func<Ch5ConnectionInstance>> _apiHandlers = [];
    private ILogger<WebServer> _logger;

    public int Port { get; }
    public int SecurePort { get; }

    public WebServer(WebServerConfiguration configuration)
    {
        CrestronEnvironment.ProgramStatusEventHandler += (status) =>
        {
            if (status == eProgramStatusEventType.Stopping)
            {
                try
                {
                    _app?.StopAsync();
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        };

        if (_app != null)
        {
            throw new InvalidOperationException("Web server is already running");
        }

        Port = configuration.Port;
        SecurePort = configuration.SecurePort;

        var builder = WebApplication.CreateBuilder();
        builder.Environment.WebRootPath = Path.Combine(SystemBase.ProgramApplicationDirectory, "webroot");

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();
        builder.Logging.AddProvider(new WebLoggerProvider());
        _logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<WebServer>>();
        _logger.LogInformation("Web server initializing...");

        builder.WebHost.UseKestrel(options =>
        {
            options.ListenAnyIP(configuration.Port);
            if (configuration.Certificate != null)
            {
                options.ListenAnyIP(configuration.SecurePort, listenOptions =>
                {
                    listenOptions.UseHttps(configuration.Certificate);
                });
            }
        });

        _app = builder.Build();
        if (configuration.Certificate != null && configuration.UseHttpsRedirection)
        {
            _app.UseHttpsRedirection();
        }
        _app.Map("/", context =>
        {
            context.Response.Redirect("/cws/app");
            return Task.CompletedTask;
        });
        if (CrestronEnvironment.DevicePlatform == eDevicePlatform.Appliance)
        {
            _app.Map("/index_device_webui.html", context =>
            {
                var newPort = UxEnvironment.ApplianceCurrentWebPort;
                context.Response.Redirect($"http://{context.Request.Host}:{newPort}/index_device_webui.html");
                return Task.CompletedTask;
            });
            _app.Map("/setup", context =>
            {
                var newPort = UxEnvironment.ApplianceCurrentWebPort;
                context.Response.Redirect($"http://{context.Request.Host}:{newPort}/index_device_webui.html");
                return Task.CompletedTask;
            });
        }

        _app.UseWebSockets();

        try
        {
            configuration.ConfigureServer?.Invoke(this);
        }
        catch (Exception e)
        {
            Logger.Error(e);
        }
    }

    /// <summary>
    /// Adds a route to the web server with a synchronous handler.
    /// </summary>
    /// <param name="path">The URL path of the route.</param>
    /// <param name="handler">The handler to process requests to the route.</param>
    public void AddRoute(string path, Action<HttpContext> handler)
    {
        _app.Map(path, handler);
    }

    /// <summary>
    /// Adds a route to the web server with an asynchronous handler.
    /// </summary>
    /// <param name="path">The URL path of the route.</param>
    /// <param name="handler">The handler to process requests to the route.</param>
    public void AddRoute(string path, Func<HttpContext, Task> handler)
    {
        _app.Map(path, handler);
    }

    /// <summary>
    /// Adds a route to the web server with a synchronous handler.
    /// </summary>
    /// <param name="middleware"></param>
    public void Use(Func<HttpContext, Func<Task>, Task> middleware)
    {
        _app.Use(middleware);
    }

    /// <summary>
    /// Setup HTML UI for the web server. Maps <paramref name="requestPath"/> + "/ws/{id}" to a WebSocket handler and serves static files from the specified directory.
    /// </summary>
    /// <param name="requestPath">Base request path</param>
    /// <param name="physicalPath"></param>
    public void SetupUI(string requestPath, string physicalPath)
    {
        if (requestPath.EndsWith("/"))
        {
            requestPath = requestPath.Substring(0, requestPath.Length - 1);
        }
        if (!requestPath.StartsWith("/"))
        {
            requestPath = $"/{requestPath}";
        }

        // setup websocket handler
        var pattern = RoutePatternFactory.Parse(requestPath + "/ws/{*id}");
        _app.Map(pattern, async (HttpContext context, string id) =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                Logger.Debug($"WebSocket request received for id: {id}");
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var path = $"{requestPath}/ws/{id}";
                if (_apiHandlers.ContainsKey(path))
                {
                    var handler = _apiHandlers[path]();
                    await handler.RunAsync(webSocket, context);
                }
                else
                {
                    Logger.Error($"No handler found for path: {path}");
                    context.Response.StatusCode = 404;
                }
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        });

        _app.UseFileServer(new FileServerOptions
        {
            FileProvider = new PhysicalFileProvider(physicalPath),
            RequestPath = requestPath,
            EnableDefaultFiles = true,
            StaticFileOptions =
            {
                ServeUnknownFileTypes = true,
                OnPrepareResponse = ctx =>
                {
                    Logger.Debug($"Request for file at path {ctx.Context.Request.Path} - Serving file: {ctx.File.Name}");
                    if (!ctx.File.Name.Equals("index.html", StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=604800");
                    }
                    else if (ctx.File.Name.Equals("index.html", StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Context.Response.Headers.Append("Cache-Control", "no-cache");
                    }
                }
            }
        });

        _app.Use(async (context, next) =>
        {
            await next();

            if (context.Response.StatusCode == 404 && context.Request.Path.StartsWithSegments(requestPath))
            {
                var filePath = Path.Combine(physicalPath, "index.html");
                if (File.Exists(filePath))
                {
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "text/html";
                    await context.Response.SendFileAsync(filePath);
                    await context.Response.CompleteAsync();
                }
            }
        });
    }

    /// <summary>
    /// Starts the web server asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the web server is not initialized.</exception>
    internal async Task StartAsync()
    {
        if (_app == null)
        {
            throw new InvalidOperationException("Web server is not initialized");
        }

        await _app.RunAsync();
    }
    internal void AddDeviceService<THandler>(Ch5UIController<THandler> controller)
            where THandler : Ch5ApiHandlerBase
    {
        var path = $"/ui/ws/{controller.Device.ID:x2}";
        var ipAddress = SystemBase.IpAddress;
        var url = $"ws://{ipAddress}:{Port}{path}";
        controller.WebSocketUrl = url;
        if (_apiHandlers.ContainsKey(path))
        {
            throw new InvalidOperationException($"Device service already exists for path: {path}");
        }
        _apiHandlers.Add(path, () =>
        {
            var ctor = typeof(THandler).GetConstructor([typeof(Core3ControllerBase)]);
            var handler = (Ch5ApiHandlerBase)ctor.Invoke([controller]);
            return new Ch5ConnectionInstance(handler, controller);
        });
        Logger.Highlight($"Websocket URL for UI Controller {controller.Id} set to: {controller.WebSocketUrl}");
    }

    /// <summary>
    /// Adds a web service to the web server.
    /// </summary>
    /// <typeparam name="THandler">Must be derived from <see cref="Ch5ApiHandlerBase"/> </typeparam>
    /// <param name="path">Default is /ui/ws/web</param>
    /// <exception cref="InvalidOperationException">Web service already exists for path</exception>
    public void AddWebService<THandler>(string path = "/ui/ws/web") where THandler : Ch5ApiHandlerBase
    {
        if (_apiHandlers.ContainsKey(path))
        {
            throw new InvalidOperationException($"Web service already exists for path: {path}");
        }
        _apiHandlers.Add(path, () =>
        {
            var ctor = typeof(THandler).GetConstructor([typeof(Core3ControllerBase)]);
            var handler = (Ch5ApiHandlerBase)ctor.Invoke([null]);
            return new Ch5ConnectionInstance(handler);
        });
        Logger.Highlight($"Web service added for path: {path}");
    }
}