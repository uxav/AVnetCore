using Crestron.SimplSharp;

namespace UXAV.AVnetCore.Models
{
    public static class EventService
    {
        static EventService()
        {
            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                if (type == eProgramStatusEventType.Stopping)
                {
                    Notify(EventMessageType.ProgramStopping, null);
                }
            };
        }

        public static void Notify(EventMessageType eventMessageType, object messageObject)
        {
            OnEventOccured(new EventMessage(eventMessageType, messageObject));
        }

        public static event EventPostedEventHandler EventOccured;

        private static void OnEventOccured(EventMessage message)
        {
            EventOccured?.Invoke(message);
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
        SystemMonitorStatsChange
    }
}