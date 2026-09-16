---
kind: luat
scope: core
verified: chua-doi-chieu
---

# DataTable

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** **bọc PrimeNG**. Theo tiêu chí ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 đây là ca rõ nhất của nhóm "khó": ảo hoá dòng, cột ghim, sắp xếp, phân trang phía máy chủ — vừa nhiều ca biên vừa nhạy hiệu năng.

**Đây là nơi DUY NHẤT trong toàn ứng dụng được import lưới dữ liệu của PrimeNG.** Một màn nghiệp vụ import thẳng là vi phạm [`../../RULES.md`](../../RULES.md) §7 F5, và quan trọng hơn: nó phá luôn khả năng đổi thư viện sau này.

**`DataTable` là chủ hợp đồng phân trang của màn danh sách.** Ở biến thể `paged`, dải phân trang là **phần cấu trúc của lớp bọc** ([`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5): `DataTable` dùng [`Pagination.md`](./Pagination.md) bên trong và vẽ nó ngay dưới khung bảng — màn **không** tự đặt thêm một `Pagination` nào. Vì vậy `page`, `pageSize`, `totalRecords` và sự kiện đổi trang vào ra qua API của `DataTable` ở mục API; [`Pagination.md`](./Pagination.md) giữ hình thức, trạng thái và accessibility của chính dải đó, không giữ hợp đồng của màn. Hai nơi cùng khai một hợp đồng là cách chúng lệch nhau ([`../../../.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §5).

**Lớp bọc không import component tự dựng** — luật chiều import giữa hai tầng ở [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.2. Vì vậy nội dung do màn quyết — khối **trống**, khung **đang tải lần đầu**, khối **lỗi** kèm nút "Thử lại" — vào qua input `TemplateRef`: màn ghép [`EmptyState.md`](./EmptyState.md) và [`SkeletonLoader.md`](./SkeletonLoader.md) vào đó. Hợp đồng slot ở mục API. Phần cấu trúc của chính bảng — như nút "Đóng hết" của công tắc `expandable` — do thư viện vẽ, tạo hình bằng token ([`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5).

---

## Mục đích

Hiển thị một tập bản ghi lớn với phân trang, sắp xếp và chọn dòng — trong một khung có chiều cao cố định và cuộn bên trong.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Danh sách bản ghi từ máy chủ, có phân trang | 🛑 Bảng tĩnh vài dòng, không phân trang, không sắp xếp → [`Table.md`](./Table.md). Kéo cả lưới dữ liệu vào chỉ để hiện năm dòng là trả giá hiệu năng cho không |
| Danh sách cần sắp xếp theo cột | 🛑 Danh sách không phải dạng bảng (thẻ, dòng thời gian) → dựng bằng [`Card.md`](./Card.md) |
| Danh sách cần chọn nhiều dòng để thao tác hàng loạt | 🛑 Ma trận tick quyền — dùng [`Table.md`](./Table.md) + [`Check.md`](./Check.md), vì nó không phân trang và mọi ô đều tương tác |
| Bảng cần ghim cột đầu khi cuộn ngang | 🛑 So sánh hai bản ghi cạnh nhau → bố cục riêng của màn |

## Biến thể

Hai **biến thể** loại trừ nhau — chúng quyết định dữ liệu về bằng cách nào:

| Biến thể | Khác gì | Dùng khi |
| --- | --- | --- |
| `paged` | Phân trang phía máy chủ, [`Pagination.md`](./Pagination.md) ở chân | **Mặc định.** Mọi danh sách bản ghi |
| `scroll` | Cuộn vô hạn, tải thêm khi tới đáy | Chỉ khi thứ tự đọc là tuần tự (nhật ký, dòng sự kiện). 🛑 Không dùng cho danh sách người ta cần quay lại một mục cụ thể — cuộn vô hạn làm mất chỗ. Không có ngưỡng số dòng nào để đổi sang `scroll`: chọn theo bản chất dữ liệu, mặc định luôn là `paged` |

Sáu **công tắc** cộng thêm vào, kết hợp được với cả hai biến thể:

| Công tắc | Khác gì | Dùng khi |
| --- | --- | --- |
| `selectable` | Cột ô đánh dấu ở đầu, có thanh hành động hàng loạt | Khi có thao tác áp cho nhiều bản ghi |
| `frozen` | Ghim cột đầu (và cột hành động cuối) khi cuộn ngang | Bảng nhiều cột trên màn hẹp |
| `tree` | Dòng có dòng con, xoè và thu được; ô đầu tiên thụt lề theo cấp | Cây đơn vị, hệ thống tài khoản, danh mục phân cấp, địa bàn |
| `grouped` | Dòng gộp theo một cột, mỗi nhóm có dòng tiêu đề mang số cộng dồn | Bảng lương gộp theo phòng, chi phí theo khoản mục |
| `expandable` | Bấm một dòng bung ra một khối chi tiết ngay dưới nó | Xem nhanh dòng chi tiết, hoặc chứa các cột đã bị ẩn ở màn hẹp |
| `summary` | Một dòng tổng khoá cứng ở đáy khung | Bất cứ bảng nào có cột số cần cộng. **Độc lập với `grouped`** — xem dưới |

**Vì sao tách biến thể khỏi công tắc:** [`../COMPONENTS.md`](../COMPONENTS.md) §2.3 cảnh báo một component quá năm biến thể gần như chắc chắn đang gánh hai vai. Nhưng những thứ ở trên không phải bấy nhiêu vai — chỉ hai biến thể đầu loại trừ nhau, các công tắc còn lại cộng dồn được. Một bảng `paged` + `selectable` + `tree` là hợp lệ và có thật. Gọi tất cả là "biến thể" làm luật ngưỡng năm báo động nhầm.

### Công tắc `tree` — mười hai quyết định đi kèm

Đây là công tắc nặng nhất, và phần lớn cái khó của nó không nhìn thấy được ở ảnh tĩnh:

| Câu hỏi | Quyết định |
| --- | --- |
| Thụt lề mỗi cấp | `--tree-indent` ([`../DESIGN.md`](../DESIGN.md) §6.1). Cùng giá trị với [`TreeSelect.md`](./TreeSelect.md), để hai chỗ hiện cùng một cây trông giống nhau |
| Nút xoè đặt đâu | Trước nội dung ô đầu. **Nút lá vẫn chiếm chỗ đúng bằng nút xoè** (`visibility: hidden`, không phải `display: none`) — bỏ hẳn chỗ thì nhãn của lá lệch trái so với nhãn của cha cùng cấp, và mắt đọc thành hai cấp khác nhau |
| Đường nối dọc | Có, một vạch `--color-border-subtle` mỗi cấp. Từ cấp ba trở lên, không có nó thì không đếm được mình đang ở nhánh nào. Dùng bậc `subtle` vì nó là vạch **bên trong** một bề mặt |
| Chọn cha có kéo theo con | Có, và cha thành nửa chọn khi con chọn một phần — dùng lại trạng thái `indeterminate` của [`Check.md`](./Check.md). 🛑 Gửi lên máy chủ **cha** hay **danh sách con** là quyết định nghiệp vụ, màn hình phải khai rõ |
| Sắp xếp | **Không phẳng hoá cây.** Chỉ sắp xếp giữa các anh em cùng một cha. Phẳng hoá khi bấm sắp xếp là cách nhanh nhất làm người dùng mất phương hướng |
| Lọc | **Giữ lại cha không khớp**, hiện mờ, để thấy đường dẫn tới kết quả. Cha giữ vì *đường dẫn*, không vì nó khớp — nên **không tính vào số kết quả** |
| Phân trang | Theo **nút gốc**, con luôn đi cùng cha. Cắt ngang một nhánh giữa hai trang là vô nghĩa. Chỉ báo phải nói "trên 6 **đơn vị gốc**", không nói "trên 6 bản ghi" |
| Nhánh chưa tải | Nút xoè đổi thành `pi-spinner`, `aria-busy` trên chính nút đó |
| Cha hoá ra không có con | Sau khi tải xong, nút xoè **biến thành dạng lá**. Để lại một nút bấm mãi không ra gì là lỗi người dùng báo nhiều nhất ở lưới cây |
| Cột đầu | **Bắt buộc ghim** khi có cuộn ngang. Cột cây trôi đi thì thụt lề mất hết nghĩa — người dùng thấy số liệu mà không biết của đơn vị nào |
| Dòng cha ở cột số | Tổng của các con, màu `--color-text-muted` và đậm để phân biệt với số liệu của chính nó. Cha vừa có số riêng vừa có tổng con thì **phải tách hai cột** — trộn vào một là chỗ sai không ai phát hiện cho tới lúc đối chiếu sổ |
| Nhớ trạng thái mở | Trong một phiên làm việc, theo **khoá bản ghi** chứ không theo chỉ số dòng. Không nhớ qua lần đăng nhập khác — trạng thái cũ thường không còn đúng với dữ liệu mới |

### Công tắc `grouped` — dòng tiêu đề nhóm

Mỗi nhóm mở đầu bằng một dòng tiêu đề mang **số cộng dồn của riêng nhóm đó**, xoè và thu được. Đây là số của nhóm, khác hẳn dòng tổng của cả bảng — dòng tổng thuộc công tắc `summary`.

### Công tắc `summary` — dòng tổng khoá cứng ở đáy

**Dòng tổng là một công tắc riêng, không phải một phần của `grouped`.** Một bảng phẳng có phân trang vẫn cần dòng tổng, và đó là ca phổ biến hơn cả. Trói nó vào `grouped` là để một bảng phẳng không có đường nào bật nó lên.

#### Ghim thế nào — và vì sao `<tfoot>` một mình không đủ

🛑 **`position: sticky; bottom: 0` trên `<tfoot>` KHÔNG đủ.** Nó chỉ ghim khi bảng **cao hơn** vùng cuộn; bảng ngắn hơn khung thì dòng tổng nằm lửng ngay sau dòng dữ liệu cuối, giữa khung là một khoảng trống. Đó không phải "khoá cứng ở đáy".

Dòng tổng vì vậy **nằm ngoài vùng cuộn dọc** — nó là một dải riêng của khung bảng, đặt dưới vùng cuộn và trên chân khung. Hệ quả phải chấp nhận, nói thẳng:

| Cái giá | Nghĩa là |
| --- | --- |
| Hai bảng thay vì một | Dải tổng là bảng thứ hai, nên cả hai phải `table-layout: fixed` và dùng **cùng một `colgroup`**; nếu không, cột số của hai bảng lệch nhau |
| Phải đồng bộ cuộn ngang bằng script | Thân bảng cuộn sang phải thì dải tổng phải cuộn theo đúng bấy nhiêu. Không đồng bộ thì con số nằm dưới sai cột — một lỗi trông như lỗi dữ liệu |
| Công tắc `frozen` giao với dải tổng | Cột đầu ghim thì ô đầu của dải tổng cũng phải ghim cùng vị trí. Đây là giao điểm khó nhất của bố cục bảng và là chỗ phải kiểm kỹ nhất khi dựng |

Dải tổng dùng nền `--color-surface-3`, viền trên `--border-w-strong` màu `--color-border`, chữ `--fw-bold`. Nó nằm **trong** khung bảng; [`Pagination.md`](./Pagination.md) nằm **dưới** khung. Hai dải ngang cạnh nhau ở chân màn phân biệt bằng ranh giới của chính khung bảng — dải tổng ở trong đường viền, phân trang ở ngoài.

#### Nhãn dòng tổng phải nói rõ PHẠM VI

🛑 **Đây là ràng buộc cứng, không phải gợi ý.** Trên một bảng có phân trang, "Tổng cộng" có thể là tổng của **trang đang xem** hoặc tổng của **toàn bộ kết quả sau lọc**. Hai số khác nhau, trông giống hệt nhau, và người dùng đối chiếu sổ sách sẽ lấy nhầm.

Nhãn vì vậy luôn mang phạm vi: **"Tổng cộng 137 bản ghi"** hoặc **"Tổng cộng trang này"**. Không bao giờ chỉ hai chữ "Tổng cộng".

Khi bộ lọc đổi mà tổng là tổng toàn bộ kết quả, con số phải đổi theo — một dòng tổng không phản ứng với bộ lọc là một dòng tổng nói dối.

#### Tổng do máy chủ tính

🛑 **Không cộng ở trình duyệt.** Bảng chỉ hiện một trang; cộng những gì đang hiện ra một dòng "Tổng cộng" là một con số sai trông rất giống số đúng. Máy chủ trả về số tổng cùng với trang dữ liệu, và component nhận nó qua `summary` ở mục API.

### Công tắc `expandable` — mở nhiều dòng cùng lúc

Khối chi tiết có dải trái `--border-w-accent` màu `--color-brand` để mắt nối nó với dòng cha. **Cho mở nhiều dòng cùng lúc**, kèm nút "Đóng hết" do `DataTable` vẽ trong một dải ngay trên hàng tiêu đề cột. Dải có mặt suốt lúc bật công tắc; nút chỉ bấm được khi có ít nhất một dòng đang mở — dải không hiện ra rồi biến mất theo số dòng mở, để khung bảng không nhảy. Nút mượn hình thức của [`Button.md`](./Button.md) theo [`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5. Cho mở một dòng thì bảng gọn hơn nhưng mất khả năng đối chiếu hai bản ghi, và đối chiếu đúng là lý do người ta mở chi tiết.

## Kích thước

| Khoản | Giá trị |
| --- | --- |
| Chiều cao vùng cuộn | Do trang quyết định, tối thiểu đủ chỗ cho vài dòng — xem ghi chú dưới |
| Chiều cao một dòng | `--size-control-md` (34px) + đệm dọc `--sp-3` |
| Đệm ô | `--sp-3` dọc / `--sp-4` ngang |
| Cỡ chữ ô | `--fs-sm` |
| Cỡ chữ tiêu đề cột | `--fs-xs`, `--fw-semibold`, màu `--color-text-muted` |
| Bo góc khung | `--radius-lg` |
| Viền khung | `--border-w` `--color-border-strong` |

**Chiều cao vùng cuộn là quyết định của trang, không phải của component.** Đặt một con số cố định trong `DataTable` nghe tiện và sai: chiều cao phần chrome phía trên (`PageHeader` + `Toolbar`) và phía dưới (`Pagination` + `Footer`) khác nhau ở mỗi màn. Trang truyền chiều cao xuống, hoặc dùng một chuỗi flex để bảng ăn hết chỗ còn lại.

**Vì sao khung dùng `--color-border-strong` chứ không `--color-border`:** khung bảng là vùng nội dung **tương tác được** — bấm sắp xếp, tick chọn, cuộn. Nó cùng nhóm với ô nhập trong luật ba bậc viền ở [`../DESIGN.md`](../DESIGN.md) §2.3.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Dòng nền `--color-surface`; vạch giữa hai dòng `--color-border-subtle`; `th` nền `--color-surface-2`, dính đỉnh khi cuộn (`--z-sticky`) | Có |
| `hover` | Dòng đổi nền `--color-surface-2`; con trỏ đổi thành `pointer` **chỉ khi** dòng bấm được. Bọc trong `@media (hover: hover)` | Có |
| `focus-visible` | Dòng nhận focus có `outline` `--color-focus` **bên trong** (`outline-offset: -2px`) — vòng ngoài sẽ bị cắt bởi vùng cuộn. `th` sắp xếp được nhận vòng focus như một nút | Có |
| `active` | Dòng đang được chọn: nền `--color-brand-subtle`, dải `--border-w-accent` `--color-brand` ở mép trái. **Cần cả hai** — nền chênh `--color-surface` chỉ 1.25:1 nên không đủ làm tín hiệu duy nhất ([`../DESIGN.md`](../DESIGN.md) §2.4) | Có |
| `disabled` | Dòng không thao tác được: chữ `--color-text-disabled`, không hover, ô đánh dấu bị khoá. **Vẫn đọc được** — không giảm opacity cả dòng | Có |
| `loading` | **Hai ca khác nhau.** *Lần đầu* — chưa có dòng nào để giữ lại: thân bảng hiện **slot đang tải** (`loadingTemplate`), màn ghép [`SkeletonLoader.md`](./SkeletonLoader.md) dạng hàng, đúng số dòng của trang. *Đổi trang / sắp xếp lại:* `DataTable` tự giữ nguyên dữ liệu cũ, phủ `--color-scrim` + spinner, khoá thao tác. `aria-busy="true"` cả hai ca | Có |
| `error` | Thân bảng hiện **slot lỗi** (`errorTemplate`): màn ghép [`EmptyState.md`](./EmptyState.md) biến thể `error` kèm nút "Thử lại" gọi thẳng việc tải lại của màn. Tiêu đề cột **giữ nguyên** — chúng cho biết đây là bảng gì | Có |
| `empty` | **Hai ca phải phân biệt**, và thân bảng hiện **slot trống** (`emptyTemplate`) cho cả hai. Màn chọn nội dung theo `state`: *Chưa có bản ghi nào* (`'empty'`) → [`EmptyState.md`](./EmptyState.md) biến thể `first-use`, mời tạo mới. *Lọc không ra kết quả* (`'empty-filtered'`) → biến thể `no-results`, nút "Xoá bộ lọc", **không** mời tạo mới. Tiêu đề cột giữ nguyên | Có |

**Ca `loading` khi đổi trang là chỗ hay làm sai nhất.** Thay dữ liệu cũ bằng skeleton mỗi lần bấm sang trang làm bảng nhấp nháy và cao thấp thất thường. Giữ dữ liệu cũ + phủ mờ giữ cho khung ổn định, và người dùng vẫn thấy mình đang ở đâu.

**Hai ca `empty` trộn làm một là lỗi hay gặp nhất ở màn danh sách.** Người dùng vừa gõ một bộ lọc và thấy màn hình mời "Thêm bản ghi đầu tiên" sẽ tưởng dữ liệu đã mất.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-surface-2`, `--color-text`, `--color-text-muted`, `--color-text-disabled`, `--color-border`, `--color-border-subtle`, `--color-border-strong`, `--color-brand`, `--color-brand-subtle`, `--color-focus`, `--color-scrim` |
| Chữ | `--fs-xs`, `--fs-sm`, `--fw-semibold`, `--fw-regular`, `--lh-snug` |
| Khoảng cách | `--sp-3`, `--sp-4` |
| Hình dạng | `--radius-lg`, `--border-w`, `--border-w-accent` |
| Kích thước | `--size-control-md`, `--size-control-sm`, `--icon-sm`, `--icon-md`, `--tree-indent` |
| Lớp | `--z-sticky` |
| Chuyển động | `--dur-fast`, `--ease-standard` |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-lg` | Mọi cột hiện; cột hành động ghim mép phải |
| `$bp-md` … `$bp-lg` | Ẩn các cột đánh dấu `priority: low`; bảng bắt đầu cuộn ngang |
| < `$bp-md` | Chỉ giữ cột định danh + một cột trạng thái + cột hành động; ghim cột đầu |
| < `$bp-xs` | Chuyển sang dạng thẻ: mỗi bản ghi một khối, nhãn cột thành nhãn trường. Dạng thẻ là hành vi responsive của chính `DataTable`, không phải component riêng ([`../COMPONENTS.md`](../COMPONENTS.md) §1 — mở rộng, không đẻ mới); ngữ nghĩa bảng giữ như [`Table.md`](./Table.md) §Accessibility dòng dạng thẻ |

🛑 **Cột bị ẩn phải nói rõ đi đâu.** Thông tin không được biến mất — hoặc nó có ở màn chi tiết, hoặc nó hiện khi mở rộng dòng. Ẩn mà không nói đi đâu là mất dữ liệu ở màn nhỏ.

**Vì sao cần dạng thẻ chứ không chỉ cuộn ngang:** một bảng bảy cột trên màn 390px phải cuộn ba lần mới đọc hết một dòng, và người dùng mất mốc dòng nào là dòng nào. Cái giá của dạng thẻ: một bản ghi chiếm nhiều chiều dọc, so sánh giữa các bản ghi khó hơn hẳn. Đó là đánh đổi đúng ở khổ màn này.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | `<table>` thật với `<thead>`, `<tbody>`, `<th scope="col">`. 🛑 Không dựng bảng bằng `<div role="grid">` khi dữ liệu là bảng thật |
| Tiêu đề bảng | `<caption>`, ẩn về mặt hình ảnh nếu tiêu đề đã hiện ở `PageHeader` — nhưng **phải có** |
| Sắp xếp | `th` sắp xếp được chứa một `<button>`; `aria-sort` nhận `ascending` / `descending` / `none` |
| Chọn dòng | Ô đánh dấu có `aria-label` nêu **bản ghi nào** ("Chọn người dùng Nguyễn Văn A"), không phải "Chọn" |
| Chọn tất cả | Ô đánh dấu ở `th` dùng trạng thái `indeterminate` khi chỉ chọn một phần. Phạm vi là **trang hiện tại** — `rows` và `selection` chỉ chứa dòng đã tải (§API); nhãn nói rõ điều đó, vì người dùng luôn hiểu nhầm thành toàn bộ kết quả. Chọn toàn bộ kết quả sau lọc cần hợp đồng phía máy chủ, không có ở v1 |
| Vùng cuộn | Vùng cuộn ngang phải nhận focus (`tabindex="0"` + `role="region"` + `aria-label`). Không có nó, người dùng bàn phím **không cuộn ngang được** |
| Bàn phím | Tab đi qua các control trong bảng theo thứ tự đọc. Không bẫy focus trong vùng cuộn |
| Thay đổi dữ liệu | Đổi trang hoặc đổi sắp xếp thì thông báo qua vùng `aria-live="polite"`: "Đang hiện 21 đến 40 trên 137 bản ghi" |
| `loading` | `aria-busy="true"` trên vùng bảng |
| Công tắc `tree` — vai trò | Bảng đổi sang `role="treegrid"`. Mỗi dòng khai `aria-level`, `aria-expanded` (nếu có con), `aria-setsize`, `aria-posinset`. Thụt lề là tín hiệu **thị giác**; không khai bốn thuộc tính này thì trình đọc màn hình nghe thấy một danh sách phẳng và toàn bộ cấu trúc biến mất |
| Công tắc `tree` — bàn phím | `→` mở nhánh, đang mở thì sang ô kế. `←` đóng nhánh, đang đóng thì **nhảy về dòng cha**. `↑` `↓` giữa các dòng **đang hiện**. `Home` / `End` về đầu / cuối. Thiếu phần này thì cây chỉ dùng được bằng chuột |
| Công tắc `grouped` | Dòng tiêu đề nhóm là `role="row"` với một ô `colspan`, mang `aria-expanded` |
| Công tắc `summary` | Dải tổng là một `<table>` riêng nên nó **mất quan hệ hàng–cột** với bảng chính. Bù lại bằng hai thứ: mỗi ô của dải mang `headers` trỏ tới `id` của `<th>` tương ứng, và cả dải mang `aria-label` nói rõ phạm vi ("Tổng cộng 137 bản ghi"). Không bù thì trình đọc màn hình đọc ra một dãy số không biết của cột nào |
| Công tắc `summary` — lớp | Dải tổng nằm ngoài vùng cuộn nên **không cần** `--z-sticky`; nó không chồng lên gì cả. Chỉ ô đầu khi bật kèm `frozen` mới cần lớp, và dùng cùng lớp với cột ghim của thân bảng |
| Công tắc `expandable` | Nút bung là `<button>` thật với `aria-expanded` và `aria-controls` trỏ tới `id` của khối chi tiết. Khối chi tiết là một dòng `<tr>` thật với ô `colspan`, không phải một khối tuyệt đối chèn ngoài bảng. Nút "Đóng hết" là `<button>` thật, mang `disabled` khi không dòng nào đang mở |
| Chữ | Tiêu đề cột, nhãn, thông báo qua i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

**Vùng cuộn không nhận được focus là lỗi accessibility bị bỏ sót nhiều nhất ở bảng.** Chuột có thanh cuộn, cảm ứng có ngón tay, bàn phím thì **không có gì cả** trừ khi vùng đó nhận được focus.

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `rows` | input | `ReadonlyArray<T>` | `[]` | Dữ liệu của **trang hiện tại**, không phải toàn bộ |
| `columns` | input | `ReadonlyArray<DataColumnDef<T>>` | `[]` | Chữ ký đầy đủ ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9. **Đây là điểm mở rộng chính**: dự án hạ nguồn thêm hoặc đổi cột qua đây, không sửa file Core |
| `variant` | input | `'paged' \| 'scroll'` | `'paged'` | Hai biến thể loại trừ nhau ở mục Biến thể. `paged` vẽ dải [`Pagination.md`](./Pagination.md) ở chân khung và nhận `page` / `pageSize` / `totalRecords`; `scroll` không vẽ dải nào và tải thêm khi tới đáy vùng cuộn |
| `switches` | input | `DataTableSwitches` | `{}` | Sáu công tắc ở mục Biến thể, khai bằng **một** object có tên. Chữ ký ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9 — `frozen` nhận cả `'first' \| 'both'`, không chỉ boolean |
| `groupBy` | input | `string \| null` | `null` | Khoá cột để gộp nhóm. Bắt buộc khác `null` khi bật công tắc `grouped` |
| `rowDetail` | input | `TemplateRef<{ $implicit: T }> \| null` | `null` | Nội dung khối bung ra. Bắt buộc khác `null` khi bật công tắc `expandable` |
| `emptyTemplate` | input | `TemplateRef<{ $implicit: 'empty' \| 'empty-filtered' }> \| null` | `null` | Nội dung slot trống; context mang ca rỗng để màn chọn biến thể `EmptyState`. Màn danh sách luôn truyền — thiếu thì thân bảng trống trơn |
| `loadingTemplate` | input | `TemplateRef<unknown> \| null` | `null` | Nội dung slot đang tải lần đầu. Màn danh sách luôn truyền |
| `errorTemplate` | input | `TemplateRef<unknown> \| null` | `null` | Nội dung slot lỗi, **gồm cả nút "Thử lại"**. Màn danh sách luôn truyền |
| `nodeExpanded` | output | `string` | — | Chỉ ở công tắc `tree`: phát khoá nhánh vừa xoè, để trang cha tải con nếu `hasChildren` là `true` mà `children` rỗng |
| `totalRecords` | input | `number` | `0` | Tổng số bản ghi phía máy chủ. Chỉ có nghĩa ở `variant = 'paged'` |
| `page` / `pageSize` | input | `number` | `1` / `20` | Chỉ có nghĩa ở `variant = 'paged'`. Danh sách số dòng mỗi trang cho dải phân trang: [`Pagination.md`](./Pagination.md) §API (`pageSizeOptions`) |
| `sortBy` / `sortDescending` | input | `string \| null` / `boolean` | `null` / `false` | Đúng tên tham số trên dây ở [`../../contracts/README.md`](../../contracts/README.md) §8. Quy đổi sang dạng `1 \| -1` của PrimeNG nằm **trong** lớp bọc |
| `selection` | input | `ReadonlyArray<T>` | `[]` | |
| `state` | input | `'idle' \| 'loading' \| 'error' \| 'empty' \| 'empty-filtered'` | `'idle'` | **Một biến duy nhất**, không phải bốn cờ boolean rời — xem dưới |
| `rowKey` | input | `string` | — | Bắt buộc. Khoá định danh dòng, dùng cho `track` ([`../../RULES.md`](../../RULES.md) §7 F13) |
| `summary` | input | `SummaryRow \| null` | `null` | Dữ liệu dòng tổng **do máy chủ tính**. `null` = không bật công tắc `summary`. Gồm: nhãn (đã kèm phạm vi), và giá trị đã định dạng theo từng khoá cột |
| `summaryScope` | input | `'page' \| 'filtered'` | `'filtered'` | Phạm vi của số tổng. Mặc định là **toàn bộ kết quả sau lọc** — sai theo hướng an toàn: người đối chiếu sổ cần con số đó, còn tổng một trang hiếm khi là thứ ai muốn |
| `pageChange` | output | `{ page: number; pageSize: number }` | — | Đổi trang **và** đổi số dòng mỗi trang đều ra bằng sự kiện này. Đổi số dòng thì `page` đã được đưa về `1` bên trong lớp bọc — luật và lý do ở [`Pagination.md`](./Pagination.md) §API. Chỉ phát ở `variant = 'paged'` |
| `sortChange` | output | `{ sortBy: string; sortDescending: boolean }` | — | Phát đúng tên trên dây; dạng `1 \| -1` của PrimeNG không lọt ra ngoài |
| `selectionChange` | output | `ReadonlyArray<T>` | — | |
| `rowClick` | output | `T` | — | Không phát khi bấm trúng một control bên trong dòng |

**`state` là một biến, không phải bốn cờ.** Bốn cờ `loading`/`error`/`empty`/`emptyFiltered` cho phép biểu diễn những tổ hợp vô nghĩa (`loading` và `error` cùng `true`), và mỗi nơi gọi lại xử lý tổ hợp đó một kiểu. Một biến kiểu liệt kê thì trình biên dịch bắt được ca thiếu.

**Ba slot nội dung trạng thái.** Cùng khuôn `rowDetail`: mỗi slot là một input `TemplateRef` do màn cấp.

| Slot | Input | Hiện khi | Màn ghép gì |
| --- | --- | --- | --- |
| trống | `emptyTemplate` | `state` là `'empty'` hoặc `'empty-filtered'` | [`EmptyState.md`](./EmptyState.md) — biến thể theo ca, xem mục Trạng thái |
| đang tải | `loadingTemplate` | `state` là `'loading'` và chưa có dòng nào để giữ lại | [`SkeletonLoader.md`](./SkeletonLoader.md) dạng hàng |
| lỗi | `errorTemplate` | `state` là `'error'` | [`EmptyState.md`](./EmptyState.md) biến thể `error`, nút "Thử lại" gọi việc tải lại của màn |

`DataTable` quyết **khi nào** hiện slot nào và giữ tiêu đề cột quanh nó; màn quyết **hiện gì** trong slot. Tách như vậy vì `DataTable` nằm ở tầng bọc và không được import component tự dựng — luật ở [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.2. Nút trong slot gọi thẳng hàm của màn, nên `DataTable` không có output riêng cho "Thử lại"; khuôn chung cho mọi lớp bọc ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5. Cú pháp khai slot là việc thi công, mẫu ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md).

`DataTable` là component **dumb**: nó **phát** yêu cầu đổi trang và đổi sắp xếp; trang cha gọi API rồi truyền dữ liệu mới xuống ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

Trang, sắp xếp, từ khoá tìm và bộ lọc sống trên URL, do tầng trạng thái danh sách của màn giữ — hợp đồng ở [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.8. `DataTable` không đọc và không ghi URL.

## Do / Don't

- ✅ Phân trang **phía máy chủ**. Tải hết rồi phân trang ở trình duyệt chỉ chạy được cho tới ngày bảng có mười nghìn dòng.
- ✅ Luôn truyền `rowKey`. Không có nó, mọi lần dữ liệu đổi là dựng lại toàn bộ DOM.
- ✅ Tách hai ca `empty`.
- ✅ Giữ dữ liệu cũ khi đang đổi trang; đừng thay bằng skeleton.
- ✅ Nói rõ "chọn tất cả" là chọn trang hiện tại hay toàn bộ kết quả.
- ❌ Không import lưới dữ liệu của PrimeNG ở bất kỳ đâu khác.
- ❌ Không để lọt kiểu dữ liệu hay tên sự kiện của PrimeNG ra API công khai — làm thế là mất luôn cái lợi duy nhất của việc bọc.
- ❌ Không đè style bằng `::ng-deep`.
- ❌ Không ẩn cột ở màn nhỏ mà không nói thông tin đó đi đâu.
- ❌ Không đặt chiều cao cố định bên trong component.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Có cần cho người dùng ẩn/hiện cột và lưu lại lựa chọn đó không? | Sau F3 — dự án hạ nguồn đầu tiên có bảng nhiều cột |