using System;
using System.Collections.Generic;
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

        private static long WriteFile(string path, int chunkSequence, Stream data)
        {
            var mode = chunkSequence > 0 ? FileMode.Append : FileMode.OpenOrCreate;

            using (var file = File.Open(path, mode, FileAccess.Write, FileShare.None))
            {
                data.CopyTo(file);
                return file.Length;
            }
        }

        public void Post()
        {
            try
            {
                var content = new StreamContent(Request.InputStream.GetNormalStream());
                content.Headers.ContentType = MediaTypeHeaderValue.Parse(Request.ContentType);
                var data = content.ReadAsMultipartAsync().Result;
                var results = new Dictionary<string, long>();
                switch (Request.RoutePatternArgs["fileType"])
                {
                    case "program":
                        foreach (var httpContent in data.Contents)
                        {
                            var name = httpContent.Headers.ContentDisposition.Name.Trim('\"');
                            var fileName = httpContent.Headers.ContentDisposition.FileName.Trim('\"');
                            if (!Regex.IsMatch(fileName, @"[\w\-\[\]\(\)\x20]+\.cpz"))
                            {
                                Logger.Warn($"File: \"{fileName}\" is not a valid cpz file name");
                                HandleError(406, "Not Acceptable",
                                    "One or more files did not match the required format");
                                return;
                            }

                            var chunkSequence = 0;
                            if (Request.Query["chunked"] != null)
                            {
                                // name should be chunk_1 etc
                                chunkSequence = int.Parse(name.Substring(6, name.Length - 6));
                                Logger.Debug($"Received chunk {chunkSequence:D3} of {fileName}");
                            }

                            var path = SystemBase.ProgramApplicationDirectory + "/" + fileName;

                            var size = WriteFile(path, chunkSequence, httpContent.ReadAsStreamAsync().Result);
                            results[fileName] = size;
                        }

                        break;
                    case "nvram":
                        foreach (var httpContent in data.Contents)
                        {
                            var name = httpContent.Headers.ContentDisposition.Name.Trim('\"');
                            var fileName = httpContent.Headers.ContentDisposition.FileName.Trim('\"');
                            var chunkSequence = 0;
                            if (Request.Query["chunked"] != null)
                            {
                                // name should be chunk_1 etc
                                chunkSequence = int.Parse(name.Substring(6, name.Length - 6));
                                Logger.Debug($"Received chunk {chunkSequence:D3} of {fileName}");
                            }

                            var path = SystemBase.ProgramNvramAppInstanceDirectory + "/" + fileName;

                            var size = WriteFile(path, chunkSequence, httpContent.ReadAsStreamAsync().Result);
                            results[fileName] = size;
                        }

                        break;
                    default:
                        HandleError(400, "Bad Request",
                            $"Invalid fileType: {Request.RoutePatternArgs["fileType"]}");
                        return;
                }

                WriteResponse(results);
            }
            catch (Exception e)
            {
                HandleError(e);
            }
        }
    }
}