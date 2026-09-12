---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Khuôn — nhật ký xuất bản thiết kế

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Nhật ký này trả lời một câu mà không nguồn nào khác trả lời được: **cái đang nằm trong công cụ thiết kế tương ứng với bản tài liệu nào của repo.**
>
> 🛑 **Ba khoá frontmatter ĐỔI khi dùng:** khuôn mang `kind: luat` / `scope: core`; nhật ký của một dự án mang **`kind: tham-chieu`** và **`scope: du-an`** — nó ghi lại việc đã xảy ra, không đặt ra luật.

---

## 1. Vấn đề nhật ký này giải

Khi thiết kế được xuất sang một công cụ ngoài (một file thiết kế dùng chung, một thư viện token, một bộ ảnh cho tài liệu hướng dẫn), lập tức có **hai bản** của cùng một thiết kế:

| Bản | Sống ở | Đổi khi nào |
| --- | --- | --- |
| Bản nguồn | `docs/Design/` | Mỗi lần có người sửa spec |
| Bản đã xuất | Công cụ ngoài | Chỉ khi có người xuất lại |

Bản thứ hai **luôn cũ hơn**, và không ai biết cũ bao nhiêu. Người thiết kế mở file trong công cụ ra xem, thấy một màu, và tin rằng đó là màu hiện hành.

Nhật ký này không sửa được việc bản xuất bị cũ — không có gì sửa được điều đó. Nó làm một việc khiêm tốn hơn và đủ dùng: **nói cho biết bản xuất cũ tới mức nào**.

---

## 2. Luật: chỉ được thêm dòng

**Nhật ký là append-only.** Không sửa dòng cũ, không xoá dòng cũ, không sắp xếp lại.

Xuất sai → **thêm một dòng sửa chữa** và ghi vào cột ghi chú rằng nó thay cho dòng nào.

**Vì sao:** một nhật ký sửa được thì không phải nhật ký, mà là một bảng trạng thái. Bảng trạng thái chỉ nói hiện tại; nhật ký nói *đã đi qua những đâu* — và câu hỏi thường gặp nhất khi có sự cố là *"lần cuối cái này đúng là bao giờ"*.

---

## 3. Dàn bài — chép từ đây xuống

### `# Nhật ký xuất bản — <tên dự án>`

Một câu: xuất đi đâu, ai giữ đích đến đó.

### `## Đích đến`

Bảng: xuất sang những nơi nào, ai chịu trách nhiệm mỗi nơi, và **cái gì ở đó là nguồn** (câu trả lời luôn là: không cái nào — nguồn ở `docs/Design/`).

### `## Quy trình xuất`

Các bước, theo thứ tự. Ngắn gọn.

### `## Nhật ký`

Bảng chính. Chỉ thêm dòng.

---

## 4. Bảng nhật ký — bảy cột

| Ngày | Xuất gì | Đích | Neo nguồn | Cổng tài liệu | Người làm | Ghi chú |
| --- | --- | --- | --- | --- | --- | --- |
| 2026-01-15 | token màu + chữ | Thư viện token dùng chung | `git rev-parse --short HEAD` tại lúc xuất | xanh | — | Lần xuất đầu |
| 2026-02-03 | token màu | Thư viện token dùng chung | *(mã commit)* | xanh | — | Thay dòng 2026-01-15 cho phần màu: `--color-warning` xuất sai một bậc |

Bốn luật cho bảng:

1. **Cột "Neo nguồn" phải là thứ tra ngược được.** Mã commit là tốt nhất — nó xác định chính xác bản `docs/Design/` tại thời điểm đó. Một câu như "bản mới nhất" là vô dụng sau hai tuần.
2. **Cột "Cổng tài liệu" ghi kết quả `bash .claude/check-docs.sh` tại lúc xuất.** Xuất khi cổng đỏ là xuất một bản tài liệu đang hỏng ra ngoài.
3. **Cột "Xuất gì" phải cụ thể.** "Toàn bộ" không cho biết gì; "token màu + chữ, không xuất spec component" thì có.
4. **Không ghi tên tài khoản, khoá truy cập, đường dẫn có chứa token xác thực.**

---

## 5. Bốn điều nhật ký này không làm

| Không làm | Ở đâu thay thế |
| --- | --- |
| 🛑 Không giữ giá trị token đã xuất | Neo bằng mã commit. Chép giá trị vào đây là tạo bản sao thứ ba |
| 🛑 Không thay cho `Lịch sử quyết định` | Nhật ký ghi *"đã xuất lúc nào"*; `DESIGN.md` ghi *"vì sao giá trị đổi"*. Hai câu hỏi khác nhau |
| 🛑 Không dùng làm nơi báo lỗi thiết kế | Lỗi thiết kế thuộc báo cáo audit ([`AuditReport.md`](./AuditReport.md)) |
| 🛑 Không coi bản đã xuất là nguồn | Nguồn ở `docs/Design/`, luôn luôn ([`../CLAUDE.md`](../CLAUDE.md) §1). Người sửa trực tiếp trong công cụ ngoài thì thay đổi đó **chưa tồn tại** cho tới khi có người mang nó về spec |

Điều thứ tư là điều dễ vi phạm nhất, vì công cụ thiết kế là nơi tự nhiên để thử một ý tưởng. Thử thì được; nhưng thứ thử ra chỉ trở thành quyết định khi nó được viết vào spec, và đó là lúc chiều cập nhật ở [`../CLAUDE.md`](../CLAUDE.md) §5 áp dụng.

---

## 6. Bắt buộc và tuỳ chọn

| Phần | Bắt buộc? | Ghi chú |
| --- | --- | --- |
| Ba khoá frontmatter, `kind: tham-chieu` | ✅ **Bắt buộc** | |
| Câu khẳng định nhật ký là append-only | ✅ **Bắt buộc** | |
| Bảng đích đến | ✅ **Bắt buộc** | Kèm câu "không cái nào là nguồn" |
| Bảng nhật ký bảy cột | ✅ **Bắt buộc** | |
| Cột neo nguồn tra ngược được | ✅ **Bắt buộc** | Mã commit, không phải mô tả bằng lời |
| Cột kết quả cổng tài liệu | ✅ **Bắt buộc** | |
| `## Quy trình xuất` | ⬜ Tuỳ chọn | Cần khi quy trình có nhiều bước tay |
| Cột người làm | ⬜ Tuỳ chọn | Bỏ được ở đội nhỏ |
| Giá trị token đã xuất | 🛑 **Cấm** | |
| Sửa hoặc xoá dòng cũ | 🛑 **Cấm** | |
| Tài khoản, khoá truy cập | 🛑 **Cấm** | |

---

## 7. Danh sách kiểm

- [ ] `kind: tham-chieu`, không phải `kind: luat`.
- [ ] Dòng mới **thêm vào cuối**, không dòng cũ nào bị đụng.
- [ ] Cột neo nguồn là mã commit, tra ngược được.
- [ ] Cột cổng tài liệu ghi kết quả thật tại lúc xuất.
- [ ] Không giá trị token nào bị chép vào đây.
- [ ] Không thông tin đăng nhập nào.
- [ ] `bash .claude/check-docs.sh` xanh.
