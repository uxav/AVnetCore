using System.Threading.Tasks;
using Crestron.SimplSharp;

namespace UXAV.AVnet.Core.WebScripting.InternalApi
{
    internal class Vc4StatusApiHandler : ApiRequestHandler
    {
        public Vc4StatusApiHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request, true)
        {
        }

        public async Task Get()
        {
            if (CrestronEnvironment.DevicePlatform != eDevicePlatform.Server)
            {
                await HandleNotFoundAsync("Not supported on this platform");
                return;
            }

            switch (Request.RoutePatternArgs["method"])
            {
                case "ethernet":
                    var ethernetInfo = await Vc4WebApi.GetEthernetAsync();
                    await WriteResponseAsync(ethernetInfo);
                    return;
                case "deviceInfo":
                    var deviceInfo = await Vc4WebApi.GetDeviceInfoAsync();
                    await WriteResponseAsync(deviceInfo);
                    return;
                case "systemTable":
                    var systemTable = await Vc4WebApi.GetSystemTableAsync();
                    await WriteResponseAsync(systemTable);
                    return;
                case "ipTable":
                    var ipTable = await Vc4WebApi.GetIpTableAsync();
                    await WriteResponseAsync(ipTable);
                    return;
                case "programLibrary":
                    var programLibrary = await Vc4WebApi.GetProgramLibraryAsync();
                    await WriteResponseAsync(programLibrary);
                    return;
                case "programInstance":
                    var programInstance = await Vc4WebApi.GetProgramInstanceAsync();
                    await WriteResponseAsync(programInstance);
                    return;
                default:
                    await HandleNotFoundAsync();
                    return;
            }
        }
    }
}