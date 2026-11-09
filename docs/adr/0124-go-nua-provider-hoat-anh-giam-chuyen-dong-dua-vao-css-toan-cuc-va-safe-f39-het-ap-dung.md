---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0124 — App không khai provider hoạt ảnh nào của Angular: gỡ nửa provider của ADR-0080; giảm chuyển động của component PrimeNG dựa vào khối CSS toàn cục và tuỳ chọn `safe` của thư viện; F39 hết áp dụng

> **Trạng thái:** Đã chấp nhận (2026-11-06) · Sửa một phần bởi [ADR-0128](0128-tai-nhap-bo-chay-hoat-anh-khong-bi-lock-chan-luat-f64-review-canh-chua-dung-cong.md) (2026-11-07)
>
> ADR-0128: gói `@angular/animations` còn trong lock, dưới dạng phụ thuộc ngang hàng tuỳ chọn của `@angular/platform-browser`, nên câu *"thủ tục thay cho cổng F39"* ở quyết định 4 không còn là rào. Phần nào còn hiệu lực: bảng *ADR cũ — phần nào còn hiệu lực* của ADR đó.

## Bối cảnh

[ADR-0080](0080-nua-provider-giam-chuyen-dong-nap-luoi-va-song-o-core.md) dựng **nửa provider** của giảm chuyển động trên một sự thật về nền ngày 2026-09-24: `Dialog`, `Toast`, `Menu`, lớp phủ của `Autocomplete` và `Pagination` vào/ra bằng bộ chạy hoạt ảnh của Angular — Web Animations, thời lượng truyền bằng đối số JavaScript — nên khối CSS giảm chuyển động toàn cục không với tới chúng. ADR đó tự đặt điều kiện xét lại. [ADR-0123](0123-chuyen-dong-component-primeng-di-bang-token-qua-css-go-hang-ms-f37-het-ap-dung.md) ghi nhận hai điều kiện đã chạm mà không quyết: điều kiện 1 — thư viện UI chuyển lớp phủ khỏi bộ chạy hoạt ảnh của Angular — và điều kiện 4 — F37 đổi hình dạng.

Người dùng chốt ngày 2026-11-06: bỏ nửa provider và luật F39. ADR này ghi lại quyết định đó và chốt phần còn mở: sau khi gỡ, giảm chuyển động còn dựa vào đâu, chỗ hở nào còn lại, và gỡ tới đâu.

`architect` đọc tĩnh các gói đã cài ngày 2026-11-06. **Chưa ai mở ứng dụng trên trình duyệt với cờ giảm chuyển động bật.**

