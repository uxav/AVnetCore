using System;
using UXAV.AVnetCore.Logging;

namespace UXAV.AVnetCore.WebScripting.InternalApi
{
    public class LogsApiHandler : ApiRequestHandler
    {
        public LogsApiHandler(WebScriptingServer server, WebScriptingRequest request)
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