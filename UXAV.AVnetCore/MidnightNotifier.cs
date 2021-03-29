using System;
using System.Timers;
using Crestron.SimplSharp;
using Microsoft.Win32;

namespace UXAV.AVnetCore
{
    public static class MidnightNotifier
    {
        private static readonly Timer Timer;

        static MidnightNotifier()
        {
            Timer = new Timer(GetSleepTime());
            CrestronEnvironment.ProgramStatusEventHandler += CrestronEnvironmentOnProgramStatusEventHandler;
            Timer.Elapsed += (s, e) =>
            {
                OnDayChanged();
                Timer.Interval = GetSleepTime();
            };
            Timer.Start();

            SystemEvents.TimeChanged += OnSystemTimeChanged;
        }

        private static void CrestronEnvironmentOnProgramStatusEventHandler(eProgramStatusEventType eventType)
        {
            if (eventType == eProgramStatusEventType.Stopping)
            {
                Timer?.Dispose();
            }
        }

        private static double GetSleepTime()
        {
            var midnightTonight = DateTime.Today.AddDays(1);
            var differenceInMilliseconds = (midnightTonight - DateTime.Now).TotalMilliseconds;
            return differenceInMilliseconds;
        }

        private static void OnDayChanged()
        {
            var handler = DayChanged;
            handler?.Invoke(null, null);
        }

        private static void OnSystemTimeChanged(object sender, EventArgs e)
        {
            Timer.Interval = GetSleepTime();
        }

        public static event EventHandler<EventArgs> DayChanged;
    }
}