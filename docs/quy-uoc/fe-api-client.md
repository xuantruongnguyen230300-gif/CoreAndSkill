---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Gọi API từ Frontend — envelope, interceptor, ranh giới DTO

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`. File này là bản thiết kế cho tầng gọi API của `src/FE`.
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

/** Cờ tắt toast mặc định cho ĐÚNG một request — không phải cờ toàn cục. */
export const BO_QUA_TOAST_LOI = new HttpContextToken<boolean>(() => false);

/** Cờ cho request mà 401 là câu trả lời BÌNH THƯỜNG, không phải hết phiên — §2.5. */
export const BO_QUA_HET_PHIEN = new HttpContextToken<boolean>(() => false);

/** Lỗi ném ra khi envelope báo thất bại; mang envelope ĐÃ BÓC. `body` là `null` khi phản hồi không phải envelope. */
export class ApiFailureError extends Error {
  constructor(readonly body: ApiFailure | null) {
    // Ma lay tu catalog BE (be-cqrs-handler.md §7.4) — FE KHONG tu che ma.
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

  const token = GHI.has(req.method) ? xsrf.token() : null;
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

  /** Gọi lúc khởi động app, sau đăng nhập, sau đăng xuất — và một lần khi gặp CSRF_REJECTED. */
  lamMoi(): Observable<string> {
    return this.http
      .get<ApiResult<{ token: string }>>('/core/antiforgery/token')
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
3. **Token XSRF lấy từ thân phản hồi của endpoint phát token, giữ trong `XsrfTokenStore` ở bộ nhớ, và chỉ gắn vào request GHI tới API.** Ba nơi phải gọi `lamMoi()`: bảng ngay trên.
4. **Đường dẫn trong service viết NGẮN, không mang tiền tố của base URL.**

```bash
# Luật F19 — đường dẫn trong service không mang tiền tố base URL.
# PASS khi không dòng nào chứa tiền tố base trong lời gọi HTTP.
[ -d src/FE/src ] || { echo "F19: không có src/FE/src để quét"; exit 1; }
grep -rnE "this\.http\.(get|post|put|patch|delete)<" src/FE/src --include='*.ts' \
  | grep -v '\.spec\.ts' | grep "'/api/"
