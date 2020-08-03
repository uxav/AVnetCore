using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using UXAV.AVnetCore.Models;
using UXAV.Logging;

namespace UXAV.AVnetCore.WebScripting.InternalApi
{
    public class FileUploadApiHandler : ApiRequestHandler
    {
        public FileUploadApiHandler(WebScriptingServer server, WebScriptingRequest request) : base(server, request)
        {
        }

        public void Post()
        {
            try
            {
                var content = new StreamContent(Request.InputStream.GetNormalStream());
                content.Headers.ContentType = MediaTypeHeaderValue.Parse(Request.ContentType);
                var data = content.ReadAsMultipartAsync().Result;
                switch (Request.RoutePatternArgs["fileType"])
                {
                    case "program":
                        foreach (var httpContent in data.Contents)
                        {
                            var name = httpContent.Headers.ContentDisposition.Name.Trim('\"');
                            if (!Regex.IsMatch(name, @"[\w\-\[\]\(\)\x20]+\.cpz"))
                            {
                                Logger.Warn($"File: \"{name}\" is not a valid cpz file name");
                                continue;
                            }
                            using (var newStream =
                                File.OpenWrite(SystemBase.ProgramApplicationDirectory + "/" + name))
                            {
                                var stream = httpContent.ReadAsStreamAsync().Result;
                                Logger.Debug($"Uploaded {name}, size: {stream.Length}");
                                stream.CopyTo(newStream);
                            }

                            Logger.Highlight($"File uploaded to: {SystemBase.ProgramApplicationDirectory}/{name}");
                            WriteResponse($"CPZ uploaded to: {SystemBase.ProgramApplicationDirectory}/{name}");
                            return;
                        }

                        HandleError(406, "Not Acceptable",
                            "One or more files did not match the required format");
                        return;
                    default:
                        HandleError(400, "Bad Request",
                            $"Invalid fileType: {Request.RoutePatternArgs["fileType"]}");
                        return;
                }
            }
            catch (Exception e)
            {
                HandleError(e);
            }
        }
    }
}