---
kind: tham-chieu
scope: core
verified: chua-doi-chieu
---

# Lý do, bẫy, ví dụ mở rộng của `script-runbook.md`

> File này giữ **lý do, bẫy, ví dụ dài** của [`docs/database/script-runbook.md`](../../../database/script-runbook.md); luật, khối lệnh và bảng nghiệm thu ở đó. Số mục dưới đây trùng số mục của file luật — mục không có gì dời thì ghi "—".

---

## 0. Quyết định nền — áp schema CHẠY TAY

Ba lý do không auto-migrate:

1. **Auto-migrate biến mỗi lần khởi động thành một lần đổi schema.** Với nhiều instance, chúng
   khởi động cùng lúc và cùng chạy migration — Postgres có khoá tư vấn nên không hỏng dữ liệu,
   nhưng instance thua cuộc chờ, và thời gian chờ đó là downtime không ai dự kiến.
2. **Không ai đọc thứ chạy tự động.** Một lệnh `DROP COLUMN` sinh nhầm sẽ chạy trước khi có người
   kịp nhìn thấy nó. Ở dự án tiền nhiệm, một migration sinh tự động chứa **năm** lệnh `DropTable`
   và lưới an toàn duy nhất là người đọc phát hiện ra kịp.
3. **Người vận hành phải chọn được thời điểm.** Đổi schema trên dữ liệu thật là thao tác có cửa
   sổ, có backup, có người trực. Gắn nó vào lần khởi động kế tiếp là bỏ hết ba thứ đó.

**Vì sao không auto-migrate kể cả máy dev** — điểm sẽ bị đòi nới, nên ghi lý do ra ngay: bật auto-migrate cho dev tạo ra **hai đường khác nhau giữa các môi
trường**. Đường chạy hằng ngày, được thử hàng trăm lần, là đường tự động; đường thật sự chạm
production là đường thủ công, và nó được thử **đúng một lần**, vào lúc căng nhất. Mọi lỗi thuộc
về script — sai thứ tự, thiếu một bước, câu lệnh chạy lại lần hai thì hỏng — sẽ **chỉ xảy ra ở
production**, vì ở dev không ai đi qua đường đó.

"Chạy tay" không được phép có nghĩa là "mỗi người tự nghĩ ra cách" — đó là lý do file luật trả lời năm câu, mỗi câu một mục, cụ thể tới mức chép ra chạy được.

## 1. Câu 1 — Script nằm ở đâu

**Vì sao tách theo bên sở hữu chứ không gộp một thư mục phẳng.** Nó ánh xạ thẳng luật E6
([`migration-policy.md`](../../../database/migration-policy.md) §7): script chạm schema `core` mà nằm trong
`modules/banhang/` là sai chỗ, và nhìn đường dẫn là thấy — không cần mở file. Nó cũng làm việc
cấp số ở §2 không bao giờ va giữa hai bên.

**Vì sao thư mục này ở gốc repo chứ không nằm trong `src/BE/`.** Người chạy script là người vận
hành, không nhất thiết là lập trình viên .NET. Bắt họ đi vào ba tầng thư mục project để tìm một
file `.sql` là một rào cản không cần thiết, và là lý do thật để người ta bỏ qua quy trình.

Vì sao hai thư mục script chỉ được chứa script schema đánh số: vòng lặp ở §3.3 chạy glob `*.sql`
**không có allowlist** — một tệp khác rơi vào đây sẽ tự chạy trên mọi bản cài, kể cả bản chạy
thật.

## 2. Câu 2 — Đặt tên thế nào

**Vì sao có tiền tố số thay vì dùng timestamp như EF.** Số bốn chữ số sắp xếp đúng bằng `ls`,
đọc được bằng mắt, và **nói thẳng thứ tự** — thứ mà một chuỗi `20260908143255` nói được nhưng
không ai đọc nổi. Người vận hành cần trả lời câu *"tôi đang ở 0006, còn thiếu mấy cái"* trong
hai giây.

