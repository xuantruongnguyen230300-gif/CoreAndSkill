---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 06. Chiến lược kiểm thử Frontend

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`; đây là bộ luật test mà giai đoạn 2 phải theo.
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
| ⚪ Thấp | Sai thì thấy ngay lần chạy đầu | Bố cục, màu, chuỗi hiển thị |

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
| Lỗi có envelope → thông báo mang đúng `message` | [`02-http-envelope.md`](02-http-envelope.md) §5.3 |
| Lỗi **không** có envelope (mất mạng, HTML từ proxy) → có câu dự phòng, không văng | §4.3 của file 02 |
| Sau khi qua interceptor, `error instanceof HttpErrorResponse` vẫn đúng | Bẫy spread, §5.1 của file 02 |
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
| Bốn trạng thái: rỗng, đang tải, lỗi, có dữ liệu | Bố cục, khoảng cách, màu |
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
| Test bố cục và pixel | Đỏ mỗi lần đổi thiết kế, và không bắt được lỗi logic nào |
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
> **Chưa nằm trong phạm vi F0–F3.** Viết khi có môi trường chạy ổn định và cách dựng dữ liệu thử. Ghi ra đây để nó là một khoản nợ nhìn thấy được, không phải một thiếu sót âm thầm.

---

## 7. Công cụ chạy test

**Karma + Jasmine, chạy qua `ng test`, trình duyệt ChromeHeadless** — chốt ở [`../../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md`](../../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md). Lý do: nó đi kèm framework, không cần cấu hình thêm, và một cấu hình test tự chế là thứ sẽ vỡ ở lần nâng Angular kế tiếp.

**Vitest không dùng ở v1.** Chuyển bộ chạy test đi cùng đợt nâng Angular trong kế hoạch của ADR-0028, không làm riêng lẻ — đổi bộ chạy test và đổi framework trong hai đợt tách rời là hai lần viết lại cấu hình test.

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

> 📐 Bảng dưới là **đích đến của giai đoạn 2**, không phải hiện trạng. Ý nghĩa ba ký hiệu trạng thái: [`../README.md`](../README.md) §9.

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Spec cạnh mọi `*.service.ts` | ✅ sẽ có | Luật F12, từ pha F0 |
| Test interceptor và guard | ✅ sẽ có | §2.2, §2.3 — hai nhóm đáng test nhất |
| Test component `shared/` ở bốn trạng thái | ✅ sẽ có |  |
| Sàn coverage ở §4, đo theo **nhánh** | ✅ sẽ có | **Luật F15**, nợ cổng ghi ở [`../../RULES.md`](../../RULES.md) §10; nợ cổng ở [`trien-khai/05-gate.md`](trien-khai/05-gate.md) §3.3 |
| Karma + Jasmine qua `ng test`, ChromeHeadless | ✅ sẽ có | §7 |
| Vitest | ❌ chưa | Điều kiện: đợt nâng Angular theo kế hoạch ở [`../../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md`](../../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md) (§7) |
| Bộ E2E cho bốn luồng ở §6 | ❌ chưa | Điều kiện: có cách dựng dữ liệu thử ổn định. Thiếu điều kiện này thì E2E sẽ đỏ ngẫu nhiên rồi bị tắt |
| Test hồi quy hình ảnh | ❌ chưa | Điều kiện: đợt nâng thư viện UI đầu tiên — [`16-nen-tang-va-nang-cap.md`](16-nen-tang-va-nang-cap.md) §4 |
| Test đột biến | ❌ chưa | Điều kiện: bộ test ổn định và có nghi ngờ về chất lượng khẳng định |
| Khẳng định theo chuỗi tiếng Việt hiển thị | ❌ loại, không hoãn `K21` | §2.4 — khẳng định theo khoá dịch |
| Gom spec vào một thư mục `tests/` riêng | ❌ loại, không hoãn `K22` | §3 |

> Cách đọc ba ký hiệu của bảng trên — và khi nào *"FE thiếu X"* là finding: [`../README.md`](../README.md) §9.

---

## 11. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| Test phía BE | [`../be/04-testing-strategy.md`](../be/04-testing-strategy.md) |
| Cái gì là finding khi review | [`../../quy-uoc/tieu-chi-review.md`](../../quy-uoc/tieu-chi-review.md) |
| Bẫy của interceptor cần test | [`02-http-envelope.md`](02-http-envelope.md) §5 |
| Guard và thứ tự guard | [`../../quy-uoc/fe-routing-guard.md`](../../quy-uoc/fe-routing-guard.md) |
| Cổng F12 chạy bằng gì | [`trien-khai/05-gate.md`](trien-khai/05-gate.md) |
