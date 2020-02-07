using Newtonsoft.Json;

namespace Smylee.PlaylistMonitor.PlaylistMonitor {

    public class PlaylistMonitorSubscription {

        [JsonProperty("userName")]
        public string UserName;
        
        [JsonProperty("playlistName")]
        public string PlaylistName;
    }
}
