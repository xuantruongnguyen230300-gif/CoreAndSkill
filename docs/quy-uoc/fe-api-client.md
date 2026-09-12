---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Gọi API từ Frontend — envelope, interceptor, ranh giới DTO

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`. File này là bản thiết kế cho tầng gọi API của `src/FE`.
>
> **Hình dạng envelope có đúng một file chủ: [`be-api-controller.md`](be-api-controller.md).** Mục §1 dưới đây khai kiểu TypeScript **phản chiếu** hình dạng đó cho phía FE. Khi hai bên lệch nhau, file BE thắng và file này phải sửa theo — không phải ngược lại.

---

## 1. Envelope — hình dạng mọi response

BE trả `Result<T>` và ánh xạ sang HTTP theo một khuôn duy nhất cho **mọi** endpoint. Hình dạng trên dây — ba khối JSON mẫu, kiểu C#, chỗ dựng duy nhất — thuộc [`be-api-controller.md`](be-api-controller.md) §2.1; **đừng chép chúng sang phía FE**.

**Kiểu envelope phía FE — định nghĩa gốc.** Khai đúng một chỗ, ở `core/http/`, và phản chiếu 1:1 hình dạng ở file chủ BE. File FE khác nhắc tới envelope thì trỏ về mục này.

```typescript
// core/http/api-result.model.ts

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

  /**
   * Loại lỗi do BE khai. **DEV-FACING — KHÔNG rẽ nhánh theo trường này.**
   * FE rẽ nhánh theo **mã HTTP**, và theo `code` khi cần phân biệt ca cụ thể.
   * Giữ trong kiểu để đọc được ở log và ở công cụ thử API.
   */
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

/**
 * Đọc envelope ra khỏi một lỗi HTTP. Trả `null` là trường hợp BÌNH THƯỜNG:
 * mất mạng, proxy trả HTML, BE chết trước khi kịp dựng envelope.
 * Nhận diện bằng đúng tên field CÓ THẬT trên dây — hỏi một tên khác thì
 * hàm này luôn trả null và mọi lỗi hiện câu dự phòng.
 */
export function docEnvelopeLoi(err: HttpErrorResponse): ApiFailure | null {
  const body: unknown = err.error;
  return body && typeof body === 'object' && 'success' in body && 'traceId' in body
    ? (body as ApiFailure)
    : null;
}

/** Cờ tắt toast mặc định cho ĐÚNG một request — không phải cờ toàn cục. */
export const BO_QUA_TOAST_LOI = new HttpContextToken<boolean>(() => false);

/**
 * Lỗi ném ra khi envelope báo thất bại. Mang theo envelope ĐÃ BÓC, nên nơi bắt
 * đọc được `code`, `fieldErrors`, `traceId` mà không phải tự bóc lại `err.error`.
 * `body` là `null` khi phản hồi không phải envelope (mất mạng, proxy trả HTML).
 */
