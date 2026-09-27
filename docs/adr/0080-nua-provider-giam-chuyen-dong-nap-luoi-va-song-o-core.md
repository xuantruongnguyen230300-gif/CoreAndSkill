---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0080 — Nửa provider của giảm chuyển động dùng `provideAnimationsAsync('noop')`, chọn một lần lúc khởi động, và hàm chọn sống ở `core/` chứ không ở composition root

> **Trạng thái:** Đã chấp nhận (2026-09-24)

## Bối cảnh

[`../Design/DESIGN.md`](../Design/DESIGN.md) §7, mục *Giảm chuyển động*, chốt rằng cờ `prefers-reduced-motion: reduce` được thi hành bằng **hai nửa độc lập**: một khối CSS toàn cục phủ chuyển động do trình duyệt chạy từ khai báo CSS, và một **nửa provider** thay bộ chạy hoạt ảnh của Angular bằng bản không chạy gì. Nửa thứ hai tồn tại vì một phép đo của `frontend-expert` (Chrome Headless, 2026-09-24): giữa chừng một hoạt ảnh Web Animations, phần tử đã nhận khối CSS vẫn có `playState` là `running`. `Dialog`, `Toast`, `Menu`, các lớp phủ của `Autocomplete` và `Pagination` vào/ra bằng bộ chạy đó — tức nửa CSS một mình không tắt được chúng.

Người dùng đã chốt **nửa provider tồn tại**, và đã chốt **cái giá** của nó: cờ chỉ được đọc lúc khởi động, đổi cờ giữa phiên thì hai nửa lệch nhau tới lần tải trang kế tiếp (DESIGN.md §7). ADR này không mở lại hai điều đó.

Cái còn mở khi ADR này được viết là bốn câu: **API nào**, **hàm chọn nằm ở đâu**, **có cổng không**, và **làm gì với một độ trễ focus vừa đo được**.

Phép đo — ghi rõ ai đo, vì phần lớn không do `architect` lặp lại:

| Phép đo | Ai đo, khi nào | Kết quả |
| --- | --- | --- |
| Bundle initial khi nhánh giảm dùng `provideNoopAnimations()` | `frontend-expert`, 2026-09-24 | 538.38 → **602.85 kB**: import tĩnh kéo cả bộ máy hoạt ảnh vào bundle khởi động của **mọi** người dùng, kể cả người không bật cờ |
| Bundle initial khi nhánh giảm dùng `provideAnimationsAsync('noop')` | `frontend-expert`, 2026-09-24 | **538.51 kB**. Cùng giá trị `ANIMATION_MODULE_TYPE` là `'NoopAnimations'`, bộ máy vẫn nạp lười. Khác biệt kỹ thuật duy nhất tìm thấy: bản này dùng `NoopAnimationStyleNormalizer` |
| Ngân sách F14 có chặn ca 602.85 kB không | `architect`, 2026-09-24, đọc `src/FE/angular.json` | **Không.** Mục `"type": "initial"` khai `"maximumWarning": "600kB"` và `"maximumError": "900kB"` — vượt 600 kB chỉ in cảnh báo, `ng build` vẫn xanh. Thứ duy nhất ngăn một lượt "sửa cho gọn" về import tĩnh hôm nay là một chú thích trong mã |
| Hạn dùng của hai API | `architect`, 2026-09-24, đọc gói `@angular/platform-browser` 20.3.31 đã cài | Cả `provideAnimationsAsync` (`animations/async/index.d.ts`) lẫn `provideNoopAnimations` (`animations/index.d.ts`) mang ``@deprecated 20.2 Use `animate.enter` or `animate.leave` instead. Intent to remove in v23`` |
| Hàm chọn đang nằm ở đâu | `architect`, 2026-09-24 | `src/FE/src/app/app.config.ts` — `export function chonProviderHoatAnh(` — gọi từ `appConfig` bằng `chonProviderHoatAnh(globalThis.matchMedia)`. Tệp đó **không** thuộc khối `core-paths` ([`../kien-truc-core-module.md`](../kien-truc-core-module.md)): nó là composition root, tức tệp của dự án |
| Dòng nối có test nào khoá không | `frontend-expert`, 2026-09-24, bằng đột biến | **Không.** Thu dòng nối trong `appConfig` về `provideAnimationsAsync()` trần: 6/6 test của `app.config.spec.ts` vẫn xanh |
| Phủ test của các lớp phủ dưới bộ chạy rỗng | `frontend-expert`, 2026-09-24 | Không spec nào đi qua `appConfig`. Popup của `Menu`, bảng gợi ý của `Autocomplete`, `p-select` của `Pagination` và đường ra của `Toast` **chưa từng chạy** trong test nào dưới bộ chạy rỗng. Con số "17 spec phủ sẵn" đúng về số đếm, không đúng về phạm vi |
| Focus của `Dialog` dưới bộ chạy rỗng | `frontend-expert`, 2026-09-24, ChromeHeadless 153, ba lần | Hộp hiện đủ ở 9–39 ms; focus vào ở 215–247 ms. Nguyên nhân nằm trong `primeng/dialog`: `_focus` hẹn giờ bằng `setTimeout(…, parseDurationToMilliseconds(this.transitionOptions) \|\| 5)`, và `transitionOptions` lấy từ hằng `DIALOG_TRANSITION_OPTIONS` của Core — **không** phụ thuộc bộ chạy hoạt ảnh |

