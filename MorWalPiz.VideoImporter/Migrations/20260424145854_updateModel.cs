using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MorWalPiz.VideoImporter.Migrations
{
    /// <inheritdoc />
    public partial class updateModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Id",
                keyValue: 1,
                column: "ApiKey",
                value: "OnhQfkZyZxdSTZhIMGQC9gK1xRwU/8vdaBhmXYUjr50=");

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Id",
                keyValue: 2,
                column: "ApiKey",
                value: "OnhQfkZyZxdSTZhIMGQC9gK1xRwU/8vdaBhmXYUjr50=");
        }
    }
}
