using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using LambdaSharp.Logger;
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
            var dateNowString = dateNow.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
            var finalEmail = $"<h1>Playlist Monitor Report for {dateNowString}</h1><br /><br />";
            var updateDatabase = new List<Task>();
            
            // check each playlist
            // requestedPlaylists.ForEach(async playlist => {
            foreach (var playlist in requestedPlaylists) {

                var playlistTitle = playlist.PlaylistName;
                var channelId = playlist.ChannelId;

                // get the channel snippet for channel id
                var channelSnippet = await _dataAccess.GetChannelDataAsync(channelId, playlistTitle);
                if (string.IsNullOrEmpty(channelId)) {
                    _logger.Log(LambdaLogLevel.INFO, null, $"no channel snippet found for {channelId}");
                    return;
                }
                var channelTitle = channelSnippet.Title;

                // get the current playlists for that channel id
                var playlistData = await _dataAccess.GetPlaylistDataAsync(channelId, playlistTitle);
                if (playlistData == null) {
                    _logger.Log(LambdaLogLevel.INFO, null, $"channel {channelTitle} doesn't have a playlist `{playlistTitle}`");
                    return;
                }
                
                // use playlist id to get list of items in playlist
                var currentPlaylistItems = await _dataAccess.GetRecentPlaylistItemDataAsync(playlistData.Id);

                // find the previously stored playlist
                var oldPlaylistItems = await _dataAccess.GetPlaylistItemDataFromCacheAsync(channelId, playlistTitle);
                if (oldPlaylistItems == null) {
                    _logger.Log(LambdaLogLevel.INFO, null, "no existing playlist found in database");
                }

                // generate the comparison report
                finalEmail += $"<h2>{playlistTitle} playlist by {channelTitle}</h2><br /><br />" + 
                              Comparison.Report(playlistTitle, oldPlaylistItems, currentPlaylistItems);

                // log data for database entry later
                // TODO: turn this into batch write requests
                updateDatabase.Add(_dataAccess.UpdatePlaylistItemDataFromCacheAsync(channelId, playlistTitle, currentPlaylistItems));
            }
            
            // update database
            Task.WaitAll(updateDatabase.ToArray());
            
            // send email
            await _dataAccess.SendEmailAsync(_fromEmail, requestEmail, $"YouTube Playlist report for {dateNowString}", finalEmail);
            
            // update subscription with last email sent content
            await _dataAccess.UpdateSubscriptionCacheAsync(requestEmail, finalEmail);
        }
    }
}
