using UXAV.AVnet.Core.Models;
using UXAV.AVnet.Core.Models.Diagnostics;

namespace UXAV.AVnet.Core.DeviceSupport
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