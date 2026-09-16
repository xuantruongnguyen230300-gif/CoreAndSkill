---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Khuôn — spec một màn hình

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Khuôn này chưa được dùng lần nào; repo chưa có màn hình để mô tả.

> **Đây là khuôn, không phải spec.** Chép nội dung mục §2 xuống thành `Screens/NN-<ten-luong>.md` của một dự án, rồi điền.
>
> 🛑 **Ba khoá frontmatter ĐỔI khi khuôn được đem đi dùng.** Khuôn là công cụ của Core nên mang `scope: core`. File sinh ra mô tả **một dự án** nên mang **`scope: du-an`** và **`verified: chua-doi-chieu`** cho tới khi có người mở source ra đối chiếu toàn bộ. `kind: luat` giữ nguyên. Chép nguyên khối frontmatter của khuôn là gán cho file mới một `scope` sai.

---

## 1. Khi nào dùng khuôn này

Một file `Screens/*.md` mô tả **một luồng**, và trong luồng đó có một hoặc nhiều màn. Chia file theo **luồng người dùng**, không theo tuyến kỹ thuật: "đăng nhập → đổi mật khẩu lần đầu → về trang chủ" là một file, kể cả khi nó gồm ba tuyến.

Đây là chỗ **ngoại lệ Core↔nghiệp vụ** sống — xem [`../CLAUDE.md`](../CLAUDE.md) §1. Một screen spec được phép mô tả màn nghiệp vụ.

---

## 2. Dàn bài — chép từ đây xuống

```text
---
kind: luat
scope: du-an
verified: chua-doi-chieu
---
```

### `# <Tên luồng> — màn hình`

Nhãn fidelity ngay dưới tiêu đề, **bắt buộc**, một trong ba nhãn ở [`../CLAUDE.md`](../CLAUDE.md) §2.

Một tới ba câu: luồng này làm gì, ai vào được, các màn nối nhau ra sao.

Rồi hai dòng khai bối cảnh:

> **Khung:** khung ứng dụng (`Sidebar` + `Topbar`) — hoặc khung xác thực (`AuthCard`, không sidebar không topbar)
> **Quyền:** permission cần có để vào được luồng này

### `## <Tên màn> (đường dẫn tuyến)`

Lặp cả khối này một lần cho mỗi màn. **Bảy mục H3 dưới đây là bắt buộc, đúng thứ tự này.**

#### `### Sơ đồ bố cục`

Cây vùng, từ ngoài vào trong. **Chỉ được ghép component có tên trong** [`../COMPONENTS.md`](../COMPONENTS.md) §3 — đó chính là lý do file mục lục kia là một cái cổng.

Kèm số đo cấu trúc lấy từ token: bề rộng, chiều cao, đệm.

#### `### Câu chữ`

Bảng mọi câu người dùng đọc được. **Mỗi câu phải có khoá i18n** — template không được chứa chữ tiếng Việt, cổng [`../../RULES.md`](../../RULES.md) §7 F8 bắt.

Câu mà nguồn chỉ cho ý, không cho câu: soạn bản đề xuất theo khuôn nhãn *Chờ duyệt:* ở [`../CLAUDE.md`](../CLAUDE.md) §8.

| Phần tử | Câu hiển thị | Khoá i18n |
| --- | --- | --- |
| Tiêu đề màn | Danh sách người dùng | `nguoiDung.tieuDe` |
| Nút chính | Thêm người dùng | `nguoiDung.hanhDong.them` |
| Thông báo lỗi | Không tải được danh sách | `nguoiDung.loi.taiThatBai` |

Cột **Nguồn** (tuỳ chọn, thêm khi câu lấy từ nơi khác): link = câu lấy thẳng từ nguồn đó; *duyệt* = người dùng đã duyệt, link kèm sau (nếu có) chỉ cho ý. Câu không ghi nguồn là câu người dùng đã duyệt — spec ghi **ngày duyệt** một lần ngay dưới bảng, không lặp quy ước này.

#### `### Trạng thái`

Màn hình vẽ thế nào ở từng trạng thái. **Năm trạng thái bắt buộc**, cái nào không áp dụng thì ghi lý do:

- **mặc định** — có dữ liệu, không lỗi
- **đang tải** — lần đầu vào màn. `SkeletonLoader` hay spinner, ở đâu
- **rỗng** — chưa có bản ghi nào **so với** không có kết quả sau khi lọc. **Hai ca này khác nhau** và phải có hai câu chữ khác nhau: một cái mời tạo mới, một cái mời gỡ bộ lọc. Gộp làm một là lỗi hay gặp nhất ở màn danh sách
- **lỗi** — API hỏng, mất mạng, hết phiên
- **kiểm tra dữ liệu** — lỗi từng trường hiện ở đâu, tóm tắt lỗi hiện ở đâu

#### `### Responsive`

Từng ngưỡng ở [`../DESIGN.md`](../DESIGN.md) §6.3 thì cái gì xếp lại, cái gì thu, cái gì ẩn.

🛑 **"Ẩn" phải nói rõ ẩn đi đâu.** Một cột bảng ẩn ở màn nhỏ thì thông tin đó vào đâu — vào màn chi tiết, hay biến mất hẳn? Ẩn mà không nói đi đâu là mất dữ liệu.

#### `### Icon`

Bảng hành động → icon → chỗ đặt. Màn chỉ dùng icon đã có trong [`../Icons.md`](../Icons.md) §5 thì viết một dòng trỏ tới đó, không chép bảng lại.

#### `### Ảnh màn hình`

🛑 **Giai đoạn 1 viết đúng một dòng:** *chưa có — repo chưa có `src/`, không có gì để chụp.*

