using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime;
using System.Text.RegularExpressions;
using UXAV.AVnet.Core.Models;
using UXAV.Logging;

namespace UXAV.AVnet.Core.WebScripting.InternalApi
{
    public class FileUploadApiHandler : ApiRequestHandler
    {
        public static readonly Dictionary<string, Dictionary<int, MemoryStream>> UploadStreams =
            new Dictionary<string, Dictionary<int, MemoryStream>>();
        public static readonly Dictionary<string, long> UploadProgress = new Dictionary<string, long>();

        public FileUploadApiHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request, true)
        {
        }

        private static long WriteFile(string path, int chunkSequence, Stream data)
        {
            if (chunkSequence == 0)
            {
                if (UploadStreams.ContainsKey(path))
                {
                    foreach (var s in UploadStreams[path].Values)
                    {
                        s.Dispose();
                    }
                }
                UploadStreams[path] = new Dictionary<int, MemoryStream>();
                UploadProgress[path] = 0;
            }

            var stream = new MemoryStream();
            UploadStreams[path][chunkSequence] = stream;

            data.CopyTo(stream);
            UploadProgress[path] += stream.Length;
            return UploadProgress[path];
        }

        private static long SaveToDisk(string path)
        {
            using (var file = File.Create(path))
            {
                var streams = UploadStreams[path].OrderBy(i => i.Key).Select(i => i.Value).ToArray();
                var chunk = 0;
                foreach (var memoryStream in streams)
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    memoryStream.CopyTo(file);
                    UploadStreams[path][chunk].Dispose();
                    UploadStreams[path][chunk] = null;
                    UploadStreams[path].Remove(chunk);
                    chunk++;
                }

                UploadStreams.Remove(path);
                Logger.Debug("Starting garbage collection...");
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect();
                Logger.Debug("Completed garbage collection!");
                return file.Length;
            }
        }

        [SecureRequest]
        public void Post()
        {
            try
            {
                var content = new StreamContent(Request.InputStream.GetNormalStream());
                content.Headers.ContentType = MediaTypeHeaderValue.Parse(Request.ContentType);
                var data = content.ReadAsMultipartAsync().Result;
                var results = new Dictionary<string, object>();
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

                            var path = SystemBase.ProgramApplicationDirectory + "/" + fileName;

                            if (name == "end")
                            {
                                results[fileName] = SaveToDisk(path);
                            }
                            else
                            {
                                var chunkSequence = 0;
                                if (Request.Query["chunked"] != null)
                                {
                                    // name should be chunk_1 etc
                                    chunkSequence = int.Parse(name.Substring(6, name.Length - 6));
                                    //Logger.Debug($"Received chunk {chunkSequence:D3} of {fileName}");
                                }

                                var size = WriteFile(path, chunkSequence, httpContent.ReadAsStreamAsync().Result);
                                results[fileName] = size;
                            }
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