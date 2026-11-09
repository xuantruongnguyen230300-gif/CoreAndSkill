---
name: "adr-new"
description: "Tạo một Architecture Decision Record mới đúng khuôn: lấy số tiếp theo bằng lệnh, hỏi đủ bối cảnh và phương án đã loại, viết file, thêm một dòng vào mục lục. Dùng khi vừa chốt một quyết định mang dấu hiệu ADR ở docs/adr/README.md §2 — quyết định nhỏ ghi một dòng ở docs/adr/nhat-ky-quyet-dinh.md, không dùng skill này."
argument-hint: "<quyết định, dạng câu khẳng định> - vd 'Dùng Outbox thay message broker ở v1'"
---

## User Input

```text
$ARGUMENTS
```

Bạn **BẮT BUỘC** phải xem xét user input trước khi tiếp tục (nếu không rỗng).

## Mục tiêu

Ghi lại **lý do** của một quyết định kiến trúc tại thời điểm quyết định — thứ mất nhanh nhất. Sáu tháng sau, quy ước vẫn còn nằm trong file, nhưng những phương án đã cân nhắc và bị loại thì không ai nhớ, và người mới sẽ đề xuất lại đúng phương án đã bị bác với đúng lập luận đã bị bác.

> **Một ADR không nêu được nhược điểm là một ADR chưa suy nghĩ đủ.** Nếu người dùng không nêu được hệ quả tiêu cực nào, **hỏi lại** — đừng viết file.

Việc này thuộc `architect`. Phiên chính chạy bước 1–5 — chuẩn bị dữ liệu, hỏi người dùng — rồi giao; `architect` ghi tệp ADR, mục lục và mọi dòng đổi kèm ở bước 6. Phiên chính không tự viết tệp ADR, không tự quyết kiến trúc.

## Các bước thực hiện

### 1. Đọc khuôn và quy tắc — không nhớ bằng đầu

Mở [`../../../docs/adr/README.md`](../../../docs/adr/README.md). Mọi thứ dưới đây lấy từ đó, **không chép sang file này**:

- §2 — khi nào viết ADR, và mục *Khi nào KHÔNG viết*.
- §3 — khuôn năm mục và bẫy thường gặp của từng mục.
- §4 — đánh số, đặt tên file, lệnh lấy số và lệnh tìm ADR đã bị lật.
- §5, §5.1 — cách lật hoặc sửa một phần một quyết định cũ.
- §7 — khuôn rỗng để chép và ba lỗi hay gặp khi điền.

Mục lục là [`../../../docs/adr/muc-luc.md`](../../../docs/adr/muc-luc.md) — §6 của README chỉ còn một dòng trỏ sang đó.

📖 Thực hành ADR như một chuẩn chung: đọc [`../../../docs/wiki-core/be/08-adr-practice.md`](../../../docs/wiki-core/be/08-adr-practice.md)

### 2. Kiểm tra đây có thật sự là một ADR không

Đối chiếu dấu hiệu và mục *Khi nào KHÔNG viết* ở README §2. Không mang dấu hiệu nào → quyết định nhỏ: một dòng ở [`../../../docs/adr/nhat-ky-quyet-dinh.md`](../../../docs/adr/nhat-ky-quyet-dinh.md), không viết ADR. Thuộc khu khác → chuyển sang khu đó và dừng.

**Phép thử nhanh:** sáu tháng nữa có ai hỏi *"vì sao hồi đó lại làm thế?"* không? Không → đây không phải ADR.

### 3. Lấy số tiếp theo bằng lệnh

🛑 **Không chép số từ trí nhớ hay từ mục lục.** Mục lục có thể lệch; đĩa thì không. Chạy lệnh ở README §4. Số **không bao giờ dùng lại**.

Nếu quyết định này **lật** hay chạm một ADR cũ, chạy thêm các lệnh tìm ADR đã bị thay thế, sửa một phần hay tạm hoãn ở cùng mục, để không lật nhầm.

### 4. Hỏi đủ thông tin — bốn câu, không bỏ câu nào

`$ARGUMENTS` gần như luôn chỉ chứa **quyết định**, tức một trong năm mục. Bốn mục còn lại phải hỏi:

| Hỏi gì | Không có thì hỏng ra sao |
| --- | --- |
| **Bối cảnh lúc quyết định** — ràng buộc nào có thật, đang bị gì ép | Người đọc sau này tưởng quyết định là tuỳ hứng, và lật nó mà không biết ràng buộc còn đúng hay không |
| **Các phương án đã cân nhắc** — ít nhất hai | Chỉ một phương án thì đây không phải quyết định, nó là ràng buộc — ghi ở các tệp `docs/RULES-*.md`, mục lục [`../../../docs/RULES.md`](../../../docs/RULES.md) |
| **Vì sao loại từng phương án** | Người mới đề xuất lại đúng phương án đã bị bác |
| **Hệ quả tiêu cực** — cái giá thật sự phải trả | Xem cảnh báo dưới đây |

