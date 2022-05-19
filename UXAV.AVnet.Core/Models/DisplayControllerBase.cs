using System;
using System.Threading.Tasks;
using UXAV.AVnet.Core.DeviceSupport;
using UXAV.AVnet.Core.Models.Rooms;
using UXAV.AVnet.Core.Models.Sources;
using UXAV.Logging;

namespace UXAV.AVnet.Core.Models
{
    public abstract class DisplayControllerBase : IGenericItem, ISourceTarget
    {
        /// <summary>
        ///     Only used if device is null
        /// </summary>
        private RoomBase _allocatedRoom;

        private bool _enabled = true;
        private SourceBase _source;
        private string _uniqueId;
        private static uint _idCount = 0;

        protected DisplayControllerBase(DisplayDeviceBase displayDevice, string name)
        {
            _idCount++;
            Id = _idCount;
            Device = displayDevice;
            Name = name;
            UxEnvironment.AddDisplay(this);
            if (Device != null)
            {
                Device.PowerStatusChange += OnDeviceOnPowerStatusChange;
            }
        }

        public event EventHandler<bool> PowerStatusChange;

        private void OnDeviceOnPowerStatusChange(IPowerDevice device, DevicePowerStatusEventArgs args)
        {
            PowerStatusChange?.Invoke(this, device.Power);
        }

        public DisplayDeviceBase Device { get; }

        public string UniqueId
        {
            get
            {
                if (string.IsNullOrEmpty(_uniqueId))
                {
                    _uniqueId = Guid.NewGuid().ToString();
                }

                return _uniqueId;
            }
        }

        public SourceCollection<SourceBase> Sources => UxEnvironment.GetSources().SourcesForDisplay(this);

        public virtual bool Enabled
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
                            SetSourceOnEnableDisable(_source);
                            if (Device != null && _source != null) SetPowerOnEnableDisable(true);
                        }
                        else
                        {
                            SetSourceOnEnableDisable(null);
                            if (Device != null) SetPowerOnEnableDisable(false);
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
                if (Device == null) return _allocatedRoom;
                return Device.AllocatedRoom;
            }
            set
            {
                if (Device != null)
                {
                    Device.AllocateRoom(value);
                    return;
                }

                _allocatedRoom = value;
            }
        }

        public SourceBase GetCurrentSource(uint forIndex = 1)
        {
            return _source;
        }

        public event EventHandler<SourceBase> SourceTargetChangedSource;

        public async Task<bool> SelectSourceAsync(SourceBase source, uint forIndex = 1)
        {
            if (_source == source && source != null && Enabled && Device != null && Device.Power == false)
            {
                Device.Power = true;
                Logger.Debug($"{Name} source already set to {_source}, but powered off so setting power to on!");
                return true;
            }

            if (_source == source) return false;
            _source = source;
            var name = _source != null ? _source.ToString() : "none";
            Logger.Debug($"Display {Name} set source to {name}");

            if (!Enabled) return false;
            await Task.Run(() =>
            {
                try
                {
                    OnSourceChangeInternal(_source);
                    if (Device != null && _source != null) Device.Power = true;
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            });
            return true;
        }

        public uint Id { get; }
        public string Name { get; }

        protected virtual void SetPowerOnEnableDisable(bool powerRequest)
        {
            if (Device != null) Device.Power = powerRequest;
        }

        protected virtual void SetSourceOnEnableDisable(SourceBase source)
        {
            OnSourceChangeInternal(source);
        }

        private void OnSourceChangeInternal(SourceBase source)
        {
            try
            {
                OnSourceChange(source);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            try
            {
                SourceTargetChangedSource?.Invoke(this, source);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        protected abstract void OnSourceChange(SourceBase source);

        public virtual void PowerOff()
        {
            if (Device == null)
            {
                throw new NotImplementedException("Display controller does not have device");
            }

            Device.Power = false;
        }
    }
}