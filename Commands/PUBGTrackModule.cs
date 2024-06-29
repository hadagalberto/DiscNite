using System.Text;
using DiscNite.Data;
using DiscNite.Services;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Pubg.Net;

namespace DiscNite.Commands
{

    public class PUBGTrackModule : InteractionModuleBase<SocketInteractionContext>
    {

        private readonly PUBGApiService _pubgService;
        private readonly AppDbContext _dbContext;

        public PUBGTrackModule(PUBGApiService pubgService, AppDbContext dbContext)
        {
            _pubgService = pubgService;
            _dbContext = dbContext;
        }


        [SlashCommand("pg-track", "Acompanha a evolução do player")]
        public async Task TrackUser(string playerName)
        {
            var playerStats = await _pubgService.GetPlayerStaticsAsync(playerName);

            if (playerStats == null)
            {
                await RespondAsync("Não foi possível encontrar o player ❌");
                return;
            }

            var guidId = Context.Guild.Id;
            var guidPlayer = playerStats.player.Id;

            var playerInDb = await _dbContext.PUBGPlayers.FirstOrDefaultAsync(x => x.DiscordServer.IdDiscord == guidId && x.IdDiscord == guidPlayer);
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
                    playerInDb = new Models.PUBGPlayer()
                    {
                        IdPUBGPlayer = Guid.NewGuid(),
                        IdDiscord = guidPlayer,
                        Nome = playerStats.player.Name,
                        IdDiscordServer = serverInDb.IdDiscordServer,
                        DateUpdated = DateTime.Now,
                        VitoriasSolo = playerStats.stats.GameModeStats.Solo.Wins + playerStats.stats.GameModeStats.SoloFPP.Wins,
                        VitoriasDuo = playerStats.stats.GameModeStats.Duo.Wins + playerStats.stats.GameModeStats.DuoFPP.Wins,
                        VitoriasQuad = playerStats.stats.GameModeStats.Squad.Wins + playerStats.stats.GameModeStats.SquadFPP.Wins,
                        PlayerStatsJSON = Newtonsoft.Json.JsonConvert.SerializeObject(playerStats.stats)
                    };

                    await _dbContext.PUBGPlayers.AddAsync(playerInDb);
                    await _dbContext.SaveChangesAsync();
                }
                else
                {
                    playerInDb.Nome = playerStats.player.Name;
                    playerInDb.DateUpdated = DateTime.Now;
                    playerInDb.VitoriasSolo = playerStats.stats.GameModeStats.Solo.Wins + playerStats.stats.GameModeStats.SoloFPP.Wins;
                    playerInDb.VitoriasDuo = playerStats.stats.GameModeStats.Duo.Wins + playerStats.stats.GameModeStats.DuoFPP.Wins;
                    playerInDb.VitoriasQuad = playerStats.stats.GameModeStats.Squad.Wins + playerStats.stats.GameModeStats.SquadFPP.Wins;
                    playerInDb.PlayerStatsJSON = Newtonsoft.Json.JsonConvert.SerializeObject(playerStats.stats);

                    _dbContext.PUBGPlayers.Update(playerInDb);
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                await RespondAsync($"Erro ao salvar no banco de dados: {ex.Message}");
                return;
            }

            var response = $"Agora estaremos acompanhando a evolução do player {playerStats.player.Name}!";

            await RespondAsync(response.ToString());
        }

        [SlashCommand("pg-untrack", "Para de acompanhar o player")]
        public async Task UntrackUser(string playerName)
        {
            var guidId = Context.Guild.Id;
            var guidPlayer = playerName;

            var playerInDb = await _dbContext.PUBGPlayers.FirstOrDefaultAsync(x => x.DiscordServer.IdDiscord == guidId && x.IdDiscord == guidPlayer);

            if (playerInDb == null)
            {
                await RespondAsync("Player não encontrado ❌");
                return;
            }

            _dbContext.PUBGPlayers.Remove(playerInDb);
            await _dbContext.SaveChangesAsync();

            await RespondAsync($"Player {playerInDb.Nome} removido da lista de acompanhamento ✅");
        }

