---
name: "ba-story"
description: "Chuyển một yêu cầu thô thành spec nghiệp vụ có kiểm chứng được: chạy ba-analyst để viết spec/<feature>/business-rules.md gồm User Story, Acceptance Criteria và Business Rule. Dùng trước khi bất kỳ ai viết code cho một feature Module."
argument-hint: "<tên feature> <mô tả yêu cầu thô> - vd 'DonNghiPhep - nhân viên xin nghỉ, quản lý duyệt'"
metadata:
  author: "core-team"
  source: "custom"
user-invocable: true
disable-model-invocation: false
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

### 3. Làm rõ yêu cầu — hỏi cho tới khi hết mơ hồ

Yêu cầu thô gần như luôn thiếu bốn thứ sau. Hỏi từng thứ:

| Hỏi gì | Thiếu thì hỏng ra sao |
| --- | --- |
| **Ai là actor**, và actor đó cần quyền gì | Không phân quyền được; FE dựng màn hình cho sai người dùng |
| **Trạng thái nào tồn tại** và chuyển giữa chúng bằng hành động nào | Mỗi agent tự đặt ra một tập trạng thái khác nhau |
| **Điều gì bị cấm** — không chỉ điều gì được phép | Luật cấm không viết ra thì không ai test, và nó chỉ lộ ra trên môi trường thật |
| **Chuyện gì xảy ra ở ca xấu** — trùng, hết hạn, thiếu quyền, dữ liệu rỗng | Ca xấu chiếm phần lớn khối lượng code nhưng thường không có trong yêu cầu thô |

Hỏi thành **một đợt**, không hỏi lắt nhắt từng câu một. Người dùng trả lời được bao nhiêu thì ghi bấy nhiêu; phần còn lại vào mục câu hỏi còn mở.

Một dấu hiệu cần nhận ra: người dùng trả lời bằng **giải pháp** thay vì bằng **nhu cầu** — "thêm một nút Duyệt ở góc phải" thay vì "quản lý cần chặn đơn sai trước khi nó có hiệu lực". Hỏi ngược lại một lần: *việc đó để giải quyết chuyện gì?* Ghi lại nhu cầu; giải pháp là việc của `design-expert` và hai agent thi công.

### 4. Đọc khuôn

Khuôn spec nằm ở [`../../../spec/_template/`](../../../spec/_template/) — `business-rules.md` và `ui-spec.md`. Mở khuôn tương ứng và giữ nguyên thứ tự mục cùng tên khoá frontmatter của nó.

Feature có màn hình mới → còn cần `spec/<feature>/ui-spec.md`. Nguồn giao diện **duy nhất** là khu Design ([`../../CLAUDE.md`](../../CLAUDE.md) §7); việc dựng screen spec thuộc `design-expert`, không thuộc skill này. Skill này chỉ ghi nghiệp vụ.

### 5. Gọi `ba-analyst`

Gọi `ba-analyst` qua `Agent`. Prompt gồm: tên feature, kết luận phân loại ở bước 2, toàn bộ câu trả lời thu được ở bước 3, và **đường dẫn** tới khuôn cùng tới file tiêu chí — không paste nguyên văn nội dung file nào.

Yêu cầu agent trả ra ba khối, theo đúng thứ tự:

**User Story** — mỗi story một câu, nêu actor, việc muốn làm, và **giá trị nhận được**. Story không nêu được giá trị thì hoặc nó là một bước kỹ thuật (thuộc task, không thuộc story), hoặc chưa ai biết vì sao làm nó.

**Acceptance Criteria** — điều kiện nghiệm thu của từng story.

**Business Rule** — luật nghiệp vụ đứng độc lập với story: ràng buộc dữ liệu, luật chuyển trạng thái, luật quyền, luật ở ca xấu. Mỗi luật có một mã ngắn để AC và code cùng trỏ tới.

### 6. Cổng chất lượng của AC — phần dễ làm dối nhất

Mỗi AC phải **kiểm chứng được đúng hay sai**, và neo vào **hành vi quan sát được**. Đó là toàn bộ giá trị của nó: một AC không kiểm chứng được thì `test-engineer` không viết được ca test từ nó, và người nghiệm thu sẽ cãi nhau về nghĩa của từ.

