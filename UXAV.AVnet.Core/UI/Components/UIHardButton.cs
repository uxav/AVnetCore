using Crestron.SimplSharpPro;
using UXAV.AVnet.Core.DeviceSupport;

namespace UXAV.AVnet.Core.UI.Components
{
    public class UIHardButton : UIButton
    {
        private readonly DeviceExtender _hardButtonExtender;
        private bool _lastSetEnabled;

        internal UIHardButton(ISigProvider uiController, uint digitalJoinNumber, DeviceExtender hardButtonExtender)
            : base(uiController, digitalJoinNumber)
        {
            _hardButtonExtender = hardButtonExtender;
            uiController.SigProvider.Device.OnlineStatusChange += DeviceOnOnlineStatusChange;
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
                _lastSetEnabled = value;
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

        private void DeviceOnOnlineStatusChange(GenericBase currentdevice, OnlineOfflineEventArgs args)
        {
            if(!args.DeviceOnLine) return;
            Enabled = _lastSetEnabled;
        }
    }
}