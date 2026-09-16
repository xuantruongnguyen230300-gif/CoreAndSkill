---
kind: tham-chieu
scope: core
verified: chua-doi-chieu
---

# Lý do, bẫy, ví dụ mở rộng của `schema-core.md`

> File này giữ **lý do, bẫy, ví dụ dài** của [`docs/database/schema-core.md`](../../../database/schema-core.md); luật, bảng cột, index, ràng buộc và câu kiểm ở đó. Số mục dưới đây trùng số mục của file luật — mục không có gì dời thì ghi "—".

---

## 1. Phạm vi và ranh giới

`public` rỗng là một quy ước có ích: nhìn cây schema trong pgAdmin/DBeaver là biết ngay bảng
nào thuộc nền tảng, bảng nào thuộc nghiệp vụ, không phải đọc code. Rẻ khi làm từ lúc DB còn
trống, đắt khi tách sau.

### 1.1 Cấm foreign key vật lý xuyên schema

**Cái giá phải trả, nói thẳng:** database **không còn** đảm bảo `nguoi_tao_id` trỏ tới một user
có thật. Xoá một user để lại id mồ côi trong bảng module, và không có lỗi nào bật ra. Đổi lại,
mỗi module tách thành service độc lập được mà **không phải sửa schema** — một FK xuyên schema là
sợi dây trói vĩnh viễn, gỡ nó trên dữ liệu thật là việc đắt nhất trong vòng đời hệ thống.

Bù lại phần mất: user **không bị xoá cứng** ([`schema-core.md`](../../../database/schema-core.md) §1.2), nên mọi id người dùng mà module đang giữ vẫn
tra ngược được. Id mồ côi trên thực tế chỉ xuất hiện khi có người `DELETE` bằng SQL tay.

### 1.2 Người dùng không bị xoá cứng

—

### 1.3 Ranh giới tenant — `core.tenant` và danh sách miễn trừ

**Vì sao `Tenant` không kế thừa `BaseEntity`** — cùng khuôn với `AppUser` (§4.1): `BaseEntity` mang theo `is_deleted`, mà **tenant không bao giờ được xoá**
([`17-multi-tenant.md`](../17-multi-tenant.md) §10). Cho nó một
cờ xoá mềm là mở đúng cánh cửa vừa đóng: một dòng `is_deleted = true` làm cả đơn vị biến mất
khỏi mọi màn hình trong khi dữ liệu của họ vẫn nằm nguyên trong mọi bảng — và hai cơ chế vô
hiệu hoá song song (`is_deleted` và `is_active`) thì ngày chúng bất đồng không cơ chế nào sai
rõ ràng để mà sửa.

> 🪤 **`is_active` của `tenant` KHÔNG liên quan gì tới hậu tố `_active` của tên index** (§2.3).
> Hậu tố đó là hợp đồng *"index này có `WHERE is_deleted = false`"*. Bảng `tenant` không có
> `is_deleted`, nên khoá duy nhất của nó mang tiền tố `uq_`, không phải `ux_…_active`.

Vì sao `code` chuẩn hoá về chữ HOA ở tầng ứng dụng trước khi ghi và trước khi tra: cùng lý do Identity chuẩn hoá tên đăng nhập.

**Vì sao miễn trừ `core.setting` kéo theo một nghĩa vụ, không phải chỉ một ngoại lệ.** Cảnh báo ở §3.7 vẫn
đúng nguyên: bộ lọc `tenant_id = @tenant` **giấu** dòng NULL khỏi mọi người. Nếu đường đọc cấu hình chỉ dựa vào bộ lọc mặc định thì
tầng hệ thống biến mất và mọi khoá rơi thẳng xuống mặc định trong code, im lặng.

**Vì sao danh sách miễn trừ phải NGẮN.** Một danh sách miễn trừ dài là dấu hiệu ranh giới tenant đang bị
hiểu sai — thường là ai đó gặp một bảng khó gắn `tenant_id` rồi miễn trừ nó thay vì hỏi tại
sao nó khó. Thêm một dòng vào bảng là một quyết định kiến trúc: nó phải nêu lý do **dữ
liệu này có ý nghĩa như nhau với mọi đơn vị**, không phải lý do *"thêm cột vào đây phiền"*.

## 2. Quy ước đặt tên — bắt buộc

### 2.1 Bảng và cột

**Vì sao snake_case chứ không PascalCase như dự án tiền nhiệm.** PostgreSQL hạ mọi định danh
không trích dẫn về chữ thường. Đặt tên `"AspNetUsers"` buộc **mọi** câu SQL viết tay phải trích
dẫn kép — quên một dấu nháy là `relation "aspnetusers" does not exist`, một lỗi vừa khó đọc vừa
chỉ lộ ra lúc chạy. Ở dự án tiền nhiệm, mọi script vận hành đều phải viết `core."RolePermissions"`.
snake_case gỡ hẳn lớp phiền đó: `SELECT * FROM core.role_permission` chạy đúng như đã gõ.

