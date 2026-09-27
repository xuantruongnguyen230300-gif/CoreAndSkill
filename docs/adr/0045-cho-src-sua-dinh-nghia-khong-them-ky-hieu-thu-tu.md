---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0045 — Ký hiệu `⏳ chờ `src/`` giữ nguyên chữ và glyph, chỉ sửa **định nghĩa** cho hết neo vào "giai đoạn 1"; ký hiệu thứ tư bị loại

> **Trạng thái:** Đã chấp nhận (2026-09-20) · Bổ sung [`0043-lenh-do-cho-phai-lat-doi-sang-nhan-dich-den.md`](0043-lenh-do-cho-phai-lat-doi-sang-nhan-dich-den.md)

## Bối cảnh

Cột *Chặn bởi* trong danh sách nợ ở [`../RULES.md`](../RULES.md) §10 là **từ vựng có kiểm soát** ba giá trị. Người đọc lọc theo glyph, và hai lệnh đếm ngay trên bảng neo vào chuỗi trong ô: một lệnh đếm ô chứa `src/`, một lệnh đếm ô mở đầu bằng `🔧`.

Đợt review BE ngày 2026-09-20 nêu hai dòng nợ mới B9 và B10 mang `⏳ chờ `src/``, trong khi `src/BE` đã có trên đĩa — thứ chặn chúng thật ra là *chưa có module nào*. Review đề xuất thêm một giá trị thứ tư cho loại *chờ đối tượng đầu tiên xuất hiện* rồi chuyển hai dòng đó sang, hoặc sửa định nghĩa của ký hiệu cũ; và khuyến nghị gộp việc này vào lượt rà tồn đọng theo [ADR-0043](0043-lenh-do-cho-phai-lat-doi-sang-nhan-dich-den.md) tầng B.

Ba phép đo chạy trên repo này ngày 2026-09-20 đã đổi kết luận so với đề xuất đó.

**Đo thứ nhất — phạm vi thật.** Lệnh đếm ở đầu §10 trả về **27** dòng mang ký hiệu này, không phải hai. B9 và B10 không phải ca đặc biệt; chúng là hai ca mới nhất của một nhãn đã cũ đi từ lúc `src/` xuất hiện. Một phần lớn trong 27 dòng đó nay có thể dựng cổng được — nhưng **chưa ai mở từng dòng ra đối chiếu**.

**Đo thứ hai, và là đo lật ngược khuyến nghị gộp lượt.** Lệnh tầng B của ADR-0043 **loại trừ tường minh** [`../RULES.md`](../RULES.md) khỏi tập đầu vào, và tệp này không mang chuỗi nhãn `📐 ĐÍCH ĐẾN` ở đâu cả — cột trạng thái của nó dùng glyph trần. Cả hai điều kiện cộng lại nghĩa là tầng B **sẽ không bao giờ in ra** §10 của tệp này. Loại trừ đó có chủ ý và vẫn đúng: [ADR-0030](0030-dieu-kien-chuyen-giai-doan-2.md) xếp tệp luật vào mục *không lật hàng loạt*, vì mỗi dòng của nó chuyển trạng thái khi **cổng của chính dòng đó** chạy. Hoãn F7 sang tầng B do đó không phải là hoãn — là bỏ.

**Đo thứ ba — cái giá của ký hiệu thứ tư.** Hai lệnh đếm hiện có neo vào `src/` và `🔧`. Một giá trị thứ tư không chứa `src/` sẽ nằm ngoài cả hai, thành nhóm thứ hai không lệnh nào đếm — §10 đã có một nhóm như thế (`🚫`) và đã phải viết một đoạn cảnh báo đừng cộng hai con số lại.

## Quyết định

1. **Ký hiệu giữ nguyên glyph và giữ nguyên chuỗi `src/` trong ô.** Hai lệnh đếm ở §10 không đổi đầu vào, không đổi kết quả.
2. **Định nghĩa của ký hiệu trong bảng chú giải viết lại để mô tả *điều kiện thật*** — cổng cần một đối tượng thật để đối chiếu mà đối tượng đó chưa tồn tại — thay cho lối neo vào mốc "giai đoạn 1".
3. **B9 và B10 ở nguyên chỗ**, giữ phần chua *cụ thể: chờ module đầu tiên*.
4. **Ký hiệu thứ tư bị loại.**
5. **Việc phải làm thật — mở từng dòng trong 27 dòng đối chiếu với `src/` đã có — ghi thành một đoạn có lệnh ngay dưới bảng chú giải**, nói rõ tầng B của ADR-0043 không phủ tệp này.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Thêm giá trị thứ tư và chuyển B9, B10 sang

