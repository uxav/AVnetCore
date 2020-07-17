using System;
using Crestron.SimplSharp.Reflection;
using UXAV.AVnetCore.Models;
using UXAV.Logging;

namespace UXAV.AVnetCore.WebScripting.StaticFiles
{
    public class WebAppFileHandler : FileHandlerBase
    {
        public WebAppFileHandler(WebScriptingServer server, WebScriptingRequest request) : base(server, request)
        {
        }

        protected override string RootFilePath => SystemBase.ProgramHtmlDirectory + "/dist/app";

        public override void Get()
        {
            try
            {
                var path = "index.html";
                if (Request.RoutePatternArgs.ContainsKey("filepath"))
                {
                    path = Request.RoutePatternArgs["filepath"];
                }

                Logger.Log("Looking for file resource: {0}", path);
                var stream = GetResourceStream(Assembly.GetExecutingAssembly(), path);
                if (stream == null)
                {
                    Logger.Log("File not found, defaulting to index.html !");
                    stream = GetResourceStream(Assembly.GetExecutingAssembly(), "index.html");
                }

                Logger.Log("Stream = " + stream);
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