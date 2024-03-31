using DiscNite.Data;
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
        private static BrShopV2 BrShop;
        private static Dictionary<ulong, int> TrackedShopUser = new Dictionary<ulong, int>();

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

            TrackedShopUser[this.Context.User.Id] = 1;

            var bundle = itemToShow.Bundle;

            var builder = new ComponentBuilder()
                .WithButton("Página anterior", "previousShop", ButtonStyle.Primary)
                .WithButton("Próxima página", "nextShop", ButtonStyle.Primary);

            if (bundle != null)
            {
                var embed = new EmbedBuilder()
                    .WithTitle("Loja do Fortnite")
                    .WithDescription(itemToShow.Bundle.Name + " | " + itemToShow.FinalPrice + "V-Bucks")
                    .WithColor(Color.Blue)
                    .WithImageUrl(itemToShow.Bundle.Image.AbsoluteUri)
                    .WithFooter("DiscNite")
                    .Build();

                await RespondAsync(embeds: new[] { embed }, components: builder.Build());
            }
            else
            {
                var items = itemToShow.Items.ToList();

                List<Embed> embeds = new List<Embed>();

                var item = items.FirstOrDefault();
                var embed = new EmbedBuilder()
                        .WithTitle("Loja do Fortnite")
                        .WithDescription(item.Name + " | " + itemToShow.FinalPrice + " V-Bucks")
                        .WithImageUrl(item.Images.Featured?.AbsoluteUri ?? item.Images.Other?.FirstOrDefault().Value.AbsoluteUri ?? item.Images.Icon?.AbsoluteUri)
                        .WithColor(Color.Blue)
                        .WithFooter("DiscNite")
                        .Build();

                embeds.Add(embed);

                foreach (var itemE in items.Skip(1))
                {
                    var embedE = new EmbedBuilder()
                        .WithImageUrl(itemE.Images.Featured?.AbsoluteUri ?? itemE.Images.Other?.FirstOrDefault().Value.AbsoluteUri ?? itemE.Images.Icon?.AbsoluteUri)
                        .WithColor(Color.Blue)
                        .WithFooter("DiscNite")
                        .Build();

                    embeds.Add(embedE);
                }

                var response = (SocketMessageComponent)this.Context.Interaction;

                await response.UpdateAsync(msg => msg.Embeds = embeds.ToArray());

            }

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

        [SlashCommand("itemshop", "Busca um item na loja pelo nome")]
        public async Task ItemShop(string item)
        {
            var shop = BrShop;

            if (shop == null)
            {
                await RespondAsync("Aguarde um momento enquanto carregamos a loja atual do Fortnite ⏳");

                await Task.Run(async () =>
                {
                    BrShop = await _fortniteApiService.GetShopAsync();
                });
                return;
            }

            if (shop.Date.Date != DateTime.Now.Date)
            {
                await RespondAsync("Aguarde um momento enquanto carregamos a loja atual do Fortnite ⏳");

                await Task.Run(async () =>
                {
                    BrShop = await _fortniteApiService.GetShopAsync();
                });
                return;
            }

            var itemToShow = shop.Featured.Entries.FirstOrDefault(x => x.Bundle?.Name.Contains(item) == true || x.Items.Any(y => y.Name.Contains(item)));

            if (itemToShow == null)
            {
                await RespondAsync("Não foi possível encontrar o item na loja ❌");
                return;
            }

            var bundle = itemToShow.Bundle;

            if (bundle != null)
            {
                var embed = new EmbedBuilder()
                    .WithTitle("Loja do Fortnite")
                    .WithDescription(itemToShow.Bundle.Name + " | " + itemToShow.FinalPrice + "V-Bucks")
                    .WithColor(Color.Blue)
                    .WithImageUrl(itemToShow.Bundle.Image.AbsoluteUri)
                    .WithFooter("DiscNite")
                    .Build();

                await RespondAsync(embeds: new[] {embed});
            }
            else
            {
                var items = itemToShow.Items.ToList();

                List<Embed> embeds = new List<Embed>();

                var itemE = items.FirstOrDefault();
                var embed = new EmbedBuilder()
                        .WithTitle("Loja do Fortnite")
                        .WithDescription(itemE.Name + " | " + itemToShow.FinalPrice + " V-Bucks")
                        .WithImageUrl(itemE.Images.Featured?.AbsoluteUri ?? itemE.Images.Other?.FirstOrDefault().Value.AbsoluteUri ?? itemE.Images.Icon?.AbsoluteUri)
                        .WithColor(Color.Blue)
                        .WithFooter("DiscNite")
                        .Build();

                embeds.Add(embed);

                foreach (var itemEE in items.Skip(1))
                {
                    var embedE = new EmbedBuilder()
                        .WithImageUrl(itemEE.Images.Featured?.AbsoluteUri ?? itemEE.Images.Other?.FirstOrDefault().Value.AbsoluteUri ?? itemEE.Images.Icon?.AbsoluteUri)
                        .WithColor(Color.Blue)
                        .WithFooter("DiscNite")
                        .Build();

                    embeds.Add(embedE);
                }

                await RespondAsync(embeds: embeds.ToArray());
            }
        }

        [SlashCommand("playersinfo", "Mostra as informações dos players trackeados")]
        public async Task PlayersInfo()
        {
            var playersCount = await _dbContext.FortnitePlayers.CountAsync();

            var serversCount = await _dbContext.DiscordServers.CountAsync();

            var response = $"Há um total de {playersCount} players sendo trackeados em {serversCount} servidores";

            await RespondAsync(response);
        }

        [ComponentInteraction("nextShop")]
        public async Task NextShopAsync()
        {
            var shop = BrShop;

            var skip = TrackedShopUser.ContainsKey(this.Context.User.Id) ? TrackedShopUser[this.Context.User.Id] : 0;
            
            var itemToShow = shop.Featured.Entries.Skip(skip).Take(1).FirstOrDefault();

            TrackedShopUser[this.Context.User.Id] = skip + 1;

            var bundle = itemToShow.Bundle;

            if (bundle != null)
            {
                var embed = new EmbedBuilder()
                    .WithTitle("Loja do Fortnite")
                    .WithDescription(itemToShow.Bundle.Name + " | " + itemToShow.FinalPrice + "V-Bucks")
                    .WithColor(Color.Blue)
                    .WithImageUrl(itemToShow.Bundle.Image.AbsoluteUri)
                    .WithFooter("DiscNite")
                    .Build();

                var response = (SocketMessageComponent)this.Context.Interaction;

                await response.UpdateAsync(msg => msg.Embed = embed);
            } 
            else
            {
                var items = itemToShow.Items.ToList();

                List<Embed> embeds = new List<Embed>();

                var item = items.FirstOrDefault();
                var embed = new EmbedBuilder()
                        .WithTitle("Loja do Fortnite")
                        .WithDescription(item.Name + " | " + itemToShow.FinalPrice + " V-Bucks")
                        .WithImageUrl(item.Images.Featured?.AbsoluteUri ?? item.Images.Other?.FirstOrDefault().Value.AbsoluteUri ?? item.Images.Icon?.AbsoluteUri)
                        .WithColor(Color.Blue)
                        .WithFooter("DiscNite")
                        .Build();

                embeds.Add(embed);

                foreach (var itemE in items.Skip(1))
                {
                    var embedE = new EmbedBuilder()
                        .WithImageUrl(itemE.Images.Featured?.AbsoluteUri ?? itemE.Images.Other?.FirstOrDefault().Value.AbsoluteUri ?? itemE.Images.Icon?.AbsoluteUri)
                        .WithColor(Color.Blue)
                        .WithFooter("DiscNite")
                        .Build();

                    embeds.Add(embedE);
                }

                var response = (SocketMessageComponent)this.Context.Interaction;

                await response.UpdateAsync(msg => msg.Embeds = embeds.ToArray());

            }
        }

        [ComponentInteraction("previousShop")]
        public async Task PreviousShopAsync()
        {
            var shop = BrShop;

            var skip = TrackedShopUser.ContainsKey(this.Context.User.Id) ? TrackedShopUser[this.Context.User.Id] : 0;

            if (skip < 0)
            {
                await RespondAsync("Não há mais itens para mostrar ❌");
                return;
            }

            TrackedShopUser[this.Context.User.Id] = skip - 1;

            var itemToShow = shop.Featured.Entries.Skip(skip).Take(1).FirstOrDefault();


            var bundle = itemToShow.Bundle;

            if (bundle != null)
            {
                var embed = new EmbedBuilder()
                    .WithTitle("Loja do Fortnite")
                    .WithDescription(itemToShow.Bundle.Name + " | " + itemToShow.FinalPrice + "V-Bucks")
                    .WithColor(Color.Blue)
                    .WithImageUrl(itemToShow.Bundle.Image.AbsoluteUri)
                    .WithFooter("DiscNite")
                    .Build();

                var response = (SocketMessageComponent)this.Context.Interaction;

                await response.UpdateAsync(msg => msg.Embed = embed);
            }
            else
            {
                var items = itemToShow.Items.ToList();

                List<Embed> embeds = new List<Embed>();

                var item = items.FirstOrDefault();
                var embed = new EmbedBuilder()
                        .WithTitle("Loja do Fortnite")
                        .WithDescription(item.Name + " | " + itemToShow.FinalPrice + " V-Bucks")
                        .WithImageUrl(item.Images.Featured?.AbsoluteUri ?? item.Images.Other?.FirstOrDefault().Value.AbsoluteUri ?? item.Images.Icon?.AbsoluteUri)
                        .WithColor(Color.Blue)
                        .WithFooter("DiscNite")
                        .Build();

                embeds.Add(embed);

                foreach (var itemE in items.Skip(1))
                {
                    var embedE = new EmbedBuilder()
                        .WithImageUrl(itemE.Images.Featured?.AbsoluteUri ?? itemE.Images.Other?.FirstOrDefault().Value.AbsoluteUri ?? itemE.Images.Icon?.AbsoluteUri)
                        .WithColor(Color.Blue)
                        .WithFooter("DiscNite")
                        .Build();

                    embeds.Add(embedE);
                }

                var response = (SocketMessageComponent)this.Context.Interaction;

                await response.UpdateAsync(msg => msg.Embeds = embeds.ToArray());

            }
        }

    }
}
