---
kind: tham-chieu
scope: core
verified: chua-doi-chieu
---

# Lý do, bẫy, ví dụ mở rộng — `fe-api-client.md`

> File này giữ **lý do, bẫy, ví dụ dài** của [`fe-api-client.md`](../../../quy-uoc/fe-api-client.md); luật ở đó. Số mục dưới đây trùng số mục của file luật; mục không có gì dời ghi "—".

---

## 1. Envelope — hình dạng mọi response

- **Đọc `code`/`fieldErrors` ở gốc envelope thì hỏng thế nào** — Đọc chúng ở gốc thì hỏng hai chỗ: điều kiện kiểm `fieldErrors` **luôn sai**, nên mọi lỗi validation rơi xuống toast, đúng thứ mục [`fe-api-client.md`](../../../quy-uoc/fe-api-client.md) §2.2 cấm; và khoá dịch dựng từ một trường không tồn tại — cho ra chuỗi `undefined` rồi một toast trống. Cả hai đều không lỗi biên dịch và không test nào bắt, vì cả hai bên vẫn là JSON hợp lệ.

- **Bind thẳng `fieldErrors` vào ô nhập** — Bind thẳng nó vào ô nhập cho ra `[object Object]`; lấy `String(...)` thì mất `code` và lỗi từng ô **không dịch được**.

**Vì sao là union phân biệt theo `success`, không phải một interface có mọi field đều optional.** Với union, TypeScript ép người viết kiểm `success` trước khi chạm `data`; sau một câu `if (res.success)` thì `res.data` có kiểu `T` chứ không phải `T | null`. Với một interface phẳng, `data` luôn `T | null` ở mọi chỗ, và cách nhanh nhất để đi tiếp là thêm dấu `!` — tức tắt đúng thứ vừa dựng lên.

Khối import của `core/http/api-result.model.ts` ([`fe-api-client.md`](../../../quy-uoc/fe-api-client.md) §1):

```typescript
import { HttpContextToken, HttpErrorResponse } from '@angular/common/http';
```

### 1.1 Bốn field ít người dùng đúng

- **Vì sao `traceId` phải hiện ra** — Không phải để người dùng hiểu, mà để câu "màn hình báo lỗi" biến thành một chuỗi tra được. Chi phí là một dòng chữ nhỏ; giá trị là khoảng cách giữa "không tái hiện được" và "mở log ra thấy ngay".

### 1.2 Đọc `data` — một cửa duy nhất

Dạng thứ ba nguy hiểm nhất và là dạng dễ lọt qua review nhất. Vấn đề không phải giá trị mặc định xấu — mà là **giá trị mặc định trùng khít với một câu trả lời hợp lệ của server**. Lưới rỗng, menu rỗng, số 0: server có quyền trả về thật những thứ đó. Khi envelope hỏng cũng cho ra đúng hình ảnh ấy, không ai — người dùng, người trực hệ thống, hay chính người viết code — phân biệt được "không có gì" với "hỏng". Lỗi loại này không vào log, không vào toast, không vào test; nó chỉ vào **quyết định sai của người đang nhìn màn hình**.

> Khi viết chú thích cho luật này, **gọi tên** dạng bị cấm ("toán tử hợp nhất null kèm giá trị mặc định") thay vì gõ lại nguyên văn nó. Lệnh grep không phân biệt code với comment, và một chú thích tử tế sẽ làm cổng đỏ mà không có gì hỏng cả.

### 1.3 Casing trên dây

—

## 2. Chuỗi interceptor FE

Thứ tự không tuỳ tiện. Interceptor chạy theo thứ tự khai lúc đi ra và **ngược lại** lúc response quay về, nên `errorInterceptor` đặt cuối danh sách sẽ là đứa **đầu tiên** thấy lỗi — đúng chỗ để dịch lỗi trước khi ai khác chạm vào. `loadingInterceptor` bọc ngoài nó để bộ đếm luôn giảm kể cả khi `errorInterceptor` biến response thành lỗi.

- **Vì sao không thêm interceptor thứ tư ghép tiền tố** — `authInterceptor` đã ghép base URL; thêm một interceptor ghép tiền tố nữa cho ra URL lặp đoạn giữa → 404 trông **hệt như BE chưa làm endpoint**.

- **Vì sao không dùng `withXsrfConfiguration`** — Nó chỉ gắn header cho URL tương đối và cùng origin, mà API ở origin khác.

### 2.1 `authInterceptor` — cookie phiên và XSRF

- **Vì sao không miễn trừ tĩnh bằng danh sách tiền tố** — danh sách đó phải nhớ cập nhật mỗi lần thêm một thư mục tĩnh, và quên là hỏng im lặng — request tĩnh bị ghép base URL, gửi nhầm sang máy API và 404. `HttpClient` tạo từ `HttpBackend` đi vòng qua toàn bộ chuỗi, nên tệp dịch không bị ghép base URL, không kèm cookie, không mang token.

- **Ràng buộc 2 — vì sao token XSRF chỉ trong bộ nhớ** — Không có lý do lưu bền: mỗi lần khởi động app đều lấy token mới.

- **Ràng buộc 1 — thiếu `withCredentials`** — Thiếu nó, cookie phiên không được gửi và mọi request đều bị coi là chưa đăng nhập — triệu chứng là "đăng nhập xong vẫn 401", rất dễ bị chẩn đoán nhầm sang phía BE.

