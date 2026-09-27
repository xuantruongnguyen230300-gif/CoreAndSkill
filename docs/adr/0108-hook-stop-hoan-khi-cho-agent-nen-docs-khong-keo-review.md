---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0108 — Hook `Stop` không chặn khi phiên đang chờ agent nền; sửa luật trong `docs/` không kéo review code; dấu hook tách theo phiên

> **Trạng thái:** Đã chấp nhận (2026-09-27)

## Bối cảnh

Đây là giai đoạn 1 của đợt refactor `.claude`/`docs`. Số đo lấy từ transcript phiên thi công 16–26/09:

- Hook `Stop` chặn **913** lần vì thiếu `core-reviewer`. Phần lớn các lần chặn xảy ra lúc phiên chính đang chờ agent nền chạy xong.
- Cổng tài liệu chạy lại sau **mọi** lệnh shell.
- Dấu `core-touched` dùng chung, nên việc của phiên này chặn luôn phiên kia.

Ba điều đo thêm ngày 2026-09-26, trên Claude Code 2.1.282, bằng một phiên `claude -p` riêng trong scratchpad:

| Đo được | Hệ quả |
| --- | --- |
| Lệnh hook chạy ở thư mục làm việc **hiện tại** của phiên. Sau một lần `cd` vào thư mục con, lệnh `bash .claude/hooks/x.sh` hỏng im lặng | Trong lúc phiên đứng ở thư mục con, cả ba cùng mất: lớp chặn git thứ hai, việc ghi dấu và cổng tài liệu |
| Harness đặt `$CLAUDE_PROJECT_DIR` cho lệnh hook, dạng `C:/…` | Lệnh hook neo vào biến này thì chạy được từ mọi thư mục |
| Payload `Stop` có mảng `background_tasks`. Agent nền đang chạy mang `"type":"subagent"` và `"status":"running"` | Hook phân biệt được phiên **đang chờ** với phiên **dừng hẳn** |

## Quyết định

| # | Quyết định | Nguồn |
| --- | --- | --- |
| 1 | Hook `Stop` không làm gì khi payload còn agent nền `running`: không chạy cổng, không chặn. Chỉ tính `subagent`, không tính việc kiểu `shell`. Hết agent nền thì chặn như trước | Người dùng chốt, 2026-09-27 |
| 2 | Khối `core-paths` bỏ `docs/quy-uoc/`, `docs/RULES.md`, `docs/OWNERSHIP.md`. Giữ `docs/kien-truc-core-module.md`, vì tệp đó chứa chính khối | Người dùng chốt, 2026-09-27 |
| 3 | Nhật ký `core-touched` và dấu `core-reviewed` giữ chung, vì review phủ cả cây. Thêm một cờ riêng cho từng phiên: chỉ phiên đã chạm Core mới bị chặn | Kế hoạch giai đoạn 1 |
| 4 | Lệnh hook trong `.claude/settings.json` neo vào `$CLAUDE_PROJECT_DIR` | Sửa lỗi |
| 5 | Cổng tài liệu chạy khi có tệp hoặc thư mục không bị gitignore mới hơn lần xanh cuối. Hook hỏi hệ thống tệp, không ghi dấu sau mỗi lệnh shell | Sửa lỗi |
| 6 | Nhánh lệnh shell của `on-edit.sh` quét theo mốc lần quét trước của phiên, không theo cửa sổ 90 giây. Nhánh Edit/Write so với mọi mục của khối, không lọc khu vực trước. Điều này đóng lỗ `scripts/` | Sửa lỗi |

## Phương án đã loại

| Phương án | Vì sao loại |
| --- | --- |
| Chặn một lần rồi thả, hoặc chỉ nhắc | Bỏ chốt bằng máy. Người dùng chọn giữ chốt |
| Bỏ cả `docs/kien-truc-core-module.md` khỏi khối | Cổng `check-docs.sh` §26 chỉ kiểm khối **đọc được**. Nếu bỏ, người sửa có thể thu hẹp khối để tắt hàng rào mà không phải qua review |
| Tách hẳn nhật ký chạm Core theo phiên | Việc Core dở dang của một phiên đã đóng sẽ không còn chặn ai |
| Tính cả việc `shell` chạy nền là "đang chờ" | Chỉ cần một lệnh `sleep` chạy nền là né được hàng rào |

## Hệ quả

- **Tốt:**
  - Phiên chính không phải ngủ chờ hay hỏi thăm agent trong lúc agent nền chạy.
  - Sửa luật trong `docs/` không bị ép review code.
  - Phiên không chạm Core không bị việc của phiên khác chặn.
- **Xấu:**
  - Nếu phiên bị đóng khi agent nền còn chạy, lần kiểm cuối không diễn ra. Việc dở nằm lại trong nhật ký chung, và phiên kế tiếp chạm Core phải review cả phần đó.
  - Đổi luật trong `docs/quy-uoc/` mà code chưa khớp thì không có gì tự kéo review nữa. Chỗ lệch đó phải ghi nợ ở [`../DEBT.md`](../DEBT.md).
- Hook `Stop` dọn thư mục trạng thái của những phiên không hoạt động quá 7 ngày.
- Cổng tài liệu có thể chạy thừa khi một tệp không bị gitignore đổi vì việc khác, ví dụ build tạo một thư mục mới. Việc đó tốn vài giây, không làm chặn sai.

## Kiểm

`bash .claude/hooks/tests/run-tests.sh` là bộ test cho các hook; các nhóm liên quan là `on-stop`, `on-edit` và `settings.json`.

Ngày 2026-09-27 đã chạy tay bảy phép đột biến trên bản sao trong scratchpad:
- bỏ hoãn khi agent nền đang chạy;
- quay lại cửa sổ 90 giây;
- đặt lại bộ lọc khu vực;
- dò trạng thái `running` bằng `grep`;
- chặn cả phiên không mang cờ;
- luôn chạy cổng;
- lệnh hook tương đối.

Phép nào cũng làm đỏ đúng các ca của phần nó phá.
