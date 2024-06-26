using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using Newtonsoft.Json.Linq;
using UXAV.AVnet.Core.Config;
using UXAV.AVnet.Core.Models;
using UXAV.Logging;

namespace UXAV.AVnet.Core.Cloud
{
    internal static class UpdateHelper
    {
        private static bool _cronSetup;
        private static bool _updatesAvailable;
        private static bool _lastAttemptError;

        public static bool UpdatesAvailable
        {
            get => _updatesAvailable;
            private set
            {
                if (_updatesAvailable == value) return;
                _updatesAvailable = value;
                EventService.Notify(EventMessageType.UpdateAvailableChange);
            }
        }

        public static async Task<SoftwareUpdateInfo[]> GetUpdatesAsync(
            bool includeDebug = false, bool includePreRelease = false, bool includeOlder = false)
        {
            try
            {
                var uri = new Uri($"https://{CloudConnector.Host}/api/updates/v1/" +
                                  $"{WebUtility.UrlEncode(CloudConnector.ApplicationName)}?token={CloudConnector.Token}");
                //Logger.Debug($"Looking for software updates from: {uri}");
                using var response = await CloudConnector.HttpClient.GetAsync(uri);
                //Logger.Debug($"Response: {response.StatusCode}");
                response.EnsureSuccessStatusCode();
                _lastAttemptError = false;
                using var content = response.Content;
                var data = await content.ReadAsStringAsync();
                var json = JToken.Parse(data);
                var updates = json["result"]?
                    .ToObject<SoftwareUpdateInfo[]>()?
                    .New(!includeOlder)
                    .Where(update => !update.Debug || includeDebug)
                    .Where(update => !update.PreRelease || includePreRelease)
                    .OrderBy(update => update.Version)
                    .ThenBy(update => update.AssemblyVersion);
                return updates?.ToArray();
            }
            catch (Exception e)
            {
                if (_lastAttemptError) return null;
                _lastAttemptError = true;
                Logger.Error(e);
                return null;
            }
        }

        private static IEnumerable<SoftwareUpdateInfo> New(this IEnumerable<SoftwareUpdateInfo> updates,
            bool onlyNew = true)
        {
            var currentVersion = UxEnvironment.System.AppAssemblyVersion;
            //Logger.Debug($"Current version = {currentVersion}");
            return updates?.Where(x => x.AssemblyVersion > currentVersion || !onlyNew);
        }

        public static IEnumerable<SoftwareUpdateInfo> Release(this IEnumerable<SoftwareUpdateInfo> updates)
        {
            return updates?.Where(x => !x.PreRelease);
        }

        public static IEnumerable<SoftwareUpdateInfo> Beta(this IEnumerable<SoftwareUpdateInfo> updates)
        {
            return updates?.Where(x => x.PreRelease);
        }

        public static IEnumerable<SoftwareUpdateInfo> Debug(this IEnumerable<SoftwareUpdateInfo> updates)
        {
            return updates?.Where(x => x.Debug);
        }

        public static async void UpdateRunningProgram(string fileName)
        {
            var uri = new Uri(
                $"https://{CloudConnector.Host}/api/updates/v1/{WebUtility.UrlEncode(CloudConnector.ApplicationName)}" +
                $"/{fileName}?token={CloudConnector.Token}");
            Logger.Log($"Looking info on update from: {uri}");
            var response = await CloudConnector.HttpClient.GetAsync(uri);
            Logger.Log($"Response: {response.StatusCode}");
            response.EnsureSuccessStatusCode();
            try
            {
                using (var content = response.Content)
                {
                    var data = await content.ReadAsStringAsync();
                    var json = JToken.Parse(data);
                    var url = json?.ToObject<SoftwareUpdateInfo>()?.DownloadUrl;
                    if (string.IsNullOrEmpty(url)) throw new OperationCanceledException("No update url found");
                    var targetPath = SystemBase.ProgramUserDirectory + "/updates";
                    if (!Directory.Exists(targetPath))
                    {
                        Logger.Log("Created directory: " + targetPath);
                        Directory.CreateDirectory(targetPath);
                    }

                    var path = await DownloadFile(url, targetPath);
                    switch (CrestronEnvironment.DevicePlatform)
                    {
                        case eDevicePlatform.Appliance:
                            File.Move(path, SystemBase.ProgramApplicationDirectory + "/update.zip");
                            var consoleResponse = "";
                            CrestronConsole.SendControlSystemCommand(
                                $"progload -p:{InitialParametersClass.ApplicationNumber}", ref consoleResponse);
                            Logger.Warn($"Console response: {consoleResponse}");
                            break;
                        case eDevicePlatform.Server:
                            await Vc4WebApi.LoadProgramAsync(path);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(CrestronEnvironment.DevicePlatform));
                    }
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

        private static async Task<string> DownloadFile(string url, string targetPath)
        {
            Logger.Log($"Downloading from: {url}");
            var response = await CloudConnector.HttpClient.GetAsync(new Uri(url));
            Logger.Log($"Response: {response.StatusCode}");
            response.EnsureSuccessStatusCode();
            var fileName = response.Headers.GetValues("x-goog-meta-app-filename").FirstOrDefault();
            if (string.IsNullOrEmpty(fileName))
                throw new OperationCanceledException("No file name found in download metadata");
            Logger.Log("File name: " + fileName);
            var path = Path.Combine(targetPath, fileName);
            try
            {
                using (var fs = new FileStream(path, FileMode.Create))
                {
                    await response.Content.CopyToAsync(fs);
                    Logger.Success($"Downloaded {fs.Length} bytes to {fs.Name}");
                }

                return path;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
            finally
            {
                response.Dispose();
            }
        }

        public static void SetupUpdateTimer()
        {
            if (_cronSetup) throw new InvalidOperationException("Update check crob job already setup");
            var expression = ConfigManager.GetOrCreatePropertyListItem("CloudUpdateCheckCron", "0 * * * *");
            Task.Run(CheckForUpdates);
            CronJobs.Add(expression, CheckForUpdates);
            _cronSetup = true;
        }

        private static async void CheckForUpdates()
        {
            var updates = await GetUpdatesAsync(includePreRelease: false);
            UpdatesAvailable = updates?.Any() ?? false;
        }
    }
}