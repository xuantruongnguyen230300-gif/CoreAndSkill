---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0071 — Luật M6 phủ cả `Database.SqlQuery*` và nhận một **allowlist có lý do**; M5 giữ nguyên phạm vi

> **Trạng thái:** Đã chấp nhận (2026-09-23) · Bổ sung bởi [ADR-0074](0074-dbset-fromsql-o-lai-trong-tam-m6.md) (2026-09-23)
>
> Sáu quyết định dưới đây **còn hiệu lực nguyên vẹn**. ADR-0074 thêm vào: `DbSet.FromSql*` ở lại trong tầm M6 vì bộ lọc toàn cục chỉ phủ tập dòng đi ra chứ không phủ thân câu. ⚠️ Phép đo ở §Bối cảnh — *"lời gọi SQL thô còn lại trong sản phẩm: chỉ `SetLockTimeoutSql`"* — đã **sót** một lời gọi, và §Hệ quả tiêu cực gạch 4 nói về `CandidateSql` đã lỗi thời; cả hai **giữ nguyên văn** theo luật D45, phép đo đúng nằm ở §Bối cảnh của ADR-0074.

## Bối cảnh

Kiến trúc sư đối chiếu ngày 2026-09-23 sau một báo cáo nói rằng luật **M5** ([`../RULES.md`](../RULES.md) §9) đang hứa nhiều hơn thứ nó canh. Bốn phép đo:

| Đo gì | Kết quả |
| --- | --- |
| Đường đọc xuyên đơn vị không đi qua `DbSet` trong source sản phẩm | Đúng **một**: `src/BE/Core/CoreAndSkill.Core.Infrastructure/Jobs/JobRecoveryHostedService.cs`, hằng `CandidateSql` chạy qua `db.Database.SqlQueryRaw<RecoveryCandidate>` |
| Câu SQL đó chạm bảng nào | `core.job` và `core.outbox_message`. Cả hai khai `tenant_id NOT NULL` trong `database/scripts/core/0005__core__add-file-and-job.sql` và `database/scripts/core/0006__core__add-outbox-message.sql`; câu SQL **không** có mệnh đề nào trên cột đó |
| Lời gọi SQL thô còn lại trong sản phẩm | `src/BE/Core/CoreAndSkill.Core.Infrastructure/Permissions/PermissionMatrixService.cs`, hằng `SetLockTimeoutSql` — `SET LOCAL lock_timeout`, không chạm bảng nào, nằm ngoài tầm M6 |
| Mọi đường đọc xuyên đơn vị khác | Đều đi qua `IgnoreQueryFilters([CoreQueryFilters.TenantKey])` trên `DbSet` — bộ phát outbox, bộ dọn tệp, lệnh phát lại. Chúng **nêu tên filter**, nên **B6** canh được và **M5** sẽ canh được |

Hai kết luận trái với lời cáo buộc ban đầu, và chúng đổi hẳn việc phải làm.

**Thứ nhất: M5 không hứa cái đó.** Câu của M5 là *"mọi lần gọi bỏ bộ lọc truy vấn nằm trong allowlist"* — nó nói về lời gọi `IgnoreQueryFilters`, không nói về mọi đường đọc xuyên đơn vị. Đề nghị *"mở rộng M5 sang mọi đường truy vấn không đi qua `DbSet`"* sẽ dựng một luật thứ hai cho một thứ đã có chủ.

