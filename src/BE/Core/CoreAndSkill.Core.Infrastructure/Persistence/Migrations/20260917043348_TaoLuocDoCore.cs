using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CoreAndSkill.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TaoLuocDoCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "core");

            migrationBuilder.CreateTable(
                name: "data_protection_key",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    friendly_name = table.Column<string>(type: "text", nullable: true),
                    xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_protection_key", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permission_resource",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    module_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permission_resource", x => x.id);
                    table.UniqueConstraint("uq_permission_resource_key", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "tenant",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permission",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    resource_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permission", x => x.id);
                    table.ForeignKey(
                        name: "fk_permission_resource_key",
                        column: x => x.resource_key,
                        principalSchema: "core",
                        principalTable: "permission_resource",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "app_role",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_role", x => x.id);
                    table.ForeignKey(
                        name: "fk_app_role_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "app_user",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    must_change_password = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    preferred_language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    is_system_operator = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    has_permission_bypass = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    locked_by_admin = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_user", x => x.id);
                    table.CheckConstraint("ck_app_user_privilege_flags_exclusive", "NOT (has_permission_bypass AND is_system_operator)");
                    table.ForeignKey(
                        name: "fk_app_user_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "menu_item",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    label_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    route = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    required_permission_id = table.Column<Guid>(type: "uuid", nullable: true),
                    module_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_item", x => x.id);
                    table.CheckConstraint("ck_menu_item_not_self_parent", "parent_id IS NULL OR parent_id <> id");
                    table.ForeignKey(
                        name: "fk_menu_item_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "core",
                        principalTable: "menu_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_menu_item_required_permission_id",
                        column: x => x.required_permission_id,
                        principalSchema: "core",
                        principalTable: "permission",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_menu_item_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "app_role_claim",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_role_claim", x => x.id);
                    table.ForeignKey(
                        name: "fk_app_role_claim_role_id",
                        column: x => x.role_id,
                        principalSchema: "core",
                        principalTable: "app_role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_app_role_claim_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_permission",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permission", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_permission_permission_id",
                        column: x => x.permission_id,
                        principalSchema: "core",
                        principalTable: "permission",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_permission_role_id",
                        column: x => x.role_id,
                        principalSchema: "core",
                        principalTable: "app_role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_permission_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "app_user_claim",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_user_claim", x => x.id);
                    table.ForeignKey(
                        name: "fk_app_user_claim_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_app_user_claim_user_id",
                        column: x => x.user_id,
                        principalSchema: "core",
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "app_user_login",
                schema: "core",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_user_login", x => new { x.tenant_id, x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_app_user_login_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_app_user_login_user_id",
                        column: x => x.user_id,
                        principalSchema: "core",
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "app_user_role",
                schema: "core",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_user_role", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_app_user_role_role_id",
                        column: x => x.role_id,
                        principalSchema: "core",
                        principalTable: "app_role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_app_user_role_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_app_user_role_user_id",
                        column: x => x.user_id,
                        principalSchema: "core",
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "app_user_token",
                schema: "core",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_user_token", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_app_user_token_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_app_user_token_user_id",
                        column: x => x.user_id,
                        principalSchema: "core",
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_item_role",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    menu_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_item_role", x => x.id);
                    table.ForeignKey(
                        name: "fk_menu_item_role_menu_item_id",
                        column: x => x.menu_item_id,
                        principalSchema: "core",
                        principalTable: "menu_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_menu_item_role_role_id",
                        column: x => x.role_id,
                        principalSchema: "core",
                        principalTable: "app_role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_menu_item_role_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "core",
                table: "app_role",
                columns: new[] { "tenant_id", "normalized_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_app_role_claim_role_id",
                schema: "core",
                table: "app_role_claim",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_app_role_claim_tenant_role_id",
                schema: "core",
                table: "app_role_claim",
                columns: new[] { "tenant_id", "role_id" });

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "core",
                table: "app_user",
                columns: new[] { "tenant_id", "normalized_email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "core",
                table: "app_user",
                columns: new[] { "tenant_id", "normalized_user_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_app_user_claim_tenant_user_id",
                schema: "core",
                table: "app_user_claim",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_app_user_claim_user_id",
                schema: "core",
                table: "app_user_claim",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_app_user_login_tenant_user_id",
                schema: "core",
                table: "app_user_login",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_app_user_login_user_id",
                schema: "core",
                table: "app_user_login",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_app_user_role_role_id",
                schema: "core",
                table: "app_user_role",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_app_user_role_tenant_role_id",
                schema: "core",
                table: "app_user_role",
                columns: new[] { "tenant_id", "role_id" });

            migrationBuilder.CreateIndex(
                name: "ix_app_user_token_tenant_id",
                schema: "core",
                table: "app_user_token",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_menu_item_parent_id",
                schema: "core",
                table: "menu_item",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_menu_item_required_permission_id",
                schema: "core",
                table: "menu_item",
                column: "required_permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_menu_item_tenant_parent_order",
                schema: "core",
                table: "menu_item",
                columns: new[] { "tenant_id", "parent_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ux_menu_item_tenant_code_active",
                schema: "core",
                table: "menu_item",
                columns: new[] { "tenant_id", "code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_menu_item_role_menu_item_id",
                schema: "core",
                table: "menu_item_role",
                column: "menu_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_menu_item_role_role_id",
                schema: "core",
                table: "menu_item_role",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_menu_item_role_tenant_role_id",
                schema: "core",
                table: "menu_item_role",
                columns: new[] { "tenant_id", "role_id" });

            migrationBuilder.CreateIndex(
                name: "ux_menu_item_role_tenant_item_role_active",
                schema: "core",
                table: "menu_item_role",
                columns: new[] { "tenant_id", "menu_item_id", "role_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_permission_resource_key_display_order",
                schema: "core",
                table: "permission",
                columns: new[] { "resource_key", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ux_permission_code_active",
                schema: "core",
                table: "permission",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_permission_resource_module_key",
                schema: "core",
                table: "permission_resource",
                column: "module_key");

            migrationBuilder.CreateIndex(
                name: "ix_role_permission_permission_id",
                schema: "core",
                table: "role_permission",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_permission_role_id",
                schema: "core",
                table: "role_permission",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_permission_tenant_perm_role",
                schema: "core",
                table: "role_permission",
                columns: new[] { "tenant_id", "permission_id", "role_id" });

            migrationBuilder.CreateIndex(
                name: "ux_role_permission_tenant_role_perm_active",
                schema: "core",
                table: "role_permission",
                columns: new[] { "tenant_id", "role_id", "permission_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_code",
                schema: "core",
                table: "tenant",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_tenant_is_system",
                schema: "core",
                table: "tenant",
                column: "is_system",
                unique: true,
                filter: "is_system");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_role_claim",
                schema: "core");

            migrationBuilder.DropTable(
                name: "app_user_claim",
                schema: "core");

            migrationBuilder.DropTable(
                name: "app_user_login",
                schema: "core");

            migrationBuilder.DropTable(
                name: "app_user_role",
                schema: "core");

            migrationBuilder.DropTable(
                name: "app_user_token",
                schema: "core");

            migrationBuilder.DropTable(
                name: "data_protection_key",
                schema: "core");

            migrationBuilder.DropTable(
                name: "menu_item_role",
                schema: "core");

            migrationBuilder.DropTable(
                name: "role_permission",
                schema: "core");

            migrationBuilder.DropTable(
                name: "app_user",
                schema: "core");

            migrationBuilder.DropTable(
                name: "menu_item",
                schema: "core");

            migrationBuilder.DropTable(
                name: "app_role",
                schema: "core");

            migrationBuilder.DropTable(
                name: "permission",
                schema: "core");

            migrationBuilder.DropTable(
                name: "tenant",
                schema: "core");

            migrationBuilder.DropTable(
                name: "permission_resource",
                schema: "core");
        }
    }
}
