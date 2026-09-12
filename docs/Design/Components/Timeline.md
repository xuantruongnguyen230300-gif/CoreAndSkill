---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Timeline

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** tự dựng, không bọc PrimeNG. Theo [`../COMPONENTS.md`](../COMPONENTS.md) §4 đây là một danh sách dọc có chấm và vạch nối — thuần trình bày, không có hành vi nào thuộc nhóm "khó".

---

## Mục đích

Kể lại một chuỗi sự việc theo thứ tự thời gian, và cho biết chuỗi đó đang dừng ở đâu.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Nhật ký thay đổi của một bản ghi: ai đổi gì, lúc nào, từ giá trị nào sang giá trị nào | 🛑 Quy trình mà **người đang nhìn màn hình** bấm để đi tiếp → [`Stepper.md`](./Stepper.md). Ranh giới ở dưới |
| Luồng trình duyệt một chứng từ: ai đã duyệt, ai đang giữ, còn ai phía sau | 🛑 Danh sách bản ghi có thể sắp xếp và lọc → [`DataTable.md`](./DataTable.md). Timeline không phân trang theo cột, không sắp xếp lại được |
| Nhật ký đăng nhập, vết kiểm toán khi thanh tra hỏi "ai sửa dòng này" | 🛑 Một thông báo đơn lẻ → [`NoticeBanner.md`](./NoticeBanner.md) |
| Lịch sử một hồ sơ qua nhiều lần nộp lại | 🛑 Nội dung không có thứ tự thời gian → [`Card.md`](./Card.md) xếp thành lưới |

### Vì sao `approval` và `history` là hai biến thể của MỘT component

[`../COMPONENTS.md`](../COMPONENTS.md) §1 ưu tiên mở rộng hơn đẻ mới. Hai thứ này dùng chung toàn bộ khung dựng — cột chấm, vạch nối dọc, khối nội dung bên phải, cách xuống dòng ở màn nhỏ. Khác biệt duy nhất là **có mục chưa xảy ra hay không**, và đó đúng là thứ một biến thể diễn tả được.

