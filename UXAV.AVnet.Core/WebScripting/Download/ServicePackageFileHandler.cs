using System;
using System.Linq;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using UXAV.AVnet.Core.Cloud;
using UXAV.AVnet.Core.Models;
using UXAV.Logging;

namespace UXAV.AVnet.Core.WebScripting.Download
{
    internal class ServicePackageFileHandler : RequestHandler
    {
        public ServicePackageFileHandler(WebScriptingServer server, WebScriptingRequest request) : base(server, request)
        {
        }

        [SecureRequest]
        public async Task Get()
        {
            try
            {
                var zipStream = await DiagnosticsArchiveTool.CreateArchiveAsync();
                if (!string.IsNullOrEmpty(CloudConnector.LogsUploadUrl))
                    Response.Headers.Add("X-App-CloudUploadUrl", CloudConnector.LogsUploadUrl);
                Response.Headers.Add("Access-Control-Expose-Headers", "Content-Disposition, X-App-CloudUploadUrl");
                var fileName = $"app_report_{InitialParametersClass.RoomId}_{DateTime.Now:yyyyMMddTHHmmss}.zip";
                Response.Headers.Add("Content-Disposition",
                    $"attachment; filename=\"{fileName}\"");

                Logger.Log("Generated zip package, {0} bytes", zipStream.Length);

                Response.ContentType = MimeTypes.GetMimeType(fileName);
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