---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Drawer

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** **bọc PrimeNG**. Cùng nhóm "khó" với [`Dialog.md`](./Dialog.md) theo [`../COMPONENTS.md`](../COMPONENTS.md) §4: bẫy focus, khôi phục focus khi đóng, khoá cuộn nền. Đúng được toàn bộ ca biên là hàng trăm dòng và rất nhiều lần sai.

---

## Mục đích

Mở một tấm trượt từ cạnh màn hình để xem hoặc sửa nhanh một bản ghi, mà danh sách phía sau vẫn còn nguyên.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Xem chi tiết một hàng trong [`DataTable.md`](./DataTable.md) mà không rời danh sách | 🛑 Việc cần **dứt điểm** trước khi làm tiếp: xác nhận xoá, form tạo mới → [`Dialog.md`](./Dialog.md). Xem ranh giới dưới đây |
| Sửa nhanh vài trường của một bản ghi, rồi sửa tiếp bản ghi kế bên | 🛑 Nội dung dài, nhiều nhóm trường, cần đọc kỹ → một **màn riêng**, không phải lớp nổi |
| Panel lọc nhiều trường của [`Toolbar.md`](./Toolbar.md) | 🛑 Danh sách hành động ngắn → [`Menu.md`](./Menu.md) |
| Dạng drawer của [`Sidebar.md`](./Sidebar.md) ở màn nhỏ | 🛑 Một dòng chú → [`Tooltip.md`](./Tooltip.md) |

### Ranh giới với `Dialog` — câu hỏi quyết định

> **Người dùng có cần nhìn thấy thứ phía sau trong lúc thao tác không?**
>
> Có → `Drawer`. Không → `Dialog`.

Đó là khác biệt thật, và nó kéo theo mọi thứ còn lại: `Dialog` nằm giữa màn hình vì nó muốn bạn nhìn nó và chỉ nó; `Drawer` nép vào cạnh vì nó muốn bạn còn thấy danh sách. Một `Dialog` đặt sát mép màn hình vẫn là `Dialog`, và một `Drawer` phủ kín màn hình thì đã không còn là `Drawer`.

Hệ quả: `Drawer` **không dùng cho thao tác phá huỷ**. Xác nhận xoá cần cắt đứt ngữ cảnh để người dùng dừng lại và đọc — đó đúng là việc của [`ConfirmDialog.md`](./ConfirmDialog.md).

`Drawer` là component riêng, không phải biến thể của `Dialog`: `inspect` không bẫy focus và không mang `aria-modal`, ngược hẳn định nghĩa `Dialog`; mục lục [`../COMPONENTS.md`](../COMPONENTS.md) §3 giữ hai dòng.

## Biến thể

| Biến thể | Nền phía sau | Dùng khi |
| --- | --- | --- |
| `inspect` | **Không có lớp phủ.** Danh sách phía sau vẫn bấm được, chọn hàng khác thì drawer đổi nội dung theo | **Mặc định.** Xem chi tiết, duyệt qua nhiều bản ghi liên tiếp |
| `edit` | Có lớp phủ `--color-overlay`, nền phía sau khoá lại | Khi drawer chứa form đang sửa — tránh mất dữ liệu đang gõ vì một cú bấm nhầm ra ngoài |

**Biến thể `inspect` là lý do component này tồn tại.** Nó cho phép bấm hàng này xem, bấm hàng kia xem tiếp, không phải đóng mở liên tục. Đó là thao tác thật của người kiểm tra dữ liệu, và `Dialog` không làm được vì nó chặn. Chọn hàng khác thì nội dung **đổi tại chỗ**, không đóng rồi mở lại; đang tải dở mà chọn tiếp thì màn bỏ kết quả của request cũ — luật ở [`Autocomplete.md`](./Autocomplete.md) §API.

Cái giá của `inspect`, phải nói rõ: nó **không** bẫy focus (nền sau vẫn thao tác được thì bẫy focus là mâu thuẫn), nên nó cũng **không** mang `aria-modal`. Hai biến thể vì vậy khác nhau cả về ngữ nghĩa ARIA — xem mục Accessibility. Việc `p-drawer` có cho tắt hai thứ đó không **chưa được xác minh** — §Cần chốt 2.

| Vị trí neo | Dùng khi |
| --- | --- |
| `right` | **Mặc định.** Chi tiết bản ghi, panel lọc — hướng đọc tự nhiên đi từ danh sách bên trái sang |
| `left` | Chỉ cho dạng drawer của `Sidebar` ở màn nhỏ, để khớp vị trí nó vốn đứng |
| `bottom` | Chỉ ở màn < `$bp-sm`, nơi tấm trượt ngang không đủ bề rộng |

