---
kind: luat
scope: core
verified: chua-doi-chieu
---

# EmptyState

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** tự dựng. Theo tiêu chí ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 đây là component trình bày thuần — không hành vi nào, chỉ có **câu chữ đúng**, và câu chữ đúng là thứ không thư viện nào cho được.

---

## Mục đích

Biến một vùng không có gì thành một chỉ dẫn: giải thích vì sao trống và làm gì tiếp theo.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Danh sách chưa có bản ghi nào | 🛑 Đang chờ dữ liệu → [`SkeletonLoader.md`](./SkeletonLoader.md). Trống vì chưa tải xong **khác hẳn** trống vì không có gì |
| Bộ lọc hoặc tìm kiếm không ra kết quả | 🛑 Lỗi kỹ thuật cần thử lại → dùng biến thể `error` của chính component này, xem §Biến thể |
| Người dùng chưa có quyền xem nội dung | 🛑 Một trường form rỗng → đó là chuyện bình thường, không cần gì cả |
| Một khu vực tính năng chưa được cấu hình | 🛑 Cả trang lỗi 404/500 → trang lỗi riêng, có bố cục toàn màn |

**"Trống" là một trạng thái phải thiết kế, không phải chỗ để trống.** Một bảng không có dòng nào và không có chữ nào khiến người dùng không phân biệt được ba khả năng: hệ thống hỏng, họ lọc sai, hay thật sự chưa có dữ liệu. Ba khả năng đó dẫn tới ba hành động khác nhau.

## Biến thể

Đây là component mà **biến thể chính là câu chữ**, không phải hình thức.

| Biến thể | Icon | Câu nói gì | Hành động |
| --- | --- | --- | --- |
| `first-use` | `pi-inbox` | Chưa có bản ghi nào, và tạo cái đầu tiên thì được gì | Nút `primary` tạo mới |
| `no-results` | `pi-search` | Bộ lọc hiện tại không khớp gì. **Nhắc lại điều kiện đang lọc** | Nút `secondary` "Xoá bộ lọc" |
| `error` | `pi-times-circle`, màu `--color-danger` | Không tải được, và có phải lỗi tạm thời không | Nút `secondary` "Thử lại" |
| `no-permission` | `pi-lock` | Không có quyền xem, và xin quyền ở đâu | Không có nút, hoặc liên kết tới hướng dẫn |
| `not-configured` | `pi-cog` | Tính năng cần cấu hình trước | Nút `primary` tới trang cấu hình |

🛑 **`first-use` và `no-results` không được dùng chung một câu.** Đây là lỗi hay gặp nhất ở màn danh sách: người dùng vừa gõ một bộ lọc, thấy màn hình mời "Thêm bản ghi đầu tiên", và tưởng dữ liệu đã mất. Hai ca này khác nhau về **nguyên nhân**, về **hành động cần làm**, và về **cảm giác người dùng** — nên chúng phải khác nhau cả về câu chữ lẫn nút.

## Kích thước

| Biến thể kích thước | Đệm dọc | Icon | Cỡ chữ tiêu đề | Dùng khi |
| --- | --- | --- | --- | --- |
| `compact` | `--sp-8` | vòng tròn 40px, icon `--icon-lg` | `--fs-md` | Trong thân một bảng, trong một `Card` nhỏ |
| `default` | `--sp-10` | vòng tròn 56px, icon `--icon-xl` | `--fs-lg` | Trong một `Card` chiếm cả trang |
| `page` | `--sp-12` | vòng tròn 72px, icon `--icon-xl` | `--fs-xl` | Cả một trang không có nội dung |

| Khoản | Giá trị |
| --- | --- |
| Nền vòng tròn icon | `--color-surface-2`; ở biến thể `error` là `--color-danger-bg` |
| Bo góc vòng tròn | `--radius-full` |
| Khe icon → tiêu đề | `--sp-6` |
| Khe tiêu đề → mô tả | `--sp-3` |
| Khe mô tả → nút | `--sp-6` |
| Bề rộng tối đa khối chữ | 420px, căn giữa |
| Màu tiêu đề | `--color-text`, `--fw-semibold` |
| Màu mô tả | `--color-text-muted`, `--fs-sm`, `--lh-normal` |

