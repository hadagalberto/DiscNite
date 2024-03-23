﻿using DiscNite.Data;
using DiscNite.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Fortnite_API.Objects.V1;
using Fortnite_API.Objects.V2;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace DiscNite.Commands
{
    public class TrackModule : InteractionModuleBase<SocketInteractionContext>
    {

        private readonly FortniteApiService _fortniteApiService;
        private readonly AppDbContext _dbContext;
        private BrShopV2 BrShop;
        private Dictionary<ulong, int> TrackedShopUser = new Dictionary<ulong, int>();

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
                        Vitorias = stats.Stats.All.Overall.Wins,
                        PlayerStatsJSON = Newtonsoft.Json.JsonConvert.SerializeObject(stats)
                    };

                    await _dbContext.FortnitePlayers.AddAsync(playerInDb);
                    await _dbContext.SaveChangesAsync();
                }
                else
                {
                    playerInDb.Nome = stats.Account.Name;
                    playerInDb.DateUpdated = stats.Stats.All.Overall.LastModified;
                    playerInDb.Vitorias = stats.Stats.All.Overall.Wins;
                    playerInDb.PlayerStatsJSON = Newtonsoft.Json.JsonConvert.SerializeObject(stats);

                    _dbContext.FortnitePlayers.Update(playerInDb);
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                await RespondAsync($"Erro ao salvar no banco de dados: {ex.Message}");
                return;
            }

            var response = $"Agora estaremos acompanhando a evolução do player {stats.Account.Name}!";

            await RespondAsync(response.ToString());
        }

        [SlashCommand("untrack", "Deixa de acompanhar a evolução do player")]
        public async Task UntrackUser(string player)
        {
            var guidId = this.Context.Guild.Id;

            var playerInDb = await _dbContext.FortnitePlayers.FirstOrDefaultAsync(x => x.DiscordServer.IdDiscord == guidId && x.Nome == player);

            if (playerInDb == null)
            {
                await RespondAsync($"Não estamos acompanhando o player {player} ❌");
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

            await RespondAsync($"Não estaremos mais acompanhando a evolução do player {player} 😢");
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

        [SlashCommand("top5all", "Mostra os top 5 jogadores de todos os servidores")]
        public async Task ShowTop5All()
        {
            try
            {
                var topPlayersByServer = await _dbContext.FortnitePlayers
                    .GroupBy(x => x.DiscordServer)
                    .SelectMany(group => group.OrderByDescending(player => player.Vitorias).Take(5))
                    .ToListAsync();

                if (topPlayersByServer.Count == 0)
                {
                    await RespondAsync("Não há jogadores acompanhados para mostrar ❌");
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine("**🏆 Top 5 Jogadores de todos os servidores 🏆**:");

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

        [SlashCommand("shop", "Mostra a loja atual do Fortnite")]
        public async Task ShopAsync()
        {
            if (BrShop == null)
            {
                await RespondAsync("Aguarde um momento enquanto carregamos a loja atual do Fortnite ⏳");

                await Task.Run(async () =>
                {
                    BrShop = await _fortniteApiService.GetShopAsync();
                });
                return;
            }

            if (BrShop.Date.Date != DateTime.Now.Date)
            {
                await RespondAsync("Aguarde um momento enquanto carregamos a loja atual do Fortnite ⏳");

                await Task.Run(async () =>
                {
                    BrShop = await _fortniteApiService.GetShopAsync();
                });
                return;
            }

            var shop = BrShop;

            var itemToShow = shop.Featured.Entries.Take(1).FirstOrDefault();

            // update TrackedShopUser dictionary even if the user is not in the dictionary
            TrackedShopUser[this.Context.User.Id] = 1;

            var builder = new ComponentBuilder()
                .WithButton("Próxima página", "nextShop", ButtonStyle.Primary)
                .WithButton("Página anterior", "previousShop", ButtonStyle.Primary);

            var embed = new EmbedBuilder()
                .WithTitle("Loja do Fortnite")
                .WithDescription(itemToShow.Bundle.Name)
                .WithColor(Color.Blue)
                .WithImageUrl(itemToShow.Bundle.Image.AbsoluteUri)
                .WithFooter("Loja atual do Fortnite")
                .Build();

            await RespondAsync(embeds: new[] { embed }, components: builder.Build());
        }

        [SlashCommand("ping", "Pong!")]
        public async Task PingAsync()
        {
            await RespondAsync("Pong!");
        }

        [SlashCommand("info", "Mostra informações sobre o bot")]
        public async Task InfoAsync()
        {
            var description = "DiscNite é um bot para o Discord que fornece informações sobre o Fortnite";

            description += "\n\n";

            description += "Ele pode mostrar a loja atual do Fortnite, estatísticas de jogadores e muito mais!";

            description += "\n\n";

            description += "Para ver todos os comandos disponíveis, digite /help";

            var embed = new EmbedBuilder()
                .WithTitle("DiscNite")
                .WithDescription(description)
                .WithColor(Color.Blue)
                .WithFooter("DiscNite")
                .Build();

            await RespondAsync(embeds: new[] { embed });
        }

        [SlashCommand("help", "Mostra todos os comandos disponíveis")]
        public async Task HelpAsync()
        {
            var builder = new ComponentBuilder()
                .WithButton("Loja", "shop", ButtonStyle.Primary)
                .WithButton("Estatísticas", "stats", ButtonStyle.Primary);

            var embed = new EmbedBuilder()
                .WithTitle("Comandos disponíveis")
                .WithDescription("Aqui estão todos os comandos disponíveis")
                .WithColor(Color.Blue)
                .WithFooter("Comandos disponíveis")
                .Build();

            await RespondAsync(embeds: new[] { embed }, components: builder.Build());
        }

        [ComponentInteraction("nextShop")]
        public async Task NextShopAsync()
        {
            var shop = BrShop;

            var skip = TrackedShopUser[this.Context.User.Id];
            
            var itemToShow = shop.Featured.Entries.Skip(skip).Take(1).FirstOrDefault();

            TrackedShopUser[this.Context.User.Id] = skip + 1;

            var builder = new ComponentBuilder()
                .WithButton("Próxima página", "nextShop", ButtonStyle.Primary)
                .WithButton("Página anterior", "previousShop", ButtonStyle.Primary);

            var embed = new EmbedBuilder()
                .WithTitle("Loja do Fortnite")
                .WithDescription(itemToShow.Bundle.Name)
                .WithColor(Color.Blue)
                .WithImageUrl(itemToShow.Bundle.Image.AbsoluteUri)
                .WithFooter("Loja atual do Fortnite")
                .Build();

            var response = (SocketMessageComponent)this.Context.Interaction;

            await response.UpdateAsync(msg => msg.Embed = embed);
        }

        [ComponentInteraction("previousShop")]
        public async Task PreviousShopAsync()
        {
            var shop = BrShop;

            var skip = TrackedShopUser[this.Context.User.Id];

            var itemToShow = shop.Featured.Entries.Skip(skip).Take(1).FirstOrDefault();

            TrackedShopUser[this.Context.User.Id] = skip - 1;

            var builder = new ComponentBuilder()
                .WithButton("Próxima página", "nextShop", ButtonStyle.Primary)
                .WithButton("Página anterior", "previousShop", ButtonStyle.Primary);

            var embed = new EmbedBuilder()
                .WithTitle("Loja do Fortnite")
                .WithDescription(itemToShow.Bundle.Name)
                .WithColor(Color.Blue)
                .WithImageUrl(itemToShow.Bundle.Image.AbsoluteUri)
                .WithFooter("Loja atual do Fortnite")
                .Build();

            var response = (SocketMessageComponent)this.Context.Interaction;

            await response.UpdateAsync(msg => msg.Embed = embed);
        }




    }
}