| Đo gì | Kết quả |
| --- | --- |
| Phiên bản | `@angular/core` 21.2.26 · `primeng` 21.1.10 · `@primeuix/motion` 0.0.10 |
| Thư viện còn dùng bộ chạy của Angular không | Không. Không tệp nào trong `primeng/fesm2022` import `@angular/animations`; không component nào của thư viện khai `animations` trong metadata. `peerDependencies` của `primeng` không còn `@angular/animations` |
| Mã của app | Không tệp nào dưới `src/FE/src/app` dùng `@angular/animations` hay khai `animations: [`. Ngoài `core/theme/core-animations.ts`, chỉ tệp `*.spec.ts` import từ `@angular/platform-browser/animations`, để gọi `provideNoopAnimations()` |
| Provider hôm nay làm gì | Không gì nhìn thấy được. `provideAnimationsAsync` chỉ nạp bộ máy khi dựng một component mang dữ liệu `animation` — gói `@angular/platform-browser` đã cài, tệp `fesm2022/animations-async.mjs`, chuỗi `rendererType?.data?.['animation']` — và hôm nay không component nào mang. Hai API đó không đặt token `ANIMATIONS_DISABLED` mà `@angular/core` dùng cho `animate.enter` / `animate.leave`. Token `ANIMATION_MODULE_TYPE` chỉ overlay của `@angular/cdk` đọc; app không dùng overlay CDK, PrimeNG không import `@angular/cdk/overlay` |
| Hạn dùng | `provideAnimationsAsync` mang ``@deprecated 20.2 Use `animate.enter` or `animate.leave` instead. Intent to remove in v23`` — gói `@angular/platform-browser` đã cài, tệp `types/animations-async.d.ts` |
| Chuyển động PrimeNG 21 chạy bằng gì | Lớp CSS theo pha do `@primeuix/motion` gắn rồi gỡ — bảng *Bối cảnh* của ADR-0123 |
| Lớp `safe` của thư viện | `MotionOptions.safe` — *"Indicates whether to respect the user's reduced motion preference"* (gói `@primeuix/motion` đã cài, tệp `dist/index.d.mts`). Mặc định `true` trong `DEFAULT_MOTION_OPTIONS` của gói. Khi `safe` bật và `prefers-reduced-motion: reduce` khớp **lúc tạo** bộ chuyển động, thư viện bỏ qua cả pha và gọi thẳng các hook. Template của `dialog`, `drawer`, `toast`, `menu`, `overlay`, `datepicker` không bind `pMotionSafe`; app không truyền `motionOptions`, `overlayOptions` hay `pt` nào, nên mặc định giữ nguyên. Không có `matchMedia` thì hàm dò của thư viện trả `false` — ngược với ADR-0080, nơi vắng `matchMedia` nghĩa là tắt hoạt ảnh |
| Lớp CSS | `src/FE/src/styles/styles.scss`, chuỗi `@media (prefers-reduced-motion: reduce)`: ép `animation-duration`, `transition-duration` về `0.01ms !important`. CSS thư viện vào `@layer primeng` (`cssLayer: true`); khai báo `!important` thắng mọi khai báo thường, có lớp hay không. Không khai báo `animation*` / `transition*` nào trong `@primeuix/styles` hay `primeng/fesm2022` mang `!important`. Khối đè thời lượng của ADR-0123 ở `_thu-vien.scss` cũng không mang `!important`, nên khối toàn cục thắng cả nó |
| Chuyển động loại 3 trong các module PrimeNG app import | Chỉ `tooltip` — `fadeIn` qua `requestAnimationFrame`. Nó đã có khối riêng ở `src/FE/src/styles/_thu-vien.scss`, chuỗi `opacity: 1 !important;`, và khối đó không phụ thuộc provider |
| Component còn thuộc tính tổng hợp `@…` mà không có provider | Chế độ dev — `ng serve`, `ng test` — ném `NG05105` (gói `@angular/platform-browser` đã cài, tệp `fesm2022/_dom_renderer-chunk.mjs`, chuỗi `Unexpected synthetic`). Bản production bỏ qua lặng lẽ: phần tử hiện, ẩn mà không hoạt ảnh |

Ba điều rút ra:

**Một — nửa provider không còn thay gì.** Cả nhánh thật lẫn nhánh `'noop'` đều không chạm tới chuyển động nào trên màn hình.

**Hai — F39 xanh mà rỗng nghĩa.** Cổng khoá một dòng nối không còn ảnh hưởng gì tới màn hình — cùng loại với F37 mà ADR-0123 vừa gỡ.

**Ba — mã đang dạy một điều sai.** Chú thích ở `core/theme/core-animations.ts` khẳng định toast, hộp thoại, menu và lớp phủ của thư viện vào/ra bằng bộ chạy của Angular. Từ PrimeNG 21, điều đó không còn đúng.

## Quyết định

**Người dùng chốt:** bỏ nửa provider và luật F39. **`architect` chốt cách thi hành:**

