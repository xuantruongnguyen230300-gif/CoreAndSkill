---
kind: luat
scope: core
verified: chua-doi-chieu
---

# OWNERSHIP — mỗi nội dung có đúng MỘT nguồn đối chiếu

> **Đây là sổ đăng ký chủ quyền, và nó là ĐẦU VÀO CỦA CỔNG.** `check-docs.sh` §15 đọc bảng ở §3 và kiểm từng dòng. Bảng §4 là nợ, cổng không đọc. Sổ này không lệch khỏi thứ nó ép **cho những dòng cổng đọc được**, vì chính chúng là thứ được ép. Giới hạn của phạm vi đó nói ở §6 — đọc trước khi tin.

---

## 1. Vì sao có file này

Luật "một chủ đề một file chủ" ([`../.claude/CLAUDE.md`](../.claude/CLAUDE.md) §5, luật D13 ở [`RULES.md`](RULES.md)) không có cổng thì chỉ là gợi ý. Khuôn hỏng nó chặn: **hai hoặc ba file cùng `kind: luat` định nghĩa cùng một thứ, và chúng nói khác nhau**:

| Nội dung | Số file cùng định nghĩa | Hậu quả nếu thi công theo |
| --- | --- | --- |
| Danh mục khoá phân quyền | 3 | Code chặn theo bộ này, dữ liệu seed nạp bộ kia → **403 cho mọi người, kể cả tài khoản đủ quyền** |
| Loại lỗi của một mã lỗi | 2 | Người dùng nhận "bạn không có quyền" trong khi họ có quyền |
| Tiền tố đường dẫn API | 2 | Một hàng rào chống dò mật khẩu **chạy mỗi request và không chặn gì cả** |
| Chỗ đặt lớp đọc `HttpContext` | 2 | Kéo phụ thuộc web vào project bị cấm chứa nó |

Không lỗi nào trong số đó bị cổng bắt, vì cổng khớp chuỗi và kiểm sự tồn tại — nó **không so nội dung hai file với nhau**.

**Bài học:** hai bản sao của một định nghĩa thì chúng sẽ lệch nhau, và bản sao không bao giờ được sửa cùng lúc. Đồng bộ hai bản là hoãn vấn đề; gỡ một bản mới là giải nó.

---

## 2. Ranh giới — sổ này quản cái gì

**Quản: ĐỊNH NGHĨA.** Một catalog, một bảng ánh xạ, một chữ ký kiểu, một hình dạng dữ liệu — thứ tồn tại ở đúng một chỗ và mọi nơi khác chỉ *tra cứu*.

**Không quản: CÁCH DÙNG.** Một quy ước áp khắp nơi (viết đường dẫn thế nào, đặt tên biến ra sao) xuất hiện ở mọi file là bình thường — đó không phải bản sao của định nghĩa, đó là việc tuân thủ nó.

Phép phân biệt:

> Nếu sửa ở một chỗ mà chỗ kia trở thành **sai**, đó là **định nghĩa bị nhân bản** → thuộc sổ này.
> Nếu sửa ở một chỗ mà chỗ kia vẫn đúng theo cách của nó, đó là **cách dùng** → không thuộc sổ này.

### Dạng trỏ đường thay cho nhân bản

File không phải chủ **không được chép lại định nghĩa**. Nó viết một dòng:

```markdown
> 📖 Bảng ánh xạ loại lỗi → mã HTTP: đọc `docs/quy-uoc/be-api-controller.md` §Ánh xạ.
```

Được phép nhắc **tên** của thứ đó, và nhắc **một giá trị cụ thể** khi đang nói về ca đó (một card API ghi endpoint này cần quyền nào là hợp lệ). Không được phép **chép lại cả bảng hoặc cả danh sách**.

---

## 3. Bảng chủ quyền — cổng đọc bảng này

Cột **Chuỗi định danh** là một đoạn văn bản **chỉ xuất hiện ở nơi định nghĩa**. Cổng đếm số file trong `docs/` chứa chuỗi đó; nhiều hơn một file là vi phạm.

Chọn chuỗi cho tốt: phải đủ đặc trưng để không khớp nhầm vào một câu văn xuôi. Đặt nó ở **tiêu đề của chính phần định nghĩa** là cách ổn định nhất — tiêu đề ít bị viết lại hơn thân bài, và mắt người đọc thấy ngay.

### Hợp đồng hai chiều — đây là chỗ luật §1 trở thành cổng

