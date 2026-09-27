---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0049 — Trạng thái và quy trình của một hộp thoại sống ở `state/*.store.ts`, không ở `services/`; component của feature không nhận store

> **Trạng thái:** Đã chấp nhận (2026-09-21)

## Bối cảnh

Khu quản trị đơn vị (`src/FE/src/app/platform/he-thong/don-vi/`) tách logic của hai nhóm hộp thoại ("Tạo đơn vị", "Khôi phục quản trị / Tạo quản trị mới") khỏi page để `*.page.ts` không vượt ngưỡng cứng của luật F22. Cách tách: hai lớp thường `TaoDonViHop` và `KhoiPhucTaoQuanTriHop`, đặt trong `services/`, page tự `new`, mỗi lớp giữ `FormGroup`, các cờ `signal` (mở/đóng, đang lưu, hỏi xác nhận huỷ), gọi `DonViService` và dịch mã lỗi thành lỗi từng ô. Hai component `HopTaoDonViComponent`, `HopQuanTriDonViComponent` nhận cả lớp đó qua `input.required()` và tự khai là component dumb.

Khuôn này không do tài liệu nào cho phép. Nó được sao từ `GanVaiTroChon` ở `platform/quan-tri/nguoi-dung/services/`, và `TraCuuVaiTro` ở `platform/quan-tri/vai-tro/services/` cùng hình dạng. Đếm bằng lệnh: `find src/FE/src/app/platform -path '*/services/*' -type f ! -name '*.service.ts' ! -name '*.spec.ts'` ra bốn tệp.

