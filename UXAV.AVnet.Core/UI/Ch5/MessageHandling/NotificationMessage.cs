using Newtonsoft.Json.Linq;

namespace UXAV.AVnet.Core.UI.Ch5.MessageHandling
{
    public sealed class NotificationMessage : MessageBase
    {
        internal NotificationMessage(string method, object messageParams)
        {
            Method = method;
            RequestParams = JToken.FromObject(messageParams);
        }
    }
}