Tách đôi sẽ cho hai component có 90% CSS trùng nhau, và ngày đổi khoảng cách dọc thì phải sửa hai chỗ — đúng khuôn hỏng mà [`../../../.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §5 cấm.

### Ranh giới với `Stepper` — một câu

> **Ai quyết định mục tiếp theo xảy ra khi nào?**
>
> Người đang nhìn màn hình → `Stepper`. Người khác, hoặc hệ thống → `Timeline` biến thể `approval`.

## Biến thể

| Biến thể | Có mục chưa xảy ra? | Thứ tự | Dùng khi |
| --- | --- | --- | --- |
| `history` | 🛑 Không — mọi mục đều đã xảy ra | **Mới nhất ở trên** | **Mặc định.** Nhật ký thay đổi, vết kiểm toán |
| `approval` | ✅ Có — các cấp chưa tới lượt vẫn hiện | **Cũ nhất ở trên**, theo đúng chiều đi của hồ sơ | Luồng trình duyệt |

**Hai biến thể đảo ngược thứ tự, và điều đó có chủ đích.** Người mở nhật ký gần như luôn hỏi *"vừa có chuyện gì"* — mới nhất phải ở trên. Người mở luồng duyệt hỏi *"hồ sơ đang ở đâu và còn qua ai"* — phải đọc xuôi chiều mới hiểu. Dùng chung một chiều cho cả hai là làm sai một trong hai.

## Kích thước

`Timeline` **không** dùng thang `--size-control-*`.

| Khoản | Giá trị |
| --- | --- |
| Đường kính chấm | 24px, dùng lại `--step-dot` mà [`Stepper.md`](./Stepper.md) khai |
| Độ dày vạch nối | 2px |
| Khe chấm → nội dung | `--sp-5` |
| Khe dọc giữa hai mục | `--sp-6` |
| Cỡ chữ tiêu đề mục | `--fs-sm`, `--fw-semibold` |
| Cỡ chữ dòng siêu dữ liệu | `--fs-xs`, màu `--color-text-muted` |
| Khối trích dẫn | Nền `--color-surface-2`, `--radius-sm`, đệm `--sp-4`, dải trái `--border-w-strong` |

Hai mật độ:

| Cỡ | Khe dọc | Dùng khi |
| --- | --- | --- |
| `sm` | `--sp-5` | Trong [`Drawer.md`](./Drawer.md), trong [`Card.md`](./Card.md) hẹp |
| `md` | `--sp-6` | **Mặc định.** Tab nhật ký của một màn chi tiết |

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Cột chấm bên trái, vạch nối chạy từ chấm này xuống chấm kế. Mục cuối **không** có vạch nối kéo xuống — một vạch treo lửng nói rằng còn gì đó phía dưới mà không có gì cả | Có |
| `hover` | Chỉ khi mục bấm được (mở bản ghi liên quan): nền khối nội dung đổi `--color-surface-2`. Mục thường **không** có hover — nó là thông tin, không phải nút. **Bọc trong `@media (hover: hover)`** | Có |
| `focus-visible` | Mục bấm được nhận `outline` `--color-focus`, `outline-offset: 2px` quanh khối nội dung | Có |
| `active` | Mục bấm được đang bị nhấn: nền `--color-surface-3` | Có |
| `disabled` | **Không áp dụng cho cả dải.** Một mục quá khứ không thể bị "vô hiệu hoá" — nó đã xảy ra rồi. Ở `approval`, mục chưa tới lượt dùng trạng thái riêng, xem bảng dưới | — |
| `loading` | *Lần đầu:* ba mục [`SkeletonLoader.md`](./SkeletonLoader.md) đúng hình dạng mục thật. *Tải thêm:* một dòng spinner ở **cuối** dải, giữ nguyên các mục đã có. `aria-busy` cả hai ca | Có |
| `error` | Tải hỏng: [`EmptyState.md`](./EmptyState.md) biến thể `error` kèm nút "Thử lại". Nếu đã tải được một phần thì **giữ phần đó** và chỉ báo lỗi ở cuối dải | Có |
| `empty` | Chưa có sự việc nào: `EmptyState` biến thể `compact`, câu nói rõ là *chưa có gì xảy ra* chứ không phải *không tải được*. Hai ca này người dùng luôn hiểu nhầm nếu dùng chung một câu | Có |

### Bốn trạng thái của một mục ở biến thể `approval`

| Trạng thái mục | Chấm | Vạch nối phía dưới | Nghĩa |
| --- | --- | --- | --- |
| `done` | Nền `--color-success`, icon `pi-check` | `--color-success` | Đã duyệt, đã qua |
| `current` | Viền `--color-brand`, icon `pi-spinner`, nền `--color-surface` | `--color-border-subtle` | Đang chờ người này |
| `rejected` | Nền `--color-danger`, icon `pi-times` | `--color-border-subtle` | Đã trả lại — hồ sơ quay về người lập |
| `upcoming` | Viền `--color-border`, icon `pi-lock`, chữ `--color-text-muted` | `--color-border-subtle` | Chưa tới lượt |

Bốn trạng thái khác nhau bằng **icon**, không chỉ bằng màu — [`../DESIGN.md`](../DESIGN.md) §2.7.

Ở biến thể `history`, chấm mang **icon của loại sự việc** (`pi-pencil` sửa, `pi-plus` tạo, `pi-lock` khoá, `pi-check` duyệt) trên nền `--color-surface` viền `--color-border`. Không tô màu theo loại — nhật ký không có "tốt" và "xấu", nó chỉ có "đã xảy ra".

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-surface-2`, `--color-surface-3`, `--color-text`, `--color-text-muted`, `--color-text-on-brand`, `--color-brand`, `--color-success`, `--color-danger`, `--color-border`, `--color-border-subtle`, `--color-focus` |
| Chữ | `--fs-2xs`, `--fs-xs`, `--fs-sm`, `--fw-medium`, `--fw-semibold`, `--lh-snug`, `--lh-normal` |
| Khoảng cách | `--sp-2`, `--sp-3`, `--sp-4`, `--sp-5`, `--sp-6` |
| Hình dạng | `--radius-sm`, `--radius-full`, `--border-w`, `--border-w-strong` |
| Kích thước | `--icon-sm`, `--step-dot` |
| Icon | `pi-check`, `pi-times`, `pi-spinner`, `pi-lock`, `pi-pencil`, `pi-plus` — [`../Icons.md`](../Icons.md) §5 |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `--bp-md` | Cột chấm bên trái, nội dung bên phải. Dòng siêu dữ liệu nằm cùng hàng với tiêu đề nếu đủ chỗ |
| `--bp-sm` … `--bp-md` | Dòng siêu dữ liệu xuống dòng riêng dưới tiêu đề |
| < `--bp-sm` | Cột chấm **thu hẹp**: chấm giảm còn 16px, khe còn `--sp-4`. Khối trích dẫn trải hết bề rộng còn lại |

Cột chấm không bao giờ bị bỏ hẳn. Nó là thứ duy nhất nói rằng đây là một chuỗi chứ không phải một danh sách rời rạc.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | `<ol>` — có thứ tự, và thứ tự là thông tin. Biến thể `history` đảo chiều bằng CSS, **không** đảo trong DOM: thứ tự DOM giữ đúng chiều thời gian để trình đọc màn hình kể lại đúng |
| Chấm và vạch nối | Thuần trang trí: `aria-hidden="true"`. Ý nghĩa của chúng phải có trong chữ |
| Trạng thái mục | Nói bằng **chữ**, không chỉ bằng icon: "đã duyệt", "đang chờ", "chưa tới lượt". Icon một mình không đọc lên được |
| Thời gian | Dùng `<time datetime="…">` với giá trị máy đọc được. Hiện thời gian **tuyệt đối**; thời gian tương đối ("2 giờ trước") chỉ làm chú thích thêm |
| Mục bấm được | `<button>` hoặc `<a>` thật bên trong mục, không phải bắt sự kiện trên cả khối |
| Tải thêm | Nút "Tải thêm" là `<button>` thật. Sau khi tải, focus về **mục mới đầu tiên**, không về đầu dải |
| Thay đổi giá trị | Cặp cũ → mới phải đọc được thành câu: "từ Nhân viên nhập liệu sang Kế toán trưởng". Gạch ngang chữ cũ là tín hiệu thị giác, không phải ngữ nghĩa |
| Chữ | Đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `items` | input | `ReadonlyArray<TimelineItem>` | `[]` | Chữ ký đầy đủ ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9 |
| `variant` | input | `'history' \| 'approval'` | `'history'` | |
| `size` | input | `'sm' \| 'md'` | `'md'` | |
| `loading` | input | `boolean` | `false` | |
| `hasMore` | input | `boolean` | `false` | Còn mục chưa tải; bật nút "Tải thêm" ở cuối dải |
| `itemSelected` | output | `string` | — | Phát khoá mục, chỉ với mục được khai là bấm được |
| `loadMore` | output | `void` | — | |

Nút hành động của bước hiện hành ở biến thể `approval` (Duyệt / Trả lại) **không** thuộc component này — chúng là [`Button.md`](./Button.md) do trang truyền vào qua slot của mục hiện hành. Lý do: quyền bấm hai nút đó là chuyện nghiệp vụ, và một component dumb không được quyết ai thấy nút nào.

`Timeline` là component **dumb** ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

## Do / Don't

- ✅ Ở `history` để mới nhất lên trên; ở `approval` đọc xuôi chiều.
- ✅ Hiện giá trị cũ → giá trị mới, không chỉ ghi "đã cập nhật". Một dòng "đã cập nhật thông tin" không trả lời được câu hỏi duy nhất mà người đọc nhật ký có.
- ✅ Hiện thời gian chờ ở mục `current`: "đã 1 ngày" là thứ làm người ta hành động; một dấu thời gian trần thì không.
- ✅ Bắt buộc có lý do ở mục `rejected`, và hiện nó ở chỗ người lập nhìn thấy đầu tiên.
- ❌ Không dùng thời gian tương đối một mình.
- ❌ Không vẽ vạch nối kéo xuống từ mục cuối.
- ❌ Không tô màu theo loại sự việc ở biến thể `history`.
- ❌ Không hiện nút Duyệt ở mục chưa tới lượt — và ẩn nút **không** thay được kiểm tra quyền ở máy chủ.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Nhật ký dài tải thêm bằng nút hay tự tải khi cuộn tới đáy? Nút thì kiểm soát được và thân thiện với bàn phím; tự tải thì mượt hơn nhưng dễ làm người dùng mất chỗ đang đọc | Dự án đầu tiên có màn nhật ký thật |
| 2 | Một lượt sửa đổi nhiều trường cùng lúc hiện thành **một** mục hay nhiều mục? Một mục thì dải gọn nhưng phải gấp/mở; nhiều mục thì dải dài ra rất nhanh với những thao tác sửa hàng loạt | `ba-analyst`, vì nó phụ thuộc cách ghi vết ở tầng dữ liệu |
| 3 | Ở `approval`, luồng có nhánh (hai người duyệt song song) thì vẽ thế nào? Hôm nay spec chỉ mô tả luồng **tuyến tính**. Luồng nhánh cần một hình thức khác hẳn và chưa có ở đây | Dự án đầu tiên có quy trình duyệt song song |
