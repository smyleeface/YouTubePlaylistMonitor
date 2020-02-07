using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json;
using Smylee.PlaylistMonitor.PlaylistMonitor;

namespace Smylee.YouTube.PlaylistCompare {

    public interface IDependencyProvider {
        Task<List<PlaylistItemListResponse>> YouTubeApiPlaylistItemsAllAsync(string playlistId);
        Task<ChannelListResponse> YouTubeApiChannelsListAsync(string requestedPlaylistUserName);
        Task<List<PlaylistListResponse>> YouTubeApiPlaylistsListAllAsync(string channelId);
        Task<VideoListResponse> YouTubeApiVideosAsync(string videoId);
        Task<GetItemResponse> DynamoDbGetPlaylistListAsync(string requestEmail, string playlistId);
        Task DynamoDbPutPlaylistListAsync(string email, string channelId, string playlistId, string playlistName, List<PlaylistMonitorPlaylistItem> playlistItems, string dateNowTimestamp);
        Task SesSendEmail(string fromEmail, string requestEmail, string emailSubject,string emailBody);
    }

    public class DependencyProvider : IDependencyProvider {
        private readonly string _dynamoDbTableName;
        private readonly YouTubeService _youtubeApiClient;
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IAmazonSimpleEmailServiceV2 _sesClient;

        public DependencyProvider(YouTubeService youtubeApiClient, string dynamoDbTableName, IAmazonDynamoDB dynamoDbClient, IAmazonSimpleEmailServiceV2 sesClient) {
            _dynamoDbTableName = dynamoDbTableName;
            _youtubeApiClient = youtubeApiClient;
            _dynamoDbClient = dynamoDbClient;
            _sesClient = sesClient;
        }
        
        public async Task<List<PlaylistItemListResponse>> YouTubeApiPlaylistItemsAllAsync(string playlistId) {
            string nextPageToken = null;
            var playlistListItemsRequest = new List<PlaylistItemListResponse>();
            do {
                var playlistItemsResponse = await YouTubeApiPlaylistItemsAsync(playlistId, nextPageToken);
                playlistListItemsRequest.Add(playlistItemsResponse);
                nextPageToken = playlistItemsResponse.NextPageToken;
            } while (nextPageToken != null);
            return playlistListItemsRequest;
        }
        
        public async Task<PlaylistItemListResponse> YouTubeApiPlaylistItemsAsync(string playlistId, string nextPageToken = null) {
            var playlistListItemsRequest = _youtubeApiClient.PlaylistItems.List("snippet");
            playlistListItemsRequest.PlaylistId = playlistId;
            if (nextPageToken != null) {
                playlistListItemsRequest.PageToken = nextPageToken;
            }
            var response = await playlistListItemsRequest.ExecuteAsync();
            Thread.Sleep(1000);
            return response;
        }

        public async Task<ChannelListResponse> YouTubeApiChannelsListAsync(string requestedPlaylistUserName) {
            var channelsListRequest = _youtubeApiClient.Channels.List("id");
            channelsListRequest.ForUsername = requestedPlaylistUserName;
            var response = await channelsListRequest.ExecuteAsync();
            Thread.Sleep(1000);
            return response;
        }

        public async Task<List<PlaylistListResponse>> YouTubeApiPlaylistsListAllAsync(string channelId) {
            string nextPageToken = null;
            var playlistListResponse = new List<PlaylistListResponse>();
            do {
                var playlistItemsResponse = await YouTubeApiPlaylistsListAsync(channelId, nextPageToken);
                playlistListResponse.Add(playlistItemsResponse);
                nextPageToken = playlistItemsResponse.NextPageToken;
            } while (nextPageToken != null);
            return playlistListResponse;
        }

        public async Task<PlaylistListResponse> YouTubeApiPlaylistsListAsync(string channelId, string nextPageToken = null) {
            var playlistListRequest = _youtubeApiClient.Playlists.List("snippet");
            playlistListRequest.ChannelId = channelId;
            var response = await playlistListRequest.ExecuteAsync();
            Thread.Sleep(1000);
            return response;
        }
        
        public async Task<VideoListResponse> YouTubeApiVideosAsync(string videoId) {
            var playlistListRequest = _youtubeApiClient.Videos.List("snippet");
            playlistListRequest.Id = videoId;
            var response = await playlistListRequest.ExecuteAsync();
            Thread.Sleep(1000);
            return response;
        }
        
        public async Task<GetItemResponse> DynamoDbGetPlaylistListAsync(string requestEmail, string playlistId) {
        var getRequest = new GetItemRequest {
                TableName = _dynamoDbTableName,
                Key = new Dictionary<string, AttributeValue> {
                    { "email", new AttributeValue { S = requestEmail } },
                    { "playlistId", new AttributeValue { S = playlistId } }
                }
            };
            return await _dynamoDbClient.GetItemAsync(getRequest);
        }
        
        public async Task DynamoDbPutPlaylistListAsync(string email, string channelId, string playlistId, string playlistName, List<PlaylistMonitorPlaylistItem> playlistItems, string dateNow) {
            
            var playlistItemsAttributeValues = new List<AttributeValue>();
            foreach (var item in playlistItems) {
                var attributeValue = new AttributeValue {S = JsonConvert.SerializeObject(item)};
                playlistItemsAttributeValues.Add(attributeValue);
            }
            var putRequest = new PutItemRequest {
                TableName = _dynamoDbTableName,
                Item = new Dictionary<string, AttributeValue> {
                    {"email", new AttributeValue {
                        S = email
                    }},
                    {"channelId", new AttributeValue {
                        S = channelId
                    }},
                    {"playlistId", new AttributeValue {
                        S = playlistId
                    }},
                    {"playlistName", new AttributeValue {
                        S = playlistName
                    }},
                    {"playlistItems", new AttributeValue {
                        L = playlistItemsAttributeValues
                    }},
                    {"lastCheckedTimestamp", new AttributeValue {
                        N = dateNow
                    }}
                }
            };
            await _dynamoDbClient.PutItemAsync(putRequest);
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
