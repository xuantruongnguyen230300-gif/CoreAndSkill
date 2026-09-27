---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0092 — Dòng `AuditLog`/`OutboxMessage` mà interceptor thêm vào một lượt lưu hỏng được **để nguyên** trong bộ theo dõi và ghi thành nợ; khi một trong hai điều kiện kích hoạt xảy ra, mỗi interceptor tự gỡ dòng nó đã thêm

> **Trạng thái:** Đã chấp nhận (2026-09-25) · Bổ sung bởi [ADR-0096](0096-lenh-ghi-usermanager-kiem-ket-qua-hong-thi-dung.md) (2026-09-25)

## Bối cảnh

[ADR-0089](0089-trung-unique-khi-ghi-dong-thoi-dich-o-kho-identity.md) cho kho người dùng của Core bắt vi phạm `23505` trên `UserNameIndex` và `EmailIndex`, rồi trả một `IdentityResult` thất bại thay cho ngoại lệ. Quyết định 4 của ADR đó yêu cầu kho gỡ bản ghi vừa hỏng khỏi bộ theo dõi, *"để không lần lưu nào sau đó thử ghi lại nó"*.

Kiến trúc sư đọc code ngày 2026-09-25 và thấy lệnh gỡ đó chỉ gỡ **đúng một** entity:

| Sự thật | Neo |
| --- | --- |
| Kho chỉ gỡ tài khoản vừa hỏng | `src/BE/Core/CoreAndSkill.Core.Infrastructure/Identity/AppUserStore.cs`, hàm `Forget(AppUser user)` |
| Trong cùng lượt lưu đó, hai interceptor **thêm** dòng mới vào bộ theo dõi, ngay trước lệnh ghi | `src/BE/Core/CoreAndSkill.Core.Infrastructure/Persistence/Interceptors/AuditLogInterceptor.cs`, chuỗi `context.Set<AuditLog>().Add(record.Value)`. `OutboxInterceptor.cs` cùng thư mục, chuỗi `context.Set<OutboxMessage>().Add(message.Value)` |
| `OutboxInterceptor` còn xoá danh sách sự kiện trên entity nguồn ngay khi thu | `OutboxInterceptor.cs`, chuỗi `source.ClearDomainEvents()` |
| Không interceptor nào trong Core xử lý lượt lưu hỏng | Không tệp nào dưới `src/BE/Core` chứa chuỗi `SaveChangesFailed` |

Vì vậy, sau một lượt lưu tài khoản thua do `23505`, dòng nhật ký `core.user.*` và dòng outbox của chính thay đổi đó **vẫn nằm trong bộ theo dõi, ở trạng thái Added**. Chúng mô tả một thay đổi không xảy ra.

Hôm nay các dòng đó vô hại, vì hai điều cùng đúng:

1. **Không chỗ gọi nào lưu tiếp sau một lượt thua vì `23505` trong cùng phạm vi.** Mọi lời gọi `UserManager.CreateAsync`, và mọi lời gọi `UpdateAsync` có thể đổi tên đăng nhập hay email (`UserAdminService.cs`, `UserProfileService.cs`), đều trả thất bại ngay khi nhận thất bại. `TransactionBehavior` rollback, hoặc lệnh bootstrap thoát. Đường nhập dữ liệu có lưu tiếp sau một dòng hỏng, nhưng nó gọi `ChangeTracker.Clear()` trước khi lưu tiếp (`src/BE/Core/CoreAndSkill.Core.Infrastructure/Import/EfImportRowWriter.cs`, hàm `DiscardTrackedChanges`), nên các dòng thừa bị gỡ theo.

   Có những chỗ gọi **bỏ qua** kết quả `UpdateAsync` rồi lưu tiếp: `ResetOperatorPasswordAsync` và `RecoveryResetAdminPasswordAsync` trong `TenantProvisioningService.cs` (chuỗi `await userManager.UpdateAsync(user);` ngay trước `UpdateSecurityStampAsync`). Chúng không đổi tên đăng nhập hay email nên không vấp `23505` trên hai index kia. Nhưng kho mặc định của Identity cũng đổi lỗi **xung đột đồng thời** thành một kết quả thất bại thay vì ngoại lệ, và lượt lưu hỏng vì xung đột cũng để lại đúng các dòng thừa này. Hôm nay chúng không commit được, vì entity tài khoản còn mang token cũ làm mọi lượt lưu sau trong cùng phạm vi cũng hỏng. Đó là một sự may, không phải một thiết kế. Câu hỏi *hai chỗ này có tính là điều kiện kích hoạt (a) hay không* được đưa lại cho người dùng cùng ngày.
