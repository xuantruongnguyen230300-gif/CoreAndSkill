---
name: "audit-log"
description: "Ghi một mục postmortem vào docs/audit/ theo khuôn bốn phần Hiện tượng / Nguyên nhân gốc / Cách vá / Cổng nào lẽ ra phải bắt. Dùng khi một sự cố hoặc một lỗi tài liệu vừa được phát hiện và xử lý."
argument-hint: "<mô tả sự cố ngắn> - vd 'bảng định tuyến trỏ vào file đã đổi tên, agent đọc nhầm'"
---

## User Input

```text
$ARGUMENTS
```

Bạn **BẮT BUỘC** phải xem xét user input trước khi tiếp tục (nếu không rỗng).

## Mục tiêu

Biến một sự cố đã xảy ra thành **thiết kế cổng ở phần 4** — chỉ dựng khi vi phạm lặp lại —, thay vì thành một câu chuyện được kể lại rồi quên.

> 📖 Khuôn bốn phần và vì sao phần 4 có giá trị nhất: đọc `docs/audit/README.md` §3
> 📖 Văn hoá không đổ lỗi: đọc `docs/audit/README.md` §6

## Các bước thực hiện

### 1. Đọc khuôn của khu

Mở [`../../../docs/audit/README.md`](../../../docs/audit/README.md) lấy khuôn, quy tắc đặt tên file và mục lục. **Không chép nội dung khuôn vào file này** — nó có thể đổi.

Đọc một mục có sẵn trong khu để thấy độ sâu mong đợi — danh sách lấy bằng lệnh:

```bash
ls docs/audit/2*.md
```

### 2. Kiểm tra đây có thuộc khu audit không

Đối chiếu README §2. Quyết định kiến trúc và lý do chọn → skill `adr-new`.

Sự cố **chưa xảy ra**, mới chỉ là rủi ro dự đoán → không viết audit. Audit là bản ghi thực tế; điền nó bằng giả định làm mất giá trị của cả khu.

### 3. Thu thập hiện tượng — quan sát được, không diễn giải

Hỏi người dùng cho tới khi trả lời được:

- Triệu chứng **quan sát được** là gì? Không viết "hệ thống bị lỗi" mà viết cái người ta thật sự thấy.
- Phát hiện ra bằng cách nào — tình cờ, hay có thứ gì báo?
- Phát hiện ngày nào, và kéo dài bao lâu trước khi bị phát hiện? Con số thứ hai đo **thời gian mù**, và thời gian mù dài là dấu hiệu thiếu cổng chứ không phải thiếu cẩn thận.

Bằng chứng trong repo thì neo theo [`../../CLAUDE.md`](../../CLAUDE.md) §9, sau khi đã mở đúng tệp đó ra đọc.

### 4. Đào nguyên nhân gốc — dừng ở cơ chế, không dừng ở hành vi

Hỏi "vì sao" cho tới khi câu trả lời là một **thuộc tính của hệ thống**, không phải một hành động của người — bẫy của phần 2 ở README §3, phép thử cơ chế ở README §8.

> 📖 Bốn khuôn nguyên nhân đã trả giá thật, để đối chiếu: đọc `.claude/README.md` mục *Vì sao*, phần §3

### 5. Ghi cách vá

Đã sửa gì, ở đâu. Nếu bản vá mới chỉ vá triệu chứng chứ chưa chạm nguyên nhân gốc, **nói thẳng điều đó** — một bản vá được mô tả như thể đã giải quyết tận gốc sẽ khiến không ai quay lại.

### 6. Phần 4 — cổng nào lẽ ra phải bắt

Ba khả năng, không có khả năng thứ tư:

**(a) Có cổng, nhưng cổng không chạy.** Ghi rõ cổng nào, vì sao không chạy — script không tồn tại, bị prompt của harness chặn, hay bị bỏ qua. Vì sao cổng bị prompt chặn tính là cổng không chạy: `.claude/README.md` mục *Vì sao*, phần §8.

**(b) Có cổng, cổng chạy, nhưng không phủ ca này.** Ghi rõ ca bị hở, và đề xuất mở rộng cổng.

**(c) Không cổng nào bắt được.** Đây là ca hay gặp nhất, và **không được dừng ở đó**. Phải làm một trong hai:

- Đề xuất một **cổng mới** — mô tả nó kiểm gì, chạy bằng lệnh nào, tiêu chí PASS là gì. Nếu cổng đó khả thi bằng máy, nói rõ nó thuộc `check-docs.sh` hay thuộc một skill.
- Nếu chưa nghĩ ra cổng khả thi, ghi rõ luật chỉ review canh — ô trạng thái của dòng luật ở `docs/RULES-*.md` nói điều đó; không mở việc dựng cổng khi chưa có vi phạm lặp lại (ADR-0114).

Đề xuất một dòng luật hay đổi ô trạng thái ở tệp `docs/RULES-*.md` thì giao `architect` ghi, kèm đường dẫn mục audit — `docs/adr/0111-git-danh-sach-cho-phep-cong-docs-chan-mot-lan-architect-truoc-thi-cong-theo-dau-hieu.md` quyết định 1.

🛑 Không được để phần 4 trống, và không được viết "cần cẩn thận hơn". Cẩn thận không phải là cổng.

### 7. Viết file và cập nhật mục lục

Tên file theo README §4 — ngày trong tên là ngày **phát hiện** sự cố (bước 3), không phải ngày viết; chưa rõ thì hỏi. Ba khoá frontmatter theo [`../../CLAUDE.md`](../../CLAUDE.md) §9. Thêm một dòng vào mục lục ở README §9.

## Dừng lại và hỏi khi

1. **Phần 4 chưa có câu trả lời.** Không ghi file với phần 4 trống — file như thế chỉ là một câu chuyện.
2. **Nguyên nhân gốc dừng ở hành vi của người.** Đào tiếp, hoặc hỏi người dùng thêm bối cảnh.
3. **Sự cố chưa thật sự xảy ra.** Đây là rủi ro dự đoán, không phải audit — hỏi người dùng muốn ghi ở đâu.
4. **Bản vá chưa được xác nhận là có tác dụng.** Ghi rõ trạng thái đó trong file, đừng viết như thể đã xong.
5. **Phần 4 dẫn tới việc phải sửa một cổng đang chạy.** Sửa cổng là việc riêng — báo người dùng, đừng gộp vào lượt này.
6. **Cần lệnh git ghi** — xem [`../../CLAUDE.md`](../../CLAUDE.md) §1.

## Đầu ra

- Một file mới `docs/audit/YYYY-MM-DD-<slug>.md` đủ bốn phần, đủ ba khoá frontmatter.
- Một dòng thêm vào mục lục của khu audit.
- Nếu phần 4 rơi vào ca (c): hoặc một đề xuất cổng cụ thể, hoặc ghi rõ luật chỉ review canh ở ô trạng thái — giao `architect`.

Báo cáo trong hội thoại: file đã tạo, kết luận của phần 4, và cổng nào được đề xuất thêm.

## Trước khi coi là xong

1. Bốn phần đều có nội dung; phần 4 không trống và không phải câu "cần cẩn thận hơn".
2. Không có tên người trong file.
3. Mọi neo đúng [`../../CLAUDE.md`](../../CLAUDE.md) §9.
4. Mục lục của khu audit đã cập nhật.
5. Chạy `bash .claude/check-docs.sh`.
