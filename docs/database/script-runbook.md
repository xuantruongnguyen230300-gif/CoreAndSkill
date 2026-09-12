---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Runbook script database — đường đi cụ thể từ model tới database

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`, chưa có thư mục script, chưa có
> database nào. Mọi lệnh dưới đây là lệnh **sẽ** chạy được ở giai đoạn 2, không phải lệnh đã
> chạy.
>
> **File chủ về thao tác áp schema.** Nội dung schema: [`schema-core.md`](schema-core.md).
> Ai sở hữu migration nào: [`migration-policy.md`](migration-policy.md).

---

## 0. Quyết định nền — áp schema CHẠY TAY

> **Không có `Database.Migrate()` ở bất kỳ đâu trong code sản phẩm. Không auto-migrate lúc khởi
> động. Schema áp bằng script `.sql` do con người chạy.**

Ba lý do:

1. **Auto-migrate biến mỗi lần khởi động thành một lần đổi schema.** Với nhiều instance, chúng
   khởi động cùng lúc và cùng chạy migration — Postgres có khoá tư vấn nên không hỏng dữ liệu,
   nhưng instance thua cuộc chờ, và thời gian chờ đó là downtime không ai dự kiến.
2. **Không ai đọc thứ chạy tự động.** Một lệnh `DROP COLUMN` sinh nhầm sẽ chạy trước khi có người
   kịp nhìn thấy nó. Ở dự án tiền nhiệm, một migration sinh tự động chứa **năm** lệnh `DropTable`
   và lưới an toàn duy nhất là người đọc phát hiện ra kịp.
3. **Người vận hành phải chọn được thời điểm.** Đổi schema trên dữ liệu thật là thao tác có cửa
   sổ, có backup, có người trực. Gắn nó vào lần khởi động kế tiếp là bỏ hết ba thứ đó.

Ràng buộc này được cưỡng chế bằng máy, không bằng câu văn: khối `permissions.deny` trong cấu
hình harness chặn thẳng `dotnet ef database update`, `dotnet ef database drop`,
`dotnet ef migrations remove`. Agent không chạm database, kể cả khi được nhờ.

📖 Quyết định gốc và các phương án đã loại:
[`../adr/0009-ap-schema-chay-tay.md`](../adr/0009-ap-schema-chay-tay.md).

> 🛑 **Không auto-migrate ở BẤT KỲ môi trường nào — kể cả máy dev.** Đây là điểm sẽ bị đòi nới,
> nên ghi lý do ra ngay: bật auto-migrate cho dev tạo ra **hai đường khác nhau giữa các môi
> trường**. Đường chạy hằng ngày, được thử hàng trăm lần, là đường tự động; đường thật sự chạm
> production là đường thủ công, và nó được thử **đúng một lần**, vào lúc căng nhất. Mọi lỗi thuộc
> về script — sai thứ tự, thiếu một bước, câu lệnh chạy lại lần hai thì hỏng — sẽ **chỉ xảy ra ở
> production**, vì ở dev không ai đi qua đường đó.
>
> Nguyên tắc: đường đưa thay đổi vào database phải **giống nhau ở mọi môi trường**; chỉ khác ở
> người bấm nút.

**Nhưng "chạy tay" không được phép có nghĩa là "mỗi người tự nghĩ ra cách".** Toàn bộ file này
tồn tại để trả lời năm câu, mỗi câu một mục, cụ thể tới mức chép ra chạy được.

---

## 1. Câu 1 — Script nằm ở đâu

**Một đường dẫn cố định, tính từ gốc repo: `<gốc repo>/database/scripts/`.**

```text
database/
  scripts/
    core/                          ← Core sở hữu, schema `core`
      0001__core__initial.sql
      0002__core__add-notification.sql
    modules/
      banhang/                     ← Module `banhang` sở hữu, schema `banhang`
        0001__banhang__initial.sql
    seed/                          ← Dữ liệu danh mục, KHÔNG phải schema
      core-permission-catalog.sql
    README.md                      ← trỏ về chính file này, không chép nội dung
