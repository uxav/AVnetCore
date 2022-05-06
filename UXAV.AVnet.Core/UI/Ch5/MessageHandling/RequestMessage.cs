using System;
using Newtonsoft.Json.Linq;

namespace UXAV.AVnet.Core.UI.Ch5.MessageHandling
{
    public sealed class RequestMessage : MessageBase
    {
        internal RequestMessage(JToken token)
        {
            if (token["jsonrpc"]?.Value<string>() != "2.0")
                throw new FormatException("message format not specified at JSON RPC 2.0");

            Id = token["id"]?.Value<int>();
            Method = token["method"]?.Value<string>() ?? string.Empty;

            if (string.IsNullOrEmpty("method")) throw new ArgumentException("method not specified");

            RequestParams = token["params"];
        }
    }
}