1. **App không khai provider hoạt ảnh nào của Angular.** Gỡ `core/theme/core-animations.ts` cùng spec của nó; gỡ lời gọi `provideCoreAnimations()` và import của nó ở composition root. Không hàm Core nào thay chỗ.
2. **Giảm chuyển động của component PrimeNG dựa vào hai lớp đã có, cả hai nằm ngoài mã TypeScript của app.** Lớp chính là khối CSS toàn cục ở `styles.scss`: nó với tới mọi chuyển động chạy bằng CSS — loại 1 ở [`../Design/DESIGN.md`](../Design/DESIGN.md) §7 — và được trình duyệt đánh giá lại ngay khi cờ đổi. Lớp phụ là `safe` mặc định của `@primeuix/motion`. Hai ràng buộc giữ cho chúng còn tác dụng: khối toàn cục giữ `!important`, vì CSS thư viện và khối đè của ADR-0123 chỉ thua nó nhờ dấu đó; app không đặt `safe` ở đâu cả — không `pMotionSafe`, không `motionOptions.safe`, không qua `overlayOptions` hay `pt`. Hai ràng buộc này là **luật F63**, mang `🕳️`.
3. **Gỡ `provideNoopAnimations()` khỏi mọi spec, cùng lượt.** Tiền đề của ADR này — *không gì trong app cần bộ chạy hoạt ảnh* — mới chỉ được kiểm bằng đọc tĩnh. Chạy `ng test` khi không spec nào cấp bộ chạy là phép thử động rẻ nhất cho tiền đề đó: một thuộc tính `@…` còn sót trong cây component mà spec dựng sẽ ném `NG05105`. Spec đỏ vì `NG05105` nghĩa là tiền đề sai: dừng, không đặt lại provider, báo `architect`. Giữ provider trong spec còn có hại riêng: spec chạy dưới một cấu hình mà production không có.
4. **Gỡ `@angular/animations` khỏi `dependencies` của `src/FE/package.json`** sau bước 3, khi không tệp nào dưới `src/FE/src` và `src/FE/e2e` còn import nó hay `@angular/platform-browser/animations`. Từ đó, đưa bộ chạy của Angular trở lại là **thêm phụ thuộc ngoài** — một dấu hiệu bắt buộc viết ADR ([`README.md`](README.md) §2). Thủ tục đó thay cho cổng F39, không tốn section nào.
5. **F39 mang trạng thái `Hết áp dụng (2026-11-06)`.** Mã luật mang một nghĩa mãi mãi ([`../RULES.md`](../RULES.md)), nên F39 không được dùng lại cho nghĩa khác. Không dựng luật, section hay canary nào thay F39 ([ADR-0114](0114-bo-so-no-luat-ve-rules-viec-ton-sang-issues-review-khong-sinh-viec-stop-chan-review-mot-lan-go-nhanh-chua-co-src.md)). Section `F39` của `scripts/fe-gate.sh` và canary của nó gỡ **cùng lượt** với bước 1. Gỡ mã trước thì section đỏ ở chốt *"không tìm ra tệp mang hàm xuất"*; giữ section mà gỡ mã thì không còn gì để nó canh.
6. **Kiểm dưới cờ bật là bước cuối của lượt thi công.** Điều kiện xét lại 1 của ADR-0080 đòi kiểm dưới cờ bật trước khi gỡ. Provider không thay gì (bảng *Bối cảnh*), nên kiểm trước hay sau khi gỡ cho cùng một kết quả. Kiểm sau, trên CSS đã sửa theo ADR-0123, mới kiểm đúng thứ sẽ chạy. Còn chuyển động nào chạy, hay lớp phủ nào kẹt, thì lượt chưa xong: báo `architect`.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Giữ nguyên tới Angular 23

Đây là **phương án đơn giản nhất**: không đụng gì, để lượt di trú bắt buộc ở Angular 23 gỡ cả hai API một lần.

**Được:** không lượt thi công nào; không rủi ro nào hôm nay.

**Mất:** phải tiếp tục bảo trì một hàm Core, một section cổng và một canary chỉ để canh một dòng nối không đổi gì trên màn hình. Chú thích trong mã tiếp tục dạy một điều sai cho người đọc kế tiếp. Một API deprecated nằm lại trong Core.

