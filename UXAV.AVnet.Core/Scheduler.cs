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

            Logger.AddCommand((argString, args, connection, respond) =>
            {
                lock (Schedules)
                {
                    foreach (var valuePair in Schedules)
                    {
                        respond($"{valuePair.Value.Time} -- {valuePair.Value.Callback.Method}\r\n");
                    }
                }
            }, "SchedulesList", "List schedules in scheduler");
        }

        public static int AddSchedule(string time, Action callback)
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
                return _nextId;
            }
        }

        public static void EditSchedule(int scheduleId, string time)
        {
            if (!Schedules.ContainsKey(scheduleId))
            {
                throw new KeyNotFoundException($"No schedule with ID {scheduleId}");
            }

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

            lock (Schedules)
            {
                Schedules[scheduleId].Time = time;
            }
        }

        private static void CheckSchedules(DateTime dateTime)
        {
            var timeString = $"{dateTime.Hour:D2}:{dateTime.Minute:D2}";
            lock (Schedules)
            {
                foreach (var item in Schedules.Values.Where(i => i.Time == timeString))
                {
                    try
                    {
                        Task.Run(item.Callback);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }
                }
            }
        }
    }

    internal class ScheduleItem
    {
        private readonly int _id;
        private readonly Action _callback;

        internal ScheduleItem(int id, string time, Action callback)
        {
            _id = id;
            Time = time;
            _callback = callback;
        }

        public int Id => _id;
        public string Time { get; set; }

        public Action Callback => _callback;
    }
}