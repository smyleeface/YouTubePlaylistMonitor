using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.SimpleEmailV2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using LambdaSharp;
using LambdaSharp.Schedule;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Smylee.PlaylistMonitor.PlaylistMonitor {

    public class Function : ALambdaScheduleFunction {

        private Logic _logic;


        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            var dynamoDbUserSubscriptionTableName = AwsConverters.ConvertDynamoDBArnToName(config.ReadText("UserSubscriptions"));
            var dynamoDbTableName = AwsConverters.ConvertDynamoDBArnToName(config.ReadText("UserPlaylist"));
            var youtubeApiKey = config.ReadText("YouTubeApiKey");
            var youtubeApiClient = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = youtubeApiKey,
                ApplicationName = GetType().ToString()
            });
            var dynamoDbClient = new AmazonDynamoDBClient();
            var sesClient = new AmazonSimpleEmailServiceV2Client();
            var provider = new DependencyProvider(youtubeApiClient, dynamoDbTableName, dynamoDbClient, sesClient, dynamoDbUserSubscriptionTableName);
            _logic = new Logic(provider, Logger);
        }

        public override async Task ProcessEventAsync(LambdaScheduleEvent schedule) {
            var result = await _logic.Run();
            LogInfo(result);
        }
    }
}
