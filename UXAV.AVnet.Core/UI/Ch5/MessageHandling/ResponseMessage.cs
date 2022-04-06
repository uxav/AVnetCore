using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace UXAV.AVnet.Core.UI.Ch5.MessageHandling
{
    public class ResponseMessage : MessageBase
    {
        internal ResponseMessage(string id, object result)
        {
            Id = id;
            Result = result;
        }

        internal ResponseMessage(string id, Exception error)
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

        public string Id { get; }
        public override string Method { get; }
        public object Result { get; }
        public object Error { get; }

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

                if (Result != null)
                {
                    json.WritePropertyName("result");
                    json.WriteRawValueAsync(JsonConvert.SerializeObject(Result, Formatting.None,
                        new JsonSerializerSettings
                        {
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                            NullValueHandling = NullValueHandling.Ignore,
                            Converters = new List<JsonConverter>
                            {
                                new StringEnumConverter()
                            }
                        }));
                }
                else if (Error != null)
                {
                    json.WritePropertyName("error");
                    json.WriteRawValueAsync(JToken.FromObject(Error).ToString(Formatting.None));
                }
                else
                {
                    json.WritePropertyName("result");
                    json.WriteRawValueAsync("null");
                }

                json.WriteEndObject();

                return sw.ToString();
            }
        }
    }
}