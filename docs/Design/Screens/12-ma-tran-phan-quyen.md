---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Ma trận phân quyền — màn hình

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Repo chưa có `src/`; màn thuộc pha F3 ([04-f3-man-quan-tri.md](../../wiki-core/fe/trien-khai/04-f3-man-quan-tri.md) §1, §3.5).

Quản trị đơn vị xem, sửa ma trận vai trò × quyền của đơn vị hiện hành: tick ô rồi lưu **toàn bộ** ma trận một lần — bước 3–5 của [P1](../../luong/P1-tao-vai-tro-va-gan-quyen.md). Một màn, một hộp xác nhận. Nghiệp vụ (deny-by-default, `version`, luật phủ đủ danh mục, bất biến vai trò hệ thống): [permissions.md](../../contracts/permissions.md) §2–§6 — không chép lại.

> **Khung:** khung ứng dụng ([00-khung-ung-dung.md](./00-khung-ung-dung.md))
> **Quyền:** `core.permission.read` để vào và đọc; `core.permission.write` để sửa và lưu ([permissions.md](../../contracts/permissions.md) §1). Guard theo [fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §1 — thiếu quyền đọc thì bị chặn, không thấy nội dung.

**Không phải màn danh sách** — ngoại lệ có tên ở [ListScreen.md](../Templates/ListScreen.md): không theo sáu vùng, không `Toolbar`, không `Pagination` (card cấm phân trang danh mục quyền, §4 Ghi chú); [DataTable.md](../Components/DataTable.md) §Khi nào dùng chỉ đích danh ma trận tick quyền là ca dùng `Table` + `Check`.

---

## Ma trận phân quyền (`/quan-tri/phan-quyen`)

### Sơ đồ bố cục

```text
- Khung ứng dụng — 00-khung-ung-dung.md
  - main — đệm --layout-page-pad (dưới $bp-md: --layout-page-pad-sm); không cuộn
    - PageHeader biến thể default
      - tiêu đề + mô tả
      - nhóm hành động (chỉ khi có core.permission.write):
          Button secondary "Huỷ thay đổi" — disabled khi chưa có gì đổi
          Button primary "Lưu ma trận" — disabled khi chưa có gì đổi; loading khi đang gửi
    - NoticeBanner md vai info — chỉ khi THIẾU core.permission.write: màn ở chế độ chỉ xem
    - NoticeBanner md vai danger — chỉ khi lưu hỏng (bảng mã lỗi); ca 409 mang nút "Tải lại ma trận"
    - khung Table biến thể bordered, cỡ sm, stickyHeader, cardLayout = false — vùng cuộn DUY NHẤT của màn, cuộn cả dọc lẫn ngang
      - th dính đỉnh khi cuộn dọc (stickyHeader, Table.md)
      - cardLayout = false: dưới $bp-xs vẫn là lưới cuộn ngang, không chuyển dạng thẻ (mục Responsive)
      - KHÔNG có cột ghim: Table.md §Khi nào dùng đẩy cột ghim sang DataTable, và màn này
        dùng Table (Table + Check là ca được gọi đích danh). Cột "Quyền" cuộn đi cùng thân
      - thân chia nhóm qua input groupBy của Table, đặt vào field resourceName mà FE dẫn xuất cho từng hàng (bản dịch của resourceNameKey; thiếu bản dịch → resourceKey — card §5)
          nhóm theo thứ tự xuất hiện đầu tiên trong rows; hàng trong nhóm giữ thứ tự của response
          Table KHÔNG sắp xếp lại rows, nên màn truyền rows đã xếp để hàng cùng resourceKey nằm liền nhau
        - hàng tiêu đề nhóm — do Table vẽ theo groupBy; không có ô tick
            chữ = giá trị field resourceName (Table vẽ đúng giá trị field groupBy)
          (hình thức, thẻ và ngữ nghĩa của hàng đó: Table.md §Gom dòng theo nhóm)
      - cột 1 — "Quyền": th scope="row" mỗi hàng
          nhãn = bản dịch của nameKey; dòng phụ = code, chữ --color-text-muted
      - cột 2..n — một cột mỗi phần tử roles[] (thứ tự theo response)
          th = role.name; isSystem → Badge neutral cỡ sm "Vai trò hệ thống", onSurface2 (th nền --color-surface-2)
          ô = Check checkbox cỡ sm, ariaLabel nêu cả quyền lẫn vai trò
            checked  = roleId ∈ grantedRoleIds của hàng (hoặc giá trị đã sửa, chưa lưu)
            disabled = thiếu core.permission.write
            disabled = hàng core.permission.write × cột isSystem, kèm Tooltip lý do — xem ghi chú dưới
          ô đã đổi so với lần tải gần nhất → dải --border-w-accent --color-brand ở mép trái ô (không chỉ màu: kèm chữ chỉ-đọc-lên "đã đổi")
  - Footer
```

| Quyết định | Căn cứ |
| --- | --- |
| Dữ liệu từ **một** lời gọi `GET /api/v1/core/permissions/matrix`: hàng = mọi quyền của danh mục, cột = mọi vai trò; hàng **không bao giờ** dựng từ bảng cấp quyền; màn không gọi `.../by-resource` | card §5, §2 điểm 2, §7 |
| Gom nhóm theo tài nguyên **ở FE** bằng `groupBy` của [Table.md](../Components/Table.md) đặt vào field `resourceName` — FE dẫn xuất từ bản dịch `resourceNameKey`, thiếu bản dịch thì dùng `resourceKey` (dự phòng hiển thị; `resourceKey` là khoá gom nhóm) | card §5 |
| Ô hàng `core.permission.write` × cột `is_system` hiện `disabled` + [Tooltip](../Components/Tooltip.md) lý do, thay vì cho tick rồi nhận 422 `CORE.PERMISSION.SYSTEM_ROLE_CANNOT_LOSE_WRITE`; mã vẫn có chỗ hiện ở bảng lỗi phòng dữ liệu lệch | card §6 |
| Luồng lưu: xác nhận nếu có gỡ quyền, tải lại từ server sau khi lưu; `version` cho lần lưu kế lấy từ lần `GET` đó, không dùng `version` trong response 200 của `PUT` | [04-f3-man-quan-tri.md](../../wiki-core/fe/trien-khai/04-f3-man-quan-tri.md) §3.5 |

```text
bấm "Lưu ma trận"
   ├─ không ô nào chuyển từ có → không  ─────────────▶ gửi PUT
   └─ có ít nhất một ô gỡ quyền ─▶ ConfirmDialog warning, câu mang SỐ ô bị gỡ
                                      ├─ huỷ  → giữ nguyên thay đổi trên màn
                                      └─ xác nhận → gửi PUT
PUT gửi version của lần GET gần nhất + entries phủ đủ MỌI hàng đã tải (card §6)
   ├─ 200 → Toast success → GET lại ma trận → bỏ mọi đánh dấu "đã đổi"
   ├─ 409 VERSION_MISMATCH → GIỮ thay đổi trên màn, NoticeBanner danger "dữ liệu đã thay đổi" + nút "Tải lại ma trận"
   │      └─ bấm → GET ma trận (version mới) → áp lại các ô đã đổi lên dữ liệu mới
   │                 → người dùng xem, rồi bấm "Lưu ma trận" lần nữa (lại qua bước xác nhận nếu có gỡ quyền)
   └─ lỗi khác → giữ nguyên thay đổi trên màn, NoticeBanner danger (bảng mã lỗi)
```

**Áp lại sau 409.** Mỗi ô đã đổi là một *giá trị đích*; tải lại **không** bỏ giá trị đích, **không** tự lưu (card §3.1):

| Ca trên dữ liệu mới | Màn làm gì |
| --- | --- |
| Ô còn, giá trị mới khác giá trị đích | Đặt ô về giá trị đích, giữ dấu "đã đổi" |
| Ô còn, giá trị mới đã bằng giá trị đích | Bỏ dấu "đã đổi" |
| Hàng quyền hoặc cột vai trò không còn | Thay đổi đó rơi |
| Ô bị khoá theo bất biến vai trò hệ thống | Không áp — giữ giá trị máy chủ |

"Huỷ thay đổi" đưa mọi ô về giá trị lần tải gần nhất, không gọi server, không hỏi xác nhận. Rời màn khi còn thay đổi chưa lưu → `unsavedChangesGuard` ([fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §4.1).

### Câu chữ

Câu không ghi nguồn: người dùng duyệt 2026-09-15 ([Screen.md](../Templates/Screen.md) §2).

| Phần tử | Câu hiển thị | Khoá i18n |
| --- | --- | --- |
| Tiêu đề màn, tiêu đề tab | Phân quyền | `phanQuyen.tieuDe` |
| Mô tả dưới tiêu đề | Tick để cấp quyền cho vai trò. Thay đổi chỉ có hiệu lực sau khi lưu. | `phanQuyen.moTa` |
| Nút | Huỷ thay đổi · Lưu ma trận | `phanQuyen.hanhDong.huyThayDoi` · `phanQuyen.hanhDong.luu` |
| Banner chỉ xem | Bạn đang xem ma trận ở chế độ chỉ đọc. Cần quyền sửa phân quyền để thay đổi. | `phanQuyen.chiXem` |
| `<caption>` bảng (ẩn) · tiêu đề cột đầu | Ma trận vai trò và quyền · Quyền | `phanQuyen.bang.caption` · `phanQuyen.cot.quyen` |
| Tiêu đề nhóm | Bản dịch của `resourceNameKey` — khoá **đến từ dữ liệu** (card §5); thiếu bản dịch thì hiện `resourceKey` | `resource.*` |
| Nhãn nhóm rỗng (`emptyGroupLabel` của `Table`; card §5 không cho `resourceKey` rỗng, truyền để đủ hợp đồng [Table.md](../Components/Table.md) §API) | (Trống) | `chung.nhom.trong` |
| Nhãn hàng | Bản dịch của `nameKey` do BE trả (vd. `permission.core.user.read`) — khoá **đến từ dữ liệu**; thiếu bản dịch thì hiện chính khoá (card §4, Ghi chú) | `permission.*` |
| Badge cột vai trò hệ thống (dùng chung với [11-vai-tro.md](./11-vai-tro.md)) | Vai trò hệ thống | `vaiTro.loai.heThong` |
| `aria-label` một ô | Cấp quyền {{quyen}} cho vai trò {{vaiTro}} | `phanQuyen.o.nhan` |
| Chữ chỉ-đọc-lên ô đã đổi | đã đổi, chưa lưu | `phanQuyen.o.daDoi` |
| Tooltip ô khoá bất biến | Vai trò hệ thống luôn giữ quyền sửa phân quyền. | `phanQuyen.o.lyDoKhoaHeThong` |
| Tiêu đề xác nhận gỡ | Gỡ {{soLuong}} quyền đã cấp? | `phanQuyen.xacNhanGo.tieuDe` |
| Mô tả xác nhận gỡ (hiệu lực từ request kế tiếp — [users.md](../../contracts/users.md) §7) · nút | Người dùng mang các vai trò này mất quyền tương ứng từ thao tác kế tiếp của họ. · Lưu và gỡ quyền | `phanQuyen.xacNhanGo.moTa` · `phanQuyen.xacNhanGo.xacNhan` |
| Toast lưu thành công | Đã lưu ma trận phân quyền. | `phanQuyen.thongBao.luuThanhCong` |
| Banner 409 — tiêu đề | Dữ liệu đã thay đổi | `phanQuyen.xungDot.tieuDe` |
| Banner 409 — thân | Người khác vừa lưu ma trận. Tải lại để xem dữ liệu mới; các ô bạn đã đổi được áp lại để bạn xem trước khi lưu lần nữa. | `phanQuyen.xungDot.noiDung` |
| Nút tải lại ở banner 409 | Tải lại ma trận | `phanQuyen.hanhDong.taiLai` |
| Câu hỏi khi rời màn | Rời trang? Thay đổi chưa lưu sẽ mất | khoá chung của `unsavedChangesGuard` — không đặt ở màn này |
| Rỗng — danh mục không có quyền nào | Chưa có quyền nào trong danh mục | `phanQuyen.trong.tieuDe` |
| Rỗng — đơn vị chưa có vai trò nào | Đơn vị chưa có vai trò nào · nút "Tạo vai trò" | `phanQuyen.trongVaiTro.tieuDe` · `phanQuyen.trongVaiTro.hanhDong` |

Mã lỗi → chỗ hiện (câu sống ở `loi.<mã>`):

| Mã | Endpoint | Hiện ở |
| --- | --- | --- |
| `CORE.PERMISSION.VERSION_MISMATCH` | lưu | `NoticeBanner` `danger` dưới `PageHeader`, câu "dữ liệu đã thay đổi", nút "Tải lại ma trận"; thay đổi trên màn **giữ nguyên** và áp lại sau khi tải |
| `CORE.PERMISSION.SYSTEM_ROLE_CANNOT_LOSE_WRITE` | lưu | `NoticeBanner` `danger` |
| `CORE.VALIDATION.FAILED` · `CORE.PERMISSION.ENTRIES_INCOMPLETE` · `CORE.PERMISSION.DUPLICATE_ENTRY` (khoá `Entries`) | lưu | `NoticeBanner` `danger` — lỗi dựng payload của FE, không phải lỗi người dùng |
| `CORE.PERMISSION.NOT_FOUND` (`Entries[i].PermissionId`) · `CORE.PERMISSION.ROLE_NOT_FOUND` (`Entries[i].RoleIds[j]`) | lưu | `NoticeBanner` `danger`, **và** tô viền lỗi ([Check.md](../Components/Check.md) trạng thái `error`) cho đúng hàng / ô mà chỉ số trỏ tới |
| `CORE.AUTH.FORBIDDEN` | đọc, lưu | Đường chung ở [fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §8 |

### Trạng thái

- **mặc định:** bảng đủ hàng, đủ cột; nút lưu và huỷ `disabled` cho tới khi có ô đổi.
- **đang tải:** lần đầu — `Table` `loading`: giữ `<thead>` nếu đã có vai trò, thân là `SkeletonLoader` dạng hàng; nút ở `PageHeader` `disabled`. 🛑 **Không cho tick khi còn đang tải** (sự cố "tải thiếu rồi lưu", card §3.1). Tải lại sau khi lưu hoặc sau "Tải lại ma trận": phủ `--color-scrim` lên thân, giữ chữ cũ đọc được, khoá mọi ô.
- **rỗng (chưa có bản ghi):** hai ca, hai câu. *Danh mục không có quyền nào* — không kỳ vọng xảy ra (danh mục vào bằng migration, card §2): hàng `colspan` chứa `EmptyState` `not-configured`, không có nút. *Đơn vị chưa có vai trò nào* — có hàng mà không có cột: `EmptyState` `first-use` mời sang [11-vai-tro.md](./11-vai-tro.md), nút chỉ hiện khi có `core.role.write`.
- **rỗng (lọc không ra kết quả):** không áp dụng — không có tìm, không có lọc.
- **lỗi:** tải hỏng → `Table` `error` (hàng `colspan` chứa `NoticeBanner` `danger` + thử lại); nút lưu `disabled`. Lưu hỏng → bảng mã lỗi; thay đổi trên màn giữ nguyên.
- **kiểm tra dữ liệu:** không có ô nhập chữ. Lỗi theo chỉ số từ BE tô đúng ô theo bảng mã lỗi; không có lỗi phía client.

### Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-lg` | Bảng chiếm hết bề rộng `main`; cuộn ngang khi số vai trò vượt bề rộng |
| `$bp-md` … `$bp-lg` | Dòng phụ `code` dưới nhãn quyền ẩn; `code` vẫn trong `aria-label` của ô và Tooltip của nhãn hàng — không mất |
| < `$bp-md` | Giữ dạng lưới, cuộn hai chiều, `th` dính đỉnh. Nút ở `PageHeader` xuống hàng dưới |
| < `$bp-xs` | `cardLayout = false` ([Table.md](../Components/Table.md) §API): **giữ dạng lưới** như `< $bp-md` — dạng thẻ xoá quan hệ hàng–cột, đúng thứ màn này tồn tại để cho thấy |

**Không ghim cột "Quyền" ở ngưỡng nào** ([Table.md](../Components/Table.md) §Khi nào dùng đẩy cột ghim sang `DataTable`); cái giá chấp nhận ở mọi khổ: nhãn quyền trôi khỏi khung khi cuộn ngang với người dùng chuột — `aria-label` từng ô nêu **cả** quyền lẫn vai trò nên bàn phím và trình đọc màn hình không mất. Không có chế độ xem riêng cho khổ nhỏ. Vùng cuộn nhận focus (`tabindex="0"`, `role="region"`, nhãn) theo Table.md.

### Icon

Không dùng icon ngoài bộ component tự mang (`Check`, `Tooltip`, `NoticeBanner`, `ConfirmDialog` `warning` — `pi-exclamation-triangle` ở [Icons.md](../Icons.md) §5). Nút ở `PageHeader` và banner là nút chữ; nếu cần icon thì "Lưu ma trận" `pi-save`, "Tải lại ma trận" `pi-refresh` — đã có.

### Ảnh màn hình

Chưa có — repo chưa có `src/`, không có gì để chụp.

### Cần chốt

Không còn.
