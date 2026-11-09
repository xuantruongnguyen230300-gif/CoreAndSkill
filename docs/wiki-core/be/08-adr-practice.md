---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 08. Ghi lại quyết định kiến trúc (ADR)

> 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Tệp này áp cho các ADR ở `docs/adr/`, không mô tả code; chưa ai đối chiếu các ADR hiện có với nó (2026-09-24).
>
> Danh sách ADR thật của repo và khuôn cụ thể đang dùng: [`../../adr/README.md`](../../adr/README.md). File này giữ **thói quen và lý do**, không giữ danh sách.

---

## 1. Quy trình ADR — khi nào, đánh số ra sao, lật thế nào

> 📖 **Khu ADR có README làm file chủ cho quy trình: [`../../adr/README.md`](../../adr/README.md).** Ở đó: ADR là gì · khi nào viết và khi nào KHÔNG · khuôn năm mục và khuôn rỗng để chép · đánh số và đặt tên file · cách lật một quyết định cũ · mục lục toàn bộ ADR.

File bạn đang đọc giữ phần **nghề viết**: viết thế nào cho ADR không rỗng, bốn sai lầm hay gặp, và nhóm ADR cho quyết định *không làm* — nhóm có khuôn riêng.

---

## 2. Viết ADR cho không rỗng

> 📖 **Khuôn rỗng để chép: [`../../adr/README.md`](../../adr/README.md) §7.** Mục này không chép lại khuôn — nó nói về **cách viết** từng mục cho khỏi rỗng.

> 📖 **Từng mục hay bị viết hỏng thế nào: cột "Bẫy thường gặp" ở [`../../adr/README.md`](../../adr/README.md) §3.** Mục này không giữ bảng thứ hai.

Ví dụ về cái mất của mục **Hệ quả**, viết đúng cách: quyết định *Core sở hữu migration của schema `core`* kéo theo hệ quả **`Core.Infrastructure` từ nay biết provider là PostgreSQL** — một sự đánh mất tính trung lập, có thật, phải ghi thẳng. Xem [`13-core-data-migration.md`](13-core-data-migration.md).

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

Chống sai lầm thứ tư bằng quy trình, không bằng lời nhắc: việc mang dấu hiệu cần quyết định — dấu hiệu ở [`../../adr/README.md`](../../adr/README.md) §2 — và việc đưa code mới vào Core đi qua agent `architect` trước khi thi công, và bước đầu của nó là đọc [`../../adr/README.md`](../../adr/README.md). Việc chạm Core nào qua `architect`, việc nào thi công thẳng rồi qua `core-reviewer`: [`../../kien-truc-core-module.md`](../../kien-truc-core-module.md) §4.2.

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

Một quyết định thường để lại dấu ở **ba** chỗ: ADR ghi lý do, một tệp `RULES-*.md` thêm một dòng luật kèm cột ép bằng gì, `quy-uoc/` mô tả cách làm. Thiếu dòng ở `RULES-*.md` thì quyết định không có ai canh; thiếu ADR thì luật không có ai bảo vệ khi bị chất vấn.

---

## 5. §Áp dụng

| Hạng mục | Trạng thái |
| --- | --- |
| Bộ ADR của repo | ✅ CÓ THẬT (đối chiếu 2026-10-05) — mục lục `docs/adr/muc-luc.md:13`; đếm bằng `ls docs/adr/[0-9][0-9][0-9][0-9]-*.md \| wc -l` |
| Khuôn năm mục, áp cho mọi ADR mới | ✅ CÓ THẬT (đối chiếu 2026-10-05) — luật ở `docs/adr/README.md:56`. Không cổng máy nào kiểm một ADR đủ năm mục; ADR đã có chưa ai đối chiếu với khuôn (khối đầu tệp này) |
| Lật bằng ADR mới, không sửa ADR cũ | ✅ CÓ THẬT (đối chiếu 2026-10-05) — luật ở `docs/adr/README.md:90`, không có ngoại lệ |
| Việc nào cần ADR — kể cả việc chạm Core | ✅ CÓ THẬT (đối chiếu 2026-10-05) — cần khi mang ít nhất một dấu hiệu ở `docs/adr/README.md:33`; việc chạm Core không mang dấu hiệu nào thì không cần ADR — `docs/kien-truc-core-module.md:114` |

**Cách kiểm nhanh một ADR có đủ chất lượng chưa:** đưa cho một người không dự cuộc bàn. Họ đọc xong có nói lại được **vì sao phương án B bị loại** không? Không nói lại được thì ADR chưa viết xong.
