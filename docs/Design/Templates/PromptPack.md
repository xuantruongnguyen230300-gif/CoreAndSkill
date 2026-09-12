---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Khuôn — bộ prompt sinh màn hình

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Một bộ prompt là **bản dịch** của một screen spec sang thứ mà công cụ sinh giao diện đọc được.
>
> 🛑 **Ba khoá frontmatter ĐỔI khi dùng:** khuôn mang `kind: luat` / `scope: core`; bộ prompt của một dự án mang **`kind: tham-chieu`** và **`scope: du-an`** — nó là **sản phẩm sinh ra**, không phải luật. Đây là chỗ dễ đặt sai nhất trong cả khu Design.

---

## 1. Ba luật xương sống

### Luật 1 — bộ prompt là sản phẩm SINH RA, không phải nguồn

Nó được **sinh lại từ** screen spec. Sửa thẳng vào bộ prompt là tạo ra nguồn UI thứ hai — thứ mà [`../CLAUDE.md`](../CLAUDE.md) §1 cấm.

Thấy bộ prompt sai → **sửa screen spec rồi sinh lại**. Đừng vá tại chỗ.

Ghi ngay đầu file:

> Sinh từ `Screens/<tên>.md`. Sửa spec rồi sinh lại; đừng sửa file này.

### Luật 2 — token phải giải thành giá trị thật

Công cụ ngoài **không hiểu** `var(--color-brand)` và không đọc được [`../DESIGN.md`](../DESIGN.md). Prompt phải mang mã màu thật, số px thật, tên font thật.

**Đây là ngoại lệ duy nhất được phép của luật "không chép giá trị token"** ([`../CLAUDE.md`](../CLAUDE.md) §8), và nó hợp lệ vì đủ ba điều:

| Điều kiện | Vì sao đủ để miễn trừ |
| --- | --- |
| Bản chép là **sản phẩm sinh ra**, không phải nguồn | Sinh lại là xoá sạch bản cũ, nên nó không thể trôi lặng lẽ |
| Người đọc là **máy**, không phải người | Không ai đọc bộ prompt để tra token |
| Có **hạn dùng** ghi ngay trong file | Xem luật 3 |

### Luật 3 — bộ prompt có hạn dùng

Ghi ngay đầu file: **sinh ngày nào, từ bản spec nào**. Spec đổi thì bộ prompt hết hạn.

Không có dòng đó, bộ prompt biến thành một bảng token cũ nằm mãi trong repo, và một ngày nào đó có người chép ngược nó ra làm giá trị thật.

---

## 2. Dàn bài — chép từ đây xuống

### `# Bộ prompt — <tên luồng>`

Ba dòng bắt buộc: sinh từ file nào, sinh ngày nào, và câu "sửa spec rồi sinh lại".

### `## Prompt gốc`

Một khối **tự đứng được**, không phụ thuộc công cụ nào. Đây là phần chính; các mục sau chỉ là biến thể của nó.

### `## Biến thể theo công cụ`

Một mục con cho mỗi công cụ đội thật sự dùng. 🛑 **Không viết sẵn mục cho công cụ chưa ai dùng** — đó là chỗ để tài liệu mục ruỗng.

### `## Tệp cần đính kèm`

Chỉ liệt kê tệp **có thật trên đĩa**. Chưa có ảnh thì ghi `Chưa có ảnh nào`.

### `## Cần chốt`

---

## 3. Khuôn của prompt gốc — sáu khối

```text
Dựng lại đúng màn hình được mô tả dưới đây. Không tự ý cải tiến.

TOKEN (giá trị thật, không phải tên biến):
  màu chính #......, nền trang #......, nền thẻ #......,
  chữ #......, chữ phụ #......, viền #......,
  font "<họ font>", cỡ chữ thân <n>px, bo góc <n>px, đệm thẻ <n>px

BỐ CỤC:
  <cây vùng viết thành văn xuôi: vùng nào, rộng bao nhiêu, ghép component nào, theo thứ tự nào>

CÂU CHỮ (chép nguyên văn):
  <mọi câu người dùng đọc được, lấy từ bảng Câu chữ của screen spec>

TRẠNG THÁI:
  mặc định / đang tải / rỗng / lỗi — mỗi cái trông thế nào

RESPONSIVE:
  <hành vi ở từng ngưỡng>

RÀNG BUỘC:
  - Chỉ dùng màu trong khối TOKEN. Không thêm màu nào khác.
  - Không thêm component nào không có trong khối BỐ CỤC.
  - Không đổi câu chữ, không "viết lại cho hay hơn".
```

