using DiscNite.Data;
using DiscNite.Services;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

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

                if (player.Vitorias != stats.Stats.All.Overall.Wins)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"O jogador **{player.Nome}** ganhou mais {stats.Stats.All.Overall.Wins - player.Vitorias} {(stats.Stats.All.Overall.Wins - player.Vitorias == 1 ? "vitória" : "vitórias")} na temporada atual!");
                    sb.AppendLine($"No total agora são {stats.Stats.All.Overall.Wins}");

                    player.Vitorias = stats.Stats.All.Overall.Wins;

                    await _discord.GetGuild(player.DiscordServer.IdDiscord).GetTextChannel(player.DiscordServer.IdTextChannel).SendMessageAsync(sb.ToString());
                }

                player.PlayerStatsJSON = Newtonsoft.Json.JsonConvert.SerializeObject(stats);

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating player stats");
            }
        }

        public async Task ProcessTopFiveDaily()
        {
            try
            {
                _logger.LogInformation("Processando os 5 melhores jogadores por servidor...");

                // Agrupa os jogadores por servidor e obtém os 5 melhores jogadores em cada servidor com base nas vitórias
                var topPlayersByServer = await _dbContext.FortnitePlayers
                    .GroupBy(x => x.DiscordServer)
                    .SelectMany(group => group.OrderByDescending(player => player.Vitorias).Take(5))
                    .ToListAsync();

                if (topPlayersByServer.Count == 0)
                {
                    return;
                }

                foreach (var serverGroup in topPlayersByServer.GroupBy(player => player.DiscordServer.IdDiscord))
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"**🏆 Top 5 Jogadores Hoje para o Servidor {serverGroup.Key} 🏆**:");

                    foreach (var player in serverGroup)
                    {
                        sb.AppendLine($"🎮 **Jogador:** {player.Nome}");
                        sb.AppendLine($"🏅 **Vitórias:** {player.Vitorias} {(player.Vitorias == 1 ? "vitória" : "vitórias")}");
                        sb.AppendLine("-------------------------------");
                    }

                    // Supondo que você tenha um canal de texto dedicado para os melhores jogadores
                    var topPlayersChannel = _discord.GetGuild(serverGroup.First().DiscordServer.IdDiscord).GetTextChannel(serverGroup.First().DiscordServer.IdDiscord);

                    await topPlayersChannel.SendMessageAsync(sb.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar os 5 melhores jogadores por servidor");
            }
        }

    }
}