**Thứ hai: chủ của nó là M6**, đã có từ trước, với câu *"truy vấn SQL thô trên bảng có tenant phải tự thêm điều kiện `TenantId`"* và tên cổng đã chốt `EveryRawSqlOnTenantTable_FiltersByTenant`. Văn bản gốc của M6 ở [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §8.

Nhưng M6 có **hai lỗ**, và đúng hai lỗ đó là cái làm chỗ này hở thật:

1. **Bảng liệt kê hình dạng của M6 không có `Database.SqlQueryRaw` / `Database.SqlQuery`.** Nó liệt kê `FromSqlRaw`, `FromSqlInterpolated`, `ExecuteSqlRaw`, `ExecuteSqlInterpolated`, `migrationBuilder.Sql`, script chạy tay. Hình dạng đang được dùng thật là hình dạng duy nhất không có tên trong danh sách — và nó khác các hình dạng kia ở đúng chỗ khiến người ta quên: nó trả về một **kiểu phẳng**, không phải entity, nên nó không "trông như" một truy vấn trên bảng.
2. **M6 không có allowlist.** Câu của nó là tuyệt đối. Một lượt quét xuyên đơn vị **có chủ đích** — thứ mà chính §8 của văn bản gốc đã thừa nhận là hợp lệ khi nói về backfill toàn hệ — không có đường nào tồn tại hợp pháp.

Hệ quả của lỗ thứ hai là thứ đắt nhất: ngày `EveryRawSqlOnTenantTable_FiltersByTenant` được viết, nó **đỏ ngay ở `CandidateSql`**, và cách sửa dễ nhất sẽ là nới câu luật hoặc bỏ cổng. Đây đúng khuôn mà [ADR-0068](0068-nhanh-tai-tep-di-qua-handlefile.md) đã loại ở phương án D: một cổng vừa dựng xong đã đỏ mà không có lối hợp lệ nào để đi qua thì nó không trả nợ, nó đổi hình nợ.

Hành vi của `JobRecoveryHostedService` hôm nay **đúng**: nó chạy lúc khởi động, khi chưa có request nào nên chưa có đơn vị nào để lọc theo, và nó mở lại phạm vi từng đơn vị ngay sau đó bằng `executionContextScope.Enter(candidate.TenantId, …)` cho **mỗi** việc. Chính dịch vụ này đã có tên trong allowlist của luật **A12** ở [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md), mục *Danh tính và đơn vị khi không có request*. Vấn đề không phải mã sai — vấn đề là **không cổng nào biết chỗ này tồn tại**.

## Quyết định

Kiến trúc sư chốt:

1. **M5 giữ nguyên phạm vi.** Nó canh lời gọi `IgnoreQueryFilters`; nó **không** nhận thêm hình dạng nào. Đề nghị mở rộng nó bị loại — lý do ở phương án A.
2. **`Database.SqlQueryRaw<T>` và `Database.SqlQuery<T>` vào bảng hình dạng của M6** ở [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §8, cùng hàng với `FromSqlRaw`. Bảng đó là văn bản gốc của M6; không chép danh sách sang chỗ thứ hai.
3. **M6 nhận một allowlist khai TRONG cổng, mỗi mục bắt buộc kèm lý do** — cùng hình dạng `NamedExceptions` của B16 và `Allowlist` của T11, không phải một bảng trong `docs/`. Allowlist là **đầu vào của cổng**; một bản chép trong tài liệu sẽ lệch, và lệch theo chiều nới lỏng thì không ai thấy.
4. **Mỗi mục allowlist phải nêu được *cái gì lập lại phạm vi đơn vị sau đó*.** Lý do kiểu *"job nền thì phải quét toàn hệ"* chưa đủ: nó nói vì sao **đọc** rộng, không nói vì sao **ghi** không rộng theo. Mục cho `CandidateSql` nêu `IExecutionContextScope.Enter` mở theo từng việc.
5. **Allowlist của M6 KHÔNG có chốt "không được rỗng".** T11 có chốt đó vì tệp dùng chung của nó phải luôn được miễn trừ; M6 thì ngược lại — **rỗng là trạng thái đích**. Chốt chống mục ruỗng và chốt bắt buộc có lý do thì vẫn giữ.
6. **Cổng do `test-engineer` viết**, đặc tả ở mục *Việc test* dưới. `architect` không viết mã sản phẩm và không viết cổng.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Mở rộng M5 sang mọi đường truy vấn không đi qua `DbSet`

**Được:** một mã luật duy nhất cho câu *"đường đọc xuyên đơn vị nào cũng có người duyệt"*, và M5 đã có sẵn khái niệm allowlist trong khi M6 thì chưa.

**Mất:** M6 tồn tại, mang đúng chủ đề đó, có tên cổng đã chốt và có văn bản gốc dài một mục ở [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md). Nhập SQL thô vào M5 làm hai mã luật cùng mô tả một thứ — và khi hai câu lệch nhau thì không có cách nào biết câu nào đúng. Thêm nữa, allowlist của M5 hôm nay sẽ có hàng chục mục (mọi lời gọi `IgnoreQueryFilters` của bộ phát outbox và bộ dọn tệp); trộn một mục SQL thô vào đó làm một danh sách phải trả lời hai câu hỏi, và người soát nó không còn đọc được nó như một danh sách.

**Vì sao loại:** nó tạo nguồn thứ hai cho một luật đã có chủ — đúng thứ [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §5 cấm, và cách sửa mà §5 chỉ ra là gỡ một bản chứ không đồng bộ hai bản.

### Phương án B — Cấm tuyệt đối SQL thô trên bảng có `tenant_id` trong Core, không allowlist

**Được:** câu luật ngắn nhất và không có chỗ nào để nới. Không allowlist thì không có danh sách mục ruỗng, không có mục thêm vào lúc gấp rồi ở lại mãi.

**Mất:** §8 của văn bản gốc **đã** khai một ca hợp lệ — backfill toàn hệ có chủ đích qua `migrationBuilder.Sql` — nên lệnh cấm tuyệt đối mâu thuẫn với chính tài liệu nó ép. Và với `CandidateSql`, cấm tuyệt đối không xoá nhu cầu: lúc khởi động **không có** đơn vị nào để lọc theo, đó là tiền đề chứ không phải thiếu sót. Người thi công sẽ hoặc viết một mệnh đề `tenant_id = tenant_id` cho qua cổng, hoặc dời câu SQL sang một tệp mà cổng không quét.

**Vì sao loại:** một lệnh cấm mà ca hợp lệ vẫn tồn tại thì không sinh ra sự tuân thủ, nó sinh ra đường vòng — và đường vòng thì không ai đếm được.

### Phương án C — Viết lại `CandidateSql` thành LINQ kèm `IgnoreQueryFilters([CoreQueryFilters.TenantKey])`

**Được:** đây là phương án **tốt nhất về đích đến**, và phải nói rõ điều đó. Nó đưa chỗ đọc xuyên đơn vị duy nhất còn nằm ngoài tầm về đúng con đường mà mọi chỗ khác trong Core đã đi — nơi B6 đang canh thật và M5 sẽ canh. Allowlist của M6 khi đó rỗng, tức đạt ngay trạng thái đích của quyết định 5.

**Mất:** nó là một thay đổi **mã sản phẩm** trên đường khởi động, và phần đẩy câu này sang SQL thô ngay từ đầu — phép dò `payload ->> 'jobId'` trên thân JSON của dòng outbox, lồng trong một `NOT EXISTS` — là đúng phần khó dịch sang LINQ. Không ai chứng minh được nó dịch được mà không viết thử.

**Vì sao loại — và loại ở mức nào:** loại **khỏi lượt này**, không loại khỏi bàn. Hai lý do. Một: nó không thay được quyết định — M6 vẫn cần allowlist cho ca `migrationBuilder.Sql` mà §8 đã khai là hợp lệ, nên bỏ công viết lại câu SQL rồi vẫn phải quay lại đúng quyết định 3. Hai: gộp một thay đổi mã sản phẩm vào một lượt quyết định về cổng là cách một quyết định kiểm thử lặng lẽ trở thành một thay đổi hành vi khởi động. Việc này giao riêng cho `backend-expert`, và mục allowlist của `CandidateSql` ghi sẵn nó làm điều kiện gỡ mục.

### Phương án D — Ghi một dòng nợ, chưa làm gì

**Được:** không tốn gì hôm nay, và chỗ hở nhìn thấy được.

**Vì sao loại:** chỗ hở này **đã** nhìn thấy được — M6 có mặt trong [`../RULES.md`](../RULES.md) §9 với trạng thái `📐` từ trước. Một dòng nợ nữa không thêm thông tin nào; thứ còn thiếu không phải tầm nhìn mà là *lối hợp lệ để cổng dựng được*. Và một mã luật đang mang `📐` trong bảng có cổng thì theo luật của chính [`../RULES.md`](../RULES.md) không được đồng thời nằm ở sổ nợ.

## Hệ quả

### Tích cực

- Cổng của M6 dựng được mà không đỏ ngay ngày đầu, nên nó không bị nới để cho xanh.
- Hình dạng `Database.SqlQuery*` — hình dạng dễ quên nhất vì nó trả kiểu phẳng chứ không trả entity — có tên trong văn bản luật, nên lần sau người viết một truy vấn báo cáo sẽ đọc thấy nó.
- Chỗ đọc xuyên đơn vị duy nhất của Core có một mục khai bằng chữ, kèm lý do và kèm điều kiện gỡ. Trước quyết định này nó chỉ có một khối chú thích trong mã, mà chú thích thì không cổng nào đọc.
- Ranh giới M5 ↔ M6 thành một câu trả lời được: **M5 hỏi *ai được bỏ bộ lọc*, M6 hỏi *câu SQL nào tự lọc lấy*.** Hai câu hỏi, hai danh sách, không giao nhau.

### Tiêu cực

- **Một allowlist mới là một chỗ mới để nới.** Mọi allowlist đều bắt đầu với một mục chính đáng. Quyết định 4 (phải nêu cái gì lập lại phạm vi) và quyết định 5 (không chốt "không rỗng") làm việc thêm mục **đắt hơn** và làm việc bỏ mục **rẻ hơn**, nhưng không chặn được. Dấu hiệu phải đọc lại ADR này: allowlist có mục thứ hai, hoặc một mục nêu lý do không tên được cơ chế lập lại phạm vi nào.
- **Cổng vẫn không đọc được ngữ nghĩa.** Nó bắt được câu SQL thiếu hẳn chữ `tenant_id`; nó không bắt được `WHERE tenant_id = <đơn vị sai>`. Giới hạn này đã khai ở §8 văn bản gốc và quyết định này **không** thu hẹp nó. Ai đọc dòng M6 sang trạng thái đang chạy mà tưởng cách ly đã được máy chứng minh thì đang đọc rộng hơn thực tế.
- **Tập bảng có `tenant_id` đọc từ DDL, nên cổng phụ thuộc `database/scripts/core/`.** Một bảng mới có `tenant_id` mà script chưa về thì cổng không biết bảng đó cần lọc, và một câu SQL trên bảng ấy đi lọt. Chỗ này im lặng; nó chỉ lộ ra khi có người so hai nguồn.
- **`CandidateSql` ở lại nguyên trạng ít nhất tới khi phương án C được xét riêng.** Nghĩa là trong khoảng đó, đường đọc xuyên đơn vị duy nhất ngoài `IgnoreQueryFilters` vẫn tồn tại — được khai, được duyệt, nhưng vẫn tồn tại. Một allowlist có một mục không phải là không có chỗ hở; nó là một chỗ hở có tên.

### Rút lui nếu sai

Rẻ. Allowlist sống trong một tệp test: gỡ mục đi thì cổng đỏ, và chỗ đỏ chỉ đúng vào lời gọi đã khai. Muốn quay về phương án B (cấm tuyệt đối) thì xoá mảng allowlist và xoá hai chốt hygiene đi kèm — nửa giờ, không chạm mã sản phẩm. Muốn đi tiếp sang phương án C thì việc gỡ mục là bước cuối cùng chứ không phải bước đầu.

## Việc test — giao `test-engineer`

Cổng `EveryRawSqlOnTenantTable_FiltersByTenant`, đủ để thi công mà không phải đoán:

1. **Tầm quét:** source sản phẩm `Core.*` và host, cùng tập với B16 và S17 (`ProductSourceFiles.Core()`).
2. **Hình dạng bắt:** đối số chuỗi — kể cả chuỗi thô nhiều dòng và chuỗi nội suy — của `FromSql`, `FromSqlRaw`, `FromSqlInterpolated`, `ExecuteSql*`, **`SqlQuery`, `SqlQueryRaw`**, và `migrationBuilder.Sql`. Hằng số được gán ở chỗ khác rồi truyền vào (đúng hình dạng của `CandidateSql`) phải bắt được — nếu không, phép dò bỏ sót đúng ca đang có thật.
3. **Tập bảng có đơn vị:** đọc ra từ DDL dưới `database/scripts/core/` — bảng nào khai cột `tenant_id`. **Không hằng hoá danh sách bảng trong cổng.**
4. **Điều kiện PASS của một câu:** hoặc nó không chạm bảng nào trong tập trên, hoặc nó có một mệnh đề trên `tenant_id`, hoặc nơi gọi nằm trong allowlist.
5. **Allowlist:** mảng `(đường dẫn tương đối, tên hằng hoặc tên phương thức, lý do)` khai một chỗ. Mục đầu tiên và duy nhất hôm nay: `CandidateSql` của `JobRecoveryHostedService` — lý do phải nêu rằng phạm vi đơn vị được lập lại bằng `IExecutionContextScope.Enter` theo từng việc, và nêu điều kiện gỡ mục là phương án C của ADR này.
6. **Chốt chống xanh rỗng (T6), ba cái:** tập bảng có `tenant_id` khác rỗng; số lời gọi SQL thô đọc được khác rỗng (hôm nay có hai, một trong đó — `SetLockTimeoutSql` — hợp lệ vì không chạm bảng nào, nên nó là ca đối chứng sẵn có); và **canary** — một câu `SELECT … FROM core.notification_recipient` không mệnh đề đơn vị phải bị bắt.
7. **Chốt hygiene của allowlist:** mục trỏ vào chỗ không còn tồn tại là FAIL; mục không có lý do là FAIL. **Không** thêm chốt "allowlist không rỗng" — xem quyết định 5.
8. **Nhóm `Detector_M6_*` (T1):** bắt được `SqlQueryRaw` trên bảng có đơn vị thiếu mệnh đề; bỏ qua câu không chạm bảng (`SET LOCAL lock_timeout`); bỏ qua câu có mệnh đề `tenant_id`; bỏ qua chuỗi SQL nằm trong chú thích; và bỏ qua chuỗi SQL nằm trong **chuỗi ký tự của chính tệp cổng** — cùng bẫy mà `Detector_T11_Ignores_AConstructionInsideAStringLiteral` đã gặp.

## Liên quan

- [`../RULES.md`](../RULES.md) §9 — **M5**, **M6**, **B6**; §3 — **A12**.
- [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §8 — văn bản gốc của M6 và giới hạn đã khai của cổng.
- [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) — allowlist của A12, nơi `JobRecoveryHostedService` đã có tên.
- [`0068-nhanh-tai-tep-di-qua-handlefile.md`](0068-nhanh-tai-tep-di-qua-handlefile.md) phương án D — khuôn *cổng vừa dựng đã đỏ thì nợ chỉ đổi hình*.
- [`0047-job-dinh-ky-khong-di-qua-seam-lap-lich.md`](0047-job-dinh-ky-khong-di-qua-seam-lap-lich.md) — `JobRecoveryHostedService` là `BackgroundService` đứng riêng, không qua seam lập lịch.
