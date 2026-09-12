---
name: "audit-log"
description: "Ghi một mục postmortem vào docs/audit/ theo khuôn bốn phần Hiện tượng / Nguyên nhân gốc / Cách vá / Cổng nào lẽ ra phải bắt. Dùng sau khi một sự cố hoặc một lỗi tài liệu vừa được phát hiện và xử lý."
argument-hint: "<mô tả sự cố ngắn> - vd 'bảng định tuyến trỏ vào file đã đổi tên, agent đọc nhầm'"
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

Biến một sự cố đã xảy ra thành **một cổng mới**, thay vì thành một câu chuyện được kể lại rồi quên.

Bốn phần của khuôn, và phần thứ tư là phần duy nhất tạo ra giá trị lâu dài:

| Phần | Trả lời | Ai dùng |
| --- | --- | --- |
| **1. Hiện tượng** | Ai thấy gì, lúc nào, sai ra sao | Người gặp lại triệu chứng giống thế |
| **2. Nguyên nhân gốc** | Vì sao hệ thống cho phép điều đó xảy ra | Người muốn hiểu, không chỉ muốn vá |
| **3. Cách vá** | Đã sửa gì | Người kiểm chứng bản vá |
| **4. Cổng nào lẽ ra phải bắt** | Cơ chế nào đã không có mặt | **Người viết cổng tiếp theo** |

> **Văn hoá không đổ lỗi.** Viết về hệ thống và cơ chế, không viết về người. "Ai làm sai" không dùng được vào việc gì; "cơ chế nào đã không có mặt" thì dùng được ngay. Một postmortem có tên người trong đó sẽ khiến lần sau không ai muốn viết postmortem.

## Các bước thực hiện

### 1. Đọc khuôn của khu

Mở [`../../../docs/audit/README.md`](../../../docs/audit/README.md) lấy khuôn, quy tắc đặt tên file và bảng mục lục. **Không chép nội dung khuôn vào file này** — nó có thể đổi.

Hai mục đã có sẵn trong khu, đọc một mục để thấy độ sâu mong đợi:

- [`../../../docs/audit/2026-08-23-cong-khong-ton-tai.md`](../../../docs/audit/2026-08-23-cong-khong-ton-tai.md) — một cổng không tồn tại trên đĩa nhưng tài liệu vẫn ghi bình thường.
- [`../../../docs/audit/2026-09-05-reflection-envelope.md`](../../../docs/audit/2026-09-05-reflection-envelope.md)

### 2. Kiểm tra đây có thuộc khu audit không

| Nội dung | Khu đúng |
| --- | --- |
| Một sự cố **đã xảy ra**, có triệu chứng quan sát được | `docs/audit/` — đúng khu, đi tiếp |
| Một quyết định kiến trúc và lý do chọn | [`../../../docs/adr/README.md`](../../../docs/adr/README.md) — dùng skill `adr-new` |
| Một luật cần được ép | [`../../../docs/RULES.md`](../../../docs/RULES.md) |
| Một quy ước thi công | Khu quy ước — xem [`../../../docs/README.md`](../../../docs/README.md) |

Sự cố **chưa xảy ra**, mới chỉ là rủi ro dự đoán → không viết audit. Audit là bản ghi thực tế; điền nó bằng giả định làm mất giá trị của cả khu.

### 3. Thu thập hiện tượng — quan sát được, không diễn giải

Hỏi người dùng cho tới khi trả lời được:

- Triệu chứng **quan sát được** là gì? Không viết "hệ thống bị lỗi" mà viết cái người ta thật sự thấy.
- Phát hiện ra bằng cách nào — tình cờ, hay có thứ gì báo?
- Kéo dài bao lâu trước khi bị phát hiện? Con số này quan trọng: nó đo **thời gian mù**, và thời gian mù dài là dấu hiệu thiếu cổng chứ không phải thiếu cẩn thận.

Nếu bằng chứng neo được vào file trong repo thì neo bằng đường dẫn. 🛑 Repo chưa có `src/` — không bịa trích dẫn dạng đường-dẫn-kèm-số-dòng vào `src/`.

### 4. Đào nguyên nhân gốc — dừng ở cơ chế, không dừng ở hành vi

Hỏi "vì sao" cho tới khi câu trả lời là một **thuộc tính của hệ thống**, không phải một hành động của người.

| Câu trả lời | Đủ sâu chưa |
| --- | --- |
| "Có người quên cập nhật bảng định tuyến" | ❌ Chưa — đây là hành vi |
| "Bảng định tuyến và file đích là hai nguồn, không có gì buộc chúng khớp nhau" | ✅ Đây là cơ chế |

