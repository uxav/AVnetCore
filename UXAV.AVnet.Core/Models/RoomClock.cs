using System;
using System.Threading;
using Crestron.SimplSharp;
using UXAV.Logging;

namespace UXAV.AVnet.Core.Models
{
    public static class RoomClock
    {
        private static bool _started;
        private static Thread _thread;
        private static bool _stopping;
        private static readonly AutoResetEvent Wait = new AutoResetEvent(false);

        public static DateTime Time => DateTime.Now;

        public static string Formatted => DateTime.Now.ToString("t");

        public static void Start()
        {
            if (_started) return;
            _started = true;
            _thread = new Thread(RoomClockProcess)
            {
                Name = "RoomClock timer process",
                Priority = ThreadPriority.Lowest
            };
            _thread.Start();
            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                if (type != eProgramStatusEventType.Stopping) return;
                _stopping = true;
                Wait.Set();
            };
        }

        private static void RoomClockProcess()
        {
            Logger.Log($"{nameof(RoomClockProcess)}() thread started");

            while (!_stopping)
            {
                var now = DateTime.Now;
                //Logger.Log("Time is now " + now.ToString("R"));
                EventService.Notify(EventMessageType.TimeChanged, new {@Time = now, @Formatted = now.ToString("t")});
                var timeToNextMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0) +
                                       TimeSpan.FromSeconds(60.001) - now;
                if (timeToNextMinute == TimeSpan.Zero)
                {
                    timeToNextMinute += TimeSpan.FromSeconds(1);
                }

                Wait.WaitOne(timeToNextMinute);
            }

            ErrorLog.Notice($"Leaving {nameof(RoomClockProcess)}() thread");
        }
    }
}