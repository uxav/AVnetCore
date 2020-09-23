using System;
using System.Diagnostics;
using System.Timers;
using Crestron.SimplSharpPro;
using UXAV.AVnetCore.DeviceSupport;
using UXAV.Logging;

namespace UXAV.AVnetCore.UI.Components
{
    public class UIButton : UIObject, IButton, IVisibleItem, IEnableItem
    {
        private int _subscribeCount;
        private ButtonEventHandler _buttonEvent;
        private bool _sigChangesRegistered;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private TimeSpan _holdTime = TimeSpan.FromSeconds(1);
        private TimeSpan _repeatTime = TimeSpan.Zero;
        private readonly Timer _holdTimer;
        private readonly Timer _holdRepeatTimer;
        private bool _held;
        private readonly string _feedbackJoinName;
        private string _name;
        private uint? _id;

        public UIButton(ISigProvider sigProvider, uint digitalJoinNumber)
            : base(sigProvider)
        {
            DigitalJoinNumber = digitalJoinNumber;
            _holdTimer = new Timer();
            _holdTimer.Elapsed += HoldTimerOnElapsed;
            _holdTimer.AutoReset = false;
            _holdRepeatTimer = new Timer();
            _holdRepeatTimer.Elapsed += HoldTimerOnElapsed;
            _holdRepeatTimer.AutoReset = true;
        }

        public UIButton(ISigProvider sigProvider, uint digitalJoinNumber, uint enableJoinNumber, uint visibleJoinNumber)
            : this(sigProvider, digitalJoinNumber)
        {
            EnableJoinNumber = enableJoinNumber;
            VisibleJoinNumber = visibleJoinNumber;
        }

        public UIButton(ISigProvider sigProvider, string pressJoinName, string feedbackJoinName)
            : base(sigProvider)
        {
            DigitalJoinNumber = SigProvider.BooleanOutput[pressJoinName].Number;
            _feedbackJoinName = feedbackJoinName;
            _holdTimer = new Timer();
            _holdTimer.Elapsed += HoldTimerOnElapsed;
            _holdTimer.AutoReset = false;
            _holdRepeatTimer = new Timer();
            _holdRepeatTimer.Elapsed += HoldTimerOnElapsed;
            _holdRepeatTimer.AutoReset = true;
        }

        public UIButton(ISigProvider sigProvider, string pressJoinName, string feedbackJoinName, string enableJoinName,
            string visibleJoinName)
            : this(sigProvider, pressJoinName, feedbackJoinName)
        {
            EnableJoinNumber = SigProvider.BooleanInput[enableJoinName].Number;
            VisibleJoinNumber = SigProvider.BooleanInput[visibleJoinName].Number;
        }

        public uint DigitalJoinNumber { get; }
        public uint EnableJoinNumber { get; }
        public uint VisibleJoinNumber { get; }

        public virtual uint Id
        {
            get
            {
                if (_id == null) return 0;
                return (uint) _id;
            }
        }

        public virtual string Name
        {
            get
            {
                if (string.IsNullOrEmpty(_name))
                {
                    if (_id != null)
                    {
                        return $"Button {_id}";
                    }

                    return $"Button with join {DigitalJoinNumber}";
                }

                return _name;
            }
            set => _name = value;
        }

        public bool IsPressed => SigProvider.BooleanOutput[DigitalJoinNumber].BoolValue;

        public bool Feedback
        {
            get
            {
                if (string.IsNullOrEmpty(_feedbackJoinName))
                {
                    return SigProvider.BooleanInput[DigitalJoinNumber].BoolValue;
                }

                return SigProvider.BooleanInput[_feedbackJoinName].BoolValue;
            }
            set
            {
                if (string.IsNullOrEmpty(_feedbackJoinName))
                {
                    SigProvider.BooleanInput[DigitalJoinNumber].BoolValue = value;
                }

                SigProvider.BooleanInput[_feedbackJoinName].BoolValue = value;
            }
        }

