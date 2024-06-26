using System;
using UXAV.AVnet.Core.DeviceSupport;
using UXAV.AVnet.Core.Models;

namespace UXAV.AVnet.Core.UI.Components
{
    public interface IButton : ISigProvider, IGenericItem
    {
        uint DigitalJoinNumber { get; }
        uint EnableJoinNumber { get; }
        uint VisibleJoinNumber { get; }
        bool IsPressed { get; }
        bool Feedback { get; set; }

        /// <summary>
        ///     Set or get the time for the button to trigger a hold event
        /// </summary>
        TimeSpan HoldTime { get; }

        TimeSpan RepeatTime { get; }
        void SetId(uint id);

        void SetupHoldAndRepeatTimes(double holdTimeInSeconds, double repeatTimeInSeconds);

        void SetFeedback(bool value);

        event ButtonEventHandler ButtonEvent;
    }
}