namespace UXAV.AVnet.Core.DeviceSupport
{
    public interface IDeviceWithStandbyMode : IDevice
    {
        bool InStandby { get; }
        void Wake();
        void Sleep();
        event StandbyModeChangedEventHandler InStandbyChanged;
    }

    public delegate void StandbyModeChangedEventHandler(IDeviceWithStandbyMode device, bool inStandby);
}