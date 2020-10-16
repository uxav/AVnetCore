using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UXAV.AVnetCore.DeviceSupport;
using UXAV.AVnetCore.Models.Sources;
using UXAV.Logging;

namespace UXAV.AVnetCore.Models.Rooms
{
    /// <summary>
    /// The base class of all room models in the program
    /// </summary>
    public abstract class RoomBase : IGenericItem
    {
        private RoomBase _parentRoom;
        private Thread _sourceSelectionThread;
        private bool _power;

        protected RoomBase(uint id, string name, string screenName)
        {
            if (UxEnvironment.GetRooms().Any(r => r.Id == id))
            {
                throw new ArgumentException($"Room with ID {id} already defined");
            }

            Id = id;
            Name = name;
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentOutOfRangeException(nameof(name), "cannot be null or empty");
            }

            ScreenName = string.IsNullOrEmpty(screenName) ? Name : screenName;
            UxEnvironment.AddRoom(this);
        }

        public uint Id { get; }

        public string Name { get; }

        public string ScreenName { get; }

        public RoomBase ParentRoom
        {
            get => _parentRoom;
            set
            {
                if (_parentRoom == value) return;

                if (_parentRoom != null)
                {
                }

                _parentRoom = value;

                if (_parentRoom != null)
                {
                }
            }
        }

        public RoomCollection<RoomBase> ChildRooms
        {
            get
            {
                return new RoomCollection<RoomBase>(UxEnvironment.GetRooms()
                    .Where(r => r != this && r.ParentRoom == this));
            }
        }

        public SystemBase System => UxEnvironment.System;

        public event SourceChangedEventHandler SourceChanged;

        public SourceBase CurrentSource { get; private set; }

        public SourceBase LastSource { get; private set; }

        public virtual SourceBase DefaultSource => LastSource ?? Sources.FirstOrDefault();

        public SourceCollection<SourceBase> Sources
        {
            get { return UxEnvironment.GetSources().SourcesForRoomOrGlobal(this); }
        }

        public bool SourceSelectionBusy => _sourceSelectionThread != null && _sourceSelectionThread.IsAlive;

        public bool SelectSource(SourceBase source)
        {
            Logger.Log(
                $"Room {Id} SelectSource(SourceBase source), Source = \"{source?.ToString() ?? "null"}\" requested");
            if (SourceSelectionBusy)
            {
                Logger.Warn($"Cannot select source in room {Id}, source selection is busy");
                return false;
            }

            if (source == CurrentSource)
            {
                Logger.Warn($"Source already {CurrentSource?.ToString() ?? "null"}");
                return false;
            }

            Logger.Debug("Creating source selection thread handler");
            _sourceSelectionThread = new Thread(SelectSourceInternalThread);
            var previousSource = CurrentSource;
            if (previousSource != null)
            {
                previousSource.RoomCount--;
            }
            CurrentSource = source;
            if (CurrentSource != null)
            {
                LastSource = CurrentSource;
                CurrentSource.RoomCount++;
            }

            Logger.Highlight($"Room {Id} Source changed to \"{source?.ToString() ?? "null"}\", loading..");
            OnSourceChanged(this, new SourceChangedEventArgs()
            {
                Type = SourceChangedEventArgs.EventType.Pending,
                Source = source
            });

            Logger.Debug("Starting source load thread process");
            _sourceSelectionThread.Start((previousSource, source));

            Logger.Debug("Returning SelectSource true");
            return true;
        }

        public virtual bool Power
        {
            get { return _power; }
            private set
            {
                if (_power == value)
                {
                    return;
                }

                _power = value;
                Logger.Highlight($"Room {Id} Power set to {_power}");
                if (_power) Task.Run(RoomPowerOnProcess);
                else Task.Run(RoomPowerOffProcess);
                EventService.Notify(EventMessageType.OnPowerChange, new
                {
                    Room = Id,
                    Power = _power
                });
            }
        }

        public void PowerOn()
        {
            if (Power) return;
            if (SourceSelectionBusy)
            {
                throw new InvalidOperationException("Source selection is busy");
            }

            Power = true;
        }

        public void PowerOff()
        {
            if (!Power) return;
            if (SourceSelectionBusy)
            {
                throw new InvalidOperationException("Source selection is busy");
            }

            Power = false;
        }

        internal bool SetPower(bool roomPower)
        {
            if (SourceSelectionBusy || _power == roomPower) return false;
            Power = roomPower;
            return true;
        }

        protected abstract void RoomPowerOnProcess();

        protected abstract void RoomPowerOffProcess();

        private void SelectSourceInternalThread(object sourcesObject)
        {
            var previousSource = ((ValueTuple<SourceBase, SourceBase>) sourcesObject).Item1;
            var newSource = ((ValueTuple<SourceBase, SourceBase>) sourcesObject).Item2;

            if (newSource != null && !Power)
            {
                Logger.Log($"Room {Id}, Source request, powering on first!");
                _power = true;
                RoomPowerOnProcess();
                EventService.Notify(EventMessageType.OnPowerChange, new
                {
                    Room = Id,
                    Power = true
                });
                Thread.Sleep(500);
                Logger.Debug($"Room {Id}, Source request, continuing with source load process..");
            }

            try
            {
                SourceShouldLoad(previousSource, newSource);
                Thread.Sleep(500);
                Logger.Success($"Source selection complete, Room {Id} source = {CurrentSource?.ToString() ?? "None"}");
                if (previousSource != newSource)
                {
                    OnSourceChanged(this, new SourceChangedEventArgs()
                    {
                        Type = SourceChangedEventArgs.EventType.Complete,
                        Source = newSource
                    });
                }
            }
            catch (Exception e)
            {
                if (previousSource != newSource)
                {
                    OnSourceChanged(this, new SourceChangedEventArgs()
                    {
                        Type = SourceChangedEventArgs.EventType.Failed,
                        Source = newSource
                    });
                }

                Logger.Error(e);
            }

            Logger.Log($"Room {Id} Source loading complete");
        }

        protected abstract void SourceShouldLoad(SourceBase previousSource, SourceBase newSource);

        private void OnSourceChanged(RoomBase room, SourceChangedEventArgs args)
        {
            EventService.Notify(EventMessageType.OnSourceChange, new
            {
                Room = room.Id,
                Source = args.Source?.Id ?? 0,
                Status = args.Type.ToString()
            });

            try
            {
                SourceChanged?.Invoke(room, args);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        protected IEnumerable<DisplayDeviceBase> GetDisplayDevices()
        {
            return System.GetDisplayDevices().Where(d => d.AllocatedRoom == this);
        }

        /// <summary>
        /// Get a display device registered to the room
        /// </summary>
        /// <param name="index">0 based index of display</param>
        /// <returns>DisplayDeviceBase</returns>
        protected DisplayDeviceBase GetDisplayDevice(int index)
        {
            var displays = GetDisplayDevices().ToArray();
            return displays[index];
        }

        internal void InternalInitialize()
        {
            try
            {
                OnInitialize();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        protected abstract void OnInitialize();

        public override string ToString()
        {
            return $"Room {Id}: {Name} \"{ScreenName}\" ({GetType().Name})";
        }
    }

    public class SourceChangedEventArgs : EventArgs
    {
        public enum EventType
        {
            Pending,
            Complete,
            Failed
        }

        public EventType Type { get; internal set; }
        public SourceBase Source { get; internal set; }
    }

    public delegate void SourceChangedEventHandler(RoomBase room, SourceChangedEventArgs args);
}