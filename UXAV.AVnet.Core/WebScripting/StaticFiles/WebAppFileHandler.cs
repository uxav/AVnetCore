using System;
using System.Reflection;
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

        public override void Get()
        {
            try
            {
                var path = "index.html";
                if (Request.RoutePatternArgs.ContainsKey("filepath")) path = Request.RoutePatternArgs["filepath"];
#if DEBUG
                Logger.Debug("Looking for file resource: {0}", path);
#endif
                var stream = GetResourceStream(Assembly.GetExecutingAssembly(), path);
                if (stream == null)
                {
#if DEBUG
                    Logger.Debug("File not found, defaulting to index.html !");
#endif
                    stream = GetResourceStream(Assembly.GetExecutingAssembly(), "index.html");
                }
                //Logger.Debug("Stream = " + stream);
                if (stream == null)
                {
                    HandleNotFound();
                    return;
                }

                Response.Write(stream.GetCrestronStream(), true);
            }
            catch (Exception e)
            {
                HandleError(e);
            }
        }
    }
}