2. **Tiền đề của ADR-0089 quyết định 4:** transaction đã hỏng sau lượt thua, nên lệnh lưu nào chạy sau cũng vấp. Tiền đề này **chưa ai kiểm**. EF mặc định đặt savepoint khi lưu trong một transaction đang mở, và lùi về savepoint khi lỗi, nên transaction có thể **vẫn dùng được**. Nợ E18 ở [`../DEBT.md`](../DEBT.md) đã ghi câu hỏi này.

Nếu cả hai điều cùng đổi, một lượt lưu tiếp theo sẽ commit dòng nhật ký kiểm toán của một thao tác không xảy ra, và phát một sự kiện tích hợp cho một thay đổi không có thật.

## Quyết định

Người dùng chốt ngày 2026-09-25:

1. **Không sửa lúc này.** Các dòng thừa được để nguyên. Chỗ hở được ghi thành nợ **E19** ở [`../DEBT.md`](../DEBT.md).
2. **Hai điều kiện kích hoạt.** Xảy ra **một trong hai** thì phải sửa, trước khi khép việc đang làm:
   - (a) xuất hiện chỗ gọi đầu tiên **lưu tiếp** sau một lệnh ghi thất bại, trong cùng phạm vi `DbContext`, mà không gỡ sạch bộ theo dõi;
   - (b) test trên PostgreSQL thật cho thấy transaction **còn dùng được** sau lượt thua, nhờ savepoint.
3. **Hướng sửa khi kích hoạt: mỗi interceptor tự gỡ dòng chính nó đã thêm** khi lượt lưu hỏng. Chỗ bắt lỗi **không** dọn hộ.

ADR-0089 còn hiệu lực nguyên vẹn. ADR này **bổ sung** cho quyết định 4 của nó: lệnh gỡ ở đó vẫn đúng và vẫn cần, nó chỉ không phủ dòng do interceptor thêm.

## Phương án đã cân nhắc và vì sao loại

Kiến trúc sư ghi các phương án dưới đây ngày 2026-09-25, sau khi người dùng đã chốt.

### Phương án — Sửa ngay theo hướng mỗi interceptor tự dọn

**Được:** chỗ hở đóng lại trước khi có ai giẫm vào.

**Mất:** mỗi interceptor phải giữ trạng thái theo từng lượt lưu (dòng nào nó vừa thêm), và phải xử lý lượt lưu hỏng ở cả nhánh đồng bộ lẫn bất đồng bộ. `OutboxInterceptor` còn phải quyết thêm một việc: trả lại danh sách sự kiện đã xoá khỏi entity, hay chấp nhận mất. Tất cả để bảo vệ một đường mà hôm nay không chỗ gọi nào đi.

**Vì sao loại:** trả chi phí chắc chắn cho một lợi ích chưa có ca nào cần. Hai điều kiện kích hoạt đều quan sát được, nên hoãn không có nghĩa là quên.

### Phương án — Dọn ở chỗ bắt lỗi

Chỗ bắt lỗi — hôm nay là `AppUserStore` — gỡ mọi dòng `AuditLog` và `OutboxMessage` đang ở trạng thái Added, hoặc gọi thẳng `ChangeTracker.Clear()`.

**Được:** sửa ở đúng một tệp, ngay cạnh chỗ đã gỡ tài khoản.

**Mất:**

- Chỗ bắt lỗi không phân biệt được dòng do interceptor thêm cho lượt lưu hỏng với dòng mà service đã thêm tường minh từ trước và chưa lưu. Có những chỗ thêm tường minh như vậy: `TenantProvisioningService.cs` và `EfAuditTrail.cs` đều gọi `db.AuditLogs.Add(`. Gỡ theo kiểu thì mất cả dòng hợp lệ. `ChangeTracker.Clear()` thì gỡ luôn mọi thứ người gọi đang giữ.
- Kho người dùng của Identity phải biết nội tình của nhật ký kiểm toán và outbox.
- Mọi chỗ bắt lỗi lưu viết sau này đều phải nhớ lặp lại khối dọn. ADR-0089 đã loại đúng khuôn *"mỗi chỗ gọi lặp một khối"* ở phương án C của nó, vì cùng lý do.

