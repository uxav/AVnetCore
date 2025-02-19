using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using UXAV.Logging;

namespace UXAV.AVnet.Core.WebScripting.InternalApi
{
    internal class PlogApiHandler : ApiRequestHandler
    {
        public PlogApiHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request)
        {
        }

        [SecureRequest]
        public async void Get()
        {
            try
            {
                IEnumerable<FileInfo> files;
                var logs = new List<object>();
                switch (CrestronEnvironment.DevicePlatform)
                {
                    case eDevicePlatform.Server:
                        {
                            var logFolder = new DirectoryInfo("/var/log/crestron");
                            files = logFolder.GetFiles($"*{InitialParametersClass.RoomId}*.log");
                            break;
                        }
                    case eDevicePlatform.Appliance:
                        {
                            var logFolder = new DirectoryInfo("/logs/CurrentBoot");
                            files = logFolder.EnumerateFiles("Crestron_*.log", SearchOption.TopDirectoryOnly);
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (Request.Query.ContainsKey("raw"))
                {
                    var contents = string.Empty;
                    foreach (var fileInfo in files.OrderBy(f => f.Name))
                    {
                        Logger.Debug($"Reading log file: {fileInfo.FullName}");
                        using (var reader = fileInfo.OpenText())
                        {
                            while (!reader.EndOfStream) contents += reader.ReadToEnd();
                        }
                    }

                    await WriteResponseAsync(contents);
                    return;
                }

                foreach (var fileInfo in files.OrderBy(f => f.Name))
                {
                    Logger.Debug($"Reading log file: {fileInfo.FullName}");
                    using (var reader = fileInfo.OpenText())
                    {
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();
                            if (string.IsNullOrEmpty(line)) continue;
                            var entry = Regex.Match(line,
                                @"^(\w+): ([\w\.]+)(?: *\[App *(\d+)\])? +# +(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) +# +(.+)");
                            if (!entry.Success) continue;
                            logs.Add(new
                            {
                                Level = entry.Groups[1].Value,
                                SubSystem = entry.Groups[2].Value,
                                Time = entry.Groups[4].Value,
                                Message = entry.Groups[5].Value,
                                AppIndex = entry.Groups[3].Value
                            });
                        }
                    }
                }

                await WriteResponseAsync(logs);
            }
            catch (Exception e)
            {
                await HandleErrorAsync(e);
            }
        }
    }
}