using UXAV.AVnetCore.Models;

namespace UXAV.AVnetCore.WebScripting
{
    public class NvramFileRequestHandler : FileHandlerBase
    {
        public NvramFileRequestHandler(WebScriptingServer server, WebScriptingRequest request) : base(server, request)
        {
        }

        protected override string RootFilePath => SystemBase.ProgramNvramDirectory;
    }
}