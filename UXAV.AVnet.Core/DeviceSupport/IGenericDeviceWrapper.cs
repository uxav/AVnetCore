using Crestron.SimplSharpPro;

namespace UXAV.AVnet.Core.DeviceSupport
{
    public interface IGenericDeviceWrapper
    {
        GenericDevice GenericDevice { get; }
    }
}