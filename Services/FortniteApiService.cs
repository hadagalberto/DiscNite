using Fortnite_API;
using Fortnite_API.Objects.V1;
using Microsoft.Extensions.Configuration;

namespace DiscNite.Services
{
    public class FortniteApiService
    {

        private readonly FortniteApiClient _fortniteApiClient;

        public FortniteApiService(IConfiguration config)
        {
            var apiKey = config["fortniteApiToken"];
            _fortniteApiClient = new FortniteApiClient(apiKey);
        }

        public async Task<BrStatsV2V1> GetPlayerStaticsCurrentSeasonAsync(string player)
        {
            var requestParameters = new Action<Fortnite_API.Objects.V1.BrStatsV2V1RequestProperties>(x =>
            {
                x.Name = player;
                x.TimeWindow = BrStatsV2V1TimeWindow.Season;
            });
            
            var stats = await _fortniteApiClient.V2.Stats.GetBrV2Async(requestParameters);

            return stats.Data;
        }

        public async Task<BrStatsV2V1> GetPlayerStaticsAllTimeAsync(string player)
        {
            var requestParameters = new Action<Fortnite_API.Objects.V1.BrStatsV2V1RequestProperties>(x =>
            {
                x.Name = player;
                x.TimeWindow = BrStatsV2V1TimeWindow.Lifetime;
            });

            var stats = await _fortniteApiClient.V2.Stats.GetBrV2Async(requestParameters);

            return stats.Data;
        }

    }
}
