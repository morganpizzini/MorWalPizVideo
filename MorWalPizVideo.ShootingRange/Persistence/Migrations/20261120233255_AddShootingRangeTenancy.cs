using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MorWalPizVideo.ShootingRange.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShootingRangeTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_shooting_range_users_normalized_username",
                table: "shooting_range_users");

            migrationBuilder.DropIndex(
                name: "ux_shooting_range_mechanism_types_normalized_name",
                table: "shooting_range_mechanism_types");

            migrationBuilder.DropIndex(
                name: "ix_shooting_range_sessions_user_expiry",
                table: "shooting_range_local_sessions");

            migrationBuilder.DropIndex(
                name: "ix_shooting_range_bookings_interval",
                table: "shooting_range_bookings");

            migrationBuilder.DropIndex(
                name: "ix_shooting_range_bookings_user_start",
                table: "shooting_range_bookings");

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: "shooting_range_users",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: "shooting_range_threads",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: "shooting_range_mechanism_types",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: "shooting_range_local_sessions",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: "shooting_range_exceptions",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: "shooting_range_configs",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: "shooting_range_bookings",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: "shooting_range_bays",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_shooting_range_users_tenant_id_id",
                table: "shooting_range_users",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_shooting_range_threads_tenant_id_id",
                table: "shooting_range_threads",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_shooting_range_mechanism_types_tenant_id_id",
                table: "shooting_range_mechanism_types",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_shooting_range_exceptions_tenant_id_id",
                table: "shooting_range_exceptions",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_shooting_range_bays_tenant_id_id",
                table: "shooting_range_bays",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateTable(
                name: "shooting_range_tenants",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    slug = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    client_origin = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    creation_date_time = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shooting_range_tenants", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_shooting_range_users_tenant_normalized_username",
                table: "shooting_range_users",
                columns: new[] { "tenant_id", "normalized_username" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shooting_range_threads_tenant_id_user_id",
                table: "shooting_range_threads",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ux_shooting_range_mechanism_types_tenant_normalized_name",
                table: "shooting_range_mechanism_types",
                columns: new[] { "tenant_id", "normalized_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shooting_range_sessions_user_expiry",
                table: "shooting_range_local_sessions",
                columns: new[] { "tenant_id", "user_id", "revoked", "expires_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_shooting_range_exceptions_tenant_id_bay_id",
                table: "shooting_range_exceptions",
                columns: new[] { "tenant_id", "bay_id" });

            migrationBuilder.CreateIndex(
                name: "ux_shooting_range_configs_tenant",
                table: "shooting_range_configs",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shooting_range_bookings_interval",
                table: "shooting_range_bookings",
                columns: new[] { "tenant_id", "bay_id", "start_utc", "end_utc", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_shooting_range_bookings_user_start",
                table: "shooting_range_bookings",
                columns: new[] { "tenant_id", "user_id", "start_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_shooting_range_tenants_client_origin",
                table: "shooting_range_tenants",
                column: "client_origin",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_shooting_range_tenants_slug",
                table: "shooting_range_tenants",
                column: "slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_shooting_range_bays_shooting_range_tenants_tenant_id",
                table: "shooting_range_bays",
                column: "tenant_id",
                principalTable: "shooting_range_tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_shooting_range_bookings_shooting_range_bays_tenant_id_bay_id",
                table: "shooting_range_bookings",
                columns: new[] { "tenant_id", "bay_id" },
                principalTable: "shooting_range_bays",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_shooting_range_bookings_shooting_range_tenants_tenant_id",
                table: "shooting_range_bookings",
                column: "tenant_id",
                principalTable: "shooting_range_tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_shooting_range_bookings_shooting_range_users_tenant_id_user_id",
                table: "shooting_range_bookings",
                columns: new[] { "tenant_id", "user_id" },
                principalTable: "shooting_range_users",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_shooting_range_configs_shooting_range_tenants_tenant_id",
                table: "shooting_range_configs",
                column: "tenant_id",
                principalTable: "shooting_range_tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_shooting_range_exceptions_shooting_range_bays_tenant_id_bay_id",
                table: "shooting_range_exceptions",
                columns: new[] { "tenant_id", "bay_id" },
                principalTable: "shooting_range_bays",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_shooting_range_exceptions_shooting_range_tenants_tenant_id",
                table: "shooting_range_exceptions",
                column: "tenant_id",
                principalTable: "shooting_range_tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_shooting_range_local_sessions_shooting_range_tenants_tenant_id",
                table: "shooting_range_local_sessions",
                column: "tenant_id",
                principalTable: "shooting_range_tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_shooting_range_local_sessions_shooting_range_users_tenant_id_user_id",
                table: "shooting_range_local_sessions",
                columns: new[] { "tenant_id", "user_id" },
                principalTable: "shooting_range_users",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_shooting_range_mechanism_types_shooting_range_tenants_tenant_id",
                table: "shooting_range_mechanism_types",
                column: "tenant_id",
                principalTable: "shooting_range_tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_shooting_range_threads_shooting_range_tenants_tenant_id",
                table: "shooting_range_threads",
                column: "tenant_id",
                principalTable: "shooting_range_tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_shooting_range_threads_shooting_range_users_tenant_id_user_id",
                table: "shooting_range_threads",
                columns: new[] { "tenant_id", "user_id" },
                principalTable: "shooting_range_users",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_shooting_range_users_shooting_range_tenants_tenant_id",
                table: "shooting_range_users",
                column: "tenant_id",
                principalTable: "shooting_range_tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_shooting_range_bays_shooting_range_tenants_tenant_id",
                table: "shooting_range_bays");

            migrationBuilder.DropForeignKey(
                name: "FK_shooting_range_bookings_shooting_range_bays_tenant_id_bay_id",
                table: "shooting_range_bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_shooting_range_bookings_shooting_range_tenants_tenant_id",
                table: "shooting_range_bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_shooting_range_bookings_shooting_range_users_tenant_id_user_id",
                table: "shooting_range_bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_shooting_range_configs_shooting_range_tenants_tenant_id",
                table: "shooting_range_configs");

            migrationBuilder.DropForeignKey(
                name: "FK_shooting_range_exceptions_shooting_range_bays_tenant_id_bay_id",
                table: "shooting_range_exceptions");

            migrationBuilder.DropForeignKey(
                name: "FK_shooting_range_exceptions_shooting_range_tenants_tenant_id",
                table: "shooting_range_exceptions");

            migrationBuilder.DropForeignKey(
                name: "FK_shooting_range_local_sessions_shooting_range_tenants_tenant_id",
                table: "shooting_range_local_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_shooting_range_local_sessions_shooting_range_users_tenant_id_user_id",
                table: "shooting_range_local_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_shooting_range_mechanism_types_shooting_range_tenants_tenant_id",
                table: "shooting_range_mechanism_types");

            migrationBuilder.DropForeignKey(
                name: "FK_shooting_range_threads_shooting_range_tenants_tenant_id",
                table: "shooting_range_threads");

            migrationBuilder.DropForeignKey(
                name: "FK_shooting_range_threads_shooting_range_users_tenant_id_user_id",
                table: "shooting_range_threads");

            migrationBuilder.DropForeignKey(
                name: "FK_shooting_range_users_shooting_range_tenants_tenant_id",
                table: "shooting_range_users");

            migrationBuilder.DropTable(
                name: "shooting_range_tenants");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_shooting_range_users_tenant_id_id",
                table: "shooting_range_users");

            migrationBuilder.DropIndex(
                name: "ux_shooting_range_users_tenant_normalized_username",
                table: "shooting_range_users");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_shooting_range_threads_tenant_id_id",
                table: "shooting_range_threads");

            migrationBuilder.DropIndex(
                name: "IX_shooting_range_threads_tenant_id_user_id",
                table: "shooting_range_threads");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_shooting_range_mechanism_types_tenant_id_id",
                table: "shooting_range_mechanism_types");

            migrationBuilder.DropIndex(
                name: "ux_shooting_range_mechanism_types_tenant_normalized_name",
                table: "shooting_range_mechanism_types");

            migrationBuilder.DropIndex(
                name: "ix_shooting_range_sessions_user_expiry",
                table: "shooting_range_local_sessions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_shooting_range_exceptions_tenant_id_id",
                table: "shooting_range_exceptions");

            migrationBuilder.DropIndex(
                name: "IX_shooting_range_exceptions_tenant_id_bay_id",
                table: "shooting_range_exceptions");

            migrationBuilder.DropIndex(
                name: "ux_shooting_range_configs_tenant",
                table: "shooting_range_configs");

            migrationBuilder.DropIndex(
                name: "ix_shooting_range_bookings_interval",
                table: "shooting_range_bookings");

            migrationBuilder.DropIndex(
                name: "ix_shooting_range_bookings_user_start",
                table: "shooting_range_bookings");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_shooting_range_bays_tenant_id_id",
                table: "shooting_range_bays");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "shooting_range_users");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "shooting_range_threads");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "shooting_range_mechanism_types");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "shooting_range_local_sessions");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "shooting_range_exceptions");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "shooting_range_configs");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "shooting_range_bookings");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "shooting_range_bays");

            migrationBuilder.CreateIndex(
                name: "ux_shooting_range_users_normalized_username",
                table: "shooting_range_users",
                column: "normalized_username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_shooting_range_mechanism_types_normalized_name",
                table: "shooting_range_mechanism_types",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shooting_range_sessions_user_expiry",
                table: "shooting_range_local_sessions",
                columns: new[] { "user_id", "revoked", "expires_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_shooting_range_bookings_interval",
                table: "shooting_range_bookings",
                columns: new[] { "bay_id", "start_utc", "end_utc", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_shooting_range_bookings_user_start",
                table: "shooting_range_bookings",
                columns: new[] { "user_id", "start_utc" });
        }
    }
}
