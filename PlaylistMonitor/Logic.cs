using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SimpleEmailV2.Model;
using Google.Apis.YouTube.v3.Data;
using LambdaSharp.Logger;
using Newtonsoft.Json;

namespace Smylee.PlaylistMonitor.PlaylistMonitor {

    public class Logic {

        private readonly ILambdaLogLevelLogger _logger;
        private readonly IDepenedencyProvder _provider;

        //--- Methods ---
        public Logic(IDepenedencyProvder provider, ILambdaLogLevelLogger logger) {
            _provider = provider;
            _logger = logger;
        }

        public async Task<string> Run() {
            
            // TODO: turn into a domain email
            var fromEmail = "patty.ramert@gmail.com";
            
            // get subscriptions
            var userSubscriptions = await GetSubscriptions();
            foreach (var userSubscription in userSubscriptions) {
                var requestEmail = userSubscription.Key;
                var subscriptions = userSubscription.Value;
                foreach (var subscription in subscriptions) {
                    var requestedPlaylistUserName = subscription.UserName;
                    var requestedPlaylistName = subscription.PlaylistName;
                    _logger.LogInfo($"Processing subscription for {requestEmail} on channel {requestedPlaylistUserName} playlist {requestedPlaylistName}");
                    
                    // init
                    var addedItems = new List<PlaylistItem>();
                    var deletedItems = new List<PlaylistItem>();

                    // get the channel id for username requested
                    var channelId = await GetChannelId(requestedPlaylistUserName);

                    // get the current playlists for that channel id
                    var currentPlaylists = await _provider.YouTubeApiPlaylistsList(channelId);
                    if (currentPlaylists.Items.Count <= 0) return "no playlists";

                    // find the requested playlist and get the id from current playlists
                    var playlistId = GetPlaylistId(currentPlaylists.Items, requestedPlaylistName);
                    if (playlistId == null) return "no playlist";
                    
                    // use playlist id to get list of items in playlist
                    var currentPlaylistItemList = await CreatePlaylistItemsFromPlaylistIdAsync(playlistId);
                    
                    // find the previously stored playlist
                    var existingPlaylist = await GetExistingPlaylist(requestEmail, playlistId);
                    
                    // if there's an existing record in the database, compare with the current from youtube, and report
                    if (existingPlaylist.Count > 0) {
                        deletedItems.AddRange(ComparePlaylistsForDelete(existingPlaylist, currentPlaylistItemList));
                        addedItems.AddRange(ComparePlaylistsForAdd(existingPlaylist, currentPlaylistItemList));

                        if (deletedItems.Count > 0) {
                            var emailContent = GenerateReportDeleted(requestedPlaylistName, deletedItems);
                            await SendEmail(emailContent.EmailSubject, emailContent.EmailBody, requestEmail, fromEmail);
                        }

                        if (addedItems.Count > 0) {
                            GenerateReportAdded(addedItems);
                        }
                    }
                    await _provider.DynamoDbPutPlaylistList(requestEmail, channelId, playlistId, requestedPlaylistName, currentPlaylistItemList, deletedItems);   
                }
            }
            return "done";
        }

        public async Task<Dictionary<string, List<Subscription>>> GetSubscriptions() {
            var subscriptions = new Dictionary<string, List<Subscription>>();
            var subscriptionsDbList = await _provider.DynamoDbGetSubscriptionList();
            foreach (var item in subscriptionsDbList.Items) {
                var email = item.GetValueOrDefault("email").S;
                var playlistSubs = subscriptionsDbList.Items.Select(x => x.GetValueOrDefault("playlists").L);
                var subs = new List<Subscription>();
                foreach (var playlistData in playlistSubs) {
                    subs.AddRange(playlistData.Select(x => JsonConvert.DeserializeObject<Subscription>(x.S)));
                }
                subscriptions.Add(email, subs);
            }
            return subscriptions;
        }

        public async Task<List<PlaylistItem>> GetExistingPlaylist(string requestEmail, string playlistId) {
            var existingPlaylistList = new List<PlaylistItem>();
            var existingPlaylist = await _provider.DynamoDbGetPlaylistList(requestEmail, playlistId);

            // if there's an existing record in the database, compare with the current from youtube, and report
            if (existingPlaylist.Item.TryGetValue("playlists", out var existingPlaylistListOfStrings)) {
                existingPlaylistList.AddRange(existingPlaylistListOfStrings.L.Select(item => JsonConvert.DeserializeObject<PlaylistItem>(item.S)));
            }
            return existingPlaylistList;
        }

        public void GenerateReportAdded(List<PlaylistItem> addedItems) {
            _logger.LogInfo("addedItems");
            _logger.LogInfo(JsonConvert.SerializeObject(addedItems));
        }

