using Crestron.SimplSharp;

namespace UXAV.AVnet.Core.WebScripting.InternalApi
{
    public class Vc4StatusApiHandler : ApiRequestHandler
    {
        public Vc4StatusApiHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request, true)
        {
        }

        public void Get()
        {
            if (CrestronEnvironment.DevicePlatform != eDevicePlatform.Server)
            {
                HandleNotFound("Not supported on this platform");
                return;
            }

            switch (Request.RoutePatternArgs["method"])
            {
                case "ethernet":
                    var ethernetInfo = Vc4WebApi.GetEthernetAsync().Result;
                    WriteResponse(ethernetInfo);
                    return;
                case "deviceInfo":
                    var deviceInfo = Vc4WebApi.GetDeviceInfoAsync().Result;
                    WriteResponse(deviceInfo);
                    return;
                case "systemTable":
                    var systemTable = Vc4WebApi.GetSystemTableAsync().Result;
                    WriteResponse(systemTable);
                    return;
                case "ipTable":
                    var ipTable = Vc4WebApi.GetIpTableAsync().Result;
                    WriteResponse(ipTable);
                    return;
                case "programLibrary":
                    var programLibrary = Vc4WebApi.GetProgramLibraryAsync().Result;
                    WriteResponse(programLibrary);
                    return;
                case "programInstance":
                    var programInstance = Vc4WebApi.GetProgramInstanceAsync().Result;
                    WriteResponse(programInstance);
                    return;
                default:
                    HandleNotFound();
                    return;
            }
        }
    }
}