using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UXAV.AVnetCore.WebScripting
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

        protected void WriteResponse(object response)
        {
            Response.ContentType = "application/json";
            var json = JToken.FromObject(new
            {
                @Request = new
                {
                    Request.Path,
                    Request.Method,
                    Request.RoutePattern,
                    Request.RoutePatternArgs,
                    Request.ContentLength
                },
                @Handler = GetType().FullName,
                @Code = Response.StatusCode,
                @Response = response
            });
            Response.Write(json.ToString(Formatting.None), true);
        }
    }
}