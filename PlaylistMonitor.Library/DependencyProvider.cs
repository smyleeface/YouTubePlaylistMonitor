using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json;
using Smylee.YouTube.PlaylistMonitor.Library.Models;

namespace Smylee.YouTube.PlaylistMonitor.Library {

    public interface IDependencyProvider {
        Task<GetItemResponse> DynamoDbGetCacheChannelAsync(string channelId);
        Task DynamoDbPutPCacheChannelAsync(string channelId, ChannelSnippet channelSnippet);
        Task<ChannelSnippet> YouTubeApiChannelSnippetAsync(string channelId);
        Task<GetItemResponse> DynamoDbGetCachePlaylistsAsync(string channelId, string playlistTitle);
        Task<PlaylistListResponse> YouTubeApiPlaylistsAsync(string channelId, string nextPageToken = null);
        Task DynamoDbUpdateCachePlaylistAsync(string channelId, PlaylistSnippetDb playlistSnippet);
        Task<PlaylistItemListResponse> YouTubeApiPlaylistItemsAsync(string playlistId, string nextPageToken = null);
        Task<GetItemResponse> DynamoDbGetCachePlaylistsItemsAsync(string channelId, string playlistTitle);
        Task SesSendEmail(string fromEmail, string requestEmail, string emailSubject,string emailBody);
        Task DynamoDbUpdateCachePlaylistItemsAsync(string channelId, string playlistTitle, List<PlaylistItemsSnippetDb> playlistItems);
        Task DynamoDbUpdateSubscriptionCacheAsync(string requestEmail, string finalEmail);
    }

    public class DependencyProvider : IDependencyProvider {
        private readonly string _dynamoDbPlaylistTableName;
        private readonly YouTubeService _youtubeApiClient;
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IAmazonSimpleEmailServiceV2 _sesClient;
        private string _dynamoDbSubscriptionTableName;

        public DependencyProvider(YouTubeService youtubeApiClient, string dynamoDbPlaylistTableName, string dynamoDbSubscriptionTableName, IAmazonDynamoDB dynamoDbClient, IAmazonSimpleEmailServiceV2 sesClient) {
            _dynamoDbPlaylistTableName = dynamoDbPlaylistTableName;
            _dynamoDbSubscriptionTableName = dynamoDbSubscriptionTableName;
            _youtubeApiClient = youtubeApiClient;
            _dynamoDbClient = dynamoDbClient;
            _sesClient = sesClient;
        }
        
#region ChannelSnippet
        
        public async Task<GetItemResponse> DynamoDbGetCacheChannelAsync(string channelId) {
            var getRequest = new GetItemRequest {
                TableName = _dynamoDbPlaylistTableName, //TODO variable
                Key = new Dictionary<string, AttributeValue> {
                    { "channelId", new AttributeValue { S = channelId } }
                }
            };
            return await _dynamoDbClient.GetItemAsync(getRequest);
        }

        public async Task DynamoDbPutPCacheChannelAsync(string channelId, ChannelSnippet channelSnippet) {
            var dateNow = DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString(CultureInfo.InvariantCulture);
            var putRequest = new PutItemRequest {
                TableName = _dynamoDbPlaylistTableName,
                Item = new Dictionary<string, AttributeValue> {
                    {"channelId", new AttributeValue {
                        S = channelId
                    }},
                    {"channelSnippet", new AttributeValue {
                        S = JsonConvert.SerializeObject(channelSnippet.ToChannelSnippetDb())
                    }},
                    {"timestamp", new AttributeValue {
                        N = dateNow
                    }}
                }
            };
            await _dynamoDbClient.PutItemAsync(putRequest);
        }
        
        public async Task<ChannelSnippet> YouTubeApiChannelSnippetAsync(string channelId) {
            var channelsListRequest = _youtubeApiClient.Channels.List("snippet");
            channelsListRequest.Id = channelId;
            channelsListRequest.MaxResults = 50;
            var response = await channelsListRequest.ExecuteAsync();
            Thread.Sleep(1000);
            return response.Items.Count > 0 ? response.Items.First().Snippet : null;
        }

#endregion

#region PlaylistSnippet
        
