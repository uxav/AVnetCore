using System;
using System.Collections.Generic;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Newtonsoft.Json.Linq;

namespace UXAV.AVnet.Core.WebScripting.InternalApi
{
    internal class ConsoleApiHandler : ApiRequestHandler
    {
        public ConsoleApiHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request, true)
        {
        }

        public void Get()
        {
            try
            {
                var cmd = Request.Query.Get("cmd");
                var response = string.Empty;
                CrestronConsole.SendControlSystemCommand(cmd, ref response);
                WriteResponse(response);
            }
            catch (Exception e)
            {
                HandleError(e);
            }
        }

        public void Post()
        {
            try
            {
                var content = new StreamReader(Request.InputStream).ReadToEnd();
                var json = JToken.Parse(content);
                var response = new List<string>();
                var r = string.Empty;
                foreach (var command in json["commands"])
                {
                    var cmd = command.Value<string>();
                    CrestronConsole.SendControlSystemCommand(cmd, ref r);
                    //Logger.Debug($"Received response for \"{cmd}\":\r\n{r}");
                    response.Add(r);
                }

                WriteResponse(response);
            }
            catch (Exception e)
            {
                HandleError(e);
            }
        }
    }
}