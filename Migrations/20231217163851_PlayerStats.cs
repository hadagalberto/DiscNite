using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscNite.Migrations
{
    /// <inheritdoc />
    public partial class PlayerStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlayerStatsJSON",
                table: "FortnitePlayer",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlayerStatsJSON",
                table: "FortnitePlayer");
        }
    }
}