Quy ước đánh mốc: tiêu đề (hoặc một cụm in đậm) của phần định nghĩa mang hậu tố **`— định nghĩa gốc`**.

| Chiều | Luật | Cổng |
| --- | --- | --- |
| Sổ → tài liệu | Mỗi chuỗi đã đăng ký xuất hiện ở **đúng một** file, và đúng file khai làm chủ | `check-docs.sh` §15 (luật D13, D17) |
| Tài liệu → sổ | Mỗi mốc `— định nghĩa gốc` **bắt buộc** có một dòng ở bảng dưới | `check-docs.sh` §16 (luật **D22**) |

Hai chiều cộng lại cho một câu duy nhất mà người viết cần nhớ:

> **Viết một định nghĩa mới thì gắn mốc `— định nghĩa gốc` vào tiêu đề của nó. Cổng lo phần còn lại.**

Gắn mốc mà quên đăng ký → cổng đỏ, kèm `file:dòng`. Đăng ký một chuỗi không tồn tại → cổng đỏ. Chép định nghĩa sang file thứ hai → cổng đỏ. Nhờ vậy sổ này **tự duy trì trong phạm vi §6** — nó không lệch khỏi tài liệu **ở những chỗ có mốc**.

🛑 **Giới hạn phải biết:** cả hai chiều chỉ canh được thứ **có gắn mốc**. Hai file mô tả cùng một thứ bằng câu chữ khác, không file nào gắn mốc, thì cổng im lặng — đó là nợ **D13 (phần còn lại)** ở [`RULES.md`](RULES.md) §10, và lớp bắt được nó là `core-reviewer` đọc hiểu, không phải máy.

