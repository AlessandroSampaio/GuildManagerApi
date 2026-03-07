using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GuildManagerApi.Api.Migrations
{
    /// <inheritdoc />
    public partial class ScoringSettings_Indentity_Removal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScoringTiers_ScoringSettings_ScoringSettingsId",
                table: "ScoringTiers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ScoringTiers",
                table: "ScoringTiers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ScoringSettings",
                table: "ScoringSettings");

            migrationBuilder.RenameTable(
                name: "ScoringTiers",
                newName: "scoring_tiers");

            migrationBuilder.RenameTable(
                name: "ScoringSettings",
                newName: "scoring_settings");

            migrationBuilder.RenameIndex(
                name: "IX_ScoringTiers_ScoringSettingsId",
                table: "scoring_tiers",
                newName: "IX_scoring_tiers_ScoringSettingsId");

            migrationBuilder.AlterColumn<string>(
                name: "Label",
                table: "scoring_tiers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "scoring_settings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_scoring_tiers",
                table: "scoring_tiers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_scoring_settings",
                table: "scoring_settings",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_scoring_tiers_ScoringSettingsId_MinPercent",
                table: "scoring_tiers",
                columns: new[] { "ScoringSettingsId", "MinPercent" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_scoring_tiers_scoring_settings_ScoringSettingsId",
                table: "scoring_tiers",
                column: "ScoringSettingsId",
                principalTable: "scoring_settings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_scoring_tiers_scoring_settings_ScoringSettingsId",
                table: "scoring_tiers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_scoring_tiers",
                table: "scoring_tiers");

            migrationBuilder.DropIndex(
                name: "IX_scoring_tiers_ScoringSettingsId_MinPercent",
                table: "scoring_tiers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_scoring_settings",
                table: "scoring_settings");

            migrationBuilder.RenameTable(
                name: "scoring_tiers",
                newName: "ScoringTiers");

            migrationBuilder.RenameTable(
                name: "scoring_settings",
                newName: "ScoringSettings");

            migrationBuilder.RenameIndex(
                name: "IX_scoring_tiers_ScoringSettingsId",
                table: "ScoringTiers",
                newName: "IX_ScoringTiers_ScoringSettingsId");

            migrationBuilder.AlterColumn<string>(
                name: "Label",
                table: "ScoringTiers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ScoringSettings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ScoringTiers",
                table: "ScoringTiers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ScoringSettings",
                table: "ScoringSettings",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScoringTiers_ScoringSettings_ScoringSettingsId",
                table: "ScoringTiers",
                column: "ScoringSettingsId",
                principalTable: "ScoringSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
