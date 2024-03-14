using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UXAV.Logging;

namespace UXAV.AVnet.Core.WebScripting
{
    public abstract class FileHandlerBase : RequestHandler
    {
        protected FileHandlerBase(WebScriptingServer server, WebScriptingRequest request) : base(server, request)
        {
        }

        protected abstract string RootFilePath { get; }

        public virtual void Get()
        {
            try
            {
                Logger.Debug("Looking for file resource: {0}", Request.RoutePatternArgs["filepath"]);
                var stream = GetResourceStream(Assembly.GetExecutingAssembly(), Request.RoutePatternArgs["filepath"]);
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

        protected void SetCacheTime(TimeSpan time)
        {
            Response.Headers.Add("Cache-Control", $"public, max-age={time.TotalSeconds}");
        }

        protected virtual Stream GetResourceStream(Assembly assembly, string fileName)
        {
            var pathSlash = Path.DirectorySeparatorChar.ToString();
            if (RootFilePath.Contains(pathSlash))
            {
                var filePath = RootFilePath + Path.DirectorySeparatorChar +
                               fileName.Replace('/', Path.DirectorySeparatorChar);
#if DEBUG
                Logger.Debug($"Looking for file: {filePath}");
#endif
                if (!File.Exists(filePath))
                {
                    return null;
                }
                var fileInfo = new FileInfo(filePath);
#if DEBUG
                Logger.Debug($"File found: {fileInfo.FullName}");
#endif
                Response.ContentType = MimeTypes.GetMimeType(fileInfo.Extension);
                Response.Headers.Add("Last-Modified", fileInfo.LastWriteTime.ToUniversalTime().ToString("R"));
                return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }

            if (RootFilePath.Contains("."))
            {
                var pathMatch = Regex.Match(fileName, @"^\/?(.*(?=\/)\/)?([\/\w\.\-\[\]\(\)\x20]+)$");
                if (!pathMatch.Success) return null;

                var fPath = pathMatch.Groups[1].Value;
                var fName = pathMatch.Groups[2].Value;
                fPath = Regex.Replace(fPath, @"[\x20\[\]]", "_");
                fPath = Regex.Replace(fPath, @"\/", ".");

                var resourcePath = RootFilePath + "." + fPath + fName;
#if DEBUG
                Logger.Debug("Looking for resource stream: {0}", resourcePath);
#endif
                Response.ContentType =
                    MimeTypes.GetMimeType(Regex.Match(resourcePath, @".+(\.\w+)$").Groups[1].Value);
                try
                {
#if DEBUG
                    foreach (var resourceName in assembly.GetManifestResourceNames())
                    {
                        Logger.Debug("Possible resource: {0}", resourceName);
                    }
#endif
                    var result = assembly.GetManifestResourceStream(resourcePath);
                    if (result != null)
                    {
#if DEBUG
                        Logger.Debug("Resource File Found");
#endif
                        return result;
                    }
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }
    }
}