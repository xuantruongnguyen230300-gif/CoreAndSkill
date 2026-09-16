---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Table

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** tự dựng, không bọc PrimeNG. Lý do ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 — bảng này **không** sắp xếp, **không** phân trang, **không** ảo hoá dòng, nên nó không thuộc nhóm hành vi khó. Nó là một `<table>` được tạo hình. Mọi thứ khó nằm ở [`DataTable.md`](./DataTable.md), và đó chính là lý do hai component này tách nhau.

---

## Mục đích

Trình bày một tập dữ liệu **đã có sẵn, đã đủ nhỏ để hiện hết** dưới dạng hàng và cột.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Danh sách ngắn, cố định: dòng chi tiết của một đơn, bảng cấu hình, bảng đối chiếu | 🛑 Cần phân trang, sắp xếp, chọn dòng, cột ghim → [`DataTable.md`](./DataTable.md). Đừng thêm một trong bốn thứ đó vào đây |
| Bảng đặt bên trong [`Card.md`](./Card.md) hoặc bên trong [`Dialog.md`](./Dialog.md) | 🛑 Dữ liệu tới từ máy chủ theo trang → [`DataTable.md`](./DataTable.md); `Table` nhận trọn một mảng và vẽ hết |
| Dữ liệu **thật sự dạng bảng**: có nhiều bản ghi, mỗi bản ghi cùng một bộ thuộc tính | 🛑 Bố cục hai cột của một form → dùng lưới và [`FormRow.md`](./FormRow.md). Dùng `<table>` để dàn trang là lỗi ngữ nghĩa mà trình đọc màn hình đọc thành một bảng dữ liệu vô nghĩa |
| Bảng chỉ đọc, có thể kèm một cột hành động | 🛑 Danh sách một cột → dùng `<ul>`; một bảng một cột không có quan hệ hàng–cột nào để mô tả |

**Ranh giới với `DataTable` là một biên cứng, không phải một gợi ý.** Cám dỗ luôn là "thêm nhanh một lần sắp xếp vào `Table` cho màn này". [`../COMPONENTS.md`](../COMPONENTS.md) §1 kể chuyện có thật ở dự án tiền nhiệm: một class từng gánh hai component khác hẳn nhau và phải tách ra sau khi đã lan khắp nơi. Sắp xếp kéo theo trạng thái cột đang sắp, kéo theo icon ở `th`, kéo theo `aria-sort` — và lúc đó `Table` đã là một `DataTable` viết dở.

## Biến thể

| Biến thể | Vai | Vạch hàng | Ghi chú |
| --- | --- | --- | --- |
| `default` | **Mặc định.** Bảng đứng trong `Card` hoặc trong `Dialog` | `--color-border-subtle` giữa các hàng | Không có khung riêng — khung là của `Card` |
| `bordered` | Bảng đứng một mình trên nền trang | `--color-border-subtle` giữa các hàng, khung ngoài `--color-border`, `--radius-lg` | Khung ngoài là ranh giới component, đúng vai của bậc `--color-border` |
| `zebra` | Bảng nhiều cột, mắt dễ trượt dòng khi đọc ngang | Thêm nền `--color-surface-2` cho hàng chẵn | Xem đánh đổi dưới |

**`zebra` và hover không sống chung được, phải chọn một.** Cả hai dùng `--color-surface-2` ([`../DESIGN.md`](../DESIGN.md) §2.1 khai đúng token đó cho cả "nền hover của hàng bảng" lẫn "vạch zebra"). Bật cả hai thì hàng đang rê chuột trên nền zebra không đổi gì cả — người dùng mất phản hồi ở đúng một nửa số hàng. Quyết định: khi `zebra` bật, hover chuyển sang `--color-surface-3`. Cái giá phải trả là nền hover đậm hơn bình thường và trông hơi nặng ở bảng nhiều hàng — chấp nhận được, vì mất phản hồi hoàn toàn thì tệ hơn.

🛑 **Vạch giữa các hàng dùng `--color-border-subtle`, không dùng `--color-border`.** [`../DESIGN.md`](../DESIGN.md) §2.3: `subtle` là vạch **bên trong** một bề mặt, `border` là ranh giới **của** một component. Dùng nhầm bậc biến hai mươi hàng thành hai mươi cái hộp.

