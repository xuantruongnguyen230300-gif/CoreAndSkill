---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Khuôn — khai bộ icon của một dự án

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Dùng khi một dự án dựng trên Core cần **mở rộng** bảng ánh xạ icon ở [`../Icons.md`](../Icons.md) — thêm hành động nghiệp vụ mà Core không có.
>
> 🛑 **Ba khoá frontmatter ĐỔI khi dùng:** khuôn mang `scope: core`; file icon của một dự án mang **`scope: du-an`**, `verified: chua-doi-chieu`.

---

## 1. Đọc trước: dự án được đổi gì

| Loại | Được? | Vì sao |
| --- | --- | --- |
| **Thêm dòng** ánh xạ cho hành động nghiệp vụ | ✅ Được | Đây là việc file này tồn tại để làm |
| **Đổi icon** của một hành động đã có trong Core | 🛑 Không | Mọi màn Core dùng icon đó. Đổi ở một dự án là tạo ra hai quy ước |
| **Nạp thêm một bộ icon thứ hai** | 🛑 Không | Hai bộ icon = hai độ dày nét, hai bán kính bo, hai cách vẽ mũi tên. Người dùng thấy ngay dù không gọi tên được |
| **Thêm SVG riêng** khi bộ chuẩn thật sự không có | ⚠️ Được, có điều kiện | Xem §4 |

---

## 2. Dàn bài — chép từ đây xuống

### `# Icons — <tên dự án>`

Nhãn fidelity. Rồi một câu: file này mở rộng [`../Icons.md`](../Icons.md), **không thay thế** nó.

### `## Bộ icon`

Một dòng khẳng định bộ chuẩn là bộ nào, và **trỏ về** [`../Icons.md`](../Icons.md) §1 cho phần lý do. Không chép lại lý do.

### `## Ánh xạ hành động → icon (riêng dự án)`

Bảng chỉ chứa **hành động nghiệp vụ**. Hành động đã có trong Core thì không lặp lại — lặp là tạo bản sao sẽ lệch.

### `## SVG riêng`

Chỉ có mục này khi thật sự có SVG riêng. Không có thì ghi `Không có`.

### `## Cần chốt`

---

## 3. Bảng ánh xạ — ba cột

| Hành động | Icon | Dùng ở |
| --- | --- | --- |
| Duyệt hồ sơ | `pi-check-square` | `IconButton` trong hàng bảng hồ sơ chờ duyệt |
| Trả lại hồ sơ | `pi-undo` | Cạnh nút Duyệt |
| Bàn giao | `pi-send` | `Toolbar` màn chi tiết hồ sơ |

Bốn luật, giống hệt Core:

1. **Một icon không mang hai nghĩa.** Trong toàn ứng dụng, không chỉ trong file này.
2. **Một nghĩa không có hai icon.**
3. **Kiểm bảng Core trước.** Rất nhiều hành động nghiệp vụ thật ra là một hành động chung đã có: "Duyệt" thường chỉ là một dạng của "Lưu"; "Trả lại" thường là "Huỷ".
4. **Icon không tự giải thích được thì thêm chữ, đừng đổi icon.** Nhiều hành động nghiệp vụ đơn giản là không có icon nào nói được — đó là lúc dùng nút có chữ.

**Bẫy hay gặp:** đội nghiệp vụ đề nghị một icon rất "đúng nghĩa" với người trong ngành (một cái con dấu cho "phê duyệt") mà người ngoài ngành không đọc được. Người dùng thật của phần mềm quản trị đổi liên tục; icon phải hiểu được ở lần gặp đầu tiên.

---

## 4. SVG riêng — điều kiện

Chỉ khi bộ chuẩn thật sự không có, và phải đủ **năm** điều:

