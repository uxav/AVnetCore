using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronDataStore;
using Crestron.SimplSharpPro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UXAV.AVnet.Core.Models;
using UXAV.AVnet.Core.UI.ReservedJoins;
using UXAV.AVnet.Core.Web;
using UXAV.Logging;

namespace UXAV.AVnet.Core.UI.Ch5
{
    public abstract class Ch5UIController<THandler> : Core3ControllerBase where THandler : Ch5ApiHandlerBase
    {
        private readonly Mutex _settingsMutex = new Mutex();
        private string _webSocketUrl;
        private System.Threading.Timer _onlineDelay;

        protected Ch5UIController(SystemBase system, uint roomId, string typeName, uint ipId, string description,
            string pathOfPanelArchiveFile)
            : base(system, roomId, typeName, ipId, description, pathOfPanelArchiveFile)
        {
            Device.StringInput[Serial.DeviceIdString].StringValue = Device.ID.ToString("X2");

            Device.SigChange += (device, args) =>
            {
                if (args.Event == eSigEvent.StringChange && args.Sig.Number == Serial.LogSend)
                {
                    Logger.Log($"Received log over CIP from Device {device}: {args.Sig.StringValue}");
                    return;
                }
            };
        }

        public string WebSocketUrl
        {
            get => _webSocketUrl;
            internal set
            {
                _webSocketUrl = value;
                Logger.Log($"Setting websocket URL: {_webSocketUrl}");
                Device.StringInput[Serial.WebsocketUrl].StringValue = _webSocketUrl;
            }
        }

        private string StorageTagForSettings => $"UI_SETTINGS_APP-{InitialParametersClass.ApplicationNumber:D2}_IPID-{Device.ID:X2}";

        protected override void OnOnlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {
            base.OnOnlineStatusChange(currentDevice, args);
            if (!args.DeviceOnLine) return;
            _onlineDelay?.Dispose();
            _onlineDelay = new System.Threading.Timer(async (e) =>
            {
                Logger.Log("Device online, sending websocket URL");
                Device.StringInput[Serial.WebsocketUrl].StringValue = "";
                await Task.Delay(500);
                Device.StringInput[Serial.WebsocketUrl].StringValue = WebSocketUrl;
                Device.StringInput[Serial.DeviceIdString].StringValue = Device.ID.ToString("X2");
            }, null, 2000, 0);
        }

        internal override void WebsocketConnected(Ch5ApiHandlerBase ch5ApiHandlerBase)
        {
            try
            {
                var settings = GetSettings();
                if (settings == null)
                {
                    Logger.Warn("No UI settings found, sending default settings");
                    var newSettings = GetDefaultUiSettings();
                    settings = JToken.FromObject(newSettings);
                    SaveSettings(settings);
                }

                Logger.Debug("Sending UI settings to websocket:\r\n" + settings.ToString(Formatting.Indented));

                OnNotifyWebsocket("SettingsInit", settings);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        protected abstract object GetDefaultUiSettings();

        internal override void SaveSettings(JToken args)
        {
            _settingsMutex.WaitOne(TimeSpan.FromSeconds(5));
            try
            {
                Logger.Debug("Saving UI settings, received settings:\r\n" + args.ToString(Formatting.Indented));
                CrestronDataStoreStatic.GetLocalStringValue(StorageTagForSettings, out var settingsString);
                if (string.IsNullOrEmpty(settingsString))
                {
                    Logger.Debug("Saving UI settings, no data to merge. Saved as sent!");
                    CrestronDataStoreStatic.SetLocalStringValue(StorageTagForSettings, args.ToString());
                    return;
                }

                var currentSettings = JToken.Parse(settingsString);
                var settings = new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Union };
                var mergedSettings = (JContainer)currentSettings;
                mergedSettings.Merge(args, settings);
                Logger.Debug("Saving UI settings, merged copy:\r\n" + mergedSettings.ToString(Formatting.Indented));
                CrestronDataStoreStatic.SetLocalStringValue(StorageTagForSettings, mergedSettings.ToString());
            }
            finally
            {
                _settingsMutex.ReleaseMutex();
            }
        }

        internal override JToken GetSettings()
        {
            _settingsMutex.WaitOne(TimeSpan.FromSeconds(5));
            try
            {
                Logger.Debug("Getting UI settings with tag: " + StorageTagForSettings);
                CrestronDataStoreStatic.GetLocalStringValue(StorageTagForSettings, out var settingsString);
                return string.IsNullOrEmpty(settingsString) ? null : JToken.Parse(settingsString);
            }
            finally
            {
                _settingsMutex.ReleaseMutex();
            }
        }

        internal override void InitializeInternal()
        {
            try
            {
                SystemBase.WebServer.AddDeviceService(this);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            base.InitializeInternal();
        }
    }
}