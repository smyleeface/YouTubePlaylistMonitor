using System.ComponentModel;
using Newtonsoft.Json;

namespace Smylee.PlaylistMonitor.PlaylistMonitor {

    public class PlaylistItem {
        [JsonProperty("id")]
        public string Id;
        
        [JsonProperty("title")]
        public string Title;
        
        [JsonProperty("link")]
        public string Link;
        
        [JsonProperty("author")]
        public string ChannelTitle;
        
        [JsonProperty("position")]
        public long? Position;
        
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description;
    }
}
