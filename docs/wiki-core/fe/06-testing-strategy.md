---
kind: luat
scope: core
verified: 2026-11-05
---

# 06. Chiến lược kiểm thử Frontend

> **Hiện trạng:** hàng ghi `✅ CÓ THẬT` ở §10 đã đối chiếu với `src/` ngày 2026-11-05, neo ở từng hàng; hàng mang `🚧` có bảng *có thật hôm nay → sẽ thành* — lưới giao diện ở §6.1, component `shared/` ngay dưới bảng §10. Hiện trạng repo: [`../../README.md`](../../README.md) mục *Trạng thái repo*.
>
> Đối chiếu phía BE: [`../be/04-testing-strategy.md`](../be/04-testing-strategy.md). Hai phía có ngưỡng khác nhau, có chủ đích — xem §6.

---

## 1. Nguyên tắc chi phối: test cái dễ hỏng im lặng

Test FE dễ trở thành hoạt động tốn công mà không bắt được gì: viết một trăm test kiểm rằng component hiển thị đúng chuỗi đã truyền vào, rồi một lỗi phân quyền thật đi thẳng ra sản phẩm.

Tiêu chí chọn: **ưu tiên thứ hỏng mà không báo lỗi, không văng exception, và không lộ ra khi bấm thử vài lần.**

| Ưu tiên | Vì sao | Ví dụ hỏng im lặng |
| --- | --- | --- |
| 🔴 Cao | Sai mà không có triệu chứng nhìn thấy | Guard thiếu một nhánh; khoá `fieldErrors` lệch hoa/thường; interceptor nuốt lỗi |
| 🟡 Vừa | Sai thì thấy được nhưng chỉ ở đúng một trạng thái ít gặp | Trạng thái rỗng, trạng thái lỗi, danh sách một phần tử |
| ⚪ Thấp | Sai thì thấy ngay lần chạy đầu | Câu chữ của bản dịch |

**Bố cục, màu, font không thuộc hạng thấp.** Giả định *thấy ngay lần chạy đầu* sai với chính lớp lỗi này: tám lỗi ở [ADR-0117](../../adr/0117-luoi-giao-dien-playwright-do-dom-theo-spec-design-axe-aa-api-gia-anh-chi-tham-khao.md) §Bối cảnh đi qua nhiều lần chạy mà không ai thấy. Lớp này không kiểm bằng unit test (§5) mà bằng lưới giao diện (§6.1).

---

## 2. Test gì ở từng loại tệp

### 2.1 Service — bắt buộc

**Mọi `*.service.ts` phải có `*.service.spec.ts` cạnh nó** (luật F12).

| Test gì | Cụ thể |
| --- | --- |
| Ánh xạ DTO → model | Field đổi tên, kiểu ngày, giá trị `null` từ server |
| Dựng tham số truy vấn | Trang, cỡ trang, sắp xếp, bộ lọc rỗng có bị gửi lên không |
| Nhánh lỗi | Ném lỗi có envelope → service lộ ra thông điệp nghiệp vụ, không phải chuỗi Angular sinh |
| Cache và vô hiệu hoá | Xem [`03-state-management.md`](03-state-management.md) §5 |

Dùng `HttpTestingController` để khẳng định **URL, phương thức và body** — đó chính là hợp đồng với BE. Test đó bắt được đúng loại lỗi mà TypeScript không bắt: gõ sai tên tham số truy vấn.

### 2.2 Interceptor — bắt buộc, và là nơi đáng test nhất

Interceptor là điểm mà **mọi** request đi qua. Một lỗi ở đây ảnh hưởng toàn app và thường chỉ lộ ra ở nhánh lỗi — tức lúc đã có sự cố.

| Test gì | Bẫy tương ứng |
| --- | --- |
| Lỗi có envelope → thông báo dịch từ mã lỗi; FE chưa có khoá dịch thì mang đúng `message` của BE | [`02-http-envelope.md`](02-http-envelope.md) §5.3 |
| Lỗi **không** có envelope (mất mạng, HTML từ proxy) → có câu dự phòng, không văng | §4.3 của file 02 |
| Lỗi ra khỏi interceptor là `ApiFailureError` mang envelope đã bóc và status thật — không phải `HttpErrorResponse`, không phải object spread | Bẫy spread, §5.1 của file 02 |
| 401 → phát tín hiệu hết phiên đúng một lần, không phải mỗi request một lần | [`07-auth-identity.md`](07-auth-identity.md) §7 |
| Cờ tắt thông báo mặc định có tác dụng | §4.4 của file 02 |
| Thứ tự chuỗi interceptor | Request đi ra đã mang tiền tố API **và** header XSRF |

