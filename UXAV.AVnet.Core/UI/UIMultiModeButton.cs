using UXAV.AVnet.Core.DeviceSupport;
using UXAV.AVnet.Core.UI.Components;

namespace UXAV.AVnet.Core.UI
{
    public class UIMultiModeButton : UIButton, IAnalogItem
    {
        public UIMultiModeButton(ISigProvider sigProvider, uint digitalJoinNumber, uint analogJoinNumber,
            uint enableJoinNumber = 0, uint visibleJoinNumber = 0, uint id = 0)
            : base(sigProvider, digitalJoinNumber, enableJoinNumber, visibleJoinNumber, id)
        {
            AnalogJoinNumber = analogJoinNumber;
        }

        public UIMultiModeButton(ISigProvider sigProvider, string pressJoinName, string feedbackJoinName,
            string analogJoinName)
            : base(sigProvider, pressJoinName, feedbackJoinName)
        {
            AnalogJoinNumber = SigProvider.UShortInput[analogJoinName].Number;
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

        public virtual void SetPosition(double position)
        {
        }

        public ushort Value
        {
            get => SigProvider.UShortInput[AnalogJoinNumber].UShortValue;
            set => SigProvider.UShortInput[AnalogJoinNumber].UShortValue = value;
        }

        public short SignedValue
        {
            get => SigProvider.UShortInput[AnalogJoinNumber].ShortValue;
            set => SigProvider.UShortInput[AnalogJoinNumber].ShortValue = value;
        }

        public virtual double Position { get; set; }
    }
}