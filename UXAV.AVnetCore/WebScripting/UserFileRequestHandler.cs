using UXAV.AVnetCore.Models;

namespace UXAV.AVnetCore.WebScripting
{
    public class UserFileRequestHandler : FileHandlerBase
    {
        public UserFileRequestHandler(WebScriptingServer server, WebScriptingRequest request) : base(server, request)
        {
        }

        protected override string RootFilePath => SystemBase.ProgramUserDirectory;
    }
}