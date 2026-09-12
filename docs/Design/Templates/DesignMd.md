---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Khuôn — `DESIGN.md` của một dự án dựng trên Core

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> `DESIGN.md` của một dự án **không phải bản sao** của [`../DESIGN.md`](../DESIGN.md). Nó là **danh sách chênh lệch**: dự án này khác hệ nền ở chỗ nào.
>
> 🛑 **Ba khoá frontmatter ĐỔI khi dùng:** khuôn mang `scope: core`; file của dự án mang **`scope: du-an`**, `verified: chua-doi-chieu`.

---

## 1. Luật quan trọng nhất của khuôn này

**🛑 KHÔNG chép bảng token của Core sang file dự án.**

Cám dỗ rất lớn: chép toàn bộ bảng màu vào để "file này tự đứng được". Đừng.

Cái giá của việc chép, cụ thể:

| Hệ quả | Vì sao chắc chắn xảy ra |
| --- | --- |
| Hai bảng lệch nhau | Core sửa một giá trị, bản sao không ai sửa cùng lúc |
| Không ai biết bảng nào đúng | Cả hai đều trông chính thức, cả hai đều có nhãn |
| Agent đọc bản sao rồi dừng | Nó đã có câu trả lời tự tin, đầy đủ, nên **không bao giờ mở** file gốc |

Đây đúng khuôn mà [`../../../.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §3 và §5 cấm, và đó là luật đã trả giá thật ở dự án tiền nhiệm.

**File dự án chỉ chứa ba thứ:** cái gì bị ghi đè, cái gì thêm mới, và cái gì **cố ý không dùng**.

---

## 2. Dàn bài — chép từ đây xuống

### `# DESIGN — <tên dự án>`

Nhãn fidelity. Rồi một dòng: *"File này ghi chênh lệch so với [`../DESIGN.md`](../DESIGN.md). Mọi giá trị không nhắc tới ở đây đều lấy nguyên từ hệ nền."*

### `## Tổng quan`

Hai tới bốn câu: sản phẩm này là gì, ai dùng, nó khác một ứng dụng quản trị thông thường ở đâu về mặt giao diện.

### `## Ghi đè`

Bảng token bị đổi giá trị + **bảng kiểm tương phản đã tính lại**. Khuôn chi tiết ở [`Tokens.md`](./Tokens.md).

Không có gì ghi đè thì ghi `Không có — dùng nguyên bảng token nền.` Đó là một câu trả lời tốt, không phải một thiếu sót.

### `## Thêm mới`

Token riêng của dự án. Mỗi dòng nói **vì sao token nền không đủ**.

### `## Cố ý không dùng`

Mục hay bị bỏ quên và rất có giá trị. Ví dụ: *"Dự án này không có màn nào cần `Tabs`. Không dựng."*

Nó ngăn được câu hỏi *"tại sao chưa làm cái này"* lặp đi lặp lại, và ngăn người sau dựng một component không ai cần.

### `## Bố cục riêng`

Chỉ khi khung của dự án khác Core (thêm một cột phải, bỏ sidebar, có một thanh trạng thái dưới cùng). Không khác thì ghi `Giống hệ nền.`

### `## Do / Don't riêng`

Chỉ những luật **chỉ đúng với dự án này**. Luật chung ở [`../DESIGN.md`](../DESIGN.md) §9, không chép lại.

### `## Cần chốt`

### `## Lịch sử quyết định`

Bảng: quyết định gì, giá trị cũ → mới, vì sao. Đây là cái làm cho spec **quyết định được** thay vì chỉ ghi lại — xem [`../CLAUDE.md`](../CLAUDE.md) §5.

---

## 3. Ví dụ điền

```text
# DESIGN — Cổng dịch vụ công

📐 ĐÍCH ĐẾN — CHƯA THI CÔNG.

File này ghi chênh lệch so với docs/Design/DESIGN.md. Mọi giá trị không nhắc tới
ở đây đều lấy nguyên từ hệ nền.

## Tổng quan

Cổng tra cứu và nộp hồ sơ cho người dân. Khác ứng dụng quản trị nội bộ ở hai điểm
ảnh hưởng tới giao diện: người dùng vào một lần rồi thôi (không có thời gian học
giao diện), và một phần lớn truy cập từ điện thoại.

Hai hệ quả về thiết kế: cỡ chữ thân nâng một bậc so với hệ nền, và mọi vùng bấm
đạt tối thiểu 44×44px thay vì 24×24px.

## Ghi đè

| Token | Sáng | Tối | Dùng ở đâu |
| --- | --- | --- | --- |
| --color-brand | #RRGGBB | #RRGGBB | Nền nút primary, link, mục nav đang chọn, vòng focus |

Kiểm tương phản sau khi đổi:

| Cặp | Sáng | Tối | Ngưỡng | Đạt? |
| --- | --- | --- | --- | --- |
| --color-text-on-brand trên --color-brand | 7.31:1 | 8.02:1 | ≥ 4.5:1 | ✅ |

## Thêm mới

| Token | Giá trị | Vì sao token nền không đủ |
| --- | --- | --- |
| --size-touch-min | 44px | Hệ nền đặt 24px cho ứng dụng nội bộ dùng chuột. Cổng công dân dùng ngón tay |

## Cố ý không dùng

- Tabs — không màn nào có nhiều phần nội dung trong cùng một trang.
- Theme tối — người dùng vào một lần, không ai đổi theme. Vẫn khai đủ token
  để không phá hệ nền, nhưng không dựng nút chuyển.

## Bố cục riêng

Không có Sidebar. Điều hướng nằm hết trong Topbar vì cây menu chỉ hai mục.
```

---

## 4. Bắt buộc và tuỳ chọn

| Phần | Bắt buộc? | Ghi chú |
| --- | --- | --- |
| Ba khoá frontmatter | ✅ **Bắt buộc** | |
| Nhãn fidelity | ✅ **Bắt buộc** | |
| Dòng "file này ghi chênh lệch" | ✅ **Bắt buộc** | Không có nó, người đọc tưởng đây là bảng token đầy đủ |
| `## Tổng quan` | ✅ **Bắt buộc** | |
| `## Ghi đè` (kể cả khi rỗng) | ✅ **Bắt buộc** có tiêu đề | Rỗng thì ghi rõ là rỗng |
| Bảng kiểm tương phản khi có đổi màu | ✅ **Bắt buộc** | |
| `## Cố ý không dùng` | ✅ **Bắt buộc** có tiêu đề | Không có gì thì ghi `Không có` |
| `## Lịch sử quyết định` | ✅ **Bắt buộc** | Bắt đầu bằng một dòng "khởi tạo" |
| `## Bố cục riêng` | ⬜ Tuỳ chọn | Bỏ được khi khung giống hệt Core |
| `## Do / Don't riêng` | ⬜ Tuỳ chọn | Bỏ được khi không có luật riêng |
| Chép bảng token của Core | 🛑 **Cấm** | Xem §1 |
| Chép luật chung của Core | 🛑 **Cấm** | Trỏ đường, đừng chép |

---

## 5. Danh sách kiểm

- [ ] Không chép một dòng nào của bảng token Core.
- [ ] Mọi giá trị ghi đè đã tính lại tương phản, không dòng nào dưới ngưỡng.
- [ ] Mọi token thêm mới có lý do "vì sao token nền không đủ".
- [ ] Mục `Cố ý không dùng` có mặt.
- [ ] Mục `Lịch sử quyết định` có ít nhất một dòng.
- [ ] Không trích dẫn `src/` khi `src/` chưa có.
- [ ] `bash .claude/check-docs.sh` xanh.
