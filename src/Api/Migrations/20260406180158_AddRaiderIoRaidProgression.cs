using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GuildManagerApi.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRaiderIoRaidProgression : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "raiderio_raid_progressions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    snapshot_id = table.Column<int>(type: "integer", nullable: false),
                    raid_slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    summary = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    expansion_id = table.Column<int>(type: "integer", nullable: false),
                    total_bosses = table.Column<int>(type: "integer", nullable: false),
                    normal_bosses_killed = table.Column<int>(type: "integer", nullable: false),
                    heroic_bosses_killed = table.Column<int>(type: "integer", nullable: false),
                    mythic_bosses_killed = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raiderio_raid_progressions", x => x.id);
                    table.ForeignKey(
                        name: "FK_raiderio_raid_progressions_raiderio_character_snapshots_sna~",
                        column: x => x.snapshot_id,
                        principalTable: "raiderio_character_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_raiderio_raid_progressions_snapshot_id_raid_slug",
                table: "raiderio_raid_progressions",
                columns: new[] { "snapshot_id", "raid_slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "raiderio_raid_progressions");
        }
    }
}
