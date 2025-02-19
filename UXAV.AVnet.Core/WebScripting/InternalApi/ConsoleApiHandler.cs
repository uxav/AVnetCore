using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Crestron.SimplSharp;
using Newtonsoft.Json.Linq;

namespace UXAV.AVnet.Core.WebScripting.InternalApi
{
    internal class ConsoleApiHandler : ApiRequestHandler
    {
        private static readonly object _lock = new();
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
                    if (Monitor.TryEnter(_lock, 10000))
                    {
                        try
                        {
                            CrestronConsole.SendControlSystemCommand(cmd, ref r);
                            response.Add(r);
                        }
                        finally
                        {
                            Monitor.Exit(_lock);
                        }
                    }
                    else
                    {
                        throw new Exception("Timeout waiting for access to console");
                    }
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