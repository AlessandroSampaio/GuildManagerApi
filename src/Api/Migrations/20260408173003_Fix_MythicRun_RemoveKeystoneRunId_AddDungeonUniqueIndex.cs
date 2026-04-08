using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuildManagerApi.Api.Migrations
{
    /// <inheritdoc />
    public partial class Fix_MythicRun_RemoveKeystoneRunId_AddDungeonUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_raiderio_mythic_runs_keystone_run_id",
                table: "raiderio_mythic_runs");

            migrationBuilder.DropColumn(
                name: "keystone_run_id",
                table: "raiderio_mythic_runs");

            migrationBuilder.CreateIndex(
                name: "IX_raiderio_mythic_runs_snapshot_id_dungeon",
                table: "raiderio_mythic_runs",
                columns: new[] { "snapshot_id", "dungeon" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_raiderio_mythic_runs_snapshot_id_dungeon",
                table: "raiderio_mythic_runs");

            migrationBuilder.AddColumn<long>(
                name: "keystone_run_id",
                table: "raiderio_mythic_runs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_raiderio_mythic_runs_keystone_run_id",
                table: "raiderio_mythic_runs",
                column: "keystone_run_id",
                unique: true);
        }
    }
}