- **Ràng buộc 3 — vì sao không dùng `withXsrfConfiguration`** — Không dùng `withXsrfConfiguration` của Angular: nó chỉ gắn header cho URL tương đối và cùng origin, nên với API ở origin khác nó **im lặng không gắn** — mọi request ghi 403. Gắn cho mọi request mà không lọc là rò token sang origin khác — nên chỉ gắn ở nhánh đã xác định là request tới API.

- **Ràng buộc 4 — đường dẫn mang tiền tố base** — Interceptor ghép base vào; service tự thêm nữa thì request bay tới một URL lặp đoạn giữa, trả 404, và trông hệt như "BE chưa làm endpoint".

Khối import của `XsrfTokenStore` ([`fe-api-client.md`](../../../quy-uoc/fe-api-client.md) §2.1):

```typescript
// core/auth/xsrf-token.store.ts — phần import
import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, map, tap } from 'rxjs';
import { ApiResult, BO_QUA_TOAST_LOI } from '../http/api-result.model';
import { unwrapData } from '../http/unwrap';
```

### 2.2 `errorInterceptor` — nơi DUY NHẤT dịch lỗi

- **Vì sao `inject()` gọi ở thân hàm interceptor** — Thân hàm là injection context; callback của `catchError` thì không, vì nó chạy ở tick khác.

- **Nhánh CSRF — vì sao chỉ gửi lại một lần, và vì sao `ORIGIN_REJECTED` không vào nhánh này** — Token mới vẫn bị từ chối nghĩa là lỗi thật, rơi xuống lớp lỗi xuyên suốt — toast kèm `traceId`, có cờ hay không. `Origin` do trình duyệt đặt nên lấy token mới rồi gửi lại vẫn nhận đúng mã đó — gửi lại vô nghĩa.

- **Nhánh CSRF — vì sao lần gửi lại phải bọc lại bằng chính bộ xử lý lỗi, và vì sao context phải là bản sao** — `catchError` không bắt lỗi của observable do callback của chính nó trả về, nên lần gửi lại trả thẳng từ nhánh CSRF sẽ để lỗi (422 kèm `fieldErrors`, 401, CSRF lần hai, mạng đứt) thoát ra khỏi interceptor dưới dạng lỗi HTTP thô: màn không nhận `ApiFailureError`, không toast, không nhánh 401 chạy. Bộ xử lý lỗi vì thế dựng theo từng request (`xuLyLoi(yeuCau)`) để áp lại cho bản clone. Cờ `DA_THU_LAI_XSRF` đặt trên context của **bản clone**: `HttpContext.set()` sửa tại chỗ, mà một service giữ một context dùng chung cho mọi lệnh ghi — gắn cờ vào chính instance đó thì cờ nằm lại vĩnh viễn và những lần CSRF hết hạn sau không còn được thử lại. Bản ghi sự cố: [`../../../audit/2026-09-21-mau-ma-trong-luat-khong-ai-chay.md`](../../../audit/2026-09-21-mau-ma-trong-luat-khong-ai-chay.md).

- **Nhánh CSRF — vì sao chỉ lệnh ghi (`LENH_AN_TOAN`)** — `XsrfTokenStore.lamMoi()` là một `HttpClient.get` đi qua **chính** `errorInterceptor`, và GET đó không mang cờ `DA_THU_LAI_XSRF`: cờ nằm trên context của bản clone được gửi lại, không phải của lời gọi làm mới. Cho GET vào nhánh thì một GET token bị `CSRF_REJECTED` gọi lại `lamMoi()` liên tục, im lặng, không lỗi hiển thị. BE bỏ qua kiểm CSRF cho phương thức an toàn nên ca này hiếm — mẫu vẫn phải mang điều kiện, vì người dựng lại interceptor từ mẫu không biết BE làm gì. Tập phương thức phản chiếu tập BE bỏ qua kiểm CSRF ([`be-api-controller.md`](../../../quy-uoc/be-api-controller.md) §7.5) — bốn phương thức; `TRACE` hầu như không phát sinh qua `HttpClient` nhưng giữ cho hai tập không lệch nhau. Kiểm lại chuỗi `LENH_AN_TOAN` trong code nếu tập đổi. Bản ghi sự cố: [`../../../audit/2026-09-21-mau-ma-trong-luat-khong-ai-chay.md`](../../../audit/2026-09-21-mau-ma-trong-luat-khong-ai-chay.md).

- **Thiếu đường lùi ở `dichLoi`** — `TranslateService.instant` trả về **chính khoá** khi không tìm thấy bản dịch; so sánh kết quả với khoá là cách duy nhất phát hiện điều đó. Không có nhánh này, một mã lỗi mới do BE thêm sẽ hiện ra màn hình dưới dạng `loi.CORE.USER.SOMETHING` — vừa vô nghĩa với người dùng, vừa rò cấu trúc mã lỗi nội bộ.

- **Khoá dịch dựng từ trường sai** — Đọc nhầm chỗ cho ra `undefined`, khoá thành `loi.undefined`, và người dùng nhận một toast trống — không lỗi, không log, không dấu vết.

