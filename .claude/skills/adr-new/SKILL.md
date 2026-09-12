---
name: "adr-new"
description: "Tạo một Architecture Decision Record mới đúng khuôn: lấy số tiếp theo bằng lệnh, hỏi đủ bối cảnh và phương án đã loại, viết file, cập nhật bảng mục lục. Dùng khi vừa chốt một quyết định kiến trúc đắt để đảo ngược."
argument-hint: "<quyết định, dạng câu khẳng định> - vd 'Dùng Outbox thay message broker ở v1'"
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

Ghi lại **lý do** của một quyết định kiến trúc tại thời điểm quyết định — thứ mất nhanh nhất. Sáu tháng sau, quy ước vẫn còn nằm trong file, nhưng những phương án đã cân nhắc và bị loại thì không ai nhớ, và người mới sẽ đề xuất lại đúng phương án đã bị bác với đúng lập luận đã bị bác.

> **Một ADR không nêu được nhược điểm là một ADR chưa suy nghĩ đủ.** Nếu người dùng không nêu được hệ quả tiêu cực nào, **hỏi lại** — đừng viết file.

Việc này thuộc `architect`. Skill chuẩn bị dữ liệu rồi giao, không tự quyết kiến trúc.

## Các bước thực hiện

### 1. Đọc khuôn và quy tắc — không nhớ bằng đầu

Mở [`../../../docs/adr/README.md`](../../../docs/adr/README.md). Bốn thứ lấy từ đó, **không chép sang file này**:

- Khuôn năm mục bắt buộc và bẫy thường gặp của từng mục.
- Quy tắc đánh số và đặt tên file.
- Cách lật một quyết định cũ.
- Bảng mục lục cần cập nhật ở bước 6.

📖 Thực hành ADR như một chuẩn chung: đọc [`../../../docs/wiki-core/be/08-adr-practice.md`](../../../docs/wiki-core/be/08-adr-practice.md)

### 2. Kiểm tra đây có thật sự là một ADR không

Mục "Khi nào KHÔNG viết" trong file mục lục ở bước 1 liệt kê bốn tình huống thuộc khu khác. Đối chiếu trước khi viết.

**Phép thử nhanh:** sáu tháng nữa có ai hỏi *"vì sao hồi đó lại làm thế?"* không? Không → đây không phải ADR, chuyển sang khu đúng của nó và dừng.

### 3. Lấy số tiếp theo bằng lệnh

🛑 **Không chép số từ trí nhớ hay từ bảng mục lục.** Bảng mục lục có thể lệch; đĩa thì không.

```bash
ls docs/adr/[0-9]*.md | tail -1
```

Số tiếp theo = số của file cuối cùng + 1, bốn chữ số. Số **không bao giờ dùng lại**, kể cả khi ADR mang số đó đã bị loại bỏ.

Nếu quyết định này **lật** một ADR cũ, tìm các quyết định đã bị lật trước đó để không lật nhầm:

```bash
grep -l 'Đã thay thế bởi' docs/adr/*.md
```

### 4. Hỏi đủ thông tin — bốn câu, không bỏ câu nào

`$ARGUMENTS` gần như luôn chỉ chứa **quyết định**, tức một trong năm mục. Bốn mục còn lại phải hỏi:

| Hỏi gì | Không có thì hỏng ra sao |
| --- | --- |
| **Bối cảnh lúc quyết định** — ràng buộc nào có thật, đang bị gì ép | Người đọc sau này tưởng quyết định là tuỳ hứng, và lật nó mà không biết ràng buộc còn đúng hay không |
| **Các phương án đã cân nhắc** — ít nhất hai | Chỉ một phương án thì đây không phải quyết định, nó là ràng buộc, và ràng buộc thì ghi ở [`../../../docs/RULES.md`](../../../docs/RULES.md) |
| **Vì sao loại từng phương án** | Người mới đề xuất lại đúng phương án đã bị bác |
| **Hệ quả tiêu cực** — cái giá thật sự phải trả | Xem cảnh báo dưới đây |

🛑 **Cổng của skill này: hệ quả tiêu cực.** Nếu người dùng trả lời "không có nhược điểm gì" hoặc đưa một nhược điểm trá hình dạng *"hơi tốn công lúc đầu nhưng về sau rất đáng"* — **dừng lại, hỏi lại**. Hệ quả tiêu cực phải là thứ ta thật sự mất và không lấy lại được bằng nỗ lực.

Cũng cảnh giác với **bù nhìn**: phương án bị loại được mô tả yếu đi để phương án đã chọn thắng dễ. Nếu lý do loại một phương án nghe quá gọn, hỏi lại người dùng phương án đó thật sự mạnh ở điểm nào.

