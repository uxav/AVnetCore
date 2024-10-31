using System;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using UXAV.Logging;

namespace UXAV.AVnet.Core.Web;

public static class WebServer
{
    private static WebApplication _app;

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

    public static void AddRoute(string path, Action<HttpContext> handler)
    {
        _app.Map(path, handler);
    }

    public static void AddRoute(string path, Func<HttpContext, Task> handler)
    {
        _app.Map(path, handler);
    }

    public static void MapStaticFiles(string requestPath, string physicalPath)
    {
        if (_app == null)
        {
            throw new InvalidOperationException("Web server is not initialized");
        }

        _app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(physicalPath),
            RequestPath = requestPath
        });
    }

    public static void Init(int port)
    {
        if (_app != null)
        {
            throw new InvalidOperationException("Web server is already running");
        }

        _app = WebApplication.Create();
        _app.Urls.Add($"http://*:{port}");
        _app.Map("/", context =>
        {
            context.Response.Redirect("/cws/app");
            return Task.CompletedTask;
        });
    }

    public async static Task StartAsync()
    {
        if (_app == null)
        {
            throw new InvalidOperationException("Web server is not initialized");
        }

        await _app.RunAsync();
    }
}