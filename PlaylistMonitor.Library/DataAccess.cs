using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json;
using Smylee.PlaylistMonitor.Library.Models;

namespace Smylee.PlaylistMonitor.Library {

    public class DataAccess {
        
        // --- Fields ---
        private readonly IDependencyProvider _provider;

        // --- Constructor ---
        public DataAccess(IDependencyProvider provider) {
            _provider = provider;
        }

        // --- Methods ---
#region ChannelSnippet

        public async Task<ChannelSnippetDb> GetChannelDataAsync(string channelId, string playlistTitle) {
            var cacheResponse = await GetChannelDataFromCacheAsync(channelId, playlistTitle);
            var snippet = cacheResponse ?? await GetChannelDataFromYouTubeAsync(channelId, playlistTitle);
            return snippet;
        }
        
        private async Task<ChannelSnippetDb> GetChannelDataFromCacheAsync(string channelId, string playlistTitle) {
            var cacheResponse = await _provider.DynamoDbGetCacheChannelAsync(channelId, playlistTitle);
            if (cacheResponse.Item.Count <= 0) return null;
            var snippetValues = cacheResponse.Item.Where(values => values.Key == "channelSnippet").ToList();
            return snippetValues.Count <= 0 ? null : JsonConvert.DeserializeObject<ChannelSnippetDb>(snippetValues.First().Value.S);
        }
        
        private async Task<ChannelSnippetDb> GetChannelDataFromYouTubeAsync(string channelId, string playlistTitle) {
            var snippet = await _provider.YouTubeApiChannelSnippetAsync(channelId);
            await _provider.DynamoDbPutCacheChannelAsync(channelId, playlistTitle, snippet);
            return snippet.ToChannelSnippetDb();
        }

#endregion

#region PlaylistSnippet
        
        public async Task<PlaylistSnippetDb> GetPlaylistDataAsync(string channelId, string playlistTitle) {
            var cacheResponse = await GetPlaylistDataFromCacheAsync(channelId, playlistTitle);
            var playlistSnippets = cacheResponse ?? await GetPlaylistDataFromYouTubeAsync(channelId, playlistTitle);
            return playlistSnippets;
        }
        
        private async Task<PlaylistSnippetDb> GetPlaylistDataFromCacheAsync(string channelId, string playlistTitle) {
            var cacheResponse = await _provider.DynamoDbGetCachePlaylistsAsync(channelId, playlistTitle);
            if (cacheResponse.Item.Count <= 0) return null;
            var snippetValues = cacheResponse.Item.Where(values => values.Key == "playlistSnippet").Select(x => JsonConvert.DeserializeObject<PlaylistSnippetDb>(x.Value.S)).ToList();
            return snippetValues.Count <= 0 ? null : snippetValues.First(x => x.Title == playlistTitle);
        }
        
        private async Task<PlaylistSnippetDb> GetPlaylistDataFromYouTubeAsync(string channelId, string playlistTitle) {
            string nextPageToken = null;
            var playlists = new List<Playlist>();
            do {
                var response = await _provider.YouTubeApiPlaylistsAsync(channelId, nextPageToken);
                playlists.AddRange(response.Items);
                nextPageToken = response.NextPageToken;
            } while (nextPageToken != null);
            if (playlists.Count > 0) {
                var playlist = playlists.FirstOrDefault(x => x.Snippet.Title == playlistTitle);
                if (playlist == null) {
                    return null;
                }
                var playlistSnippetDb = playlist.ToPlaylistSnippetDb();
                await _provider.DynamoDbUpdateCachePlaylistAsync(channelId, playlistSnippetDb);
                return playlistSnippetDb;
            }
            return null;
        }

#endregion

#region PlaylistItemsSnippet

        public async Task<List<PlaylistItemsSnippetDb>> GetPlaylistItemDataFromCacheAsync(string channelId, string playlistTitle) {
            var databaseResponse = await _provider.DynamoDbGetCachePlaylistsItemsAsync(channelId, playlistTitle);
            if (!databaseResponse.Item.TryGetValue("playlistItemsSnippet", out var playlistItems)) return null;
            return playlistItems.L.Select(x => JsonConvert.DeserializeObject<PlaylistItemsSnippetDb>(x.S)).ToList(); //TODO double check this
        }


        public async Task<List<PlaylistItemsSnippetDb>> GetRecentPlaylistItemDataAsync(string playlistId) {
            var playListDataFromYouTube = await GetPlaylistItemDataFromYouTubeAsync(playlistId);
            var videoData = await GetVideoDataAsync(playListDataFromYouTube.Select(x => x.Id).ToList());
            playListDataFromYouTube.AddVideoChannel(videoData);
            return playListDataFromYouTube;
        }

