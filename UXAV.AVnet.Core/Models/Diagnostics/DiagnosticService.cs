using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UXAV.AVnet.Core.Models.Diagnostics
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
            var dangerCount = messages.Count(m => m.Level == MessageLevel.Danger);
            var warningCount = messages.Count(m => m.Level == MessageLevel.Warning);
            var infoCount = messages.Count(m => m.Level == MessageLevel.Info);
            var successCount = messages.Count(m => m.Level == MessageLevel.Success);
            var level = "success";
            var levelCount = successCount;
            if (infoCount > 0)
            {
                level = "info";
                levelCount = infoCount;
            }

            if (warningCount > 0)
            {
                level = "warning";
                levelCount = warningCount;
            }

            if (dangerCount > 0)
            {
                level = "danger";
                levelCount = dangerCount;
            }

            var stats = new
            {
                @Danger = dangerCount,
                @Warning = messages.Count(m => m.Level == MessageLevel.Warning),
                @Info = messages.Count(m => m.Level == MessageLevel.Info),
                @Success = messages.Count(m => m.Level == MessageLevel.Success),
                @Level = level,
                @LevelCount = levelCount
            };
            EventService.Notify(EventMessageType.DiagnosticsMessagesUpdated, stats);
            return messages;
        }
    }

    internal delegate IEnumerable<DiagnosticMessage> GetSystemMessagesHandler();
}