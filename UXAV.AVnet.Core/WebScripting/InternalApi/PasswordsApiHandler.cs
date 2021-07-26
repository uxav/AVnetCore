using System;
using UXAV.AVnet.Core.Config;
using UXAV.Logging;

namespace UXAV.AVnet.Core.WebScripting.InternalApi
{
    public class PasswordsApiHandler : ApiRequestHandler
    {
        public PasswordsApiHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request)
        {
        }

        public PasswordsApiHandler(WebScriptingServer server, WebScriptingRequest request, bool suppressLogging)
            : base(server, request, suppressLogging)
        {
        }

        [SecureRequest]
        public void Get()
        {
            try
            {
                var passwords = ConfigManager.PasswordsGetAll();
                WriteResponse(passwords);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                HandleError(e);
            }
        }
    }
}