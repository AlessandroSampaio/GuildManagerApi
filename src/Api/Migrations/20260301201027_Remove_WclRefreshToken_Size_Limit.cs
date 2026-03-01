using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuildManagerApi.Api.Migrations
{
    /// <inheritdoc />
    public partial class Remove_WclRefreshToken_Size_Limit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "WclRefreshToken",
                table: "WclUserTokens",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "WclRefreshToken",
                table: "WclUserTokens",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