| Chủ đề | File chủ | Chuỗi định danh |
| --- | --- | --- |
| Danh sách màn hình Core | `docs/quy-uoc/fe-architecture.md` | `Màn hình Core — định nghĩa gốc` |
| Trên dây là DTO, không phải entity | `docs/quy-uoc/be-cqrs-handler.md` | `Trên dây là DTO, không bao giờ là entity — định nghĩa gốc` |
| Thứ tự ưu tiên của cấu hình theo đơn vị | `docs/wiki-core/be/19-cau-hinh-theo-don-vi.md` | `Thứ tự ưu tiên của cấu hình — định nghĩa gốc` |
| Khuôn sinh mã nghiệp vụ | `docs/wiki-core/be/20-sinh-ma-nghiep-vu.md` | `Khuôn sinh mã nghiệp vụ — định nghĩa gốc` |
| Bảng ánh xạ loại lỗi → mã HTTP | `docs/quy-uoc/be-api-controller.md` | `Bảng ánh xạ ErrorType → HTTP — định nghĩa gốc` |
| Danh mục khoá phân quyền của Core | `docs/database/schema-core.md` | `Danh mục khoá phân quyền Core — định nghĩa gốc` |
| Hình dạng envelope lỗi | `docs/quy-uoc/be-api-controller.md` | `Hình dạng envelope — định nghĩa gốc` |
| Tiền tố đường dẫn và phiên bản API | `docs/quy-uoc/be-api-controller.md` | `Tiền tố đường dẫn — định nghĩa gốc` |
| Chữ ký `CoreDbContext` và hai bộ lọc toàn cục | `docs/quy-uoc/be-entity-domain.md` | `Chữ ký CoreDbContext — định nghĩa gốc` |
| Thứ tự middleware trong `UseCore()` | `docs/quy-uoc/be-architecture.md` | `Thứ tự pipeline — định nghĩa gốc` |
| Vòng đời DI của các seam Core | `docs/quy-uoc/be-architecture.md` | `Vòng đời DI — định nghĩa gốc` |
| Danh sách project của Core và ranh giới | `docs/kien-truc-core-module.md` | `Năm project Core — định nghĩa gốc` |
| Kiểu envelope phía FE | `docs/quy-uoc/fe-api-client.md` | `Kiểu envelope phía FE — định nghĩa gốc` |
| Chuỗi interceptor FE và thứ tự | `docs/quy-uoc/fe-api-client.md` | `Chuỗi interceptor FE — định nghĩa gốc` |
| Hình dạng phân trang phía FE | `docs/quy-uoc/fe-api-client.md` | `Hình dạng phân trang phía FE — định nghĩa gốc` |
| Allowlist import thư viện UI (luật F5) | `docs/wiki-core/fe/05-component-library.md` | `Allowlist import thư viện UI — định nghĩa gốc` |
| Danh sách rule ESLint cấm tắt (luật F3) | `docs/quy-uoc/fe-architecture.md` | `Danh sách rule cấm tắt — định nghĩa gốc` |
| Hàm gắn `fieldErrors` vào form | `docs/wiki-core/fe/09-forms-validation.md` | `Hàm gắn fieldErrors vào form — định nghĩa gốc` |
| Khuôn mã lỗi và biểu thức kiểm | `docs/quy-uoc/be-cqrs-handler.md` | `Khuôn mã lỗi — định nghĩa gốc` |
| Ngưỡng cân nhắc thêm thư viện store FE | `docs/wiki-core/fe/03-state-management.md` | `Ba ngưỡng để cân nhắc thêm thư viện store — định nghĩa gốc` |
| Kiểu CLR hợp lệ cho concurrency token | `docs/quy-uoc/be-entity-domain.md` | `Kiểu CLR nào hợp lệ cho concurrency token — định nghĩa gốc` |
| Hợp đồng `code` / `message` / `messageParams` | `docs/quy-uoc/be-cqrs-handler.md` | `Hợp đồng thông điệp lỗi — định nghĩa gốc` |
| Sơ đồ bốn tầng FE và chiều phụ thuộc | `docs/quy-uoc/fe-architecture.md` | `Bốn tầng — và một chiều duy nhất — định nghĩa gốc` |
| Luật sở hữu migration theo schema | `docs/database/migration-policy.md` | `Mỗi module sở hữu migration của schema mình.** — định nghĩa gốc` |
| Cột của bảng nhật ký kiểm toán | `docs/database/schema-core.md` | `Bảng nhật ký kiểm toán — định nghĩa gốc` |
| Danh tính và đơn vị cho mã chạy ngoài request | `docs/quy-uoc/be-architecture.md` | `Danh tính và đơn vị khi không có request — định nghĩa gốc` |
| Seam danh mục khoá quyền do module cấp | `docs/quy-uoc/be-architecture.md` | `Danh mục khoá quyền do module cấp — định nghĩa gốc` |
| Hai tầng tệp dịch Core / dự án | `docs/wiki-core/fe/08-i18n.md` | `Hai tầng tệp dịch — định nghĩa gốc` |
| Cỡ icon (`--icon-sm` … `--icon-xl`) | `docs/Design/DESIGN.md` | `Cỡ icon — định nghĩa gốc` |
| Bố cục màn danh sách của Core | `docs/Design/Templates/ListScreen.md` | `Bố cục màn danh sách — định nghĩa gốc` |
| Kiểu dữ liệu công khai của thư viện UI | `docs/quy-uoc/fe-ui-conventions.md` | `Kiểu dữ liệu công khai của thư viện UI — định nghĩa gốc` |
| Seam mở rộng cho màn Core | `docs/quy-uoc/fe-architecture.md` | `Seam cho MÀN Core — định nghĩa gốc` |
| Tầng giữ trạng thái màn danh sách | `docs/quy-uoc/fe-architecture.md` | `Trạng thái màn danh sách — định nghĩa gốc` |
| Mã hướng khoá `K##` | `docs/wiki-core/README.md` | `Mã hướng khoá `K##` — định nghĩa gốc` |

> Bảng này **chưa phủ hết** mọi định nghĩa trong repo. Nó phủ những chỗ đã thật sự lệch. Thêm dòng khi phát hiện một định nghĩa bị nhân bản — đó là cách sổ này lớn lên, và mỗi dòng thêm vào là một lớp bảo vệ vĩnh viễn.

---

## 4. Chờ áp — đã chọn chủ, CHƯA đặt mốc

Cổng **không** đọc bảng này. Nó là nợ nhìn thấy được, cùng khuôn [`RULES.md`](RULES.md) §10.

Một dòng chỉ được chuyển lên §3 **sau khi** chuỗi định danh đã thật sự nằm trong file chủ. Đưa lên sớm thì cổng báo *"chuỗi không xuất hiện ở đâu cả — dòng này đang không canh gì"*, và một dòng sổ không canh gì còn tệ hơn không có dòng nào: nó làm bảng trông đầy đủ hơn thực tế.

| Chủ đề | File chủ đã chọn | Chuỗi định danh sẽ đặt |
| --- | --- | --- |
| — | — | — |

