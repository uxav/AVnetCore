using System;
using UXAV.AVnetCore.DeviceSupport;
using UXAV.AVnetCore.Models;
using UXAV.AVnetCore.UI.Components;

namespace UXAV.AVnetCore.UI
{
    public interface IButton : ISigProvider, IGenericItem
    {
        uint DigitalJoinNumber { get; }
        uint EnableJoinNumber { get; }
        uint VisibleJoinNumber { get; }
        bool IsPressed { get; }
        bool Feedback { get; set; }
        void SetId(uint id);

        /// <summary>
        /// Set or get the time for the button to trigger a hold event
        /// </summary>
        TimeSpan HoldTime { get; }

        TimeSpan RepeatTime { get; }

        void SetupHoldAndRepeatTimes(double holdTimeInSeconds, double repeatTimeInSeconds);

        void SetFeedback(bool value);

        event ButtonEventHandler ButtonEvent;
    }
}