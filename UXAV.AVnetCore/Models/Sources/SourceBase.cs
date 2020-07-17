using System;
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
            if(room == null) throw new ArgumentException("room cannot be null");

            if(AssignedRooms.Contains(room.Id)) return;

            AssignedRooms.Add(room);
        }

        public void UnassignRoom(RoomBase room)
        {
            if(room == null) throw new ArgumentException("room cannot be null");

            if (AssignedRooms.Contains(room.Id))
            {
                AssignedRooms.Remove(room);
            }
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