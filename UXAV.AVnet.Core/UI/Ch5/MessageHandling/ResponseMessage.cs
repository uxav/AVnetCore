using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace UXAV.AVnet.Core.UI.Ch5.MessageHandling
{
    public sealed class ResponseMessage : MessageBase
    {
        internal ResponseMessage(int id, object result)
        {
            Id = id;
            Result = result;
        }

        internal ResponseMessage(int id, Exception error)
        {
            Id = id;
            Error = new
            {
                Type = error.GetType().Name,
                error.Message,
                error.StackTrace
            };
        }

        internal ResponseMessage(Exception error)
        {
            Error = error;
        }

        [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
        public object Result { get; }

        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public object Error { get; }
    }
}