```

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §2.1

### 2.2 `errorInterceptor` — nơi DUY NHẤT dịch lỗi

```typescript
// core/interceptors/error.interceptor.ts — khối import ở ly-do §2.2
/** Đánh dấu request đã được gửi lại một lần sau CSRF_REJECTED — chặn vòng lặp. */
const DA_THU_LAI_XSRF = new HttpContextToken<boolean>(() => false);

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  // inject() gọi Ở ĐÂY — callback của catchError KHÔNG phải injection context.
  const toast = inject(ToastService);
  const translate = inject(TranslateService);
  const auth = inject(AuthService);
  const hetPhien = inject(SessionExpiryHandler);
  const xsrf = inject(XsrfTokenStore);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const body = docEnvelopeLoi(err);

      // 403 CSRF — lấy token mới rồi gửi lại ĐÚNG MỘT lần; cờ trên context chặn vòng lặp.
      // CORE.AUTH.ORIGIN_REJECTED KHÔNG vào nhánh này.
      if (body?.error?.code === 'CORE.AUTH.CSRF_REJECTED' && !req.context.get(DA_THU_LAI_XSRF)) {
        return xsrf.lamMoi().pipe(
          switchMap((token) =>
            next(req.clone({
              setHeaders: { 'X-XSRF-TOKEN': token },
              context: req.context.set(DA_THU_LAI_XSRF, true),
            })),
          ),
        );
      }

      // 401 — phiên chết. Không toast. Request mang BO_QUA_HET_PHIEN tự xử lý (§2.5).
      if (err.status === 401) {
        if (!req.context.get(BO_QUA_HET_PHIEN)) {
          hetPhien.handle();
        }
        return throwError(() => new ApiFailureError(body));
      }

      // 403 CORE.AUTH.FORBIDDEN — làm mới quyền và menu, rồi xuống nhánh toast. KHÔNG điều hướng.
      if (err.status === 403 && body?.error.code === 'CORE.AUTH.FORBIDDEN') {
        auth.lamMoiQuyen();
      } else if (err.status === 403 && body?.error.code === 'CORE.AUTH.PASSWORD_CHANGE_REQUIRED') {
        // Cờ buộc đổi mật khẩu bật giữa phiên: làm mới phiên, guard lo phần còn lại (fe-routing-guard.md §5.4). KHÔNG điều hướng, KHÔNG toast.
        void auth.lamMoiPhien();   // Promise — interceptor không chờ; màn cần chờ thì await (fe-routing-guard.md §3.3)
        return throwError(() => new ApiFailureError(body));
      } else if (err.status === 403 && body !== null && !body.error.code.startsWith('CORE.AUTH.')) {
        // 403 mang mã nghiệp vụ — màn tự xử lý theo card. Không toast chung, không làm mới quyền.
        return throwError(() => new ApiFailureError(body));
      }

      // 429 — siết tần suất ở MỌI màn (contracts/auth.md §10–§11). Số giây chờ đọc từ Retry-After.
      if (err.status === 429) {
        if (!req.context.get(BO_QUA_TOAST_LOI)) {
          toast.loi(dichLoi(translate, body, thamSoRetryAfter(err)), body?.traceId ?? null);
        }
        return throwError(() => new ApiFailureError(body));
      }

      // 409 CORE.CONCURRENCY.CONFLICT — người khác đã ghi sau khi màn đọc (contracts/auth.md §11).
      // Toast rồi trả lỗi về màn. KHÔNG gửi lại, KHÔNG tải lại hộ.
      if (err.status === 409 && body?.error.code === 'CORE.CONCURRENCY.CONFLICT') {
        if (!req.context.get(BO_QUA_TOAST_LOI)) {
          toast.loi(dichLoi(translate, body), body?.traceId ?? null);
        }
        return throwError(() => new ApiFailureError(body));
      }

      // 400/409/422 kèm fieldErrors — lỗi thuộc về form, KHÔNG toast. Trường thật ở body.error.fieldErrors.
      if (body?.error?.fieldErrors) {
        return throwError(() => new ApiFailureError(body));
      }

      // Màn tự hiển thị lỗi của mình thì tắt toast cho ĐÚNG request đó.
      if (!req.context.get(BO_QUA_TOAST_LOI)) {
        toast.loi(dichLoi(translate, body), body?.traceId ?? null);
      }
      return throwError(() => new ApiFailureError(body));
    }),
  );
};

/** Tra bảng dịch theo mã; đường lùi là câu BE gửi kèm. `thamSoThem` ghi đè tham số cùng tên trong envelope. */
function dichLoi(translate: TranslateService, body: ApiFailure | null, thamSoThem: MessageParams = {}): string {
  if (!body) {
    return translate.instant('loi.CORE.CLIENT.NO_CONNECTION');
  }
  const khoa = `loi.${body.error.code}`;
  const cau = translate.instant(khoa, { ...(body.error.messageParams ?? {}), ...thamSoThem });
  return cau === khoa ? body.error.message : cau;
}

