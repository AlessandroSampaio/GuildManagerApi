using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GuildManagerApi.Api.Migrations
{
    /// <inheritdoc />
    public partial class ScoringSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScoringSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoringSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScoringTiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MinPercent = table.Column<float>(type: "real", nullable: false),
                    MaxPercent = table.Column<float>(type: "real", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    ScoringSettingsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoringTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoringTiers_ScoringSettings_ScoringSettingsId",
                        column: x => x.ScoringSettingsId,
                        principalTable: "ScoringSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScoringTiers_ScoringSettingsId",
                table: "ScoringTiers",
                column: "ScoringSettingsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScoringTiers");

            migrationBuilder.DropTable(
                name: "ScoringSettings");
        }
    }
}
