using Newtonsoft.Json;

namespace Smylee.PlaylistMonitor.Library.Models {

    public class PlaylistItemsSnippetDb {
        
        [JsonProperty("Id")]
        public string Id;
        
        [JsonProperty("title")]
        public string Title;
        
        [JsonProperty("channelName")]
        public string ChannelTitle;
        
        [JsonProperty("channelId")]
        public string ChannelId;
        
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description;

        [JsonProperty("thumbnail")]
        public string Thumbnail;
        
        [JsonProperty("thumbnailWidth")]
        public long? ThumbnailWidth;
        
        [JsonProperty("thumbnailHeight")]
        public long? ThumbnailHeight;
    }
    
}