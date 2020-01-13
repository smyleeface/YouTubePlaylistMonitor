using System.ComponentModel;
using Newtonsoft.Json;

namespace Smylee.PlaylistMonitor.PlaylistMonitor {

    public class Subscription {
        [JsonProperty("userName")]
        public string UserName;
        
        [JsonProperty("playlistName")]
        public string PlaylistName;
    }
}
