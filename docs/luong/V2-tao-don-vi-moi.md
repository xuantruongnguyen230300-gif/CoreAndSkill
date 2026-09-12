---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `V2` — Tạo một đơn vị mới

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Đây là luồng **duy nhất** ngoài lệnh bootstrap được phép sinh ra một tài khoản mang cờ đặc quyền (luật **M12**). Nó cũng là luồng duy nhất mà người thao tác đứng **ngoài** đơn vị bị tác động.

---

## 1. Ai bắt đầu, ở đâu

**Tài khoản vận hành hệ thống**, từ khu quản trị hệ thống.

Từ đơn vị thứ hai trở đi, việc này **không cần quyền shell** — khác hẳn đơn vị đầu tiên ở luồng `V1`.

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Tài khoản mang `is_system_operator` | Đăng nhập được, tức luồng `V1` đã chạy xong |
| Mã đơn vị chưa tồn tại | Mã là thứ người dùng gõ ở form đăng nhập, chuẩn hoá về chữ HOA trước khi lưu |
| Danh mục quyền đã nạp | Dùng chung toàn hệ, nạp một lần lúc cài đặt |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | Vận hành | `POST /system/tenants` kèm mã, tên, và **thông tin tài khoản quản trị đầu tiên** | [`../contracts/tenants.md`](../contracts/tenants.md) §2 |
| 2 | BE | Thêm một dòng vào bảng đơn vị | [`../database/schema-core.md`](../database/schema-core.md) §1.3 |
| 3 | BE | Mở **ngữ cảnh thực thi của đơn vị vừa tạo**, rồi mới ghi tiếp | [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11.4 |
| 4 | BE | Seed **bốn** thứ theo thứ tự: bộ vai trò · ánh xạ vai trò→quyền · tài khoản quản trị đầu tiên · menu và danh mục | cùng trên |
| 5 | BE | Tài khoản đó mang **hai** cờ: buộc đổi mật khẩu **và** cờ đặc quyền | [`../contracts/tenants.md`](../contracts/tenants.md) §2 |
| 6 | BE | Trả về đơn vị vừa tạo — **không** trả mật khẩu trong thân phản hồi | cùng trên |

### Vì sao bước 3 không bỏ được

Giống hệt bước 4 của luồng `D1`: ghi khi ngữ cảnh đơn vị còn rỗng thì interceptor **từ chối lưu** (luật **M8**). Ở đây hậu quả nặng hơn — nó làm hỏng giữa chừng một quy trình nhiều bước, và để lại một đơn vị nửa vời.

### Vì sao tài khoản đầu tiên phải mang cờ đặc quyền

Ngay sau bước 4, đơn vị mới **chưa có ai gán vai trò cho ai**. Tài khoản chỉ mang cờ buộc đổi mật khẩu thì đăng nhập được và **không làm được gì** — kể cả việc tạo vai trò đầu tiên. Đó là cùng vòng lặp con-gà-quả-trứng mà luồng `V1` giải ở mức cả hệ, nay lặp lại ở mức một đơn vị.

## 4. Hỏng ở đâu — và người vận hành thấy gì

| Ca | Mã lỗi | Thấy gì |
| --- | --- | --- |
| Mã đơn vị trùng | `CORE.TENANT.CODE_DUPLICATE` (409) | |
| Seed hỏng giữa chừng | `CORE.TENANT.SEED_FAILED` (422) | Đơn vị **được để ở trạng thái ngưng hoạt động**, không phải xoá đi. Một đơn vị "hoạt động nhưng thiếu menu" là thứ không ai chẩn đoán được từ giao diện |
| Quên bước 5 (chỉ đặt một cờ) | không có mã lỗi | 🛑 Quản trị đơn vị mới đăng nhập được, đổi mật khẩu xong, rồi **bấm gì cũng bị từ chối**. Không lỗi nào bắn ra |

## 5. Quan hệ với đơn vị

**Đây là luồng duy nhất mà người thao tác và đối tượng thuộc HAI đơn vị khác nhau.**

Người thao tác thuộc đơn vị hệ thống; đối tượng là một đơn vị nghiệp vụ. Bước 3 chuyển ngữ cảnh từ đơn vị này sang đơn vị kia **trong cùng một request** — và đó là thao tác không luồng nào khác làm.

Hệ quả cần chú ý: sau bước 4, ngữ cảnh phải trả về đúng chỗ cũ. Rò ngữ cảnh sang các bước sau của cùng request là một lỗi **không có triệu chứng ngay**.

## 6. Câu chưa trả lời được

- **Mật khẩu của quản trị đơn vị mới đi đường nào tới họ?** Phản hồi cố ý không trả mật khẩu. Vậy người vận hành đọc nó ở đâu để chuyển cho khách hàng — hay hệ sinh và gửi qua kênh khác? Không file nào nói.
- **Nhật ký kiểm toán của thao tác này ghi vào đơn vị nào?** Cùng câu hỏi chưa trả lời với luồng `N6` §6 — và luồng `V2` chính là ca làm câu hỏi đó có thật.
- **Bộ vai trò mặc định lấy ở đâu?** [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11.4 nói nó thuộc **dự án**, không thuộc Core. Vậy Core chạy bước 4 bằng cách nào khi chưa có dự án nào khai bộ đó — đây là một seam chưa có tên.