```

| Thư mục | Ai được thêm file vào | Chạy khi nào |
| --- | --- | --- |
| `core/` | Chỉ người đang sửa `Core.Infrastructure` | **Trước** mọi script module |
| `modules/<x>/` | Chỉ người đang sửa `Modules.<X>.Infrastructure` | Sau `core/` |
| `seed/` | Tuỳ chủ sở hữu dữ liệu | Sau khi schema tương ứng đã áp |

**Vì sao tách theo bên sở hữu chứ không gộp một thư mục phẳng.** Nó ánh xạ thẳng luật E6
([`migration-policy.md`](migration-policy.md) §7): script chạm schema `core` mà nằm trong
`modules/banhang/` là sai chỗ, và nhìn đường dẫn là thấy — không cần mở file. Nó cũng làm việc
cấp số ở §2 không bao giờ va giữa hai bên.

**Vì sao thư mục này ở gốc repo chứ không nằm trong `src/BE/`.** Người chạy script là người vận
hành, không nhất thiết là lập trình viên .NET. Bắt họ đi vào ba tầng thư mục project để tìm một
file `.sql` là một rào cản không cần thiết, và là lý do thật để người ta bỏ qua quy trình.

**`seed/` tách khỏi `core/` có chủ đích.** Script schema và script dữ liệu có vòng đời khác
nhau: schema chạy một lần theo thứ tự, seed có thể chạy lại bất cứ lúc nào. Trộn chúng làm câu
hỏi *"script này đã chạy chưa"* mất nghĩa cho một nửa số file.

---

## 2. Câu 2 — Đặt tên thế nào

```text
NNNN__<owner>__<mo-ta-ngan-khong-dau>.sql
```

| Phần | Luật | Ví dụ |
| --- | --- | --- |
| `NNNN` | Bốn chữ số, có số 0 đứng đầu, **cấp theo từng owner** | `0007` |
| `owner` | `core` hoặc khoá module, chữ thường | `core`, `banhang` |
| Mô tả | kebab-case, **không dấu**, ngắn nhưng đủ đoán nội dung | `add-notification` |
| Ngăn cách | **Hai** dấu gạch dưới giữa các phần | `__` |

Ví dụ hợp lệ: `0001__core__initial.sql`, `0008__core__rename-menu-name-to-label-key.sql`,
`0003__banhang__add-index-don-hang-ngay-tao.sql`.

**Vì sao có tiền tố số thay vì dùng timestamp như EF.** Số bốn chữ số sắp xếp đúng bằng `ls`,
đọc được bằng mắt, và **nói thẳng thứ tự** — thứ mà một chuỗi `20260908143255` nói được nhưng
không ai đọc nổi. Người vận hành cần trả lời câu *"tôi đang ở 0006, còn thiếu mấy cái"* trong
hai giây.

**Vì sao cấp số theo owner chứ không cấp chung.** Core và module phát triển song song và không
biết nhau. Một dãy số chung nghĩa là mỗi lần một module thêm script, Core phải đi hỏi số tiếp
theo là bao nhiêu — một điểm phối hợp không đổi lấy gì.

### 2.1 Hai người cùng thêm script — cách xử lý

Hai nhánh cùng tạo `0007__core__…` là chuyện sẽ xảy ra. Luật:

> **Số thứ tự được chốt lúc MERGE, không phải lúc tạo nhánh. Ai merge sau thì đổi tên file của
> mình.**

Đổi tên là an toàn **khi và chỉ khi** script đó chưa được áp lên bất kỳ database dùng chung nào
— và điều đó luôn đúng với script chưa merge, vì quy trình cấm chạy script từ một nhánh chưa
vào `main` lên database dùng chung.

> 🛑 **Không bao giờ đổi tên một script đã có dòng trong `core.schema_script_history` của bất kỳ
> database nào.** Bảng đó ghi nhận theo **tên file**. Đổi tên làm mọi database đã chạy tưởng
> mình còn thiếu script, và lần chạy kế tiếp áp lại nó. Với script idempotent thì vô hại; với
> một script có `INSERT` không phòng trùng thì đó là dữ liệu nhân đôi.
>
> Nếu buộc phải đổi tên một script đã áp: `UPDATE core.schema_script_history SET script_name = …`
> trên **mọi** database, cùng lượt. Đây là lý do luật trên tồn tại — để không bao giờ phải làm
> việc đó.

**Cổng CI kiểm trùng số** (thêm vào workflow ở giai đoạn 2):

```bash
for dir in database/scripts/core database/scripts/modules/*; do
  dup=$(ls "$dir" 2>/dev/null | grep -oE '^[0-9]{4}' | sort | uniq -d)
  if [ -n "$dup" ]; then echo "TRÙNG SỐ trong $dir: $dup"; exit 1; fi
done
```

PASS: không in gì, thoát 0.

---

## 3. Câu 3 — Chạy theo thứ tự nào, và biết mình đang ở đâu

### 3.1 Bảng lịch sử áp dụng

`core.schema_script_history` — định nghĩa cột đầy đủ ở [`schema-core.md`](schema-core.md) §9.1.
Nó trả lời câu *"database này đã chạy những script nào"* cho **con người**.

> **Đừng nhầm nó với `core.__ef_migrations_history`.** Bảng của EF trả lời *"EF tin migration nào
> đã áp"*, và được ghi tự động bởi chính script sinh với `--idempotent`. Bảng này thì do **người
> chạy script ghi**, và giữ thêm thứ EF không có: băm nội dung file, ai chạy, chạy lúc nào, mất
> bao lâu. Hai bảng, hai câu hỏi, không suy ra nhau được.

Mỗi script kết thúc bằng đúng khối này:

```sql
-- Ghi nhận đã áp. Đặt ở CUỐI file, TRONG cùng transaction với phần thay đổi schema.
INSERT INTO core.schema_script_history (script_name, checksum_sha256, owner, applied_by, note)
VALUES ('0002__core__add-notification.sql',
        'a3f1…',                      -- sha256 nội dung file, xem §3.2
        'core',
        current_user,
        NULL)
ON CONFLICT (script_name) DO NOTHING;
```

**`ON CONFLICT DO NOTHING` là bắt buộc**, không phải phòng xa: người vận hành sẽ chạy lại một
file khi không chắc lần trước đã xong chưa, và đó là hành vi đúng cần được hỗ trợ chứ không phải
hành vi cần chặn.

**Nằm trong cùng transaction cũng bắt buộc.** Ghi lịch sử ngoài transaction nghĩa là có một cửa
sổ mà schema đã đổi nhưng lịch sử chưa ghi (hoặc ngược lại) — và cửa sổ đó, nếu tiến trình chết
đúng lúc, để lại một database mà không ai biết nó ở đâu.

### 3.2 Tính `checksum_sha256`

```bash
sha256sum database/scripts/core/0002__core__add-notification.sql
```

Trên Windows PowerShell:

```powershell
Get-FileHash database/scripts/core/0002__core__add-notification.sql -Algorithm SHA256
```

Dán giá trị vào khối `INSERT` **trước khi** commit script. Băm bắt được đúng một ca, và là ca
khó thấy nhất: **ai đó sửa một script đã áp**. Người sửa tin cả hệ đã có thay đổi; mọi database
đã chạy bản cũ thì không, và không có gì báo. Câu kiểm ở §3.5 phát hiện ca này.

### 3.3 Quy trình từ database TRỐNG

**Bốn phần, theo đúng thứ tự.** Lược đồ → danh mục dùng chung → đơn vị hệ thống và tài khoản vận hành → đơn vị nghiệp vụ đầu tiên.

Phần 3 và 4 là chỗ quy trình này khác một bản cài không có nhiều đơn vị: [`../adr/0017-khu-quan-tri-he-thong.md`](../adr/0017-khu-quan-tri-he-thong.md) tách hai vai không chồng lấn, và **tài khoản thứ hai chỉ ra đời sau khi tài khoản thứ nhất đăng nhập được**. Xem luồng [`../luong/V1-cai-dat-lan-dau.md`](../luong/V1-cai-dat-lan-dau.md).

```bash
# 0. Tao database rong (mot lan).
createdb -h "$PGHOST" -U "$PGUSER" coreandskill

# 1. Script Core, theo dung thu tu so.
for f in database/scripts/core/*.sql; do
  echo ">>> $f"
  psql -h "$PGHOST" -U "$PGUSER" -d coreandskill -v ON_ERROR_STOP=1 -f "$f" || break
done

# 2. Script tung module (chi module du an nay that su lap).
for f in database/scripts/modules/banhang/*.sql; do
  echo ">>> $f"
  psql -h "$PGHOST" -U "$PGUSER" -d coreandskill -v ON_ERROR_STOP=1 -f "$f" || break
done

# 3. Danh muc quyen — DUNG CHUNG toan he, khong thuoc don vi nao.
psql -h "$PGHOST" -U "$PGUSER" -d coreandskill -v ON_ERROR_STOP=1 \
     -f database/scripts/seed/core-permission-catalog.sql

# 4. Mat khau — nguoi van hanh tu dat, KHONG hardcode, KHONG commit.
dotnet user-secrets set "Bootstrap:SysOpUserName" "<tu-dat>" --project src/BE/CoreAndSkill.Api
dotnet user-secrets set "Bootstrap:SysOpPassword" "<tu-dat>" --project src/BE/CoreAndSkill.Api

# 5. Don vi he thong + tai khoan van hanh. Chay mot lan roi thoat, KHONG mo cong.
dotnet run --project src/BE/CoreAndSkill.Api -- --seed

# 6. Don vi nghiep vu dau tien: KHONG lam o day.
#    Dang nhap bang tai khoan vua tao, roi tao don vi qua khu quan tri he thong.
#    Xem luong V2.
```

> 🛑 **Bước 6 cố ý không có lệnh.** Từ đơn vị thứ hai trở đi — và cả đơn vị **đầu tiên** — việc tạo đơn vị đi qua `POST /api/v1/core/system/tenants`, không qua dòng lệnh. Endpoint đó seed đủ **bốn** thứ mà một đơn vị cần và tạo tài khoản quản trị đầu tiên của nó ([`../luong/V2-tao-don-vi-moi.md`](../luong/V2-tao-don-vi-moi.md)).
>
> Dựng đơn vị nghiệp vụ bằng SQL ở đây là tạo **đường thứ hai** cho cùng một việc, và hai đường sẽ lệch nhau ngay lần đầu có người sửa một bên.

> 🛑 **`-v ON_ERROR_STOP=1` là bắt buộc.** Mặc định `psql` **chạy tiếp** sau khi một câu lệnh lỗi và thoát với mã 0. Không có cờ này, một script hỏng giữa chừng vẫn kết thúc như thành công, và database còn lại một nửa. Đây là mặc định gây bất ngờ nhất của `psql`.

> **Trong DBeaver/pgAdmin:** bấm **Execute script** (`Alt+X` ở DBeaver), **không** phải `Ctrl+Enter`. `Ctrl+Enter` chỉ chạy một câu lệnh dưới con trỏ, và người chạy sẽ tưởng cả tệp đã chạy.

**Nghiệm thu từng phần:**

| Sau bước | Câu kiểm | Mong đợi |
| --- | --- | --- |
| 1 | Các câu kiểm ở [`schema-core.md`](schema-core.md) §11 | Đúng như "mong đợi" ghi trong từng câu |
| 3 | `SELECT count(*) FROM core.permission` | Khác 0 |
| 5 | `SELECT code, is_system FROM core.tenant` | Đúng **một** dòng, và `is_system` là `true` |
| 5 | Đăng nhập bằng tài khoản vận hành | Thành công, buộc đổi mật khẩu ngay |
| 6 | `SELECT code, is_system FROM core.tenant` | Thêm một dòng `is_system = false` |

> **Vì sao bước 5 không làm bằng SQL.** Mật khẩu băm bằng PBKDF2 với salt ngẫu nhiên — không hàm SQL nào sinh được chuỗi băm hợp lệ ([`schema-core.md`](schema-core.md) §4.1). Thiếu secret ở bước 4 thì lệnh **cố ý dừng và không ghi dòng nào**.

> 🛑 **`-v ON_ERROR_STOP=1` là bắt buộc.** Mặc định `psql` **chạy tiếp** sau khi một câu lệnh
> lỗi và thoát với mã 0. Không có cờ này, một script hỏng giữa chừng vẫn kết thúc như thành
> công, và database còn lại một nửa. Đây là mặc định gây bất ngờ nhất của `psql`.

> **Trong DBeaver/pgAdmin:** bấm **Execute script** (`Alt+X` ở DBeaver), **không** phải
> `Ctrl+Enter`. `Ctrl+Enter` chỉ chạy một câu lệnh dưới con trỏ, và người chạy sẽ tưởng cả file
> đã chạy.

### 3.4 Quy trình trên database ĐANG CÓ DỮ LIỆU

Khác ở chỗ: **không chạy từ đầu**, chỉ chạy phần còn thiếu.

```sql
-- Bước 1: xem đang ở đâu.
SELECT script_name, owner, applied_at, applied_by
FROM   core.schema_script_history
ORDER  BY owner, script_name;
```

```bash
# Bước 2: đối chiếu với file trong repo — in ra script còn THIẾU.
psql -h "$PGHOST" -U "$PGUSER" -d "$PGDATABASE" -At \
     -c "SELECT script_name FROM core.schema_script_history" | sort > /tmp/da-chay.txt
(cd database/scripts && ls core/*.sql modules/*/*.sql | xargs -n1 basename) | sort > /tmp/trong-repo.txt
comm -13 /tmp/da-chay.txt /tmp/trong-repo.txt        # còn thiếu, theo đúng thứ tự
```

```bash
# Bước 3: chạy đúng những file ở bước 2, theo thứ tự, TỪNG FILE MỘT, đọc kết quả từng cái.
psql -h "$PGHOST" -U "$PGUSER" -d "$PGDATABASE" -v ON_ERROR_STOP=1 \
     -f database/scripts/core/0007__core__add-outbox-retry-columns.sql
