using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuildManagerApi.Api.Migrations
{
    /// <inheritdoc />
    public partial class Class_Specialization_Unknown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Specialization",
                keyColumns: new[] { "ClassId", "Id" },
                keyValues: new object[] { 99, 1 });

            migrationBuilder.DeleteData(
                table: "Classes",
                keyColumn: "Id",
                keyValue: 99);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Classes",
                columns: new[] { "Id", "Name", "SlugName" },
                values: new object[] { 99, "Unknown", "Unknown" });

            migrationBuilder.InsertData(
                table: "Specialization",
                columns: new[] { "ClassId", "Id", "Name", "SlugName" },
                values: new object[] { 99, 1, "Unknown", "Unknown" });
        }
    }
}
