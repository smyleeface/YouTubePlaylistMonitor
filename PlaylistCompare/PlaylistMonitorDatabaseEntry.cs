using System.Collections.Generic;
using Newtonsoft.Json;
using Smylee.PlaylistMonitor.PlaylistMonitor;

namespace Smylee.YouTube.PlaylistCompare {

    public class PlaylistMonitorDatabaseEntry {

        [JsonProperty("email")]
        public string Email;
        
        [JsonProperty("channelId")]
        public string ChannelId;
        
        [JsonProperty("playlistId")]
        public string PlaylistId;
        
        [JsonProperty("playlistName")]
        public string PlaylistName;
        
        [JsonProperty("playlistItems")]
        public List<PlaylistMonitorPlaylistItem> PlaylistItems;
        
        [JsonProperty("lastCheckedTimestamp")]
        public string LastCheckedTimestamp;
                
    }
}
