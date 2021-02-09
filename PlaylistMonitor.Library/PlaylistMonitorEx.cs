using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.Model;
using Google.Apis.YouTube.v3.Data;
using Smylee.PlaylistMonitor.Library.Models;

namespace Smylee.PlaylistMonitor.Library {

    public static class PlaylistMonitorEx {
        public static ChannelSnippetDb ToChannelSnippetDb(this ChannelSnippet channelSnippet) {
            var channelSnippetDb = new ChannelSnippetDb {
                Title = channelSnippet.Title
            };
            if (!string.IsNullOrEmpty(channelSnippet.Description)) {
                channelSnippetDb.Description = channelSnippet.Description;
            }
            if (channelSnippet.Thumbnails != null) {
                channelSnippetDb.Thumbnail = channelSnippet.Thumbnails.High.Url;
                channelSnippetDb.ThumbnailWidth = channelSnippet.Thumbnails.High.Width;
                channelSnippetDb.ThumbnailHeight = channelSnippet.Thumbnails.High.Height;
            }
            return channelSnippetDb;
        }
        
        public static PlaylistSnippetDb ToPlaylistSnippetDb(this Playlist playlistItem) {
            var playlistSnippetDb = new PlaylistSnippetDb {
                Id = playlistItem.Id,
                Title = playlistItem.Snippet.Title
            };
            if (!string.IsNullOrEmpty(playlistItem.Snippet.Description)) {
                playlistSnippetDb.Description = playlistItem.Snippet.Description;
            }
            if (playlistItem.Snippet.Thumbnails != null) {
                playlistSnippetDb.Thumbnail = playlistItem.Snippet.Thumbnails.High.Url;
                playlistSnippetDb.ThumbnailWidth = playlistItem.Snippet.Thumbnails.High.Width;
                playlistSnippetDb.ThumbnailHeight = playlistItem.Snippet.Thumbnails.High.Height;
            }
            return playlistSnippetDb;
        }
        
        public static PlaylistItemsSnippetDb ToPlaylistItemsSnippetDb(this PlaylistItem playlistItem) {
            var playlistItemsSnippetDb =  new PlaylistItemsSnippetDb {
                Id = playlistItem.Snippet.ResourceId.VideoId,
                Title = playlistItem.Snippet.Title
            };
            if (!string.IsNullOrEmpty(playlistItem.Snippet.Description)) {
                playlistItemsSnippetDb.Description = playlistItem.Snippet.Description;
            }
            if (playlistItem.Snippet.Thumbnails != null) {
                playlistItemsSnippetDb.Thumbnail = playlistItem.Snippet.Thumbnails.High.Url;
                playlistItemsSnippetDb.ThumbnailWidth = playlistItem.Snippet.Thumbnails.High.Width;
                playlistItemsSnippetDb.ThumbnailHeight = playlistItem.Snippet.Thumbnails.High.Height;
            }
            return playlistItemsSnippetDb;
        }
        
        public static VideoSnippetDb ToVideoSnippetDb(this Video video) {
            return new VideoSnippetDb {
                ChannelId = video.Snippet.ChannelId,
                ChannelTitle = video.Snippet.ChannelTitle,
                VideoTitle = video.Snippet.Title,
                VideoId = video.Id
            };
        }
        
        public static VideoSnippetDb ToVideoSnippetDb(this Dictionary<string, AttributeValue> data) {
            var videoSnippet = new VideoSnippetDb();
            foreach (var it in data.Keys.Select((x, i) => new { Value = x, Index = i }) ) {
                switch (it.Value) {
                    case "videoId": {
                        videoSnippet.VideoId = data.Values.ElementAt(it.Index).S;
                        break;
                    }
                    case "videoTitle": {
                        videoSnippet.VideoTitle = data.Values.ElementAt(it.Index).S;
                        break;
                    }
                    case "channelId": {
                        videoSnippet.ChannelId = data.Values.ElementAt(it.Index).S;
                        break;
                    }
                    case "channelTitle": {
                        videoSnippet.ChannelTitle = data.Values.ElementAt(it.Index).S;
                        break;
                    }
                }
            }
            return videoSnippet;
        }

        public static void AddVideoChannel(this List<PlaylistItemsSnippetDb> playListDataFromYouTube, List<VideoSnippetDb> videoData) {
            foreach (var playListData in playListDataFromYouTube) {
                playListData.ChannelId = videoData.Where(x => x.VideoId == playListData.Id).Select(y => y.ChannelId).Single();
                playListData.ChannelTitle = videoData.Where(x => x.VideoId == playListData.Id).Select(y => y.ChannelTitle).Single();
            }
        }
    }
    
}
