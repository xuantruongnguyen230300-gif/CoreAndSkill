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
import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, map, tap } from 'rxjs';
import type { ApiResult } from '../http/api-result.model';
import { unwrapData } from '../http/unwrap';
```

### 2.2 `errorInterceptor` — nơi DUY NHẤT dịch lỗi

- **Vì sao `inject()` gọi ở thân hàm interceptor** — Thân hàm là injection context; callback của `catchError` thì không, vì nó chạy ở tick khác.

- **Nhánh CSRF — vì sao chỉ gửi lại một lần, và vì sao `ORIGIN_REJECTED` không vào nhánh này** — Token mới vẫn bị từ chối nghĩa là lỗi thật, rơi xuống nhánh toast. `Origin` do trình duyệt đặt nên lấy token mới rồi gửi lại vẫn nhận đúng mã đó — gửi lại vô nghĩa.

- **Thiếu đường lùi ở `dichLoi`** — `TranslateService.instant` trả về **chính khoá** khi không tìm thấy bản dịch; so sánh kết quả với khoá là cách duy nhất phát hiện điều đó. Không có nhánh này, một mã lỗi mới do BE thêm sẽ hiện ra màn hình dưới dạng `loi.CORE.USER.SOMETHING` — vừa vô nghĩa với người dùng, vừa rò cấu trúc mã lỗi nội bộ.

- **Khoá dịch dựng từ trường sai** — Đọc nhầm chỗ cho ra `undefined`, khoá thành `loi.undefined`, và người dùng nhận một toast trống — không lỗi, không log, không dấu vết.

- **403 `FORBIDDEN` — vì sao làm mới cả menu, vì sao một lời gọi** — Đổi vai trò hay ma trận quyền có hiệu lực từ request kế tiếp ([`../contracts/users.md`](../../../contracts/users.md) §7), nên mã này giữa phiên thường nghĩa là tập quyền FE đang giữ đã cũ: nút vẫn hiện cho thao tác người dùng không còn quyền làm. — menu do server lọc theo quyền ([`../contracts/meta-menu.md`](../../../contracts/meta-menu.md)), nên thay quyền mà giữ menu cũ là sidebar vẫn mời vào màn vừa mất quyền. Directive và guard đọc cùng signal nên tự cập nhật. Nhiều 403 cùng lúc chỉ sinh một lời gọi. Nhánh CSRF ở trên chạy trước và tự thử lại, nên token hết hạn không kéo theo lời gọi làm mới.

- **403 mang mã nghiệp vụ** — Một card có thể trả 403 với mã riêng — mã đó nói về **dữ liệu** của thao tác, không nói tập quyền đã cũ, nên câu "không có quyền" chung sai nghĩa và lời gọi làm mới là thừa. Hệ quả phải nhớ: màn gọi một endpoint có 403 nghiệp vụ trong card mà **không** xử lý mã đó thì thao tác hỏng im lặng — không toast, không dòng lỗi.

- **403 `PASSWORD_CHANGE_REQUIRED` — vì sao không toast, cơ chế** — Mã này giữa phiên nghĩa là cờ buộc đổi mật khẩu đang bật mà `AuthService` chưa biết. Không toast vì đổi màn đã là thông điệp, cùng lý do với nhánh 401. Interceptor giữ nguyên luật không điều hướng khi gặp 403 ([`fe-routing-guard.md`](../../../quy-uoc/fe-routing-guard.md) §8); khi `me` về, `AuthService` cho router chạy lại guard của URL hiện tại và `mustChangePasswordGuard` trả đích.

- **429 — vì sao ở interceptor, vì sao header thắng envelope** — Giới hạn tần suất áp cho mọi endpoint, không riêng đăng nhập, nên xử lý nằm ở interceptor. Header thắng tham số trong envelope vì header là con số limiter đặt cho **đúng** response này.

- **Bẫy `Retry-After` qua CORS** — 🪤 API ở origin khác, nên trình duyệt chỉ cho JavaScript đọc `Retry-After` khi BE khai nó trong `Access-Control-Expose-Headers`; thiếu khai báo thì `headers.get` trả `null` — không lỗi, không cảnh báo — và câu dịch rơi về tham số trong envelope. Kiểm bằng tab Network **và** một test đọc header thật, không kiểm bằng mắt câu toast.

- **409 `CONFLICT` — vì sao không gửi lại, không tải lại hộ; ca hiện trong trang; mã 409 khác** — Mã dùng chung này ([`../contracts/auth.md`](../../../contracts/auth.md) §11) nghĩa là người khác đã ghi bản ghi sau khi màn đọc nó; mô hình, lý do không tự thử lại, và ba câu mà thông điệp phải trả lời ở [`../wiki-core/be/06-concurrency-control.md`](../../../wiki-core/be/06-concurrency-control.md) §6. Interceptor **không** gửi lại — gửi lại là ghi đè thay đổi của người kia, đúng thứ cơ chế này tồn tại để chặn — và **không** tải lại dữ liệu hộ: thứ người dùng đang nhập chưa mất, và chỉ màn biết giữ nó thế nào. Màn muốn hiện xung đột ngay trong trang thay vì toast — ma trận phân quyền với token cấp tập ([`../Design/Screens/12-ma-tran-phan-quyen.md`](../../../Design/Screens/12-ma-tran-phan-quyen.md)) — tắt toast bằng `BO_QUA_TOAST_LOI` như mọi lỗi khác. Mã 409 khác (`*_DUPLICATED`, `*_DUPLICATE`) không vào nhánh này: chúng nói về dữ liệu vừa gửi, tải lại không đổi được gì, nên đi theo nhánh toast chung.

**Vì sao lỗi có `fieldErrors` không bắn toast:** người dùng đang nhìn cái form. Lỗi hiện ngay dưới ô nhập là thông tin; cùng lỗi đó bay lên góc màn hình dưới dạng toast là tiếng ồn, và tệ hơn là nó biến mất trước khi người ta đọc xong.

- **Vì sao không dùng cờ toàn cục** — Cờ toàn cục nghĩa là hai request chạy song song giẫm lên nhau, và không có gì báo.

Khối import của `errorInterceptor` ([`fe-api-client.md`](../../../quy-uoc/fe-api-client.md) §2.2):

```typescript
// core/interceptors/error.interceptor.ts — phần import
import { HttpContextToken, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { SessionExpiryHandler } from '../auth/session-expiry.handler';
import { XsrfTokenStore } from '../auth/xsrf-token.store';
import {
  type ApiFailure, ApiFailureError, BO_QUA_HET_PHIEN, BO_QUA_TOAST_LOI, type MessageParams, docEnvelopeLoi,
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

Khối import của `core/http/crud.ts` ([`fe-api-client.md`](../../../quy-uoc/fe-api-client.md) §5.2):

```typescript
// core/http/crud.ts — phần import
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import type { ApiResult } from './api-result.model';
import type { PageQuery, PagedList } from './paged.model';
import { unwrapData } from './unwrap';
```

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

## 7. Khi endpoint chưa tồn tại

Card sai vẫn tốt hơn giả định ngầm: card sai thì có một chỗ để sửa và một người phản đối; giả định ngầm thì chỉ lộ ra lúc ghép nối, khi cả hai bên đều đã viết xong.

- **Vì sao không stub bằng dữ liệu cứng trong service** — Dữ liệu cứng đó sẽ ở lại sau khi endpoint có thật.

## 8. Secret — không có, không bao giờ

—

## 9. Đối chiếu luật

—
