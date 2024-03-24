# DiscNite

Este � um bot open source para Discord que permite o tracking dos stats de jogadores de Fortnite e tamb�m mostra os itens dispon�veis na loja do jogo. O bot utiliza as APIs do Fortnite para obter os dados dos jogadores e da loja. Al�m disso, ele utiliza o Hangfire para agendar tarefas e o SQL Server para armazenar os dados.

## Funcionalidades
* Tracking de Stats: Os usu�rios podem utilizar comandos para obter informa��es sobre as estat�sticas de jogadores de Fortnite.
* Informa��es da loja: O bot exibe os itens dispon�veis na loja do Fortnite, atualizando automaticamente conforme as mudan�as na loja.
* Busca por itens da loja: Os usu�rios podem buscar por itens espec�ficos na loja do Fortnite.

## Comandos
* /stats [nome do jogador]: Exibe as estat�sticas de um jogador de Fortnite.
* /track [nome do jogador]: Adiciona um jogador � lista de jogadores a serem rastreados.
* /untrack [nome do jogador]: Remove um jogador da lista de jogadores a serem rastreados.
* /shop: Exibe os itens dispon�veis na loja do Fortnite.
* /itemshop [nome do item]: Exibe informa��es sobre um item espec�fico da loja do Fortnite.
* /top5: Exibe os cinco jogadores com mais vit�rias.
* /update: Atualiza as informa��es dos jogadores rastreados.
* /help: Exibe a lista de comandos dispon�veis.
* /info: Exibe informa��es sobre o bot.

Mais comandos podem ser adicionados facilmente ao bot.

## Configura��o
Para configurar o bot, � necess�rio configurar o arquivo `_config.json` com as informa��es necess�rias. O arquivo deve conter as seguintes informa��es:
* `Token`: Token do bot no Discord.
* `FortniteAPIKey`: API Key do Fortnite.

## Instala��o
Para instalar o bot, basta clonar o reposit�rio e compilar o projeto. � necess�rio ter o .NET Core SDK instalado.

Para facilitar a instala��o, � poss�vel utilizar o Docker. Basta executar o comando `docker-compose up` na pasta do projeto.
O docker ir� criar um container com o SQL Server e outro com o bot.

## Depend�ncias
* Discord.Net
* Fortnite-API-Wrapper
* Hangfire

## Contribui��o
Contribui��es s�o bem-vindas! Sinta-se � vontade para abrir issues e pull requests.

## Licen�a
Este projeto est� licenciado sob a licen�a MIT. Veja o arquivo [LICENSE](LICENSE) para mais detalhes.
