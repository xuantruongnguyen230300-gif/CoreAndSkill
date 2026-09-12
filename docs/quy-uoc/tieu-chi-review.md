---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Tiêu chí chấm review — cái gì là finding, cái gì không

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/` để review. File này khai sẵn luật chấm để lượt review đầu tiên ở giai đoạn 2 không phải tự nghĩ ra tiêu chí giữa chừng.
>
> File này **không nhắc lại quy ước** — quy ước nằm ở các file `be-*.md` và `fe-*.md` cạnh đây. Nó chỉ trả lời: *lệch khỏi quy ước thì chấm mức nào, và trường hợp nào lệch mà **không** phải lỗi.*

---

## 1. Ba mức chấm

| Mức | Nghĩa |
| --- | --- |
| **PASS** | Đúng quy ước, hoặc lệch có ghi nhận hợp lệ |
| **PARTIAL** | Có nhưng thiếu hoặc sai một phần — ghi rõ thiếu cái gì |
| **MISSING** | Không có |

Ba mức, không hơn. Thêm mức trung gian ("gần đúng", "chấp nhận được") làm báo cáo mất khả năng ra quyết định: người đọc không biết phải sửa hay không.

---

## 2. Năm loại finding và mức nghiêm trọng

| Loại | Định nghĩa | Mức mặc định |
| --- | --- | --- |
| **Bảo mật** | Dữ liệu hoặc thao tác tới tay người không được phép; secret lộ; kiểm quyền thiếu hoặc đặt sai tầng | 🔴 Chặn |
| **Correctness** | Code cho ra kết quả sai với một input mô tả được | 🔴 Chặn |
| **Vi phạm ranh giới** | Phá luật kiến trúc: tầng import ngược, module import chéo, DTO lọt ra ngoài `services/`, Core biết tên nghiệp vụ | 🟠 Phải sửa |
| **Hiệu năng** | Query N+1, thiếu index cho cột lọc nóng, tải toàn bộ rồi lọc trong bộ nhớ, bundle vượt ngân sách | 🟡 Sửa nếu có số đo, ghi nhận nếu chưa đo |
| **Đơn giản hoá** | Có cách ngắn hơn, ít lớp gián tiếp hơn, dùng lại thứ đã có | 🔵 Đề xuất |

### 2.1 Nguyên tắc phân mức

**Mức không do độ khó sửa quyết định, mà do hậu quả nếu không sửa.** Một lỗi bảo mật sửa trong ba dòng vẫn là mức chặn; một tái cấu trúc lớn giúp code đẹp hơn vẫn là mức đề xuất.

**Nâng mức khi lỗi im lặng.** Cùng một hậu quả, nhánh lỗi *không có triệu chứng* nghiêm trọng hơn nhánh lỗi *nổ ra*. Một envelope hỏng làm trang trắng thì có người báo trong ngày; một envelope hỏng làm lưới hiện rỗng thì trông giống "không có dữ liệu" và có thể sống hàng tháng — trong lúc đó người dùng ra quyết định dựa trên một màn hình nói dối.

**Hạ mức khi đã có cổng bắt.** Vi phạm mà cổng tự động bắt được và cổng đang chạy thì ghi nhận ở mức thấp hơn: nó không thể lọt vào nhánh chính. Ngược lại, một vi phạm **chỉ** người mới bắt được thì nâng mức, vì lần sau có thể không ai nhìn.

### 2.2 Ví dụ phân loại cho từng loại

| Tình huống | Loại | Mức |
| --- | --- | --- |
| Quyền kiểm bằng tên role thay vì permission | Bảo mật | 🔴 |
| Endpoint mới không có kiểm quyền, dựa vào việc FE ẩn nút | Bảo mật | 🔴 |
| Giá trị bí mật khai trong cấu hình FE | Bảo mật | 🔴 |
| Đọc `data` của envelope bằng giá trị mặc định thay vì hàm mở gói | Correctness | 🔴 (lỗi im lặng) |
| `@for` dùng chỉ số làm khoá theo dõi trên danh sách có sắp xếp lại | Correctness | 🔴 |
| Điều hướng bằng cách gọi lệnh chuyển trang rồi trả về false trong guard | Correctness | 🟠 |
| `core/` import một thứ từ `shared/` | Vi phạm ranh giới | 🟠 |
| Component trong `components/` tự inject service lấy dữ liệu | Vi phạm ranh giới | 🟠 |
| Một dòng tắt rule ranh giới bằng comment | Vi phạm ranh giới | 🔴 — xem §2.3 |
| Vòng lặp gọi API cho từng dòng của lưới | Hiệu năng | 🟡 |
| Ba component gần giống nhau, gộp được thành một | Đơn giản hoá | 🔵 |

### 2.3 Một ngoại lệ: tắt rule ranh giới luôn là mức chặn

Một dòng `eslint-disable` cho rule ranh giới **không** phải vi phạm ranh giới bình thường — nó là vi phạm **hàng rào**. Vi phạm ranh giới làm hỏng một chỗ; tắt hàng rào làm hỏng khả năng phát hiện mọi chỗ sau đó.

Vì FE không có compiler ép ranh giới ([`fe-architecture.md`](fe-architecture.md) §4.1), hàng rào duy nhất là ESLint cộng với luật cấm tắt nó. Một ngoại lệ được chấp nhận là tiền lệ, và tiền lệ thứ hai không còn ai phản đối được.

---

## 3. KHÔNG phải finding

Ba nhóm dưới đây bị báo nhầm thường xuyên nhất. Báo nhầm không vô hại: nó làm loãng báo cáo, và một báo cáo có nhiều mục sai sẽ khiến người đọc bỏ qua cả những mục đúng.

### 3.1 Lệch khỏi `wiki-core/` vì đã cố ý đơn giản hoá

[`../wiki-core/`](../wiki-core/) mô tả *một core tốt gồm những gì* — nó có thể **vượt nhu cầu** của dự án hiện tại. Khoảng cách giữa nó và [`../quy-uoc/`](../quy-uoc/) là cố ý, không phải nợ.

**Không phải finding:** code không có một thành phần mà `wiki-core/` mô tả, trong khi quyết định bỏ nó đã được ghi nhận — trong ADR, trong một mục "vì sao chưa làm" của chính file quy ước, hoặc trong bảng nợ ở [`../RULES.md`](../RULES.md) §10.

Ví dụ cụ thể theo các quyết định đã chốt: chỉ có hai pipeline behavior ([`../adr/0006-pipeline-behavior.md`](../adr/0006-pipeline-behavior.md)); chưa có tầng cache phân tán; chưa có message broker. Báo "thiếu behavior ghi log" là báo một thứ đã được quyết định là không làm.

**Là finding:** code lệch khỏi `wiki-core/` **và** không có ghi nhận nào. Lúc đó thứ đáng báo không hẳn là code — mà là *thiếu ghi nhận*. Cách báo đúng: nêu khoảng cách, hỏi đây là chủ đích hay sót, và đề nghị ghi vào §9 của [`../RULES.md`](../RULES.md) nếu là chủ đích.

### 3.2 Thứ đang mang nhãn `📐 ĐÍCH ĐẾN`

Nhãn `📐 ĐÍCH ĐẾN — CHƯA THI CÔNG` nghĩa là *chưa ai viết code cho phần này*. Báo "doc mô tả X, code không có X" cho một mục mang nhãn đó là báo lại chính nội dung của cái nhãn.

Ở giai đoạn 1 điều này áp cho gần như toàn bộ `docs/` — chưa có `src/`. Ở giai đoạn 2, đọc nhãn ở **đầu mục** trước khi chấm mục đó.

**Là finding:** một mục mang nhãn `📐` nhưng ở chỗ khác lại có câu khẳng định nó đã tồn tại. Hai câu nói ngược nhau về cùng một thứ luôn là finding, bất kể câu nào đúng.

### 3.3 Sở thích cá nhân về đặt tên

Tên biến, tên hàm, cách sắp xếp thứ tự thành viên trong một class, chọn giữa hai cách viết đều đúng: **không phải finding**, trừ khi vi phạm một quy ước đã ghi thành luật (tiền tố selector, hậu tố tên file, khuôn khoá i18n — [`fe-ui-conventions.md`](fe-ui-conventions.md) §1.5 và §5.3).

Phép thử: *"quy ước tôi đang viện dẫn có nằm trong một file `kind: luat` không?"* Không → đó là góp ý, ghi ở mục riêng của báo cáo hoặc bỏ qua, **không** vào danh sách finding.

Lý do khắt khe ở điểm này: góp ý đặt tên là loại góp ý dễ viết nhất và rẻ nhất, nên nó tự nhiên chiếm chỗ của những mục khó tìm hơn. Một báo cáo review mà phần lớn mục là góp ý đặt tên đã thất bại ở đúng lý do nó tồn tại.

### 3.4 Những thứ khác không phải finding

| Tình huống | Vì sao không |
| --- | --- |
| Thiếu cache ở một endpoint chậm | Quyết định đã chốt: cache đi **sau** khi sửa query và **sau** khi đo |
| Chưa có module nghiệp vụ nào | Hiện trạng đã biết ở giai đoạn đầu, không phải MISSING |
| Một cổng "pass rỗng" vì chưa có gì để kiểm | Không phải cổng chết — nó có hiệu lực ngay khi đối tượng xuất hiện. Đề nghị **xoá** cổng đó thì **là** finding |
| Comment ít trong code | Quyết định đã chốt là comment tối thiểu ([`../adr/0010-comment-toi-thieu.md`](../adr/0010-comment-toi-thieu.md)) |
| Tài liệu mang `verified: chua-doi-chieu` | Đó là giá trị trung thực, không phải nợ cần dọn |

---

## 4. Mọi finding phải neo bằng bằng chứng

> **Không neo được thì không phải finding.**

Bằng chứng hợp lệ có đúng hai dạng:

**Dạng 1 — đường dẫn kèm số dòng.** Trỏ vào một file có thật, đang được theo dõi trong repo. Không trỏ vào file bị `.gitignore` loại trừ ([`repo-artifact.md`](repo-artifact.md) §11), không trỏ vào file mang `kind: lich-su`.

**Dạng 2 — kịch bản hỏng cụ thể.** Khi lỗi nằm ở tương tác giữa nhiều chỗ chứ không ở một dòng, bằng chứng là một chuỗi *input → điều gì xảy ra → output sai*:

```text
Người dùng ở trang 7 của danh sách, gõ từ khoá mới vào ô tìm kiếm.
→ Tham số trang giữ nguyên giá trị 7 khi gọi API.
→ API trả về trang rỗng.
→ Màn hình hiện "không có kết quả" trong khi kết quả có ở trang 1.
```

Kịch bản phải cụ thể tới mức người đọc **tái hiện được** mà không hỏi lại. "Có thể gây lỗi trong một số trường hợp" không phải kịch bản — đó là một linh cảm chưa kiểm chứng.

### 4.1 Vì sao khắt khe đến vậy

Một finding không neo được có ba khả năng, và người đọc không phân biệt được ba khả năng đó: (a) lỗi có thật nhưng người viết chưa tìm ra chỗ, (b) người viết nhớ nhầm sang một dự án khác, (c) suy diễn từ một mô tả trong tài liệu chứ không từ code.

Khả năng (c) đã xảy ra thật ở dự án tiền nhiệm và là lý do §5 tồn tại.

### 4.2 Khi nghi ngờ nhưng chưa neo được

Đừng bỏ đi, và cũng đừng ghi thành finding. Ghi vào mục **"Cần xác minh"** của báo cáo, kèm câu hỏi cụ thể và lệnh cần chạy để trả lời. Mục đó có giá trị thật — nó chuyển một linh cảm thành một việc làm được — nhưng nó không được lẫn vào danh sách finding, vì hai thứ đòi hai loại hành động khác nhau.

---

## 5. Cảnh báo về nguồn tri thức — đọc trước khi chấm bất cứ mục nào

> 🛑 **Chỉ đối chiếu code với file mang `kind: luat`.**

Ba khoá phân loại ở đầu mọi file `docs/` ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §9) trả lời một câu duy nhất mà người review cần: *code có phải tuân file này không?*

| `kind` | Code phải tuân? | Dùng trong review thế nào |
| --- | --- | --- |
| `luat` | **Có** | Đây là chuẩn để đối chiếu |
| `tham-chieu` | **Không** | Bối cảnh, so sánh, bài học. **Không** dùng làm chuẩn |
| `quyet-dinh` | Có, gián tiếp | Giải thích vì sao luật là như vậy. Dùng để hiểu, không để chấm |
| `lich-su` | **Không** | Tài liệu đã chết. Không trích dẫn làm bằng chứng |

**Sai lầm đã xảy ra thật:** một file mô tả lộ trình của **một dự án khác** bị đọc như luật của repo này, và kết quả là hàng loạt báo cáo dạng *"doc yêu cầu X, code không có X"* cho những X chưa bao giờ là luật ở đây. Mọi mục trong báo cáo đó đều trông chặt chẽ — có trích dẫn tài liệu, có mô tả rõ ràng, và đều sai.

Loại lỗi này đắt vì nó **không tự lộ ra**: người đọc báo cáo thấy có trích dẫn thì tin, và việc kiểm lại đòi mở đúng file gốc để đọc dòng `kind:` — thao tác mà không ai làm nếu không được nhắc.

**Quy trình bắt buộc trước khi trích một file làm chuẩn:**

```bash
head -5 <đường-dẫn-file>   # đọc kind, scope, verified
```

`kind` không phải `luat` → file đó không phải chuẩn. Nếu vẫn muốn dùng nội dung của nó, cách đúng là **đề nghị nâng nội dung đó thành luật** (thêm dòng vào [`../RULES.md`](../RULES.md) kèm cột "ép bằng gì"), không phải chấm code theo nó.

Tương tự với `verified:`. Giá trị `chua-doi-chieu` nghĩa là **chưa ai mở source ra so với file này**. Nội dung vẫn có thể đúng, nhưng nó chưa được xác nhận — nên khi code và tài liệu lệch nhau, chưa chắc code sai. Ghi cả hai vế và hỏi, đừng mặc định tài liệu thắng.

---

## 6. Khuôn báo cáo review

Dùng đúng khuôn này. Mục cố định, thứ tự cố định — người đọc quen khuôn thì đọc nhanh hơn và không bỏ sót mục nào.

```markdown
# Review — <phạm vi thay đổi>

