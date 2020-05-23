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
            var finalEmail = $"<h1>Playlist Monitor Report for {dateNowString}</h1><br /><br />";
            var updateDatabase = new List<Task>();
            var changesFromPrevious = false;
            
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
                
                // generate the comparison report
                finalEmail += $"<h2>{playlistTitle} playlist by {channelSnippet.Title}</h2><br /><br />" + 
                              Comparison.Report(playlistTitle, oldPlaylistItems, currentPlaylistItems);

                // if there were any changes, or this is the first run, an email should be sent
                if(oldPlaylistItems == null || !oldPlaylistItems.Equals(currentPlaylistItems)) {
                    changesFromPrevious = true;
                }
                
                // log data for database entry later
                // TODO: turn this into batch write requests
                updateDatabase.Add(_dataAccess.UpdatePlaylistItemDataFromCacheAsync(channelId, playlistTitle, currentPlaylistItems));
            }
            
            // update database
            Task.WaitAll(updateDatabase.ToArray());

            if (changesFromPrevious) {
                
                // send email
                await _dataAccess.SendEmailAsync(_fromEmail, requestEmail, $"YouTube Playlist report for {dateNowString}", finalEmail);
            }
            
            // update subscription with last email sent content
            await _dataAccess.UpdateSubscriptionCacheAsync(requestEmail, finalEmail);
        }
    }
}
