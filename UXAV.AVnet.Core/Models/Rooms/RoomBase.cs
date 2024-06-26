using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UXAV.AVnet.Core.DeviceSupport;
using UXAV.AVnet.Core.Fusion;
using UXAV.AVnet.Core.Models.Sources;
using UXAV.AVnet.Core.UI;
using UXAV.Logging;

namespace UXAV.AVnet.Core.Models.Rooms
{
    /// <summary>
    ///     The base class of all room models in the program
    /// </summary>
    public abstract class RoomBase : IGenericItem, ISourceTarget
    {
        public enum PowerOffRequestType
        {
            UserRequested,
            FusionRequested,
            Scheduled
        }

        private readonly ConcurrentDictionary<uint, SourceBase> _currentSource =
            new ConcurrentDictionary<uint, SourceBase>();

        private readonly ConcurrentDictionary<uint, bool> _sourceSelectBusy =
            new ConcurrentDictionary<uint, bool>();

        private SourceBase _lastMainSource;
        private RoomBase _parentRoom;
        private bool _power;
        private string _uniqueId;

        protected RoomBase(uint id, string name, string screenName)
        {
            if (UxEnvironment.GetRooms().Any(r => r.Id == id))
                throw new ArgumentException($"Room with ID {id} already defined");

            Id = id;
            Name = name;
            if (string.IsNullOrEmpty(name))
                throw new ArgumentOutOfRangeException(nameof(name), "cannot be null or empty");

            ScreenName = string.IsNullOrEmpty(screenName) ? Name : screenName;
            UxEnvironment.AddRoom(this);
        }

        public string ScreenName { get; }
        public abstract string Description { get; }
        public abstract bool HasBookingFacility { get; }
        public abstract bool Booked { get; }
        public abstract bool HasOccupancy { get; }
        public abstract bool Occupied { get; }
        public abstract bool HasConferenceFacility { get; }
        public abstract bool InCall { get; }

        public string HtmlUiUrl { get; protected set; }

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

        public virtual SourceBase DefaultSource => _lastMainSource ?? Sources.FirstOrDefault();

        public virtual SourceCollection<SourceBase> Sources => UxEnvironment.GetSources().SourcesForRoomOrGlobal(this);

        public abstract IVolumeControl VolumeControl { get; }

        public abstract IMuteControl MicMute { get; }

        public virtual bool Power => _power;

        public uint Id { get; }
        public string Name { get; }

        public virtual SourceBase GetCurrentSource(uint forIndex = 1)
        {
            return !_currentSource.ContainsKey(forIndex) ? null : _currentSource[forIndex];
        }