```

**Từng file một, không vòng lặp**, khi database có dữ liệu thật. Vòng lặp ở §3.3 chỉ dành cho DB
trống — nơi hỏng thì dựng lại.

### 3.5 Ba câu kiểm sức khoẻ

```sql
-- (1) Script trong repo mà DB chưa chạy?  → dùng lệnh comm ở §3.4.

-- (2) Script DB đã chạy mà repo không còn?  Mong đợi: 0 dòng.
--     Có dòng nghĩa là ai đó đã xoá hoặc đổi tên một script đã áp (§2.1).
SELECT script_name FROM core.schema_script_history
WHERE  script_name NOT IN ( /* dán danh sách tên file từ repo vào đây */ );

-- (3) Nội dung script đã đổi sau khi áp?
--     So checksum trong DB với sha256sum của file cùng tên trong repo.
SELECT script_name, checksum_sha256 FROM core.schema_script_history ORDER BY script_name;
```

Câu (3) là câu bắt được ca nguy hiểm nhất trong ba: schema thật và schema mà repo mô tả đã lệch
nhau, mà tên file thì vẫn khớp nên không gì nghi ngờ.

---

## 4. Câu 4 — Sinh script bằng lệnh gì

**Chạy từ gốc repo.** Project chứa migration là `Core.Infrastructure`; startup project là host.

```bash
# Script cho MỘT migration mới (từ migration trước đó tới nó).
dotnet ef migrations script AddIndexRolePermission AddNotificationRecipientTable \
    --idempotent \
    --project        src/BE/Core/CoreAndSkill.Core.Infrastructure \
    --startup-project src/BE/CoreAndSkill.Api \
    --context        CoreDbContext \
    --output         database/scripts/core/0009__core__add-notification-recipient.sql