Đề xuất thứ nhất của lượt review.

**Được:** ô của hai dòng đó thành đúng nghĩa đen. Người đọc lọc glyph phân biệt được *chờ hạ tầng* với *chờ một đối tượng nghiệp vụ*.

**Vì sao loại:** hai cái giá, cái thứ hai nặng hơn hẳn.

Thứ nhất, nhóm mới nằm ngoài cả hai lệnh đếm, thành vùng mù thứ hai trong cùng một bảng.

Thứ hai — và đây là lý do quyết định — **chuyển đúng hai dòng làm 25 dòng còn lại trông như đã được soát.** Hôm nay cả 27 dòng mang một nhãn giống hệt nhau, và sự đồng nhất đó tự nó nói rằng chưa ai soát từng dòng. Sau khi hai dòng được tách ra có chủ đích, cột đó đọc như một cột đã phân loại cẩn thận, trong khi 25 dòng chưa ai mở. Đó đúng là khuôn mà [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §4 cấm: làm một chỉ số đẹp lên bằng thao tác không phải là việc kiểm.

### Phương án B — Gộp vào lượt rà tồn đọng theo ADR-0043 tầng B

Khuyến nghị của lượt review.

**Được:** không làm lẻ, tránh sửa vặt một bảng mà cả cột đang cũ.

**Vì sao loại:** đã đo, và tầng B không với tới được tệp này — lệnh của nó loại trừ tường minh, còn tệp thì không mang chuỗi nhãn mà lệnh dò. Nhận khuyến nghị này là gửi một việc vào một hàng đợi không tồn tại. Nguyên tắc *đừng để nó tan biến* mà chính lượt review đặt ra là thứ loại phương án này.

### Phương án C — Đổi nhãn cho cả 27 dòng trong lượt này

**Được:** hết cũ một lần, không để lại nợ.

**Vì sao loại:** 27 dòng nằm rải khắp §10, và phần lớn thuộc phạm vi mà `backend-expert` đang cầm song song trong cùng đợt. Nặng hơn: đổi nhãn đúng cho một dòng đòi mở đúng luật đó, xem cổng của nó dựng được chưa với `src/` hôm nay — tức là **27 lần phán đoán kỹ thuật**, không phải một thao tác thay chuỗi. Làm gộp trong một lượt viết tài liệu là cách để 27 phán đoán đó không cái nào được làm thật.

## Hệ quả

### Tích cực

- Định nghĩa ký hiệu hết mâu thuẫn với hiện trạng, và hết neo vào mốc giai đoạn mà [ADR-0043](0043-lenh-do-cho-phai-lat-doi-sang-nhan-dich-den.md) đã chỉ ra là đầu vào sai.
- Hai lệnh đếm giữ nguyên, nên không mục cổng hay thói quen đọc nào phải sửa theo.
- Tồn đọng thật — 27 dòng chưa soát — thành một đoạn có lệnh chạy được, kèm lời khai rằng không lượt rà nào khác phủ nó.

### Tiêu cực

- **Chữ trên ký hiệu vẫn không đúng nghĩa đen với B9, B10.** Ô vẫn đọc là *chờ `src/`* trong khi `src/` đã có; người đọc phải đọc tới phần chua và tới định nghĩa mới hiểu đúng. Đây là cái giá trả để giữ hai lệnh đếm và để không giả vờ đã phân loại.
- 27 dòng vẫn mang một nhãn chưa ai đối chiếu. ADR này làm nợ đó **nhìn thấy được**, không làm nó nhỏ đi.
- Thêm một đoạn văn dưới bảng chú giải làm §10 dài thêm, trong khi §10 vốn đã là mục dài nhất của tệp.

## Liên quan

- [ADR-0043](0043-lenh-do-cho-phai-lat-doi-sang-nhan-dich-den.md) — lệnh tầng A/B và bốn điều kiện loại trừ của nó; ADR này bổ sung bằng cách chỉ ra một vùng mà tầng B cố ý không phủ.
- [ADR-0030](0030-dieu-kien-chuyen-giai-doan-2.md) — mục *không lật*, nguồn của nguyên tắc mỗi dòng luật chuyển trạng thái theo cổng của chính nó.
