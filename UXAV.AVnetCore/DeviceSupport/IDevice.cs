using UXAV.AVnetCore.Models;
using UXAV.AVnetCore.Models.Diagnostics;

namespace UXAV.AVnetCore.DeviceSupport
{
    public interface IDevice : IDiagnosticItem, IInitializable, IAsset, IConnectedItem
    {
        /// <summary>
        /// Version information string
        /// </summary>
        string VersionInfo { get; }

        bool DebugEnabled { get; set; }
    }
}