/** Retry-After (giây) → tham số dịch cùng tên BE gửi kèm 429 (contracts/auth.md §10). Vắng hoặc sai dạng → rỗng. */
function thamSoRetryAfter(err: HttpErrorResponse): MessageParams {
  const giay = Number.parseInt(err.headers.get('Retry-After') ?? '', 10);
  return Number.isInteger(giay) && giay > 0 ? { RetryAfterSeconds: String(giay) } : {};
}
```

**Cơ chế đường lùi ở `dichLoi` là bắt buộc.**

**Khoá dịch dựng từ `body.error.code`, không phải từ một trường ở gốc envelope.**

**403 `CORE.AUTH.FORBIDDEN` làm mới tập quyền và menu, không điều hướng.** `AuthService.lamMoiQuyen()` ([`fe-routing-guard.md`](fe-routing-guard.md) §3.3) gọi lại `me`, thay tập quyền, rồi **nạp lại menu trong cùng bước**. Vì sao không điều hướng: [`fe-routing-guard.md`](fe-routing-guard.md) §8.

**403 mang mã nghiệp vụ đi thẳng tới màn, không toast chung.** Màn gọi endpoint đó đọc `code` từ `ApiFailureError` và hiển thị theo card. Toast chung giữ cho nhóm `CORE.AUTH.*` ([`../contracts/auth.md`](../contracts/auth.md) §11): `FORBIDDEN`; `CSRF_REJECTED` khi lần gửi lại vẫn bị từ chối; `ORIGIN_REJECTED` ngay lần đầu — **không** gửi lại. Mã duy nhất của nhóm không toast là `PASSWORD_CHANGE_REQUIRED` (đoạn dưới).

**403 `CORE.AUTH.PASSWORD_CHANGE_REQUIRED` làm mới phiên, không toast, không điều hướng** ([`../contracts/auth.md`](../contracts/auth.md) §1.2). Interceptor gọi `AuthService.lamMoiPhien()`; khi `me` về mang `mustChangePassword: true`, `mustChangePasswordGuard` trả đích — cơ chế ở [`fe-routing-guard.md`](fe-routing-guard.md) §5.4.

**429 hiện toast kèm số giây chờ, đọc từ `Retry-After`** — xử lý ở interceptor, không ở màn đăng nhập ([`../contracts/auth.md`](../contracts/auth.md) §10). Header thắng tham số trong envelope.

**409 `CORE.CONCURRENCY.CONFLICT` hiện toast, không gửi lại, không tải lại hộ.** Màn nhận `ApiFailureError`, tải lại bản ghi, cho người dùng xem bản mới rồi mới lưu lần nữa. Token đồng thời khai một lần ở [`../wiki-core/be/06-concurrency-control.md`](../wiki-core/be/06-concurrency-control.md) §6.3, card của endpoint chỉ khai field; FE **không** tự chọn header hay trường để mang nó, và model của màn giữ nguyên chuỗi `version` nhận từ `GET` để gửi lại.

**`BO_QUA_TOAST_LOI` là `HttpContextToken`, không phải cờ toàn cục.** Cách dùng:

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

```typescript
// core/auth/session-expiry.handler.ts — khối import ở ly-do §2.5
/** Khoá báo tab khác. Giá trị là thời điểm — ghi cùng giá trị thì trình duyệt không phát sự kiện. */
const KHOA_BAO_TAB = 'core.session-ended';

@Injectable({ providedIn: 'root' })
export class SessionExpiryHandler {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly routes = inject(CORE_ROUTES);