        /// <summary>
        /// True if the item is visible
        /// </summary>
        public virtual bool Visible
        {
            get => VisibleJoinNumber == 0 || SigProvider.BooleanInput[VisibleJoinNumber].BoolValue;
            set
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

        public virtual bool Enabled
        {
            get => EnableJoinNumber == 0 || SigProvider.BooleanInput[EnableJoinNumber].BoolValue;
            set
            {
                if (EnableJoinNumber == 0) return;
                SigProvider.BooleanInput[EnableJoinNumber].BoolValue = value;
            }
        }

        public bool RequestedVisibleState { get; protected set; }

        public TimeSpan HoldTime
        {
            get => _holdTime;
            private set => _holdTime = value;
        }

        public TimeSpan RepeatTime
        {
            get => _repeatTime;
            private set => _repeatTime = value;
        }

        public void SetupHoldAndRepeatTimes(double holdTimeInSeconds, double repeatTimeInSeconds)
        {
            HoldTime = TimeSpan.FromSeconds(holdTimeInSeconds);
            RepeatTime = TimeSpan.FromSeconds(repeatTimeInSeconds);
        }

        public event ButtonEventHandler ButtonEvent
        {
            add
            {
                if (_subscribeCount == 0)
                    RegisterToSigChanges();
                _subscribeCount++;
                _buttonEvent += value;
            }
            remove
            {
                if (_subscribeCount == 0) return;
                _subscribeCount--;
                // ReSharper disable once DelegateSubtraction
                _buttonEvent -= value;
                if (_subscribeCount == 0)
                {
                    UnregisterToSigChanges();
                }
            }
        }

        /// <summary>
        /// Subscribe to visibility change events
        /// </summary>
        public event VisibilityChangeEventHandler VisibilityChanged;

        public void SetFeedback(bool value)
        {
            if (string.IsNullOrEmpty(_feedbackJoinName))
            {
                SigProvider.BooleanInput[DigitalJoinNumber].BoolValue = value;
            }

            SigProvider.BooleanInput[_feedbackJoinName].BoolValue = value;
        }

        private void RegisterToSigChanges()
        {
            if (_sigChangesRegistered) return;
            SigProvider.SigChange += OnSigChange;
            _sigChangesRegistered = true;
        }

        private void UnregisterToSigChanges()
        {
            if (!_sigChangesRegistered) return;
            SigProvider.SigChange -= OnSigChange;
            _sigChangesRegistered = false;
        }

        public void Show()
        {
            Visible = true;
        }

        public void Hide()
        {
            Visible = false;
        }

        public void Enable()
        {
            Enabled = true;
        }

        public void Disable()
        {
            Enabled = false;
        }

        public void SetId(uint id)
        {
            _id = id;
        }

        private void OnSigChange(SigProviderDevice sigProviderDevice, SigEventArgs args)
        {
            if (args.Event != eSigEvent.BoolChange || args.Sig.Number != DigitalJoinNumber) return;

            if (args.Sig.BoolValue)
            {
                _stopwatch.Start();
                if (_holdTime.TotalMilliseconds > 0)
                {
                    _holdTimer.Interval = _holdTime.TotalMilliseconds;
                    _holdTimer.Start();
                }

                OnButtonEvent(this, new ButtonEventArgs(ButtonEventType.Pressed, _stopwatch.Elapsed));
            }
            else
            {
                _holdTimer.Stop();
                _holdRepeatTimer.Stop();
                _stopwatch.Stop();
                if (!_held)
                {
                    OnButtonEvent(this, new ButtonEventArgs(ButtonEventType.Tapped, _stopwatch.Elapsed));
                }

                _held = false;
                OnButtonEvent(this, new ButtonEventArgs(ButtonEventType.Released, _stopwatch.Elapsed));
                _stopwatch.Reset();
            }
        }

        private void HoldTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            if (!_held)
            {
                _held = true;
                if (_repeatTime.TotalMilliseconds > 0)
                {
                    _holdRepeatTimer.Interval = _repeatTime.TotalMilliseconds;
                    _holdRepeatTimer.Start();
                }

                OnButtonEvent(this, new ButtonEventArgs(ButtonEventType.Held, _stopwatch.Elapsed));
            }
            else
            {
                OnButtonEvent(this, new ButtonEventArgs(ButtonEventType.HoldRepeat, _stopwatch.Elapsed));
            }

            if (_subscribeCount != 0) return;
            _stopwatch.Stop();
            _holdTimer.Stop();
            _holdRepeatTimer.Stop();
        }

        protected virtual void OnButtonEvent(UIButton button, ButtonEventArgs args)
        {
#if DEBUG
            Logger.Log("{0}, {1} {2}", this, args.EventType, args.HoldTime);
#endif
            var handler = _buttonEvent;
            try
            {
                handler?.Invoke(button, args);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        protected virtual void OnVisibilityChanged(IVisibleItem item, VisibilityChangeEventArgs args)
        {
            VisibilityChanged?.Invoke(item, args);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _holdTimer.Dispose();
        }

        public override string ToString()
        {
            return $"{SigProvider.Device} Button {DigitalJoinNumber}";
        }
    }

    /// <summary>
    /// A handler delegate for a button event
    /// </summary>
    /// <param name="button">The button which triggered the event</param>
    /// <param name="args">Information about the event</param>
    public delegate void ButtonEventHandler(IButton button, ButtonEventArgs args);

    /// <summary>
    /// Args for a button event
    /// </summary>
    public class ButtonEventArgs : EventArgs
    {
        internal ButtonEventArgs(ButtonEventType eventType, TimeSpan timeSincePress)
        {
            EventType = eventType;
            HoldTime = timeSincePress;
        }

        internal ButtonEventArgs(ButtonEventType eventType, TimeSpan timeSincePress, UIButtonCollection collection,
            uint collectionKey)
        {
            EventType = eventType;
            HoldTime = timeSincePress;
            CalledFromCollection = true;
            Collection = collection;
            CollectionKey = collectionKey;
        }

        /// <summary>
        /// The button event type occuring for this event
        /// </summary>
        public ButtonEventType EventType { get; }

        /// <summary>
        /// The time the button has been held since a press occured
        /// </summary>
        public TimeSpan HoldTime { get; }

        /// <summary>
        /// True if the event was called from a button collection
        /// </summary>
        public bool CalledFromCollection { get; }

        /// <summary>
        /// Returns a collection if the event is called from a collection
        /// </summary>
        public UIButtonCollection Collection { get; }

        /// <summary>
        /// The key value of the button in the collection
        /// </summary>
        public uint CollectionKey { get; }
    }

    /// <summary>
    /// Describes the type of button event occuring
    /// </summary>
    public enum ButtonEventType
    {
        /// <summary>
        /// The button was pressed
        /// </summary>
        Pressed,

        /// <summary>
        /// The button was pressed and immediately released before a hold
        /// </summary>
        Tapped,

        /// <summary>
        /// The button was held
        /// </summary>
        Held,

        /// <summary>
        /// The button is being held and a repeat event was triggered
        /// </summary>
        HoldRepeat,

        /// <summary>
        /// The button was released
        /// </summary>
        Released
    }
}