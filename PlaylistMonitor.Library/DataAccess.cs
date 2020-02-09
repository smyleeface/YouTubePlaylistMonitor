using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json;
using Smylee.YouTube.PlaylistMonitor.Library.Models;

namespace Smylee.YouTube.PlaylistMonitor.Library {

    public class DataAccess {
        private readonly IDependencyProvider _provider;

        public DataAccess(IDependencyProvider provider) {
            _provider = provider;
        }
        

#region ChannelSnippet

        public async Task<ChannelSnippetDb> GetChannelDataAsync(string channelId) {
            var cacheResponse = await GetChannelDataFromCacheAsync(channelId);
            var snippet = cacheResponse ?? await GetChannelDataFromYouTubeAsync(channelId);
            return snippet;
        }
        
        private async Task<ChannelSnippetDb> GetChannelDataFromCacheAsync(string channelId) {
            var cacheResponse = await _provider.DynamoDbGetCacheChannelAsync(channelId);
            if (cacheResponse.Item.Count <= 0) return null;
            var snippetValues = cacheResponse.Item.Where(values => values.Key == "channelSnippet").ToList();
            return snippetValues.Count <= 0 ? null : JsonConvert.DeserializeObject<ChannelSnippetDb>(snippetValues.First().Value.S);
        }
        
        private async Task<ChannelSnippetDb> GetChannelDataFromYouTubeAsync(string channelId) {
            var snippet = await _provider.YouTubeApiChannelSnippetAsync(channelId);
            await _provider.DynamoDbPutPCacheChannelAsync(channelId, snippet);
            return snippet.ToChannelSnippetDb();
        }

#endregion

#region Playlist
        
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
            var playlist = playlists.First(x => x.Snippet.Title == playlistTitle).ToPlaylistSnippetDb();
            await _provider.DynamoDbPutPCachePlaylistAsync(channelId, playlist);
            return playlist;
        }

#endregion

#region PlaylistItems

        public async Task<List<PlaylistItemsSnippetDb>> GetPlaylistItemDataFromCacheAsync(string channelId, string playlistTitle) {
            var databaseResponse = await _provider.DynamoDbGetCachePlaylistsItemsAsync(channelId, playlistTitle);
            if (!databaseResponse.Item.TryGetValue("playlistItemsSnippet", out var playlistItems)) return null;
            return playlistItems.L.Select(x => JsonConvert.DeserializeObject<PlaylistItemsSnippetDb>(x.S)).ToList();
        }
        public async Task<List<PlaylistItemsSnippetDb>> GetPlaylistItemDataFromYouTubeAsync(string playlistId) {
            string nextPageToken = null;
            var playlistItem = new List<PlaylistItem>();
            do {
                var response = await _provider.YouTubeApiPlaylistItemsAsync(playlistId, nextPageToken);
                playlistItem.AddRange(response.Items);
                nextPageToken = response.NextPageToken;
            } while (nextPageToken != null);
            Console.Write(JsonConvert.SerializeObject(playlistItem));
            return playlistItem.Where(x => x.Snippet.Title != "Deleted video" && x.Snippet.Title != "Private video").Select(x => x.ToPlaylistItemsSnippetDb()).ToList();
        }
        
        public async Task PutPlaylistItemDataFromCacheAsync(string playlistId, string playlistTitle, PlaylistSnippetDb playlistSnippet, List<PlaylistItemsSnippetDb> playlistsItems) {
            await _provider.DynamoDbPutPCachePlaylistItemsAsync(playlistId, playlistTitle, playlistSnippet, playlistsItems);
        }

#endregion

        public async Task SendEmail(string fromEmail, string requestEmail, string subject, string finalEmail) {
            await _provider.SesSendEmail(fromEmail, requestEmail, subject, finalEmail);
        }
    }
}