| Điều kiện | Cụ thể |
| --- | --- |
| Đã tìm kỹ bộ chuẩn | Phần lớn "thiếu icon" là chưa tìm kỹ |
| Cùng phong cách | Khung 24×24, nét 1.5px, đầu nét bo — để đứng cạnh bộ chuẩn không lệch |
| Kế thừa màu | `fill="currentColor"` hoặc `stroke="currentColor"`. 🛑 Không hardcode màu — cổng [`../../RULES.md`](../../RULES.md) §7 F6 bắt |
| Đặt trong tài sản dùng chung | Không nội tuyến vào template từng màn. Nội tuyến nghĩa là cùng một icon tồn tại ở nhiều bản sao |
| Có dòng trong bảng §3 | Ghi chú `SVG riêng` ở cột icon |

---

## 5. Accessibility — không được viết lại, phải trỏ về

🛑 **Không chép luật accessibility icon vào file của dự án.** Nó nằm ở [`../Icons.md`](../Icons.md) §7 và chỉ được nằm ở đó.

Nhắc lại đúng một dòng cho người đọc nhanh, rồi trỏ:

> Icon trang trí: `aria-hidden="true"`. Icon mang nghĩa: nhãn đặt trên phần tử tương tác. Phép thử và lý do đầy đủ ở [`../Icons.md`](../Icons.md) §7.

**Vì sao không chép:** hai bản sao của một luật sẽ lệch nhau, và bản trong file dự án là bản không ai sửa. Đây đúng khuôn mà [`../../../.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §5 cấm.

---

## 6. Ví dụ điền

```text
# Icons — Quản lý hồ sơ

📐 ĐÍCH ĐẾN — CHƯA THI CÔNG.

File này mở rộng docs/Design/Icons.md bằng các hành động riêng của nghiệp vụ hồ sơ.
Không thay thế, không chép lại.

## Bộ icon

PrimeIcons, đúng bộ Core khai. Lý do và luật kích thước/màu ở docs/Design/Icons.md §1, §3, §4.

## Ánh xạ hành động → icon (riêng dự án)

| Hành động | Icon | Dùng ở |
| --- | --- | --- |
| Duyệt hồ sơ | pi-check-square | IconButton trong hàng bảng hồ sơ chờ duyệt |
| Trả lại hồ sơ | pi-undo | Cạnh nút Duyệt |
| In phiếu | pi-print | Toolbar màn chi tiết |

"Lưu nháp" KHÔNG có dòng riêng — nó dùng pi-save của bảng Core.

## SVG riêng

Không có.
```

---

## 7. Bắt buộc và tuỳ chọn

| Phần | Bắt buộc? | Ghi chú |
| --- | --- | --- |
| Ba khoá frontmatter | ✅ **Bắt buộc** | |
| Nhãn fidelity | ✅ **Bắt buộc** | |
| Câu khẳng định "mở rộng, không thay thế" | ✅ **Bắt buộc** | |
| Bảng ánh xạ chỉ chứa hành động nghiệp vụ | ✅ **Bắt buộc** | Lặp lại dòng của Core là lỗi |
| Dòng trỏ về luật accessibility của Core | ✅ **Bắt buộc** | |
| Mục SVG riêng | ✅ **Bắt buộc** có tiêu đề | Không có thì ghi `Không có` |
| Bảng icon theo trạng thái nghiệp vụ | ⬜ Tuỳ chọn | Cần khi nghiệp vụ có nhiều trạng thái vòng đời |
| Ghi chú về icon thư viện UI tự chèn lúc chạy | ⬜ Tuỳ chọn | Rất nên có — chúng không xuất hiện trong source nên tìm bằng grep không ra |

---

## 8. Danh sách kiểm

- [ ] Không dòng nào lặp lại bảng Core.
- [ ] Không đổi icon của một hành động Core đã có.
- [ ] Không nạp bộ icon thứ hai.
- [ ] Mọi SVG riêng đủ năm điều kiện ở §4.
- [ ] Không chép luật accessibility, chỉ trỏ về.
- [ ] `bash .claude/check-docs.sh` xanh.
