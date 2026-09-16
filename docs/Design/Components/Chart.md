---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Chart

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** **bọc PrimeNG.** Biến thể `line` dùng component biểu đồ của PrimeNG, vì nó cần thang đo, nội suy và lớp tương tác theo con trỏ, đúng nhóm "khó" ở [`../COMPONENTS.md`](../COMPONENTS.md) §4. **Bốn biến thể còn lại vẽ bằng HTML/CSS bên trong cùng lớp bọc**, vì chúng chỉ là những khối có kích thước tính theo phần trăm — lý do ở mục Do / Don't. Vì cả component là một lớp bọc, nó nằm ở tầng bọc theo quy tắc ánh xạ cột "Nền" ở [`../../wiki-core/fe/05-component-library.md`](../../wiki-core/fe/05-component-library.md).

Cái giá của `line`, nói thẳng: PrimeNG vẽ biểu đồ bằng canvas, nên dạng này **không để lại gì cho trình đọc màn hình** và không tự kế thừa token CSS — màu phải nạp bằng script. Bù bằng bảng số liệu tương đương bắt buộc ở mục dưới.

Tỉ lệ bốn trên năm này là điều làm [`../../adr/0019-ba-component-nang-thuoc-core.md`](../../adr/0019-ba-component-nang-thuoc-core.md) ràng buộc 1 khả thi: thư viện chỉ nạp khi màn đầu tiên dùng `line`, nên dự án không vẽ đường **không tải một byte nào** của nó.

> 📖 Giá trị màu: [`../DESIGN.md`](../DESIGN.md) §2.8. File này khai **cách vẽ**; file kia khai **màu gì**.

---

## Mục đích

Vẽ một tập số liệu thành hình để người đọc so sánh được nhanh hơn so với đọc bảng.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| So sánh nhiều giá trị cùng đơn vị: chi theo tháng, chi theo khoản mục | 🛑 Câu trả lời là **một con số** → một ô số liệu, không phải biểu đồ. Vẽ một cột đơn độc là trang trí |
| Đối chiếu một giá trị với một mốc: thực hiện so với dự toán | 🛑 Người đọc cần **giá trị chính xác** để đối chiếu sổ sách → [`Table.md`](./Table.md). Biểu đồ trả lời "nhiều hay ít", bảng trả lời "bao nhiêu" |
| Cho thấy một xu hướng theo thời gian | 🛑 Tiến trình một việc đang chạy → [`ProgressBar.md`](./ProgressBar.md) |
| Cho thấy cơ cấu của một tổng | 🛑 Dữ liệu chỉ có hai ba dòng → bảng đọc nhanh hơn |

🛑 **Mỗi biểu đồ có một bảng số liệu tương đương.** `Chart` tự vẽ một `<table>` từ chính dữ liệu đang vẽ. Từ `$bp-xs` trở lên bảng **ẩn trực quan**: trình đọc màn hình đọc được, mắt không thấy. Dưới `$bp-xs` bảng **hiện thay cho vùng vẽ** — mục Responsive. Bảng này không phải một định nghĩa bảng thứ hai theo [`../COMPONENTS.md`](../COMPONENTS.md) §1: khi hiện, nó mượn hình thức của [`Table.md`](./Table.md) chứ không khai hình thức riêng. Ở các ngưỡng còn vẽ biểu đồ, bảng ẩn trực quan nên **không** là kênh bù thị giác cho ba màu dưới ngưỡng tương phản ở theme sáng ([`../DESIGN.md`](../DESIGN.md) §2.8 ràng buộc 2); kênh đó ở mục Accessibility.

## Biến thể

**Năm** biến thể, loại trừ nhau — mỗi cái trả lời một câu hỏi khác nhau về dữ liệu:

| Biến thể | Câu hỏi nó trả lời | Vẽ bằng |
| --- | --- | --- |
| `column` | So sánh nhiều giá trị rời rạc | HTML/CSS |
| `line` | Xu hướng liên tục, nhiều chuỗi cùng đơn vị | component biểu đồ của PrimeNG |
| `donut` | Cơ cấu của một tổng, **tối đa ba bốn phần** | HTML/CSS |
| `diverging` | Chênh lệch hai chiều quanh một mốc | HTML/CSS |
| `heatmap` | Mức độ theo hai chiều | HTML/CSS |

Một **công tắc**, chỉ áp cho `column`:

| Công tắc | Hiệu ứng | Dùng khi |
| --- | --- | --- |
| `orientation` | `vertical` (mặc định) hoặc `horizontal` | `horizontal` khi nhãn dài, hoặc khi so sánh là **xếp hạng** chứ không phải diễn biến theo thời gian |

**Nhãn tiếng Việt dài thì bật `horizontal`.** Xoay nhãn 45° dưới một biểu đồ cột là cách nhanh nhất làm một biểu đồ không đọc được — và tên khoản mục trong phần mềm quản lý gần như luôn dài. Cột dọc và thanh ngang là **cùng một biểu đồ ở hai hướng**, không phải hai biến thể: cùng thang, cùng quy cách mark, cùng luật nhãn. Tách đôi chúng là đẩy `Chart` vượt ngưỡng năm biến thể mà [`../COMPONENTS.md`](../COMPONENTS.md) §2.3 cảnh báo, đổi lại không được gì.

🛑 **Không có biến thể `meter`.** Một giá trị so với một mốc là [`ProgressBar.md`](./ProgressBar.md) biến thể `meter` — component đó đã sở hữu hình dạng này, đã khai ba mức màu và ngưỡng cảnh báo. Khai lại ở đây là tạo nguồn thứ hai cho cùng một hình, và hai nguồn sẽ lệch ([`../../../.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §5). Màn hình cần "thực hiện so với dự toán" thì dùng `ProgressBar`, không dùng `Chart`.

🛑 **`donut` quá bốn phần thì đổi sang `column` bật `horizontal`.** Bánh tròn bảy phần là bảy góc không ai so được. Phần nhỏ gom thành "Khác", đừng chia nhỏ thêm.

🛑 **Không có biến thể hai trục tung.** Xem Do / Don't — đây là luật quan trọng nhất của cả file.

## Kích thước

`Chart` **không** dùng thang `--size-control-*`.

| Cỡ | Chiều cao vùng vẽ | Dùng khi |
| --- | --- | --- |
| `sm` | `--chart-h-sm` | Trong một ô số liệu, trong [`Drawer.md`](./Drawer.md) |
| `md` | `--chart-h-md` | **Mặc định.** Trong [`Card.md`](./Card.md) |
| `lg` | `--chart-h-lg` | Biểu đồ là nội dung chính của cả vùng |

Ba chiều cao là token tầng 3 của component này; số thật khai ở [`../DESIGN.md`](../DESIGN.md) §6.5. Bề rộng luôn theo vùng chứa. Biểu đồ **không** tự đặt `width`.

Quy cách mark — cố định cho mọi biến thể:

| Mark | Quy cách |
| --- | --- |
| Cột, thanh | Dày **tối đa 24px**; đầu phía dữ liệu bo `--radius-xs`, chân vuông dính đường gốc |
| Đường | Dày 2px, đầu và khớp nối bo tròn |
| Điểm đánh dấu | Đường kính tối thiểu 8px, viền 2px màu bề mặt để không lẫn khi hai điểm chồng nhau |
| Vùng tô dưới đường | Chính màu chuỗi ở độ mờ ~10% — một lớp mỏng, không phải một mảng đặc |
| Lưới, trục | Nét mảnh 1px **liền**, màu `--color-border-subtle`. Không bao giờ đứt nét |
| Khe giữa hai mark chạm nhau | **2px màu bề mặt**, đều nhau trên toàn biểu đồ |

Khe 2px màu nền là thứ tách hai đoạn chồng nhau, **không phải một đường viền**. Viền là mực không mang dữ liệu.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Vùng vẽ trên nền `--color-surface`. Lưới lùi về sau, dữ liệu là thứ duy nhất được phép nổi bật | Có |
| `hover` | Mark dưới con trỏ hiện hộp giá trị; dạng `line` hiện thêm một đường kẻ dọc cắt qua mọi chuỗi tại điểm đó. Vùng bắt sự kiện **rộng hơn mark** — một điểm 8px không bắt được con trỏ. **Bọc trong `@media (hover: hover)`** | Có |
| `focus-visible` | Biểu đồ là **một** điểm dừng Tab; vào rồi thì `←` `→` đi giữa các điểm, hiện đúng hộp giá trị như khi rê chuột. `outline` `--color-focus` quanh vùng vẽ | Có |
| `active` | Chỉ khi mark bấm được để lọc: mark đang bị nhấn đậm thêm một bậc | Có |
| `disabled` | Cả biểu đồ chỉ đọc: bỏ lớp tương tác, giữ nguyên hình. **Không** giảm opacity — dữ liệu vẫn phải đọc được | Có |
| `loading` | Vùng vẽ hiện **slot đang tải** (`loadingTemplate`); màn ghép [`SkeletonLoader.md`](./SkeletonLoader.md) đúng hình dạng biểu đồ sắp tới (vài cột xám cho `column`, một dải cho `line`). `aria-busy="true"`. 🛑 **Không vẽ biểu đồ rỗng rồi cho dữ liệu nhảy vào** — người dùng đọc mất một khung hình sai | Có |
| `error` | Vùng vẽ hiện **slot lỗi** (`errorTemplate`); màn ghép [`EmptyState.md`](./EmptyState.md) biến thể `error` kèm nút "Thử lại". **Giữ nguyên tiêu đề và chú giải** — chúng cho biết đây là biểu đồ gì | Có |
| `empty` | **Hai ca phải phân biệt**, và vùng vẽ hiện **slot trống** (`emptyTemplate`) cho cả hai; màn chọn nội dung. *Chưa có dữ liệu kỳ này:* câu nói rõ là chưa phát sinh. *Bộ lọc không ra kết quả:* câu khác kèm nút xoá lọc. Trộn hai ca là lỗi hay gặp nhất ở màn báo cáo. Tiêu đề và chú giải giữ nguyên | Có |

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu chuỗi | `--chart-1` … `--chart-8` |
| Màu mức độ | `--chart-seq-1` … `--chart-seq-5` |
| Màu hai chiều | `--chart-div-neg`, `--chart-div-mid`, `--chart-div-pos` |
| Màu trạng thái | `--color-success`, `--color-warning`, `--color-danger`, `--color-info` — **chỉ** khi chuỗi là trạng thái |
| Bề mặt, chữ | `--color-surface`, `--color-surface-2`, `--color-surface-3`, `--color-text`, `--color-text-muted`, `--color-border-subtle`, `--color-inverse-surface`, `--color-inverse-text`, `--color-focus` |
| Chữ | `--fs-2xs`, `--fs-xs`, `--fs-sm`, `--fs-3xl`, `--fw-medium`, `--fw-semibold` |
| Khoảng cách | `--sp-2`, `--sp-3`, `--sp-4`, `--sp-5`, `--sp-6` |
| Hình dạng | `--radius-xs`, `--radius-sm`, `--radius-pill`, `--border-w` |
| Kích thước | `--chart-h-sm`, `--chart-h-md`, `--chart-h-lg` |
| Bóng, lớp | `--shadow-2`, `--z-popover` — cho hộp giá trị |

🛑 **Chữ không bao giờ mặc màu dữ liệu.** Nhãn, số, chú giải, chữ trên trục đều dùng token màu chữ. Vàng và ngọc đọc không ra khi làm màu chữ trên nền sáng. Danh tính đến từ **chấm màu bên cạnh** chữ, không từ việc tô màu chính chữ đó. Ngoại lệ duy nhất: nhãn đặt **bên trong** một mảng màu đặc, lúc đó chọn trắng hoặc mực theo độ sáng của mảng.

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-lg` | Đầy đủ: tiêu đề, chú giải, nhãn trục, nhãn trực tiếp trên mark |
| `$bp-md` … `$bp-lg` | Bỏ nhãn trực tiếp, giữ chú giải và nhãn trục. `heatmap` bắt đầu cuộn ngang |
| < `$bp-md` | Nhãn trục thưa đi — chỉ hiện mốc đầu, giữa, cuối. Chú giải xuống dòng dưới vùng vẽ |
| < `$bp-xs` | **Mọi biến thể: không vẽ biểu đồ; bảng tương đương thoát trạng thái ẩn trực quan và hiện thay cho vùng vẽ.** Tiêu đề giữ nguyên; chú giải không vẽ — tên cột của bảng đã làm việc đó. Mười hai cột trên một màn điện thoại là mười hai vạch không so được gì, còn bảng thì đọc được số chính xác. Hình thức bảng khi hiện: theo [`Table.md`](./Table.md) §Kích thước cỡ `sm`, biến thể `default` — file này không khai lại token nào |

**Cái giá của việc hiện bảng, nói thẳng:** `Chart` ở tầng bọc nên **không import** `Table` ([`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.2). Phần SCSS cho `<table>` này là một bản áp lại các token mà [`Table.md`](./Table.md) đã khai — đổi hình thức `Table` cỡ `sm` thì phải đổi cả đây. Chấp nhận, vì hai hướng kia tệ hơn: tự bật `horizontal` và cuộn ngang vẫn là một biểu đồ không đọc được số; bắt màn tự đặt `Table` thay biểu đồ ở khổ này là bắt mọi màn có biểu đồ phải nhớ.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Vai trò | Vùng vẽ mang `role="img"` với `aria-label` **tóm tắt kết luận**, không mô tả hình: "Thực hiện luỹ kế 2.152 triệu, thấp hơn dự toán 2.625 triệu tại tháng 9" |
| Bảng tương đương | Bắt buộc, luôn có. `<table>` dựng từ `series` và `categories`. Từ `$bp-xs` trở lên ẩn trực quan bằng kỹ thuật **vẫn giữ trong cây truy cập** — 🛑 không `display: none`, không `aria-hidden`; dưới `$bp-xs` hiện thay vùng vẽ (mục Responsive). `<caption>` là `title`; `<th scope>` cho cả hàng lẫn cột; số theo `valueFormat`; `null` đọc thành "không có dữ liệu", khác `0`. Không được chỉ tồn tại trong hộp giá trị khi rê chuột |
| Bàn phím | Biểu đồ là một điểm dừng Tab; `←` `→` đi giữa các điểm; `Escape` thoát khỏi chế độ đọc điểm |
| Chú giải | Từ **hai chuỗi trở lên luôn có chú giải**; một chuỗi thì không — tiêu đề đã nói đang vẽ gì, và một ô chú giải cho một chuỗi chỉ lặp lại tiêu đề |
| Màu | Không bao giờ là kênh duy nhất. Chuỗi phải phân biệt được bằng chú giải, nhãn trực tiếp, hoặc thứ tự trong bảng |
| Ba màu dưới 3:1 ở theme sáng | Biểu đồ dùng ngọc, vàng hoặc hồng **bắt buộc** kèm nhãn số hiện rõ, hoặc màn đặt [`Table.md`](./Table.md) xem được ngay cạnh biểu đồ. Ở các ngưỡng còn vẽ biểu đồ, bảng tương đương của `Chart` ẩn trực quan nên không tính vào kênh này. Ràng buộc, không phải khuyến nghị |
| Chuyển động | Hoạt ảnh vào khi vẽ lần đầu tối đa `--dur-slow`, và **tắt hẳn** khi `prefers-reduced-motion: reduce`. Không bao giờ hoạt ảnh khi dữ liệu đổi do lọc — mark nhảy làm mắt mất chỗ đang đọc |
| Chữ | Đi qua tầng i18n; số theo định dạng Việt Nam, phân cách nghìn bằng dấu chấm — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `variant` | input | `'column' \| 'line' \| 'donut' \| 'diverging' \| 'heatmap'` | `'column'` | |
| `orientation` | input | `'vertical' \| 'horizontal'` | `'vertical'` | Chỉ có nghĩa với `column`. Bật `horizontal` khi nhãn dài |
| `series` | input | `ReadonlyArray<ChartSeries>` | `[]` | Chữ ký ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9. `null` trong `points` nghĩa là **không có dữ liệu**, khác `0`. Màu **do component gán theo thứ tự**, không nhận từ ngoài — xem dưới |
| `categories` | input | `string[]` | `[]` | Nhãn trục hoành |
| `size` | input | `'sm' \| 'md' \| 'lg'` | `'md'` | |
| `title` | input | `string` | — | **Bắt buộc** |
| `summary` | input | `string` | — | **Bắt buộc.** Nội dung `aria-label`; phải là một kết luận, không phải mô tả hình |
| `valueFormat` | input | `'number' \| 'currency' \| 'percent'` | `'number'` | |
| `statusSeries` | input | `boolean` | `false` | `true` thì dùng bảng màu trạng thái thay bảng màu chuỗi |
| `loading` | input | `boolean` | `false` | |
| `loadingTemplate` | input | `TemplateRef<unknown> \| null` | `null` | Nội dung slot đang tải |
| `emptyTemplate` | input | `TemplateRef<unknown> \| null` | `null` | Nội dung slot trống. Màn biết ca rỗng nào đang xảy ra, nên context không mang ca |
| `errorTemplate` | input | `TemplateRef<unknown> \| null` | `null` | Nội dung slot lỗi, gồm nút "Thử lại". Màn chỉ truyền khi đang lỗi; khác `null` thì thắng hai slot kia |
| `pointSelected` | output | `{series, index}` | — | Chỉ phát khi màn hình khai biểu đồ bấm được để lọc |

🛑 **`series` không nhận màu từ ngoài.** Màu gán theo **thứ tự slot** trong [`../DESIGN.md`](../DESIGN.md) §2.8, và gán theo **đối tượng chứ không theo thứ hạng** — lọc bỏ một chuỗi thì các chuỗi còn lại **không đổi màu**. Cho phép truyền màu vào là mở đường cho mỗi màn một bảng màu, và đó là lúc hệ màu chết.

`Chart` là component **dumb** ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

**Ba slot nội dung trạng thái**, theo khuôn ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5: `loadingTemplate` hiện khi `loading` là `true`, `emptyTemplate` hiện ở trạng thái `empty`, `errorTemplate` hiện khi màn truyền nó khác `null`. `Chart` quyết khi nào hiện slot nào và giữ tiêu đề, chú giải quanh nó; màn quyết hiện gì trong slot. Lý do: `Chart` nằm ở tầng bọc nên không được import `SkeletonLoader`, `EmptyState` hay `Button` — luật ở [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.2.

## Do / Don't

- ✅ Gán màu theo đối tượng, không theo thứ hạng. Phòng Kế toán luôn một màu, dù đứng thứ nhất hay thứ năm.
- ✅ Gắn nhãn số **có chọn lọc**: điểm cuối, cực trị, hoặc chuỗi đang được nói tới. Số trên mọi điểm là nhiễu và không ai đọc.
- ✅ Dừng đường ở chỗ dữ liệu dừng. Kéo tới hết trục bằng 0 sẽ vẽ ra một cú sụt không có thật.
- ✅ Tự dựng những dạng đơn giản bằng HTML/CSS — chúng thừa hưởng token và theme miễn phí, còn canvas thì phải nạp màu bằng script và không để lại gì cho trình đọc màn hình.
- ❌ **Không bao giờ dùng hai trục tung.** Đây là lỗi biểu đồ phổ biến nhất: hai thang khác nhau trên cùng một khung làm hai đường cắt nhau ở chỗ do người vẽ chọn thang, không do dữ liệu — và người đọc sẽ kết luận sai. Hai đại lượng khác đơn vị thì tách hai biểu đồ, hoặc quy về cùng một gốc so sánh.
- ❌ Không quay vòng màu. Chuỗi thứ chín gom thành "Khác" hoặc tách thành nhiều biểu đồ nhỏ.
- ❌ Không dùng dải cầu vồng cho mức độ.
- ❌ Không dùng màu trạng thái làm màu chuỗi của một biểu đồ phân loại.
- ❌ Không vẽ biểu đồ phân tán quá ba chuỗi.
- ❌ Không cắt trục tung ở một giá trị khác 0 với dạng cột — nó phóng đại chênh lệch. Dạng đường thì được, và phải nói rõ trên trục.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Có cần dạng biểu đồ phân tán không? Hôm nay **không khai** vì chưa có màn nào cần, và nó kéo theo giới hạn ba chuỗi ở §2.8 | Sau F3 — dự án hạ nguồn đầu tiên có nhu cầu thật |
| 2 | Số liệu tiền tệ rút gọn ở mức nào — "2.152 tr" hay "2.152.000.000"? Rút gọn thì trục đọc được; đầy đủ thì đối chiếu sổ được. Có thể phải khác nhau giữa nhãn trục và hộp giá trị | Sau F3 — `ba-analyst` của màn báo cáo đầu tiên |
| 3 | Cả `Chart` nằm ở tầng bọc, nhưng [`../../adr/0019-ba-component-nang-thuoc-core.md`](../../adr/0019-ba-component-nang-thuoc-core.md) ràng buộc 1 đòi phần thư viện của `line` chỉ nạp khi màn đầu tiên dùng `line` được mở. Lớp bọc tách phần `line` thành khối tải trễ bên trong nó bằng cách nào — phải chốt trước khi dựng, vì import thẳng là kéo thư viện vào mọi màn có biểu đồ cột | F1 — khi dựng `Chart` |
