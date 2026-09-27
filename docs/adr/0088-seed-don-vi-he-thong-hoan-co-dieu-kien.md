---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0088 — Đơn vị hệ thống tiếp tục nhận seed như mọi đơn vị mới; việc tách nó ra được hoãn tới khi có nguồn seed ngoài Core hoặc lệnh chạy lại seed

> **Trạng thái:** Đã chấp nhận (2026-09-24)
>
> Đây là một ADR thuộc nhóm **quyết định KHÔNG làm**, loại **hoãn** ([`../wiki-core/be/08-adr-practice.md`](../wiki-core/be/08-adr-practice.md) §3.1). Mục *Điều kiện kích hoạt* ở cuối là mục bắt buộc của nhóm này.

## Bối cảnh

Ngày 2026-09-24, `core-reviewer` nêu câu hỏi V-01: đơn vị hệ thống có nhận seed không. `architect` đối chiếu code và thấy **có**, nhưng không tài liệu nào chủ ý viết ra điều đó:

- `src/BE/Core/CoreAndSkill.Core.Infrastructure/Tenants/TenantProvisioningService.cs` gọi `await ApplySeedAsync(ct);` dưới điều kiện `if (tenantWasCreated)`. Lời gọi không rẽ nhánh theo đơn vị có phải đơn vị hệ thống hay không.
- `src/BE/Core/CoreAndSkill.Core.Web/Commands/CoreCommandRunner.cs` tạo đơn vị hệ thống qua đúng service đó, với `IsSystem: true,` và `AdminIsSystemOperator: true),`.
- [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11.4 và [`0023-dich-vu-tao-don-vi-dung-chung.md`](0023-dich-vu-tao-don-vi-dung-chung.md) chỉ nói *"đơn vị mới"*. Dòng 3 của bảng §11.4 khai tài khoản đầu tiên mang `has_permission_bypass` — điều này sai với đơn vị hệ thống, vì tài khoản vận hành mang cờ loại trừ với cờ đó ([`0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md)).
- Lệnh chạy lại seed `core seed-tenant-defaults` được khai ở [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §3 với phạm vi *mọi đơn vị nghiệp vụ*, và **chưa thi công**. Tức là hai đường seed đang được mô tả với hai phạm vi khác nhau.

Thiệt hại hôm nay gần như bằng không:

- Nguồn seed duy nhất là `CoreTenantSeedSource`. Nó trả `GetRoles() => []`, `GetRolePermissions() => []`, và một ít mục menu.
- Tài khoản vận hành không có quyền nào: nó không mang cờ bypass, và luật M9 cấm gán vai trò cho nó. Vì vậy nó chỉ thấy các mục menu không đòi quyền.

## Quyết định

Người dùng chốt ngày 2026-09-24 theo **phương án D**:

1. **Không đổi code.** Đơn vị hệ thống tiếp tục nhận seed của mọi nguồn, như mọi đơn vị mới.
2. `architect` ghi hành vi này vào §11.4 của [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) cùng điều kiện xét lại, và sửa dòng 3 của bảng §11.4 để phân biệt đơn vị nghiệp vụ với đơn vị hệ thống.
3. Câu hỏi *có seed đơn vị hệ thống hay không* được xét lại khi một điều kiện ở mục *Điều kiện kích hoạt* xảy ra.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Ghi nhận việc seed đơn vị hệ thống là chủ đích, vĩnh viễn

**Được:** không đổi code, và câu hỏi đóng hẳn.

**Mất:** khi module đầu tiên đăng ký nguồn seed, vai trò và ánh xạ quyền của module sẽ thành dữ liệu chết trong đơn vị hệ thống. Mục menu nào của module không đòi quyền sẽ hiện cho người vận hành và dẫn tới màn rỗng hoặc 403 — đúng kiểu hỏng mà [`0079-khu-he-thong-khong-nhan-duong-vao-nhin-thay-duoc-o-v1.md`](0079-khu-he-thong-khong-nhan-duong-vao-nhin-thay-duoc-o-v1.md) tránh.

**Vì sao loại:** chốt vĩnh viễn một hướng khi chưa thấy seed thật của module nào.

### Phương án B — Làm ngay: không seed đơn vị hệ thống

Thêm điều kiện `!IsSystem` vào lời gọi seed.

**Được:** đơn vị hệ thống sạch ngay từ lần cài đầu. Dễ đảo lại.

**Mất:** người vận hành mất cả mục menu không đòi quyền, và sidebar của họ trống — cần `design-expert` xác nhận khung chịu được trạng thái đó. Đây là thay đổi Core, kéo theo một lượt `core-reviewer`. Các bản đã cài vẫn giữ dòng menu cũ; muốn dọn thì người dùng phải tự chạy lệnh database.

**Vì sao loại:** trả chi phí chắc chắn cho một thiệt hại chưa xảy ra.

### Phương án C — Làm ngay: chỉ seed menu của Core vào đơn vị hệ thống

**Được:** người vận hành giữ được mục menu của Core, còn seed của module không vào đơn vị hệ thống.

**Mất:** seam phải biết nguồn nào là của Core. Việc này trái quyết định 2 của ADR-0079 và cần một ADR thay thế một phần ADR đó.

**Vì sao loại:** đắt nhất trong bốn phương án, cho cùng một thiệt hại chưa xảy ra.

## Hệ quả

### Tích cực

- Không đổi code, không tốn lượt `core-reviewer`, không đụng dữ liệu đã cài.
- Hành vi được viết ra, nên người sau không phải đọc code mới biết.
- Chọn giữa A và B sẽ chính xác hơn khi đã có seed thật của module để nhìn.

### Tiêu cực

| Cái giá | Nghĩa là |
| --- | --- |
| **Hai đường seed đang mô tả hai phạm vi** | Đường tạo đơn vị seed cả đơn vị hệ thống. Lệnh chạy lại seed (khi thi công) chỉ seed đơn vị nghiệp vụ. Nếu lệnh đó được thi công mà không ai nhìn lại ADR này, đơn vị hệ thống sẽ âm thầm lệch khỏi mọi đơn vị khác |
| **Không cổng nào báo khi điều kiện kích hoạt xảy ra** | Việc một nguồn `ITenantSeedSource` mới xuất hiện ngoài Core không làm cổng nào đổi màu. Việc xét lại chỉ xảy ra nếu có người đọc ADR này hoặc §11.4 |
| **Dữ liệu seed trong đơn vị hệ thống tích luỹ theo thời gian** | Chọn B về sau thì các bản đã cài phải dọn tay |

### Điều kiện kích hoạt — xét lại quyết định này khi

1. Có **nguồn `ITenantSeedSource` đầu tiên ngoài Core** được đăng ký — tức có module đầu tiên seed vai trò, quyền hoặc menu.
2. **Lệnh chạy lại seed được thi công** (`core seed-tenant-defaults`). Lúc đó phải chốt phạm vi của cả hai đường cùng một lần.

## Liên quan

- [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11.4 — nơi ghi hành vi
- [`0023-dich-vu-tao-don-vi-dung-chung.md`](0023-dich-vu-tao-don-vi-dung-chung.md) — service tạo đơn vị dùng chung
- [`0079-khu-he-thong-khong-nhan-duong-vao-nhin-thay-duoc-o-v1.md`](0079-khu-he-thong-khong-nhan-duong-vao-nhin-thay-duoc-o-v1.md) — cùng khuôn hoãn; ràng buộc về mục menu của khu hệ thống
