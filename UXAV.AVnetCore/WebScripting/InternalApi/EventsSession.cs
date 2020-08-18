using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UXAV.AVnetCore.Models;
using UXAV.Logging;

namespace UXAV.AVnetCore.WebScripting.InternalApi
{
    public class EventsSession : IDisposable
    {
        private static int _count;
        private readonly BlockingCollection<EventMessage> _queue;
        private DateTime _lastCheckedTime;

        internal EventsSession()
        {
            _count++;
            Id = _count;
            _queue = new BlockingCollection<EventMessage>();
            EventService.EventOccured += EventHandler;
            Logger.Log("Created new {0} with ID {1}", GetType().Name, Id);
        }

        /// <summary>
        /// The session ID
        /// </summary>
        public int Id { get; }

        private void EventHandler(EventMessage eventObject)
        {
            _queue.Add(eventObject);
        }

        /// <summary>
        /// Blocking call to get event messages when available. Returns empty after 60 seconds if no updates.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<EventMessage> GetMessages()
        {
            _lastCheckedTime = DateTime.Now;

            var messages = new List<EventMessage>();

            try
            {
                var itemValid = _queue.TryTake(out var item, 30000);
                if (itemValid)
                {
                    messages.Add(item);

                    while (itemValid)
                    {
                        itemValid = _queue.TryTake(out item, 20);
                        if (itemValid)
                        {
                            messages.Add(item);
                        }
                    }
                }
            }
            catch
            {
                return messages;
            }
            
            Thread.Sleep(200);

            return messages;
        }

        public bool IsActive => DateTime.Now - _lastCheckedTime < TimeSpan.FromMinutes(5);

        public void Dispose()
        {
            EventService.EventOccured -= EventHandler;
            _queue?.Dispose();
        }
    }
}