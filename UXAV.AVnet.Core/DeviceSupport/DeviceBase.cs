using System;
using System.Collections.Generic;
using Crestron.SimplSharp;
using UXAV.AVnet.Core.Models;
using UXAV.AVnet.Core.Models.Diagnostics;
using UXAV.AVnet.Core.Models.Rooms;
using UXAV.Logging;

namespace UXAV.AVnet.Core.DeviceSupport
{
    public abstract class DeviceBase : IDevice
    {
        private static uint _idCount;
        private readonly uint _roomIdAllocated;
        private bool _deviceCommunicating;
        private string _name;

        [Obsolete("Method is no longer used. Please use ctor without passing SystemBase.")]
        // ReSharper disable once UnusedParameter.Local
        protected DeviceBase(SystemBase system, string name, uint roomIdAllocated = 0)
        {
            _name = name;
            _roomIdAllocated = roomIdAllocated;
            _idCount++;
            Id = _idCount;
            System.DevicesDict[Id] = this;
            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                if (type == eProgramStatusEventType.Stopping) OnProgramStopping();
            };
            Logger.Log($"Created device {GetType().Name} \"{Name}\" with device ID {Id}");
        }

        protected DeviceBase(string name, uint roomIdAllocated = 0)
        {
            _name = name;
            _roomIdAllocated = roomIdAllocated;
            _idCount++;
            Id = _idCount;
            System.DevicesDict[Id] = this;
            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                if (type == eProgramStatusEventType.Stopping) OnProgramStopping();
            };
            Logger.Log($"Created device {GetType().Name} \"{Name}\" with device ID {Id}");
        }

        [Obsolete("Method is no longer used. Please use ctor(name, room).")]
        protected DeviceBase(RoomBase room, string name)
            : this(name, room.Id)
        {
        }

        protected DeviceBase(string name, RoomBase room)
            : this(name, room.Id)
        {
        }

        public SystemBase System => UxEnvironment.System;

        public abstract IEnumerable<DiagnosticMessage> GetMessages();
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
                    Logger.Success($"{Name} is now online.", GetType().Name, true);
                else
                    Logger.Warn($"{Name} is offline!", GetType().Name, false);

                try
                {
                    DeviceCommunicatingChange?.Invoke(this, _deviceCommunicating);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }

                if (System.BootStatus != SystemBase.EBootStatus.Running) return;
                EventService.Notify(EventMessageType.DeviceConnectionChange, new
                {
                    Device = Name,
                    Description = AllocatedRoom?.Name,
                    ConnectionInfo,
                    Online = value
                });
            }
        }

        public virtual bool DebugEnabled { get; set; }

        /// <summary>
        ///     Event called if the comms status changes on the device
        /// </summary>
        public event DeviceCommunicatingChangeHandler DeviceCommunicatingChange;

        public abstract void Initialize();

        internal void AllocateRoomOnStart()
        {
            if (_roomIdAllocated <= 0) return;
            if (!UxEnvironment.RoomWithIdExists(_roomIdAllocated))
            {
                Logger.Warn($"Cannot allocated {Name} to room with ID {_roomIdAllocated}, no room with that ID");
                return;
            }

            AllocatedRoom = UxEnvironment.GetRoom(_roomIdAllocated);
        }

        internal void AllocateRoom(RoomBase room)
        {
            if (AllocatedRoom == room) return;
            AllocatedRoom = room;
        }

        protected void SetName(string name)
        {
            _name = name;
        }

        protected abstract void OnProgramStopping();
    }
}