```

| Tham số | Vì sao cần |
| --- | --- |
| `--from` (đối số thứ nhất) | Migration **đã có** trên database đích. Bỏ trống ⇒ EF sinh từ số 0, tức script dựng lại **cả** những bảng đang có dữ liệu |
| `--to` (đối số thứ hai) | Migration đích. Bỏ trống ⇒ tới migration cuối cùng |
| `--idempotent` | Xem §4.1 — bắt buộc |
| `--project` | Nơi migration sống. Với schema `core` là `Core.Infrastructure` ([`migration-policy.md`](migration-policy.md) §1) |
| `--startup-project` | Nơi `dotnet ef` lấy cấu hình (connection string, design-time factory) |
| `--context` | **Bắt buộc khi có nhiều `DbContext`.** Bỏ nó, `dotnet ef` báo lỗi hoặc — tệ hơn — chọn nhầm context và sinh script cho schema khác |

Script của module đổi ba tham số cuối, giữ nguyên khuôn:

```bash
dotnet ef migrations script <từ> <tới> \
    --idempotent \
    --project        src/BE/Modules/BanHang/CoreAndSkill.Modules.BanHang.Infrastructure \
    --startup-project src/BE/CoreAndSkill.Api \
    --context        BanHangDbContext \
    --output         database/scripts/modules/banhang/0003__banhang__them-cot-ghi-chu.sql
