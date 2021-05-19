using System;
using System.Threading;
using UXAV.Logging;

namespace UXAV.AVnet.Core.UI
{
    public class ActivityTimeOut
    {
        private readonly ControllerActivityMonitor _monitor;
        private TimeSpan _timeOut;
        private readonly Timer _timer;

        internal ActivityTimeOut(ControllerActivityMonitor monitor, TimeSpan timeOut, bool usesProximity)
        {
            _monitor = monitor;
            UsesProximity = usesProximity;
            _timeOut = timeOut;
            if (_timeOut == TimeSpan.Zero)
            {
                _timer = new Timer(OnTimerCallback, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                return;
            }
            _timer = new Timer(OnTimerCallback, null, _timeOut, Timeout.InfiniteTimeSpan);
        }

        public bool UsesProximity { get; set; }

        public TimeSpan TimeOut
        {
            get => _timeOut;
            set => Reset(value);
        }

        private void OnTimerCallback(object state)
        {
            OnTimedOut(this, new ActivityTimedOutEventArgs(TimeOutEventType.TouchActivtyTimedOut));
        }

        public event ActivityTimedOutEventHandler TimedOut;
        
        internal void Cancel()
        {
            _timeOut = TimeSpan.Zero;
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
        
        internal void HoldOff()
        {
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        internal void Restart()
        {
            if (_timeOut > TimeSpan.Zero)
            {
                Reset(_timeOut);
            }
        }
        
        public void Reset()
        {
            Reset(_timeOut);
        }

        public void Reset(TimeSpan time)
        {
            _timeOut = time;
            if (_timeOut == TimeSpan.Zero)
            {
                _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                return;
            }
            _timer.Change(_timeOut, Timeout.InfiniteTimeSpan);
        }

        internal void NoProximityPresent()
        {
            if (UsesProximity)
            {
                OnTimedOut(this, new ActivityTimedOutEventArgs(TimeOutEventType.NoProximityPresent));
            }
        }

        protected virtual void OnTimedOut(ActivityTimeOut timeOut, ActivityTimedOutEventArgs args)
        {
            try
            {
                _timeOut = TimeSpan.Zero;
                TimedOut?.Invoke(timeOut, args);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }

    public enum TimeOutEventType
    {
        TouchActivtyTimedOut,
        NoProximityPresent
    }

    public class ActivityTimedOutEventArgs : EventArgs
    {
        internal ActivityTimedOutEventArgs(TimeOutEventType eventType)
        {
            EventType = eventType;
        }

        private TimeOutEventType EventType { get; }
    }

    public delegate void ActivityTimedOutEventHandler(ActivityTimeOut timeOut, ActivityTimedOutEventArgs args);
}