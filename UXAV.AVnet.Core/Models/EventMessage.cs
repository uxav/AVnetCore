using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UXAV.AVnet.Core.Models
{
    public class EventMessage
    {
        internal EventMessage(EventMessageType eventMessageType, object messageObject)
        {
            MessageType = eventMessageType;
            Message = messageObject;
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public EventMessageType MessageType { get; }

        public object Message { get; }
    }
}