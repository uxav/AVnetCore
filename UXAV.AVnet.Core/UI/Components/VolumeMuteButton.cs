using UXAV.AVnet.Core.DeviceSupport;

namespace UXAV.AVnet.Core.UI.Components
{
    public class VolumeMuteButton : UIButton
    {
        private IVolumeControl _volumeControl;
        private readonly bool _mutedState;

        public VolumeMuteButton(ISigProvider sigProvider, uint digitalJoinNumber, uint enableJoinNumber = 0,
            uint visibleJoinNumber = 0, uint id = 0, bool mutedState = true)
            : base(sigProvider, digitalJoinNumber, enableJoinNumber, visibleJoinNumber, id)
        {
            _mutedState = mutedState;
        }

        public IVolumeControl VolumeControl
        {
            get => _volumeControl;
            set
            {
                if (_volumeControl == value) return;
                if (_volumeControl != null)
                {
                    _volumeControl.MuteChange -= VolumeControlOnMuteChange;
                    ButtonEvent -= OnButtonEvent;
                }

                _volumeControl = value;

                if (_volumeControl != null)
                {
                    _volumeControl.MuteChange += VolumeControlOnMuteChange;
                    ButtonEvent += OnButtonEvent;
                    SetFeedback(_volumeControl.Muted ? _mutedState : !_mutedState);
                }
            }
        }

        private void VolumeControlOnMuteChange(bool muted)
        {
            SetFeedback(muted ? _mutedState : !_mutedState);
        }

        private void OnButtonEvent(IButton button, ButtonEventArgs args)
        {
            if (args.EventType != ButtonEventType.Pressed) return;
            if (_volumeControl != null)
            {
                var mute = !_volumeControl.Muted;
                SetFeedback(mute ? _mutedState : !_mutedState);
                _volumeControl.Muted = mute;
            }
        }
    }
}