---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0052 — Mọi lần ghi ma trận phân quyền có đổi ô đều vào nhật ký kiểm toán: mỗi vai trò bị chạm một dòng, kèm các khoá được cấp và bị thu

> **Trạng thái:** Đã chấp nhận (2026-09-22) · Bổ sung bởi [ADR-0054](0054-o-ma-tran-chi-doi-bang-them-dong-va-xoa-mem.md) (2026-09-22)

## Bối cảnh

[`0051-permission-write-la-khoa-goc-cua-don-vi.md`](0051-permission-write-la-khoa-goc-cua-don-vi.md) chốt rằng người giữ `core.permission.write` tự gắn được mọi khoá vào vai trò của mình. ADR đó để mở một câu trong phần cái giá: việc tự gắn khoá có truy vết được không. Người dùng trả lời câu đó ngày 2026-09-22: **giữ 0051**, và **mọi lần ghi ma trận phải vào nhật ký kiểm toán**, ghi rõ ai ghi, lúc nào, cấp hoặc thu ô nào. Mục đích là truy được mọi lần tự leo thang.

Đối chiếu code ngày 2026-09-22:

- `src/BE/Core/CoreAndSkill.Core.Infrastructure/Persistence/Interceptors/AuditLogInterceptor.cs` **đã** ghi một dòng cho mỗi vai trò có dòng `RolePermission` được thêm hoặc bị đổi cờ xoá mềm trong lượt `SaveChanges`. Chuỗi tìm được: `AuditActionCodes.PermissionMatrixUpdate, "core.role", roleId.ToString(), targetDisplay: null`. Dòng đó đã trả lời được ai, lúc nào, đơn vị nào, vai trò nào, từ IP nào.
- Dòng đó **không** nói khoá nào được cấp hay bị thu: `beforeValue` và `afterValue` của `AuditLog.Record` không được truyền. `target_display` luôn rỗng, trái với yêu cầu lưu nhãn tại thời điểm ghi ở [`../wiki-core/be/10-data-retention.md`](../wiki-core/be/10-data-retention.md) §5.2.
- `src/BE/Core/CoreAndSkill.Core.Infrastructure/Permissions/PermissionMatrixService.cs` ghi qua `ChangeTracker`: thu bằng `row.IsDeleted = true`, cấp bằng `db.RolePermissions.Add(...)`. Hai thao tác này đều đi qua interceptor.
- Chưa có test nào khẳng định dòng nhật ký đó được ghi. Chuỗi `matrix_update` trong `src/BE/Tests/` chỉ xuất hiện trong một chú thích.

Hệ quả: một lần tự leo thang hôm nay để lại dòng *"X đã sửa ma trận của vai trò R"*. Dòng đó không phân biệt được với một lần chỉnh quyền thường ngày. Nó cho biết có chuyện xảy ra, nhưng không cho biết chuyện gì.

## Quyết định

Kiến trúc sư chốt, theo lựa chọn của người dùng ngày 2026-09-22:

1. Mỗi lần ghi qua ứng dụng làm đổi **ít nhất một ô** của `core.role_permission` sinh **đúng một** dòng `core.permission.matrix_update` **cho mỗi vai trò có ô đổi**. Dòng này nằm trong cùng lượt `SaveChanges`, tức cùng transaction với thay đổi ma trận. Không ghi được nhật ký thì thay đổi cũng không được ghi.
2. Dòng mang **danh sách khoá được cấp và danh sách khoá bị thu**, dạng mã khoá quyền, cùng tên vai trò tại thời điểm ghi. Hình dạng dòng nằm ở [`../contracts/permissions.md`](../contracts/permissions.md) §6 mục *Nhật ký kiểm toán*, và mục đó là nơi duy nhất định nghĩa nó.
3. Đường ghi vẫn là `AuditLogInterceptor`, không chuyển sang handler tự gọi.
4. Mã ứng dụng **không** ghi `core.role_permission` bằng đường bỏ qua `ChangeTracker` (`ExecuteUpdateAsync`, `ExecuteDeleteAsync`, SQL thô).
5. Một lần `PUT` không đổi ô nào thì **không** sinh dòng nào.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Mỗi request một dòng, `after_value` chứa diff của mọi vai trò

