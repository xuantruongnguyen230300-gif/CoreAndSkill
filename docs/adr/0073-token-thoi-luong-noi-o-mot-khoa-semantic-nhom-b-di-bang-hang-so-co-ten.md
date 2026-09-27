---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0073 — Token thời lượng nối ở **một khoá semantic** của preset; nhóm hoạt ảnh Angular đi bằng **hằng số có tên** truy ngược được về token; `Dialog` và `Drawer` bỏ bất đối xứng vào/ra

> **Trạng thái:** Đã chấp nhận (2026-09-23) · Sửa một phần bởi [ADR-0075](0075-ba-component-lop-noi-vao-nhom-b-va-bac-thang-chon-theo-vai.md) (2026-09-23)
>
> Tám quyết định đầu **còn hiệu lực nguyên vẹn**. Quyết định 9 chỉ còn phần phân loại (*"spec thiếu, không phải code lệch"*); phần phương thuốc — để `Menu`/`DatePicker`/`Autocomplete` nhận mặc định thư viện và viết một câu nói rõ `.12s` là trùng số — **hết hiệu lực**: ba component đó nay thuộc nhóm B và nối về vai `--dur-base`. ⚠️ §Hệ quả tiêu cực gạch 2 nêu rủi ro *"giật ở `120ms`"*; lượt rà mà chính nó đặt hàng đã bác rủi ro đó — xem ADR-0075 vế 2. Cả hai chỗ **giữ nguyên văn** theo luật D45.

## Bối cảnh

`design-expert` rà 12 component bọc PrimeNG và tách chúng làm hai nhóm theo **token có tới được chuyển động hay không**. Kiến trúc sư đối chiếu lại bằng cách đọc thẳng gói đang cài, ngày 2026-09-23:

| Đo gì | Kết quả |
| --- | --- |
| Thang thời lượng của hệ | `src/FE/src/styles/_tokens.scss` khai `--dur-instant` `--dur-fast` `--dur-base` `--dur-slow` `--dur-loop`; `--dur-fast` là `120ms` |
| `src/FE/src/styles` có khai biến thời lượng nào của thư viện không | Không. Không dòng `--p-*-transition-duration` nào |
| Chuỗi token của nhóm A | Mỗi sub-preset component khai `root.transitionDuration` trỏ tới **một trong hai** tên: `{transition.duration}` (`datatable`, `paginator`, `menu`, `toast`) hoặc `{form.field.transition.duration}` (`checkbox`, `autocomplete`) |
| Hai tên đó trỏ đâu | `@primeuix/themes/aura/base` khai `semantic.formField.transitionDuration = "{transition.duration}"` — tức nó **cũng** trỏ về tên thứ nhất. Tên thứ nhất là `semantic.transitionDuration`, và nó mang một chuỗi trần: `"0.2s"` |
| Vậy nhóm A có bao nhiêu chỗ phải sửa | **Một.** Cả hai nhánh hội tụ về `semantic.transitionDuration` |
| `Tabs` và `FileUpload` | **Chưa tồn tại** trong `src/FE/src/app/shared/ui/`, và không tệp nào import `primeng/tabs` hay `primeng/fileupload` |
| Nhóm B — `Dialog`, `Drawer` | `primeng/dialog` và `primeng/drawer` đều khai `transitionOptions = '150ms cubic-bezier(0, 0, 0.2, 1)'`: **một** input, dùng chung cho cả chiều vào lẫn chiều ra |
| Nhóm B — `Toast` | `primeng/toast` khai `showTransitionOptions = '300ms ease-out'` và `hideTransitionOptions = '250ms ease-in'`: **hai** input rời nhau |
| Vì sao token không tới được nhóm B | Ba input trên nhận chuỗi tham số hoạt ảnh Angular. Chuỗi đó bị phân tích thành số, **không** giải `var()` |
| Backdrop của `Dialog` | Đi đường khác hẳn hộp: `@primeuix/styles/base` chạy `animation: p-overlay-mask-enter-animation dt('mask.transition.duration') forwards`. Khoá đó là `semantic.mask.transitionDuration`, và Aura cho nó một chuỗi trần **riêng**: `"0.3s"` — nó **không** đi theo `semantic.transitionDuration` |
| `Drawer` còn hai con số ngoài tầm `transitionOptions` | `@primeuix/styles/drawer` khai `transition: transform 0.3s` và, cho biến thể toàn màn, `transition: opacity 400ms cubic-bezier(0.25, 0.8, 0.25, 1)`. Đây là **CSS**, nên nó đi đường khác nhóm B |
| `Menu`, `DatePicker`, `Autocomplete` mở/đóng bằng gì | `showTransitionOptions = '.12s cubic-bezier(0, 0, 0.2, 1)'`, `hideTransitionOptions = '.1s linear'` — và spec của ba component im lặng về khoản này |

