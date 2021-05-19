using UXAV.AVnet.Core.Models;

namespace UXAV.AVnet.Core.WebScripting
{
    public class UserFileRequestHandler : FileHandlerBase
    {
        public UserFileRequestHandler(WebScriptingServer server, WebScriptingRequest request) : base(server, request)
        {
        }

        protected override string RootFilePath => SystemBase.ProgramUserDirectory;
    }
}