**Được:** mỗi hành động của người dùng là đúng một dòng. Đọc *"lần lưu lúc 10:02 đã đổi gì"* chỉ cần một dòng.

**Vì sao loại:** câu hỏi của người điều tra leo thang là *"vai trò R có khoá K từ lúc nào, do ai gắn"*. Index `ix_audit_log_tenant_target (tenant_id, target_type, target_id)` ở [`../database/schema-core.md`](../database/schema-core.md) §9.4 trả lời câu đó khi đích là vai trò. Nếu đích là cả ma trận thì phải lục trong `jsonb` của mọi dòng. Ngoài ra, đường tạo đơn vị cũng ghi `role_permission` qua cùng interceptor, và ở đó không có "một request" nào để gom theo.

### Phương án B — Mỗi ô một dòng

**Được:** dòng không cần `jsonb`, truy vấn phẳng.

**Vì sao loại:** [`../wiki-core/be/10-data-retention.md`](../wiki-core/be/10-data-retention.md) §5.4 cấm đúng việc này: ghi hết thì bảng phình và không ai đọc nổi. Riêng lần seed ma trận của một đơn vị mới đã ra một dòng cho mỗi cặp (vai trò, khoá). Chú thích trong `AuditLogInterceptor.cs` cũng đã loại phương án này.

### Phương án C — Handler `PermissionMatrixService` tự ghi dòng tường minh

**Được:** handler có sẵn mã khoá và tên vai trò trong tay, nên interceptor không phải tra thêm.

**Vì sao loại:** việc ghi nhật ký sẽ phụ thuộc vào chỗ gọi tự nhớ. Mọi đường ghi `role_permission` khác sẽ lặng lẽ không để lại dấu: seed khi tạo đơn vị, seed của module sau này, và bất kỳ handler nào viết sau. Nguyên tắc *"gắn vào tầng dữ liệu, không phụ thuộc handler tự nhớ gọi"* ở [`../wiki-core/be/trien-khai/04-b3-van-hanh.md`](../wiki-core/be/trien-khai/04-b3-van-hanh.md) §3 tồn tại vì đúng lý do này.

### Phương án D — Ghi ảnh chụp đầy đủ tập khoá của vai trò trước và sau

**Được:** mỗi dòng tự đủ. Muốn biết trạng thái của vai trò tại thời điểm T chỉ cần đọc một dòng.

**Vì sao loại:** cỡ dòng tỉ lệ với cỡ danh mục khoá, không tỉ lệ với cỡ thay đổi. Mỗi module thêm khoá thì mọi dòng về sau phình theo. Câu hỏi người dùng đặt ra là *ai cấp hoặc thu khoá nào*, và diff trả lời trực tiếp câu đó. Cái giá của lựa chọn này ghi ở mục Hệ quả.

### Phương án E — Ghi cả lần `PUT` không đổi ô nào

**Được:** khớp nghĩa đen của câu *"mọi lần ghi"*.

**Vì sao loại:** không có ô nào đổi thì không có leo thang nào để truy. Dòng rỗng chỉ làm loãng đúng thứ người điều tra cần đọc. Đảo quyết định này là việc code nhỏ, không đổi lược đồ.