**Bảng này đang rỗng, và đó là trạng thái đúng.** Bảy dòng từng nằm ở đây (danh mục khoá phân quyền, hình dạng envelope, tiền tố đường dẫn, chữ ký `CoreDbContext`, thứ tự pipeline, vòng đời DI, danh sách project) đã đặt xong mốc ở file chủ và **đã chuyển lên §3** — cùng lượt với việc gỡ các bản sao của chúng ở file khác.

Rỗng **không** có nghĩa là repo đã hết định nghĩa bị nhân bản: nó có nghĩa là hết những chỗ **đã được chọn chủ mà chưa đặt mốc**. Chỗ chưa ai phát hiện thì không có dòng nào ở cả hai bảng — giới hạn đó nói ở §6.

> **Vì sao bảng này tồn tại thay vì gộp vào §3:** bản đầu của sổ đưa cả tám dòng vào §3 khi mới có một mốc được đặt. Cổng lập tức báo bảy dòng *"đang không canh gì"* — đúng, và đó là bài học: một sổ đăng ký khai chủ quyền cho thứ chưa đánh dấu là đang mô tả **ý định**, không phải **thực tế**. Cùng lỗi mà [`../.claude/CLAUDE.md`](../.claude/CLAUDE.md) §4 cấm, chỉ khác chỗ áp.

---

## 5. Quy trình khi phát hiện hai file cùng định nghĩa một thứ

1. **Chọn file chủ.** Tiêu chí, theo thứ tự: (a) file mà thứ đó *sinh ra* ở đó — schema sở hữu tên cột, quy ước thi công sở hữu chữ ký kiểu; (b) file mà người đọc tìm đến trước; (c) file `kind: luat` thay vì `kind: quyet-dinh`.
2. **Rút file kia còn một dòng trỏ đường.** Không xoá mục — xoá mục thì người đang đọc file đó mất đường đi tiếp.
3. **Đặt một chuỗi định danh** vào phần định nghĩa ở file chủ, và thêm một dòng vào bảng §3.
4. **Chạy cổng.** Nó phải xanh, và phải **đỏ thật** nếu bạn thử chép lại định nghĩa sang file khác.

🛑 **Không "giữ cả hai cho chắc".** Hai bản đầy đủ của một định nghĩa là hai nguồn, và người sửa sẽ chỉ sửa một. Đó chính là cách repo tiền nhiệm có bốn sơ đồ đặt tên project nói ngược nhau.

---

## 6. Giới hạn của cổng này — nói rõ để không ai tin quá

Cổng §15 chỉ bắt được **bản sao gần như nguyên văn** của một chuỗi đã đăng ký. Năm thứ nó **không** bắt:

1. **Diễn đạt lại.** Một file mô tả cùng bảng ánh xạ bằng câu chữ khác thì chuỗi định danh không khớp, và cổng im lặng.
2. **Định nghĩa chưa đăng ký MÀ KHÔNG GẮN MỐC.** §16 ép việc đăng ký cho mọi định nghĩa **có mốc**; một định nghĩa viết ra mà không gắn mốc thì nằm ngoài cả hai chiều. Việc gắn mốc vẫn là kỷ luật của người viết — cổng không thể biết một đoạn văn *có phải* định nghĩa hay không.
3. **Nội dung sai ở chính file chủ.** Cổng bảo đảm *chỉ có một nguồn*, không bảo đảm *nguồn đó đúng*.
4. **Bản sao đặt ngoài `docs/`.** Cổng chỉ đếm file trong `docs/`. Một định nghĩa bị chép sang `.claude/` hoặc `spec/` **không bị bắt** — mà chép nội dung sang `.claude/` chính là hành vi [`../.claude/CLAUDE.md`](../.claude/CLAUDE.md) §3 dành bốn lý do để cấm. Luật C2 canh chiều đó bằng review, xem [`RULES.md`](RULES.md) §10.
5. **Chuỗi định danh chọn kém.** Một chuỗi quá ngắn hoặc quá phổ thông sẽ khớp nhầm vào văn xuôi của file vô can, và cổng đổ lỗi sai chỗ. §16 có sàn độ dài 12 ký tự để chặn ca tệ nhất, nhưng phần còn lại là phán đoán của người thêm dòng.

Vì vậy sổ này là lớp bổ sung cho `core-reviewer`, không phải thay thế. Việc đọc hiểu và đối chiếu ngữ nghĩa vẫn thuộc về người và về agent review.
