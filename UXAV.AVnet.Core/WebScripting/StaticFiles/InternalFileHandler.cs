using System;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.Reflection;

namespace UXAV.AVnet.Core.WebScripting.StaticFiles
{
    public class InternalFileHandler : FileHandlerBase
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