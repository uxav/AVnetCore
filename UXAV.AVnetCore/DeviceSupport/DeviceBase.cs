using System;
using System.Collections.Generic;
using Crestron.SimplSharp;
using UXAV.AVnetCore.Models;
using UXAV.AVnetCore.Models.Diagnostics;
using UXAV.AVnetCore.Models.Rooms;
using UXAV.Logging;

namespace UXAV.AVnetCore.DeviceSupport
{
    public abstract class DeviceBase : IDevice
    {
        private readonly uint _roomIdAllocated;
        private static uint _idCount;
        private string _name;
        private bool _deviceCommunicating;

        protected DeviceBase(SystemBase system, string name, uint roomIdAllocated = 0)
        {
            _name = name;
            _roomIdAllocated = roomIdAllocated;
            System = system;
            _idCount++;
            Id = _idCount;
            System.DevicesDict[Id] = this;
            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                if (type == eProgramStatusEventType.Stopping)
                {
                    OnProgramStopping();
                }
            };
            Logger.Log($"Created device {GetType().Name} \"{Name}\" with device ID {Id}");
        }

        protected DeviceBase(RoomBase room, string name)
            : this(room.System, name, room.Id)
        {
        }

        public abstract IEnumerable<DiagnosticMessage> GetMessages();
        public SystemBase System { get; }
        public uint Id { get; }

        public string Name => string.IsNullOrEmpty(_name) ? GetType().Name : _name;

        public abstract string ConnectionInfo { get; }
        public abstract string ManufacturerName { get; }
        public abstract string ModelName { get; }
        public abstract string SerialNumber { get; }
        public abstract string VersionInfo { get; }
        public abstract string Identity { get; }
        public RoomBase AllocatedRoom { get; private set; }

        public bool DeviceCommunicating
        {
            get => _deviceCommunicating;
            protected set
            {
                if (_deviceCommunicating == value) return;
                _deviceCommunicating = value;
                if (_deviceCommunicating)
                {
                    Logger.Success($"{Name} is now online.", GetType().Name, true);
                }
                else
                {
                    Logger.Warn($"{Name} is offline!", GetType().Name, false);
                }

                try
                {
                    DeviceCommunicatingChange?.Invoke(this, _deviceCommunicating);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }

                EventService.Notify(EventMessageType.DeviceConnectionChange, new
                {
                    @Device = Name,
                    @Description = AllocatedRoom?.Name,
                    @ConnectionInfo = ConnectionInfo,
                    @Online = value
                });
                DiagnosticService.GetMessages();
            }
        }

        public virtual bool DebugEnabled { get; set; }

        /// <summary>
        /// Event called if the comms status changes on the device
        /// </summary>
        public event DeviceCommunicatingChangeHandler DeviceCommunicatingChange;

        internal void AllocateRoom()
        {
            if (_roomIdAllocated > 0)
            {
                if (!UxEnvironment.RoomWithIdExists(_roomIdAllocated))
                {
                    Logger.Warn($"Cannot allocated {Name} to room with ID {_roomIdAllocated}, no room with that ID");
                    return;
                }

                AllocatedRoom = UxEnvironment.GetRoom(_roomIdAllocated);
            }
        }

        protected void SetName(string name)
        {
            _name = name;
        }

        public abstract void Initialize();
        protected abstract void OnProgramStopping();
    }
}