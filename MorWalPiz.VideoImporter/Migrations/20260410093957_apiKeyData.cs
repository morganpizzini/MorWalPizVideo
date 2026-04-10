using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MorWalPiz.VideoImporter.Migrations
{
    /// <inheritdoc />
    public partial class apiKeyData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiKey",
                table: "Settings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Id",
                keyValue: 1,
                column: "ApiKey",
                value: "w5pH-nVupIi6fqUXZC9Fa3IRQ1NGDuQkCSN77o6Y3nw");

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Id",
                keyValue: 2,
                column: "ApiKey",
                value: "w5pH-nVupIi6fqUXZC9Fa3IRQ1NGDuQkCSN77o6Y3nw");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiKey",
                table: "Settings");
        }
    }
}
