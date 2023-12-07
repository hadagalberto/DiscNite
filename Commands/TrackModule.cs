using DiscNite.Data;
using DiscNite.Services;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace DiscNite.Commands
{
    public class TrackModule : InteractionModuleBase<SocketInteractionContext>
    {

        private readonly FortniteApiService _fortniteApiService;
        private readonly AppDbContext _dbContext;

        public TrackModule(FortniteApiService fortniteApiService, AppDbContext dbContext)
        {
            _fortniteApiService = fortniteApiService;
            _dbContext = dbContext;
        }


        [SlashCommand("track", "Acompanha a evolução do player")]
        public async Task TrackUser(string player)
        {
            var stats = await _fortniteApiService.GetPlayerStaticsCurrentSeasonAsync(player);

            if (stats == null)
            {
                await RespondAsync("Não foi possível encontrar o player ❌");
                return;
            }

            var guidId = this.Context.Guild.Id;
            var guidPlayer = stats.Account.Id;

            var playerInDb = await _dbContext.FortnitePlayers.FirstOrDefaultAsync(x => x.DiscordServer.IdDiscord == guidId && x.IdDiscord == guidPlayer);
            var serverInDb = await _dbContext.DiscordServers.FirstOrDefaultAsync(x => x.IdDiscord == guidId);

            try
            {
                if (serverInDb == null)
                {
                    serverInDb = new Models.DiscordServer()
                    {
                        IdDiscord = guidId,
                        Nome = this.Context.Guild.Name,
                        Descricao = this.Context.Guild.Description ?? "Sem descrição",
                        IdTextChannel = this.Context.Channel.Id
                    };

                    await _dbContext.DiscordServers.AddAsync(serverInDb);
                    await _dbContext.SaveChangesAsync();
                }

                if (playerInDb == null)
                {
                    playerInDb = new Models.FortnitePlayer()
                    {
                        IdFortnitePlayer = Guid.NewGuid(),
                        IdDiscord = guidPlayer,
                        Nome = stats.Account.Name,
                        IdDiscordServer = serverInDb.IdDiscordServer,
                        DateUpdated = stats.Stats.All.Overall.LastModified,
                        Vitorias = stats.Stats.All.Overall.Wins
                    };

                    await _dbContext.FortnitePlayers.AddAsync(playerInDb);
                    await _dbContext.SaveChangesAsync();
                }
                else
                {
                    playerInDb.Nome = stats.Account.Name;
                    playerInDb.DateUpdated = stats.Stats.All.Overall.LastModified;
                    playerInDb.Vitorias = stats.Stats.All.Overall.Wins;

                    _dbContext.FortnitePlayers.Update(playerInDb);
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                await RespondAsync($"Erro ao salvar no banco de dados: {ex.Message}");
                return;
            }

            //var response = new StringBuilder();

            //response.AppendLine($"**{stats.Account.Name}**");
            //response.AppendLine($"🎮 Partidas: {stats.Stats.All.Overall.Matches}");
            //response.AppendLine($"**Nível:** {stats.BattlePass.Level}");
            //response.AppendLine($"🏆 Vitórias: {stats.Stats.All.Overall.Wins}");
            //response.AppendLine($"📊 W/L: {stats.Stats.All.Overall.WinRate}");
            //response.AppendLine($"💀 Kills: {stats.Stats.All.Overall.Kills}");
            //response.AppendLine($"💔 Mortes: {stats.Stats.All.Overall.Deaths}");
            //response.AppendLine($"🔪 K/D: {stats.Stats.All.Overall.Kd}");
            //response.AppendLine($"🎖️ Pontuação acumulada: {stats.Stats.All.Overall.Score}");

            var response = $"Agora estaremos acompanhando a evolução do player {stats.Account.Name}!";

            await RespondAsync(response.ToString());
        }

        [SlashCommand("untrack", "Deixa de acompanhar a evolução do player")]
        public async Task UntrackUser(string player)
        {
            var stats = await _fortniteApiService.GetPlayerStaticsCurrentSeasonAsync(player);

            if (stats == null)
            {
                await RespondAsync("Não foi possível encontrar o player ❌");
                return;
            }

            var guidId = this.Context.Guild.Id;
            var guidPlayer = stats.Account.Id;

            var playerInDb = await _dbContext.FortnitePlayers.FirstOrDefaultAsync(x => x.DiscordServer.IdDiscord == guidId && x.IdDiscord == guidPlayer);

            if (playerInDb == null)
            {
                await RespondAsync($"Não estamos acompanhando o player {stats.Account.Name} ❌");
                return;
            }

            try
            {
                _dbContext.FortnitePlayers.Remove(playerInDb);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                await RespondAsync($"Erro ao salvar no banco de dados: {ex.Message}");
                return;
            }

            await RespondAsync($"Não estaremos mais acompanhando a evolução do player {stats.Account.Name} 😢");
        }

        [SlashCommand("list", "Lista os players que estamos acompanhando")]
        public async Task ListTrackedUsers()
        {
            var guidId = this.Context.Guild.Id;

            var playersInDb = await _dbContext.FortnitePlayers.Where(x => x.DiscordServer.IdDiscord == guidId).ToListAsync();

            if (playersInDb == null || playersInDb.Count == 0)
            {
                await RespondAsync("Não estamos acompanhando nenhum player 😢");
                return;
            }

            var response = new StringBuilder();

            response.AppendLine("**Players acompanhados:**");

            foreach (var player in playersInDb)
            {
                response.AppendLine($"- {player.Nome}");
            }

            await RespondAsync(response.ToString());
        }

        [SlashCommand("update", "Atualiza os dados dos players que estamos acompanhando")]
        public async Task UpdateTrackedUsers()
        {
            var guidId = this.Context.Guild.Id;

            var playersInDb = await _dbContext.FortnitePlayers.Where(x => x.DiscordServer.IdDiscord == guidId).ToListAsync();

            if (playersInDb == null || playersInDb.Count == 0)
            {
                await RespondAsync("Não estamos acompanhando nenhum player 😢");
                return;
            }

            var response = new StringBuilder();

            response.AppendLine("**Atualizando os dados dos players acompanhados:**");

            foreach (var player in playersInDb)
            {
                var stats = await _fortniteApiService.GetPlayerStaticsCurrentSeasonAsync(player.IdDiscord);

                if (stats == null)
                {
                    response.AppendLine($"Não foi possível encontrar o player {player.Nome} 😢");
                    continue;
                }

                player.Nome = stats.Account.Name;
                player.DateUpdated = stats.Stats.All.Overall.LastModified;

                _dbContext.FortnitePlayers.Update(player);
                await _dbContext.SaveChangesAsync();

                response.AppendLine($"- {player.Nome}");
            }

            await RespondAsync(response.ToString());
        }

        [SlashCommand("stats", "Mostra as estatísticas do player")]
        public async Task ShowStats(string player)
        {
            var seasonStats = await _fortniteApiService.GetPlayerStaticsCurrentSeasonAsync(player);
            var lifetimeStats = await _fortniteApiService.GetPlayerStaticsAllTimeAsync(player);

            var response = new StringBuilder();

            response.AppendLine($"**{seasonStats.Account.Name}**");

            if (seasonStats == null)
            {
                response.AppendLine("❌ Não foi possível encontrar dados do player na **Season Atual**");
            }
            else
            {
                response.AppendLine("**Season Atual**");
                response.AppendLine($"🎮 Partidas: {seasonStats.Stats.All.Overall.Matches}");
                response.AppendLine($"🌟 **Nível:** {seasonStats.BattlePass.Level}");
                response.AppendLine($"🏆 Vitórias: {seasonStats.Stats.All.Overall.Wins}");
                response.AppendLine($"📊 W/L: {seasonStats.Stats.All.Overall.WinRate}%");
                response.AppendLine($"💀 Kills: {seasonStats.Stats.All.Overall.Kills}");
                response.AppendLine($"💔 Mortes: {seasonStats.Stats.All.Overall.Deaths}");
                response.AppendLine($"🔪 K/D: {seasonStats.Stats.All.Overall.Kd}");
                response.AppendLine($"🎖️ Pontuação acumulada: {seasonStats.Stats.All.Overall.Score}");
                response.AppendLine("");
            }
            
            if (lifetimeStats == null)
            {
                response.AppendLine("❌ Não foi possível encontrar dados do player fora dessa season");
            }
            else
            {
                response.AppendLine("**Geral**");
                response.AppendLine($"🎮 Partidas: {lifetimeStats.Stats.All.Overall.Matches}");
                response.AppendLine($"🏆 Vitórias: {lifetimeStats.Stats.All.Overall.Wins}");
                response.AppendLine($"📊 W/L: {lifetimeStats.Stats.All.Overall.WinRate}%");
                response.AppendLine($"💀 Kills: {lifetimeStats.Stats.All.Overall.Kills}");
                response.AppendLine($"💔 Mortes: {lifetimeStats.Stats.All.Overall.Deaths}");
                response.AppendLine($"🔪 K/D: {lifetimeStats.Stats.All.Overall.Kd}");
                response.AppendLine($"🎖️ Pontuação acumulada: {lifetimeStats.Stats.All.Overall.Score}");
            }
            

            await RespondAsync(response.ToString());
        }

        [SlashCommand("updatechannel", "Atualiza o canal de texto onde as mensagens serão enviadas")]
        public async Task UpdateChannel()
        {
            var guidId = this.Context.Guild.Id;

            var serverInDb = await _dbContext.DiscordServers.FirstOrDefaultAsync(x => x.IdDiscord == guidId);

            if (serverInDb == null)
            {
                await RespondAsync("Não estamos acompanhando nenhum player ❌");
                return;
            }

            serverInDb.IdTextChannel = this.Context.Channel.Id;

            _dbContext.DiscordServers.Update(serverInDb);
            await _dbContext.SaveChangesAsync();

            await RespondAsync($"Canal atualizado para {this.Context.Channel.Name}");
        }

        [SlashCommand("top5", "Mostra os top 5 jogadores por servidor")]
        public async Task ShowTop5()
        {
            try
            {
                var guidId = this.Context.Guild.Id;

                var topPlayersByServer = await _dbContext.FortnitePlayers
                    .Where(x => x.DiscordServer.IdDiscord == guidId)
                    .OrderByDescending(x => x.Vitorias)
                    .Take(5)
                    .ToListAsync();

                if (topPlayersByServer.Count == 0)
                {
                    await RespondAsync("Não há jogadores acompanhados para mostrar ❌");
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine("**🏆 Top 5 Jogadores desse servidor 🏆**:");

                foreach (var player in topPlayersByServer)
                {
                    sb.AppendLine($"🥇 {player.Nome} - {player.Vitorias} {(player.Vitorias == 1 ? "vitória" : "vitórias")}");
                }

                await RespondAsync(sb.ToString());
            }
            catch (Exception ex)
            {
                await RespondAsync("Ocorreu um erro ao processar a solicitação ❌");
            }
        }


    }
}