## Hệ quả

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| **Truy vết không phải ngăn chặn** | Cái giá đầu tiên của 0051 giữ nguyên: người giữ khoá vẫn tự gắn được mọi khoá. Nhật ký chỉ cho biết **sau khi** việc đã xảy ra. Màn đọc nhật ký thuộc Nhóm B ([`../database/schema-core.md`](../database/schema-core.md) §9.4), nên hôm nay đọc dòng này nghĩa là người vận hành chạy SQL |
| **Mỗi lần ghi `role_permission` tốn thêm truy vấn** | Interceptor phải tra mã khoá theo id quyền và tên vai trò theo id vai trò, trong lượt `SaveChanges`. Việc này áp cho mọi lần lưu ma trận và mọi lần tạo đơn vị |
| **Muốn biết trạng thái tại thời điểm T phải cộng dồn diff** | Theo phương án D đã loại, dòng không chứa ảnh chụp đầy đủ. Tái dựng tập khoá của vai trò tại T nghĩa là cộng dồn mọi dòng từ lúc vai trò được tạo. Chuỗi đó chỉ liền nếu **mọi** lần ghi đều đi qua ứng dụng |
| **Đường ngoài ứng dụng không để lại dấu** | Script dưới `database/scripts/`, `UPDATE` tay của người vận hành, và dữ liệu có từ trước khi quyết định này được thi công đều không sinh dòng nào. Riêng các dòng cũ có `after_value` rỗng: người đọc phải hiểu là *"không biết ô nào"*, **không** phải *"không ô nào"* |
| **Ghép chặt khả dụng** | Ghi nhật ký hỏng thì lần lưu ma trận hỏng theo. Đây là chủ đích: một thay đổi quyền không có dấu vết là thứ quyết định này tồn tại để ngăn. Nhưng nó cũng có nghĩa là một lỗi trong interceptor chặn mọi lần chỉnh quyền |
| **Luật 4 chỉ canh được mã ứng dụng** | Cổng quét mã nguồn. Nó không thấy SQL dựng lúc chạy |

### Tích cực

- Câu hỏi còn mở của 0051 có lời đáp: người điều tra đọc được ai đã gắn khoá nào vào vai trò nào, lúc nào.
- Không thêm bảng, không thêm cột: hình dạng đi vào hai cột `jsonb` đã có sẵn của `core.audit_log`.
- Mọi đường ghi `role_permission` qua `ChangeTracker` đều được phủ, kể cả những đường chưa ai viết.

### Điều gì là dấu hiệu quyết định này bắt đầu sai

- Có một dòng `core.permission.matrix_update` mà cả hai danh sách đều rỗng. Nghĩa là phép nhận biết "ô đổi" đang đọc cờ `IsModified` chứ không đọc giá trị, cùng loại bẫy mà `ValueChanged` trong interceptor đã phải tránh.
- Cỡ `after_value` của một dòng lớn cỡ cả danh mục khoá xuất hiện thường xuyên. Nghĩa là người vận hành đang dựng lại ma trận từ đầu, và câu hỏi thật đã chuyển thành *"trạng thái tại T"*, tức nhu cầu của phương án D.
- Một sự cố leo thang được phát hiện bằng đường khác trong khi nhật ký không có dòng tương ứng. Nghĩa là có đường ghi đi vòng qua `ChangeTracker`.
- Có người phải truy vấn nhật ký này thường xuyên bằng SQL tay. Nghĩa là màn đọc nhật ký của Nhóm B đã đến hạn.

### Rút lui nếu sai

Đổi hình dạng dòng là việc code trong `AuditLogInterceptor` cộng một lần sửa [`../contracts/permissions.md`](../contracts/permissions.md) §6. Lược đồ không đổi. Bảng nhật ký chỉ ghi thêm (luật M13), nên các dòng đã ghi **giữ nguyên hình dạng cũ mãi mãi**, và mọi thứ đọc bảng phải hiểu cả hai hình dạng. Chuyển sang phương án A hay D cũng theo đúng khuôn đó. Muốn bỏ hẳn việc ghi thì gỡ vòng lặp `touchedRoleIds` khỏi interceptor. Muốn làm vậy phải có một ADR mới lật quyết định này, vì 0051 dựa vào nó để chấp nhận cái giá của mình.

## Liên quan

- [`0051-permission-write-la-khoa-goc-cua-don-vi.md`](0051-permission-write-la-khoa-goc-cua-don-vi.md): quyết định được bổ sung
- [`../contracts/permissions.md`](../contracts/permissions.md) §6 mục *Nhật ký kiểm toán*: hình dạng dòng
- [`../database/schema-core.md`](../database/schema-core.md) §9.4: cột của bảng · [`../wiki-core/be/10-data-retention.md`](../wiki-core/be/10-data-retention.md) §5: ghi gì và vì sao
- [`../RULES.md`](../RULES.md) §6 luật S17
