using System;
using System.Drawing;
using System.Threading;
using Crestron.SimplSharpPro;
using UXAV.AVnet.Core.DeviceSupport;

namespace UXAV.AVnet.Core.UI.Components.Views
{
    public class UIPinCodeView : UISubPageViewController

    {
        private readonly UIButtonCollection _keypadButtons;
        private readonly UILabel _pinCodeLabel;
        private readonly UILabel _titleLabel;
        private Action _callback;
        private string _code;
        private string _enteredCode;
        private Timer _resetTimer;

        public UIPinCodeView(ISigProvider sigProvider, uint visibleJoinNumber, SmartObject keypadSmartObject,
            uint titleJoinNumber, uint codeJoinNumber)
            : base(sigProvider, visibleJoinNumber, createTimeOutWithProximity: false)
        {
            var keypad = new UIKeypad(keypadSmartObject);
            _keypadButtons = new UIButtonCollection(keypad.Buttons);
            _titleLabel = new UILabel(this, titleJoinNumber);
            _pinCodeLabel = new UILabel(this, codeJoinNumber);
        }

        public UIPinCodeView(ISigProvider sigProvider, uint visibleJoinNumber, uint keypadJoinNumber,
            uint titleJoinNumber, uint codeJoinNumber)
            : base(sigProvider, visibleJoinNumber, createTimeOutWithProximity: false)
        {
            var keypad = new UIKeypad(this, keypadJoinNumber);
            _keypadButtons = new UIButtonCollection(keypad.Buttons);
            _titleLabel = new UILabel(this, titleJoinNumber);
            _pinCodeLabel = new UILabel(this, codeJoinNumber);
        }

        private string EnteredCode
        {
            get => _enteredCode;
            set
            {
                _enteredCode = value;
                var stars = string.Empty;
                for (var i = 0; i < _enteredCode.Length; i++) stars += '*';
                _pinCodeLabel.SetText(stars);
            }
        }

        public Color ErrorTextColor { get; set; } = Color.DarkOrange;

        public override void Show()
        {
            throw new NotSupportedException("Use method with callback");
        }

        public override void Show(TimeSpan time)
        {
            throw new NotSupportedException("Use method with callback");
        }

        public void Show(string title, string code, Action successCallback)
        {
            _titleLabel.SetText(title);
            _code = code;
            _callback = successCallback;
            base.Show(TimeSpan.FromSeconds(10));
        }

        protected override void WillShow()
        {
            EnteredCode = string.Empty;
            _keypadButtons.ButtonEvent += KeypadButtonsOnButtonEvent;
            if (_resetTimer != null) return;
            _resetTimer = new Timer(state => { _pinCodeLabel.Clear(); }, null, Timeout.InfiniteTimeSpan,
                Timeout.InfiniteTimeSpan);
        }

        protected override void DidShow()
        {
        }

        protected override void WillHide()
        {
            _keypadButtons.ButtonEvent -= KeypadButtonsOnButtonEvent;
        }

        protected override void DidHide()
        {
        }

        private void KeypadButtonsOnButtonEvent(IButton button, ButtonEventArgs args)
        {
            if (args.EventType != ButtonEventType.Pressed) return;
            _resetTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            switch (args.CollectionKey)
            {
                case 10:
                    Hide();
                    break;
                case 11:
                    if (EnteredCode == _code)
                    {
                        Hide();
                        _callback();
                    }
                    else
                    {
                        EnteredCode = string.Empty;
                        var color = ErrorTextColor;
                        var value = $"<FONT color=\"#{color.R:X2}{color.G:X2}{color.B:X2}\">Incorrect</FONT>";
                        _pinCodeLabel.SetText(value);
                        _resetTimer.Change(TimeSpan.FromSeconds(1), Timeout.InfiniteTimeSpan);
                    }

                    break;
                default:
                    if (EnteredCode.Length < _code.Length)
                        EnteredCode += args.CollectionKey.ToString();
                    else
                        EnteredCode = args.CollectionKey.ToString();
                    break;
            }
        }
    }
}