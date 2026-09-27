---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0048 — Nhãn `📐 ĐÍCH ĐẾN` cấp tệp bị cấm khi một neo khai trong chính tệp đó trỏ tới code có thật

> **Trạng thái:** Đã chấp nhận (2026-09-20)

## Bối cảnh

Ngày 2026-09-20, chín tệp trong [`../contracts/`](../contracts/) — bản mục lục và tám card — đổi nhãn đầu tệp từ `📐 ĐÍCH ĐẾN — CHƯA THI CÔNG` sang `🚧 ĐÃ CHỐT — ĐANG THI CÔNG`. Trước lượt đó, thư mục `src/BE/Core/CoreAndSkill.Core.Web/Controllers/` đã có controller phục vụ đúng những route mà các card ấy mô tả: `AuthController`, `ClientErrorsController`, `MetaController`, `PermissionsController`, `ProfileController`, `RolesController`, `TenantsController`, `UsersController`.

Hệ quả không dừng ở một nhãn cũ. [`../quy-uoc/tieu-chi-review.md`](../quy-uoc/tieu-chi-review.md) §3.2 cấp cho nhãn `📐` hiệu lực **miễn trừ**: báo *"tài liệu mô tả X, code không có X"* cho một mục mang nhãn đó không được tính là finding. Nhãn ấy là nhãn **cấp tệp**. Nên suốt quãng các card còn mang `📐`, mọi câu trong chúng — thân request, bảng lỗi, dòng `Quyền:` — nằm ngoài tầm chấm của mọi lượt review, trong khi code phục vụ chính các route đó đã chạy. Một quyền khai sai ở dòng `Quyền:` của một card như vậy không lượt review nào bắt, và cũng không cổng nào bắt.

