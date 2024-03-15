using System;
using System.IO;
using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
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
            try
            {
                Logger.Log($"Looking for Zip/Cpz archive at: {cpzPath} ...");
                using var archive = new ZipArchive(File.OpenRead(cpzPath));
                foreach (var entry in archive.Entries)
                {
                    if (!entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) continue;
                    Logger.Log($"Loading assembly from {entry.FullName} ...");
                    using var stream = entry.Open();
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    using var peReader = new PEReader(ms);
                    if (peReader.HasMetadata)
                    {
                        var reader = peReader.GetMetadataReader();
                        foreach (var handle in reader.TypeDefinitions)
                        {
                            var typeDef = reader.GetTypeDefinition(handle);
                            if (typeDef.Name.IsNil || typeDef.Namespace.IsNil) continue;
                            var typeName = reader.GetString(typeDef.Name);
                            var typeNamespace = reader.GetString(typeDef.Namespace);
#if DEBUG
                            Logger.Debug($"Found type '{typeNamespace}.{typeName}' in assembly '{entry.FullName}'");
#endif

                            var baseTypeHandle = typeDef.BaseType;
#if DEBUG
                            Logger.Debug($"Base type handle: {baseTypeHandle.Kind}");
#endif
                            if (baseTypeHandle.Kind != HandleKind.TypeReference) continue;
                            var baseType = reader.GetTypeReference((TypeReferenceHandle)baseTypeHandle);
                            var baseTypeName = reader.GetString(baseType.Name);
                            var baseTypeNamespace = reader.GetString(baseType.Namespace);
#if DEBUG
                            Logger.Debug($"Base type: '{baseTypeNamespace}.{baseTypeName}'");
#endif

                            // This is a simple check, you might need to enhance this for nested types or generics
                            if (baseTypeNamespace == "Crestron.SimplSharpPro" && baseTypeName == "CrestronControlSystem")
                            {
                                var assemblyDef = reader.GetAssemblyDefinition();
                                var assemblyName = reader.GetString(assemblyDef.Name);
                                var version = assemblyDef.Version;

                                Logger.Success($"Found CrestronControlSystem derived class '{typeName}' in assembly '{assemblyName}', version '{version}'");
                                result.Name = assemblyName;
                                result.Version = version;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }
    }
}