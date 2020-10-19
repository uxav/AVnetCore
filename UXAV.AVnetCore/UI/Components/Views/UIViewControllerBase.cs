using System;
using System.Timers;
using UXAV.AVnetCore.DeviceSupport;
using UXAV.AVnetCore.Models;
using UXAV.Logging;
using Timer = System.Timers.Timer;

namespace UXAV.AVnetCore.UI.Components.Views
{
    /// <summary>
    /// Base view controller class
    /// </summary>
    public abstract class UIViewControllerBase : UIObject, IVisibleItem, IGenericItem
    {
        private readonly Timer _timeOut;

        /// <summary>
        /// 
        /// </summary>
        internal UIViewControllerBase(ISigProvider sigProvider, uint visibleJoinNumber)
            : base(sigProvider)
        {
            VisibleJoinNumber = visibleJoinNumber;
            Parent = sigProvider as IVisibleItem;
            if (Parent != null)
            {
                Parent.VisibilityChanged += ParentVisibilityChanged;
            }

            _timeOut = new Timer(TimeSpan.FromSeconds(60).TotalMilliseconds) {AutoReset = false};
            _timeOut.Elapsed += TimeOutOnElapsed;

            Logger.Debug($"Created {GetType().Name} with join {VisibleJoinNumber}");
            if (Parent != null)
            {
                Logger.Debug($"View has parent: {Parent.GetType().Name} with join {Parent.VisibleJoinNumber}");
            }
        }

        /// <summary>
        /// Triggered when the visibility changes on the view
        /// </summary>
        public event VisibilityChangeEventHandler VisibilityChanged;

        public Core3ControllerBase Core3Controller => Core3Controllers.Get(SigProvider.Device.ID);

        /// <summary>
        /// Parent visible item which this follows on hide
        /// </summary>
        public IVisibleItem Parent { get; }

        public uint VisibleJoinNumber { get; }

        public uint Id => VisibleJoinNumber;

        public TimeSpan TimeOutTime => TimeSpan.FromMilliseconds(_timeOut.Interval);

        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// True if currently visible
        /// </summary>
        public virtual bool Visible
        {
            get => VisibleJoinNumber == 0 || SigProvider.BooleanInput[VisibleJoinNumber].BoolValue;
            protected set
            {
                if (VisibleJoinNumber == 0 || SigProvider.BooleanInput[VisibleJoinNumber].BoolValue == value) return;

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
            }
        }

        public bool RequestedVisibleState { get; protected set; }

        /// <summary>
        /// Show the view. Also cancels a timeout if active.
        /// </summary>
        public virtual void Show()
        {
            _timeOut.Stop();
            Visible = true;
        }

        /// <summary>
        /// Show the view with a timeout. Call again to reset the timeout.
        /// </summary>
        /// <param name="time">TimeSpan duration to timeout</param>
        public virtual void Show(TimeSpan time)
        {
            if (time.TotalMilliseconds > 0)
            {
                _timeOut.Interval = time.TotalMilliseconds;
                _timeOut.Start();
            }

            Visible = true;
        }

        /// <summary>
        /// Set the timeout if the page is already showing
        /// </summary>
        /// <param name="time">TimeSpan duration to timeout</param>
        protected void SetTimeOut(TimeSpan time)
        {
            _timeOut.Stop();
            _timeOut.Interval = time.TotalMilliseconds;
            _timeOut.Enabled = true;
            _timeOut.Start();
        }

        /// <summary>
        /// Hide the view
        /// </summary>
        public void Hide()
        {
            Visible = false;
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
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }

                    _timeOut.Stop();
                    break;
            }

            handler?.Invoke(item, args);
        }

        private void TimeOutOnElapsed(object sender, ElapsedEventArgs e)
        {
            Hide();
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Parent != null)
                    Parent.VisibilityChanged -= ParentVisibilityChanged;
            }

            base.Dispose(disposing);
        }
    }
}