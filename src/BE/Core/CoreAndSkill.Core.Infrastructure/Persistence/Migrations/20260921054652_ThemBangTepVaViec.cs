using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAndSkill.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ThemBangTepVaViec : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "file",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_table = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    original_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    purpose = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_file", x => x.id);
                    table.ForeignKey(
                        name: "fk_file_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "job",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "queued"),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    progress = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    result = table.Column<string>(type: "jsonb", nullable: true),
                    error = table.Column<string>(type: "jsonb", nullable: true),
                    result_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job", x => x.id);
                    table.CheckConstraint("ck_job_progress", "progress BETWEEN 0 AND 100");
                    table.CheckConstraint("ck_job_status", "status IN ('queued','running','succeeded','failed','cancelled')");
                    table.ForeignKey(
                        name: "fk_job_result_file_id",
                        column: x => x.result_file_id,
                        principalSchema: "core",
                        principalTable: "file",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_job_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_file_owner",
                schema: "core",
                table: "file",
                columns: new[] { "tenant_id", "owner_table", "owner_id" });

            migrationBuilder.CreateIndex(
                name: "ux_file_tenant_storage_key_active",
                schema: "core",
                table: "file",
                columns: new[] { "tenant_id", "storage_key" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_job_result_file_id",
                schema: "core",
                table: "job",
                column: "result_file_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_tenant_created_at",
                schema: "core",
                table: "job",
                columns: new[] { "tenant_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "job",
                schema: "core");

            migrationBuilder.DropTable(
                name: "file",
                schema: "core");
        }
    }
}
