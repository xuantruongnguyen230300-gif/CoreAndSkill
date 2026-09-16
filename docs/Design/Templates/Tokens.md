---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Khuôn — khai một nhóm token

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Dùng khi một dự án dựng trên Core cần **mở rộng** hoặc **ghi đè** bảng token nền ở [`../DESIGN.md`](../DESIGN.md).
>
> 🛑 **Ba khoá frontmatter ĐỔI khi dùng:** khuôn mang `scope: core`; file token của một dự án mang **`scope: du-an`**, `verified: chua-doi-chieu`.

---

## 1. Đọc trước: dự án được đổi gì

| Loại | Được? | Vì sao |
| --- | --- | --- |
| **Ghi đè giá trị** của một token đã có (đổi `--color-brand` sang màu thương hiệu của dự án) | ✅ Được | Đây chính là việc token tồn tại để làm |
| **Thêm token mới** cho một component riêng của dự án | ✅ Được | Đặt tên theo khuôn `--<component>-<thuộc-tính>` |
| **Đổi tên** một token của Core | 🛑 Không | Mọi component Core đọc tên đó. Đổi tên là hỏng cả thư viện |
| **Xoá** một token của Core | 🛑 Không | Cùng lý do |
| **Đổi ý nghĩa** một token (dùng `--color-danger` cho một vai khác) | 🛑 Không | Tên nói vai; đổi vai mà giữ tên là gài bẫy cho người sau |
| **Thêm cỡ chữ / bậc khoảng cách / bo góc ngoài thang** | ⚠️ Cân nhắc rất kỹ | Gần như luôn là dấu hiệu bố cục sai. Muốn thêm thì phải nêu vì sao thang hiện tại không đủ |

**Ghi đè `--color-brand` là ca thường gặp nhất và cũng là ca nguy hiểm nhất.** Đổi màu thương hiệu nghĩa là **phải tính lại tương phản** của tất cả các cặp liên quan: `--color-text-on-brand` trên nền mới, `--color-brand` trên `--color-surface`, vòng focus trên cả hai theme. Đổi màu mà không tính lại là cách chắc chắn nhất để một sản phẩm tụt xuống dưới chuẩn accessibility mà không ai biết.

---

## 2. Dàn bài — chép từ đây xuống

### `# <Nhóm token> — <tên dự án>`

Nhãn fidelity, rồi một câu: file này ghi đè hay mở rộng cái gì của Core.

### `## Ghi đè`

Bảng: token nào của Core bị đổi giá trị, giá trị mới ở cả hai theme, và **tỉ lệ tương phản đã tính lại**.

### `## Thêm mới`

Bảng: token riêng của dự án. Mỗi dòng phải nói **vì sao token nền không đủ**.

### `## Kiểm tương phản`

Bảng mọi cặp bị ảnh hưởng bởi phần Ghi đè, kèm số đo và ngưỡng.

### `## Cần chốt`

---

## 3. Bảng token — bốn cột bắt buộc

| Token | Giá trị sáng | Giá trị tối | Dùng ở đâu |
| --- | --- | --- | --- |
| `--color-brand` | `#RRGGBB` | `#RRGGBB` | Nền nút primary, link, mục nav đang chọn, vòng focus |

Bốn luật:

1. **Luôn khai cả hai theme.** Một token chỉ có giá trị sáng sẽ trống ở theme tối — lỗi âm thầm, không cảnh báo nào.
2. **Cột "Dùng ở đâu" phải liệt kê thật**, không viết "nhiều chỗ". Đây là danh sách những chỗ sẽ đổi khi giá trị đổi.
3. **Đặt tên theo vai, không theo màu.** `--color-danger`, không phải `--color-red`.
4. **Không tự sinh biến thể** (`--color-brand-light-2`). Cần một bậc mới thì khai thẳng nó với vai rõ ràng.

---

## 4. Bảng kiểm tương phản — bắt buộc khi đổi màu

Tính bằng công thức WCAG 2.2 (relative luminance). **Tính, đừng đoán** — mắt người rất tệ ở việc ước lượng tỉ lệ tương phản, đặc biệt với hai màu cùng tông.

