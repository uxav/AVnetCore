using System;
using System.Linq;
using UXAV.AVnetCore.Models.Rooms;
using UXAV.Logging;

namespace UXAV.AVnetCore.Models.Sources
{
    /// <summary>
    /// The base class of all AV source items
    /// </summary>
    public abstract class SourceBase : IGenericItem
    {
        private readonly string _groupName;
        private readonly string _iconName;
        private readonly string _name;
        private DisplayControllerBase _assignedDisplay;
        private int _activeUseCount;
        private bool _hasActiveVideo;

        protected SourceBase(uint id, SourceType type, string name, string groupName, string iconName)
        {
            Id = id;
            Type = type;
            _name = name;
            _groupName = groupName;
            _iconName = iconName;
            UxEnvironment.AddSource(this);
        }

        /// <summary>
        /// Unique Id for the source within the system
        /// </summary>
        /// <remarks>Note this is not done by room and is global throughout the program</remarks>
        public uint Id { get; }

        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(_name))
                {
                    return "Source " + Id;
                }

                return _name;
            }
        }

        /// <summary>
        /// A group name for the source
        /// </summary>
        public string GroupName => _groupName ?? string.Empty;

        /// <summary>
        /// Icon name for UI
        /// </summary>
        public string IconName => _iconName ?? string.Empty;

        /// <summary>
        /// The <see cref="SourceType"/> of the source
        /// </summary>
        public SourceType Type { get; }

        /// <summary>
        /// Used to organize priority order of sources in a collection
        /// </summary>
        public uint Priority { get; set; }

        public RoomCollection<RoomBase> AssignedRooms { get; } = new RoomCollection<RoomBase>();

        public void AssignRoom(RoomBase room)
        {
            if (room == null) throw new ArgumentException("room cannot be null");

            if (AssignedRooms.Contains(room.Id)) return;

            AssignedRooms.Add(room);
        }

        public void UnassignRoom(RoomBase room)
        {
            if (room == null) throw new ArgumentException("room cannot be null");

            if (AssignedRooms.Contains(room.Id))
            {
                AssignedRooms.Remove(room);
            }
        }

        public bool IsAssignedToRoom => AssignedRooms.Any();

        public void AssignToDisplay(DisplayControllerBase display)
        {
            _assignedDisplay = display ?? throw new ArgumentException("display cannot be null");
        }

        public int ActiveUseCount
        {
            get => _activeUseCount;
            internal set
            {
                var newValue = value;
                if (newValue < 0) newValue = 0;
                if (_activeUseCount == newValue) return;
                _activeUseCount = newValue;
                Logger.Debug($"Source: {this}, RoomCount = {newValue}");
                try
                {
                    OnActiveUseCountChange(_activeUseCount);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }

        protected abstract void OnActiveUseCountChange(int useCount);

        public DisplayControllerBase AssignedDisplay => _assignedDisplay;

        public bool IsLocalToDisplay => _assignedDisplay != null;

        public bool IsConferenceSource
        {
            get
            {
                switch (Type)
                {
                    case SourceType.VideoConference:
                    case SourceType.LiveStream:
                    case SourceType.PolycomTrio:
                    case SourceType.Hangouts:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool IsPresentationSource
        {
            get
            {
                switch (Type)
                {
                    case SourceType.PC:
                    case SourceType.Laptop:
                    case SourceType.AirPlay:
                    case SourceType.Chromecast:
                    case SourceType.AirMedia:
                    case SourceType.ClickShare:
                    case SourceType.GenericWirelessPresentationDevice:
                    case SourceType.Pano:
                    case SourceType.SolsticePod:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool IsWirelessPresentationSource
        {
            get
            {
                switch (Type)
                {
                    case SourceType.AirPlay:
                    case SourceType.Chromecast:
                    case SourceType.AirMedia:
                    case SourceType.ClickShare:
                    case SourceType.GenericWirelessPresentationDevice:
                    case SourceType.Pano:
                    case SourceType.SolsticePod:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool IsMediaSource
        {
            get
            {
                switch (Type)
                {
                    case SourceType.DVD:
                    case SourceType.BluRay:
                    case SourceType.TV:
                    case SourceType.IPTV:
                    case SourceType.Satellite:
                    case SourceType.Tuner:
                    case SourceType.DAB:
                    case SourceType.InternetRadio:
                    case SourceType.iPod:
                    case SourceType.AirPlay:
                    case SourceType.MovieServer:
                    case SourceType.MusicServer:
                    case SourceType.InternetService:
                    case SourceType.AppleTV:
                    case SourceType.Chromecast:
                    case SourceType.AndroidTV:
                    case SourceType.XBox:
                    case SourceType.PlayStation:
                    case SourceType.NintendoWii:
                    case SourceType.AirMedia:
                    case SourceType.LiveStream:
                    case SourceType.SignagePlayer:
                    case SourceType.Sky:
                    case SourceType.SkyHD:
                    case SourceType.SkyQ:
                    case SourceType.FreeView:
                    case SourceType.FreeSat:
                    case SourceType.YouView:
                    case SourceType.YouTube:
                    case SourceType.FireBox:
                    case SourceType.Skype:
                    case SourceType.Sonos:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public event SourceVideoStatusChanged HasActiveVideoChanged;

        public virtual bool HasActiveVideo
        {
            get => _hasActiveVideo;
            protected set
            {
                if (_hasActiveVideo == value) return;
                _hasActiveVideo = value;
                HasActiveVideoChanged?.Invoke(this, value);
            }
        }

        public void SetVideoStatus(bool videoActiveStatus)
        {
            HasActiveVideo = videoActiveStatus;
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
            return $"Source ID {Id}, {Type}, \"{Name}\"";
        }
    }

    public delegate void SourceVideoStatusChanged(SourceBase source, bool videoActive);

    public enum SourceType
    {
        Unknown,
        VideoConference,

        // ReSharper disable once InconsistentNaming
        PC,
        Laptop,

        // ReSharper disable once InconsistentNaming
        DVD,
        BluRay,
        TV,
        IPTV,
        Satellite,
        Tuner,
        DAB,
        InternetRadio,

        // ReSharper disable once InconsistentNaming
        iPod,
        AirPlay,
        MovieServer,
        MusicServer,
        InternetService,
        AppleTV,
        Chromecast,
        AndroidTV,
        XBox,
        PlayStation,
        NintendoWii,
        AirMedia,
        ClickShare,
        CCTV,
        AuxInput,
        LiveStream,
        SignagePlayer,
        GenericWirelessPresentationDevice,
        Sky,

        // ReSharper disable once InconsistentNaming
        SkyHD,
        SkyQ,
        FreeView,
        FreeSat,
        YouView,
        YouTube,
        FireBox,
        Skype,
        Hangouts,
        Sonos,
        Pano,
        SolsticePod,
        PolycomTrio
    }
}