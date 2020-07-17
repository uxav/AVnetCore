using System;
using Crestron.SimplSharpPro;
using UXAV.AVnetCore.Logging;

namespace UXAV.AVnetCore.UI.Components
{
    public sealed class UIDynamicButtonListItem : UIObject, IButton, IVisibleItem, IEnableItem, ITextItem
    {
        private readonly UITextButton _button;
        private readonly UShortInputSig _iconAnalogSig;
        private readonly StringInputSig _iconSerialSig;

        /// <summary>
        /// Creates a base class for subpage reference list items
        /// </summary>
        /// <param name="list">The subpage list</param>
        /// <param name="id">The index of the item</param>
        internal UIDynamicButtonListItem(UIDynamicButtonList list, uint id)
            : base(list)
        {
            try
            {
                List = list;
                Id = id;

                _iconAnalogSig = list.SigProvider.UShortInput[$"Set Item {Id} Icon Analog"];
                _iconSerialSig = list.SigProvider.StringInput[$"Set Item {Id} Icon Serial"];

                _button = new UITextButton(list, $"Item {Id} Pressed", $"Item {Id} Selected",
                    $"Item {Id} Enabled", $"Item {Id} Visible", $"Set Item {Id} Text");
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public uint DigitalJoinNumber => _button.DigitalJoinNumber;
        public uint EnableJoinNumber => _button.EnableJoinNumber;

        public bool Enabled
        {
            get => _button.Enabled;
            set => _button.Enabled = value;
        }

        public void Enable()
        {
            _button.Enable();
        }

        public void Disable()
        {
            _button.Disable();
        }

        public event VisibilityChangeEventHandler VisibilityChanged
        {
            add => _button.VisibilityChanged += value;
            remove => _button.VisibilityChanged -= value;
        }

        public bool Visible
        {
            get => _button.Visible;
            set => _button.Visible = value;
        }

        public bool RequestedVisibleState => _button.RequestedVisibleState;
        public uint VisibleJoinNumber => _button.VisibleJoinNumber;

        public void Show()
        {
            _button.Show();
        }

        public void Hide()
        {
            _button.Hide();
        }

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

        public void SetFeedback(bool value)
        {
            _button.SetFeedback(value);
        }

        public void SetId(uint id)
        {
            throw new NotSupportedException("Cannot set an ID value");
        }

        public string Name => _button.Name;

        public event ButtonEventHandler ButtonEvent
        {
            add => _button.ButtonEvent += value;
            remove => _button.ButtonEvent -= value;
        }

        public UIButton Button => _button;

        public UIDynamicButtonList List { get; }

        public ushort IconNumber
        {
            get => _iconAnalogSig.UShortValue;
            set => _iconAnalogSig.UShortValue = value;
        }

        public string IconName
        {
            get => _iconSerialSig.StringValue;
            set => _iconSerialSig.StringValue = value;
        }

        public uint Id { get; }

        public object LinkedObject { get; set; }

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