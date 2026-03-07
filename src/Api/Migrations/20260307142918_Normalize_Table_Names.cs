using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuildManagerApi.Api.Migrations
{
    /// <inheritdoc />
    public partial class Normalize_Table_Names : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Classes_ClassId",
                table: "Characters");

            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Guilds_GuildId",
                table: "Characters");

            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Players_PlayerId",
                table: "Characters");

            migrationBuilder.DropForeignKey(
                name: "FK_Fights_Reports_ReportId",
                table: "Fights");

            migrationBuilder.DropForeignKey(
                name: "FK_PerformanceEntries_Characters_CharacterId",
                table: "PerformanceEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_PerformanceEntries_Fights_FightId",
                table: "PerformanceEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_raid_week_reports_Reports_ReportId",
                table: "raid_week_reports");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_Users_UserId",
                table: "RefreshTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Guilds_GuildId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Specialization_Classes_ClassId",
                table: "Specialization");

            migrationBuilder.DropForeignKey(
                name: "FK_WclUserTokens_Users_UserId",
                table: "WclUserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Reports",
                table: "Reports");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Players",
                table: "Players");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Guilds",
                table: "Guilds");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Fights",
                table: "Fights");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Classes",
                table: "Classes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Characters",
                table: "Characters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WclUserTokens",
                table: "WclUserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Specialization",
                table: "Specialization");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RefreshTokens",
                table: "RefreshTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PerformanceEntries",
                table: "PerformanceEntries");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "users");

            migrationBuilder.RenameTable(
                name: "Reports",
                newName: "reports");

            migrationBuilder.RenameTable(
                name: "Players",
                newName: "players");

            migrationBuilder.RenameTable(
                name: "Guilds",
                newName: "guilds");

            migrationBuilder.RenameTable(
                name: "Fights",
                newName: "fights");

            migrationBuilder.RenameTable(
                name: "Classes",
                newName: "classes");

            migrationBuilder.RenameTable(
                name: "Characters",
                newName: "characters");

            migrationBuilder.RenameTable(
                name: "WclUserTokens",
                newName: "wcl_user_tokens");

            migrationBuilder.RenameTable(
                name: "Specialization",
                newName: "specializations");

            migrationBuilder.RenameTable(
                name: "RefreshTokens",
                newName: "refresh_tokens");

            migrationBuilder.RenameTable(
                name: "PerformanceEntries",
                newName: "performance_entries");

            migrationBuilder.RenameIndex(
                name: "IX_Users_Username",
                table: "users",
                newName: "IX_users_Username");

            migrationBuilder.RenameIndex(
                name: "IX_Users_Email",
                table: "users",
                newName: "IX_users_Email");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_StartTime",
                table: "reports",
                newName: "IX_reports_StartTime");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_ImportStatus",
                table: "reports",
                newName: "IX_reports_ImportStatus");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_GuildId",
                table: "reports",
                newName: "IX_reports_GuildId");

            migrationBuilder.RenameIndex(
                name: "IX_Players_Name",
                table: "players",
                newName: "IX_players_Name");

            migrationBuilder.RenameIndex(
                name: "IX_Guilds_Region",
                table: "guilds",
                newName: "IX_guilds_Region");

            migrationBuilder.RenameIndex(
                name: "IX_Guilds_Name",
                table: "guilds",
                newName: "IX_guilds_Name");

            migrationBuilder.RenameIndex(
                name: "IX_Fights_ReportId_FightIndex",
                table: "fights",
                newName: "IX_fights_ReportId_FightIndex");

            migrationBuilder.RenameIndex(
                name: "IX_Fights_ReportId",
                table: "fights",
                newName: "IX_fights_ReportId");

            migrationBuilder.RenameIndex(
                name: "IX_Characters_WclActorId_Server",
                table: "characters",
                newName: "IX_characters_WclActorId_Server");

            migrationBuilder.RenameIndex(
                name: "IX_Characters_PlayerId",
                table: "characters",
                newName: "IX_characters_PlayerId");

            migrationBuilder.RenameIndex(
                name: "IX_Characters_GuildId",
                table: "characters",
                newName: "IX_characters_GuildId");

            migrationBuilder.RenameIndex(
                name: "IX_Characters_ClassId",
                table: "characters",
                newName: "IX_characters_ClassId");

            migrationBuilder.RenameIndex(
                name: "IX_WclUserTokens_UserId",
                table: "wcl_user_tokens",
                newName: "IX_wcl_user_tokens_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Specialization_ClassId",
                table: "specializations",
                newName: "IX_specializations_ClassId");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_UserId",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_Token",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_Token");

            migrationBuilder.RenameIndex(
                name: "IX_PerformanceEntries_FightId_CharacterId",
                table: "performance_entries",
                newName: "IX_performance_entries_FightId_CharacterId");

            migrationBuilder.RenameIndex(
                name: "IX_PerformanceEntries_FightId",
                table: "performance_entries",
                newName: "IX_performance_entries_FightId");

            migrationBuilder.RenameIndex(
                name: "IX_PerformanceEntries_CharacterId",
                table: "performance_entries",
                newName: "IX_performance_entries_CharacterId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                table: "users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_reports",
                table: "reports",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_players",
                table: "players",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_guilds",
                table: "guilds",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_fights",
                table: "fights",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_classes",
                table: "classes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_characters",
                table: "characters",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_wcl_user_tokens",
                table: "wcl_user_tokens",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_specializations",
                table: "specializations",
                columns: new[] { "Id", "ClassId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_refresh_tokens",
                table: "refresh_tokens",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_performance_entries",
                table: "performance_entries",
                column: "Id");

            migrationBuilder.InsertData(
                table: "classes",
                columns: new[] { "Id", "Name", "SlugName" },
                values: new object[] { 99, "Unknown", "Unknown" });

            migrationBuilder.InsertData(
                table: "specializations",
                columns: new[] { "ClassId", "Id", "Name", "SlugName" },
                values: new object[] { 99, 1, "Unknown", "Unknown" });

            migrationBuilder.AddForeignKey(
                name: "FK_characters_classes_ClassId",
                table: "characters",
                column: "ClassId",
                principalTable: "classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_characters_guilds_GuildId",
                table: "characters",
                column: "GuildId",
                principalTable: "guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_characters_players_PlayerId",
                table: "characters",
                column: "PlayerId",
                principalTable: "players",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_fights_reports_ReportId",
                table: "fights",
                column: "ReportId",
                principalTable: "reports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_performance_entries_characters_CharacterId",
                table: "performance_entries",
                column: "CharacterId",
                principalTable: "characters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_performance_entries_fights_FightId",
                table: "performance_entries",
                column: "FightId",
                principalTable: "fights",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_raid_week_reports_reports_ReportId",
                table: "raid_week_reports",
                column: "ReportId",
                principalTable: "reports",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_tokens_users_UserId",
                table: "refresh_tokens",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_reports_guilds_GuildId",
                table: "reports",
                column: "GuildId",
                principalTable: "guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_specializations_classes_ClassId",
                table: "specializations",
                column: "ClassId",
                principalTable: "classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_wcl_user_tokens_users_UserId",
                table: "wcl_user_tokens",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_characters_classes_ClassId",
                table: "characters");

            migrationBuilder.DropForeignKey(
                name: "FK_characters_guilds_GuildId",
                table: "characters");

            migrationBuilder.DropForeignKey(
                name: "FK_characters_players_PlayerId",
                table: "characters");

            migrationBuilder.DropForeignKey(
                name: "FK_fights_reports_ReportId",
                table: "fights");

            migrationBuilder.DropForeignKey(
                name: "FK_performance_entries_characters_CharacterId",
                table: "performance_entries");

            migrationBuilder.DropForeignKey(
                name: "FK_performance_entries_fights_FightId",
                table: "performance_entries");

            migrationBuilder.DropForeignKey(
                name: "FK_raid_week_reports_reports_ReportId",
                table: "raid_week_reports");

            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_users_UserId",
                table: "refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "FK_reports_guilds_GuildId",
                table: "reports");

            migrationBuilder.DropForeignKey(
                name: "FK_specializations_classes_ClassId",
                table: "specializations");

            migrationBuilder.DropForeignKey(
                name: "FK_wcl_user_tokens_users_UserId",
                table: "wcl_user_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_reports",
                table: "reports");

            migrationBuilder.DropPrimaryKey(
                name: "PK_players",
                table: "players");

            migrationBuilder.DropPrimaryKey(
                name: "PK_guilds",
                table: "guilds");

            migrationBuilder.DropPrimaryKey(
                name: "PK_fights",
                table: "fights");

            migrationBuilder.DropPrimaryKey(
                name: "PK_classes",
                table: "classes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_characters",
                table: "characters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_wcl_user_tokens",
                table: "wcl_user_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_specializations",
                table: "specializations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_refresh_tokens",
                table: "refresh_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_performance_entries",
                table: "performance_entries");

            migrationBuilder.DeleteData(
                table: "specializations",
                keyColumns: new[] { "ClassId", "Id" },
                keyValues: new object[] { 99, 1 });

            migrationBuilder.DeleteData(
                table: "classes",
                keyColumn: "Id",
                keyValue: 99);

            migrationBuilder.RenameTable(
                name: "users",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "reports",
                newName: "Reports");

            migrationBuilder.RenameTable(
                name: "players",
                newName: "Players");

            migrationBuilder.RenameTable(
                name: "guilds",
                newName: "Guilds");

            migrationBuilder.RenameTable(
                name: "fights",
                newName: "Fights");

            migrationBuilder.RenameTable(
                name: "classes",
                newName: "Classes");

            migrationBuilder.RenameTable(
                name: "characters",
                newName: "Characters");

            migrationBuilder.RenameTable(
                name: "wcl_user_tokens",
                newName: "WclUserTokens");

            migrationBuilder.RenameTable(
                name: "specializations",
                newName: "Specialization");

            migrationBuilder.RenameTable(
                name: "refresh_tokens",
                newName: "RefreshTokens");

            migrationBuilder.RenameTable(
                name: "performance_entries",
                newName: "PerformanceEntries");

            migrationBuilder.RenameIndex(
                name: "IX_users_Username",
                table: "Users",
                newName: "IX_Users_Username");

            migrationBuilder.RenameIndex(
                name: "IX_users_Email",
                table: "Users",
                newName: "IX_Users_Email");

            migrationBuilder.RenameIndex(
                name: "IX_reports_StartTime",
                table: "Reports",
                newName: "IX_Reports_StartTime");

            migrationBuilder.RenameIndex(
                name: "IX_reports_ImportStatus",
                table: "Reports",
                newName: "IX_Reports_ImportStatus");

            migrationBuilder.RenameIndex(
                name: "IX_reports_GuildId",
                table: "Reports",
                newName: "IX_Reports_GuildId");

            migrationBuilder.RenameIndex(
                name: "IX_players_Name",
                table: "Players",
                newName: "IX_Players_Name");

            migrationBuilder.RenameIndex(
                name: "IX_guilds_Region",
                table: "Guilds",
                newName: "IX_Guilds_Region");

            migrationBuilder.RenameIndex(
                name: "IX_guilds_Name",
                table: "Guilds",
                newName: "IX_Guilds_Name");

            migrationBuilder.RenameIndex(
                name: "IX_fights_ReportId_FightIndex",
                table: "Fights",
                newName: "IX_Fights_ReportId_FightIndex");

            migrationBuilder.RenameIndex(
                name: "IX_fights_ReportId",
                table: "Fights",
                newName: "IX_Fights_ReportId");

            migrationBuilder.RenameIndex(
                name: "IX_characters_WclActorId_Server",
                table: "Characters",
                newName: "IX_Characters_WclActorId_Server");

            migrationBuilder.RenameIndex(
                name: "IX_characters_PlayerId",
                table: "Characters",
                newName: "IX_Characters_PlayerId");

            migrationBuilder.RenameIndex(
                name: "IX_characters_GuildId",
                table: "Characters",
                newName: "IX_Characters_GuildId");

            migrationBuilder.RenameIndex(
                name: "IX_characters_ClassId",
                table: "Characters",
                newName: "IX_Characters_ClassId");

            migrationBuilder.RenameIndex(
                name: "IX_wcl_user_tokens_UserId",
                table: "WclUserTokens",
                newName: "IX_WclUserTokens_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_specializations_ClassId",
                table: "Specialization",
                newName: "IX_Specialization_ClassId");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_UserId",
                table: "RefreshTokens",
                newName: "IX_RefreshTokens_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_Token",
                table: "RefreshTokens",
                newName: "IX_RefreshTokens_Token");

            migrationBuilder.RenameIndex(
                name: "IX_performance_entries_FightId_CharacterId",
                table: "PerformanceEntries",
                newName: "IX_PerformanceEntries_FightId_CharacterId");

            migrationBuilder.RenameIndex(
                name: "IX_performance_entries_FightId",
                table: "PerformanceEntries",
                newName: "IX_PerformanceEntries_FightId");

            migrationBuilder.RenameIndex(
                name: "IX_performance_entries_CharacterId",
                table: "PerformanceEntries",
                newName: "IX_PerformanceEntries_CharacterId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Reports",
                table: "Reports",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Players",
                table: "Players",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Guilds",
                table: "Guilds",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Fights",
                table: "Fights",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Classes",
                table: "Classes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Characters",
                table: "Characters",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WclUserTokens",
                table: "WclUserTokens",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Specialization",
                table: "Specialization",
                columns: new[] { "Id", "ClassId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_RefreshTokens",
                table: "RefreshTokens",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PerformanceEntries",
                table: "PerformanceEntries",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Classes_ClassId",
                table: "Characters",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Guilds_GuildId",
                table: "Characters",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Players_PlayerId",
                table: "Characters",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Fights_Reports_ReportId",
                table: "Fights",
                column: "ReportId",
                principalTable: "Reports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PerformanceEntries_Characters_CharacterId",
                table: "PerformanceEntries",
                column: "CharacterId",
                principalTable: "Characters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PerformanceEntries_Fights_FightId",
                table: "PerformanceEntries",
                column: "FightId",
                principalTable: "Fights",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_raid_week_reports_Reports_ReportId",
                table: "raid_week_reports",
                column: "ReportId",
                principalTable: "Reports",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_Users_UserId",
                table: "RefreshTokens",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Guilds_GuildId",
                table: "Reports",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Specialization_Classes_ClassId",
                table: "Specialization",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WclUserTokens_Users_UserId",
                table: "WclUserTokens",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