export class ApiFailureError extends Error {
  constructor(readonly body: ApiFailure | null) {
    // Ma lay tu catalog BE (be-cqrs-handler.md §7.4) — FE KHONG tu che ma.
    super(body?.error.code ?? 'CORE.CLIENT.NO_CONNECTION');
  }
}
```

🛑 **`code`, `messageParams` và `fieldErrors` nằm TRONG `error`, không nằm phẳng ở gốc envelope.** Đọc chúng ở gốc thì hỏng hai chỗ: điều kiện kiểm `fieldErrors` **luôn sai**, nên mọi lỗi validation rơi xuống toast, đúng thứ mục §2.2 cấm; và khoá dịch dựng từ một trường không tồn tại — cho ra chuỗi `undefined` rồi một toast trống. Cả hai đều không lỗi biên dịch và không test nào bắt, vì cả hai bên vẫn là JSON hợp lệ.

**Giá trị của `fieldErrors` là mảng OBJECT, không phải mảng chuỗi.** Bind thẳng nó vào ô nhập cho ra `[object Object]`; lấy `String(...)` thì mất `code` và lỗi từng ô **không dịch được**. Cách dùng đúng: dịch từng `code` — xem [`../wiki-core/fe/09-forms-validation.md`](../wiki-core/fe/09-forms-validation.md) §4.

**Vì sao là union phân biệt theo `success`, không phải một interface có mọi field đều optional.** Với union, TypeScript ép người viết kiểm `success` trước khi chạm `data`; sau một câu `if (res.success)` thì `res.data` có kiểu `T` chứ không phải `T | null`. Với một interface phẳng, `data` luôn `T | null` ở mọi chỗ, và cách nhanh nhất để đi tiếp là thêm dấu `!` — tức tắt đúng thứ vừa dựng lên.

### 1.1 Bốn field ít người dùng đúng

| Field | Dùng để làm gì | Sai lầm thường gặp |
| --- | --- | --- |
| `error.code` | **So sánh** để rẽ nhánh, tra bảng dịch | So sánh bằng `error.message` — chuỗi dev-facing, đổi theo câu chữ và theo ngôn ngữ |
| `error.messageParams` | Ghép tham số vào câu dịch phía FE | Ghép chuỗi ở BE rồi gửi câu hoàn chỉnh — mất khả năng đa ngôn ngữ |
| `error.fieldErrors` | Dịch từng mã rồi gắn vào lỗi của đúng control | Gộp hết vào một toast dù BE đã trả lỗi từng ô; hoặc bind thẳng object vào ô nhập |
| `traceId` | Hiển thị cho người dùng đọc cho người trực hệ thống; đính vào log FE | Bỏ qua — rồi không nối được sự cố người dùng kể với log server |

**`traceId` phải hiện ra trong mọi thông báo lỗi hệ thống.** Không phải để người dùng hiểu, mà để câu "màn hình báo lỗi" biến thành một chuỗi tra được. Chi phí là một dòng chữ nhỏ; giá trị là khoảng cách giữa "không tái hiện được" và "mở log ra thấy ngay".

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

Dạng thứ ba nguy hiểm nhất và là dạng dễ lọt qua review nhất. Vấn đề không phải giá trị mặc định xấu — mà là **giá trị mặc định trùng khít với một câu trả lời hợp lệ của server**. Lưới rỗng, menu rỗng, số 0: server có quyền trả về thật những thứ đó. Khi envelope hỏng cũng cho ra đúng hình ảnh ấy, không ai — người dùng, người trực hệ thống, hay chính người viết code — phân biệt được "không có gì" với "hỏng". Lỗi loại này không vào log, không vào toast, không vào test; nó chỉ vào **quyết định sai của người đang nhìn màn hình**.

```bash
# PASS khi không in ra dòng nào.
grep -rnE '\.data\s*(!|as |\?\?)' src/FE/src --include='*.ts' | grep -v '\.spec\.ts'
```

> Khi viết chú thích cho luật này, **gọi tên** dạng bị cấm ("toán tử hợp nhất null kèm giá trị mặc định") thay vì gõ lại nguyên văn nó. Lệnh grep không phân biệt code với comment, và một chú thích tử tế sẽ làm cổng đỏ mà không có gì hỏng cả.

### 1.3 Casing trên dây

Payload dùng **camelCase** — BE đặt chính sách đặt tên JSON một lần ở composition root. FE không tự chuyển đổi casing ở tầng nào cả.

> 📖 **Luật casing của khoá `fieldErrors` (PascalCase, và vì sao "sửa cho nhất quán" sẽ phá nó): đọc [`be-api-controller.md`](be-api-controller.md) §2.3.**

Hệ quả cho FE, và chỉ hệ quả: tên control trong `FormGroup` là camelCase, còn khoá BE gửi thì không — nên phải có **một** bước chuyển, đặt ở đúng một chỗ là hàm gắn lỗi vào form ([`../wiki-core/fe/09-forms-validation.md`](../wiki-core/fe/09-forms-validation.md) §4). Không rải bước chuyển đó ra từng màn.

---

## 2. Chuỗi interceptor FE — định nghĩa gốc

Mảng dưới đây là **Chuỗi interceptor FE — định nghĩa gốc**: ba interceptor, và chỉ ba. Đăng ký một lần ở `app.config.ts`, đúng thứ tự này. Tài liệu FE khác nhắc tới chuỗi interceptor thì trỏ về mục này, không khai lại danh sách của riêng mình.

```typescript
provideHttpClient(
  withInterceptors([authInterceptor, loadingInterceptor, errorInterceptor]),
);
```

Cả ba đặt ở `core/interceptors/`; `core/http/` giữ kiểu envelope, `unwrapData` và mapper lỗi ([`fe-architecture.md`](fe-architecture.md) §2.1).

Thứ tự không tuỳ tiện. Interceptor chạy theo thứ tự khai lúc đi ra và **ngược lại** lúc response quay về, nên `errorInterceptor` đặt cuối danh sách sẽ là đứa **đầu tiên** thấy lỗi — đúng chỗ để dịch lỗi trước khi ai khác chạm vào. `loadingInterceptor` bọc ngoài nó để bộ đếm luôn giảm kể cả khi `errorInterceptor` biến response thành lỗi.

> 🚨 **Không thêm một interceptor thứ tư ghép tiền tố API.** `authInterceptor` đã ghép base URL, nên thêm một interceptor ghép tiền tố nữa cho ra URL lặp đoạn giữa → 404 trông **hệt như BE chưa làm endpoint**.
>
> XSRF **không** có interceptor riêng: `authInterceptor` gắn header (§2.1, ràng buộc 3). **Không** dùng `withXsrfConfiguration` của Angular — nó chỉ gắn header cho URL tương đối và cùng origin, mà API ở origin khác.

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

**Tài nguyên tĩnh không đi qua chuỗi interceptor.** Bộ nạp tệp dịch ([`../wiki-core/fe/08-i18n.md`](../wiki-core/fe/08-i18n.md) §2.3) tạo `HttpClient` từ `HttpBackend` — thứ đi vòng qua **toàn bộ** chuỗi interceptor, nên tệp dịch không bị ghép base URL, không kèm cookie, không mang token. **Không** thay bằng danh sách tiền tố miễn trừ trong interceptor: danh sách đó phải nhớ cập nhật mỗi lần thêm một thư mục tĩnh, và quên là hỏng im lặng — request tĩnh bị ghép base URL, gửi nhầm sang máy API và 404.

```typescript
// core/auth/xsrf-token.store.ts
@Injectable({ providedIn: 'root' })
export class XsrfTokenStore {
  private readonly http = inject(HttpClient);
  private readonly _token = signal<string | null>(null);
  readonly token = this._token.asReadonly();

