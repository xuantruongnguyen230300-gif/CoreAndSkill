---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Gọi API từ Frontend — envelope, interceptor, ranh giới DTO

> 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG** (đối chiếu 2026-09-21 cho §4.2, §5.1 và ba điểm của §2.2 — bản sao context ở nhánh CSRF, chỗ đặt `dichLoi`, điều kiện phương thức của nhánh CSRF; bổ sung 2026-09-23 cho §2.1, nhánh blob của §2.2, luật 2–5 của §6.4 và §6.2. Phần còn lại chưa ai đối chiếu):
>
> | Có thật hôm nay | Sẽ thành |
> | --- | --- |
> | `src/FE/src/app/core/http/paged.model.ts` — `interface PagedList`, `interface PageQuery` khớp §5.1 | — |
> | `src/FE/src/app/platform/he-thong/don-vi/services/don-vi.mapper.ts` — `mapDonVi`; cả năm `*.mapper.ts` của `platform/` nằm ở `services/`, khớp §4.2 ([ADR-0049](../adr/0049-trang-thai-quy-trinh-hop-thoai-o-state-khong-o-services.md)) | — |
> | `src/FE/src/app/core/interceptors/error.interceptor.ts` — `ctxDaThuLaiXsrf` dựng bản sao context trước khi gắn cờ; `const xuLyLoi =` dựng theo request và `catchError(xuLyLoi(guiLai))` bọc lần gửi lại sau CSRF; `src/FE/src/app/core/http/dich-loi.ts` — `export function dichLoi`, `export function thamSoRetryAfter`; cùng file `error.interceptor.ts`: `const LENH_AN_TOAN` và `!LENH_AN_TOAN.has(yeuCau.method)` ở điều kiện nhánh CSRF | — |
> | §2.1 và §6.4 luật 2 (đối chiếu 2026-09-23): `src/FE/src/app/core/http/api-result.model.ts` — `export const CO_TAC_DUNG_PHU`; `core/interceptors/auth.interceptor.ts` — `const canToken = GHI.has(req.method) || req.context.get(CO_TAC_DUNG_PHU);`; `error.interceptor.ts` — `const duocGuiLai = !LENH_AN_TOAN.has(yeuCau.method) || yeuCau.context.get(CO_TAC_DUNG_PHU);` | — |
> | §2.2 nhánh blob và §6.4 luật 3–5 (đối chiếu 2026-09-23): `error.interceptor.ts` — `if (err.error instanceof Blob)` bóc thân trước khi dịch; `src/FE/src/app/core/http/tai-tep.ts` — `export function tenTepTuPhanHoi(`, `export function luuTepXuat(`, nhánh `header === null` **ném lỗi** chứ không đặt tên dự phòng | — |
> | §2.2 lớp lỗi xuyên suốt và §1 `status` **đã khớp** (đối chiếu 2026-09-25): `src/FE/src/app/core/http/loi-xuyen-suot.ts` — `export function laLoiXuyenSuot(`; `error.interceptor.ts` — `if (laLoiXuyenSuot(err.status, body))` đứng trước nhánh 429, và `new ApiFailureError(body, err.status)`; `dich-loi.ts` — `export function dichLoiChoMan(`; `api-result.model.ts` — `readonly status = 0,`; `vi.json` có `loi.CORE.SYSTEM.UNEXPECTED` | — |
> | §2.1 `lamMoi`, §2.2 ý 2 và 5xx không envelope (đối chiếu 2026-09-26): `error.interceptor.ts` — `translate.instant('loi.CORE.CLIENT.SERVER_UNAVAILABLE')`, `xsrf.lamMoi({ boQuaToastLoi:`; `xsrf-token.store.ts` — `lamMoi(tuyChon:`; `vi.json` có khoá | — |
> | 🚧 §5.2 `context` (chốt 2026-09-25): `crud.ts` — `export function theoId<`, `export function danhSachTrang<` chưa nhận `HttpContext`; để né, `NguoiDungService.chiTiet`, `VaiTroService.chiTiet`, `timKiem(searchText: string` gọi thẳng `HttpClient` | Hai hàm nhận `context?`; ba phương thức quay về hàm chung |
> | §6.4 luật 1 và luật 6 **chưa đối chiếu được**: chưa service nào gọi endpoint xuất, và `TIMEOUT_XUAT_FILE` của §6.3 chưa có trong `src/FE` | Màn đầu tiên có nút xuất dựng theo sáu luật §6.4, khai hằng số timeout cùng lượt |
> | 📐 §6.2 và §6.3 **chưa thi công** (đối chiếu 2026-09-23, 2026-09-25): `src/FE/src/app/core/http/` không có tệp `retry.ts` nào, và `thuLaiKhiLoiMang`/`TIMEOUT_MAC_DINH`/`TIMEOUT_XUAT_FILE` không xuất hiện ở đâu dưới `src/FE`. Khuôn để dựng của cả hai mục đã chuyển sang [`fe-api-client-chua-thi-cong.md`](fe-api-client-chua-thi-cong.md), không phải thứ `import` được hôm nay — đọc nó như mã có sẵn thì lỗi biên dịch, và lối thoát dễ nhất khỏi lỗi đó là viết bản thứ hai ngay trong feature | Service đầu tiên cần retry dựng `core/http/retry.ts` theo khuôn ở [`fe-api-client-chua-thi-cong.md`](fe-api-client-chua-thi-cong.md) §6.2 rồi áp tại service đó. **Một** bản, ở `core/` — retry rải mỗi feature một bản là ca §5 của CLAUDE.md. Timeout: khai hằng số theo khuôn §6.3 của tệp đó, luật ép bằng [F42](../DEBT.md) |
>
> **Hình dạng envelope có đúng một file chủ: [`be-api-controller.md`](be-api-controller.md).** §1 khai kiểu TypeScript **phản chiếu** nó; lệch thì file BE thắng, file này sửa theo.

---

## 1. Envelope — hình dạng mọi response

Hình dạng trên dây thuộc [`be-api-controller.md`](be-api-controller.md) §2.1; **đừng chép sang phía FE**.

**Kiểu envelope phía FE — định nghĩa gốc.** Khai đúng một chỗ, ở `core/http/`, và phản chiếu 1:1 hình dạng ở file chủ BE. File FE khác nhắc tới envelope thì trỏ về mục này.

