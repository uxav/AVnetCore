using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UXAV.Logging;
using Formatting = Newtonsoft.Json.Formatting;

namespace UXAV.AVnet.Core.DeviceSupport
{
    public static class QsysDiscoveryProtocol
    {
        public static async Task<DiscoveredDevice[]> DiscoverAsync(int timeoutInMilliseconds = 5000)
        {
            var data = new List<JToken>();
            var deviceData = new Dictionary<string, DiscoveredDevice>();
            var controlData = new Dictionary<string, DiscoveredControlInfo>();
            var multicastAddress = System.Net.IPAddress.Parse("224.0.23.175");
            const int startPort = 2467;
            const int endPort = 2470;

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(timeoutInMilliseconds);

            var tasks = new Task[endPort - startPort + 1];

            for (var port = startPort; port <= endPort; port++)
            {
                var localPort = port; // Local copy for the closure below
                tasks[port - startPort] = Task.Run(async () =>
                {
                    var result = await ListenOnPortAsync(multicastAddress, localPort, cancellationTokenSource.Token);
                    data.AddRange(result);
                }, cancellationTokenSource.Token);
            }

            await Task.WhenAll(tasks);

            var dataArray = data.ToArray();
            foreach (var jToken in dataArray)
            {
                if (!(jToken is JObject jObject)) continue;
                foreach (var property in jObject.Properties())
                    switch (property.Name)
                    {
                        case "device":
                            var deviceRef = property.Value["ref"]?.Value<string>();
                            if (deviceRef == null || deviceData.ContainsKey(deviceRef))
                                continue;
                            deviceData[deviceRef] = property.Value.ToObject<DiscoveredDevice>();
                            break;
                        case "control":
                            var controlRef = property.Value["device_ref"]?.Value<string>();
                            if (controlRef == null || controlData.ContainsKey(controlRef))
                                continue;
                            controlData[controlRef] = property.Value.ToObject<DiscoveredControlInfo>();
                            break;
                        case "query_ref":
                            break;
                        default:
                            Logger.Debug("Unknown property: " + property.Name);
                            break;
                    }
            }

            foreach (var device in deviceData.Values)
                if (controlData.TryGetValue(device.Ref, out var controlInfo))
                    device.ControlInfo = controlInfo;

            return deviceData.Values
                .OrderByDescending(d => d.ControlInfo?.DesignName ?? string.Empty)
                .ThenBy(d => d.PartNumber ?? string.Empty)
                .ThenBy(d => d.IpAddress ?? string.Empty)
                .ToArray();
        }

        private static async Task<JToken[]> ListenOnPortAsync(System.Net.IPAddress multicastAddress, int port,
            CancellationToken cancellationToken = default)
        {
            var data = new List<JToken>();
            using (var udpClient = new UdpClient())
            {
                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, true);
                udpClient.Client.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, port));
                udpClient.JoinMulticastGroup(multicastAddress);

                //Logger.Debug($"Listening for multicast UDP packets on {multicastAddress}:{port}...");

                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var receiveTask = udpClient.ReceiveAsync();
                        var delayTask =
                            Task.Delay(60000,
                                cancellationToken); // Wait for 5 seconds or until cancellation is requested

                        var completedTask = await Task.WhenAny(receiveTask, delayTask);

                        if (completedTask == receiveTask && !cancellationToken.IsCancellationRequested)
                        {
                            var receivedResult = await receiveTask; // Receive the data
                            var receivedData = Encoding.ASCII.GetString(receivedResult.Buffer);

                            var xmlDoc = new XmlDocument();
                            try
                            {
                                xmlDoc.LoadXml(receivedData);
                                var json = JsonConvert.SerializeXmlNode(xmlDoc, Formatting.Indented);
                                var jToken = JToken.Parse(json);
                                if (jToken["QDP"] != null)
                                    data.Add(jToken["QDP"]);
                            }
                            catch (System.Xml.XmlException xmlEx)
                            {
                                Console.WriteLine($"XML Parsing Error: {xmlEx.Message}");
                            }
                        }
                        else
                        {
                            // Timeout or cancellation requested
                            break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Logger.Debug($"Listening on port {port} cancelled due to timeout.");
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
                finally
                {
                    udpClient.DropMulticastGroup(multicastAddress);
                    //Logger.Debug($"Stopped listening on port {port}.");
                }
            }

            return data.ToArray();
        }
        
        public class DiscoveredDevice
        {
            [JsonProperty("ref")] public string Ref { get; set; }
            [JsonProperty("name")] public string Name { get; set; }
            [JsonProperty("type")] public string Type { get; set; }
            [JsonProperty("part_number")] public string PartNumber { get; set; }
            [JsonProperty("platform")] public string Platform { get; set; }
            [JsonProperty("lan_a_ip")] public string IpAddress { get; set; }
            [JsonProperty("lan_b_ip")] public string IpAddressLanB { get; set; }
            [JsonProperty("aux_a_ip")] public string IpAddressAux { get; set; }
            [JsonProperty("lan_a_lldp")] public string LldpInfo { get; set; }
            [JsonProperty("lan_b_lldp")] public string LldpInfoLanB { get; set; }
            [JsonProperty("web_cfg_url")] public string WebConfigUrl { get; set; }
            [JsonProperty("secure_comm")] public string SecureComm { get; set; }
            [JsonProperty("is_virtual")] public bool? IsVirtual { get; set; }
            [JsonProperty("https_server_up")] public bool? HttpsServerUp { get; set; }
            [JsonProperty("control")] public DiscoveredControlInfo ControlInfo { get; set; }
        }

        public class DiscoveredControlInfo
        {
            [JsonProperty("ref")] public string Ref { get; set; }
            [JsonProperty("role")] public string Role { get; set; }
            [JsonProperty("device_ref")] public string DeviceRef { get; set; }
            [JsonProperty("design_pretty")] public string DesignName { get; set; }
            [JsonProperty("design_code")] public string DesignCode { get; set; }
            [JsonProperty("primary")] public int Primary { get; set; }
            [JsonProperty("redundant")] public int Redundant { get; set; }
        }
    }
}