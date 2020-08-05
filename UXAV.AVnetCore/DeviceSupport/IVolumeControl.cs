using UXAV.AVnetCore.Models;

namespace UXAV.AVnetCore.DeviceSupport
{
    public interface IVolumeControl : IGenericItem
    {
        bool SupportsVolumeLevel { get; }
        ushort VolumeLevel { get; set; }
        string VolumeLevelString { get; }
        void SetDefaultVolumeLevel();
        bool SupportsMute { get; }
        bool Muted { get; set; }
        void Mute();
        void Unmute();
        event MuteChangeEventHandler MuteChange;
        event VolumeLevelChangeEventHandler VolumeLevelChange;
    }

    public delegate void MuteChangeEventHandler(bool muted);
    public delegate void VolumeLevelChangeEventHandler(ushort level);

    public enum AudioLevelControlChangeEventType
    {
        VolumeLevel,
        Mute
    }
}