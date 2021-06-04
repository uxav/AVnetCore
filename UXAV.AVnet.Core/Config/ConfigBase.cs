using System.Collections.Concurrent;
using System.ComponentModel;
using Newtonsoft.Json.Linq;

namespace UXAV.AVnet.Core.Config
{
    public abstract class ConfigBase
    {
        [DisplayName("PList Dictionary")]
        [Description("PList for custom values as strings")]
        public ConcurrentDictionary<string, object> PropertyList { get; set; }

        public abstract void CreateDefault();

        public string ConfigName { get; set; }
        public string SystemType { get; set; }
        public string SystemName { get; set; }

        public override string ToString()
        {
            return JToken.FromObject(this).ToString();
        }
    }
}