---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `V1` — Cài đặt lần đầu: từ database trống tới hai đơn vị và hai tài khoản đăng nhập được

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Luồng này giải một vòng lặp: **không có đăng ký, nên chỉ quản trị tạo được tài khoản — nhưng quản trị đầu tiên thì chưa ai tạo.**
>
> Vòng đó **không cắt được bên trong HTTP**. Mọi cách cắt bằng một endpoint đều là một cánh cửa mở ra internet.

---

## 1. Ai bắt đầu, ở đâu

**Người vận hành**, từ **dòng lệnh trên chính máy chủ** — không qua trình duyệt.

Điểm mấu chốt về bảo mật: ai chạy được lệnh này thì **đã** có quyền shell và quyền vào database. Người đó vốn đã làm được mọi thứ. Luồng này **không trao thêm quyền cho ai** — nó chỉ là cách thuận tiện để người đã có toàn quyền tạo ra tài khoản đầu tiên.

**Máy dev và bản chạy thật đi cùng một lệnh.** Không có đường dựng dữ liệu thứ hai dành riêng cho dev ([`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md)).

## 2. Điều kiện trước

| Cần có | Không cần có |
| --- | --- |
| Quyền shell trên máy chủ; PostgreSQL chạy được | Bất kỳ tài khoản nào trong hệ |
| Bộ script lược đồ trong `database/scripts/` — sinh từ migration, mang theo cả dòng danh mục quyền | Bất kỳ đơn vị nào |
| `dotnet` chạy được trên máy đó | Kết nối mạng ra ngoài |
| Cấu hình và `user-secrets` khai mã, tên của hai đơn vị cùng tên đăng nhập, mật khẩu của hai tài khoản — **do người vận hành tự đặt** | Tệp `.sql` dữ liệu, hay script nạp quyền tách khỏi lược đồ |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | Người vận hành | Tạo database rỗng và tài khoản database của ứng dụng | [`../database/script-runbook.md`](../database/script-runbook.md) §3.3 |
| 2 | Người vận hành | Chạy script lược đồ → sinh các bảng `core.*` **và** dòng **danh mục quyền**. Danh mục vào database bằng migration idempotent, dùng chung toàn hệ, không thuộc đơn vị nào. Khoá của một module vào cùng hai bảng đó bằng migration **của chính module** — ngoại lệ có tên của luật E6 | cùng trên · [`../database/schema-core.md`](../database/schema-core.md) §5 · [`../database/migration-policy.md`](../database/migration-policy.md) §4.1 |
| 3 | Người vận hành | Khai cấu hình và `user-secrets` cho hai đơn vị và hai tài khoản | [`../database/script-runbook.md`](../database/script-runbook.md) §3.3 · [`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md) |
| 4 | Người vận hành | Chạy lệnh bootstrap → kiểm đủ cấu hình **trước khi ghi bất cứ gì**, rồi gọi **service tạo đơn vị dùng chung** tạo đơn vị hệ thống và tài khoản `superadmin` mang `is_system_operator` và `must_change_password` | [`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md) · [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §3.6 |
| 5 | Lệnh bootstrap | Gọi **cùng service đó** tạo đơn vị nghiệp vụ đầu tiên — seed vai trò, ánh xạ vai trò→quyền, menu từ các nguồn seed đã đăng ký — và tài khoản `admin` mang `has_permission_bypass` cùng `must_change_password`; ghi nhật ký kiểm toán, rồi **thoát**, không mở cổng. Mỗi lần tạo một đơn vị là **một transaction** | cùng trên · [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11.4 · luồng `V2` bước 2–5 |
| 6 | Vận hành hệ thống | Đăng nhập bằng `superadmin`, gõ **mã đơn vị hệ thống đã đặt ở bước 3** vào ô mã đơn vị → hệ bắt đổi mật khẩu ngay | luồng `D1`, rồi `D2` |
| 7 | Quản trị đơn vị | Đăng nhập bằng `admin` → hệ **bắt đổi mật khẩu ngay**, chưa đổi thì chưa đi đâu được | luồng `D1`, rồi `D2` |
| 8 | Quản trị đơn vị | Dựng bộ vai trò thật, gán quyền — trong đó **ít nhất một tài khoản khác, không bị khoá,** giữ `core.permission.write` qua vai trò — rồi tạo các tài khoản người dùng | luồng `P1`, `P2`, `N1` |
| 9 | Quản trị đơn vị | **Từ bỏ cờ đặc quyền** của chính tài khoản `admin`: `POST /api/v1/core/profile/renounce-permission-bypass`. Một chiều — không đường nào bật lại. Không phiên nào bị huỷ, không cấp lại cookie: tập quyền mới có hiệu lực ở request kế tiếp của mọi phiên ([`../contracts/profile.md`](../contracts/profile.md) §3) | [`../contracts/profile.md`](../contracts/profile.md) §3 |

### Vì sao hai tài khoản, không phải một

Hai vai không chồng lấn, và **không vai nào thay được vai kia**:

| | Quản trị đơn vị | Vận hành hệ thống |
| --- | --- | --- |
| Cờ | `has_permission_bypass` | `is_system_operator` |
| Thuộc | Đơn vị nghiệp vụ | Đơn vị hệ thống |
| Thấy | Mọi thứ **trong đơn vị mình** | **Danh sách** đơn vị và trạng thái |
| Không thấy | Đơn vị khác | Dữ liệu nghiệp vụ của **mọi** đơn vị |

Hai cờ **loại trừ nhau bằng ràng buộc ở database** ([`../RULES.md`](../RULES.md) M11), nên một tài khoản không mang được cả hai.

### Vì sao bước 4–5 không làm bằng SQL

Mật khẩu băm bằng PBKDF2 với salt ngẫu nhiên mỗi lần — không hàm SQL nào sinh được chuỗi băm hợp lệ ([`../database/schema-core.md`](../database/schema-core.md) §4.1). Một dòng `INSERT` tay tạo ra tài khoản **tồn tại nhưng không bao giờ đăng nhập được**.

### Vì sao bước 5 dùng chung service với luồng `V2`

Đơn vị nghiệp vụ đầu tiên và mọi đơn vị sau đó cần **cùng một** bộ dữ liệu. Hai đường dựng đơn vị là hai đường sẽ lệch nhau ngay lần đầu có người sửa một bên — nên lệnh bootstrap và endpoint tạo đơn vị gọi vào cùng một chỗ ([`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md)).

