---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Khuôn — `README.md` cho khu Design của một dự án

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> File này là **cửa vào** khu Design của một dự án: người mới mở nó ra là biết đọc tiếp cái gì.
>
> 🛑 **Ba khoá frontmatter ĐỔI khi dùng:** khuôn mang `scope: core`; README của một dự án mang **`scope: du-an`**, `verified: chua-doi-chieu`.

---

## 1. README này KHÔNG chứa gì

Đây là luật khó giữ nhất, vì một README luôn có xu hướng phình ra thành "chỗ để mọi thứ".

| Không chứa | Ở đâu thay thế |
| --- | --- |
| Giá trị token | `DESIGN.md` của dự án, hoặc [`../DESIGN.md`](../DESIGN.md) |
| Mô tả component | `COMPONENTS.md` và `Components/` |
| Mô tả màn hình | `Screens/` |
| Luật khu Design | [`../CLAUDE.md`](../CLAUDE.md) — trỏ, đừng chép |
| Trạng thái từng file | Đầu chính file đó ([`../CLAUDE.md`](../CLAUDE.md) §4) |

**Trạng thái là chỗ sai nhiều nhất.** Ở dự án tiền nhiệm, mục lục từng gắn nhãn cho bảy dòng file; một lần đối chiếu tìm ra **năm trên bảy đã sai**. Trạng thái được chép ra chỗ thứ hai, và chỗ thứ hai không bao giờ được sửa cùng lúc.

README được phép có một bảng trạng thái **cấp khu** ("thư mục `Screens/` đã bắt đầu chưa"), không phải cấp file.

---

## 2. Dàn bài — chép từ đây xuống

### `# Design — <tên dự án>`

Nhãn fidelity. Rồi một câu: khu này mô tả giao diện của sản phẩm nào.

### `## Đọc theo thứ tự này`

Bảng: *"bạn đang muốn làm gì" → "mở file nào"*. Đây là phần có giá trị nhất của cả file.

### `## Quan hệ với Core`

Dự án này lấy gì từ [`../DESIGN.md`](../DESIGN.md), ghi đè gì, thêm gì. **Một đoạn**, chi tiết ở `DESIGN.md` của dự án.

### `## Cấu trúc thư mục`

Cây thư mục kèm một dòng mô tả mỗi mục. Chỉ liệt kê thư mục **đã tồn tại**.

🛑 **Không liệt kê thư mục sẽ tạo sau.** Một cây thư mục mô tả thứ chưa có là loại lỗi mà cổng không bao giờ bắt được — khối không gắn ngôn ngữ không bị luật nào chặn, và người đọc tin nó ([`../../../.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §8).

### `## Trạng thái các khu`

Bảng **cấp thư mục**, không cấp file.

### `## Luật riêng của dự án`

Chỉ luật **chỉ đúng với dự án này**. Không có thì ghi `Không có — theo nguyên luật ở docs/Design/CLAUDE.md.`

### `## Trước khi coi một thay đổi là xong`

Lệnh cổng. Một dòng.

---

## 3. Ví dụ điền — mục quan trọng nhất

```text
## Đọc theo thứ tự này

| Bạn đang muốn | Mở file |
| --- | --- |
| Biết màu/cỡ chữ/khoảng cách của dự án này | DESIGN.md — chỉ ghi chênh lệch so với Core |
| Biết một component trông thế nào | ../../Design/COMPONENTS.md rồi mở spec tương ứng |
| Dựng một màn hình mới | Screens/ — tìm luồng chứa màn đó |
| Biết icon nào cho hành động nào | Icons.md của dự án, rồi ../../Design/Icons.md §5 |
| Biết luật của khu Design | ../../Design/CLAUDE.md |
| Thêm một component mới | ../../Design/COMPONENTS.md §7 danh sách kiểm |

Mở ĐÚNG MỘT file. Đừng đọc cả thư mục.
```

Bảng trạng thái cấp khu, viết đúng:

```text
| Khu | Trạng thái |
| --- | --- |
| DESIGN.md | 📐 đích đến — chưa có src/ để đối chiếu |
| Screens/ | 🚧 mới có luồng xác thực |
| Icons.md | 📐 đích đến |
| Assets/ | ⬜ chưa tạo — chưa có app để chụp màn hình |
```

Viết **sai** — trạng thái cấp file, sẽ lệch:

```text
| File | Trạng thái |
| --- | --- |
| Screens/01-dang-nhap.md | ✅ xong |
| Screens/02-nguoi-dung.md | 🚧 đang viết |
```

---

## 4. Bắt buộc và tuỳ chọn

| Phần | Bắt buộc? | Ghi chú |
| --- | --- | --- |
| Ba khoá frontmatter | ✅ **Bắt buộc** | |
| Nhãn fidelity | ✅ **Bắt buộc** | |
| `## Đọc theo thứ tự này` | ✅ **Bắt buộc** | Đây là lý do file tồn tại |
| `## Quan hệ với Core` | ✅ **Bắt buộc** | Một đoạn, không hơn |
| `## Cấu trúc thư mục` — chỉ thư mục đã có | ✅ **Bắt buộc** | |
| Bảng trạng thái **cấp khu** | ✅ **Bắt buộc** | |
| Lệnh cổng | ✅ **Bắt buộc** | |
| `## Luật riêng của dự án` | ⬜ Tuỳ chọn | Không có thì ghi rõ là không có |
| Ghi chú về công cụ thiết kế đang dùng | ⬜ Tuỳ chọn | Chỉ khi đội có dùng thật |
| Bảng trạng thái **cấp file** | 🛑 **Cấm** | Xem §1 |
| Chép luật từ `docs/Design/CLAUDE.md` | 🛑 **Cấm** | Trỏ đường |
| Liệt kê thư mục chưa tạo | 🛑 **Cấm** | Xem §2 |
| Chép số lượng file/màn/component | 🛑 **Cấm** | Đếm bằng lệnh ([`../../../.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §6) |

---

## 5. Danh sách kiểm

- [ ] Không có bảng trạng thái cấp file.
- [ ] Cây thư mục chỉ liệt kê thứ đã tồn tại trên đĩa.
- [ ] Không chép luật, chỉ trỏ đường.
- [ ] Không chép số lượng.
- [ ] Bảng "Đọc theo thứ tự này" trỏ tới file có thật.
- [ ] `bash .claude/check-docs.sh` xanh.
