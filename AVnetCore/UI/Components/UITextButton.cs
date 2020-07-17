using UXAV.AVnetCore.DeviceSupport;

namespace UXAV.AVnetCore.UI.Components
{
    public class UITextButton : UIButton, ITextItem
    {
        public UITextButton(ISigProvider sigProvider, uint digitalJoinNumber, uint serialJoinNumber)
            : base(sigProvider, digitalJoinNumber)
        {
            SerialJoinNumber = serialJoinNumber;
        }

        public UITextButton(ISigProvider sigProvider, uint digitalJoinNumber, uint enableJoinNumber,
            uint visibleJoinNumber, uint serialJoinNumber)
            : base(sigProvider, digitalJoinNumber, enableJoinNumber, visibleJoinNumber)
        {
            SerialJoinNumber = serialJoinNumber;
        }

        public UITextButton(ISigProvider sigProvider, string pressJoinName, string feedbackJoinName,
            string serialJoinName)
            : base(sigProvider, pressJoinName, feedbackJoinName)
        {
            SerialJoinNumber = SigProvider.StringInput[serialJoinName].Number;
        }

        public UITextButton(ISigProvider sigProvider, string pressJoinName,
            string feedbackJoinName, string enableJoinName, string visibleJoinName, string serialJoinName)
            : base(sigProvider, pressJoinName, feedbackJoinName, enableJoinName, visibleJoinName)
        {
            SerialJoinNumber = SigProvider.StringInput[serialJoinName].Number;
        }

        public uint SerialJoinNumber { get; }

        public string Text
        {
            get => SigProvider.StringInput[SerialJoinNumber].StringValue;
            set => SigProvider.StringInput[SerialJoinNumber].StringValue = value;
        }

        public void SetText(string text)
        {
            SigProvider.StringInput[SerialJoinNumber].StringValue = text;
        }
    }
}