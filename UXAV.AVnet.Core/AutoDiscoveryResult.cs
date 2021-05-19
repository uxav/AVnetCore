using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;

namespace UXAV.AVnet.Core
{
    public class AutoDiscoveryResult
    {
        private readonly string _ipAddress;
        private readonly ushort _ipId;
        private readonly string _hostname;
        private readonly EthernetAdapterType _adapter;
        private readonly string _detailsString;
        private readonly string _model;
        private readonly string _version;
        private readonly string _macAddress;
        private readonly string _tsid;

        internal AutoDiscoveryResult(EthernetAutodiscovery.AutoDiscoveredDeviceElement element)
        {
            _ipAddress = element.IPAddress;
            _ipId = element.IPId;
            _hostname = element.HostName;
            _adapter = element.AdapterType;
            _detailsString = element.DeviceIdString;
            var details = Regex.Match(_detailsString,
                @"([\w-]+).*\[(.+?)(?: \((.+)\))?, *[^\w]?(\w{8,})\]\ ?(?:@E-(\w{12}))*");
            if (details.Success)
            {
                _model = details.Groups[1].Value;
                _version = details.Groups[2].Value;
                _tsid = details.Groups[4].Value;
                _macAddress = details.Groups[5].Value;
                if (!string.IsNullOrEmpty(_macAddress))
                {
                    _macAddress = string.Join(":", Enumerable.Range(0, 6)
                        .Select(i => _macAddress.Substring(i * 2, 2)));
                }
            }
            else
            {
                _model = "Unknown";
                _version = string.Empty;
                _macAddress = string.Empty;
            }
        }

        public string IpAddress => _ipAddress;

        public ushort IpId => _ipId;

        public string IpIdString => _ipId == 0 ? string.Empty : _ipId.ToString("X2");

        public string Hostname => _hostname;

        public EthernetAdapterType Adapter => _adapter;

        public string AdapterString
        {
            get
            {
                switch (_adapter)
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

        public string DetailsString => _detailsString;

        public string Model => _model;

        public string Version => _version;

        public string MacAddress => _macAddress;

        public string SerialNumber =>
            string.IsNullOrEmpty(_tsid) ? string.Empty : CrestronEnvironment.ConvertTSIDToSerialNumber(_tsid);
    }
}