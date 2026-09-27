---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0042 — `schema-core.md` liệt kê index thiết kế tay, không liệt kê index EF tự sinh từ khoá ngoại

> **Trạng thái:** Đã chấp nhận (2026-09-19)

## Bối cảnh

Một lượt `core-reviewer` phạm vi BE ngày 2026-09-19 báo `docs/database/schema-core.md` liệt kê
hai index cho `core.audit_log` trong khi DDL thật có ba: thiếu `ix_audit_log_actor_tenant_id`.
Reviewer nêu cùng khuôn thiếu ở sáu bảng khác và hỏi: **cố ý hay sót?** — họ tìm trong file
không thấy câu nào khai quy ước đó.

Đối chiếu khi viết ADR này cho ra một dữ kiện mà báo cáo chưa có. Chạy
`grep -rn 'CREATE .*INDEX' database/scripts/core/` rồi so với từng dòng `**Index:**` trong tài
liệu: quy luật **đúng tuyệt đối, không một ngoại lệ nào theo cả hai chiều**.

- Mọi index có mặt trong tài liệu đều là index đa cột, hoặc index một cột trên cột **không phải
  khoá ngoại** (`ix_permission_resource_module_key` trên `module_key`, một cột `varchar` không
  khai FK; `ix_schema_script_history_applied_at`).
- Mọi index vắng mặt trong tài liệu đều là index **một cột trên đúng một cột khoá ngoại** — thứ
  EF Core tự sinh theo quy ước mặc định, không ai gõ ra.

Một quy luật không sai chỗ nào qua ngần ấy bảng thì không phải sót ngẫu nhiên: nó là một quy ước
có thật trong đầu người viết, chưa bao giờ được viết ra. Và vì chưa viết ra, nó đã tiêu tốn một
lượt review để phát hiện lại — lượt sau sẽ tiêu tốn thêm một lượt nữa.

Dữ kiện thứ hai: danh sách bảng thiếu mà reviewer đưa ra (`role_permission`, `menu_item`,
`app_user_claim`, `app_user_login`, `app_user_role`, `app_role_claim`, `audit_log`) **bỏ sót
`core.menu_item_role`**, bảng cũng có hai index FK tự sinh không được liệt kê. Đó là bằng chứng
trực tiếp cho luận điểm chính dưới đây: danh sách này liệt kê tay thì đếm nhầm ngay lần đầu, kể
cả khi người đếm đang chú tâm vào đúng việc đó.

Ràng buộc còn lại lúc quyết định: chưa có DDL nào từng được áp lên một Postgres thật (luật **T2**
ở `RULES.md` mang `⏸️` — fixture Testcontainers có thật, máy hiện tại không có Docker, job CI
backend chưa chạy lần nào vì `src/` chưa vào git). Nghĩa là không ai có `pg_indexes` thật để đối
chiếu; cả hai phương án dưới đây đều phải lấy DDL trong `database/scripts/core/` làm sự thật.

## Quyết định

`architect` chốt: dòng `**Index:**` của mỗi bảng trong `docs/database/schema-core.md` liệt kê
**chỉ** index do con người chọn đặt và có lý do thiết kế. Index một cột mà EF Core tự sinh cho
mỗi cột khoá ngoại **không** được liệt kê.

Quy ước này được viết thành `schema-core.md` §3.8, kèm câu nói thẳng rằng dòng `**Index:**`
**không phải danh sách đầy đủ index của bảng**, và kèm lệnh đọc danh sách đầy đủ từ DDL.

Quyết định này đóng cả tám bảng cùng lúc, không sửa riêng `audit_log`.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — liệt kê đầy đủ mọi index cho cả tám bảng

**Được:** dòng `**Index:**` trả lời trọn câu hỏi *"bảng này có index nào"*. Người đọc không phải
mở file thứ hai. Không cần quy ước nào phải nhớ.

**Mất:** danh sách phải cập nhật mỗi lần một cột khoá ngoại được thêm vào bất kỳ bảng `core` nào
— và không có gì ép việc đó.

**Vì sao loại:** danh sách này **đếm được bằng một lệnh** (`grep -rn 'CREATE .*INDEX'
database/scripts/core/`), nên chép nó vào tài liệu rơi đúng vào điều `.claude/CLAUDE.md` §6 cấm.
Lý do §6 tồn tại áp vào đây không cần suy diễn: chính lượt review sinh ra ADR này đã liệt kê tay
bảy bảng và bỏ sót bảng thứ tám. Nếu một agent đang tập trung vào đúng câu hỏi đó còn đếm thiếu,
thì một người sửa model sáu tháng sau — đang nghĩ về chuyện khác — sẽ không cập nhật gì cả. Kết
quả không phải "tài liệu đầy đủ", mà là "tài liệu trông đầy đủ và sai ở vài dòng không ai biết là
dòng nào". Đó tệ hơn hẳn một tài liệu khai rõ mình không đầy đủ.