**Vì sao cấp số theo owner chứ không cấp chung.** Core và module phát triển song song và không
biết nhau. Một dãy số chung nghĩa là mỗi lần một module thêm script, Core phải đi hỏi số tiếp
theo là bao nhiêu — một điểm phối hợp không đổi lấy gì.

### 2.1 Hai người cùng thêm script — cách xử lý

Vì sao đổi tên lúc merge là an toàn: quy trình cấm chạy script từ một nhánh chưa
vào `main` lên database dùng chung, nên script chưa merge chưa được áp ở đâu.

Vì sao không bao giờ đổi tên một script đã có dòng trong `core.schema_script_history`: bảng đó ghi nhận theo **tên file**. Đổi tên làm mọi database đã chạy tưởng
mình còn thiếu script, và lần chạy kế tiếp áp lại nó. Với script idempotent thì vô hại; với
một script có `INSERT` không phòng trùng thì đó là dữ liệu nhân đôi. Luật tồn tại để không bao giờ phải `UPDATE core.schema_script_history` trên mọi database cùng lượt.

## 3. Câu 3 — Chạy theo thứ tự nào, và biết mình đang ở đâu

### 3.1 Bảng lịch sử áp dụng

Vì sao không nhầm với `core.__ef_migrations_history`: bảng của EF trả lời *"EF tin migration nào
đã áp"*, và được ghi tự động bởi chính script sinh với `--idempotent`. Bảng này thì do **người
chạy script ghi**, và giữ thêm thứ EF không có: băm nội dung file, ai chạy, chạy lúc nào, mất
bao lâu. Hai bảng, hai câu hỏi, không suy ra nhau được.

Vì sao `ON CONFLICT DO NOTHING` là bắt buộc, không phải phòng xa: người vận hành sẽ chạy lại một
file khi không chắc lần trước đã xong chưa, và đó là hành vi đúng cần được hỗ trợ chứ không phải
hành vi cần chặn.

Vì sao phải nằm trong cùng transaction: ghi lịch sử ngoài transaction nghĩa là có một cửa
sổ mà schema đã đổi nhưng lịch sử chưa ghi (hoặc ngược lại) — và cửa sổ đó, nếu tiến trình chết
đúng lúc, để lại một database mà không ai biết nó ở đâu.

### 3.2 Tính `checksum_sha256`

Băm bắt được đúng một ca, và là ca
khó thấy nhất: **ai đó sửa một script đã áp**. Người sửa tin cả hệ đã có thay đổi; mọi database
đã chạy bản cũ thì không, và không có gì báo. Câu kiểm ở §3.5 phát hiện ca này.

### 3.3 Quy trình từ database TRỐNG — một đường cho mọi môi trường

Vì sao không có bước nạp danh mục quyền riêng: migration của Core mang khoá của Core, migration của từng
module mang khoá của module đó ([`migration-policy.md`](../../../database/migration-policy.md) §1, §4.1). Đó cũng là lý do Core áp trước module: script module ghi vào
bảng mà script Core tạo.

Vì sao `-v ON_ERROR_STOP=1` là bắt buộc: mặc định `psql` **chạy tiếp** sau khi một câu lệnh
lỗi và thoát với mã 0. Không có cờ này, một script hỏng giữa chừng vẫn kết thúc như thành
công, và database còn lại một nửa. Đây là mặc định gây bất ngờ nhất của `psql`. Cùng lý do, trong DBeaver/pgAdmin `Ctrl+Enter` chỉ chạy một câu lệnh dưới con trỏ, và người chạy sẽ tưởng cả file đã chạy.

Vì sao dựng lại từ đầu khi một vòng in `DUNG`: database còn trống nên dựng lại là thao tác rẻ nhất.

Lý do của bốn tính chất lệnh bootstrap:

| Tính chất | Vì sao |
| --- | --- |
| Thiếu một giá trị ⇒ dừng, không ghi dòng nào | Sinh giá trị mặc định "cho tiện" là cách nhanh nhất để một mật khẩu mặc định đi thẳng lên bản chạy thật |
| Chạy lại không nhân đôi | Người vận hành không chắc lần trước đã xong thì chạy lại — phản xạ đúng, cần được hỗ trợ |
| Tài khoản đi qua `UserManager`, không qua SQL | Mật khẩu băm bằng PBKDF2 với salt ngẫu nhiên — không hàm SQL nào sinh được chuỗi băm hợp lệ ([`schema-core.md`](../../../database/schema-core.md) §4.1) |
| Tiến trình API phục vụ thật không tạo gì cả | Tạo bản cài là thao tác của người vận hành, không phải tác dụng phụ của việc khởi động |