## Kích thước

`Drawer` **không** dùng thang `--size-control-*`. Cỡ ở đây là **bề rộng**, và chiều cao luôn là trọn màn hình.

| Cỡ | Bề rộng | Dùng khi |
| --- | --- | --- |
| `sm` | `--layout-drawer-w-sm` | Xem nhanh vài trường |
| `md` | `--layout-drawer-w-md` | **Mặc định.** Chi tiết bản ghi, form ngắn |
| `lg` | `--layout-drawer-w-lg` | Panel lọc nhiều trường, nội dung có bảng con |

Ba bậc là bí danh của thang `--layout-dialog-w-*` ([`../DESIGN.md`](../DESIGN.md) §6.1) — cùng họ lớp nổi có đầu–thân–chân, một thang, không đẻ bậc mới.

Trần cứng: `min(<bề rộng cỡ>, 92vw)`. Một drawer rộng hơn 92% màn hình đã là một `Dialog` toàn màn, và lúc đó nên dùng đúng component đó.

Ba phần, giống `Dialog`: đầu (tiêu đề + nút đóng), thân (cuộn được), chân (hành động). **Chỉ thân cuộn** — đầu và chân đứng yên, vì nút đóng phải luôn với tới được và nút Lưu không được trôi mất ở một form dài.

Bo góc: `--radius-0` ở cạnh dính mép màn hình, `--radius-lg` ở hai góc còn lại. Đổ bóng `--shadow-4`, lớp `--z-dialog`.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Nền `--color-surface`, viền `--color-border` ở cạnh giáp nội dung, `--shadow-4`. Trượt bằng `transform` trong `--dur-base` với `--ease-standard` — **một thời lượng và một đường cong cho cả chiều vào lẫn chiều ra** | Có |
| `hover` | **Không áp dụng cho chính tấm drawer.** Nó là vùng chứa; hover thuộc về từng control bên trong | — |
| `focus-visible` | **Không áp dụng cho chính tấm drawer** — nó không nhận focus. Nhưng thứ tự Tab bên trong là bắt buộc: nút đóng → nội dung theo thứ tự đọc → nhóm hành động ở chân | — |
| `active` | **Không áp dụng.** Không có gì để nhấn xuống | — |
| `disabled` | **Không áp dụng cho chính tấm drawer.** Từng control con nhận `disabled` thật. 🛑 Không phủ một lớp `pointer-events: none` lên cả drawer — control biến mất khỏi thứ tự Tab mà trình đọc màn hình không biết vì sao | — |
| `loading` | Nội dung chưa về: màn đặt [`SkeletonLoader.md`](./SkeletonLoader.md) theo đúng hình dạng nội dung sắp tới vào slot thân; `Drawer` đặt `aria-busy="true"` trên thân. **Đầu và chân dựng ngay** — tiêu đề và nút đóng phải có mặt từ khung hình đầu tiên, nếu không người dùng mở nhầm thì không có đường thoát | Có |
| `error` | Tải nội dung hỏng: màn đặt [`EmptyState.md`](./EmptyState.md) biến thể `error` kèm nút "Thử lại" vào slot thân. **Drawer không tự đóng** — đóng đi thì người dùng không biết vừa có chuyện gì | Có |
| `empty` | Bản ghi không còn tồn tại (vừa bị người khác xoá): màn đặt `EmptyState` nói rõ điều đó vào slot thân, chỉ có nút "Đóng". Phân biệt hẳn với `error` — một cái là hỏng, một cái là mất | Có |

### Chuyển động vào/ra — một bộ giá trị, đến từ ba cơ chế

Chiều vào và chiều ra dùng **chung** `--dur-base` và `--ease-standard`. Bất đối xứng "vào chậm hơn ra" đã bị bỏ có chủ ý: nền bọc thư viện chở hoạt ảnh vào/ra qua **một** input duy nhất, nên hai đường cong là thứ nền này không dựng được — [`../../adr/0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md`](../../adr/0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md) quyết định 5. `--ease-standard` là token [`../DESIGN.md`](../DESIGN.md) §7 giao cho vai "mặc định cho mọi chuyển tiếp", nên nó là token đúng khi một đường cong phải phục vụ cả hai hướng; `--ease-decelerate` và `--ease-accelerate` chia theo hướng nên không dùng được ở đây.

🛑 **Người dựng phải biết: chuyển động của component này đến từ BA cơ chế, không phải một.** Đây là chỗ phức tạp nhất spec để lại, và giấu nó đi thì người sửa sau sẽ đổi một cơ chế rồi tưởng đã đổi hết.

