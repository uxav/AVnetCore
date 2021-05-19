using System;
using System.Threading.Tasks;
using UXAV.AVnet.Core.DeviceSupport;
using UXAV.AVnet.Core.Models.Rooms;
using UXAV.AVnet.Core.Models.Sources;
using UXAV.Logging;

namespace UXAV.AVnet.Core.Models
{
    public abstract class DisplayControllerBase : ISourceTarget
    {
        private readonly DisplayDeviceBase _device;
        private readonly string _name;
        private SourceBase _source;
        private bool _enabled = true;
        /// <summary>
        /// Only used if device is null
        /// </summary>
        private RoomBase _allocatedRoom;

        protected DisplayControllerBase(DisplayDeviceBase displayDevice, string name)
        {
            _device = displayDevice;
            _name = name;
        }

        public SourceBase GetCurrentSource(uint forIndex = 1)
        {
            return _source;
        }

        public async Task<bool> SelectSourceAsync(SourceBase source, uint forIndex = 1)
        {
            if (_source == source) return false;
            _source = source;
            var name = _source != null ? _source.ToString() : "none";
            Logger.Debug($"Display {Name} set source to {name}");

            if (!Enabled) return false;
            await Task.Run(() =>
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
            return true;
        }

        public DisplayDeviceBase Device => _device;

        public SourceCollection<SourceBase> Sources => UxEnvironment.GetSources().SourcesForDisplay(this);

        public string Name => _name;

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


        public virtual RoomBase Room
        {
            get
            {
                if (_device == null) return _allocatedRoom;
                return _device.AllocatedRoom;
            }
            set
            {
                if (_device != null)
                {
                    _device.AllocateRoom(value);
                    return;
                }
                _allocatedRoom = value;
            }
        }

        protected abstract void OnSourceChange(SourceBase source);
    }
}