| Cặp | Sáng | Tối | Ngưỡng | Đạt? |
| --- | --- | --- | --- | --- |
| `--color-text-on-brand` trên `--color-brand` | 6.85:1 | 7.19:1 | ≥ 4.5:1 | ✅ |
| `--color-brand` trên `--color-surface` | 6.85:1 | 6.57:1 | ≥ 4.5:1 (là chữ link) | ✅ |
| `--color-focus` trên `--color-surface` | 6.85:1 | 8.59:1 | ≥ 3:1 | ✅ |

Ngưỡng lấy từ [`../DESIGN.md`](../DESIGN.md) §2. Một dòng không đạt là một dòng **chặn**, không phải một dòng ghi chú.

**Bẫy hay gặp:** đổi `--color-brand` sang một màu sáng hơn thì chữ trắng trên nó tụt dưới 4.5:1. Cách sửa **không phải** làm chữ đậm hơn — đó là làm cho vấn đề khó thấy chứ không giải quyết nó. Cách sửa là làm màu nền tối đi, hoặc lật `--color-text-on-brand` sang màu tối.

---

## 5. Ví dụ điền

```text
## Ghi đè

| Token | Giá trị sáng | Giá trị tối | Dùng ở đâu |
| --- | --- | --- | --- |
| --color-brand | #RRGGBB | #RRGGBB | Nền nút primary, link, mục nav đang chọn, vòng focus |
| --color-brand-hover | #RRGGBB | #RRGGBB | Nền nút primary khi hover |
| --color-brand-subtle | #RRGGBB | #RRGGBB | Nền mục nav đang chọn, chip đang chọn |
| --color-brand-on-subtle | #RRGGBB | #RRGGBB | Chữ trên --color-brand-subtle |

Bốn token còn lại của họ brand giữ nguyên giá trị Core.
```

---

## 6. Bắt buộc và tuỳ chọn

| Phần | Bắt buộc? | Ghi chú |
| --- | --- | --- |
| Ba khoá frontmatter | ✅ **Bắt buộc** | |
| Nhãn fidelity | ✅ **Bắt buộc** | |
| Cả hai cột theme cho mọi token | ✅ **Bắt buộc** | |
| Cột "Dùng ở đâu" liệt kê thật | ✅ **Bắt buộc** | |
| Bảng kiểm tương phản khi có đổi màu | ✅ **Bắt buộc** | Không có bảng này thì phần Ghi đè chưa xong |
| Lý do cho mỗi token thêm mới | ✅ **Bắt buộc** | |
| Xuất ra định dạng công cụ thiết kế | ⬜ Tuỳ chọn | Chỉ khi đội thiết kế dùng công cụ đó thật. Một file xuất không ai nhập vào đâu là một nguồn thứ hai sẽ lệch |
| Bảng ánh xạ token → theme của thư viện UI | ⬜ Tuỳ chọn | Cần khi dự án đè thêm component token của thư viện; mỗi giá trị trỏ một `var(--color-*)`, không mang mã màu |

🛑 **Preset của thư viện UI không giữ bảng màu.** Nó chỉ trỏ `var(--color-*)` — cơ chế ở [`../../wiki-core/fe/04-design-token-system.md`](../../wiki-core/fe/04-design-token-system.md) §7 — nên đổi token ở file này là đổi luôn màu của component thư viện. Một mã màu nằm trong preset hay trong hằng số TypeScript là **lỗi phải gỡ ở code**, không phải một chỗ thứ hai để ghi nhận: đổi một chỗ mà quên chỗ kia thì CSS và thư viện component vẽ **hai màu khác nhau**, và **không có gì báo lỗi**. Đây là bẫy đã xảy ra thật ở dự án tiền nhiệm.

---

## 7. Danh sách kiểm

- [ ] Mọi token khai đủ hai theme.
- [ ] Mọi cặp bị ảnh hưởng đã tính lại tương phản, không dòng nào dưới ngưỡng.
- [ ] Không đổi tên, không xoá, không đổi ý nghĩa token của Core.
- [ ] Mỗi token thêm mới có lý do.
- [ ] Đã ghi mọi chỗ khác trong code khai lại bảng màu.
- [ ] `bash .claude/check-docs.sh` xanh.
