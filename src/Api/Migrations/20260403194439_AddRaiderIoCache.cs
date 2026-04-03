using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GuildManagerApi.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRaiderIoCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "raiderio_character_snapshots",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    realm = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    region = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    thumbnail_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    last_crawled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    cached_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    character_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raiderio_character_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_raiderio_character_snapshots_characters_character_id",
                        column: x => x.character_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "raiderio_mythic_runs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    snapshot_id = table.Column<int>(type: "integer", nullable: false),
                    keystone_run_id = table.Column<long>(type: "bigint", nullable: false),
                    dungeon = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    short_name = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    mythic_level = table.Column<int>(type: "integer", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    score = table.Column<double>(type: "double precision", nullable: false),
                    icon_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    background_image_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raiderio_mythic_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_raiderio_mythic_runs_raiderio_character_snapshots_snapshot_~",
                        column: x => x.snapshot_id,
                        principalTable: "raiderio_character_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "raiderio_run_affixes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    mythic_run_id = table.Column<int>(type: "integer", nullable: false),
                    affix_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    icon_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raiderio_run_affixes", x => x.id);
                    table.ForeignKey(
                        name: "FK_raiderio_run_affixes_raiderio_mythic_runs_mythic_run_id",
                        column: x => x.mythic_run_id,
                        principalTable: "raiderio_mythic_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_raiderio_character_snapshots_character_id",
                table: "raiderio_character_snapshots",
                column: "character_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_raiderio_character_snapshots_name_realm_region",
                table: "raiderio_character_snapshots",
                columns: new[] { "name", "realm", "region" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_raiderio_mythic_runs_keystone_run_id",
                table: "raiderio_mythic_runs",
                column: "keystone_run_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_raiderio_mythic_runs_snapshot_id",
                table: "raiderio_mythic_runs",
                column: "snapshot_id");

            migrationBuilder.CreateIndex(
                name: "IX_raiderio_run_affixes_mythic_run_id_affix_id",
                table: "raiderio_run_affixes",
                columns: new[] { "mythic_run_id", "affix_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "raiderio_run_affixes");

            migrationBuilder.DropTable(
                name: "raiderio_mythic_runs");

            migrationBuilder.DropTable(
                name: "raiderio_character_snapshots");
        }
    }
}
