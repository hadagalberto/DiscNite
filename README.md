# DiscNite

Este é um bot open source para Discord que permite o tracking dos stats de jogadores de Fortnite e também mostra os itens disponíveis na loja do jogo. O bot utiliza as APIs do Fortnite para obter os dados dos jogadores e da loja. Além disso, ele utiliza o Hangfire para agendar tarefas e o SQL Server para armazenar os dados.

## Funcionalidades
* Tracking de Stats: Os usuários podem utilizar comandos para obter informações sobre as estatísticas de jogadores de Fortnite.
* Informações da loja: O bot exibe os itens disponíveis na loja do Fortnite, atualizando automaticamente conforme as mudanças na loja.
* Busca por itens da loja: Os usuários podem buscar por itens específicos na loja do Fortnite.

## Comandos
* /stats [nome do jogador]: Exibe as estatísticas de um jogador de Fortnite.
* /track [nome do jogador]: Adiciona um jogador à lista de jogadores a serem rastreados.
* /untrack [nome do jogador]: Remove um jogador da lista de jogadores a serem rastreados.
* /shop: Exibe os itens disponíveis na loja do Fortnite.
* /itemshop [nome do item]: Exibe informações sobre um item específico da loja do Fortnite.
* /top5: Exibe os cinco jogadores com mais vitórias.
* /update: Atualiza as informações dos jogadores rastreados.
* /help: Exibe a lista de comandos disponíveis.
* /info: Exibe informações sobre o bot.

Mais comandos podem ser adicionados facilmente ao bot.

## Configuração
Para configurar o bot, é necessário configurar o arquivo `_config.json` com as informações necessárias. O arquivo deve conter as seguintes informações:
* `Token`: Token do bot no Discord.
* `FortniteAPIKey`: API Key do Fortnite.

## Instalação
Para instalar o bot, basta clonar o repositório e compilar o projeto. É necessário ter o .NET Core SDK instalado.

Para facilitar a instalação, é possível utilizar o Docker. Basta executar o comando `docker-compose up` na pasta do projeto.
O docker irá criar um container com o SQL Server e outro com o bot.

## Dependências
* Discord.Net
* Fortnite-API-Wrapper
* Hangfire

## Contribuição
Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests.

## Licença
Este projeto está licenciado sob a licença MIT. Veja o arquivo [LICENSE](LICENSE) para mais detalhes.