        public async Task<List<PlaylistItemsSnippetDb>> GetPlaylistItemDataFromYouTubeAsync(string playlistId) {
            string nextPageToken = null;
            var playlistItem = new List<PlaylistItem>();
            do {
                var response = await _provider.YouTubeApiPlaylistItemsAsync(playlistId, nextPageToken);
                playlistItem.AddRange(response.Items);
                nextPageToken = response.NextPageToken;
            } while (nextPageToken != null);
            return playlistItem.Where(x => x.Snippet.Title != "Deleted video" && x.Snippet.Title != "Private video").Select(x => x.ToPlaylistItemsSnippetDb()).ToList();
        }
        
        public async Task UpdatePlaylistItemDataFromCacheAsync(string playlistId, string playlistTitle, List<PlaylistItemsSnippetDb> playlistsItems) {
            await _provider.DynamoDbUpdateCachePlaylistItemsAsync(playlistId, playlistTitle, playlistsItems);
        }

#endregion
        
#region VideoSnippet
        
        public async Task<List<VideoSnippetDb>> GetVideoDataAsync(List<string> videoIds) {
            var cachedData = await GetVideoDataFromCacheAsync(videoIds);
            var missingVideoId = videoIds.Where(videoId => !cachedData.Select(y => y.VideoId).Contains(videoId)).ToList();
            if (!missingVideoId.Any()) return cachedData;
            var youtubeData = await GetVideoDataFromYouTubeAsync(missingVideoId);
            cachedData.AddRange(youtubeData);
            await UpdateVideoDataInCacheAsync(youtubeData);
            return cachedData;
        }
        
        public async Task<List<VideoSnippetDb>> GetVideoDataFromCacheAsync(List<string> videoIds) {
            var videos = new List<VideoSnippetDb>();
            var dynamoDbResponse = await _provider.DynamoDbGetCacheVideoDataAsync(videoIds);
            foreach (var item in dynamoDbResponse.SelectMany(response => response.Responses.Values)) {
                videos.AddRange(item.Select(data => data.ToVideoSnippetDb()));
            }
            return videos;
        }
        
        public async Task UpdateVideoDataInCacheAsync(List<VideoSnippetDb> videoSnippets) {
            var dateNow = DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString(CultureInfo.InvariantCulture);
            var allWriteRequests = new List<List<WriteRequest>>();
            var writeRequests = new List<WriteRequest>();
            foreach (var videoSnippet in videoSnippets.Select((value, index) => new {value, index})) {
                writeRequests.Add(new WriteRequest {
                    PutRequest = new PutRequest {
                        Item = new Dictionary<string, AttributeValue> {
                            {"videoId", new AttributeValue {
                                S = videoSnippet.value.VideoId
                            }},
                            {"videoTitle", new AttributeValue {
                                S = videoSnippet.value.VideoTitle
                            }},
                            {"channelId", new AttributeValue {
                                S = videoSnippet.value.ChannelId
                            }},
                            {"channelTitle", new AttributeValue {
                                S = videoSnippet.value.ChannelTitle
                            }},
                            {"timestamp", new AttributeValue {
                                N = dateNow
                            }}
                        }
                    }
                });
            }
            for (int i = 0; i < writeRequests.Count; i += 25) { 
                allWriteRequests.Add(writeRequests.GetRange(i, Math.Min(25, writeRequests.Count - i))); 
            } 
            foreach (var writeRequest in allWriteRequests) {
                await _provider.DynamoDbUpdateCacheVideoDataAsync(writeRequest);
            }
        }
        public async Task<List<VideoSnippetDb>> GetVideoDataFromYouTubeAsync(List<string> videoIds) {
            var videos = new List<VideoSnippetDb>();
            foreach (var videoId in videoIds) {
                var youtubeResponse = await _provider.YouTubeApiVideosAsync(videoId);
                videos.Add(youtubeResponse.Items.First().ToVideoSnippetDb());
            }
            return videos;
        }
        
#endregion

        public async Task SendEmailAsync(string fromEmail, string requestEmail, string subject, string finalEmail) {
            await _provider.SesSendEmail(fromEmail, requestEmail, subject, finalEmail);
        }

        public async Task UpdateSubscriptionCacheAsync(string requestEmail, string finalEmail) {
            await _provider.DynamoDbUpdateSubscriptionCacheAsync(requestEmail, finalEmail);
        }

        public async Task<(ChannelSnippetDb, PlaylistSnippetDb, List<PlaylistItemsSnippetDb>)> GetSubscriptionData(string channelId, string playlistTitle) {
            var channelSnippet = await GetChannelDataAsync(channelId, playlistTitle);
            var playlistData = await GetPlaylistDataAsync(channelId, playlistTitle);
            var oldPlaylistItems = await GetPlaylistItemDataFromCacheAsync(channelId, playlistTitle);
            return (channelSnippet, playlistData, oldPlaylistItems);
        }
    }
}
