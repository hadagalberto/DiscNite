using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscNite.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscordServer",
                columns: table => new
                {
                    IdDiscordServer = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdDiscord = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    IdTextChannel = table.Column<decimal>(type: "decimal(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordServer", x => x.IdDiscordServer);
                });

            migrationBuilder.CreateTable(
                name: "FortnitePlayer",
                columns: table => new
                {
                    IdFortnitePlayer = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdDiscord = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdDiscordServer = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Vitorias = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FortnitePlayer", x => x.IdFortnitePlayer);
                    table.ForeignKey(
                        name: "FK_FortnitePlayer_DiscordServer_IdDiscordServer",
                        column: x => x.IdDiscordServer,
                        principalTable: "DiscordServer",
                        principalColumn: "IdDiscordServer",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FortnitePlayer_IdDiscordServer",
                table: "FortnitePlayer",
                column: "IdDiscordServer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FortnitePlayer");

            migrationBuilder.DropTable(
                name: "DiscordServer");
        }
    }
}
