using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.Model;
using Google.Apis.YouTube.v3.Data;
using Smylee.PlaylistMonitor.Library.Models;

namespace Smylee.PlaylistMonitor.Library {

    public static class PlaylistMonitorEx {
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
