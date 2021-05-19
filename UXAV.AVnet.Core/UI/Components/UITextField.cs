using System;
using Crestron.SimplSharpPro;
using UXAV.AVnet.Core.DeviceSupport;
using UXAV.Logging;

namespace UXAV.AVnet.Core.UI.Components
{
    public class UITextField : UIObject, ITextItem
    {
        private string _text;

        public UITextField(ISigProvider sigProvider, uint indirectTextJoinNumber, uint textOutJoinNumber,
            uint enterJoinNumber = 0, uint escapeJoinNumber = 0, uint focusJoinNumber = 0)
            : base(sigProvider)
        {
            SerialJoinNumber = indirectTextJoinNumber;
            TextOutJoinNumber = textOutJoinNumber;
            EnterJoinNumber = enterJoinNumber;
            EscapeJoinNumber = escapeJoinNumber;
            FocusJoinNumber = focusJoinNumber;
            SigProvider.SigChange += SigProviderOnSigChange;
        }

        public event UITextFieldEventHandler HasChanged;

        private void SigProviderOnSigChange(SigProviderDevice device, SigEventArgs args)
        {
            if (args.Event == eSigEvent.StringChange && args.Sig.Number == TextOutJoinNumber)
            {
                _text = args.Sig.StringValue;
                OnHasChanged(this, new UITextFieldEventArgs(UITextFieldEventType.TextChanged, _text));
            }

            if (args.Event == eSigEvent.BoolChange && EnterJoinNumber > 0 && args.Sig.Number == EnterJoinNumber && args.Sig.BoolValue)
            {
                OnEnter();
            }

            if (args.Event == eSigEvent.BoolChange && EscapeJoinNumber > 0 && args.Sig.Number == EscapeJoinNumber && args.Sig.BoolValue)
            {
                OnEscape();
            }

            if (args.Event == eSigEvent.BoolChange && FocusJoinNumber > 0 && args.Sig.Number == FocusJoinNumber)
            {
                HasFocus = args.Sig.BoolValue;
                OnFocusChange(args.Sig.BoolValue);
            }
        }

        public void SetText(string text)
        {
            Text = text;
        }

        public uint SerialJoinNumber { get; }
        public uint TextOutJoinNumber { get; }
        public uint EnterJoinNumber { get; }
        public uint EscapeJoinNumber { get; }
        public uint FocusJoinNumber { get; }

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                SigProvider.StringInput[SerialJoinNumber].StringValue = _text;
            }
        }

        public bool HasFocus
        {
            set
            {
                if(FocusJoinNumber == 0) return;
                SigProvider.BooleanInput[FocusJoinNumber].BoolValue = value;
            }
            get => FocusJoinNumber != 0 && SigProvider.BooleanOutput[FocusJoinNumber].BoolValue;
        }

        protected virtual void OnEnter()
        {
            OnHasChanged(this, new UITextFieldEventArgs(UITextFieldEventType.DidEnter, _text));
        }

        protected virtual void OnEscape()
        {
            OnHasChanged(this, new UITextFieldEventArgs(UITextFieldEventType.DidEscape, _text));
        }

        protected virtual void OnFocusChange(bool hasFocus)
        {
            OnHasChanged(this, new UITextFieldEventArgs(UITextFieldEventType.FocusChanged, _text));
        }

        protected virtual void OnHasChanged(UITextField textfield, UITextFieldEventArgs args)
        {
            try
            {
                HasChanged?.Invoke(textfield, args);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }

    public enum UITextFieldEventType
    {
        TextChanged,
        DidEnter,
        DidEscape,
        FocusChanged,
    }

    public class UITextFieldEventArgs : EventArgs
    {
        public UITextFieldEventType EventType { get; }
        public string TextValue { get; }

        internal UITextFieldEventArgs(UITextFieldEventType eventType, string textValue)
        {
            EventType = eventType;
            TextValue = textValue;
        }
    }

    public delegate void UITextFieldEventHandler(UITextField textField, UITextFieldEventArgs args);
}