**Vì sao loại:** cùng lập luận mà ADR-0123 dùng để gỡ F37 — cổng xanh mà không canh gì là nợ, không phải lưới an toàn. Còn chú thích dạy sai thì tốn công mỗi lần có người đọc nó.

### Phương án B — Gỡ tối thiểu: hàm, dòng nối, section F39; giữ spec và `package.json`

**Được:** ít tệp đổi nhất — ba tệp mã, hai tệp script.

**Mất:** tiền đề chỉ được kiểm bằng đọc tĩnh. Spec vẫn chạy dưới bộ chạy rỗng mà production không có. Đưa bộ chạy của Angular trở lại mã production không đổi `package.json` nào, nên không gì đòi ADR.

**Vì sao loại:** quyết định 3 và 4 chỉ thêm một lượt sửa cơ học và một lần `ng test`. Đổi lại được phép thử động duy nhất cho tiền đề, cùng một rào thay F39 mà không tốn cổng nào.

### Phương án C — Giữ `provideCoreAnimations()` làm hàm Core đặt chính sách chuyển động tường minh

Ví dụ: hàm ép `safe: true`, để giảm chuyển động không phụ thuộc mặc định của thư viện.

**Được:** một chỗ Core canh chính sách, đứng vững nếu thư viện đổi mặc định.

**Mất:** `PrimeNGConfigType` không có đường cấu hình toàn cục nào cho `safe`. `overlayOptions` chỉ phủ lớp nổi đi qua `p-overlay`; `Dialog`, `Drawer`, `Toast`, `Menu` phải bind ở từng lớp bọc. Hàm còn lại là một abstraction Core với đúng một hiện thực, chép lại một giá trị vốn đã là mặc định.

**Vì sao loại:** trả bề mặt API Core để phòng một lần đổi mặc định chưa từng xảy ra, trong khi lớp CSS đứng độc lập với `safe`. Lối này vẫn mở ở bước 3 của *Rút lui*.

### Phương án D — Luật mới thay F39: cấm `@angular/animations` trong mã không phải spec, có section cổng

**Được:** chặn bằng máy việc tái nhập bộ chạy.

**Vì sao loại:** dựng cổng cho một vi phạm chưa từng xảy ra ([ADR-0114](0114-bo-so-no-luat-ve-rules-viec-ton-sang-issues-review-khong-sinh-viec-stop-chan-review-mot-lan-go-nhanh-chua-co-src.md)). Quyết định 4 đạt cùng mục đích bằng một thủ tục đã có.

### Phương án E — Thêm một ca lưới giao diện khoá hành vi giảm chuyển động

Ca Playwright gọi `page.emulateMedia({ reducedMotion: 'reduce' })`, mở các lớp phủ rồi đo thời lượng đã tính.

**Được:** lần đầu tiên **hành vi** có máy canh, chứ không phải một dòng nối. Ca này bắt được cả hai lớp, kể cả khi khối toàn cục bị gỡ.

**Mất:** thêm một ca cổng khi chưa có vi phạm thật nào ([ADR-0114](0114-bo-so-no-luat-ve-rules-viec-ton-sang-issues-review-khong-sinh-viec-stop-chan-review-mot-lan-go-nhanh-chua-co-src.md)); lưới thuộc `core-paths`, nên mỗi ca thêm là thêm việc bảo trì.

**Vì sao hoãn, không loại:** đây là cách đóng chỗ hở lớn nhất ở *Hệ quả tiêu cực*. Điều kiện kích hoạt nằm ở *Điều kiện xét lại* 2.

## Hệ quả

### Tích cực

