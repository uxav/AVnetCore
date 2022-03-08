using System;
using UXAV.AVnet.Core.DeviceSupport;

namespace UXAV.AVnet.Core.UI.Components
{
    public class UIGuage : UIObject, IAnalogItem
    {
        public UIGuage(ISigProvider sigProvider, uint analogJoinNumber, ushort minValue = ushort.MinValue,
            ushort maxValue = ushort.MaxValue)
            : base(sigProvider)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            AnalogJoinNumber = analogJoinNumber;
        }

        public UIGuage(ISigProvider sigProvider, string analogFeedbackJoinName, ushort minValue = ushort.MinValue,
            ushort maxValue = ushort.MaxValue)
            : base(sigProvider)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            AnalogJoinNumber = sigProvider.SigProvider.UShortInput[analogFeedbackJoinName].Number;
        }

        public ushort MinValue { get; }

        public ushort MaxValue { get; }

        public uint AnalogJoinNumber { get; }

        public void SetValue(ushort value)
        {
            SigProvider.UShortInput[AnalogJoinNumber].UShortValue = value;
        }

        public void SetSignedValue(short value)
        {
            SigProvider.UShortInput[AnalogJoinNumber].ShortValue = value;
        }

        public void SetPosition(double position)
        {
            if (position > 1) throw new ArgumentOutOfRangeException(nameof(position), "value must be between 0 and 1");
            SigProvider.UShortInput[AnalogJoinNumber].UShortValue =
                (ushort)Tools.ScaleRange(position, 0, 1, MinValue, MaxValue);
        }

        public ushort Value
        {
            get => SigProvider.UShortInput[AnalogJoinNumber].UShortValue;
            set => SetValue(value);
        }

        public short SignedValue
        {
            get => SigProvider.UShortInput[AnalogJoinNumber].ShortValue;
            set => SetSignedValue(value);
        }

        public double Position
        {
            get => Tools.ScaleRange(Value, MinValue, MaxValue, 0, 1);
            set => SetPosition(value);
        }

        public void SetPositionScaled(double fromValue, double fromMinValue, double fromMaxValue)
        {
            SigProvider.UShortInput[AnalogJoinNumber].UShortValue =
                (ushort)Tools.ScaleRange(fromValue, fromMinValue, fromMaxValue, MinValue, MaxValue);
        }
    }
}