**Vì sao số ít.** Một dòng là một thực thể; `app_user` đọc là *"bảng của thực thể app_user"*.
Số ít cũng tránh phải quyết định số nhiều bất quy tắc (`person`/`people`, `status`/`statuses`) —
quyết định mà mỗi người sẽ tự trả lời khác nhau.

Vì sao đổi tên bảy bảng Identity là bắt buộc, không phải tuỳ chọn: bỏ qua
nó thì bảy bảng Identity trở thành ngoại lệ duy nhất của quy ước, và ngoại lệ duy nhất là thứ
không ai nhớ.

Vì sao ánh xạ bằng naming convention áp cho toàn model, không `HasColumnName(...)` gõ tay ở từng property: cách đó là một danh sách sẽ thiếu.

Về bẫy SQL thô trong EF configuration: dòng `HasFilter("\"IsDeleted\" = false")` là khuôn đúng cho một model **không** đổi tên cột; ở repo này nó sinh SQL trỏ vào một
cột không tồn tại. Migration sẽ đỏ ngay lúc áp, nên bẫy này ồn ào — nhưng vẫn tốn một vòng nếu
chép mẫu từ nơi khác mà không đọc.

### 2.2 Ràng buộc và index

Vì sao phải kiểm độ dài tên trước: PostgreSQL cắt định danh ở 63 byte **âm thầm** — không lỗi, không cảnh báo, tên bị
cụt. Hai index dài mà 63 byte đầu trùng nhau sẽ va vào nhau với thông báo *"relation already
exists"* trỏ vào một cái tên bạn chưa từng gõ.

### 2.3 Hậu tố `_active` là một hợp đồng, không phải trang trí

—

## 3. Quy ước cột — áp cho mọi bảng `core`

### 3.1 Khoá chính: `uuid`, ứng dụng tự sinh, UUID v7

Ba quyết định gói trong một dòng, và lý do từng cái:

| Quyết định | Vì sao |
| --- | --- |
| Kiểu `uuid`, không `bigserial` | Id sinh được ở phía ứng dụng **trước khi** chạm DB, nên gộp nhiều insert vào một transaction mà vẫn nối được quan hệ cha–con. Cũng là điều kiện để tách module thành service mà không phải điều phối dãy số |
| **Không** `DEFAULT gen_random_uuid()` | Ứng dụng tự sinh (`Guid.CreateVersion7()`). Để DB sinh thì EF phải đọc ngược giá trị sau insert, và entity con thêm vào một collection đã tracked bị EF hiểu nhầm là "đã tồn tại" |
| **UUID v7**, không v4 | v7 có tiền tố thời gian nên **tăng dần**. v4 ngẫu nhiên hoàn toàn làm mỗi insert rơi vào một trang B-tree khác nhau — index phình, cache miss cao, và chi phí đó không bao giờ giảm |

Vì sao hai ngoại lệ không được "sửa cho đồng bộ": `app_user_claim`, `app_role_claim` giữ `integer` identity vì đó là schema chuẩn của Identity — sửa nó không mua được gì và làm bản nâng cấp Identity sau này khó hơn; `schema_script_history` dùng `bigint` identity vì là bảng vận hành, cần **thứ tự áp dụng** đọc được bằng mắt — một id tăng dần trả lời thẳng câu *"script nào chạy trước"*.

### 3.2 Năm cột audit

Vì sao `created_by` giữ **tên đăng nhập**, không phải id: để đọc log không phải join.

**Vì sao `updated_*` được điền ngay lúc tạo.** Để `NULL` thì mọi chỗ hiển thị *"sửa lần cuối"*
phải tự viết `updated_at ?? created_at`, và mọi câu `ORDER BY updated_at DESC` đẩy bản ghi mới
tạo xuống cuối. Bù lại, phép thử *"bản ghi này đã từng bị sửa chưa"* không còn là `updated_at IS
NOT NULL` mà là `updated_at <> created_at` — nên interceptor phải gán **cùng một** giá trị thời
gian cho cả hai, không phải gọi đồng hồ hai lần cách nhau vài mili giây. Xoá mềm cũng là `Modified`, nên hai cột này trả lời luôn *"ai xoá, lúc nào"*.

Vì sao `is_deleted` là `NOT NULL DEFAULT false`: cột nullable làm mọi query filter phải xử lý ba trạng thái thay vì hai.

