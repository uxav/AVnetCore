using System;
using System.Reflection;
using UXAV.Logging;

namespace UXAV.AVnet.Core.UI.Ch5
{
    public abstract class EventSubscription
    {
        private readonly Ch5ApiHandlerBase _apiHandler;
        private readonly EventInfo _eventInfo;
        private readonly object _eventObject;

        protected EventSubscription(Ch5ApiHandlerBase apiHandler, int id, string name, object eventObject, string eventName)
        {
            _apiHandler = apiHandler;
            Id = id;
            Name = name;
            _eventInfo = eventObject.GetType().GetEvent(eventName);
            _eventObject = eventObject;
            var handler = Delegate.CreateDelegate(_eventInfo.EventHandlerType, this, "OnEvent");
            _eventInfo.AddEventHandler(_eventObject, handler);
        }

        protected void Notify(object value)
        {
            _apiHandler.SendNotificationInternal("event", new
            {
                Name,
                Id,
                Value = value
            });
        }

        internal void NotifyInternal(object value)
        {
            Notify(value);
        }

        public void Unsubscribe()
        {
            var handler = Delegate.CreateDelegate(_eventInfo.EventHandlerType, this, "OnEvent");
            _eventInfo.RemoveEventHandler(_eventObject, handler);
        }

        public int Id { get; }
        public string Name { get; }
    }

    /// <summary>
    /// Event subscription handler for notification subscriptions on CH5 API
    /// </summary>
    /// <typeparam name="TEventArgs">Event args type for the event handler</typeparam>
    public sealed class EventSubscription<TEventArgs> : EventSubscription
    {
        public EventSubscription(Ch5ApiHandlerBase handler, int id, string name, object eventObject, string eventName)
            : base(handler, id, name, eventObject, eventName)
        {
        }

        public void OnEvent(object sender, TEventArgs args)
        {
            Notify(args);
        }
    }
}