        [SlashCommand("pg-list", "Lista os players que estão sendo acompanhados")]
        public async Task ListTrackedUsers()
        {
            var guidId = Context.Guild.Id;

            var players = await _dbContext.PUBGPlayers
                .Where(x => x.DiscordServer.IdDiscord == guidId)
                .ToListAsync();

            if (players.Count == 0)
            {
                await RespondAsync("Nenhum player está sendo acompanhado no momento ❌");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Players sendo acompanhados:");
            foreach (var player in players)
            {
                sb.AppendLine($"- {player.Nome}");
            }

            await RespondAsync(sb.ToString());
        }

        // fortinte method:
        /*
         [SlashCommand("fn-update", "Atualiza os dados dos players que estamos acompanhando")]
           public async Task UpdateTrackedUsers()
           {
               var guidId = this.Context.Guild.Id;
           
               var playersInDb = await _dbContext.FortnitePlayers.Where(x => x.DiscordServer.IdDiscord == guidId).ToListAsync();
           
               if (playersInDb == null || playersInDb.Count == 0)
               {
                   await RespondAsync("Não estamos acompanhando nenhum player 😢");
                   return;
               }
           
               await RespondAsync("**Atualizando os dados dos players acompanhados:**");
           
               await Task.Run(async () =>
               {
                   foreach (var player in playersInDb)
                   {
                       var stats = await _fortniteApiService.GetPlayerStaticsCurrentSeasonByPlayerIdAsync(player.IdDiscord);
           
                       if (stats == null)
                       {
                           continue;
                       }
           
                       player.Nome = stats.Account.Name;
                       player.DateUpdated = stats.Stats.All.Overall.LastModified;
           
                       _dbContext.FortnitePlayers.Update(player);
                       await _dbContext.SaveChangesAsync();
                   }
           
                   var channel = this.Context.Guild.GetTextChannel(playersInDb.FirstOrDefault().DiscordServer.IdTextChannel);
           
                   await channel.SendMessageAsync("Dados atualizados com sucesso! 🎉");
               });
           }
         */

        [SlashCommand("pg-update", "Atualiza os dados dos players que estamos acompanhando")]
        public async Task UpdateTrackedUsers()
        {
            var guidId = this.Context.Guild.Id;

            var playersInDb = await _dbContext.PUBGPlayers.Where(x => x.DiscordServer.IdDiscord == guidId).ToListAsync();

            if (playersInDb == null || playersInDb.Count == 0)
            {
                await RespondAsync("Não estamos acompanhando nenhum player 😢");
                return;
            }

            _ = Task.Run(async () =>
            {
                foreach (var player in playersInDb)
                {
                    var stats = await _pubgService.GetPlayerStaticsAsync(player.IdDiscord);

                    if (stats == null)
                    {
                        continue;
                    }

                    player.Nome = stats.player.Name;
                    player.DateUpdated = DateTime.Now;
                    player.VitoriasSolo = stats.stats.GameModeStats.Solo.Wins + stats.stats.GameModeStats.SoloFPP.Wins;
                    player.VitoriasDuo = stats.stats.GameModeStats.Duo.Wins + stats.stats.GameModeStats.DuoFPP.Wins;
                    player.VitoriasQuad = stats.stats.GameModeStats.Squad.Wins + stats.stats.GameModeStats.SquadFPP.Wins;
                    player.PlayerStatsJSON = Newtonsoft.Json.JsonConvert.SerializeObject(stats.stats);

                    _dbContext.PUBGPlayers.Update(player);
                    await _dbContext.SaveChangesAsync();
                }

                var channel = this.Context.Guild.GetTextChannel(playersInDb.FirstOrDefault().DiscordServer.IdTextChannel);

                await channel.SendMessageAsync("Dados atualizados com sucesso! 🎉");
            });

            await RespondAsync("**Atualizando os dados dos players acompanhados:**");
        }

        /*
         [SlashCommand("fn-stats", "Mostra as estatísticas do player")]
           public async Task ShowStats([Summary("player"), Autocomplete(typeof(FortnitePlayerHandler))]string player)
           {
               var seasonStatsJSON = _dbContext.FortnitePlayers.FirstOrDefault(x => x.Nome == player).PlayerStatsJSON;
               var lifetimeStats = await _fortniteApiService.GetPlayerStaticsAllTimeAsync(player);
           
               BrStatsV2V1 seasonStats;
           
               if (seasonStatsJSON == null || seasonStatsJSON == string.Empty)
               {
                   seasonStats = await _fortniteApiService.GetPlayerStaticsCurrentSeasonAsync(player);
               }
               else
               {
                   seasonStats = Newtonsoft.Json.JsonConvert.DeserializeObject<BrStatsV2V1>(seasonStatsJSON);
               }
           
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
         */

        [SlashCommand("pg-stats", "Mostra as estatísticas do player")]
        public async Task Stats([Summary("player")] string player)
        {
            var playerInDb = await _dbContext.PUBGPlayers.FirstOrDefaultAsync(x => x.Nome == player);

            if (playerInDb == null)
            {
                await RespondAsync("Player não encontrado ❌");
                return;
            }

            var stats = Newtonsoft.Json.JsonConvert.DeserializeObject<PubgStatEntity>(playerInDb.PlayerStatsJSON);
            var lifetimeStats = await _pubgService.GetPlayerLifetimeStats(player);

            var response = new StringBuilder();

            response.AppendLine($"**{player}**");

            if (stats == null)
            {
                response.AppendLine("❌ Não foi possível encontrar dados do player na **Season Atual**");
            }
            else
            {
                var totalPartidas = stats.GameModeStats.Solo.Wins 
                                    + stats.GameModeStats.Solo.Losses 
                                    + stats.GameModeStats.Duo.Wins 
                                    + stats.GameModeStats.Duo.Losses 
                                    + stats.GameModeStats.Squad.Wins 
                                    + stats.GameModeStats.Squad.Losses
                                    + stats.GameModeStats.SoloFPP.Wins
                                    + stats.GameModeStats.SoloFPP.Losses
                                    + stats.GameModeStats.DuoFPP.Wins
                                    + stats.GameModeStats.DuoFPP.Losses
                                    + stats.GameModeStats.SquadFPP.Wins
                                    + stats.GameModeStats.SquadFPP.Losses;

                var totalVitorias = stats.GameModeStats.Solo.Wins 
                                    + stats.GameModeStats.Duo.Wins 
                                    + stats.GameModeStats.Squad.Wins
                                    + stats.GameModeStats.SoloFPP.Wins
                                    + stats.GameModeStats.DuoFPP.Wins
                                    + stats.GameModeStats.SquadFPP.Wins;

                var totalLoss = stats.GameModeStats.Solo.Losses 
                                    + stats.GameModeStats.Duo.Losses 
                                    + stats.GameModeStats.Squad.Losses
                                    + stats.GameModeStats.SoloFPP.Losses
                                    + stats.GameModeStats.DuoFPP.Losses
                                    + stats.GameModeStats.SquadFPP.Losses;

                var totalKills = stats.GameModeStats.Solo.Kills 
                                    + stats.GameModeStats.Duo.Kills 
                                    + stats.GameModeStats.Squad.Kills
                                    + stats.GameModeStats.SoloFPP.Kills
                                    + stats.GameModeStats.DuoFPP.Kills
                                    + stats.GameModeStats.SquadFPP.Kills;

                var totalDeaths = stats.GameModeStats.Solo.Losses
                                    + stats.GameModeStats.Duo.Losses
                                    + stats.GameModeStats.Squad.Losses
                                    + stats.GameModeStats.SoloFPP.Losses
                                    + stats.GameModeStats.DuoFPP.Losses
                                    + stats.GameModeStats.SquadFPP.Losses;

                var totalKDRatio = totalDeaths == 0 ? totalKills : totalKills / totalDeaths;

                var totalWLRation = totalLoss == 0 ? totalVitorias : totalVitorias / totalLoss;

                response.AppendLine("**Season Atual**");
                response.AppendLine($"🎮 Partidas: {totalPartidas}");
                response.AppendLine($"🏆 Vitórias Solo: {stats.GameModeStats.Solo.Wins + stats.GameModeStats.SoloFPP.Wins}");
                response.AppendLine($"🏆 Vitórias Duo: {stats.GameModeStats.Duo.Wins + stats.GameModeStats.DuoFPP.Wins}");
                response.AppendLine($"🏆 Vitórias Squad: {stats.GameModeStats.Squad.Wins + stats.GameModeStats.SquadFPP.Wins}");
                response.AppendLine($"📊 W/L: {totalWLRation}");
                response.AppendLine($"💀 Kills: {totalKills}");
                response.AppendLine($"💔 Mortes: {totalDeaths}");
                response.AppendLine($"🔪 K/D: {totalKDRatio}");
                response.AppendLine("");
            }

            if (lifetimeStats == null)
            {
                response.AppendLine("❌ Não foi possível encontrar dados do player fora dessa season");
            }
            else
            {
                var totalPartidas = lifetimeStats.GameModeStats.Solo.Wins 
                                    + lifetimeStats.GameModeStats.Solo.Losses 
                                    + lifetimeStats.GameModeStats.Duo.Wins 
                                    + lifetimeStats.GameModeStats.Duo.Losses 
                                    + lifetimeStats.GameModeStats.Squad.Wins 
                                    + lifetimeStats.GameModeStats.Squad.Losses
                                    + lifetimeStats.GameModeStats.SoloFPP.Wins
                                    + lifetimeStats.GameModeStats.SoloFPP.Losses
                                    + lifetimeStats.GameModeStats.DuoFPP.Wins
                                    + lifetimeStats.GameModeStats.DuoFPP.Losses
                                    + lifetimeStats.GameModeStats.SquadFPP.Wins
                                    + lifetimeStats.GameModeStats.SquadFPP.Losses;

                var totalVitorias = lifetimeStats.GameModeStats.Solo.Wins 
                                    + lifetimeStats.GameModeStats.Duo.Wins 
                                    + lifetimeStats.GameModeStats.Squad.Wins
                                    + lifetimeStats.GameModeStats.SoloFPP.Wins
                                    + lifetimeStats.GameModeStats.DuoFPP.Wins
                                    + lifetimeStats.GameModeStats.SquadFPP.Wins;

                var totalLoss = lifetimeStats.GameModeStats.Solo.Losses 
                                    + lifetimeStats.GameModeStats.Duo.Losses 
                                    + lifetimeStats.GameModeStats.Squad.Losses
                                    + lifetimeStats.GameModeStats.SoloFPP.Losses
                                    + lifetimeStats.GameModeStats.DuoFPP.Losses
                                    + lifetimeStats.GameModeStats.SquadFPP.Losses;

                var totalKills = lifetimeStats.GameModeStats.Solo.Kills 
                                    + lifetimeStats.GameModeStats.Duo.Kills 
                                    + lifetimeStats.GameModeStats.Squad.Kills
                                    + lifetimeStats.GameModeStats.SoloFPP.Kills
                                    + lifetimeStats.GameModeStats.DuoFPP.Kills
                                    + lifetimeStats.GameModeStats.SquadFPP.Kills;

                var totalDeaths = lifetimeStats.GameModeStats.Solo.Losses
                                    + lifetimeStats.GameModeStats.Duo.Losses
                                    + lifetimeStats.GameModeStats.Squad.Losses
                                    + lifetimeStats.GameModeStats.SoloFPP.Losses
                                    + lifetimeStats.GameModeStats.DuoFPP.Losses
                                    + lifetimeStats.GameModeStats.SquadFPP.Losses;

                var totalKDRatio = totalDeaths == 0 ? totalKills : totalKills / totalDeaths;

                var totalWLRation = totalLoss == 0 ? totalVitorias : totalVitorias / totalLoss;

                response.AppendLine("**Geral**");
                response.AppendLine($"🎮 Partidas: {totalPartidas}");
                response.AppendLine($"🏆 Vitórias Solo: {lifetimeStats.GameModeStats.Solo.Wins + lifetimeStats.GameModeStats.SoloFPP.Wins}");
                response.AppendLine($"🏆 Vitórias Duo: {lifetimeStats.GameModeStats.Duo.Wins + lifetimeStats.GameModeStats.DuoFPP.Wins}");
                response.AppendLine($"🏆 Vitórias Squad: {lifetimeStats.GameModeStats.Squad.Wins + lifetimeStats.GameModeStats.SquadFPP.Wins}");
                response.AppendLine($"📊 W/L: {totalWLRation}");
                response.AppendLine($"💀 Kills: {totalKills}");
                response.AppendLine($"💔 Mortes: {totalDeaths}");
                response.AppendLine($"🔪 K/D: {totalKDRatio}");
                response.AppendLine("");
            }

            await RespondAsync(response.ToString());
        }

        [SlashCommand("pg-playersinfo", "Mostra as informações dos players trackeados")]
        public async Task PlayersInfo()
        {
            var playersCount = await _dbContext.PUBGPlayers.CountAsync();

            var serversCount = await _dbContext.DiscordServers.CountAsync();

            var response = $"Há um total de {playersCount} players sendo trackeados em {serversCount} servidores";

            await RespondAsync(response);
        }

    }
}