### 2.3 Guard — bắt buộc

Guard là logic phân nhánh thuần, không có DOM — rẻ để test và đắt khi sai.

| Test gì |
| --- |
| Chưa đăng nhập → chặn và điều hướng kèm đường dẫn quay lại |
| Đã đăng nhập nhưng buộc đổi mật khẩu → ép sang màn đổi mật khẩu **từ mọi route** |
| Đang ở chính màn đổi mật khẩu → **không** lặp vô hạn |
| Thiếu permission → điều hướng tới đích đã định, không phải màn trắng |
| Có permission → cho qua |

Nhánh "không lặp vô hạn" là nhánh hay quên nhất, và triệu chứng của nó là trình duyệt treo — một lỗi tệ hơn nhiều so với việc chặn sai.

### 2.4 Component — có chọn lọc

| Test | Không test |
| --- | --- |
| Component `shared/` dùng ở nhiều nơi | Component chỉ dùng một chỗ và chỉ hiển thị dữ liệu truyền vào |
| Bốn trạng thái: rỗng, đang tải, lỗi, có dữ liệu | Bố cục, khoảng cách, màu — lưới giao diện đo, §6.1 |
| `output()` phát đúng lúc, đúng dữ liệu | Rằng thư viện UI bên dưới chạy |
| Hành vi bàn phím của modal, menu, dropdown | Nội dung chuỗi đã dịch |
| Nhãn liên kết đúng ô nhập | |

**Đừng khẳng định theo chuỗi hiển thị.** App bọc mọi câu qua i18n, nên khẳng định theo chuỗi tiếng Việt sẽ đỏ ngay lần đầu ai đó sửa một bản dịch — một test đỏ vì lý do không phải lỗi là test sẽ bị xoá. Khẳng định theo **khoá dịch** hoặc theo thuộc tính dữ liệu.

### 2.5 Store

Xem [`03-state-management.md`](03-state-management.md) §9.

### 2.6 Pipe và directive thuần

Rẻ, nhanh, và bắt được lỗi biên. Ưu tiên test pipe định dạng ngày, số, tiền tệ — chúng có nhiều trường hợp biên (giá trị `null`, số 0, số âm, múi giờ) hơn người ta tưởng.

### 2.7 Lớp cộng tác thuần tách khỏi component/trang lớn — bắt buộc (luật F26)

