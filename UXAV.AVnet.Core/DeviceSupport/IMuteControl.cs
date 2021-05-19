using UXAV.AVnet.Core.Models;

namespace UXAV.AVnet.Core.DeviceSupport
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