## 4. Hỏng ở đâu — và người vận hành thấy gì

| Bước | Hỏng thế nào | Thấy gì |
| --- | --- | --- |
| 2 | Một script lỗi giữa chừng | **Nếu quên `-v ON_ERROR_STOP=1`: không thấy gì cả.** `psql` mặc định chạy tiếp sau lỗi và thoát mã 0 — script hỏng vẫn kết thúc như thành công, database còn lại một nửa |
| 2 | Một khoá quyền có trong code mà migration không mang dòng tương ứng | Cổng CI đối chiếu hai chiều hằng số ↔ dòng seed (luật **B7**) phải đỏ trước khi bản đó tới máy chủ. Lọt qua thì đúng tính năng vừa thêm bị từ chối ở bản chạy thật ([`../contracts/permissions.md`](../contracts/permissions.md) §8.1) |
| 3–4 | Thiếu cấu hình hoặc thiếu secret | Lệnh bootstrap **dừng và không ghi dòng nào**. Cố ý — sinh một giá trị mặc định "cho tiện" là cách nhanh nhất để nó đi thẳng lên bản chạy thật |
| 4–5 | Mật khẩu không đạt chính sách | `UserManager` trả về thất bại, lệnh dừng. Chính sách **giống nhau ở mọi môi trường** (luật S9) |
| 4–5 | Chạy lệnh lần thứ hai | Không nhân đôi đơn vị hay tài khoản — lệnh idempotent ([`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md)) |
| 5 | Một nguồn seed khai khoá quyền không có trong danh mục | Ứng dụng **không khởi động** — lỗi lộ ra ngay lúc chạy lệnh, không đợi tới lúc có người bấm vào màn thiếu quyền |
| 5 | Seed hỏng giữa chừng | Lệnh dừng, và thao tác tạo đơn vị đó **không để lại dòng nào** — không đơn vị nửa vời nào để chẩn đoán. Sửa nguyên nhân rồi chạy lại lệnh ([`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md) · [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11.4) |
| 7 | Đăng nhập được nhưng bấm gì cũng bị từ chối | Tài khoản thiếu cờ đặc quyền. Đây là ca **không có lỗi nào bắn ra** — cùng triệu chứng với luồng `V2` §4 |
| 9 | Gọi khi đơn vị chưa có tài khoản nào khác, không bị khoá, giữ `core.permission.write` qua vai trò | Bị từ chối bằng `CORE.PROFILE.NO_OTHER_PERMISSION_ADMIN`, cờ giữ nguyên — chặn đơn vị tự khoá mình ra ngoài ([`../contracts/profile.md`](../contracts/profile.md) §3) |
| 9 | Quên bước 9 | Không ai báo. Endpoint có sẵn, nhưng gọi hay không là việc vận hành — **không ép được bằng code** |

## 5. Quan hệ với đơn vị

**Thuộc đơn vị — và luồng này tạo ra chính hai đơn vị đầu tiên: đơn vị hệ thống và đơn vị nghiệp vụ đầu tiên.**

Thứ tự bắt buộc: đơn vị trước, tài khoản sau. `core.app_user.tenant_id` là `NOT NULL`, nên không có tài khoản nào tồn tại ngoài một đơn vị. Ghi khi ngữ cảnh đơn vị còn rỗng thì interceptor **từ chối lưu** (luật M8).

Đơn vị hệ thống nhận ra bằng cột `core.tenant.is_system`, **không** bằng mã đơn vị — mã do người vận hành đặt ở bước 3 và gõ ở bước 6, và một đơn vị thật tình cờ trùng mã sẽ làm sập bản cài ([`../adr/0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](../adr/0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md)).

## 6. Câu chưa trả lời được

Không còn câu riêng của luồng này. Lời nhắc bước 9 là `NoticeBanner` ở màn hồ sơ khi tài khoản còn mang cờ ([`../Design/Screens/03-ho-so-ca-nhan.md`](../Design/Screens/03-ho-so-ca-nhan.md)).
