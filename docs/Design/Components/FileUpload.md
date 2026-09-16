---
kind: luat
scope: core
verified: chua-doi-chieu
---

# FileUpload

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** **bọc PrimeNG**. Theo tiêu chí ở [`../COMPONENTS.md`](../COMPONENTS.md) §4, kéo thả tệp, đọc tệp và tiến trình tải lên là nhóm "khó": nhiều API trình duyệt, nhiều ca biên bảo mật, và một loạt hành vi kéo thả rất dễ sai (sự kiện `dragleave` bắn khi con trỏ đi qua phần tử con, chẳng hạn).

---

## Mục đích

Cho người dùng chọn tệp — bằng nút hoặc kéo thả — kiểm tra hợp lệ, và hiện tiến trình tải lên.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Đính kèm tài liệu vào một bản ghi | 🛑 Nhập dữ liệu từ tệp bảng tính → đó là một luồng nhiều bước (chọn tệp → đối chiếu cột → xem trước → xác nhận), thuộc màn riêng chứ không phải một control |
| Tải ảnh đại diện | 🛑 Chọn tệp đã có trên máy chủ → đó là một trình duyệt tệp, component khác |
| Tải một loạt minh chứng | 🛑 Chỉ hiện danh sách tệp đã có, không tải thêm → dùng [`Table.md`](./Table.md) hoặc một danh sách thường |

## Biến thể

| Biến thể | Hình thức | Dùng khi |
| --- | --- | --- |
| `dropzone` | Vùng chữ nhật có viền đứt, có icon và chữ hướng dẫn, kéo thả được | **Mặc định.** Khi tải tệp là hành động chính của vùng đó |
| `button` | Chỉ một nút "Chọn tệp"; danh sách tệp hiện bên dưới | Khi tải tệp là một trường trong form dài, và một dropzone lớn sẽ phá nhịp form |
| `avatar` | Vùng tròn hiện ảnh hiện tại, phủ lớp "Đổi ảnh" khi hover | Riêng cho ảnh đại diện |

**Biến thể `dropzone` vẫn phải có nút bấm được bên trong.** Kéo thả **không** dùng được bằng bàn phím và rất khó với người dùng có hạn chế vận động. Một vùng chỉ kéo thả được là một chức năng loại trừ người dùng.

