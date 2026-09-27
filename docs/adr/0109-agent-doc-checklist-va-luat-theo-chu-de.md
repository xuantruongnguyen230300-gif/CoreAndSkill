---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0109 — Agent đọc checklist luôn đọc cộng luật theo chủ đề; model ghim theo vai; review giới hạn phần đổi và hai vòng; docs chỉ ghi khi được giao

> **Trạng thái:** Đã chấp nhận (2026-09-27)

## Bối cảnh

Đây là giai đoạn 2–5 của đợt refactor `.claude`/`docs`; giai đoạn 1 ở [ADR-0108](0108-hook-stop-hoan-khi-cho-agent-nen-docs-khong-keo-review.md). Số đo phiên thi công 16–26/09:

- `core-reviewer` chạy 82 lượt, 80% không sạch. Phạm vi BE lên tới vòng 7.
- Người soát đọc gấp đôi lượng luật người viết đọc. Nên người soát bắt lỗi theo những luật người viết chưa từng thấy.
- Mọi agent đều chạy theo model của phiên chính.
- Số ADR tăng từ 35 lên 106 trong chín ngày. `architect` ghi docs hơn một nghìn lần.

Phân tích từng tệp luật ngày 2026-09-27: phần lớn nội dung là luật thật, khớp code. Cắt chữ an toàn chỉ bớt 4–16% mỗi tệp. Muốn nhanh hơn thật thì phải đổi **cách đọc**.

## Quyết định

Mọi quyết định trong bảng do người dùng chốt ngày 2026-09-27.

| # | Quyết định |
| --- | --- |
| 1 | Ghim model theo vai. `core-reviewer` và `architect` dùng Opus; các agent viết code, test, spec, tài liệu bàn giao dùng Sonnet. Một lượt khó thì phiên chính nâng model cho riêng lượt đó |
| 2 | Mỗi lượt giao agent một việc nhỏ, khoảng 30–40 phút. Chờ agent nền bằng thông báo, không ngủ chờ |
| 3 | `core-reviewer` mặc định soát phần đổi sau lượt review gần nhất cùng code nó gọi trực tiếp. Soát toàn bộ khi người dùng yêu cầu, hoặc trước khi chốt một giai đoạn |
| 4 | Mỗi phạm vi tối đa hai vòng review → sửa. Finding 🔴 thì sửa rồi review lại; từ 🟠 trở xuống thì ghi nợ. Hết vòng hai mà còn 🔴 thì hỏi người dùng |
| 5 | `architect` và `design-expert` chỉ ghi `docs/` khi được giao một việc cụ thể |
| 6 | Quyết định nhỏ ghi một dòng ở [`nhat-ky-quyet-dinh.md`](nhat-ky-quyet-dinh.md). ADR chỉ cho các dấu hiệu ở [`README.md`](README.md) §2 |
| 7 | **Checklist luôn đọc + luật theo chủ đề.** `RULES.md` tách thành `RULES-BE.md`, `RULES-FE.md`, `RULES-DOCS.md`, `RULES-CHUNG.md`; `RULES.md` còn là mục lục, giữ số §. Người viết và người soát cùng đọc checklist của phạm vi. Tệp quy ước chỉ mở khi việc chạm đúng chủ đề — người soát mở đủ tệp của mọi chủ đề trong phần đổi |
| 8 | Phần chưa thi công của một tệp luật tách sang `<tên>-chua-thi-cong.md` cạnh nó. Mục lục ADR tách sang [`muc-luc.md`](muc-luc.md) |
| 9 | Trích dẫn `file:dòng` bên trong ADR có số là lịch sử: cổng §7 không kiểm, D45 giữ nguyên |
| 10 | `.claude/CLAUDE.md` giữ luật dưới 10 KB; lý do chuyển sang `.claude/README.md` mục *Vì sao* |

## Phương án đã loại

| Phương án | Vì sao loại |
| --- | --- |
| Cắt nội dung mạnh — thay khối code mẫu D44 bằng dòng trỏ vào code | Sửa rất nhiều tệp, và mỗi lượt agent vẫn phải đọc hết |
| Chỉ cắt an toàn, giữ cách đọc cũ | Mỗi tệp chỉ bớt được 4–16% |
| Để mọi agent theo model của phiên chính | Chậm và tốn nhất; lỗi của người viết đã có test và reviewer bắt |
| Không giới hạn vòng review | Đã có phạm vi kéo tới vòng 7 |

## Hệ quả

- **Tốt.** Bộ luật luôn đọc, theo cổng §23, sau đợt này:
  - `core-reviewer` từ khoảng 400 KB còn 89 KB;
  - `backend-expert` từ 245 KB còn 78 KB;
  - `frontend-expert` từ 222 KB còn 45 KB;
  - `architect` từ 186 KB còn 55 KB.

  Người viết và người soát chấm theo cùng một bảng.
- **Xấu.**
  - Agent có thể bỏ sót một tệp chủ đề liên quan. Checklist nêu mọi luật kèm chỗ đọc chi tiết, và người soát mở đủ tệp của phần đổi.
  - Lỗi cũ nằm ngoài phần đổi chỉ lộ ra ở lượt soát toàn bộ.
  - Số dòng trong ADR có thể cũ; người đọc tìm theo tên mục.
- Kiểm kê trước và sau ngày 2026-09-27: không mất mã luật, mã lỗi, endpoint, token, seam, khoá cấu hình, mốc định nghĩa gốc hay tiêu đề mục quy ước nào.
