using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.SimpleNotificationService;
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
            var compareTopicArn = config.ReadText("CompareTopicPublish");
            var dynamoDbClient = new AmazonDynamoDBClient();
            var snsClient = new AmazonSimpleNotificationServiceClient();
            var provider = new DependencyProvider(dynamoDbClient, snsClient, dynamoDbUserSubscriptionTableName, compareTopicArn);
            _logic = new Logic(provider, Logger);
        }

        public override async Task ProcessEventAsync(LambdaScheduleEvent schedule) {
            await _logic.Run();
        }
    }
}
