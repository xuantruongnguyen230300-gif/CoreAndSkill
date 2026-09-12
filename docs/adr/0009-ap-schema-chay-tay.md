---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0009 — Không auto-migrate: sinh script cho người vận hành, kèm cơ chế phát hiện DB lệch model

> **Trạng thái:** Đã chấp nhận (2026-09-08)

## Bối cảnh

Dự án tiền nhiệm đã chuyển từ tự động áp migration lúc khởi động sang **chỉ sinh tệp script** để người vận hành tự chạy trên cơ sở dữ liệu. Đó là quyết định có chủ đích, và nó mua được một thứ có giá: người vận hành nhìn thấy câu lệnh trước khi nó chạm dữ liệu thật, và có thể từ chối.

Nhưng kết quả audit chỉ ra chỗ thiếu: **không có cơ chế nào bảo đảm script đã chạy khớp với model hiện tại.** Lệch giữa cơ sở dữ liệu thật và model là loại lỗi chỉ lộ ra lúc chạy — thường là ở một truy vấn hiếm, trên môi trường mà không ai đang nhìn.

Đây là kiểu hỏng tệ nhất trong nhóm: ứng dụng khởi động bình thường, phần lớn màn hình chạy bình thường, và chỉ vỡ ở đúng chỗ chạm cột mới. Người dùng gặp trước đội phát triển.

## Quyết định

Giữ nguyên chính sách **chạy tay**, và bổ sung phần còn thiếu:

1. **Không auto-migrate ở bất kỳ môi trường nào**, kể cả máy dev.
2. Script sinh ra bằng lệnh sinh script của công cụ migration, ở dạng chạy lại được nhiều lần mà không hỏng, và đặt tại **một đường dẫn cố định** với **tên có thứ tự**.
3. **Ứng dụng từ chối khởi động khi còn migration chưa áp.** Lúc khởi động, ứng dụng hỏi cơ sở dữ liệu xem còn migration nào chưa áp không; nếu còn, nó dừng lại và in ra tên các migration thiếu cùng đường dẫn script cần chạy. Luật E8 ở [`../RULES.md`](../RULES.md) ép điều này, và nó được kiểm bằng một integration test chạy trên cơ sở dữ liệu thật.

Chi tiết vận hành — đặt tên, thứ tự, lệnh sinh, bảng lịch sử áp — ở [`../database/script-runbook.md`](../database/script-runbook.md).

Điểm mấu chốt: **chạy tay không có nghĩa là không kiểm.** Bỏ auto-migrate là bỏ việc tự động **ghi**, không phải bỏ việc tự động **phát hiện lệch**.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Auto-migrate ở mọi môi trường

**Được:** không bao giờ lệch; deploy là một bước; dev không phải nhớ gì.

**Vì sao loại:** nó trao cho tiến trình ứng dụng quyền đổi cấu trúc dữ liệu production, tự động, không ai duyệt. Một migration xoá cột, đổi kiểu, hay khoá bảng lớn sẽ chạy ngay khi tiến trình lên — và nếu chạy nhiều bản cùng lúc thì hai tiến trình có thể cùng áp. Không có cửa sổ nào để một người nhìn câu lệnh trước khi nó chạm dữ liệu thật. Với dữ liệu nghiệp vụ, mất mát đó không đảo ngược được bằng deploy lại.

### Phương án B — Auto-migrate ở dev, chạy tay ở production

**Được:** nghe rất hợp lý — dev tiện, production an toàn.

**Vì sao loại, và đây là phương án nguy hiểm nhất trong ba:** nó tạo ra **hai đường khác nhau giữa các môi trường**. Đường chạy hằng ngày và được thử hàng trăm lần là đường tự động; đường thật sự chạm production là đường thủ công, và nó được thử đúng một lần, vào lúc căng nhất. Mọi lỗi thuộc về script — sai thứ tự, thiếu một bước, câu lệnh không chạy lại được lần hai — sẽ **chỉ xảy ra ở production**, vì ở dev không ai đi qua đường đó.

Nguyên tắc chung áp dụng ở đây: đường đưa thay đổi vào cơ sở dữ liệu phải **giống nhau ở mọi môi trường**; chỉ khác ở người bấm nút.

### Phương án C — Chạy tay, không có cơ chế phát hiện lệch

Đây là trạng thái của dự án tiền nhiệm.

**Được:** đơn giản nhất; ứng dụng không cần biết gì về trạng thái migration.

**Vì sao loại:** đúng lỗ hổng mà audit chỉ ra. Quên chạy script là chuyện sẽ xảy ra — không phải vì ai đó cẩu thả, mà vì nó là một bước thủ công nằm ngoài quy trình deploy. Và khi đã quên, hệ thống không nói gì cả cho tới lúc một người dùng chạm vào tính năng mới.

## Hệ quả

### Tích cực

- **Không còn lệch âm thầm.** Trạng thái xấu nhất trở thành "ứng dụng không khởi động", và đó là trạng thái an toàn: nó ồn ào, xảy ra ngay lúc deploy, và có thông điệp chỉ đúng việc phải làm.
- **Người vận hành giữ quyền quyết định** với dữ liệu production, và có script để đọc trước.
- Một đường duy nhất cho mọi môi trường, nên script được thử ở dev trước khi tới production.

### Tiêu cực — cái giá thật

- **Thêm một bước thủ công trong quy trình deploy**, và bước đó phải có người biết làm. Đây là ràng buộc về con người, không phải về code: nếu người duy nhất biết chạy script đi vắng, việc deploy dừng lại. Runbook tồn tại để giảm rủi ro này, nhưng không xoá được nó.
- **Deploy có thể thất bại ở bước khởi động** nếu ai đó quên chạy script. Đây là chủ ý, nhưng nó có nghĩa thời gian ngừng dịch vụ dài hơn so với auto-migrate trong đúng tình huống đó.
- **Dev phải chạy script trên máy mình sau mỗi lần kéo code có migration mới.** Việc này gây khó chịu thường xuyên, và người ta sẽ đòi tự động hoá riêng cho dev — đúng phương án B đã bị loại. Cần nói rõ lý do từ chối, nếu không nó sẽ được lén thêm vào.
- **Cơ chế chặn khởi động chỉ phát hiện được migration chưa áp, không phát hiện được cơ sở dữ liệu bị sửa tay.** Nếu ai đó đổi một cột trực tiếp trên cơ sở dữ liệu, bảng lịch sử áp vẫn đầy đủ và ứng dụng vẫn khởi động. Đây là giới hạn thật của cơ chế này và không được coi nó là hàng rào chống mọi kiểu lệch.
- **Bảng lịch sử áp trở thành thứ không được đụng.** Sửa tay bảng đó để "cho qua" sẽ vô hiệu hoá toàn bộ cơ chế, và sẽ có người bị cám dỗ làm thế vào lúc gấp.

## Liên quan

- [`../database/script-runbook.md`](../database/script-runbook.md) — đường dẫn, đặt tên, thứ tự, lệnh sinh script
- [`../database/migration-policy.md`](../database/migration-policy.md) — ai sở hữu migration nào
- [`../RULES.md`](../RULES.md) — luật E8
- [`0008-core-so-huu-migration.md`](0008-core-so-huu-migration.md) — quyền sở hữu migration
