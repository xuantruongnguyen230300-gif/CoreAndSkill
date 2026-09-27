---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Khuôn — màn danh sách

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> **File này khác [`Screen.md`](./Screen.md), và khác ở một điều duy nhất.**
>
> `Screen.md` khai **cách viết một file spec màn hình** — frontmatter nào, mục nào, thứ tự nào. Nó áp cho mọi loại màn.
>
> File bạn đang đọc khai **bố cục bắt buộc của riêng loại màn danh sách**. Viết spec một màn danh sách thì dùng cấu trúc file của `Screen.md` **và** bố cục của file này. Không file nào thay được file kia.
>
> 🛑 Khuôn này mang `scope: core` — nghĩa là nó **đi theo Core sang mọi dự án**. Đây là khuôn duy nhất trong `Templates/` làm việc đó; chín khuôn còn lại sinh ra file `scope: du-an`.

---

## 1. Vì sao Core cần khuôn này

Core có đủ mảnh để dựng một màn danh sách: [`../Components/PageHeader.md`](../Components/PageHeader.md), [`../Components/Toolbar.md`](../Components/Toolbar.md), [`../Components/DataTable.md`](../Components/DataTable.md), [`../Components/Pagination.md`](../Components/Pagination.md). Mỗi mảnh đặc tả rất kỹ.

**Nhưng không có gì ép một màn danh sách phải lắp chúng lại cho đủ.** Hệ quả đã xảy ra thật: một bản dựng thử giao diện ra đời với màn lưới **không có thanh tìm kiếm và bộ lọc**, và nó không vi phạm spec nào cả — vì không spec nào nói màn danh sách phải có.

Đây không phải lỗi của người dựng. Đó là lỗ hổng của thư viện: **một mục lục component không mô tả được một màn hình.**

### Vì sao là khuôn, không phải một component gộp

Cách chữa hiển nhiên — gom bốn component thành một `ListPage` — đã bị loại sau khi cân:

| Chiều | Đảo ngược được không |
| --- | --- |
| Bốn component rời + một khuôn | ✅ Một màn không theo khuôn vẫn chạy. Sửa khuôn không sửa code |
| Một component gộp | 🛑 Khi đã lan ra 30 màn thì gỡ ra là sửa 30 màn cùng lúc |

Thêm hai lý do cứng: một component gộp phải phơi toàn bộ biến thể của bốn con ra thành input, nên nó vượt ngưỡng năm biến thể mà [`../COMPONENTS.md`](../COMPONENTS.md) §2.3 cấm; và để lọc được, nó phải biết *"lọc theo trường nào"* — tức Core bắt đầu mang từ vựng của nghiệp vụ đầu tiên, đúng thứ [`../../kien-truc-core-module.md`](../../kien-truc-core-module.md) gọi là *"Core biết tên của nghiệp vụ"*.

Khuôn ép được kỷ luật lắp ghép mà không chạm một dòng nào của bề mặt Core. Đó là cách chữa rẻ hơn và đảo ngược được.

---

## 2. Bố cục màn danh sách — định nghĩa gốc


Sáu vùng, theo đúng thứ tự dọc này:

```text
┌─ PageHeader ──────────────────────────────────┐
│  đường dẫn phân cấp · tiêu đề · mô tả         │
│                        [hành động chính] ─────┤
├─ NoticeBanner (tuỳ chọn) ─────────────────────┤
├─ Toolbar ─────────────────────────────────────┤
│  [ô tìm]  [nút lọc ⓷]        [tải lại][xuất]  │
│  chip điều kiện đang bật                      │
├─ khung DataTable ─────────────────────────────┤
│  ┌ th dính đỉnh ───────────────────────────┐  │
│  │ thân bảng — vùng cuộn DUY NHẤT của màn  │  │
│  └──────────────────────────────────────────┘ │
│  dải tổng (tuỳ chọn) — khoá cứng, ngoài cuộn  │
├─ Pagination ──────────────────────────────────┤
└───────────────────────────────────────────────┘
```

