using System;
using UXAV.AVnet.Core.Models.Diagnostics;
using UXAV.AVnet.Core.Models;

namespace UXAV.AVnet.Core.WebScripting.InternalApi
{
    public class DiagnosticsApiHandler : ApiRequestHandler
    {
        public DiagnosticsApiHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request)
        {
        }

        public void Get()
        {
            try
            {
                var messages = DiagnosticService.GetMessages();
                WriteResponse(messages);
            }
            catch (Exception e)
            {
                HandleError(e);
            }
        }
    }
}