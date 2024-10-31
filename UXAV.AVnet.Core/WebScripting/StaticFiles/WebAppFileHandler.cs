using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using UXAV.AVnet.Core.Models;
using UXAV.Logging;

namespace UXAV.AVnet.Core.WebScripting.StaticFiles
{
    internal class WebAppFileHandler : FileHandlerBase
    {
        public WebAppFileHandler(WebScriptingServer server, WebScriptingRequest request) : base(server, request)
        {
        }

        protected override string RootFilePath => SystemBase.ProgramApplicationDirectory + "/webapp";

        public override async Task Get()
        {
            try
            {
                var path = "index.html";
                if (Request.RoutePatternArgs.ContainsKey("filepath")) path = Request.RoutePatternArgs["filepath"];
#if DEBUG
                Logger.Debug("Looking for file resource: {0}", path);
#endif
                var file = GetFile(path);
                if (file == null)
                {
#if DEBUG
                    Logger.Debug("File not found, defaulting to index.html !");
#endif
                    file = GetFile("index.html");
                }
                if (file == null)
                {
                    await HandleNotFoundAsync();
                    return;
                }

                await WriteFileAsync(file);
            }
            catch (Exception e)
            {
                await HandleErrorAsync(e);
            }
        }
    }
}