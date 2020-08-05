using System.Collections.Generic;

namespace UXAV.AVnetCore.DeviceSupport
{
    public interface IVolumeLevelDevice : IDevice
    {
        IVolumeControl VolumeLevel { get; }
        bool SupportsAuxVolumeLevels { get; }
        IEnumerable<IVolumeControl> AuxVolumeLevels { get; }
    }
}