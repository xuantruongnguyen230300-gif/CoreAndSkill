---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `N6` — Ghi nhật ký kiểm toán

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Bảng nhật ký kiểm toán đã được đặc tả đầy đủ. **Không endpoint nào đọc nó.** Đó là phát hiện chính của luồng này — xem §6.

---

## 1. Ai bắt đầu, ở đâu

**Không ai trực tiếp.** Nhật ký sinh ra như tác dụng phụ của những thao tác được xếp là đáng ghi lại.

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Một thao tác thuộc diện phải ghi | Danh sách thao tác đó nằm ở [`../wiki-core/be/10-data-retention.md`](../wiki-core/be/10-data-retention.md) §5 |
| Ngữ cảnh đơn vị và người thực hiện | Bản ghi mang cả `tenant_id` lẫn định danh người thao tác |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | Handler | Thực hiện thao tác nghiệp vụ | [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) |
| 2 | Hệ thống | Ghi một dòng vào bảng nhật ký: thời điểm, người thao tác, mã hành động, đối tượng, giá trị trước và sau, địa chỉ IP, mã lần gọi | [`../database/schema-core.md`](../database/schema-core.md) §9.4 |
| 3 | Hệ thống | **Không bao giờ** sửa hoặc xoá dòng đã ghi | cùng trên §3 |
| 4 | Người kiểm toán | Đọc lại nhật ký | 🛑 **chưa có đường** — xem §6 |

### Vì sao bảng này chỉ được ghi thêm

Sửa hay xoá một dòng nhật ký là **phá bằng chứng**. Bảng mang ràng buộc khoá ngoại tới đơn vị ở dạng `ON DELETE RESTRICT`, nghĩa là **không xoá được một đơn vị khi nhật ký của nó còn** — đó là chủ đích, không phải phiền toái: một đơn vị biến mất cùng toàn bộ dấu vết của nó là thứ không ai điều tra được.

Nó cũng khớp với luồng `V3`: đơn vị **ngưng hoạt động**, không bị xoá.

### Thời gian giữ

Nhật ký giữ **dài hơn hẳn** dữ liệu nghiệp vụ, vì lý do tuân thủ chứ không vì lý do kỹ thuật ([`../wiki-core/be/10-data-retention.md`](../wiki-core/be/10-data-retention.md) §5). Nghĩa là bảng này sẽ là **bảng lớn nhất hệ** theo thời gian, và chỉ mục của nó đã được đặt theo `(tenant_id, occurred_at DESC)` cho đúng kiểu truy vấn *"xem gần đây nhất của đơn vị này"*.

## 4. Hỏng ở đâu — và ai thấy gì

| Ca | Biểu hiện |
| --- | --- |
| Quên ghi nhật ký cho một thao tác | **Không ai thấy gì**, và chỉ phát hiện khi cần điều tra một sự việc — tức đúng lúc không còn cứu được |
| Ghi nhật ký **ngoài** transaction của thao tác | Thao tác rollback mà nhật ký vẫn ghi ⇒ bằng chứng về một việc chưa xảy ra. Cùng lớp lỗi với bước 2 của luồng `N5` |
| Bảng phình quá lớn | Truy vấn danh sách chậm dần. Chỉ mục đã đặt đúng chiều, nhưng không có cơ chế chuyển dữ liệu cũ sang kho lạnh |

## 5. Quan hệ với đơn vị

**Thuộc đơn vị.** Bản ghi mang `tenant_id`, chỉ mục dẫn đầu bằng `tenant_id`, khoá ngoại tới bảng đơn vị ở dạng `ON DELETE RESTRICT`.

Một câu cần cẩn thận: bản ghi ghi lại **người thao tác** và **đối tượng bị tác động**. Hai thứ đó thuộc cùng một đơn vị trong mọi luồng hiện có — **trừ** luồng `V2` và `V3`, nơi người thao tác là tài khoản vận hành thuộc **đơn vị hệ thống** còn đối tượng là **một đơn vị khác**. Xem §6.

## 6. Câu chưa trả lời được

> 🛑 **Bảng nhật ký đã đặc tả đủ cột, đủ chỉ mục, đủ ràng buộc — nhưng không endpoint nào đọc nó.**

Quét toàn bộ `contracts/`: không có đường dẫn nào dưới `/audit`. Nghĩa là hôm nay nhật ký kiểm toán là thứ **chỉ ghi vào, không đọc ra** trừ khi có người mở database bằng tay. Với một bảng tồn tại vì lý do tuân thủ, đó là một nửa cơ chế.

Ba câu còn lại:

1. **Ai được đọc nhật ký?** Chưa có khoá quyền nào cho việc này trong danh mục ở [`../database/schema-core.md`](../database/schema-core.md) §5.
2. **Thao tác của tài khoản vận hành ghi vào đơn vị nào** — đơn vị hệ thống của người thao tác, hay đơn vị bị tác động? Chọn sai thì việc ngưng hoạt động một đơn vị sẽ **không hiện** trong nhật ký của chính đơn vị đó.
3. **Danh sách thao tác phải ghi có gồm việc ĐỌC không?** Hiện nó thiên về thao tác ghi. Nhưng xuất dữ liệu (luồng `N3`) và tải tệp (luồng `N2`) là hai thao tác **đọc** mang dữ liệu ra khỏi hệ — và chúng thường là thứ người điều tra cần nhất.
