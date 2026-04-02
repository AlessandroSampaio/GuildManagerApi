using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuildManagerApi.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerAppUserRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "app_user_id",
                table: "players",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_players_app_user_id",
                table: "players",
                column: "app_user_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_players_users_app_user_id",
                table: "players",
                column: "app_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_players_users_app_user_id",
                table: "players");

            migrationBuilder.DropIndex(
                name: "IX_players_app_user_id",
                table: "players");

            migrationBuilder.DropColumn(
                name: "app_user_id",
                table: "players");
        }
    }
}
