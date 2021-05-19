using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UXAV.Logging;

namespace UXAV.AVnet.Core.WebScripting
{
    public class ApiWebScriptingServer : WebScriptingServer
    {
        public ApiWebScriptingServer(Models.SystemBase system, string directory)
            : base(system, directory)
        {

        }

        public override void AddRoute(string routePattern, Type handlerType)
        {
            if (!handlerType.IsSubclassOf(typeof(ApiRequestHandler)))
                throw new Exception($"Type \"{handlerType.Name}\" is not derived from {typeof(ApiRequestHandler).Name}");

            base.AddRoute(routePattern, handlerType);
        }

        public override void HandleError(WebScriptingRequest request, Exception e)
        {
            Logger.Error(e);
            request.Response.StatusCode = 500;
            request.Response.StatusDescription = "Server Error";
            request.Response.ContentType = "application/json";
            var json = JToken.FromObject(new
            {
                @Request = new
                {
                    request.Path,
                    request.Method
                },
                @Code = request.Response.StatusCode,
                @Error = new
                {
                    @Status = request.Response.StatusDescription,
                    e.Message,
                    e.StackTrace
                }
            });
            request.Response.Write(json.ToString(Formatting.Indented), true);
        }

        public override void HandleError(WebScriptingRequest request, int statusCode, string statusDescription, string message)
        {
            Logger.Warn("\"{3}\" Error {0} {1}: {2}", statusCode, statusDescription, message, request.Path);
            request.Response.StatusCode = statusCode;
            request.Response.StatusDescription = statusDescription;
            request.Response.ContentType = "application/json";
            var json = JToken.FromObject(new
            {
                @Request = new
                {
                    request.Path,
                    request.Method
                },
                @Code = request.Response.StatusCode,
                @Error = new
                {
                    @Status = request.Response.StatusDescription,
                    @Message = message,
                    @StackTrace = ""
                }
            });
            request.Response.Write(json.ToString(Formatting.Indented), true);
        }
    }
}