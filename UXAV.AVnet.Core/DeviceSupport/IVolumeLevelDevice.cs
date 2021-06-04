using System.Collections.Generic;

namespace UXAV.AVnet.Core.DeviceSupport
{
    public interface IVolumeLevelDevice : IDevice
    {
        IVolumeControl VolumeLevel { get; }
        bool SupportsAuxVolumeLevels { get; }
        IEnumerable<IVolumeControl> AuxVolumeLevels { get; }
    }
}