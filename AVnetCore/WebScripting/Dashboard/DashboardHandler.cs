using Scriban;
using UXAV.AVnetCore.Logging;

namespace UXAV.AVnetCore.WebScripting.Dashboard
{
    public class DashboardHandler : RequestHandler
    {
        public DashboardHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request)
        {

        }

        [SecureRequest(true)]
        public void Get()
        {
            var template = Template.Parse("Hello {{name}}!");
            var name = "World";
            if (Request.RoutePatternArgs.ContainsKey("name"))
            {
                name = Request.RoutePatternArgs["name"];
            }

            var result = template.Render(new {name});
            Response.Write(result, true);
        }
    }
}