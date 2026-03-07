using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GuildManagerApi.Api.Migrations
{
    /// <inheritdoc />
    public partial class RaidWeek : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "raid_weeks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Label = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raid_weeks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "raid_week_reports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RaidWeekId = table.Column<int>(type: "integer", nullable: false),
                    ReportCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReportId = table.Column<string>(type: "character varying(16)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raid_week_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_raid_week_reports_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_raid_week_reports_raid_weeks_RaidWeekId",
                        column: x => x.RaidWeekId,
                        principalTable: "raid_weeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_raid_week_reports_RaidWeekId_ReportCode",
                table: "raid_week_reports",
                columns: new[] { "RaidWeekId", "ReportCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_raid_week_reports_ReportId",
                table: "raid_week_reports",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_raid_weeks_StartsAt",
                table: "raid_weeks",
                column: "StartsAt",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "raid_week_reports");

            migrationBuilder.DropTable(
                name: "raid_weeks");
        }
    }
}