| Cơ chế | Chở khoản nào | Token tới bằng đường nào |
| --- | --- | --- |
| Hoạt ảnh Angular — một input `transitionOptions` | Nhịp vào/ra của chính tấm trượt | **Không** giải `var()`. Số đi qua một hằng số có tên cạnh lớp bọc, nêu đích danh `--dur-base` — luật **F37** ([`../../RULES.md`](../../RULES.md) §7) |
| CSS tĩnh của thư viện — `@primeuix/styles/drawer` § `transition: transform 0.3s` trên `.p-drawer` | Thời lượng của chính phép trượt `transform` | Một con số trần, không qua biến nào. Phải **đè** bằng một khối override trong `src/FE/src/styles/_thu-vien.scss`, đặt `--dur-base` |
| CSS tĩnh của thư viện — § `transition: opacity 400ms cubic-bezier(0.25, 0.8, 0.25, 1)` trên `.p-drawer-full` | Mờ dần của chế độ toàn màn thư viện | Cùng đường đè như trên |

Hai khối override là CSS, nên chúng thuộc `_thu-vien.scss` chứ **không** thuộc hằng số TypeScript — quyết định 8 của ADR trên, và là ngoại lệ có tên của luật "không khai thời lượng thư viện theo từng component".

⚠️ **Chế độ toàn màn của thư viện không phải một biến thể của spec này.** §Biến thể chỉ có `inspect` và `edit`, và §Kích thước chặn trần ở `min(<bề rộng cỡ>, 92vw)` — một tấm trượt phủ kín màn đã là `Dialog` toàn màn. Khối override thứ hai vì thế canh một chế độ Core hiện **không** vẽ tới; nó tồn tại để con số `400ms` không nằm ngoài hệ nếu có ngày ai bật chế độ đó. Xem §Cần chốt 3.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-surface-2`, `--color-border`, `--color-border-subtle`, `--color-text`, `--color-text-muted`, `--color-overlay`, `--color-focus` |
| Chữ | `--fs-xs`, `--fs-sm`, `--fs-lg`, `--fw-semibold`, `--lh-snug` |
| Khoảng cách | `--sp-4`, `--sp-5`, `--sp-6` |
| Hình dạng | `--radius-0`, `--radius-lg`, `--border-w`, `--border-w-strong` |
| Kích thước | `--layout-drawer-w-sm`, `--layout-drawer-w-md`, `--layout-drawer-w-lg` |
| Bóng, lớp | `--shadow-4`, `--z-dialog`, `--z-backdrop` |
| Chuyển động | `--dur-base`, `--ease-standard` |

🛑 **`--dur-base` ở dòng cuối bảng đi tới đích bằng hai đường khác nhau, và một trong hai không phải CSS.** Phần nhịp vào/ra sống trong một hằng số TypeScript; phần trượt `transform` sống trong một khối override CSS. Bảng này vẫn giữ token vì `Drawer` vẫn **quyết** giá trị — khác hẳn [`Tooltip.md`](./Tooltip.md), nơi bảng token bỏ hẳn nhóm chuyển động vì spec ở đó không đặt được số nào cả.

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-lg` | Neo phải, bề rộng theo cỡ. Biến thể `inspect` không có lớp phủ |
| `$bp-md` … `$bp-lg` | Giữ nguyên; cỡ `lg` co xuống 92vw nếu cần |
| < `$bp-md` | **Biến thể `inspect` chuyển thành `edit`** — có lớp phủ, có bẫy focus. Ở màn hẹp drawer che gần hết danh sách rồi, nên lời hứa "vẫn thấy phía sau" không còn đúng, và giữ nó là nói dối |
| < `$bp-sm` | Neo **đáy**, cao tối đa 85vh, có vạch kéo ở đầu. Trượt từ đáy đặt nút hành động vào vùng ngón cái với tới. **Miễn trừ:** `position: left` giữ neo trái ở mọi ngưỡng — drawer điều hướng của [`Sidebar.md`](./Sidebar.md) trượt từ trái, không đổi sang `bottom` |

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Vai trò | `role="dialog"`. Biến thể `edit` thêm `aria-modal="true"`; biến thể `inspect` **không** mang thuộc tính đó — nền sau vẫn thao tác được thì khai `aria-modal` là khai sai |
| Nhãn | `aria-labelledby` trỏ vào tiêu đề ở đầu drawer. Tiêu đề phải nói **bản ghi nào**, không phải "Chi tiết" |
| Bẫy focus | Chỉ ở `edit`. `Tab` vòng trong drawer, không thoát ra nền sau |
| Focus khi mở | Vào **nút đóng** ở `inspect`; vào **ô nhập đầu tiên** ở `edit` |
| Focus khi đóng | Trả về đúng phần tử đã mở drawer — thường là một [`IconButton.md`](./IconButton.md) trong hàng bảng. Đây là khoản hay bị bỏ nhất |
| Bàn phím | `Escape` đóng. Ở `edit` có dữ liệu chưa lưu thì `Escape` **hỏi xác nhận** trước, không đóng thẳng |
| Cuộn nền | Khoá ở `edit`, giữ nguyên ở `inspect` |
| Đổi bản ghi khi đang mở | Ở `inspect`, chọn hàng khác thì nội dung đổi — phải báo qua `aria-live="polite"`, nếu không người dùng trình đọc màn hình không biết drawer vừa đổi nội dung |
| Chữ | Đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `open` | input | `boolean` | `false` | Trang cha giữ nguồn sự thật |
| `variant` | input | `'inspect' \| 'edit'` | `'inspect'` | Mặc định là dạng nhẹ nhất, sai theo hướng an toàn |
| `position` | input | `'right' \| 'left' \| 'bottom'` | `'right'` | |
| `size` | input | `'sm' \| 'md' \| 'lg'` | `'md'` | |
| `title` | input | `string` | — | **Bắt buộc.** Không có mặc định; một lớp nổi không tiêu đề là một lớp nổi vô danh |
| `loading` | input | `boolean` | `false` | |
| `dirty` | input | `boolean` | `false` | Có dữ liệu chưa lưu. Khi `true`, mọi đường đóng đều đi qua bước hỏi xác nhận |
| `closed` | output | `void` | — | Phát mọi lần drawer đóng: nút đóng, `Escape`, bấm lớp phủ |
| `confirmDiscard` | output | `void` | — | Phát khi người dùng chọn bỏ thay đổi ở bước hỏi xác nhận |

