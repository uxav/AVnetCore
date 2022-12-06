using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using Cronos;
using UXAV.Logging;

namespace UXAV.AVnet.Core
{
    public static class CronJobs
    {
        private static readonly List<EventWaitHandle> Waits = new List<EventWaitHandle>();

        static CronJobs()
        {
            CrestronEnvironment.ProgramStatusEventHandler += CrestronEnvironmentOnProgramStatusEventHandler;
        }

        private static void CrestronEnvironmentOnProgramStatusEventHandler(eProgramStatusEventType programEventType)
        {
            if (programEventType != eProgramStatusEventType.Stopping) return;
            foreach (var waitHandle in Waits)
                waitHandle.Set();
        }

        /// <summary>
        ///     Add a cronjob using a cron expression
        /// </summary>
        /// <param name="expression">Expression for the timing of the job</param>
        /// <example>"30 07 * * 1-5" would equate to 07:30 on every day-of-week from Monday through Friday</example>
        /// <param name="callback">Callback action when job is triggered</param>
        /// <returns>Task</returns>
        public static Task Add(string expression, Action callback)
        {
            var cronJob = CronExpression.Parse(expression);
            return Task.Run(() =>
            {
                var waitHandle = CreateWaitHandle();
                while (true)
                {
                    var offset = cronJob.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Local);
                    if (offset == null) return;
                    var time = ((DateTimeOffset)offset).DateTime;
                    var waitTime = time - DateTime.Now;
                    var signaled = waitHandle.WaitOne(waitTime);
                    if (signaled) return;

                    try
                    {
                        Task.Run(callback);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }
                }
            });
        }

        private static EventWaitHandle CreateWaitHandle()
        {
            var handle = new AutoResetEvent(false);
            Waits.Add(handle);
            return handle;
        }
    }
}