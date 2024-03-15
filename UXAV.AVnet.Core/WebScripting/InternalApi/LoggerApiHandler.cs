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
        public void Get()
        {
            try
            {
                var logs = Logger.GetHistory();
                WriteResponse(logs);
            }
            catch (Exception e)
            {
                HandleError(e);
            }
        }
    }
}