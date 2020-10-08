using System;
using System.Collections.Generic;
using UXAV.AVnetCore.Models;
using UXAV.AVnetCore.Models.Rooms;
using UXAV.Logging;

namespace UXAV.AVnetCore.DeviceSupport
{
    public abstract class DisplayDeviceBase : DeviceBase, IPowerDevice
    {
        private DevicePowerStatus _powerStatus;
        private ushort _displayUsage;
        private IHoistControl _screenHoist;
        private IHoistControl _deviceHoist;

        /// <summary>
        /// The default Constructor.
        /// </summary>
        protected DisplayDeviceBase(SystemBase system, string name, uint roomAllocatedId = 0)
            : base(system, name, roomAllocatedId)
        {
        }

        protected DisplayDeviceBase(RoomBase room, string name)
            : base(room.System, name, room.Id)
        {
        }

        /// <summary>
        /// Power status has changed
        /// </summary>
        public event DevicePowerStatusEventHandler PowerStatusChange;

        /// <summary>
        /// DisplayUsage value has changed
        /// </summary>
        public event DisplayUsageChangeEventHandler DisplayUsageChange;

        public virtual bool Power
        {
            get => PowerStatus == DevicePowerStatus.PowerOn || PowerStatus == DevicePowerStatus.PowerWarming;
            set
            {
                RequestedPower = value;
                PowerRequested(value, Power);
                ActionPowerRequest(RequestedPower);
            }
        }

        public bool RequestedPower { get; private set; }

        public DevicePowerStatus PowerStatus
        {
            get => _powerStatus;
            protected set
            {
                if (_powerStatus == value) return;
                var oldState = _powerStatus;
                _powerStatus = value;
                Logger.Debug("{0} PowerStatus = {1}", this, value);

                OnPowerStatusChange(this, new DevicePowerStatusEventArgs(_powerStatus, oldState));
            }
        }

        /// <summary>
        /// The current input for the display
        /// </summary>
        public abstract DisplayDeviceInput CurrentInput { get; }

        /// <summary>
        /// Get a list of available supported inputs
        /// </summary>
        public abstract IEnumerable<DisplayDeviceInput> AvailableInputs { get; }

        /// <summary>
        /// True if the display uses DisplayUsage
        /// </summary>
        public abstract bool SupportsDisplayUsage { get; }

        /// <summary>
        /// Get the display usage value as a ushort, use for a analog guage.
        /// </summary>
        public ushort DisplayUsage
        {
            get => _displayUsage;
            protected set
            {
                if (_displayUsage == value) return;
                _displayUsage = value;
                OnDisplayUsageChange(this);
            }
        }

        public void AssignScreenHoistDevice(IHoistControl hoistDevice)
        {
            _screenHoist = hoistDevice;
        }

        public void AssignDeviceHoistDevice(IHoistControl hoistDevice)
        {
            _deviceHoist = hoistDevice;
        }

        protected abstract void PowerRequested(bool requested, bool actualState);

        protected virtual void OnPowerStatusChange(IPowerDevice device, DevicePowerStatusEventArgs args)
        {
            if (_deviceHoist != null)
            {
                try
                {
                    switch (args.NewPowerStatus)
                    {
                        case DevicePowerStatus.PowerOff:
                            Logger.Debug($"{Name} device hoist Up()");
                            _deviceHoist.Up();
                            break;
                        case DevicePowerStatus.PowerOn:
                        case DevicePowerStatus.PowerWarming:
                            Logger.Debug($"{Name} device hoist Down()");
                            _deviceHoist.Down();
                            break;
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }

            if (_screenHoist != null)
            {
                try
                {
                    switch (args.NewPowerStatus)
                    {
                        case DevicePowerStatus.PowerCooling:
                        case DevicePowerStatus.PowerOff:
                            Logger.Debug($"{Name} screen hoist Up()");
                            _screenHoist.Up();
                            break;
                        case DevicePowerStatus.PowerOn:
                        case DevicePowerStatus.PowerWarming:
                            Logger.Debug($"{Name} screen hoist Down()");
                            _screenHoist.Down();
                            break;
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }

            var handler = PowerStatusChange;
            if (handler == null) return;
            try
            {
                handler(device, args);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        protected virtual void OnDisplayUsageChange(DisplayDeviceBase display)
        {
            var handler = DisplayUsageChange;
            if (handler == null) return;
            try
            {
                handler(display);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        /// <summary>
        /// Call this when receiving power status feedback from device.
        /// PowerStatus = newPowerState;
        /// </summary>
        /// <param name="newPowerState"></param>
        protected abstract void SetPowerFeedback(DevicePowerStatus newPowerState);

        /// <summary>
        /// This is called when the power is set by the API. Make necessary arrangements to make sure the device follows the request
        /// </summary>
        /// <param name="powerRequest"></param>
        protected abstract void ActionPowerRequest(bool powerRequest);

        /// <summary>
        /// Set the input on the display
        /// </summary>
        /// <param name="input"></param>
        public abstract void SetInput(DisplayDeviceInput input);

        public override string ToString()
        {
            return $"{GetType().Name} \"{Name}\"";
        }
    }

    /// <summary>
    /// Event handler for a display when the value changes
    /// </summary>
    /// <param name="display">The display which calls the event</param>
    public delegate void DisplayUsageChangeEventHandler(DisplayDeviceBase display);

    public enum DisplayDeviceInput
    {
        Unknown,
        HDMI1,
        HDMI2,
        HDMI3,
        HDMI4,
        DisplayPort1,
        DisplayPort2,
        DVI,
        DVI2,
        VGA,
        RGBHV,
        Composite,
        YUV,
        SVideo,
        MagicInfo,
        TV,
        HDBaseT,
        BuiltIn,
        Wireless,
        AirPlay,
        SDI,
        USB
    }
}