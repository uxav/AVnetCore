using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using UXAV.AVnet.Core.Models;
using UXAV.Logging;

namespace UXAV.AVnet.Core.WebScripting.InternalApi
{
    internal class UploadedFilesApiHandler : ApiRequestHandler
    {
        public UploadedFilesApiHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request)
        {
        }

        [SecureRequest]
        public async Task Get()
        {
            var files = new List<object>();
            try
            {
                switch (Request.RoutePatternArgs["fileType"])
                {
                    case "program":
                        var appDirectory = new DirectoryInfo(SystemBase.ProgramApplicationDirectory);
                        var cpzFiles = appDirectory.GetFiles("*.cpz", SearchOption.TopDirectoryOnly);
                        foreach (var fileInfo in cpzFiles)
                            try
                            {
                                var info = ProgramFileVersion.Get(fileInfo.FullName);
                                files.Add(new
                                {
                                    FileInfo = new
                                    {
                                        Name = fileInfo.Name,
                                        LastWriteTime = fileInfo.LastWriteTime,
                                        Length = fileInfo.Length
                                    },
                                    Size = Tools.PrettyByteSize(fileInfo.Length, 1),
                                    ProgramInfo = info
                                });
                            }
                            catch (Exception e)
                            {
                                Logger.Error(e);
                            }

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

            await WriteResponseAsync(files);
        }

        public void Options()
        {
            Response.Headers.Append("Access-Control-Allow-Methods", "GET, OPTIONS, DELETE");
            Response.StatusCode = 204;
        }

        [SecureRequest]
        public async void Delete()
        {
            Logger.Highlight($"File Delete Request: {Request.PathAndQueryString}");
            var fileName = Request.Query["file"];
            switch (Request.RoutePatternArgs["fileType"])
            {
                case "program":
                    if (File.Exists(SystemBase.ProgramApplicationDirectory + "/" + fileName))
                        File.Delete(SystemBase.ProgramApplicationDirectory + "/" + fileName);

                    await WriteResponseAsync(true);
                    return;
                case "nvram":
                    if (File.Exists(SystemBase.ProgramNvramAppInstanceDirectory + "/" + fileName))
                        File.Delete(SystemBase.ProgramNvramAppInstanceDirectory + "/" + fileName);

                    await WriteResponseAsync(true);
                    return;
            }

            await WriteResponseAsync(false);
        }
    }
}