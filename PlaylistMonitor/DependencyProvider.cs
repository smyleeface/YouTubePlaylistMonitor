using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using LambdaSharp;
using LambdaSharp.Logger;
using LambdaSharp.Schedule;
using Newtonsoft.Json;

namespace Smylee.PlaylistMonitor.PlaylistMonitor {

    public interface IDepenedencyProvder {
        Task<PlaylistItemListResponse> YouTubeApiPlaylistItemsList(string playlistId, string nextPageToken = null);
        Task<ChannelListResponse> YouTubeApiChannelsList(string requestedPlaylistUserName);
        Task<PlaylistListResponse> YouTubeApiPlaylistsList(string channelId);
        Task<VideoListResponse> YouTubeApiVideos(string videoId);
        Task<GetItemResponse> DynamoDbGetPlaylistList(string requestEmail, string playlistId);
        Task DynamoDbPutPlaylistList(string email, string channelId, string playlistId, string playlistName, List<PlaylistItem> playlistItems, List<PlaylistItem> deletedItems);
        Task SesSendEmail(SendEmailRequest sendEmailRequest);
        Task<ScanResponse> DynamoDbGetSubscriptionList();
    }

    public class DependencyProvider : IDepenedencyProvder {
        private string _dynamoDbTableName;
        private YouTubeService _youtubeApiClient;
        private IAmazonDynamoDB _dynamoDbClient;
        private IAmazonSimpleEmailServiceV2 _sesClient;
        private string _dynamoDbUserSubscriptionTableName;

        public DependencyProvider(YouTubeService youtubeApiClient, string dynamoDbTableName, IAmazonDynamoDB dynamoDbClient, IAmazonSimpleEmailServiceV2 sesClient, string dynamoDbUserSubscriptionTableName) {
            _dynamoDbUserSubscriptionTableName = dynamoDbUserSubscriptionTableName;
            _dynamoDbTableName = dynamoDbTableName;
            _youtubeApiClient = youtubeApiClient;
            _dynamoDbClient = dynamoDbClient;
            _sesClient = sesClient;
        }
        
        public async Task<PlaylistItemListResponse> YouTubeApiPlaylistItemsList(string playlistId, string nextPageToken = null) {
            var playlistListItemsRequest = _youtubeApiClient.PlaylistItems.List("snippet");
            playlistListItemsRequest.PlaylistId = playlistId;
            if (nextPageToken != null) {
                playlistListItemsRequest.PageToken = nextPageToken;
            }
            
            var response = await playlistListItemsRequest.ExecuteAsync();
            Thread.Sleep(1000);
            return response;
        }

        public async Task<ChannelListResponse> YouTubeApiChannelsList(string requestedPlaylistUserName) {
            var channelsListRequest = _youtubeApiClient.Channels.List("id");
            channelsListRequest.ForUsername = requestedPlaylistUserName;
            var response = await channelsListRequest.ExecuteAsync();
            Thread.Sleep(1000);
            return response;
        }

        public async Task<PlaylistListResponse> YouTubeApiPlaylistsList(string channelId) {
            var playlistListRequest = _youtubeApiClient.Playlists.List("snippet");
            playlistListRequest.ChannelId = channelId;
            var response = await playlistListRequest.ExecuteAsync();
            Thread.Sleep(1000);
            return response;
        }
        
        public async Task<VideoListResponse> YouTubeApiVideos(string videoId) {
            var playlistListRequest = _youtubeApiClient.Videos.List("snippet");
            playlistListRequest.Id = videoId;
            var response = await playlistListRequest.ExecuteAsync();
            Thread.Sleep(1000);
            return response;
        }
        
        public async Task<GetItemResponse> DynamoDbGetPlaylistList(string requestEmail, string playlistId) {
        var getRequest = new GetItemRequest {
                TableName = _dynamoDbTableName,
                Key = new Dictionary<string, AttributeValue> {
                    { "email", new AttributeValue { S = requestEmail } },
                    { "playlistId", new AttributeValue { S = playlistId } }
                }
            };
            return await _dynamoDbClient.GetItemAsync(getRequest);
        }
        
        public async Task<ScanResponse> DynamoDbGetSubscriptionList() {
        var getRequest = new ScanRequest {
                TableName = _dynamoDbUserSubscriptionTableName
            };
            return await _dynamoDbClient.ScanAsync(getRequest);
        }
        
        public async Task DynamoDbPutPlaylistList(string email, string channelId, string playlistId, string playlistName, List<PlaylistItem> playlistItems, List<PlaylistItem> deletedItems) {
            
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
                        N = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString(CultureInfo.InvariantCulture)
                    }}
                }
            };
            
            // deletedItemsAttributeValues
            var deletedItemsAttributeValues = new List<AttributeValue>();
            foreach (var item in deletedItems) {
                var attributeValue = new AttributeValue {S = JsonConvert.SerializeObject(item)};
                deletedItemsAttributeValues.Add(attributeValue);
            }
            if (deletedItemsAttributeValues.Count > 0) {
                putRequest.Item.Add("deletedItems", new AttributeValue {
                    L = deletedItemsAttributeValues
                });
            }
            await _dynamoDbClient.PutItemAsync(putRequest);
        }

        public async Task SesSendEmail(SendEmailRequest sendEmailRequest) {
            await _sesClient.SendEmailAsync(sendEmailRequest);
        }
    }
}
