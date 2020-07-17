using UXAV.AVnetCore.DeviceSupport;

namespace UXAV.AVnetCore.UI
{
    /// <summary>
    /// A UI item which can be enabled or disabled by a digital enable join
    /// </summary>
    public interface IEnableItem : ISigProvider
    {
        #region Properties

        /// <summary>
        /// The UI item enable digital join
        /// </summary>
        uint EnableJoinNumber { get; }

        /// <summary>
        /// True if enabled
        /// </summary>
        bool Enabled { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Enable the UI item
        /// </summary>
        void Enable();

        /// <summary>
        /// Disable the UI item
        /// </summary>
        void Disable();

        #endregion
    }
}