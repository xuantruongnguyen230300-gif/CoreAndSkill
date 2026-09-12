---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `V1` — Cài đặt lần đầu: từ database trống tới hai tài khoản đăng nhập được

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Luồng này giải một vòng lặp: **không có đăng ký, nên chỉ quản trị tạo được tài khoản — nhưng quản trị đầu tiên thì chưa ai tạo.**
>
> Vòng đó **không cắt được bên trong HTTP**. Mọi cách cắt bằng một endpoint đều là một cánh cửa mở ra internet.

---

## 1. Ai bắt đầu, ở đâu

**Người vận hành**, từ **dòng lệnh trên chính máy chủ** — không qua trình duyệt.

Điểm mấu chốt về bảo mật: ai chạy được lệnh này thì **đã** có quyền shell và quyền vào database. Người đó vốn đã làm được mọi thứ. Luồng này **không trao thêm quyền cho ai** — nó chỉ là cách thuận tiện để người đã có toàn quyền tạo ra tài khoản đầu tiên.

## 2. Điều kiện trước

| Cần có | Không cần có |
| --- | --- |
| Quyền shell trên máy chủ; PostgreSQL chạy được | Bất kỳ tài khoản nào trong hệ |
| Bộ script lược đồ trong `database/scripts/` | Bất kỳ đơn vị nào |
| `dotnet` chạy được trên máy đó | Kết nối mạng ra ngoài |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | Người vận hành | Tạo database rỗng | [`../database/script-runbook.md`](../database/script-runbook.md) §6 |
| 2 | Người vận hành | Chạy script lược đồ → sinh các bảng `core.*` | cùng trên, §3.3 · [`../database/schema-core.md`](../database/schema-core.md) |
| 3 | Người vận hành | Nạp **danh mục quyền** — bộ khoá `core.user.read`… Danh mục dùng chung toàn hệ, không thuộc đơn vị nào | [`../database/schema-core.md`](../database/schema-core.md) §5 |
| 4 | Người vận hành | Nạp **dữ liệu**: đơn vị hệ thống, đơn vị nghiệp vụ đầu tiên, bộ vai trò, ánh xạ vai trò→quyền, menu | [`../adr/0022-seed-dev-khong-co-duong-code-rieng.md`](../adr/0022-seed-dev-khong-co-duong-code-rieng.md) · [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11.4 |
| 5 | Người vận hành | Đặt tên đăng nhập và mật khẩu vào `user-secrets` — **do họ tự nghĩ ra** | [`../database/script-runbook.md`](../database/script-runbook.md) §6 |
| 6 | Người vận hành | Chạy lệnh bootstrap → tạo **hai** tài khoản qua `UserManager`, đặt cờ, ghi nhật ký kiểm toán, rồi **thoát** | [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §3.6 |
| 7 | Quản trị đơn vị | Đăng nhập → hệ **bắt đổi mật khẩu ngay**, chưa đổi thì chưa đi đâu được | luồng `D1`, rồi `D2` |
| 8 | Quản trị đơn vị | Dựng bộ vai trò thật, gán quyền, rồi tạo các tài khoản người dùng | luồng `P1`, `P2`, `N1` |
| 9 | Quản trị đơn vị | **Tắt cờ đặc quyền** của tài khoản bootstrap, hoặc vô hiệu hoá nó | [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §3.6 |

### Vì sao hai tài khoản, không phải một

Hai vai không chồng lấn, và **không vai nào thay được vai kia**:

| | Quản trị đơn vị | Vận hành hệ thống |
| --- | --- | --- |
| Cờ | `has_permission_bypass` | `is_system_operator` |
| Thuộc | Đơn vị nghiệp vụ | Đơn vị hệ thống |
| Thấy | Mọi thứ **trong đơn vị mình** | **Danh sách** đơn vị và trạng thái |
| Không thấy | Đơn vị khác | Dữ liệu nghiệp vụ của **mọi** đơn vị |

Hai cờ **loại trừ nhau bằng ràng buộc ở database** ([`../RULES.md`](../RULES.md) M11), nên một tài khoản không mang được cả hai.

### Vì sao bước 6 không làm bằng SQL

Mật khẩu băm bằng PBKDF2 với salt ngẫu nhiên mỗi lần — không hàm SQL nào sinh được chuỗi băm hợp lệ ([`../database/schema-core.md`](../database/schema-core.md) §4.1). Một dòng `INSERT` tay tạo ra tài khoản **tồn tại nhưng không bao giờ đăng nhập được**.

## 4. Hỏng ở đâu — và người vận hành thấy gì

| Bước | Hỏng thế nào | Thấy gì |
| --- | --- | --- |
| 2 | Một script lỗi giữa chừng | **Nếu quên `-v ON_ERROR_STOP=1`: không thấy gì cả.** `psql` mặc định chạy tiếp sau lỗi và thoát mã 0 — script hỏng vẫn kết thúc như thành công, database còn lại một nửa |
| 4 | Seed hỏng giữa chừng | Đơn vị ở trạng thái **ngưng hoạt động**, không phải "hoạt động nhưng thiếu menu". Một đơn vị nửa vời thì không ai chẩn đoán được từ giao diện ([`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11.4) |
| 5 | Thiếu secret | Lệnh bootstrap **dừng và không ghi dòng nào**. Cố ý — sinh một mật khẩu mặc định "cho tiện" là cách nhanh nhất để nó đi thẳng lên bản chạy thật |
| 6 | Mật khẩu không đạt chính sách | `UserManager` trả về thất bại, không tài khoản nào được tạo. Chính sách **giống nhau ở mọi môi trường** (luật S9) |
| 7 | Đăng nhập được nhưng bấm gì cũng bị từ chối | Tài khoản thiếu cờ đặc quyền. Đây là ca **không có lỗi nào bắn ra** — xem §6 |
| 9 | Quên tắt cờ | Không ai báo. Đây là khuyến nghị vận hành, **không ép được bằng code** |

## 5. Quan hệ với đơn vị

**Thuộc đơn vị — và luồng này tạo ra chính đơn vị đầu tiên.**

Thứ tự bắt buộc: đơn vị trước, tài khoản sau. `core.app_user.tenant_id` là `NOT NULL`, nên không có tài khoản nào tồn tại ngoài một đơn vị. Ghi khi ngữ cảnh đơn vị còn rỗng thì interceptor **từ chối lưu** (luật M8).

Đơn vị hệ thống nhận ra bằng cột `core.tenant.is_system`, **không** bằng mã đơn vị — mã do người gõ, và một đơn vị thật tình cờ trùng mã sẽ làm sập bản cài ([`../adr/0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](../adr/0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md)).

## 6. Câu chưa trả lời được

- **Bước 9 không có cơ chế nào nhắc.** Tài khoản bootstrap mang cờ đặc quyền sống mãi nếu không ai tắt. [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §3.6 đề xuất một health check cảnh báo khi số tài khoản mang cờ vượt ngưỡng, nhưng ngưỡng đó **chưa được khai ở đâu**, và nó phải tính theo số đơn vị chứ không phải một hằng số.
- **Đường cài đặt thật ở [`../database/script-runbook.md`](../database/script-runbook.md) §3.3 chưa biết multi-tenant.** Nó chưa tạo đơn vị hệ thống, chưa tạo tài khoản vận hành. Mục §6 (đường dev) đã sửa; §3.3 thì chưa.
