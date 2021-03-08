using System;
using System.Globalization;
using Crestron.SimplSharp.CrestronIO;
using UXAV.AVnetCore.DeviceSupport;

namespace UXAV.AVnetCore.WebScripting
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
                var ipId = uint.Parse(Request.RoutePatternArgs["ipid"], NumberStyles.HexNumber);
                var extension = Request.RoutePatternArgs["extension"];

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