- **Bớt một hàm `provideCore…` mà dự án hạ nguồn phải biết gọi** — ADR-0080 tự kể đó là một cái giá — cùng một API deprecated, một section cổng, một canary và một phụ thuộc.
- Lượt di trú ở Angular 23 không còn gì phải làm cho phần hoạt ảnh.
- **Cái giá *"hai nửa lệch nhau khi đổi cờ giữa phiên"* của ADR-0080 hết đối với chuyển động PrimeNG:** lớp chính là media query, nên trình duyệt đánh giá lại ngay khi cờ đổi.
- Điều 4 của ADR-0080 — hoãn sửa độ trễ focus của `Dialog` — hết đối tượng. Độ trễ nằm trong `_focus` của thư viện, mà lớp bọc không chạy hàm đó (ADR-0123, bảng *Bối cảnh*). Chưa ai đo lại trên trình duyệt.

### Tiêu cực

| Cái giá | Nghĩa là |
| --- | --- |
| **Giảm chuyển động không còn dòng mã nào của app chịu trách nhiệm, và không máy nào canh** | Gỡ khối toàn cục ở `styles.scss`, hay một lớp bọc truyền `safe: false`, vẫn đi qua mọi cổng. Lúc viết ADR này, `src/FE/src/styles/` nằm ngoài `core-paths`, nên sửa `styles.scss` không kéo lượt `core-reviewer` nào. Trước ADR này khối CSS cũng không ai canh; cái khác là trước đây có một tệp Core mang tên giảm chuyển động để người đọc thấy, nay thì không. F63 làm chỗ hở nhìn thấy được, không đóng nó |
| **Lớp `safe` là mặc định của bên thứ ba, ở phiên bản 0.0.x** | Một lượt nâng đổi mặc định thì lớp này tắt lặng lẽ. Lớp CSS vẫn đứng |
| **`safe` đọc cờ một lần, lúc tạo bộ chuyển động** | Tắt cờ giữa phiên thì component đã dựng khi cờ còn bật vẫn không chuyển động, cho tới khi được dựng lại. Ngược chiều với cái giá của ADR-0080 nhưng nhẹ hơn: thiếu chuyển động, không thừa |
| **Vắng `matchMedia` thì thư viện coi như không giảm** | ADR-0080 chọn ngược lại. Mọi trình duyệt app nhắm tới đều có `matchMedia`, và app không chạy SSR |
| **Tái nhập bộ chạy của Angular không có cổng** | Quyết định 4 biến nó thành một lần thêm phụ thuộc, tức một ADR — nhưng đó là thủ tục, không phải máy. Một lượt cài lại gói rồi thêm `provideAnimationsAsync()` trần thì đưa hoạt ảnh loại 2 trở lại, không nửa nào phủ, và mọi cổng vẫn xanh |
| **Toàn bộ dựa trên đọc tĩnh** | Phép kiểm động duy nhất là `ng test` ở quyết định 3 và lần kiểm trình duyệt ở quyết định 6. Lúc viết ADR này, chưa cái nào chạy |

### Rút lui nếu sai

**Dấu hiệu sai:** lần kiểm ở quyết định 6 thấy chuyển động PrimeNG còn chạy dưới cờ bật; `ng test` ở quyết định 3 đỏ vì `NG05105`; sau một lượt nâng, `grep -rl '@angular/animations' src/FE/node_modules/primeng/fesm2022` ra tệp, hoặc `DEFAULT_MOTION_OPTIONS` của `@primeuix/motion` không còn `safe` bật.

**Quy trình:**

1. **Sai lộ ra trong chính lượt thi công**, ở bước 3 hay bước 6: lượt dừng trước khi đóng. Người dùng hoàn các tệp của lượt bằng git. `architect` xét lại: nguyên nhân là bộ chạy của Angular thì giữ nửa provider, ADR này mang trạng thái `Đã loại bỏ`; nguyên nhân khác thì sửa nguyên nhân đó, không đặt lại provider.
2. **Một lượt nâng PrimeNG đưa bộ chạy của Angular trở lại:** viết ADR mới, vì đây là thêm phụ thuộc ngoài. Cài lại `@angular/animations` đúng phiên bản `@angular/core`. Dựng lại hàm chọn ở `core/` theo khuôn ADR-0080 — mã nằm trong lịch sử git, tệp `src/FE/src/app/core/theme/core-animations.ts`. Cần cổng thì cấp mã luật mới; F39 không hồi sinh.
3. **Mặc định `safe` đổi:** lớp CSS vẫn giữ giảm chuyển động, nên không bắt buộc làm gì. Muốn giữ lớp kép thì truyền `safe: true` tường minh ở từng lớp bọc — phương án C.

