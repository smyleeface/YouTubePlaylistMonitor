using Newtonsoft.Json;

namespace Smylee.YouTube.PlaylistMonitor.Library.Models {

    public class VideoSnippetDb {
        
        [JsonProperty("videoId")]
        public string VideoId;
        
        [JsonProperty("videoTitle")]
        public string VideoTitle;
        
        [JsonProperty("channelTitle")]
        public string ChannelTitle;
        
        [JsonProperty("channelId")]
        public string ChannelId;
    }
    
}