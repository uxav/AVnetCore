using System;
using UXAV.Logging;

namespace UXAV.AVnet.Core.WebScripting.InternalApi
{
    internal class LoggerApiHandler : ApiRequestHandler
    {
        internal LoggerApiHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request, true)
        {
        }

        [SecureRequest]
        public async void Get()
        {
            try
            {
                var logs = Logger.GetHistory();
                await WriteResponseAsync(logs);
            }
            catch (Exception e)
            {
                await HandleErrorAsync(e);
            }
        }
    }
}