using System;
using System.Threading;
using UXAV.AVnet.Core.DeviceSupport;
using UXAV.AVnet.Core.Models;
using UXAV.Logging;

namespace UXAV.AVnet.Core.UI.Components.Views
{
    /// <summary>
    ///     Base view controller class
    /// </summary>
    public abstract class UIViewControllerBase : UIObject, IVisibleItem, IGenericItem
    {
        private readonly Mutex _mutex = new Mutex();

        /// <summary>
        /// </summary>
        internal UIViewControllerBase(ISigProvider sigProvider, uint visibleJoinNumber,
            bool createTimeOutWithProximity = false)
            : base(sigProvider)
        {
            VisibleJoinNumber = visibleJoinNumber;
            Parent = sigProvider as IVisibleItem;
            if (Parent != null) Parent.VisibilityChanged += ParentVisibilityChanged;

            Logger.Debug($"Created {GetType().Name} with join {VisibleJoinNumber}");
            if (Parent != null)
                Logger.Debug($"View has parent: {Parent.GetType().Name} with join {Parent.VisibleJoinNumber}");

            if (createTimeOutWithProximity) CreateTimeOut(TimeSpan.Zero, true);
        }

        public Core3ControllerBase Core3Controller => Core3Controllers.Get(SigProvider.Device.ID);

        /// <summary>
        ///     Parent visible item which this follows on hide
        /// </summary>
        public IVisibleItem Parent { get; }

        public ActivityTimeOut TimeOut { get; private set; }

        public uint Id => VisibleJoinNumber;

        public string Name { get; set; } = string.Empty;

        /// <summary>
        ///     Triggered when the visibility changes on the view
        /// </summary>
        public event VisibilityChangeEventHandler VisibilityChanged;

        public uint VisibleJoinNumber { get; }

        /// <summary>
        ///     True if currently visible
        /// </summary>
        public virtual bool Visible
        {
            get => VisibleJoinNumber == 0 || SigProvider.BooleanInput[VisibleJoinNumber].BoolValue;
            protected set
            {
                _mutex.WaitOne();
                if (VisibleJoinNumber == 0 || SigProvider.BooleanInput[VisibleJoinNumber].BoolValue == value)
                {
                    _mutex.ReleaseMutex();
                    return;
                }

                RequestedVisibleState = value;

                OnVisibilityChanged(this,
                    new VisibilityChangeEventArgs(value, value
                        ? VisibilityChangeEventType.WillShow
                        : VisibilityChangeEventType.WillHide));

                SigProvider.BooleanInput[VisibleJoinNumber].BoolValue = value;

                OnVisibilityChanged(this,
                    new VisibilityChangeEventArgs(value, value
                        ? VisibilityChangeEventType.DidShow
                        : VisibilityChangeEventType.DidHide));
                _mutex.ReleaseMutex();
            }
        }

        public bool RequestedVisibleState { get; protected set; }

        /// <summary>
        ///     Show the view. Also cancels a timeout if active.
        /// </summary>
        public virtual void Show()
        {
            TimeOut?.Reset(TimeSpan.Zero);
            Visible = true;
        }

        /// <summary>
        ///     Hide the view
        /// </summary>
        public void Hide()
        {
            Visible = false;
        }

        /// <summary>
        ///     Show the view with a timeout. Call again to reset the timeout.
        /// </summary>
        /// <param name="time">TimeSpan duration to timeout</param>
        public virtual void Show(TimeSpan time)
        {
            Logger.Debug($"{this} {nameof(Show)} with Timeout: {time}");
            if (TimeOut == null && time > TimeSpan.Zero)
                CreateTimeOut(time);
            else
                TimeOut?.Reset(time);

            Visible = true;
        }

        /// <summary>
        ///     Set the timeout if the page is already showing. Resets timer each time it set.
        /// </summary>
        /// <param name="time">TimeSpan duration to timeout</param>
        protected void SetTimeOut(TimeSpan time)
        {
            Logger.Debug($"{this} {nameof(SetTimeOut)}(time = {time})");
            if (TimeOut == null && time > TimeSpan.Zero)
                CreateTimeOut(time);
            else if (TimeOut != null) TimeOut.TimeOut = time;
        }

        private void CreateTimeOut(TimeSpan time, bool useProximity = false)
        {
            TimeOut = ActivityMonitor.CreateTimeOut(this, time, useProximity);
            TimeOut.TimedOut += OnTimedOut;
        }

        protected void OnVisibilityChanged(IVisibleItem item, VisibilityChangeEventArgs args)
        {
            var handler = VisibilityChanged;
            Logger.Debug("{0} ({1}) : {2}", GetType().Name, VisibleJoinNumber, args.EventType);
            switch (args.EventType)
            {
                case VisibilityChangeEventType.WillShow:
                    try
                    {
                        WillShow();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }

                    break;
                case VisibilityChangeEventType.DidShow:
                    try
                    {
                        DidShow();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }

                    break;
                case VisibilityChangeEventType.WillHide:
                    try
                    {
                        WillHide();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }

                    break;
                case VisibilityChangeEventType.DidHide:
                    try
                    {
                        DidHide();
                        TimeOut?.Cancel();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }

                    break;
            }

            handler?.Invoke(item, args);
        }

        protected abstract void WillShow();

        protected abstract void DidShow();

        protected abstract void WillHide();

        protected abstract void DidHide();

        private void ParentVisibilityChanged(IVisibleItem item, VisibilityChangeEventArgs args)
        {
            if (args.EventType != VisibilityChangeEventType.DidHide) return;
            Hide();
        }

        protected virtual void OnTimedOut(ActivityTimeOut timeout, ActivityTimedOutEventArgs args)
        {
            Logger.Debug($"{this}, Timed Out!");
            Hide();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                if (Parent != null)
                    Parent.VisibilityChanged -= ParentVisibilityChanged;

            base.Dispose(disposing);
        }

        public override string ToString()
        {
            return $"{GetType().Name}, view with join {VisibleJoinNumber}";
        }
    }
}