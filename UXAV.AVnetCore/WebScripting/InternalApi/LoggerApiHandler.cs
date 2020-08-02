using System;
using UXAV.Logging;

namespace UXAV.AVnetCore.WebScripting.InternalApi
{
    public class LoggerApiHandler : ApiRequestHandler
    {
        public LoggerApiHandler(WebScriptingServer server, WebScriptingRequest request)
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