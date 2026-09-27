---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0054 — Một ô của ma trận phân quyền chỉ đổi bằng hai đường: thêm dòng mới để cấp, xoá mềm để thu; khôi phục dòng đã xoá mềm và xoá cứng qua ứng dụng bị cấm, interceptor chặn lúc chạy

> **Trạng thái:** Đã chấp nhận (2026-09-22)

## Bối cảnh

[`0052-ghi-ma-tran-phan-quyen-luon-vao-nhat-ky-kiem-toan.md`](0052-ghi-ma-tran-phan-quyen-luon-vao-nhat-ky-kiem-toan.md) chốt rằng mọi lần ghi `core.role_permission` có đổi ô đều sinh dòng nhật ký. Hợp đồng ở [`../contracts/permissions.md`](../contracts/permissions.md) §6 mục *Nhật ký kiểm toán* định nghĩa "ô đổi" theo giá trị bằng đúng hai chuyển trạng thái: dòng mới thêm, và `is_deleted` từ `false` sang `true`.

Sau khi thi công, `backend-expert` báo hai chuyển trạng thái khác cũng đổi ô mà không sinh dòng nào:

- **Khôi phục:** `is_deleted` từ `true` về `false`. Thực chất đây là một lần cấp quyền.
- **Xoá cứng:** entry mang `EntityState.Deleted`. Nếu dòng đang hiệu lực thì đây là một lần thu quyền.

Kiến trúc sư đối chiếu code ngày 2026-09-22:

- `src/BE/Core/CoreAndSkill.Core.Infrastructure/Permissions/PermissionMatrixService.cs` cấp bằng `db.RolePermissions.Add(` và thu bằng `row.IsDeleted = true`. Tập "đang cấp" đọc qua bộ lọc xoá mềm, nên một cặp đã bị thu rồi được cấp lại sẽ sinh **dòng mới**, không khôi phục dòng cũ. Unique index `ux_role_permission_tenant_role_perm_active` mang `HasFilter("is_deleted = false")`, nên cách làm này hợp lệ.
- `src/BE/Core/CoreAndSkill.Core.Infrastructure/Tenants/TenantProvisioningService.cs` chỉ cấp bằng `db.RolePermissions.Add(`.
- Không mã sản phẩm nào gán `IsDeleted = false`, gọi `.Remove(` hay `RemoveRange` trên `RolePermissions`, hay đặt `EntityState.Deleted` cho một `RolePermission`.
- **Có một đường xoá cứng ở tầng database.** `RoleAdminService.DeleteAsync` gọi `roleManager.DeleteAsync(role)`, tức xoá cứng `core.app_role`. Khoá ngoại `fk_role_permission_role_id` mang `ON DELETE CASCADE`, nên PostgreSQL xoá mọi dòng `role_permission` của vai trò đó, cả dòng đang hiệu lực lẫn dòng đã xoá mềm. Đường này không đi qua `ChangeTracker`. Interceptor ghi một dòng `core.role.delete` cho vai trò, không ghi dòng `matrix_update` nào.
- `fk_role_permission_permission_id` cũng mang `ON DELETE CASCADE`. Hôm nay không mã sản phẩm nào xoá dòng `core.permission`.

Như vậy hai đường báo cáo nêu **chưa tồn tại** trong code. Lỗ ở đây là lỗ tiềm tàng: đường nào viết sau mà đi một trong hai lối đó thì không để lại dấu, và không có gì báo.

## Quyết định

Kiến trúc sư chốt:

1. Một ô của ma trận chỉ đổi bằng **hai** đường: cấp bằng cách **thêm dòng mới**, thu bằng cách **xoá mềm** dòng đang hiệu lực. Định nghĩa "ô đổi" ở hợp đồng giữ nguyên.
2. Mã ứng dụng **không** khôi phục một dòng `RolePermission` đã xoá mềm, và **không** xoá cứng một dòng `RolePermission` qua `ChangeTracker`.
3. `AuditLogInterceptor` ép luật 2 **lúc chạy**, tại đúng chỗ phân loại ô. Gặp một entry `RolePermission` mà `IsDeleted` đổi từ `true` về `false`, hoặc một entry mang `EntityState.Deleted`, thì interceptor ném ngoại lệ và lượt `SaveChanges` hỏng. Ngoại lệ có tên duy nhất: entry `Deleted` mà vai trò chủ của nó cũng đang `Deleted` trong cùng lượt, tức cascade của một lần xoá vai trò mà EF đang theo dõi.
4. Cascade ở tầng database khi xoá cứng vai trò là **ngoại lệ có tên**. Dòng `core.role.delete` đóng chuỗi nhật ký của vai trò đó. Người đọc nhật ký hiểu rằng mọi khoá của vai trò đã hết hiệu lực tại thời điểm đó.
5. Xoá dòng `core.permission` qua ứng dụng không có đường nào hôm nay. Thêm một đường như vậy thì phải có ADR mới, vì cascade của nó thu một khoá khỏi mọi vai trò của mọi đơn vị mà không sinh dòng nào.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Mở rộng định nghĩa "ô đổi": khôi phục là cấp, xoá cứng dòng đang hiệu lực là thu