```

### 4.1 Vì sao `--idempotent`

Không có cờ này, script sinh ra là một dãy `ALTER`/`CREATE` trần. Có cờ này, **mỗi migration**
được bọc trong một khối kiểm:

```sql
DO $EF$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM core."__ef_migrations_history"
                   WHERE "migration_id" = '20260908143255_AddNotificationRecipientTable') THEN
        CREATE TABLE core.notification_recipient ( ... );
    END IF;
END $EF$;
```

Bốn thứ mua được, và cả bốn đều là hệ quả trực tiếp của quyết định "chạy tay" ở §0:

| # | Mua được gì |
| --- | --- |
| 1 | **Chạy lại an toàn.** Người vận hành không chắc lần trước đã xong chưa thì chạy lại — và đó là phản xạ đúng, không phải sai lầm cần chặn |
| 2 | **Chạy chồng lấn an toàn.** Một script `0005→0009` chạy trên database đã ở `0007` chỉ áp phần từ `0008` |
| 3 | **Không cần biết chính xác database đang ở đâu trước khi chạy.** Với thao tác tay, không ai bảo đảm được điều đó |
| 4 | **`__ef_migrations_history` được ghi đúng**, nên cơ chế §5 hoạt động |

**Cái giá, nói rõ để không ai bất ngờ:**

- File dài gấp vài lần và khó đọc — mỗi câu lệnh nằm trong một khối `DO`. Đọc diff của một script
  idempotent tốn công hơn hẳn.
- **Bảo vệ theo `migration_id`, không theo nội dung.** DDL viết tay chèn thêm vào file, **ngoài**
  khối `DO`, hoàn toàn không được bảo vệ và sẽ chạy lại mỗi lần. Chèn tay thì phải tự bọc:

  ```sql
  CREATE INDEX IF NOT EXISTS ix_notification_recipient_unread
      ON core.notification_recipient (user_id, created_at DESC)
      WHERE read_at IS NULL AND is_deleted = false;
  ```

- **Không phải phép thử tính đúng.** Idempotent nghĩa là chạy nhiều lần cũng như một lần; nó
  không nói gì về việc một lần đó có đúng không.

### 4.2 Sau khi sinh — ba việc bắt buộc

1. **Đọc file.** Đối chiếu với bảng dấu hiệu ở [`migration-policy.md`](migration-policy.md) §3.2.
   `DropTable` hoặc `CreateTable` cho bảng đã có ⇒ **dừng**, `--from` sai hoặc snapshot lệch.
2. **Đổi tên file theo §2** nếu `--output` chưa đúng khuôn.
3. **Thêm khối ghi `schema_script_history`** ở cuối file (§3.1), kèm checksum tính ở §3.2.
   Checksum tính **sau** khi đã thêm mọi thứ khác — nó là băm của file cuối cùng.

---

## 5. Câu 5 — Làm sao biết DB đã lệch model

> **Đây là câu quan trọng nhất của cả runbook, và là thứ dự án tiền nhiệm hoàn toàn không có.**

Ở đó, `dotnet ef database update` bị chặn, không code sản phẩm nào gọi `Database.Migrate()` hay
`GetPendingMigrations()`, và `__EFMigrationsHistory` **không** được coi là nguồn sự thật. Hệ quả:
câu hỏi *"database này đã có schema mà bản build này cần chưa"* **không có ai trả lời**. Cách duy
nhất để biết là chạy thử và xem có nổ không.

### 5.1 Cơ chế: app TỪ CHỐI KHỞI ĐỘNG khi thiếu migration

Lúc khởi động, với **từng** `DbContext`, so migration mà assembly biết với migration mà database
đã ghi nhận:

| Trạng thái | Nghĩa | Hành động |
| --- | --- | --- |
| Database **thiếu** migration mà app biết | Chưa chạy script | 🛑 **Từ chối khởi động** |
| Database **có thừa** migration mà app không biết | Schema đi trước code — trạng thái **hợp lệ** ở bước "mở rộng" của §5 [`migration-policy.md`](migration-policy.md) | ⚠️ Ghi log cảnh báo, **vẫn khởi động** |
| Khớp | Bình thường | Khởi động |

Ca thứ hai **cố ý không chặn**. Quy trình thay đổi phá vỡ bốn bước đòi schema mới lên trước code
mới; chặn nó là chặn đúng quy trình an toàn mà ta vừa dựng ra. Nhưng nó vẫn phải **ồn ào**, vì
nếu không phải đang deploy thì đó là dấu hiệu ai đó chạy nhầm script lên nhầm môi trường.

### 5.2 Code mẫu

`Core.Infrastructure` — bộ kiểm:

```csharp
namespace CoreAndSkill.Core.Infrastructure.Persistence;

public sealed record SchemaDriftReport(
    string ContextName,
    IReadOnlyList<string> MissingInDatabase,
    IReadOnlyList<string> UnknownToApplication);

public interface ISchemaVerifier
{
    Task<IReadOnlyList<SchemaDriftReport>> InspectAsync(CancellationToken ct);
}

