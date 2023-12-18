using DiscNite.Data;
using DiscNite.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace DiscNite.Utils
{
    public class HangfireUpdater
    {

        private readonly AppDbContext _dbContext;
        private readonly FortniteApiService _fortniteApiService;
        private readonly DiscordSocketClient _discord;
        private readonly ILogger<HangfireUpdater> _logger;
        private readonly IConfiguration _config;

        public HangfireUpdater(AppDbContext dbContext, FortniteApiService fortniteApiService, DiscordSocketClient discord, IConfiguration config)
        {
            _dbContext = dbContext;
            _fortniteApiService = fortniteApiService;
            _discord = discord;
            _logger = new Logger<HangfireUpdater>(new LoggerFactory());
            _config = config;
        }

        public async Task UpdatePlayerStats()
        {
            _logger.LogInformation("Updating player stats...");
            var players = await _dbContext.FortnitePlayers
                .Include(x => x.DiscordServer)
                .ToListAsync();

            await _discord.SetActivityAsync(new Game($"{players.Count()} players sendo trackeados", ActivityType.CustomStatus));

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
                var servers = await _dbContext.DiscordServers
                    .ToListAsync();

                // download all guids from discord client

                foreach (var server in servers)
                {
                    var topPlayers = await _dbContext.FortnitePlayers
                        .Where(x => x.DiscordServer.IdDiscord == server.IdDiscord)
                        .OrderByDescending(x => x.Vitorias)
                        .Take(5)
                        .ToListAsync();

                    if (topPlayers.Count == 0)
                    {
                        return;
                    }

                    var sb = new StringBuilder();
                    sb.AppendLine($"**🏆 Top 5 Jogadores Hoje para o Servidor {server.Nome} 🏆**:");

                    foreach (var player in topPlayers)
                    {
                        var playerStats = Newtonsoft.Json.JsonConvert.DeserializeObject<Fortnite_API.Objects.V1.BrStatsV2V1>(player.PlayerStatsJSON);

                        sb.AppendLine($"🎮 **Jogador:** {player.Nome}");
                        sb.AppendLine($"🏅 **Vitórias:** {player.Vitorias} {(player.Vitorias == 1 ? "vitória" : "vitórias")}");
                        sb.AppendLine($"🔫 **Kills:** {playerStats.Stats.All.Overall.Kills}");
                        sb.AppendLine($"🔪 **K/D:** {playerStats.Stats.All.Overall.Kd}");
                        sb.AppendLine($"🏹 **Level:** {playerStats.BattlePass.Level}");
                        sb.AppendLine("-------------------------------");
                    }

                    if(_discord.LoginState != LoginState.LoggedIn)
                    {
                        await _discord.LoginAsync(TokenType.Bot, _config["token"]);
                    }

                    var channel = await _discord.GetChannelAsync(server.IdTextChannel);

                    if (channel == null)
                    {
                        return;
                    }

                    await (channel as IMessageChannel).SendMessageAsync(sb.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar os 5 melhores jogadores por servidor");
            }
        }

    }
}
