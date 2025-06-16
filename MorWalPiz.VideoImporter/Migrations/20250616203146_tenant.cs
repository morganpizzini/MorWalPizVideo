using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MorWalPiz.VideoImporter.Migrations
{
    /// <inheritdoc />
    public partial class tenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Languages_IsDefault",
                table: "Languages");

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PublishSchedules",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Languages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Disclaimers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: 1,
                column: "TenantId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: 2,
                column: "TenantId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: 3,
                column: "TenantId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: 4,
                column: "TenantId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "Id",
                keyValue: 5,
                column: "TenantId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "PublishSchedules",
                keyColumn: "Id",
                keyValue: 1,
                column: "TenantId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "PublishSchedules",
                keyColumn: "Id",
                keyValue: 2,
                column: "TenantId",
                value: 1);

            migrationBuilder.InsertData(
                table: "PublishSchedules",
                columns: new[] { "Id", "CreatedDate", "DaysOfWeek", "IsActive", "Name", "PublishTime", "TenantId" },
                values: new object[] { 3, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 31, true, "Giorni feriali 1", new TimeSpan(0, 12, 0, 0, 0), 1 });

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Id",
                keyValue: 1,
                column: "TenantId",
                value: 1);

            migrationBuilder.InsertData(
                table: "Tenants",
                columns: new[] { "Id", "CreatedDate", "IsActive", "Name" },
                values: new object[] { 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "MorWalPiz" });

            migrationBuilder.CreateIndex(
                name: "IX_Languages_IsDefault_TenantId",
                table: "Languages",
                columns: new[] { "IsDefault", "TenantId" },
                unique: true,
                filter: "[IsDefault] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Languages_IsDefault_TenantId",
                table: "Languages");

            migrationBuilder.DeleteData(
                table: "PublishSchedules",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PublishSchedules");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Languages");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Disclaimers");

            migrationBuilder.CreateIndex(
                name: "IX_Languages_IsDefault",
                table: "Languages",
                column: "IsDefault",
                unique: true,
                filter: "[IsDefault] = 1");
        }
    }
}