Cấm viết trước tên file ảnh. Cấm tạo trước thư mục ảnh. Lý do ở [`../CLAUDE.md`](../CLAUDE.md) §3.

Khi đã có app: một ảnh desktop cho mỗi màn là **mặc định**. Ảnh cho từng trạng thái và từng khổ màn chỉ chụp khi có người thật sự cần ca đó.

#### `### Cần chốt`

Chỗ cố ý để ngỏ, kèm câu hỏi cụ thể và ai trả lời được. Không còn gì thì ghi `Không còn`.

🛑 **Không dùng mục `Normalize on redesign` ở giai đoạn 1.** Mục đó là danh sách "cái đang chạy xấu ở chỗ nào"; chưa có cái đang chạy thì nó chỉ có thể là bịa.

---

## 3. Ví dụ điền — một mục `Sơ đồ bố cục`

```text
- Khung ứng dụng
  - Sidebar (rộng --layout-sidebar-w, thu còn --layout-sidebar-w-collapsed dưới $bp-lg)
  - Topbar (cao --layout-topbar-h, dính đỉnh)
  - main (rộng tối đa --layout-container-max, đệm --sp-8)
    - PageHeader — tiêu đề + nút "Thêm người dùng" (Button primary)
    - Card (đệm --sp-6, khe --sp-6)
      - Toolbar — ô tìm + nút lọc có badge đếm + nhóm hành động
      - DataTable — cột: Tên, Email, Vai trò (Badge định danh), Trạng thái (Badge trạng thái), Hành động (IconButton sửa/xoá)
      - Pagination
  - Footer
```

🛑 **Mọi số đo trong sơ đồ là TÊN TOKEN, không phải con số.** Giá trị của `--layout-*` đọc ở [`../DESIGN.md`](../DESIGN.md) §6.1 — chép con số vào spec màn là tạo bản sao thứ hai của một giá trị đã có chủ, và bản sao đó sẽ không được sửa cùng lúc ([`../../../.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §5).

Một mục `Trạng thái` điền đúng:

```text
- mặc định: DataTable có dữ liệu, Pagination hiện "1–20 trên 137"
- đang tải: SkeletonLoader dạng hàng bảng, đúng số hàng của trang trước; Toolbar vẫn bấm được
- rỗng (chưa có bản ghi): EmptyState trong thân bảng — icon pi-inbox, câu mời tạo mới, nút primary "Thêm người dùng"
- rỗng (lọc không ra kết quả): EmptyState — câu khác hẳn, nút secondary "Xoá bộ lọc". KHÔNG mời tạo mới ở đây
- lỗi: NoticeBanner vai danger phía trên bảng, nút "Thử lại". Bảng giữ dữ liệu cũ nếu có
- kiểm tra dữ liệu: không áp dụng — màn này không có form
```

---

## 4. Bắt buộc và tuỳ chọn

| Phần | Bắt buộc? | Ghi chú |
| --- | --- | --- |
| Ba khoá frontmatter | ✅ **Bắt buộc** | Cổng [`../../RULES.md`](../../RULES.md) §1 D1 chặn nếu thiếu |
| Nhãn fidelity dưới tiêu đề | ✅ **Bắt buộc** | |
| Hai dòng Khung / Quyền | ✅ **Bắt buộc** | Không có chúng thì không dựng được màn |
| Bảy mục H3 mỗi màn | ✅ **Bắt buộc**, đúng thứ tự | Mục không áp dụng vẫn giữ tiêu đề, ghi lý do bên dưới |
| Bảng câu chữ có cột khoá i18n | ✅ **Bắt buộc** | |
| Hai ca `rỗng` tách riêng | ✅ **Bắt buộc** ở màn danh sách | |
| Số đo lấy từ token | ✅ **Bắt buộc** | Số thô là lỗi |
| Bảng *Mã lỗi → chỗ hiện* | ✅ **Bắt buộc** khi màn gọi API | Request nào bảng bảo hiện lỗi ở `NoticeBanner` hay ở ô thì đặt cờ `BO_QUA_TOAST_LOI` ([`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §1): màn tự hiện, interceptor không toast chồng; `CORE.AUTH.FORBIDDEN` đi đường chung ([`../../quy-uoc/fe-routing-guard.md`](../../quy-uoc/fe-routing-guard.md) §8) |
| Sơ đồ luồng giữa các màn | ⬜ Tuỳ chọn | Nên có khi luồng hơn hai màn |
| Bảng phân quyền theo từng nút | ⬜ Tuỳ chọn | Cần khi cùng một màn hiện khác nhau theo quyền |
| Ghi chú hiệu năng | ⬜ Tuỳ chọn | Cần khi màn tải danh sách lớn |
| `Normalize on redesign` | 🛑 **Cấm** ở giai đoạn 1 | Mở khi màn đã dựng xong — [`../CLAUDE.md`](../CLAUDE.md) §2 |

---

## 5. Danh sách kiểm trước khi coi là xong

- [ ] Mọi component nhắc tới đều có dòng trong [`../COMPONENTS.md`](../COMPONENTS.md) §3.
- [ ] Mọi số đo là token, không phải số thô.
- [ ] Mọi câu chữ có khoá i18n.
- [ ] Đủ bảy mục H3 cho mỗi màn.
- [ ] Hai ca `rỗng` được tách (nếu là màn danh sách).
- [ ] Mục ảnh màn hình **không** trỏ tới file không tồn tại.
- [ ] Không có trích dẫn nào trỏ vào `src/`.
- [ ] `bash .claude/check-docs.sh` xanh.
