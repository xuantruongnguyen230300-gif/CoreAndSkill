---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `V4` — Khôi phục quản trị đơn vị: vận hành đặt lại mật khẩu cho người giữ cửa quản trị của đơn vị

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Luồng này đóng một ngõ cụt: người giữ cửa quản trị của một đơn vị — tài khoản mang `has_permission_bypass`, hoặc người giữ vai trò hệ thống — quên mật khẩu thì có thể **không ai trong đơn vị đặt lại hộ được**: luồng `D4` từ chối đích mang cờ, và từ chối đích có quyền vượt người gọi. Đường còn lại đi từ **ngoài** đơn vị.

---

## 1. Ai bắt đầu, ở đâu

**Tài khoản vận hành hệ thống**, từ khu quản trị hệ thống, sau khi nhận yêu cầu của đơn vị qua một kênh **ngoài hệ thống**.

Luồng này **không** đặt lại mật khẩu cho mọi người dùng — chỉ cho tài khoản của một đơn vị đang mang cờ đặc quyền, hoặc đang giữ vai trò hệ thống của đơn vị đó. Người dùng thường đi luồng `D4`. Khôi phục chính tài khoản `superadmin` là **lệnh chạy tay trên máy chủ**, không qua HTTP ([`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md) · [`../database/script-runbook.md`](../database/script-runbook.md) §8).

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Tài khoản mang `is_system_operator` | Đăng nhập được, tức luồng `V1` đã chạy xong |
| Đơn vị đích tồn tại | Chọn từ danh sách đơn vị của khu quản trị hệ thống |
| Tên đăng nhập của tài khoản đích | Vận hành **không** thấy danh sách người dùng của đơn vị — tên này đến từ chính đơn vị, qua kênh ngoài hệ thống |
| Tài khoản đích mang `has_permission_bypass` **hoặc** đang giữ vai trò `is_system` của đơn vị đích | Không thuộc cả hai thì luồng này từ chối — đường của nó là `D4` |
| Mật khẩu tạm do vận hành tự gõ, đạt chính sách | Chính sách **giống nhau ở mọi môi trường**, luật **S9** |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | Vận hành | `POST /api/v1/core/system/tenants/{id}/recovery-reset-password` với tên đăng nhập và mật khẩu tạm | [`../contracts/tenants.md`](../contracts/tenants.md) §4 |
| 2 | BE | Kiểm chính sách mật khẩu tạm **trước khi** tra tài khoản đích | cùng trên — Ghi chú |
| 3 | BE | 🛑 Tra tài khoản **trong phạm vi đơn vị đích** — không trong phạm vi đơn vị hệ thống của người gọi. Phạm vi đơn vị đích mở **bên trong** service tạo đơn vị (`ITenantProvisioningService`), vốn đã có trong allowlist của luật **A12** — không thêm mục allowlist nào | [`../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md) · [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11 |
| 4 | BE | Kiểm tài khoản đích mang `has_permission_bypass` **hoặc** giữ vai trò `is_system` của đơn vị. Không có tài khoản đó **hoặc** không đủ điều kiện ⇒ cùng một mã lỗi | [`../contracts/tenants.md`](../contracts/tenants.md) §4 |
| 4b | Vận hành | **Rẽ nhánh** — đơn vị không còn tài khoản nào đủ điều kiện ở bước 4: tạo tài khoản quản trị **mới** mang cờ đặc quyền, `POST /api/v1/core/system/tenants/{id}/admins`, rồi đi tiếp từ bước 9 | [`../contracts/tenants.md`](../contracts/tenants.md) §6 |
| 5 | BE | Đặt mật khẩu tạm qua `UserManager`, bật cờ buộc đổi mật khẩu | cùng trên |
| 6 | BE | 🛑 Đổi `security_stamp` ⇒ **mọi phiên đang mở** của tài khoản đích bị chấm dứt | cùng trên |
| 7 | BE | Ghi nhật ký kiểm toán **hai dòng**: một ở đơn vị hệ thống, một ở đơn vị đích | [`../contracts/tenants.md`](../contracts/tenants.md) §5 · [`../database/schema-core.md`](../database/schema-core.md) §9.4 |
| 8 | BE | Trả thành công — **không** trả dữ liệu nào về tài khoản đích | [`../contracts/tenants.md`](../contracts/tenants.md) §4 |
| 9 | Vận hành | Chuyển mật khẩu tạm cho đơn vị — ngoài hệ thống | xem §6 |
| 10 | Quản trị đơn vị | Đăng nhập, rồi buộc đổi mật khẩu | luồng `D1`, rồi `D2` |

### Vì sao chỉ nhận người giữ cửa quản trị

Vận hành hệ thống **không** thấy dữ liệu nghiệp vụ của đơn vị nào ([`../adr/0017-khu-quan-tri-he-thong.md`](../adr/0017-khu-quan-tri-he-thong.md)). Một endpoint đặt lại mật khẩu cho **bất kỳ** ai trong **bất kỳ** đơn vị nào sẽ biến vai đó thành vai chiếm được mọi tài khoản của toàn hệ. Giới hạn vào tài khoản mang cờ hoặc giữ vai trò hệ thống là giới hạn vào đúng những người mở lại được đường quản trị của đơn vị, ở đúng ca không đường nào khác mở được. Nhận cả vai trò hệ thống vì đơn vị đã từ bỏ cờ (luồng `V1` bước 9) thì cửa quản trị nằm ở vai trò — [`../contracts/tenants.md`](../contracts/tenants.md) §4, Ghi chú. Lý do đầy đủ và phương án đã loại: [`../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md).

### Vì sao bước 2 đứng trước bước 3, và bước 4 gộp hai ca vào một mã

Vận hành không có quyền thấy ai tồn tại trong đơn vị. Phân biệt "không có tài khoản này" với "tài khoản này không mang cờ" là cho họ một công cụ **dò tên đăng nhập** của đơn vị — đúng thứ vai đó không được có.

Mã lỗi chính sách mật khẩu cũng phải không nói gì về đích. Kiểm chính sách **sau** khi tra thì mã đó chỉ xuất hiện khi đích có thật, và lỗ dò mở lại qua cửa sau.

## 4. Hỏng ở đâu — và ai thấy gì

> 📖 Loại lỗi và HTTP status của từng mã: [`../contracts/tenants.md`](../contracts/tenants.md) §4. Bảng dưới chỉ giữ `code`.

| Ca | Mã lỗi | Thấy gì |
| --- | --- | --- |
| Tài khoản gọi không mang cờ vận hành | `CORE.AUTH.FORBIDDEN` | |
| Không có đơn vị đó | `CORE.TENANT.NOT_FOUND` | |
| Mật khẩu tạm không đạt chính sách | `CORE.TENANT.RECOVERY_RESET_FAILED` | Lý do ở `fieldErrors`. Không nói gì về việc tài khoản đích có tồn tại hay không |
| Không có tên đăng nhập đó trong đơn vị, **hoặc** tài khoản không mang cờ đặc quyền và không giữ vai trò hệ thống | `CORE.TENANT.RECOVERY_TARGET_NOT_ELIGIBLE` | **Cùng một câu cho hai ca** — cố ý. Vận hành phải quay lại hỏi đơn vị, không đoán tiếp |
| Bước 2 chạy sau bước 3 | không có mã lỗi mới | 🛑 Mã lỗi chính sách chỉ hiện khi đích có thật — người gọi dò được tên đăng nhập dù bước 4 đã gộp mã |
| Bước 3 tra trong phạm vi sai | không có mã lỗi | 🛑 Hoặc không thấy ai — tài khoản có thật bị báo "không đủ điều kiện"; hoặc truy vấn chạy như không có bộ lọc — và mật khẩu bị đặt lại cho **tài khoản trùng tên ở đơn vị khác** |
| Bước 6 bị bỏ | không có mã lỗi | 🛑 Người đang giữ phiên của tài khoản đích vẫn thao tác bình thường. Vận hành thấy "thành công" và tin là xong |
| Bước 7 chỉ ghi một dòng | không có mã lỗi | 🛑 Đơn vị đích không thấy trong nhật ký của chính nó rằng tài khoản quản trị của mình vừa bị người ngoài đặt lại mật khẩu |

## 5. Quan hệ với đơn vị

**Người thao tác và đối tượng thuộc HAI đơn vị khác nhau** — cùng nhóm với `V2` và `V3`.

Người thao tác thuộc đơn vị hệ thống; đối tượng là **một tài khoản** trong một đơn vị nghiệp vụ. Tên đăng nhập chỉ duy nhất **trong phạm vi một đơn vị** ([`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11), nên cặp *(đơn vị đích, tên đăng nhập)* mới định danh được một người — và bước 3 là chỗ cặp đó được ghép lại.

Khác `V2` ở một điểm: `V2` tạo ra đơn vị rồi mới ghi vào nó; luồng này **đọc và ghi vào một đơn vị đã có dữ liệu**. Tra nhầm phạm vi ở đây không để lại một đơn vị nửa vời — nó đổi mật khẩu của **một người thật khác**.

## 6. Câu chưa trả lời được

Không còn câu riêng của luồng này. Xác minh người yêu cầu và kênh chuyển mật khẩu tạm (bước 9) là **quy trình vận hành ngoài phần mềm** ([`../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md)); đơn vị không còn ai đủ điều kiện đi bước 4b.
