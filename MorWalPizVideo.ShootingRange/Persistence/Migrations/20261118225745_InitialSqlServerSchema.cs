using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MorWalPizVideo.ShootingRange.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlServerSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shooting_range_bays",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    mechanisms = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    partitions = table.Column<int>(type: "int", nullable: false),
                    plates = table.Column<int>(type: "int", nullable: false),
                    pepper = table.Column<bool>(type: "bit", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    reserved_until_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    whitelist_user_ids = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    creation_date_time = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shooting_range_bays", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shooting_range_bookings",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    user_id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    bay_id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    local_date = table.Column<DateOnly>(type: "date", nullable: false),
                    period_key = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    start_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    request = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    creation_date_time = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shooting_range_bookings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shooting_range_configs",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    time_zone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    session_mode = table.Column<int>(type: "int", nullable: false),
                    opening_days = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    morning_start = table.Column<TimeSpan>(type: "time", nullable: false),
                    morning_end = table.Column<TimeSpan>(type: "time", nullable: false),
                    afternoon_start = table.Column<TimeSpan>(type: "time", nullable: false),
                    afternoon_end = table.Column<TimeSpan>(type: "time", nullable: false),
                    hourly_minutes = table.Column<int>(type: "int", nullable: false),
                    continuous_start = table.Column<TimeSpan>(type: "time", nullable: true),
                    continuous_end = table.Column<TimeSpan>(type: "time", nullable: true),
                    reserved_release_days_before = table.Column<int>(type: "int", nullable: false),
                    creation_date_time = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shooting_range_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shooting_range_exceptions",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    local_date = table.Column<DateOnly>(type: "date", nullable: false),
                    bay_id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    is_closed = table.Column<bool>(type: "bit", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    creation_date_time = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shooting_range_exceptions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shooting_range_local_sessions",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    user_id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    security_version = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    expires_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    revoked = table.Column<bool>(type: "bit", nullable: false),
                    creation_date_time = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shooting_range_local_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shooting_range_mutation_guards",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shooting_range_mutation_guards", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shooting_range_threads",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    user_id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    messages = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    creation_date_time = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shooting_range_threads", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shooting_range_users",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    username = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    normalized_username = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, collation: "Latin1_General_100_BIN2"),
                    first_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    is_admin = table.Column<bool>(type: "bit", nullable: false),
                    force_password_change = table.Column<bool>(type: "bit", nullable: false),
                    security_version = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    creation_date_time = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shooting_range_users", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "shooting_range_mutation_guards",
                column: "id",
                value: 1);

            migrationBuilder.CreateIndex(
                name: "ix_shooting_range_bookings_interval",
                table: "shooting_range_bookings",
                columns: new[] { "bay_id", "start_utc", "end_utc", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_shooting_range_bookings_user_start",
                table: "shooting_range_bookings",
                columns: new[] { "user_id", "start_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_shooting_range_sessions_user_expiry",
                table: "shooting_range_local_sessions",
                columns: new[] { "user_id", "revoked", "expires_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_shooting_range_users_normalized_username",
                table: "shooting_range_users",
                column: "normalized_username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shooting_range_bays");

            migrationBuilder.DropTable(
                name: "shooting_range_bookings");

            migrationBuilder.DropTable(
                name: "shooting_range_configs");

            migrationBuilder.DropTable(
                name: "shooting_range_exceptions");

            migrationBuilder.DropTable(
                name: "shooting_range_local_sessions");

            migrationBuilder.DropTable(
                name: "shooting_range_mutation_guards");

            migrationBuilder.DropTable(
                name: "shooting_range_threads");

            migrationBuilder.DropTable(
                name: "shooting_range_users");
        }
    }
}
