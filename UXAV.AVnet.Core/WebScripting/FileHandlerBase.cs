using System;
using System.Text.RegularExpressions;
using System.Web;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.Reflection;
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

                Response.Write(stream, true);
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

                Logger.Debug($"Looking for file: {filePath}");

                var fileInfo = new FileInfo(filePath);
                Logger.Debug($"File found: {fileInfo.FullName}");
                Response.ContentType = MimeMapping.GetMimeMapping(fileInfo.Extension);
                Response.Headers.Add("Last-Modified", fileInfo.LastWriteTime.ToUniversalTime().ToString("R"));
                try
                {
                    return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                catch (Exception e)
                {
                    if (e is FileNotFoundException)
                        return null;
                    // ReSharper disable once PossibleIntendedRethrow
                    throw e;
                }
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
                Logger.Log("Looking for resource stream: {0}", resourcePath);
                Response.ContentType =
                    MimeMapping.GetMimeMapping(Regex.Match(resourcePath, @".+(\.\w+)$").Groups[1].Value);
                try
                {
                    foreach (var resourceName in assembly.GetManifestResourceNames())
                        Logger.Log("Possible resource: {0}", resourceName);

                    var result = assembly.GetManifestResourceStream(resourcePath);
                    if (result != null)
                    {
                        Logger.Success("Resource File Found");
                        return result;
                    }
                }
                catch
                {
                    Logger.Warn("Resource Not Found");
                    return null;
                }
            }

            return null;
        }
    }
}