Khi một trang vượt ngưỡng dòng (§Ngưỡng ở [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §5), cách đúng là tách hằng số biên và hàm parse/validate ra một lớp cộng tác thuần (kiểu "flow"/"filters"/"helper") bên cạnh trang. Tách xong thì lớp đó **phải có `.spec.ts` riêng** — test tích hợp của trang (`*.page.spec.ts`) không tự động phủ được các hằng số biên nằm trong nó.

**Vì sao không được coi test tích hợp của trang là đủ:** một `.page.spec.ts` dài hàng trăm dòng, hàng chục khối `it`, vẫn hoàn toàn có thể không đụng tới một hằng số biên nào của lớp vừa tách — nó test *hành vi của trang qua các đường phổ biến*, không test *từng nhánh biên của logic thuần bên trong*. Đây là bài học có thật: một module tách đúng bảy file flow/filter khỏi trang, `.page.spec.ts` 724 dòng/50 khối test không hề nhắc tới các hằng số ngưỡng năm, regex kỳ báo cáo, hay giá trị kẹp % tiến độ nằm trong đó.

**Chỗ hay bị bỏ sót nhất:** input đến từ URL (tham số route, query string) và input người dùng gõ tay ở ô inline-edit — đây chính là hai loại input mà logic biên vừa tách ra tồn tại để xử lý.

---

## 3. Vì sao mọi service phải có spec cạnh nó

Đây là luật cứng (F12), và cổng kiểm nó bằng đối chiếu tên file — không kiểm nội dung.

**Vì sao ép ở mức thô như vậy:** một cổng đếm coverage tổng có thể xanh trong khi vài service không có test nào, miễn phần còn lại đủ dày. Cổng "mỗi service một spec" bắt đúng thứ mà coverage tổng che mất: **service không ai chạm tới**.

**Đặt spec cạnh file, không gom vào một thư mục `tests/`.** Ba lý do: đổi tên hoặc di chuyển service thì spec đi theo; mở file là thấy ngay có spec hay không; và một service mới không có spec lộ ra ngay trong diff của PR.

**Giới hạn phải nói thẳng:** cổng này kiểm **sự tồn tại của file**, không kiểm chất lượng test bên trong. Một spec rỗng vẫn qua. Nó chặn được ca "quên hẳn", không chặn được ca "viết cho có" — ca sau thuộc về review ([`../../quy-uoc/tieu-chi-review.md`](../../quy-uoc/tieu-chi-review.md)).

---

## 4. Ngưỡng coverage

Hai con số khác nhau, đừng lẫn:

- **Đích** — cột *Ngưỡng* của bảng dưới: mức mỗi tầng phải đạt. Thứ tự `core/` cao nhất, `modules/` thấp nhất của luật F15 áp vào cột này.
- **Sàn ở CI** — mức làm cổng đỏ. Đặt bằng **số đo coverage nhánh của tầng lúc bật cổng, làm tròn xuống tới số nguyên, trừ 1 điểm phần trăm** — tính trên tổng mọi tệp của tầng, không trên từng tệp. Tầng đo ra dưới đích thì sàn nằm dưới đích, rồi nâng dần bằng tay; tầng đo ra trên đích thì sàn vẫn đặt theo số đo, không hạ về đích. Sàn không tự nâng; hạ sàn là việc người dùng quyết, ghi ở [`../../adr/nhat-ky-quyet-dinh.md`](../../adr/nhat-ky-quyet-dinh.md). `modules/` nhận sàn theo cùng phép khi module đầu tiên có spec. Giá trị sàn khai ở **đúng một chỗ** — cấu hình test FE — và không chép vào tài liệu nào (chốt 2026-10-06, dòng cùng ngày ở nhật ký trên). ✅ CÓ THẬT (2026-11-05): sàn là hằng `SAN_NHANH` trong `src/FE/karma.conf.js`, reporter `coverage-floor` cùng tệp làm đỏ `ng test --code-coverage` — bước 3b ở [`trien-khai/05-gate.md`](trien-khai/05-gate.md) §5; `modules/` chưa có sàn. CI đã chạy bước đó chưa: dòng F15 ở [`../../RULES-FE.md`](../../RULES-FE.md).

| Phạm vi | Ngưỡng | Vì sao mức đó |
| --- | --- | --- |
| `core/` (interceptor, guard, auth, http) | **≥ 80%** | Mọi request và mọi lần điều hướng đi qua đây; lỗi ở đây lan ra toàn app |
| `shared/` | **≥ 70%** | Dùng lại nhiều nơi, nhưng phần lớn là hiển thị |
| `platform/` | **≥ 60%** | Màn hình; giá trị test giảm dần khi càng gần tầng hiển thị |
| `modules/` | **≥ 60%** | Do dự án sở hữu; ngưỡng để không tụt về 0 |

### 4.1 Ba điều phải hiểu về những con số này

**(1) Chúng là sàn, không phải mục tiêu.** Chạy theo phần trăm sinh ra test khẳng định lại chính code nó test. Một hàm được gọi trong test mà không có `expect` nào vẫn tính là đã phủ.

**(2) Đo theo nhánh, không chỉ theo dòng.** Ba nhánh `if` trong một hàm có thể đạt 100% dòng chỉ với một test đi qua một nhánh. Coverage theo dòng là chỉ số dễ đẹp và dễ vô nghĩa nhất.

**(3) Loại trừ phải có tên.** File cấu hình, file khai kiểu thuần, và điểm khởi động app không nên tính vào mẫu số. Nhưng danh sách loại trừ phải khai tường minh trong cấu hình test — không dùng mẫu rộng, vì một mẫu rộng sẽ âm thầm loại trừ code thật.

> 🛑 **Đừng chép con số coverage hiện tại vào bất kỳ tài liệu nào** ([`../../../.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §6). Chạy lệnh và đọc báo cáo.

---

## 5. Ba loại test không nên viết

| Đừng viết | Vì sao |
| --- | --- |
| Test khẳng định lại chính implementation | `expect(component.name()).toBe('x')` sau khi vừa set `name` là `'x'` — nó đúng kể cả khi logic sai |
| Test so điểm ảnh, hay khẳng định số đo bố cục viết cứng | Đỏ mỗi lần đổi thiết kế, và không bắt được lỗi logic nào. Khác phép đo của lưới giao diện (§6.1): phép đo neo một dòng spec `docs/Design/` và đọc giá trị token từ trang lúc chạy, nên đổi giá trị token không làm nó đỏ |
| Test mock hết mọi thứ tới mức chỉ còn kiểm mock | Nếu mọi phụ thuộc đều bị thay thế, cái được kiểm là bản dựng mock chứ không phải code |

---

## 6. E2E — chỉ cho vài luồng sống còn

E2E đắt gấp nhiều lần unit test ở mọi chiều: chậm, hay đỏ vì lý do không liên quan (mạng, thời gian chờ), và tốn công bảo trì mỗi khi giao diện đổi. Bù lại nó bắt được đúng loại lỗi mà không tầng nào khác bắt được: **các mảnh ghép đúng riêng lẻ nhưng sai khi ghép lại.**

Danh sách luồng E2E của Core — **cố ý ngắn**:

| Luồng | Vì sao sống còn |
| --- | --- |
| Đăng nhập → vào được màn đích | Hỏng là không ai dùng được gì |
| Tài khoản buộc đổi mật khẩu → bị ép sang màn đổi → đổi xong vào thẳng app | Nhiều mảnh (guard, interceptor, phiên) phải khớp nhau |
| Đăng xuất → route cũ bị chặn ngay, không cần tải lại trang | Bắt lỗi "state cũ sống sót sau đăng xuất" |
| Tạo một người dùng với dữ liệu sai → lỗi hiện **trên từng ô** | Bắt lỗi khoá `fieldErrors` lệch — thứ không unit test nào ở một phía bắt được |

Mọi thứ khác kiểm bằng unit test hoặc bằng tay.

**Điều kiện cần trước khi viết E2E:** một cách dựng dữ liệu ổn định (tài khoản thử biết trước trạng thái). Không có nó, E2E sẽ đỏ ngẫu nhiên và bị tắt trong vòng vài tuần — kết cục của gần như mọi bộ E2E bị bỏ rơi.

> **Công cụ: Playwright** (chốt 2026-09-10). Chạy được nhiều trình duyệt, viết bằng TypeScript như phần còn lại của FE, và **không phụ thuộc Angular** nên không vỡ khi nâng phiên bản framework — đúng chỗ Protractor đã chết và kéo theo mọi bộ E2E viết bằng nó.
>
> **Chưa nằm trong phạm vi F0–F3.** Viết khi có môi trường chạy ổn định và cách dựng dữ liệu thử — hàng *Bộ E2E* ở §10 giữ trạng thái và điều kiện, để nó là một thiếu sót nhìn thấy được, không âm thầm.

### 6.1 Lưới giao diện — không phải E2E luồng

Lưới giao diện chạy app thật bằng Playwright trên API giả đóng mặc định: đo DOM và bố cục theo spec `docs/Design/` (lớp A), quét axe WCAG A/AA — [ADR-0117](../../adr/0117-luoi-giao-dien-playwright-do-dom-theo-spec-design-axe-aa-api-gia-anh-chi-tham-khao.md); luật F56, F57. Nó không chạm BE, nên điều kiện *dựng dữ liệu thử ổn định* của §6 không áp cho nó — và nó không nói gì về cầu nối thật. Bốn luồng ở §6 vẫn hoãn.

**Phép đo lớp A:** A1–A7 và nguồn của chúng ở bảng *Lớp A* của ADR-0117 QC3. Người dùng chốt thêm ngày 2026-10-06 ([`../../adr/nhat-ky-quyet-dinh.md`](../../adr/nhat-ky-quyet-dinh.md)):

| Phép đo | Khác ADR ở chỗ | Nguồn |
| --- | --- | --- |
| A2 | Nhánh *chuỗi mang hình khoá dịch* có nguồn, bật được | [`DESIGN.md`](../../Design/DESIGN.md) §9, dòng ❌ khoá dịch |
| A4 | Chỉ đo `--font-sans`: mọi phần tử hiển thị chữ dùng họ đầu của nó, font đó đã nạp. Nhánh `--font-mono` bật khi có phần tử đầu tiên dùng `var(--font-mono)` | `DESIGN.md` §3.1, đoạn *Phần tử nào dùng họ nào* |
| A5 | Vế hover: ô hợp lệ giữ viền nghỉ, ô lỗi không đổi gì | `DESIGN.md` §2.6 *Ô nhập chữ*, dòng `hover` |
| A8 — mới | Phần tử ghi ở bảng *Cố ý không có* của spec màn không có trong DOM, ở mọi trạng thái được kiểm. Dòng không gọi tên phần tử — chỉ tả một trạng thái — không thành phép đo | Bảng *Cố ý không có* của từng spec ở `docs/Design/Screens/` |
| Trang không cuộn ngang | Không đo: người dùng chọn không thêm dòng spec, nên phép đo không có nguồn | — |

**A9–A12 — có trong code, chưa có quyết định ghi nhận.** ✅ CÓ THẬT (2026-11-05): mỗi phép đo một tệp dưới `src/FE/e2e/ha-tang/lop-a/`, chuỗi `ma: 'A9'` … `ma: 'A12'`, canary ở `canary-phep-do.spec.ts`; ADR-0117 và nhật ký quyết định không nhắc tới chúng.

| Phép đo | Đo gì | Nguồn |
| --- | --- | --- |
| A9 — `a9-auth-card-responsive.ts` | Khung xác thực theo bề rộng: dưới `$bp-xs` trải hết, không viền, không bo góc, đệm `--sp-6`, căn trên; từ `$bp-xs` tới `$bp-md` đệm `--sp-8`. Tự đổi khung nhìn rồi trả lại | [`AuthCard.md`](../../Design/Components/AuthCard.md) §Responsive, hai dòng `< $bp-xs` và `$bp-xs … $bp-md` |
| A10 — `a10-o-mat-khau-dem-phai.ts` | Ô có nút hiện/ẩn chồng lên: mép phải vùng chữ không vượt mép trái nút | [`AuthField.md`](../../Design/Components/AuthField.md) §Kích thước, đoạn đệm phải của `password`; [`Input.md`](../../Design/Components/Input.md) §Biến thể, dòng `password` |
| A11 — `a11-toast-khong-cuop-focus.ts` | Sau khi toast hiện, `document.activeElement` không nằm trong `app-toast` | [`Toast.md`](../../Design/Components/Toast.md) §Accessibility, dòng *Bàn phím* |
| A12 — `a12-focus-o-loi-dau.ts` | Gửi form còn lỗi → focus ở ô `aria-invalid="true"` đầu tiên theo thứ tự DOM | [`FormRow.md`](../../Design/Components/FormRow.md) §Accessibility dòng *Submit lỗi*, §Trạng thái dòng *Người dùng bấm submit*; [`01-dang-nhap.md`](../../Design/Screens/01-dang-nhap.md) §Trạng thái, mục *kiểm tra dữ liệu* |

**A1 trên URL `/`:** URL `/` không có spec màn. Màn nó dẫn tới lấy từ [`../../quy-uoc/fe-routing-guard.md`](../../quy-uoc/fe-routing-guard.md) §2.1 — nguồn ngoài `docs/Design/`; phép đo `<h1>` vẫn neo dòng Design của A1.

```bash
grep -ln '^| Cố ý không có' docs/Design/Screens/*.md  # spec màn đang làm nguồn cho A8
grep -rn 'var(--font-mono' src/FE/src                  # rỗng = nhánh mono của A4 chưa có gì để đo
grep -rn 'loiDaBiet:' src/FE/e2e/man                   # phép đo đang mang test.fail() vì lỗi đã biết
```

**Đồng hồ — chỉ bật ở spec cần ngày giờ.** Cách áp dòng *đồng hồ cố định qua `page.clock`* ở QC1 của ADR-0117 — kiến trúc sư chốt 2026-10-06 ([`../../adr/nhat-ky-quyet-dinh.md`](../../adr/nhat-ky-quyet-dinh.md)):

- Fixture không bật `page.clock` mặc định. Spec có trạng thái được đo mà chữ hay bố cục phụ thuộc ngày giờ hiện tại — lịch mở ở tháng hiện tại, thời gian tương đối, đếm ngược — bật qua một tuỳ chọn của fixture ở `src/FE/e2e/ha-tang/`, không gọi `page.clock` thẳng trong spec.
- Trước spec đầu tiên bật, hạ tầng có một ca canary chạy với đồng hồ bật: ngoại lệ ném trong `setTimeout` phải đỏ ở H1, nhánh nào cũng được. `ClockController` của Playwright chuyển ngoại lệ đó từ `pageerror` sang `console.error` — đo 2026-10-06, ghi ở đầu `src/FE/e2e/ha-tang/fixture.ts`.
- Màn có `debounceTime` — mọi màn danh sách, qua `src/FE/src/app/core/list/list-state.store.ts`, chuỗi `debounceTime(NGUNG_GO_MS)` — không dùng `setFixedTime`: nó giữ đứng `Date.now` trong khi timer vẫn chạy, còn `debounceTime` so `Date.now` với lúc gõ cuối nên không phát (đọc mã RxJS, chưa chạy thử).
- Cái giá: người viết spec phải tự nhận ra trạng thái phụ thuộc ngày giờ; sót thì kết quả đổi theo ngày chạy, và chỉ dấu hiệu *bất định* của ADR-0117 bắt được — muộn.

> 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG** — các việc ở bảng cuối ADR-0117.

| Có thật hôm nay (2026-11-05) | Sẽ thành |
| --- | --- |
| Cấu hình `e2e` build và serve: `src/FE/angular.json`, chuỗi `"with": "src/environments/environment.e2e.ts"` và `"port": 4300` | — |
| `src/FE/playwright.config.ts` bốn project — chuỗi `name: 'rong-sang'`, `name: 'vua-sang'`, `name: 'hep-sang'`, `name: 'rong-toi'`. `rong-sang` chạy mọi spec; ba project kia chạy mọi spec dưới `e2e/man/` (chuỗi `const SPEC_CO_LOP_A`), lọc theo thẻ trạng thái bằng `grep:` — bảng thẻ → project ở đầu tệp. Script `e2e`, `e2e:kieu` ở `src/FE/package.json` | — |
| API giả `src/FE/e2e/ha-tang/api-gia.ts`, chuỗi `ĐÓNG MẶC ĐỊNH`; bốn canary của việc T2 ở `src/FE/e2e/ha-tang/canary-api-gia.spec.ts`, chuỗi `DO request GET chưa khai báo`, `preflight qua được`, `mã bịa bị từ chối`, `thư mục rỗng cũng ĐỎ` | — |
| Lớp A ở `src/FE/e2e/ha-tang/lop-a/`, mỗi phép đo một tệp `a<số>-…`, khai dòng spec nguồn ở `nguon:`; axe `src/FE/e2e/ha-tang/axe.ts`, chuỗi `THE_AXE`; tệp ngoại lệ `ngoai-le-axe.ts` rỗng, mục mồ côi đỏ — chuỗi `moCoi`; ảnh tham khảo `anh-tham-khao.ts`, chuỗi `chupThamKhao`; canary phép đo `canary-phep-do.spec.ts`. A7 ép trạng thái tự điền bằng `CSS.forcePseudoState` — kiểm CSS của app, không kiểm tự điền thật. Áp ở mọi tệp spec dưới `src/FE/e2e/man/`: màn xác thực qua `lopA(` của `ca-xac-thuc.ts`, màn trong khung — gồm trang chủ và màn đợt 2 — qua `phuManTrongKhung(` của `ca-trong-khung.ts`; ca khói vẫn có (chuỗi `vaoVaKiemMotH1`). Dấu lỗi đã biết: `src/FE/e2e/man/ca-xac-thuc.ts`, chuỗi `if (p.loiDaBiet) test.fail(true` | Mỗi trạng thái ở mục *Trạng thái* của spec màn có ca — lượt đối chiếu 2026-11-05 chưa xét từng trạng thái; lớp nổi — hàng 4 của bảng trạng thái QC3, chưa xét lớp nào đã có ca |
| Đồng hồ cố định chưa bật — đầu `src/FE/e2e/ha-tang/fixture.ts`, chuỗi `CHƯA bật` | Tuỳ chọn fixture và ca canary H1 ở đoạn *Đồng hồ* trên, khi có spec đầu tiên cần |
| Cả lưới chạy cục bộ khoảng 6 phút (lượt T3a đo trên máy dev, chưa đo lại) — ADR ước 2–3 phút. `.github/workflows/docs-gate.yml` không có bước 5 — `grep -n 'e2e' .github/workflows/docs-gate.yml` rỗng | Bước 5a–5d của [`trien-khai/05-gate.md`](trien-khai/05-gate.md) §5 trong `frontend-gate`; thời gian CI đo ở lần chạy đầu |

---

## 7. Công cụ chạy test

**Karma + Jasmine, chạy qua `ng test`, trình duyệt ChromeHeadless** — chốt ở [`../../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md`](../../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md). Lý do: nó đi kèm framework, không cần cấu hình thêm, và một cấu hình test tự chế là thứ sẽ vỡ ở lần nâng Angular kế tiếp.

**Vitest chưa dùng.** Chuyển bộ chạy test **không** đi cùng đợt nâng bản lớn 2026-11-06: PR đó đã gộp nhiều thay đổi dựa vào `ng test`, nên bộ chạy giữ nguyên để một lần đỏ còn quy được về một nguyên nhân. Việc chuyển làm ở PR riêng ngay sau đó, muộn nhất trước lần nâng bản lớn kế tiếp, để không lần nâng nào đổi cùng lúc framework và bộ chạy test — [ADR-0126](../../adr/0126-hoan-chuyen-vitest-sang-pr-rieng-ngay-sau-pr-nang-angular-21-muon-nhat-truoc-angular-22.md).

Chạy trong CI ở chế độ không giám sát với ChromeHeadless. Nếu môi trường chưa trỏ được tới trình duyệt, bộ chạy sẽ hỏng với thông báo về việc không tìm thấy trình duyệt — đó là lỗi môi trường, không phải lỗi test.

---

## 8. Một test phải TỪNG ĐỎ trước khi được coi là xong

Một test viết xong xanh ngay từ đầu **không chứng minh được gì** — nó có thể đang xanh vì khẳng định sai, vì mock trả đúng thứ nó khẳng định, hoặc vì nó không chạy nhánh cần chạy.

Quy trình bắt buộc cho mọi test của `core/`:

1. Viết test khẳng định hành vi **đúng**.
2. Chạy — phải **đỏ** (code chưa có, hoặc tạm sửa code cho sai).
3. Sửa code cho xanh.
4. Hoàn nguyên bước sửa tạm nếu có.

Đây chính là luật T1 của [`../../RULES.md`](../../RULES.md) áp cho phía FE: **một cổng hỏng âm thầm còn tệ hơn không có cổng, vì nó tạo cảm giác được bảo vệ.**

---

## 9. Kiểm chứng

- [ ] Cổng F12 xanh — lệnh ở [`trien-khai/05-gate.md`](trien-khai/05-gate.md) §8.9. Lệnh đó in **đích danh** từng service thiếu spec: so hai tổng số thì một service thiếu spec bù bằng một spec mồ côi vẫn cho hai số bằng nhau
- [ ] Mọi interceptor có spec phủ cả nhánh có envelope lẫn nhánh không có envelope
- [ ] Mọi guard có spec phủ đủ nhánh, gồm nhánh chống lặp vô hạn
- [ ] Coverage đạt sàn ở §4, đo theo **nhánh**
- [ ] Ít nhất một test của `core/` đã được chứng minh là từng đỏ
- [ ] Không test nào khẳng định theo chuỗi tiếng Việt hiển thị

---

## 10. §Áp dụng — Core này có gì, cố ý thiếu gì

> Hàng `✅ CÓ THẬT` đã đối chiếu với `src/` ngày 2026-11-05, neo ở cột *Ghi chú*; hàng `🚧` có bảng *có thật hôm nay → sẽ thành*. Ý nghĩa ba ký hiệu trạng thái: [`../README.md`](../README.md) §9.

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Spec cạnh mọi `*.service.ts` | ✅ CÓ THẬT (2026-11-05) | Luật F12, từ pha F0 — `scripts/fe-gate.sh`, chuỗi `section "F12 - `; lệnh in đích danh service thiếu spec ở [`trien-khai/05-gate.md`](trien-khai/05-gate.md) §8.9 |
| Test interceptor và guard | ✅ CÓ THẬT (2026-11-05) | §2.2, §2.3 — hai nhóm đáng test nhất. Mỗi tệp ở `src/FE/src/app/core/interceptors/` và `src/FE/src/app/core/guards/` có `.spec.ts` cạnh nó, ví dụ `error.interceptor.spec.ts`, chuỗi `toBeInstanceOf(ApiFailureError)`; từng nhánh ở bảng §2.2, §2.3 chưa đối chiếu |
| Test component `shared/` ở bốn trạng thái | 🚧 ĐÃ CHỐT — ĐANG THI CÔNG | Bảng *có thật hôm nay → sẽ thành* ở cuối mục này |
| Sàn coverage ở §4, đo theo **nhánh** | ✅ CÓ THẬT (2026-11-05) | **Luật F15** ở [`../../RULES-FE.md`](../../RULES-FE.md); nơi đặt lệnh ở [`trien-khai/05-gate.md`](trien-khai/05-gate.md) §3.3 — hằng `SAN_NHANH` và reporter `coverage-floor` ở `src/FE/karma.conf.js` (§4) |
| Karma + Jasmine qua `ng test`, ChromeHeadless | ✅ CÓ THẬT (2026-11-05) | §7 — `src/FE/angular.json`, chuỗi `"builder": "@angular/build:karma"`; `src/FE/karma.conf.js`, chuỗi `frameworks: ['jasmine']`. ChromeHeadless qua cờ `--browsers=ChromeHeadless` ở `.github/workflows/docs-gate.yml`; `karma.conf.js` tự khai `browsers: ['Chrome']` cho máy dev |
| Vitest | ❌ chưa | Điều kiện: PR riêng, mở ngay sau khi PR nâng bản lớn 2026-11-06 hợp nhất, kèm ADR lật phần hoãn; muộn nhất trước lần nâng bản lớn kế tiếp — [ADR-0126](../../adr/0126-hoan-chuyen-vitest-sang-pr-rieng-ngay-sau-pr-nang-angular-21-muon-nhat-truoc-angular-22.md), tạm hoãn một phần [ADR-0028](../../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md) quyết định 5 (§7) |
| Bộ E2E cho bốn luồng ở §6 | ❌ chưa | Điều kiện: có cách dựng dữ liệu thử ổn định. Thiếu điều kiện này thì E2E sẽ đỏ ngẫu nhiên rồi bị tắt |
| Lưới giao diện — Playwright, API giả, axe | 🚧 ĐÃ CHỐT — ĐANG THI CÔNG | §6.1 — bảng *có thật hôm nay → sẽ thành* ở đó; luật F56, F57 |
| So ảnh chụp với ảnh chuẩn commit vào repo | ❌ loại, không hoãn `K53` | Phương án B của [ADR-0117](../../adr/0117-luoi-giao-dien-playwright-do-dom-theo-spec-design-axe-aa-api-gia-anh-chi-tham-khao.md): ảnh chuẩn thành nguồn giao diện thứ hai cạnh `docs/Design/`. Ảnh của lưới chỉ để tham khảo. Đợt nâng thư viện UI vẫn so ảnh bằng mắt — [`16-nen-tang-va-nang-cap.md`](16-nen-tang-va-nang-cap.md) §4 |
| Lưới trên Firefox, WebKit | ❌ chưa | Điều kiện: một lỗi lọt riêng ở Firefox hay Safari — ADR-0117 phương án F |
| Test đột biến | ❌ chưa | Điều kiện: bộ test ổn định và có nghi ngờ về chất lượng khẳng định |
| Khẳng định theo chuỗi tiếng Việt hiển thị | ❌ loại, không hoãn `K21` | §2.4 — khẳng định theo khoá dịch |
| Gom spec vào một thư mục `tests/` riêng | ❌ loại, không hoãn `K22` | §3 |

> Cách đọc ba ký hiệu của bảng trên — và khi nào *"FE thiếu X"* là finding: [`../README.md`](../README.md) §9.

> 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG** — hàng *Test component `shared/` ở bốn trạng thái*.

| Có thật hôm nay (2026-11-05) | Sẽ thành |
| --- | --- |
| Có component `shared/` mang spec cạnh nó, ví dụ `src/FE/src/app/shared/ui/data-table/data-table.component.spec.ts`; còn component chưa có spec nào — lệnh dưới in chúng. Vế *bốn trạng thái* trong các spec đã có chưa đối chiếu | Mọi component `shared/` có spec phủ bốn trạng thái của §2.4 |

```bash
# Thư mục component shared/ chưa có tệp spec nào. PASS vế "có spec" = không in dòng nào.
find src/FE/src/app/shared/ui src/FE/src/app/shared/components -mindepth 1 -maxdepth 1 -type d \
  '!' -exec sh -c 'ls "$1"/*.spec.ts >/dev/null 2>&1' _ {} ';' -print
```

---

## 11. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| Test phía BE | [`../be/04-testing-strategy.md`](../be/04-testing-strategy.md) |
| Cái gì là finding khi review | [`../../quy-uoc/tieu-chi-review.md`](../../quy-uoc/tieu-chi-review.md) |
| Bẫy của interceptor cần test | [`02-http-envelope.md`](02-http-envelope.md) §5 |
| Guard và thứ tự guard | [`../../quy-uoc/fe-routing-guard.md`](../../quy-uoc/fe-routing-guard.md) |
| Cổng F12 chạy bằng gì | [`trien-khai/05-gate.md`](trien-khai/05-gate.md) |