**Vì sao KHÔNG có `deleted_at` / `deleted_by`.** Chúng trả lời đúng câu mà `updated_at`/`updated_by`
đã trả lời tại thời điểm xoá mềm, vì xoá mềm là một lần `UPDATE`. Thêm hai cột nữa là thêm hai
cột phải giữ đồng bộ và hai cột có thể lệch. Dự án nào thật sự cần tách *"lần sửa cuối"* khỏi
*"lần xoá"* thì thêm — nhưng phải nới detector E1 lên bảy tên **cùng lượt**, nếu không cổng đỏ
ở một chỗ không liên quan gì tới thay đổi vừa làm.

Lý do từng bảng không mang khối audit: bảy bảng Identity tự quản vòng đời (`lockout_end`, `security_stamp`); `code_sequence` — mã đã phát ra ngoài không thu hồi được, nên một bộ đếm không có trạng thái *"đã xoá"* nào mang nghĩa, và mang cờ đó thì bộ lọc xoá mềm chen vào câu cập nhật-và-trả-về; `outbox_message` — hàng đợi vận hành, dòng bị dọn theo chính sách lưu trữ; `audit_log` — chỉ ghi thêm, sửa hay xoá đều là phá bằng chứng; `schema_script_history` — xoá mềm một dòng nhật ký là tự nói dối.

### 3.3 🪤 Soft delete phá unique index — bẫy thật, đọc kỹ

**Đây là cái bẫy đắt nhất của cả file luật.** Kịch bản hỏng với unique index viết theo thói quen, đủ ba bước:

1. Tạo menu `code = 'quan-tri'`. Thành công.
2. Xoá mềm nó — `UPDATE core.menu_item SET is_deleted = true WHERE code = 'quan-tri'`.
   Dòng **vẫn nằm trong bảng**.
3. Tạo lại menu `code = 'quan-tri'` → `23505 duplicate key value violates unique constraint`.

Người dùng thấy: *"Tôi vừa xoá nó xong mà, sao bảo trùng?"* — và không cách nào tự gỡ, vì thứ
đang chiếm khoá là một dòng không màn hình nào hiển thị.

Ba hệ quả ở file luật, mỗi cái đều từng làm hỏng việc ở dự án tiền nhiệm:

1. PK ghép cấm đúng điều ta cần cho phép: **cấp lại một quyền đã từng thu hồi**.
2. Postgres chỉ nhận index một phần làm đích của `ON CONFLICT` khi mệnh đề `WHERE` khớp đúng. Thiếu nó, câu lệnh **abort** với *"there is no unique or
   exclusion constraint matching the ON CONFLICT specification"* — một script tự nhận là
   "idempotent" chết trước khi ghi được dòng nào.
3. Nếu token phiên bản ma trận quyền tính cả dòng đã xoá mềm, nó đổi
   giá trị sau mỗi lần lưu kể cả khi trạng thái nhìn thấy được không đổi — và hai người lưu
   **tuần tự** sẽ nhận 409 sai.

Vì sao cần cả hai phép thử trong integration test: một mình ca "xoá mềm rồi tạo lại phải thành công" không đủ — gỡ hẳn unique index cũng làm nó xanh.

### 3.4 Thời gian: `timestamptz`, không bao giờ `timestamp`

`timestamp without time zone` lưu một con số **không có nghĩa** nếu không biết nó thuộc múi giờ
nào. Hai tiến trình chạy khác `TimeZone` ghi cùng một khoảnh khắc thành hai giá trị khác nhau,
và không có cách nào phát hiện sau đó. `timestamptz` chuẩn hoá về UTC lúc ghi, đổi về múi giờ
phiên lúc đọc — đây là kiểu duy nhất đúng cho một hệ có thể chạy nhiều instance, hoặc có người
dùng ở nhiều múi giờ.

### 3.5 `text` hay `varchar(n)`

PostgreSQL lưu hai kiểu này **giống hệt nhau**; `varchar(n)` chỉ thêm một phép kiểm độ dài.
Không có chênh lệch hiệu năng — vì vậy chọn theo **ý nghĩa**.

> 🪤 Nới `varchar(50)` → `varchar(100)` là `ALTER TABLE` rẻ (Postgres không rewrite bảng).
> **Siết lại thì không** — phải quét toàn bảng, và sẽ hỏng nếu có dòng vượt giới hạn mới. Đó là lý do chọn
> rộng hơn một bậc so với nhu cầu hôm nay.

### 3.6 Concurrency token — hệ quả trên schema

Vì sao không thêm token thứ hai lên `app_user` / `app_role`: hai token trên cùng một dòng nghĩa là hai lớp code cùng tin mình đang giữ quyền quyết định, và ca chúng bất đồng không có đường xử lý đúng.

### 3.7 Cột `tenant_id` — áp cho mọi bảng dữ liệu tenant

Lý do từng quyết định:

| Quyết định | Vì sao |
| --- | --- |
| `NOT NULL`, không mặc định | Cột nullable nghĩa là có trạng thái *"dòng không thuộc đơn vị nào"* — và bộ lọc `tenant_id = @tenant` sẽ **giấu** dòng đó khỏi mọi người, kể cả người vừa tạo |
| Ứng dụng điền, **không** `DEFAULT` | Giá trị đến từ claim của phiếu xác thực, DB không biết ai đang gọi. Chi tiết: [`17-multi-tenant.md`](../17-multi-tenant.md) §2 |
| Không bao giờ `UPDATE` | Một bản ghi không đổi chủ |
| FK `ON DELETE RESTRICT` **trong schema `core`** | Tenant không xoá được, và `RESTRICT` biến điều đó thành một ràng buộc DB thay vì một lời hứa |
| Bảng của module: id trần, KHÔNG có FK | FK sẽ vượt ranh giới schema — vi phạm luật E5 (§1.1). Cùng quy tắc đã áp cho `nguoi_tao_id` |

Vì sao `tenant_id` đứng đầu mọi index đa cột: mọi truy vấn đều lọc theo tenant trước. Ngoại lệ `outbox_message` (§8): bộ phát chạy xuyên tenant nên lọc theo `next_attempt_at` trước.

Vì sao mọi index duy nhất trên bảng có `tenant_id` phải gồm `tenant_id` (luật M3): thiếu nó thì đơn vị B không tạo được bản
ghi mang mã mà đơn vị A đã dùng, **và không tìm thấy bản ghi đang chiếm khoá** vì bộ lọc đã
giấu nó đi. Triệu chứng người dùng báo: *"hệ thống nói mã trùng nhưng tôi tìm không thấy"*.

## 4. Nhóm Identity — bảy bảng

### 4.0 Bảng đổi tên — bắt buộc khai tường minh

Vì sao không đổi tên ba index Identity tự đặt (`UserNameIndex`, `EmailIndex`, `RoleNameIndex`): chúng là tên do Identity khai trong model; đổi chúng thêm một điểm phải đồng bộ tay mỗi lần
nâng cấp Identity, đổi lại được đúng sự nhất quán về hình thức. Ghi ngoại lệ ở file luật để lần sau
không ai "dọn" chúng rồi phá một bản nâng cấp.

Chỗ dễ đọc lướt nhất: tên ba index đứng yên nhưng **tập cột đổi** (gồm `tenant_id`) — giữ tên không có nghĩa là giữ định nghĩa cũ.

### 4.1 `core.app_user`

Vì sao `phone_number` nới kiểu so với cột gốc của Identity: trần 20 ký tự là luật nghiệp vụ FE cũng phải biết (`maxlength` của ô nhập, [`profile.md`](../../../contracts/profile.md) §2), nên §3.5 buộc dùng `varchar(n)` chứ không `text`.

Vì sao có `locked_by_admin`: phân biệt hai đường khoá trên màn quản trị — `lockout_end` một mình không nói được khoá do quản trị đặt tay hay khoá tự động sau nhiều lần sai.

Tên đăng nhập và email không còn duy nhất toàn hệ — hai đơn vị hoàn toàn có thể có cùng
một `admin`, cùng một `vanthu@…` — là thay đổi
có ảnh hưởng lan rộng nhất của cả mô hình multi-tenant:

| Hệ quả | Ở đâu |
| --- | --- |
| `UserManager.FindByNameAsync` không còn định danh được một người nếu chưa biết tenant | Luồng đăng nhập phải nạp tenant **trước** khi gọi Identity |
| Form đăng nhập cần thêm **ô mã đơn vị** | [`17-multi-tenant.md`](../17-multi-tenant.md) §11 |
| `EmailIndex` chuyển từ **không** duy nhất sang duy nhất theo cặp | Chỉ đúng khi tuỳ chọn *"email phải duy nhất"* của Identity đang được dùng |

Chi tiết luồng đăng nhập và phương án đã loại:
[`17-multi-tenant.md`](../17-multi-tenant.md) §11 ·
[`02-identity-auth.md`](../02-identity-auth.md) §6.2.

**Vì sao `AppUser` không kế thừa `BaseEntity`.** Identity quản vòng đời bằng field
riêng. Ép nó kế thừa `BaseEntity` để "cho đồng bộ" tạo ra hai cơ chế vô hiệu hoá song song
(`is_deleted` và `lockout_end`), và ngày chúng bất đồng thì không cơ chế nào sai rõ ràng để mà
sửa: một user `is_deleted = true` nhưng `lockout_end IS NULL` đăng nhập được hay không?

Vì sao không ghi `lockout_end` bằng SQL tay: thao tác khoá còn phải đổi `security_stamp` tường minh trong cùng thao tác — bỏ qua bước đó nghĩa
là phiên đang chạy của người vừa bị khoá vẫn sống bình thường.

Vì sao ràng buộc "hai cờ đặc quyền loại trừ nhau" đặt ở database (luật M11), không ở test: nó đúng cả với dòng ghi bằng SQL tay, thứ test không canh được.