Không cơ chế nào buộc một nhãn cấp tệp được xét lại khi code về. [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §4 nói nhãn chỉ lật khi có người mở đúng tệp đó ra đối chiếu — đúng, và cố ý — nhưng đó là luật về **cách** lật, không phải cơ chế **nhắc** lật. Lệnh tầng A của [`0043-lenh-do-cho-phai-lat-doi-sang-nhan-dich-den.md`](0043-lenh-do-cho-phai-lat-doi-sang-nhan-dich-den.md) dò tệp mang nhãn `📐` **và** được mã nguồn trích dẫn; hai khu dưới đây không bị mã nguồn trích dẫn, nên tầng A không phủ chúng.

Lúc soạn ADR này, một lượt dò thử phía FE cho thấy khu `contracts/` **không** phải ca duy nhất, và cũng không phải ca lớn nhất: nhiều tệp trong [`../Design/Components/`](../Design/Components/) đang mang `📐` cấp tệp trong khi `src/FE/src/app/shared/` đã có tệp component cùng tên ở dạng kebab. Tập vi phạm đó **khác rỗng hôm nay**. Phép dò nó khai ở ô *Ý tưởng cổng* của D42 trong [`../RULES.md`](../RULES.md); danh sách tệp thì chạy ra chứ không chép vào đây.

## Quyết định

`architect` cấm nhãn `📐 ĐÍCH ĐẾN` ở **cấp tệp** cho một tệp `docs/` khi một **neo khai ngay trong chính tệp đó** trỏ tới code có thật. Luật mang mã **D42** trong [`../RULES.md`](../RULES.md), mở ở §10 kèm ý tưởng cổng vì chưa ai viết cổng — ý tưởng cổng sống ở đúng ô đó, không chép sang đây.

Hai loại neo có hiệu lực hôm nay, và chỉ hai:

1. **Route** viết nguyên văn ở tiêu đề một card trong [`../contracts/`](../contracts/), đối chiếu với action dưới `src/BE/**/Controllers/`.
2. **Tên component** trong `Design/Components/<Tên>.md`, đối chiếu với tệp component cùng tên ở dạng kebab dưới `src/FE/src/app/`.

Luật **không** áp cho tệp `docs/` không khai neo nào — không suy ra neo từ tên tệp.

## Phương án đã cân nhắc và vì sao loại

### A — Không thêm luật, dựa vào `core-reviewer` nhớ xét lại nhãn

**Được:** không thêm cổng, không thêm dòng luật, không thêm chi phí nào.
**Mất:** đây đúng là cơ chế vừa hỏng. Tám card ở ngoài tầm chấm nhiều tháng chính vì lớp duy nhất canh chỗ này là trí nhớ người đọc — mà nhãn `📐` được thiết kế để người đọc **thôi** nhìn vào đó.
**Vì sao loại:** nó là hiện trạng, và hiện trạng đã để lọt hai khu chứ không phải một.

### B — Chỉ khoanh ở `contracts/`

**Được:** hẹp nhất, cổng đơn giản nhất, không đụng khu Design.
**Mất:** để nguyên tập vi phạm phía FE, vốn nhiều tệp hơn tập `contracts/` vừa dọn. Các tệp đó mô tả biến thể, trạng thái, token của component đang chạy thật trong `src/FE`; miễn trừ review cho chúng đắt ngang miễn trừ cho card hợp đồng.
**Vì sao loại:** lý do duy nhất để hẹp là *thiếu neo máy đọc được*, và phía Design neo **có sẵn**: quy ước đặt tên tệp component. Hẹp ở đây là bỏ sót có chủ ý mà không có lý do kỹ thuật nào đỡ.

### C — Luật rộng: mọi tệp `kind: luat` mang `📐` cấp tệp trong khi có code tương ứng

**Được:** phủ cả `wiki-core/`, `quy-uoc/`, `database/`. [`../wiki-core/be/07-observability.md`](../wiki-core/be/07-observability.md) hôm nay là một ca.
**Mất:** không có neo. *"Code tương ứng"* của [`../quy-uoc/be-performance.md`](../quy-uoc/be-performance.md) là tệp nào? Không tệp nào khai. Cổng cho luật rộng sẽ hoặc đoán bằng tên tệp — báo sai hàng loạt — hoặc đòi mỗi tệp tự khai tệp code đối ứng, tức thêm một khoá frontmatter thứ tư, thứ [`0043-lenh-do-cho-phai-lat-doi-sang-nhan-dich-den.md`](0043-lenh-do-cho-phai-lat-doi-sang-nhan-dich-den.md) đã loại vì nó là lời tự khai.
**Vì sao loại:** luật rộng mạnh hơn nhưng **không cổng nào dựng được**. Loại nó không làm mất câu khẳng định tương ứng: *nhãn phải mô tả thứ có thật* đã là luật ở [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §4, và thêm một mã luật nhắc lại câu đó là tạo nguồn thứ hai. Thứ thiếu chưa bao giờ là câu khẳng định — thứ thiếu là **một phép dò**, và D42 lấy đúng phần dò được.

### D — Bỏ hiệu lực miễn trừ của `📐` ở [`../quy-uoc/tieu-chi-review.md`](../quy-uoc/tieu-chi-review.md) §3.2

**Được:** đóng lỗ tận gốc — không nhãn nào loại được thứ gì khỏi tầm chấm nữa.
**Mất:** mỗi lượt review sẽ báo lại chính nội dung của nhãn cho hàng chục tệp thật sự chưa thi công. Danh sách finding ngập thứ vô nghĩa, và người đọc bỏ qua cả danh sách — mất luôn phần finding thật.
**Vì sao loại:** §3.2 giải đúng một bài toán có thật. Thứ hỏng là *nhãn không được xét lại khi code về*, không phải *nhãn có hiệu lực miễn trừ*.

### E — Cổng sinh từ OpenAPI, đúng cơ chế luật D18 đã khai

**Được:** chính xác hơn hẳn quét văn bản ở nửa `contracts/`, và phủ luôn D18.
**Mất:** cần một lần build chạy được trong CI và cần `src/` đã vào git — cả hai chưa có ([`../README.md`](../README.md) mục *Trạng thái repo*). Dựng nó hôm nay là dựng một cổng chưa chạy lần nào, đúng trạng thái `⏸️` mà [`../RULES.md`](../RULES.md) vừa tách riêng để khỏi bị đọc nhầm. Và nó không giúp gì cho nửa Design.
**Vì sao loại:** hoãn chứ không loại. Khi D18 có cổng OpenAPI, nửa `contracts/` của D42 nên đọc cùng nguồn đó thay vì tự quét văn bản.

## Hệ quả

### Tích cực

- Lỗ đã để lọt tám card nay có một mã luật tra được, và phép dò dựng được **hôm nay**: nó chỉ so hai tập chuỗi, không cần build, không cần `src/` vào git.
- Nửa `contracts/` đi ngược chiều D18 nên rẻ hơn hẳn. D18 hỏi *route trong tài liệu có thật không* và cần OpenAPI để trả lời. D42 hỏi *tệp có đang tự nhận là chưa thi công trong khi neo của nó trỏ tới code thật không* — câu hỏi này trả lời được bằng văn bản tĩnh.
- Ràng buộc *chạy đúng trên một cây không có `src/`* được khai ngay từ lúc chốt, nên cổng không sinh ra ở dạng chỉ chạy được trên máy một người.

### Tiêu cực

- **Luật này có tập vi phạm khác rỗng ngay khi được chốt**, toàn bộ ở nửa Design. Chúng **không** được dọn trong lượt chốt ADR này: gỡ nhãn của một tệp spec đòi mở tệp đó ra đối chiếu với component thật, từng tệp một — đúng thứ [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §4 cấm làm hàng loạt. Nghĩa là repo đang **biết mình sai và chưa sửa**, và đó là trạng thái xấu hơn không biết ở đúng một điểm: nó tiêu uy tín của luật nếu để lâu.
- **Neo loại 2 dựa vào một quy ước đặt tên, không dựa vào một khai báo.** Đổi cách đặt tên tệp component ở `src/FE` sẽ làm nửa Design của cổng ngừng tìm thấy gì mà không ai báo — nên chốt T6 của mục cổng phải đếm cả **số cặp khớp được**, không chỉ số vi phạm.
- **Cổng chỉ so được route viết nguyên văn** ở nửa `contracts/`. Card khuôn — `exports.md` với `/api/v1/<khu>/<tài nguyên>/export` — nằm ngoài tầm, và đó là giới hạn vĩnh viễn chứ không phải chỗ sót chờ vá.
- **Có một chiều báo đỏ biết trước.** Một card mô tả action **chưa** viết, nằm chung tệp với các card đã có action, vẫn buộc cả tệp rời `📐`. Khi rời, câu *"chưa thi công"* của action kia phải chuyển xuống dòng `Status:` của chính card đó. Đó là công phải làm tay, mỗi lần.
- **Luật chưa có cổng.** Tới khi có người viết mục cổng, D42 chỉ được ép bằng review — đúng thứ vừa hỏng. Dòng ở §10 làm nợ nhìn thấy được, không làm nó nhỏ đi.
- **Hai loại neo không phủ hết.** Tệp `docs/` không khai neo nào vẫn giữ được `📐` sau khi code về, và chỉ người đọc bắt được. Hình dạng đúng lâu dài có thể là nhãn cấp **mục** thay vì cấp tệp; ADR này không chốt điều đó.

## Liên quan

| Đọc gì | Vì sao |
| --- | --- |
| [`../RULES.md`](../RULES.md) §10, dòng **D42** | Ý tưởng cổng cho cả hai nửa, chốt T6, và điều kiện chạy khi không có `src/` |
| [`../quy-uoc/tieu-chi-review.md`](../quy-uoc/tieu-chi-review.md) §3.2 | Hiệu lực miễn trừ của nhãn — thứ ADR này khoanh lại chứ không gỡ |
| [`0043-lenh-do-cho-phai-lat-doi-sang-nhan-dich-den.md`](0043-lenh-do-cho-phai-lat-doi-sang-nhan-dich-den.md) | Lệnh dò hai tầng, và lý do tầng A không phủ hai khu này |
| [`../RULES.md`](../RULES.md) §10, dòng **D18** | Cổng OpenAPI hai chiều — nơi nửa `contracts/` của D42 nên về khi D18 có cổng |
