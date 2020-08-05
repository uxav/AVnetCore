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
        event AudioMuteChangeEventHandler MuteChange;
        event AudioLevelChangeEventHandler LevelChange;
    }

    public delegate void AudioMuteChangeEventHandler(bool muted);
    public delegate void AudioLevelChangeEventHandler(ushort level);

    public enum AudioLevelControlChangeEventType
    {
        VolumeLevel,
        Mute
    }
}