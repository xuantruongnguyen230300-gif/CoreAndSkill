---
kind: luat
scope: core
verified: 2026-10-06
---

# `V3` — Ngưng và bật lại hoạt động của một đơn vị

✅ **CÓ THẬT** — đối chiếu trọn tệp với `src/` ngày 2026-10-06, chỉ bằng đọc mã, không chạy ứng dụng. Neo từng bước ở mục *Đối chiếu với `src/`* cuối tệp.

> Đơn vị **không bị xoá, chỉ bị ngưng hoạt động**. Lý do ở §3.

---

## 1. Ai bắt đầu, ở đâu

Tài khoản vận hành hệ thống, từ danh sách đơn vị trong khu quản trị hệ thống.

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Tài khoản mang `is_system_operator` | |
| Đơn vị đích **không phải** đơn vị hệ thống | Ngưng đơn vị hệ thống là tự khoá mình ra ngoài |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | Vận hành | `PUT /api/v1/core/system/tenants/{id}/active` với trạng thái mong muốn | [`../contracts/tenants.md`](../contracts/tenants.md) §3 |
| 2 | BE | Kiểm đơn vị đích không mang cờ đơn vị hệ thống | [`../database/schema-core.md`](../database/schema-core.md) §1.3 |
| 3 | BE | 🛑 Đổi cờ hoạt động của đơn vị **bằng thao tác ngưng/bật của service tạo đơn vị** (`ITenantProvisioningService`). Bảng đơn vị không thuộc đơn vị nào, nên service ghi thẳng vào dòng đơn vị, **không mở phạm vi ngữ cảnh nào** — và không thêm mục allowlist nào của luật **A12** | [`../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md) · [`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md) · [`../database/schema-core.md`](../database/schema-core.md) §1.3 |
| 4 | BE | Ghi nhật ký kiểm toán **hai dòng**: một ở đơn vị hệ thống, một ở đơn vị bị ngưng hoặc bật lại. Mỗi dòng nhận đơn vị của nó **qua tham số**, không qua phạm vi ngữ cảnh — bảng nhật ký không theo phạm vi đang mở | [`../contracts/tenants.md`](../contracts/tenants.md) §5 · [`../database/schema-core.md`](../database/schema-core.md) §9.4, §1.3 *Miễn trừ thứ ba* |
| 5 | BE | Ngưng: **mọi** request kế tiếp của phiên đang mở thuộc đơn vị đó bị chặn ở bước dựng danh tính — không chỉ lần đăng nhập sau | [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §10 |

### Vì sao ngưng chứ không xoá

| Nếu xoá | Hệ quả |
| --- | --- |
| Dữ liệu nghiệp vụ của đơn vị | Mất, không lấy lại |
| Nhật ký kiểm toán | Khoá ngoại tới bảng đơn vị ở dạng `ON DELETE RESTRICT` **chặn thẳng** — xem luồng `N6`. Nghĩa là xoá một đơn vị **không thực hiện được** khi nhật ký của nó còn, và đó là chủ đích |
| Điều tra về sau | Không còn gì để điều tra |

Hướng **xoá đơn vị** đã bị loại và khoá, khai ở [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §10. Giải phóng dung lượng đi qua một thao tác lưu trữ có kiểm soát, không qua lệnh xoá.

## 4. Hỏng ở đâu — và ai thấy gì

> 📖 Loại lỗi và HTTP status của từng mã: [`../contracts/tenants.md`](../contracts/tenants.md) §3, [`../contracts/auth.md`](../contracts/auth.md) §3 và §11. Bảng dưới chỉ giữ `code`.

| Ca | Mã lỗi | Thấy gì |
| --- | --- | --- |
| Không có đơn vị đó | `CORE.TENANT.NOT_FOUND` | |
| Ngưng hoặc bật lại đơn vị hệ thống | `CORE.TENANT.SYSTEM_IMMUTABLE` | Chặn thẳng, không ghi nhật ký — với *bật lại* cũng vậy dù đơn vị hệ thống luôn đang hoạt động: lời gọi không đổi gì thì không được ghi nhật ký cho một việc không xảy ra. **Phép kiểm này dựa vào cột `is_system`**, không dựa vào mã đơn vị |
| Đơn vị vừa bị ngưng — người đăng nhập mới | `CORE.AUTH.INVALID_CREDENTIALS` | Mọi tài khoản của nó **không đăng nhập được nữa** — cùng câu với "sai mật khẩu", cố ý không phân biệt |
| Đơn vị vừa bị ngưng — người đang có phiên mở | `CORE.AUTH.NOT_AUTHENTICATED` | Request kế tiếp bị đưa về màn đăng nhập như phiên hết hạn; đăng nhập lại thì rơi vào dòng trên |
| Bước 5 chỉ chặn ở đăng nhập | không có mã lỗi | 🛑 Người đang có phiên thao tác tiếp tới khi phiên hết — lệnh ngưng **có vẻ đã thi hành nhưng chưa** |
| Bước 4 chỉ ghi một dòng | không có mã lỗi | 🛑 Hoặc đơn vị không thấy trong nhật ký của chính nó rằng mình vừa bị ngưng, hoặc nhật ký người vận hành thiếu việc họ vừa làm |

## 5. Quan hệ với đơn vị

**Đối tượng là chính một đơn vị.** Người thao tác thuộc đơn vị hệ thống, như luồng `V2` và `V4`.

Cột `is_system` là thứ phân biệt "đơn vị được phép ngưng" với "đơn vị không được phép". Nó **không** dựa vào mã đơn vị — mã do người gõ, và một đơn vị thật tình cờ trùng mã sẽ làm sập phép kiểm ([`../adr/0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](../adr/0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md)).

## 6. Câu chưa trả lời được

Không còn.

---

## Đối chiếu với `src/`

Neo của `V3`, đối chiếu 2026-10-06. Đường dẫn BE tính từ `src/BE/Core/`, FE từ `src/FE/src/app/platform/he-thong/don-vi/`.

| Bước, ca | Neo bằng chuỗi |
| --- | --- |
| Bước 1 — route, chỉ tài khoản vận hành | `CoreAndSkill.Core.Web/Controllers/TenantsController.cs`: `[RequireSystemOperator]` trên controller, `[HttpPut("{id:guid}/active")]` |
| Bước 2 — chặn đơn vị hệ thống theo cột `is_system`, cả hai chiều | `CoreAndSkill.Core.Domain/Tenants/Tenant.cs`: `public Result Deactivate()` và `public Result Activate()`, mỗi hàm mở đầu bằng `if (IsSystem)` → `TenantErrors.SystemImmutable` |
| Bước 3 — service ghi thẳng dòng đơn vị, không mở phạm vi | `CoreAndSkill.Core.Infrastructure/Tenants/TenantProvisioningService.cs`, hàm `SetTenantActiveAsync`: `var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);`, `await db.SaveChangesAsync(ct); // Tenant KHÔNG ITenantScoped` |
| Bước 4 — hai dòng nhật ký, đơn vị truyền qua tham số | cùng tệp, hàm `WriteCrossTenantAuditAsync`: hai lời `db.AuditLogs.Add(AuditLog.Record(` — một cho `caller`, một cho `targetTenantId` |
| Bước 5 — phiên đang mở bị chặn ở request kế tiếp | `CoreAndSkill.Core.Web/DependencyInjection/CoreWebServiceCollectionExtensions.cs`: `if (tenant is null \|\| !tenant.IsActive)` rồi `context.RejectPrincipal();` |
| Đăng nhập vào đơn vị đã ngưng → cùng mã với sai mật khẩu | `CoreAndSkill.Core.Application/Auth/LoginCommandHandler.cs`: `if (tenant is null \|\| !tenant.IsActive)` rồi `AuthErrors.InvalidCredentials` |
| Không có đơn vị | `TenantProvisioningService.cs`: `return Result.Failure(TenantProvisioningErrors.NotFound);` trong `SetTenantActiveAsync` |
| FE — thao tác từ danh sách đơn vị | `services/don-vi.service.ts`: `` `${this.duongDan}/${id}/active` ``; trang `pages/danh-sach/danh-sach-don-vi.page.ts` |
