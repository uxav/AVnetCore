using Crestron.SimplSharp;
using Newtonsoft.Json.Linq;
using UXAV.Logging;

namespace UXAV.AVnetCore.WebScripting.InternalApi
{
    public class AppControlApiHandler : ApiRequestHandler
    {
        public AppControlApiHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request)
        {
        }

        [SecureRequest]
        public void Post()
        {
            var json = JToken.Parse(Request.GetStringContents());
            var cmd = (json["command"] ?? string.Empty).Value<string>();
            var response = string.Empty;
            switch (cmd)
            {
                case "restart":
                    Logger.Warn("Remote restart requested from {0}", Request.UserHostAddress);
                    CrestronConsole.SendControlSystemCommand($"progres -P:{InitialParametersClass.ApplicationNumber}",
                        ref response);
                    WriteResponse(response);
                    return;
                case "reboot":
                    Logger.Warn("Remote reboot requested from {0}", Request.UserHostAddress);
                    WriteResponse("App will now send reboot command!");
                    System.RebootAppliance();
                    return;
                default:
                    HandleError(400, "Bad Request", $"Unknown command: \"{cmd}\"");
                    return;
            }
        }
    }
}