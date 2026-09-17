using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MorWalPiz.VideoImporter.Migrations;

public partial class socialPublishing : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "HashtagHistory",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                Value = table.Column<string>(type: "TEXT", nullable: false),
                ChannelId = table.Column<string>(type: "TEXT", nullable: false),
                TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                LastUsedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            }, constraints: table => table.PrimaryKey("PK_HashtagHistory", x => x.Id));
        migrationBuilder.CreateIndex("IX_HashtagHistory_TenantId_ChannelId_Value", "HashtagHistory", new[] { "TenantId", "ChannelId", "Value" }, unique: true);
        migrationBuilder.CreateTable(
            name: "SocialChannelConfigurations",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                ChannelId = table.Column<string>(type: "TEXT", nullable: false),
                Provider = table.Column<int>(type: "INTEGER", nullable: false),
                AccountId = table.Column<string>(type: "TEXT", nullable: false),
                AccessToken = table.Column<string>(type: "TEXT", nullable: false),
                TenantId = table.Column<int>(type: "INTEGER", nullable: false)
            }, constraints: table => table.PrimaryKey("PK_SocialChannelConfigurations", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("SocialChannelConfigurations");
        migrationBuilder.DropTable("HashtagHistory");
    }
}