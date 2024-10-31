using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UXAV.AVnet.Core.DeviceSupport;
using UXAV.Logging;
using FileInfo = System.IO.FileInfo;

namespace UXAV.AVnet.Core.WebScripting
{
    public class XPanelResourceFileHandler : RequestHandler
    {
        public XPanelResourceFileHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request)
        {
        }

        public async void Get()
        {
            try
            {
                var fileName = Request.RoutePatternArgs["filename"];
                var match = Regex.Match(fileName, @"_(\w{2})\.(?:c3p|vtz)$");
                var ipId = uint.Parse(match.Groups[1].Value, NumberStyles.HexNumber);

                if (!CipDevices.ContainsDevice(ipId))
                {
                    await HandleNotFoundAsync("No devices found with specified IP ID");
                    return;
                }

                var path = CipDevices.GetPathOfVtzFileForXPanel(ipId);
                if (string.IsNullOrEmpty(path))
                {
                    await HandleNotFoundAsync("No resource path set for specified device");
                    return;
                }

                try
                {
                    if (!File.Exists(path))
                    {
                        await HandleNotFoundAsync($"No file found at \"{path}\"");
                        return;
                    }
                }
                catch (FileNotFoundException)
                {
                    Logger.Debug("FileNotFoundException, Looking for full path...");
                    if (File.Exists(path))
                    {
                        var info = new FileInfo(path);
                        path = info.FullName;
                    }

                    Logger.Debug($"Path is {path}");
                }

                var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                Response.ContentType = "application/x-zip-compressed";
                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    var fileBytes = memoryStream.ToArray();
                    await Response.Body.WriteAsync(fileBytes, 0, fileBytes.Length);
                }
            }
            catch (Exception e)
            {
                await HandleErrorAsync(e);
            }
        }
    }
}