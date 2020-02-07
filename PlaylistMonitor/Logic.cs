using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LambdaSharp.Logger;
using Newtonsoft.Json;

namespace Smylee.PlaylistMonitor.PlaylistMonitor {

    public class Logic {

        private readonly ILambdaLogLevelLogger _logger;
        private readonly IDependencyProvider _provider;

        //--- Methods ---
        public Logic(IDependencyProvider provider, ILambdaLogLevelLogger logger) {
            _provider = provider;
            _logger = logger;
        }

        public async Task Run() {
            var userSubscriptions = await GetSubscriptions();
            if (userSubscriptions.Count > 0) {
                await SendSubscriptions(userSubscriptions);
            }
        }

        public async Task<Dictionary<string, List<PlaylistMonitorSubscription>>> GetSubscriptions() {
            var allSubscriptions = new Dictionary<string, List<PlaylistMonitorSubscription>>();
            var subscriptionsDbList = await _provider.DynamoDbGetSubscriptionList();
            if (subscriptionsDbList.Items.Count <= 0) return allSubscriptions;
            _logger.LogInfo(JsonConvert.SerializeObject(subscriptionsDbList));
            foreach (var item in subscriptionsDbList.Items) {
                var email = item.GetValueOrDefault("email").S;
                var playlistSubs = item.GetValueOrDefault("playlists").L.Select(x => JsonConvert.DeserializeObject<PlaylistMonitorSubscription>(x.S)).ToList();
                _logger.LogInfo(JsonConvert.SerializeObject(playlistSubs));
                allSubscriptions.Add(email, playlistSubs);
            }
            return allSubscriptions;
        }

        public async Task SendSubscriptions(Dictionary<string, List<PlaylistMonitorSubscription>> allSubscriptions) {
            _logger.LogInfo(JsonConvert.SerializeObject(allSubscriptions));
            var sendRequest = new List<Task>();
            allSubscriptions.ToList().ForEach(subscription => sendRequest.Add(_provider.SnsPublishMessageAsync(JsonConvert.SerializeObject(subscription))));
            Task.WaitAll(sendRequest.ToArray());
        }
    }
}
