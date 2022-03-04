using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UXAV.Logging;

namespace UXAV.AVnet.Core
{
    public static class Scheduler
    {
        private static Task _timer;
        private static bool _ready;
        private static int _nextId;
        private static readonly Dictionary<int, ScheduleItem> Schedules = new Dictionary<int, ScheduleItem>();

        internal static void Init()
        {
            if (_timer != null) return;

            _timer = Task.Run(() =>
            {
                while (true)
                {
                    var now = DateTime.Now;
                    if (_ready)
                    {
                        //Logger.Debug($"Checking for schedules matching {now:T} ...");
                        Task.Run(() => CheckSchedules(now));
                    }

                    //Logger.Debug($"TOD: {now.TimeOfDay:g} / {now.TimeOfDay.Hours:D2}:{now.TimeOfDay.Minutes:D2}");
                    var nextMin = now.AddMinutes(1);
                    var next = DateTime.Parse($"{nextMin.Date:d} {nextMin.Hour:D2}:{nextMin.Minute}:00");
                    var ms = (next - now);
                    //Logger.Debug($"Waiting {ms}");
                    _ready = true;
                    Thread.Sleep(ms);
                }
            });
        }

        public static void AddSchedule(string time, Action callback)
        {
            if (!Regex.IsMatch(time, @"\d{2}\:\d{2}"))
            {
                throw new ArgumentException("Time should be specified as HH:mm", nameof(time));
            }

            try
            {
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                DateTime.Parse(time);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Time could not be parsed, {e.Message}", nameof(time));
            }

            if (callback == null)
            {
                throw new ArgumentException("callback cannot be null", nameof(callback));
            }

            lock (Schedules)
            {
                _nextId++;
                Schedules.Add(_nextId, new ScheduleItem(_nextId, time, callback));
            }
        }

        private static void CheckSchedules(DateTime dateTime)
        {
            var timeString = $"{dateTime.Hour:D2}:{dateTime.Minute:D2}";
            lock (Schedules)
            {
                foreach (var item in Schedules.Values.Where(i => i.Time == timeString))
                {
                    Task.Run(item.Callback);
                }
            }
        }
    }

    public class ScheduleItem
    {
        private readonly int _id;
        private readonly string _time;
        private readonly Action _callback;

        internal ScheduleItem(int id, string time, Action callback)
        {
            _id = id;
            _time = time;
            _callback = callback;
        }

        public int Id => _id;
        public string Time => _time;
        public Action Callback => _callback;
    }
}