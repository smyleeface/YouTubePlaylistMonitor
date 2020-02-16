using Newtonsoft.Json;

namespace Smylee.YouTube.PlaylistMonitor.Library.Models {

    public class PlaylistMonitorSubscription {

        [JsonProperty("channelId")]
        public string ChannelId;
        
        [JsonProperty("playlistName")]
        public string PlaylistName;
        
        [JsonProperty("finalEmail")]
        public string FinalEmail;
        
        [JsonProperty("timestamp")]
        public string Timestamp;
    }
}
