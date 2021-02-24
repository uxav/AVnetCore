using Crestron.SimplSharpPro.Fusion;
using UXAV.AVnetCore.Models.Rooms;

namespace UXAV.AVnetCore.Fusion
{
    public class FusionInstance
    {
        private readonly FusionRoom _fusionRoom;
        private readonly RoomBase _room;

        internal FusionInstance(FusionRoom fusionRoom, RoomBase room)
        {
            _fusionRoom = fusionRoom;
            _room = room;
        }

        public RoomBase Room => _room;

        public FusionRoom FusionRoom => _fusionRoom;
    }
}