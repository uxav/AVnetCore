using System;

namespace UXAV.AVnetCore.DeviceSupport
{
    /// <summary>
    /// Interface to provide power control for a device
    /// </summary>
    public interface IPowerDevice : IDevice
    {
        /// <summary>
        /// Set or get the current power status. Get may not be the value just set as may require processing of feedback state.
        /// Use RequestedPower for value set by code
        /// </summary>
        bool Power { get; set; }

        /// <summary>
        /// The power last set and requested by the API
        /// </summary>
        bool RequestedPower { get; }

        /// <summary>
        /// The current power status
        /// </summary>
        DevicePowerStatus PowerStatus { get; }

        /// <summary>
        /// The event called when the power status value changes
        /// </summary>
        event DevicePowerStatusEventHandler PowerStatusChange;
    }

    /// <summary>
    /// Power status event handler delegate
    /// </summary>
    /// <param name="device">The device</param>
    /// <param name="args">Power status args</param>
    public delegate void DevicePowerStatusEventHandler(IPowerDevice device, DevicePowerStatusEventArgs args);

    public class DevicePowerStatusEventArgs : EventArgs
    {
        /// <summary>
        /// The new power status
        /// </summary>
        public DevicePowerStatus NewPowerStatus { get; private set; }

        /// <summary>
        /// Power status prior to this event
        /// </summary>
        public DevicePowerStatus PreviousPowerStatus { get; private set; }

        public DevicePowerStatusEventArgs(DevicePowerStatus newPowerStatus, DevicePowerStatus previousPowerStatus)
        {
            NewPowerStatus = newPowerStatus;
            PreviousPowerStatus = previousPowerStatus;
        }
    }

    /// <summary>
    /// Power status for a device which uses IPowerDevice
    /// </summary>
    public enum DevicePowerStatus
    {
        /// <summary>
        /// Device is off or in standby
        /// </summary>
        PowerOff,

        /// <summary>
        /// Device is on and ready
        /// </summary>
        PowerOn,

        /// <summary>
        /// Device power is transitioning from off to on
        /// </summary>
        PowerWarming,

        /// <summary>
        /// Device power is transitioning from on to off
        /// </summary>
        PowerCooling
    }
}