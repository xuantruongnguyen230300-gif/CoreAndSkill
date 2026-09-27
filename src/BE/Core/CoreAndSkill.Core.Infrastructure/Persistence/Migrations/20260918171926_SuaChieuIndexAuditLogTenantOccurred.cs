using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAndSkill.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SuaChieuIndexAuditLogTenantOccurred : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_audit_log_tenant_occurred",
                schema: "core",
                table: "audit_log");

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_tenant_occurred",
                schema: "core",
                table: "audit_log",
                columns: new[] { "tenant_id", "occurred_at" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_audit_log_tenant_occurred",
                schema: "core",
                table: "audit_log");

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_tenant_occurred",
                schema: "core",
                table: "audit_log",
                columns: new[] { "tenant_id", "occurred_at" });
        }
    }
}
