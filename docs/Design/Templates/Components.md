---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Khuôn — spec một component

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> **Khuôn này có HAI phần.** Phần A thành `COMPONENTS.md` (mục lục thư viện) của một dự án; phần B thành `Components/<Tên>.md` (một file một component). Cắt ở dòng phân cách giữa hai phần.
>
> 🛑 **Ba khoá frontmatter ĐỔI khi khuôn được đem đi dùng:** khuôn mang `scope: core`; file sinh ra cho một dự án mang **`scope: du-an`** và **`verified: chua-doi-chieu`**. `kind: luat` giữ nguyên.
>
> Component **Core** thì spec sống ở `docs/Design/Components/` và giữ `scope: core`. Component **nghiệp vụ** thuộc thư viện riêng của dự án. Phép thử phân biệt ở [`../CLAUDE.md`](../CLAUDE.md) §1.

---

## PHẦN A — khuôn `COMPONENTS.md`

### Dàn bài

1. **Câu mở đầu nói rõ file này là CỔNG** — screen spec chỉ được ghép component có tên trong bảng; thêm file vào `Components/` mà không thêm dòng thì component đó chưa tồn tại.
2. **`## Luật mà mục lục này ép`** — một component một định nghĩa; mở rộng thay vì đẻ mới; chỉ chứa component đúng phạm vi.
3. **`## Nguyên tắc chung`** — trạng thái bắt buộc, kích thước, biến thể, tầng lồng nhau.
4. **`## Mục lục`** — bảng chính.
5. **`## Ranh giới bọc thư viện ↔ tự dựng`** — tiêu chí, không phải danh sách tuỳ hứng.
6. **`## Dumb và smart`**.
7. **`## Quy tắc đặt tên`**.
8. **`## Danh sách kiểm khi thêm component mới`**.

### Bảng mục lục — bốn cột, không hơn

| Component | Là gì | Spec | Nền | Trạng thái |
| --- | --- | --- | --- | --- |
| `Button` | Nút có nhãn chữ, bốn vai, ba cỡ | [Components/Button.md](../Components/Button.md) | tự dựng | 📐 spec xong, chưa dựng |

Cột `Trạng thái` chỉ nhận **bốn** giá trị định nghĩa ở [`../CLAUDE.md`](../CLAUDE.md) §4. Không phát minh giá trị thứ năm.

Cột `Là gì` là **một câu**, và câu đó phải phân biệt được component này với component gần giống nó. "Thông báo" không đủ; "thông báo nằm trong trang, ở lại cho tới khi bối cảnh đổi" thì đủ — vì nó nói luôn cái khác với `Toast`.

🛑 **Không chép số lượng component vào tài liệu.** Đếm bằng lệnh.

---

## PHẦN B — khuôn `Components/<Tên>.md`

### Frontmatter

```text
---
kind: luat
scope: core
verified: chua-doi-chieu
---
```

### Dàn bài bắt buộc — đúng thứ tự này

| # | Mục | Nội dung |
| --- | --- | --- |
| 0 | Tiêu đề + nhãn fidelity + dòng **Nền** | Nhãn theo [`../CLAUDE.md`](../CLAUDE.md) §2. Dòng Nền nói tự dựng hay bọc thư viện, kèm một câu lý do |
| 1 | `## Mục đích` | **Một câu.** Dài hơn một câu là dấu hiệu component đang gánh hai vai |
| 2 | `## Khi nào dùng / khi nào KHÔNG dùng` | Bảng hai cột. Mỗi dòng "không dùng" **phải trỏ sang component thay thế** |
| 3 | `## Biến thể` | Bảng. Mỗi biến thể nói vai và khi nào dùng |
| 4 | `## Kích thước` | Bảng. Dùng đúng ba cỡ `sm`/`md`/`lg` |
| 5 | `## Trạng thái` | Bảng **đủ tám dòng** |
| 6 | `## Token dùng` | Bảng nhóm → token. Chỉ tên token |
| 7 | `## Responsive` | Bảng ngưỡng → hành vi |
| 8 | `## Accessibility` | Bảng khoản → yêu cầu |
| 9 | `## API dự kiến` | Bảng: Tên · Chiều · Kiểu · Mặc định · Ghi chú |
| 10 | `## Do / Don't` | Bullet ✅ và ❌ |
| 11 | `## Cần chốt` | Bảng câu hỏi để ngỏ. Không còn thì ghi `Không còn` |

