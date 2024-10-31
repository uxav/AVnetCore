using System.Collections.Concurrent;
using System.ComponentModel;
using Newtonsoft.Json.Linq;

namespace UXAV.AVnet.Core.Config
{
    public abstract class ConfigBase
    {
        public string ConfigName { get; set; }
        public string SystemType { get; set; }
        public string SystemName { get; set; }

        public abstract void CreateDefault();

        public override string ToString()
        {
            return JToken.FromObject(this).ToString();
        }
    }
}