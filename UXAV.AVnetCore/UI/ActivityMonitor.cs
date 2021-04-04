using System;
using System.Collections.Generic;
using Crestron.SimplSharpPro;
using UXAV.AVnetCore.UI.Components.Views;

namespace UXAV.AVnetCore.UI
{
    public static class ActivityMonitor
    {
        private static readonly Dictionary<Core3ControllerBase, ControllerActivityMonitor> Monitors =
            new Dictionary<Core3ControllerBase, ControllerActivityMonitor>();

        internal static void Register(Core3ControllerBase controller, DeviceExtender touchDetectionExtender,
            DeviceExtender system3Extender)
        {
            if (Monitors.ContainsKey(controller))
            {
                throw new ArgumentException("Already registered", nameof(controller));
            }

            var monitor = new ControllerActivityMonitor(controller, touchDetectionExtender, system3Extender);
            Monitors[controller] = monitor;
        }

        public static ActivityTimeOut CreateTimeOut(Core3ControllerBase controller, TimeSpan timeOut,
            bool usesProximity = false)
        {
            var monitor = Monitors[controller];
            return monitor.CreateTimeOut(timeOut, usesProximity);
        }

        public static ActivityTimeOut CreateTimeOut(UIViewControllerBase view, TimeSpan timeOut,
            bool usesProximity = false)
        {
            var monitor = Monitors[view.Core3Controller];
            return monitor.CreateTimeOut(timeOut, usesProximity);
        }
    }
}