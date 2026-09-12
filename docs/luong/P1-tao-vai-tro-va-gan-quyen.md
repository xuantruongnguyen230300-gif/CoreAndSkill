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
| Danh mục quyền đã nạp | Nạp lúc cài đặt (luồng `V1` bước 3), dùng chung toàn hệ |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | Quản trị | `POST /roles` với mã và tên vai trò | [`../contracts/roles.md`](../contracts/roles.md) §2 |
| 2 | BE | Tạo vai trò **không có quyền nào** | cùng trên |
| 3 | Quản trị | Mở màn ma trận: `GET /permissions/matrix` | [`../contracts/permissions.md`](../contracts/permissions.md) |
| 4 | Quản trị | Tick các ô vai trò × quyền | [`../Design/Components/DataTable.md`](../Design/Components/DataTable.md) |
| 5 | Quản trị | `PUT /permissions/matrix` | [`../contracts/permissions.md`](../contracts/permissions.md) |
| 6 | BE | Ghi ánh xạ vai trò→quyền **của đơn vị này** | [`../database/schema-core.md`](../database/schema-core.md) §5 |

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

| Ca | Mã lỗi | Người dùng thấy |
| --- | --- | --- |
| Mã vai trò trùng | `CORE.ROLE.CODE_DUPLICATE` (409) | Trùng **trong phạm vi đơn vị này**, không phải toàn hệ |
| Sửa một vai trò hệ thống | `CORE.ROLE.SYSTEM_IMMUTABLE` (422) | Vai trò hệ thống không đổi tên được |
| Thiếu `core.permission.write` | `CORE.AUTH.FORBIDDEN` (403) | Người này sửa được tên vai trò nhưng **không** cấp được quyền — đúng thiết kế |
| Tạo vai trò xong, quên bước 5 | không có mã lỗi | 🛑 Vai trò tồn tại, gán được cho người, và **người đó không làm được gì**. Không lỗi nào bắn ra — cùng triệu chứng với ca thiếu cờ đặc quyền ở luồng `V1` bước 7 |

## 5. Quan hệ với đơn vị

**Thuộc đơn vị** — trừ danh mục quyền, dùng chung toàn hệ (bảng ở §3).

Đây là chỗ dễ nhầm nhất của cả nhóm phân quyền: **ba thứ nghe giống nhau, hai phạm vi khác nhau**. Một truy vấn ma trận quên lọc đơn vị sẽ trả về ánh xạ của mọi đơn vị, và màn ma trận hiện ra các vai trò không thuộc đơn vị đang đăng nhập.

## 6. Câu chưa trả lời được

- **Bỏ một quyền khỏi vai trò thì phiên đang mở của người mang vai trò đó có bị ảnh hưởng ngay không?** Tập quyền được FE lấy ở luồng `D1` bước 7 và dùng để dựng giao diện. Không file nào nói tập đó được làm mới khi nào — nếu nó chỉ lấy lúc đăng nhập thì người bị gỡ quyền vẫn thấy nút cho tới lần đăng nhập sau.
- **Vai trò hệ thống là vai trò nào, và ai tạo ra chúng?** `CORE.ROLE.SYSTEM_IMMUTABLE` giả định chúng tồn tại; bộ vai trò mặc định do **dự án** seed chứ không phải Core ([`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11.4), nên Core không biết vai trò nào là vai trò hệ thống.
- **Xoá một vai trò đang được gán cho người thì sao?** [`../contracts/roles.md`](../contracts/roles.md) §4 có endpoint xoá nhưng không nêu ca này.
