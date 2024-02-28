using System;
using System.Linq;
using Crestron.SimplSharp;
using UXAV.AVnet.Core.Cloud;
using UXAV.AVnet.Core.Models;
using UXAV.Logging;

namespace UXAV.AVnet.Core.WebScripting.Download
{
    public class ServicePackageFileHandler : RequestHandler
    {
        public ServicePackageFileHandler(WebScriptingServer server, WebScriptingRequest request) : base(server, request)
        {
        }

        [SecureRequest]
        public void Get()
        {
            try
            {
                var zipStream = DiagnosticsArchiveTool.CreateArchiveAsync().Result;
                if (!string.IsNullOrEmpty(CloudConnector.LogsUploadUrl))
                    Response.Headers.Add("X-App-CloudUploadUrl", CloudConnector.LogsUploadUrl);
                Response.Headers.Add("Access-Control-Expose-Headers", "Content-Disposition, X-App-CloudUploadUrl");
                Response.Headers.Add("Content-Disposition",
                    $"attachment; filename=\"app_report_{InitialParametersClass.RoomId}_{DateTime.Now:yyyyMMddTHHmmss}.zip\"");

                Logger.Log("Generated zip package, {0} bytes", zipStream.Length);

                Response.ContentType = MimeKit.MimeTypes.GetMimeType(".zip");
                Response.Headers.Add("Content-Length", zipStream.Length.ToString());

                var headerContents = Response.Headers.Cast<string>().Aggregate(string.Empty,
                    (current, header) => current + $"{Environment.NewLine}{header}: {Response.Headers[header]}");

                Logger.Debug("Response Headers:" + headerContents);

                Request.Response.Write(zipStream.GetCrestronStream(), true);
            }
            catch (Exception e)
            {
                HandleError(e);
            }
        }
    }
}