internal sealed class SchemaVerifier(IEnumerable<DbContext> contexts) : ISchemaVerifier
{
    public async Task<IReadOnlyList<SchemaDriftReport>> InspectAsync(CancellationToken ct)
    {
        var reports = new List<SchemaDriftReport>();

        foreach (var context in contexts)
        {
            var known   = context.Database.GetMigrations().ToHashSet(StringComparer.Ordinal);
            var applied = (await context.Database.GetAppliedMigrationsAsync(ct))
                          .ToHashSet(StringComparer.Ordinal);

            reports.Add(new SchemaDriftReport(
                ContextName:          context.GetType().Name,
                MissingInDatabase:    known.Except(applied).Order().ToList(),
                UnknownToApplication: applied.Except(known).Order().ToList()));
        }

        return reports;
    }
}
```

`Core.Web` — nối vào đường khởi động, bên trong `UseCore()`:

```csharp
public static async Task<WebApplication> UseCoreAsync(this WebApplication app)
{
    await app.VerifyDatabaseSchemaAsync();
    // … phần còn lại của UseCore()
    return app;
}

private static async Task VerifyDatabaseSchemaAsync(this WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var verifier = scope.ServiceProvider.GetRequiredService<ISchemaVerifier>();
    var logger   = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var reports = await verifier.InspectAsync(app.Lifetime.ApplicationStopping);

    foreach (var report in reports.Where(r => r.UnknownToApplication.Count > 0))
    {
        logger.LogWarning(
            "Database di TRUOC code cho {Context}: {Count} migration database co ma ban build nay khong biet ({List}). "
          + "Hop le neu dang o buoc 'mo rong'; neu khong, kiem tra da chay nham script len nham moi truong chua.",
            report.ContextName,
            report.UnknownToApplication.Count,
            string.Join(", ", report.UnknownToApplication));
    }

    var blocking = reports.Where(r => r.MissingInDatabase.Count > 0).ToList();
    if (blocking.Count == 0)
    {
        return;
    }

    var message = new StringBuilder()
        .AppendLine("KHOI DONG BI TU CHOI — database thieu migration.")
        .AppendLine();

    foreach (var report in blocking)
    {
        message.AppendLine($"  {report.ContextName} thieu {report.MissingInDatabase.Count} migration:");
        foreach (var id in report.MissingInDatabase)
        {
            message.AppendLine($"    - {id}");
        }
        message.AppendLine($"    Script can chay: database/scripts/{OwnerFolderOf(report.ContextName)}/");
        message.AppendLine();
    }

    message
        .AppendLine("Cach xu ly: doc docs/database/script-runbook.md muc 3.4 (chay phan con thieu).")
        .AppendLine("Sau khi chay xong, khoi dong lai. KHONG bo qua kiem tra nay.");

    throw new InvalidOperationException(message.ToString());
}
```

Ba tính chất của thông điệp lỗi, mỗi cái đều có lý do:

| Tính chất | Vì sao |
| --- | --- |
| Nêu **đúng tên** từng migration còn thiếu | *"Database chưa cập nhật"* không giúp ai. Tên migration tra thẳng ra được script nào chứa nó |
| Nêu **đường dẫn thư mục script** cần chạy | Người gặp lỗi này có thể là người vận hành, không phải người viết migration |
| Nêu **file tài liệu** cần đọc tiếp | Để họ không phải hỏi ai lúc 2 giờ sáng |

Luật E8 ([`../RULES.md`](../RULES.md) §4) canh cơ chế này bằng integration test
`Startup_Fails_When_PendingMigrationsExist`. Đây là một test **bắt buộc**: nếu bản thân phép kiểm
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

> **Đây cũng đúng khuôn lỗi mà [`../RULES.md`](../RULES.md) cảnh báo xuyên suốt:** hỏng im lặng
> đắt hơn hỏng ồn ào, vì hỏng im lặng không có ai để sửa nó.

### 5.4 Lệch chiều ngược — model đổi mà chưa sinh migration

Mục 5.1 bắt ca *"code đi trước database"*. Còn ca *"model đi trước migration"* — có người sửa
entity mà quên `migrations add` — thì cơ chế trên **không thấy**, vì mọi migration đã biết đều đã
áp.

Bắt nó ở CI, không ở lúc chạy:

```bash
dotnet ef migrations has-pending-model-changes \
    --project        src/BE/Core/CoreAndSkill.Core.Infrastructure \
    --startup-project src/BE/CoreAndSkill.Api \
    --context        CoreDbContext