## Kích thước

Cỡ ở đây là **mật độ hàng**, không phải chiều cao control.

| Cỡ | Đệm dọc ô | Đệm ngang ô | Cỡ chữ ô | Cỡ chữ `th` | Dùng khi |
| --- | --- | --- | --- | --- | --- |
| `sm` | `--sp-2` | `--sp-4` | `--fs-xs` | `--fs-2xs` | Bảng phụ trong `Dialog`, bảng đối chiếu nhiều hàng |
| `md` | `--sp-3` | `--sp-5` | `--fs-sm` | `--fs-xs` | **Mặc định.** Đệm dọc của ô bảng theo [`../DESIGN.md`](../DESIGN.md) §4 |
| `lg` | `--sp-5` | `--sp-6` | `--fs-sm` | `--fs-xs` | Bảng ít hàng, mỗi hàng có nội dung nhiều dòng |

`th`: nền `--color-surface-2`, chữ `--color-text`, `--fw-semibold`, căn trái. Ô số căn phải, và `th` của cột số cũng căn phải — một tiêu đề căn trái trên một cột số căn phải làm hỏng chính đường thẳng mà việc căn phải tạo ra. Chiều cao hàng **sinh ra** từ đệm dọc và `--lh-snug`, không có token riêng — bảng đọc không cần hai bảng cạnh nhau cao bằng nhau tới từng pixel.

**Công tắc `stickyHeader`** ghim `<thead>` ở đỉnh vùng cuộn khi thân bảng cuộn dọc, lớp `--z-sticky`. Nó chỉ có nghĩa khi bảng nằm trong một vùng cuộn dọc có chiều cao do **trang** quyết — cùng luật với [`DataTable.md`](./DataTable.md) §Kích thước; `Table` không tự đặt chiều cao. Dùng khi bảng không phân trang mà nhiều hàng tới mức tiêu đề cột trôi khỏi màn — ma trận nhiều hàng là ca điển hình.

Ghim tiêu đề **không** kéo `Table` sang phía `DataTable`: nó không mang trạng thái nào (không cột đang sắp, không `aria-sort`), chỉ là vị trí dính của một hàng có sẵn. Cái giá: vùng cuộn dọc là một điểm dừng `Tab` nữa, cùng luật vùng cuộn ở mục Responsive.

## Gom dòng theo nhóm

`groupBy` khác `null` thì bảng vẽ **một hàng tiêu đề nhóm** trước mỗi nhóm, và chữ trên hàng đó là **giá trị của chính field được gom**. Hàng tiêu đề nhóm không phải một bản ghi: nó không mang dữ liệu cột nào, không bấm được, không chọn được.

| Khoản | Giá trị |
| --- | --- |
| Nền hàng tiêu đề nhóm | `--color-surface-2` |
| Chữ | `--fw-semibold`, `--color-text`, cùng cỡ chữ `th` của cỡ bảng đang dùng |
| Đệm | Đệm dọc và đệm ngang của ô theo cỡ bảng, không có bậc riêng |
| Vạch trên | `--color-border-subtle`, trừ nhóm đầu tiên |

Bốn luật đi kèm, vì mỗi luật giải một chỗ va với thứ `Table` đã có:

1. 🛑 **Gom nhóm không sắp xếp lại dữ liệu.** Bảng vẽ `rows` đúng thứ tự nhận vào và mở một nhóm mới mỗi khi giá trị của field đổi so với dòng liền trước. Trang cha có trách nhiệm truyền các dòng cùng nhóm nằm liền nhau. Sắp xếp lại ở đây là mở đúng cánh cửa mà mục *Khi nào KHÔNG dùng* đã đóng — sắp xếp thuộc [`DataTable.md`](./DataTable.md). Dòng có giá trị rỗng hoặc `null` ở field gom là một nhóm như mọi nhóm: vị trí do trang truyền, hàng tiêu đề hiện **nhãn tường minh** qua `emptyGroupLabel` (§API; trang truyền bản dịch của khoá `chung.nhom.trong` — "(Trống)"), **không** phải dấu gạch: `<th scope="rowgroup">` là tên nhóm mà trình đọc màn hình gắn cho mọi hàng phía sau, một dấu gạch đọc lên không nói được gì.
2. **Hàng tiêu đề nhóm không tính vào nhịp `zebra`.** Nhịp hàng chẵn/lẻ đếm lại từ đầu **trong từng nhóm**; nếu không, một nhóm lẻ dòng sẽ lật màu của cả phần bảng phía sau nó và vạch zebra mất tác dụng dẫn mắt.
3. **`stickyHeader` chỉ ghim `<thead>`.** Hàng tiêu đề nhóm cuộn đi cùng thân bảng. Ghim thêm nó là ghim hai hàng chồng nhau và phải tính bù chiều cao lúc chạy — một cơ chế thuộc về `DataTable`, không thuộc về đây.
4. **Nhóm không thu gọn được.** Không có nút mở/đóng nhóm: thu gọn là một trạng thái mà một bảng tĩnh không giữ.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Nền `--color-surface`; `border-collapse: collapse`; vạch hàng `--color-border-subtle`; `th` nền `--color-surface-2` | Có |
| `hover` | Hàng đổi nền `--color-surface-2` (hoặc `--color-surface-3` khi `zebra` bật). Chỉ hàng trong `<tbody>`, không áp cho `<thead>`. `--dur-fast` với `--ease-standard`. **Bọc trong `@media (hover: hover)`** — không có nó, màn cảm ứng giữ nguyên trạng thái hover sau khi chạm và người dùng tưởng hàng đó đang được chọn. Hàng tiêu đề nhóm **không** đổi nền khi rê chuột: đổi nền là lời hứa bấm được mà nó không giữ | Có |
| `focus-visible` | **Chỉ áp cho phần tử tương tác bên trong ô** (nút hành động, liên kết) và cho **vùng cuộn ngang**. Bản thân hàng không nhận focus — xem mục Accessibility | Có |
| `active` | **Không áp dụng cho hàng.** Hàng không bấm được ở component này. Bấm vào hàng để mở chi tiết là hành vi của [`DataTable.md`](./DataTable.md); ở đây, lối vào chi tiết là một liên kết trong ô hoặc một [`IconButton.md`](./IconButton.md) ở cột hành động | — |
| `disabled` | **Không áp dụng cho bảng.** Bảng không phải control. Một hàng ứng với bản ghi không thao tác được thì các nút trong cột hành động của nó mang `disabled`, còn dữ liệu vẫn đọc được bình thường | — |
| `loading` | Giữ nguyên `<thead>`; `<tbody>` thay bằng vài hàng [`SkeletonLoader.md`](./SkeletonLoader.md) đúng số cột; `<table>` mang `aria-busy="true"`. Khi `groupBy` khác `null`, khung chờ **không** vẽ hàng tiêu đề nhóm nào — chưa có dữ liệu thì chưa biết có nhóm nào, và vẽ sẵn một hàng nhóm trống là bịa cấu trúc. Ca tải lại trên dữ liệu cũ thì phủ `--color-scrim` lên `<tbody>` và giữ chữ cũ đọc được | Có |
| `error` | Một hàng chiếm hết cột (`colspan`) chứa [`NoticeBanner.md`](./NoticeBanner.md) vai `danger`, kèm nút thử lại icon `pi-refresh`. `<thead>` giữ nguyên để người dùng còn biết bảng này lẽ ra chứa gì | Có |
| `empty` | Một hàng chiếm hết cột chứa [`EmptyState.md`](./EmptyState.md) thu nhỏ: icon `pi-inbox`, một câu, tối đa một hành động; đệm dọc `--sp-10`. `<thead>` giữ nguyên, và không hàng tiêu đề nhóm nào được vẽ | Có |

**Vì sao `empty` là một hàng bên trong bảng chứ không phải thay cả bảng:** thay cả bảng bằng một khối trống làm mất `<thead>`, và cùng với nó mất luôn câu trả lời cho "bảng này lẽ ra hiện cái gì". Người dùng lọc ra không kết quả cần thấy các cột để hiểu mình vừa lọc theo cái gì. Cái giá: `EmptyState` bị nhốt trong bề rộng bảng và phải là bản thu nhỏ — một cột icon lớn với hai đoạn văn không vừa ở đó.

