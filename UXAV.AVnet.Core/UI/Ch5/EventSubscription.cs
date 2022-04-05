using System;
using UXAV.Logging;

namespace UXAV.AVnet.Core.UI.Ch5
{
    public abstract class EventSubscription
    {
        private readonly string _eventName;
        private readonly Ch5ApiHandlerBase _apiHandler;
        private readonly object _eventObject;

        protected EventSubscription(Ch5ApiHandlerBase apiHandler, int id, string name, object eventObject, string eventName)
        {
            _eventName = eventName;
            _apiHandler = apiHandler;
            _eventObject = eventObject;
            Id = id;
            Name = name;
            var eventInfo = _eventObject.GetType().GetEvent(_eventName);
            var handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, this, "OnEvent");
            eventInfo.AddEventHandler(_eventObject, handler);
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
            var eventInfo = _eventObject.GetType().GetEvent(_eventName);
            var handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, this, "OnEvent");
            eventInfo.RemoveEventHandler(_eventObject, handler);
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