Không dữ liệu nào sinh ra, không migration. Chi phí: một lượt `frontend-expert`, cộng một lượt `test-engineer` nếu dựng lại cổng.

### Điều kiện xét lại

1. **Mỗi lượt nâng `primeng` hay `@primeuix/motion`:** kiểm lại hai sự thật ở bảng *Bối cảnh* — thư viện không import `@angular/animations`, và `safe` vẫn mặc định bật. Người làm lượt nâng kiểm, rồi ghi kết quả vào báo cáo của lượt nâng.
2. **Một hồi quy giảm chuyển động lọt tới người dùng, hoặc một đợt soát trợ năng nêu nó:** dựng ca lưới của phương án E, rồi đổi trạng thái F63.
3. **Core cần một chuyển động mới:** viết bằng CSS hoặc bằng `animate.enter` / `animate.leave` của Angular. Cả hai đều là lớp CSS nên khối toàn cục với tới, và không cần provider nào.

## ADR cũ — phần nào còn hiệu lực

| ADR | Còn hiệu lực | Hết hiệu lực |
| --- | --- | --- |
| [0080](0080-nua-provider-giam-chuyen-dong-nap-luoi-va-song-o-core.md) | Không phần nào | Điều 1 (API), 2 (hàm chọn ở `core/`), 3 (F39) — gỡ. Điều 4 (hoãn sửa độ trễ focus) — hết đối tượng |
| [0123](0123-chuyen-dong-component-primeng-di-bang-token-qua-css-go-hang-ms-f37-het-ap-dung.md) | Toàn bộ. ADR này trả lời câu hỏi mà đoạn *"Về ADR-0080 — ghi nhận, không quyết"* của nó để lại cho người dùng | — |

## Việc thi công

Thứ tự trong đợt: `test-engineer` bước 1 trước hoặc cùng lúc với `frontend-expert` bước 1, rồi đến các bước còn lại của `frontend-expert`. Đợt này chạy chung với ADR-0123; bước 6 của `frontend-expert` chạy sau khi phần CSS của ADR-0123 đã xong.

**`frontend-expert` — `src/FE/`:**

1. Gỡ `src/app/core/theme/core-animations.ts` và `src/app/core/theme/core-animations.spec.ts`.
2. `src/app/app.config.ts`: gỡ import `provideCoreAnimations`, gỡ khối chú thích ngay trên dòng `provideCoreAnimations(),` và chính dòng đó. Không thêm provider hoạt ảnh nào.
3. Gỡ `provideNoopAnimations()` và import của nó khỏi mọi spec. Tìm bằng `grep -rlE 'provideNoopAnimations|platform-browser/animations|@angular/animations' src e2e` chạy trong `src/FE`; lệnh phải ra rỗng. Chạy `ng test`. Spec nào đỏ vì `NG05105` thì dừng, không đặt lại provider, báo `architect` (quyết định 3).
4. Khi bước 3 xanh: `npm uninstall @angular/animations` trong `src/FE`. Sau đó `ng build`, `ng test`, `ng lint` và `npm run e2e:kieu` phải xanh. Không xanh thì hoàn riêng bước 4, giữ bước 1–3, rồi báo.
5. Rà chú thích dưới `src/` còn nhắc nửa provider, `provideCoreAnimations`, F39 hay ADR-0080 — `grep -rnE 'provideCoreAnimations|F39|ADR-0080|nửa provider' src`, chạy trong `src/FE` — rồi viết lại cho khớp quyết định 2.
6. Kiểm dưới cờ bật (quyết định 6). Bật giả lập `prefers-reduced-motion: reduce` của Chrome — DevTools *Rendering*, hoặc Playwright `page.emulateMedia({ reducedMotion: 'reduce' })` — rồi tải lại trang. Mở rồi đóng `Dialog`, `Drawer`, `Toast`, `Menu`, bảng gợi ý `Autocomplete`, ô số dòng của `Paginator`. Đạt khi: hiện và ẩn tức thì; không kẹt ở trạng thái mở; DOM được dọn sau khi đóng; focus vào hộp ngay khi hộp hiện. Ghi kết quả vào báo cáo; không viết test lưu lại (phương án E đang hoãn).

