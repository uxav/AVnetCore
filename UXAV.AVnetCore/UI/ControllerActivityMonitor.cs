using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Crestron.SimplSharpPro;

namespace UXAV.AVnetCore.UI
{
    public class ControllerActivityMonitor
    {
        private readonly Core3ControllerBase _controller;
        private readonly List<ActivityTimeOut> _timeOuts = new List<ActivityTimeOut>();

        internal ControllerActivityMonitor(Core3ControllerBase controller, DeviceExtender touchDetectionExtender, DeviceExtender system3Extender)
        {
            _controller = controller;

            if (touchDetectionExtender != null)
            {
                touchDetectionExtender.DeviceExtenderSigChange += TouchDetectionExtenderOnDeviceExtenderSigChange;
                touchDetectionExtender.SetUShortPropertyValue("Time", 1);
            }

            if (system3Extender != null)
            {
                system3Extender.DeviceExtenderSigChange += System3ExtenderOnDeviceExtenderSigChange;
            }
        }

        public Core3ControllerBase Core3Controller => _controller;

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
            var sigName = touchDetectionExtender.GetSigPropertyName(args.Sig);
            if(sigName != "TouchActivityFeedback") return;
            if(!args.Sig.BoolValue) return;
            Task.Run(() =>
            {
                lock (_timeOuts)
                {
                    foreach (var timeOut in _timeOuts)
                    {
                        timeOut.Reset();
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