using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MorWalPiz.VideoImporter.Data;

#nullable disable

namespace MorWalPiz.VideoImporter.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260918130000_tenantChannel")]
public partial class tenantChannel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>("ChannelId", "Tenants", type: "TEXT", nullable: false, defaultValue: "");

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn("ChannelId", "Tenants");
}