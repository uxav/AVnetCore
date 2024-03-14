using System;
using System.IO;
using System.Reflection;

namespace UXAV.AVnet.Core.WebScripting.StaticFiles
{
    internal class InternalFileHandler : FileHandlerBase
    {
        public InternalFileHandler(WebScriptingServer server, WebScriptingRequest request) : base(server, request)
        {
        }

        protected override string RootFilePath => GetType().Namespace;

        protected override Stream GetResourceStream(Assembly assembly, string fileName)
        {
            SetCacheTime(TimeSpan.FromHours(1));
            return base.GetResourceStream(assembly, fileName);
        }
    }
}