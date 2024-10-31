using System;
using System.Collections.Generic;
using System.IO;
using Crestron.SimplSharp;
using Newtonsoft.Json.Linq;

namespace UXAV.AVnet.Core.WebScripting.InternalApi
{
    internal class ConsoleApiHandler : ApiRequestHandler
    {
        public ConsoleApiHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request, true)
        {
        }

        public async void Get()
        {
            try
            {
                var cmd = Request.Query["cmd"];
                var response = string.Empty;
                CrestronConsole.SendControlSystemCommand(cmd, ref response);
                await WriteResponseAsync(response);
            }
            catch (Exception e)
            {
                await HandleErrorAsync(e);
            }
        }

        public async void Post()
        {
            try
            {
                var content = await Request.GetStringContentsAsync();
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

                await WriteResponseAsync(response);
            }
            catch (Exception e)
            {
                await HandleErrorAsync(e);
            }
        }
    }
}