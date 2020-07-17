using Crestron.SimplSharpPro;
using UXAV.AVnetCore.DeviceSupport;

namespace UXAV.AVnetCore.UI.Components
{
    public class UIHardButton : UIButton
    {
        private readonly DeviceExtender _hardButtonExtender;

        internal UIHardButton(ISigProvider uiController, uint digitalJoinNumber, DeviceExtender hardButtonExtender)
            : base(uiController, digitalJoinNumber)
        {
            _hardButtonExtender = hardButtonExtender;
        }

        public override bool Enabled
        {
            get
            {
                if (_hardButtonExtender == null) return true;
                var value = _hardButtonExtender.GetBoolPropertyValue($"Button{DigitalJoinNumber}BacklightOnFeedback");
                if (value == null) return false;
                return (bool) value;
            }
            set
            {
                if (_hardButtonExtender == null) return;
                var methodName = $"TurnButton{DigitalJoinNumber}BackLight" + (value ? "On" : "Off");
                _hardButtonExtender.InvokeMethod(methodName);
            }
        }

        public override uint Id => DigitalJoinNumber;

        public override bool Visible
        {
            get => Enabled;
            set => Enabled = value;
        }
    }
}