### 5. Viết file

Tên file theo quy tắc ở bước 1: bốn chữ số, gạch ngang, slug tiếng Việt không dấu **mô tả quyết định chứ không mô tả chủ đề**. Slug nói chủ đề thì không phân biệt được với một ADR khác cùng chủ đề.

Nội dung: chép **khuôn rỗng** ở cuối file mục lục, điền đủ năm mục. Ba khoá frontmatter: `kind: quyet-dinh`, `scope: core`, `verified: chua-doi-chieu`.

Hai ràng buộc về bằng chứng ở giai đoạn 1:

- Repo **chưa có `src/`** — không trích dẫn dạng đường-dẫn-kèm-số-dòng vào `src/`. Cổng sẽ bắt, và đó là bịa bằng chứng.
- Bài học từ dự án tiền nhiệm kể bằng **văn xuôi** ("ở dự án tiền nhiệm…"), không viết dạng đường-dẫn-kèm-số-dòng.

Không chép nội dung của một file quy ước vào ADR. ADR nói *vì sao*, khu quy ước nói *làm thế nào*. Chép sang là tạo bản sao thứ hai, và bản sao thứ hai không bao giờ được sửa cùng lúc với bản gốc.

### 6. Cập nhật bảng mục lục

Thêm **một dòng** vào bảng mục lục trong [`../../../docs/adr/README.md`](../../../docs/adr/README.md): link tới file mới, cộng quyết định gói trong một dòng.

Nếu ADR mới **lật** một ADR cũ: sửa **duy nhất dòng Trạng thái** của ADR cũ, kèm một dòng link sang bản mới. 🛑 Không đụng vào phần nội dung của ADR cũ — sửa nó là xoá mất lý do người ta từng nghĩ thế.

### 7. Nếu ADR sinh ra một luật mới

Một ADR **không tự nó ép được gì**. Phần ép nằm ở [`../../../docs/RULES.md`](../../../docs/RULES.md), cột "Ép bằng gì".

Hỏi người dùng: quyết định này có sinh ra luật mà code phải tuân không? Có → luật đó phải xuất hiện ở file luật kèm cột "Ép bằng gì". Chưa có cổng nào ép được → dòng đó thuộc mục Danh sách nợ, nhìn thấy được, không lờ đi.

### 8. Giao cho `architect`

Gọi `architect` qua `Agent` để soát lập luận trước khi coi là chốt — đặc biệt mục "Phương án đã cân nhắc" và "Hệ quả tiêu cực". Gửi **đường dẫn** file vừa viết, không paste nguyên văn.

## Dừng lại và hỏi khi

1. **Người dùng không nêu được hệ quả tiêu cực nào.** Đây là cổng cứng của skill — không viết file.
2. **Chỉ có một phương án.** Đây không phải quyết định mà là ràng buộc — nó thuộc file luật, không thuộc khu ADR.
3. **Quyết định thuộc khu khác** theo bảng "Khi nào KHÔNG viết" ở bước 1 — quy ước đặt tên, kể lại sự cố, kế hoạch.
4. **Không rõ ADR này có lật một ADR cũ hay không.** Lật nhầm tạo ra hai quyết định sống song song nói ngược nhau.
5. **Trạng thái của một ADR cũ cần đổi.** Nói rõ dòng nào cần đổi thành gì, rồi hỏi trước khi sửa — file cũ là bản ghi lịch sử.
6. **Cần lệnh git ghi** — xem [`../../CLAUDE.md`](../../CLAUDE.md) §1. Nói rõ cần chạy lệnh gì, người dùng tự chạy.

## Đầu ra

- Một file mới `docs/adr/NNNN-<slug>.md` đủ năm mục, đủ ba khoá frontmatter.
- Một dòng thêm vào bảng mục lục của khu ADR.
- Nếu có lật: đúng **một** dòng Trạng thái của ADR cũ được sửa.
- Nếu ADR sinh luật: một dòng đề xuất cho file luật, kèm cột "Ép bằng gì" hoặc một dòng nợ.

Báo cáo trong hội thoại: số ADR đã cấp, file đã tạo, những chỗ người dùng còn để trống, và câu hỏi còn mở.

## Trước khi coi là xong

1. Số ADR lấy bằng lệnh ở bước 3, không lấy từ trí nhớ.
2. Mục "Phương án đã cân nhắc" có **từ hai phương án trở lên**, mỗi phương án nói rõ vì sao loại.
3. Mục "Hệ quả tiêu cực" có nội dung thật, không phải lợi ích trá hình.
4. Dòng Trạng thái có ngày.
5. Bảng mục lục đã cập nhật.
6. Chạy `bash .claude/check-docs.sh`.
