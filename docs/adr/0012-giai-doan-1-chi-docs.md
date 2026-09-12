---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0012 — Giai đoạn 1 chỉ xây `docs/` và `.claude/`; `src/` làm sau và bám theo tài liệu

> **Trạng thái:** Đã chấp nhận (2026-09-08)

## Bối cảnh

CoreAndSkill không phải dự án xây mới. Nó trích xuất từ một Core đã chạy thật ở dự án tiền nhiệm, nơi phần lớn tri thức đã nằm sẵn trong tài liệu — và tài liệu đó đã tự đánh dấu bằng khoá phạm vi trong frontmatter phần nào đi theo khi tách, phần nào ở lại.

Nhưng bản trích xuất không thể chép nguyên. Một loạt quyết định của dự án tiền nhiệm bị lật: ranh giới project, cơ chế lỗi, phân quyền, quyền sở hữu migration, chính sách comment, và việc có CI hay không. Nếu code được viết trước khi những quyết định đó được ghi thành tài liệu, thì cái được viết sẽ là **thói quen cũ**, và tài liệu sau đó sẽ chỉ mô tả lại thói quen ấy thay vì điều khiển nó.

Có thêm một ràng buộc thực tế: khi bắt đầu, repo này chưa có `src/`. Nghĩa là không có gì để đối chiếu.

## Quyết định

- Giai đoạn 1 xây **`docs/` và `.claude/`**. Không viết `src/`.
- `src/` xây ở giai đoạn 2, và **bám theo `docs/`** — tài liệu là bản thiết kế, không phải bản mô tả viết sau.
- **Mọi mô tả kiến trúc trong `docs/` mang nhãn `📐 ĐÍCH ĐẾN — CHƯA THI CÔNG`**, đặt ở đầu file hoặc đầu mục.
- **Mọi file mang `verified: chua-doi-chieu`.** Đây là giá trị trung thực, không phải nợ — chưa có source thì không có gì để đối chiếu.
- **Cấm mọi tuyên bố hoàn thành** dạng đã có, đã xong, đã bật, đã sửa cho bất cứ thứ gì thuộc `src/`. Cổng tài liệu kiểm điều này qua luật D6.
- **Cấm trích dẫn neo vào `src/`.** Không có `src/` thì mọi trích dẫn kiểu đó là bằng chứng bịa; luật D4 và D7 bắt được.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Viết code trước, tài liệu sau

**Được:** có thứ chạy được sớm; tài liệu viết sau thì chắc chắn khớp với code vì nó chép từ code ra.

**Vì sao loại:** với một bộ khung, tài liệu **không phải bản mô tả — nó là hợp đồng**. Nếu nó chép từ code thì nó không thể mâu thuẫn với code, và một tài liệu không bao giờ mâu thuẫn với code cũng không bao giờ **chặn** được code. Toàn bộ tầng cổng và tầng agent kiểm agent dựa trên việc có một chuẩn độc lập để đối chiếu; chuẩn sinh ra từ chính thứ nó kiểm thì không kiểm được gì.

Riêng ở dự án này còn một lý do cụ thể hơn: đang có sáu quyết định lật ngược cách làm cũ. Viết code trước là viết theo quán tính cũ.

### Phương án B — Làm song song, mỗi tuần một ít của cả hai

**Được:** phản hồi hai chiều sớm; tài liệu được kiểm chứng ngay bởi code.

**Vì sao loại:** khi hai bên cùng chưa ổn định, mỗi lần đổi một bên là một lần phải sửa bên kia, và không bên nào là nguồn sự thật. Kinh nghiệm ở dự án tiền nhiệm cho thấy đúng hình dạng thất bại này: một tài liệu khẳng định một loạt project đã tồn tại trong khi chưa có project nào, và một công thức kỹ thuật sai tồn tại song song ở hai chỗ nên sửa nơi này không chạm nơi kia. Cả hai đều sinh ra từ việc có hai bản của cùng một sự thật.

### Phương án C — Chỉ chép tài liệu cũ sang, không viết mới

**Được:** nhanh nhất, và tài liệu cũ đã được kiểm chứng bởi một hệ đang chạy.

**Vì sao loại:** tài liệu cũ mô tả những quyết định đã bị lật. Chép nguyên là mang theo cả cơ chế lỗi cũ, cả role cứng, cả quyền sở hữu migration ở host — rồi giai đoạn 2 sẽ vừa viết code vừa phải chỉnh tài liệu, tức quay về phương án B với thêm gánh nặng dọn dẹp.

## Hệ quả

### Tích cực

- **Sáu quyết định lật ngược được ghi và soi kỹ trước khi có dòng code nào theo chúng.** Đây là lúc rẻ nhất để phát hiện một quyết định sai.
- **Có sẵn chuẩn để đối chiếu ngay từ dòng code đầu tiên** của giai đoạn 2, nên cổng và agent kiểm có việc để làm ngay.
- Cổng tài liệu chạy được từ tuần đầu, nên chất lượng tài liệu không trôi trong lúc chờ code.

### Tiêu cực — cái giá thật

- **Tài liệu chưa được kiểm chứng bởi bất cứ thứ gì chạy được.** Một quy ước có thể hợp lý trên giấy mà không viết nổi thành code — và điều đó chỉ lộ ra ở giai đoạn 2. Nhãn `📐 ĐÍCH ĐẾN` và `verified: chua-doi-chieu` tồn tại để nhắc rằng rủi ro này chưa được đóng.
- **Skill sinh code phải hoãn sang giai đoạn 2.** Một skill sinh code về bản chất là máy sao chép có sửa tên, nên nó cần một bản gốc thật. Chưa có module mẫu thì viết skill là viết theo trí nhớ về một thứ chưa tồn tại, và sản phẩm sẽ là code không build được mà không có đáp án đúng nào để đối chiếu khi đi sửa. Skill **đọc và kiểm** thì không vướng ràng buộc này và viết được ngay.
- **Không có gì chạy được để trình bày**, trong khi công sức bỏ ra là thật. Áp lực "cho thấy tiến độ" sẽ đẩy về phía viết code sớm — cần biết trước điều đó.
- **Sẽ có tài liệu viết ra rồi phải sửa** khi giai đoạn 2 chứng minh nó sai. Đó là chi phí chấp nhận, nhưng nó là chi phí thật, và mỗi lần sửa lại là một lần phải soát các file khác trỏ tới nó.

## Liên quan

- [`../README.md`](../README.md) — mục trạng thái repo và bảng trạng thái cấp khu
- [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) — luật nhãn trạng thái và ba khoá frontmatter
- [`../RULES.md`](../RULES.md) — luật D4, D6, D7
