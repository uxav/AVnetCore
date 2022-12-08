using System.Threading.Tasks;
using Crestron.SimplSharp;

namespace UXAV.AVnet.Core.Models
{
    public static class EventService
    {
        static EventService()
        {
            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                if (type == eProgramStatusEventType.Stopping) Notify(EventMessageType.ProgramStopping);
            };
        }

        public static void Notify(EventMessageType eventMessageType, object messageObject = null)
        {
            OnEventOccured(new EventMessage(eventMessageType, messageObject));
        }

        public static event EventPostedEventHandler EventOccured;

        private static void OnEventOccured(EventMessage message)
        {
            if (EventOccured == null) return;
            var handlers = EventOccured.GetInvocationList();
            Task.Factory.StartNew(() =>
            {
                foreach (var @delegate in handlers)
                {
                    var handler = (EventPostedEventHandler)@delegate;
                    handler.Invoke(message);
                }
            });
        }
    }

    public delegate void EventPostedEventHandler(EventMessage message);

    public enum EventMessageType
    {
        Generic,
        TimeChanged,
        ConfigChanged,
        OnSourceChange,
        OnPowerChange,
        BootStatus,
        LogEntry,
        ProgramStopping,
        SessionExpired,
        DeviceConnectionChange,
        DiagnosticsMessagesUpdated,
        SystemMonitorStatsChange,
        UpdateAvailableChange
    }
}