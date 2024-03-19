using System;
using System.Threading;
using Crestron.SimplSharp.CrestronDataStore;
using Crestron.SimplSharpPro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UXAV.AVnet.Core.Models;
using UXAV.AVnet.Core.UI.ReservedJoins;
using UXAV.Logging;

namespace UXAV.AVnet.Core.UI.Ch5
{
    public abstract class Ch5UIController<THandler> : Core3ControllerBase where THandler : Ch5ApiHandlerBase
    {
        private readonly Mutex _settingsMutex = new Mutex();
        private string _webSocketUrl;

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

                if (args.Event != eSigEvent.BoolChange || args.Sig.Number != 10 || !args.Sig.BoolValue) return;
                if (string.IsNullOrEmpty(WebSocketUrl)) return;
                Logger.Log($"Device received high join on 10, sending websocket URL: {WebSocketUrl}");
                device.StringInput[Serial.WebsocketUrl].StringValue = WebSocketUrl;
                device.StringInput[Serial.DeviceIdString].StringValue = device.ID.ToString("X2");
            };
        }

        public string WebSocketUrl
        {
            get => _webSocketUrl;
            internal set
            {
                _webSocketUrl = value;
                Device.StringInput[Serial.WebsocketUrl].StringValue = _webSocketUrl;
            }
        }

        private string StorageTagForSettings => $"UI_SETTINGS_{Device.ID:X2}";

        protected override void OnOnlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {
            base.OnOnlineStatusChange(currentDevice, args);
            if (!args.DeviceOnLine) return;
            Logger.Log("Device online, sending websocket URL");
            Device.StringInput[Serial.WebsocketUrl].StringValue = WebSocketUrl;
            Device.StringInput[Serial.DeviceIdString].StringValue = Device.ID.ToString("X2");
        }

        internal override void WebsocketConnected(Ch5ApiHandlerBase ch5ApiHandlerBase)
        {
            try
            {
                var settings = GetSettings();
                if (settings == null)
                {
                    var newSettings = GetDefaultUiSettings();
                    settings = JToken.FromObject(newSettings);
                    SaveSettings(settings);
                }

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
                // ReSharper disable once RedundantTypeArgumentsOfMethod
                Ch5WebSocketServer.AddDeviceService<THandler>(this);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            base.InitializeInternal();
        }
    }
}