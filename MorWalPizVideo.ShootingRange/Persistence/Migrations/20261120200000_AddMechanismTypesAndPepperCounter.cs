using Microsoft.EntityFrameworkCore.Migrations;
using MorWalPizVideo.ShootingRange.Repositories;

#nullable disable

namespace MorWalPizVideo.ShootingRange.Persistence.Migrations;

public sealed partial class AddMechanismTypesAndPepperCounter : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF EXISTS (
                SELECT 1
                FROM [shooting_range_bays]
                WHERE ISJSON([mechanisms]) <> 1
                   OR LEFT(LTRIM([mechanisms]), 1) <> '['
            )
                THROW 51000, 'Shooting Range migration stopped: a bay has malformed mechanism data.', 1;

            IF EXISTS (
                SELECT 1
                FROM [shooting_range_bays] AS [bay]
                CROSS APPLY OPENJSON([bay].[mechanisms]) AS [entry]
                CROSS APPLY (SELECT JSON_VALUE([entry].[value], '$.type') AS [name]) AS [mechanism]
                WHERE [mechanism].[name] IS NULL
                   OR [mechanism].[name] = ''
                   OR [mechanism].[name] <> LTRIM(RTRIM([mechanism].[name]))
                   OR LEN([mechanism].[name]) > 80
                   OR TRY_CONVERT(int, JSON_VALUE([entry].[value], '$.quantity')) IS NULL
                   OR TRY_CONVERT(int, JSON_VALUE([entry].[value], '$.quantity')) <= 0
            )
                THROW 51000, 'Shooting Range migration stopped: a bay has an invalid mechanism name or quantity.', 1;

            IF EXISTS (
                SELECT LOWER(LTRIM(RTRIM(JSON_VALUE([entry].[value], '$.type'))))
                FROM [shooting_range_bays] AS [bay]
                CROSS APPLY OPENJSON([bay].[mechanisms]) AS [entry]
                GROUP BY LOWER(LTRIM(RTRIM(JSON_VALUE([entry].[value], '$.type'))))
                HAVING MIN(JSON_VALUE([entry].[value], '$.type') COLLATE Latin1_General_100_BIN2)
                    <> MAX(JSON_VALUE([entry].[value], '$.type') COLLATE Latin1_General_100_BIN2)
            )
                THROW 51000, 'Shooting Range migration stopped: mechanism names differ only by case.', 1;

            IF EXISTS (
                SELECT [bay].[id], LOWER(LTRIM(RTRIM(JSON_VALUE([entry].[value], '$.type'))))
                FROM [shooting_range_bays] AS [bay]
                CROSS APPLY OPENJSON([bay].[mechanisms]) AS [entry]
                GROUP BY [bay].[id], LOWER(LTRIM(RTRIM(JSON_VALUE([entry].[value], '$.type'))))
                HAVING COUNT(*) > 1
            )
                THROW 51000, 'Shooting Range migration stopped: a bay repeats the same mechanism type.', 1;
            """);

        migrationBuilder.CreateTable(
            name: "shooting_range_mechanism_types",
            columns: table => new
            {
                id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                normalized_name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false, collation: "Latin1_General_100_BIN2"),
                creation_date_time = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_shooting_range_mechanism_types", x => x.id));

        migrationBuilder.Sql(
            """
            INSERT INTO [shooting_range_mechanism_types] ([id], [name], [normalized_name], [creation_date_time])
            SELECT CONVERT(nvarchar(32), NEWID(), 2),
                   LTRIM(RTRIM(JSON_VALUE([entry].[value], '$.type'))),
                   LOWER(LTRIM(RTRIM(JSON_VALUE([entry].[value], '$.type')))),
                   SYSUTCDATETIME()
            FROM [shooting_range_bays] AS [bay]
            CROSS APPLY OPENJSON([bay].[mechanisms]) AS [entry]
            GROUP BY LTRIM(RTRIM(JSON_VALUE([entry].[value], '$.type'))),
                     LOWER(LTRIM(RTRIM(JSON_VALUE([entry].[value], '$.type'))));
            """);

        migrationBuilder.CreateIndex(
            name: "ux_shooting_range_mechanism_types_normalized_name",
            table: "shooting_range_mechanism_types",
            column: "normalized_name",
            unique: true);

        migrationBuilder.AlterColumn<int>(
            name: "pepper",
            table: "shooting_range_bays",
            type: "int",
            nullable: false,
            oldClrType: typeof(bool),
            oldType: "bit");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM [shooting_range_bays] WHERE [pepper] NOT IN (0, 1))
                THROW 51000, 'Shooting Range rollback stopped: pepper counts above one cannot be represented as a boolean.', 1;
            """);

        migrationBuilder.DropTable(name: "shooting_range_mechanism_types");

        migrationBuilder.AlterColumn<bool>(
            name: "pepper",
            table: "shooting_range_bays",
            type: "bit",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int");
    }
}