- **403 `FORBIDDEN` — vì sao làm mới cả menu, vì sao một lời gọi** — Đổi vai trò hay ma trận quyền có hiệu lực từ request kế tiếp ([`../contracts/users.md`](../../../contracts/users.md) §7), nên mã này giữa phiên thường nghĩa là tập quyền FE đang giữ đã cũ: nút vẫn hiện cho thao tác người dùng không còn quyền làm. — menu do server lọc theo quyền ([`../contracts/meta-menu.md`](../../../contracts/meta-menu.md)), nên thay quyền mà giữ menu cũ là sidebar vẫn mời vào màn vừa mất quyền. Directive và guard đọc cùng signal nên tự cập nhật. Nhiều 403 cùng lúc chỉ sinh một lời gọi. Nhánh CSRF ở trên chạy trước và tự thử lại, nên token hết hạn không kéo theo lời gọi làm mới.

- **403 mang mã nghiệp vụ** — Một card có thể trả 403 với mã riêng — mã đó nói về **dữ liệu** của thao tác, không nói tập quyền đã cũ, nên câu "không có quyền" chung sai nghĩa và lời gọi làm mới là thừa. Hệ quả phải nhớ: màn gọi một endpoint có 403 nghiệp vụ trong card mà **không** xử lý mã đó thì thao tác hỏng im lặng — không toast, không dòng lỗi.

- **403 `PASSWORD_CHANGE_REQUIRED` — vì sao không toast, cơ chế** — Mã này giữa phiên nghĩa là cờ buộc đổi mật khẩu đang bật mà `AuthService` chưa biết. Không toast vì đổi màn đã là thông điệp, cùng lý do với nhánh 401. Interceptor giữ nguyên luật không điều hướng khi gặp 403 ([`fe-routing-guard.md`](../../../quy-uoc/fe-routing-guard.md) §8); khi `me` về, `AuthService` cho router chạy lại guard của URL hiện tại và `mustChangePasswordGuard` trả đích.

- **429 — vì sao toast ở interceptor, vì sao header thắng envelope, vì sao màn tắt toast đọc envelope** — Giới hạn tần suất áp cho mọi endpoint, không riêng đăng nhập, nên toast mặc định nằm ở interceptor. Header thắng tham số trong envelope vì header là con số limiter đặt cho **đúng** response này. Màn tắt toast thì chỉ nhận `ApiFailureError` — envelope và status, không header — nên số giây phải đi kèm trong `messageParams`; BE thiếu tham số đó thì câu hiện nguyên chỗ giữ `{{RetryAfterSeconds}}`, vì bộ dịch để nguyên chỗ giữ khi không có giá trị.

- **Bẫy `Retry-After` qua CORS** — 🪤 API ở origin khác, nên trình duyệt chỉ cho JavaScript đọc `Retry-After` khi BE khai nó trong `Access-Control-Expose-Headers`; thiếu khai báo thì `headers.get` trả `null` — không lỗi, không cảnh báo — và câu dịch rơi về tham số trong envelope. Kiểm bằng tab Network **và** một test đọc header thật, không kiểm bằng mắt câu toast.

- **409 `CONFLICT` — vì sao không gửi lại, không tải lại hộ; ca hiện trong trang; mã 409 khác** — Mã dùng chung này ([`../contracts/auth.md`](../../../contracts/auth.md) §11) nghĩa là người khác đã ghi bản ghi sau khi màn đọc nó; mô hình, lý do không tự thử lại, và ba câu mà thông điệp phải trả lời ở [`../wiki-core/be/06-concurrency-control.md`](../../../wiki-core/be/06-concurrency-control.md) §6. Interceptor **không** gửi lại — gửi lại là ghi đè thay đổi của người kia, đúng thứ cơ chế này tồn tại để chặn — và **không** tải lại dữ liệu hộ: thứ người dùng đang nhập chưa mất, và chỉ màn biết giữ nó thế nào. Màn muốn hiện xung đột ngay trong trang thay vì toast — ma trận phân quyền với token cấp tập ([`../Design/Screens/12-ma-tran-phan-quyen.md`](../../../Design/Screens/12-ma-tran-phan-quyen.md)) — tắt toast bằng `BO_QUA_TOAST_LOI` như mọi lỗi ngoài lớp lỗi xuyên suốt. Mã 409 khác (`*_DUPLICATED`, `*_DUPLICATE`) không vào nhánh này: chúng nói về dữ liệu vừa gửi, tải lại không đổi được gì, nên đi theo nhánh toast chung.

**Vì sao lỗi có `fieldErrors` không bắn toast:** người dùng đang nhìn cái form. Lỗi hiện ngay dưới ô nhập là thông tin; cùng lỗi đó bay lên góc màn hình dưới dạng toast là tiếng ồn, và tệ hơn là nó biến mất trước khi người ta đọc xong.

- **Vì sao không dùng cờ toàn cục** — Cờ toàn cục nghĩa là hai request chạy song song giẫm lên nhau, và không có gì báo.