**Vì sao khối RÀNG BUỘC quan trọng:** không có nó, công cụ sinh giao diện sẽ tự thêm màu gradient, tự đổi câu chữ cho "chuyên nghiệp hơn", tự chèn một biểu đồ không ai yêu cầu. Kết quả trông đẹp và **không phải sản phẩm này**.

**Vì sao "câu chữ chép nguyên văn":** câu chữ là thứ đã qua nghiệp vụ và i18n. Một công cụ viết lại "Thêm người dùng" thành "Tạo tài khoản mới" là đã đổi nghiệp vụ.

---

## 4. Bẫy lớn nhất: ảnh sinh ra KHÔNG phải sản phẩm

Ảnh mà một công cụ sinh ra từ bộ prompt là **phác thảo**, không phải ảnh chụp sản phẩm.

🛑 **Không bao giờ đưa ảnh sinh ra vào mục `Ảnh màn hình` của screen spec.** Mục đó chỉ nhận ảnh chụp từ ứng dụng đang chạy.

🛑 **Không đem ảnh sinh ra đi duyệt như thể đó là sản phẩm.** Đây là hậu quả nặng nhất của việc lẫn hai vai fidelity ở [`../CLAUDE.md`](../CLAUDE.md) §2: người duyệt tưởng mình vừa xem sản phẩm thật, và duyệt một thứ chưa ai xây.

Ảnh sinh ra nếu giữ lại thì để trong thư mục riêng, có nhãn rõ ràng là phác thảo, và **không được screen spec nào trỏ tới**.

---

## 5. Bắt buộc và tuỳ chọn

| Phần | Bắt buộc? | Ghi chú |
| --- | --- | --- |
| Ba khoá frontmatter, `kind: tham-chieu` | ✅ **Bắt buộc** | |
| Dòng "sinh từ file nào, ngày nào" | ✅ **Bắt buộc** | Không có nó, bộ prompt không có hạn dùng |
| Dòng "sửa spec rồi sinh lại" | ✅ **Bắt buộc** | |
| Prompt gốc đủ sáu khối | ✅ **Bắt buộc** | |
| Token giải thành giá trị thật | ✅ **Bắt buộc** | Công cụ ngoài không đọc được tên biến |
| Khối RÀNG BUỘC | ✅ **Bắt buộc** | Thiếu nó là mở cửa cho công cụ tự sáng tác |
| Câu chữ chép nguyên văn | ✅ **Bắt buộc** | |
| Biến thể cho công cụ đội đang dùng | ⬜ Tuỳ chọn | Không dùng công cụ nào thì bỏ cả mục |
| Khối CSS custom property cho công cụ nhận được | ⬜ Tuỳ chọn | |
| Danh sách tệp đính kèm | ⬜ Tuỳ chọn | Chưa có ảnh thì ghi rõ là chưa có |
| Mục cho công cụ chưa ai dùng | 🛑 **Cấm** | |
| Ảnh sinh ra được screen spec trỏ tới | 🛑 **Cấm** | Xem §4 |

---

## 6. Danh sách kiểm

- [ ] `kind: tham-chieu`, không phải `kind: luat`.
- [ ] Có dòng "sinh từ file nào, ngày nào".
- [ ] Mọi token đã giải thành giá trị thật — không còn tên biến nào trong prompt.
- [ ] Giá trị đó khớp bản [`../DESIGN.md`](../DESIGN.md) tại ngày sinh.
- [ ] Có khối RÀNG BUỘC.
- [ ] Câu chữ chép nguyên văn từ bảng Câu chữ của screen spec.
- [ ] Không mục nào cho công cụ chưa ai dùng.
- [ ] Không tệp đính kèm nào không tồn tại.
- [ ] `bash .claude/check-docs.sh` xanh.
