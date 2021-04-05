using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Crestron.SimplSharpPro;
using UXAV.Logging;

namespace UXAV.AVnetCore.UI
{
    public class ControllerActivityMonitor
    {
        private readonly Core3ControllerBase _controller;
        private readonly List<ActivityTimeOut> _timeOuts = new List<ActivityTimeOut>();
        private readonly DeviceExtender _touchDetectionExtender;

        internal ControllerActivityMonitor(Core3ControllerBase controller, DeviceExtender touchDetectionExtender, DeviceExtender system3Extender)
        {
            _controller = controller;
            _controller.Device.OnlineStatusChange += DeviceOnOnlineStatusChange;

            if (touchDetectionExtender != null)
            {
                _touchDetectionExtender = touchDetectionExtender;
                _touchDetectionExtender.DeviceExtenderSigChange += TouchDetectionExtenderOnDeviceExtenderSigChange;
            }

            if (system3Extender != null)
            {
                system3Extender.DeviceExtenderSigChange += System3ExtenderOnDeviceExtenderSigChange;
            }
        }

        public Core3ControllerBase Core3Controller => _controller;

        private void DeviceOnOnlineStatusChange(GenericBase currentdevice, OnlineOfflineEventArgs args)
        {
            if (args.DeviceOnLine && _touchDetectionExtender != null)
            {
                _touchDetectionExtender.SetUShortPropertyValue("Time", 1);
            }
        }

        internal ActivityTimeOut CreateTimeOut(TimeSpan timeOut, bool usesProximity)
        {
            var newActivityTimeOut = new ActivityTimeOut(this, timeOut, usesProximity);
            lock (_timeOuts)
            {
                _timeOuts.Add(newActivityTimeOut);
            }

            return newActivityTimeOut;
        }

        private void TouchDetectionExtenderOnDeviceExtenderSigChange(DeviceExtender touchDetectionExtender, SigEventArgs args)
        {
            if(args.Event != eSigEvent.BoolChange) return;
            var sigName = touchDetectionExtender.GetSigPropertyName(args.Sig);
            if(sigName != "TouchActivityFeedback") return;
            Logger.Debug($"Touch activity sig: {args.Sig.BoolValue}");
            var touch = args.Sig.BoolValue;
            Task.Run(() =>
            {
                lock (_timeOuts)
                {
                    foreach (var timeOut in _timeOuts)
                    {
                        if(touch) timeOut.Cancel();
                        else timeOut.Restart();
                    }
                }
            });
        }

        private void System3ExtenderOnDeviceExtenderSigChange(DeviceExtender system3Extender, SigEventArgs args)
        {
            var sigName = system3Extender.GetSigPropertyName(args.Sig);
            if(sigName != "ProximitySensorActiveFeedback") return;
            if(args.Sig.BoolValue) return;
            Task.Run(() =>
            {
                lock (_timeOuts)
                {
                    foreach (var timeOut in _timeOuts)
                    {
                        timeOut.NoProximityPresent();
                    }
                }
            });
        }
    }
}