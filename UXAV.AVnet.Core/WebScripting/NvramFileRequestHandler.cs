using UXAV.AVnet.Core.Models;

namespace UXAV.AVnet.Core.WebScripting
{
    public class NvramFileRequestHandler : FileHandlerBase
    {
        public NvramFileRequestHandler(WebScriptingServer server, WebScriptingRequest request) : base(server, request)
        {
        }

        protected override string RootFilePath => SystemBase.ProgramNvramDirectory;
    }
}