  constructor() {
    // Tab khác đã hết phiên → dọn theo, KHÔNG báo lại.
    const nghe = (e: StorageEvent): void => {
      if (e.key === KHOA_BAO_TAB) {
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
      localStorage.setItem(KHOA_BAO_TAB, String(Date.now()));
    }
    void this.router.navigate([this.routes.dangNhap], { queryParams: { returnUrl: this.router.url } });
  }
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

> **`components/` và `pages/` không được import DTO. Chỉ `services/` được.**

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
| Mapper cho kiểu Core (người dùng, quyền, menu) | `core/http/` |

Mapper là **hàm thuần**, không phải class, không inject gì.

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
# Luật F10 — DTO không lọt vào components/ hoặc pages/.
# PASS khi không in ra dòng nào.
[ -d src/FE/src/app ] || { echo "F10: không có src/FE/src/app để quét"; exit 1; }
grep -rnE 'Dto\b' src/FE/src/app --include='*.ts' | grep -E '/(components|pages)/' | grep -v '\.spec\.ts'
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

```typescript
// core/http/crud.ts — hàm thuần, không class, không kế thừa; khối import ở ly-do §5.2
export function danhSachTrang<TDto, TModel>(
  http: HttpClient,
  duongDan: string,
  query: PageQuery,
  map1: (dto: TDto) => TModel,
): Observable<PagedList<TModel>> {
  return http
    .get<ApiResult<PagedList<TDto>>>(duongDan, { params: toHttpParams(query) })
    .pipe(map(unwrapData), map((trang) => ({ ...trang, items: trang.items.map(map1) })));
}

export function theoId<TDto, TModel>(
  http: HttpClient,
  duongDan: string,
  id: string,
  map1: (dto: TDto) => TModel,
): Observable<TModel> {
  return http.get<ApiResult<TDto>>(`${duongDan}/${id}`).pipe(map(unwrapData), map(map1));
}

/** Trải truy vấn thành query string: tên dây giữ nguyên, bộ lọc thành tham số rời, bỏ ô trống. */
function toHttpParams(query: PageQuery): HttpParams {
  const { filters, ...chung } = query;
  let params = new HttpParams();
  for (const [ten, giaTri] of Object.entries({ ...filters, ...chung })) {
    if (giaTri !== undefined && giaTri !== '') {
      params = params.set(ten, String(giaTri));
    }
  }
  return params;
}
```

Service của feature:

```typescript
@Injectable({ providedIn: 'root' })
export class NguoiDungService {
  private readonly http = inject(HttpClient);
  private readonly duongDan = '/core/users';   // NGẮN — interceptor ghép base URL

  danhSach(q: PageQuery): Observable<PagedList<NguoiDung>> {
    return danhSachTrang<NguoiDungDto, NguoiDung>(this.http, this.duongDan, q, mapNguoiDung);
  }

  chiTiet(id: string): Observable<NguoiDung> {
    return theoId<NguoiDungDto, NguoiDung>(this.http, this.duongDan, id, mapNguoiDung);
  }
}
```

**Mapper là tham số bắt buộc của hàm**, nên không có đường nào lấy `TDto` ra khỏi `core/http/` mà không qua mapper; cổng F10 (§4.3) là lớp canh thứ hai.

**Hàm dùng chung không bắt lỗi.** Lỗi thuộc về interceptor.

> ⚠️ **Endpoint không theo khuôn CRUD** (thao tác hàng loạt, xuất file, quy trình nhiều bước) thì gọi thẳng `HttpClient` trong service.

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

Retry chỉ áp cho **request đọc** (`GET`) và **chỉ khi** lỗi là lỗi mạng hoặc 5xx. Không bao giờ retry `POST`/`PUT`/`DELETE`.

```typescript
// core/http/retry.ts
export function thuLaiKhiLoiMang<T>(soLan = 2) {
  return retry<T>({
    count: soLan,
    delay: (err: HttpErrorResponse, lan) =>
      err.status === 0 || err.status >= 500
        ? timer(300 * 2 ** lan)  // lùi theo cấp số nhân
        : throwError(() => err), // lỗi 4xx: hỏng do request, thử lại vô nghĩa
  });
}
```

Áp ở **service của feature**, không ở interceptor.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §6.2

### 6.3 Timeout

```typescript
export const TIMEOUT_MAC_DINH = 30_000;
export const TIMEOUT_XUAT_FILE = 120_000;
```

Đặt ở service, khai tường minh cho từng nhóm endpoint. Endpoint xuất báo cáo được phép lâu hơn — nhưng phải **khai bằng một hằng số có tên**, không phải bằng cách bỏ timeout đi.

📐 Thao tác dài hơn ngưỡng trên (kết xuất lớn, nhập hàng loạt): BE trả mã việc, FE hỏi trạng thái theo chu kỳ — không nâng timeout. Khuôn chốt ở pha B4 ([`../wiki-core/be/trien-khai/05-b4-tep-nhap-xuat-thong-bao.md`](../wiki-core/be/trien-khai/05-b4-tep-nhap-xuat-thong-bao.md)).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §6.3

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
| F12 | Mọi `services/*.service.ts` có `.spec.ts` cạnh nó | §5 |
| F18 | Không đọc `data` bằng dấu `!`, ép kiểu, hay giá trị lùi — chỉ qua `unwrapData` | §1.2 |
| F19 | Đường dẫn trong service không mang tiền tố base URL | §2.1 |
| S6 | Không secret trong bundle FE | §8 |

Bảng đầy đủ: [`../RULES.md`](../RULES.md) §7. Vì sao có envelope và bốn cái bẫy khi tiêu thụ nó: [`../wiki-core/fe/02-http-envelope.md`](../wiki-core/fe/02-http-envelope.md).