**Icon không phóng to bằng cách tăng cỡ chữ của icon font.** Một glyph font kéo lên 48px sẽ vỡ nét và mỏng dính. Cách làm: giữ icon ở `--icon-xl` (24px) và đặt nó trong một vòng tròn nền lớn hơn — xem [`../Icons.md`](../Icons.md) §3.

**Bề rộng khối chữ giới hạn 420px** vì một dòng chữ trải hết bề rộng màn 1600px thì mắt mất dòng khi xuống hàng.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Icon trong vòng tròn, tiêu đề, mô tả, nút — tất cả căn giữa theo trục ngang | Có |
| `hover` | **Không áp dụng ở mức component.** Hover thuộc nút bên trong | — |
| `focus-visible` | **Không áp dụng ở mức component** — bản thân nó không nhận focus. Nút bên trong có vòng focus của nó | — |
| `active` | **Không áp dụng.** Không phải control | — |
| `disabled` | **Không áp dụng.** Người dùng không có quyền hành động thì dùng biến thể `no-permission` và **không vẽ nút**, thay vì vẽ một nút mờ | — |
| `loading` | **Không áp dụng.** Đang tải là trạng thái của `SkeletonLoader`, không phải của `EmptyState`. Hai thứ này không được thay thế nhau — xem dưới | — |
| `error` | Là biến thể `error`, không phải một trạng thái | — |
| `empty` | Đây **là** trạng thái rỗng. Component chính là nó | Có |

