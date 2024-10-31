using DiscNite.Data;
using DiscNite.Services;
using Discord;
using Discord.WebSocket;
using Fortnite_API.Objects.V1;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Pubg.Net;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace DiscNite.Utils
{
    public class HangfireUpdater
    {

        private readonly AppDbContext _dbContext;
        private readonly FortniteApiService _fortniteApiService;
        private readonly PUBGApiService _pubgApiService;
        private readonly DiscordSocketClient _discord;
        private readonly ILogger<HangfireUpdater> _logger;
        private readonly IConfiguration _config;

        public HangfireUpdater(AppDbContext dbContext, FortniteApiService fortniteApiService, DiscordSocketClient discord, IConfiguration config, PUBGApiService pubgApiService)
        {
            _dbContext = dbContext;
            _fortniteApiService = fortniteApiService;
            _discord = discord;
            _logger = new Logger<HangfireUpdater>(new LoggerFactory());
            _config = config;
            _pubgApiService = pubgApiService;
        }

        public async Task UpdatePlayerStats()
        {
            _logger.LogInformation("Updating player stats...");
            Console.WriteLine("Updating player stats...");
            var fortnitePlayers = await _dbContext.FortnitePlayers
                .Include(x => x.DiscordServer)
                .ToListAsync();

            var servers = await _dbContext.DiscordServers
                .CountAsync();

            foreach (var player in fortnitePlayers)
            {
                await ProcessFortnitePlayerUpdate(player);
            }

            var pubgPlayers = await _dbContext.PUBGPlayers
                .Include(x => x.DiscordServer)
                .ToListAsync();

            foreach (var player in pubgPlayers)
            {
                await ProcessPUBGPlayerUpdate(player);
            }

            //await _discord.SetActivityAsync(new Game($"Trackeando {fortnitePlayers.Count + pubgPlayers.Count} players em {servers} servidores", ActivityType.CustomStatus));
        }

        private async Task ProcessFortnitePlayerUpdate(Models.FortnitePlayer player)
        {
            try
            {
                BrStatsV2V1 stats = null;
                var playerId = player.IdDiscord.Clone().ToString();
                if (playerId.IsNullOrEmpty())
                {
                    var playerName = player.Nome.Clone().ToString();
                    stats = await _fortniteApiService.GetPlayerStaticsCurrentSeasonAsync(playerName);
                }
                else
                {
                    stats = await _fortniteApiService.GetPlayerStaticsCurrentSeasonByPlayerIdAsync(playerId);
                }

                if (stats == null)
                {
                    // season reset
                    player.Vitorias = 0;
                    return;
                }

                if (player.Vitorias < stats.Stats.All.Overall.Wins)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"O jogador de Fornite **{player.Nome}** ganhou mais {stats.Stats.All.Overall.Wins - player.Vitorias} {(stats.Stats.All.Overall.Wins - player.Vitorias == 1 ? "vitória" : "vitórias")} na temporada atual!");
                    sb.AppendLine($"No total agora são {stats.Stats.All.Overall.Wins}");

                    player.Vitorias = stats.Stats.All.Overall.Wins;

                    await _discord.GetGuild(player.DiscordServer.IdDiscord).GetTextChannel(player.DiscordServer.IdTextChannel).SendMessageAsync(sb.ToString());
                }

                player.Nome = stats.Account.Name;
                player.PlayerStatsJSON = Newtonsoft.Json.JsonConvert.SerializeObject(stats);

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating player stats");
                Console.WriteLine($"Erro ao atualizar player de Fortnite: {player.Nome}" + ex);
            }
        }

        private async Task ProcessPUBGPlayerUpdate(Models.PUBGPlayer player)
        {
            try
            {
                var playerStats = await _pubgApiService.GetPlayerStaticsByIdAsync(player.IdDiscord);

                if (playerStats == null)
                {
                    return;
                }

                var stats = playerStats.stats;

                var vitoriasDb = player.VitoriasSolo + player.VitoriasDuo + player.VitoriasQuad;
                var vitoriasAtuais = stats.GameModeStats.Solo.Wins
                                     + stats.GameModeStats.Duo.Wins
                                     + stats.GameModeStats.Squad.Wins
                                     + stats.GameModeStats.SoloFPP.Wins
                                     + stats.GameModeStats.DuoFPP.Wins
                                     + stats.GameModeStats.SquadFPP.Wins;

                if (vitoriasAtuais == 0)
                {
                    // season reset
                    player.VitoriasSolo = 0;
                    player.VitoriasDuo = 0;
                    player.VitoriasQuad = 0;
                    return;
                }

                if (vitoriasAtuais > vitoriasDb)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"O jogador de PUBG **{player.Nome}** ganhou mais {vitoriasAtuais - vitoriasDb} {(vitoriasAtuais - vitoriasDb == 1 ? "vitória" : "vitórias")} na temporada atual!");
                    sb.AppendLine($"No total agora são {vitoriasAtuais}");

                    player.VitoriasSolo = stats.GameModeStats.Solo.Wins + stats.GameModeStats.SoloFPP.Wins;
                    player.VitoriasDuo = stats.GameModeStats.Duo.Wins + stats.GameModeStats.DuoFPP.Wins;
                    player.VitoriasQuad = stats.GameModeStats.Squad.Wins + stats.GameModeStats.SquadFPP.Wins;

                    await _discord.GetGuild(player.DiscordServer.IdDiscord).GetTextChannel(player.DiscordServer.IdTextChannel).SendMessageAsync(sb.ToString());
                }

                player.Nome = playerStats.player.Name;
                player.PlayerStatsJSON = Newtonsoft.Json.JsonConvert.SerializeObject(stats);

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating player stats");
                Console.WriteLine($"Erro ao atualizar player de PUBG: {player.Nome}" + ex);
            }
            
        }

        public async Task ProcessFortniteTopFiveDaily()
        {
            try
            {
                _logger.LogInformation("Processando os 5 melhores jogadores por servidor...");
                Console.WriteLine("Processando os 5 melhores jogadores por servidor...");

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
                        continue;
                    }

                    var sb = new StringBuilder();
                    sb.AppendLine($"**🏆 Top 5 Jogadores de Fortnite hoje para o servidor {server.Nome} 🏆**:");

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

        public async Task ProcessPUBGTopFiveDaily()
        {
            try
            {
                LogHelper.OnLogAsync(_logger, new LogMessage(LogSeverity.Info, "DiscNite", "Processando os 5 melhores jogadores por servidor..."));

                // Agrupa os jogadores por servidor e obtém os 5 melhores jogadores em cada servidor com base nas vitórias
                var servers = await _dbContext.DiscordServers
                    .ToListAsync();

                // download all guids from discord client
                foreach ( var server in servers) {
                    var topPlayers = await _dbContext.PUBGPlayers
                        .Where(x => x.DiscordServer.IdDiscord == server.IdDiscord)
                        .OrderByDescending(x => x.VitoriasSolo + x.VitoriasDuo + x.VitoriasQuad)
                        .Take(5)
                        .ToListAsync();

                    if (topPlayers.Count == 0)
                    {
                        continue;
                    }

                    var sb = new StringBuilder();
                    sb.AppendLine($"**🏆 Top 5 Jogadores de PUBG hoje para o servidor {server.Nome} 🏆**:");

                    foreach (var player in topPlayers)
                    {
                        var playerStats = Newtonsoft.Json.JsonConvert.DeserializeObject<PubgStatEntity>(player.PlayerStatsJSON);

                        sb.AppendLine($"🎮 **Jogador:** {player.Nome}");
                        sb.AppendLine($"🏅 **Vitórias Solo:** {player.VitoriasSolo}");
                        sb.AppendLine($"🏅 **Vitórias Duo:** {player.VitoriasDuo}");
                        sb.AppendLine($"🏅 **Vitórias Squad:** {player.VitoriasQuad}");
                        sb.AppendLine($"🔫 **Kills:** {playerStats.GameModeStats.Solo.Kills + playerStats.GameModeStats.Duo.Kills + playerStats.GameModeStats.Squad.Kills}");
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

        public async Task AtualizarAtividadeDiscord()
        {
            try
            {
                var fortnitePlayers = await _dbContext.FortnitePlayers
                    .CountAsync();

                var pubgPlayers = await _dbContext.PUBGPlayers
                    .CountAsync();

                var servers = await _dbContext.DiscordServers
                    .CountAsync();

                await _discord.SetCustomStatusAsync($"Trackeando {fortnitePlayers + pubgPlayers} players em {servers} servidores");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

    }
}
