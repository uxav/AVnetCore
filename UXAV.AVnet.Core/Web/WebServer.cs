using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using UXAV.AVnet.Core.Models;
using UXAV.AVnet.Core.UI;
using UXAV.AVnet.Core.UI.Ch5;
using UXAV.Logging;

namespace UXAV.AVnet.Core.Web;

/// <summary>
/// Provides functionality to initialize and manage a web server.
/// </summary>
public static class WebServer
{
    private static WebApplication _app;
    private static Dictionary<string, Func<Ch5ConnectionInstance>> _apiHandlers = [];

    static WebServer()
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
    }

    /// <summary>
    /// Adds a route to the web server with a synchronous handler.
    /// </summary>
    /// <param name="path">The URL path of the route.</param>
    /// <param name="handler">The handler to process requests to the route.</param>
    public static void AddRoute(string path, Action<HttpContext> handler)
    {
        _app.Map(path, handler);
    }

    /// <summary>
    /// Adds a route to the web server with an asynchronous handler.
    /// </summary>
    /// <param name="path">The URL path of the route.</param>
    /// <param name="handler">The handler to process requests to the route.</param>
    public static void AddRoute(string path, Func<HttpContext, Task> handler)
    {
        _app.Map(path, handler);
    }

    public static void SetupUI(string requestPath, string physicalPath)
    {
        _app.Map("/ui/ws/{*id}", async context =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var id = context.Request.RouteValues["id"].ToString();
                Logger.Debug($"WebSocket request received for id: {id}");
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var path = $"/ui/ws/{id}";
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
            StaticFileOptions = { ServeUnknownFileTypes = true }
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
    /// Initializes the web server on the specified port.
    /// </summary>
    /// <param name="port">The port to run the web server on.</param>
    /// <param name="securePort">The secure port to run the web server on.</param>
    /// <param name="certificate">The certificate to use for HTTPS. Default is null.</param>
    /// <exception cref="InvalidOperationException">Thrown if the web server is already running.</exception>
    public static void Init(int port, int securePort, X509Certificate2 certificate = null)
    {
        if (_app != null)
        {
            throw new InvalidOperationException("Web server is already running");
        }
        var builder = WebApplication.CreateBuilder();
        builder.Environment.WebRootPath = Path.Combine(SystemBase.ProgramApplicationDirectory, "webroot");

        builder.WebHost.UseKestrel(options =>
        {
            options.ListenAnyIP(port);
            if (certificate != null)
            {
                options.ListenAnyIP(securePort, listenOptions =>
                {
                    listenOptions.UseHttps(certificate);
                });
            }
        });

        _app = builder.Build();
        _app.Urls.Add($"http://*:{port}");
        if (certificate != null)
        {
#if !DEBUG
            _app.UseHttpsRedirection();
#endif
            _app.Urls.Add($"https://*:{securePort}");
        }
        _app.Map("/", context =>
        {
            context.Response.Redirect("/cws/app");
            return Task.CompletedTask;
        });

        _app.UseWebSockets();

        Logger.Highlight($"Web server initialized. Urls are: {string.Join(", ", _app.Urls)}");
    }

    /// <summary>
    /// Starts the web server asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the web server is not initialized.</exception>
    public async static Task StartAsync()
    {
        if (_app == null)
        {
            throw new InvalidOperationException("Web server is not initialized");
        }

        await _app.RunAsync();
    }
    internal static void AddDeviceService<THandler>(Ch5UIController<THandler> controller)
            where THandler : Ch5ApiHandlerBase
    {
        var path = $"/ui/ws/{controller.Device.ID:x2}";
        var ipAddress = SystemBase.IpAddress;
        var url = $"ws{(_app.Urls.First().Contains("https") ? "s" : "")}://{ipAddress}:{_app.Urls.First().Split(':').Last()}{path}";
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
    public static void AddWebService<THandler>(string path = "/ui/ws/web") where THandler : Ch5ApiHandlerBase
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