```

PASS: *"No changes have been made to the model since the last migration."* Chạy lệnh này cho
**từng** `DbContext`. Lệnh không sinh file nào nên chạy trong CI vô hại.

Hai lưới bắt hai chiều lệch khác nhau, và cần cả hai:

| Lệch | Ai bắt |
| --- | --- |
| Model → migration (quên sinh) | CI, lệnh `has-pending-model-changes` |
| Migration → database (quên chạy script) | Lúc khởi động, §5.1 |

### 5.5 Giới hạn thật của cơ chế — đừng coi nó là hàng rào chống mọi kiểu lệch

Phép kiểm §5.1 đọc **bảng lịch sử migration**, không đọc cấu trúc bảng. Vì vậy nó **không** bắt
được hai ca sau, và cả hai đều xảy ra được:

| Ca không bắt được | Tại sao | Lưới thay thế |
| --- | --- | --- |
| Ai đó sửa cột **trực tiếp** trên database | Lịch sử migration vẫn đầy đủ, app vẫn khởi động | Các câu kiểm ở [`schema-core.md`](schema-core.md) §11, chạy định kỳ |
| Ai đó **sửa nội dung** một script đã áp | Tên file không đổi nên câu đối chiếu ở §3.4 vẫn khớp | `checksum_sha256` ở §3.2 — đây chính là lý do cột đó tồn tại |

> 🛑 **Bảng lịch sử migration là thứ KHÔNG được đụng tay.** Chèn một dòng vào đó để "cho app
> khởi động" vô hiệu hoá toàn bộ cơ chế này — và sẽ có người bị cám dỗ làm thế vào lúc gấp. Nếu
> app từ chối khởi động, đường đúng là **chạy script**, không phải sửa bảng lịch sử.

---

## 6. Môi trường dev — dựng database từ trống

Bảy bước, không hơn. Nếu ai đó cần bước thứ tám, bước đó phải vào file này.

Hai phần tách rõ, theo [`../adr/0022-seed-dev-khong-co-duong-code-rieng.md`](../adr/0022-seed-dev-khong-co-duong-code-rieng.md): **dữ liệu** đi bằng `.sql`, **tài khoản** đi bằng lệnh bootstrap. Không tệp nào trong repo chứa mật khẩu.

```bash
# 1. Container Postgres (hoac dung Postgres da cai san, bo qua buoc nay).
docker run -d --name coreandskill-db -e POSTGRES_PASSWORD=dev -p 5432:5432 postgres:17

# 2. Database rong.
createdb -h localhost -U postgres coreandskill_dev