| # | Vùng | Bắt buộc? | Ràng buộc |
| --- | --- | --- | --- |
| 1 | `PageHeader` | ✅ **Bắt buộc** | Tiêu đề nói **danh sách của cái gì**. Đúng một hành động chính, là `Button` biến thể `primary` |
| 2 | `NoticeBanner` | ⬜ Tuỳ chọn | Chỉ khi có bối cảnh cả màn cần biết. Không dùng để báo lỗi tải — lỗi tải thuộc trạng thái của bảng |
| 3 | `Toolbar` | ✅ **Bắt buộc** | Xem §3 |
| 4 | `DataTable` | ✅ **Bắt buộc** | Thân bảng là **vùng cuộn duy nhất** của màn — xem §4 |
| 5 | Dải tổng | ⬜ Tuỳ chọn | Bật công tắc `summary`. Nhãn **phải** mang phạm vi |
| 6 | Dải phân trang | ✅ **Bắt buộc** với biến thể `paged` | Nằm **ngoài** khung bảng, và do `DataTable` vẽ — màn **không** tự đặt một [`../Components/Pagination.md`](../Components/Pagination.md). Hợp đồng phân trang ở [`../Components/DataTable.md`](../Components/DataTable.md) §API |

🛑 **Không chèn vùng nào khác vào giữa `Toolbar` và bảng.** Mỗi thứ chen vào đó đẩy bảng xuống và ăn mất chiều cao của thứ duy nhất người dùng mở màn này để xem.

### Ngoại lệ có tên — màn Core không phải màn danh sách

Màn trong bảng dưới **không** phải màn danh sách, nên sáu vùng ở trên và §3 không áp cho nó; spec màn tự khai bố cục. Một màn chỉ được coi là ngoại lệ khi có dòng ở đây, kèm lý do.

| Màn | Dựng bằng | Vì sao không phải màn danh sách |
| --- | --- | --- |
| Ma trận phân quyền — [`../Screens/12-ma-tran-phan-quyen.md`](../Screens/12-ma-tran-phan-quyen.md) | [`../Components/Table.md`](../Components/Table.md) + [`../Components/Check.md`](../Components/Check.md) | Danh mục quyền không phân trang ([`../../contracts/permissions.md`](../../contracts/permissions.md) §4) và mọi ô đều tương tác — đúng ca [`../Components/DataTable.md`](../Components/DataTable.md) loại khỏi `DataTable` |

---

## 3. Thanh tìm kiếm và bộ lọc là BẮT BUỘC

> **Một màn danh sách không có `Toolbar` là một màn danh sách chưa xong.**

Không có ngoại lệ theo số bản ghi. Lý do: số bản ghi hôm nay không phải số bản ghi sang năm. Một danh sách vai trò có sáu dòng lúc bàn giao sẽ có sáu mươi dòng sau hai năm, và lúc đó không ai quay lại thêm ô tìm.

Biến thể `Toolbar` chọn theo số **trường lọc**, không theo số bản ghi:

| Số trường lọc | Biến thể | Panel lọc ở đâu |
| --- | --- | --- |
| 0 | `search` | Không có panel |
| 1–2 | `full` — khu lọc nằm thẳng trong dải | Nằm thẳng trong `Toolbar`, cùng hàng ô tìm |
| ≥ 3 | `full` — khu lọc là nút mở panel | [`../Components/Drawer.md`](../Components/Drawer.md) cỡ `lg` — xem [`../Components/Toolbar.md`](../Components/Toolbar.md) §"Panel lọc nằm ở đâu" |

Ba điều đi kèm, không bỏ được:

1. **Mọi điều kiện đang bật phải hiện thành [`../Components/FilterChip.md`](../Components/FilterChip.md).** Một bộ lọc đang chặn dữ liệu mà không thấy được là lý do phổ biến nhất của câu hỏi *"sao dữ liệu của tôi mất rồi?"*.
2. **Điều kiện do hệ thống tự áp cũng phải hiện**, ở biến thể `readonly`. Lọc theo đơn vị của người dùng là thứ họ không đặt và không gỡ được — nhưng giấu đi thì danh sách ngắn hơn họ tưởng mà không rõ vì sao.
3. **Trạng thái rỗng phải phân biệt hai ca.** *Chưa có bản ghi nào* mời tạo mới; *lọc không ra kết quả* nói rõ điều kiện và cho xoá lọc. Trộn hai ca là lỗi hay gặp nhất ở màn danh sách — [`../Components/DataTable.md`](../Components/DataTable.md) đã ghi.