  /** Gọi lúc khởi động app, sau đăng nhập, sau đăng xuất — và một lần khi gặp CSRF_REJECTED. */
  lamMoi(): Observable<string> {
    return this.http
      .get<ApiResult<{ token: string }>>('/core/antiforgery/token')
      .pipe(map((r) => r.data.token), tap((t) => this._token.set(t)));
  }
}
```

Ba nơi gọi `lamMoi()`, mỗi nơi chặn một ca 403:

| Nơi gọi | Thiếu thì |
| --- | --- |
| Bước khởi tạo của app (fe-architecture §2.6) | Thao tác ghi đầu tiên sau tải trang 403 |
| Service đăng nhập và đăng xuất, **trước** khi điều hướng | Token gắn danh tính lúc phát — thao tác ghi đầu tiên sau đổi danh tính 403 |
| `errorInterceptor` khi gặp `CORE.AUTH.CSRF_REJECTED` (§2.2) | Token hết hạn giữa phiên làm người dùng mất thao tác đang làm |

Bốn ràng buộc, mỗi cái chặn một lỗi im lặng:

1. **`withCredentials: true` là bắt buộc.** Thiếu nó, cookie phiên không được gửi và mọi request đều bị coi là chưa đăng nhập — triệu chứng là "đăng nhập xong vẫn 401", rất dễ bị chẩn đoán nhầm sang phía BE.
2. **Phiên không bao giờ nằm trong JS.** Cookie phiên `HttpOnly` do trình duyệt quản; không sao nó ra `localStorage` hay biến. *Token XSRF thì khác* — nó được thiết kế để JS giữ (ràng buộc 3), nhưng **chỉ trong bộ nhớ**, không `localStorage`/`sessionStorage`: không có lý do lưu bền, vì mỗi lần khởi động app đều lấy token mới.
3. **Token XSRF lấy từ thân phản hồi của endpoint phát token, giữ trong `XsrfTokenStore` ở bộ nhớ, và chỉ gắn vào request GHI tới API.** Không dùng `withXsrfConfiguration` của Angular: nó chỉ gắn header cho URL tương đối và cùng origin, nên với API ở origin khác nó **im lặng không gắn** — mọi request ghi 403. Gắn cho mọi request mà không lọc là rò token sang origin khác — nên chỉ gắn ở nhánh đã xác định là request tới API. Ba nơi phải gọi `lamMoi()`: bảng ngay trên.
4. **Đường dẫn trong service viết NGẮN, không mang tiền tố của base URL.** Interceptor ghép base vào; service tự thêm nữa thì request bay tới một URL lặp đoạn giữa, trả 404, và trông hệt như "BE chưa làm endpoint".

```bash
# PASS khi không dòng nào chứa tiền tố base trong lời gọi HTTP.
grep -rnE "this\.http\.(get|post|put|patch|delete)<" src/FE/src --include='*.ts' \
  | grep -v '\.spec\.ts' | grep "'/api/"