Nội dung vào qua ba slot: đầu, thân, chân. Khác [`Menu.md`](./Menu.md) — ở đó nội dung phải vào qua mảng vì component cần biết danh sách mục để làm phím mũi tên; ở đây không có hành vi nào phụ thuộc nội dung, nên slot là đúng. Nội dung tự dựng của `loading`, `error`, `empty` cũng vào qua slot thân — `Drawer` ở tầng bọc nên không import chúng ([`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5).

`Drawer` là component **dumb** — nó không tự tải bản ghi ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

## Do / Don't

- ✅ Dùng `inspect` khi người dùng sẽ xem nhiều bản ghi liên tiếp.
- ✅ Dựng đầu và chân ngay từ khung hình đầu tiên, kể cả khi thân còn đang tải.
- ✅ Hỏi xác nhận trước khi đóng một drawer có dữ liệu chưa lưu.
- ✅ Trả focus về đúng nút đã mở nó.
- ❌ Không dùng `Drawer` cho xác nhận xoá. Đó là việc của `ConfirmDialog`.
- ❌ Không cho thân, đầu và chân cùng cuộn.
- ❌ Không mở drawer chồng lên drawer. Cần đi sâu một cấp nữa thì đó là một màn riêng.
- ❌ Không khai `aria-modal` cho biến thể `inspect`.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Có cho kéo đổi bề rộng không? Người dùng bảng nhiều cột hay muốn drawer hẹp lại. Đổi lại là một thao tác chuột nữa phải có đường đi bằng bàn phím | Sau F3 — dự án hạ nguồn đầu tiên có bảng nhiều cột kèm drawer |
| 2 | Cần xác minh `p-drawer` của PrimeNG 20 cho phép tắt bẫy focus và bỏ `aria-modal` cho `inspect`. Không cho phép thì `inspect` **tự dựng** phần khung, `edit` vẫn bọc — chưa kiểm, không ghi trước | `frontend-expert` ở F1, khi có `src/` |
| 3 | Khối override cho chế độ toàn màn (`.p-drawer-full`, §Trạng thái) có đáng dựng không, khi §Biến thể và §Kích thước của chính spec này **không** cho `Drawer` phủ kín màn? Dựng thì `_thu-vien.scss` mang một khối canh thứ chưa ai vẽ tới — và một khối override không có gì trên màn để đối chiếu là khối sẽ mục ruỗng trong im lặng. Không dựng thì con số `400ms` nằm ngoài hệ, chờ ngày có người bật chế độ đó mà không biết nó không theo token nào | `frontend-expert` khi dựng `Drawer`, dựa trên việc lớp bọc có phơi chế độ đó ra hay khoá hẳn. Khoá hẳn thì câu hỏi này tự đóng |
