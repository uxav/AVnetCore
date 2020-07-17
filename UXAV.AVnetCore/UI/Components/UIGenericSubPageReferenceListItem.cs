using System;

namespace UXAV.AVnetCore.UI.Components
{
    public class UIGenericSubPageReferenceListItem : UISubPageReferenceListItem, IButton, ITextItem
    {
        private readonly UITextButton _button;

        public UIGenericSubPageReferenceListItem(UISubPageReferenceList list, uint id)
            : base(list, id)
        {
            _button = new UITextButton(this, BoolOutputSigs[1].Name, BoolInputSigs[1].Name, StringInputSigs[1].Name);
        }

        public uint DigitalJoinNumber => _button.DigitalJoinNumber;
        public bool IsPressed => _button.IsPressed;

        public bool Feedback
        {
            get => _button.Feedback;
            set => _button.Feedback = value;
        }

        public TimeSpan HoldTime => _button.HoldTime;

        public TimeSpan RepeatTime => _button.RepeatTime;

        public void SetupHoldAndRepeatTimes(double holdTimeInSeconds, double repeatTimeInSeconds)
        {
            _button.SetupHoldAndRepeatTimes(holdTimeInSeconds, repeatTimeInSeconds);
        }

        public override void SetFeedback(bool value)
        {
            _button.SetFeedback(value);
        }

        public event ButtonEventHandler ButtonEvent
        {
            add => _button.ButtonEvent += value;
            remove => _button.ButtonEvent -= value;
        }

        public uint SerialJoinNumber => _button.SerialJoinNumber;

        public void SetText(string text)
        {
            _button.SetText(text);
        }

        public string Text
        {
            get => _button.Text;
            set => _button.Text = value;
        }
    }
}