---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0085 — Không ai trong đơn vị khoá được tài khoản mang `has_permission_bypass`; người gọi mang cờ đó được tính như mang vai trò hệ thống khi khoá

> **Trạng thái:** Đã chấp nhận (2026-09-24) · Bổ sung bởi ADR-0087 (2026-09-24) — [`0087-khoi-phuc-quan-tri-khong-go-khoa.md`](0087-khoi-phuc-quan-tri-khong-go-khoa.md)

## Bối cảnh

`core-reviewer` báo một lỗ mức **Nghiêm trọng** ở luật 4 của [`../contracts/users.md`](../contracts/users.md) §2. Luật đó chỉ bảo vệ hai loại đích: tài khoản giữ vai trò `is_system`, và tài khoản vận hành. Tài khoản quản trị mặc định của một đơn vị không thuộc loại nào: nó mang `has_permission_bypass` và không giữ vai trò nào. `src/BE/Core/CoreAndSkill.Core.Application/Tenants/CreateTenantCommandHandler.cs` truyền `AdminHasPermissionBypass: true`; `src/BE/Core/CoreAndSkill.Core.Application/Users/UserPrivilegeGuard.cs`, hàm `EnsureCanLockAsync(`, chỉ xét `IsSystemOperatorAsync(` và `UserHoldsAnySystemRoleAsync(` (đọc 2026-09-24, trước lượt sửa của `backend-expert`).

Hệ quả, lần theo code chứ chưa chạy thử:

1. Người chỉ có `core.user.lock` khoá được quản trị mặc định của đơn vị mình.
2. Khoá tay không tự hết hạn — `src/BE/Core/CoreAndSkill.Core.Infrastructure/Identity/UserAdminService.cs`, hàm `SetLockoutAsync(`, đặt `DateTimeOffset.MaxValue`.
3. Lối khôi phục của khu hệ thống ([`../contracts/tenants.md`](../contracts/tenants.md) §4) không gỡ khoá. `RecoveryResetAdminPasswordAsync(` trong `src/BE/Core/CoreAndSkill.Core.Infrastructure/Tenants/TenantProvisioningService.cs` chỉ đặt mật khẩu, bật cờ đổi mật khẩu và đổi security stamp; thân hàm không gọi `SetLockoutEndDateAsync`.

Chiều ngược lại cũng lệch. Quản trị mặc định có tập quyền hiệu lực là **toàn bộ** danh mục, nhưng luật 4 xét vai trò của người gọi chứ không xét cờ. Vì vậy chính tài khoản đó lại không khoá được một quản trị viên giữ vai trò `is_system`.

