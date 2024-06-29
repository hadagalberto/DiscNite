using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscNite.Migrations
{
    /// <inheritdoc />
    public partial class PUBG : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PUBGPlayer",
                columns: table => new
                {
                    IdPUBGPlayer = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdDiscord = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdDiscordServer = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VitoriasQuad = table.Column<long>(type: "bigint", nullable: false),
                    VitoriasDuo = table.Column<long>(type: "bigint", nullable: false),
                    VitoriasSolo = table.Column<long>(type: "bigint", nullable: false),
                    PlayerStatsJSON = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PUBGPlayer", x => x.IdPUBGPlayer);
                    table.ForeignKey(
                        name: "FK_PUBGPlayer_DiscordServer_IdDiscordServer",
                        column: x => x.IdDiscordServer,
                        principalTable: "DiscordServer",
                        principalColumn: "IdDiscordServer",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PUBGPlayer_IdDiscordServer",
                table: "PUBGPlayer",
                column: "IdDiscordServer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PUBGPlayer");
        }
    }
}