Vì sao tài khoản đầu tiên phải tạo qua `UserManager.CreateAsync`: `PasswordHasher<TUser>` dùng PBKDF2 với
salt ngẫu nhiên mỗi lần — không có cách nào tạo mật khẩu hợp lệ bằng SQL. Đây là lý do runbook tạo tài khoản bằng lệnh bootstrap
chạy từ binary chứ không bằng `psql`: [`script-runbook.md`](../../../database/script-runbook.md) §3.3.

### 4.2 `core.app_role`

**Vai trò thuộc về một đơn vị.** Mỗi tenant dựng bộ vai trò riêng, đặt tên riêng, sửa riêng —
hai đơn vị cùng có một vai trò tên *"Văn thư"* là hai dòng khác nhau, không phải một.

**`is_system` là câu trả lời cho "vai trò quản trị tối cao" mà KHÔNG hardcode role.** Thứ được bảo vệ là **một dòng dữ liệu mang cờ**, không phải một
chuỗi trong `Core.Application`. Dự án thứ hai đặt tên vai trò đó là gì cũng được — Core không
cần biết.

### 4.3 Năm bảng Identity còn lại

**Cái giá của việc thêm `tenant_id` vào năm bảng này, nói thẳng:** chúng do Identity sinh, và thêm một cột vào
chúng nghĩa là phải **khai lớp con** cho từng kiểu join (`AppUserRole : IdentityUserRole<Guid>`
và bốn kiểu tương tự), rồi chuyển `IdentityDbContext` sang dạng khai đủ kiểu. Đó là công thật,
và nó không mua thêm gì cho `app_user_role` — `user_id` đã trỏ vào một dòng đã lọc theo tenant.

**Vẫn làm, vì hai lý do.** Thứ nhất, `app_user_login` có khoá chính **toàn cục**
(`login_provider`, `provider_key`); không có `tenant_id` trong đó thì ngày bật SSO, hai đơn vị
dùng chung một nhà cung cấp sẽ va khoá nhau — và triệu chứng sẽ là *"không đăng nhập được"* ở
một đơn vị mà không ai nghĩ tới nguyên nhân. Thứ hai, một bảng mà cách ly **suy ra từ việc luôn
có người join tới bảng cha** là đúng loại quy ước hỏng khi ai đó viết một câu SQL tay (luật M6).
Miễn trừ chúng sẽ làm danh sách ở §1.3 dài thêm năm dòng — dấu hiệu chính xác mà §1.3 cảnh báo.

Vì sao ba bảng chưa dùng ở v1 vẫn phải dựng: Identity truy vấn chúng vô điều kiện ở một số luồng, và `app_user_token` được ghi ngay ở luồng
đặt lại mật khẩu. Thiếu bảng thì lỗi bật ra ở một luồng không ai ngờ tới, rất xa nguyên nhân.

Vì sao `app_user_role` dùng PK ghép: đây là bảng do Identity sở hữu, `UserManager` tra theo cặp; nó cũng không mang `is_deleted` nên bẫy §3.3 không áp dụng.

## 5. Nhóm phân quyền — ba bảng

### 5.1 `core.permission_resource` — danh mục tài nguyên

**Vì sao deny-by-default cần bảng này.** Không cấp quyền = không có dòng trong `role_permission`.
Nếu màn hình dựng danh sách hàng **từ `role_permission`**, tài nguyên chưa ai được cấp sẽ biến
mất khỏi UI — và không còn đường nào cấp nó nữa. Ở dự án tiền nhiệm, danh mục này là một lớp
hằng số trong code, phải sửa code mới thêm được tài nguyên. Chuyển nó thành bảng cho phép module
tự đóng góp tài nguyên mà không phải mổ vào Core.

### 5.2 `core.permission` — danh mục quyền

Vì sao `core.role.*` có mặt: vai trò là **dữ liệu** tạo/sửa/xoá được từ màn quản trị (§4.2) — thiếu
cặp khoá này thì màn đó không có gì để kiểm.

Lý do của ba ràng buộc khi thêm khoá:

1. Đổi tên một khoá đã phát hành làm dữ liệu phân quyền cũ trỏ vào hư không, và
   nó hỏng **im lặng**: người dùng chỉ mất quyền, không có lỗi nào bật ra.
2. Vai trò là dữ liệu; danh mục là hợp đồng giữa code và
   dữ liệu ([`adr/0005-permission-based.md`](../../../adr/0005-permission-based.md)).
3. `lock`, `reset-password`, `role.assign` tách khỏi `write` để cấp được một vai trò "trực hỗ trợ" chỉ đặt lại được mật
   khẩu mà không đụng được vai trò của ai. Gộp chúng thì mọi người sửa được email đều gán được
   vai trò.

