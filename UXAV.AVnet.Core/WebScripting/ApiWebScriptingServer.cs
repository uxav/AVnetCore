using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UXAV.AVnet.Core.Models;
using WebSocketSharp;
using Logger = UXAV.Logging.Logger;

namespace UXAV.AVnet.Core.WebScripting
{
    public class ApiWebScriptingServer : WebScriptingServer
    {
        public ApiWebScriptingServer(SystemBase system, string directory)
            : base(system, directory)
        {
        }

        public override void AddRoute(string routePattern, Type handlerType)
        {
            if (!handlerType.IsSubclassOf(typeof(ApiRequestHandler)))
                throw new Exception(
                    $"Type \"{handlerType.Name}\" is not derived from {nameof(ApiRequestHandler)}");

            base.AddRoute(routePattern, handlerType);
        }

        public override async Task HandleErrorAsync(WebScriptingRequest request, Exception e)
        {
            Logger.Error(e);
            request.Response.StatusCode = 500;
            await request.Response.WriteAsJsonAsync(new
            {
                Request = new
                {
                    request.Path,
                    request.Method,
                    request.RouteValues
                },
                Code = request.Response.StatusCode,
                Error = new
                {
                    Status = request.Response.StatusCode.GetStatusDescription(),
                    e.Message,
                    e.StackTrace
                }
            });
        }

        public override async Task HandleErrorAsync(WebScriptingRequest request, int statusCode,
            string message)
        {
            Logger.Warn($"\"{request.Path}\" Error {statusCode} {message}");
            request.Response.StatusCode = statusCode;
            await request.Response.WriteAsJsonAsync(new
            {
                Request = new
                {
                    request.Path,
                    request.Method,
                    request.RouteValues
                },
                Code = request.Response.StatusCode,
                Error = new
                {
                    Status = request.Response.StatusCode.GetStatusDescription(),
                    Message = message,
                    StackTrace = ""
                }
            });
        }
    }
}