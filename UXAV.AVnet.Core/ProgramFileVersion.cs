using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Crestron.SimplSharpPro;
using UXAV.AVnet.Core.Models;
using UXAV.Logging;

namespace UXAV.AVnet.Core
{
    public class ProgramFileVersion
    {
        internal ProgramFileVersion()
        {
        }

        public string Name { get; internal set; } = string.Empty;

        public Version Version { get; internal set; }

        public bool IsRunningProgram
        {
            get
            {
                if (string.IsNullOrEmpty(Name)) return false;
                return Name == SystemBase.AppAssembly.GetName().Name;
            }
        }

        public bool IsDowngrade
        {
            get
            {
                if (Version == null) return false;
                if (!IsRunningProgram) return false;
                return Version < SystemBase.AppAssembly.GetName().Version;
            }
        }

        public string VersionString => Version?.ToString();

        public static ProgramFileVersion Get(string cpzPath)
        {
            var result = new ProgramFileVersion();
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += CurrentDomainOnReflectionOnlyAssemblyResolve;
            try
            {
                Logger.Debug($"Looking for Zip/Cpz archive at: {cpzPath} ...");
                using (var file = File.OpenRead(cpzPath))
                {
                    using (var zipFile = new ZipArchive(file, ZipArchiveMode.Read))
                    {
                        Logger.Debug(
                            $"Checking contents of Zip for assembly containing {nameof(CrestronControlSystem)} ...");
                        var entries = zipFile.Entries
                            .Where(e => CheckFileNameForPotentialAssembly(e.FullName));
                        foreach (var entry in entries)
                            try
                            {
                                Logger.Debug($"Checking \"{entry.FullName}\" ...");
                                using (var entryStream = entry.Open())
                                {
                                    var tmpFilePath = SystemBase.TempFileDirectory + "/" + entry.FullName;
                                    Logger.Debug($"Writing file to temp file: {tmpFilePath}");
                                    using (var tmpFile = File.Open(tmpFilePath, FileMode.Create, FileAccess.ReadWrite,
                                               FileShare.None))
                                    {
                                        entryStream.CopyTo(tmpFile);
                                        var assembly = Assembly.ReflectionOnlyLoadFrom(tmpFilePath);
                                        var types = assembly.GetTypes();
                                        foreach (var type in types)
                                            try
                                            {
                                                if (!type.IsClass || type.IsNotPublic) continue;
                                                if (type.BaseType == null ||
                                                    type.BaseType != typeof(CrestronControlSystem))
                                                    continue;
                                                Logger.Debug(
                                                    $"Found class \"{type}\" which derives from {nameof(CrestronControlSystem)}");

                                                result.Name = assembly.GetName().Name;
                                                result.Version = assembly.GetName().Version;
                                                break;
                                            }
                                            catch (Exception e)
                                            {
                                                Logger.Warn(
                                                    $"Error looking at {type}, {e.GetType().Name}: {e.Message}");
                                            }
                                    }

                                    Logger.Debug($"Deleting temp file: {tmpFilePath}");
                                    File.Delete(tmpFilePath);

                                    if (!string.IsNullOrEmpty(result.Name)) break;
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.Warn($"Could not read entry: {entry.FullName}, {e.GetType().Name}");
                            }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= CurrentDomainOnReflectionOnlyAssemblyResolve;

            return result;
        }

        private static bool CheckFileNameForPotentialAssembly(string fileName)
        {
            // if not dll, return false;
            if (!fileName.EndsWith(".dll")) return false;
            // file is in directory, so return false
            if (Regex.IsMatch(fileName, @"^\w+\/.+")) return false;
            // check none of below patterns match
            return new[]
            {
                @"^Crestron.", @"^System.", @"^Microsoft.", @"^SimplSharp", @"^Newtonsoft.", @"^CsvHelper.",
                @"^CsvHelper.", @"^CsvHelper.dll$", @"^Nerdbank.Streams.dll$", @"^netstandard.dll$",
                @"^UXAV.Cisco.dll$", @"^UXAV.AVnet.Core.dll$", @"^UXAV.Logging.dll$", @"^StreamJsonRpc.dll",
                @"^MessagePack.Annotations.dll"
            }.All(ignorePattern => !Regex.IsMatch(fileName, ignorePattern));
        }

        private static Assembly CurrentDomainOnReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                Logger.Debug($"Resolving: {args.Name}");
                return Assembly.Load(args.Name);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return null;
        }
    }
}