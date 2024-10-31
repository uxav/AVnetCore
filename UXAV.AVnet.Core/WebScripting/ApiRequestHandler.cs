using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace UXAV.AVnet.Core.WebScripting
{
    public abstract class ApiRequestHandler : RequestHandler
    {
        protected ApiRequestHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request)
        {
        }

        protected ApiRequestHandler(WebScriptingServer server, WebScriptingRequest request, bool suppressLogging)
            : base(server, request, suppressLogging)
        {
        }

        protected async Task WriteResponseAsync(object response)
        {
            var json = JsonConvert.SerializeObject(new
            {
                Request = new
                {
                    Request.Path,
                    Request.Method,
                    Request.RoutePattern,
                    Request.RoutePatternArgs,
                    Request.ContentLength,
                    Request.RouteValues
                },
                Handler = GetType().FullName,
                Code = Response.StatusCode,
                Response = response
            }, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            });

            Response.ContentType = "application/json charset=utf-8";
            await Response.WriteAsync(json);
        }
    }
}