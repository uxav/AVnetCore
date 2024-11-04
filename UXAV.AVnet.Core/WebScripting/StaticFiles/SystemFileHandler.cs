using System;
using System.Threading.Tasks;
using UXAV.Logging;

namespace UXAV.AVnet.Core.WebScripting.StaticFiles
{
    internal class SystemFileHandler : FileHandlerBase
    {
        public SystemFileHandler(WebScriptingServer server, WebScriptingRequest request) : base(server, request)
        {
        }

        protected override string RootFilePath => "";

        [SecureRequest]
        public override async Task Get()
        {
            try
            {
                var filePath = Request.RoutePatternArgs["filepath"];
                if (filePath.Contains("../"))
                {
                    throw new UnauthorizedAccessException("Invalid file path.");
                }

                Logger.Debug("Looking for system file resource: {0}", filePath);
                var file = GetFile(filePath);
                await WriteFileAsync(file);
            }
            catch (Exception e)
            {
                await HandleErrorAsync(e);
            }
        }
    }
}