using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GuildManagerApi.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "core_id",
                table: "raid_weeks",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cores",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    guild_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cores", x => x.id);
                    table.ForeignKey(
                        name: "FK_cores_guilds_guild_id",
                        column: x => x.guild_id,
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "core_players",
                columns: table => new
                {
                    core_id = table.Column<int>(type: "integer", nullable: false),
                    player_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_core_players", x => new { x.core_id, x.player_id });
                    table.ForeignKey(
                        name: "FK_core_players_cores_core_id",
                        column: x => x.core_id,
                        principalTable: "cores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_core_players_players_player_id",
                        column: x => x.player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_raid_weeks_core_id",
                table: "raid_weeks",
                column: "core_id");

            migrationBuilder.CreateIndex(
                name: "IX_core_players_player_id",
                table: "core_players",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "IX_cores_guild_id",
                table: "cores",
                column: "guild_id");

            migrationBuilder.AddForeignKey(
                name: "FK_raid_weeks_cores_core_id",
                table: "raid_weeks",
                column: "core_id",
                principalTable: "cores",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_raid_weeks_cores_core_id",
                table: "raid_weeks");

            migrationBuilder.DropTable(
                name: "core_players");

            migrationBuilder.DropTable(
                name: "cores");

            migrationBuilder.DropIndex(
                name: "IX_raid_weeks_core_id",
                table: "raid_weeks");

            migrationBuilder.DropColumn(
                name: "core_id",
                table: "raid_weeks");
        }
    }
}
