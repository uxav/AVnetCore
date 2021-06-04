using System.IO;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using Newtonsoft.Json.Linq;
using UXAV.AVnet.Core.Models;
using UXAV.Logging;

namespace UXAV.AVnet.Core.WebScripting.InternalApi
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
                case "load":
                    Logger.Warn("Remote load requested from {0}", Request.UserHostAddress);
                    var appDir = new DirectoryInfo(SystemBase.ProgramApplicationDirectory);
                    var files = appDir.GetFiles("*.cpz");
                    if (files.Length == 1)
                    {
                        Logger.Warn($"Will send progload command for app {InitialParametersClass.ApplicationNumber}," +
                                    $"file found: {files[0].FullName}");
                        WriteResponse($"App will load \"{files[0].FullName}\" now!");
                        Task.Run(() =>
                        {
                            CrestronConsole.SendControlSystemCommand(
                                $"progload -p:{InitialParametersClass.ApplicationNumber}", ref response);
                            Logger.Highlight($"progload response: \"{response}\"");
                        });
                        return;
                    }

                    if (files.Length == 0)
                    {
                        HandleError(404, "Not Found", "CPZ file could not be found");
                        return;
                    }

                    HandleError(409, "Conflict", "More than one CPZ file found in app directory");
                    return;
                default:
                    HandleError(400, "Bad Request", $"Unknown command: \"{cmd}\"");
                    return;
            }
        }
    }
}