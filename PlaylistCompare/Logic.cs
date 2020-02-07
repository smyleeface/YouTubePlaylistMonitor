using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SimpleEmailV2.Model;
using Google.Apis.YouTube.v3.Data;
using LambdaSharp.Logger;
using Newtonsoft.Json;
using Smylee.PlaylistMonitor.PlaylistMonitor;

namespace Smylee.YouTube.PlaylistCompare {

    public class Logic {

        private readonly ILambdaLogLevelLogger _logger;
        private readonly IDependencyProvider _provider;
        private string _fromEmail;

        //--- Methods ---
        public Logic(string fromEmail, IDependencyProvider provider, ILambdaLogLevelLogger logger) {
            _provider = provider;
            _logger = logger;
            _fromEmail = fromEmail;
        }

        public async Task Run(DateTime dateNow, string requestEmail, List<PlaylistMonitorSubscription> requestedPlaylists) {
            
            _logger.LogInfo($"Processing subscription for {requestEmail}");
                 
            // init
            var dateNowTimestamp = dateNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString(CultureInfo.InvariantCulture);
            var dateNowString = dateNow.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
            var finalEmail = $"<h1>Playlist Monitor Report for {dateNowString}</h1><br /><br />";
            var databaseUpdates = new List<PlaylistMonitorDatabaseEntry>();
            
            // check each playlist
            // requestedPlaylists.ForEach(async playlist => {
            foreach (var playlist in requestedPlaylists) {
                
                var requestedPlaylistName = playlist.PlaylistName;
                var requestedPlaylistUserName = playlist.UserName;
                
                var playlistHeader = $"<h2>{requestedPlaylistName} playlist by {requestedPlaylistUserName}</h2><br /><br />";
                
                // get the channel id for username requested
                var channelId = await GetChannelId(requestedPlaylistUserName);
                if (string.IsNullOrEmpty(channelId)) {
                    _logger.Log(LambdaLogLevel.INFO, null, $"no channel id found for {requestedPlaylistUserName}");
                    return;
                }
                
                // get the current playlists for that channel id
                var currentPlaylists = await GetCurrentPlaylist(channelId);
                if (currentPlaylists == null) {
                    _logger.Log(LambdaLogLevel.INFO, null, $"no playlists for user {requestedPlaylistUserName}");
                    return;
                }
                
                // find the requested playlist and get the id from current playlists
                var playlistId = GetPlaylistId(currentPlaylists, requestedPlaylistName);
                if (playlistId == null) {
                    _logger.Log(LambdaLogLevel.INFO, null, $"no playlist found with the name {requestedPlaylistName}");
                    return;
                }
                     
                // use playlist id to get list of items in playlist
                var currentPlaylistItemList = await CurrentPlaylistItemsAsync(playlistId);
                
                // find the previously stored playlist
                var existingPlaylist = await GetExistingPlaylist(requestEmail, playlistId);
                if (existingPlaylist.Count <= 0) {
                    _logger.Log(LambdaLogLevel.INFO, null, "no existing playlist found in database");
                }
                
                // generate the comparison report
                finalEmail += playlistHeader + ComparisonReport(requestedPlaylistName, existingPlaylist, currentPlaylistItemList);
                
                // log data for database entry later
                databaseUpdates.Add(new PlaylistMonitorDatabaseEntry {
                    Email = requestEmail,
                    ChannelId = channelId,
                    PlaylistId = playlistId,
                    PlaylistItems = currentPlaylistItemList,
                    PlaylistName = requestedPlaylistName,
                    LastCheckedTimestamp = dateNowTimestamp
                });
            }
            
            // send email
            await _provider.SesSendEmail(_fromEmail, requestEmail, $"YouTube Playlist report for {dateNowString}", finalEmail);
            
            // update database
            var updateDatabase = new List<Task>();
            foreach (var databaseUpdate in databaseUpdates) {
                updateDatabase.Add(_provider.DynamoDbPutPlaylistListAsync(databaseUpdate.Email, databaseUpdate.ChannelId, databaseUpdate.PlaylistId, databaseUpdate.PlaylistName, databaseUpdate.PlaylistItems, databaseUpdate.LastCheckedTimestamp));
            }
            // databaseUpdates.ForEach(databaseUpdate => updateDatabase.Add(_provider.DynamoDbPutPlaylistListAsync(databaseUpdate.Email, databaseUpdate.ChannelId, databaseUpdate.PlaylistId, databaseUpdate.PlaylistName, databaseUpdate.PlaylistItems, databaseUpdate.LastCheckedTimestamp)));
            updateDatabase.ToArray();
        }

        private string ComparisonReport(string requestedPlaylistName, List<PlaylistMonitorPlaylistItem> existingPlaylist, List<PlaylistMonitorPlaylistItem> currentPlaylistItemList) {
            var deletedReport = DeletedReport(requestedPlaylistName, existingPlaylist, currentPlaylistItemList);
            var addedReport = AddedReport(requestedPlaylistName, existingPlaylist, currentPlaylistItemList);
            return deletedReport + addedReport;
        }

        private async Task<List<Playlist>> GetCurrentPlaylist(string channelId) {
            var playlists = new List<Playlist>();
            var allPlaylistItemsResponse = await _provider.YouTubeApiPlaylistsListAllAsync(channelId);
            foreach (var items in allPlaylistItemsResponse.Select(allPlaylistItem => allPlaylistItem.Items.ToList())) {
                playlists.AddRange(items);
            }
            return playlists;
        }

