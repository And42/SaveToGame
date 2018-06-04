using System.Collections.Generic;
using Newtonsoft.Json;

namespace SaveToGameWpf.Logic.JsonMappings
{
    internal class WebConfig
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("changes")]
        public Dictionary<string, string> ChangesLinks { get; set; }
    }
}
