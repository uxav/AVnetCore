using System;
using System.IO;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using Newtonsoft.Json.Linq;
using UXAV.AVnet.Core.Models;
using UXAV.Logging;

namespace UXAV.AVnet.Core.WebScripting.InternalApi
{
    internal class AppControlApiHandler : ApiRequestHandler
    {
        public AppControlApiHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request)
        {
        }

        [SecureRequest]
        public async void Post()
        {
            var json = JToken.Parse(await Request.GetStringContentsAsync());
            var cmd = (json["command"] ?? string.Empty).Value<string>();
            var response = string.Empty;
            switch (cmd)
            {
                case "restart":
                    Logger.Warn("Remote restart requested from {0}", Request.UserHostAddress);
                    await WriteResponseAsync(System.RestartAppAsync());
                    return;
                case "reboot":
                    Logger.Warn("Remote reboot requested from {0}", Request.UserHostAddress);
                    await WriteResponseAsync("App will now send reboot command!");
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
                        await WriteResponseAsync($"App will load \"{files[0].FullName}\" now!");
                        await Task.Run(() =>
                        {
                            CrestronConsole.SendControlSystemCommand(
                                $"progload -p:{InitialParametersClass.ApplicationNumber}", ref response);
                            Logger.Highlight($"progload response: \"{response}\"");
                        });
                        return;
                    }

                    if (files.Length == 0)
                    {
                        await HandleErrorAsync(404, "CPZ file could not be found");
                        return;
                    }

                    await HandleErrorAsync(409, "More than one CPZ file found in app directory");
                    return;
                default:
                    await HandleErrorAsync(400, $"Unknown command: \"{cmd}\"");
                    return;
            }
        }
    }
}