Tiền lệ trực tiếp: [`0063-locale-id-den-tu-seam-core-i18n.md`](0063-locale-id-den-tu-seam-core-i18n.md) đã dời `LOCALE_ID` và `registerLocaleData` ra khỏi `app.config.ts` vào một hàm của `core/`, vì một dự án hạ nguồn viết composition root của riêng mình sẽ đánh rơi chúng mà không gì báo.

## Quyết định

Bốn điều. Điều 1 do **người dùng** chốt; ba điều sau do `architect` chốt.

1. **Người dùng chốt API:** nhánh giảm chuyển động — và nhánh không có `matchMedia` — dùng `provideAnimationsAsync('noop')`; nhánh thường dùng `provideAnimationsAsync()`. **`provideNoopAnimations()` và `provideAnimations()` không xuất hiện trong mã không phải spec.** Cả hai là import tĩnh, và cái giá của chúng trả bởi người dùng **không** bật cờ.
2. **`architect` chốt chỗ đặt: hàm chọn sống ở `core/`, không ở composition root.** `core/` xuất một hàm `provideCoreAnimations()` — gọi hàm chọn với `globalThis.matchMedia` — và hàm chọn nhận `matchMedia` qua tham số như hôm nay, để unit test vẫn gọi được. Composition root gọi `provideCoreAnimations()` và **không** khai provider hoạt ảnh nào khác. Thư mục cụ thể dưới `core/` do `frontend-expert` chọn theo [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §2.1; cổng ở điều 3 tìm tệp theo tên hàm xuất, không theo đường dẫn.
3. **`architect` chốt có cổng: luật F39** ở [`../RULES.md`](../RULES.md) §7, cùng khuôn F36. Đặc tả cổng và canary nằm ở chính dòng luật; việc viết thuộc `test-engineer`.
4. **`architect` chốt độ trễ focus của `Dialog` là hệ quả đã biết, không mở việc sửa trong ADR này.** Lý do ở mục *Hệ quả*.

Việc thi công: `frontend-expert` dời hàm và sửa dòng nối; `test-engineer` viết section `F39` và canary; `design-expert` sửa hai chỗ ở [`../Design/DESIGN.md`](../Design/DESIGN.md) — câu ở mục *Nửa provider* còn gọi tên `provideNoopAnimations()`, và dòng *nửa provider (loại 2)* ở bảng đầu tệp còn ghi hiện trạng *"chưa có"*.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — `provideNoopAnimations()` cho nhánh giảm chuyển động

Đây là API mà DESIGN.md §7 gọi tên, và là API mà mọi spec trong repo dùng.

**Được:** tên API tự nói nó làm gì; khớp mọi tài liệu Angular về "tắt hoạt ảnh".

**Vì sao loại:** phép đo ở §Bối cảnh — +64 kB cho **mọi** người dùng, để phục vụ những người đã xin **bớt** thứ trên màn hình. Loại thêm vì một lý do thứ hai mà phép đo đó chưa nói: cổng ngân sách **không** bắt được nó. F14 chỉ cảnh báo ở 600 kB, nên lượt "sửa cho gọn" về API này sẽ đi qua mọi cổng đang có. Chính vì vậy điều 1 cần một luật có cổng, không chỉ một chú thích.

### Phương án B — Giữ hàm chọn ở composition root, dựng cổng trên `app.config.ts`

Phương án đơn giản hơn phương án đã chọn: không dời tệp nào, chỉ thêm một cổng khẳng định `app.config.ts` gọi `chonProviderHoatAnh(`.

**Được:** không chạm `core/`, không thêm bề mặt API cho Core. Cổng kiểu này bắt được đúng đột biến đã lọt ở §Bối cảnh.

**Vì sao loại:** ba lý do, và lý do thứ nhất là lý do quyết định.

Thứ nhất, cổng khoá được **lời gọi**, không khoá được **thân hàm**. Dự án hạ nguồn nhận `app.config.ts` như một tệp của mình và sửa nó tự do; một thân hàm bị rút ruột — luôn trả bộ chạy thật — vẫn đi qua cổng kiểm lời gọi, và vì tệp nằm ngoài `core-paths` nên không lượt `core-reviewer` nào bị buộc phải xem. Nửa provider là **nghĩa vụ trợ năng của các component Core** — chính `Dialog`, `Toast`, `Menu` của Core là thứ chuyển động — nên thân của nó phải nằm ở nơi Core canh.

Thứ hai, **hạn dùng**. Cả hai API hết hạn ở Angular 23. Đặt hàm ở composition root thì lượt di trú đó xảy ra một lần ở **mỗi** dự án hạ nguồn, và dự án nào không làm thì mất nửa provider đúng ngày nâng cấp. Đặt ở `core/` thì nó xảy ra một lần, ở Core, và đi theo bản phân phối.

Thứ ba, đây đúng là tình huống ADR-0063 đã giải cho `LOCALE_ID`: một giá trị Core buộc phải đúng mà lại được khai ở tệp của dự án. Giải hai ca cùng loại theo hai cách khác nhau thì người đọc không còn quy tắc nào để đoán ca thứ ba.

### Phương án C — Lắng nghe cờ đổi giữa phiên và thay bộ chạy

**Vì sao loại:** bộ chạy hoạt ảnh được cấp một lần qua cây provider môi trường; không có đường thay nó khi ứng dụng đang chạy mà không dựng lại ứng dụng. Người dùng đã chấp nhận cái giá "tải lại trang" (DESIGN.md §7). Loại, không hoãn.

### Phương án D — Chỉ nửa CSS

**Vì sao loại:** phép đo ở §Bối cảnh — hoạt ảnh Web Animations vẫn chạy dưới khối CSS. Nửa CSS một mình để hộp `Dialog` phóng to hết thời lượng trong khi backdrop hiện tức thì.

### Về điều 4 — sửa độ trễ focus ngay

Lối sửa duy nhất đã thấy là cho `DIALOG_TRANSITION_OPTIONS` mang giá trị gần bằng 0 khi cờ bật. Hằng đó do luật **F37** canh: nó phải phản chiếu **đúng một** bậc `--dur-*` theo **vai** ([`0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md`](0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md), [`0075-ba-component-lop-noi-vao-nhom-b-va-bac-thang-chon-theo-vai.md`](0075-ba-component-lop-noi-vao-nhom-b-va-bac-thang-chon-theo-vai.md)); một giá trị có điều kiện không phải một bậc. Sửa ngay nghĩa là sửa một phần hai ADR đó và đổi hình dạng một luật đang có cổng — cho một độ trễ **không phải hồi quy** (xem *Hệ quả*). Hoãn, kèm điều kiện ở cuối.

## Hệ quả

### Tích cực

- Người không bật cờ không trả gì: bundle khởi động giữ 538.51 kB.
- Nửa provider có cổng. Đột biến đã lọt mọi test hôm nay — thu dòng nối về `provideAnimationsAsync()` trần — làm F39 đỏ; lượt "sửa cho gọn" về import tĩnh cũng đỏ, thay vì chỉ để lại một cảnh báo ngân sách không ai đọc.
- Thân hàm nằm trong `core-paths`: sửa nó là một lượt `core-reviewer` bắt buộc, và dự án hạ nguồn nhận nó theo bản phân phối thay vì chép tay.
- Lượt di trú khi Angular gỡ hai API xảy ra đúng một lần, ở Core.

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| **Nửa provider có ngày hết hạn** | Cả hai API mang `Intent to remove in v23`. [`0028-toolchain-fe-va-ke-hoach-nang-cap.md`](0028-toolchain-fe-va-ke-hoach-nang-cap.md) đã lên kế hoạch nâng lên 21; tới 23 thì cả quyết định này phải làm lại. Hướng Angular chỉ ra — `animate.enter` / `animate.leave` — chạy bằng CSS, tức chuyển động loại 2 sẽ thành loại 1 và rơi vào tầm nửa CSS. **Chưa ai kiểm** thư viện UI có đi theo hướng đó không; nếu nó giữ bộ chạy riêng thì loại 2 thành loại 3, mà loại 3 thì không nửa nào với tới |
| **`Dialog` nhìn thấy được trước khi nhận focus, khoảng 200 ms** | Chỉ khi cờ bật: hộp hiện ở 9–39 ms, focus tới ở 215–247 ms. Độ trễ focus **không mới** — người không bật cờ cũng nhận focus sau cùng khoảng thời gian, vì hẹn giờ lấy từ hằng thời lượng chứ không từ bộ chạy; nhưng với họ hộp còn đang phóng to nên khoảng chờ không lộ ra. Bộ chạy rỗng chỉ làm nó nhìn thấy được. Rủi ro phải canh, **chưa đo**: một phím bấm trong khoảng đó rơi vào phần tử đang giữ focus — thường là chính nút vừa mở hộp |
| **Các lớp phủ chưa từng chạy dưới bộ chạy rỗng trong test** | F39 khoá **dòng nối**, không khoá **hành vi**. Popup `Menu`, bảng gợi ý `Autocomplete`, `p-select` của `Pagination`, đường ra của `Toast` có thể hỏng dưới `'noop'` — kẹt ở trạng thái mở, không dọn DOM — mà không test nào đỏ. Người đóng chỗ hở này là `test-engineer`; nó không phải điều kiện của ADR này |
| **`NoopAnimationStyleNormalizer`** | Khác biệt kỹ thuật duy nhất giữa hai API đã chọn và đã loại. Chưa ai chứng minh nó vô hại cho mọi component — nó chỉ chưa làm gì hỏng trong những gì đã chạy |
| **Thêm một hàm Core mà composition root phải gọi** | Dự án hạ nguồn quên gọi thì F39 đỏ — đó là mục đích. Nhưng đây là hàm `provideCore…` thứ tư, và mỗi hàm như vậy là một dòng dự án hạ nguồn phải biết tồn tại |
| **Hai nửa lệch nhau khi đổi cờ giữa phiên** | Cái giá người dùng đã chấp nhận, ghi ở DESIGN.md §7; nhắc ở đây vì nó là hệ quả trực tiếp của việc chọn một lần lúc khởi động |

### Điều kiện xét lại

1. **Kế hoạch nâng cấp chạm Angular 23**, hoặc thư viện UI chuyển lớp phủ khỏi bộ chạy hoạt ảnh của Angular. Lúc đó phân loại lại từng chuyển động theo DESIGN.md §7 và kiểm lại dưới cờ bật, **trước** khi gỡ nửa provider.
2. **Một lớp phủ được chứng minh hỏng dưới `'noop'`** — kẹt mở, không dọn DOM, sự kiện đóng không bắn. Nếu nguyên nhân là `NoopAnimationStyleNormalizer` thì phương án A được xét lại với chính cái giá bundle của nó.
3. **Độ trễ focus gây một lỗi đo được** — một phím bấm trong khoảng đó kích hoạt sai phần tử — hoặc một đợt soát trợ năng nêu nó. Lúc đó cần ADR sửa một phần 0073/0075 để hằng thời lượng của `Dialog` được phép phụ thuộc cờ.
4. **F37 đổi hình dạng vì một lý do khác** theo hướng cho phép hằng có điều kiện — điều 4 hết lý do hoãn.

## Liên quan

- [`../Design/DESIGN.md`](../Design/DESIGN.md) §7 — hai nửa thi hành, ba loại chuyển động; nguồn của cái giá "tải lại trang".
- [`0063-locale-id-den-tu-seam-core-i18n.md`](0063-locale-id-den-tu-seam-core-i18n.md) — tiền lệ dời một giá trị Core ra khỏi composition root.
- [`0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md`](0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md), [`0075-ba-component-lop-noi-vao-nhom-b-va-bac-thang-chon-theo-vai.md`](0075-ba-component-lop-noi-vao-nhom-b-va-bac-thang-chon-theo-vai.md) — luật F37 mà điều 4 không chạm.
- [`0028-toolchain-fe-va-ke-hoach-nang-cap.md`](0028-toolchain-fe-va-ke-hoach-nang-cap.md) — kế hoạch nâng cấp mà điều kiện xét lại 1 bám theo.
- [`../RULES.md`](../RULES.md) §7 — luật F39, F14, F36, F37.