- **Lớp lỗi xuyên suốt — vì sao cờ không tắt được, vì sao nhánh đứng trước 429/409** — `traceId` chỉ tới người dùng qua toast; để màn mang cờ nuốt 5xx thì câu *"báo mã theo dõi"* hiện ra mà không có mã nào để báo. Nhánh đứng ngay sau các nhánh 403 để không nhánh nào tôn trọng cờ chặn trước nó. 5xx không envelope (proxy, load balancer trả 502/504) mang mã client riêng chứ không câu mất kết nối: đã có phản hồi, nên câu đó sai sự thật và đẩy người dùng đi kiểm mạng của họ (chốt 2026-09-25). Quyết định và phương án đã loại: [`../../../adr/0094-lop-loi-xuyen-suot-luon-toast-ke-ca-khi-man-tat-toast.md`](../../../adr/0094-lop-loi-xuyen-suot-luon-toast-ke-ca-khi-man-tat-toast.md).

Khối đầy đủ của `core/interceptors/error.interceptor.ts` — chuyển từ file luật §2.2 ngày 2026-09-22; file luật giữ thứ tự nhánh, khối này là hiện thực và sửa cùng lượt với code (luật D44, [`../../../DEBT.md`](../../../DEBT.md)). Phần import ở khối kế tiếp:

```typescript
// core/interceptors/error.interceptor.ts — phần import ở khối ngay dưới
/** Đánh dấu request đã được gửi lại một lần sau CSRF_REJECTED — chặn vòng lặp. */
const DA_THU_LAI_XSRF = new HttpContextToken<boolean>(() => false);

/** Phương thức BE không kiểm CSRF (be-api-controller.md §7.5) — không có gì để "lấy token rồi gửi lại". */
const LENH_AN_TOAN: ReadonlySet<string> = new Set(['GET', 'HEAD', 'OPTIONS', 'TRACE']);

/**
 * `HttpContext.set()` sửa map TẠI CHỖ. Một service giữ MỘT context dùng chung cho mọi lệnh ghi
 * (`private readonly ctx = new HttpContext().set(BO_QUA_TOAST_LOI, true)`) — gắn cờ thẳng vào nó thì
 * cờ nằm lại vĩnh viễn và mọi lần CSRF hết hạn sau đó không còn được thử lại. Luôn dựng BẢN SAO.
 */
function ctxDaThuLaiXsrf(goc: HttpContext): HttpContext {
  const ban = new HttpContext();
  for (const token of goc.keys()) {
    ban.set(token, goc.get(token));
  }
  return ban.set(DA_THU_LAI_XSRF, true);
}

/**
 * Với `responseType: 'blob'` (tải tệp xuất — fe-api-client.md §6.4), Angular giao MỌI thân phản hồi
 * dưới dạng `Blob`, kể cả thân lỗi 4xx/5xx dù server gửi `application/json`. `docEnvelopeLoi` kiểm
 * `'success' in body` trên một `Blob` ⇒ `null` ⇒ toast *mất kết nối* cho một lỗi 422 có mã.
 * `Blob.text()` bất đồng bộ, nên nhánh gọi hàm này là `from(...).pipe(switchMap(...))`.
 */
async function bocThanBlob(err: HttpErrorResponse): Promise<HttpErrorResponse> {
  let than: unknown = null;
  try {
    const chu = await (err.error as Blob).text();
    than = chu === '' ? null : JSON.parse(chu);
  } catch {
    than = null; // proxy trả HTML, tệp rỗng — nhánh dưới xử lý như phản hồi không phải envelope.
  }
  return new HttpErrorResponse({
    error: than,
    headers: err.headers,
    status: err.status,
    statusText: err.statusText,
    url: err.url ?? undefined,
  });
}

// dichLoi và thamSoRetryAfter import từ core/http/dich-loi.ts — khối bên dưới, không khai trong file này.
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  // inject() gọi Ở ĐÂY — callback của catchError KHÔNG phải injection context.
  const toast = inject(ToastService);
  const translate = inject(TranslateService);
  const auth = inject(AuthService);
  const hetPhien = inject(SessionExpiryHandler);
  const xsrf = inject(XsrfTokenStore);

  /**
   * Bộ xử lý lỗi cho MỘT request. Dựng theo request để lần gửi lại sau CSRF_REJECTED dùng lại đúng
   * bộ này trên request đã clone (mang cờ `DA_THU_LAI_XSRF` và context của nó).
   */
  const xuLyLoi =
    (yeuCau: HttpRequest<unknown>) =>
    (err: HttpErrorResponse): Observable<HttpEvent<unknown>> => {
      // Thân lỗi là Blob (request tải tệp xuất) — bóc envelope TRƯỚC khi dịch, rồi chạy lại chính
      // bộ xử lý này trên phản hồi đã bóc. Không nhánh nào dưới đây phải biết về Blob.
      if (err.error instanceof Blob) {
        return from(bocThanBlob(err)).pipe(switchMap((daBoc) => xuLyLoi(yeuCau)(daBoc)));
      }

      const body = docEnvelopeLoi(err);
      // Mọi nhánh trả lỗi về người gọi qua đây — mang status thật để màn nhận diện lớp xuyên suốt.
      const nem = (): Observable<never> => throwError(() => new ApiFailureError(body, err.status));

      // 403 CSRF — lấy token mới rồi gửi lại ĐÚNG MỘT lần; cờ trên context chặn vòng lặp.
      // CORE.AUTH.ORIGIN_REJECTED KHÔNG vào nhánh này. Chỉ LỆNH GHI mới được thử lại — lý do ở đoạn sau mẫu.
      // Ngoại lệ có tên: GET mang dấu CO_TAC_DUNG_PHU (tải tệp xuất) — BE kiểm token cho nó như lệnh
      // ghi (§6.4), nên nó cũng được gửi lại một lần. `lamMoi()` KHÔNG mang dấu ⇒ đường đệ quy vẫn đóng.
      const duocGuiLai = !LENH_AN_TOAN.has(yeuCau.method) || yeuCau.context.get(CO_TAC_DUNG_PHU);
      if (
        body?.error?.code === 'CORE.AUTH.CSRF_REJECTED' &&
        duocGuiLai &&
        !yeuCau.context.get(DA_THU_LAI_XSRF)
      ) {
        // Lỗi của CHÍNH lần lấy token được dịch ở lượt GET đó — mang cờ tắt toast của request gốc
        // theo, để màn tự hiện lỗi không bị toast chồng (fe-ui-conventions.md §6.2).
        return xsrf.lamMoi({ boQuaToastLoi: yeuCau.context.get(BO_QUA_TOAST_LOI) }).pipe(
          switchMap((token) => {
            const guiLai = yeuCau.clone({
              setHeaders: { 'X-XSRF-TOKEN': token },
              context: ctxDaThuLaiXsrf(yeuCau.context),
            });
            // catchError KHÔNG bắt lỗi của observable do CALLBACK của chính nó trả về — nên lần gửi lại
            // phải được bọc lại bằng cùng bộ xử lý (đã mang cờ DA_THU_LAI_XSRF ⇒ không có lần gửi thứ ba).
            return next(guiLai).pipe(catchError(xuLyLoi(guiLai)));
          }),
        );
      }

      // 401 — phiên chết. Không toast. Request mang BO_QUA_HET_PHIEN tự xử lý (§2.5).
      if (err.status === 401) {
        if (!yeuCau.context.get(BO_QUA_HET_PHIEN)) {
          hetPhien.handle();
        }
        return nem();
      }

      // 403 CORE.AUTH.FORBIDDEN — làm mới quyền và menu, rồi xuống nhánh lớp xuyên suốt. KHÔNG điều hướng.
      if (err.status === 403 && body?.error.code === 'CORE.AUTH.FORBIDDEN') {
        auth.lamMoiQuyen();
      } else if (err.status === 403 && body?.error.code === 'CORE.AUTH.PASSWORD_CHANGE_REQUIRED') {
        // Cờ buộc đổi mật khẩu bật giữa phiên: làm mới phiên, guard lo phần còn lại (fe-routing-guard.md §5.4). KHÔNG điều hướng, KHÔNG toast.
        void auth.lamMoiPhien();   // Promise — interceptor không chờ; màn cần chờ thì await (fe-routing-guard.md §3.3)
        return nem();
      } else if (err.status === 403 && body !== null && !body.error.code.startsWith('CORE.AUTH.')) {
        // 403 mang mã nghiệp vụ — màn tự xử lý theo card. Không toast chung, không làm mới quyền.
        return nem();
      }

      // Lớp xuyên suốt (5xx, 403 CORE.AUTH.* còn lại) — LUÔN toast kèm traceId, KHÔNG xét
      // BO_QUA_TOAST_LOI: khu lỗi của màn bỏ qua lớp này (`laLoiXuyenSuot`), nên toast là chỗ hiện duy nhất.
      if (laLoiXuyenSuot(err.status, body)) {
        if (body === null) {
          // 5xx không envelope (proxy trả 502/504 dạng HTML): máy chủ CÓ trả lời nên không phải câu mất
          // kết nối; không mã, không traceId — câu của mã phía client `CORE.CLIENT.SERVER_UNAVAILABLE`
          // (be-cqrs-handler.md §7.4). 403 không envelope không vào lớp này, nên `null` ở đây luôn là 5xx.
          toast.loi(translate.instant('loi.CORE.CLIENT.SERVER_UNAVAILABLE'), null);
        } else {
          toast.loi(dichLoi(translate, body), body.traceId);
        }
        return nem();
      }

      // 429 — siết tần suất ở MỌI màn (contracts/auth.md §10–§11). Số giây chờ đọc từ Retry-After.
      if (err.status === 429) {
        if (!yeuCau.context.get(BO_QUA_TOAST_LOI)) {
          toast.loi(dichLoi(translate, body, thamSoRetryAfter(err)), body?.traceId ?? null);
        }
        return nem();
      }

      // 409 CORE.CONCURRENCY.CONFLICT — người khác đã ghi sau khi màn đọc (contracts/auth.md §11).
      // Toast rồi trả lỗi về màn. KHÔNG gửi lại, KHÔNG tải lại hộ.
      if (err.status === 409 && body?.error.code === 'CORE.CONCURRENCY.CONFLICT') {
        if (!yeuCau.context.get(BO_QUA_TOAST_LOI)) {
          toast.loi(dichLoi(translate, body), body?.traceId ?? null);
        }
        return nem();
      }

      // 400/409/422 kèm fieldErrors — lỗi thuộc về form, KHÔNG toast. Trường thật ở body.error.fieldErrors.
      if (body?.error?.fieldErrors) {
        return nem();
      }

      // Màn tự hiển thị lỗi của mình thì tắt toast cho ĐÚNG request đó.
      if (!yeuCau.context.get(BO_QUA_TOAST_LOI)) {
        toast.loi(dichLoi(translate, body), body?.traceId ?? null);
      }
      return nem();
    };

  return next(req).pipe(catchError(xuLyLoi(req)));
};
```

