using UXAV.AVnet.Core.DeviceSupport;

namespace UXAV.AVnet.Core.UI.Components
{
    public class UILabel : UIObject, ITextItem
    {
        public UILabel(ISigProvider sigProvider, uint serialJoinNumber) : base(sigProvider)
        {
            SerialJoinNumber = serialJoinNumber;
        }

        public UILabel(ISigProvider sigProvider, string serialJoinName) : base(sigProvider)
        {
            SerialJoinNumber = sigProvider.SigProvider.StringInput[serialJoinName].Number;
        }

        public uint SerialJoinNumber { get; }

        public void Clear()
        {
            SigProvider.StringInput[SerialJoinNumber].StringValue = string.Empty;
        }

        public void SetText(string text)
        {
            if (text == null) return;
            SigProvider.StringInput[SerialJoinNumber].StringValue = text;
        }

        public string Text
        {
            get => SigProvider.StringInput[SerialJoinNumber].StringValue;
            set => SigProvider.StringInput[SerialJoinNumber].StringValue = value;
        }
    }
}