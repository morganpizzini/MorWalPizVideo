using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MorWalPizVideo.ShootingRange.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShootingRangeMessageStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "shooting_range_threads",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM [shooting_range_threads]
                    WHERE [status] <> 0
                )
                    THROW 51000, 'Shooting Range rollback stopped: message status decisions would be lost.', 1;
                """);

            migrationBuilder.DropColumn(
                name: "status",
                table: "shooting_range_threads");
        }
    }
}
