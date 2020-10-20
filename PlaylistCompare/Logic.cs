using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using LambdaSharp.Logger;
using Smylee.PlaylistMonitor.Library;
using Smylee.PlaylistMonitor.Library.Models;

namespace Smylee.PlaylistMonitor.PlaylistCompare {

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
            var dateNowString = dateNow.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
            var finalEmail = new GenerateEmail(dateNowString,$"Playlist Monitor Report for {dateNowString}");
            var updateDatabase = new List<Task>();
            var changesFromPrevious = new List<bool>();
            
            // check each playlist
            // requestedPlaylists.ForEach(async playlist => {
            foreach (var playlist in requestedPlaylists) {

                var playlistTitle = playlist.PlaylistName;
                var channelId = playlist.ChannelId;
                
                // get cached data or lookup data from youtube
                var (channelSnippet, playlistData, oldPlaylistItems)  = await _dataAccess.GetSubscriptionData(channelId, playlistTitle);
                if (string.IsNullOrEmpty(channelSnippet.Title)) {
                    _logger.Log(LambdaLogLevel.INFO, null, $"no channel snippet found for {channelId}");
                    return;
                }
                if (playlistData == null) {
                    _logger.Log(LambdaLogLevel.INFO, null, $"channel {channelSnippet.Title} doesn't have a playlist `{playlistTitle}`");
                    return;
                }
                if (oldPlaylistItems == null) {
                    _logger.Log(LambdaLogLevel.INFO, null, "no existing playlist found in database");
                }
                
                // get existing data from youtube
                var currentPlaylistItems = await _dataAccess.GetRecentPlaylistItemDataAsync(playlistData.Id);
                
                // filter the lists
                var deletedItems = Comparison.DeletedItems(oldPlaylistItems, currentPlaylistItems);
                var addedItems = Comparison.AddedItems(oldPlaylistItems, currentPlaylistItems); 
                
                // generate the comparison report
                finalEmail.AddCard(playlistTitle, channelSnippet.Title, deletedItems, addedItems);

                // if there were any changes, or this is the first run, an email should be sent
                changesFromPrevious.Add(oldPlaylistItems == null || !oldPlaylistItems.IsSame(currentPlaylistItems));
                
                // log data for database entry later
                // TODO: turn this into batch write requests
                updateDatabase.Add(_dataAccess.UpdatePlaylistItemDataFromCacheAsync(channelId, playlistTitle, currentPlaylistItems));
            }
            
            // update database
            Task.WaitAll(updateDatabase.ToArray());

            if (changesFromPrevious.Contains(true)) {
                
                // send email
                await _dataAccess.SendEmailAsync(_fromEmail, requestEmail, $"YouTube Playlist report for {dateNowString}", finalEmail.Html);
            }
            
            // update subscription with last email sent content
            await _dataAccess.UpdateSubscriptionCacheAsync(requestEmail, finalEmail.Html);
        }
    }
}
