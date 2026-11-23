using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MorWalPizVideo.ShootingRange.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShootingRangeAttendanceAndPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_no_show",
                table: "shooting_range_bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "no_show_date",
                table: "shooting_range_bookings",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "minimum_cancellation_days",
                table: "shooting_range_configs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "no_show_rules",
                table: "shooting_range_configs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValueSql: "N'[]'");

            migrationBuilder.AddColumn<int>(
                name: "policy_revision",
                table: "shooting_range_configs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_shooting_range_bookings_attendance_date",
                table: "shooting_range_bookings",
                columns: new[] { "tenant_id", "is_no_show", "no_show_date" });

            migrationBuilder.CreateIndex(
                name: "ix_shooting_range_bookings_local_date",
                table: "shooting_range_bookings",
                columns: new[] { "tenant_id", "local_date", "start_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_shooting_range_bookings_user_attendance",
                table: "shooting_range_bookings",
                columns: new[] { "tenant_id", "user_id", "is_no_show", "local_date", "start_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_shooting_range_bookings_attendance_date",
                table: "shooting_range_bookings");

            migrationBuilder.DropIndex(
                name: "ix_shooting_range_bookings_local_date",
                table: "shooting_range_bookings");

            migrationBuilder.DropIndex(
                name: "ix_shooting_range_bookings_user_attendance",
                table: "shooting_range_bookings");

            migrationBuilder.DropColumn(
                name: "is_no_show",
                table: "shooting_range_bookings");

            migrationBuilder.DropColumn(
                name: "no_show_date",
                table: "shooting_range_bookings");

            migrationBuilder.DropColumn(
                name: "minimum_cancellation_days",
                table: "shooting_range_configs");

            migrationBuilder.DropColumn(
                name: "no_show_rules",
                table: "shooting_range_configs");

            migrationBuilder.DropColumn(
                name: "policy_revision",
                table: "shooting_range_configs");
        }
    }
}
