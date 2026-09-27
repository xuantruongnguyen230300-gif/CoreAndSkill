---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0084 — Cột mở rộng trùng khoá thì màn Core ném lỗi khi khởi tạo, ở mọi môi trường

> **Trạng thái:** Đã chấp nhận (2026-09-24)

## Bối cảnh

Seam `CORE_SCREEN_EXT` ([`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §2.7) cho dự án hạ nguồn nối cột vào màn danh sách Core. Hợp đồng ghi khoá cột *"duy nhất trong màn, không trùng khoá cột của Core"* nhưng không nói trùng thì sao.

`frontend-expert` thử ngày 2026-09-24: khai `key: 'email'` qua `provideCoreScreenExt` trên màn người dùng → không lỗi nào, bảng vẽ thừa một cột. Thứ duy nhất báo là cảnh báo NG0955 (khoá trùng trong `@for … track`) — **chỉ có ở chế độ dev**. Bản production im lặng, và khi hai cột cùng khoá thì việc khớp lại cột mỗi lần danh sách cột đổi không còn đáng tin.

Có hai đường sinh ra khoá trùng: dự án khai nhầm, hoặc **Core nâng cấp thêm một cột** mà khoá của nó trùng một cột dự án đã có từ trước. Đường thứ hai không ai trong dự án hạ nguồn làm gì sai.

## Quyết định

Kiến trúc sư chốt: màn danh sách Core **ném `Error` khi khởi tạo** nếu một cột dự án trùng khoá cột Core hoặc hai cột dự án trùng khoá nhau, ở dev lẫn production. Thông báo nêu mã màn và khoá trùng. Phép kiểm nằm ở **một** hàm dùng chung cho mọi màn — `platform/config/cot-mo-rong.ts` — không lặp ở từng màn, và chạy lúc dựng màn chứ không trong một `computed` tính lại theo bảng dịch.

## Phương án đã cân nhắc và vì sao loại

### A — Bỏ qua cột dự án, giữ cột Core

**Được:** màn luôn vẽ được; lỗi cấu hình không bao giờ làm hỏng một màn đang chạy.
**Mất:** cột dự án biến mất **không tiếng động**. Ở đường thứ hai (Core thêm cột), một lần nâng Core lặng lẽ gỡ dữ liệu dự án khỏi màn hình production, và chỉ lộ khi có người dùng hỏi cột đâu.
**Vì sao loại:** repo này chọn cho lỗi cấu hình ở composition root nổ đúng lúc, đúng chỗ — cùng lý do token seam không có `factory` mặc định ([`../wiki-core/fe/ly-do/fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §2.5). Phương án này đổi một lỗi to và rẻ thành một lỗi im lặng và đắt.

### B — Cột dự án đè cột Core

**Được:** dự án tự quyết cách hiển thị một trường đã có.
**Mất:** đây là **BỚT** một cột Core rồi **chen** cột khác vào vị trí đó — trái cả luật 1 (chỉ thêm) lẫn luật 2 (nối sau) của chính seam. Cột Core bị đè có thể là thứ một luồng của Core dựa vào (lý do của luật 1).
**Vì sao loại:** nó mở lại đúng cửa mà luật 1 đóng.

### C — Ném lỗi ở dev, bỏ qua ở production

**Được:** dev thấy ngay; production không vỡ màn.
**Mất:** hai môi trường chạy hai hành vi khác nhau cho cùng một cấu hình — đúng thứ làm NG0955 vô dụng. Dự án không mở màn đó trước khi phát hành thì production rơi về phương án A.
**Vì sao loại:** giữ nguyên chỗ hỏng mà finding này chỉ ra, chỉ dời nó sang nhánh `isDevMode()`.

## Hệ quả

### Tích cực

- Khoá trùng không lọt tới production trong im lặng; lỗi nêu đích danh màn và khoá nên sửa được trong một lần đọc.
- `track cot.key` của bảng được bảo đảm duy nhất cho mọi màn Core có mở rộng.

### Tiêu cực — cái giá thật

- **Nâng Core có thể làm vỡ một màn của dự án hạ nguồn.** Core thêm một cột mà khoá trùng cột dự án thì màn đó không mở được cho tới khi dự án đổi khoá. Dự án không mở màn đó trước khi phát hành thì người dùng production gặp màn lỗi — nặng hơn phương án A ở đúng ca này.
- Khoá cột của màn Core từ nay là **một phần hợp đồng seam**: thêm cột Core là thay đổi mà dự án hạ nguồn phải đọc thấy trong ghi chú nâng cấp.

### Rút lui nếu sai

Đổi sang phương án A là sửa đúng một hàm (`platform/config/cot-mo-rong.ts`) và luật 6 §2.7 — không dữ liệu nào sinh ra, không hợp đồng dây nào đổi. Test của hàm đó viết lại theo hành vi mới.

### Dấu hiệu quyết định này bắt đầu sai

- Có dự án hạ nguồn báo màn Core vỡ ở production **vì một lần nâng Core**, không phải vì chính họ khai nhầm.
- Số màn Core có seam tăng tới mức dự án không còn mở hết chúng trước mỗi lần phát hành.

## Liên quan

- Luật 6 của seam: [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §2.7
- Vì sao seam chỉ mang giá trị dùng được ở composition root: [`0057-seam-fe-chi-mang-gia-tri-dung-duoc-o-composition-root.md`](0057-seam-fe-chi-mang-gia-tri-dung-duoc-o-composition-root.md)