**Ngày:** YYYY-MM-DD
**Phạm vi:** <nhánh / danh sách file / mô tả thay đổi>
**Nguồn đối chiếu:** <danh sách file `kind: luat` đã dùng làm chuẩn>

## Kết luận

<Một câu: có chặn hay không, và vì sao.>

| Loại | 🔴 Chặn | 🟠 Phải sửa | 🟡 Cân nhắc | 🔵 Đề xuất |
| --- | --- | --- | --- | --- |
| Bảo mật | | | | |
| Correctness | | | | |
| Vi phạm ranh giới | | | | |
| Hiệu năng | | | | |
| Đơn giản hoá | | | | |

## Finding

### 🔴 F-01 — <tiêu đề một dòng>

- **Loại:** Bảo mật
- **Bằng chứng:** `<đường-dẫn>:<dòng>`
- **Hiện trạng:** <code đang làm gì>
- **Vì sao sai:** <hậu quả cụ thể, không phải "không đúng chuẩn">
- **Chuẩn đối chiếu:** `<file kind: luat>` §<mục>
- **Đề xuất sửa:** <cụ thể, sửa được ngay>

### 🟠 F-02 — <tiêu đề>

...

## Cần xác minh

| # | Nghi ngờ | Lệnh / cách kiểm | Ai trả lời |
| --- | --- | --- | --- |
| V-01 | | | |

