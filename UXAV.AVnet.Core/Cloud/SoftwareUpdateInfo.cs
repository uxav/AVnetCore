using System;
using Newtonsoft.Json;

namespace UXAV.AVnet.Core.Cloud
{
    public class SoftwareUpdateInfo
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("targetName")] public string TargetName { get; set; }
        [JsonProperty("version")] public Version Version { get; set; }
        [JsonProperty("versionString")] public string VersionString => Version.ToString();
        [JsonProperty("assemblyVersion")] public Version AssemblyVersion { get; set; }

        [JsonProperty("assemblyVersionString")]
        public string AssemblyVersionString => AssemblyVersion.ToString();

        [JsonProperty("time")] public DateTime Time { get; set; }
        [JsonProperty("hash")] public string Hash { get; set; }
        [JsonProperty("path")] public string Path { get; set; }
        [JsonProperty("fileName")] public string FileName => System.IO.Path.GetFileName(Path);
        [JsonProperty("prerelease")] public bool PreRelease { get; set; }
        [JsonProperty("debug")] public bool Debug { get; set; }
        [JsonProperty("signedUrl")] public string DownloadUrl { get; set; }
    }
}