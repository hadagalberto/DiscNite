using Microsoft.Extensions.Configuration;
using Pubg.Net;

namespace DiscNite.Services
{

    public class PUBGApiService
    {

        private readonly PubgPlayerService _pubgPlayerService;
        private readonly PubgSeasonService _pubgSeasonService;

        public PUBGApiService(IConfiguration config)
        {
            var apiKey = config["pubgApiToken"];
            _pubgPlayerService = new PubgPlayerService(apiKey);
            _pubgSeasonService = new PubgSeasonService(apiKey);
        }

        public PUBGApiService(string apiKey)
        {
            _pubgPlayerService = new PubgPlayerService(apiKey);
            _pubgSeasonService = new PubgSeasonService(apiKey);
        }

        public async Task<PubgPlayer> GetPlayerId(string playerName)
        {
            var requestParameters = new GetPubgPlayersRequest
            {
                PlayerNames = [playerName]
            };

            var players = await _pubgPlayerService.GetPlayersAsync(PubgPlatform.Steam, requestParameters);

            return players.FirstOrDefault();
        }

        public async Task<PubgPlayer> GetPlayerByPlayerId(string playerId)
        {
            var requestParameters = new GetPubgPlayersRequest
            {
                PlayerIds = [playerId]
            };

            var players = await _pubgPlayerService.GetPlayersAsync(PubgPlatform.Steam, requestParameters);

            return players.FirstOrDefault();
        }

        public async Task<PUBGPlayerResponse> GetPlayerStaticsAsync(string player)
        {
            PUBGPlayerResponse retorno = null;
            try
            {
                var playerId = await GetPlayerId(player);

                var seasons = await _pubgSeasonService.GetSeasonsPCAsync();

                var currentSeason = seasons.FirstOrDefault(x => x.IsCurrentSeason);

                var stats = await _pubgPlayerService.GetPlayerSeasonAsync(PubgPlatform.Steam, playerId.Id, currentSeason.Id);

                retorno = new PUBGPlayerResponse(stats, playerId);
            }
            catch (Exception)
            {
                return retorno;
            }

            return retorno;
        }

        public async Task<PUBGPlayerResponse> GetPlayerStaticsByIdAsync(string playerId)
        {
            PUBGPlayerResponse retorno = null;
            try
            {
                var seasons = await _pubgSeasonService.GetSeasonsPCAsync();

                var currentSeason = seasons.FirstOrDefault(x => x.IsCurrentSeason);

                var stats = await _pubgPlayerService.GetPlayerSeasonAsync(PubgPlatform.Steam, playerId, currentSeason.Id);

                retorno = new PUBGPlayerResponse(stats, await GetPlayerByPlayerId(playerId));
            }
            catch (Exception)
            {
                return retorno;
            }

            return retorno;
        }

        public async Task<PubgStatEntity> GetPlayerLifetimeStats(string playerName)
        {
            PubgStatEntity retorno = null;
            try
            {
                var playerId = await GetPlayerId(playerName);

                var stats = await _pubgPlayerService.GetPlayerLifetimeStatsAsync(PubgPlatform.Steam, playerId.Id);

                retorno = stats;
            }
            catch (Exception)
            {
                return retorno;
            }

            return retorno;
        }

    }

    public record PUBGPlayerResponse(PubgStatEntity stats, PubgPlayer player);
}