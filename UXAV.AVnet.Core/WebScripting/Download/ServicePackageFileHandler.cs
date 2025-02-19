using System;
using System.Linq;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using Microsoft.AspNetCore.Http;
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
                    Response.Headers.Append("X-App-CloudUploadUrl", CloudConnector.LogsUploadUrl);
                Response.Headers.Append("Access-Control-Expose-Headers", "Content-Disposition, X-App-CloudUploadUrl");
                var fileName = $"app_report_{InitialParametersClass.RoomId}_{DateTime.Now:yyyyMMddTHHmmss}.zip";
                Response.Headers.Append("Content-Disposition",
                    $"attachment; filename=\"{fileName}\"");

                Logger.Log("Generated zip package, {0} bytes", zipStream.Length);

                Response.ContentType = MimeTypes.GetMimeType(fileName);
                Response.Headers.Append("Content-Length", zipStream.Length.ToString());

                var headerContents = Response.Headers.Aggregate(string.Empty,
                    (current, header) => current + $"{Environment.NewLine}{header.Key}: {header.Value}");

                Logger.Debug("Response Headers:" + headerContents);

                var buffer = new byte[1024];
                int bytesRead = 0;
                zipStream.Position = 0;
                while ((bytesRead = zipStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    await Response.Body.WriteAsync(buffer, 0, bytesRead);
                }
            }
            catch (Exception e)
            {
                await HandleErrorAsync(e);
            }
        }
    }
}