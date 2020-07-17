using System;
using UXAV.AVnetCore.Models;
using UXAV.AVnetCore.Models.Diagnostics;

namespace UXAV.AVnetCore.WebScripting.InternalApi
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