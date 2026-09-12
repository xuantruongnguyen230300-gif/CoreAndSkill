---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Chart

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** **hỗn hợp, và ranh giới là một quyết định đã cân nhắc.** Đúng **một** biến thể — `line` — bọc thư viện, vì nó cần thang đo, nội suy và lớp tương tác theo con trỏ, đúng nhóm "khó" ở [`../COMPONENTS.md`](../COMPONENTS.md) §4. **Bốn biến thể còn lại tự dựng bằng HTML/CSS**, vì chúng chỉ là những khối có kích thước tính theo phần trăm. Lý do chọn tự dựng ở mục Do / Don't.

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

🛑 **Mỗi biểu đồ phải có một bảng số liệu tương đương xem được.** Đây vừa là kênh cho trình đọc màn hình, vừa là kênh bù bắt buộc cho ba màu dưới ngưỡng tương phản ở theme sáng ([`../DESIGN.md`](../DESIGN.md) §2.8 ràng buộc 2).

## Biến thể

**Năm** biến thể, loại trừ nhau — mỗi cái trả lời một câu hỏi khác nhau về dữ liệu:

| Biến thể | Câu hỏi nó trả lời | Nền |
| --- | --- | --- |
| `column` | So sánh nhiều giá trị rời rạc | tự dựng |
| `line` | Xu hướng liên tục, nhiều chuỗi cùng đơn vị | bọc thư viện |
| `donut` | Cơ cấu của một tổng, **tối đa ba bốn phần** | tự dựng |
| `diverging` | Chênh lệch hai chiều quanh một mốc | tự dựng |
| `heatmap` | Mức độ theo hai chiều | tự dựng |

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
| `sm` | 120px | Trong một ô số liệu, trong [`Drawer.md`](./Drawer.md) |
| `md` | 200px | **Mặc định.** Trong [`Card.md`](./Card.md) |
| `lg` | 320px | Biểu đồ là nội dung chính của cả vùng |

Bề rộng luôn theo vùng chứa. Biểu đồ **không** tự đặt `width`.

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
| `loading` | [`SkeletonLoader.md`](./SkeletonLoader.md) đúng hình dạng biểu đồ sắp tới (vài cột xám cho `column`, một dải cho `line`), `aria-busy="true"`. 🛑 **Không vẽ biểu đồ rỗng rồi cho dữ liệu nhảy vào** — người dùng đọc mất một khung hình sai | Có |
| `error` | Thay vùng vẽ bằng [`EmptyState.md`](./EmptyState.md) biến thể `error` kèm nút "Thử lại". **Giữ nguyên tiêu đề và chú giải** — chúng cho biết đây là biểu đồ gì | Có |
| `empty` | **Hai ca phải phân biệt.** *Chưa có dữ liệu kỳ này:* câu nói rõ là chưa phát sinh. *Bộ lọc không ra kết quả:* câu khác kèm nút xoá lọc. Trộn hai ca là lỗi hay gặp nhất ở màn báo cáo | Có |

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
| Bóng, lớp | `--shadow-2`, `--z-popover` — cho hộp giá trị |

🛑 **Chữ không bao giờ mặc màu dữ liệu.** Nhãn, số, chú giải, chữ trên trục đều dùng token màu chữ. Vàng và ngọc đọc không ra khi làm màu chữ trên nền sáng. Danh tính đến từ **chấm màu bên cạnh** chữ, không từ việc tô màu chính chữ đó. Ngoại lệ duy nhất: nhãn đặt **bên trong** một mảng màu đặc, lúc đó chọn trắng hoặc mực theo độ sáng của mảng.

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `--bp-lg` | Đầy đủ: tiêu đề, chú giải, nhãn trục, nhãn trực tiếp trên mark |
| `--bp-md` … `--bp-lg` | Bỏ nhãn trực tiếp, giữ chú giải và nhãn trục. `heatmap` bắt đầu cuộn ngang |
| < `--bp-md` | Nhãn trục thưa đi — chỉ hiện mốc đầu, giữa, cuối. Chú giải xuống dòng dưới vùng vẽ |
| < `--bp-xs` | `column` quá tám cột và `heatmap` chuyển sang **bảng số liệu**, không cố vẽ tiếp |

