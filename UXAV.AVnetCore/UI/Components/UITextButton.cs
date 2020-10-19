using UXAV.AVnetCore.DeviceSupport;

namespace UXAV.AVnetCore.UI.Components
{
    public class UITextButton : UIButton, ITextItem
    {
        public UITextButton(ISigProvider sigProvider, uint digitalJoinNumber, uint serialJoinNumber,
            uint enableJoinNumber = 0, uint visibleJoinNumber = 0, uint id = 0)
            : base(sigProvider, digitalJoinNumber, enableJoinNumber, visibleJoinNumber, id)
        {
            SerialJoinNumber = serialJoinNumber;
        }

        public UITextButton(ISigProvider sigProvider, string pressJoinName, string feedbackJoinName,
            string serialJoinName, uint id = 0)
            : base(sigProvider, pressJoinName, feedbackJoinName, id)
        {
            SerialJoinNumber = SigProvider.StringInput[serialJoinName].Number;
        }

        public UITextButton(ISigProvider sigProvider, string pressJoinName,
            string feedbackJoinName, string enableJoinName, string visibleJoinName, string serialJoinName, uint id = 0)
            : base(sigProvider, pressJoinName, feedbackJoinName, enableJoinName, visibleJoinName, id)
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