Bốn khuôn nguyên nhân đã trả giá thật, nêu ở [`../../CLAUDE.md`](../../CLAUDE.md) §3 — đối chiếu xem sự cố này có rơi vào khuôn nào không: hai nguồn thì chúng lệch nhau · bản sao không bao giờ được sửa cùng lúc · agent không thấy conflict mà im lặng dùng bản sao · chép nội dung làm agent cạn context.

### 5. Ghi cách vá

Đã sửa gì, ở đâu. Nếu bản vá mới chỉ vá triệu chứng chứ chưa chạm nguyên nhân gốc, **nói thẳng điều đó** — một bản vá được mô tả như thể đã giải quyết tận gốc sẽ khiến không ai quay lại.

### 6. Phần 4 — cổng nào lẽ ra phải bắt

Đây là phần có giá trị nhất của cả file. Ba khả năng, không có khả năng thứ tư:

**(a) Có cổng, nhưng cổng không chạy.** Ghi rõ cổng nào, vì sao không chạy — script không tồn tại, bị prompt của harness chặn, hay bị bỏ qua. Một cổng bị prompt chặn là một cổng, trên thực tế, không ai chạy ([`../../CLAUDE.md`](../../CLAUDE.md) §8).

**(b) Có cổng, cổng chạy, nhưng không phủ ca này.** Ghi rõ ca bị hở, và đề xuất mở rộng cổng.

**(c) Không cổng nào bắt được.** Đây là ca hay gặp nhất, và **không được dừng ở đó**. Phải làm một trong hai:

- Đề xuất một **cổng mới** — mô tả nó kiểm gì, chạy bằng lệnh nào, tiêu chí PASS là gì. Nếu cổng đó khả thi bằng máy, nói rõ nó thuộc `check-docs.sh` hay thuộc một skill.
- Nếu chưa nghĩ ra cổng khả thi, ghi một dòng vào **mục Danh sách nợ** của [`../../../docs/RULES.md`](../../../docs/RULES.md), kèm cột "vì sao chưa có cổng" và "ý tưởng cổng tương lai".

🛑 Không được để phần 4 trống, và không được viết "cần cẩn thận hơn". Cẩn thận không phải là cổng.

### 7. Viết file và cập nhật mục lục

Tên file theo quy tắc ở bước 1: ngày, gạch ngang, slug tiếng Việt không dấu. Ngày lấy bằng lệnh:

```bash
date +%F
```

Ba khoá frontmatter theo [`../../CLAUDE.md`](../../CLAUDE.md) §9. Thêm một dòng vào bảng mục lục của khu audit.

## Dừng lại và hỏi khi

1. **Phần 4 chưa có câu trả lời.** Không ghi file với phần 4 trống — file như thế chỉ là một câu chuyện.
2. **Nguyên nhân gốc dừng ở hành vi của người.** Đào tiếp, hoặc hỏi người dùng thêm bối cảnh.
3. **Sự cố chưa thật sự xảy ra.** Đây là rủi ro dự đoán, không phải audit — hỏi người dùng muốn ghi ở đâu.
4. **Bản vá chưa được xác nhận là có tác dụng.** Ghi rõ trạng thái đó trong file, đừng viết như thể đã xong.
5. **Phần 4 dẫn tới việc phải sửa một cổng đang chạy.** Sửa cổng là việc riêng — báo người dùng, đừng gộp vào lượt này.
6. **Cần lệnh git ghi** — xem [`../../CLAUDE.md`](../../CLAUDE.md) §1. Nói rõ cần chạy lệnh gì, người dùng tự chạy.

## Đầu ra

- Một file mới `docs/audit/YYYY-MM-DD-<slug>.md` đủ bốn phần, đủ ba khoá frontmatter.
- Một dòng thêm vào bảng mục lục của khu audit.
- Nếu phần 4 rơi vào ca (c) không cổng nào bắt được: hoặc một đề xuất cổng cụ thể, hoặc một dòng đề xuất cho mục Danh sách nợ của file luật.

Báo cáo trong hội thoại: file đã tạo, kết luận của phần 4, và cổng nào được đề xuất thêm.

## Trước khi coi là xong

1. Bốn phần đều có nội dung; phần 4 không trống và không phải câu "cần cẩn thận hơn".
2. Không có tên người trong file.
3. Không có trích dẫn dạng đường-dẫn-kèm-số-dòng trỏ vào `src/`.
4. Bảng mục lục của khu audit đã cập nhật.
5. Chạy `bash .claude/check-docs.sh`.