**Vì sao loại:** chỗ biết chính xác dòng nào thuộc lượt lưu hỏng là interceptor đã thêm chúng, không phải chỗ bắt lỗi.

## Hệ quả

### Tích cực

- Không thêm trạng thái hay nhánh xử lý nào vào hai interceptor cho một đường chưa ai đi.
- Hướng sửa đã chốt sẵn. Người gặp điều kiện kích hoạt không phải mở lại cuộc cân nhắc.

### Tiêu cực

| Cái giá | Nghĩa là |
| --- | --- |
| **Điều kiện (a) không cổng nào canh** | Người viết một chỗ gọi mới *"thử tạo, trùng thì làm cách khác rồi lưu"* sẽ không biết mình vừa kích hoạt nợ này. Lớp bắt được là `core-reviewer` khi soát chỗ gọi đó |
| **Nếu (a) và (b) cùng xảy ra mà không ai sửa, lỗi hoàn toàn im lặng** | Lượt lưu thứ hai thành công và commit một dòng nhật ký kiểm toán sai. Nhật ký kiểm toán là thứ người ta tin mà không kiểm lại, và dòng sai không gây lỗi nào |
| **Sự kiện đã bị xoá khỏi entity nguồn** | Nếu một chỗ gọi thử lưu lại **cùng** entity sau lượt thua, sự kiện của nó không được thu lần thứ hai. Hướng sửa ở quyết định 3 phải giải quyết luôn việc này, và nó làm phép sửa lớn hơn một lệnh gỡ |
| **Phép gỡ của ADR-0089 quyết định 4 trông như đã đủ** | Người đọc `AppUserStore` thấy lệnh gỡ và yên tâm. Chú thích trong tệp đó chưa nhắc tới hai loại dòng này |

### Rút lui nếu sai

Quyết định này không đổi dòng code nào. Rút lui nghĩa là thi công ngay hướng sửa ở quyết định 3, rồi gỡ dòng E19 khỏi `DEBT.md`. Không dữ liệu nào cần chuyển: hôm nay không dòng thừa nào tới được database.

### Dấu hiệu quyết định này bắt đầu sai

- Một trong hai điều kiện kích hoạt xảy ra. Đây là điều kiện đã khai, không phải dấu hiệu mơ hồ.
- Xuất hiện một interceptor thứ ba **thêm** dòng vào lượt lưu. Lúc đó số chỗ phải tự dọn tăng lên, và nên sửa luôn cho cả ba.
- Có đề xuất bắt và dịch `23505` cho một ràng buộc khác ngoài kho người dùng. Câu hỏi mở cuối [ADR-0089](0089-trung-unique-khi-ghi-dong-thoi-dich-o-kho-identity.md) đang chờ người dùng quyết đúng việc đó. Mỗi chỗ dịch mới là một đường nữa sinh dòng thừa.

## Việc test khi kích hoạt

- Test không database: một lượt lưu hỏng (dựng lỗi bằng tay) rồi đếm bộ theo dõi — không còn dòng `AuditLog` hay `OutboxMessage` nào ở trạng thái Added. Có một ca đối chứng: dòng `AuditLog` mà service đã thêm tường minh **trước** lượt lưu hỏng thì vẫn còn.
- Điều kiện (b) được trả lời bằng khẳng định mà nợ E18 đòi thêm vào `AppUserUniqueViolationDatabaseTests`.

## Liên quan

- [`0089-trung-unique-khi-ghi-dong-thoi-dich-o-kho-identity.md`](0089-trung-unique-khi-ghi-dong-thoi-dich-o-kho-identity.md) — quyết định 4, và câu hỏi mở về hai ràng buộc cùng lớp
- [`0052-ghi-ma-tran-phan-quyen-luon-vao-nhat-ky-kiem-toan.md`](0052-ghi-ma-tran-phan-quyen-luon-vao-nhat-ky-kiem-toan.md) — nhật ký ghi ở interceptor, không ở handler
- [`../DEBT.md`](../DEBT.md) E18, E19
