using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json;

namespace Smylee.YouTube.PlaylistMonitor.Library.Models {

    public class PlaylistItemsSnippetDb {
        
        [JsonProperty("Id")]
        public string Id;
        
        [JsonProperty("title")]
        public string Title;
        
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