## Không phải finding — ghi nhận để khỏi báo lại

| Quan sát | Vì sao không phải finding |
| --- | --- |
| | |
```

### 6.1 Bốn ô hay bị viết sai

**"Nguồn đối chiếu"** — liệt kê **file cụ thể**, không viết "theo tài liệu dự án". Ô này tồn tại để người đọc kiểm được rằng mọi chuẩn dùng để chấm đều mang `kind: luat` (§5).

**"Vì sao sai"** — nói hậu quả, không nói vi phạm. *"Không đúng quy ước"* là lặp lại ô "Chuẩn đối chiếu". Ô này phải trả lời: *nếu không sửa thì cái gì hỏng, với ai, lúc nào?*

**"Đề xuất sửa"** — cụ thể tới mức người nhận làm được ngay. *"Cần cân nhắc lại thiết kế"* không phải đề xuất; nó là cách chuyển việc khó về phía người khác.

**"Không phải finding"** — đừng bỏ trống. Ghi lại những thứ **đã xem xét và quyết định không báo** giúp lượt review sau không mất công xem lại, và giúp người viết code biết rằng chỗ đó đã được nhìn tới.

---

## 7. Trình tự một lượt review

1. **Đọc phạm vi thay đổi trước, đọc tài liệu sau.** Đọc tài liệu trước dễ dẫn tới việc đi tìm bằng chứng cho một kết luận đã có.
2. **Xác định nguồn đối chiếu**, kiểm `kind:` của từng file (§5).
3. **Chạy cổng trước khi chấm bằng mắt.** Cổng bắt hết phần máy bắt được; đừng tốn lượt review vào những thứ đó. Nếu cổng chưa chạy được, **đó là finding đầu tiên**.
4. **Chấm theo thứ tự mức:** bảo mật → correctness → ranh giới → hiệu năng → đơn giản hoá. Hết ngân sách thời gian thì phần bỏ dở nằm ở cuối, đúng chỗ ít thiệt hại nhất.
5. **Neo từng finding** (§4). Không neo được thì chuyển sang "Cần xác minh".
6. **Viết kết luận cuối cùng**, không viết trước.

### 7.1 Cổng không thay được review, và ngược lại

Cổng bắt được thứ máy kiểm được: đường dẫn tồn tại, mẫu văn bản, đồ thị tham chiếu. Nó **không** đọc hiểu. Ba loại lỗi cổng không bao giờ bắt được ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §8):

1. Văn xuôi mô tả thứ không tồn tại.
2. Sơ đồ hoặc cây thư mục chép sai.
3. Một tuyên bố hoàn thành kèm ngày hợp lệ trong khi việc chưa làm.

Cả ba đều thuộc phần việc của người review. Ngược lại, đừng dùng lượt review để làm việc của cổng — nếu một loại lỗi lặp lại qua nhiều lượt review, việc đúng là **viết cổng cho nó**, không phải review kỹ hơn.

---

## 8. Đối chiếu

Quy ước dùng làm chuẩn khi chấm: [`be-architecture.md`](be-architecture.md) · [`be-entity-domain.md`](be-entity-domain.md) · [`be-cqrs-handler.md`](be-cqrs-handler.md) · [`be-api-controller.md`](be-api-controller.md) · [`be-performance.md`](be-performance.md) · [`fe-architecture.md`](fe-architecture.md) · [`fe-api-client.md`](fe-api-client.md) · [`fe-ui-conventions.md`](fe-ui-conventions.md) · [`fe-routing-guard.md`](fe-routing-guard.md) · [`repo-artifact.md`](repo-artifact.md).

Bảng luật kèm cột "ép bằng gì" và danh sách nợ: [`../RULES.md`](../RULES.md). Bài học từ sự cố đã xảy ra: [`../audit/`](../audit/).
