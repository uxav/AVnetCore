using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UXAV.AVnet.Core.Models;
using UXAV.Logging;

namespace UXAV.AVnet.Core.WebScripting.InternalApi
{
    internal class FileUploadApiHandler : ApiRequestHandler
    {
        public static readonly Dictionary<string, Dictionary<int, MemoryStream>> UploadStreams =
            new Dictionary<string, Dictionary<int, MemoryStream>>();

        public static readonly Dictionary<string, long> UploadProgress = new Dictionary<string, long>();

        public FileUploadApiHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request, true)
        {
        }

        private static async Task<long> WriteFileAsync(string path, int chunkSequence, Stream data)
        {
            if (chunkSequence == 0)
            {
                if (UploadStreams.ContainsKey(path))
                    foreach (var s in UploadStreams[path].Values)
                        s.Dispose();
                UploadStreams[path] = [];
                UploadProgress[path] = 0;
            }

            var stream = new MemoryStream();
            UploadStreams[path][chunkSequence] = stream;

            await data.CopyToAsync(stream);
            UploadProgress[path] += stream.Length;
            return UploadProgress[path];
        }

        private static async Task<long> SaveToDiskAsync(string path)
        {
            using var file = File.Create(path);
            var streams = UploadStreams[path].OrderBy(i => i.Key).Select(i => i.Value).ToArray();
            var chunk = 0;
            foreach (var memoryStream in streams)
            {
                memoryStream.Seek(0, SeekOrigin.Begin);
                await memoryStream.CopyToAsync(file);
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

        [SecureRequest]
        public async void Post()
        {
            try
            {
                var content = new StreamContent(Request.InputStream);
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
                                await HandleErrorAsync(406, "One or more files did not match the required format");
                                return;
                            }

                            var path = SystemBase.ProgramApplicationDirectory + "/" + fileName;

                            if (name == "end")
                            {
                                results[fileName] = await SaveToDiskAsync(path);
                            }
                            else
                            {
                                var chunkSequence = 0;
                                if (!string.IsNullOrEmpty(Request.Query["chunked"]))
                                    // name should be chunk_1 etc
                                    chunkSequence = int.Parse(name.Substring(6, name.Length - 6));
                                //Logger.Debug($"Received chunk {chunkSequence:D3} of {fileName}");

                                var size = await WriteFileAsync(path, chunkSequence, await httpContent.ReadAsStreamAsync());
                                results[fileName] = size;
                            }
                        }

                        break;
                    default:
                        await HandleErrorAsync(400, $"Invalid fileType: {Request.RoutePatternArgs["fileType"]}");
                        return;
                }

                await WriteResponseAsync(results);
            }
            catch (Exception e)
            {
                await HandleErrorAsync(e);
            }
        }
    }
}