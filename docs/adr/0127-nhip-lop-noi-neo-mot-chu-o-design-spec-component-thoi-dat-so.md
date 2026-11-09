---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0127 — Nhịp bung và thu của mọi lớp nổi neo có một chủ ở `DESIGN.md` §7; spec `Menu`, `Autocomplete`, `DatePicker` thôi giữ quyền đặt số

> **Trạng thái:** Đã chấp nhận (2026-11-06)

## Bối cảnh

[ADR-0075](0075-ba-component-lop-noi-vao-nhom-b-va-bac-thang-chon-theo-vai.md) quyết định 1 có hai phần: **phân loại** — spec `Menu`, `DatePicker`, `Autocomplete` được quyền đặt thời lượng và đường cong mở/đóng lớp nổi — và **cơ chế** — số sống trong hằng TypeScript, luật F37. [ADR-0123](0123-chuyen-dong-component-primeng-di-bang-token-qua-css-go-hang-ms-f37-het-ap-dung.md) gỡ phần cơ chế và ghi phần phân loại là **còn hiệu lực** ở bảng *ADR cũ — phần nào còn hiệu lực* của nó. Cùng ADR đó, quyết định 4 đè chuyển động của lớp nổi neo **một lần**, trên lớp pha chung của thư viện, không theo lớp bọc.

Hai điều đó va nhau ngay khi thi công. Một khối CSS áp cho mọi lớp nổi neo — kể cả ô chọn số dòng của `Pagination`, mà không spec nào nói — trong khi ba spec cùng có quyền đặt số. Ba spec chép cùng một bộ ba giá trị. Spec nào đổi số riêng thì không có chỗ nào áp được con số đó: khối đè không theo lớp bọc.

Ngày 2026-11-06 người dùng chốt: [`../Design/DESIGN.md`](../Design/DESIGN.md) §7 mục *Lớp nổi neo* là nơi duy nhất quyết, bốn spec trỏ về. Quyết định được ghi một dòng ở [`nhat-ky-quyet-dinh.md`](nhat-ky-quyet-dinh.md), và dòng đó tự nêu là *sửa phần phân loại của quyết định 1 ở ADR-0075*. Nhưng lật một phần ADR là dấu hiệu phải viết ADR ([`README.md`](README.md) §2), và dòng trạng thái của ADR cũ chỉ trỏ được tới một ADR ([`README.md`](README.md) §5.1). ADR này ghi lại quyết định đã chốt để dòng trạng thái của ADR-0075 có chỗ trỏ. Nó không thêm gì vào nội dung đã chốt.

## Quyết định

Người dùng chốt, kiến trúc sư ghi:

1. **Nhịp bung và thu của mọi lớp nổi neo vào phần tử đã gọi nó có một chủ:** [`../Design/DESIGN.md`](../Design/DESIGN.md) §7 *Lớp nổi neo*. Spec `Menu`, `Autocomplete`, `DatePicker`, `Pagination` trỏ về mục đó và không đặt số riêng.
2. **Phần phân loại của quyết định 1 ở ADR-0075 hết hiệu lực.** Phần cơ chế đã hết hiệu lực từ ADR-0123, nên quyết định 1 của ADR-0075 không còn phần nào áp dụng. Quyết định 2, 3, 5 của ADR-0075 giữ nguyên; quyết định 4 giữ như ADR-0123 đã ghi.
3. **Một lớp nổi neo muốn chạy nhịp khác thì phải mở một vai mới ở `DESIGN.md` §7**, không sửa riêng spec của nó.

Không giá trị nào đổi, không dòng mã nào đổi: khối đè trên lớp pha chung đã áp một bộ giá trị cho mọi lớp nổi neo từ lượt thi công ADR-0123.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Chỉ giữ dòng nhật ký, không viết ADR

Đây là phương án đơn giản hơn, và là việc đã làm trước ADR này.

**Được:** không thêm tệp; quyết định đã có ghi nhận và đã thi công ở `docs/Design/`.
**Mất:** dòng trạng thái của ADR-0075 không có chỗ trỏ. Người đọc ADR-0075 lần theo dòng trạng thái tới ADR-0123, đọc bảng *ADR cũ* ở đó và thấy phần phân loại **còn hiệu lực** — tin rằng ba spec vẫn có quyền đặt số. Nội dung ADR-0123 không sửa được để chữa câu đó.
**Vì sao loại:** trái [`README.md`](README.md) §2 và §5.1. Lỗ đọc sai ở trên là thật, không phải lý thuyết.

### Phương án B — Giữ quyền đặt số ở ba spec

Nguyên trạng mà ADR-0123 để lại.

**Được:** mỗi component giữ được đường xin một nhịp riêng mà không chạm component khác.
**Mất:** ba nguồn cho một giá trị áp ở một chỗ; ô chọn số dòng của `Pagination` không có nguồn nào. Một spec đổi số mà khối chung không đổi thì spec nói một đằng, màn hình chạy một nẻo.
**Vì sao loại:** người dùng chốt một nguồn. Lý do: hai nơi giữ cùng một giá trị thì sẽ lệch nhau.

### Phương án C — Đè theo từng lớp bọc để mỗi spec giữ quyền đặt số

