using System;
using Newtonsoft.Json.Linq;
using UXAV.AVnet.Core.Cloud;
using UXAV.Logging;

namespace UXAV.AVnet.Core.WebScripting.InternalApi
{
    public class UpdatesApiHandler : ApiRequestHandler
    {
        public UpdatesApiHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request)
        {
        }

        public UpdatesApiHandler(WebScriptingServer server, WebScriptingRequest request, bool suppressLogging)
            : base(server, request, suppressLogging)
        {
        }

        [SecureRequest]
        public void Get()
        {
            try
            {
                bool.TryParse(Request.Query["beta"], out var beta);
                bool.TryParse(Request.Query["debug"], out var debug);
                bool.TryParse(Request.Query["rollbacks"], out var rollBack);
                Logger.Debug($"Beta = {beta}, Debug = {debug}, RollBack = {rollBack}");
                var updates = UpdateHelper.GetUpdatesAsync(
                    debug, beta, rollBack).Result;
                WriteResponse(updates);
            }
            catch (Exception e)
            {
                HandleError(e);
            }
        }

        [SecureRequest]
        public void Post()
        {
            try
            {
                var json = JToken.Parse(Request.GetStringContents());
                var command = (json["command"] ?? string.Empty).Value<string>();
                switch (command)
                {
                    case "update":
                        var fileName = (json["fileName"] ?? string.Empty).Value<string>();
                        if (string.IsNullOrEmpty(fileName))
                            throw new ArgumentException("Argument missing", nameof(fileName));
                        UpdateHelper.UpdateRunningProgram(fileName);
                        WriteResponse("OK");
                        return;
                    default:
                        throw new ArgumentException("Invalid command", nameof(command));
                }
            }
            catch (Exception e)
            {
                HandleError(e);
            }
        }
    }
}