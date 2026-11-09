---
name: "ba-story"
description: "Chuyển một yêu cầu thô thành spec nghiệp vụ có kiểm chứng được: chạy ba-analyst để viết spec/<feature>/business-rules.md gồm User Story, Acceptance Criteria và Business Rule, cộng spec/<feature>/ui-spec.md khi feature có màn mới. Dùng khi một feature Module chưa có spec, trước khi bất kỳ ai viết code cho nó."
argument-hint: "<tên feature> <mô tả yêu cầu thô> - vd 'DonNghiPhep - nhân viên xin nghỉ, quản lý duyệt'"
---

## User Input

```text
$ARGUMENTS
```

Bạn **BẮT BUỘC** phải xem xét user input trước khi tiếp tục (nếu không rỗng).

## Mục tiêu

Không có spec thì `backend-expert` và `frontend-expert` sẽ **tự suy diễn nghiệp vụ** — và suy diễn của hai agent đó không bao giờ trùng nhau. Skill này đóng khoảng trống đó bằng một file duy nhất mà cả hai cùng trỏ tới.

Ranh giới ba khu theo [`../../CLAUDE.md`](../../CLAUDE.md) §2: `docs/` giữ **quy tắc** kiến trúc và code, `spec/` giữ **nghiệp vụ theo feature**. Skill này chỉ ghi vào `spec/`. Nếu trong lúc làm phát hiện một quy tắc kiến trúc cần đổi, đó là việc của `architect`, không viết vào spec.

🛑 **Không suy diễn.** Chỗ nào yêu cầu mơ hồ thì hỏi; hỏi xong vẫn chưa rõ thì ghi vào mục câu hỏi còn mở, **không** điền một đáp án hợp lý rồi đi tiếp. Một câu hỏi còn mở nhìn thấy được thì rẻ; một giả định được viết như sự thật thì đắt.

## Các bước thực hiện

### 1. Resolve feature

`$ARGUMENTS` rỗng → hỏi người dùng tên feature và mô tả ngắn. **Không tự đoán feature nào đang cần làm.**

Tên feature là tên thư mục dưới `spec/`, viết không dấu. Kiểm đã có chưa:

```bash
ls spec 2>/dev/null
```

Đã có `spec/<feature>/business-rules.md` → hỏi người dùng: bổ sung vào file cũ, hay đây là feature khác bị đặt trùng tên. Không ghi đè.

### 2. Phân loại Core hay Module — không đoán

Tiêu chí ở [`../../../docs/kien-truc-core-module.md`](../../../docs/kien-truc-core-module.md) §4. Đọc mục đó, đừng suy ra từ tên feature.

| Kết luận | Làm gì |
| --- | --- |
| **Module** (nghiệp vụ riêng của một domain) | Đi tiếp — đây đúng là việc của skill này |
| **Core** (có ý nghĩa với mọi sản phẩm dựng trên nền tảng) | **Dừng.** Core không có `spec/`. Việc chạm kiến trúc Core thuộc `architect`; báo người dùng |
| **Không chắc** | 🛑 **Dừng, hỏi người dùng.** Đây là quyết định ranh giới kiến trúc, không suy đoán được từ tên |

Ngưỡng chống Core phình to nằm ở mục 4.2 của cùng file. **Mở ra đọc, đừng trả lời từ trí nhớ** — ngưỡng là một con số quy tắc và nó có thể được sửa.

### 3. Làm rõ yêu cầu — hỏi từng câu một cho tới khi hết mơ hồ

Câu hỏi làm rõ đến từ `ba-analyst`: gọi nó qua `Agent` với tên feature, kết luận bước 2 và nguyên văn yêu cầu thô, giao đúng việc liệt kê câu hỏi (Bước 1 của nó) — chưa viết spec. Yêu cầu thô gần như luôn thiếu bốn thứ sau; danh sách nó trả về chưa phủ thứ nào thì trả lại cho nó bổ sung:

| Hỏi gì | Thiếu thì hỏng ra sao |
| --- | --- |
| **Ai là actor**, và actor đó cần quyền gì | Không phân quyền được; FE dựng màn hình cho sai người dùng |
| **Trạng thái nào tồn tại** và chuyển giữa chúng bằng hành động nào | Mỗi agent tự đặt ra một tập trạng thái khác nhau |
| **Điều gì bị cấm** — không chỉ điều gì được phép | Luật cấm không viết ra thì không ai test, và nó chỉ lộ ra trên môi trường thật |
| **Chuyện gì xảy ra ở ca xấu** — trùng, hết hạn, thiếu quyền, dữ liệu rỗng | Ca xấu chiếm phần lớn khối lượng code nhưng thường không có trong yêu cầu thô |

Rồi hỏi người dùng **từng câu một** bằng `AskUserQuestion` — mỗi lần gọi một câu:

- Câu hỏi kèm thông tin cần để quyết: chỗ nào của feature phụ thuộc câu trả lời, và hệ quả của từng phương án.
- Phương án gợi ý đứng đầu, nhãn có hậu tố `(Recommended)`. Gợi ý không phải nghiệp vụ đã chốt — chỉ phương án người dùng chọn mới vào spec.
- Câu phụ thuộc câu trước thì hỏi sau: câu trả lời trước có thể làm câu sau thừa hoặc đổi tập phương án của nó.
- Người dùng không trả lời được câu nào → câu đó vào mục câu hỏi còn mở, không đoán.

