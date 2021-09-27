using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UXAV.AVnet.Core.UI.Ch5.MessageHandling
{
    public class NotificationMessage : MessageBase
    {
        internal NotificationMessage(string method, object messageParams)
        {
            Method = method;
            RequestParams = JToken.FromObject(messageParams);
        }

        public override string Method { get; }

        public JToken RequestParams { get; }

        public override string ToString()
        {
            using (var sw = new StringWriter())
            {
                var json = new JsonTextWriter(sw);
                json.WriteStartObject();
                json.WritePropertyName("jsonrpc");
                json.WriteValue("2.0");

                if (!string.IsNullOrEmpty(Method))
                {
                    json.WritePropertyName("method");
                    json.WriteValue(Method);
                }

                if (RequestParams != null)
                {
                    json.WritePropertyName("params");
                    json.WriteRawValueAsync(RequestParams.ToString());
                }

                json.WriteEndObject();

                return sw.ToString();
            }
        }
    }
}