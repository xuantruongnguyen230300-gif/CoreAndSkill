---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0083 — Mọi lần đổi vai trò của người dùng ghi một dòng `core.user.role_assign`, phần tử là cặp `{ id, name }`

> **Trạng thái:** Đã chấp nhận (2026-09-24)

## Bối cảnh

[`../wiki-core/be/10-data-retention.md`](../wiki-core/be/10-data-retention.md) §5.4 bắt ghi nhật ký cho *"thay đổi phân quyền và vai trò"*. Ma trận quyền đã có dòng riêng và luật S17 ([ADR-0052](0052-ghi-ma-tran-phan-quyen-luon-vao-nhat-ky-kiem-toan.md)). Gán và gỡ vai trò của một **người dùng** thì không: `core-reviewer` (lượt BE, 2026-09-24) chỉ ra `AuditLogInterceptor` không có nhánh nào cho `AppUserRole`, nên `PUT /users/{id}/roles` — thao tác nâng quyền trực tiếp nhất của hệ — không để lại dấu vết.

`backend-expert` đã dựng phần gom dòng (`CollectUserRoleChangesAsync`) và chờ hai thứ: mã hành động, và hình dạng `after_value`. Không có danh mục mã hành động nào trong `docs/`: mã sống ở `AuditActionCodes.cs`, hình dạng dòng sống ở card của thao tác sinh ra nó — tiền lệ là [`../contracts/permissions.md`](../contracts/permissions.md) §6.

## Quyết định

Kiến trúc sư chốt:

1. Mã hành động là **`core.user.role_assign`** — ba đoạn, cùng khuôn mọi mã audit đang có (`core.user.lock`, `core.permission.matrix_update`).
2. Một dòng cho mỗi người dùng có dòng `app_user_role` thêm hoặc xoá trong một lượt `SaveChanges`, cho **mọi** đường ghi qua ứng dụng — kể cả tạo người dùng, nơi dòng này đi kèm `core.user.create` chứ không gộp.
3. `after_value` mang `granted` và `revoked`, mỗi phần tử là **`{ id, name }`** — `name` là tên vai trò lúc ghi, `null` khi không tra được.
4. Hình dạng dòng khai ở [`../contracts/users.md`](../contracts/users.md) §7 mục *Nhật ký kiểm toán*, và thành luật **S21** có cổng, cùng khuôn S17.

## Phương án đã cân nhắc và vì sao loại

### Mã hành động `core.user.role.assign` — trùng khoá quyền

**Được:** một chuỗi cho cả quyền lẫn hành động, như `core.user.lock`.
**Mất:** mã audit nào cũng ba đoạn; bốn đoạn là hình dạng của khoá quyền. Truy vấn nhật ký tách tài nguyên theo đoạn thứ hai sẽ phải xử lý riêng một mã.
**Vì sao loại:** trùng tên với quyền là tình cờ ở `lock`, không phải quy tắc.

### `after_value` chỉ mang tên vai trò — cùng khuôn ma trận

**Được:** gọn, đọc thẳng, giống hệt `core.permission.matrix_update`.
**Mất:** ma trận dùng **mã khoá quyền** — vừa là định danh ổn định vừa là nhãn. Vai trò không có mã: tên đổi được (`core.role.rename`) và dùng lại được sau khi vai trò cũ bị xoá. Nhật ký chỉ mang tên thì hai vai trò khác nhau cùng tên "Kế toán" ở hai thời điểm là một. Bản dựng thử còn thay tên bằng id khi không tra được, trộn hai kiểu trong một danh sách.
**Vì sao loại:** [`../wiki-core/be/10-data-retention.md`](../wiki-core/be/10-data-retention.md) §5.2 đòi *định danh **và** nhãn lúc ghi* cho đối tượng; phần tử của diff là đối tượng.

### `after_value` chỉ mang id

**Được:** truy được chắc chắn.
**Mất:** vai trò đã xoá thì id trỏ vào khoảng trống, dòng mất nghĩa đúng lúc cần đọc.
**Vì sao loại:** cùng lý do trên, theo chiều ngược lại.

### Chỉ ghi ở handler của `PUT /users/{id}/roles`

**Được:** không đụng interceptor.
**Mất:** tạo người dùng kèm vai trò, tạo quản trị đơn vị, seed — ba đường cấp vai trò không để dấu. Lý do S17 gắn vào tầng dữ liệu (ADR-0052) áp y nguyên.
**Vì sao loại:** nhật ký phụ thuộc handler tự nhớ gọi là nhật ký có lỗ.

## Hệ quả

### Tích cực

- Mọi thay đổi vai trò của người dùng qua ứng dụng trả lời được *ai, lúc nào, cấp gì, gỡ gì*, kể cả sau khi vai trò bị đổi tên hay xoá.
- Cùng cơ chế với ma trận: một interceptor, một ngoại lệ có tên (vai trò chủ bị xoá cùng lượt).

### Tiêu cực — cái giá thật

- **Mã và hình dạng là vĩnh viễn.** Nhật ký chỉ ghi thêm (luật M13): dòng đã ghi với `core.user.role_assign` không bao giờ đổi. Đổi tên mã về sau là hai mã cùng nghĩa trong một bảng mãi mãi.
- `after_value` khác khuôn ma trận: công cụ đọc nhật ký phải biết hai hình dạng phần tử — chuỗi mã quyền và cặp `{ id, name }`.
- Tạo người dùng kèm vai trò sinh hai dòng; ai đếm "số thao tác" bằng số dòng sẽ đếm dư.
- Đường ghi `app_user_role` bằng SQL thô hoặc `ExecuteDelete` không bị interceptor thấy — cổng S21 lớp (2) canh mã nguồn, không canh được script vận hành.

### Rút lui nếu sai

Dòng đã ghi không sửa được. Nếu hình dạng sai, ADR mới chốt hình dạng thứ hai và **ngày bắt đầu** của nó; công cụ đọc nhật ký xử lý cả hai theo `occurred_at`. Nối nhánh vào interceptor là thay đổi một chỗ; gỡ nó ra cũng vậy.

### Dấu hiệu quyết định này bắt đầu sai

- Một đường cấp vai trò mới xuất hiện mà nhật ký không có dòng — cổng S21 lớp (1) không phủ tới đường đó.
- Người đọc nhật ký cần biết **tập vai trò đầy đủ** tại một thời điểm, không chỉ phần đổi — khi đó `before_value` phải mang tập cũ.

## Liên quan

- Hình dạng dòng: [`../contracts/users.md`](../contracts/users.md) §7 · luật S21: [`../RULES.md`](../RULES.md) §6
- Token đồng thời của cùng thao tác: [`0082-gan-vai-tro-dung-token-cua-tai-khoan.md`](0082-gan-vai-tro-dung-token-cua-tai-khoan.md)
