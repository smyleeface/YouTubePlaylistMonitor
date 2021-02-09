using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.Lambda.SNSEvents;
using Amazon.SimpleEmailV2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using LambdaSharp;
using Newtonsoft.Json;
using Smylee.PlaylistMonitor.Library;
using Smylee.PlaylistMonitor.Library.Models;
using DependencyProvider = Smylee.PlaylistMonitor.Library.DependencyProvider;

namespace Smylee.PlaylistMonitor.PlaylistCompare {

    // public class Function : ALambdaTopicFunction<Dictionary<string, List<PlaylistMonitorSubscription>>> {
    public class Function : ALambdaFunction<SNSEvent, string> {

        private Logic _logic;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            var dynamoDbSubscriptionTableName = AwsConverters.ConvertDynamoDBArnToName(config.ReadText("UserSubscriptions"));
            var dynamoDbVideoTableName = AwsConverters.ConvertDynamoDBArnToName(config.ReadText("CacheVideos"));
            var dynamoDbPlaylistsTableName = AwsConverters.ConvertDynamoDBArnToName(config.ReadText("CachePlaylists"));
            var fromEmail = config.ReadText("FromEmail");
            var youtubeApiKey = config.ReadText("YouTubeApiKey");
            var youtubeApiClient = new YouTubeService(new BaseClientService.Initializer {
                ApiKey = youtubeApiKey,
                ApplicationName = GetType().ToString()
            });
            var dynamoDbClient = new AmazonDynamoDBClient();
            var sesClient = new AmazonSimpleEmailServiceV2Client();
            var provider = new DependencyProvider(youtubeApiClient, dynamoDbPlaylistsTableName, dynamoDbSubscriptionTableName, dynamoDbVideoTableName, dynamoDbClient, sesClient);
            var dataAccess = new DataAccess(provider);
            _logic = new Logic(fromEmail, dataAccess, Logger);
        }

        public override async Task<string> ProcessMessageAsync(SNSEvent eventMessage) {
            var message = eventMessage.Records.First().Sns.Message;
            LogInfo($"message {message}");
            var playlistMonitorSubscription = JsonConvert.DeserializeObject<KeyValuePair<string, List<PlaylistMonitorSubscription>>>(message);
            var dateNow = DateTime.Now.Date;
            var requestEmail = playlistMonitorSubscription.Key;
            Task.WaitAll(_logic.Run(dateNow, requestEmail, playlistMonitorSubscription.Value));
            return "done";
        }
    }
}
