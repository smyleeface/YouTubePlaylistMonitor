using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using LambdaSharp.Logger;
using Newtonsoft.Json;
using Smylee.PlaylistMonitor.PlaylistMonitor;
using Smylee.YouTube.PlaylistMonitor.Library;
using Smylee.YouTube.PlaylistMonitor.Library.Models;

namespace Smylee.YouTube.PlaylistCompare {

    public class Logic {

        private readonly ILambdaLogLevelLogger _logger;
        private readonly DataAccess _dataAccess;
        private string _fromEmail;

        //--- Methods ---
        public Logic(string fromEmail, DataAccess dataAccess, ILambdaLogLevelLogger logger) {
            _dataAccess = dataAccess;
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
            var updateDatabase = new List<Task>();
            
            // check each playlist
            // requestedPlaylists.ForEach(async playlist => {
            foreach (var playlist in requestedPlaylists) {
                
                var playlistTitle = playlist.PlaylistName;
                var channelId = playlist.ChannelId;
                
                // get the channel snippet for channel id
                var channelSnippet = await _dataAccess.GetChannelDataAsync(channelId);
                if (string.IsNullOrEmpty(channelId)) {
                    _logger.Log(LambdaLogLevel.INFO, null, $"no channel snippet found for {channelId}");
                    return;
                }

                var channelTitle = channelSnippet.Title;
                var playlistHeader = $"<h2>{playlistTitle} playlist by {channelTitle}</h2><br /><br />";

                // get the current playlists for that channel id
                var playlistData = await _dataAccess.GetPlaylistDataAsync(channelId, playlistTitle);
                if (playlistData == null) {
                    _logger.Log(LambdaLogLevel.INFO, null, $"channel {channelTitle} doesn't have a playlist `{playlistTitle}`");
                    return;
                }
                
                // use playlist id to get list of items in playlist
                var currentPlaylistItems = await _dataAccess.GetPlaylistItemDataFromYouTubeAsync(playlistData.Id);
                
                // find the previously stored playlist
                var oldPlaylistItems = await _dataAccess.GetPlaylistItemDataFromCacheAsync(channelId, playlistTitle);
                if (oldPlaylistItems == null) {
                    _logger.Log(LambdaLogLevel.INFO, null, "no existing playlist found in database");
                }
                
                // generate the comparison report
                finalEmail += playlistHeader + ComparisonReport(playlistTitle, oldPlaylistItems, currentPlaylistItems);
                
                // log data for database entry later
                // todo turn this into bulk
                updateDatabase.Add(_dataAccess.PutPlaylistItemDataFromCacheAsync(channelId, playlistTitle, playlistData, currentPlaylistItems));
            }
            
            // update database
            Task.WaitAll(updateDatabase.ToArray());
            
            // send email
            await _dataAccess.SendEmail(_fromEmail, requestEmail, $"YouTube Playlist report for {dateNowString}", finalEmail);
        }
        
        private string ComparisonReport(string playlistTitle, List<PlaylistItemsSnippetDb> cachedPlaylistItems, List<PlaylistItemsSnippetDb> currentPlaylistItems) {
            var deletedReport = DeletedReport(playlistTitle, cachedPlaylistItems, currentPlaylistItems);
            var addedReport = AddedReport(playlistTitle, cachedPlaylistItems, currentPlaylistItems);
            return deletedReport + " " + addedReport;
        }
        
        private string DeletedReport(string playlistTitle, List<PlaylistItemsSnippetDb> cachedPlaylistItems, List<PlaylistItemsSnippetDb> currentPlaylistItems) {
            if (cachedPlaylistItems == null) return "Deleted video status will be in next report.";
            var items = ComparePlaylistsForDelete(cachedPlaylistItems, currentPlaylistItems);
            _logger.LogInfo("deletedItems " + JsonConvert.SerializeObject(items));
            return items.Count <= 0 ? "No videos deleted." : GenerateEmailBody($"{playlistTitle} missing videos", playlistTitle, items);
        }
        
        public List<PlaylistItemsSnippetDb> ComparePlaylistsForDelete(List<PlaylistItemsSnippetDb> existingPlaylistList, List<PlaylistItemsSnippetDb> currentPlaylistItems) {
        
            // find all items that are in the existing list that are not in the current (deleted items)
            var currentItemIds = currentPlaylistItems.Select(x => x.Id).ToList();
            return existingPlaylistList.Where(item => !currentItemIds.Contains(item.Id)).ToList();
        } 
        
        private string AddedReport(string playlistTitle, List<PlaylistItemsSnippetDb> existingPlaylistList, List<PlaylistItemsSnippetDb> currentPlaylistItemList) {
            if (existingPlaylistList == null) return "Added video status will be in next report.";
            var items = ComparePlaylistsForAdd(existingPlaylistList, currentPlaylistItemList);
            _logger.LogInfo("addedItems " + JsonConvert.SerializeObject(items));
            return items.Count <= 0 ? "No new videos added." : GenerateEmailBody($"{playlistTitle} new videos", playlistTitle, items);
        }

        public List<PlaylistItemsSnippetDb> ComparePlaylistsForAdd(List<PlaylistItemsSnippetDb> existingPlaylistList, List<PlaylistItemsSnippetDb> currentPlaylistItemList) {

            // find all items that are in the current list that are not in the existing (added items)
            var existingItemIds = existingPlaylistList.Select(x => x.Id).ToList();
            return currentPlaylistItemList.Where(item => !existingItemIds.Contains(item.Id)).ToList();
        }

        public string GenerateEmailBody(string header, string playlisTitle, List<PlaylistItemsSnippetDb> deletedItems) {
            var output = $"<h3>{header}</h3><br /><br />";
            foreach (var item in deletedItems) {
                output += $"<strong><a href=\"https://www.youtube.com/watch?v={item.Id}\">{item.Title}</a></strong><br />";
                // output += $"by {item.ChannelTitle}<br />"; //TODO get video author
                if (!string.IsNullOrEmpty(item.Description)) {
                    output += $"{item.Description}<br />";
                }
                output += "<br />";
            }
            return output;
        }
    }
}
