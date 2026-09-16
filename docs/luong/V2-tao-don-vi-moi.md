---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `V2` — Tạo một đơn vị mới

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Đây là một trong **ba** đường được phép sinh ra một tài khoản mang cờ đặc quyền (luật **M12**): lệnh bootstrap (`V1`), tạo đơn vị (luồng này), và tạo quản trị mới cho đơn vị đã có ([`../contracts/tenants.md`](../contracts/tenants.md) §6). Cùng `V3` và `V4`, nó thuộc nhóm luồng mà người thao tác đứng **ngoài** đơn vị bị tác động.

---

## 1. Ai bắt đầu, ở đâu

**Tài khoản vận hành hệ thống**, từ khu quản trị hệ thống.

Từ đơn vị thứ hai trở đi, việc này **không cần quyền shell** — khác đơn vị nghiệp vụ đầu tiên, do lệnh bootstrap ở luồng `V1` tạo. Hai đường vào gọi **cùng một service tạo đơn vị** ([`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md)).

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Tài khoản mang `is_system_operator` | Đăng nhập được, tức luồng `V1` đã chạy xong |
| Mã đơn vị chưa tồn tại | Mã là thứ người dùng gõ ở form đăng nhập; quy tắc chuẩn hoá ở [`../contracts/tenants.md`](../contracts/tenants.md) §2 |
| Danh mục quyền đã có trong database | Dùng chung toàn hệ, vào database bằng migration lúc áp lược đồ (luồng `V1` bước 2) |
| Các nguồn seed đã đăng ký và qua phép kiểm lúc khởi động | Nguồn seed khai một khoá quyền lạ thì ứng dụng không khởi động — luồng này không bao giờ bắt đầu được |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | Vận hành | `POST /api/v1/core/system/tenants` kèm mã, tên, **thông tin tài khoản quản trị đầu tiên** và **mật khẩu tạm do vận hành tự gõ** | [`../contracts/tenants.md`](../contracts/tenants.md) §2 |
| 2 | BE | Service tạo đơn vị thêm một dòng vào bảng đơn vị | [`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md) · [`../database/schema-core.md`](../database/schema-core.md) §1.3 |
| 3 | BE | Mở **ngữ cảnh thực thi của đơn vị vừa tạo**, rồi mới ghi tiếp | [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11.4 |
| 4 | BE | Seed theo thứ tự: vai trò mặc định · ánh xạ vai trò→quyền · tài khoản quản trị đầu tiên · menu. Vai trò, ánh xạ và menu lấy từ **các nguồn seed đã đăng ký** (`ITenantSeedSource`); Core tự đăng ký menu của Core | [`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md) · [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11.4 |
| 5 | BE | Tài khoản đó mang **hai** cờ: buộc đổi mật khẩu **và** cờ đặc quyền | [`../contracts/tenants.md`](../contracts/tenants.md) §2 |
| 6 | BE | Ghi nhật ký kiểm toán **hai dòng**: một ở đơn vị hệ thống, một ở đơn vị vừa tạo | [`../contracts/tenants.md`](../contracts/tenants.md) §5 · [`../database/schema-core.md`](../database/schema-core.md) §9.4 |
| 7 | BE | Trả về đơn vị vừa tạo — **không** trả mật khẩu trong thân phản hồi | [`../contracts/tenants.md`](../contracts/tenants.md) §2 |
| 8 | Vận hành | Chuyển mật khẩu tạm cho quản trị đơn vị mới — ngoài hệ thống | xem §6 |

### Vì sao bước 3 không bỏ được

Giống hệt bước 4 của luồng `D1`: ghi khi ngữ cảnh đơn vị còn rỗng thì interceptor **từ chối lưu** (luật **M8**). Ở đây hậu quả nặng hơn — nó làm hỏng giữa chừng một quy trình nhiều bước, và cả thao tác tạo đơn vị bị huỷ.

### Vì sao bước 2–6 là một transaction

Một đơn vị có dòng nhưng thiếu vai trò, thiếu tài khoản quản trị hay thiếu menu là thứ không ai chẩn đoán được từ giao diện. Bước 2–6 nằm trong **một transaction** nên ca đó không tồn tại: hỏng ở bước nào thì như chưa gọi — không dòng đơn vị, không tài khoản, không nhật ký ([`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md) · [`../contracts/tenants.md`](../contracts/tenants.md) §2).

### Vì sao tài khoản đầu tiên phải mang cờ đặc quyền

Ngay sau bước 4, đơn vị mới **chưa có ai gán vai trò cho ai**. Tài khoản chỉ mang cờ buộc đổi mật khẩu thì đăng nhập được và **không làm được gì** — kể cả việc tạo vai trò đầu tiên. Đó là cùng vòng lặp con-gà-quả-trứng mà luồng `V1` giải ở mức cả hệ, nay lặp lại ở mức một đơn vị.

Khi không nguồn seed nào khai vai trò, cờ này là thứ **duy nhất** cho đơn vị mới dùng được.

## 4. Hỏng ở đâu — và người vận hành thấy gì

> 📖 Loại lỗi và HTTP status của từng mã: [`../contracts/tenants.md`](../contracts/tenants.md) §2. Bảng dưới chỉ giữ `code`.

| Ca | Mã lỗi | Thấy gì |
| --- | --- | --- |
| Mã đơn vị trùng | `CORE.TENANT.CODE_DUPLICATE` | |
| Mật khẩu tạm không đạt chính sách, hoặc thông tin tài khoản quản trị bị Identity từ chối | `CORE.TENANT.ADMIN_CREATE_FAILED` | Lý do ở `fieldErrors` |
| Seed hỏng giữa chừng | `CORE.TENANT.SEED_FAILED` | **Không dòng nào còn lại** — không đơn vị, không tài khoản, không nhật ký. Mã đơn vị vẫn trống; sửa nguyên nhân rồi gửi lại |
| Bước 2–6 không nằm trong cùng một transaction | không có mã lỗi | 🛑 Hỏng giữa chừng để lại một đơn vị thiếu vai trò hoặc thiếu menu, và lần gửi lại vướng `CORE.TENANT.CODE_DUPLICATE` cho chính đơn vị hỏng đó |
| Quên bước 5 (chỉ đặt một cờ) | không có mã lỗi | 🛑 Quản trị đơn vị mới đăng nhập được, đổi mật khẩu xong, rồi **bấm gì cũng bị từ chối**. Không lỗi nào bắn ra |
| Bước 6 chỉ ghi một dòng | không có mã lỗi | 🛑 Một trong hai bên mất dấu vết: hoặc đơn vị mới không thấy trong nhật ký của chính nó ai đã tạo nó và tài khoản quản trị đầu tiên, hoặc nhật ký người vận hành không có việc họ vừa làm |

## 5. Quan hệ với đơn vị

**Người thao tác và đối tượng thuộc HAI đơn vị khác nhau** — cùng nhóm với `V3` và `V4`.

Người thao tác thuộc đơn vị hệ thống; đối tượng là một đơn vị nghiệp vụ. Bước 3 chuyển ngữ cảnh từ đơn vị này sang đơn vị kia **trong cùng một request**, bằng đường mở phạm vi đã khai cho service tạo đơn vị (luật **A12**).

Hệ quả cần chú ý: sau bước 4, ngữ cảnh phải trả về đúng chỗ cũ — bước 6 còn ghi một dòng ở đơn vị hệ thống. Rò ngữ cảnh sang các bước sau của cùng request là một lỗi **không có triệu chứng ngay**.

## 6. Câu chưa trả lời được

Không còn câu riêng của luồng này. Kênh chuyển mật khẩu tạm (bước 8) là **quy trình vận hành ngoài phần mềm** ([`../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md)).