Ba điều đáng nói ra vì chúng đổi hình dạng của quyết định:

**Một — nhóm A không phải sáu chỗ, mà một chỗ.** Cả sáu component `design-expert` khảo sát đều chỉ là *lá* của cùng một cây token. Sửa ở gốc thì sáu lá theo, và mọi lá chưa mọc cũng theo. Điều này biến câu hỏi *"nối cho component nào"* thành câu hỏi sai: không có danh sách component nào để khai.

**Hai — vì thế tầm ảnh hưởng rộng hơn sáu component.** Cùng khoá gốc đó cũng nuôi `button`, `select`, `inputtext`, `chip`, `tooltip` và mọi sub-preset sẽ thêm về sau. Đổi nó là đổi thời lượng của mọi chuyển tiếp nhỏ trong thư viện, không riêng sáu chỗ đã đo.

**Ba — `.12s` của `Menu`/`DatePicker`/`Autocomplete` trùng đúng `--dur-fast`, và đó là trùng số chứ không phải token.** Một lượt rà sau này rất dễ đọc nó thành *"chỗ này đã đạt"*. Nó chưa đạt; nó chỉ chưa lệch.

Người dùng đã chốt cả ba vế. ADR này ghi lại và bổ sung phần kiến trúc mà quyết định đó kéo theo.

## Quyết định

Người dùng chốt ba vế; kiến trúc sư chốt cách thi hành:

1. **Nhóm A nối ở đúng MỘT khoá của preset dùng chung:** `semantic.transitionDuration` trong `definePreset` của `src/FE/src/app/core/theme/prime-preset.ts`, giá trị `var(--dur-fast)`. **Không** khai `semantic.formField.transitionDuration` — nó đã trỏ về khoá trên, và khai lại là dựng nguồn thứ hai cho cùng một giá trị.
2. **Không khai thời lượng thư viện trong `src/FE/src/styles/_thu-vien.scss`, và không khai theo từng component.** Một chỗ áp, để component bọc thứ bảy thừa hưởng mà không ai phải nhớ.
3. **Nhóm B giữ quyền đặt số ở spec, và số sống trong một hằng số TypeScript có tên**, khai cạnh component bọc, theo đúng khuôn `TOOLTIP_DELAY_CHUOT_MS` ở `src/FE/src/app/shared/ui/tooltip/tooltip.component.ts`. `--dur-*` **không** bị gỡ khỏi ba spec `Dialog`, `Drawer`, `Toast` — khác hẳn ca `Tooltip`, nơi spec đã nhường hẳn quyền.
4. **Mỗi hằng số nhóm B phải truy ngược được về token nó phản chiếu, bằng một chú thích nêu ĐÍCH DANH tên token.** Không nêu tên thì hằng số đó là một con số mồ côi, và cái giá "hai nơi" ở mục Hệ quả trở thành vô hạn. Luật **F37** ([`../RULES.md`](../RULES.md) §7) ép câu này.
5. **`Dialog` và `Drawer` bỏ bất đối xứng vào/ra: một thời lượng và một đường cong cho cả hai chiều.** `transitionOptions` là **một** input; spec đòi hai bộ giá trị là đòi thứ nền này không chở được.
6. **`Toast` giữ bất đối xứng.** Nó có hai input rời nhau nên cả bốn khoản của spec khớp được; gộp nó vào quyết định 5 là bỏ một thứ đang dựng được chỉ để cho ba component trông giống nhau.
7. **Backdrop của `Dialog` nối CÙNG LÚC với hộp, không nối lẻ.** `semantic.mask.transitionDuration` khai trong cùng `definePreset`, và giá trị của nó là **cùng một token** với hằng số thời lượng của `Dialog` ở quyết định 3 — không phải `var(--dur-fast)` của quyết định 1. Nối lẻ backdrop về `--dur-fast` làm khoảng lệch với hộp **rộng ra**, không hẹp lại.
8. **Hai con số CSS còn lại của `Drawer` — `transform 0.3s` và `opacity 400ms` của biến thể toàn màn — là CSS, nên chúng đi bằng override trong `_thu-vien.scss`, không bằng hằng số TypeScript.** Đây là ngoại lệ có tên của quyết định 2: quyết định 2 cấm khai **token thời lượng của thư viện** ở đó; đè một khai báo `transition` cứng của thư viện là việc mà `_thu-vien.scss` sinh ra để làm, và nó đã có khối override `.p-dialog` cùng loại.
9. **`Menu`, `DatePicker`, `Autocomplete`: đây là *spec thiếu*, không phải *code lệch*.** Ba spec bổ sung một câu về thời lượng mở/đóng. Trùng số `.12s` ↔ `--dur-fast` phải được nói ra là trùng số, để lượt rà sau không đọc nó thành đã nối.

