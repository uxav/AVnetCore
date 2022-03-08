using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UXAV.AVnet.Core.DeviceSupport;

namespace UXAV.AVnet.Core.Models.Diagnostics
{
    public class DiagnosticMessage
    {
        private readonly string _description;
        private readonly IDevice _device;

        public DiagnosticMessage(MessageLevel level, string message, string detailsMessage, string typeName,
            string description = "")
        {
            Level = level;
            Message = message;
            DetailsMessage = detailsMessage;
            TypeName = typeName;
            _description = description;
        }

        public DiagnosticMessage(MessageLevel level, string message, string detailsMessage, IDevice device)
        {
            _device = device;
            Level = level;
            Message = message;
            DetailsMessage = detailsMessage;
            TypeName = device.GetType().Name;
            _description = device.Identity;
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public MessageLevel Level { get; }

        public string Message { get; }

        public string DetailsMessage { get; }

        public string TypeName { get; }

        public string Room => _device?.AllocatedRoom != null ? _device.AllocatedRoom.ScreenName : _description;
    }

    public enum MessageLevel
    {
        Info,
        Success,
        Warning,
        Danger
    }
}