Vì sao từ đơn vị thứ hai trở đi không dùng dòng lệnh: dựng đơn vị bằng SQL tay là tạo **đường thứ hai**
cho cùng một việc, và hai đường sẽ lệch nhau ngay lần đầu có người sửa một bên. Endpoint tạo đơn vị ở khu quản trị hệ thống gọi đúng service của bước 5.

Tên `superadmin` / `admin` là **dữ liệu** do người vận hành đặt, không phải vai trò. Luật **S10**
cấm mọi đoạn mã rẽ nhánh theo chuỗi tên đăng nhập — nên cái tên không bao giờ thành một `if`.

### 3.4 Quy trình trên database ĐANG CÓ DỮ LIỆU

Vì sao từng file một, không vòng lặp, khi database có dữ liệu thật: vòng lặp ở §3.3 chỉ dành cho DB
trống — nơi hỏng thì dựng lại.

### 3.5 Ba câu kiểm sức khoẻ

Câu (3) là câu bắt được ca nguy hiểm nhất trong ba: schema thật và schema mà repo mô tả đã lệch
nhau, mà tên file thì vẫn khớp nên không gì nghi ngờ.

### 3.6 Quyền của hai tài khoản database

**Vì sao ứng dụng không chạy bằng tài khoản chủ.** Chủ bảng tự cấp lại được mọi quyền mà nó đã tự
thu hồi. Ứng dụng chạy bằng tài khoản chủ thì hai dòng đầu của bảng quyền chỉ còn là lời hứa.

Vì sao `GRANT` và `REVOKE` nằm trong **một** transaction: không có khoảnh khắc nào tài khoản ứng dụng cầm
`UPDATE` trên `core.audit_log`.

**Vì sao chạy lại sau mỗi lần áp script, không dùng `ALTER DEFAULT PRIVILEGES`.** Quyền mặc định
áp cho **mọi** bảng tạo sau — kể cả khi một script dựng lại `core.audit_log`, bảng mới nhận luôn
`UPDATE` và `DELETE` mà không gì báo. Chạy lại khối cấp quyền thì quên là hỏng **ồn ào**: bảng mới báo
`permission denied` ngay lần đầu ứng dụng chạm tới nó.

Vì sao câu nghiệm thu dùng `has_table_privilege`: nó tính cả quyền thừa hưởng qua role, nên câu này bắt được cả ca
`coreandskill_app` bị gán làm thành viên của `coreandskill_owner` hay được nâng lên superuser — không
riêng ca quên `REVOKE`.

Phần (b) tìm bảng lịch sử theo **khuôn tên chung**, nên schema module mới lắp vào được kiểm mà không
phải sửa câu này. Phần (c) canh ca ngược: khuôn tên bị đổi ở một `DbContext` làm (b) khớp 0 bảng mà cả
câu vẫn trả 0 dòng — luật T6 ([`RULES.md`](../../../RULES.md) §8). Chốt (c) chỉ bắt được ca **mọi** bảng
lịch sử lệch khuôn; một schema module lệch khuôn một mình thì lọt — nên khuôn tên là luật ở
[`migration-policy.md`](../../../database/migration-policy.md) §1, không phải một thói quen.

## 4. Câu 4 — Sinh script bằng lệnh gì

Vì sao từng tham số cần thiết:

| Tham số | Vì sao cần |
| --- | --- |
| `--from` | Bỏ trống ⇒ EF sinh từ số 0, tức script dựng lại **cả** những bảng đang có dữ liệu |
| `--context` | Bỏ nó, `dotnet ef` báo lỗi hoặc — tệ hơn — chọn nhầm context và sinh script cho schema khác |

### 4.1 Vì sao `--idempotent`