### Bảng trạng thái — tám dòng, không được bỏ trống

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | | |
| `hover` | Bọc trong `@media (hover: hover)` | |
| `focus-visible` | `outline` + `outline-offset` ≥ 2px | |
| `active` | | |
| `disabled` | Thuộc tính thật, không chỉ đổi màu | |
| `loading` | `aria-busy`, khoá thao tác | |
| `error` | | |
| `empty` | | |

**Không áp dụng thì ghi `Không áp dụng.` kèm lý do** — ví dụ: *"Không áp dụng. Component này không nhận dữ liệu ngoài nên không có trạng thái chờ."*

Bỏ trống một dòng nguy hiểm hơn viết sai nó: dòng sai thì có người cãi, dòng trống thì mỗi người dựng tự bịa một kiểu.

### Bảng API — hai điều bắt buộc

- Cột `Mặc định` **luôn có giá trị**. "Không có mặc định" cũng là một quyết định và phải viết ra.
- Giá trị mặc định phải **sai theo hướng an toàn**. Ví dụ `variant` mặc định là `secondary` chứ không phải `primary`: quên khai thì ra nút phụ, không ra một màn có bốn nút chính.

---

## Ví dụ điền

Mục 2, viết đúng:

| Dùng | Không dùng |
| --- | --- |
| Hành động xảy ra tại chỗ: lưu, huỷ, mở dialog | 🛑 Điều hướng sang tuyến khác → dùng `<a routerLink>`. Giả liên kết bằng nút lấy đi thao tác "mở tab mới" của người dùng |
| Hành động cần nhãn chữ để hiểu | 🛑 Hành động rõ nghĩa bằng icon, lặp mỗi hàng bảng → `IconButton` |

Mục 5, một dòng viết đúng:

| `loading` | Icon quay thay icon dẫn; nhãn **giữ nguyên**; nút bị vô hiệu hoá; `aria-busy="true"`. Bề rộng nút không đổi | Có |

Mục 11, viết đúng:

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Có cần biến thể `link` không? Hôm nay `ghost` gánh vai đó. Rủi ro: hai thứ gần giống nhau mà không ai phân biệt được | Dự án đầu tiên gặp nhu cầu thật |

---

## Bắt buộc và tuỳ chọn

| Phần | Bắt buộc? | Ghi chú |
| --- | --- | --- |
| Ba khoá frontmatter | ✅ **Bắt buộc** | |
| Nhãn fidelity + dòng Nền | ✅ **Bắt buộc** | |
| Mười một mục theo đúng thứ tự | ✅ **Bắt buộc** | |
| Tám dòng trạng thái | ✅ **Bắt buộc** | Không áp dụng thì ghi lý do |
| Chỉ dùng tên token, không giá trị thô | ✅ **Bắt buộc** | Ngoại lệ: nhắc lại giá trị của một token kích thước khi đang giải thích |
| Ít nhất một đoạn giải thích đánh đổi | ✅ **Bắt buộc** | Spec không nói được vì sao thì không ai theo |
| Khối markup mẫu | ⬜ Tuỳ chọn | Chỉ khi cấu trúc DOM khó tả bằng lời. Dùng khối `text`, không dùng khối `html` — nó là minh hoạ hình dạng, không phải code chạy được |
| Sơ đồ giải phẫu | ⬜ Tuỳ chọn | |
| Bảng so sánh với component gần giống | ⬜ Tuỳ chọn | Rất nên có với cặp dễ nhầm (`Toast` ↔ `NoticeBanner`, `Tabs` ↔ `SegmentedControl`) |
| `Normalize on redesign` | 🛑 **Cấm** ở giai đoạn 1 | |

---

## Danh sách kiểm

- [ ] Đã kiểm rằng không component nào đang có mở rộng được để làm việc này.
- [ ] Đúng phạm vi (Core hay nghiệp vụ) theo phép thử ở [`../CLAUDE.md`](../CLAUDE.md) §1.
- [ ] Đủ mười một mục.
- [ ] Đủ tám dòng trạng thái, không dòng nào trống.
- [ ] Chỉ tên token, không giá trị thô.
- [ ] Đã khai tự dựng hay bọc thư viện, kèm lý do theo tiêu chí ở mục lục.
- [ ] Đã khai vai trò ARIA, phím tắt, hành vi focus, vùng bấm.
- [ ] Đã thêm một dòng vào bảng mục lục.
- [ ] Không trích dẫn `src/`, không trích dẫn ảnh không tồn tại.
- [ ] `bash .claude/check-docs.sh` xanh.