        public virtual async Task<bool> SelectSourceAsync(SourceBase newSource, uint forIndex = 1)
        {
            Logger.Log($"Room {Id} SelectSource(SourceBase source), " +
                       $"Source = \"{newSource?.ToString() ?? "none"}\" requested for index: {forIndex}");

            if (_sourceSelectBusy.ContainsKey(forIndex) && _sourceSelectBusy[forIndex])
                throw new InvalidOperationException(
                    $"Source selection failed for index {forIndex}, as busy selecting other source");

            var currentSource = _currentSource.ContainsKey(forIndex) ? _currentSource[forIndex] : null;

            try
            {
                if (newSource == currentSource)
                {
                    Logger.Debug($"Source already {currentSource?.ToString() ?? "null"}");
                    return false;
                }

                _sourceSelectBusy[forIndex] = true;

                Logger.Debug("Creating source selection task..");
                var previousSource = currentSource;
                if (previousSource != null) previousSource.ActiveUseCount--;

                _currentSource[forIndex] = newSource;
                if (newSource != null)
                {
                    if (forIndex == 1) _lastMainSource = newSource;

                    newSource.ActiveUseCount++;
                }

                Logger.Highlight($"Room {Id} Source changed to \"{newSource?.ToString() ?? "null"}\", loading..");
                OnSourceChanged(this, new SourceChangedEventArgs
                {
                    Type = SourceChangedEventArgs.EventType.Pending,
                    Source = newSource,
                    RoomSourceIndex = forIndex
                });

                Logger.Debug($"Starting source load task forIndex = {forIndex}");
                await Task.Run(() => { SelectSourceInternal(previousSource, newSource, forIndex); });
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            _sourceSelectBusy[forIndex] = false;
            Logger.Debug("Returning SelectSource true");
            return true;
        }

        public event SourceChangedEventHandler SourceChanged;
        public event EventHandler<SourceBase> SourceTargetChangedSource;

        public virtual void FusionRequestedPowerOn()
        {
            PowerOn();
        }

        public virtual void FusionRequestedPowerOff()
        {
            PowerOff(PowerOffRequestType.FusionRequested);
        }

        public void PowerOn()
        {
            if (Power) return;
            if (_sourceSelectBusy.Values.Any(busy => busy))
                throw new InvalidOperationException("Source selection is busy");

            _power = true;
            Logger.Highlight($"Room \"{ScreenName}\", Power set to {_power}");
            Task.Run(RoomPowerOnProcess);
            Task.Run(SetFusionPowerState);
            EventService.Notify(EventMessageType.OnPowerChange, new
            {
                Room = Id,
                Power = _power
            });
        }

        public void PowerOff(PowerOffRequestType type = PowerOffRequestType.UserRequested)
        {
            if (!Power) return;

            _power = false;
            Logger.Highlight($"Room \"{ScreenName}\", Power set to {_power}");
            Task.Run(() => RoomPowerOffProcessInternal(type));
            Task.Run(SetFusionPowerState);
            EventService.Notify(EventMessageType.OnPowerChange, new
            {
                Room = Id,
                Power = _power
            });
        }

        internal bool SetPower(bool roomPower)
        {
            if (_sourceSelectBusy.Values.Any(busy => busy) || _power == roomPower) return false;
            if (roomPower)
                PowerOn();
            else
                PowerOff();

            return true;
        }

        protected abstract void RoomPowerOnProcess();

        private void RoomPowerOffProcessInternal(PowerOffRequestType requestType)
        {
            var count = 0;
            while (_sourceSelectBusy.Values.Any(busy => busy))
            {
                Logger.Debug("RoomPowerOffProcessInternal(), Busy, Waiting");
                Thread.Sleep(1000);
                count++;
                if (count <= 10) continue;
                Logger.Error("Giving up waiting for source selection not to be busy, powering off anyway");
                _sourceSelectBusy.Clear();
                break;
            }

            foreach (var controller in this.GetCore3Controllers()) controller.RoomPoweringOff();

            RoomPowerOffProcess(requestType);
        }

        private void SetFusionPowerState()
        {
            try
            {
                var fusion = this.GetFusionInstance();
                if (fusion == null) return;
                fusion.FusionRoom.SystemPowerOn.InputSig.BoolValue = Power;
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        protected abstract void RoomPowerOffProcess(PowerOffRequestType powerOffRequestType);

        private void SelectSourceInternal(SourceBase previousSource, SourceBase newSource, uint forIndex)
        {
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
                SourceShouldLoad(previousSource, newSource, forIndex);
                Thread.Sleep(500);
                Logger.Success($"Source selection complete, Room {Id} source (forIndex = {forIndex})" +
                               $" from \"{previousSource?.ToString() ?? "None"}\" to \"{newSource?.ToString() ?? "None"}\"");
                if (previousSource != newSource)
                    OnSourceChanged(this, new SourceChangedEventArgs
                    {
                        Type = SourceChangedEventArgs.EventType.Complete,
                        Source = newSource,
                        RoomSourceIndex = forIndex
                    });
            }
            catch (Exception e)
            {
                OnSourceChanged(this, new SourceChangedEventArgs
                {
                    Type = SourceChangedEventArgs.EventType.Failed,
                    Source = newSource,
                    RoomSourceIndex = forIndex
                });

                Logger.Error(e);
            }

            try
            {
                SourceTargetChangedSource?.Invoke(this, newSource);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            Logger.Log($"Room {Id} Source loading complete");
        }

        protected abstract void SourceShouldLoad(SourceBase previousSource, SourceBase newSource, uint forIndex);

        private void OnSourceChanged(RoomBase room, SourceChangedEventArgs args)
        {
            Logger.Debug($"OnSourceChanged(room, args) {args.Type}");
            EventService.Notify(EventMessageType.OnSourceChange, new
            {
                Room = room.Id,
                RoomId = room.UniqueId,
                Source = args.Source?.Id ?? 0,
                SourceId = args.Source?.UniqueId ?? string.Empty,
                Status = args.Type.ToString(),
                args.RoomSourceIndex
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
        ///     Get a display device registered to the room
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

        /// <summary>
        ///     Index of the source in the room. 1 is usually main source, others are aux sources;
        /// </summary>
        public uint RoomSourceIndex { get; internal set; }
    }

    public delegate void SourceChangedEventHandler(RoomBase room, SourceChangedEventArgs args);
}