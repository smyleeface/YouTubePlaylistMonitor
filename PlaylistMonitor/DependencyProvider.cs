using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace Smylee.PlaylistMonitor.PlaylistMonitor {

    public interface IDependencyProvider {
        
        Task<ScanResponse> DynamoDbGetSubscriptionList();
        Task SnsPublishMessageAsync(string message);
    }

    public class DependencyProvider : IDependencyProvider {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly string _dynamoDbUserSubscriptionTableName;
        private readonly IAmazonSimpleNotificationService _snsClient;
        private readonly string _compareTopicArn;

        public DependencyProvider(IAmazonDynamoDB dynamoDbClient, IAmazonSimpleNotificationService snsClient, string dynamoDbUserSubscriptionTableName, string compareTopicArn) {
            _dynamoDbUserSubscriptionTableName = dynamoDbUserSubscriptionTableName;
            _dynamoDbClient = dynamoDbClient;
            _snsClient = snsClient;
            _compareTopicArn = compareTopicArn;
        }
        
        public async Task<ScanResponse> DynamoDbGetSubscriptionList() {
        var getRequest = new ScanRequest {
                TableName = _dynamoDbUserSubscriptionTableName
            };
            return await _dynamoDbClient.ScanAsync(getRequest);
        }
        
        public async Task SnsPublishMessageAsync(string message) {
            var publishRequest = new PublishRequest {
                Message = message,
                TopicArn = _compareTopicArn
            };
            await _snsClient.PublishAsync(publishRequest);
        }
    }
}