**Vì sao FK trỏ vào `permission_resource.key` buộc một ngoại lệ của §3.3.** Foreign key cần một unique constraint trên cột đích, mà Postgres **không** nhận index **một
phần** làm đích của foreign key. Nếu `permission_resource.key` chỉ có
`ux_permission_resource_key_active`, câu `ADD FOREIGN KEY` thất bại với *"there is no unique
constraint matching given keys"*.

Hai đường đã cân nhắc:

1. Dùng `uq_permission_resource_key UNIQUE (key)` — unique **đầy đủ**. Hệ quả: một tài nguyên
   đã xoá mềm vẫn giữ khoá, không tạo lại được cùng `key`.
2. Bỏ FK, kiểm toàn vẹn ở tầng ứng dụng.

Chốt đường 1 vì `permission_resource.key` là định danh kỹ thuật do lập trình viên đặt, gần
như không bao giờ xoá rồi dựng lại bằng tay — trả cái giá đó rẻ hơn mất ràng buộc.

### 5.3 `core.role_permission` — cấp quyền

**Đây là bảng thể hiện rõ nhất ranh giới tenant chạy ở đâu.** `permission` là danh mục dùng
chung; `app_role` thuộc về một đơn vị; và **ô ma trận nối hai thứ đó thuộc về đơn vị**. Hai
đơn vị cấp cùng một quyền cho hai vai trò cùng tên là hai dòng độc lập, không đụng nhau.

**Vì sao `ix_role_permission_tenant_perm_role` là bắt buộc, không phải tối ưu sớm.**
Truy vấn nóng nhất hệ thống — kiểm quyền, chạy trên **mọi** request có `[RequirePermission]` —
lọc theo tenant (bộ lọc toàn cục tự thêm), rồi theo `permission_id`, rồi mới ghép `role_id`
của người gọi. PK là khoá thay thế nên không seek được cho truy vấn nào; unique index nghiệp
vụ thì dẫn đầu bằng `tenant_id, role_id`, sai cột thứ hai. Index này dẫn đúng thứ tự cột được
lọc và phủ luôn cột join → index-only scan.

> 📌 **Đọc `EXPLAIN` cho đúng ở bảng nhỏ.** Với vài chục dòng, Postgres vẫn chọn Seq Scan vì cả
> bảng nằm gọn trong một page. Đó là lựa chọn **đúng** của planner, không phải bằng chứng index
> vô dụng. Giá trị của index xuất hiện khi số tổ hợp (vai trò × quyền) tăng; chi phí duy trì gần
> bằng 0 vì bảng này ghi rất hiếm. Đừng gỡ index vì `EXPLAIN` trên dữ liệu seed không dùng tới nó.

Vì sao có bất biến chống tự khoá cửa: không có nó, một lần lưu ma trận sai làm **không ai** còn sửa được
ma trận nữa — và đường sửa duy nhất còn lại là `UPDATE` bằng SQL tay trên production.

Bất biến này kiểm bằng một truy vấn trên **đúng các dòng đang ghi**, không cần đếm toàn bảng, nên
nó không dính bài toán tranh chấp giữa hai request đồng thời. Đây là điểm khác với luật *"không
được hạ vai trò quản trị cuối cùng"* — luật đó phải đếm toàn bảng mỗi lần ghi và vẫn thua một ca
race, nên **đã loại**.

## 6. Nhóm menu — hai bảng

### 6.1 `core.menu_item`

**Menu thuộc về đơn vị.** Mỗi cơ quan chỉnh cây điều hướng của mình mà không đụng cơ quan khác;
`code` duy nhất **trong một đơn vị**, nên hai đơn vị cùng có mục `code = 'quan-tri'` là bình
thường. `required_permission_id` vẫn trỏ vào danh mục quyền dùng chung (§5.2).

Vì sao `label_key` là khoá i18n chứ không phải nhãn: dự án tiền nhiệm lưu thẳng câu tiếng Việt vào cột
`Name`; khi thêm cơ chế đổi ngôn ngữ ngay trong app, mọi nhãn menu trở thành chuỗi **không dịch
được** — nó đã cố định trước khi người dùng chọn ngôn ngữ, và không có gì ở FE tra ngược lại
được.

Vì sao "cây đúng một cấp" kiểm ở handler chứ không bằng constraint hay trigger: `CHECK` không nhìn được
sang dòng khác; trigger là logic nghiệp vụ nằm ngoài tầm mọi test C# và ngoài tầm mọi lượt review code. Vì sao `code` không đổi sau khi đã dùng: nó
là khoá `track` phía FE và là điểm neo của mọi tham chiếu menu.

### 6.2 `core.menu_item_role`

—

### 6.3 Thứ tự quyết định mục menu có hiện hay không

