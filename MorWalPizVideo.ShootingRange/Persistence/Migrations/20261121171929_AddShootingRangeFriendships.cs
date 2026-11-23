using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MorWalPizVideo.ShootingRange.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShootingRangeFriendships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shooting_range_friendships",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    user_a_id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    user_b_id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    requester_user_id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    tenant_id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    creation_date_time = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shooting_range_friendships", x => x.id);
                    table.UniqueConstraint("AK_shooting_range_friendships_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_shooting_range_friendships_canonical_pair",
                        "[user_a_id] COLLATE Latin1_General_100_BIN2 < [user_b_id] COLLATE Latin1_General_100_BIN2");
                    table.CheckConstraint("ck_shooting_range_friendships_requester",
                        "[requester_user_id] COLLATE Latin1_General_100_BIN2 = [user_a_id] COLLATE Latin1_General_100_BIN2 OR [requester_user_id] COLLATE Latin1_General_100_BIN2 = [user_b_id] COLLATE Latin1_General_100_BIN2");
                    table.CheckConstraint("ck_shooting_range_friendships_status", "[status] IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_shooting_range_friendships_shooting_range_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "shooting_range_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shooting_range_friendships_shooting_range_users_tenant_id_user_a_id",
                        columns: x => new { x.tenant_id, x.user_a_id },
                        principalTable: "shooting_range_users",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shooting_range_friendships_shooting_range_users_tenant_id_user_b_id",
                        columns: x => new { x.tenant_id, x.user_b_id },
                        principalTable: "shooting_range_users",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shooting_range_friendships_shooting_range_users_tenant_id_requester_user_id",
                        columns: x => new { x.tenant_id, x.requester_user_id },
                        principalTable: "shooting_range_users",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_shooting_range_friendships_requester",
                table: "shooting_range_friendships",
                columns: new[] { "tenant_id", "requester_user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_shooting_range_friendships_user_b",
                table: "shooting_range_friendships",
                columns: new[] { "tenant_id", "user_b_id" });

            migrationBuilder.CreateIndex(
                name: "ux_shooting_range_friendships_pair",
                table: "shooting_range_friendships",
                columns: new[] { "tenant_id", "user_a_id", "user_b_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM [shooting_range_friendships])
                    THROW 51000, 'Shooting Range rollback stopped: friendship consent state would be lost.', 1;
                """);

            migrationBuilder.DropTable(
                name: "shooting_range_friendships");
        }
    }
}
