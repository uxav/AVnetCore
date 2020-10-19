using System;
using Crestron.SimplSharpPro;
using UXAV.AVnetCore.DeviceSupport;
using UXAV.Logging;

namespace UXAV.AVnetCore.UI.Components
{
    public class UISlider : UIGuage
    {
        private int _subscribeCount;
        private bool _sigChangesRegistered;
        private UISliderValueChangedEventHandler _valueChanged;
        private readonly UShortInputSig _feedbackJoin;

        public UISlider(ISigProvider sigProvider, uint analogJoinNumber, ushort minValue = ushort.MinValue,
            ushort maxValue = ushort.MaxValue)
            : base(sigProvider, analogJoinNumber, minValue, maxValue)
        {
            _feedbackJoin = sigProvider.SigProvider.UShortInput[analogJoinNumber];
        }

        public UISlider(ISigProvider sigProvider, string analogJoinName, string analogFeedbackJoinName,
            ushort minValue = ushort.MinValue, ushort maxValue = ushort.MaxValue)
            : base(sigProvider, analogJoinName, minValue, maxValue)
        {
            _feedbackJoin = sigProvider.SigProvider.UShortInput[analogFeedbackJoinName];
        }

        public event UISliderValueChangedEventHandler ValueChanged
        {
            add
            {
                if (_subscribeCount == 0)
                    RegisterToSigChanges();
                _subscribeCount++;
                _valueChanged += value;
            }
            remove
            {
                if (_subscribeCount == 0) return;
                _subscribeCount--;
                // ReSharper disable once DelegateSubtraction
                _valueChanged -= value;
                if (_subscribeCount == 0)
                {
                    UnregisterToSigChanges();
                }
            }
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

        private void OnSigChange(SigProviderDevice sigProviderDevice, SigEventArgs args)
        {
            if (args.Event != eSigEvent.UShortChange ||
                args.Sig != sigProviderDevice.UShortOutput[AnalogJoinNumber]) return;
            OnValueChanged(this, args.Sig.UShortValue);
        }

        public new void SetValue(ushort value)
        {
            _feedbackJoin.UShortValue = value;
        }

        public new void SetSignedValue(short value)
        {
            _feedbackJoin.ShortValue = value;
        }

        public new ushort Value
        {
            get => SigProvider.UShortOutput[AnalogJoinNumber].UShortValue;
            set => SetValue(value);
        }

        public new short SignedValue
        {
            get => SigProvider.UShortOutput[AnalogJoinNumber].ShortValue;
            set => SetSignedValue(value);
        }

        public new double Position
        {
            get
            {
                var value = Tools.ScaleRange(Value, MinValue, MaxValue, 0, 1);
                if (value < 0.005)
                {
                    value = 0;
                }

                return value;
            }
            set => SetPosition(value);
        }

        protected virtual void OnValueChanged(UISlider slider, ushort newValue)
        {
            try
            {
                _valueChanged?.Invoke(slider, newValue);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }

    public delegate void UISliderValueChangedEventHandler(UISlider slider, ushort newValue);
}