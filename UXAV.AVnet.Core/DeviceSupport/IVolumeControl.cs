namespace UXAV.AVnet.Core.DeviceSupport
{
    public interface IVolumeControl : IMuteControl
    {
        bool SupportsVolumeLevel { get; }
        ushort VolumeLevel { get; set; }
        string VolumeLevelString { get; }
        void SetDefaultVolumeLevel();
        event VolumeLevelChangeEventHandler VolumeLevelChange;
    }

    public delegate void VolumeLevelChangeEventHandler(ushort level);
}