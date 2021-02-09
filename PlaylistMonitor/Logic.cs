using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LambdaSharp.Logging;
using Newtonsoft.Json;
using Smylee.PlaylistMonitor.Library.Models;

namespace Smylee.PlaylistMonitor.PlaylistMonitor {

    public class Logic {

        private readonly ILambdaSharpLogger _logger;
        private readonly IDependencyProvider _provider;

        //--- Methods ---
        public Logic(IDependencyProvider provider, ILambdaSharpLogger logger) {
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
            foreach (var item in subscriptionsDbList.Items) {
                var email = item.GetValueOrDefault("email").S;
                var playlistSubs = item.GetValueOrDefault("playlists").L.Select(x => JsonConvert.DeserializeObject<PlaylistMonitorSubscription>(x.S)).ToList();
                allSubscriptions.Add(email, playlistSubs);
            }
            return allSubscriptions;
        }

        public async Task SendSubscriptions(Dictionary<string, List<PlaylistMonitorSubscription>> allSubscriptions) {
            var sendRequest = new List<Task>();
            allSubscriptions.ToList().ForEach(subscription => sendRequest.Add(_provider.SnsPublishMessageAsync(JsonConvert.SerializeObject(subscription))));
            Task.WaitAll(sendRequest.ToArray());
        }
    }
}
