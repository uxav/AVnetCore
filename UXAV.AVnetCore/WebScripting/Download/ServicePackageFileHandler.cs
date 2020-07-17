using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Crestron.SimplSharp;
using UXAV.AVnetCore.Config;
using UXAV.AVnetCore.Models;
using UXAV.Logging;

namespace UXAV.AVnetCore.WebScripting.Download
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
                Logger.Highlight("Get()");
                Logger.Log("Creating memory stream");
                var zipStream = new MemoryStream();
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                {
                    var config = ConfigManager.GetConfigStream();
                    if (config != null)
                    {
                        var configEntry = archive.CreateEntry("ConfigManager/config.json");
                        using (var entryStream = configEntry.Open())
                        {
                            config.CopyTo(entryStream);
                        }
                    }

                    FileInfo[] files;

                    try
                    {
                        var userFolder = new DirectoryInfo(SystemBase.ProgramUserDirectory);
                        files = userFolder.GetFiles();

                        foreach (var fileInfo in files)
                        {
                            var entry = archive.CreateEntry("User/" + fileInfo.Name);
                            using (var stream = entry.Open())
                            {
                                fileInfo.OpenRead().CopyTo(stream);
                            }
                        }
                    }
                    catch
                    {
                        Logger.Warn("Error getting files in {0} for report", SystemBase.ProgramUserDirectory);
                    }

                    try
                    {
                        Logger.Log("Zipping files from " + SystemBase.ProgramNvramDirectory);
                        var nvramFolder = new DirectoryInfo(SystemBase.ProgramNvramDirectory);
                        files = nvramFolder.GetFiles("*", SearchOption.AllDirectories);

                        foreach (var fileInfo in files)
                        {
                            Logger.Log("Creating zip entry for " + fileInfo.FullName);
                            var entry = archive.CreateEntry(fileInfo.FullName);
                            using (var entryStream = entry.Open())
                            {
                                fileInfo.OpenRead().CopyTo(entryStream);
                            }
                        }
                    }
                    catch
                    {
                        Logger.Warn("Error getting files in {0} for report", SystemBase.ProgramNvramDirectory);
                    }

                    var infoEntry = archive.CreateEntry("systeminfo.txt");
                    using (var infoStream = new StreamWriter(infoEntry.Open()))
                    {
                        infoStream.WriteLine($"Time {DateTime.Now}");
                        infoStream.WriteLine($"TimeZone {CrestronEnvironment.GetTimeZone().Formatted}");
                    }

                    try
                    {
                        var loggerEntry = archive.CreateEntry("Logs/" + "logger.txt");
                        using (var stream = new StreamWriter(loggerEntry.Open()))
                        {
                            foreach (var message in Logger.History)
                            {
                                stream.WriteLine(message);
                            }
                        }
                    }
                    catch
                    {
                        Logger.Warn("Error creating logger.txt in report");
                    }

                    try
                    {
                        switch (CrestronEnvironment.DevicePlatform)
                        {
                            case eDevicePlatform.Server:
                            {
                                var logFolder = new DirectoryInfo("/var/log/crestron");
                                var logFiles = logFolder.GetFiles($"*{InitialParametersClass.RoomId}*.log").ToList();
                                logFiles.AddRange(logFolder.GetFiles("crestron.log"));

                                foreach (var fileInfo in logFiles)
                                {
                                    var logEntry = archive.CreateEntry("Logs/" + fileInfo.Name);
                                    using (var entryStream = logEntry.Open())
                                    {
                                        fileInfo.OpenRead().CopyTo(entryStream);
                                    }
                                }

                                break;
                            }
                            case eDevicePlatform.Appliance:
                            {
                                var logFolder = new DirectoryInfo("/logs");
                                var logFiles = logFolder.EnumerateFiles("*.*", SearchOption.AllDirectories);
                                foreach (var fileInfo in logFiles)
                                {
                                    try
                                    {
                                        var logEntry = archive.CreateEntry("Logs/" + fileInfo.FullName);
                                        using (var entryStream = logEntry.Open())
                                        {
                                            fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read)
                                                .CopyTo(entryStream);
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Logger.Error(e);
                                    }
                                }

                                break;
                            }
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }
                }
                Response.Headers.Add("Access-Control-Expose-Headers", "Content-Disposition");
                Response.Headers.Add("Content-Disposition",
                    $"attachment; filename=\"app_report_{InitialParametersClass.RoomId}_{DateTime.Now:yyyyMMddTHHmmss}.zip\"");

                Logger.Log("Generated zip package, {0} bytes", zipStream.Length);

                Response.ContentType = global::System.Web.MimeMapping.GetMimeMapping(".zip");
                Response.Headers.Add("Content-Length", zipStream.Length.ToString());

                var headerContents = Response.Headers.Cast<string>().Aggregate(string.Empty,
                    (current, header) => current + $"{Environment.NewLine}{header}: {Response.Headers[header]}");

                Logger.Log("Response Headers:" + headerContents);

                Request.Response.Write(zipStream.GetCrestronStream(), true);
            }
            catch (Exception e)
            {
                HandleError(e);
            }
        }
    }
}