        public async Task<GetItemResponse> DynamoDbGetCachePlaylistsAsync(string channelId, string playlistTitle) {
            var getRequest = new GetItemRequest {
                TableName = _dynamoDbPlaylistTableName, //TODO variable
                Key = new Dictionary<string, AttributeValue> {
                    { "channelId", new AttributeValue { S = channelId } },
                    { "playlistTitle", new AttributeValue { S = playlistTitle } }
                }
            };
            return await _dynamoDbClient.GetItemAsync(getRequest);
        }
        
        
        public async Task DynamoDbUpdateCachePlaylistAsync(string channelId, PlaylistSnippetDb playlistSnippet) {
            var dateNow = DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString(CultureInfo.InvariantCulture);
            var putRequest = new UpdateItemRequest {
                TableName = _dynamoDbPlaylistTableName,
                Key = new Dictionary<string, AttributeValue> {
                    {"channelId", new AttributeValue {
                        S = channelId
                    }},
                    {"playlistTitle", new AttributeValue {
                        S = playlistSnippet.Title
                    }},
                    {"playlistSnippet", new AttributeValue {
                        S = JsonConvert.SerializeObject(playlistSnippet)
                    }},
                    {"timestamp", new AttributeValue {
                        N = dateNow
                    }}
                }
            };
            await _dynamoDbClient.UpdateItemAsync(putRequest);
        }

        public async Task<PlaylistListResponse> YouTubeApiPlaylistsAsync(string channelId, string nextPageToken = null) {
            var playlistListRequest = _youtubeApiClient.Playlists.List("snippet");
            playlistListRequest.ChannelId = channelId;
            playlistListRequest.PageToken = nextPageToken;
            playlistListRequest.MaxResults = 50;
            var response = await playlistListRequest.ExecuteAsync();
            Thread.Sleep(1000);
            return response;
        }

#endregion
        
#region PlaylistItemsSnippet
        
        public async Task<GetItemResponse> DynamoDbGetCachePlaylistsItemsAsync(string channelId, string playlistTitle) {
            var getRequest = new GetItemRequest {
                TableName = _dynamoDbPlaylistTableName, //TODO variable
                Key = new Dictionary<string, AttributeValue> {
                    { "channelId", new AttributeValue { S = channelId } },
                    { "playlistTitle", new AttributeValue { S = playlistTitle } }
                }
            };
            return await _dynamoDbClient.GetItemAsync(getRequest);
        }
        
        public async Task DynamoDbUpdateCachePlaylistItemsAsync(string channelId, string playlistTitle, List<PlaylistItemsSnippetDb> playlistItems) {
            var dateNow = DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString(CultureInfo.InvariantCulture);
            var putRequest = new UpdateItemRequest {
                TableName = _dynamoDbPlaylistTableName,
                Key = new Dictionary<string, AttributeValue> {
                    {"channelId", new AttributeValue {
                        S = channelId
                    }},
                    {"playlistTitle", new AttributeValue {
                        S = playlistTitle
                    }},
                    {"playlistItemsSnippet", new AttributeValue {
                        L = playlistItems.Select(x => new AttributeValue {
                                S = JsonConvert.SerializeObject(x)
                            }).ToList()
                    }},
                    {"timestamp", new AttributeValue {
                        N = dateNow
                    }}
                }
            };
            await _dynamoDbClient.UpdateItemAsync(putRequest);
        }

        public async Task<PlaylistItemListResponse> YouTubeApiPlaylistItemsAsync(string playlistId, string nextPageToken = null) {
             var playlistListItemsRequest = _youtubeApiClient.PlaylistItems.List("snippet");
             playlistListItemsRequest.PlaylistId = playlistId;
             playlistListItemsRequest.PageToken = nextPageToken;
             playlistListItemsRequest.MaxResults = 50;
             var response = await playlistListItemsRequest.ExecuteAsync();
             Thread.Sleep(1000);
             return response;
         }

#endregion
        
#region YouTubeApiVideos
        
        public async Task<VideoListResponse> YouTubeApiVideosAsync(string videoId) {
            var playlistListRequest = _youtubeApiClient.Videos.List("snippet");
            playlistListRequest.Id = videoId;
            playlistListRequest.MaxResults = 50;
            var response = await playlistListRequest.ExecuteAsync();
            Thread.Sleep(1000);
            return response;
        }      
        
#endregion

        public async Task DynamoDbUpdateSubscriptionCacheAsync(string requestEmail, string finalEmail) {
            var dateNow = DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString(CultureInfo.InvariantCulture);
            var putRequest = new UpdateItemRequest {
                TableName = _dynamoDbSubscriptionTableName,
                Key = new Dictionary<string, AttributeValue> {
                    {"email", new AttributeValue {
                        S = requestEmail
                    }},
                    {"finalEmail", new AttributeValue {
                        S = finalEmail
                    }}
                }
            };
            await _dynamoDbClient.UpdateItemAsync(putRequest);        
        }
        
         public async Task SesSendEmail(string fromEmail, string requestEmail, string emailSubject,string emailBody) {
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
             await _sesClient.SendEmailAsync(sendEmailRequest);
         }
    }
}
