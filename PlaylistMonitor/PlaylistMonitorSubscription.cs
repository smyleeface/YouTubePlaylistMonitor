using Newtonsoft.Json;

namespace Smylee.PlaylistMonitor.PlaylistMonitor {

    public class PlaylistMonitorSubscription {

        [JsonProperty("channelId")]
        public string ChannelId;
        
        [JsonProperty("playlistName")]
        public string PlaylistName;
    }
}
