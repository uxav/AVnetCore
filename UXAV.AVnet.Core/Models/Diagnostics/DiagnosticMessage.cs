using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UXAV.AVnet.Core.DeviceSupport;

namespace UXAV.AVnet.Core.Models.Diagnostics
{
    public class DiagnosticMessage
    {
        private readonly MessageLevel _level;
        private readonly string _message;
        private readonly string _detailsMessage;
        private readonly string _typeName;
        private readonly string _description;
        private readonly IDevice _device;

        public DiagnosticMessage(MessageLevel level, string message, string detailsMessage, string typeName,
            string description = "")
        {
            _level = level;
            _message = message;
            _detailsMessage = detailsMessage;
            _typeName = typeName;
            _description = description;
        }

        public DiagnosticMessage(MessageLevel level, string message, string detailsMessage, IDevice device)
        {
            _device = device;
            _level = level;
            _message = message;
            _detailsMessage = detailsMessage;
            _typeName = device.GetType().Name;
            _description = device.Identity;
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public MessageLevel Level => _level;

        public string Message => _message;

        public string DetailsMessage => _detailsMessage;

        public string TypeName => _typeName;

        public string Room => _device?.AllocatedRoom != null ? _device.AllocatedRoom.ScreenName : _description;
    }

    public enum MessageLevel
    {
        Info,
        Success,
        Warning,
        Danger,
    }
}