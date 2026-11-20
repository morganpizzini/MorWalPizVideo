using Microsoft.EntityFrameworkCore.Migrations;
using MorWalPizVideo.ShootingRange.Repositories;

#nullable disable

namespace MorWalPizVideo.ShootingRange.Persistence.Migrations;

public sealed partial class AddBayTargetsAndDimensions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "targets",
            table: "shooting_range_bays",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<decimal>(
            name: "height",
            table: "shooting_range_bays",
            type: "decimal(18,2)",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "width",
            table: "shooting_range_bays",
            type: "decimal(18,2)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF EXISTS (
                SELECT 1
                FROM [shooting_range_bays]
                WHERE [targets] <> 0 OR [height] IS NOT NULL OR [width] IS NOT NULL
            )
                THROW 51000, 'Shooting Range rollback stopped: bay target or dimension data would be lost.', 1;
            """);

        migrationBuilder.DropColumn(name: "targets", table: "shooting_range_bays");
        migrationBuilder.DropColumn(name: "height", table: "shooting_range_bays");
        migrationBuilder.DropColumn(name: "width", table: "shooting_range_bays");
    }
}