        private string AddedReport(string requestedPlaylistName, List<PlaylistMonitorPlaylistItem> existingPlaylistList, List<PlaylistMonitorPlaylistItem> currentPlaylistItemList) {
            var items = ComparePlaylistsForAdd(existingPlaylistList, currentPlaylistItemList);
            _logger.LogInfo("addedItems " + JsonConvert.SerializeObject(items));
            return items.Count <= 0 ? "No new videos added." : GenerateEmailBody($"{requestedPlaylistName} new videos", requestedPlaylistName, items);
        }

        private string DeletedReport(string requestedPlaylistName, List<PlaylistMonitorPlaylistItem> existingPlaylistList, List<PlaylistMonitorPlaylistItem> currentPlaylistItemList) {
            var items = ComparePlaylistsForDelete(existingPlaylistList, currentPlaylistItemList);
            _logger.LogInfo("deletedItems " + JsonConvert.SerializeObject(items));
            return items.Count <= 0 ? "No videos deleted." : GenerateEmailBody($"{requestedPlaylistName} missing videos", requestedPlaylistName, items);
        }

        public async Task<string> GetChannelId(string requestedPlaylistUserName) {
            var channelListResponse = await _provider.YouTubeApiChannelsListAsync(requestedPlaylistUserName);
            return channelListResponse.Items.Count <= 0 ? null : channelListResponse.Items.First().Id;
        }
        
        public string GetPlaylistId(List<Playlist> currentPlaylistsItems, string requestedPlaylistName) {
            return currentPlaylistsItems.Where(x => x.Snippet.Title == requestedPlaylistName).Select(x => x.Id).FirstOrDefault();
        }
        
        public async Task<List<PlaylistMonitorPlaylistItem>> CurrentPlaylistItemsAsync(string playlistId) {
            var playlistItems = new List<PlaylistItem>();
            var allPlaylistItemsResponse = await _provider.YouTubeApiPlaylistItemsAllAsync(playlistId);
            allPlaylistItemsResponse.ForEach(x => playlistItems.AddRange(x.Items));
            return await FormatCurrentPlaylistItems(playlistItems);
        }
        
        public async Task<List<PlaylistMonitorPlaylistItem>> FormatCurrentPlaylistItems(List<PlaylistItem> playlistItems) {
            var currentPlaylistItemList = new List<PlaylistMonitorPlaylistItem>();
            playlistItems.ForEach(async playlistItem => {
                var channelTitle = await GetChannelTitle(playlistItem.Snippet.ResourceId.VideoId);
                var playListItem = new PlaylistMonitorPlaylistItem {
                    Id = playlistItem.Id,
                    Title = playlistItem.Snippet.Title,
                    Link = $"https://www.youtube.com/watch?v={playlistItem.Snippet.ResourceId.VideoId}",
                    ChannelTitle = channelTitle,
                    Description = playlistItem.Snippet.Description,
                    Position = playlistItem.Snippet.Position
                };
                currentPlaylistItemList.Add(playListItem);
            });
            return currentPlaylistItemList;
        }
        
        public async Task<string> GetChannelTitle(string videoId) {
            var videoData = await _provider.YouTubeApiVideosAsync(videoId);
            var channelTitle = "N/A";
            if (videoData.Items.Count > 0) {
                channelTitle = videoData.Items.FirstOrDefault()?.Snippet.ChannelTitle;
            }
            return channelTitle;
        }

        public async Task<List<PlaylistMonitorPlaylistItem>> GetExistingPlaylist(string requestEmail, string playlistId) {
            var existingPlaylist = new List<PlaylistMonitorPlaylistItem>();
            var databaseResponse = await _provider.DynamoDbGetPlaylistListAsync(requestEmail, playlistId);
            if (databaseResponse.Item.TryGetValue("playlists", out var playlistItems)) {
                existingPlaylist.AddRange(playlistItems.L.Select(item => JsonConvert.DeserializeObject<PlaylistMonitorPlaylistItem>(item.S)));
            }
            return existingPlaylist;
        }
        
        public List<PlaylistMonitorPlaylistItem> ComparePlaylistsForDelete(List<PlaylistMonitorPlaylistItem> existingPlaylistList, List<PlaylistMonitorPlaylistItem> currentPlaylistItemList) {

            // find all items that are in the existing list that are not in the current (deleted items)
            var currentItemIds = currentPlaylistItemList.Select(x => x.Id).ToList();
            return existingPlaylistList.Where(item => !currentItemIds.Contains(item.Id)).ToList();
        }
        
        public List<PlaylistMonitorPlaylistItem> ComparePlaylistsForAdd(List<PlaylistMonitorPlaylistItem> existingPlaylistList, List<PlaylistMonitorPlaylistItem> currentPlaylistItemList) {
                
            // find all items that are in the current list that are not in the existing (added items)
            var existingItemIds = existingPlaylistList.Select(x => x.Id).ToList();
            return currentPlaylistItemList.Where(item => !existingItemIds.Contains(item.Id)).ToList();
        }

        public string GenerateEmailBody(string header, string playlistName, List<PlaylistMonitorPlaylistItem> deletedItems) {
            var output = $"<h3>{header}</h3><br /><br />";
            foreach (var item in deletedItems) {
                output += $"<strong><a href=\"{item.Link}\">{item.Title}</a></strong><br />";
                output += $"by {item.ChannelTitle}<br />";
                if (!string.IsNullOrEmpty(item.Description)) {
                    output += $"{item.Description}<br />";
                }
                output += "<br />";
            }
            return output;
        }
    }
}