**Vì sao `<thead>` sống sót qua cả ba trạng thái `loading`, `error`, `empty`:** ba lần giữ nguyên cùng một thứ không phải trùng lặp, nó là một quyết định duy nhất — **cấu trúc bảng là thông tin, không phải là dữ liệu**. Nó đúng ngay cả khi không có dòng nào.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-surface-2`, `--color-surface-3`, `--color-text`, `--color-text-muted`, `--color-border`, `--color-border-subtle`, `--color-focus`, `--color-scrim` |
| Chữ | `--fs-2xs`, `--fs-xs`, `--fs-sm`, `--fw-regular`, `--fw-semibold`, `--lh-snug`, `--lh-normal` |
| Khoảng cách | `--sp-2`, `--sp-3`, `--sp-4`, `--sp-5`, `--sp-6`, `--sp-10` |
| Hình dạng | `--radius-lg`, `--border-w`, `--border-w-strong` |
| Lớp | `--z-sticky` — chỉ khi bật `stickyHeader` |
| Chuyển động | `--dur-fast`, `--ease-standard` |

Nội dung ô đổi sau khi lọc thì **đổi ngay, không transition** — [`../DESIGN.md`](../DESIGN.md) §7 nói rõ: hàng nhấp nháy làm mắt mất chỗ đang đọc.

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-lg` | Bảng chiếm hết bề rộng vùng chứa; các cột chia theo nội dung |
| `$bp-md` … `$bp-lg` | Cột phụ (ngày tạo, người tạo) ẩn trước; cột định danh và cột hành động luôn giữ |
| < `$bp-md` | Vùng cuộn ngang bật; **không ghim cột nào**; mật độ hạ xuống `sm` |
| < `$bp-xs` | `cardLayout` là `true` (mặc định): chuyển sang dạng thẻ — mỗi bản ghi là một khối, nhãn cột thành nhãn trường bên trong; khi `groupBy` khác `null`, hàng tiêu đề nhóm thành một dải nhãn đứng trước cụm thẻ của nhóm đó. `cardLayout` là `false`: **giữ dạng lưới**, vùng cuộn ngang và mật độ `sm` như ngưỡng `< $bp-md` — dùng khi quan hệ hàng–cột **chính là** nội dung (ma trận tick), vì dạng thẻ xoá đúng quan hệ đó |

🛑 **`Table` KHÔNG có cột ghim, ở mọi ngưỡng.** Mục *Khi nào KHÔNG dùng* đã xếp cột ghim vào [`DataTable.md`](./DataTable.md), và `ColumnDef` không có khoá `frozen` — chữ ký ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9. Ghim một cột không phải là một dòng `position: sticky`: nó kéo theo lớp xếp chồng, kéo theo phép đo bù bề rộng cột lúc chạy, và kéo theo giao điểm với dải tổng — đúng ba thứ [`DataTable.md`](./DataTable.md) đã giải. Bảng nào cần ghim cột đầu khi cuộn ngang thì bảng đó cần `DataTable`, kể cả khi nó không phân trang.

Cái giá phải chấp nhận, nói thẳng: ở màn nhỏ, cuộn ngang một `Table` nhiều cột thì cột định danh trôi khỏi màn và người đọc mất mốc hàng. Đó là lý do ngưỡng `< $bp-xs` mặc định chuyển hẳn sang dạng thẻ thay vì cuộn tiếp. Bảng nào tắt `cardLayout` là nhận lại đúng cái giá đó ở khổ nhỏ nhất, có chủ ý — và phải giữ `<th scope="row">` cùng nhãn đọc lên của từng ô cho đủ, vì đó là thứ duy nhất còn giữ mốc hàng cho người dùng bàn phím và trình đọc màn hình.

**`colspan` của hàng tiêu đề nhóm bám theo số cột đang hiện**, không phải số cột khai trong `columns`. Cột phụ bị ẩn ở `$bp-md` … `$bp-lg` thì `colspan` giảm theo — một con số cứng để lại một ô thừa ở mép phải và làm lệch lưới của cả bảng.

