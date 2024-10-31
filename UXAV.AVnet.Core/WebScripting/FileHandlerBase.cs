using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using UXAV.Logging;

namespace UXAV.AVnet.Core.WebScripting
{
    public abstract class FileHandlerBase : RequestHandler
    {
        protected FileHandlerBase(WebScriptingServer server, WebScriptingRequest request) : base(server, request)
        {
        }

        protected abstract string RootFilePath { get; }

        public virtual async Task Get()
        {
            try
            {
                Logger.Debug("Looking for file resource: {0}", Request.RoutePatternArgs["filepath"]);
                var file = GetFile(Request.RoutePatternArgs["filepath"]);
                await WriteFileAsync(file);
            }
            catch (Exception e)
            {
                await HandleErrorAsync(e);
            }
        }

        protected async Task WriteFileAsync(IFileInfo file)
        {
            var extension = Path.GetExtension(file.Name);
            var mimeType = MimeTypes.GetMimeType(extension);
            Response.ContentType = mimeType;
            await Response.SendFileAsync(file);
        }

        protected void SetCacheTime(TimeSpan time)
        {
            Response.Headers.Append("Cache-Control", $"public, max-age={time.TotalSeconds}");
        }

        protected virtual IFileInfo GetFile(string fileName)
        {
            var pathSlash = Path.DirectorySeparatorChar.ToString();
            var filePath = RootFilePath + Path.DirectorySeparatorChar + fileName.Replace('/', Path.DirectorySeparatorChar);
#if DEBUG
            Logger.Debug($"Looking for file: {filePath}");
#endif
            if (!File.Exists(filePath))
            {
                return null;
            }
            var fileInfo = new FileInfo(filePath);
            return new PhysicalFileInfo(fileInfo);
        }
    }
}