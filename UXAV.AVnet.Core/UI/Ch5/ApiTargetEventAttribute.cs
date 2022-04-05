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
        /// <param name="subscriptionType">The type of subscription class</param>
        /// <param name="methodToGetCurrentValue">The optional name of a method to call on the object upon
        /// subscription to notify current value</param>
        public ApiTargetEventAttribute(string name, string eventName, Type subscriptionType, string methodToGetCurrentValue = null)
        {
            Name = name;
            EventName = eventName;
            SubscriptionType = subscriptionType;
            MethodToGetCurrentValue = methodToGetCurrentValue;
        }

        public override string Name { get; }
        public string EventName { get; }
        public Type SubscriptionType { get; }
        public string MethodToGetCurrentValue { get; }
    }
}