Không có cờ này, script sinh ra là một dãy `ALTER`/`CREATE` trần. Có cờ này, **mỗi migration**
được bọc trong một khối kiểm `IF NOT EXISTS (… __ef_migrations_history …)`. Bốn thứ mua được, và cả bốn đều là hệ quả trực tiếp của quyết định "chạy tay" ở §0:

| # | Mua được gì |
| --- | --- |
| 1 | **Chạy lại an toàn.** Người vận hành không chắc lần trước đã xong chưa thì chạy lại — và đó là phản xạ đúng, không phải sai lầm cần chặn |
| 2 | **Chạy chồng lấn an toàn.** Một script `0005→0009` chạy trên database đã ở `0007` chỉ áp phần từ `0008` |
| 3 | **Không cần biết chính xác database đang ở đâu trước khi chạy.** Với thao tác tay, không ai bảo đảm được điều đó |
| 4 | **`__ef_migrations_history` được ghi đúng**, nên cơ chế §5 hoạt động |

**Cái giá, nói rõ để không ai bất ngờ:**

- File dài gấp vài lần và khó đọc — mỗi câu lệnh nằm trong một khối `DO`. Đọc diff của một script
  idempotent tốn công hơn hẳn.
- Bảo vệ theo `migration_id`, không theo nội dung — nên DDL chèn tay ngoài khối `DO` phải tự bọc (ví dụ ở file luật).
- **Không phải phép thử tính đúng.** Idempotent nghĩa là chạy nhiều lần cũng như một lần; nó
  không nói gì về việc một lần đó có đúng không.

### 4.2 Sau khi sinh — ba việc bắt buộc

Vì sao checksum tính **sau** khi đã thêm mọi thứ khác: nó là băm của file cuối cùng.

## 5. Câu 5 — Làm sao biết DB đã lệch model

> **Đây là câu quan trọng nhất của cả runbook, và là thứ dự án tiền nhiệm hoàn toàn không có.**

Ở đó, `dotnet ef database update` bị chặn, không code sản phẩm nào gọi `Database.Migrate()` hay
`GetPendingMigrations()`, và `__EFMigrationsHistory` **không** được coi là nguồn sự thật. Hệ quả:
câu hỏi *"database này đã có schema mà bản build này cần chưa"* **không có ai trả lời**. Cách duy
nhất để biết là chạy thử và xem có nổ không.

### 5.1 Cơ chế: app TỪ CHỐI KHỞI ĐỘNG khi thiếu migration

Vì sao ca "database có thừa migration" **cố ý không chặn**: quy trình thay đổi phá vỡ bốn bước đòi schema mới lên trước code
mới; chặn nó là chặn đúng quy trình an toàn mà ta vừa dựng ra. Nhưng nó vẫn phải **ồn ào**, vì
nếu không phải đang deploy thì đó là dấu hiệu ai đó chạy nhầm script lên nhầm môi trường.

### 5.2 Code mẫu

Lý do của ba tính chất thông điệp lỗi:

| Tính chất | Vì sao |
| --- | --- |
| Nêu **đúng tên** từng migration còn thiếu | *"Database chưa cập nhật"* không giúp ai. Tên migration tra thẳng ra được script nào chứa nó |
| Nêu **đường dẫn thư mục script** cần chạy | Người gặp lỗi này có thể là người vận hành, không phải người viết migration |
| Nêu **file tài liệu** cần đọc tiếp | Để họ không phải hỏi ai lúc 2 giờ sáng |

Vì sao test `Startup_Fails_When_PendingMigrationsExist` là bắt buộc: nếu bản thân phép kiểm
hỏng thì mọi thứ trở về đúng trạng thái mà mục này sinh ra để chấm dứt, và không có gì báo.

### 5.3 Vì sao im lặng chạy tiếp với DB lệch là cách hỏng TỆ NHẤT

Ba phương án khi phát hiện lệch, xếp theo mức độ tệ:

