using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Crestron.SimplSharp;
using UXAV.Logging;

namespace UXAV.AVnetCore.WebScripting.InternalApi
{
    public class EventsApiHandler : ApiRequestHandler
    {
        private static readonly Dictionary<int, EventsSession> Sessions = new Dictionary<int, EventsSession>();
        private static Thread _thread;
        private static readonly AutoResetEvent ThreadWait = new AutoResetEvent(false);

        public EventsApiHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request, true)
        {
        }

        // ReSharper disable once UnusedMember.Global - Called using reflection
        public void Get()
        {
            switch (Request.RoutePatternArgs["method"].ToLower())
            {
                case "start":
                    WriteResponse(new
                    {
                        @SessionId = Start()
                    });
                    return;
                case "poll":
                    if (!Request.RoutePatternArgs.ContainsKey("id"))
                    {
                        HandleError(400, "Bad Request", $"No session id specified");
                        return;
                    }

                    var id = int.Parse(Request.RoutePatternArgs["id"]);
                    lock (Sessions)
                    {
                        if (!Sessions.ContainsKey(id))
                        {
                            HandleError(400, "Bad Request", $"No sessions for this id value");
                            return;
                        }
                    }

                    //CrestronConsole.PrintLine("Event poll for session " + id);
                    try
                    {
                        EventsSession session;
                        lock (Sessions)
                        {
                            session = Sessions[id];
                        }

                        WriteResponse(session.GetMessages());
                        //CrestronConsole.PrintLine("Responded for session " + id);
                        return;
                    }
                    catch (ThreadAbortException)
                    {
                        //CrestronConsole.PrintLine("ThreadAbortException for session " + id);
                        return;
                    }
                    catch(Exception e)
                    {
                        Logger.Log($"Error in {GetType().FullName}, ${e.Message}");
                        return;
                    }
                default:
                    HandleError(400, "Bad Request", $"Method \"{Request.RoutePatternArgs["method"]}\" is not valid");
                    return;
            }
        }

        private static int Start()
        {
            var session = new EventsSession();
            lock (Sessions)
            {
                Sessions.Add(session.Id, session);
            }

            Logger.Debug($"Created session event handler with ID {session.Id}");

            if (_thread != null) return session.Id;

            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                if (type == eProgramStatusEventType.Stopping) ThreadWait.Set();
            };

            void SessionCleanupProcess(object o)
            {
                try
                {
                    while (true)
                    {
                        if (ThreadWait.WaitOne(60000))
                        {
                            ErrorLog.Notice("Program stopping, exiting thread and disposing any events");
                            lock (Sessions)
                            {
                                foreach (var s in Sessions.Values)
                                {
                                    ErrorLog.Info($"Disposing {s.GetType().Name} with ID {s.Id}");
                                    s.Dispose();
                                }
                            }

                            return;
                        }

                        lock (Sessions)
                        {
                            var expiredSessionIds = Sessions.Where(kvp => !kvp.Value.IsActive)
                                .Select(kvp => kvp.Key)
                                .ToArray();
                            foreach (var id in expiredSessionIds)
                            {
                                Sessions[id].Dispose();
                                Sessions.Remove(id);
                            }

                            foreach (var id in expiredSessionIds)
                            {
                                Logger.Debug($"Disposed and removed session event handler with ID {id}");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    ErrorLog.Exception("Error in events api cleanup thread", e);
                }

                ErrorLog.Notice("Exiting events api cleanup thread");
            }

            _thread = new Thread(SessionCleanupProcess)
            {
                IsBackground = true,
                Priority = ThreadPriority.Lowest,
                Name = "EventsApiHandler session cleanup worker"
            };
            _thread.Start();

            return session.Id;
        }
    }
}