        public (string EmailSubject, string EmailBody) GenerateReportDeleted(string requestedPlaylistName, List<PlaylistItem> deletedItems) {
            var emailSubject = $"YouTube Playlist {requestedPlaylistName} has missing video";
            var emailBody = GenerateEmailBody(requestedPlaylistName, deletedItems);
            return (emailSubject, emailBody);
        }

        public List<PlaylistItem> ComparePlaylistsForDelete(List<PlaylistItem> existingPlaylistList, List<PlaylistItem> currentPlaylistItemList) {

            // find all items that are in the existing list that are not in the current (deleted items)
            var found = new List<PlaylistItem>();
            foreach (var existingPlaylistListItem in existingPlaylistList) {
                var foundInExisting = currentPlaylistItemList.Any(item => item.Id == existingPlaylistListItem.Id);
                if (!foundInExisting) {
                    found.Add(existingPlaylistListItem);
                }
            }
            return found;
        }

        public List<PlaylistItem> ComparePlaylistsForAdd(List<PlaylistItem> existingPlaylistList, List<PlaylistItem> currentPlaylistItemList) {
                
            // find all items that are in the current list that are not in the existing (added items)
            var found = new List<PlaylistItem>();
            foreach (var currentPlaylistItemListItem in currentPlaylistItemList) {
                var foundInCurrent = existingPlaylistList.Any(item => item.Id == currentPlaylistItemListItem.Id);
                if (!foundInCurrent) {
                    found.Add(currentPlaylistItemListItem);
                }
            }
            return found;
        }

        public async Task<List<PlaylistItem>> CreatePlaylistItemsFromPlaylistIdAsync(string playlistId) {
            string nextPageToken = null;
            var currentPlaylistItemList = new List<PlaylistItem>();
            do {

                var playlistItemsResponse = await _provider.YouTubeApiPlaylistItemsList(playlistId, nextPageToken);
                _logger.LogInfo(JsonConvert.SerializeObject(playlistItemsResponse));
                
                // create playlist items 
                foreach (var item in playlistItemsResponse.Items) {
                    var channelTitle = await GetChannelTitle(item.Id, item.Snippet);
                    var playListItem = GenerateMonitorPlaylistItem(channelTitle, item.Id, item.Snippet.ResourceId.VideoId, item.Snippet.Title, item.Snippet.Description, item.Snippet.Position);
                    currentPlaylistItemList.Add(playListItem);
                }
                nextPageToken = playlistItemsResponse.NextPageToken;
            } while (nextPageToken != null);
            return currentPlaylistItemList;
        }

        public async Task<string> GetChannelTitle(string videoId, PlaylistItemSnippet snippet) {
            var videoData = await _provider.YouTubeApiVideos(videoId);
            var channelTitle = "N/A";
            if (videoData.Items.Count > 0) {
                channelTitle = snippet.ChannelTitle;
            }
            return channelTitle;
        }

        public PlaylistItem GenerateMonitorPlaylistItem(string channelTitle, string itemId, string videoId, string videoTitle, string description, long? position) {
            var playlistItem = new PlaylistItem {
                Id = itemId,
                Title = videoTitle,
                Link = $"https://www.youtube.com/watch?v={videoId}",
                ChannelTitle = channelTitle,
                Position = position
            };
            if (!string.IsNullOrEmpty(description)) {
                playlistItem.Description = description;
            }
            return playlistItem;
        }

        public string GetPlaylistId(IEnumerable<Playlist> currentPlaylistsItems, string requestedPlaylistName) {
            // string playlistId2 = currentPlaylistsItems.Where(x => x.Snippet.Title == requestedPlaylistName).Select(x => x.Id).Single();
            // return playlistId2;
            string playlistId = null;
            foreach (var playlistItems in currentPlaylistsItems) {
                if (playlistItems.Snippet.Title != requestedPlaylistName) continue;
                playlistId = playlistItems.Id;
            }
            return playlistId;
        }

        public async Task SendEmail(string emailSubject, string emailBody, string requestEmail, string fromEmail) {
            var sendEmailRequest = new SendEmailRequest {
                Content = new EmailContent {
                    Simple = new Message {
                        Subject = new Content {
                            Data = emailSubject
                        },
                        Body = new Body {
                            Html = new Content {
                                Data = emailBody
                            }
                        }
                    }
                },
                Destination = new Destination {
                    ToAddresses = new List<string> {
                        requestEmail
                    }
                },
                FromEmailAddress = fromEmail
            };
            await _provider.SesSendEmail(sendEmailRequest);
        }

        public string GenerateEmailBody(string playlistName, List<PlaylistItem> deletedItems) {
            var output = $"<h3>{playlistName}</h3><br /><br />";
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

        public async Task<string> GetChannelId(string requestedPlaylistUserName) {
            var channelListResponse = await _provider.YouTubeApiChannelsList(requestedPlaylistUserName);
            return channelListResponse.Items.Count <= 0 ? null : channelListResponse.Items.First().Id;
        }
    }
}
