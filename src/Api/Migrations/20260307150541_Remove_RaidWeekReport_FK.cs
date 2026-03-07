using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuildManagerApi.Api.Migrations
{
    /// <inheritdoc />
    public partial class Fixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_raid_week_reports_reports_ReportId",
                table: "raid_week_reports");

            migrationBuilder.DropIndex(
                name: "IX_raid_week_reports_ReportId",
                table: "raid_week_reports");

            migrationBuilder.DropColumn(
                name: "ReportId",
                table: "raid_week_reports");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReportId",
                table: "raid_week_reports",
                type: "character varying(16)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_raid_week_reports_ReportId",
                table: "raid_week_reports",
                column: "ReportId");

            migrationBuilder.AddForeignKey(
                name: "FK_raid_week_reports_reports_ReportId",
                table: "raid_week_reports",
                column: "ReportId",
                principalTable: "reports",
                principalColumn: "Id");
        }
    }
}
