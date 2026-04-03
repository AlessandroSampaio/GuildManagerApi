using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GuildManagerApi.Api.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyRaiderIoSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_raiderio_run_affixes_raiderio_mythic_runs_mythic_run_id",
                table: "raiderio_run_affixes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_raiderio_run_affixes",
                table: "raiderio_run_affixes");

            migrationBuilder.DropIndex(
                name: "IX_raiderio_run_affixes_mythic_run_id_affix_id",
                table: "raiderio_run_affixes");

            migrationBuilder.DropColumn(
                name: "id",
                table: "raiderio_run_affixes");

            migrationBuilder.DropColumn(
                name: "mythic_run_id",
                table: "raiderio_run_affixes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_raiderio_run_affixes",
                table: "raiderio_run_affixes",
                column: "affix_id");

            migrationBuilder.CreateTable(
                name: "raiderio_run_affix_links",
                columns: table => new
                {
                    AffixesAffixId = table.Column<int>(type: "integer", nullable: false),
                    MythicRunsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raiderio_run_affix_links", x => new { x.AffixesAffixId, x.MythicRunsId });
                    table.ForeignKey(
                        name: "FK_raiderio_run_affix_links_raiderio_mythic_runs_MythicRunsId",
                        column: x => x.MythicRunsId,
                        principalTable: "raiderio_mythic_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_raiderio_run_affix_links_raiderio_run_affixes_AffixesAffixId",
                        column: x => x.AffixesAffixId,
                        principalTable: "raiderio_run_affixes",
                        principalColumn: "affix_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_raiderio_run_affix_links_MythicRunsId",
                table: "raiderio_run_affix_links",
                column: "MythicRunsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "raiderio_run_affix_links");

            migrationBuilder.DropPrimaryKey(
                name: "PK_raiderio_run_affixes",
                table: "raiderio_run_affixes");

            migrationBuilder.AddColumn<int>(
                name: "id",
                table: "raiderio_run_affixes",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "mythic_run_id",
                table: "raiderio_run_affixes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_raiderio_run_affixes",
                table: "raiderio_run_affixes",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_raiderio_run_affixes_mythic_run_id_affix_id",
                table: "raiderio_run_affixes",
                columns: new[] { "mythic_run_id", "affix_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_raiderio_run_affixes_raiderio_mythic_runs_mythic_run_id",
                table: "raiderio_run_affixes",
                column: "mythic_run_id",
                principalTable: "raiderio_mythic_runs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