```

### 2.2 `errorInterceptor` — nơi DUY NHẤT dịch lỗi

```typescript
// core/interceptors/error.interceptor.ts
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  // inject() gọi Ở ĐÂY — thân hàm interceptor là injection context,
  // callback của catchError thì KHÔNG (nó chạy ở tick khác).
  const toast = inject(ToastService);
  const translate = inject(TranslateService);
  const router = inject(Router);
  const routes = inject(CORE_ROUTES);
  const xsrf = inject(XsrfTokenStore);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const body = docEnvelopeLoi(err);

      // 403 CSRF — lấy token mới rồi gửi lại ĐÚNG MỘT lần. Cờ trên context chặn vòng lặp:
      // token mới vẫn bị từ chối nghĩa là lỗi thật, rơi xuống nhánh toast bên dưới.
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

      // 401 — phiên chết. Không toast: điều hướng đã là thông điệp.
      if (err.status === 401) {
        router.navigate([routes.dangNhap], { queryParams: { returnUrl: router.url } });
        return throwError(() => err);
      }

      // 400/409/422 kèm fieldErrors — lỗi thuộc về form, KHÔNG phải toast.
      // Form tự đọc qua ApiFailureError; interceptor chỉ đi qua.
      // Trường thật nằm ở body.error.fieldErrors — hỏi ở gốc envelope thì
      // điều kiện LUÔN sai và mọi lỗi validation rơi xuống toast.
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

/** Tra bảng dịch theo mã; đường lùi là câu BE gửi kèm. */
function dichLoi(translate: TranslateService, body: ApiFailure | null): string {
  if (!body) {
    return translate.instant('loi.khongKetNoiDuocMayChu');
  }
  const khoa = `loi.${body.error.code}`;
  const cau = translate.instant(khoa, body.error.messageParams ?? {});
  return cau === khoa ? body.error.message : cau;
}
```

**Cơ chế đường lùi ở `dichLoi` là bắt buộc.** `TranslateService.instant` trả về **chính khoá** khi không tìm thấy bản dịch; so sánh kết quả với khoá là cách duy nhất phát hiện điều đó. Không có nhánh này, một mã lỗi mới do BE thêm sẽ hiện ra màn hình dưới dạng `loi.CORE.USER.SOMETHING` — vừa vô nghĩa với người dùng, vừa rò cấu trúc mã lỗi nội bộ.

**Khoá dịch dựng từ `body.error.code`, không phải từ một trường ở gốc envelope.** Đọc nhầm chỗ cho ra `undefined`, khoá thành `loi.undefined`, và người dùng nhận một toast trống — không lỗi, không log, không dấu vết.

**Vì sao lỗi có `fieldErrors` không bắn toast:** người dùng đang nhìn cái form. Lỗi hiện ngay dưới ô nhập là thông tin; cùng lỗi đó bay lên góc màn hình dưới dạng toast là tiếng ồn, và tệ hơn là nó biến mất trước khi người ta đọc xong.

**`BO_QUA_TOAST_LOI` là `HttpContextToken`, không phải cờ toàn cục.** Cờ toàn cục nghĩa là hai request chạy song song giẫm lên nhau, và không có gì báo. Cách dùng:

```typescript
// Màn này hiển thị lỗi ngay trong form nên tự lo, không cần toast.
const ctx = new HttpContext().set(BO_QUA_TOAST_LOI, true);
this.http.post<ApiResult<NguoiDungDto>>('/nguoi-dung', body, { context: ctx });
```

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
    // Không bao giờ để âm: một lần ketThuc() thừa sẽ khoá vĩnh viễn chỉ báo tải
    // ở trạng thái ẩn cho mọi request sau đó.
    this.dangChay.update((n) => Math.max(0, n - 1));
  }
}
```

