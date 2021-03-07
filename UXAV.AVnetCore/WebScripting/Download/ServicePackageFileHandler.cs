using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
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
                Logger.Log("Creating zip archive for service package");
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
                        Logger.Debug("Zipping files from " + SystemBase.ProgramNvramDirectory);
                        var nvramFolder = new DirectoryInfo(SystemBase.ProgramNvramDirectory);
                        files = nvramFolder.GetFiles("*", SearchOption.AllDirectories);

                        foreach (var fileInfo in files)
                        {
                            Logger.Debug("Creating zip entry for " + fileInfo.FullName);
                            var zipPath = Regex.Replace(fileInfo.FullName, "^" + SystemBase.ProgramNvramDirectory + "/",
                                "");
                            var entry = archive.CreateEntry("NVRAM/" + zipPath);
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
                        var appNumber = InitialParametersClass.ApplicationNumber;
                        var commands = new[]
                        {
                            "hostname",
                            "mycrestron",
                            "showlicense",
                            "osd",
                            "uptime",
                            "ver -v",
                            "ver all",
                            "uptime",
                            "time",
                            "timezone",
                            "sntp",
                            "showhw",
                            "ipconfig /all",
                            "progregister",
                            "progcomments:1",
                            "progcomments:2",
                            "progcomments:3",
                            "progcomments:4",
                            "progcomments:5",
                            "progcomments:6",
                            "progcomments:7",
                            "progcomments:8",
                            "progcomments:9",
                            "progcomments:10",
                            "proguptime:1",
                            "proguptime:2",
                            "proguptime:3",
                            "proguptime:4",
                            "proguptime:5",
                            "proguptime:6",
                            "proguptime:7",
                            "proguptime:8",
                            "proguptime:9",
                            "proguptime:10",
                            "ssptasks:1",
                            "ssptasks:2",
                            "ssptasks:3",
                            "ssptasks:4",
                            "ssptasks:5",
                            "ssptasks:6",
                            "ssptasks:7",
                            "ssptasks:8",
                            "ssptasks:9",
                            "ssptasks:10",
                            "appstat -p:" + appNumber,
                            "taskstat",
                            "ramfree",
                            "cpuload",
                            "cpuload",
                            "cpuload",
                            "showportmap -all",
                            "ramfree",
                            "showdiskinfo",
                            "ethwdog",
                            "iptable -p:all -t",
                            "who",
                            "netstat",
                            "threadpoolinfo",
                            "autodiscover query tableformat",
                            "reportcresnet",
                        };

                        foreach (var command in commands)
                        {
                            infoStream.WriteLine("Ran Console Command: {0}", command);
                            var response = string.Empty;
                            CrestronConsole.SendControlSystemCommand(command, ref response);
                            infoStream.WriteLine(response);
                            infoStream.WriteLine(string.Empty);
                        }
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
                                    Logger.Debug("Creating zip entry for " + fileInfo.FullName);
                                    var zipPath = Regex.Replace(fileInfo.FullName, "^/var/log/crestron/", "");
                                    var logEntry = archive.CreateEntry("Logs/" + zipPath);
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
                                var logFiles = logFolder.EnumerateFiles("*", SearchOption.AllDirectories);
                                foreach (var fileInfo in logFiles)
                                {
                                    try
                                    {
                                        Logger.Debug("Creating zip entry for " + fileInfo.FullName);
                                        var zipPath = Regex.Replace(fileInfo.FullName, "^/logs/", "");
                                        var logEntry = archive.CreateEntry("Logs/" + zipPath);
                                        using (var entryStream = logEntry.Open())
                                        {
                                            fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read)
                                                .CopyTo(entryStream);
                                        }
                                    }
                                    catch (UnauthorizedAccessException)
                                    {
                                        Logger.Warn($"No access to the file: {fileInfo.FullName}");
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