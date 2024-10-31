using System;
using UXAV.AVnet.Core.Models.Diagnostics;

namespace UXAV.AVnet.Core.WebScripting.InternalApi
{
    internal class DiagnosticsApiHandler : ApiRequestHandler
    {
        public DiagnosticsApiHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request)
        {
        }

        public async void Get()
        {
            try
            {
                var messages = DiagnosticService.GetMessages();
                await WriteResponseAsync(messages);
            }
            catch (Exception e)
            {
                await HandleErrorAsync(e);
            }
        }
    }
}