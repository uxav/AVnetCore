using Crestron.SimplSharpPro;

namespace UXAV.AVnetCore.DeviceSupport
{
    public interface IGenericDeviceWrapper
    {
        GenericDevice GenericDevice { get; }
    }
}