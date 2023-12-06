using DiscNite.Data;
using DiscNite.Services;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiscNite.Utils
{
    public class HangfireUpdater
    {

        private readonly AppDbContext _dbContext;
        private readonly FortniteApiService _fortniteApiService;
        private readonly DiscordSocketClient _discord;
        private readonly ILogger<HangfireUpdater> _logger;

        public HangfireUpdater(AppDbContext dbContext, FortniteApiService fortniteApiService, DiscordSocketClient discord)
        {
            _dbContext = dbContext;
            _fortniteApiService = fortniteApiService;
            _discord = discord;
            _logger = new Logger<HangfireUpdater>(new LoggerFactory());
        }

        public async Task UpdatePlayerStats()
        {
            _logger.LogInformation("Updating player stats...");
            var players = await _dbContext.FortnitePlayers
                .Include(x => x.DiscordServer)
                .ToListAsync();

            foreach (var player in players)
            {
                await ProcessPlayerUpdate(player);                
            }
        }

        private async Task ProcessPlayerUpdate(Models.FortnitePlayer player)
        {
            try
            {
                var playerName = player.Nome.Clone().ToString();
                var stats = await _fortniteApiService.GetPlayerStaticsCurrentSeasonAsync(playerName);

                if (stats == null)
                {
                    return;
                }

                if (player.Vitorias == stats.Stats.All.Overall.Wins)
                {
                    return;
                }

                var response = $"O jogador {player.Nome} ganhou mais {stats.Stats.All.Overall.Wins - player.Vitorias} {(stats.Stats.All.Overall.Wins - player.Vitorias == 1 ? "vitória" : "vitórias")} na temporada atual!";

                player.Vitorias = stats.Stats.All.Overall.Wins;
                await _dbContext.SaveChangesAsync();

                await _discord.GetGuild(player.DiscordServer.IdDiscord).GetTextChannel(player.DiscordServer.IdTextChannel).SendMessageAsync(response.ToString());
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating player stats");
            }
        }

    }
}
