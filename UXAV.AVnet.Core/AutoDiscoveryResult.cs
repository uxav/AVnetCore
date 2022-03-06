using System.Linq;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;

namespace UXAV.AVnet.Core
{
    public class AutoDiscoveryResult
    {
        private readonly string _tsid;

        internal AutoDiscoveryResult(EthernetAutodiscovery.AutoDiscoveredDeviceElement element)
        {
            IpAddress = element.IPAddress;
            IpId = element.IPId;
            Hostname = element.HostName;
            Adapter = element.AdapterType;
            DetailsString = element.DeviceIdString;
            var details = Regex.Match(DetailsString,
                @"([\w-]+).*\[(.+?)(?: \((.+)\))?, *[^\w]?(\w{8,})\]\ ?(?:@E-(\w{12}))*");
            if (details.Success)
            {
                Model = details.Groups[1].Value;
                Version = details.Groups[2].Value;
                _tsid = details.Groups[4].Value;
                MacAddress = details.Groups[5].Value;
                if (!string.IsNullOrEmpty(MacAddress))
                    MacAddress = string.Join(":", Enumerable.Range(0, 6)
                        .Select(i => MacAddress.Substring(i * 2, 2)));
            }
            else
            {
                Model = "Unknown";
                Version = string.Empty;
                MacAddress = string.Empty;
            }
        }

        public string IpAddress { get; }

        public ushort IpId { get; }

        public string IpIdString => IpId == 0 ? string.Empty : IpId.ToString("X2");

        public string Hostname { get; }

        public EthernetAdapterType Adapter { get; }

        public string AdapterString
        {
            get
            {
                switch (Adapter)
                {
                    case EthernetAdapterType.EthernetLANAdapter:
                        return "LAN";
                    case EthernetAdapterType.EthernetCSAdapter:
                        return "ControlSubnet";
                    case EthernetAdapterType.EthernetWIFIAdapter:
                        return "WiFi";
                    case EthernetAdapterType.EthernetLAN2Adapter:
                        return "LAN 2";
                    default:
                        return "Unknown";
                }
            }
        }

        public string DetailsString { get; }

        public string Model { get; }

        public string Version { get; }

        public string MacAddress { get; }

        public string SerialNumber =>
            string.IsNullOrEmpty(_tsid) ? string.Empty : CrestronEnvironment.ConvertTSIDToSerialNumber(_tsid);
    }
}