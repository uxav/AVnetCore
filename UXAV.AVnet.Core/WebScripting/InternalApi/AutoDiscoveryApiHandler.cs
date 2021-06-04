using System;

namespace UXAV.AVnet.Core.WebScripting.InternalApi
{
    public class AutoDiscoveryApiHandler : ApiRequestHandler
    {
        public AutoDiscoveryApiHandler(WebScriptingServer server, WebScriptingRequest request) : base(server, request)
        {
        }

        public void Get()
        {
            try
            {
                var results = AutoDiscovery.Get();
                WriteResponse(results);
            }
            catch (OperationCanceledException e)
            {
                HandleError(503, "Service Unavailable", e.Message);
            }
            catch (Exception e)
            {
                HandleError(500, "Server Error", e.Message);
            }
        }
    }
}