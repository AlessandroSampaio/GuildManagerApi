using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GuildManagerApi.Api.Migrations
{
    /// <inheritdoc />
    public partial class BattleNet_Auth_Flow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bnet_credentials",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    client_id_encrypted = table.Column<byte[]>(type: "bytea", nullable: false),
                    client_secret_encrypted = table.Column<byte[]>(type: "bytea", nullable: false),
                    label = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bnet_credentials", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bnet_user_tokens",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_token = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_refreshed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    battle_tag = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    sub = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bnet_user_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_bnet_user_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bnet_user_tokens_user_id",
                table: "bnet_user_tokens",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bnet_credentials");

            migrationBuilder.DropTable(
                name: "bnet_user_tokens");
        }
    }
}
