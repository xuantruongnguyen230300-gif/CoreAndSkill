---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `P1` — Tạo vai trò và gán quyền cho nó

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Hai việc, **hai màn, hai endpoint** — cố ý không gộp. Lý do ở §3.

---

## 1. Ai bắt đầu, ở đâu

Quản trị đơn vị, từ màn **Vai trò**, rồi sang màn **Ma trận phân quyền**.

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Quyền `core.role.write` | Tạo và sửa vai trò |
| Quyền `core.permission.write` | Ghi ma trận — **khoá riêng**, không gộp với khoá trên |
| Danh mục quyền đã có trong database | Vào database bằng migration lúc áp lược đồ (luồng `V1` bước 2), dùng chung toàn hệ |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | Quản trị | `POST /api/v1/core/roles` với tên vai trò | [`../contracts/roles.md`](../contracts/roles.md) §2 |
| 2 | BE | Tạo vai trò **không có quyền nào** | cùng trên |
| 3 | Quản trị | Mở màn ma trận: `GET /api/v1/core/permissions/matrix` | [`../contracts/permissions.md`](../contracts/permissions.md) |
| 4 | Quản trị | Tick các ô vai trò × quyền | [`../Design/Components/DataTable.md`](../Design/Components/DataTable.md) |
| 5 | Quản trị | `PUT /api/v1/core/permissions/matrix` | [`../contracts/permissions.md`](../contracts/permissions.md) |
| 6 | BE | Ghi ánh xạ vai trò→quyền **của đơn vị này** | [`../database/schema-core.md`](../database/schema-core.md) §5 |
| 7 | BE, rồi FE của người mang vai trò đó | Ma trận mới có hiệu lực từ **request kế tiếp** của họ. Giao diện của họ còn giữ tập quyền cũ; FE làm mới tập quyền khi nhận 403 | [`../contracts/users.md`](../contracts/users.md) §7 |

### Vì sao tạo vai trò và cấp quyền là hai việc

Vai trò mới sinh ra **rỗng**. Gộp hai việc lại thì **mọi người sửa được tên vai trò cũng cấp được quyền** — và hai việc đó có mức rủi ro hoàn toàn khác nhau. Đổi tên một vai trò là việc hành chính; cấp thêm một quyền là việc bảo mật.

Hai khoá quyền riêng (`core.role.write` và `core.permission.write`) là cách tách đó được ép ra tới tận API.

### Danh mục quyền dùng chung, ánh xạ thuộc đơn vị

| Thứ | Phạm vi | Vì sao |
| --- | --- | --- |
| **Danh mục quyền** (`core.user.read`…) | Dùng chung toàn hệ | Chuỗi khoá nằm trong code (`[RequirePermission(...)]`) và phải khớp một dòng dữ liệu. Cho mỗi đơn vị một bản sao nghĩa là một endpoint từ chối ở đơn vị này mà cho qua ở đơn vị kia |
| **Ánh xạ vai trò → quyền** | Thuộc đơn vị | Mỗi đơn vị tự quyết vai trò của mình gồm những quyền nào |
| **Vai trò** | Thuộc đơn vị | Mỗi đơn vị đặt tên riêng, sửa riêng |

## 4. Hỏng ở đâu — và người dùng thấy gì

> 📖 Loại lỗi và HTTP status của từng mã: [`../contracts/roles.md`](../contracts/roles.md) §2–§3 và [`../contracts/permissions.md`](../contracts/permissions.md) §6. Bảng dưới chỉ giữ `code`.

| Ca | Mã lỗi | Người dùng thấy |
| --- | --- | --- |
| Tên vai trò trùng, khi tạo hoặc khi đổi tên | `CORE.ROLE.NAME_DUPLICATE` | Trùng **trong phạm vi đơn vị này**, không phải toàn hệ |
| Sửa một vai trò hệ thống | `CORE.ROLE.SYSTEM_IMMUTABLE` | Vai trò hệ thống không đổi tên được |
| Thiếu `core.permission.write` | `CORE.AUTH.FORBIDDEN` | Người này sửa được tên vai trò nhưng **không** cấp được quyền — đúng thiết kế |
| FE không làm mới tập quyền khi nhận 403 ở bước 7 | `CORE.AUTH.FORBIDDEN` | 🛑 Người vừa bị gỡ quyền thấy nút cũ mãi tới lần đăng nhập sau, bấm lần nào cũng bị từ chối |
| Tạo vai trò xong, quên bước 5 | không có mã lỗi | 🛑 Vai trò tồn tại, gán được cho người, và **người đó không làm được gì**. Không lỗi nào bắn ra — cùng triệu chứng với ca thiếu cờ đặc quyền ở luồng `V1` bước 7 |

## 5. Quan hệ với đơn vị

**Thuộc đơn vị** — trừ danh mục quyền, dùng chung toàn hệ (bảng ở §3).

Đây là chỗ dễ nhầm nhất của cả nhóm phân quyền: **ba thứ nghe giống nhau, hai phạm vi khác nhau**. Một truy vấn ma trận quên lọc đơn vị sẽ trả về ánh xạ của mọi đơn vị, và màn ma trận hiện ra các vai trò không thuộc đơn vị đang đăng nhập.

## 6. Câu chưa trả lời được

Không còn.
