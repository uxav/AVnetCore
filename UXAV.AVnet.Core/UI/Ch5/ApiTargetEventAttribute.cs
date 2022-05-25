using System;

namespace UXAV.AVnet.Core.UI.Ch5
{
    /// <summary>
    /// Attribute for API target event subscription info
    /// </summary>
    public class ApiTargetEventAttribute : ApiTargetAttributeBase
    {
        /// <summary>
        /// Create a new ApiTargetEventAttribute
        /// </summary>
        /// <param name="name">Name of event in API</param>
        /// <param name="eventName">Event name of the event to subscribe to on the returned object</param>
        /// <param name="subscriptionType">The type of subscription class. Create a custom class inheriting from
        /// <see cref="EventSubscription"/> should you require a custom notification or to notify on creation</param>
        public ApiTargetEventAttribute(string name, string eventName, Type subscriptionType)
        {
            Name = name;
            EventName = eventName;
            SubscriptionType = subscriptionType;
        }

        public override string Name { get; }
        public string EventName { get; }
        public Type SubscriptionType { get; }
    }
}