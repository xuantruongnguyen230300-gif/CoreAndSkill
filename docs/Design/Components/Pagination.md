---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Pagination

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** bọc PrimeNG — theo [`../COMPONENTS.md`](../COMPONENTS.md) §4, phân trang thuộc nhóm hành vi khó: tính dãy số trang có dấu lược, đồng bộ với phân trang phía máy chủ, giữ đúng chỉ số khi tổng số bản ghi đổi giữa hai lần tải. Đây cũng là component đi cặp với [`DataTable.md`](./DataTable.md), vốn đã bọc cùng thư viện — dùng hai nguồn khác nhau cho hai nửa của một cơ chế là cách chúng lệch nhau.

---

## Mục đích

Cho người dùng đi giữa các trang của một danh sách và chọn số dòng hiển thị mỗi trang.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Dưới một [`DataTable.md`](./DataTable.md) hoặc [`Table.md`](./Table.md) có nhiều trang | 🛑 Danh sách ngắn, vừa hết một trang → không render gì; thêm phân trang vào một danh sách năm dòng là thêm nhiễu |
| Danh sách thẻ, danh sách bản ghi phân trang phía máy chủ | 🛑 Chuyển giữa các phần nội dung khác nhau → [`Tabs.md`](./Tabs.md) |
| Chỗ người dùng cần biết mình đang ở đâu trong tổng thể | 🛑 Tìm, lọc, hành động của danh sách → [`Toolbar.md`](./Toolbar.md); 🛑 cuộn vô hạn — một cơ chế khác, và nó không cùng tồn tại với phân trang trên một danh sách |

## Biến thể

| Biến thể | Chứa gì | Dùng khi |
| --- | --- | --- |
| `full` | Trang đầu/cuối + trước/sau + dãy số trang + ô chọn số dòng + chỉ báo "x–y trên tổng z" | **Mặc định.** Bảng dữ liệu toàn trang |
| `compact` | Trước/sau + chỉ báo "x–y trên tổng z" | Danh sách trong `Card` hẹp, trong `Dialog` |
| `simple` | Chỉ trước/sau | Danh sách phụ, nơi tổng số không quan trọng với người dùng |

## Kích thước

| Cỡ | Chiều cao nút | Khe giữa nút | Cỡ chữ | Icon | Dùng khi |
| --- | --- | --- | --- | --- | --- |
| `sm` | `--size-control-sm` (28px) | `--sp-2` | `--fs-xs` | `--icon-sm` | Trong `Dialog`, trong `Card` hẹp |
| `md` | `--size-control-md` (34px) | `--sp-3` | `--fs-sm` | `--icon-md` | **Mặc định.** Dưới bảng toàn trang |