**Đếm chứ không phải cờ boolean.** Với một cờ, hai request chạy song song mà request nhanh về trước sẽ tắt chỉ báo trong khi request chậm vẫn đang chạy. Bộ đếm không có lỗi đó.

**`finalize` chứ không phải `tap`.** `finalize` chạy cho cả ba nhánh — thành công, lỗi, và **hủy** (unsubscribe). Dùng `tap` thì một request bị hủy sẽ để bộ đếm treo ở giá trị dương và chỉ báo tải quay mãi.

> Một chỉ báo tải toàn cục **không** thay thế trạng thái tải cục bộ của một lưới hay một nút bấm. Nó trả lời "app đang bận"; nút bấm cần trả lời "thao tác của TÔI đang chạy". Hai câu hỏi khác nhau, hai signal khác nhau.

### 2.4 Cái gì KHÔNG làm interceptor

| Ý tưởng | Vì sao không |
| --- | --- |
| Refresh token | Không có JWT ở hệ này. Phiên là cookie; hết hạn thì 401 và đăng nhập lại |
| Log mọi request lên server | Ồn, tốn băng thông, và trùng với log của BE vốn đã có `traceId` |
| Cache response | Cache thuộc về tầng service của feature, nơi biết dữ liệu nào cache được. Xem [`../wiki-core/fe/13-performance.md`](../wiki-core/fe/13-performance.md) |
| Chuyển đổi casing | Không cần — casing đã thống nhất (§1.3) |

---

## 3. Xử lý lỗi tập trung — component KHÔNG có `try/catch` rải rác

Luật: **một lỗi HTTP được xử lý ở đúng một chỗ.** `errorInterceptor` dịch và hiển thị; page chỉ xử lý phần *riêng của màn hình*.

```typescript
// pages/danh-sach-nguoi-dung/danh-sach-nguoi-dung.page.ts
@Component({
  selector: 'app-danh-sach-nguoi-dung',
  standalone: true,
  imports: [TranslateModule, DataTableComponent],
  templateUrl: './danh-sach-nguoi-dung.page.html',
})
export class DanhSachNguoiDungPage {
  private readonly service = inject(NguoiDungService);

  protected readonly items = signal<NguoiDung[]>([]);
  protected readonly dangTai = signal(false);
  protected readonly loi = signal(false);

  protected tai(query: PageQuery): void {
    this.dangTai.set(true);
    this.loi.set(false);
    this.service
      .danhSach(query)
      .pipe(finalize(() => this.dangTai.set(false)))
      .subscribe({
        next: (trang) => this.items.set(trang.items),
        // Toast đã do interceptor bắn. Ở đây chỉ ghi nhận để template
        // đổi sang khối "tải thất bại, thử lại" thay vì lưới rỗng.
        error: () => this.loi.set(true),
      });
  }
}
```

**Ba dạng bị cấm trong `pages/` và `components/`:**

| Dạng | Vì sao cấm |
| --- | --- |
| `catchError` bắn toast | Hai toast cho một sự cố — interceptor đã bắn rồi |
| `catchError(() => of([]))` | Nuốt lỗi, biến hỏng thành rỗng (§1.2) |
| `try/catch` quanh lời gọi API | Lỗi RxJS không đi vào `catch` của khối `try` bao ngoài `subscribe` — khối đó chỉ tạo cảm giác đã xử lý |