**`test-engineer` — `scripts/`:**

1. `scripts/fe-gate.sh`:
   - gỡ section `F39`, từ dòng mốc `# ====…==== F39` tới hết khối `if` của nó;
   - gỡ `F39` khỏi dòng liệt kê mã ở đầu tệp;
   - sửa chú thích của bộ bóc dòng — chuỗi `mọi phép dò của F39 (in dòng mã)` — và chú thích chuỗi `qua bộ bóc F22_DEM (như F39)` cho đúng những section còn dùng chúng.
   - **Không** gỡ chế độ `in_ma` của bộ bóc: section F58 dùng nó. Không gỡ biến nào ngoài tiền tố `F39_` / `f39_`.
2. Gỡ `scripts/tests/fe-gate-f39.test.sh`.
3. Kiểm: `bash scripts/fe-gate.sh` không còn dòng `F39`, các section khác giữ nguyên kết quả; mọi `scripts/tests/fe-gate-*.test.sh` xanh — nhất là canary của F22 và F58, hai section dùng chung bộ bóc. Không dựng section hay canary nào thay F39 hay cho F63.

**`design-expert` — `docs/Design/`:** [`../Design/DESIGN.md`](../Design/DESIGN.md) §7, mục *Giảm chuyển động*, đang tả **hai nửa thi hành**; nửa provider không còn. Phải sửa: bảng ba loại; đoạn *Nửa provider*; đoạn cái giá *"đổi cờ giữa phiên"*; câu ở loại 3 nhắc *"bộ chạy mà provider thay"*; dòng hiện trạng *nửa provider (loại 2)* ở bảng đầu tệp; dòng *"`prefers-reduced-motion` thi hành bằng hai nửa"* ở nhật ký quyết định của tệp. Thêm lớp `safe` (quyết định 2) cùng ba giới hạn của nó ở *Hệ quả tiêu cực*. [`../Design/Components/Toast.md`](../Design/Components/Toast.md), điểm (3) của đoạn *chưa so*: `Toast` không còn là loại 2, và các neo vào `core-animations.ts` sẽ chết.

**`architect` — `docs/`, đã làm cùng lượt viết ADR này:** dòng F39 và dòng F63 ở [`../RULES-FE.md`](../RULES-FE.md); dòng F39 ở [`../wiki-core/fe/trien-khai/05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) §3.1; §2.6 và bảng đầu tệp của [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md).

## Liên quan

- [`0080-nua-provider-giam-chuyen-dong-nap-luoi-va-song-o-core.md`](0080-nua-provider-giam-chuyen-dong-nap-luoi-va-song-o-core.md) — quyết định bị thay thế.
- [`0123-chuyen-dong-component-primeng-di-bang-token-qua-css-go-hang-ms-f37-het-ap-dung.md`](0123-chuyen-dong-component-primeng-di-bang-token-qua-css-go-hang-ms-f37-het-ap-dung.md) — nền đã đổi; F37, F62.
- [`0028-toolchain-fe-va-ke-hoach-nang-cap.md`](0028-toolchain-fe-va-ke-hoach-nang-cap.md) — kế hoạch nâng cấp mà điều kiện xét lại 1 bám theo.
- [`../RULES-FE.md`](../RULES-FE.md) §7 — F39 (hết áp dụng), F63.
- [`../Design/DESIGN.md`](../Design/DESIGN.md) §7 — ba loại chuyển động, khối CSS toàn cục.