Khối import của `errorInterceptor` ([`fe-api-client.md`](../../../quy-uoc/fe-api-client.md) §2.2):

```typescript
// core/interceptors/error.interceptor.ts — phần import
import {
  HttpContext, HttpContextToken, HttpErrorResponse, HttpEvent, HttpInterceptorFn, HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Observable, catchError, from, switchMap, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { SessionExpiryHandler } from '../auth/session-expiry.handler';
import { XsrfTokenStore } from '../auth/xsrf-token.store';
import { dichLoi, thamSoRetryAfter } from '../http/dich-loi';
import { laLoiXuyenSuot } from '../http/loi-xuyen-suot';
import {
  ApiFailureError, BO_QUA_HET_PHIEN, BO_QUA_TOAST_LOI, CO_TAC_DUNG_PHU, docEnvelopeLoi,
} from '../http/api-result.model';
import { ToastService } from '../toast/toast.service';
```

### 2.3 `loadingInterceptor` — đếm request đang chạy

- **Vì sao đếm chứ không cờ boolean** — Với một cờ, hai request chạy song song mà request nhanh về trước sẽ tắt chỉ báo trong khi request chậm vẫn đang chạy. Bộ đếm không có lỗi đó. Không để bộ đếm âm: một `ketThuc()` thừa sẽ khoá vĩnh viễn chỉ báo tải ở trạng thái ẩn cho mọi request sau đó.

- **Vì sao `finalize` chứ không `tap`** — `finalize` chạy cho cả nhánh **hủy** (unsubscribe); dùng `tap` thì một request bị hủy để bộ đếm treo ở giá trị dương và chỉ báo tải quay mãi.

> Một chỉ báo tải toàn cục **không** thay thế trạng thái tải cục bộ của một lưới hay một nút bấm. Nó trả lời "app đang bận"; nút bấm cần trả lời "thao tác của TÔI đang chạy". Hai câu hỏi khác nhau, hai signal khác nhau.

### 2.4 Cái gì KHÔNG làm interceptor

—

### 2.5 `SessionExpiryHandler`

Chốt "một lần" dựa vào `daDangNhap()` chứ không vào một cờ riêng: cờ riêng thì phải nhớ hạ nó sau khi đăng nhập, và quên hạ là lần hết phiên thứ hai không điều hướng gì cả. Tab nhận sự kiện `storage` dọn theo mà **không** báo lại, để không dội qua lại giữa các tab.

- **Sai mật khẩu không cần `BO_QUA_HET_PHIEN`** — nó là 422 `CORE.AUTH.INVALID_CREDENTIALS`, không phải 401 ([`../../../contracts/auth.md`](../../../contracts/auth.md) §3).

Khối import của `SessionExpiryHandler` ([`fe-api-client.md`](../../../quy-uoc/fe-api-client.md) §2.5):

```typescript
// core/auth/session-expiry.handler.ts — phần import
import { DestroyRef, Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { CORE_ROUTES } from '../config/core-routes';
import { AuthService } from './auth.service';
```

Phần import mà `dangXuat()` cần trong `core/auth/auth.service.ts`:

```typescript
import { HttpClient, HttpContext } from '@angular/common/http';
import { Observable, catchError, map, of, switchMap, tap, throwError } from 'rxjs';
import { ApiFailureError, ApiResult, BO_QUA_HET_PHIEN } from '../http/api-result.model';
import { XsrfTokenStore } from './xsrf-token.store';
import { TabSessionBroadcastService } from './tab-session-broadcast.service';
```

## 3. Xử lý lỗi tập trung — component KHÔNG có `try/catch` rải rác

- **Vì sao đường lùi phải kèm điều kiện** — Không có điều kiện đó thì "đường lùi trong service" chính là dạng đã cấm ở [`fe-api-client.md`](../../../quy-uoc/fe-api-client.md) §1.2, chỉ khác tên gọi.

## 4. Ranh giới DTO ↔ model — luật F10

—

### 4.1 Luật

**Vì sao tách, kể cả khi hai kiểu trông giống hệt nhau lúc mới viết.** DTO là hình dạng của dây; model là hình dạng của màn hình. Trộn hai thứ nghĩa là mỗi lần server đổi tên một field, thay đổi lan tới mọi template đang bind field đó — và TypeScript bị xoá lúc chạy nên không có gì báo ở biên. Giữ hai kiểu và một mapper, thay đổi dừng lại ở đúng một hàm.

Cái giá phải trả nói thẳng: **thêm một file và một hàm cho mỗi thực thể, kể cả khi mapper chỉ đổi tên vài field.** Cảm giác thừa là có thật ở tuần đầu. Nó hết thừa ở lần đầu tiên BE đổi field.

### 4.2 Mapper đặt ở đâu

- **Vì sao mapper là hàm thuần** — Hàm thuần thì test không cần `TestBed`, và không có chỗ nào để lén gọi HTTP.

- **Việc 1 — chuyển ngày** — Trên dây ngày là chuỗi. Nếu không chuyển ở mapper, mỗi template sẽ tự chuyển theo một kiểu và ít nhất một chỗ sẽ quên múi giờ.

