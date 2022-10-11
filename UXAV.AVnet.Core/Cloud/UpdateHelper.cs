using System;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UXAV.Logging;

namespace UXAV.AVnet.Core.Cloud
{
    internal static class UpdateHelper
    {
        public static async Task<SoftwareUpdateInfo[]> GetUpdatesAsync(string applicationName, string token)
        {
            try
            {
                var uri = new Uri(
                    $"https://{CloudConnector.Host}/api/updates/v1/{HttpUtility.UrlEncode(applicationName)}?token={token}");
                Logger.Debug($"Looking for software updates from: {uri}");
                var response = await CloudConnector.HttpClient.GetAsync(uri);
                Logger.Debug($"Response: {response.StatusCode}");
                response.EnsureSuccessStatusCode();
                try
                {
                    using (var content = response.Content)
                    {
                        var data = await content.ReadAsStringAsync();
                        var json = JToken.Parse(data);
                        return json["result"]?.ToObject<SoftwareUpdateInfo[]>();
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
                finally
                {
                    response.Dispose();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return null;
        }
    }

    public class SoftwareUpdateInfo
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("targetName")] public string TargetName { get; set; }
        [JsonProperty("version")] public string Version { get; set; }
        [JsonProperty("assemblyVersion")] public string AssemblyVersion { get; set; }
        [JsonProperty("time")] public DateTime Time { get; set; }
        [JsonProperty("hash")] public string Hash { get; set; }
        [JsonProperty("path")] public string Path { get; set; }
        [JsonProperty("prerelease")] public bool PreRelease { get; set; }
        [JsonProperty("debug")] public bool Debug { get; set; }
        [JsonProperty("url")] public string DownloadUrl { get; set; }
    }
}