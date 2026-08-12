using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuildManagerApi.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBackupRestoreJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "backup_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backup_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "restore_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_backup_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_file_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    is_upload = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_restore_jobs", x => x.id);
                    table.ForeignKey(
                        name: "FK_restore_jobs_backup_jobs_source_backup_id",
                        column: x => x.source_backup_id,
                        principalTable: "backup_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_backup_jobs_created_at",
                table: "backup_jobs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_backup_jobs_status",
                table: "backup_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_restore_jobs_created_at",
                table: "restore_jobs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_restore_jobs_source_backup_id",
                table: "restore_jobs",
                column: "source_backup_id");

            migrationBuilder.CreateIndex(
                name: "IX_restore_jobs_status",
                table: "restore_jobs",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "restore_jobs");

            migrationBuilder.DropTable(
                name: "backup_jobs");
        }
    }
}
