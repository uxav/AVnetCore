using System;
using System.Net.Http;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using Newtonsoft.Json.Linq;

namespace UXAV.AVnet.Core
{
    public static class Vc4WebApi
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        private static async Task<object> GetAsync(string path)
        {
            if (!path.StartsWith("/"))
            {
                path = path + "/";
            }

            var uri = new Uri($"http://localhost:5000{path}");

            using (var response = await HttpClient.GetAsync(uri))
            {
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JObject.Parse(content);
            }
        }

        private static void ThrowIfNotCorrectPlatform()
        {
            if (CrestronEnvironment.DevicePlatform != eDevicePlatform.Server)
                throw new InvalidOperationException("Device is not server platform");
        }

        public static async Task<object> GetEthernetAsync()
        {
            ThrowIfNotCorrectPlatform();
            dynamic data = await GetAsync("/Ethernet");
            return data.Device.Ethernet;
        }

        public static async Task<object> GetDeviceInfoAsync()
        {
            ThrowIfNotCorrectPlatform();
            dynamic data = await GetAsync("/DeviceInfo");
            return data.Device.DeviceInfo;
        }

        public static async Task<object> GetSystemTableAsync()
        {
            ThrowIfNotCorrectPlatform();
            dynamic data = await GetAsync("/SystemTable");
            return data.Device.Programs.SystemTable;
        }

        public static async Task<object> GetIpTableAsync(string programInstanceId = null)
        {
            ThrowIfNotCorrectPlatform();
            if (string.IsNullOrEmpty(programInstanceId))
            {
                programInstanceId = InitialParametersClass.RoomId;
            }
            dynamic data = await GetAsync($"/IpTableByPID/{programInstanceId}");
            return data.Device.Programs.IpTableByPID;
        }

        public static async Task<object> GetProgramLibraryAsync()
        {
            ThrowIfNotCorrectPlatform();
            dynamic data = await GetAsync("/ProgramLibrary");
            return data.Device.Programs.ProgramLibrary;
        }

        public static async Task<object> GetProgramInstancesAsync()
        {
            ThrowIfNotCorrectPlatform();
            dynamic data = await GetAsync("/ProgramInstance");
            return data.Device.Programs.ProgramInstanceLibrary;
        }

        public static async Task<object> GetProgramInstanceAsync(string roomId = null)
        {
            dynamic library = await GetProgramInstancesAsync();
            if (roomId == null) return library[InitialParametersClass.RoomId];
            return library[roomId];
        }
    }
}