**Ngoại lệ có điều kiện — đường lùi trong service hạ tầng.** Một service hạ tầng dùng chung mà hỏng nó không được phép chặn cả ứng dụng (điển hình là menu điều hướng) **được phép** trả giá trị lùi trong chính service, với đúng một điều kiện: **lỗi phải hiện ra một lần trước khi trả giá trị lùi**. Không có điều kiện đó thì "đường lùi trong service" chính là dạng đã cấm ở §1.2, chỉ khác tên gọi.

Phép thử trước khi viện dẫn ngoại lệ này: *service này hỏng thì người dùng có mất luôn khả năng dùng phần còn lại của app không?* Không → đường lùi thuộc về nơi gọi, không phải service.

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

**Vì sao tách, kể cả khi hai kiểu trông giống hệt nhau lúc mới viết.** DTO là hình dạng của dây; model là hình dạng của màn hình. Trộn hai thứ nghĩa là mỗi lần server đổi tên một field, thay đổi lan tới mọi template đang bind field đó — và TypeScript bị xoá lúc chạy nên không có gì báo ở biên. Giữ hai kiểu và một mapper, thay đổi dừng lại ở đúng một hàm.

Cái giá phải trả nói thẳng: **thêm một file và một hàm cho mỗi thực thể, kể cả khi mapper chỉ đổi tên vài field.** Cảm giác thừa là có thật ở tuần đầu. Nó hết thừa ở lần đầu tiên BE đổi field.

### 4.2 Mapper đặt ở đâu

| Trường hợp | Đặt ở |
| --- | --- |
| Mapper của một feature | `<feature>/services/<feature>.mapper.ts` |
| Mapper dùng bởi nhiều feature trong cùng tầng | `<tang>/services/` của tầng đó |
| Mapper cho kiểu Core (người dùng, quyền, menu) | `core/http/` |

Mapper là **hàm thuần**, không phải class, không inject gì. Hàm thuần thì test không cần `TestBed`, và không có chỗ nào để lén gọi HTTP.

```typescript
// services/nguoi-dung.mapper.ts
import type { NguoiDungDto } from '../models/nguoi-dung.dto';
import type { NguoiDung } from '../models/nguoi-dung.model';

export function mapNguoiDung(dto: NguoiDungDto): NguoiDung {
  return {
    id: dto.id,
    hoTen: `${dto.ho} ${dto.ten}`.trim(),
    email: dto.email,
    dangHoatDong: dto.trangThai === 'Active',
    lanDangNhapCuoi: dto.lanDangNhapCuoi ? new Date(dto.lanDangNhapCuoi) : null,
  };
}
```

Hai việc mapper **phải** làm và không ai khác làm được:

1. **Chuyển chuỗi ngày sang `Date`.** Trên dây ngày là chuỗi. Nếu không chuyển ở mapper, mỗi template sẽ tự chuyển theo một kiểu và ít nhất một chỗ sẽ quên múi giờ.
2. **Chuyển mã trạng thái sang thứ màn hình cần.** Template không nên biết chuỗi `'Active'` là gì.

### 4.3 Cổng

```bash
# Luật F10 — DTO không lọt vào components/ hoặc pages/.
# PASS khi không in ra dòng nào.
grep -rnE 'Dto\b' src/FE/src/app --include='*.ts' | grep -E '/(components|pages)/' | grep -v '\.spec\.ts'
```

Hai chi tiết của mẫu này đã được kiểm chứng ở dự án tiền nhiệm, đừng "đơn giản hoá" ngược lại:

- Mẫu phải là `Dto\b`, **không** phải `\bDto\b`. Tên DTO thật luôn dạng `NguoiDungDto` — giữa `g` và `D` không có ranh giới từ, nên mẫu có `\b` ở đầu **không bao giờ khớp**, và cổng xanh vì mù chứ không vì sạch.
- Loại trừ `*.spec.ts` là **phạm vi đúng** của luật, không phải ngoại lệ nới tay: luật bảo vệ đường code chạy thật, còn test dựng stub tầng HTTP thì bắt buộc phải nói bằng hình dạng của dây.

