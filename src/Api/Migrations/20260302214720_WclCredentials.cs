using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuildManagerApi.Api.Migrations
{
    /// <inheritdoc />
    public partial class WclCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "wcl_credentials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    ClientIdEncrypted = table.Column<byte[]>(type: "bytea", nullable: false),
                    ClientSecretEncrypted = table.Column<byte[]>(type: "bytea", nullable: false),
                    Label = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wcl_credentials", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "wcl_credentials");
        }
    }
}
