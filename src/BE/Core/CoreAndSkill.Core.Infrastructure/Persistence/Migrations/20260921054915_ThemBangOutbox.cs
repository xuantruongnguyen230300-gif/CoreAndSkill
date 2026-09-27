using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAndSkill.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ThemBangOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox_message",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    event_type = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    trace_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    triggered_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    triggered_by_user_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_message", x => x.id);
                    table.CheckConstraint("ck_outbox_message_status", "status IN ('pending','dead','done')");
                    table.ForeignKey(
                        name: "fk_outbox_message_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_message_dead",
                schema: "core",
                table: "outbox_message",
                column: "occurred_at",
                filter: "status = 'dead'");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_message_pending",
                schema: "core",
                table: "outbox_message",
                column: "next_attempt_at",
                filter: "status = 'pending'");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_message_tenant_id",
                schema: "core",
                table: "outbox_message",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_message",
                schema: "core");
        }
    }
}
