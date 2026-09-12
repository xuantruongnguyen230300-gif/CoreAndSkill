---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 08. Ghi lại quyết định kiến trúc (ADR)

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`.
>
> Danh sách ADR thật của repo và khuôn cụ thể đang dùng: [`../../adr/README.md`](../../adr/README.md). File này giữ **thói quen và lý do**, không giữ danh sách.

---

## 1. Quy trình ADR — khi nào, đánh số ra sao, lật thế nào

> 📖 **Khu ADR có README làm file chủ cho quy trình: [`../../adr/README.md`](../../adr/README.md).** Ở đó: ADR là gì · khi nào viết và khi nào KHÔNG · khuôn năm mục và khuôn rỗng để chép · đánh số và đặt tên file · cách lật một quyết định cũ · mục lục toàn bộ ADR.

File bạn đang đọc giữ phần **nghề viết**: viết thế nào cho ADR không rỗng, bốn sai lầm hay gặp, và nhóm ADR cho quyết định *không làm* — nhóm có khuôn riêng.

---

## 2. Viết ADR cho không rỗng

> 📖 **Khuôn rỗng để chép: [`../../adr/README.md`](../../adr/README.md) §7.** Mục này không chép lại khuôn — nó nói về **cách viết** từng mục cho khỏi rỗng.

### Ba mục hay bị viết hỏng

| Mục | Cách viết hỏng | Cách viết đúng |
| --- | --- | --- |
| **Bối cảnh** | Mô tả giải pháp trước khi mô tả vấn đề | Chỉ nêu tình huống và ràng buộc. Người đọc phải tự thấy vì sao cần quyết |
| **Phương án đã loại** | Dựng bù nhìn — liệt kê phương án hiển nhiên tệ để phương án đã chọn trông hay | Liệt kê phương án **thật sự hấp dẫn**, và nói rõ nó thua ở điểm nào. Không có phương án nào hấp dẫn thì có lẽ không cần ADR |
| **Hệ quả** | Chỉ ghi cái được | **Bắt buộc ghi cái mất.** Một ADR không có cái mất là một ADR chưa suy nghĩ xong |

Ví dụ về cái mất, viết đúng cách: quyết định *Core sở hữu migration của schema `core`* kéo theo hệ quả **`Core.Infrastructure` từ nay biết provider là PostgreSQL** — một sự đánh mất tính trung lập, có thật, phải ghi thẳng. Xem [`13-core-data-migration.md`](13-core-data-migration.md).

### Độ dài

Một ADR đọc hết trong vài phút. Dài hơn nghĩa là đang lẫn **quyết định** với **hướng dẫn thi công** — phần thi công thuộc [`../../quy-uoc/`](../../quy-uoc/), phần lý thuyết nền thuộc khu này.

---

## 3. Bốn sai lầm hay gặp

| Sai lầm | Vì sao hỏng |
| --- | --- |
| **Viết ADR sau khi đã code xong, cho khớp code** | ADR khi đó chỉ mô tả hiện trạng. Nó mất hẳn phần giá trị nhất: các phương án đã cân nhắc lúc còn cân nhắc được |
| **ADR chép nội dung của tài liệu quy ước** | Hai bản sao thì chúng sẽ lệch nhau. ADR ghi **quyết định và lý do**; cách làm nằm ở [`../../quy-uoc/`](../../quy-uoc/) |
| **Không ADR nào có mục "cái mất"** | Dấu hiệu ADR đang được dùng để hợp thức hoá, không phải để quyết định |
| **ADR không được ai đọc trước khi đề xuất một thay đổi lớn** | ADR chỉ có giá trị khi nó nằm trên đường đi của người sắp lật nó |

Chống sai lầm thứ tư bằng quy trình, không bằng lời nhắc: mọi thay đổi chạm `Core/` đi qua agent `architect`, và bước đầu của nó là đọc [`../../adr/README.md`](../../adr/README.md). Xem [`../../kien-truc-core-module.md`](../../kien-truc-core-module.md) §4.2.

---

## 3.1 ADR cho quyết định KHÔNG làm

Nhóm ADR này có khuôn hơi khác, và nó là nhóm giá trị nhất về lâu dài — vì một thứ *không tồn tại* thì không để lại dấu vết nào trong code để người sau hiểu.

| Mục | Nội dung riêng của nhóm này |
| --- | --- |
| Bối cảnh | Thứ đang được đề xuất là gì, nó hứa hẹn giải quyết gì |
| Quyết định | **Không làm X ở thời điểm này** |
| Phương án đã cân nhắc | Bao gồm cả phương án "làm ngay" — nêu đúng lợi ích của nó |
| Hệ quả | Cái mất khi không làm; và **rủi ro cụ thể sẽ phải chấp nhận** |
| **Điều kiện kích hoạt** | Sự kiện quan sát được nào khiến quyết định này phải xem lại |

Mục cuối là mục chỉ nhóm này mới có, và nó là mục quan trọng nhất. Không có nó, ADR biến thành một lời từ chối vĩnh viễn — và người sau sẽ hoặc phá luật, hoặc bỏ lỡ đúng thời điểm cần làm.

**Phân biệt hai loại "không":**

| Loại | Nghĩa | Có điều kiện kích hoạt? |
| --- | --- | --- |
| **Hoãn** | Chưa tới lúc | ✅ Bắt buộc |
| **Loại** | Hướng này sai, không phải sai thời điểm | ❌ Không có — thay vào đó nêu rõ vì sao nó sai về bản chất |

Gộp hai loại này lại là nguồn của những cuộc tranh luận lặp vô tận: một bên nhớ là "đã bác", một bên nhớ là "để sau".

---

## 4. ADR nằm ở đâu trong bức tranh chung

| Khu | Trả lời | Ví dụ |
| --- | --- | --- |
| [`../../adr/`](../../adr/) | **Vì sao** chọn thế này | Vì sao cookie chứ không JWT |
| [`../../RULES.md`](../../RULES.md) | Luật nào, **ép bằng gì** | Không có hằng số vai trò trong Core — canh bằng ArchTest |
| [`../../quy-uoc/`](../../quy-uoc/) | **Làm thế nào** | Viết một handler ra sao |
| `wiki-core/` (khu này) | **Một core tốt gồm gì** | Mô hình phân quyền có những dạng nào |
| [`../../audit/`](../../audit/) | **Chuyện gì đã hỏng** | Cầu nối dựng bằng reflection và hậu quả |

Một quyết định thường để lại dấu ở **ba** chỗ: ADR ghi lý do, `RULES.md` thêm một dòng luật kèm cột ép bằng gì, `quy-uoc/` mô tả cách làm. Thiếu dòng ở `RULES.md` thì quyết định không có ai canh; thiếu ADR thì luật không có ai bảo vệ khi bị chất vấn.

---

## 5. §Áp dụng

| Hạng mục | Trạng thái |
| --- | --- |
| Bộ ADR ban đầu cho các quyết định nền | 📐 xem [`../../adr/README.md`](../../adr/README.md) |
| Khuôn năm mục | 📐 áp cho mọi ADR mới |
| Lật bằng ADR mới, không sửa ADR cũ | 📐 luật, không có ngoại lệ |
| Mọi thay đổi chạm `Core/` phải có ADR | 📐 [`../../kien-truc-core-module.md`](../../kien-truc-core-module.md) §4.2 |

**Cách kiểm nhanh một ADR có đủ chất lượng chưa:** đưa cho một người không dự cuộc bàn. Họ đọc xong có nói lại được **vì sao phương án B bị loại** không? Không nói lại được thì ADR chưa viết xong.