---

## 5. CRUD và phân trang dùng chung — bằng HÀM, không base class

`core/` **không cung cấp base class cho service** ([`../wiki-core/fe/01-core-components.md`](../wiki-core/fe/01-core-components.md) §9). Một `BaseService<T>` luôn tiến hoá thành nơi chứa mọi thứ ai đó thấy "chung chung", và kế thừa làm một thay đổi ở Core vỡ mọi service nghiệp vụ cùng lúc. Thứ dùng chung ở đây là **hàm thuần**, service của feature *gọi* chúng thay vì *kế thừa* chúng.

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

/** Tham số phân trang gửi LÊN DÂY. Tên field khớp đúng chuỗi query param của hợp đồng. */
export interface PageQuery {
  readonly page: number;
  readonly pageSize: number;
  readonly sortBy?: string;
  readonly sortDescending?: boolean;
  readonly keyword?: string;
}
```

> 📖 **Tên tham số, khoảng hợp lệ, giá trị mặc định và mã lỗi khi ngoài khoảng: đọc [`../contracts/README.md`](../contracts/README.md) §8.** Đó là file chủ của hình dạng phân trang; mục này chỉ khai kiểu TypeScript phản chiếu nó.

Ba điều dễ sai, cả ba đều hỏng im lặng:

1. **`page`, không phải `pageNumber`.** Tên field trong kiểu TypeScript là thứ đi thẳng vào query string. Lệch tên thì BE không thấy tham số, **áp mặc định**, và người dùng bấm trang 7 luôn nhận trang 1 — không lỗi, không cảnh báo.
2. **Không có `totalPages` trên dây.** Số trang là dữ liệu phái sinh; FE tự tính từ `totalCount` và `pageSize`. Gửi qua dây là hai nguồn sự thật cho một con số, và bản cũ sẽ sống sót sau khi `pageSize` đổi.
3. **Kiểu đọc-từ-URL không phải kiểu gửi-lên-dây.** Query param trên URL đặt theo ngôn ngữ của giao diện; chuyển sang `PageQuery` là một bước ánh xạ **tường minh**, khai ở đúng một chỗ — xem [`fe-routing-guard.md`](fe-routing-guard.md) §7.2.

### 5.2 Hàm dùng chung, và cơ chế giữ luật F10

```typescript
// core/http/crud.ts — hàm thuần, không class, không kế thừa

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
```

Service của feature:

```typescript
@Injectable({ providedIn: 'root' })
export class NguoiDungService {
  private readonly http = inject(HttpClient);
  private readonly duongDan = '/nguoi-dung';   // NGẮN — interceptor ghép base URL

  danhSach(q: PageQuery): Observable<PagedList<NguoiDung>> {
    return danhSachTrang<NguoiDungDto, NguoiDung>(this.http, this.duongDan, q, mapNguoiDung);
  }