| AC | Đạt? | Vì sao |
| --- | --- | --- |
| "Hệ thống hoạt động ổn định" | ❌ | Không có phép thử nào cho ra đúng/sai |
| "Giao diện thân thiện, dễ dùng" | ❌ | Chủ quan, hai người chấm ra hai kết quả |
| "Xử lý nhanh" | ❌ | Nhanh là bao nhiêu, đo ở đâu |
| "Đơn đã duyệt thì nút Sửa không còn bấm được" | ✅ | Quan sát được, một phép thử cho ra đúng/sai |
| "Xin nghỉ trùng khoảng ngày với đơn đang chờ duyệt thì bị từ chối, kèm thông điệp nêu mã đơn trùng" | ✅ | Nêu đủ input, kết quả, và dấu hiệu quan sát được |

Ba phép thử để tự soát một AC:

1. **Ai chấm cũng ra cùng kết quả?** Không → viết lại.
2. **Có input cụ thể nào làm nó sai không?** Không nghĩ ra được → AC đang nói điều hiển nhiên, bỏ đi.
3. **Dấu hiệu nào cho biết nó đúng?** Phải là thứ nhìn thấy hoặc đo được, không phải trạng thái nội bộ.

AC nào không qua ba phép thử này thì trả lại `ba-analyst` viết lại, đừng tự sửa cho xuôi.

### 7. Cổng chất lượng của Business Rule

AC gắn với một story; Business Rule đứng độc lập và **áp cho mọi story chạm tới nó**. Trộn hai thứ là lỗi hay gặp nhất khi viết spec: một luật bị nhét vào AC của một story sẽ biến mất khỏi tầm nhìn ngay khi story đó đóng.

Ba yêu cầu:

1. **Có mã ngắn, ổn định.** AC trỏ tới luật bằng mã, `test-engineer` trỏ tới luật bằng mã, và sau này code cũng vậy. Mã đã cấp thì không dùng lại cho luật khác — cùng lý do số ADR không dùng lại.
2. **Đứng một mình vẫn đọc hiểu được.** Một luật chỉ có nghĩa khi đọc kèm story của nó thì nó thuộc AC, không thuộc khối luật.
3. **Nói được ca vi phạm.** Luật không mô tả được chuyện gì xảy ra khi bị vi phạm thì chưa phải luật — nó là một mong muốn.

🛑 **Business Rule không chứa quyết định kỹ thuật.** "Lưu ở bảng nào", "gọi endpoint nào", "dùng kiểu dữ liệu gì" đều thuộc `docs/`, không thuộc `spec/`. Ranh giới này giữ cho spec còn dùng được khi kỹ thuật đổi — và giữ cho `docs/` vẫn là nguồn quy tắc duy nhất.

Ngược lại, nếu nghiệp vụ đòi một cách xử lý khác với quy tắc chung trong `docs/` — ví dụ một luồng lỗi riêng — thì **không ghi ngoại lệ đó vào spec**. Quy tắc trong `docs/` thắng; báo mâu thuẫn và chuyển cho `architect`.

### 8. Đánh dấu câu hỏi còn mở

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
- Báo cáo trong hội thoại: kết luận phân loại Core/Module, đường dẫn file đã tạo, số câu hỏi còn mở và câu nào đang chặn.

Bàn giao: đưa **đường dẫn** file cho `backend-expert`/`frontend-expert`, không paste nguyên văn. Nếu feature cần màn hình mới, nói rõ bước tiếp theo là `design-expert`.

## Trước khi coi là xong

1. Đã phân loại Core/Module dứt khoát, hoặc đã dừng lại hỏi.
2. Mọi AC qua được ba phép thử ở bước 6.
3. Mỗi Business Rule có mã ngắn, và mọi AC nhắc tới luật đều trỏ đúng mã.
4. Mục câu hỏi còn mở có mặt — kể cả khi rỗng, ghi rõ là rỗng.
5. Không có chỗ nào suy diễn nghiệp vụ mà người dùng chưa xác nhận.
6. Chạy `bash .claude/check-docs.sh`.