🛑 **Vùng cuộn ngang phải focus được bằng bàn phím.** Phần tử bao mang `tabindex="0"`, `role="region"` và một nhãn (`aria-label`, hoặc `aria-labelledby` trỏ vào `<caption>`). Thiếu `tabindex="0"`, người dùng bàn phím **không cuộn ngang được** — họ không thể đưa con trỏ vào một vùng không nhận focus, nên các cột bên phải đơn giản là không tồn tại với họ. Đây là bẫy accessibility hay gặp bậc nhất ở bảng, và nó không lộ ra trong bất kỳ lần kiểm bằng chuột nào.

Mặt trái phải chấp nhận: vùng cuộn trở thành một điểm dừng `Tab`, kể cả khi bảng không thật sự tràn. `tabindex="0"` **đặt luôn**, không đo lúc chạy — cùng cách với [`DataTable.md`](./DataTable.md) §Accessibility; một điểm dừng thừa rẻ hơn một phép đo bố cục chạy lại ở mỗi lần đổi cỡ cửa sổ.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | `<table>` thật, với `<thead>` và `<tbody>`. 🛑 Không dựng bảng bằng `<div role="table">` — mất hết điều hướng theo ô của trình đọc màn hình |
| `<caption>` | 🛑 **Bắt buộc.** Nó là tên của bảng và là thứ trình đọc màn hình đọc khi vào bảng. Không muốn hiện trên màn thì dùng lớp chỉ-đọc-lên, **không** dùng `display: none` — thứ bị `display: none` biến mất khỏi cả cây accessibility |
| Tiêu đề cột | `<th scope="col">` cho mọi cột. Thiếu `scope`, trình đọc màn hình không ghép được ô với tiêu đề của nó, và người dùng nghe một chuỗi giá trị không có tên |
| Tiêu đề hàng | Cột định danh dùng `<th scope="row">` khi mỗi hàng có một tên riêng. Đây là thứ cho phép nghe "Nguyễn Văn A, Trạng thái, Hoạt động" thay vì "Hoạt động" |
| Hàng tiêu đề nhóm | Mỗi nhóm là một `<tbody>` riêng, và hàng tiêu đề nhóm là một `<tr>` chứa **đúng một** `<th scope="rowgroup">` mang `colspan`. `scope="rowgroup"` là thứ cho trình đọc màn hình ghép mọi hàng phía sau với tên nhóm — thiếu nó, người dùng nghe một chuỗi bản ghi mà không biết chúng thuộc nhóm nào. Hàng này **không** chọn được: không ô đánh dấu, không nhận focus, không chứa phần tử tương tác nào |
| Vùng cuộn | `tabindex="0"` + `role="region"` + nhãn, như mục Responsive |
| Dạng thẻ | Là hành vi responsive của chính `Table` (`cardLayout`), không phải component riêng. DOM **vẫn là `<table>`**; vì `display` khác `table-*` làm vài trình duyệt bỏ ngữ nghĩa bảng, khai tường minh `role="table"` / `"row"` / `"columnheader"` / `"cell"`, và nhãn cột đặt vào từng ô qua thuộc tính dữ liệu để vẽ bằng CSS — mọi luật ở bảng này vẫn áp |
| Bảng trống | Hàng `colspan` phải nằm trong `<tbody>` và có một ô `<td>` thật. Một `<tbody>` rỗng khiến vài trình đọc màn hình thông báo số hàng sai |
| Hàng hành động | Mỗi [`IconButton.md`](./IconButton.md) trong hàng cần nhãn phân biệt được giữa các hàng — ghép định danh hàng, hoặc `aria-describedby` trỏ vào ô `<th scope="row">` |
| Cập nhật động | Vùng chứa bảng mang `aria-live="polite"` khi số hàng đổi do lọc, và thông báo là **số kết quả**, không phải toàn bộ nội dung |
| Ô trống | Ô không có giá trị vẫn phải là một `<td>` có nội dung — một dấu gạch, hoặc chữ chỉ-đọc-lên. `<td></td>` rỗng bị đọc lướt qua và người dùng không biết mình vừa bỏ sót một cột |
| Chữ | Tiêu đề cột và `<caption>` đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `columns` | input | `ReadonlyArray<ColumnDef<T>>` | — | **Bắt buộc.** Chữ ký đầy đủ ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9 — `Table` dùng thẳng kiểu gốc, không mở rộng |
| `rows` | input | `readonly T[]` | `[]` | Toàn bộ dữ liệu. Component vẽ hết — không có phân trang |
| `caption` | input | `string` | — | **Bắt buộc.** Không có mặc định, và không suy ra từ tiêu đề `Card` bao ngoài |
| `captionVisible` | input | `boolean` | `false` | `false` = chỉ trình đọc màn hình nghe được, vẫn nằm trong cây accessibility |
| `rowKeyField` | input | `string` | — | **Bắt buộc.** Khoá định danh hàng, để khung nhìn không vẽ lại toàn bộ khi một hàng đổi |
| `groupBy` | input | `string \| null` | `null` | Tên field dùng để gom dòng. Khác `null` thì bảng vẽ **một hàng tiêu đề nhóm** trước mỗi nhóm, chữ trên hàng là giá trị của chính field đó. Bảng **không** sắp xếp lại `rows` — trang cha truyền các dòng cùng nhóm nằm liền nhau. Luật đầy đủ ở §Gom dòng theo nhóm |
| `emptyGroupLabel` | input | `string \| null` | `null` | Nhãn hàng tiêu đề của nhóm có giá trị gom rỗng hoặc `null` — chuỗi **đã dịch**, `Table` là component dumb. Bắt buộc khác `null` khi `groupBy` khác `null` |
| `variant` | input | `'default' \| 'bordered' \| 'zebra'` | `'default'` | |
| `size` | input | `'sm' \| 'md' \| 'lg'` | `'md'` | Mật độ hàng |
| `stickyHeader` | input | `boolean` | `false` | Ghim `<thead>` ở đỉnh vùng cuộn dọc. Trang phải cho bảng một vùng cuộn có chiều cao — không có thì `th` không có gì để dính vào |
| `cardLayout` | input | `boolean` | `true` | `true`: dưới `$bp-xs` chuyển sang dạng thẻ. `false`: dưới `$bp-xs` giữ dạng lưới và cuộn ngang — §Responsive. Chỉ tắt khi dạng thẻ phá mất nội dung, không tắt vì tiện |
| `loading` | input | `boolean` | `false` | |
| `errorMessage` | input | `string \| null` | `null` | Có giá trị thì vẽ hàng lỗi thay cho `<tbody>` |
| `retry` | output | `void` | — | Phát khi bấm nút thử lại ở hàng lỗi |

