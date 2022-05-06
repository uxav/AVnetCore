using Newtonsoft.Json.Linq;

namespace UXAV.AVnet.Core.UI.Ch5.MessageHandling
{
    public sealed class NotificationMessage : MessageBase
    {
        internal NotificationMessage(string method, object messageParams)
        {
            Method = method;
            if(messageParams == null) return;
            RequestParams = JToken.FromObject(messageParams);
        }
    }
}