Vì sao bước 1 thắng tuyệt đối: nếu hai cơ chế cùng có hiệu lực — giao hoặc
hợp — sẽ có ca một mục ẩn đi mà không ai chỉ ra được dòng dữ liệu nào gây ra, vì cả hai đều
"đúng một nửa". Ưu tiên cứng cho permission cũng là hướng hợp với luật S2.

Vì sao mục cha luôn được kéo vào: thiếu dòng cha trong response, FE dựng cây từ `parent_id` sẽ coi con là mục gốc và
đặt nó sai chỗ trên sidebar.

## 7. `core.notification` và `core.notification_recipient`

### 7.1 `core.notification` — nội dung

Vì sao `severity` và `link_route` vẫn có mặt dù chưa dùng ở v1: để mức độ và đường dẫn đích không phải thêm bằng một
migration đổi hình dạng bảng về sau.

Vì sao `code` + `params` thay vì một cột `message` chứa câu hoàn chỉnh: cùng lý do như `label_key` ở
§6.1. Vì sao tham số truyền theo TÊN (luật R7): `params` dạng
`{"UserName": "an.nguyen"}` dịch được sang mọi ngôn ngữ; `params` dạng `["an.nguyen"]` thì không,
vì vị trí chỗ trống trong câu đổi theo ngôn ngữ.

### 7.2 `core.notification_recipient` — ai nhận, đã đọc chưa

Vì sao `ix_notification_recipient_unread` là index một phần: chuông thông báo hỏi *"người này còn mấy tin chưa đọc"* trên **mọi** lần tải trang. Index một
phần chỉ chứa dòng chưa đọc, nên nó nhỏ hơn hẳn bảng và **không phình theo lịch sử đã đọc** —
đúng đặc tính cần cho một bảng chỉ tăng.

Vì sao tên unique rút gọn thành `notif_user`: tên đầy đủ chạm ngưỡng 63 byte của §2.2.
Thêm `tenant` vào giữa vẫn nằm dưới ngưỡng; phần `tenant` mới là thứ người đọc `\di core.*` cần thấy, nên không bao giờ rút gọn phần đó.

## 8. `core.outbox_message` — outbox cho integration event

**Vì sao có `tenant_id` trên outbox — "phát event này trong ngữ cảnh của ai".** Bộ phát
chạy như một job nền, ngoài mọi request, nên nó **bỏ bộ lọc tenant** — và lời gọi đó nằm trong
allowlist của luật M5 ([`17-multi-tenant.md`](../17-multi-tenant.md)
§7). Nhưng khi giao event cho handler, nó phải nạp lại `tenant_id` của **chính dòng đó** vào
ngữ cảnh; không có cột này thì không có cách nào làm đúng, và handler sẽ chạy với một tenant
tuỳ tiện.

Vì sao `ix_outbox_message_pending` dẫn đầu bằng `next_attempt_at` chứ không `tenant_id`: truy vấn duy nhất chạm nó lọc
xuyên tenant. Thêm `tenant_id` vào đầu ở đây làm index vô dụng cho đúng truy vấn nó sinh ra để
phục vụ.

Vì sao không xoá mềm: nó là hàng đợi vận hành, không phải dữ liệu nghiệp vụ. Xoá
mềm một dòng outbox không có nghĩa gì.

**Vì sao index là index MỘT PHẦN trên `status = 'pending'`.** Bảng này chỉ tăng, và tuyệt
đại đa số dòng là đã xử lý. Index đầy đủ sẽ lớn dần vô hạn trong khi truy vấn duy nhất chạm
tới nó chỉ quan tâm phần chờ phát — phần gần như luôn nhỏ. Đây là ca sách giáo khoa của
partial index, và bỏ mệnh đề `WHERE` là một trong những cách rẻ nhất để làm chậm hệ thống sau
sáu tháng chạy.

**Vì sao `payload` là `jsonb`, không `text`.** `jsonb` kiểm cú pháp lúc ghi, nên một payload hỏng bị
chặn tại nguồn thay vì nổ ở tiến trình dispatcher lúc 2 giờ sáng, cách xa nơi sinh ra nó.

Vì sao có `trace_id`: không có nó, một event lỗi là một dòng không truy ngược được về thao tác nào —
[`07-observability.md`](../07-observability.md).

Vì sao `triggered_by_user_name` cùng kiểu với `created_by` (§3.2): cùng giữ tên đăng nhập.

## 9. Bảng lịch sử và bảng hạ tầng

### 9.1 `core.schema_script_history` — script nào đã chạy

Vì sao có `checksum_sha256`: nó bắt được ca **sửa một script đã áp** — người
sửa tin rằng cả hệ đã có thay đổi, trong khi mọi database đã chạy bản cũ thì không, và không có
gì báo.

### 9.2 `core.__ef_migrations_history`

