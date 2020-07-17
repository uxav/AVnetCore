using UXAV.AVnetCore.DeviceSupport;

namespace UXAV.AVnetCore.UI.Components
{
    public class UILabel : UIObject, ITextItem
    {
        public UILabel(ISigProvider sigProvider, uint serialJoinNumber) : base(sigProvider)
        {
            SerialJoinNumber = serialJoinNumber;
        }

        public uint SerialJoinNumber { get; }

        public void SetText(string text)
        {
            SigProvider.StringInput[SerialJoinNumber].StringValue = text;
        }

        public string Text
        {
            get => SigProvider.StringInput[SerialJoinNumber].StringValue;
            set => SigProvider.StringInput[SerialJoinNumber].StringValue = value;
        }
    }
}