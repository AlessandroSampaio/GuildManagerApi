using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuildManagerApi.Api.Migrations
{
    /// <inheritdoc />
    public partial class WclActorId_UniqueKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_characters_wcl_actor_id_server",
                table: "characters");

            migrationBuilder.CreateIndex(
                name: "IX_characters_wcl_actor_id",
                table: "characters",
                column: "wcl_actor_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_characters_wcl_actor_id",
                table: "characters");

            migrationBuilder.CreateIndex(
                name: "IX_characters_wcl_actor_id_server",
                table: "characters",
                columns: new[] { "wcl_actor_id", "server" },
                unique: true);
        }
    }
}
