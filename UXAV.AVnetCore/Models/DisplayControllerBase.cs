using System;
using System.Threading.Tasks;
using UXAV.AVnetCore.DeviceSupport;
using UXAV.AVnetCore.Models.Rooms;
using UXAV.AVnetCore.Models.Sources;
using UXAV.Logging;

namespace UXAV.AVnetCore.Models
{
    public abstract class DisplayControllerBase : ISourceTarget
    {
        private readonly DisplayDeviceBase _device;
        private SourceBase _source;
        private bool _enabled;

        protected DisplayControllerBase(DisplayDeviceBase displayDevice)
        {
            _device = displayDevice;
        }

        public SourceBase Source
        {
            get => _source;
            set
            {
                if (_source == value) return;
                _source = value;

                if (!Enabled) return;
                Task.Run(() =>
                {
                    try
                    {
                        OnSourceChange(_source);
                        if (_device != null && _source != null)
                        {
                            _device.Power = true;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }
                });
            }
        }

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;

                _enabled = value;

                Task.Run(() =>
                {
                    try
                    {
                        if (_enabled)
                        {
                            OnSourceChange(_source);
                            if (_device != null && _source != null)
                            {
                                _device.Power = true;
                            }
                        }
                        else
                        {
                            OnSourceChange(null);
                            if (_device != null)
                            {
                                _device.Power = false;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }
                });
            }
        }


        public RoomBase Room => _device.AllocatedRoom;

        protected abstract void OnSourceChange(SourceBase source);
    }
}