🛑 **Cổng của skill này: hệ quả tiêu cực.** Nếu người dùng trả lời "không có nhược điểm gì" hoặc đưa một nhược điểm trá hình dạng *"hơi tốn công lúc đầu nhưng về sau rất đáng"* — **dừng lại, hỏi lại**. Hệ quả tiêu cực phải là thứ ta thật sự mất và không lấy lại được bằng nỗ lực.

Cũng cảnh giác với **bù nhìn**: phương án bị loại được mô tả yếu đi để phương án đã chọn thắng dễ. Nếu lý do loại một phương án nghe quá gọn, hỏi lại người dùng phương án đó thật sự mạnh ở điểm nào.

### 5. Nếu ADR sinh ra một luật mới

Một ADR **không tự nó ép được gì**. Phần ép nằm ở bảng luật — các tệp `docs/RULES-*.md`, mục lục [`../../../docs/RULES.md`](../../../docs/RULES.md) — cột "Ép bằng gì".

Hỏi người dùng: quyết định này có sinh ra luật mà code phải tuân không? Có → luật đó phải xuất hiện ở tệp `RULES-*` kèm cột "Ép bằng gì". Chưa có cổng nào ép được → ô trạng thái của dòng luật nói rõ và ô *Ép bằng gì* nêu ai canh, nhìn thấy được, không lờ đi. Ghi dòng luật là việc của `architect`, ở bước 6.

### 6. Giao `architect` ghi tệp

Gọi `architect` qua `Agent`. Lời giao gồm: quyết định, câu trả lời của người dùng ở bước 4–5 giữ nguyên lời, số đã lấy ở bước 3, và **đường dẫn** ADR cũ bị chạm nếu có — không paste nội dung ADR cũ. Lời giao nêu đủ việc `architect` làm trong lượt đó:

- **Tệp ADR** — tên theo README §4; chép **khuôn rỗng** ở README §7 — đã gồm ba khoá frontmatter — rồi điền đủ năm mục, tránh ba lỗi hay gặp nêu ngay dưới khuôn. Neo tới `src/` theo [`../../CLAUDE.md`](../../CLAUDE.md) §9, và chỉ sau khi đã mở đúng tệp đó ra đọc.
- **Mục lục** — thêm **một dòng** vào bảng ở [`../../../docs/adr/muc-luc.md`](../../../docs/adr/muc-luc.md): link tới file mới, cộng quyết định gói trong một dòng. Không ghi trạng thái hay ngày vào mục lục — README §6.
- **ADR cũ** bị lật, sửa một phần, bổ sung hay hoãn — chỉ sửa **dòng Trạng thái** theo README §5.1, sau khi người dùng đã đồng ý (*Dừng lại* mục 5). 🛑 Không đụng vào phần nội dung của ADR cũ.
- **Luật mới** khi bước 5 trả lời có — dòng ở tệp `RULES-*` kèm cột "Ép bằng gì" và ô trạng thái.
- **Soát lập luận** trước khi coi là chốt — đặc biệt mục "Phương án đã cân nhắc" và "Hệ quả tiêu cực".

`architect` trả lại câu hỏi hay từ chối → hỏi người dùng rồi giao lại; phiên chính không tự sửa tệp ADR.

## Dừng lại và hỏi khi

1. **Người dùng không nêu được hệ quả tiêu cực nào.** Đây là cổng cứng của skill — không viết file.
2. **Chỉ có một phương án.** Đây không phải quyết định mà là ràng buộc — nó thuộc tệp luật, không thuộc khu ADR.
3. **Quyết định thuộc khu khác** theo mục *Khi nào KHÔNG viết* ở README §2 — quyết định nhỏ, quy ước đặt tên, kể lại sự cố, kế hoạch.
4. **Không rõ ADR này có lật một ADR cũ hay không.** Lật nhầm tạo ra hai quyết định sống song song nói ngược nhau.
5. **Trạng thái của một ADR cũ cần đổi.** Nói rõ dòng nào cần đổi thành gì, rồi hỏi trước khi sửa — file cũ là bản ghi lịch sử.
6. **Cần lệnh git ghi** — xem [`../../CLAUDE.md`](../../CLAUDE.md) §1.

## Đầu ra

Do `architect` ghi ở bước 6:

- Một file mới `docs/adr/NNNN-<slug>.md` theo khuôn README §7, đủ năm mục.
- Một dòng thêm vào `docs/adr/muc-luc.md`.
- Nếu chạm ADR cũ: đúng **một** dòng Trạng thái của ADR cũ được sửa.
- Nếu ADR sinh luật: một dòng ở tệp `RULES-*`, kèm cột "Ép bằng gì" và ô trạng thái.

Báo cáo trong hội thoại: số ADR đã cấp, file đã tạo, những chỗ người dùng còn để trống, và câu hỏi còn mở.

## Trước khi coi là xong

1. Số ADR lấy bằng lệnh ở README §4, không lấy từ trí nhớ.
2. Mục "Phương án đã cân nhắc" có **từ hai phương án trở lên**, mỗi phương án nói rõ vì sao loại.
3. Mục "Hệ quả tiêu cực" có nội dung thật, không phải lợi ích trá hình.
4. Dòng Trạng thái có ngày.
5. Mục lục đã có dòng mới, không mang trạng thái.
6. Chạy `bash .claude/check-docs.sh`.
