using Google.Apis.YouTube.v3.Data;
using Smylee.YouTube.PlaylistMonitor.Library.Models;

namespace Smylee.YouTube.PlaylistMonitor.Library {

    public static class GoogleApiEx {
        public static ChannelSnippetDb ToChannelSnippetDb(this ChannelSnippet channelSnippet) {
            return new ChannelSnippetDb {
                Description = channelSnippet.Description,
                Title = channelSnippet.Title,
                Thumbnail = channelSnippet.Thumbnails.High.Url,
                ThumbnailWidth = channelSnippet.Thumbnails.High.Width,
                ThumbnailHeight = channelSnippet.Thumbnails.High.Height
            };
        }
        
        public static PlaylistSnippetDb ToPlaylistSnippetDb(this Playlist playlistItem) {
            return new PlaylistSnippetDb {
                Id = playlistItem.Id,
                Description = playlistItem.Snippet.Description,
                Title = playlistItem.Snippet.Title,
                Thumbnail = playlistItem.Snippet.Thumbnails.High.Url,
                ThumbnailWidth = playlistItem.Snippet.Thumbnails.High.Width,
                ThumbnailHeight = playlistItem.Snippet.Thumbnails.High.Height
            };
        }
        
        public static PlaylistItemsSnippetDb ToPlaylistItemsSnippetDb(this PlaylistItem playlistItem) {
            return new PlaylistItemsSnippetDb {
                Id = playlistItem.Snippet.ResourceId.VideoId,
                Description = playlistItem.Snippet.Description,
                Title = playlistItem.Snippet.Title,
                Thumbnail = playlistItem.Snippet.Thumbnails.High.Url,
                ThumbnailWidth = playlistItem.Snippet.Thumbnails.High.Width,
                ThumbnailHeight = playlistItem.Snippet.Thumbnails.High.Height
            };
        }
    }
    
}
