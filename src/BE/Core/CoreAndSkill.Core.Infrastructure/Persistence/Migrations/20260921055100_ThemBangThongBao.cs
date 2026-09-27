using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAndSkill.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ThemBangThongBao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    @params = table.Column<string>(name: "params", type: "jsonb", nullable: true),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "info"),
                    link_route = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    module_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification", x => x.id);
                    table.CheckConstraint("ck_notification_severity", "severity IN ('info','success','warning','error')");
                    table.ForeignKey(
                        name: "fk_notification_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_recipient",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_recipient", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_recipient_notification_id",
                        column: x => x.notification_id,
                        principalSchema: "core",
                        principalTable: "notification",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_notification_recipient_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_recipient_user_id",
                        column: x => x.user_id,
                        principalSchema: "core",
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notification_tenant_created_at",
                schema: "core",
                table: "notification",
                columns: new[] { "tenant_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_notification_recipient_notification_id",
                schema: "core",
                table: "notification_recipient",
                column: "notification_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_recipient_unread",
                schema: "core",
                table: "notification_recipient",
                columns: new[] { "tenant_id", "user_id", "created_at" },
                descending: new[] { false, false, true },
                filter: "read_at IS NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_notification_recipient_user_id",
                schema: "core",
                table: "notification_recipient",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_notification_recipient_tenant_notif_user_active",
                schema: "core",
                table: "notification_recipient",
                columns: new[] { "tenant_id", "notification_id", "user_id" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_recipient",
                schema: "core");

            migrationBuilder.DropTable(
                name: "notification",
                schema: "core");
        }
    }
}
