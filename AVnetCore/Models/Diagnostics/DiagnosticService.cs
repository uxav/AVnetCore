using System.Collections.Generic;
using System.Threading.Tasks;

namespace UXAV.AVnetCore.Models.Diagnostics
{
    public static class DiagnosticService
    {
        private static SystemBase _system;
        private static GetSystemMessagesHandler _callback;

        static DiagnosticService()
        {
        }

        internal static void RegisterSystemCallback(SystemBase system, GetSystemMessagesHandler callback)
        {
            _system = system;
            _callback = callback;
        }

        private static IEnumerable<DiagnosticMessage> GetDeviceMessages()
        {
            var messages = new List<DiagnosticMessage>();
            return messages;
        }

        public static IEnumerable<DiagnosticMessage> GetMessages()
        {
            var messages = new List<DiagnosticMessage>();
            var systemMessagesTask = Task.Run(() => _callback.Invoke());
            var deviceMessagesTask = Task.Run(GetDeviceMessages);
            Task.WhenAll(systemMessagesTask, deviceMessagesTask);
            messages.AddRange(systemMessagesTask.Result);
            messages.AddRange(deviceMessagesTask.Result);
            return messages;
        }
     }

    internal delegate IEnumerable<DiagnosticMessage> GetSystemMessagesHandler();
}