**Được:** quyền đặt số của từng spec có chỗ áp thật.
**Mất:** lớp nổi neo không đi qua lớp bọc nào chạy nhịp mặc định của thư viện, khác các lớp nổi khác trong cùng một màn.
**Vì sao loại:** đúng lý do ADR-0123 quyết định 4 đã loại. Lật quyết định đó cần ADR riêng, và chưa ai đề xuất.

## Hệ quả

### Tích cực

- Một nguồn cho một bộ giá trị áp ở một chỗ. `Pagination` có nguồn cho nhịp của lớp nổi trong nó.
- Spec component gọn lại thành một dòng trỏ đường.

### Tiêu cực

| Cái giá | Nghĩa là |
| --- | --- |
| **Xin nhịp riêng cho một component đắt hơn** | Panel lịch của `DatePicker` lớn hơn hẳn hộp `Menu`; nếu nhịp chung không vừa nó thì đường sửa là mở vai mới ở `DESIGN.md` §7, thêm khối đè có selector hẹp hơn và spec đo riêng — không còn là sửa một dòng spec |
| **Một lần đổi chạm mọi lớp nổi neo** | Câu hỏi *nhịp này có nặng tay không* ở `DESIGN.md` §10 câu 5 nay trả lời một lần cho cả bốn component. Không thử được nhịp mới trên một component trước |
| **Đọc ADR-0075 phải nối ba tệp** | Muốn biết quyết định 1 còn gì phải đi 0075 → 0123 → 0127. Dòng trạng thái và dòng link dưới nó là thứ duy nhất dẫn đường |

### Rút lui nếu sai

**Dấu hiệu sai:** một lượt rà giao diện thấy một lớp nổi neo cần nhịp khác hẳn các lớp còn lại, vì lý do của chính nó — cỡ, khoảng di chuyển — chứ không vì nhịp chung quá nặng hay quá nhẹ.

**Quy trình:**

1. Viết ADR mới, sửa một phần ADR này; ADR này mang thêm trạng thái tương ứng.
2. `design-expert` mở vai mới ở `DESIGN.md` §7; spec của component đó trỏ vào vai mới, không giữ số riêng.
3. `frontend-expert` thêm khối đè có selector theo lớp bọc, đứng sau khối chung trong `src/FE/src/styles/_thu-vien.scss`, và spec F62 của lớp bọc đo bậc mới.

Không dữ liệu nào sinh ra, không migration. Chi phí: một lượt `design-expert`, một lượt `frontend-expert`, một lượt review vì `src/FE/src/styles/` thuộc `core-paths`.

## Sáu câu hỏi trước khi chấp nhận

| # | Câu hỏi | Trả lời |
| --- | --- | --- |
| 1 | Vấn đề thật, đã đo chưa | Ba spec chép cùng một bộ ba giá trị và một lớp nổi không spec nào nói — đọc được ở bản spec trước lượt sửa của `design-expert` ngày 2026-11-06. Lỗ đọc sai ở phương án A là đọc thấy, không cần đo |
| 2 | Phương án đơn giản hơn, vì sao không đủ | Phương án A. Dòng trạng thái không trỏ được tới nhật ký |
| 3 | Chi phí vận hành thêm | Không thêm: không mã, không cổng. Thêm một tệp ADR để đọc |
| 4 | Ai bảo trì | `design-expert` giữ `DESIGN.md` §7; `frontend-expert` giữ khối đè; `core-reviewer` soát khi khối đè đổi |
| 5 | Rút lui thế nào | Mục *Rút lui nếu sai* |
| 6 | Có buộc Core biết nghiệp vụ không | Không. Đây là hệ thiết kế |

## ADR cũ — phần nào còn hiệu lực

| ADR | Còn hiệu lực | Hết hiệu lực |
| --- | --- | --- |
| [0075](0075-ba-component-lop-noi-vao-nhom-b-va-bac-thang-chon-theo-vai.md) | Quyết định 2, 3, 5 nguyên vẹn. 4 — như ADR-0123 đã ghi | 1 — phần cơ chế (ADR-0123), phần phân loại (ADR này) · 6 (ADR-0123) |

## Việc thi công

Không còn việc. `docs/Design/DESIGN.md` §7 và bốn spec component đã sửa cùng ngày theo dòng nhật ký (`design-expert`). Cùng lượt viết ADR này, `architect` sửa dòng trạng thái của ADR-0075 và thêm dòng ở [`muc-luc.md`](muc-luc.md).

## Liên quan

- [`0075-ba-component-lop-noi-vao-nhom-b-va-bac-thang-chon-theo-vai.md`](0075-ba-component-lop-noi-vao-nhom-b-va-bac-thang-chon-theo-vai.md) — ADR bị sửa một phần.
- [`0123-chuyen-dong-component-primeng-di-bang-token-qua-css-go-hang-ms-f37-het-ap-dung.md`](0123-chuyen-dong-component-primeng-di-bang-token-qua-css-go-hang-ms-f37-het-ap-dung.md) quyết định 4 — khối đè chung của lớp nổi neo.
- [`../Design/DESIGN.md`](../Design/DESIGN.md) §7 *Lớp nổi neo* — nơi quyết nhịp.
- [`nhat-ky-quyet-dinh.md`](nhat-ky-quyet-dinh.md) — dòng 2026-11-06 ghi lần chốt.
