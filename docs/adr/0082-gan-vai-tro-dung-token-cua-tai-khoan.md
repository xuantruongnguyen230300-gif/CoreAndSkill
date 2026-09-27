---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0082 — `PUT /users/{id}/roles` mang `version` của tài khoản, và phép so-và-đổi token chạy trước mọi thay đổi vai trò

> **Trạng thái:** Đã chấp nhận (2026-09-24)

## Bối cảnh

[`../quy-uoc/be-entity-domain.md`](../quy-uoc/be-entity-domain.md) §6 xếp *"Người dùng, vai trò, phân quyền"* vào nhóm **cần** token đồng thời. Card [`../contracts/users.md`](../contracts/users.md) cho `version` vào §6, §8, §9 nhưng bỏ §7 — đổi tập vai trò — mà không nêu lý do. `core-reviewer` (lượt BE, 2026-09-24) và kiến trúc sư đối chiếu `src/BE`:

| Chỗ | Có gì |
| --- | --- |
| `src/BE/Core/CoreAndSkill.Core.Application/Users/AssignUserRolesCommand.cs` | Chỉ `(Guid UserId, IReadOnlyList<Guid> RoleIds)` — không token |
| `src/BE/Core/CoreAndSkill.Core.Infrastructure/Identity/UserAdminService.cs`, `AssignRolesAsync` | Đọc tập hiện có, `RemoveRange` / `Add` trên `db.UserRoles`; không đọc, không đổi `ConcurrencyStamp` của tài khoản |

Hai hậu quả, cả hai tái hiện được bằng hai quản trị viên:

1. **Mất cập nhật im lặng.** A và B cùng mở tài khoản đang có `{R1}`. A lưu `{R1, R2}`, B lưu `{R1, R3}`. Kết quả `{R1, R3}`: vai trò A vừa cấp biến mất, không ai được báo.
2. **500 thay vì 409.** Hai `PUT` cùng thêm `R2` đều thấy `R2` chưa có, cùng chèn `(user_id, role_id)`; lượt sau vỡ khoá chính `app_user_role` ([`../database/schema-core.md`](../database/schema-core.md) §4.3) và client nhận `CORE.SYSTEM.UNEXPECTED`.

## Quyết định

Kiến trúc sư chốt: `PUT /api/v1/core/users/{id}/roles` nhận `version` bắt buộc — token `concurrency_stamp` của **tài khoản đích**, cùng token với §6, §8, §9. BE so và đổi token đó bằng **một** câu `UPDATE` có điều kiện trên `core.app_user`, chạy **trước** mọi thay đổi `core.app_user_role` trong cùng transaction; 0 dòng thì trả 409 `CORE.CONCURRENCY.CONFLICT` và không ghi gì. FE gửi `version` lấy từ `GET` gần nhất. Ba ràng buộc thi công ở card §7 mục *Ghi chú — đồng thời*.

## Phương án đã cân nhắc và vì sao loại

### A — Miễn trừ kèm lý do, như `Tenant.IsActive`

**Được:** không đổi hợp đồng, không đổi FE.
**Mất:** miễn trừ của `Tenant` đứng được vì request mang **giá trị đích của một cờ** — ý định của người ghi sau là trọn vẹn. Ở đây request mang một **tập** dựng từ bản chụp cũ: B không hề định gỡ `R2`, B chỉ không biết `R2` tồn tại. Và miễn trừ không chữa được hậu quả 2.
**Vì sao loại:** lập luận miễn trừ không áp được cho tập, và 500 vẫn còn.

### B — Băm của tập vai trò, như ma trận quyền

**Được:** xung đột chỉ giữa hai lần đổi vai trò; đổi email hay khoá tài khoản không làm hộp gán vai trò nhận 409.
**Mất:** thêm field vào `GET` §3/§4 và một mã lỗi riêng. Phép so băm là *đọc-rồi-so*: hai `PUT` đồng thời cùng đọc một tập, cùng khớp băm, cùng chèn — hậu quả 2 còn nguyên, phải thêm khoá dòng riêng. Ma trận quyền dùng băm vì tập đó **không có một bản ghi cha** để giữ token; tập vai trò của một người dùng thì có.
**Vì sao loại:** trả thêm hai field hợp đồng mà vẫn phải tự dựng khoá — trong khi token của bản ghi cha cho cả phép so lẫn khoá trong một câu.

### C — Chỉ bắt lỗi khoá chính, đổi 500 thành 409

**Được:** vá hậu quả 2 bằng vài dòng.
**Mất:** hậu quả 1 — ca người dùng thật gặp — nguyên vẹn.
**Vì sao loại:** chữa triệu chứng ồn, giữ nguyên lỗi im lặng.

## Hệ quả

### Tích cực

- Hai lần đổi vai trò chồng nhau: lượt sau nhận 409, không mất cấp quyền nào, không còn 500.
- FE không học thêm gì: cùng field, cùng mã, cùng nhánh xử lý 409 với §6, §8, §9.

### Tiêu cực — cái giá thật

- **Đổi hợp đồng API.** FE phải gửi `version` ở hộp gán vai trò; request cũ không có `version` nhận 409.
- **Xung đột giả giữa các thao tác trên cùng tài khoản.** Đổi vai trò làm hộp *Sửa* đang mở nhận 409, và ngược lại; một lần đăng nhập sai của chính tài khoản đó (Identity đổi stamp) cũng làm hộp gán vai trò nhận 409. Người dùng tải lại rồi làm lại.
- Mọi lần đổi vai trò ghi thêm một câu `UPDATE` trên `core.app_user` và giữ khoá dòng đó tới hết transaction.

### Rút lui nếu sai

Nếu xung đột giả thành phiền toái đo được: chuyển sang phương án B **cộng** khoá dòng `SELECT … FOR UPDATE` trên tài khoản. `GET` §3/§4 thêm một field băm, §7 đổi nguồn của `version`, FE đổi chỗ đọc token cho đúng hộp gán vai trò — ba chỗ, một lượt. Không dữ liệu nào cần chuyển: quyết định này không thêm cột, chỉ dùng cột Identity đã có.

### Dấu hiệu quyết định này bắt đầu sai

- Báo cáo người dùng về 409 ở hộp gán vai trò mà không có người thứ hai nào sửa cùng tài khoản.
- Có đường ghi thứ hai vào `app_user_role` (ngoài §5, §7, tạo quản trị đơn vị) mà không đi qua phép so-và-đổi token.

## Liên quan

- Khuôn token trên dây: [`../wiki-core/be/06-concurrency-control.md`](../wiki-core/be/06-concurrency-control.md) §6.3
- Nhật ký của cùng thao tác: [`0083-doi-vai-tro-nguoi-dung-luon-vao-nhat-ky-kiem-toan.md`](0083-doi-vai-tro-nguoi-dung-luon-vao-nhat-ky-kiem-toan.md)