Bảng trách nhiệm ở [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §3.1 đã cấm `services/*` giữ trạng thái UI, và §5 đã chỉ đường khi page vượt ngưỡng: *"state phức tạp thì lên `state/`"*. Tức là F22 chưa bao giờ đòi đặt logic này ở `services/` — page hôm nay 267 dòng, cộng hai lớp trên (142 và 194 dòng) sẽ ra hơn 600, nên **tách là bắt buộc, còn tách vào đâu thì không bị ép**.

Ba thứ hỏng cùng nguồn:

1. **Không cổng nào nhìn tới.** Luật F12 (mọi `*.service.ts` có `.spec.ts`) khớp theo *đuôi tên*; tệp `tao-don-vi-hop.ts` không mang đuôi đó nên thoát. Luật F11 chỉ quét `shared/components/` ([`../wiki-core/fe/trien-khai/05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) đã ghi lỗ mù này). Kết quả đo được: dưới `platform/` chỉ có `*.service.spec.ts`, không một spec nào cho bốn lớp trên. Cùng lượt review ghi nhận hai lỗi nghiêm trọng nằm đúng trong logic đó (hộp không đóng sau thành công; lỗi 422 thiếu `fieldErrors` thì im lặng) — hai lỗi này là lời của người review, chưa được đối chiếu ở đây; điều đã đối chiếu là lớp chứa chúng không có spec và không cổng nào nhìn tới.
2. **Cửa sau của F11.** Component không `inject` gì, nhưng `hop()` mang theo `DonViService` và tự gọi API. Câu hỏi F11 muốn hỏi — component có biết dữ liệu đến từ đâu không — vẫn trả lời "có", chỉ là không qua từ khoá `inject(`. Nới F11 sang `platform/*/components/` không bắt được nó, vì phép dò tìm `inject(`, còn ở đây là `input()`.
3. **Ngoài tầm quy ước state.** [`../wiki-core/fe/03-state-management.md`](../wiki-core/fe/03-state-management.md) §1 xếp UI state vào `signal()` trần trong page và server state vào `state/`; một *quy trình* (form + gửi + xử lý lỗi + hỏi xác nhận huỷ) lớn hơn một signal trần nhưng không phải server state, nên không nơi nào được gọi tên — và người viết chọn chỗ dễ nhất.

## Quyết định

1. Trạng thái UI và quy trình của một hộp thoại hoặc luồng nhiều bước, khi nó lớn tới mức làm page vượt ngưỡng, sống ở `<feature>/state/<ten>.store.ts`. `services/` của feature chỉ chứa `*.service.ts`, `*.service.spec.ts` và `*.mapper.ts`; mapper của feature nằm ở `services/`, không ở `models/` (đúng như [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §3 đã viết).
2. Store là lớp `@Injectable()`, lấy service của feature bằng `inject()`, và được cấp ở `providers` của page (hoặc route) để chết cùng màn. Lớp thường khởi tạo bằng `new` chỉ được phép khi **một page cần nhiều thể hiện độc lập** của cùng một lớp (trường hợp `TraCuuVaiTro`: ba ô tìm kiếm, ba trạng thái riêng); lý do phải nằm ở đầu tệp.
3. Mỗi `*.store.ts` có `*.store.spec.ts` cạnh nó.
4. `components/` của feature không import tệp `state/*.store` và không nhận store qua `input()`. Page đọc store rồi truyền **giá trị** (form, cờ, chuỗi lỗi) xuống bằng `input()`, và nghe `output()` để gọi hàm của store.

`architect` chốt bốn điều này; ba luật F31, F32, F33 ở [`../RULES.md`](../RULES.md) §7 là phần ép, và chưa có cổng nào chạy — xem Hệ quả.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Viết ADR hợp thức hoá "lớp trạng thái hộp thoại" ở `services/`, nới F11 sang `platform/*/components/`

**Được:** không đổi code nào; phản ánh đúng thứ đang chạy; đường của [`../wiki-core/fe/trien-khai/05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) đã vạch sẵn cho việc nới F11.
**Mất:** ba thứ. Một, `services/` thành nơi chứa cả gọi API lẫn trạng thái UI — xoá đúng ranh giới mà §3.1 dựng để tìm lỗi nhanh. Hai, F12 khớp theo đuôi tên, nên để lớp này ở `services/` với tên tự do thì spec của nó **không bao giờ** bị ép; muốn ép phải bịa đuôi tên mới cho một thư mục vốn dành cho `*.service.ts`. Ba, cửa sau không đóng: F11 dò `inject(`, còn component nhận đối tượng qua `input()` vẫn lọt.
**Vì sao loại:** nó biến một vi phạm thành ngoại lệ hợp lệ mà không đổi được điều làm nó nguy hiểm — logic gọi API và xử lý lỗi nằm ở chỗ không cổng nào canh, và hai lỗi nghiêm trọng đã chứng minh điều đó.

### Phương án B — Chỉ chuyển hai lớp sang `state/`, giữ nguyên `new` trong page và `input.required<Lớp>()` ở component

**Được:** đổi tên và thư mục là một lượt cơ học; không đụng template.
**Mất:** lớp vẫn không phải store theo khuôn của `03-state-management.md` §4 (không DI, không `providers`, không test được bằng `TestBed`), và component vẫn nhận đối tượng mang hành vi gọi API — cửa sau giữ nguyên, chỉ đổi địa chỉ.
**Vì sao loại:** giải quyết tên thư mục, không giải quyết điều đã hỏng. Người review vẫn không có gì để phân biệt "component nhận giá trị" với "component nhận một cái đã biết API".

### Phương án C — Đưa logic trở lại page, bỏ tách

**Được:** hết lớp trung gian, không có cửa sau.
**Mất:** page ~600 dòng, vượt ngưỡng cứng 400 của F22; đây chính là lý do việc tách xảy ra.
**Vì sao loại:** F22 là cổng đang chạy, và là cổng duy nhất canh cỡ file. Chọn phương án này nghĩa là nới F22 — bị cấm ở phạm vi giao việc, và cũng không có lý do.

### Phương án D — Component hộp thoại là component "thông minh": tự cấp store ở `providers` của chính nó và tự `inject` nó

**Được:** gọn nhất về số dòng; page chỉ bật cờ mở.
**Mất:** những component này không còn dumb, tức `components/` mất nghĩa; và F11 sau khi nới không phân biệt được "inject store của chính mình" với "inject service dữ liệu bất kỳ", nên mở cửa cho cả hai.
**Vì sao loại:** cần một loại component thứ ba mà tài liệu chưa có, cho hai component. Chưa đủ lý do để mở rộng bảng §3.1; nếu sau này có đủ ca, đó là ADR riêng.

## Hệ quả

### Tích cực

- Logic hộp thoại nằm ở nơi test được bằng `TestBed` với service giả, và F32 (khi có cổng) bắt được việc thiếu spec bằng đúng tên tệp — máy không cần hiểu logic. Điều này cũng cho dòng nợ F26 ở [`../RULES.md`](../RULES.md) §10 một quy ước tên mà nó đã nói là còn thiếu, cho riêng lớp store.
- Hai luật đọc được bằng `find` và `grep` (F31, F33), không cần đọc hiểu.
- `services/` lại có đúng một nghĩa: gọi API và ánh xạ DTO ↔ model.
- F22 giữ nguyên, không ai phải lách: page chỉ `inject` store, không tự chứa nó.

### Tiêu cực

- **Việc thi công không phải một lượt đổi tên.** Component dumb thật đòi thêm `input()`/`output()`: mỗi hộp mang vài cờ, một `FormGroup` và bốn năm sự kiện, và `*.page.html` thêm binding tương ứng. Cỡ `hop-quan-tri-don-vi.component.html` hiện 196 dòng (ngưỡng cứng 250 của `*.html`) và `danh-sach-don-vi.page.html` 124 dòng — phải đo lại sau khi làm, không đoán.
- Cả ba luật mới chưa có cổng. Hôm nay bốn tệp ở `services/`, năm tệp mapper ở `models/`, hai component nhận đối tượng, và `core/list/list-state.store.ts` (không có spec) đều vi phạm mà không cổng nào đỏ. Nợ được ghi ở `RULES.md` §10; đến khi có cổng, chỉ review người canh.
- `state/` không có ngưỡng cỡ tệp: bảng ở `fe-architecture.md` §5 không có hàng cho `*.store.ts`. Một store nhận hết quy trình của một page có thể phình thành "god store" và không cổng nào kêu.
- Kỷ luật `moHop()` reset trạng thái khi mở lại vẫn thuộc người viết — DI theo page không tự làm việc đó, và lỗi đóng/mở hộp mà review ghi nhận cho thấy chỗ này dễ sai.
- Quy tắc "được `new` khi cần nhiều thể hiện" là một ngoại lệ có thể bị mượn để né DI. Chỉ một lớp (`TraCuuVaiTro`) hôm nay thoả nó; số ca tăng thì xét lại.

### Rút lui nếu sai

Quyết định chỉ chạm vị trí tệp và cách nối, không chạm dữ liệu, hợp đồng API hay hành vi hiển thị. Nếu sáu tháng sau thấy bốn điều trên gây nhiều ma sát hơn giá trị: viết ADR mới lật ADR này, ba luật F31–F33 đổi mức hoặc gỡ ở `RULES.md`, và việc đảo là một lượt đổi vị trí tệp cùng đường nhập — cùng cỡ với lượt thi công đi tới. Không có bước nào không đảo được.

**Dấu hiệu quyết định này bắt đầu sai:** hai ba store liên tiếp mà component dumb của chúng cần hơn khoảng mười `input()` để hiển thị được; hoặc ngoại lệ `new` ở điều 2 được viện dẫn lần thứ hai.

## Liên quan

- [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §3.1, §3.2, §5 — bảng trách nhiệm, khi nào cần `state/`, ngưỡng cỡ file.
- [`../wiki-core/fe/03-state-management.md`](../wiki-core/fe/03-state-management.md) §1, §4.1, §9 — khuôn store, quy tắc component dumb, cách test.
- [`../RULES.md`](../RULES.md) §7 — F11, F12, F22, và ba luật mới F31–F33; §10 — nợ ép.
- [`0007-fe-giu-cau-truc-thu-muc.md`](0007-fe-giu-cau-truc-thu-muc.md) — nền của bốn tầng thư mục.