- **Việc 2 — rút hình dạng dây** — `roles` trên dây là mảng object mang `id` và `isSystem` cho màn gán vai trò; màn danh sách chỉ cần tên. Template không nên biết hình dạng lồng của dây.

### 4.3 Cổng

Hai chi tiết của mẫu này đã được kiểm chứng ở dự án tiền nhiệm, đừng "đơn giản hoá" ngược lại:

- Mẫu phải là `Dto\b`, **không** phải `\bDto\b`. Tên DTO thật luôn dạng `NguoiDungDto` — giữa `g` và `D` không có ranh giới từ, nên mẫu có `\b` ở đầu **không bao giờ khớp**, và cổng xanh vì mù chứ không vì sạch.
- Loại trừ `*.spec.ts` là **phạm vi đúng** của luật, không phải ngoại lệ nới tay: luật bảo vệ đường code chạy thật, còn test dựng stub tầng HTTP thì bắt buộc phải nói bằng hình dạng của dây.

## 5. CRUD và phân trang dùng chung — bằng HÀM, không base class

- **Vì sao không có base class** — Một `BaseService<T>` luôn tiến hoá thành nơi chứa mọi thứ ai đó thấy "chung chung", và kế thừa làm một thay đổi ở Core vỡ mọi service nghiệp vụ cùng lúc.

### 5.1 Hình dạng phân trang phía FE

- **Điều 1 — lệch tên `page`** — Lệch tên thì BE không thấy tham số, **áp mặc định**, và người dùng bấm trang 7 luôn nhận trang 1 — không lỗi, không cảnh báo.

- **Điều 2 — `totalPages`** — Gửi qua dây là hai nguồn sự thật cho một con số, và bản cũ sẽ sống sót sau khi `pageSize` đổi.

- **Điều 3 — hàm đổi tên** — mỗi bước đổi tên là một chỗ để một tham số lệch tên đi ra khỏi trình duyệt.

### 5.2 Hàm dùng chung, và cơ chế giữ luật F10

- **Mapper là tham số bắt buộc** — Quên mapper là **lỗi biên dịch**, sớm hơn và rõ hơn một lỗi phạm vi truy cập.

- **Vì sao hàm dùng chung không bắt lỗi** — Một lớp bắt lỗi ẩn trong hàm dùng chung nghĩa là mỗi service đều có xử lý lỗi vô hình mà người đọc service không thấy.

> **Vì sao không ép endpoint ngoài khuôn vào hàm chung** — Ép mọi thứ vào một hàm dùng chung bằng cách thêm tham số điều kiện là cách hàm đó biến thành thứ khó đọc hơn code nó thay thế — đúng chế độ hỏng mà việc bỏ base class tránh được.

- **Vì sao `context` lại là tham số của hàm chung** — Nó không rẽ nhánh gì trong hàm, chỉ chuyền nguyên cho `HttpClient`, nên không phải loại tham số điều kiện ở trên. Thiếu nó thì mọi lần đọc cần tắt toast phải tự viết lại `theoId` trong service — đã xảy ra ở ba phương thức — và ở bản chép tay đó không còn gì buộc đi qua mapper (chốt 2026-09-25).

## 6. Hủy request, retry, timeout

—

### 6.1 Hủy — mặc định, không phải tính năng thêm

- **Vì sao trang không chờ ngừng gõ lần hai** — chờ hai lớp là cộng dồn độ trễ mà không ai thấy nguồn.

```typescript
// Ô gợi ý vai trò — phần liên quan của page; decorator, import và template lược bớt.
// Endpoint nhận searchText: contracts/roles.md §1.

/** Một trang đầu, bằng mặc định của hợp đồng — contracts/README.md §8. */
const KICH_THUOC_GOI_Y = 20;

export class GanVaiTroDialog {
  private readonly vaiTro = inject(VaiTroService);
  private readonly tuKhoa = new Subject<string>();

  protected readonly goiY = signal<readonly Option[]>([]);
  protected readonly dangTaiGoiY = signal(false);
  protected readonly loiGoiY = signal(false);

  constructor() {
    this.tuKhoa
      .pipe(
        // Request mới huỷ request cũ: kết quả của từ khoá cũ về muộn không ghi đè được kết quả mới.
        switchMap((searchText) =>
          this.vaiTro.danhSach({ page: 1, pageSize: KICH_THUOC_GOI_Y, searchText }).pipe(
            tap({
              subscribe: () => {
                this.dangTaiGoiY.set(true);
                this.loiGoiY.set(false);
              },
              next: (trang) => this.goiY.set(trang.items.map((v) => ({ key: v.id, label: v.name }))),
              finalize: () => this.dangTaiGoiY.set(false),
            }),
            // errorInterceptor đã hiển thị lỗi; ở đây chỉ bật cờ để Autocomplete hiện errorTemplate của màn.
            catchError(() => {
              this.loiGoiY.set(true);
              return EMPTY;
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe();
  }

  /** Nối vào output `search` của Autocomplete; `goiY()`, `dangTaiGoiY()` đi vào `options`, `loading`. */
  protected timVaiTro(searchText: string): void {
    this.tuKhoa.next(searchText);
  }
}
```

