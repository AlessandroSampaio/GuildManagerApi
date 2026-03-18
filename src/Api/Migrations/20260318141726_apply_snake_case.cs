using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuildManagerApi.Api.Migrations
{
    /// <inheritdoc />
    public partial class apply_snake_case : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                name: "FK_player_week_penalties_penalty_events_PenaltyEventId",
                table: "player_week_penalties");

            migrationBuilder.DropForeignKey(
                name: "FK_player_week_penalties_players_PlayerId",
                table: "player_week_penalties");

            migrationBuilder.DropForeignKey(
                name: "FK_player_week_penalties_raid_weeks_RaidWeekId",
                table: "player_week_penalties");

            migrationBuilder.DropForeignKey(
                name: "FK_raid_week_reports_raid_weeks_RaidWeekId",
                table: "raid_week_reports");

            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_users_UserId",
                table: "refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "FK_reports_guilds_GuildId",
                table: "reports");

            migrationBuilder.DropForeignKey(
                name: "FK_scoring_tiers_scoring_settings_ScoringSettingsId",
                table: "scoring_tiers");

            migrationBuilder.DropForeignKey(
                name: "FK_specializations_classes_ClassId",
                table: "specializations");

            migrationBuilder.DropForeignKey(
                name: "FK_wcl_user_tokens_users_UserId",
                table: "wcl_user_tokens");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "wcl_user_tokens",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "WclRefreshToken",
                table: "wcl_user_tokens",
                newName: "wcl_refresh_token");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "wcl_user_tokens",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "AccessToken",
                table: "wcl_user_tokens",
                newName: "access_token");

            migrationBuilder.RenameIndex(
                name: "IX_wcl_user_tokens_UserId",
                table: "wcl_user_tokens",
                newName: "IX_wcl_user_tokens_user_id");

            migrationBuilder.RenameColumn(
                name: "Label",
                table: "wcl_credentials",
                newName: "label");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "wcl_credentials",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ClientSecretEncrypted",
                table: "wcl_credentials",
                newName: "client_secret_encrypted");

            migrationBuilder.RenameColumn(
                name: "ClientIdEncrypted",
                table: "wcl_credentials",
                newName: "client_id_encrypted");

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "users",
                newName: "username");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "users",
                newName: "role");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "users",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "users",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "users",
                newName: "password_hash");

            migrationBuilder.RenameIndex(
                name: "IX_users_Username",
                table: "users",
                newName: "IX_users_username");

            migrationBuilder.RenameIndex(
                name: "IX_users_Email",
                table: "users",
                newName: "IX_users_email");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "specializations",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "specializations",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "SlugName",
                table: "specializations",
                newName: "slug_name");

            migrationBuilder.RenameColumn(
                name: "ClassId",
                table: "specializations",
                newName: "class_id");

            migrationBuilder.RenameIndex(
                name: "IX_specializations_ClassId",
                table: "specializations",
                newName: "IX_specializations_class_id");

            migrationBuilder.RenameColumn(
                name: "Label",
                table: "scoring_tiers",
                newName: "label");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "scoring_tiers",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ScoringSettingsId",
                table: "scoring_tiers",
                newName: "scoring_settings_id");

            migrationBuilder.RenameColumn(
                name: "MinPercent",
                table: "scoring_tiers",
                newName: "min_percent");

            migrationBuilder.RenameIndex(
                name: "IX_scoring_tiers_ScoringSettingsId_MinPercent",
                table: "scoring_tiers",
                newName: "IX_scoring_tiers_scoring_settings_id_min_percent");

            migrationBuilder.RenameIndex(
                name: "IX_scoring_tiers_ScoringSettingsId",
                table: "scoring_tiers",
                newName: "IX_scoring_tiers_scoring_settings_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "scoring_settings",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "reports",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "reports",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "reports",
                newName: "start_time");

            migrationBuilder.RenameColumn(
                name: "ImportStatus",
                table: "reports",
                newName: "import_status");

            migrationBuilder.RenameColumn(
                name: "ImportError",
                table: "reports",
                newName: "import_error");

            migrationBuilder.RenameColumn(
                name: "GuildId",
                table: "reports",
                newName: "guild_id");

            migrationBuilder.RenameIndex(
                name: "IX_reports_StartTime",
                table: "reports",
                newName: "IX_reports_start_time");

            migrationBuilder.RenameIndex(
                name: "IX_reports_ImportStatus",
                table: "reports",
                newName: "IX_reports_import_status");

            migrationBuilder.RenameIndex(
                name: "IX_reports_GuildId",
                table: "reports",
                newName: "IX_reports_guild_id");

            migrationBuilder.RenameColumn(
                name: "Token",
                table: "refresh_tokens",
                newName: "token");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "refresh_tokens",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "refresh_tokens",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_Token",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_token");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_UserId",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_user_id");

            migrationBuilder.RenameColumn(
                name: "Label",
                table: "raid_weeks",
                newName: "label");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "raid_weeks",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "StartsAt",
                table: "raid_weeks",
                newName: "starts_at");

            migrationBuilder.RenameIndex(
                name: "IX_raid_weeks_StartsAt",
                table: "raid_weeks",
                newName: "IX_raid_weeks_starts_at");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "raid_week_reports",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ReportCode",
                table: "raid_week_reports",
                newName: "report_code");

            migrationBuilder.RenameColumn(
                name: "RaidWeekId",
                table: "raid_week_reports",
                newName: "raid_week_id");

            migrationBuilder.RenameIndex(
                name: "IX_raid_week_reports_RaidWeekId_ReportCode",
                table: "raid_week_reports",
                newName: "IX_raid_week_reports_raid_week_id_report_code");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "players",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "players",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "IX_players_Name",
                table: "players",
                newName: "IX_players_name");

            migrationBuilder.RenameColumn(
                name: "Note",
                table: "player_week_penalties",
                newName: "note");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "player_week_penalties",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "RaidWeekId",
                table: "player_week_penalties",
                newName: "raid_week_id");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "player_week_penalties",
                newName: "player_id");

            migrationBuilder.RenameColumn(
                name: "PenaltyEventId",
                table: "player_week_penalties",
                newName: "penalty_event_id");

            migrationBuilder.RenameIndex(
                name: "IX_player_week_penalties_RaidWeekId_PlayerId_PenaltyEventId",
                table: "player_week_penalties",
                newName: "IX_player_week_penalties_raid_week_id_player_id_penalty_event_~");

            migrationBuilder.RenameIndex(
                name: "IX_player_week_penalties_PlayerId",
                table: "player_week_penalties",
                newName: "IX_player_week_penalties_player_id");

            migrationBuilder.RenameIndex(
                name: "IX_player_week_penalties_PenaltyEventId",
                table: "player_week_penalties",
                newName: "IX_player_week_penalties_penalty_event_id");

            migrationBuilder.RenameColumn(
                name: "Spec",
                table: "performance_entries",
                newName: "spec");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "performance_entries",
                newName: "role");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "performance_entries",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "FightId",
                table: "performance_entries",
                newName: "fight_id");

            migrationBuilder.RenameColumn(
                name: "CharacterId",
                table: "performance_entries",
                newName: "character_id");

            migrationBuilder.RenameIndex(
                name: "IX_performance_entries_FightId_CharacterId",
                table: "performance_entries",
                newName: "IX_performance_entries_fight_id_character_id");

            migrationBuilder.RenameIndex(
                name: "IX_performance_entries_FightId",
                table: "performance_entries",
                newName: "IX_performance_entries_fight_id");

            migrationBuilder.RenameIndex(
                name: "IX_performance_entries_CharacterId",
                table: "performance_entries",
                newName: "IX_performance_entries_character_id");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "penalty_events",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "penalty_events",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Region",
                table: "guilds",
                newName: "region");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "guilds",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "guilds",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "IX_guilds_Region",
                table: "guilds",
                newName: "IX_guilds_region");

            migrationBuilder.RenameIndex(
                name: "IX_guilds_Name",
                table: "guilds",
                newName: "IX_guilds_name");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "fights",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "fights",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ReportId",
                table: "fights",
                newName: "report_id");

            migrationBuilder.RenameColumn(
                name: "FightIndex",
                table: "fights",
                newName: "fight_index");

            migrationBuilder.RenameIndex(
                name: "IX_fights_ReportId_FightIndex",
                table: "fights",
                newName: "IX_fights_report_id_fight_index");

            migrationBuilder.RenameIndex(
                name: "IX_fights_ReportId",
                table: "fights",
                newName: "IX_fights_report_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "classes",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "classes",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "SlugName",
                table: "classes",
                newName: "slug_name");

            migrationBuilder.RenameColumn(
                name: "Server",
                table: "characters",
                newName: "server");

            migrationBuilder.RenameColumn(
                name: "Region",
                table: "characters",
                newName: "region");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "characters",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "characters",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "WclActorId",
                table: "characters",
                newName: "wcl_actor_id");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "characters",
                newName: "player_id");

            migrationBuilder.RenameColumn(
                name: "GuildId",
                table: "characters",
                newName: "guild_id");

            migrationBuilder.RenameColumn(
                name: "ClassId",
                table: "characters",
                newName: "class_id");

            migrationBuilder.RenameIndex(
                name: "IX_characters_WclActorId_Server",
                table: "characters",
                newName: "IX_characters_wcl_actor_id_server");

            migrationBuilder.RenameIndex(
                name: "IX_characters_PlayerId",
                table: "characters",
                newName: "IX_characters_player_id");

            migrationBuilder.RenameIndex(
                name: "IX_characters_GuildId",
                table: "characters",
                newName: "IX_characters_guild_id");

            migrationBuilder.RenameIndex(
                name: "IX_characters_ClassId",
                table: "characters",
                newName: "IX_characters_class_id");

            migrationBuilder.AddForeignKey(
                name: "FK_characters_classes_class_id",
                table: "characters",
                column: "class_id",
                principalTable: "classes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_characters_guilds_guild_id",
                table: "characters",
                column: "guild_id",
                principalTable: "guilds",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_characters_players_player_id",
                table: "characters",
                column: "player_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_fights_reports_report_id",
                table: "fights",
                column: "report_id",
                principalTable: "reports",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_performance_entries_characters_character_id",
                table: "performance_entries",
                column: "character_id",
                principalTable: "characters",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_performance_entries_fights_fight_id",
                table: "performance_entries",
                column: "fight_id",
                principalTable: "fights",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_player_week_penalties_penalty_events_penalty_event_id",
                table: "player_week_penalties",
                column: "penalty_event_id",
                principalTable: "penalty_events",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_week_penalties_players_player_id",
                table: "player_week_penalties",
                column: "player_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_player_week_penalties_raid_weeks_raid_week_id",
                table: "player_week_penalties",
                column: "raid_week_id",
                principalTable: "raid_weeks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_raid_week_reports_raid_weeks_raid_week_id",
                table: "raid_week_reports",
                column: "raid_week_id",
                principalTable: "raid_weeks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_tokens_users_user_id",
                table: "refresh_tokens",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_reports_guilds_guild_id",
                table: "reports",
                column: "guild_id",
                principalTable: "guilds",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_scoring_tiers_scoring_settings_scoring_settings_id",
                table: "scoring_tiers",
                column: "scoring_settings_id",
                principalTable: "scoring_settings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_specializations_classes_class_id",
                table: "specializations",
                column: "class_id",
                principalTable: "classes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_wcl_user_tokens_users_user_id",
                table: "wcl_user_tokens",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_characters_classes_class_id",
                table: "characters");

            migrationBuilder.DropForeignKey(
                name: "FK_characters_guilds_guild_id",
                table: "characters");

            migrationBuilder.DropForeignKey(
                name: "FK_characters_players_player_id",
                table: "characters");

            migrationBuilder.DropForeignKey(
                name: "FK_fights_reports_report_id",
                table: "fights");

            migrationBuilder.DropForeignKey(
                name: "FK_performance_entries_characters_character_id",
                table: "performance_entries");

            migrationBuilder.DropForeignKey(
                name: "FK_performance_entries_fights_fight_id",
                table: "performance_entries");

            migrationBuilder.DropForeignKey(
                name: "FK_player_week_penalties_penalty_events_penalty_event_id",
                table: "player_week_penalties");

            migrationBuilder.DropForeignKey(
                name: "FK_player_week_penalties_players_player_id",
                table: "player_week_penalties");

            migrationBuilder.DropForeignKey(
                name: "FK_player_week_penalties_raid_weeks_raid_week_id",
                table: "player_week_penalties");

            migrationBuilder.DropForeignKey(
                name: "FK_raid_week_reports_raid_weeks_raid_week_id",
                table: "raid_week_reports");

            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_users_user_id",
                table: "refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "FK_reports_guilds_guild_id",
                table: "reports");

            migrationBuilder.DropForeignKey(
                name: "FK_scoring_tiers_scoring_settings_scoring_settings_id",
                table: "scoring_tiers");

            migrationBuilder.DropForeignKey(
                name: "FK_specializations_classes_class_id",
                table: "specializations");

            migrationBuilder.DropForeignKey(
                name: "FK_wcl_user_tokens_users_user_id",
                table: "wcl_user_tokens");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "wcl_user_tokens",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "wcl_refresh_token",
                table: "wcl_user_tokens",
                newName: "WclRefreshToken");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "wcl_user_tokens",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "access_token",
                table: "wcl_user_tokens",
                newName: "AccessToken");

            migrationBuilder.RenameIndex(
                name: "IX_wcl_user_tokens_user_id",
                table: "wcl_user_tokens",
                newName: "IX_wcl_user_tokens_UserId");

            migrationBuilder.RenameColumn(
                name: "label",
                table: "wcl_credentials",
                newName: "Label");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "wcl_credentials",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "client_secret_encrypted",
                table: "wcl_credentials",
                newName: "ClientSecretEncrypted");

            migrationBuilder.RenameColumn(
                name: "client_id_encrypted",
                table: "wcl_credentials",
                newName: "ClientIdEncrypted");

            migrationBuilder.RenameColumn(
                name: "username",
                table: "users",
                newName: "Username");

            migrationBuilder.RenameColumn(
                name: "role",
                table: "users",
                newName: "Role");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "users",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "password_hash",
                table: "users",
                newName: "PasswordHash");

            migrationBuilder.RenameIndex(
                name: "IX_users_username",
                table: "users",
                newName: "IX_users_Username");

            migrationBuilder.RenameIndex(
                name: "IX_users_email",
                table: "users",
                newName: "IX_users_Email");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "specializations",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "specializations",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "slug_name",
                table: "specializations",
                newName: "SlugName");

            migrationBuilder.RenameColumn(
                name: "class_id",
                table: "specializations",
                newName: "ClassId");

            migrationBuilder.RenameIndex(
                name: "IX_specializations_class_id",
                table: "specializations",
                newName: "IX_specializations_ClassId");

            migrationBuilder.RenameColumn(
                name: "label",
                table: "scoring_tiers",
                newName: "Label");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "scoring_tiers",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "scoring_settings_id",
                table: "scoring_tiers",
                newName: "ScoringSettingsId");

            migrationBuilder.RenameColumn(
                name: "min_percent",
                table: "scoring_tiers",
                newName: "MinPercent");

            migrationBuilder.RenameIndex(
                name: "IX_scoring_tiers_scoring_settings_id_min_percent",
                table: "scoring_tiers",
                newName: "IX_scoring_tiers_ScoringSettingsId_MinPercent");

            migrationBuilder.RenameIndex(
                name: "IX_scoring_tiers_scoring_settings_id",
                table: "scoring_tiers",
                newName: "IX_scoring_tiers_ScoringSettingsId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "scoring_settings",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "reports",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "reports",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "start_time",
                table: "reports",
                newName: "StartTime");

            migrationBuilder.RenameColumn(
                name: "import_status",
                table: "reports",
                newName: "ImportStatus");

            migrationBuilder.RenameColumn(
                name: "import_error",
                table: "reports",
                newName: "ImportError");

            migrationBuilder.RenameColumn(
                name: "guild_id",
                table: "reports",
                newName: "GuildId");

            migrationBuilder.RenameIndex(
                name: "IX_reports_start_time",
                table: "reports",
                newName: "IX_reports_StartTime");

            migrationBuilder.RenameIndex(
                name: "IX_reports_import_status",
                table: "reports",
                newName: "IX_reports_ImportStatus");

            migrationBuilder.RenameIndex(
                name: "IX_reports_guild_id",
                table: "reports",
                newName: "IX_reports_GuildId");

            migrationBuilder.RenameColumn(
                name: "token",
                table: "refresh_tokens",
                newName: "Token");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "refresh_tokens",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "refresh_tokens",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_token",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_Token");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_user_id",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_UserId");

            migrationBuilder.RenameColumn(
                name: "label",
                table: "raid_weeks",
                newName: "Label");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "raid_weeks",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "starts_at",
                table: "raid_weeks",
                newName: "StartsAt");

            migrationBuilder.RenameIndex(
                name: "IX_raid_weeks_starts_at",
                table: "raid_weeks",
                newName: "IX_raid_weeks_StartsAt");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "raid_week_reports",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "report_code",
                table: "raid_week_reports",
                newName: "ReportCode");

            migrationBuilder.RenameColumn(
                name: "raid_week_id",
                table: "raid_week_reports",
                newName: "RaidWeekId");

            migrationBuilder.RenameIndex(
                name: "IX_raid_week_reports_raid_week_id_report_code",
                table: "raid_week_reports",
                newName: "IX_raid_week_reports_RaidWeekId_ReportCode");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "players",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "players",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_players_name",
                table: "players",
                newName: "IX_players_Name");

            migrationBuilder.RenameColumn(
                name: "note",
                table: "player_week_penalties",
                newName: "Note");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "player_week_penalties",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "raid_week_id",
                table: "player_week_penalties",
                newName: "RaidWeekId");

            migrationBuilder.RenameColumn(
                name: "player_id",
                table: "player_week_penalties",
                newName: "PlayerId");

            migrationBuilder.RenameColumn(
                name: "penalty_event_id",
                table: "player_week_penalties",
                newName: "PenaltyEventId");

            migrationBuilder.RenameIndex(
                name: "IX_player_week_penalties_raid_week_id_player_id_penalty_event_~",
                table: "player_week_penalties",
                newName: "IX_player_week_penalties_RaidWeekId_PlayerId_PenaltyEventId");

            migrationBuilder.RenameIndex(
                name: "IX_player_week_penalties_player_id",
                table: "player_week_penalties",
                newName: "IX_player_week_penalties_PlayerId");

            migrationBuilder.RenameIndex(
                name: "IX_player_week_penalties_penalty_event_id",
                table: "player_week_penalties",
                newName: "IX_player_week_penalties_PenaltyEventId");

            migrationBuilder.RenameColumn(
                name: "spec",
                table: "performance_entries",
                newName: "Spec");

            migrationBuilder.RenameColumn(
                name: "role",
                table: "performance_entries",
                newName: "Role");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "performance_entries",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "fight_id",
                table: "performance_entries",
                newName: "FightId");

            migrationBuilder.RenameColumn(
                name: "character_id",
                table: "performance_entries",
                newName: "CharacterId");

            migrationBuilder.RenameIndex(
                name: "IX_performance_entries_fight_id_character_id",
                table: "performance_entries",
                newName: "IX_performance_entries_FightId_CharacterId");

            migrationBuilder.RenameIndex(
                name: "IX_performance_entries_fight_id",
                table: "performance_entries",
                newName: "IX_performance_entries_FightId");

            migrationBuilder.RenameIndex(
                name: "IX_performance_entries_character_id",
                table: "performance_entries",
                newName: "IX_performance_entries_CharacterId");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "penalty_events",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "penalty_events",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "region",
                table: "guilds",
                newName: "Region");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "guilds",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "guilds",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_guilds_region",
                table: "guilds",
                newName: "IX_guilds_Region");

            migrationBuilder.RenameIndex(
                name: "IX_guilds_name",
                table: "guilds",
                newName: "IX_guilds_Name");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "fights",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "fights",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "report_id",
                table: "fights",
                newName: "ReportId");

            migrationBuilder.RenameColumn(
                name: "fight_index",
                table: "fights",
                newName: "FightIndex");

            migrationBuilder.RenameIndex(
                name: "IX_fights_report_id_fight_index",
                table: "fights",
                newName: "IX_fights_ReportId_FightIndex");

            migrationBuilder.RenameIndex(
                name: "IX_fights_report_id",
                table: "fights",
                newName: "IX_fights_ReportId");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "classes",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "classes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "slug_name",
                table: "classes",
                newName: "SlugName");

            migrationBuilder.RenameColumn(
                name: "server",
                table: "characters",
                newName: "Server");

            migrationBuilder.RenameColumn(
                name: "region",
                table: "characters",
                newName: "Region");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "characters",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "characters",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "wcl_actor_id",
                table: "characters",
                newName: "WclActorId");

            migrationBuilder.RenameColumn(
                name: "player_id",
                table: "characters",
                newName: "PlayerId");

            migrationBuilder.RenameColumn(
                name: "guild_id",
                table: "characters",
                newName: "GuildId");

            migrationBuilder.RenameColumn(
                name: "class_id",
                table: "characters",
                newName: "ClassId");

            migrationBuilder.RenameIndex(
                name: "IX_characters_wcl_actor_id_server",
                table: "characters",
                newName: "IX_characters_WclActorId_Server");

            migrationBuilder.RenameIndex(
                name: "IX_characters_player_id",
                table: "characters",
                newName: "IX_characters_PlayerId");

            migrationBuilder.RenameIndex(
                name: "IX_characters_guild_id",
                table: "characters",
                newName: "IX_characters_GuildId");

            migrationBuilder.RenameIndex(
                name: "IX_characters_class_id",
                table: "characters",
                newName: "IX_characters_ClassId");

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
                name: "FK_player_week_penalties_penalty_events_PenaltyEventId",
                table: "player_week_penalties",
                column: "PenaltyEventId",
                principalTable: "penalty_events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_player_week_penalties_players_PlayerId",
                table: "player_week_penalties",
                column: "PlayerId",
                principalTable: "players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_player_week_penalties_raid_weeks_RaidWeekId",
                table: "player_week_penalties",
                column: "RaidWeekId",
                principalTable: "raid_weeks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_raid_week_reports_raid_weeks_RaidWeekId",
                table: "raid_week_reports",
                column: "RaidWeekId",
                principalTable: "raid_weeks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
                name: "FK_scoring_tiers_scoring_settings_ScoringSettingsId",
                table: "scoring_tiers",
                column: "ScoringSettingsId",
                principalTable: "scoring_settings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
    }
}
