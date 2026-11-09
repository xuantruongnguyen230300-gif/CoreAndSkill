using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAndSkill.Core.Infrastructure.Persistence.Migrations
{
    // ADR-0119 quyết định 1-3: collation ICU `core.vi_vn`, extension `unaccent` ở schema `public`, ba cột tên mang collation.
    // AlterDatabase sinh CREATE COLLATION và CREATE EXTENSION. Ba lệnh ALTER COLUMN viết tay vì EF bọc cả "core.vi_vn"
    // thành MỘT định danh có dấu nháy kép (COLLATE "core.vi_vn") — Postgres tìm collation tên đó trong search_path và
    // không thấy; phải là COLLATE core.vi_vn. Kiểu và độ dài khớp model (full_name 200, tenant.name 200, app_role.name 256).
    /// <inheritdoc />
    public partial class TaoCollationViVnVaExtensionUnaccent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:core.vi_vn", "vi-VN,vi-VN,icu,True")
                .Annotation("Npgsql:PostgresExtension:public.unaccent", ",,");

            migrationBuilder.Sql("ALTER TABLE core.tenant ALTER COLUMN name TYPE character varying(200) COLLATE core.vi_vn;");
            migrationBuilder.Sql("ALTER TABLE core.app_user ALTER COLUMN full_name TYPE character varying(200) COLLATE core.vi_vn;");
            migrationBuilder.Sql("ALTER TABLE core.app_role ALTER COLUMN name TYPE character varying(256) COLLATE core.vi_vn;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Cột trả về collation mặc định TRƯỚC khi bỏ collation — DROP COLLATION hỏng khi còn cột dùng nó.
            migrationBuilder.Sql("ALTER TABLE core.tenant ALTER COLUMN name TYPE character varying(200) COLLATE \"default\";");
            migrationBuilder.Sql("ALTER TABLE core.app_user ALTER COLUMN full_name TYPE character varying(200) COLLATE \"default\";");
            migrationBuilder.Sql("ALTER TABLE core.app_role ALTER COLUMN name TYPE character varying(256) COLLATE \"default\";");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:CollationDefinition:core.vi_vn", "vi-VN,vi-VN,icu,True")
                .OldAnnotation("Npgsql:PostgresExtension:public.unaccent", ",,");
        }
    }
}
