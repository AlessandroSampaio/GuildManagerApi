using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GuildManagerApi.Api.Migrations
{
    /// <inheritdoc />
    public partial class Initial_Setup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Class",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SlugName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Class", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Guilds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Server = table.Column<string>(type: "text", nullable: false),
                    Region = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guilds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Email = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Specialization",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    ClassId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SlugName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specialization", x => new { x.Id, x.ClassId });
                    table.ForeignKey(
                        name: "FK_Specialization_Class_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Class",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WclActorId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Server = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Region = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    ClassId = table.Column<int>(type: "integer", nullable: false),
                    GuildId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Characters_Class_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Class",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Characters_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GuildId = table.Column<int>(type: "integer", nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reports_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Fights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FightIndex = table.Column<int>(type: "integer", nullable: false),
                    ReportId = table.Column<string>(type: "character varying(16)", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Kill = table.Column<bool>(type: "boolean", nullable: true),
                    StartTimeMs = table.Column<long>(type: "bigint", nullable: false),
                    EndTimeMs = table.Column<long>(type: "bigint", nullable: false),
                    Difficulty = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fights_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FightId = table.Column<int>(type: "integer", nullable: false),
                    CharacterId = table.Column<int>(type: "integer", nullable: false),
                    Spec = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Amount = table.Column<float>(type: "real", nullable: false),
                    RankPercent = table.Column<float>(type: "real", nullable: true),
                    TotalParses = table.Column<int>(type: "integer", nullable: true),
                    BestPercent = table.Column<float>(type: "real", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformanceEntries_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PerformanceEntries_Fights_FightId",
                        column: x => x.FightId,
                        principalTable: "Fights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Class",
                columns: new[] { "Id", "Name", "SlugName" },
                values: new object[,]
                {
                    { 1, "Death Knight", "DeathKnight" },
                    { 2, "Druid", "Druid" },
                    { 3, "Hunter", "Hunter" },
                    { 4, "Mage", "Mage" },
                    { 5, "Monk", "Monk" },
                    { 6, "Paladin", "Paladin" },
                    { 7, "Priest", "Priest" },
                    { 8, "Rogue", "Rogue" },
                    { 9, "Shaman", "Shaman" },
                    { 10, "Warlock", "Warlock" },
                    { 11, "Warrior", "Warrior" },
                    { 12, "Demon Hunter", "DemonHunter" },
                    { 13, "Evoker", "Evoker" }
                });

            migrationBuilder.InsertData(
                table: "Specialization",
                columns: new[] { "ClassId", "Id", "Name", "SlugName" },
                values: new object[,]
                {
                    { 1, 1, "Blood", "Blood" },
                    { 2, 1, "Balance", "Balance" },
                    { 3, 1, "Beast Mastery", "BeastMastery" },
                    { 4, 1, "Arcane", "Arcane" },
                    { 5, 1, "Brewmaster", "Brewmaster" },
                    { 6, 1, "Holy", "Holy" },
                    { 7, 1, "Discipline", "Discipline" },
                    { 8, 1, "Assassination", "Assassination" },
                    { 9, 1, "Elemental", "Elemental" },
                    { 10, 1, "Affliction", "Affliction" },
                    { 11, 1, "Arms", "Arms" },
                    { 12, 1, "Havoc", "Havoc" },
                    { 13, 1, "Devastation", "Devastation" },
                    { 1, 2, "Frost", "Frost" },
                    { 2, 2, "Feral", "Feral" },
                    { 3, 2, "Marksmanship", "Marksmanship" },
                    { 4, 2, "Fire", "Fire" },
                    { 5, 2, "Mistweaver", "Mistweaver" },
                    { 6, 2, "Protection", "Protection" },
                    { 7, 2, "Holy", "Holy" },
                    { 8, 2, "Subtlety", "Subtlety" },
                    { 9, 2, "Enhancement", "Enhancement" },
                    { 10, 2, "Demonology", "Demonology" },
                    { 11, 2, "Fury", "Fury" },
                    { 12, 2, "Vengeance", "Vengeance" },
                    { 13, 2, "Preservation", "Preservation" },
                    { 1, 3, "Unholy", "Unholy" },
                    { 2, 3, "Guardian", "Guardian" },
                    { 3, 3, "Survival", "Survival" },
                    { 4, 3, "Frost", "Frost" },
                    { 5, 3, "Windwalker", "Windwalker" },
                    { 6, 3, "Retribution", "Retribution" },
                    { 7, 3, "Shadow", "Shadow" },
                    { 8, 3, "Outlaw", "Outlaw" },
                    { 9, 3, "Restoration", "Restoration" },
                    { 10, 3, "Destruction", "Destruction" },
                    { 11, 3, "Protection", "Protection" },
                    { 13, 3, "Augmentation", "Augmentation" },
                    { 2, 4, "Restoration", "Restoration" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_ClassId",
                table: "Characters",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_GuildId",
                table: "Characters",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_WclActorId_Server",
                table: "Characters",
                columns: new[] { "WclActorId", "Server" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fights_ReportId",
                table: "Fights",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_Fights_ReportId_FightIndex",
                table: "Fights",
                columns: new[] { "ReportId", "FightIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_Name",
                table: "Guilds",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_Region",
                table: "Guilds",
                column: "Region");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceEntries_CharacterId",
                table: "PerformanceEntries",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceEntries_FightId",
                table: "PerformanceEntries",
                column: "FightId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceEntries_FightId_CharacterId",
                table: "PerformanceEntries",
                columns: new[] { "FightId", "CharacterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_GuildId",
                table: "Reports",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_StartTime",
                table: "Reports",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_Specialization_ClassId",
                table: "Specialization",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerformanceEntries");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "Specialization");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "Fights");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Class");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "Guilds");
        }
    }
}