## Phương án đã cân nhắc và vì sao loại

### Vế 1 — Phương án A: khai `--p-<comp>-transition-duration` trong `_thu-vien.scss`

**Được:** nhìn thấy ngay khi đọc stylesheet, không phải hiểu cơ chế token của PrimeNG. Sửa một component không đụng component khác.

**Mất:** một giá trị chép ra sáu chỗ, và chỗ thứ bảy sẽ bị quên. Đó đúng khuôn *hai bản sẽ lệch nhau* mà [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §5 cấm — ở đây là sáu bản.

**Vì sao loại:** nó khai ở lá trong khi thứ cần sửa nằm ở gốc, nên nó tốn sáu lần công để mua một kết quả yếu hơn.

### Vế 1 — Phương án B: khai riêng cả `semantic.transitionDuration` lẫn `semantic.formField.transitionDuration`

**Được:** đọc preset thấy ngay hai nhánh, không phải lần theo `{transition.duration}` để biết nhánh formField đi về đâu.

**Vì sao loại:** Aura **đã** nối nhánh formField về nhánh gốc. Khai lại là chép một giá trị sang chỗ thứ hai, và chỗ thứ hai sẽ không được sửa cùng lúc. Muốn dễ đọc thì viết một dòng chú thích, không viết một dòng khai báo.

### Vế 2 — Phương án A: gỡ `--dur-*` khỏi ba spec nhóm B, để thư viện quyết, như đã làm cho `Tooltip`

**Được:** nhất quán với tiền lệ vừa lập; spec thôi khai một thẩm quyền nó không có; không có con số nào sống ở hai nơi.

**Mất:** ca `Tooltip` khác ba ca này ở một điểm quyết định. Thư viện tự chạy vòng mờ dần của tooltip bằng `requestAnimationFrame` — spec **không có cách nào** đặt thời lượng. Với `Dialog`, `Drawer`, `Toast` thì có: ba input nhận số. Nhường quyền khi vẫn còn đường đặt là vứt bỏ một quyền, không phải thừa nhận một giới hạn.

**Vì sao loại:** nó dùng đúng tiền lệ nhưng cho sai ca. Ranh giới của tiền lệ `Tooltip` là *"spec nhường khi không dựng được"*, không phải *"spec nhường khi khó"*.

### Vế 2 — Phương án B: một hàm đọc giá trị token lúc chạy (`getComputedStyle`) rồi truyền vào input

**Được:** giá trị thật sự chỉ sống một nơi — token CSS. Không có con số nào trong TypeScript.

**Mất:** nó buộc mỗi component nhóm B chờ layout để biết thời lượng của chính mình, thêm một lần đọc bố cục trên đường mở hộp thoại, và hỏng im lặng khi chạy trong test hoặc khi stylesheet chưa gắn — lúc đó chuỗi rỗng đi thẳng vào tham số hoạt ảnh. Đổi một chỗ lệch **nhìn thấy được** lấy một chế độ hỏng **không nhìn thấy được**.

**Vì sao loại:** cùng lập luận mà [ADR-0068](0068-nhanh-tai-tep-di-qua-handlefile.md) dùng để loại phương án middleware — biến một ràng buộc kiểm được lúc viết thành một ràng buộc kiểm lúc chạy, trên một phép đoán.

### Vế 3 — Phương án A: giữ spec bất đối xứng, ghi một dòng nợ

**Được:** không sửa spec hôm nay; ý đồ thiết kế còn nguyên trong văn bản, chờ ngày nền đổi.

**Vì sao loại:** tài liệu sẽ mô tả một thứ code không làm được, và dòng nợ đó không có điều kiện trả nào — nó chờ một thay đổi ở thư viện bên thứ ba mà không ai kiểm soát. Đó là nợ vĩnh viễn đội lốt nợ.

### Vế 3 — Phương án B: đổi nền, tự dựng `Dialog` và `Drawer`

**Được:** lấy lại toàn bộ quyền: hai thời lượng, hai đường cong, và mọi con số về CSS nên token tới được hết.

**Vì sao loại:** phải tự làm bẫy tiêu điểm, khoá cuộn nền, chồng lớp nổi, phím `ESC`, và trả cái giá *hai nền lớp nổi song song* mà [`../Design/Components/Tooltip.md`](../Design/Components/Tooltip.md) đã từ chối một lần. Một khoản bất đối xứng 50 ms không mua nổi cái giá đó.

## Hệ quả

### Tích cực

- Nhóm A nối bằng **một dòng**, và component bọc thứ bảy, thứ tám thừa hưởng mà không ai phải nhớ gì — kể cả `Tabs` và `FileUpload` khi chúng được dựng.
- Ba spec nhóm B thôi mô tả thứ không dựng được: sau quyết định 5 và 6, mỗi khoản trong chúng hoặc đã có đường thi hành, hoặc đã bị bỏ có chủ đích.
- Backdrop và hộp của `Dialog` về cùng một nguồn, nên câu *"mờ dần cùng lúc"* của spec thành câu kiểm được thay vì câu mong muốn.
- Chỗ trùng số `.12s` được nói thẳng là trùng số. Một lượt rà sau này sẽ không đóng nhầm nó.

### Tiêu cực

- **Giá trị thời lượng từ nay sống ở HAI nơi: token CSS cho nhóm A, hằng số TypeScript cho nhóm B.** Đây là cái giá chính, và nó không có cách nào tránh trên nền này — nhóm B không đọc `var()`. Quyết định 4 và luật F37 làm hai nơi **truy được về nhau**; chúng không làm hai nơi thành một. Ai đổi một giá trị trong `_tokens.scss` phải nhớ rà hằng số nhóm B, và "phải nhớ" là thứ sẽ hỏng.
- **Tầm ảnh hưởng của quyết định 1 rộng hơn sáu component đã khảo sát.** Cùng khoá đó nuôi `button`, `select`, `inputtext`, `chip` và mọi sub-preset thêm sau. Không ai đã xem `120ms` có đúng cho từng chỗ đó không — `design-expert` phải rà một lượt sau khi nối, và nếu một chỗ cần khác thì chỗ đó mới là ca khai riêng. Rủi ro cụ thể: một chuyển tiếp vốn dịu ở `200ms` trở nên giật ở `120ms`, và không test nào bắt.
- **`Drawer` vẫn còn hai con số thư viện không đi theo `transitionOptions`.** Quyết định 8 đưa chúng về `_thu-vien.scss`, nghĩa là một component có chuyển động đến từ **ba** cơ chế: hằng số TypeScript, override CSS, và preset. Ai chỉnh `Drawer` phải biết cả ba. Đây là chỗ phức tạp nhất mà quyết định này để lại, và nó không được giấu.
- **Không cổng nào canh việc nhóm B còn khớp spec.** F37 (quyết định 4) ép được *hằng số có nêu tên token*; nó **không** ép được *hằng số mang đúng giá trị của token đó* cho tới khi cổng đối chiếu hai nguồn được viết — trạng thái của nó ở [`../RULES.md`](../RULES.md) §7.
- **Bỏ bất đối xứng của `Dialog` và `Drawer` là mất một ý đồ thiết kế thật.** Vào chậm hơn ra là quy ước chuyển động có lý do: vào thì mắt cần theo kịp, ra thì đường đi đã biết. Quyết định 5 đổi nó lấy một spec nói thật. Đó là mất mát, không phải hoà.

### Rút lui nếu sai

Vế 1: xoá một khoá trong `definePreset` — thư viện quay về `0.2s`, không chạm component nào. Vế 2: mỗi hằng số là một dòng, gỡ thì component quay về mặc định thư viện. Vế 3 là vế đắt nhất để đảo, vì nó đã sửa spec: quay lại cần một ADR mới nêu **nền đã đổi thế nào** — ví dụ PrimeNG tách `transitionOptions` thành hai input — chứ không quay lại bằng cách viết lại spec như cũ.

## Ranh giới với ADR-0038 và ADR-0056

Ba quyết định này chạm cùng một tệp nhưng **hai trục khác nhau**, và trộn chúng là cách `prime-preset.ts` mất chủ:

| Trục | Ai quyết | Quyết cái gì |
| --- | --- | --- |
| **Tập sub-preset** — `AURA_SUBSET.components` | [ADR-0038](0038-preset-primeng-tung-sub-theo-phan-dung.md), sửa một phần bởi [ADR-0039](0039-sua-mot-phan-0038-hai-sub-preset-khong-phai-bon.md) và [ADR-0056](0056-f29-tinh-component-con-duoc-dung-ben-trong.md) | Sub-preset **nào** được ghép vào, và trần là bao đóng phụ thuộc. Ép bằng luật **F29** |
| **Giá trị semantic ghi đè** — đối số thứ hai của `definePreset` | ADR này, và phần màu thì đã có từ trước | Khoá semantic **nào** bị ghi đè và bằng giá trị gì |

ADR này **không thêm, không bớt sub-preset nào**, nên nó không chạm F29 và không sửa phần nào của ADR-0038 hay ADR-0056. Ngược lại, thêm một sub-preset theo ADR-0056 cũng không cần mở lại ADR này: sub-preset mới tự trỏ về `{transition.duration}` và thừa hưởng — đó chính là tính chất mà quyết định 1 mua được.

## Việc thi công

**`design-expert` — `docs/Design/`:**

1. `Components/Dialog.md`, `Components/Drawer.md` — §Trạng thái bỏ bất đối xứng vào/ra, chốt **một** thời lượng và **một** đường cong cho cả hai chiều; gỡ hai dòng `🛑 chờ quyết định chung`. Bảng `Chuyển động` rút theo.
2. `Components/Dialog.md` — khai backdrop dùng **cùng** token với hộp (quyết định 7).
3. `Components/Drawer.md` — thêm khoản cho hai con số CSS của quyết định 8: thời lượng trượt, và thời lượng của biến thể toàn màn.
4. `Components/Toast.md` — giữ bất đối xứng, gỡ dòng chờ quyết định, nói rõ số đi bằng hằng số TypeScript.
5. `Components/Menu.md`, `Components/DatePicker.md`, `Components/Autocomplete.md` — thêm một câu về thời lượng mở/đóng, và **nói rõ `.12s` hiện tại là trùng số với `--dur-fast`, không phải đã nối**.
6. Ba spec nhóm B giữ `--dur-*` trong bảng token, kèm một câu nói rõ giá trị đi vào TypeScript chứ không vào CSS — không lặp lại khuôn `Tooltip`.

**`frontend-expert` — `src/FE/`:**

1. `core/theme/prime-preset.ts` — thêm `semantic.transitionDuration: 'var(--dur-fast)'` và `semantic.mask.transitionDuration` theo quyết định 7. Chỉ hai khoá, không khai `formField`.
2. `prime-preset.spec.ts` — thêm ca khẳng định hai khoá đó có mặt sau `definePreset`, cùng khuôn ca đang canh sub-preset con.
3. Hằng số thời lượng cho `shared/ui/dialog/`, `shared/ui/toast/` và `shared/ui/drawer/` (khi dựng), mỗi hằng kèm chú thích nêu **đích danh** token nó phản chiếu — quyết định 4, luật F37.
4. `styles/_thu-vien.scss` — override hai khai báo `transition` cứng của `Drawer` theo quyết định 8, mỗi khối kèm hai câu *"Vì sao"* và *"Hỏng khi nào"* như quy ước của tệp đó đòi.
5. Rà lượt một sau khi nối: chuyển tiếp nào trở nên giật ở `120ms` thì báo `design-expert`, **không** tự khai riêng một khoá cho nó.

**`test-engineer`:** cổng của F37 — đặc tả ở chính dòng F37 trong [`../RULES.md`](../RULES.md) §7.

## Liên quan

- [`0038-preset-primeng-tung-sub-theo-phan-dung.md`](0038-preset-primeng-tung-sub-theo-phan-dung.md) · [`0056-f29-tinh-component-con-duoc-dung-ben-trong.md`](0056-f29-tinh-component-con-duoc-dung-ben-trong.md) — trục tập sub-preset, xem mục *Ranh giới*.
- [`../Design/Components/Tooltip.md`](../Design/Components/Tooltip.md) — tiền lệ *spec nhường quyền*, và ranh giới của tiền lệ đó.
- [`../RULES.md`](../RULES.md) §7 — luật **F29** (tập sub-preset) và **F37** (hằng số nhóm B truy ngược về token).
- [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) — quy ước override thư viện gom một chỗ.
