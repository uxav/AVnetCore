using System;
using Microsoft.Extensions.FileProviders;

namespace UXAV.AVnet.Core.WebScripting.StaticFiles
{
    internal class InternalFileHandler : FileHandlerBase
    {
        public InternalFileHandler(WebScriptingServer server, WebScriptingRequest request) : base(server, request)
        {
        }

        protected override string RootFilePath => GetType().Namespace;

        protected override IFileInfo GetFile(string fileName)
        {
            var file = base.GetFile(fileName);
            if (file != null && file.Exists)
                SetCacheTime(TimeSpan.FromHours(1));
            return file;
        }
    }
}