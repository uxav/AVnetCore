using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UXAV.AVnet.Core.Models;
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
            try
            {
                Logger.Error(e);
                request.Response.Clear();
                request.Response.StatusCode = 500;
                var json = JToken.FromObject(new
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
                        Status = ReasonPhrases.GetReasonPhrase(request.Response.StatusCode),
                        e.Message,
                        e.StackTrace
                    }
                });
                request.Response.ContentType = "application/json charset=utf-8";
                await request.Response.WriteAsync(json.ToString());
            }
            catch (Exception e2)
            {
                Logger.Error(e2);
            }
        }

        public override async Task HandleErrorAsync(WebScriptingRequest request, int statusCode,
            string message)
        {
            Logger.Warn($"\"{request.Path}\" Error {statusCode} {message}");
            request.Response.StatusCode = statusCode;
            var json = JToken.FromObject(new
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
                    Status = ReasonPhrases.GetReasonPhrase(statusCode),
                    Message = message,
                    StackTrace = ""
                }
            });
            request.Response.ContentType = "application/json charset=utf-8";
            await request.Response.WriteAsync(json.ToString());
        }
    }
}