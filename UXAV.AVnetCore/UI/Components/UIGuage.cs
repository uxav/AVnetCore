using System;
using UXAV.AVnetCore.DeviceSupport;

namespace UXAV.AVnetCore.UI.Components
{
    public class UIGuage : UIObject, IAnalogItem
    {
        public UIGuage(ISigProvider sigProvider, uint analogJoinNumber)
            : base(sigProvider)
        {
            AnalogJoinNumber = analogJoinNumber;
        }

        public UIGuage(ISigProvider sigProvider, string analogJoinName)
            : base(sigProvider)
        {
            AnalogJoinNumber = sigProvider.SigProvider.UShortInput[analogJoinName].Number;
        }

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
            if (position > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(position), "value must be between 0 and 1");
            }
            SigProvider.UShortInput[AnalogJoinNumber].UShortValue =
                (ushort) Tools.ScaleRange(position, 0, 1, ushort.MinValue, ushort.MinValue);
        }

        public void SetPositionScaled(double fromValue, double fromMinValue, double fromMaxValue)
        {
            SigProvider.UShortInput[AnalogJoinNumber].UShortValue =
                (ushort) Tools.ScaleRange(fromValue, fromMinValue, fromMaxValue, ushort.MinValue, ushort.MaxValue);
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
            get => Tools.ScaleRange(Value, ushort.MinValue, ushort.MaxValue, 0, 1);
            set => SetPosition(value);
        }
    }
}