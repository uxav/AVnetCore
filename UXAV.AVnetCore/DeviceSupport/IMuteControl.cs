using UXAV.AVnetCore.Models;

namespace UXAV.AVnetCore.DeviceSupport
{
    public interface IMuteControl : IGenericItem
    {
        bool SupportsMute { get; }
        bool Muted { get; set; }
        void Mute();
        void Unmute();
        event MuteChangeEventHandler MuteChange;
    }

    public delegate void MuteChangeEventHandler(bool muted);
}