  chiTiet(id: string): Observable<NguoiDung> {
    return theoId<NguoiDungDto, NguoiDung>(this.http, this.duongDan, id, mapNguoiDung);
  }
}
```

**Mapper là tham số bắt buộc của hàm**, nên không có đường nào lấy được `TDto` ra khỏi `core/http/` mà không đi qua một mapper. Quên mapper là **lỗi biên dịch**, sớm hơn và rõ hơn một lỗi phạm vi truy cập. Cổng F10 (§4.3) vẫn là lớp canh thứ hai, quét việc DTO lọt vào `components/` và `pages/`.

**Hàm dùng chung không bắt lỗi.** Lỗi thuộc về interceptor. Một lớp bắt lỗi ẩn trong hàm dùng chung nghĩa là mỗi service đều có xử lý lỗi vô hình mà người đọc service không thấy.

> ⚠️ **Endpoint không theo khuôn CRUD** (thao tác hàng loạt, xuất file, quy trình nhiều bước) thì gọi thẳng `HttpClient` trong service. Ép mọi thứ vào một hàm dùng chung bằng cách thêm tham số điều kiện là cách hàm đó biến thành thứ khó đọc hơn code nó thay thế — đúng chế độ hỏng mà việc bỏ base class tránh được.

---

## 6. Hủy request, retry, timeout

### 6.1 Hủy — mặc định, không phải tính năng thêm

Angular hủy request HTTP khi observable bị unsubscribe. Hai chỗ phải khai thác điều đó:

```typescript
// Ô tìm kiếm: hủy request cũ khi người dùng gõ tiếp.
protected readonly ketQua = toSignal(
  toObservable(this.tuKhoa).pipe(
    debounceTime(300),
    distinctUntilChanged(),
    switchMap((tu) => this.service.timKiem(tu)),
  ),
  { initialValue: [] },
);
```

**`switchMap` chứ không `mergeMap`.** Với `mergeMap`, gõ năm ký tự sinh năm request và kết quả hiển thị là **request nào về sau cùng**, không phải request của từ khoá cuối. Đó là lỗi hiện ra như "kết quả tìm kiếm sai ngẫu nhiên" và gần như không tái hiện được.

Component tự subscribe thì phải hủy khi bị hủy:

```typescript
private readonly huy = inject(DestroyRef);
// ...
this.service.danhSach(query).pipe(takeUntilDestroyed(this.huy)).subscribe(/* ... */);
```

### 6.2 Retry — hẹp, có điều kiện

Retry chỉ áp cho **request đọc** (`GET`) và **chỉ khi** lỗi là lỗi mạng hoặc 5xx. Không bao giờ retry `POST`/`PUT`/`DELETE`: request ghi có thể đã tới server và thành công trước khi kết nối đứt, và lần retry sẽ tạo bản ghi thứ hai.

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

Áp ở **service của feature**, không ở interceptor. Ở interceptor thì mọi request đều retry, gồm cả những request mà retry là sai.

### 6.3 Timeout

Không có timeout, một request treo sẽ giữ chỉ báo tải quay mãi và người dùng không biết nên chờ hay tải lại trang.

```typescript
export const TIMEOUT_MAC_DINH = 30_000;
export const TIMEOUT_XUAT_FILE = 120_000;
```

Đặt ở service, khai tường minh cho từng nhóm endpoint. Endpoint xuất báo cáo được phép lâu hơn — nhưng phải **khai bằng một hằng số có tên**, không phải bằng cách bỏ timeout đi.

📐 Với thao tác dài hơn ngưỡng trên (kết xuất lớn, nhập dữ liệu hàng loạt), khuôn đúng là BE trả về một mã việc và FE hỏi trạng thái theo chu kỳ, chứ không phải nâng timeout. Chưa chốt chi tiết — ghi lại đây để không ai âm thầm nâng timeout lên vài phút.

---

## 7. Khi endpoint chưa tồn tại

Đừng tự đoán hình dạng response rồi code như thật. Viết **API Contract Card** vào [`../contracts/`](../contracts/), mỗi endpoint một card, để phía BE review và chốt trước khi cả hai bên thi công.

Card sai vẫn tốt hơn giả định ngầm: card sai thì có một chỗ để sửa và một người phản đối; giả định ngầm thì chỉ lộ ra lúc ghép nối, khi cả hai bên đều đã viết xong.

Trong lúc chờ, FE dựng service với mapper và kiểu đầy đủ, dữ liệu lấy từ stub trong `*.spec.ts` — **không** stub bằng cách trả dữ liệu cứng ngay trong service, vì dữ liệu cứng đó sẽ ở lại sau khi endpoint có thật.

---

## 8. Secret — không có, không bao giờ

Bundle FE là **văn bản công khai**. Mọi thứ trong `environments/*.ts` đều đọc được bằng cách mở tab Network.

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
| S6 | Không secret trong bundle FE | §8 |

Bảng đầy đủ: [`../RULES.md`](../RULES.md) §7. Vì sao có envelope và bốn cái bẫy khi tiêu thụ nó: [`../wiki-core/fe/02-http-envelope.md`](../wiki-core/fe/02-http-envelope.md).