Chuyển sang bảng ở màn rất nhỏ không phải là thua cuộc: mười hai cột trên màn 390px là mười hai vạch rộng hai pixel, không so được gì. Bảng số liệu vốn đã phải có sẵn, nên đây chỉ là hiện cái đã có.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Vai trò | Vùng vẽ mang `role="img"` với `aria-label` **tóm tắt kết luận**, không mô tả hình: "Thực hiện luỹ kế 2.152 triệu, thấp hơn dự toán 2.625 triệu tại tháng 9" |
| Bảng tương đương | Bắt buộc. Hoặc hiện ngay dưới biểu đồ, hoặc sau một nút "Xem số liệu". Không được chỉ tồn tại trong hộp giá trị khi rê chuột |
| Bàn phím | Biểu đồ là một điểm dừng Tab; `←` `→` đi giữa các điểm; `Escape` thoát khỏi chế độ đọc điểm |
| Chú giải | Từ **hai chuỗi trở lên luôn có chú giải**; một chuỗi thì không — tiêu đề đã nói đang vẽ gì, và một ô chú giải cho một chuỗi chỉ lặp lại tiêu đề |
| Màu | Không bao giờ là kênh duy nhất. Chuỗi phải phân biệt được bằng chú giải, nhãn trực tiếp, hoặc thứ tự trong bảng |
| Ba màu dưới 3:1 ở theme sáng | Biểu đồ dùng ngọc, vàng hoặc hồng **bắt buộc** kèm nhãn số hiện rõ hoặc bảng số liệu. Ràng buộc, không phải khuyến nghị |
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
| `showTable` | input | `boolean` | `true` | Mặc định **hiện** bảng số liệu. Sai theo hướng an toàn: quên khai thì kênh bù vẫn còn |
| `statusSeries` | input | `boolean` | `false` | `true` thì dùng bảng màu trạng thái thay bảng màu chuỗi |
| `loading` | input | `boolean` | `false` | |
| `pointSelected` | output | `{series, index}` | — | Chỉ phát khi màn hình khai biểu đồ bấm được để lọc |

🛑 **`series` không nhận màu từ ngoài.** Màu gán theo **thứ tự slot** trong [`../DESIGN.md`](../DESIGN.md) §2.8, và gán theo **đối tượng chứ không theo thứ hạng** — lọc bỏ một chuỗi thì các chuỗi còn lại **không đổi màu**. Cho phép truyền màu vào là mở đường cho mỗi màn một bảng màu, và đó là lúc hệ màu chết.

`Chart` là component **dumb** ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

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
| 1 | Dạng `line` bọc thư viện nào? PrimeNG đi kèm Chart.js, nhưng nó vẽ bằng canvas — không có gì cho trình đọc màn hình và không kế thừa token CSS. Đánh đổi và ngưỡng quyết định ghi ở [`../DESIGN.md`](../DESIGN.md) §10 | `architect`, khi có màn biểu đồ thật |
| 2 | Bảng số liệu tương đương hiện sẵn hay giấu sau một nút? Hiện sẵn thì luôn có mặt nhưng chiếm chỗ gấp đôi; giấu thì gọn nhưng thêm một lần bấm cho người cần con số | Dự án đầu tiên có màn báo cáo |
| 3 | Có cần dạng biểu đồ phân tán không? Hôm nay **không khai** vì chưa có màn nào cần, và nó kéo theo giới hạn ba chuỗi ở §2.8 | Dự án đầu tiên có nhu cầu thật |
| 4 | Số liệu tiền tệ rút gọn ở mức nào — "2.152 tr" hay "2.152.000.000"? Rút gọn thì trục đọc được; đầy đủ thì đối chiếu sổ được. Có thể phải khác nhau giữa nhãn trục và hộp giá trị | `ba-analyst` của màn báo cáo đầu tiên |