Nội dung ô do slot theo cột quyết định, không do một input chuỗi — ô hay chứa [`Badge.md`](./Badge.md), [`Avatar.md`](./Avatar.md) hoặc một liên kết. Nội dung của `empty` cũng vào qua slot, để mỗi màn tự viết câu phù hợp thay vì dùng chung một câu vô nghĩa.

`Table` là component **dumb** — nhận `rows` qua `input()`, phát `retry` qua `output()`, không tự gọi API ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

## Do / Don't

- ✅ Luôn có `<caption>`, kể cả khi ẩn trên màn.
- ✅ Luôn có `scope` trên mọi `<th>`.
- ✅ Cho vùng cuộn ngang `tabindex="0"` và một nhãn.
- ✅ Giữ `<thead>` ở cả `loading`, `error` và `empty`.
- ✅ Dùng `--color-border-subtle` cho vạch hàng.
- ✅ Khi bật `groupBy`, truyền `rows` đã xếp sẵn để các dòng cùng nhóm nằm liền nhau.
- ❌ Không thêm sắp xếp, phân trang hay cột ghim vào đây — cả ba là `DataTable`.
- ❌ Không dùng `groupBy` để thay việc sắp xếp: nó gom dòng liền kề, nó không xếp lại thứ tự.
- ❌ Không dùng `<table>` để dàn trang.
- ❌ Không ẩn `<caption>` bằng `display: none`.
- ❌ Không bật `zebra` cùng nền hover mặc định.
- ❌ Không để một `<td>` rỗng trơn.

## Cần chốt

Không còn.