**Vì sao `EmptyState` không có trạng thái `loading`:** chúng là hai component cho hai câu trả lời khác nhau. Hiện `EmptyState` trong lúc còn đang tải là **nói dối** — người dùng đọc "Chưa có bản ghi nào" rồi một giây sau thấy hai mươi dòng hiện ra. Trang phải giữ trạng thái `loading` cho tới khi có câu trả lời chắc chắn, rồi mới chọn giữa dữ liệu và `EmptyState`.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-text`, `--color-text-muted`, `--color-surface-2`, `--color-danger`, `--color-danger-bg` |
| Chữ | `--fs-sm`, `--fs-md`, `--fs-lg`, `--fs-xl`, `--fw-semibold`, `--fw-regular`, `--lh-tight`, `--lh-normal` |
| Khoảng cách | `--sp-3`, `--sp-6`, `--sp-8`, `--sp-10`, `--sp-12` |
| Hình dạng | `--radius-full` |
| Kích thước | `--icon-lg`, `--icon-xl` |

**`EmptyState` không có nền riêng và không có viền.** Nó luôn nằm trong một vùng chứa đã có nền — thân bảng, `Card`, cả trang. Tự vẽ nền là vẽ một hộp trong một hộp.

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `--bp-md` | Giữ nguyên đệm và cỡ theo biến thể kích thước |
| < `--bp-md` | Đệm dọc giảm một bậc (`page` → `default`, `default` → `compact`); bề rộng khối chữ theo vùng chứa |
| < `--bp-xs` | Nút chuyển sang `block`; đệm dọc giảm thêm một bậc |

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Icon | `aria-hidden="true"` — nó lặp lại thông tin đã có trong tiêu đề ([`../Icons.md`](../Icons.md) §7) |
| Tiêu đề | Thẻ heading đúng cấp **theo vị trí trong trang**, không phải cấp cố định. Trong thân một bảng thì thường là `<h3>`; cả một trang thì `<h2>` dưới `<h1>` của `PageHeader` |
| Xuất hiện sau khi tải xong | Vùng chứa bảng phải có `aria-live="polite"` để trình đọc màn hình biết kết quả — nếu không, người dùng bấm lọc và không nghe thấy gì |
| Nút | Là [`Button.md`](./Button.md) thật, nằm trong thứ tự Tab |
| Tương phản | Mô tả dùng `--color-text-muted`, đạt **7.56:1** ở theme sáng và **7.08:1** ở theme tối trên `--color-surface` ([`../DESIGN.md`](../DESIGN.md) §2.2) |
| Chữ | Tiêu đề, mô tả, nhãn nút qua i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `variant` | input | `'first-use' \| 'no-results' \| 'error' \| 'no-permission' \| 'not-configured'` | — | **Bắt buộc, không có mặc định.** Nếu có mặc định thì mọi nơi sẽ dùng mặc định và hai ca `first-use` / `no-results` lại nhập làm một |
| `size` | input | `'compact' \| 'default' \| 'page'` | `'default'` | |
| `title` | input | `string` | — | Bắt buộc |
| `description` | input | `string \| null` | `null` | |
| `icon` | input | `string \| null` | `null` | `null` = dùng icon mặc định của biến thể |
| `actionLabel` | input | `string \| null` | `null` | `null` = không vẽ nút |
| `headingLevel` | input | `2 \| 3 \| 4` | `3` | Cấp heading, do nơi gọi quyết định theo vị trí |
| `actionClicked` | output | `void` | — | |

**`variant` bắt buộc là quyết định API quan trọng nhất của component này.** Nó buộc mỗi nơi gọi phải trả lời câu hỏi *"trống vì lý do gì"* — chính câu hỏi mà component tồn tại để trả lời. Một giá trị mặc định sẽ khiến câu hỏi đó không bao giờ được hỏi.

`EmptyState` là component **dumb**: nó không biết vì sao vùng đó trống, nó nhận biết đó qua `input()`.

## Viết câu chữ cho trạng thái rỗng

Đây là phần khó nhất và cũng là phần quyết định component này có ích hay không.

| Yếu tố | Viết đúng | Viết sai |
| --- | --- | --- |
| Tiêu đề | Nói **tình huống**: "Chưa có người dùng nào" | "Không có dữ liệu" — đúng nhưng vô dụng |
| Mô tả | Nói **làm gì tiếp**: "Thêm người dùng đầu tiên để bắt đầu phân quyền" | "Danh sách trống" — lặp lại tiêu đề |
| `no-results` | Nhắc lại điều kiện: "Không có người dùng nào khớp \"nguyen\" trong vai trò Quản trị" | "Không tìm thấy kết quả" — không giúp sửa bộ lọc |
| `error` | Nói có nên thử lại không: "Không tải được danh sách. Kiểm tra kết nối rồi thử lại" | "Đã có lỗi xảy ra" |
| `no-permission` | Nói xin quyền ở đâu: "Bạn không có quyền xem danh sách này. Liên hệ quản trị viên để được cấp quyền" | "Truy cập bị từ chối" |

Ba luật:

- **Không đùa cợt.** Người dùng gặp trạng thái rỗng đang bối rối hoặc đang gặp lỗi; một câu hài hước lúc đó gây khó chịu chứ không dễ thương.
- **Không đổ lỗi cho người dùng.** "Bộ lọc của bạn quá hẹp" nghe như trách móc; "Không có kết quả nào khớp bộ lọc hiện tại" thì không.
- **Không hứa thứ không làm được.** Đừng viết "Thử lại sau vài phút" nếu không biết lỗi có tự khỏi hay không.

## Do / Don't

- ✅ Luôn khai `variant`.
- ✅ Tách `first-use` và `no-results` bằng cả câu chữ lẫn nút.
- ✅ Với `no-results`, nhắc lại điều kiện đang lọc.
- ✅ Truyền `headingLevel` đúng theo vị trí trong trang.
- ✅ Giữ trạng thái `loading` cho tới khi có câu trả lời chắc chắn.
- ❌ Không hiện `EmptyState` trong lúc còn đang tải.
- ❌ Không dùng "Không có dữ liệu" làm tiêu đề.
- ❌ Không vẽ nút mờ khi người dùng không có quyền — dùng `no-permission` và bỏ nút.
- ❌ Không phóng to icon font. Phóng vòng tròn nền.
- ❌ Không tự vẽ nền hoặc viền.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Có dùng hình minh hoạ thay icon không? Hình minh hoạ thân thiện hơn nhưng cần bộ tài sản riêng, cần bản cho theme tối, và tốn băng thông | Khi có nguồn lực thiết kế đồ hoạ |
| 2 | Câu chữ mặc định cho từng `variant` đặt ở i18n của Core hay bắt mỗi màn tự viết? Mặc định thì nhanh nhưng sẽ chung chung; bắt tự viết thì tốt hơn nhưng sẽ có màn viết ẩu | Người thiết kế tầng i18n |
| 3 | Trang lỗi 404/500 toàn màn có tái dùng `EmptyState` ở cỡ `page` không, hay là component riêng? Tái dùng thì gọn nhưng trang lỗi cần cả logo và liên kết về trang chủ | Khi dựng trang lỗi |
