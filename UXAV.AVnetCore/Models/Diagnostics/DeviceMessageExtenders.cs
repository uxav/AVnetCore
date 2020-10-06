using System.Linq;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.UI;
using UXAV.AVnetCore.DeviceSupport;

namespace UXAV.AVnetCore.Models.Diagnostics
{
    public static class DeviceMessageExtenders
    {
        public static DiagnosticMessage CreateStatusMessage(this GenericDevice device)
        {
            if (device is CrestronGo && !device.IsOnline)
                return new DiagnosticMessage(MessageLevel.Warning, $"{device.Name} is offline!",
                    $"IP ID: {device.ID:X2}", device.GetType().Name, device.Description);
            if (!device.IsOnline)
                return new DiagnosticMessage(MessageLevel.Danger, $"{device.Name} is offline!",
                    $"IP ID: {device.ID:X2}", device.GetType().Name, device.Description);
            var ipAddresses = device.ConnectedIpList.Select(information => information.DeviceIpAddress);
            var ipAddressString = string.Join(", ", ipAddresses);
            return new DiagnosticMessage(MessageLevel.Success, $"{device.Name} is online.",
                $"IP ID: {device.ID:X2} ({ipAddressString})", device.GetType().Name, device.Description);
        }

        public static DiagnosticMessage CreateOfflineMessage(this IDevice device)
        {
            return new DiagnosticMessage(MessageLevel.Danger, $"{device.Name} is offline!", device.ConnectionInfo,
                device);
        }

        public static DiagnosticMessage CreateOnlineMessage(this IDevice device)
        {
            return new DiagnosticMessage(MessageLevel.Success, $"{device.Name} is online.", device.ConnectionInfo,
                device);
        }

        public static DiagnosticMessage CreateOfflineMessage(this IDevice device, string details)
        {
            return new DiagnosticMessage(MessageLevel.Danger, $"{device.Name} is offline!", details, device);
        }

        public static DiagnosticMessage CreateOnlineMessage(this IDevice device, string details)
        {
            return new DiagnosticMessage(MessageLevel.Success, $"{device.Name} is online.", details, device);
        }
    }
}