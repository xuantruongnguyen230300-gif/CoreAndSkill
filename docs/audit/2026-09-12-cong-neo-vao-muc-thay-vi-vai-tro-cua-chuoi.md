---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 2026-09-12 — Phép dò neo vào MỤC chứa chuỗi, trong khi luật nói về VAI của chuỗi

> Phát hiện khi dựng mục §18 cho [`../../.claude/check-docs.sh`](../../.claude/check-docs.sh) — lát cắt máy kiểm được của luật D27 ([`../adr/0019-ba-component-nang-thuoc-core.md`](../adr/0019-ba-component-nang-thuoc-core.md) ràng buộc 3).
>
> Viết theo khuôn bốn phần ở [`README.md`](README.md).


---

## 1. Hiện tượng

Bản dò đầu tiên của §18 quét từ vựng ngành (*chứng từ · dự toán · kỳ kế toán · hạch toán · khoá sổ · bảng lương*) trong [`../Design/Components/`](../Design/Components/) và **fail khi từ đó nằm trong mục `Biến thể` / `Trạng thái` / `API dự kiến`** — với lập luận: trong *"Khi nào dùng"* thì từ ngành là ví dụ hợp lệ, còn trong ba mục kia thì nó đã ăn vào mô hình.

Chạy lần đầu trên repo hiện tại: **5 FAIL**.

| Chỗ bị bắt | Nội dung thật |
| --- | --- |
| `Chart.md` | đoạn văn giải thích vì sao **không** có biến thể `meter`, lấy *"thực hiện so với dự toán"* làm minh hoạ |
| `Input.md` | ô cuối bảng biến thể của `daterange`: *"Kỳ báo cáo, lọc chứng từ theo khoảng"* |
| `ProgressBar.md` | ô cuối bảng biến thể của `meter`: *"Thực hiện so với dự toán, dung lượng đã dùng so với hạn mức"* |
| `TreeSelect.md` ×2 | ô cuối bảng biến thể, và một bảng quyết định *"nút cha có chọn được không"* |

Đem đúng phép thử của ADR-0019 ra soi — *"xoá phần nghiệp vụ mà spec vẫn còn nghĩa"* — thì **cả năm đều xoá được mà spec không đổi nghĩa**. Chúng là ví dụ, chỉ tình cờ đứng trong mục mô hình. Phép dò sai **5 trên 5**.

---

## 2. Nguyên nhân gốc

**Luật nói về VAI của chuỗi; phép dò lại neo vào MỤC chứa chuỗi.** Hai thứ đó tương quan nhưng không trùng nhau.

Một bảng biến thể có hình dạng ổn định: hai cột đầu là **mô hình** (tên biến thể, định nghĩa), cột cuối là **ví dụ dùng**. Nghĩa là ngay bên trong một mục mô hình vẫn có một cột cố ý dành cho ví dụ — và ví dụ chính là thứ làm spec dễ hiểu. Phép dò theo mục quét cả hàng, nên nó bắt đúng cái cột mà tài liệu cố tình đặt ở đó.