### Phương án B — sinh danh sách index tự động vào tài liệu bằng script

**Được:** có đủ danh sách mà không mục ruỗng.

**Mất:** phải viết và nuôi một script sinh, một khối được đánh dấu để ghi đè, và một cổng kiểm
khối đó không lệch. Tài liệu có một vùng người không được sửa tay.

**Vì sao loại:** chi phí vận hành trả mãi mãi cho một lợi ích mà §3.8 đạt được bằng một lệnh
`grep` người đọc tự chạy. Cân nhắc lại khi nào có ai đó thật sự cần danh sách index đầy đủ trong
tài liệu chứ không phải trong DDL — hiện chưa có ca nào như vậy.

### Phương án C — để nguyên, ghi vào danh sách nợ

**Được:** không tốn gì hôm nay.

**Vì sao loại:** trạng thái "để nguyên" chính là trạng thái đã tiêu một lượt review. Câu hỏi
*"cố ý hay sót?"* sẽ được hỏi lại bởi lượt review sau, và lượt sau nữa. Một câu quy ước chấm dứt
việc đó vĩnh viễn với chi phí gần bằng không.

## Hệ quả

### Tích cực

- Câu hỏi *"dòng `**Index:**` thiếu hay cố ý"* có câu trả lời viết ra, tra được, cho cả tám bảng
  cùng lúc. Lượt review sau không phải điều tra lại.
- `schema-core.md` giữ đúng phần nó làm tốt hơn DDL — *vì sao* một index tồn tại — và nhường cho
  DDL phần *có những index nào*, là phần DDL luôn đúng còn tài liệu thì không.
- Không thêm một danh sách nào phải nuôi tay.

### Tiêu cực

- 🛑 **Index EF tự sinh không còn bị bất kỳ tài liệu nào soi, nên không ai xét nó có đáng tồn
  tại hay không.** Đây là cái giá thật và nó có một ca đang mở ngay lúc chốt:
  `ix_audit_log_actor_tenant_id` (`database/scripts/core/0003__core__add-audit-log.sql:31`) nằm
  trên cột `actor_tenant_id`, mà chính §9.4 khai là `null` ở gần như mọi dòng — một index btree
  gần như toàn `NULL`, tốn ghi và tốn dung lượng cho một chọn lọc hiếm khi dùng. Quy ước này làm
  nó **vô hình, không làm nó đúng**. ADR này cố ý **không** quyết ca đó: nó là câu hỏi hiệu năng
  thuộc `backend-expert`, không phải câu hỏi trình bày tài liệu.
- Người đọc `schema-core.md` phải biết quy ước §3.8 mới đọc đúng dòng `**Index:**`. Ai nhảy thẳng
  vào §9.4 mà không đọc §3 sẽ vẫn tưởng danh sách là đầy đủ. Đây là **lỗ mù còn lại sau quyết
  định này**, và nó không được đóng — đóng nó đòi lặp câu quy ước ở tám chỗ, tức tạo tám bản sao,
  tức vi phạm `.claude/CLAUDE.md` §5.
- Quy ước dựa vào một giả định về hành vi EF Core: nó tự tạo index cho mỗi cột khoá ngoại theo
  quy ước mặc định. Ai tắt quy ước đó, hoặc gõ tay một index một cột trên cột khoá ngoại vì lý do
  thiết kế thật, sẽ tạo ra một index vừa không có trong tài liệu vừa không phải EF sinh — và
  không có gì phân biệt được hai ca.
- **Chưa có cổng.** Xem dưới.

## Ép bằng gì

Quy ước §3.8 **chưa có cổng** tại thời điểm chốt. Cổng dựng được — nó so hai tập tên index và
khai được tiêu chí PASS — nhưng nó cần đọc DDL trong `database/`, mà `database/` chưa vào git nên
CI chưa thấy thư mục đó.

Luật tương ứng và dòng nợ đã ghi vào `RULES.md`. Cho tới khi có cổng, quy ước này được giữ bằng
mắt người, và chỗ hỏng của nó là chỗ đã mô tả ở mục Hệ quả tiêu cực.

## Liên quan

| Đọc gì | Vì sao |
| --- | --- |
| [`../database/schema-core.md`](../database/schema-core.md) §3.8 | Chính câu quy ước và lệnh đọc danh sách đầy đủ |
| [`../RULES.md`](../RULES.md) | Luật tương ứng và dòng nợ cổng |
| [`0008-core-so-huu-migration.md`](0008-core-so-huu-migration.md) | Ai sở hữu DDL của schema `core` — nguồn mà §3.8 nhường phần *"có index nào"* cho |
| [`0009-ap-schema-chay-tay.md`](0009-ap-schema-chay-tay.md) | Vì sao DDL sống ở `database/scripts/` chứ không sinh lúc khởi động |
