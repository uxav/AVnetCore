using System;
using System.Collections.Generic;
using System.IO;
using UXAV.AVnetCore.Models;
using UXAV.Logging;

namespace UXAV.AVnetCore.WebScripting.InternalApi
{
    public class UploadedFilesApiHandler : ApiRequestHandler
    {
        public UploadedFilesApiHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request)
        {
        }

        [SecureRequest]
        public void Get()
        {
            var files = new List<FileInfo>();
            try
            {
                switch (Request.RoutePatternArgs["fileType"])
                {
                    case "program":
                        var appDirectory = new DirectoryInfo(SystemBase.ProgramApplicationDirectory);
                        files.AddRange(appDirectory.GetFiles("*.cpz", SearchOption.TopDirectoryOnly));
                        break;
                    case "nvram":
                        var nvramDirectory = new DirectoryInfo(SystemBase.ProgramNvramAppInstanceDirectory);
                        files.AddRange(nvramDirectory.GetFiles("*", SearchOption.TopDirectoryOnly));
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            WriteResponse(files);
        }

        public void Options()
        {
            Response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS, DELETE");
            Response.StatusCode = 204;
            Response.StatusDescription = "No Content";
            Response.Flush();
        }

        [SecureRequest]
        public void Delete()
        {
            Logger.Highlight($"File Delete Request: {Request.PathAndQueryString}");
            var fileName = Request.Query["file"];
            switch (Request.RoutePatternArgs["fileType"])
            {
                case "program":
                    if (File.Exists(SystemBase.ProgramApplicationDirectory + "/" + fileName))
                    {
                        File.Delete(SystemBase.ProgramApplicationDirectory + "/" + fileName);
                    }

                    WriteResponse(true);
                    return;
                case "nvram":
                    if (File.Exists(SystemBase.ProgramNvramAppInstanceDirectory + "/" + fileName))
                    {
                        File.Delete(SystemBase.ProgramNvramAppInstanceDirectory + "/" + fileName);
                    }

                    WriteResponse(true);
                    return;
            }

            WriteResponse(false);
        }
    }
}