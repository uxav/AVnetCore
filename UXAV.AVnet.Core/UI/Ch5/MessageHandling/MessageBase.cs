using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace UXAV.AVnet.Core.UI.Ch5.MessageHandling
{
    public abstract class MessageBase
    {
        [JsonProperty("jsonrpc")] public string JsonRpc => "2.0";

        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public virtual int? Id { get; protected set; }

        [JsonProperty("method", NullValueHandling = NullValueHandling.Ignore)]
        public virtual string Method { get; protected set; }

        [JsonProperty("params", NullValueHandling = NullValueHandling.Ignore)]
        public virtual JToken RequestParams { get; protected set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.None, new JsonSerializerSettings
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