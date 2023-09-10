using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UXAV.AVnet.Core.UI.Ch5.MessageHandling
{
    public sealed class NotificationMessage : MessageBase
    {
        internal NotificationMessage(string method, object messageParams)
        {
            Method = method;
            if (messageParams == null) return;
            RequestParams = JsonConvert.SerializeObject(messageParams, Formatting.None, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                Converters = new List<JsonConverter>
                {
                    new StringEnumConverter()
                }
            });
        }
    }
}