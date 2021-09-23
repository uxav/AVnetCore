using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using UXAV.AVnet.Core.Config;
using UXAV.Logging;

namespace UXAV.AVnet.Core.Models
{
    public static class DiagnosticsArchiveTool
    {
        private static readonly Mutex Mutex = new Mutex();

        public static async Task<MemoryStream> CreateArchiveAsync()
        {
            return await Task.Run(() =>
            {
                if (!Mutex.WaitOne(TimeSpan.FromSeconds(10))) throw new TimeoutException("Resource busy");
                Logger.Log("Creating zip archive for service package");

                try
                {
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
                                var zipPath = Regex.Replace(fileInfo.FullName,
                                    "^" + SystemBase.ProgramNvramDirectory + "/",
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
                            switch (CrestronEnvironment.DevicePlatform)
                            {
                                case eDevicePlatform.Appliance:
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

                                    break;
                                }
                                case eDevicePlatform.Server:
                                    infoStream.WriteLine(
                                        $"App running on server platform: {InitialParametersClass.ControllerPromptName}");
                                    infoStream.WriteLine();
                                    infoStream.WriteLine("IP Address: {0}", SystemBase.IpAddress);
                                    infoStream.WriteLine("MAC Address: {0}", SystemBase.MacAddress);
                                    infoStream.WriteLine("FrameworkDescription: {0}",
                                        RuntimeInformation.FrameworkDescription);
                                    infoStream.WriteLine("ProcessArchitecture: {0}",
                                        RuntimeInformation.ProcessArchitecture);
                                    infoStream.WriteLine("OSArchitecture: {0}", RuntimeInformation.OSArchitecture);
                                    infoStream.WriteLine("OSDescription: {0}", RuntimeInformation.OSDescription);
                                    infoStream.WriteLine("Include4.dat Version: {0}",
                                        UxEnvironment.System.Include4DatInfo);
                                    infoStream.WriteLine("ApplicationNumber: {0}",
                                        InitialParametersClass.ApplicationNumber);
                                    infoStream.WriteLine("FirmwareVersion: {0}",
                                        InitialParametersClass.FirmwareVersion);
                                    infoStream.WriteLine("SerialNumber: {0}",
                                        CrestronEnvironment.SystemInfo.SerialNumber);
                                    infoStream.WriteLine("App Info: {0}", SystemBase.AppAssembly.GetName().FullName);
                                    infoStream.WriteLine("{0} Version: {1}", UxEnvironment.Name, UxEnvironment.Version);
                                    infoStream.WriteLine("{0} Assembly Version: {1}", UxEnvironment.Name,
                                        UxEnvironment.AssemblyVersion);
                                    infoStream.WriteLine("App version {0}", SystemBase.AppAssembly.GetName().Version);
                                    infoStream.WriteLine(
                                        $"Program Info states build time as: {UxEnvironment.System.ProgramBuildTime:R}");
                                    infoStream.WriteLine("ProgramIDTag: {0}", InitialParametersClass.ProgramIDTag);
                                    infoStream.WriteLine("Server Room ID: {0}", InitialParametersClass.RoomId);
                                    infoStream.WriteLine("Server Room Name: {0}", InitialParametersClass.RoomName);
                                    infoStream.WriteLine("ProcessId: {0}",
                                        global::System.Diagnostics.Process.GetCurrentProcess().Id);
                                    infoStream.WriteLine("ApplicationNumber: {0}",
                                        InitialParametersClass.ApplicationNumber);
                                    var tz = CrestronEnvironment.GetTimeZone();
                                    infoStream.WriteLine("TimeZone: {0}{1}", tz.Formatted,
                                        tz.InDayLightSavings ? " (DST)" : string.Empty);
                                    infoStream.WriteLine("Location: 🌍 {0},{1}", CrestronEnvironment.Latitude,
                                        CrestronEnvironment.Longitude);
                                    infoStream.WriteLine("Local Time is {0}", DateTime.Now);
                                    infoStream.WriteLine("Universal Time is {0:R}", DateTime.Now.ToUniversalTime());
                                    infoStream.WriteLine("ProgramRootDirectory = {0}", SystemBase.ProgramRootDirectory);
                                    infoStream.WriteLine("ProgramApplicationDirectory = {0}",
                                        SystemBase.ProgramApplicationDirectory);
                                    infoStream.WriteLine("ProgramUserDirectory = {0}", SystemBase.ProgramUserDirectory);
                                    infoStream.WriteLine("ProgramNvramDirectory = {0}",
                                        SystemBase.ProgramNvramDirectory);
                                    infoStream.WriteLine("ProgramHtmlDirectory = {0}", SystemBase.ProgramHtmlDirectory);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
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
                                    var logFiles = logFolder.GetFiles($"*{InitialParametersClass.RoomId}*.log")
                                        .ToList();
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
                        catch (UnauthorizedAccessException e)
                        {
                            Logger.Warn(e.Message);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e);
                        }
                    }

                    return zipStream;
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    throw;
                }
                finally
                {
                    Mutex.ReleaseMutex();
                }
            });
        }
    }
}