**Được:** không có đường ghi nào bị cấm. Đường nào viết sau cũng được nhật ký phủ mà không ai phải nhớ gì. Sửa code nhỏ: thêm hai nhánh vào `ClassifyCell`.

**Vì sao loại:** cách này hợp thức hoá hai cách biểu diễn cùng một lần cấp. Từ đó thời điểm tạo của dòng không còn là thời điểm cấp, và tập dòng của một cặp (vai trò, khoá) không còn đọc được như một chuỗi cấp rồi thu. Xoá cứng còn một hại riêng mà phân loại không che được: xoá cứng một dòng **đã** xoá mềm không đổi ô nào, nên không sinh dòng nhật ký nào, nhưng nó xoá mất lịch sử. Phương án này chỉ đáng chọn khi có một nhu cầu thật cần khôi phục dòng cũ. Hôm nay không có nhu cầu đó.

### Phương án B — Cấm bằng ArchTest quét mã nguồn

**Được:** bắt lỗi lúc build, trước khi chạy.

**Vì sao loại:** phép gán `IsDeleted = false` chỉ sai khi đối tượng là một `RolePermission`. Muốn biết điều đó thì phép dò phải hiểu kiểu, tức phải phân tích ngữ nghĩa bằng Roslyn chứ không quét chữ được. Đường qua một repository chung hay qua `DbContext.Update(entity)` vẫn lọt. Interceptor đứng ở đúng chỗ mọi đường qua `ChangeTracker` đều phải đi qua, nên phép chặn ở đó không phụ thuộc cú pháp.

### Phương án C — Chỉ ghi luật, không chặn

**Được:** không đổi code.

**Vì sao loại:** đây đúng là loại luật sẽ bị vi phạm mà không ai biết. Hậu quả của vi phạm là một lần cấp quyền không để lại dấu, tức thứ ADR-0052 tồn tại để ngăn.

## Hệ quả

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| **Một lần ghi sai đường thành 500, không thành một lần ghi âm thầm** | Người viết một tính năng *"hoàn tác thay đổi quyền"* sẽ gặp lỗi lúc chạy. Nếu không có test đi qua đường đó thì lỗi lộ ra ở người dùng. Đây là chủ đích, nhưng vẫn là một cái giá |
| **Interceptor gánh thêm một vai** | Trước đây nó chỉ ghi. Nay nó còn từ chối. Một lỗi trong phép từ chối, ví dụ nhận nhầm cascade hợp lệ, chặn luôn việc xoá vai trò |
| **Xoá vai trò xoá cả lịch sử ô của vai trò đó khỏi `role_permission`** | Cascade ở database xoá cả dòng đã xoá mềm. Chuỗi *"ai cấp khoá nào"* vẫn còn trong `core.audit_log`, vì bảng đó chỉ ghi thêm (luật M13), nhưng không còn trong bảng ma trận |
| **Luật không phủ đường ngoài ứng dụng** | SQL tay và script dưới `database/scripts/` vẫn khôi phục hay xoá cứng được. Giới hạn này giống hệt giới hạn đã ghi ở ADR-0052 |

### Tích cực

- Lỗ báo cáo nêu được đóng trước khi có đường nào đi vào nó.
- Tập dòng của một cặp (vai trò, khoá) đọc được như một chuỗi: mỗi dòng là một lần cấp, cờ xoá mềm là lần thu.
- Phép chặn đứng ở đúng một chỗ, không phụ thuộc người viết handler nhớ luật.

### Điều gì là dấu hiệu quyết định này bắt đầu sai

- Có một nhu cầu nghiệp vụ thật cần khôi phục đúng dòng cũ, ví dụ để giữ nguyên `created_at`. Khi đó phương án A đáng được xét lại.
- Lỗi từ phép chặn xuất hiện ở production. Nghĩa là có một đường ghi chưa ai biết. Việc đầu tiên là tìm đường đó, không phải nới phép chặn.
- Có người cần biết một vai trò đã xoá từng giữ khoá nào, và phải dựng lại từ nhật ký. Nghĩa là xoá cứng vai trò đã đắt hơn dự kiến, và việc cần xét là chuyển vai trò sang xoá mềm.

### Rút lui nếu sai

Mọi thứ là code, không đổi lược đồ, không sinh dữ liệu. Gỡ phép chặn là gỡ một nhánh trong `ClassifyCell` của `AuditLogInterceptor.cs` cùng test của nó. Chuyển sang phương án A là thêm hai nhánh phân loại vào cùng chỗ đó, sửa luật 2 ở hợp đồng [`../contracts/permissions.md`](../contracts/permissions.md) §6, và viết một ADR mới lật quyết định này.

## Liên quan

- [`0052-ghi-ma-tran-phan-quyen-luon-vao-nhat-ky-kiem-toan.md`](0052-ghi-ma-tran-phan-quyen-luon-vao-nhat-ky-kiem-toan.md): quyết định được bổ sung. Mọi điều của 0052 giữ nguyên hiệu lực
- [`../contracts/permissions.md`](../contracts/permissions.md) §6 mục *Nhật ký kiểm toán*: định nghĩa gốc của luật
- [`../RULES.md`](../RULES.md) §6 luật S18