Một dấu hiệu cần nhận ra: người dùng trả lời bằng **giải pháp** thay vì bằng **nhu cầu** — "thêm một nút Duyệt ở góc phải" thay vì "quản lý cần chặn đơn sai trước khi nó có hiệu lực". Hỏi ngược lại một lần: *việc đó để giải quyết chuyện gì?* Ghi lại nhu cầu; giải pháp là việc của `design-expert` và hai agent thi công.

### 4. Khuôn và phân vai

Khuôn spec nằm ở [`../../../spec/_template/`](../../../spec/_template/) — `business-rules.md` và `ui-spec.md`.

Feature có màn hình mới → còn cần `spec/<feature>/ui-spec.md`: nghiệp vụ của màn — dữ liệu nào hiển thị, thao tác nào có, điều kiện hiện theo nghiệp vụ — do `ba-analyst` viết cùng lượt. Spec thiết kế màn và component nằm ở khu Design ([`../../CLAUDE.md`](../../CLAUDE.md) §7), thuộc `design-expert`.

### 5. Gọi `ba-analyst` viết spec

Gọi `ba-analyst` qua `Agent` lượt viết. Prompt gồm: tên feature, kết luận phân loại ở bước 2, toàn bộ câu trả lời thu được ở bước 3, feature có màn mới hay không, và **đường dẫn** tới khuôn `spec/_template/` — không paste nguyên văn nội dung file nào.

Agent viết User Story, Acceptance Criteria, Business Rule theo khuôn và theo tiêu chí chất lượng khai trong chính nó.

> 📖 Tiêu chí AC, Business Rule và ranh giới giữa hai thứ: đọc `.claude/agents/ba-analyst.md`

### 6. Soát spec agent trả về

Soát AC và Business Rule theo tiêu chí ở `.claude/agents/ba-analyst.md` — mục *Chất lượng Acceptance Criteria* và *Business Rule khác Acceptance Criteria ở chỗ nào*. Không đạt thì trả lại `ba-analyst` viết lại, đừng tự sửa cho xuôi.

Nếu nghiệp vụ đòi một cách xử lý khác với quy tắc chung trong `docs/` — ví dụ một luồng lỗi riêng — thì **không ghi ngoại lệ đó vào spec**. Quy tắc trong `docs/` thắng; báo mâu thuẫn và chuyển cho `architect`.

### 7. Đánh dấu câu hỏi còn mở

File spec kết thúc bằng một mục câu hỏi còn mở. Mỗi câu ghi: hỏi ai, chặn phần nào của feature, và **hệ quả nếu đoán sai**.

Câu hỏi chặn cả feature thì nói rõ — `backend-expert` và `frontend-expert` cần biết mình đang xây trên nền chưa chốt.

## Dừng lại và hỏi khi

1. **Không chắc feature thuộc Core hay Module.** Dừng ở bước 2, hỏi. Đây là quyết định ranh giới kiến trúc.
2. **Yêu cầu thô mâu thuẫn nội bộ** — hai câu trong cùng mô tả nói ngược nhau. Báo mâu thuẫn, đừng tự chọn một bên.
3. **Yêu cầu đụng tới một quy tắc trong `docs/`** — ví dụ nghiệp vụ đòi một cách xử lý lỗi khác quy ước chung. Quy tắc trong `docs/` thắng; báo người dùng và chuyển cho `architect`.
4. **Đã có `spec/<feature>/business-rules.md`.** Không ghi đè, hỏi trước.
5. **Người dùng muốn bỏ qua spec để "code trước cho nhanh".** Nói rõ hệ quả: hai agent sẽ suy diễn hai nghiệp vụ khác nhau, và sai lệch chỉ lộ ra khi ghép.
6. **Cần lệnh git ghi** — xem [`../../CLAUDE.md`](../../CLAUDE.md) §1.

## Đầu ra

- Một file `spec/<feature>/business-rules.md`, đủ ba khoá frontmatter theo [`../../CLAUDE.md`](../../CLAUDE.md) §9, gồm ba khối User Story · Acceptance Criteria · Business Rule, cộng mục câu hỏi còn mở.
- Feature có màn mới: thêm `spec/<feature>/ui-spec.md`.
- Báo cáo trong hội thoại: kết luận phân loại Core/Module, đường dẫn file đã tạo, số câu hỏi còn mở và câu nào đang chặn.

Bàn giao: đưa **đường dẫn** file cho `backend-expert`/`frontend-expert`, không paste nguyên văn. Nếu màn mới cần component hay spec thiết kế chưa có trong khu Design, nói rõ bước tiếp theo là `design-expert`.

## Trước khi coi là xong

1. Đã phân loại Core/Module dứt khoát, hoặc đã dừng lại hỏi.
2. AC và Business Rule qua được tiêu chí ở `.claude/agents/ba-analyst.md`.
3. Mọi AC nhắc tới luật đều trỏ đúng mã Business Rule.
4. Mục câu hỏi còn mở có mặt — kể cả khi rỗng, ghi rõ là rỗng.
5. Không có chỗ nào suy diễn nghiệp vụ mà người dùng chưa xác nhận.
6. Chạy `bash .claude/check-docs.sh`.
