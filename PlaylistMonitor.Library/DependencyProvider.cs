using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        Task<GetItemResponse> DynamoDbGetCacheChannelAsync(string channelId, string playlistTitle);
        Task DynamoDbPutCacheChannelAsync(string channelId, string playlistTitle, ChannelSnippet channelSnippet);
        Task<ChannelSnippet> YouTubeApiChannelSnippetAsync(string channelId);
        Task<GetItemResponse> DynamoDbGetCachePlaylistsAsync(string channelId, string playlistTitle);
        Task<PlaylistListResponse> YouTubeApiPlaylistsAsync(string channelId, string nextPageToken = null);
        Task DynamoDbUpdateCachePlaylistAsync(string channelId, PlaylistSnippetDb playlistSnippet);
        Task<PlaylistItemListResponse> YouTubeApiPlaylistItemsAsync(string playlistId, string nextPageToken = null);
        Task<GetItemResponse> DynamoDbGetCachePlaylistsItemsAsync(string channelId, string playlistTitle);
        Task SesSendEmail(string fromEmail, string requestEmail, string emailSubject,string emailBody);
        Task DynamoDbUpdateCachePlaylistItemsAsync(string channelId, string playlistTitle, List<PlaylistItemsSnippetDb> playlistItems);
        Task DynamoDbUpdateSubscriptionCacheAsync(string requestEmail, string finalEmail);
        Task<VideoListResponse> YouTubeApiVideosAsync(string videoId);
        Task<List<BatchGetItemResponse>> DynamoDbGetCacheVideoDataAsync(List<string> videoIds);
        Task DynamoDbUpdateCacheVideoDataAsync(List<WriteRequest> batchWriteRequest);
    }

    public class DependencyProvider : IDependencyProvider {
        private readonly string _dynamoDbPlaylistTableName;
        private readonly YouTubeService _youtubeApiClient;
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IAmazonSimpleEmailServiceV2 _sesClient;
        private readonly string _dynamoDbSubscriptionTableName;
        private readonly string _dynamoDbVideoTableName;

        public DependencyProvider(YouTubeService youtubeApiClient, string dynamoDbPlaylistTableName, string dynamoDbSubscriptionTableName, string dynamoDbVideoTableName, IAmazonDynamoDB dynamoDbClient, IAmazonSimpleEmailServiceV2 sesClient) {
            _dynamoDbVideoTableName = dynamoDbVideoTableName;
            _dynamoDbPlaylistTableName = dynamoDbPlaylistTableName;
            _dynamoDbSubscriptionTableName = dynamoDbSubscriptionTableName;
            _youtubeApiClient = youtubeApiClient;
            _dynamoDbClient = dynamoDbClient;
            _sesClient = sesClient;
        }
        
#region ChannelSnippet
        
        public async Task<GetItemResponse> DynamoDbGetCacheChannelAsync(string channelId, string playlistTitle) {
            var getRequest = new GetItemRequest {
                TableName = _dynamoDbPlaylistTableName,
                Key = new Dictionary<string, AttributeValue> {
                    { "channelId", new AttributeValue { S = channelId } },
                    { "playlistTitle", new AttributeValue { S = playlistTitle } }
                }
            };
            return await _dynamoDbClient.GetItemAsync(getRequest);
        }

        public async Task DynamoDbPutCacheChannelAsync(string channelId, string playlistTitle, ChannelSnippet channelSnippet) {
            var dateNow = DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString(CultureInfo.InvariantCulture);
            var putRequest = new PutItemRequest {
                TableName = _dynamoDbPlaylistTableName,
                Item = new Dictionary<string, AttributeValue> {
                    {"channelId", new AttributeValue {
                        S = channelId
                    }},
                    {"playlistTitle", new AttributeValue {
                        S = playlistTitle
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
            await Task.Delay(500);
            return response.Items.Count > 0 ? response.Items.First().Snippet : null;
        }

#endregion

#region PlaylistSnippet
        
        public async Task<GetItemResponse> DynamoDbGetCachePlaylistsAsync(string channelId, string playlistTitle) {
            var getRequest = new GetItemRequest {
                TableName = _dynamoDbPlaylistTableName,
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
                    }}
                },
                AttributeUpdates = new Dictionary<string, AttributeValueUpdate>{
                    {"playlistSnippet", new AttributeValueUpdate {
                        Action = AttributeAction.PUT,
                        Value = new AttributeValue {
                            S = JsonConvert.SerializeObject(playlistSnippet)
                        }
                    }},
                    {"timestamp", new AttributeValueUpdate {
                        Action = AttributeAction.PUT,
                        Value = new AttributeValue {
                            N = dateNow 
                        }
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
            await Task.Delay(500);
            return response;
        }

#endregion
        
#region PlaylistItemsSnippet
        
        public async Task<GetItemResponse> DynamoDbGetCachePlaylistsItemsAsync(string channelId, string playlistTitle) {
            var getRequest = new GetItemRequest {
                TableName = _dynamoDbPlaylistTableName,
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
                    }}
                },
                AttributeUpdates = new Dictionary<string, AttributeValueUpdate> {
                    {"playlistItemsSnippet", new AttributeValueUpdate {
                        Action = AttributeAction.PUT,
                        Value = new AttributeValue {
                            L = playlistItems.Select(x => new AttributeValue {
                                S = JsonConvert.SerializeObject(x)
                            }).ToList()
                        }
                    }},
                    {"timestamp", new AttributeValueUpdate {
                        Action = AttributeAction.PUT,
                        Value = new AttributeValue {
                            N = dateNow
                        }
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
             await Task.Delay(500);
             return response;
         }

#endregion

#region VideosSnippet

        public async Task<List<BatchGetItemResponse>> DynamoDbGetCacheVideoDataAsync(List<string> videoIds) {
            var listOfResponses = new List<BatchGetItemResponse>();
            var batchVideoRequests = new List<List<Dictionary<string, AttributeValue>>>();
            var batchVideoIds = videoIds.Select(x => new Dictionary<string, AttributeValue> {
                {"videoId", new AttributeValue {
                    S = x
                }}
            }).ToList();
            for (int i = 0; i < batchVideoIds.Count; i += 25) { 
                batchVideoRequests.Add(batchVideoIds.GetRange(i, Math.Min(25, batchVideoIds.Count - i))); 
            }
            foreach (var batchVideo in batchVideoRequests) {
                var getBatchRequest = new BatchGetItemRequest {
                    RequestItems = new Dictionary<string, KeysAndAttributes> {
                        {_dynamoDbVideoTableName, new KeysAndAttributes {
                            Keys = batchVideo
                        }}
                    }
                };
                listOfResponses.Add(await _dynamoDbClient.BatchGetItemAsync(getBatchRequest));
            }
            return listOfResponses;
        }
        
        
        public async Task DynamoDbUpdateCacheVideoDataAsync(List<WriteRequest> writeRequests) {
            var batchPutRequest = new BatchWriteItemRequest {
                RequestItems = new Dictionary<string, List<WriteRequest>> {
                    {_dynamoDbVideoTableName, writeRequests}
                }
            };
            await _dynamoDbClient.BatchWriteItemAsync(batchPutRequest);
        }
        
        public async Task<VideoListResponse> YouTubeApiVideosAsync(string videoId) {
            var playlistListRequest = _youtubeApiClient.Videos.List("snippet");
            playlistListRequest.Id = videoId;
            playlistListRequest.MaxResults = 1;
            var response = await playlistListRequest.ExecuteAsync();
            await Task.Delay(500);
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
                    }}
                },
                AttributeUpdates = new Dictionary<string, AttributeValueUpdate> {
                    {"finalEmail", new AttributeValueUpdate {
                            Action = AttributeAction.PUT,
                            Value = new AttributeValue {
                                S = finalEmail
                            }
                        }
                    },
                    {"timestamp", new AttributeValueUpdate {
                            Action = AttributeAction.PUT,
                            Value = new AttributeValue {
                                N = dateNow
                            }
                        }
                    }
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