```typescript
// core/http/api-result.model.ts — khối import ở ly-do §1
/** Mã lỗi nghiệp vụ do BE khai trong catalog. Chuỗi ổn định, KHÔNG dịch, KHÔNG hiển thị. */
export type BusinessCode = string;

/** Tham số của thông điệp, truyền theo TÊN. Khoá giữ nguyên như BE gửi. */
export type MessageParams = Readonly<Record<string, string>>;

/** Một lỗi của một ô nhập. `code` là khoá dịch, KHÔNG phải câu hiển thị. */
export interface ApiFieldError {
  readonly code: BusinessCode;
  readonly messageParams: MessageParams | null;
}

export interface ApiError {
  /** Mã nghiệp vụ để FE ra quyết định và tra bảng dịch. */
  readonly code: BusinessCode;

  /** Loại lỗi do BE khai. DEV-FACING — KHÔNG rẽ nhánh theo trường này; rẽ theo mã HTTP, và theo `code` khi cần. */
  readonly type: string;

  /** Câu mặc định của BE, dev-facing — chỉ dùng làm đường lùi khi FE chưa có bản dịch. */
  readonly message: string;

  readonly messageParams: MessageParams | null;

  /** Lỗi theo từng ô nhập. Khoá là tên property phía BE; giá trị là DANH SÁCH MÃ LỖI. */
  readonly fieldErrors: Readonly<Record<string, readonly ApiFieldError[]>> | null;
}

export interface ApiSuccess<T> {
  readonly success: true;
  readonly data: T;
  readonly error: null;
  readonly traceId: string;
}

export interface ApiFailure {
  readonly success: false;
  readonly data: null;
  readonly error: ApiError;
  readonly traceId: string;
}

export type ApiResult<T> = ApiSuccess<T> | ApiFailure;

/** Đọc envelope ra khỏi lỗi HTTP. `null` là BÌNH THƯỜNG (mất mạng, proxy trả HTML). Nhận diện bằng tên field CÓ THẬT trên dây. */
export function docEnvelopeLoi(err: HttpErrorResponse): ApiFailure | null {
  const body: unknown = err.error;
  return body && typeof body === 'object' && 'success' in body && 'traceId' in body
    ? (body as ApiFailure)
    : null;
}

/** Cờ tắt toast mặc định cho ĐÚNG một request — không phải cờ toàn cục; không tắt lớp lỗi xuyên suốt (§2.2). */
export const BO_QUA_TOAST_LOI = new HttpContextToken<boolean>(() => false);

/** Cờ cho request mà 401 là câu trả lời BÌNH THƯỜNG, không phải hết phiên — §2.5. */
export const BO_QUA_HET_PHIEN = new HttpContextToken<boolean>(() => false);

/** Envelope ĐÃ BÓC (`null` khi không phải envelope) + HTTP status thật (`0`: không phản hồi, lỗi `unwrapData`) — §2.2. */
export class ApiFailureError extends Error {
  constructor(
    readonly body: ApiFailure | null,
    readonly status = 0,
  ) {
    // Mã lấy từ danh mục ở be-cqrs-handler.md §7.4 — FE KHÔNG tự chế mã.
    super(body?.error.code ?? 'CORE.CLIENT.NO_CONNECTION');
  }
}
```

🛑 **`code`, `messageParams` và `fieldErrors` nằm TRONG `error`, không nằm phẳng ở gốc envelope.**

**Giá trị của `fieldErrors` là mảng OBJECT, không phải mảng chuỗi.** Cách dùng đúng: dịch từng `code` — xem [`../wiki-core/fe/09-forms-validation.md`](../wiki-core/fe/09-forms-validation.md) §4.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §1

### 1.1 Bốn field ít người dùng đúng

| Field | Dùng để làm gì | Sai lầm thường gặp |
| --- | --- | --- |
| `error.code` | **So sánh** để rẽ nhánh, tra bảng dịch | So sánh bằng `error.message` — chuỗi dev-facing, đổi theo câu chữ và theo ngôn ngữ |
| `error.messageParams` | Ghép tham số vào câu dịch phía FE | Ghép chuỗi ở BE rồi gửi câu hoàn chỉnh — mất khả năng đa ngôn ngữ |
| `error.fieldErrors` | Dịch từng mã rồi gắn vào lỗi của đúng control | Gộp hết vào một toast dù BE đã trả lỗi từng ô; hoặc bind thẳng object vào ô nhập |
| `traceId` | Hiển thị cho người dùng đọc cho người trực hệ thống; đính vào log FE | Bỏ qua — rồi không nối được sự cố người dùng kể với log server |

**`traceId` phải hiện ra trong mọi thông báo lỗi hệ thống.**

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §1.1

### 1.2 Đọc `data` — một cửa duy nhất

```typescript
// core/http/unwrap.ts
export function unwrapData<T>(res: ApiResult<T>): T {
  if (!res.success) {
    throw new ApiFailureError(res);
  }
  return res.data;
}
```

🛑 **Ba dạng bị cấm khi đọc `data`:**

| Dạng | Envelope thiếu `data` thì chuyện gì xảy ra |
| --- | --- |
| `res.data!` | Nổ muộn, ở tận trong mapper, câu lỗi vô nghĩa |
| `res.data as T` | Như trên, và còn tắt luôn kiểm tra kiểu |
| `res.data ?? []` | **Không nổ.** Giao diện hiện "không có dữ liệu" — hợp lệ y như thật |
| `unwrapData(res)` | Nổ ngay, đúng chỗ, mang theo `traceId`, rơi vào nhánh lỗi sẵn có |

```bash
# Luật F18 — không đọc `data` bằng ba dạng bị cấm. PASS khi không in ra dòng nào.
[ -d src/FE/src ] || { echo "F18: không có src/FE/src để quét"; exit 1; }
grep -rnE '\.data\s*(!|as |\?\?)' src/FE/src --include='*.ts' | grep -v '\.spec\.ts'
```

Cổng quét **văn bản**: chú thích **gọi tên** dạng bị cấm, không gõ lại.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §1.2

### 1.3 Casing trên dây

Payload dùng **camelCase** — BE đặt chính sách đặt tên JSON một lần ở composition root. FE không tự chuyển đổi casing ở tầng nào cả.

> 📖 Luật casing của khoá `fieldErrors` (PascalCase): [`be-api-controller.md`](be-api-controller.md) §2.3.

