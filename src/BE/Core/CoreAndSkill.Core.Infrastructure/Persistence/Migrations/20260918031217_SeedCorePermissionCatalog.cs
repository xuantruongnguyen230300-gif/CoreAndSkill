using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAndSkill.Core.Infrastructure.Persistence.Migrations
{
    // Migration DỮ LIỆU, không phải schema — docs/database/migration-policy.md §4.1. Danh mục khoá
    // khai trong code qua CorePermissionCatalogSource (Core.Infrastructure/Permissions/); dòng ở
    // đây PHẢI khớp giá trị của đúng nguồn đó — cổng đối chiếu hai chiều (luật B7) là
    // src/BE/Tests/CoreAndSkill.ArchTests/PermissionSeedParityTests.cs, đọc SQL dưới đây ra từ
    // UpOperations nên không cần database. Không cột nào ở đây tự nghĩ ra ngoài giá trị nguồn đó cấp.
    //
    // Ba điều kiện bắt buộc của phần seed (migration-policy.md §4.1): (1) IDEMPOTENT — ON CONFLICT
    // DO NOTHING, lặp lại NGUYÊN VĂN vị từ của index một phần (schema-core.md §3.3); (2) ĐỊNH DANH
    // CỐ ĐỊNH — cùng khoá mang cùng id ở mọi môi trường; (3) KHÔNG XOÁ TỰ ĐỘNG — bỏ một khoá khỏi
    // code không kéo theo DELETE ở đây.
    /// <inheritdoc />
    public partial class SeedCorePermissionCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // permission_resource — unique ĐẦY ĐỦ trên key (uq_permission_resource_key, không lọc
            // — ngoại lệ có chủ đích của schema-core.md §3.3, vì đây là đích của foreign key).
            migrationBuilder.Sql("""
                INSERT INTO core.permission_resource (id, key, name_key, module_key, display_order, is_deleted, created_at, created_by, updated_at, updated_by)
                VALUES
                    ('8f1e0000-0000-7000-8000-000000000001', 'core.user',       'resource.core.user',       NULL, 10, false, now(), 'system', now(), 'system'),
                    ('8f1e0000-0000-7000-8000-000000000002', 'core.user.role',  'resource.core.user.role',  NULL, 20, false, now(), 'system', now(), 'system'),
                    ('8f1e0000-0000-7000-8000-000000000003', 'core.role',       'resource.core.role',       NULL, 30, false, now(), 'system', now(), 'system'),
                    ('8f1e0000-0000-7000-8000-000000000004', 'core.permission', 'resource.core.permission', NULL, 40, false, now(), 'system', now(), 'system'),
                    ('8f1e0000-0000-7000-8000-000000000005', 'core.menu',       'resource.core.menu',       NULL, 50, false, now(), 'system', now(), 'system')
                ON CONFLICT (key) DO NOTHING;
                """);

            // permission — unique MỘT PHẦN trên code (ux_permission_code_active WHERE is_deleted = false)
            // — vị từ WHERE phải lặp lại NGUYÊN VĂN trong ON CONFLICT (schema-core.md §3.3 rule 2).
            migrationBuilder.Sql("""
                INSERT INTO core.permission (id, code, resource_key, action, name_key, is_system, display_order, is_deleted, created_at, created_by, updated_at, updated_by)
                VALUES
                    ('8f1e0001-0000-7000-8000-000000000001', 'core.user.read',           'core.user',      'read',           'permission.core.user.read',           true, 10, false, now(), 'system', now(), 'system'),
                    ('8f1e0001-0000-7000-8000-000000000002', 'core.user.write',          'core.user',      'write',          'permission.core.user.write',          true, 20, false, now(), 'system', now(), 'system'),
                    ('8f1e0001-0000-7000-8000-000000000003', 'core.user.lock',           'core.user',      'lock',           'permission.core.user.lock',           true, 30, false, now(), 'system', now(), 'system'),
                    ('8f1e0001-0000-7000-8000-000000000004', 'core.user.reset-password', 'core.user',      'reset-password', 'permission.core.user.reset-password', true, 40, false, now(), 'system', now(), 'system'),
                    ('8f1e0001-0000-7000-8000-000000000005', 'core.user.export',         'core.user',      'export',         'permission.core.user.export',         true, 50, false, now(), 'system', now(), 'system'),
                    ('8f1e0001-0000-7000-8000-000000000006', 'core.user.role.assign',    'core.user.role', 'assign',         'permission.core.user.role.assign',    true, 10, false, now(), 'system', now(), 'system'),
                    ('8f1e0001-0000-7000-8000-000000000007', 'core.role.read',           'core.role',      'read',           'permission.core.role.read',           true, 10, false, now(), 'system', now(), 'system'),
                    ('8f1e0001-0000-7000-8000-000000000008', 'core.role.write',          'core.role',      'write',          'permission.core.role.write',          true, 20, false, now(), 'system', now(), 'system'),
                    ('8f1e0001-0000-7000-8000-000000000009', 'core.permission.read',     'core.permission','read',           'permission.core.permission.read',     true, 10, false, now(), 'system', now(), 'system'),
                    ('8f1e0001-0000-7000-8000-00000000000a', 'core.permission.write',    'core.permission','write',          'permission.core.permission.write',    true, 20, false, now(), 'system', now(), 'system'),
                    ('8f1e0001-0000-7000-8000-00000000000b', 'core.menu.read',           'core.menu',      'read',           'permission.core.menu.read',           true, 10, false, now(), 'system', now(), 'system'),
                    ('8f1e0001-0000-7000-8000-00000000000c', 'core.menu.write',          'core.menu',      'write',          'permission.core.menu.write',          true, 20, false, now(), 'system', now(), 'system')
                ON CONFLICT (code) WHERE is_deleted = false DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Chính sách xuôi-chỉ (docs/database/migration-policy.md §6) — Down() giữ cho máy dev
            // dựng lại nhanh, KHÔNG phải kế hoạch khôi phục production.
            migrationBuilder.Sql("""
                DELETE FROM core.permission WHERE id IN (
                    '8f1e0001-0000-7000-8000-000000000001', '8f1e0001-0000-7000-8000-000000000002',
                    '8f1e0001-0000-7000-8000-000000000003', '8f1e0001-0000-7000-8000-000000000004',
                    '8f1e0001-0000-7000-8000-000000000005', '8f1e0001-0000-7000-8000-000000000006',
                    '8f1e0001-0000-7000-8000-000000000007', '8f1e0001-0000-7000-8000-000000000008',
                    '8f1e0001-0000-7000-8000-000000000009', '8f1e0001-0000-7000-8000-00000000000a',
                    '8f1e0001-0000-7000-8000-00000000000b', '8f1e0001-0000-7000-8000-00000000000c');
                """);

            migrationBuilder.Sql("""
                DELETE FROM core.permission_resource WHERE id IN (
                    '8f1e0000-0000-7000-8000-000000000001', '8f1e0000-0000-7000-8000-000000000002',
                    '8f1e0000-0000-7000-8000-000000000003', '8f1e0000-0000-7000-8000-000000000004',
                    '8f1e0000-0000-7000-8000-000000000005');
                """);
        }
    }
}