**Mọi nút và thanh tiến trình của component này là phần cấu trúc của lớp bọc** — nút "Chọn tệp", nút xoá / huỷ / "Thử lại" trên từng dòng tệp, thanh tiến trình theo dòng. Thư viện vẽ chúng, tạo hình bằng token theo hình thức của [`Button.md`](./Button.md) và [`ProgressBar.md`](./ProgressBar.md); chúng báo ra ngoài qua output ở mục API, không qua template ([`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5).

## Kích thước

| Khoản | Giá trị |
| --- | --- |
| Chiều cao tối thiểu dropzone | `--file-upload-dropzone-h` — token tầng 3, số thật ở [`../DESIGN.md`](../DESIGN.md) §6.5 |
| Đệm dropzone | `--sp-8` |
| Viền dropzone | `--border-w-strong` nét đứt, `--color-border` |
| Bo góc dropzone | `--radius-lg` |
| Nền dropzone | `--color-surface` |
| Icon dropzone | `pi-upload`, `--icon-xl`, màu `--color-text-muted` |
| Chiều cao một dòng tệp | `--size-control-lg` (42px) |
| Đệm dòng tệp | `--sp-4` |
| Khe giữa các dòng tệp | `--sp-3` |
| Cỡ chữ tên tệp | `--fs-sm` |
| Cỡ chữ dung lượng và trạng thái | `--fs-xs`, `--color-text-muted` |

**Viền dùng `--color-border-strong` với nét đứt.** Nét đứt là quy ước phổ biến cho "thả vào đây"; độ dày `--border-w-strong` là vì vùng này **tương tác được** — cùng nhóm với ô nhập trong luật ba bậc viền ở [`../DESIGN.md`](../DESIGN.md) §2.3.

## Trạng thái

Component này có nhiều trạng thái nhất trong thư viện, vì nó có trạng thái ở **hai mức**: vùng thả và từng tệp.

### Mức vùng thả

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Viền đứt `--color-border`, nền `--color-surface`, icon và chữ hướng dẫn `--color-text-muted` | Có |
| `hover` | Viền `--color-border-strong`, nền `--color-surface-2`. Bọc `@media (hover: hover)` | Có |
| `focus-visible` | Nút bên trong nhận vòng focus. Vùng thả **không** tự nhận focus — nó không phải control | Có |
| `active` | **Đang kéo tệp qua vùng:** viền `--color-brand` liền nét, nền `--color-brand-subtle`, chữ đổi thành "Thả tệp vào đây". Đây là trạng thái quan trọng nhất — không có nó người dùng không biết mình đã thả đúng chỗ chưa | Có |
| `disabled` | Viền và chữ `--color-text-disabled`, nền `--color-surface-3`, không nhận kéo thả, nút bên trong `disabled` | Có |
| `loading` | Có ít nhất một tệp đang tải: vùng thả **vẫn nhận thêm tệp** trừ khi đã đủ số lượng tối đa. Tiến trình hiện ở từng dòng tệp, không ở vùng thả | Có |
| `error` | Tệp bị từ chối ngay khi thả (sai loại, quá lớn): viền `--color-danger-border`, một dòng lỗi dưới vùng thả nói **tệp nào** và **vì sao**. Trạng thái này tự hết sau lần thả hợp lệ tiếp theo | Có |
| `empty` | Chưa có tệp nào — đây chính là trạng thái `default` của vùng thả. Danh sách tệp không vẽ gì | Có |

### Mức một dòng tệp

| Trạng thái | Xử lý |
| --- | --- |
| `queued` | Icon `pi-file`, tên tệp, dung lượng, nút xoá khỏi hàng đợi |
| `uploading` | Thanh tiến trình mảnh dưới tên tệp, phần trăm, nút huỷ. Không xác định được phần trăm thì dùng thanh chạy vô định. Tải nhiều tệp cùng lúc thì **mỗi tệp một thanh** — không có thanh tổng, vì thanh tổng giấu mất tệp nào đang hỏng |
| `done` | Icon `pi-check-circle` màu `--color-success`, nút xoá |
| `failed` | Icon `pi-times-circle` màu `--color-danger`, câu lỗi ngắn, nút "Thử lại" và nút xoá |
| `rejected` | Tệp không qua kiểm tra: nền `--color-danger-bg`, câu lỗi nói rõ luật bị vi phạm, chỉ có nút xoá |

**Tệp bị từ chối vẫn hiện trong danh sách chứ không biến mất.** Người dùng kéo năm tệp và ba cái biến mất không dấu vết sẽ tưởng thao tác kéo thả bị lỗi. Hiện chúng kèm lý do là cách duy nhất họ biết phải sửa gì.

## Kiểm tra hợp lệ

Kiểm ở **cả hai phía** và spec này chỉ nói phía trình duyệt.

| Luật | Cách kiểm | Câu lỗi phải nói gì |
| --- | --- | --- |
| Loại tệp | Đuôi tệp **và** kiểu MIME | Nói rõ loại nào được chấp nhận |
| Dung lượng một tệp | Thuộc tính `size` | Nói giới hạn **và** dung lượng của tệp bị từ chối |
| Tổng dung lượng | Cộng dồn | Nói tổng hiện tại và giới hạn |
| Số lượng tệp | Đếm | Nói giới hạn |
| Trùng tên | So tên với danh sách đã có | Hỏi ghi đè hay đổi tên |

🛑 **Kiểm ở trình duyệt là để trải nghiệm tốt, KHÔNG phải để bảo mật.** Mọi luật ở đây bỏ qua được bằng cách gọi thẳng API. Máy chủ phải kiểm lại toàn bộ, và phải kiểm **nội dung** tệp chứ không chỉ đuôi — một tệp thực thi đổi tên thành `.pdf` sẽ qua mọi kiểm tra phía trình duyệt. Đây là luật của tầng backend và nó nằm ngoài phạm vi khu Design; ghi ở đây để người dựng giao diện không tưởng rằng mình đã chặn được gì.

**Thuộc tính `accept` của phần tử chọn tệp chỉ là gợi ý.** Nó lọc danh sách trong hộp thoại chọn tệp của hệ điều hành, nhưng người dùng chuyển sang "Tất cả tệp" là chọn được bất cứ gì. Luôn kiểm lại sau khi nhận.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-surface-2`, `--color-surface-3`, `--color-text`, `--color-text-muted`, `--color-text-disabled`, `--color-border`, `--color-border-strong`, `--color-brand`, `--color-brand-subtle`, `--color-success`, `--color-danger`, `--color-danger-bg`, `--color-focus` |
| Chữ | `--fs-xs`, `--fs-sm`, `--fw-medium`, `--fw-regular`, `--lh-snug`, `--lh-normal` |
| Khoảng cách | `--sp-3`, `--sp-4`, `--sp-6`, `--sp-8` |
| Hình dạng | `--radius-lg`, `--radius-sm`, `--radius-pill`, `--border-w`, `--border-w-strong` |
| Kích thước | `--size-control-lg`, `--size-control-sm`, `--icon-md`, `--icon-xl`, `--file-upload-dropzone-h` |
| Chuyển động | `--dur-fast`, `--dur-base`, `--ease-standard` |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-md` | Dropzone chiều cao đầy đủ; dòng tệp một hàng: icon · tên · dung lượng · trạng thái · nút |
| < `$bp-md` | Dropzone thấp hơn; chữ hướng dẫn đổi thành "Chọn tệp" — **kéo thả gần như không dùng được trên di động** |
| < `$bp-xs` | Dòng tệp xuống hai hàng: tên ở hàng trên, dung lượng và trạng thái ở hàng dưới; nút dồn về phải |

**Trên di động, biến thể `dropzone` tự hạ thành `button`.** Không có thao tác kéo thả tệp trên phần lớn thiết bị cảm ứng, nên một vùng thả to là chỗ trống vô nghĩa chiếm nửa màn hình.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Phần tử gốc | `<input type="file">` thật, ẩn về mặt hình ảnh nhưng **không** dùng `display: none` — phần tử ẩn kiểu đó không nhận được focus. Dùng kỹ thuật ẩn giữ được khả năng focus |
| Nút | `<button>` thật, gắn với input tệp qua `<label>` hoặc bằng cách gọi `click()` |
| Nhãn | Input tệp có nhãn nói rõ đang tải gì lên ("Tải tài liệu đính kèm") |
| Luật hợp lệ | Nói **trước** khi người dùng chọn, không chỉ sau khi từ chối: loại tệp và dung lượng tối đa hiện ngay trong vùng thả. Gắn qua `aria-describedby` |
| Kéo thả | **Luôn phải có đường thay thế bằng bàn phím.** Kéo thả là bổ sung, không bao giờ là con đường duy nhất — WCAG 2.2 SC 2.5.7 |
| Tiến trình | Thanh tiến trình có `role="progressbar"` với `aria-valuenow` / `aria-valuemin` / `aria-valuemax`; không xác định được thì bỏ `aria-valuenow` |
| Thay đổi trạng thái | Danh sách tệp nằm trong vùng `aria-live="polite"`: thêm tệp, tải xong, tải lỗi đều được đọc lên |
| Lỗi | Câu lỗi gắn với dòng tệp qua `aria-describedby`; lỗi mức vùng thả dùng `role="alert"` |
| Nút xoá / huỷ | `aria-label` nêu **tên tệp**: "Xoá tệp hop-dong.pdf", không phải "Xoá" |
| Chữ | Qua i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `variant` | input | `'dropzone' \| 'button' \| 'avatar'` | `'dropzone'` | |
| `accept` | input | `ReadonlyArray<string>` | `[]` | Danh sách đuôi/MIME. Rỗng = chấp nhận mọi loại |
| `maxFileSize` | input | `number \| null` | `null` | Byte |
| `maxTotalSize` | input | `number \| null` | `null` | Byte |
| `maxFiles` | input | `number` | `1` | Mặc định **một** — sai theo hướng an toàn; nhiều tệp phải là lựa chọn có ý thức |
| `files` | input | `ReadonlyArray<UploadItem>` | `[]` | Danh sách hiện tại **và trạng thái từng tệp**, do nơi gọi quản lý |
| `disabled` | input | `boolean` | `false` | |
| `filesSelected` | output | `ReadonlyArray<File>` | — | Tệp đã qua kiểm tra phía trình duyệt |
| `filesRejected` | output | `ReadonlyArray<{ file: File; reason: string }>` | — | **Phát riêng**, không lẫn vào `filesSelected` |
| `removeRequested` | output | `string` | — | Id tệp |
| `retryRequested` | output | `string` | — | Id tệp |
| `cancelRequested` | output | `string` | — | Id tệp đang tải |

**`UploadItem`:** chữ ký đầy đủ ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9. `progress` nhận `null` khi chưa bắt đầu **hoặc không đo được** — tải qua một proxy không trả tiến độ là chuyện thật, và lúc đó thanh tiến trình của dòng chuyển sang hình thức biến thể `indeterminate` của [`ProgressBar.md`](./ProgressBar.md) chứ không bịa một con số.

**Component KHÔNG tự tải tệp lên.** Nó phát `filesSelected` và nhận lại danh sách kèm trạng thái qua `input()`. Đây là điều giữ nó **dumb** ([`../COMPONENTS.md`](../COMPONENTS.md) §5), và nó cũng đúng về mặt kiến trúc: cách tải lên (một request hay nhiều phần, có chữ ký tạm hay không, tải lại từ đâu khi lỗi) là quyết định của tầng dữ liệu, không phải của một control giao diện.

**`filesRejected` phát riêng** vì nơi gọi cần biết có tệp bị từ chối để hiện thông báo — trộn vào một luồng thì mọi nơi gọi phải tự lọc, và sẽ có nơi quên.

## Do / Don't

- ✅ Luôn có nút bấm được, kể cả ở biến thể `dropzone`.
- ✅ Nói luật hợp lệ **trước** khi người dùng chọn.
- ✅ Giữ tệp bị từ chối trong danh sách kèm lý do.
- ✅ Trạng thái "đang kéo qua" phải rõ ràng.
- ✅ `aria-label` của nút xoá nêu tên tệp.
- ❌ Không coi kiểm tra phía trình duyệt là bảo mật.
- ❌ Không tin `accept`.
- ❌ Không ẩn input tệp bằng `display: none`.
- ❌ Không để `FileUpload` tự gọi API tải lên.
- ❌ Không mặc định cho phép nhiều tệp.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Có hỗ trợ tải lại từ chỗ dở khi mất mạng giữa chừng không? Nó cần hạ tầng tải theo phần ở máy chủ và làm API phức tạp hẳn | Sau F3 — khi chốt tầng lưu trữ tệp ở pha B4 |
| 2 | Có xem trước ảnh ngay trong danh sách không? Hữu ích cho ảnh nhưng cần đọc tệp ra `data:` và tốn bộ nhớ với tệp lớn | Sau F3 — dự án hạ nguồn đầu tiên tải nhiều ảnh |
| 3 | Biến thể `avatar` có cần cắt ảnh không? Cắt ảnh là một component riêng khá lớn | Sau F3 — khi hồ sơ có ảnh đại diện (phiên chưa mang ảnh, [`Topbar.md`](./Topbar.md) §API) |