Chỗ này có một cái bẫy thứ hai, sâu hơn: khi cổng đỏ, **đường đi ít kháng cự nhất là sửa tài liệu cho cổng xanh** — viết lại "lọc chứng từ theo khoảng" thành "lọc theo khoảng thời gian". Làm thế thì cổng xanh, mô hình **không đổi một chữ**, và spec mất đi thứ duy nhất giúp người đọc biết `daterange` dùng vào việc gì. Đó là tẩy chữ, không phải sửa lỗi — và nó là biến thể của đúng hành vi mà [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §8 cấm, chỉ khác chiều: ở đó là nới luật cho cổng xanh, ở đây là bóp tài liệu cho cổng xanh.

Vì sao không lộ sớm hơn: phép dò được nghĩ ra bằng lập luận ("mục nào thì từ ngành ăn vào mô hình") chứ không bằng cách chạy thử trên repo thật. Lập luận nghe hợp lý; tỉ lệ báo sai chỉ hiện ra ở lần chạy đầu tiên.

---

## 3. Cách vá

Thu hẹp phép dò từ **phạm vi mục** xuống **vị trí cú pháp**: chỉ xét chuỗi nằm trong code span (`` ` ``) hoặc trong code block. Đó là chỗ từ ngành trở thành **tên biến thể, tên prop, tên trường trong chữ ký kiểu** — và là chỗ duy nhất mà phép thử *"xoá đi spec vẫn còn nghĩa"* trả lời **không**: xoá một tên trường thì chữ ký đổi.

Kèm theo, pattern được mở rộng sang dạng ASCII không dấu (`kyKeToan`, `hachToan`, `khoaSo`…), vì định danh trong code hầu như không bao giờ mang dấu — bản chỉ có dấu sẽ là một mục cổng không bao giờ khớp gì, đúng lớp lỗi mà [`2026-09-09-cong-no-op-va-hai-mo-hinh-nguon.md`](2026-09-09-cong-no-op-va-hai-mo-hinh-nguon.md) ghi.

Mục có **hai** cơ chế tự chứng minh, vì điều kiện PASS của nó thuần phủ định:

- **canary** — pattern phải khớp được một chuỗi dựng sẵn, nếu không mục tự báo hỏng;
- **bộ đếm đầu vào** — số đoạn định danh đã xét phải lớn hơn 0.

Đây là vá gốc cho lát cắt đã chọn, **không** phải vá cho cả D27. Phần còn lại của D27 — từ ngành trong văn xuôi, trong tên mục, trong cách diễn đạt — vẫn thuộc nợ ở §10 [`../RULES.md`](../RULES.md), vẫn do `core-reviewer` và người đọc bắt.

---

## 4. Cổng nào lẽ ra phải bắt

Không có cổng nào bắt được lỗi này, vì lỗi nằm **trong chính một cổng đang được viết**. Cái bắt được nó là một thói quen, và thói quen đó đáng thành luật của khu này:

> **Một mục cổng mới phải được chạy trên repo thật TRƯỚC khi được chốt, và tỉ lệ báo sai phải được đọc từng ca — không phải đếm.**
>
> Mục nào báo sai từ ca đầu tiên thì phép dò sai, không phải tài liệu sai. Sửa tài liệu để mục cổng mới xanh là hành vi cấm: nó biến một phép dò hỏng thành một phép dò hỏng **có tài liệu bọc quanh cho khớp**.

Lát cắt đã thi công vào bảng luật thành **D28** ([`../RULES.md`](../RULES.md) §1), kèm cột *"Ép bằng gì"*. D27 ở §10 giữ nguyên phần chưa có cổng và trỏ sang D28.

---

## 5. Bài học tổng quát

**Khi một luật nói về *vai* của một chuỗi, phép dò phải neo vào thứ mang vai đó, không neo vào vùng chứa nó.**

Vùng chứa — một mục, một thư mục, một file — là xấp xỉ rẻ tiền của vai. Xấp xỉ đó sai theo **cả hai chiều**: nó bắt nhầm chuỗi hợp lệ đứng trong vùng (chuyện vừa xảy ra), và nó bỏ sót chuỗi vi phạm đứng ngoài vùng — một tên prop mang từ ngành nằm trong mục `Token dùng` thì phép dò theo mục không thấy.

Thứ mang vai ở đây là **vị trí cú pháp**: backtick và code fence là dấu hiệu máy đọc được của *"đây là định danh"*. Neo vào đó thì phép dò nói đúng cái luật nói.

Cùng họ với hai bản ghi trước: [`2026-09-09-cong-no-op-va-hai-mo-hinh-nguon.md`](2026-09-09-cong-no-op-va-hai-mo-hinh-nguon.md) (phép đếm không biết mình đếm gì) và [`2026-09-11-lenh-kiem-tu-dem-chinh-no.md`](2026-09-11-lenh-kiem-tu-dem-chinh-no.md) (phép kiểm neo vào chuỗi thay vì cấu trúc). Cả ba đều là **phép dò neo sai chỗ**; chỉ khác neo lệch đi đâu.
