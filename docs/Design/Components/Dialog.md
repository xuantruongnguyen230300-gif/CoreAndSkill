---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Dialog

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** **bọc PrimeNG**. Theo tiêu chí ở [`../COMPONENTS.md`](../COMPONENTS.md) §4, bẫy focus và khôi phục focus là ca "khó" điển hình: làm đúng toàn bộ ca biên (Tab vòng lại, Shift+Tab ngược, phần tử xuất hiện động, Escape, khoá cuộn nền, iframe bên trong) là hàng trăm dòng và rất nhiều lần sai.

---

## Mục đích

Một lớp nổi **chặn** buộc người dùng xử lý xong rồi mới quay lại trang.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Form tạo/sửa một bản ghi ngắn | 🛑 Hỏi xác nhận một hành động → [`ConfirmDialog.md`](./ConfirmDialog.md), nó có đúng hai nút và có mức nguy hiểm |
| Xem chi tiết nhanh mà không rời màn danh sách | 🛑 Báo kết quả một thao tác → [`Toast.md`](./Toast.md). Dialog bắt người dùng bấm để đóng; một thông báo thành công không đáng bị thế |
| Một bước cần tập trung tuyệt đối | 🛑 Thông tin luôn hiện, gắn với bối cảnh trang → [`NoticeBanner.md`](./NoticeBanner.md) |
| | 🛑 Form dài nhiều bước → dùng một trang riêng. Dialog cuộn nội bộ ở màn nhỏ là trải nghiệm tệ |
| | 🛑 Menu hoặc chọn nhanh → lớp nổi không chặn, không thuộc component này |

**Ngưỡng quyết định giữa dialog và trang riêng:** nếu form có trên khoảng bảy trường, hoặc cần cuộn ở màn desktop, hoặc người dùng cần tra cứu thông tin ở trang nền để điền — thì đó là một trang, không phải một dialog.

## Biến thể

| Biến thể | Bề rộng | Dùng khi |
| --- | --- | --- |
| `sm` | 420px | Form một tới ba trường; thông báo cần xác nhận có nội dung |
| `md` | 640px | **Mặc định.** Form thông thường |
| `lg` | 880px | Form hai cột; nội dung có bảng |
| `full` | Gần hết viewport, chừa `--sp-8` mỗi bên | Chỉ khi nội dung thật sự cần — xem trước tài liệu, biên tập nội dung dài |

Bề rộng là **tối đa**. Ở màn hẹp hơn, dialog co lại và giữ khoảng hở `--sp-5` mỗi bên.

Không có biến thể "không đóng được". Mọi dialog phải đóng được bằng Escape — xem §Accessibility.

## Kích thước

| Khoản | Giá trị |
| --- | --- |
| Đệm phần đầu / thân / chân | `--sp-6` |
| Khe giữa tiêu đề và mô tả | `--sp-3` |
| Khe giữa các nút ở chân | `--sp-4` |
| Bo góc | `--radius-lg` |
| Bóng | `--shadow-4` |
| Chiều cao tối đa | `min(<chiều cao viewport> - --sp-9 * 2, <chiều cao nội dung>)` — phần thân cuộn, đầu và chân đứng yên |
| Lớp | `--z-dialog`; backdrop ở `--z-backdrop` |

**Phần đầu và phần chân không cuộn.** Nội dung dài mà nút "Lưu" trôi khỏi tầm nhìn là cách chắc chắn để người dùng tưởng dialog không có nút lưu.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Nền `--color-surface`; viền `--color-border`; `--radius-lg`; `--shadow-4`. Backdrop `--color-overlay` phủ toàn màn | Có |
| `hover` | **Không áp dụng ở mức dialog.** Hover thuộc các control bên trong | — |
| `focus-visible` | **Không áp dụng ở mức dialog** — bản thân hộp không nhận focus. Nhưng focus **bên trong** bị bẫy: xem §Accessibility | — |
| `active` | **Không áp dụng.** Dialog không phải control | — |
| `disabled` | **Không áp dụng.** Dialog không có trạng thái vô hiệu hoá; muốn chặn thao tác thì khoá từng control bên trong | — |
| `loading` | Phần thân phủ `--color-scrim` + spinner; các nút ở chân vào trạng thái `loading`; **Escape vẫn đóng được** trừ khi đang gửi dữ liệu — xem dưới. `aria-busy="true"` | Có |
| `error` | [`NoticeBanner.md`](./NoticeBanner.md) vai `danger` chèn ngay đầu phần thân, **trên** nội dung form; phần thân cuộn lên đầu để người dùng thấy nó | Có |
| `empty` | **Không áp dụng.** Một dialog không có nội dung thì không nên mở | — |