Hệ quả cho FE: tên control trong `FormGroup` là camelCase, khoá BE gửi thì không — **một** bước chuyển, ở đúng một chỗ là hàm gắn lỗi vào form ([`../wiki-core/fe/09-forms-validation.md`](../wiki-core/fe/09-forms-validation.md) §4); không rải ra từng màn.

---

## 2. Chuỗi interceptor FE — định nghĩa gốc

Mảng dưới đây là **Chuỗi interceptor FE — định nghĩa gốc**: ba interceptor, và chỉ ba. Đăng ký một lần ở `app.config.ts`, đúng thứ tự này. Tài liệu FE khác trỏ về mục này, không khai lại.

```typescript
provideHttpClient(
  withInterceptors([authInterceptor, loadingInterceptor, errorInterceptor]),
);
```

Cả ba đặt ở `core/interceptors/`; `core/http/` giữ kiểu envelope, `unwrapData` và mapper lỗi ([`fe-architecture.md`](fe-architecture.md) §2.1).

> 🚨 **Không thêm một interceptor thứ tư ghép tiền tố API** — `authInterceptor` đã ghép base URL.
>
> XSRF **không** có interceptor riêng: `authInterceptor` gắn header (§2.1, ràng buộc 3). **Không** dùng `withXsrfConfiguration` của Angular.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §2

### 2.1 `authInterceptor` — cookie phiên và XSRF

Phiên bằng **cookie**, không JWT ([`../adr/0004-giu-aspnet-identity.md`](../adr/0004-giu-aspnet-identity.md)). Hệ quả cho FE:

```typescript
// core/interceptors/auth.interceptor.ts
const GHI = new Set(['POST', 'PUT', 'PATCH', 'DELETE']);

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const base = inject(API_BASE_URL);
  const xsrf = inject(XsrfTokenStore);

  // URL tuyệt đối đi thẳng: không ghép base URL, không kèm cookie, KHÔNG mang token.
  if (req.url.startsWith('http')) {
    return next(req);
  }

  // Lệnh ghi, CỘNG đúng một ngoại lệ có tên: request đánh dấu tường minh là có tác dụng phụ (§6.4).
  const canToken = GHI.has(req.method) || req.context.get(CO_TAC_DUNG_PHU);
  const token = canToken ? xsrf.token() : null;
  return next(
    req.clone({
      url: `${base}${req.url}`,
      withCredentials: true,
      setHeaders: token ? { 'X-XSRF-TOKEN': token } : {},
    }),
  );
};
```

**Base URL** (`API_BASE_URL`, cấp từ `environment.apiBaseUrl` — nhúng lúc build, [`../wiki-core/fe/17-phuc-vu-va-trien-khai.md`](../wiki-core/fe/17-phuc-vu-va-trien-khai.md) §5.2) là origin của API **cộng tiền tố gốc và phiên bản**: `https://<api>/api/v1`. Service chỉ viết phần sau đó — `/core/users`, `/<module>/…`. Tiền tố đường dẫn: [`be-api-controller.md`](be-api-controller.md) §8.1.

**Tài nguyên tĩnh không đi qua chuỗi interceptor.** Bộ nạp tệp dịch ([`../wiki-core/fe/08-i18n.md`](../wiki-core/fe/08-i18n.md) §2.3) tạo `HttpClient` từ `HttpBackend`, đi vòng qua **toàn bộ** chuỗi. **Không** thay bằng danh sách tiền tố miễn trừ trong interceptor.

```typescript
// core/auth/xsrf-token.store.ts — khối import ở ly-do §2.1
@Injectable({ providedIn: 'root' })
export class XsrfTokenStore {
  private readonly http = inject(HttpClient);
  private readonly _token = signal<string | null>(null);
  readonly token = this._token.asReadonly();

  /** Gọi lúc khởi động app, sau đăng nhập, sau đăng xuất — và một lần khi gặp CSRF_REJECTED (§2.2). */
  lamMoi(tuyChon: { readonly boQuaToastLoi?: boolean } = {}): Observable<string> {
    const context = new HttpContext().set(BO_QUA_TOAST_LOI, tuyChon.boQuaToastLoi ?? false);
    return this.http
      .get<ApiResult<{ token: string }>>('/core/antiforgery/token', { context })
      .pipe(map(unwrapData), map((d) => d.token), tap((t) => this._token.set(t)));
  }
}
```

Ba nơi gọi `lamMoi()`, mỗi nơi chặn một ca 403:

| Nơi gọi | Thiếu thì |
| --- | --- |
| Bước khởi tạo của app — song song với `me` ([`fe-routing-guard.md`](fe-routing-guard.md) §3.6) | Thao tác ghi đầu tiên sau tải trang 403 |
| Service đăng nhập và đăng xuất, **trước** khi điều hướng | Token gắn danh tính lúc phát — thao tác ghi đầu tiên sau đổi danh tính 403 |
| `errorInterceptor` khi gặp `CORE.AUTH.CSRF_REJECTED` (§2.2) | Token hết hạn giữa phiên làm người dùng mất thao tác đang làm |

Bốn ràng buộc:

1. **`withCredentials: true` là bắt buộc.**
2. **Phiên không bao giờ nằm trong JS.** Cookie phiên `HttpOnly` do trình duyệt quản; không sao ra `localStorage` hay biến. Token XSRF thì JS giữ (ràng buộc 3), nhưng **chỉ trong bộ nhớ**, không `localStorage`/`sessionStorage`.
3. **Token XSRF lấy từ thân phản hồi của endpoint phát token, giữ trong `XsrfTokenStore` ở bộ nhớ, và chỉ gắn vào request GHI tới API** — cộng đúng một ngoại lệ có tên: request `GET` **được đánh dấu tường minh** là có tác dụng phụ (hôm nay chỉ có tải tệp xuất, §6.4) đi qua nhánh gắn token như lệnh ghi. Ba nơi phải gọi `lamMoi()`: bảng ngay trên.
4. **Đường dẫn trong service viết NGẮN, không mang tiền tố của base URL.** Cổng F19 so trên nội dung cả tệp — lệnh gốc ở [`../wiki-core/fe/trien-khai/05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) §8.13.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §2.1

### 2.2 `errorInterceptor` — nơi DUY NHẤT dịch lỗi

Thứ tự nhánh trong bộ xử lý lỗi `xuLyLoi(yeuCau)` — dựng theo **từng request**; khối đầy đủ của `error.interceptor.ts` ở [`ly-do/fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §2.2, sửa cùng lượt với code (luật D44, [`../DEBT.md`](../DEBT.md)):

1. `inject()` gọi ở **thân hàm interceptor**, không trong callback của `catchError`.
2. 403 `CORE.AUTH.CSRF_REJECTED` trên **lệnh ghi** (`!LENH_AN_TOAN.has(method)`) hoặc request `GET` mang dấu `CO_TAC_DUNG_PHU` (§2.1 ràng buộc 3, §6.4), chưa mang cờ `DA_THU_LAI_XSRF` ⇒ `xsrf.lamMoi()` (GET token mang `BO_QUA_TOAST_LOI` của request gốc) rồi gửi lại **đúng một lần**: bản clone mang header mới và cờ đặt trên **bản sao** context (`ctxDaThuLaiXsrf`), lần gửi lại bọc lại bằng chính `catchError(xuLyLoi(guiLai))`. `ORIGIN_REJECTED` không vào nhánh này.
3. 401 ⇒ `SessionExpiryHandler.handle()` trừ request mang `BO_QUA_HET_PHIEN` (§2.5); không toast.
4. 403 `CORE.AUTH.FORBIDDEN` ⇒ `auth.lamMoiQuyen()` rồi đi tiếp xuống nhánh 5; `CORE.AUTH.PASSWORD_CHANGE_REQUIRED` ⇒ `void auth.lamMoiPhien()`, không toast, không điều hướng; 403 mang mã **nghiệp vụ** ⇒ trả thẳng cho màn, không toast, không làm mới quyền.
5. Lớp lỗi xuyên suốt (`laLoiXuyenSuot`, dưới) ⇒ toast theo định nghĩa gốc dưới, **không** xét `BO_QUA_TOAST_LOI`.
6. 429 ⇒ toast `dichLoi(translate, body, thamSoRetryAfter(err))`; 409 `CORE.CONCURRENCY.CONFLICT` ⇒ toast, không gửi lại, không tải lại hộ. Cả hai tôn trọng `BO_QUA_TOAST_LOI`.
7. Lỗi kèm `fieldErrors` ⇒ **không toast** — lỗi thuộc về form.
8. Còn lại ⇒ toast `dichLoi(translate, body)` kèm `traceId` trừ khi `BO_QUA_TOAST_LOI`.

Mọi nhánh kết thúc bằng `throwError(() => new ApiFailureError(body, err.status))`; `body` là `docEnvelopeLoi(err)` — thân lỗi là `Blob` kiểu JSON (request `responseType: 'blob'`, §6.4) thì đọc thành text và parse **trước** bước này.

Hiện thực: `src/FE/src/app/core/http/dich-loi.ts` — `export function dichLoi(`, `export function thamSoRetryAfter(` và `export function dichLoiChoMan(` (câu cho khu lỗi riêng của màn: `null` với lớp lỗi xuyên suốt). Store và page import từ đây; interceptor không giữ bản riêng. `thamSoThem` ghi đè tham số cùng tên trong envelope; không có `body` thì trả câu mất kết nối.

**Cơ chế đường lùi ở `dichLoi` là bắt buộc**, và khoá dịch dựng từ `body.error.code`, không từ một trường ở gốc envelope.

**Nhánh 4 — `lamMoiQuyen()` nạp lại tập quyền VÀ menu trong cùng bước** ([`fe-routing-guard.md`](fe-routing-guard.md) §3.3); vì sao không điều hướng: cùng file §8. `PASSWORD_CHANGE_REQUIRED` đi qua `lamMoiPhien()` rồi `mustChangePasswordGuard` trả đích (§5.4 của file đó). 403 mang mã **nghiệp vụ** thì màn đọc `code` từ `ApiFailureError` và hiển thị theo card.

**Lớp lỗi xuyên suốt — định nghĩa gốc** ([ADR-0094](../adr/0094-lop-loi-xuyen-suot-luon-toast-ke-ca-khi-man-tat-toast.md)): mọi 5xx, có envelope hay không; 403 `CORE.AUTH.*` ([`../contracts/auth.md`](../contracts/auth.md) §11) trừ `PASSWORD_CHANGE_REQUIRED`; 403 không envelope thì ngoài lớp. Nhận diện ở **một** hàm, `core/http/loi-xuyen-suot.ts` — `laLoiXuyenSuot(status, body): boolean`, cho cả interceptor lẫn mọi hàm dựng câu cho khu lỗi của màn (chúng trả `null` với lớp này). Lớp này **luôn** toast kèm `traceId`, có cờ hay không; 5xx không envelope toast câu của `CORE.CLIENT.SERVER_UNAVAILABLE`, không `traceId`. `vi.json` có `loi.CORE.SYSTEM.UNEXPECTED`, câu không nhúng `traceId`. Mất kết nối, 429, 409 ngoài lớp.

**Nhánh 6 — toast 429 lấy số giây từ header `Retry-After`, header thắng envelope.** Request mang `BO_QUA_TOAST_LOI` (như màn đăng nhập) hiện 429 ở khu lỗi của màn, số giây từ `messageParams.RetryAfterSeconds` — `ApiFailureError` không mang header; hợp đồng buộc BE gửi cùng số ([`../contracts/auth.md`](../contracts/auth.md) §10). Với 409 `CONCURRENCY.CONFLICT`, màn tải lại bản ghi và cho người dùng **xem bản mới** rồi mới lưu lần nữa; token đồng thời khai một lần ở [`../wiki-core/be/06-concurrency-control.md`](../wiki-core/be/06-concurrency-control.md) §6.3 — FE không tự chọn header hay trường mang nó, model của màn giữ nguyên chuỗi `version` nhận từ `GET`.

**`BO_QUA_TOAST_LOI` là `HttpContextToken`, không phải cờ toàn cục, và chỉ tắt toast cho lỗi của màn.** Cách dùng:

```typescript
// Màn này hiển thị lỗi ngay trong form nên tự lo, không cần toast.
const ctx = new HttpContext().set(BO_QUA_TOAST_LOI, true);
this.http.post<ApiResult<NguoiDungDto>>('/core/users', body, { context: ctx });
```

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §2.2

### 2.3 `loadingInterceptor` — đếm request đang chạy

```typescript
// core/interceptors/loading.interceptor.ts
export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loading = inject(LoadingService);
  loading.batDau();
  return next(req).pipe(finalize(() => loading.ketThuc()));
};
```

```typescript
// core/http/loading.service.ts
@Injectable({ providedIn: 'root' })
export class LoadingService {
  private readonly dangChay = signal(0);
  readonly hienThi = computed(() => this.dangChay() > 0);

  batDau(): void {
    this.dangChay.update((n) => n + 1);
  }

  ketThuc(): void {
    // Không bao giờ để âm.
    this.dangChay.update((n) => Math.max(0, n - 1));
  }
}
```

**Đếm chứ không phải cờ boolean.**

**`finalize` chứ không phải `tap`.**

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §2.3

### 2.4 Cái gì KHÔNG làm interceptor

| Ý tưởng | Vì sao không |
| --- | --- |
| Refresh token | Không có JWT ở hệ này. Phiên là cookie; hết hạn thì 401 và đăng nhập lại |
| Log mọi request lên server | Ồn, tốn băng thông, và trùng với log của BE vốn đã có `traceId` |
| Cache response | Cache thuộc về tầng service của feature, nơi biết dữ liệu nào cache được. Xem [`../wiki-core/fe/13-performance.md`](../wiki-core/fe/13-performance.md) |
| Chuyển đổi casing | Không cần — casing đã thống nhất (§1.3) |

### 2.5 `SessionExpiryHandler` — định nghĩa gốc

Phiên kết thúc có ba đường ([`../wiki-core/fe/07-auth-identity.md`](../wiki-core/fe/07-auth-identity.md) §7.1), và cả ba quy về **một** lớp ở `core/auth`. `errorInterceptor` gọi nó ở nhánh 401 (§2.2); tab khác gọi nó qua sự kiện `storage` của trình duyệt.

Khoá `localStorage` dùng để báo tab khác nằm ở một service riêng, **không** khai trong chính lớp
này — `SessionExpiryHandler` đã `inject(AuthService)`, nên `AuthService.dangXuat()` (đăng xuất
CHỦ ĐỘNG, không đi qua nhánh 401) không thể `inject(SessionExpiryHandler)` ngược lại để tự báo tab
khác (vòng DI, NG0200). Một service trung lập giữ khoá và hàm ghi khoá, dùng chung bởi cả hai đường:

```typescript
// core/auth/tab-session-broadcast.service.ts — khối import ở ly-do §2.5
/** Khoá báo tab khác. Giá trị là thời điểm — ghi cùng giá trị thì trình duyệt không phát sự kiện. */
export const KHOA_BAO_TAB_PHIEN_KET_THUC = 'core.session-ended';

@Injectable({ providedIn: 'root' })
export class TabSessionBroadcastService {
  baoPhienKetThuc(): void {
    try {
      localStorage.setItem(KHOA_BAO_TAB_PHIEN_KET_THUC, String(Date.now()));
    } catch {
      // Chế độ riêng tư, bộ nhớ đầy: tab này đã dọn phiên — chỉ mất việc báo tab khác, không chặn đăng xuất.
    }
  }
}
```

```typescript
// core/auth/session-expiry.handler.ts — khối import ở ly-do §2.5
@Injectable({ providedIn: 'root' })
export class SessionExpiryHandler {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly routes = inject(CORE_ROUTES);
  private readonly broadcast = inject(TabSessionBroadcastService);

  constructor() {
    // Tab khác đã hết phiên (401 hoặc đăng xuất chủ động) → dọn theo, KHÔNG báo lại.
    const nghe = (e: StorageEvent): void => {
      if (e.key === KHOA_BAO_TAB_PHIEN_KET_THUC) {
        this.ketThuc(false);
      }
    };
    window.addEventListener('storage', nghe);
    inject(DestroyRef).onDestroy(() => window.removeEventListener('storage', nghe));
  }

  /** errorInterceptor gọi khi gặp 401 ở request không mang BO_QUA_HET_PHIEN. */
  handle(): void {
    this.ketThuc(true);
  }

  private ketThuc(baoTabKhac: boolean): void {
    // Chốt là CHÍNH trạng thái phiên: đã dọn thì mọi 401 đến sau bỏ qua, tới lần đăng nhập kế.
    if (!this.auth.daDangNhap()) {
      return;
    }
    this.auth.donPhien(); // dọn gì, giữ gì: wiki-core/fe/07-auth-identity.md §7.2
    if (baoTabKhac) {
      this.broadcast.baoPhienKetThuc();
    }
    void this.router.navigate([this.routes.dangNhap], { queryParams: { returnUrl: this.router.url } });
  }
}
```

```typescript
// core/auth/auth.service.ts — phần liên quan, đăng xuất CHỦ ĐỘNG; khối import ở ly-do §2.5
dangXuat(): Observable<void> {
  const ctx = new HttpContext().set(BO_QUA_HET_PHIEN, true); // 401 ở đây là câu trả lời bình thường — bảng dưới
  return this.http.post<ApiResult<null>>('/core/auth/logout', {}, { context: ctx }).pipe(
    map(() => undefined),
    // Phiên đã hết phía server: đăng xuất vẫn thành công, không phải lỗi (contracts/auth.md §4).
    catchError((err: unknown) =>
      err instanceof ApiFailureError && err.body?.error.code === 'CORE.AUTH.NOT_AUTHENTICATED'
        ? of(undefined)
        : throwError(() => err),
    ),
    tap(() => {
      this.donPhien();
      // Đường 401 báo qua SessionExpiryHandler; đường này không đi qua đó (tránh vòng DI ở trên)
      // nên tự gọi CÙNG service quảng bá — hai đường, một khoá localStorage.
      this.broadcast.baoPhienKetThuc();
    }),
    // Token XSRF mới về TRƯỚC khi nơi gọi điều hướng (§2.1). Hỏng thì không chặn đăng xuất:
    // lấy lại ở lần gặp CSRF_REJECTED.
    switchMap(() => this.xsrf.lamMoi().pipe(catchError(() => of(null)))),
    map(() => undefined),
  );
}
```

| Lớp này bảo đảm | Thiếu thì |
| --- | --- |
| Chạy **một lần** tới lần đăng nhập kế | Bốn request song song cùng 401 cho bốn lần điều hướng chồng nhau |
| Dọn trạng thái người dùng qua **một** lời gọi | Mỗi nơi tự dọn thì có nơi quên — và thứ bị quên thường là cache |
| Báo tab khác | Tab còn mở thành giao diện chết: đầy dữ liệu, mọi thao tác 401 |
| Điều hướng về đăng nhập kèm `returnUrl` | Đăng nhập lại xong người dùng phải tự tìm về chỗ cũ |

**Request mang `BO_QUA_HET_PHIEN` không đi qua lớp này.** Đó là hai lời gọi mà 401 là câu trả lời bình thường: `GET /api/v1/core/auth/me` lúc khởi động (chưa đăng nhập) và `POST /api/v1/core/auth/logout` (gọi khi đã hết phiên vẫn 401 — [`../contracts/auth.md`](../contracts/auth.md) §4).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §2.5

---

## 3. Xử lý lỗi tập trung — component KHÔNG có `try/catch` rải rác

Luật: **một lỗi HTTP được xử lý ở đúng một chỗ.** `errorInterceptor` dịch và hiển thị; page chỉ xử lý phần *riêng của màn hình*.

> 📖 Màn danh sách — trạng thái `error` và nút "Thử lại" do `ListStateStore` giữ, page không bắt lỗi: đọc [`fe-architecture.md`](fe-architecture.md) §2.8. Form — lỗi từng ô đi qua `ApiFailureError`: [`fe-ui-conventions.md`](fe-ui-conventions.md) §6.2.

**Ba dạng bị cấm trong `pages/` và `components/`:**

| Dạng | Vì sao cấm |
| --- | --- |
| `catchError` bắn toast | Hai toast cho một sự cố — interceptor đã bắn rồi |
| `catchError(() => of([]))` | Nuốt lỗi, biến hỏng thành rỗng (§1.2) |
| `try/catch` quanh lời gọi API | Lỗi RxJS không đi vào `catch` của khối `try` bao ngoài `subscribe` — khối đó chỉ tạo cảm giác đã xử lý |

**Ngoại lệ có điều kiện — đường lùi trong service hạ tầng.** Service hạ tầng mà hỏng nó không được phép chặn cả ứng dụng (điển hình: menu) **được phép** trả giá trị lùi trong chính service, với đúng một điều kiện: **lỗi phải hiện ra một lần trước khi trả giá trị lùi**.

Phép thử trước khi viện dẫn ngoại lệ này: *service này hỏng thì người dùng có mất luôn khả năng dùng phần còn lại của app không?* Không → đường lùi thuộc về nơi gọi, không phải service.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §3

---

## 4. Ranh giới DTO ↔ model — luật F10

### 4.1 Luật

> **`components/` và `pages/` không được import DTO** — không import tệp `*.dto.ts`, không nhắc kiểu `*Dto`. Mọi kiểu khai trong `*.dto.ts` đều là DTO, kể cả `…Payload`. Người đọc DTO là service và mapper: `services/`, hoặc `core/<mảng>/` theo §4.2.

```
DTO  (models/*.dto.ts)      ← hình dạng của DÂY. Server quyết. Đổi khi API đổi.
  │
  │  mapper (services/*.mapper.ts)
  ▼
model (models/*.model.ts)   ← hình dạng của MÀN HÌNH. FE quyết. Đổi khi UI đổi.
```

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §4.1

### 4.2 Mapper đặt ở đâu

| Trường hợp | Đặt ở |
| --- | --- |
| Mapper của một feature | `<feature>/services/<feature>.mapper.ts` |
| Mapper dùng bởi nhiều feature trong cùng tầng | `<tang>/services/` của tầng đó |
| Mapper cho kiểu của `core/` (phiên, menu) | `core/<mảng>/<tên>.mapper.ts`, cạnh DTO và model của chính mảng đó — `core/http/` chỉ giữ thứ của **đường truyền** (envelope, unwrap, dịch lỗi), không giữ mapper của mảng nào |

Mapper là **hàm thuần**, không phải class, không inject gì.

Ở `core/` cũng vậy: DTO trong `*.dto.ts`, model trong `*.model.ts`, **model không `extends` DTO** — kế thừa đưa hình dạng dây tới màn mà F10 không thấy.

> ✅ **CÓ THẬT (đối chiếu 2026-09-23):** `src/FE/src/app/core/auth/phien.mapper.ts` đúng khuôn — chuỗi `export function sangNguoiDungHienTai`. Mảng `menu` của `core/` cũng vậy: DTO ở `core/menu/menu.dto.ts` (chuỗi `export interface MenuItemDto`), model độc lập ở `core/menu/menu.model.ts` (chuỗi `export interface MenuNode`, không `extends`), hàm dựng cây ở `core/menu/menu.mapper.ts` (chuỗi `export function dungCayMenu`).

```typescript
// services/nguoi-dung.mapper.ts
import type { NguoiDungDto } from '../models/nguoi-dung.dto';
import type { NguoiDung } from '../models/nguoi-dung.model';

export function mapNguoiDung(dto: NguoiDungDto): NguoiDung {
  return {
    id: dto.id,
    userName: dto.userName,
    fullName: dto.fullName,
    email: dto.email,
    isLocked: dto.isLocked,
    lockedUntil: dto.lockoutEnd ? new Date(dto.lockoutEnd) : null,
    roleNames: dto.roles.map((r) => r.name),
    createdAt: new Date(dto.createdAt),
  };
}
```

Tên trường của DTO theo đúng card [`../contracts/users.md`](../contracts/users.md) §3.

Hai việc mapper **phải** làm:

1. **Chuyển chuỗi ngày sang `Date`.**
2. **Rút hình dạng dây về đúng thứ màn hình cần.**

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §4.2

### 4.3 Cổng

```bash
# Luật F10 — components/ và pages/ không import tệp *.dto.ts, không nhắc kiểu *Dto.
# PASS khi không in ra dòng nào. Tập tệp quét rỗng, hoặc hết tệp *.dto.ts, là ĐỎ (T6).
TEP=$(find src/FE/src/app -type f -name '*.ts' ! -name '*.spec.ts' 2>/dev/null | grep -E '/(components|pages)/')
[ -n "$TEP" ] || { echo "F10: không có tệp .ts nào dưới components/ hoặc pages/ để quét"; exit 1; }
[ -n "$(find src/FE/src/app -name '*.dto.ts')" ] || { echo "F10: hết tệp *.dto.ts, dò đường nhập mù"; exit 1; }
printf '%s\n' "$TEP" | xargs grep -nE "from ['\"][^'\"]*\.dto['\"]|Dto\b"
```

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §4.3

---

## 5. CRUD và phân trang dùng chung — bằng HÀM, không base class

`core/` **không cung cấp base class cho service** ([`../wiki-core/fe/01-core-components.md`](../wiki-core/fe/01-core-components.md) §9). Thứ dùng chung là **hàm thuần**; service của feature *gọi*, không *kế thừa*.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §5

### 5.1 Hình dạng phân trang phía FE

Khối dưới đây là **Hình dạng phân trang phía FE — định nghĩa gốc**; nó phản chiếu hợp đồng trên dây và không được khai lại ở tài liệu FE nào khác.

```typescript
// core/http/paged.model.ts

/** Khớp 1:1 phần `data` của mọi endpoint danh sách — hợp đồng ở ../contracts/README.md §8. */
export interface PagedList<T> {
  readonly items: readonly T[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
}

/** Tham số danh sách gửi LÊN DÂY. Tên field khớp đúng chuỗi query param của hợp đồng. */
export interface PageQuery {
  readonly page: number;
  readonly pageSize: number;
  readonly sortBy?: string;
  readonly sortDescending?: boolean;
  readonly searchText?: string;
  /** Bộ lọc riêng của endpoint — mỗi khoá thành một tham số rời, tên theo card. */
  readonly filters?: Readonly<Record<string, string>>;
}
```

> 📖 Tên tham số, khoảng hợp lệ, mặc định, mã lỗi khi ngoài khoảng: [`../contracts/README.md`](../contracts/README.md) §8 (file chủ; mục này chỉ phản chiếu).

Ba điều dễ sai:

1. **`page`, không phải `pageNumber`.** Tên field trong kiểu TypeScript là thứ đi thẳng vào query string.
2. **Không có `totalPages` trên dây.** Số trang là dữ liệu phái sinh; FE tự tính từ `totalCount` và `pageSize`.
3. **Một bộ tên cho cả ba chỗ.** Tên field của kiểu này cũng là tên query param trên thanh địa chỉ và tên trong `GridQuery` ([`fe-architecture.md`](fe-architecture.md) §2.8). Không có kiểu đọc-từ-URL riêng, không có hàm đổi tên.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §5.1

### 5.2 Hàm dùng chung, và cơ chế giữ luật F10

Hiện thực: `src/FE/src/app/core/http/crud.ts` — hàm thuần, không class, không kế thừa: `export function danhSachTrang<` và `export function theoId<`, cùng `toHttpParams(` trải truy vấn thành query string — tên dây giữ nguyên, bộ lọc thành tham số rời, bỏ ô trống. 🚧 Cả hai **sẽ** nhận `context?: HttpContext` cuối, chuyền nguyên cho `HttpClient` — **chưa thi công** hôm nay (bảng đầu file, chốt 2026-09-25); để né việc thiếu tham số này, ba phương thức đọc gọi thẳng `HttpClient` thay vì qua hàm chung (xem cảnh báo cuối mục).

Service của feature:

```typescript
@Injectable({ providedIn: 'root' })
export class NguoiDungService {
  private readonly http = inject(HttpClient);
  private readonly duongDan = '/core/users';   // NGẮN — interceptor ghép base URL
  private readonly ctx = new HttpContext().set(BO_QUA_TOAST_LOI, true);

  danhSach(q: PageQuery): Observable<PagedList<NguoiDung>> {
    // Không context: lỗi tải danh sách đi toast.
    return danhSachTrang<NguoiDungDto, NguoiDung>(this.http, this.duongDan, q, mapNguoiDung);
  }

  chiTiet(id: string): Observable<NguoiDung> {
    // Màn chi tiết tự hiện lỗi — tắt toast.
    return theoId<NguoiDungDto, NguoiDung>(this.http, this.duongDan, id, mapNguoiDung, this.ctx);
  }
}
```

**Mapper là tham số bắt buộc của hàm**, nên không có đường nào lấy `TDto` ra khỏi `core/http/` mà không qua mapper; cổng F10 (§4.3) là lớp canh thứ hai.

**Hàm dùng chung không bắt lỗi.** Lỗi thuộc về interceptor.

> ⚠️ **Hai hàm chỉ phủ phần ĐỌC.** Đọc danh sách hoặc một bản ghi thì qua chúng — cần `HttpContext` không phải lý do gọi thẳng. Lệnh ghi và endpoint ngoài khuôn (thao tác hàng loạt, xuất file, quy trình nhiều bước) gọi thẳng `HttpClient` trong service.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §5.2

---

## 6. Hủy request, retry, timeout

### 6.1 Hủy — mặc định, không phải tính năng thêm

Angular hủy request HTTP khi observable bị unsubscribe. Hai chỗ phải khai thác điều đó:

> 📖 Màn danh sách **không** tự viết vòng huỷ — `ListStateStore` đã giữ ([`fe-architecture.md`](fe-architecture.md) §2.8). Ô tìm không dẫn tới danh sách phân trang (gợi ý của `Autocomplete`) thì theo khuôn `GanVaiTroDialog` ở [`ly-do/fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §6.1, với hai luật: `switchMap` trên `Subject` từ khoá, và **`catchError` nằm TRONG `switchMap`** (đổi trạng thái rồi trả `EMPTY`).

**Gợi ý lọc ở máy chủ, không tải hết rồi lọc ở FE.** Trang gửi chuỗi gõ làm `searchText` của endpoint danh sách, lấy một trang đầu. `Autocomplete` tự chờ ngừng gõ và giữ `minChars` rồi mới phát `search` ([`../Design/Components/Autocomplete.md`](../Design/Components/Autocomplete.md)); trang **không** chờ ngừng gõ lần hai.

**`switchMap` chứ không `mergeMap`.**

Component tự subscribe thì phải hủy khi bị hủy:

```typescript
private readonly huy = inject(DestroyRef);
// ...
this.service.danhSach(query).pipe(takeUntilDestroyed(this.huy)).subscribe(/* ... */);
```

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §6.1

### 6.2 Retry — hẹp, có điều kiện

📐 **Chưa thi công** — `core/http/retry.ts` chưa có trong `src/FE` hôm nay (bảng đầu file, đối chiếu 2026-09-23).

> 📐 Phần chưa thi công (khuôn `thuLaiKhiLoiMang`, điều kiện áp — chỉ `GET`, chỉ lỗi mạng/5xx, áp ở service chứ không ở interceptor): [`fe-api-client-chua-thi-cong.md`](fe-api-client-chua-thi-cong.md) §6.2
>
> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §6.2

### 6.3 Timeout

📐 **Chưa thi công** — `src/FE` chưa có `TIMEOUT_MAC_DINH`/`TIMEOUT_XUAT_FILE` hôm nay; luật ép bằng máy là nợ [F42](../DEBT.md).

> 📐 Phần chưa thi công (hai hằng số, và khuôn cho thao tác dài hơn ngưỡng — BE trả mã việc, FE hỏi trạng thái theo chu kỳ, chốt ở pha B4): [`fe-api-client-chua-thi-cong.md`](fe-api-client-chua-thi-cong.md) §6.3
>
> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §6.3

### 6.4 Tải tệp xuất về máy — blob qua `HttpClient`, tên tệp từ `Content-Disposition`

Chốt 2026-09-22 ([ADR-0062](../adr/0062-endpoint-xuat-kiem-token-chong-gia-mao-nhu-lenh-ghi.md)); phía BE **đã có** (`[RequireAntiforgery]` trên action `Export`, kiểm qua HTTP 2026-09-23). Phía FE: **đường ống đã có, chưa màn nào gọi** — luật 2–5 khớp code (đối chiếu 2026-09-23, bảng đầu file), luật 1 chưa có chỗ gọi để đối chiếu, luật 6 còn 📐 (`TIMEOUT_XUAT_FILE` chưa có trong `src/FE`). Endpoint xuất ([`../contracts/exports.md`](../contracts/exports.md) §1) đòi `X-XSRF-TOKEN` dù là `GET`, nên **không mở được bằng URL** — sáu luật:

1. **Tải qua `HttpClient`** với `responseType: 'blob'` và `observe: 'response'`; **không** `window.open`, không `<a href>` trỏ thẳng API, không `<form>` — điều hướng không mang được header, và BE từ chối là đúng.
2. **Request mang dấu tác dụng phụ** — `HttpContextToken` tên `CO_TAC_DUNG_PHU`, khai ở `core/http/api-result.model.ts` cùng chỗ `BO_QUA_TOAST_LOI`/`BO_QUA_HET_PHIEN`. **Phải ở `core/`**: hai interceptor đều đọc nó, mà `core/` không import ngược lên được (F1). Service gọi export đặt cờ; `authInterceptor` gắn token (§2.1 ràng buộc 3), `errorInterceptor` gửi lại đúng một lần khi `CSRF_REJECTED` (§2.2). Không tự đặt header trong service, và **không** gắn token cho mọi `GET` — làm vậy thì `errorInterceptor` hết phân biệt được `GET` nào được gửi lại, kể cả `xsrf.lamMoi()`.
3. **Tên tệp đọc từ `Content-Disposition`**: `filename*` (giải mã percent-encoding) trước, `filename` sau. **Header đọc ra `null` là lỗi, không phải lý do đặt tên dự phòng** — nó nghĩa là BE chưa khai header vào `Access-Control-Expose-Headers` ([`be-api-controller.md`](be-api-controller.md) §7.3).
4. **Kích hoạt tải bằng một hàm dùng chung ở `core/http/`** — object URL từ blob, thẻ `<a download>` tạm, click, rồi `URL.revokeObjectURL`. Feature không viết lại đoạn này.
5. **Nhánh lỗi:** với `responseType: 'blob'`, `err.error` của lỗi 4xx/5xx là một `Blob` — `docEnvelopeLoi` trả `null` và người dùng thấy câu *mất kết nối* cho một lỗi 422 có mã. `errorInterceptor` **bóc envelope từ thân `Blob` kiểu JSON trước khi dịch** (§2.2, nơi duy nhất dịch lỗi).
6. Timeout dùng `TIMEOUT_XUAT_FILE` (§6.3); trần số dòng của BE nay cũng là trần bộ nhớ trình duyệt, và không có thanh tiến trình của trình duyệt — nút gọi export tự báo đang chờ.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §6.4

---

## 7. Khi endpoint chưa tồn tại

Không tự đoán hình dạng response. Viết **API Contract Card** vào [`../contracts/`](../contracts/), mỗi endpoint một card, BE review và chốt trước khi thi công.

Trong lúc chờ, FE dựng service với mapper và kiểu đầy đủ, dữ liệu lấy từ stub trong `*.spec.ts` — **không** stub bằng cách trả dữ liệu cứng ngay trong service.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §7

---

## 8. Secret — không có, không bao giờ

Bundle FE là **văn bản công khai** — mọi thứ trong `environments/*.ts` đọc được qua tab Network.

| Được để trong FE | **Cấm** |
| --- | --- |
| `apiBaseUrl` | Chuỗi kết nối, mật khẩu, khoá ký |
| Cờ `production` | Khoá API của dịch vụ bên thứ ba |
| Tên sản phẩm, danh sách ngôn ngữ | Bất cứ thứ gì mà lộ ra là phải đổi |

Chi tiết quy ước file cấu hình theo môi trường và cách chúng vào/không vào git: [`repo-artifact.md`](repo-artifact.md).

---

## 9. Đối chiếu luật

| Luật | Nội dung | Mục |
| --- | --- | --- |
| F10 | `components/`/`pages/` không import DTO | §4 |
| F12 | Service có `.spec.ts` cạnh nó — phạm vi: [`../RULES.md`](../RULES.md) F12 | §5 |
| F18 | Không đọc `data` bằng dấu `!`, ép kiểu, hay giá trị lùi — chỉ qua `unwrapData` | §1.2 |
| F19 | Đường dẫn trong service không mang tiền tố base URL | §2.1 |
| S20 | Export mang `X-XSRF-TOKEN` như lệnh ghi — FE gắn token cho request mang dấu, tải blob | §2.1, §6.4 |
| S6 | Không secret trong bundle FE | §8 |

Bảng đầy đủ: [`../RULES.md`](../RULES.md) §7. Vì sao có envelope và bốn cái bẫy khi tiêu thụ nó: [`../wiki-core/fe/02-http-envelope.md`](../wiki-core/fe/02-http-envelope.md).
