---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0007 — FE giữ cấu trúc thư mục, không dùng Angular workspace nhiều library

> **Trạng thái:** Đã chấp nhận (2026-09-08)

## Bối cảnh

Ở dự án tiền nhiệm, các tầng `core/`, `shared/`, `platform/` chỉ là **thư mục** bên trong một ứng dụng Angular, không phải project hay library riêng. Ranh giới giữa chúng do một luật ESLint giới hạn đường dẫn import giữ.

Kết quả audit xếp đây là vấn đề cần sửa, với ba hệ quả: không đóng gói được từng tầng độc lập; dự án mới lại phải copy thư mục — đúng bệnh đã gặp ở backend; và **luật ESLint bỏ qua được bằng một dòng comment, trong khi tham chiếu project thì không**.

Cùng lúc, có một dấu hiệu cho thấy hàng rào hiện tại chưa từng được thử thật: luật chặn import chéo giữa các module hôm nay là vô tác dụng, vì danh sách module nghiệp vụ trong cấu hình đang rỗng nên cả khối luật bị bỏ qua. Điều này được ghi trung thực trong tài liệu của họ, và nó có lý do — chưa có module nghiệp vụ nào. Nhưng nó cũng có nghĩa là ranh giới module ở FE **chưa từng được kiểm chứng**.

Ngược lại, một hàng rào khác của cùng dự án đã chứng minh hiệu quả: luật cấm component nghiệp vụ import trực tiếp thư viện UI được tuân đúng — không màn hình nghiệp vụ nào vi phạm.

## Quyết định

FE giữ `core/ shared/ platform/ modules/` là **thư mục trong một ứng dụng duy nhất**. Không tách thành nhiều library Angular, không dùng Nx.

Ranh giới ép bằng ba thứ cùng lúc:

1. **Hai vùng luật ESLint** giới hạn đường dẫn import: một vùng cho chiều phụ thuộc giữa các tầng, một vùng cho ranh giới giữa các module (luật F1, F2 ở [`../RULES.md`](../RULES.md)).
2. **Cấm `eslint-disable` cho đúng danh sách rule ranh giới** (luật F3). Đây là điều kiện tồn tại của hai luật trên, không phải bổ sung tuỳ chọn.
3. **Cổng kiểm danh sách module trong cấu hình khớp thư mục `modules/` thật** (luật F4), để tình trạng danh sách rỗng làm luật thành vô tác dụng không tái diễn trong im lặng.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Angular workspace nhiều library

**Được:** ranh giới do **compiler** ép. Một library không khai phụ thuộc thì không import được, và không dòng comment nào bỏ qua được điều đó. Ngoài ra mỗi library đóng gói và phát hành riêng được.

**Vì sao loại:**

- **Chi phí thường trực trên mỗi thay đổi.** Thêm một thành phần dùng chung là thêm một mục xuất bản, cập nhật đường dẫn ánh xạ trong cấu hình TypeScript, và build lại library trước khi ứng dụng thấy nó. Vòng lặp phát triển dài ra ở việc làm thường xuyên nhất.
- **Lợi ích chính chưa dùng tới.** Đóng gói phát hành độc lập chỉ đáng khi có nhiều ứng dụng cùng tiêu thụ. Hiện có một ứng dụng, và phương án đóng gói qua package đã bị loại từ vòng chốt trước.
- **Chi phí chuyển đổi trả ngay**, còn ích lợi trả sau và có điều kiện.

### Phương án B — Nx

**Được:** mọi thứ của phương án A, cộng đồ thị phụ thuộc kiểm được bằng luật, cộng build tăng tiến.

**Vì sao loại:** thêm một lớp công cụ nữa lên trên Angular CLI — một hệ thống plugin riêng, một nhịp nâng cấp riêng, và một cách hỏng riêng khi hai nhịp nâng cấp lệch nhau. Với một ứng dụng và một đội nhỏ, thời gian build chưa phải vấn đề, nên phần lớn giá trị của Nx không thu được. Đây cũng là một quyết định đắt để đảo ngược.

### Phương án C — Giữ thư mục, giữ nguyên luật ESLint như dự án tiền nhiệm

**Được:** không phải làm gì.

**Vì sao loại:** giữ nguyên nghĩa là giữ luôn cả hai lỗ hổng — một dòng comment bỏ qua được luật, và danh sách module rỗng làm cả khối luật thành vô tác dụng mà không ai biết. Nếu chọn thư mục thì phải trả bằng cách siết hàng rào còn lại; chọn thư mục mà không siết là chọn không có ranh giới.

## Hệ quả

### Tích cực

- **Vòng lặp phát triển ngắn:** một lệnh chạy, không có bước build library trung gian.
- **Chi phí chuyển đổi bằng không**, và cấu trúc thư mục vẫn là hình dạng đúng nếu sau này quyết định tách library — việc tách sẽ là di chuyển thư mục, không phải thiết kế lại.
- Ranh giới **vẫn nhìn thấy được** trong cây thư mục, nên người mới đọc là hiểu.

### Tiêu cực — ghi thẳng, đây là điểm yếu chính của quyết định này

- **Không có compiler ép ranh giới.** Đây là mất mát thật, không bù lại được bằng công cụ khác. ESLint là hàng rào duy nhất.
- **ESLint bỏ qua được bằng một dòng comment.** Vì vậy luật F3 — cấm `eslint-disable` cho các rule ranh giới — **không phải luật phụ, nó là điều kiện tồn tại của F1 và F2**. Thiếu F3 thì ranh giới chỉ là gợi ý, và một gợi ý sẽ bị bỏ qua vào đúng lúc gấp nhất, kèm một dòng chú thích hứa sẽ sửa sau.
- **Hàng rào chỉ chạy khi có ai đó chạy nó.** Ranh giới sống nhờ cổng, mà cổng thì cần CI — xem [`0011-ci-github-actions.md`](0011-ci-github-actions.md). Trên máy cá nhân, lệnh kiểm bỏ qua được trong im lặng.
- **Danh sách module trong cấu hình là điểm hỏng thầm lặng.** Quên thêm tên module mới vào đó thì luật chặn import chéo không báo gì cả — nó đơn giản là không kiểm module ấy. Luật F4 sinh ra vì chuyện này đã xảy ra một lần.
- **Không đóng gói được tầng nào cho ứng dụng thứ hai dùng.** Nếu ngày mai có ứng dụng FE thứ hai, cách chia sẻ duy nhất là copy thư mục — đúng thứ đang phê phán ở backend. Quyết định này chấp nhận điều đó, với điều kiện chỉ có một ứng dụng.

## Liên quan

- [`../RULES.md`](../RULES.md) — luật F1, F2, F3, F4
- [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) — bốn tầng và chiều phụ thuộc
- [`../wiki-core/fe/trien-khai/05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) — cổng FE, mục nào đang chạy thật
- [`0011-ci-github-actions.md`](0011-ci-github-actions.md) — vì sao hàng rào cần CI mới có tác dụng
