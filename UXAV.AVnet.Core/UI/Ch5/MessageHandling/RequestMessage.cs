using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UXAV.AVnet.Core.UI.Ch5.MessageHandling
{
    public class RequestMessage : MessageBase
    {
        internal RequestMessage(JToken token)
        {
            if (token["jsonrpc"]?.Value<string>() != "2.0")
                throw new FormatException("message format not specified at JSON RPC 2.0");

            Id = token["id"]?.Value<string>();
            Method = token["method"]?.Value<string>() ?? string.Empty;

            if (string.IsNullOrEmpty("method")) throw new ArgumentException("method not specified");

            RequestParams = token["params"];
        }

        public string Id { get; }
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
                if (Id != null)
                {
                    json.WritePropertyName("id");
                    json.WriteValue(Id);
                }

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