Có sẵn hai tiền lệ cùng khuôn. Vế tài khoản vận hành của luật 4 chặn mọi người gọi. Vế 2 của luật 5 chặn đặt lại mật khẩu cho đích mang cờ, dù người gọi là ai ([`0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md)).

Ràng buộc có thật: thay đổi chỉ nằm trong code và hợp đồng, không đụng lược đồ. `backend-expert` thi công song song.

## Quyết định

Người dùng chốt ngày 2026-09-24:

1. **Đích mang `has_permission_bypass` thì luật 4 luôn chặn**, trả `CORE.USER.SYSTEM_ROLE_LOCK_FORBIDDEN`, bất kể người gọi là ai — kể cả khi người gọi cũng mang cờ đó.
2. **Người gọi mang `has_permission_bypass` được tính như mang vai trò hệ thống** khi xét luật 4.

Kiến trúc sư chốt thêm, để khớp hợp đồng đang có — đây không phải quyết định mới:

3. **Mở khoá tiếp tục không áp luật nào**, kể cả khi đích mang cờ.

Hợp đồng nằm ở [`../contracts/users.md`](../contracts/users.md) §2: mục luật 4 và mục *Mở khoá*.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Đối xứng: đích mang cờ chỉ khoá được bởi người mang vai trò hệ thống hoặc mang cờ

**Được:** một luật, không vế nào chặn tuyệt đối. Người giữ quản trị mặc định nghỉ việc thì đơn vị vẫn tự khoá được tài khoản đó.

**Mất:** một đơn vị có thể có nhiều tài khoản mang cờ ([`../contracts/tenants.md`](../contracts/tenants.md) §6), nên tài khoản mang cờ này khoá được tài khoản mang cờ kia. Người giữ vai trò hệ thống cũng khoá được quản trị mặc định. Kẻ chiếm được **một** tài khoản quản trị bất kỳ vẫn đóng được cửa quản trị mặc định, và khôi phục không mở lại cửa đó.

**Vì sao loại:** nó để ngỏ đúng con đường mà vế 2 của luật 5 đã đóng với thao tác đặt lại mật khẩu. Hai luật sẽ đối xử khác nhau với cùng một cờ.

### Phương án B — Giữ nguyên luật 4, cho lệnh khôi phục gỡ luôn khoá

**Được:** không đổi luật nào. Cửa quản trị luôn mở lại được từ khu hệ thống.

**Mất:** người chỉ có `core.user.lock` vẫn khoá được quản trị mặc định, và khoá bao nhiêu lần cũng được. Mỗi lần khoá tốn một lượt khôi phục của người vận hành, kèm một lần đổi mật khẩu bắt buộc.

**Vì sao loại:** nó vá hậu quả chứ không vá lỗ. Việc một quyền đóng được cửa quản trị vẫn còn nguyên, chỉ là dọn dẹp rẻ hơn.

### Phương án C — Luật 4 so tập quyền, như vế 1 của luật 5

Người gọi chỉ khoá được đích khi tập quyền hiệu lực của người gọi bao trùm tập quyền của đích.

**Được:** guard không phải đọc cờ của người gọi. Bộ kiểm quyền đã trả toàn bộ danh mục cho người mang cờ, nên vế 2 tự đúng mà không cần đọc cờ thêm một lần — tránh được dấu hiệu mà [`0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md) cảnh báo.

**Mất:**

- Luật 4 đổi nghĩa với **mọi** cặp người gọi–đích, không riêng ca có cờ. Người không giữ vai trò hệ thống nhưng đủ quyền sẽ khoá được quản trị viên. Người giữ vai trò hệ thống mà vai trò của họ cấp ít quyền hơn đích thì mất quyền khoá.
- Mỗi lần khoá phải dựng tập quyền hiệu lực hai lần — đúng chi phí mà nợ B15 ở [`../DEBT.md`](../DEBT.md) đang để mở.
- Vẫn phải đọc cờ của đích. Hai tài khoản mang cờ có cùng tập quyền là toàn bộ danh mục, nên chúng bao trùm nhau và khoá được nhau.

**Vì sao loại:** nó rộng hơn cái lỗ cần bịt, và đổi hành vi của những cặp người gọi–đích đang đúng.

### Phương án D — Áp luật 4 cho cả mở khoá

**Được:** câu *"tài khoản mang cờ chỉ quản lý được từ khu hệ thống"* đúng theo nghĩa trọn vẹn nhất.

**Mất:** khu hệ thống không có lệnh mở khoá, và khôi phục không gỡ khoá. Tài khoản mang cờ bị khoá tự động vì đăng nhập sai sẽ chỉ còn cách chờ tới `lockout_end`. Nếu có một khoá tay đặt lên tài khoản mang cờ trước khi quyết định này thi công, sẽ không còn đường HTTP nào gỡ nó.

**Vì sao loại:** nó chặn đúng con đường sửa sai. Mở khoá không cấp gì thêm cho người gọi — đây là lý do đã ghi ở mục *Mở khoá* của hợp đồng, và lý do đó vẫn đúng với đích mang cờ.

## Hệ quả

### Tích cực

- Một quyền `core.user.lock` không còn đóng được cửa quản trị mặc định của đơn vị.
- Luật 4 và luật 5 cùng một khuôn với cờ của đích: tài khoản mang cờ chỉ quản lý được từ khu hệ thống, cả khi khoá lẫn khi đặt lại mật khẩu.
- Tài khoản có toàn bộ danh mục quyền nay khoá được quản trị viên giữ vai trò hệ thống.
- Không đổi lược đồ, không sinh dữ liệu: guard chạy trước `SetLockoutAsync(`, nên một lần khoá bị chặn không ghi gì.

### Tiêu cực

| Cái giá | Nghĩa là |
| --- | --- |
| **Không đường HTTP nào khoá được tài khoản mang cờ** | Người giữ quản trị mặc định nghỉ việc thì đơn vị không tự khoá được tài khoản đó. Đường còn lại là người vận hành chạy khôi phục (§4): mật khẩu đổi, mọi phiên bị chấm dứt, nhưng tài khoản **vẫn mở**, và mật khẩu tạm đi qua tay người vận hành. Tự bỏ cờ ([`../contracts/profile.md`](../contracts/profile.md) §3) chỉ chính chủ làm được — người đã nghỉ thì sẽ không làm |
| **Guard đọc thêm cờ, và lần đọc của vế 2 là lần đọc để CHO QUA** | Luật 5 đọc cờ của đích để **chặn**. Vế 2 đọc cờ của người gọi để **cho qua** — tức cờ đứng vào chỗ của một vai trò. ADR-0021 xếp việc này vào nhóm dấu hiệu cờ đang biến thành vai trò trá hình. Quyết định này chấp nhận việc đó và gọi đích danh ở đây |
| **Câu hiển thị của mã lỗi sai với vế mới** | Câu ở `UserErrors.SystemRoleLockForbidden` và khoá `SYSTEM_ROLE_LOCK_FORBIDDEN` của `src/FE/public/i18n/vi.json` nói *chỉ người mang vai trò hệ thống mới khoá được*. Người đang giữ vai trò hệ thống thử khoá tài khoản mang cờ sẽ đọc một câu sai về chính mình. Cần đổi sang một câu không nêu lý do, cùng khuôn với `RESET_PASSWORD_TARGET_FORBIDDEN` |
| **Vế luôn chặn không khớp gọn phép thử 403/422 của §2** | Không ai trong đơn vị khoá được tài khoản mang cờ. Câu trả lời đúng vì thế gần với *"việc này không làm được, kể cả với bạn"* (422) hơn là *"bạn không đủ tư cách"*. Vẫn giữ 403 và cùng mã, vì tách mã hoặc tách `type` sẽ cho người gọi biết đích mang cờ nào — cùng lý do ba vế của luật 5 dùng chung một mã. Vế tài khoản vận hành cũng đã dùng 403 |
| **Mỗi lần khoá tốn thêm lượt đọc cờ** | Đọc cờ của đích, và của người gọi khi đích giữ vai trò hệ thống. Chi phí nhỏ trên một thao tác hiếm |

### Rút lui nếu sai

Quyết định chỉ nằm trong code và hợp đồng. Rút lui gồm ba bước:

1. Viết một ADR thay thế.
2. `backend-expert` gỡ hai nhánh mới trong `EnsureCanLockAsync(` cùng các unit test của chúng.
3. Kiến trúc sư sửa mục luật 4 ở `users.md` §2.

Không có dữ liệu cần chuyển, vì lần khoá bị chặn không ghi gì. Một điều kiện trước khi gỡ vế 1: lỗ gốc phải có lời giải khác trước, thường là phương án B. Gỡ vế 1 mà không có lời giải thay thì lỗ Nghiêm trọng mở lại.

### Dấu hiệu quyết định này bắt đầu sai

- Có yêu cầu khoá một quản trị mặc định vừa nghỉ việc, và câu trả lời duy nhất là SQL tay. Lúc đó cần một lệnh khoá ở khu hệ thống, và lệnh đó cần ADR riêng.
- Cờ `has_permission_bypass` bị đọc ở một chỗ thứ tư ngoài bộ kiểm quyền. Hoặc một luật khác cũng *"tính người mang cờ như mang vai trò hệ thống"*. Lúc đó nên xét lại phương án C.
- Một đơn vị đã tự bỏ cờ mà tất cả quản trị viên đều bị khoá. Vế 1 không bảo vệ được đơn vị không còn tài khoản mang cờ — xem câu hỏi dưới đây.

## Một câu hỏi ADR này không trả lời — khôi phục có gỡ khoá không

**Hiện trạng** (đối chiếu 2026-09-24): lệnh khôi phục không gỡ khoá. Neo ở [`../contracts/tenants.md`](../contracts/tenants.md) — dòng mở đầu bằng *§4 không gỡ khoá* trong bảng đầu file.

Sau quyết định này, tài khoản đích của lệnh khôi phục có thể đang bị khoá theo các cách sau:

| Đích | Khoá có thể có | Khôi phục không gỡ khoá thì sao |
| --- | --- | --- |
| Mang cờ | Chỉ khoá tự động — luật 4 chặn khoá tay | Tài khoản đăng nhập được khi khoá tự động hết hạn, hoặc khi một người trong đơn vị mở khoá |
| Giữ vai trò `is_system` | Khoá tay của người giữ vai trò hệ thống hoặc người mang cờ; khoá tự động | Khoá tay không bao giờ tự hết. Khôi phục đặt mật khẩu mới cho một tài khoản vẫn không đăng nhập được |
| Mang cờ, bị khoá tay trước khi quyết định này thi công (nếu có dữ liệu như vậy) | Khoá tay | Như dòng trên |

Ca tốn kém nhất: đơn vị đã tự bỏ cờ, và quản trị viên còn lại khoá tất cả quản trị viên khác. Khi đó khôi phục một người đã bị khoá không mở lại được cửa quản trị. Ca tương tự: hai quản trị viên khoá nhau cùng lúc, cả hai đều qua guard, và đơn vị không còn ai đăng nhập được.

Hai ca đó đã có một lối ra mà không cần đổi lệnh khôi phục: người vận hành tạo một tài khoản mới mang cờ ([`../contracts/tenants.md`](../contracts/tenants.md) §6). Nhờ quyết định 1, không ai trong đơn vị khoá được tài khoản đó. Nhờ quyết định 2, nó khoá được quản trị viên giữ vai trò hệ thống. Và vì mở khoá không áp luật nào, nó mở khoá được mọi người. Card §6 gọi endpoint đó là *lối cuối*, nhưng đó là hướng dẫn dùng chứ không phải phép kiểm: `CreateAdditionalAdminAsync(` trong `TenantProvisioningService.cs` không xét xem đơn vị còn đích đủ điều kiện hay không (đọc 2026-09-24).

Có ba lối. **Người dùng chốt:**

1. **Giữ nguyên** — khôi phục chỉ đặt lại mật khẩu. Không tốn gì thêm, nhưng ở hai ca trên, cửa quản trị không mở lại được.
2. **Gỡ mọi khoá** — đặt `lockout_end = null` và `locked_by_admin = false`. Cửa luôn mở lại được. Cái giá: người vận hành, từ ngoài đơn vị, hoàn tác một quyết định khoá của quản trị trong đơn vị. Với đích đang bị khoá tay, `AuditLogInterceptor` ghi dòng `core.user.unlock` ở đơn vị đích — nó bắt thay đổi của `LockedByAdmin` — mang dấu xuyên đơn vị. Gỡ khoá tự động thì không sinh dòng nào.
3. **Chỉ gỡ khoá tự động** (khi `locked_by_admin = false`), giữ nguyên khoá tay. Phủ trọn mọi đích mang cờ, và không hoàn tác quyết định của đơn vị. Không giải được ca quản trị viên còn lại khoá tất cả người khác.

## Liên quan

- [`../contracts/users.md`](../contracts/users.md) §2 — luật 4, mục *Mở khoá*; §8 — khoá và mở khoá
- [`../contracts/tenants.md`](../contracts/tenants.md) §4 — khôi phục; §6 — tạo thêm tài khoản mang cờ
- [`0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md) — ngữ nghĩa cờ `has_permission_bypass`, dấu hiệu cờ biến thành vai trò trá hình
- [`0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md) — luật 5 và lối khôi phục
