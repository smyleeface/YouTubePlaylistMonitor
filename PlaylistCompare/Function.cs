using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Amazon.SimpleEmailV2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using LambdaSharp;
using Newtonsoft.Json;
using Smylee.PlaylistMonitor.PlaylistMonitor;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Smylee.YouTube.PlaylistCompare {

    // public class Function : ALambdaTopicFunction<Dictionary<string, List<PlaylistMonitorSubscription>>> {
    public class Function : ALambdaFunction<SNSEvent, string> {

        private Logic _logic;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            var dynamoDbTableName = AwsConverters.ConvertDynamoDBArnToName(config.ReadText("UserPlaylist"));
            var fromEmail = config.ReadText("FromEmail");
            var youtubeApiKey = config.ReadText("YouTubeApiKey");
            var youtubeApiClient = new YouTubeService(new BaseClientService.Initializer {
                ApiKey = youtubeApiKey,
                ApplicationName = GetType().ToString()
            });
            var dynamoDbClient = new AmazonDynamoDBClient();
            var sesClient = new AmazonSimpleEmailServiceV2Client();
            var provider = new DependencyProvider(youtubeApiClient, dynamoDbTableName, dynamoDbClient, sesClient);
            _logic = new Logic(fromEmail, provider, Logger);
        }

        public override async Task<string> ProcessMessageAsync(SNSEvent eventMessage) {
            var playlistMonitorSubscription = JsonConvert.DeserializeObject<KeyValuePair<string, List<PlaylistMonitorSubscription>>>(eventMessage.Records.First().Sns.Message);
            var dateNow = DateTime.Now.Date;
            Task.WaitAll(_logic.Run(dateNow, playlistMonitorSubscription.Key, playlistMonitorSubscription.Value));
            return "done";
        }
    }
}
