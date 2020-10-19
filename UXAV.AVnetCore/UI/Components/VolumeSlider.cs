using System;
using UXAV.AVnetCore.DeviceSupport;
using UXAV.Logging;

namespace UXAV.AVnetCore.UI.Components
{
    public class VolumeSlider : UISlider
    {
        private IVolumeControl _volumeControl;

        public VolumeSlider(ISigProvider sigProvider, uint analogJoinNumber, ushort minValue = 0, ushort maxValue = 100)
            : base(sigProvider, analogJoinNumber, minValue, maxValue)
        {
            ValueChanged += OnThisValueChanged;
        }

        public VolumeSlider(ISigProvider sigProvider, string analogJoinName, string analogFeedbackJoinName,
            ushort minValue = 0, ushort maxValue = 100)
            : base(sigProvider, analogJoinName, analogFeedbackJoinName, minValue, maxValue)
        {
            ValueChanged += OnThisValueChanged;
        }

        public IVolumeControl VolumeControl
        {
            get => _volumeControl;
            set
            {
                if (_volumeControl == value) return;

                if (_volumeControl != null)
                {
                    _volumeControl.VolumeLevelChange -= VolumeControlOnVolumeLevelChange;
                }

                _volumeControl = value;

                if (_volumeControl != null)
                {
                    _volumeControl.VolumeLevelChange += VolumeControlOnVolumeLevelChange;
                    VolumeControlOnVolumeLevelChange(_volumeControl.VolumeLevel);
                }
            }
        }

        private void VolumeControlOnVolumeLevelChange(ushort level)
        {
            SetValue((ushort) Tools.ScaleRange(level, 0, 100, MinValue, MaxValue));
        }

        private void OnThisValueChanged(UISlider slider, ushort newValue)
        {
            if(_volumeControl == null) return;

            try
            {
                _volumeControl.VolumeLevel = (ushort) Tools.ScaleRange(newValue, MinValue, MaxValue, 0, 100);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}