using System;
using Crestron.SimplSharp.Reflection;
using UXAV.AVnet.Core.Models;
using UXAV.Logging;

namespace UXAV.AVnet.Core.WebScripting.StaticFiles
{
    public class WebAppFileHandler : FileHandlerBase
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
                if (Request.RoutePatternArgs.ContainsKey("filepath"))
                {
                    path = Request.RoutePatternArgs["filepath"];
                }

                //Logger.Debug("Looking for file resource: {0}", path);
                var stream = GetResourceStream(Assembly.GetExecutingAssembly(), path);
                if (stream == null)
                {
                    //Logger.Debug("File not found, defaulting to index.html !");
                    stream = GetResourceStream(Assembly.GetExecutingAssembly(), "index.html");
                }

                //Logger.Debug("Stream = " + stream);
                if (stream == null)
                {
                    HandleNotFound();
                    return;
                }

                Response.Write(stream, true);
            }
            catch (Exception e)
            {
                HandleError(e);
            }
        }
    }
}