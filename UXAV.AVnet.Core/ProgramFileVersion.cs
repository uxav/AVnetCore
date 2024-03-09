using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
                Logger.Debug("Loading assembly paths from application directory to resolve dependencies...");
                var paths = Directory
                    .GetFiles(SystemBase.ProgramApplicationDirectory, "*.dll", SearchOption.AllDirectories)
                    .Where(CheckFileNameForPotentialAssembly);
                var resolver = new PathAssemblyResolver(paths);
                using var metadataLoadContext = new MetadataLoadContext(resolver);
                Logger.Debug($"Looking for Zip/Cpz archive at: {cpzPath} ...");
                using var archive = new ZipArchive(File.OpenRead(cpzPath));
                foreach (var entry in archive.Entries)
                {
                    if (!entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) continue;
                    Logger.Debug($"Loading assembly from {entry.FullName} ...");
                    using var stream = entry.Open();
                    var assembly = metadataLoadContext.LoadFromStream(stream);
                    foreach (var type in assembly.GetTypes())
                    {
                        var baseType = type.BaseType;
                        if (baseType == null) continue;
                        if (!baseType.Name.Equals("CrestronControlSystem", StringComparison.Ordinal)) continue;
                        Console.WriteLine(
                            $"Type {type.FullName} in {entry.FullName} derives from CrestronControlSystem");
                        break;
                    }

                    result.Name = assembly.GetName().Name;
                    result.Version = assembly.GetName().Version;
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