`VaiTroService.danhSach` theo đúng khuôn service ở [`fe-api-client.md`](../../../quy-uoc/fe-api-client.md) §5.2. Lỗi đi theo đúng khuôn của `ListStateStore` ([`fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §2.8): `catchError` **trong** `switchMap` đổi trạng thái rồi trả `EMPTY`. Để lỗi chạy ra ngoài `switchMap` thì luồng `tuKhoa` chết sau lỗi đầu tiên, và ô gợi ý im lặng không gọi máy chủ nữa cho tới khi tải lại trang.

- **`switchMap` chứ không `mergeMap`** — Với `mergeMap`, gõ năm ký tự sinh năm request và kết quả hiển thị là **request nào về sau cùng**, không phải request của từ khoá cuối. Đó là lỗi hiện ra như "kết quả tìm kiếm sai ngẫu nhiên" và gần như không tái hiện được.

### 6.2 Retry — hẹp, có điều kiện

- **Vì sao không retry request ghi** — request ghi có thể đã tới server và thành công trước khi kết nối đứt, và lần retry sẽ tạo bản ghi thứ hai.

- **Vì sao retry ở service, không ở interceptor** — Ở interceptor thì mọi request đều retry, gồm cả những request mà retry là sai.

### 6.3 Timeout

Không có timeout, một request treo sẽ giữ chỉ báo tải quay mãi và người dùng không biết nên chờ hay tải lại trang.

### 6.4 Tải tệp xuất về máy — blob qua `HttpClient`

- **Vì sao không `<a href>` hay `window.open`** — cookie `SameSite=Lax` **vẫn đi theo** một điều hướng cấp cao nhất xuyên site, và trình duyệt không gắn `Origin` cho điều hướng `GET`. Một trang lạ đặt liên kết tới URL export là đủ để server xuất dữ liệu dưới tên người bấm và ghi nhật ký kiểm toán mang tên họ. Token trong header là thứ duy nhất điều hướng không mang được — đó là toàn bộ lý do BE đòi nó ([`../../../adr/0062-endpoint-xuat-kiem-token-chong-gia-mao-nhu-lenh-ghi.md`](../../../adr/0062-endpoint-xuat-kiem-token-chong-gia-mao-nhu-lenh-ghi.md)), và vì thế FE **phải** tải bằng `HttpClient`. Cái giá: mất thanh tiến trình của trình duyệt, tệp đi qua bộ nhớ.

- **Vì sao đánh dấu request thay vì gắn token cho mọi `GET`** — gắn cho mọi `GET` cũng vô hại về bảo mật (token chỉ tới API), nhưng `errorInterceptor` sẽ không biết `GET` nào được phép gửi lại sau `CSRF_REJECTED`: gửi lại mọi `GET` là vòng lặp với chính `GET` phát token (§2.2), không gửi lại thì export đổ vào toast *token sai* mỗi lần token hết hạn giữa phiên. Dấu tường minh phía FE soi đúng attribute tường minh phía BE — hai bên cùng khai ngoại lệ bằng metadata, và một cổng tương lai so được hai tập.

- **Bẫy `Content-Disposition` qua CORS** — cùng bẫy với `Retry-After` ở §2.2: API ở origin khác, header không nằm trong `Access-Control-Expose-Headers` thì `headers.get('Content-Disposition')` trả `null`, không lỗi, không cảnh báo. Đặt tên dự phòng là giấu đúng lỗi này; ném lỗi thì nó lộ ra ở lần dùng đầu tiên trên máy dev.

- **Đọc `filename*` trước `filename`** — tên tài nguyên có dấu tiếng Việt đi ở `filename*=UTF-8''…` (percent-encoding); `filename=` chỉ mang bản ASCII đã bỏ dấu cho client cũ. Đọc nhầm thứ tự thì tệp tên không dấu, đúng cú pháp, sai câu chữ — không ai báo.

- **Bẫy thân lỗi là `Blob`** — với `responseType: 'blob'`, Angular giao **mọi** thân phản hồi dưới dạng `Blob`, kể cả thân của lỗi 4xx/5xx, dù server gửi `application/json`. `docEnvelopeLoi` kiểm `'success' in body` trên một `Blob` → `false` → `null` → toast *mất kết nối* cho một lỗi 422 `CORE.EXPORT.TOO_MANY_ROWS` mà thông điệp của nó nói rõ phải lọc hẹp thêm bao nhiêu. Bóc ở interceptor: `Blob.text()` là bất đồng bộ, nên nhánh này thành một `from(...).pipe(switchMap(...))` như nhánh CSRF — bóc ở từng service là chép lại đúng đoạn đó ở mọi export.

- **Vì sao một hàm dùng chung kích hoạt tải** — `URL.createObjectURL` không tự thu hồi; quên `revokeObjectURL` là rò bộ nhớ đúng bằng cỡ tệp, mỗi lần bấm. Một hàm ở `core/http/` làm đúng một lần; export của module chỉ gọi.

## 7. Khi endpoint chưa tồn tại

Card sai vẫn tốt hơn giả định ngầm: card sai thì có một chỗ để sửa và một người phản đối; giả định ngầm thì chỉ lộ ra lúc ghép nối, khi cả hai bên đều đã viết xong.

- **Vì sao không stub bằng dữ liệu cứng trong service** — Dữ liệu cứng đó sẽ ở lại sau khi endpoint có thật.

## 8. Secret — không có, không bao giờ

—

## 9. Đối chiếu luật

—
