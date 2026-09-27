---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0074 — `DbSet.FromSql*` ở lại trong tầm **M6**: bộ lọc toàn cục chỉ phủ **tập dòng ngoài**, không phủ thân câu; `OutboxDispatcher` tự thêm mệnh đề đơn vị thay vì vào allowlist

> **Trạng thái:** Đã chấp nhận (2026-09-23)

## Bối cảnh

Cổng `EveryRawSqlOnTenantTable_FiltersByTenant` — đặc tả ở [ADR-0071](0071-m6-nhan-allowlist-va-phu-duong-sqlquery.md) mục *Việc test* — đã được `test-engineer` dựng. Chạy ngày 2026-09-23: **1 đỏ / 21 xanh**. Chỗ đỏ duy nhất là một lời gọi `FromSql` trên `core.outbox_message` trong `src/BE/Core/CoreAndSkill.Core.Infrastructure/Outbox/OutboxDispatcher.cs` (tìm bằng chuỗi `FOR UPDATE SKIP LOCKED`), không mang mệnh đề nào trên `tenant_id`.

Hai lượt đo độc lập kết luận **hành vi hôm nay đúng, không rò dữ liệu**, và lần được về gốc của chỗ mơ hồ: [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §8 — văn bản gốc của M6 — xếp `FromSqlRaw` / `FromSqlInterpolated` vào nhóm *"không đi qua bộ lọc"*. **Câu đó sai trên EF Core.** Câu hỏi đặt lên bàn kiến trúc sư: M6 có còn lý do phủ `DbSet.FromSql*` không?

### Phép đo — chạy thật, không đọc mã

Lập luận "có/không đi qua bộ lọc" là lập luận về hành vi của một thư viện bên thứ ba, nên nó được **chạy** chứ không được suy. Kiến trúc sư dựng một dự án dùng một lần **ngoài cây repo** (thư mục tạm của phiên, không phải `src/`), cùng bộ đang cài — EF Core `10.0.12`, `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` — với một entity mang bộ lọc toàn cục **có tên**, rồi in `ToQueryString()`. Không cần database sống. Ai muốn lặp lại chỉ cần một `DbContext`, một `HasQueryFilter(tên, biểu thức)` và `ToQueryString()`.

| Hình dạng | SQL mà EF thật sự dựng | Bộ lọc có phủ không |
| --- | --- | --- |
| LINQ thường (đối chứng) | `WHERE ... AND o.tenant_id = @ef_filter__…` | Có |
| `DbSet.FromSql` / `FromSqlRaw` / `FromSqlInterpolated` | câu thô bị **bọc thành subquery**, ngoài phủ `WHERE o.tenant_id = @ef_filter__…` | **Có** |
| Câu trên kèm `IgnoreQueryFilters([…])` (đối chứng ngược) | câu thô đi thẳng, không bọc, không mệnh đề nào thêm | Không |
| `Database.SqlQueryRaw<T>` / `SqlQuery<T>` | câu thô đi thẳng, không bọc | **Không** |

Ba dòng đầu cùng nhau chứng minh phần bọc ở dòng 2 **đúng là bộ lọc**, không phải một tạo tác của phép in.

Kết luận thứ nhất: **câu ở §8 sai**, và nó sai đúng theo chiều nguy hiểm nhất — nó dạy người đọc rằng một cơ chế an toàn *không* hoạt động, nên người đọc sẽ không tin vào nó ở chỗ nó thật sự đang giữ, và sẽ tin vào nó… không ở đâu cả. Sự phân biệt mà ADR-0071 §Bối cảnh đã chỉ ra cho `SqlQueryRaw` — trả **kiểu phẳng**, không đi qua model — chính là ranh giới thật, và §8 vẽ ranh giới đó sai chỗ.

### Phép đo thứ hai — thứ bộ lọc KHÔNG phủ

Nếu dừng ở kết luận thứ nhất thì câu trả lời hiển nhiên là *"gỡ `FromSql` khỏi tầm M6"*. Trước khi chốt theo chiều đó, cùng phép đo được chạy tiếp trên ba hình dạng mà một câu `FromSql` **được phép** viết:

| Câu viết trong `FromSql` | EF dựng ra gì | Điều gì nằm ngoài bộ lọc |
| --- | --- | --- |
| `SELECT o.id, j.status AS status, o.tenant_id FROM core.outbox_message o CROSS JOIN core.job j` | bọc subquery, ngoài phủ `WHERE o.tenant_id = @ef_filter__…` | Vị từ ngoài chỉ ràng **cột `tenant_id` do câu trong sinh ra**. Cột `status` lấy từ `core.job` của **mọi đơn vị** vẫn đi vào thuộc tính của entity — dữ liệu xuyên đơn vị về tới nơi gọi, **mang nhãn đơn vị hiện tại** |
| `WITH upd AS (UPDATE core.outbox_message SET status = 'done' RETURNING …) SELECT * FROM upd` | bọc subquery, ngoài phủ vị từ đơn vị | Lệnh `UPDATE` trong CTE chạy trên **mọi đơn vị**. Bộ lọc ngoài chỉ tỉa thứ *trả về*; nó không hoàn tác thứ đã *ghi*. Một lệnh ghi xuyên đơn vị đi qua một API trông như chỉ đọc |
| `FromSqlRaw` trên một `DbSet` mà entity **không** mang bộ lọc, câu bên trong `JOIN` sang bảng có `tenant_id` | câu thô đi thẳng, không bọc | Không có bộ lọc nào ở bất cứ đâu |

Kết luận thứ hai, và là cái quyết định lượt này: **bộ lọc toàn cục phủ *tập dòng đi ra*, không phủ *thân câu*.** Mọi thứ câu SQL làm bên trong — đọc bảng khác, nối bảng khác, khoá dòng, và trên PostgreSQL là cả **ghi** qua CTE sửa dữ liệu — xảy ra **trước** và **ngoài** vị từ mà EF thêm vào.

Nên hai câu sau đều đúng cùng lúc, và đọc thiếu một câu là đọc sai:

> `DbSet.FromSql*` **có** đi qua bộ lọc. Và điều đó **không** làm nó an toàn về cách ly đơn vị.

### Lời gọi đang làm cổng đỏ

Câu SQL của `OutboxDispatcher` khoá một dòng theo `id` với `FOR UPDATE SKIP LOCKED`, trong một phạm vi đã mở bằng `executionContextScope.Enter(row.TenantId, …)` ngay trước đó. Vị từ đơn vị mà EF thêm vào khớp, nên **không rò dữ liệu** — điều hai lượt đo trước đã kết luận đúng.

Nhưng theo phép đo thứ hai, chỗ này vẫn có một khoản thật, nhỏ và có tên: **`FOR UPDATE` nằm trong câu trong, nên khoá được lấy TRƯỚC khi vị từ đơn vị được áp.** Một dòng của đơn vị khác lọt vào câu trong sẽ bị **khoá** trong suốt giao dịch rồi mới bị lọc bỏ khỏi kết quả. Hôm nay điều đó không xảy ra vì `row.Id` và `row.TenantId` đến từ cùng một dòng; nó là một bất biến của *nơi gọi*, không phải của *câu SQL*.

### Hai chỗ nay đã cũ trong ADR-0071 — và luật D45

[ADR-0071](0071-m6-nhan-allowlist-va-phu-duong-sqlquery.md) có hai chỗ không còn khớp thực tế:

1. §Bối cảnh, dòng *"Lời gọi SQL thô còn lại trong sản phẩm: chỉ `SetLockTimeoutSql`"* — **phép đo đó sót** đúng lời gọi đang làm cổng đỏ.
2. §Hệ quả tiêu cực gạch 4, *"`CandidateSql` ở lại nguyên trạng ít nhất tới khi phương án C được xét riêng"* — phương án C đã về; `CandidateSql` không còn tồn tại, và allowlist của cổng **rỗng**.

Đây là ca thử thứ hai của **D45** ([`../RULES.md`](../RULES.md) §1), và nó khó hơn ca ADR-0070: ở ca đó thông tin chỉ **lỗi thời**; ở đây một phép đo **sai từ lúc viết**. Câu hỏi thật: D45 có chừa ngoại lệ cho "ADR khai một phép đo mà phép đo đó sót" không?

## Quyết định

Kiến trúc sư chốt:

1. **`DbSet.FromSql`, `FromSqlRaw`, `FromSqlInterpolated` Ở LẠI trong tầm M6**, cùng hạng với `Database.SqlQuery*` — nhưng **vì một lý do khác**, và lý do đó phải được viết ra. Không phải *"bộ lọc không phủ"* (sai), mà *"bộ lọc chỉ phủ tập dòng đi ra, còn thân câu thì không"*. Tầm quét của cổng **không đổi**; `test-engineer` không phải sửa gì.
2. **`OutboxDispatcher` tự thêm mệnh đề đơn vị vào câu SQL** — việc của `backend-expert`. **Không** thêm mục allowlist nào; allowlist của M6 giữ nguyên trạng thái **rỗng**.
3. **[`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §8 viết lại**, chia các hình dạng thành **hai hạng theo *vì sao* chúng trong tầm**, thay cho một câu gộp sai. Cùng lượt đó thi hành **quyết định 2 của ADR-0071** — `Database.SqlQueryRaw` / `SqlQuery` vào bảng hình dạng — thứ chưa ai làm.
4. **Bảng hình dạng ở §8 mang mốc `— định nghĩa gốc` và có một dòng ở [`../OWNERSHIP.md`](../OWNERSHIP.md) §3.** Câu *"không chép danh sách sang chỗ thứ hai"* của ADR-0071 quyết định 2 từ nay được cổng `check-docs.sh` §15/§16 ép, thay vì được mong đợi.
5. **D45 áp nguyên vẹn cho ca "phép đo sai": không có ngoại lệ.** Nội dung ADR-0071 giữ nguyên từng chữ, kể cả phép đo sót; nó chỉ đổi dòng **Trạng thái** thành *Bổ sung bởi ADR-0074*. Phép đo đúng sống ở §Bối cảnh của **ADR này**. Và **D45 được viết thêm một vế** nêu đích danh ca này, để lần sau không ai phải suy lại.
6. **Không đòi một integration test chứng minh EF phủ bộ lọc lên `FromSql`.** Lý do ở mục *Việc test* dưới — đây là một lời **từ chối có điều kiện đã không xảy ra**, không phải một khoản bỏ qua.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Gỡ `FromSql*` khỏi tầm quét của M6

Đây là **phương án đơn giản nhất**, và nó là kết luận tự nhiên của phép đo thứ nhất: nếu bộ lọc đã phủ thì bắt câu SQL tự lọc lần nữa là thừa. Nó làm cổng xanh ngay, không chạm mã sản phẩm, không thêm chữ nào vào luật.

**Được:** luật ngắn hơn, cổng hẹp hơn, không ai phải viết mệnh đề thừa. Và nó thành thật với phép đo thứ nhất.

**Mất:** ba hình dạng ở phép đo thứ hai đi lọt **toàn bộ**. Nặng nhất là CTE sửa dữ liệu: một lệnh `UPDATE` xuyên đơn vị nấp trong một lời gọi mà cả tên phương thức lẫn kiểu trả về đều nói rằng đây là một truy vấn đọc. Không cổng nào khác trong repo nhìn vào đó — M5 canh `IgnoreQueryFilters`, B6 canh việc nêu tên bộ lọc, cả hai đều không thấy một chuỗi SQL.

**Vì sao loại:** nó lấy một câu đúng (*bộ lọc có phủ*) và suy ra một câu sai (*nên chỗ này an toàn*). Khoảng cách giữa hai câu đó chính là thứ phép đo thứ hai đo được, và nó không nhỏ.

### Phương án B — Thêm một mục allowlist cho `OutboxDispatcher`

**Được:** đúng bộ máy mà ADR-0071 đã dựng cho tình huống này, không chạm mã sản phẩm, một dòng là xong.

**Mất:** allowlist nghĩa là **miễn trừ** — "chỗ này lẽ ra vi phạm, ta cho qua vì lý do X". Ở đây không có vi phạm để miễn trừ: câu SQL *nên* mang mệnh đề đơn vị, và không gì cản nó mang. Một mục miễn trừ cho một ngoại lệ không tồn tại là **mục ruỗng ngay từ mục đầu tiên** — và chốt `EveryAllowlistEntry_PointsAtACallThatWouldOtherwiseViolate` của cổng tồn tại đúng để bắt loại mục đó. Thêm nữa, ADR-0071 tự khai *"Dấu hiệu phải đọc lại ADR này: allowlist có mục thứ hai"*; tiêu một lượt đọc lại đó cho một mục không cần thiết là làm hỏng chính cái đồng hồ báo động.

**Vì sao loại:** nó đổi một khoản nợ mã nguồn dài một dòng lấy một khoản nợ luật vĩnh viễn, và làm rỗng nghĩa của từ *allowlist* ngay lần dùng đầu.

### Phương án C — Cổng thêm một nhánh PASS: "bên nhận là `DbSet` của entity có bộ lọc"

**Được:** đây là phương án *có vẻ* đúng nhất về nguyên tắc — nó cho câu SQL đi qua **vì cấu trúc**, không phải vì được tha, nên không có allowlist và không có mệnh đề thừa.

**Mất:** hai khoản, một khoản kỹ thuật và một khoản chí mạng. Kỹ thuật: phép dò hôm nay là bộ đọc **cú pháp** Roslyn; biết `db.OutboxMessages` là `DbSet<OutboxMessage>` và `OutboxMessage` mang bộ lọc là việc của tầng ngữ nghĩa, tức một cơ chế thứ hai phải dựng và phải tự canh. Chí mạng: nhánh PASS đó **khẳng định một điều sai** — nó dán nhãn "an toàn" lên đúng ba hình dạng của phép đo thứ hai, và hình dạng thứ ba (`DbSet` không có bộ lọc, câu bên trong nối sang bảng có `tenant_id`) thì nó cho đi lọt **theo đúng thiết kế của nó**.

**Vì sao loại:** một cổng nói sai còn tệ hơn một cổng không nói gì. Cổng không nói gì để lại một chỗ trống mà người ta biết là trống; cổng nói sai để lại một chỗ trống mà người ta tin là đã kín.

### Phương án D — Để cổng đỏ, ghi một dòng nợ, quyết sau

**Được:** không quyết vội trên một chủ đề vừa lộ ra là bị hiểu sai suốt từ đầu.

**Vì sao loại:** cổng đỏ chặn commit, nên "quyết sau" trên thực tế là "gỡ cổng bây giờ". Và thứ còn thiếu không phải thời gian suy nghĩ — phép đo đã có, cả hai chiều. Một dòng nợ ở đây chỉ ghi lại rằng ta đã biết đủ để quyết và đã không quyết.

## Hệ quả

### Tích cực

- Câu luật của M6 từ nay đứng trên một lý do **đo được**, thay cho một lời khai sai về EF Core. Ai không tin có thể chạy lại phép đo trong mười phút.
- Hai câu hỏi tách hẳn nhau, và đây là thứ §8 chưa bao giờ nói rõ: *"bộ lọc có phủ không"* là câu hỏi về **tập dòng đi ra**; *"câu SQL có tự lọc không"* là câu hỏi về **thân câu**. M6 hỏi câu thứ hai, và vì thế nó vẫn còn việc để làm kể cả khi câu thứ nhất trả lời "có".
- Allowlist của M6 giữ nguyên **rỗng** — trạng thái đích mà ADR-0071 quyết định 5 đặt ra. Cổng M6 sẽ xanh mà chưa ai phải viết mục miễn trừ đầu tiên.
- Khoản khoá-trước-khi-lọc của `OutboxDispatcher` được đóng như một hệ quả phụ: mệnh đề đơn vị nằm trong câu trong nên `FOR UPDATE` cũng thành có phạm vi đơn vị. Bất biến chuyển từ **nơi gọi** vào **câu SQL**, tức vào chỗ cổng nhìn thấy.
- Bảng hình dạng có mốc chủ quyền, nên lần sau có người chép nó sang file thứ hai thì cổng đỏ, không phải chờ ai đọc ra.

### Tiêu cực

- **Mệnh đề đơn vị mà quyết định 2 đòi là THỪA lúc chạy, và người đọc hiểu EF sẽ thấy nó thừa.** Đây là cái giá chính. Một lập trình viên biết rằng bộ lọc đã phủ sẽ đọc dòng đó như nhiễu và có lúc sẽ xoá nó — và việc xoá chỉ lộ ra khi cổng chạy. Chú thích cạnh câu SQL không chữa được điều đó; nó chỉ làm chỗ đó **có thể** giải thích được.
- **Luật giữ hình phạt cho một hành vi mà nền tảng đã che phần lớn.** Bảo hiểm kép có tuổi thọ: càng lâu không có ca nào nó bắt được, người ta càng tin rằng nó không cần thiết. Dấu hiệu phải đọc lại ADR này: có người đề nghị gỡ `FromSql*` khỏi tầm M6 **mà không** nhắc tới CTE sửa dữ liệu hay phép nối xuyên bảng — nghĩa là lập luận của phép đo thứ hai đã rơi khỏi trí nhớ tập thể.
- **Cổng vẫn đọc văn bản, không đọc ngữ nghĩa.** Nó bắt được câu thiếu hẳn chữ `tenant_id` ở vị trí vị từ; nó không bắt được `WHERE tenant_id = <đơn vị sai>`, và nó **không** bắt được câu có mệnh đề đơn vị đúng trên bảng gốc nhưng nối sang một bảng khác không lọc. Quyết định này **không** thu hẹp giới hạn đó — nó chỉ làm hàng rào đứng đúng chỗ.
- **Ba hình dạng của phép đo thứ hai vẫn không có cổng riêng.** M6 sau lượt này bắt được ca *thiếu mệnh đề*; ca *có mệnh đề trên bảng gốc mà nối sang bảng khác* và ca *CTE sửa dữ liệu* thì vẫn chỉ có người đọc canh. Chúng được **nói ra** ở §8 chứ không được **ép**.
- **§Bối cảnh của ADR-0071 ở lại sai vĩnh viễn trên hồ sơ.** Đó là cái giá của D45, và quyết định 5 trả nó một cách có ý thức. Ai đọc ADR-0071 một mình sẽ tin rằng sản phẩm chỉ còn một lời gọi SQL thô. Dòng trạng thái trỏ sang đây là thứ duy nhất cứu họ, và nó chỉ cứu được người đọc hết dòng đầu.

### Rút lui nếu sai

Rẻ theo cả hai chiều, và không đối xứng theo chiều tốt.

Muốn quay về phương án A (gỡ `FromSql*` khỏi tầm): sửa mảng tên phương thức trong `Support/RawSqlTenantScanner.cs`, sửa bảng ở §8, viết ADR mới nêu **cái gì đã đổi** — hợp lý nhất là PostgreSQL bỏ CTE sửa dữ liệu, hoặc EF đổi cách bọc. Nửa giờ. Mệnh đề đơn vị đã thêm vào `OutboxDispatcher` thì **cứ để lại**: nó đúng dù luật có đòi hay không.

Chiều ngược lại đắt hơn nhưng vẫn có: muốn siết thêm để bắt ca nối xuyên bảng thì đó là một phép dò mới trên cùng bộ đọc, không phải một kiến trúc mới.

## Việc test — vì sao KHÔNG thêm test nào

`test-engineer` nêu đúng một điều: chứng minh `OutboxDispatcher` an toàn hôm nay là **đọc mã**, không phải chạy test; và nếu kiến trúc sư chốt **miễn trừ**, thì chỗ đó *nên* có một integration test chứng minh EF thật sự phủ bộ lọc lên `FromSql`.

Điều kiện đó **đã không xảy ra** — quyết định 2 không miễn trừ gì cả. Và sau quyết định 2, cách ly của lời gọi đó **không còn phụ thuộc** vào việc EF có phủ bộ lọc hay không: mệnh đề đơn vị nằm trong chính câu SQL. Một test khẳng định hành vi bọc subquery của EF sẽ canh một thứ mà **không dòng mã nào của ta còn dựa vào** — đúng loại test xanh mãi mãi và không bao giờ nói cho ai điều gì.

Phép đo trong §Bối cảnh vì thế ở lại đúng thân phận của nó: **bằng chứng cho một quyết định**, không phải một bất biến phải canh. Nếu về sau có mã sản phẩm dựa vào hành vi đó, lúc ấy mới là lúc nó cần một test — và lúc ấy nó sẽ là test của **mã đó**, không phải của EF.

`test-engineer` không có việc phát sinh từ ADR này.

## Việc thi công

**`backend-expert` — `src/BE/`:** thêm mệnh đề đơn vị tham số hoá vào câu SQL của `OutboxDispatcher` (tìm bằng `FOR UPDATE SKIP LOCKED`), dùng đơn vị của chính dòng đang xử lý, và một chú thích ngắn nêu rằng mệnh đề này ràng **phạm vi của lệnh khoá**, chứ không phải bù cho một bộ lọc thiếu — nếu không, người sau sẽ xoá nó như một dòng thừa. Không chạm allowlist của cổng.

**`architect` — đã làm trong lượt này:** §8 của [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md); dòng M6 ở [`../RULES.md`](../RULES.md) §9; vế mới của D45; dòng chủ quyền ở [`../OWNERSHIP.md`](../OWNERSHIP.md) §3; dòng trạng thái của ADR-0071.

## Liên quan

- [`0071-m6-nhan-allowlist-va-phu-duong-sqlquery.md`](0071-m6-nhan-allowlist-va-phu-duong-sqlquery.md) — ADR được bổ sung; sáu quyết định của nó **còn hiệu lực nguyên vẹn**, kể cả allowlist và chốt "không đòi khác rỗng".
- [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §8 — văn bản gốc của M6, nơi câu sai đã được thay.
- [`../RULES.md`](../RULES.md) §9 **M6**, §1 **D45**.
- [`0070-ma-test-dung-chung-la-tep-nguon-noi-vao-hai-project.md`](0070-ma-test-dung-chung-la-tep-nguon-noi-vao-hai-project.md) — ca thử đầu tiên của D45, dạng *thông tin lỗi thời*; ca này là dạng *phép đo sai*.