| Phương án | Chuyện gì xảy ra |
| --- | --- |
| **Tự chạy migration** | Đúng thứ §0 đã loại: đổi schema không ai đọc, không ai chọn thời điểm |
| **Im lặng chạy tiếp** | Xem dưới — tệ nhất |
| **Từ chối khởi động** | Hỏng **ngay**, hỏng **ồn ào**, hỏng **đúng chỗ** |

Im lặng chạy tiếp hỏng theo cách tệ nhất vì lỗi **lộ ra ở chỗ ngẫu nhiên và rất xa nguyên nhân**:

- App khởi động bình thường. Trang đăng nhập chạy. Mọi thứ **trông như** ổn.
- Lỗi bật ra ở **request đầu tiên chạm đúng cột còn thiếu** — có thể là mười phút sau, có thể là
  ba ngày sau khi có người dùng đầu tiên mở đúng màn hình đó.
- Triệu chứng là `42703 column "label_key" does not exist` từ một endpoint không liên quan gì tới
  lần deploy vừa rồi. Người trực nghi API vừa sửa, nghi dữ liệu, nghi cache — **không nghi rằng
  script schema chưa chạy**, vì bước đó "đã làm rồi".
- Trong khoảng thời gian đó, mọi request khác **vẫn ghi dữ liệu** theo hình dạng cũ. Khi cuối
  cùng chạy được script, có thể đã có dữ liệu cần backfill mà không ai biết là cần.

Từ chối khởi động biến toàn bộ chuỗi đó thành **một dòng log tại đúng giây deploy**, nêu đích
danh cái còn thiếu. Cái giá là một lần deploy hỏng — rẻ hơn mọi phương án khác một bậc độ lớn.

> **Đây cũng đúng khuôn lỗi mà [`RULES.md`](../../../RULES.md) cảnh báo xuyên suốt:** hỏng im lặng
> đắt hơn hỏng ồn ào, vì hỏng im lặng không có ai để sửa nó.

### 5.4 Lệch chiều ngược — model đổi mà chưa sinh migration

Vì sao cơ chế §5.1 không thấy ca này: mọi migration đã biết đều đã áp, nên so sánh hai tập không lệch. Lệnh `has-pending-model-changes` không sinh file
nào nên chạy trong CI vô hại.

### 5.5 Giới hạn thật của cơ chế — đừng coi nó là hàng rào chống mọi kiểu lệch

Vì sao không được chèn tay vào bảng lịch sử migration: chèn một dòng vào đó để "cho app
khởi động" vô hiệu hoá toàn bộ cơ chế — và sẽ có người bị cám dỗ làm thế vào lúc gấp.

## 6. Môi trường dev — cùng đường với §3.3

Vì sao máy dev chạy đúng năm bước của §3.3: nhờ vậy đường cài đặt thật được chạy mỗi ngày
trên máy dev, thay vì chỉ chạy một lần vào lúc căng nhất (§0).

Vì sao dựng lại từ đầu thay vì sửa một database dev đã lệch: trên máy dev đó là thao tác rẻ nhất.

## 7. Checklist trước khi chạy script trên PRODUCTION

Vì sao dòng "xác nhận connection string" không phải thủ tục thừa: chạy nhầm script lên nhầm môi trường là loại sự cố xảy ra
với người có kinh nghiệm nhất, vì nó không đòi hỏi sai sót kỹ thuật nào — chỉ cần một cửa sổ
terminal còn mở từ hôm trước.

## 8. Khôi phục mật khẩu tài khoản vận hành hệ thống

Vì sao không ai trong ứng dụng đặt lại hộ được: không tài khoản nào đứng trên tài khoản vận hành, và thao tác đặt lại hộ ở khu quản trị hệ
thống chỉ nhận tài khoản quản trị của một đơn vị nghiệp vụ.

Ai chạy được lệnh này thì **đã** có quyền shell trên máy chủ và đọc được cấu hình của ứng dụng —
lệnh không trao thêm quyền nào cho ai. Cùng lập luận với
[`V1-cai-dat-lan-dau.md`](../../../luong/V1-cai-dat-lan-dau.md) §1.

## 9. Seed lại dữ liệu mặc định cho mọi đơn vị — khi lắp module mới

—

## 10. Phát lại bản ghi outbox chết

—