Nút số trang là hình vuông với bề rộng tối thiểu bằng chiều cao — số trang lên ba chữ số vẫn không được làm nút phình ngang và đẩy các nút bên cạnh nhảy chỗ.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Nút trang thường: nền trong suốt, chữ `--color-text-muted`, `--radius-sm`. Trang hiện hành: nền `--color-brand-subtle`, chữ `--color-brand-on-subtle`, `--fw-semibold` | Có |
| `hover` | Nút chưa chọn: nền `--color-surface-2`, chữ `--color-text`. Bọc trong `@media (hover: hover)` | Có |
| `focus-visible` | `outline` `--border-w-strong` màu `--color-focus`, `outline-offset` theo [`../DESIGN.md`](../DESIGN.md) §2.6 | Có |
| `active` | Nền đậm thêm một bậc theo cùng thang; không dịch chuyển vị trí | Có |
| `disabled` | Ở trang đầu thì nút "trang đầu" và "trang trước" nhận `disabled` thật; ở trang cuối thì hai nút kia. Chữ `--color-text-disabled`, con trỏ `not-allowed`. 🛑 Không **ẩn** nút — dải sẽ đổi bề rộng và các nút còn lại trượt sang chỗ khác | Có |
| `loading` | Cả dải nhận `disabled`, `aria-busy="true"`; **chỉ số trang giữ nguyên trang cũ** cho tới khi dữ liệu mới về. Nhảy chỉ số trước rồi request hỏng là hiển thị một trang không tồn tại | Có |
| `error` | **Không áp dụng cho chính dải.** Tải trang hỏng thì lỗi hiện ở vùng bảng qua [`NoticeBanner.md`](./NoticeBanner.md); `Pagination` chỉ quay về trạng thái nghỉ ở trang cũ | — |
| `empty` | Tổng số bản ghi bằng `0` → **không render phần tử nào**; chỗ đó thuộc về [`EmptyState.md`](./EmptyState.md). Tổng vừa đủ một trang → ẩn dãy số nhưng **vẫn giữ** chỉ báo "x–y trên tổng z" nếu biến thể có nó | Có |

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-surface-2`, `--color-text`, `--color-text-muted`, `--color-text-disabled`, `--color-brand-subtle`, `--color-brand-on-subtle`, `--color-border`, `--color-border-strong`, `--color-focus` |
| Chữ, khoảng cách | `--fs-xs`, `--fs-sm`, `--fw-medium`, `--fw-semibold`, `--lh-snug`, `--sp-2`, `--sp-3`, `--sp-4`, `--sp-5` |
| Hình dạng, chuyển động | `--radius-sm`, `--border-w`, `--border-w-strong`, `--dur-fast`, `--ease-standard` |
| Kích thước | `--size-control-sm`, `--size-control-md`, `--icon-sm`, `--icon-md`. Icon theo [`../Icons.md`](../Icons.md) §5: `pi-angle-left` · `pi-angle-right` cho trang trước/sau, `pi-angle-double-left` · `pi-angle-double-right` cho trang đầu/cuối |

⚠️ Trang hiện hành dùng `--color-brand-subtle`, chỉ chênh 1.25:1 với bề mặt ([`../DESIGN.md`](../DESIGN.md) §2.4) — nên nó **không được là dấu hiệu duy nhất**. Kênh thứ hai: chữ đổi sang `--color-brand-on-subtle` và nét chữ đổi sang `--fw-semibold`; kênh thứ ba là `aria-current="page"` cho trình đọc màn hình.

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `--bp-lg` | Một hàng: chỉ báo "x–y trên tổng z" bên trái, dãy nút giữa, ô chọn số dòng bên phải |
| `--bp-md` … `--bp-lg` | Dãy số trang rút bớt: giữ trang đầu, trang cuối, trang hiện hành và một trang liền kề mỗi bên; phần bị lược thay bằng dấu lược |
| < `--bp-md` | Chuyển sang biến thể `compact`: chỉ trước/sau kèm chỉ báo; ô chọn số dòng xuống hàng dưới. Dưới `--bp-xs` chỉ báo rút gọn còn "trang m / n" và hai nút giãn ra để đạt vùng chạm thoải mái |

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | `<nav>` bọc ngoài với `aria-label` nói rõ nó phân trang cái gì: "Phân trang danh sách người dùng". Từng nút là `<button>` thật |
| Vai trò ARIA | Trang hiện hành mang `aria-current="page"`. Không dùng `aria-selected` — đó là ngữ nghĩa của tab, không phải của trang |
| Nhãn từng nút | Mỗi nút có `aria-label` riêng: "Trang đầu", "Trang trước", "Trang 3", "Trang sau", "Trang cuối". 🛑 Một nút chỉ có icon mà không có nhãn thì trình đọc màn hình đọc lên một chuỗi rác — [`../Icons.md`](../Icons.md) §7 |
| Nhãn khác | "x–y trên tổng z" hiển thị bằng chữ và nằm trong vùng `aria-live="polite"`; ô chọn số dòng có `<label>` thật ("Số dòng mỗi trang"), không chỉ một con số trần đứng cạnh ô |
| Bàn phím | Tab đi qua từng nút theo thứ tự hiển thị. Không bắt phím mũi tên — người dùng đang ở trong một bảng và mũi tên thuộc về bảng |
| Focus | Sau khi đổi trang, focus **ở lại nút vừa bấm**. Nếu nút đó vừa thành `disabled` (bấm "trang trước" tới trang đầu) thì chuyển focus sang nút liền kề còn dùng được — nếu không, focus rơi về `<body>` và người dùng bàn phím mất hết vị trí |
| Vùng bấm | ≥ 28×28px kể cả cỡ `sm`, nới bằng `padding` |
| Chữ | Đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8. Chuỗi "x–y trên tổng z" là chuỗi có tham số, không ghép bằng nối chuỗi |

**Vì sao "x–y trên tổng z" là bắt buộc ở biến thể `full`:** một dãy số trang trần chỉ nói người dùng đang ở trang mấy, không nói danh sách có bao nhiêu bản ghi. Với một người vừa lọc xong, con số tổng chính là câu trả lời họ đang tìm — và nó tiết kiệm cho họ một lần đếm tay hoặc một lần đi tới trang cuối.

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `variant` | input | `'full' \| 'compact' \| 'simple'` | `'full'` | |
| `size` | input | `'sm' \| 'md'` | `'md'` | |
| `page` | input | `number` | `1` | Trang hiện hành, đếm từ 1. Trang cha giữ nguồn sự thật |
| `pageSize` | input | `number` | — | Bắt buộc. Phải là một giá trị có trong `pageSizeOptions` |
| `pageSizeOptions` | input | `number[]` | — | Bắt buộc. Danh sách lựa chọn số dòng mỗi trang |
| `totalRecords` | input | `number` | `0` | Tổng số bản ghi **sau khi đã lọc**, không phải tổng của cả bảng |
| `disabled` · `loading` | input | `boolean` | `false` | `loading` khoá cả dải và giữ nguyên chỉ số trang |
| `showPageSize` | input | `boolean` | `true` | Chỉ có nghĩa với `variant = 'full'` |
| `pageChanged` | output | `number` | — | Số trang người dùng **yêu cầu**. Không phát khi bấm lại trang đang đứng |
| `pageSizeChanged` | output | `{ pageSize: number; page: number }` | — | Phát kèm `page` đã đưa về `1` — xem dưới |

**`Pagination` chỉ phát sự kiện, không tự gọi API.** Đây là component **dumb** ([`../COMPONENTS.md`](../COMPONENTS.md) §5, ca thứ ba trong bảng "dễ nhầm"): trang cha nghe sự kiện, gọi API, rồi truyền `page` và `totalRecords` mới xuống. Cái giá là một vòng đi vòng lại và một khoảng thời gian ngắn mà chỉ số trang hiển thị chưa khớp ý định người dùng — chính là lý do dòng `loading` ở trên bắt giữ nguyên trang cũ. Cái mua được: cùng một `Pagination` chạy với dữ liệu từ máy chủ, từ bộ nhớ tạm, hay từ một mảng dựng sẵn trong test, mà không sửa một dòng nào.

🛑 **Bẫy: đổi số dòng mỗi trang phải đưa về trang 1.** Người dùng đang ở trang 8 với 10 dòng mỗi trang, đổi sang 100 dòng mỗi trang — trang 8 của khổ mới cần tới 800 bản ghi, và danh sách chỉ có 90. Kết quả là một trang trống, không có thông báo lỗi nào, và người dùng tưởng dữ liệu vừa biến mất. Vì vậy `pageSizeChanged` phát **cả hai** giá trị cùng lúc, với `page` đã đưa về `1`; nơi gọi không phải nhớ luật này, và không nơi gọi nào quên được nó.

**Luật bọc phải giữ** ([`../COMPONENTS.md`](../COMPONENTS.md) §4): API trên đây **không để lọt** kiểu dữ liệu hay tên sự kiện nào của PrimeNG — không có chỉ số dòng bắt đầu đếm từ 0, không có đối tượng sự kiện của thư viện. Tạo hình bằng token và bằng cơ chế theme của thư viện, 🛑 không dùng `::ng-deep`.

## Do / Don't

- ✅ Phát `pageSizeChanged` kèm `page` đã về `1`.
- ✅ `aria-label` riêng cho từng nút, `aria-current="page"` cho trang hiện hành.
- ✅ Hiện "x–y trên tổng z" ở biến thể `full`.
- ✅ Giữ nguyên chỉ số trang trong lúc `loading`.
- ✅ Chuyển focus sang nút liền kề khi nút vừa bấm trở thành `disabled`.
- ❌ Không tự gọi API trong component.
- ❌ Không ẩn nút điều hướng ở trang đầu/cuối — cho chúng `disabled`.
- ❌ Không để lọt kiểu dữ liệu PrimeNG ra API, và không tạo hình bằng `::ng-deep`.
- ❌ Không đặt `Pagination` vào trong [`Toolbar.md`](./Toolbar.md).

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Danh sách `pageSizeOptions` mặc định gồm những giá trị nào, và ai quyết? Đặt ở mỗi màn thì mỗi màn một kiểu; đặt cứng ở Core thì màn có dòng rất cao sẽ không hợp | Chủ sản phẩm cùng người dựng khung |
| 2 | Số trang có phản ánh lên URL không? Có thì người dùng chia sẻ được liên kết tới đúng trang và F5 không mất chỗ; nhưng nó kéo theo cả bộ lọc phải lên URL, nếu không thì trang 8 của một bộ lọc khác là vô nghĩa | Người dựng khung định tuyến |
| 3 | `Pagination` đặt trên bảng, dưới bảng, hay cả hai? Cả hai tiện với bảng dài nhưng nhân đôi landmark điều hướng và phải đồng bộ hai dải | Sau khi có `DataTable` thật |
