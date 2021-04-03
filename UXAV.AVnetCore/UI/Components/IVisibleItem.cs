using UXAV.AVnetCore.DeviceSupport;

namespace UXAV.AVnetCore.UI.Components
{
    public interface IVisibleItem : ISigProvider
    {
        /// <summary>
        /// Triggered when the visibility changes on the item
        /// </summary>
        event VisibilityChangeEventHandler VisibilityChanged;

        /// <summary>
        /// True if currently visible
        /// </summary>
        bool Visible { get; }

        /// <summary>
        /// The state set for Visible.
        /// This will be set before the feedback on Visible and may be used
        /// to reference if something is currently transitioning.
        /// </summary>
        bool RequestedVisibleState { get; }

        /// <summary>
        /// The digital join for the visible feedback
        /// </summary>
        uint VisibleJoinNumber { get; }

        /// <summary>
        /// Make the object visible
        /// </summary>
        void Show();

        /// <summary>
        /// Hide the object
        /// </summary>
        void Hide();
    }

    /// <summary>
    /// Event handler delegate for when the visibility changes on an IVisibleItem
    /// </summary>
    /// <param name="item">The item triggering the event</param>
    /// <param name="args">More information on the change</param>
    public delegate void VisibilityChangeEventHandler(IVisibleItem item, VisibilityChangeEventArgs args);

    /// <summary>
    /// Aruments for a visibility change event
    /// </summary>
    public class VisibilityChangeEventArgs
    {
        internal VisibilityChangeEventArgs(bool willBeVisible, VisibilityChangeEventType eventType)
        {
            EventType = eventType;
            WillBeVisible = willBeVisible;
        }

        /// <summary>
        /// The type of visible change
        /// </summary>
        public VisibilityChangeEventType EventType { get; }

        public bool WillBeVisible { get; }
    }

    /// <summary>
    /// The type of event which can trigger
    /// </summary>
    public enum VisibilityChangeEventType
    {
        /// <summary>
        /// The item will show
        /// </summary>
        WillShow,
        /// <summary>
        /// The item did show
        /// </summary>
        DidShow,
        /// <summary>
        /// The item will hide
        /// </summary>
        WillHide,
        /// <summary>
        /// The item did hide
        /// </summary>
        DidHide,
        /// <summary>
        /// Signalled if the animation complete join is triggered
        /// </summary>
        AnimationComplete
    }
}