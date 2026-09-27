---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0087 — Khôi phục quản trị đơn vị không gỡ khoá; tài khoản quản trị bị khoá mà đơn vị không tự mở được thì lối ra là tạo quản trị mới

> **Trạng thái:** Đã chấp nhận (2026-09-24)
>
> **Bổ sung [`0085-khoa-tai-khoan-mang-co-bypass-luon-bi-chan.md`](0085-khoa-tai-khoan-mang-co-bypass-luon-bi-chan.md).** ADR này trả lời câu hỏi mà mục *Một câu hỏi ADR này không trả lời* của ADR-0085 để ngỏ. Mọi quyết định của ADR-0085 giữ nguyên hiệu lực.

## Bối cảnh

ADR-0085 để ngỏ một câu hỏi: lệnh khôi phục ở [`../contracts/tenants.md`](../contracts/tenants.md) §4 có nên gỡ khoá tài khoản đích không. Hôm nay lệnh đó **không** gỡ khoá. Đối chiếu ngày 2026-09-24: `RecoveryResetAdminPasswordAsync(` trong `src/BE/Core/CoreAndSkill.Core.Infrastructure/Tenants/TenantProvisioningService.cs` không gọi `SetLockoutEndDateAsync`. ADR-0085 đưa ra ba lối: giữ nguyên, gỡ mọi khoá, hoặc chỉ gỡ khoá tự động.

Đã có hai sự thật từ trước:

- **ADR-0085 đổi tập đích có thể bị khoá tay.** Tài khoản mang `has_permission_bypass` không còn bị khoá tay được. Người mang cờ nay khoá được người giữ vai trò `is_system`.
- **Endpoint tạo quản trị bổ sung** ([`../contracts/tenants.md`](../contracts/tenants.md) §6) không kiểm đơn vị còn đích khôi phục đủ điều kiện hay không. `CreateAdditionalAdminAsync(` không có phép kiểm đó.

## Quyết định

Người dùng chốt ngày 2026-09-24:

1. **Lệnh khôi phục chỉ đặt lại mật khẩu, không gỡ khoá.** `lockout_end` và `locked_by_admin` giữ nguyên.
2. **Khi tài khoản quản trị bị khoá tay và trong đơn vị không còn ai mở được khoá, lối ra là §6:** người vận hành tạo một tài khoản quản trị mới mang cờ.
3. **Mở khoá tiếp tục không áp luật nào.** Người dùng xác nhận quyết định 3 của ADR-0085. Câu *"tài khoản mang cờ chỉ quản lý được từ khu hệ thống"* **không** tính mở khoá.

## Phương án đã cân nhắc và vì sao loại

### Phương án B — Khôi phục gỡ mọi khoá

**Được:** một lệnh mở lại cửa quản trị trong mọi ca, kể cả khi đích bị khoá tay.

**Mất:** người vận hành, đứng ngoài đơn vị, hoàn tác một quyết định khoá do chính đơn vị đưa ra. Ví dụ: đơn vị khoá một quản trị viên vì người đó nghỉ việc. Người đó gọi người vận hành xin "khôi phục", và nhận lại một tài khoản vừa được mở khoá.

**Vì sao loại:** ca cần mở lại cửa đã có lối ra ở §6. Lối đó không hoàn tác quyết định nào của đơn vị: tài khoản mới là một tài khoản khác, và đơn vị tự quyết mở khoá ai.

### Phương án C — Khôi phục chỉ gỡ khoá tự động

**Được:** phủ trọn mọi đích mang cờ — sau ADR-0085, loại đích này chỉ còn bị khoá tự động. Không hoàn tác quyết định nào của đơn vị.

**Mất:** thêm một nhánh vào lệnh khôi phục. Đổi lại chỉ bớt được thời gian chờ tới `lockout_end`, vốn là cấu hình có hạn ([`../contracts/auth.md`](../contracts/auth.md) §3, *Khoá tự động*).

**Vì sao loại:** lợi ích quá nhỏ so với một thay đổi code trên đường khôi phục xuyên đơn vị.

## Hệ quả

### Tích cực

- Lệnh khôi phục giữ đúng một việc — đặt lại mật khẩu — và không đổi code.
- Người vận hành không bao giờ hoàn tác quyết định khoá của một đơn vị.

### Tiêu cực

| Cái giá | Nghĩa là |
| --- | --- |
| **Khôi phục có thể trả 200 cho một tài khoản vẫn không đăng nhập được** | Đích bị khoá tay thì mật khẩu mới được đặt nhưng đăng nhập vẫn nhận `CORE.AUTH.LOCKED_OUT`. Phản hồi của §4 không cho người vận hành biết điều này, vì §4 cố ý không trả dữ liệu nào về đích |
| **Lối ra §6 đẻ thêm tài khoản mang cờ** | Mỗi lần dùng, đơn vị có thêm một tài khoản không ai trong đơn vị khoá được (ADR-0085). Số tài khoản mang cờ vốn đã tăng theo số đơn vị ([`0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md)); lối này làm nó tăng thêm |
| **Đích mang cờ bị khoá tự động phải chờ** | Sau khi khôi phục, tài khoản chưa đăng nhập được cho tới khi hết khoá tự động, hoặc tới khi một người trong đơn vị mở khoá |

### Rút lui nếu sai

Chọn phương án B hoặc C bằng một ADR thay thế. Việc thi công gói trong `RecoveryResetAdminPasswordAsync(`: thêm lời gọi gỡ khoá trước bước đổi security stamp, cùng một test. Không có dữ liệu nào cần chuyển.

### Dấu hiệu quyết định này bắt đầu sai

- Người vận hành khôi phục rồi nhận phản ánh *"vẫn bị khoá"*, lặp lại nhiều lần.
- §6 được dùng như lối khôi phục thường ngày, chứ không còn là lối cuối.

## Liên quan

- [`0085-khoa-tai-khoan-mang-co-bypass-luon-bi-chan.md`](0085-khoa-tai-khoan-mang-co-bypass-luon-bi-chan.md) — câu hỏi mà ADR này trả lời
- [`0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md) — lệnh khôi phục và §6
- [`../contracts/tenants.md`](../contracts/tenants.md) §4, §6