# 3. Schema Core.
for f in database/scripts/core/*.sql; do
  psql -h localhost -U postgres -d coreandskill_dev -v ON_ERROR_STOP=1 -f "$f" || break
done

# 4. Danh muc quyen (dung chung toan he, khong thuoc don vi nao).
psql -h localhost -U postgres -d coreandskill_dev -v ON_ERROR_STOP=1 -f database/scripts/seed/core-permission-catalog.sql

# 5. DU LIEU dev: don vi he thong + don vi DEV, vai tro, anh xa vai tro-quyen, menu.
#    Tep nay nam TRONG src/BE/, KHONG nam trong database/scripts/ - xem canh bao duoi.
psql -h localhost -U postgres -d coreandskill_dev -v ON_ERROR_STOP=1 -f src/BE/CoreAndSkill.Api/Seed/seed-dev-data.sql

# 6. Mat khau - KHONG hardcode, KHONG commit. Moi may tu dat.
dotnet user-secrets set "Bootstrap:AdminUserName" "admin"      --project src/BE/CoreAndSkill.Api
dotnet user-secrets set "Bootstrap:AdminPassword" "<tu-dat>"   --project src/BE/CoreAndSkill.Api
dotnet user-secrets set "Bootstrap:SysOpUserName" "superadmin" --project src/BE/CoreAndSkill.Api
dotnet user-secrets set "Bootstrap:SysOpPassword" "<tu-dat>"   --project src/BE/CoreAndSkill.Api

# 7. HAI tai khoan, qua UserManager.
dotnet run --project src/BE/CoreAndSkill.Api -- --seed
```

> 🛑 **Bước 5: tệp `.sql` dev phải nằm NGOÀI `database/scripts/`.** Bước 3 và §3.3 chạy **glob** `database/scripts/core/*.sql` **không có allowlist** — một tệp seed dev rơi vào thư mục đó sẽ tự chạy trên **mọi** bản cài, kể cả bản chạy thật.
>
> Đổi lại, tệp đó **không** được cổng checksum ở §3.4 canh. Chấp nhận được vì nó chỉ dựng dữ liệu dev, không dựng lược đồ.

> **Vì sao hai tài khoản, không phải một.** [`../adr/0017-khu-quan-tri-he-thong.md`](../adr/0017-khu-quan-tri-he-thong.md) tách hai vai không chồng lấn: `admin` mang `has_permission_bypass`, thuộc đơn vị `DEV`, thấy mọi thứ **trong đơn vị mình**; `superadmin` mang `is_system_operator`, thuộc đơn vị hệ thống, thấy **danh sách** đơn vị nhưng không thấy dữ liệu bên trong đơn vị nào. Hai cờ loại trừ nhau bằng ràng buộc ở database ([`schema-core.md`](schema-core.md) §4.1, luật M11).
>
> Tên `superadmin` là **dữ liệu**, không phải vai trò. Luật **S10** cấm mọi đoạn mã rẽ nhánh theo chuỗi tên đăng nhập — nên cái tên không bao giờ thành một `if`.

> **Gộp bước 3–7 thành một lệnh** bằng một script bọc (`seed-dev.ps1` / `seed-dev.sh`). Script đó gọi `psql` và `dotnet`, và **không chứa bí mật nào** — mật khẩu vẫn nằm ở `user-secrets` của từng máy.

**PASS của từng bước:**

| Bước | Kiểm | Mong đợi |
| --- | --- | --- |
| 3 | `psql -d coreandskill_dev -c '\dt core.*'` | Liệt kê được `core.app_user` |
| 3 | Các câu kiểm ở [`schema-core.md`](schema-core.md) §11 | Đúng như "mong đợi" ghi trong từng câu |
| 4 | `SELECT count(*) FROM core.permission` | Khác 0 |
| 5 | `SELECT code, is_system FROM core.tenant` | Đúng **hai** dòng: đơn vị hệ thống (`is_system = true`) và `DEV` |
| 5 | `SELECT count(*) FROM core.role_permission` | Khác 0 — thiếu ánh xạ thì vai trò có tên mà không có quyền nào |
| 7 | Đăng nhập `admin` | Thành công, và `mustChangePassword` là `true` |
| 7 | Đăng nhập `superadmin` | Thành công, vào được khu quản trị hệ thống, **không** thấy dữ liệu nghiệp vụ của đơn vị `DEV` |

> **Vì sao bước 6–7 tách khỏi bước 3–5, và vì sao không gộp vào script SQL.** Mật khẩu Identity
> băm bằng PBKDF2 với salt ngẫu nhiên — **không có cách nào tạo hash hợp lệ bằng SQL thuần**
> ([`schema-core.md`](schema-core.md) §4.1). Lệnh `--seed` gọi `UserManager.CreateAsync` thật.
>
> Lệnh này **chạy một lần rồi thoát, không mở cổng**. Tiến trình API phục vụ thật **không seed
> gì cả** — đó là chủ đích: seed là thao tác của người vận hành, không phải tác dụng phụ của
> việc khởi động.
>
> Thiếu secret ở bước 6, lệnh `--seed` **cố ý dừng và không ghi dòng nào**. Sinh một mật khẩu
> mặc định để "cho tiện" là cách nhanh nhất để một mật khẩu mặc định đi thẳng lên production.

**Dựng lại từ đầu khi lỡ tay:** `dropdb coreandskill_dev` rồi làm lại từ bước 2. Trên máy dev đó
là thao tác rẻ nhất — đừng cố sửa một database dev đã lệch.

---

## 7. Checklist trước khi chạy script trên PRODUCTION

Chạy theo thứ tự. **Không bỏ bước nào**, kể cả với script "chỉ thêm một index".

### 7.1 Trước — chuẩn bị

| ☐ | Việc | PASS |
| --- | --- | --- |
| ☐ | **Backup database**, và **kiểm tra khôi phục được** trên một database khác | Khôi phục xong, đếm được số dòng của một bảng lớn |
| ☐ | Đọc **toàn bộ** script sắp chạy, từ dòng đầu tới dòng cuối | Không có `DROP`/`TRUNCATE`/`DELETE` ngoài dự kiến |
| ☐ | Chạy script trên **bản sao production** trước | Không lỗi; đo được thời gian chạy |
| ☐ | Xác nhận đây là bước nào trong quy trình bốn bước §5 [`migration-policy.md`](migration-policy.md) | Nếu là bước 4 (thu hẹp): đã qua ít nhất một chu kỳ phát hành từ bước 3 |
| ☐ | Đối chiếu script còn thiếu bằng lệnh ở §3.4 | Danh sách khớp với thứ định chạy |
| ☐ | Ước lượng thời gian khoá bảng | Có `ALTER COLUMN … TYPE` hoặc `SET NOT NULL` trên bảng lớn ⇒ cần cửa sổ bảo trì |
| ☐ | Xác nhận connection string trỏ đúng database | `SELECT current_database(), inet_server_addr();` |

> Dòng cuối không phải thủ tục thừa. Chạy nhầm script lên nhầm môi trường là loại sự cố xảy ra
> với người có kinh nghiệm nhất, vì nó không đòi hỏi sai sót kỹ thuật nào — chỉ cần một cửa sổ
> terminal còn mở từ hôm trước.

### 7.2 Trong khi chạy

| ☐ | Việc |
| --- | --- |
| ☐ | Chạy **từng file một**, `-v ON_ERROR_STOP=1`, đọc kết quả từng file trước khi sang file kế |
| ☐ | Lưu toàn bộ output ra file: `psql … -f script.sql 2>&1 \| tee logs/apply-$(date +%F-%H%M).log` |
| ☐ | Có lỗi ⇒ **DỪNG**. Không chạy file tiếp theo, không "thử lại xem sao" |

### 7.3 Sau — nghiệm thu

| ☐ | Việc | PASS |
| --- | --- | --- |
| ☐ | `SELECT script_name, applied_at FROM core.schema_script_history ORDER BY id DESC LIMIT 5` | Có dòng của script vừa chạy |
| ☐ | Các câu kiểm ở [`schema-core.md`](schema-core.md) §11 | Đúng "mong đợi" của từng câu |
| ☐ | Khởi động lại app | Lên được — tức phép kiểm §5.1 đã qua |
| ☐ | Gọi thử một endpoint chạm bảng vừa đổi | Trả đúng, không 500 |
| ☐ | Đọc log 15 phút đầu | Không có cảnh báo *"Database di TRUOC code"* ngoài dự kiến |

### 7.4 Ba thứ TUYỆT ĐỐI không làm trên production

| Không làm | Vì sao |
| --- | --- |
| Sửa script cho khớp lỗi rồi chạy tiếp | Script trong repo và thứ đã chạy trên DB sẽ khác nhau, và checksum ở §3.2 sẽ tố cáo điều đó sau — nhưng lúc đó đã muộn |
| Chạy `dotnet ef database update` | Bị chặn bằng máy, và đi ngược §0. Nó cũng bỏ qua bước ghi `schema_script_history` |
| Chạy một script từ nhánh chưa merge | Nó có thể bị đổi số thứ tự lúc merge (§2.1), và khi đó database mang một tên script không tồn tại trong repo |