---

## 4. Chỉ một vùng cuộn

**Thân bảng là vùng cuộn duy nhất của màn danh sách.** `PageHeader`, `Toolbar`, hàng chip, dải tổng và `Pagination` đứng yên.

Vì sao: người dùng màn danh sách làm ba việc lặp đi lặp lại — đổi điều kiện lọc, đọc dữ liệu, đổi trang. Nếu cả trang cuộn thì hai trong ba việc đó đòi cuộn ngược lên trước. Ở một bảng 25 dòng, đó là hàng chục lần cuộn thừa mỗi phiên làm việc.

Hệ quả cho người dựng: khung ứng dụng cao trọn màn hình, `main` không cuộn, khung bảng ăn hết chiều cao còn lại. Chiều cao vùng cuộn là **quyết định của trang**, không phải của component — [`../Components/DataTable.md`](../Components/DataTable.md) §Kích thước.

---

## 5. Điểm mở rộng — chỗ dự án hạ nguồn được phép khác

Đây là mục làm khuôn này có ích cho **dự án thứ hai**, không chỉ dự án đầu.

**Màn danh sách của một DỰ ÁN** tự ghép component, tự quyết tất cả. **Màn danh sách của CORE** — người dùng, vai trò, đơn vị — thì dự án hạ nguồn chỉ đổi được qua seam `CORE_SCREEN_EXT`, và seam đó hôm nay mở đúng một việc: **thêm cột dạng chữ**. Hợp đồng từng trường sống ở [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.7; bảng dưới chỉ nói hệ quả nhìn thấy trên màn, không chép lại hợp đồng. Bảng nói về ca thứ hai, ca khó:

| Dự án đổi gì | Đổi ở đâu | Có phải sửa file Core không |
| --- | --- | --- |
| **Thêm** cột | Trường `columns` của seam, khai theo mã màn; cột nối vào **sau** cột của Core | Không |
| Tiêu đề cột thêm | Dự án cấp **khoá i18n**; màn Core dịch khi dựng cột. Không cấp chuỗi đã dịch | Không |
| Nội dung ô của cột thêm | Chỉ **chữ** — một hàm nhận dòng, trả chuỗi đã định dạng. Ô không chứa được liên kết, `Badge` hay nút: seam không nhận `TemplateRef` | Không |
| Bề rộng, căn lề, ẩn ở màn hẹp của cột thêm | Các trường tuỳ chọn của cột, cùng nghĩa với `width`, `align`, `hideBelow`, `priority` ở [`../Components/DataTable.md`](../Components/DataTable.md) | Không |
| Sắp xếp theo cột thêm | 🛑 Không có đường. Endpoint danh sách của Core chỉ sắp xếp theo cột nó biết | — |
| **Thêm** trường lọc | **Chưa có** điểm mở rộng — hoãn theo [`../../adr/0057-seam-fe-chi-mang-gia-tri-dung-duoc-o-composition-root.md`](../../adr/0057-seam-fe-chi-mang-gia-tri-dung-duoc-o-composition-root.md) | Có — hôm nay chỉ qua một PR Core kèm ADR mới |
| **Thêm** hành động trên một dòng | **Chưa có** điểm mở rộng — hoãn theo cùng ADR | Có — như dòng trên |
| Chèn nút riêng vào `Toolbar` | **Chưa có** điểm mở rộng — hoãn theo cùng ADR | Có — như dòng trên |
| Thêm hành động hàng loạt | **Chưa có** điểm mở rộng — hợp đồng ở §2.7 không có trường nào cho việc này | Có — như dòng trên |
| **Bớt** cột, **bớt** hành động | 🛑 Không có đường. Cần bớt là dấu hiệu màn Core sai — sửa ở Core cho mọi dự án | — |
| Đổi **thứ tự sáu vùng** ở §2 | 🛑 Không được. Đây là phần khuôn ép | — |

🛑 **Một màn danh sách của Core mà dự án hạ nguồn cần cột riêng thì mở rộng qua `columns`, không phải bằng cách dựng một màn thứ hai.** Nhân đôi màn Core là hình thức fork tinh vi nhất: nó không sửa file nào của Core nên cổng không thấy, nhưng bản vá Core lần sau sẽ không tới được màn đã nhân đôi. Cột dạng chữ không đủ — cần ô bấm được, cần `Badge` — thì đó là tín hiệu nới hợp đồng ở Core bằng một ADR mới, cũng không phải lý do dựng màn thứ hai.

---

## 6. Danh sách kiểm — trước khi coi một màn danh sách là xong

- [ ] Có `PageHeader` với đúng **một** hành động chính.
- [ ] Có `Toolbar`, và ô tìm hoạt động — **kể cả khi danh sách hôm nay chỉ có vài dòng**.
- [ ] Mọi điều kiện lọc đang bật đều hiện thành chip, gỡ được; điều kiện hệ thống áp hiện ở dạng `readonly`.
- [ ] Hai ca rỗng (*chưa có gì* / *lọc không ra*) có câu chữ và nút khác nhau.
- [ ] Chỉ thân bảng cuộn; năm vùng còn lại đứng yên.
- [ ] Có dải tổng thì nhãn mang **phạm vi** ("Tổng cộng 137 bản ghi"), và số do máy chủ tính.
- [ ] Trạng thái `loading` lần đầu dùng skeleton; đổi trang thì giữ dữ liệu cũ và phủ mờ.
- [ ] Trạng thái bảng (trang, sắp xếp, lọc, từ khoá) giữ trên URL — mở lại link ra đúng danh sách đó.
- [ ] Đã trả lời: **dự án hạ nguồn đổi được thứ gì ở màn này mà không sửa file Core?**
- [ ] `bash .claude/check-docs.sh` xanh.

---

## 7. Việc khuôn này KHÔNG giải

Nói ra để không ai tưởng đã xong:

- **Tầng trạng thái danh sách đã có hiện thực, nhưng khuôn này không ép được chất lượng của nó.** [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.8 khai `GridQuery` và `ListStateStore` kèm các luật của mục đó — giữ truy vấn, đồng bộ URL, chống gọi dồn dập, huỷ đáp ứng cũ. Code đã về (đối chiếu 2026-09-22, chỉ bằng đọc mã, không chạy test): `src/FE/src/app/core/list/list-state.store.ts` — class `ListStateStore`, đọc truy vấn từ URL ở § `docQuery`, ghi lại URL ở § `ghiUrl`, chống gọi dồn dập bằng § `debounceTime(NGUNG_GO_MS)` và § `distinctUntilChanged(cungTruyVan)`, huỷ đáp ứng cũ bằng § `switchMap`; `GridQuery` ở `src/FE/src/app/core/list/grid-query.ts`. Khuôn này chỉ khai bố cục màn — nó không kiểm được hiện thực đó đúng với từng luật của §2.8.
- **Mọi màn Core phải tự đọc `CORE_SCREEN_EXT` và tự nối phần mở rộng.** Việc **đọc** token nay có cổng — F25 ở [`../../RULES.md`](../../RULES.md) §7: section `F25` của `scripts/fe-gate.sh` đòi mọi `*.page.ts` dưới `platform/` có dùng `ListStateStore` phải chứa `inject(CORE_SCREEN_EXT`. Cổng **chỉ** kiểm việc đọc. Màn đã đọc token mà quên nối một điểm mở rộng thì vẫn **im lặng không mở rộng được** ở điểm đó — dự án hạ nguồn khai cột mà cột không hiện ra, không lỗi biên dịch; và một màn danh sách không dùng `ListStateStore` thì cổng không nhận ra là màn danh sách. [`../../RULES.md`](../../RULES.md) giao phần "có hiện ra trên màn hay không" cho test của từng màn. Xem [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.7.

Hai khoản này là nợ nhìn thấy được, cùng khuôn với [`../../DEBT.md`](../../DEBT.md).
