using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GuildManagerApi.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPenaltyEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "penalty_events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Description = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_penalty_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "player_week_penalties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    RaidWeekId = table.Column<int>(type: "integer", nullable: false),
                    PenaltyEventId = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_week_penalties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_player_week_penalties_penalty_events_PenaltyEventId",
                        column: x => x.PenaltyEventId,
                        principalTable: "penalty_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_player_week_penalties_players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_player_week_penalties_raid_weeks_RaidWeekId",
                        column: x => x.RaidWeekId,
                        principalTable: "raid_weeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_week_penalties_PenaltyEventId",
                table: "player_week_penalties",
                column: "PenaltyEventId");

            migrationBuilder.CreateIndex(
                name: "IX_player_week_penalties_PlayerId",
                table: "player_week_penalties",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_player_week_penalties_RaidWeekId_PlayerId_PenaltyEventId",
                table: "player_week_penalties",
                columns: new[] { "RaidWeekId", "PlayerId", "PenaltyEventId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_week_penalties");

            migrationBuilder.DropTable(
                name: "penalty_events");
        }
    }
}
