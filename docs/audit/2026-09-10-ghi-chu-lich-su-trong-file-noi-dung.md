---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 2026-09-10 — Ghi chú lịch sử tích tụ trong file nội dung

## 1. Hiện tượng

File luật và file tham chiếu mang những đoạn *"Bản trước của mục này…"*, *"LẬT <ngày>"*, *"SỬA <ngày> — câu này từng có một lỗ"*: kể lại bản sai trước khi nói luật hiện hành. Có cả trong `.claude/CLAUDE.md` và trong comment của một khối SQL mẫu. Người đọc — và agent — phải đọc qua bản sai để tới bản đúng.

## 2. Nguyên nhân gốc

Mỗi lượt vá một chỗ lệch để lại một câu giải thích **ngay tại chỗ vá**. Câu đó có ích đúng một lần — cho người review lượt vá — rồi nằm lại mãi, vì không có gì phân biệt nó với luật. [`../adr/0010-comment-toi-thieu.md`](../adr/0010-comment-toi-thieu.md) cấm kể lịch sử trong code; chưa luật nào nói điều tương tự cho file tài liệu, và không cổng nào dò.

## 3. Cách vá

Mọi đoạn loại này trong file đang có hiệu lực được gỡ phần kể chuyện, giữ phần luật. Chỗ nào bài học nằm ở **hậu quả** thì hậu quả được viết lại dạng điều kiện (*"viết thiếu … thì …"*) — luật vẫn nói được vì sao mà không cần kể bản trước.

Bài học của các đoạn đã gỡ, theo cơ chế:

| Cơ chế | Luật đang giữ nó |
| --- | --- |
| Hai bản của cùng một định nghĩa (hình dạng envelope, bảng `ErrorType`, bảng seam, khoá quyền, `CoreDbContext`, allowlist bọc UI, hàm gắn lỗi form, chuỗi interceptor) lệch nhau vì người sửa chỉ sửa một bản | [`../OWNERSHIP.md`](../OWNERSHIP.md) và cổng §15 |
| Chuỗi đường dẫn gõ tay ở hai chỗ thiếu đoạn phiên bản → điều kiện rate limit không bao giờ đúng, hàng rào không chặn gì | [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §8.1 — một hằng số dùng chung |
| Câu kiểm lọc theo **tên** index nên index đặt tên lệch quy ước vô hình | [`../database/schema-core.md`](../database/schema-core.md) — lọc theo thuộc tính |
| Phép tự kiểm so **số lần khớp** thay vì so tập, bằng nhau do trùng số | [`../RULES.md`](../RULES.md) §8 |
| Ánh xạ rule ESLint → luật theo tên "trông có vẻ liên quan" | [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) |
| Cột "ép bằng gì" trỏ vào tên section của script ở repo khác | [`../RULES.md`](../RULES.md) §7 |
| Deny chỉ chặn một phần dạng ghi của `git remote` trong khi tài liệu khai cả lệnh bị cấm | [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §1 — dòng khai là đầu vào cổng |
| Miễn trừ tài nguyên tĩnh bằng danh sách tiền tố trong interceptor — thêm thư mục tĩnh mà quên cập nhật là hỏng im lặng | [`../quy-uoc/fe-api-client.md`](../quy-uoc/fe-api-client.md) §2.1 — tải tĩnh qua `HttpBackend` |

## 4. Cổng nào lẽ ra phải bắt

Một mục cổng dò các dấu hiệu kể lịch sử (*"Bản trước"*, *"LẬT <ngày>"*, *"SỬA <ngày>"*) trong `docs/` và `.claude/`, trừ `docs/audit/`, `docs/adr/` và file `kind: lich-su`; FAIL khi khớp. Theo luật T6, mục đó phải khẳng định tập file quét khác rỗng.

Đã dựng: `check-docs.sh` §17, luật D24 ở [`../RULES.md`](../RULES.md). Kèm một dòng quy trình ở bảng *Chiều cập nhật* của [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §3 — lớp chặn gốc, vì cổng chỉ bắt cụm đã biết.

## 5. Bài học tổng quát

Lý do một luật tồn tại thuộc về luật; **câu chuyện luật ra đời thế nào** thuộc về audit. Viết câu chuyện cạnh luật thì mỗi lượt vá làm file dài thêm, và bản sai được đọc lại ở mọi lượt đọc.
