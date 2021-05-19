using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Crestron.SimplSharp.CrestronIO;
using UXAV.AVnet.Core.DeviceSupport;

namespace UXAV.AVnet.Core.WebScripting
{
    public class XPanelResourceFileHandler : RequestHandler
    {
        public XPanelResourceFileHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request)
        {
        }

        public void Get()
        {
            try
            {
                var fileName = Request.RoutePatternArgs["filename"];
                var match = Regex.Match(fileName, @"_(\w{2})\.(?:c3p|vtz)$");
                var ipId = uint.Parse(match.Groups[1].Value, NumberStyles.HexNumber);

                if (!CipDevices.ContainsDevice(ipId))
                {
                    HandleNotFound("No devices found with specified IP ID");
                    return;
                }

                var path = CipDevices.GetPathOfVtzFileForXPanel(ipId);
                if (string.IsNullOrEmpty(path))
                {
                    HandleNotFound("No resource path set for specified device");
                    return;
                }

                if (!File.Exists(path))
                {
                    HandleNotFound($"No file found at \"{path}\"");
                    return;
                }

                var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                Response.ContentType = "application/x-zip-compressed";
                Response.Write(stream, true);
            }
            catch (Exception e)
            {
                HandleError(e);
            }
        }
    }
}