**Escape trong lúc đang gửi dữ liệu:** đóng dialog lúc request đang bay khiến người dùng không biết thao tác thành công hay không. Luật: trong lúc `loading` **do một thao tác ghi**, Escape **không** đóng; thay vào đó không làm gì và giữ nguyên. Trong lúc `loading` do đang tải dữ liệu để hiển thị, Escape **đóng bình thường** — chưa có gì để mất.

**Mở và đóng:** vào bằng mờ dần + phóng nhẹ (`--dur-base`, `--ease-decelerate`); ra bằng mờ dần (`--dur-fast`, `--ease-accelerate`). Backdrop mờ dần cùng lúc. Cả hai tắt khi `prefers-reduced-motion: reduce` ([`../DESIGN.md`](../DESIGN.md) §7).

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-text`, `--color-text-muted`, `--color-border`, `--color-overlay`, `--color-scrim`, `--color-focus` |
| Chữ | `--fs-lg`, `--fs-sm`, `--fw-bold`, `--fw-regular`, `--lh-tight`, `--lh-normal` |
| Khoảng cách | `--sp-3`, `--sp-4`, `--sp-5`, `--sp-6`, `--sp-8`, `--sp-9` |
| Hình dạng | `--radius-lg`, `--border-w` |
| Bóng | `--shadow-4` |
| Lớp | `--z-dialog`, `--z-backdrop` |
| Chuyển động | `--dur-fast`, `--dur-base`, `--ease-decelerate`, `--ease-accelerate` |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `--bp-md` | Dialog căn giữa cả hai chiều; bề rộng theo biến thể |
| `--bp-sm` … `--bp-md` | Bề rộng co theo viewport, chừa `--sp-5` mỗi bên; căn giữa dọc |
| < `--bp-sm` | Dialog **dính đáy** và chiếm hết bề rộng, bo góc chỉ ở hai góc trên; nhóm nút ở chân xếp dọc, nút chính lên trên |

**Vì sao ở màn nhỏ dialog dính đáy chứ không căn giữa:** bàn phím ảo mở lên chiếm nửa dưới màn hình. Một dialog căn giữa sẽ bị đẩy lên và cắt mất phần đầu. Dính đáy thì dialog trượt lên cùng bàn phím và giữ được ô đang gõ trong tầm nhìn.

## Accessibility

Đây là phần nặng nhất của component này, và cũng là lý do nó bọc thư viện.

| Khoản | Yêu cầu |
| --- | --- |
| Vai trò | `role="dialog"` + `aria-modal="true"` |
| Nhãn | `aria-labelledby` trỏ tới id của tiêu đề. Không có tiêu đề nhìn thấy thì dùng `aria-label` |
| Mô tả | `aria-describedby` trỏ tới đoạn mô tả, nếu có |
| **Bẫy focus** | Tab và Shift+Tab **chỉ đi vòng trong dialog**. Không thoát ra được nền phía sau |
| **Focus lúc mở** | Đặt vào phần tử tương tác **đầu tiên có ý nghĩa** — thường là ô nhập đầu, hoặc chính hộp dialog nếu chỉ có chữ. 🛑 **Không** đặt vào nút đóng: người dùng bàn phím sẽ vô tình bấm Enter và đóng ngay |
| **Focus lúc đóng** | Trả về **đúng phần tử đã mở dialog**. Không trả về là người dùng bàn phím bị ném về đầu trang |
| Escape | Đóng dialog (trừ ca đang gửi dữ liệu ở §Trạng thái) |
| Bấm ra ngoài | Đóng — **trừ khi form đã có thay đổi chưa lưu**. Lúc đó hỏi xác nhận. Mất mười phút gõ vì một cú bấm hụt là lỗi không tha thứ được |
| Khoá cuộn nền | Nền không cuộn khi dialog mở. 🛑 Nhưng **phải giữ vị trí cuộn** — nhiều cách khoá cuộn làm trang nhảy về đầu khi đóng |
| Nội dung nền | Nội dung phía sau mang `inert` hoặc `aria-hidden="true"` để trình đọc màn hình không đọc xuyên qua |
| Nút đóng | Là một [`IconButton.md`](./IconButton.md) có `aria-label`, ở góc phải phần đầu |
| Chữ | Tiêu đề, mô tả, nhãn nút qua i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

**Không lồng dialog trong dialog.** Tầng lồng nhau ở [`../COMPONENTS.md`](../COMPONENTS.md) §2.4 đặt `Dialog` ở tầng cao nhất, nên nó không được chứa một `Dialog` khác. Ngoại lệ duy nhất là [`ConfirmDialog.md`](./ConfirmDialog.md) hỏi xác nhận cho một thao tác phát sinh từ dialog đang mở — và ngay cả khi đó, hai lớp là tối đa tuyệt đối. Bẫy focus lồng ba tầng gần như luôn hỏng.

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `open` | input | `boolean` | `false` | |
| `size` | input | `'sm' \| 'md' \| 'lg' \| 'full'` | `'md'` | |
| `title` | input | `string` | — | Bắt buộc. Không có mặc định |
| `description` | input | `string \| null` | `null` | |
| `closable` | input | `boolean` | `true` | `false` chỉ ẩn nút đóng và chặn bấm-ra-ngoài; **Escape vẫn hoạt động** |
| `dismissOnBackdrop` | input | `boolean` | `true` | Tự động thành `false` khi `dirty` là `true` |
| `dirty` | input | `boolean` | `false` | Form đã có thay đổi chưa lưu. Bật cờ này thì bấm ra ngoài và Escape sẽ hỏi xác nhận |
| `loading` | input | `boolean` | `false` | |
| `loadingBlocksClose` | input | `boolean` | `false` | Bật khi `loading` là do một thao tác ghi |
| `error` | input | `string \| null` | `null` | |
| `closed` | output | `void` | — | Phát sau khi đã trả focus về nơi mở |
| `dismissAttempted` | output | `void` | — | Phát khi người dùng cố đóng lúc `dirty` — trang cha quyết định hỏi gì |

Nội dung vào qua ba slot: phần đầu (mặc định là tiêu đề + mô tả), phần thân, phần chân. Slot chân **không có nút mặc định** — mỗi dialog tự khai nút của mình, vì thứ tự và nhãn nút là quyết định của từng ca.

`Dialog` là component **dumb**: nó không tự đóng mình. Nó **phát** `closed` và trang cha đặt `open` về `false`. Tự đóng làm trạng thái nằm ở hai nơi và chúng sẽ lệch.

## Do / Don't

- ✅ Luôn có tiêu đề, và tiêu đề nói **việc gì đang xảy ra**: "Sửa người dùng", không phải "Biểu mẫu".
- ✅ Đặt focus vào ô nhập đầu tiên khi mở.
- ✅ Trả focus về nơi mở khi đóng.
- ✅ Bật `dirty` khi form có thay đổi.
- ✅ Nút chính bên phải ở desktop, lên trên ở màn nhỏ.
- ❌ Không lồng quá hai lớp dialog.
- ❌ Không nhồi form dài vào dialog. Dùng một trang.
- ❌ Không đặt focus vào nút đóng khi mở.
- ❌ Không chặn Escape, kể cả khi `closable` là `false`.
- ❌ Không dùng dialog để báo thành công.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Hộp hỏi "bạn có thay đổi chưa lưu" khi `dirty` do `Dialog` tự dựng hay do trang cha dựng? Tự dựng thì nhất quán nhưng ghép cứng `Dialog` vào `ConfirmDialog`; để trang cha thì mỗi màn có thể quên | Người dựng component |
| 2 | Ngưỡng "form dài quá thì dùng trang" đặt bằng số trường hay bằng chiều cao? Số trường dễ kiểm hơn nhưng thô | Sau khi có vài màn form thật |
| 3 | Có cần biến thể trượt từ mép phải (drawer) không? Nó hợp với xem chi tiết mà vẫn thấy danh sách. Rủi ro: thêm một cách hiển thị nữa và mỗi màn tự chọn khác nhau | Dự án đầu tiên có nhu cầu |