Vai của nó khác hẳn `schema_script_history`: nó nói *"EF tin rằng migration nào đã áp"*, và được
ghi bởi chính script sinh từ `dotnet ef migrations script --idempotent`. Hai bảng, hai câu
hỏi — đừng gộp, và đừng để một bên suy ra bên kia.

Vì sao đổi tên sang snake_case: để không phải trích dẫn kép khi tra tay.

### 9.3 `core.data_protection_key` — kho khoá bảo vệ dữ liệu

Phiên đăng nhập dùng **cookie** mã hoá bằng ASP.NET Core Data Protection. Mặc định, khoá bảo vệ
nằm trên đĩa của từng máy. Chạy **nhiều instance** thì instance A không giải mã được cookie do B
phát ra, và triệu chứng nhìn thấy là *"đang dùng thì bị đăng xuất ngẫu nhiên"* — một triệu chứng
gần như không ai truy ra nguyên nhân, vì nó không giống lỗi cấu hình chút nào. Cách xử lý chuẩn là lưu key ring vào DB (`PersistKeysToDbContext`).

Vì sao dựng bảng này ngay từ đầu kể cả khi v1 chỉ chạy một instance — không phải để sẵn sàng cho nhiều instance: khoá để trên đĩa cục bộ **mất khi
container được tạo lại**, nên ngay một instance cũng đá toàn bộ người dùng ra mỗi lần triển
khai lại. Xem thêm [`02-identity-auth.md`](../02-identity-auth.md).

Vì sao không mang `tenant_id`: khoá bảo vệ dữ liệu
thuộc về **bản cài**, không thuộc đơn vị nào: mọi tenant dùng chung tiến trình, nên phiếu xác
thực của mọi tenant đều được ký bằng cùng một key ring. Tách key ring theo tenant không thêm
cách ly nào mà chỉ thêm N thứ phải xoay vòng.

### 9.4 `core.audit_log`

—

### 9.5 `core.setting` — cấu hình theo đơn vị

—

### 9.6 `core.code_sequence` — bộ đếm sinh mã nghiệp vụ

—

### 9.7 `core.file` — tệp đính kèm

—

### 9.8 `core.job` — việc chạy nền

—

## 10. Thêm bảng mới — vào schema nào

> 🪤 **Bẫy "schema mặc định" — và vì sao repo này không dính.** Ở dự án tiền nhiệm, mọi entity
> nằm chung **một** `DbContext` với schema mặc định là `core`. Một bảng nghiệp vụ quên khai schema
> rơi vào `core` mà không lỗi gì, và chỉ lộ ra vào đúng ngày người ta thật sự tách Core mang đi.
>
> Ở repo này, **mỗi bên một `DbContext`** ([`migration-policy.md`](../../../database/migration-policy.md) §1.2), và
> mỗi `DbContext` đặt schema mặc định của **chính bên mình**. Entity của module quên khai schema
> rơi vào schema **của module đó**, không phải `core` — tức mặc định luôn nghiêng về câu trả lời
> đúng.
>
> Vẫn còn một ca sai: một entity `core` bị đăng ký nhầm vào `DbContext` của module. Luật E4
> (`EveryMappedEntity_LivesInTheSchemaOfItsSide`) bắt đúng ca đó.

## 11. Kiểm sau khi áp schema

Câu (3) là câu quan trọng nhất: nó bắt đúng cái bẫy §3.3, và bắt được cả khi lỗi do một script
SQL viết tay chứ không do EF sinh — tức đúng loại lỗi mà `dotnet test` không bao giờ thấy.

Vì sao câu (3) và (6) có nhánh chốt tập đầu vào: cả hai lọc theo thuộc tính, nên tập đầu vào của chúng có thể rỗng — một script
tạo bảng mà quên cột `is_deleted` hoặc `tenant_id` làm bộ lọc khớp 0 dòng, và cả hai câu trả về
**rỗng**, trông y hệt "mọi dòng đều đạt". Cùng khuôn phần (c) ở
[`script-runbook.md`](../../../database/script-runbook.md) §3.6, luật T6 ([`RULES.md`](../../../RULES.md) §8).

Câu (4) là bản kiểm chạy trên **database thật**, bổ sung cho ArchTest E5 vốn chỉ đọc model EF.
Hai lưới này bắt hai lớp lỗi khác nhau: E5 bắt người viết entity, câu (4) bắt người viết SQL tay.

**Câu (5) và (6) canh rủi ro nghiêm trọng nhất của toàn hệ: rò dữ liệu giữa hai đơn vị.** Chúng
đứng cạnh ArchTest M3 và M4 chứ không thay thế: ArchTest đọc model EF, hai câu này đọc **database
đã áp**, nên chúng bắt được cả ca một script SQL viết tay tạo bảng hoặc index mà không ai nhìn.

Vì sao danh sách miễn trừ xuất hiện hai lần (bảng §1.3 và câu (5)): một câu SQL không đọc được bảng markdown.
