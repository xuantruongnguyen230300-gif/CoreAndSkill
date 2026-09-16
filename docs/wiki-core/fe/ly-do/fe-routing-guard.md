---
kind: tham-chieu
scope: core
verified: chua-doi-chieu
---

# Lý do, bẫy, ví dụ mở rộng — `fe-routing-guard.md`

> File này giữ **lý do, bẫy, ví dụ dài** của [`fe-routing-guard.md`](../../../quy-uoc/fe-routing-guard.md); luật ở đó. Số mục dưới đây trùng số mục của file luật; mục không có gì dời ghi "—".

---

## 1. Bản đồ route

- **Trang lỗi trong khung — được gì, mất gì** — Đã đăng nhập thì `Sidebar` và `Topbar` còn nguyên, nên người dùng luôn có đường đi tiếp mà trang lỗi không phải tự cấp. Cái giá chấp nhận: người chưa đăng nhập không bao giờ thấy trang 404.

> **Đoạn URL ≠ khoá phân quyền** — Hai thứ này
> trông giống nhau ở sơ đồ trên nhưng thuộc hai hệ khác nhau: đoạn URL là chuỗi người dùng
> nhìn thấy, còn khoá phân quyền là **dữ liệu trong database** mà BE so khớp từng ký tự. Một
> khoá dựng theo đoạn URL — kiểu `core.nguoi-dung.xem` — **không tồn tại** ở phía BE. Guard
> theo khoá như vậy thì FE ẩn sạch mọi mục menu, kể cả với tài khoản đủ quyền, và không có
> lỗi nào bật ra.

### 1.1 Quy ước đặt tên đoạn URL

URL là thứ người dùng nhìn thấy, gửi cho nhau, và đánh dấu lại. Đổi URL là breaking change với người dùng — cân nhắc như đổi hợp đồng API.

## 2. Lazy-load theo tầng

—

### 2.1 `app.routes.ts` chỉ khai `loadChildren`

**Vì sao nhánh xác thực khai TRƯỚC nhánh khung app.** Nhánh khung kết thúc bằng `'**'` — khớp mọi URL — nên đặt nhánh xác thực sau thì `/dang-nhap` rơi vào 404 sau `authGuard`, và `authGuard` lại đẩy về `/dang-nhap`: vòng lặp điều hướng. Cùng lý do, `xac-thuc.routes.ts` không khai `path: ''` hay `'**'`. Trong nhánh khung, `'**'` phải là dòng cuối: đứng trước route nào thì nuốt route đó.

**Nhánh xác thực đứng đầu nên router nạp chunk của nó ở lần điều hướng đầu tiên**, kể cả khi đích là một màn trong khung: nó phải nạp route con để biết URL có khớp không. Chunk đó chỉ chứa hai màn xác thực — đó là cái giá của việc đặt 404 trong khung.

- **Import tĩnh thì sao** — Một `import` tĩnh kéo cả feature vào bundle khởi động, và triệu chứng duy nhất là bundle to dần — không lỗi, không cảnh báo, không ai để ý cho tới lúc ngân sách bundle (luật F14) đỏ.

Hai nhánh `platform/` còn lại trong khung, cùng khuôn `loadChildren` với `trang-chu` ([`fe-routing-guard.md`](../../../quy-uoc/fe-routing-guard.md) §2.1):

```typescript
      {
        path: 'quan-tri',
        loadChildren: () => import('./platform/quan-tri/quan-tri.routes').then((m) => m.QUAN_TRI_ROUTES),
      },
      {
        path: 'he-thong',
        loadChildren: () => import('./platform/he-thong/he-thong.routes').then((m) => m.HE_THONG_ROUTES),
      },
```

### 2.2 Mỗi feature một `<feature>.routes.ts`

- **Vì sao guard đặt ở route của feature** — Lý do là câu hỏi *"màn này ai vào được"* phải trả lời được bằng cách mở đúng một file — file của feature. Đặt guard ở cấp cha thì thêm một màn hình mới sẽ thừa hưởng guard mà người viết không hề biết, và tệ hơn là thừa hưởng **thiếu** guard.

- **Vì sao `title` giữ khoá** — nếu để câu thẳng ở đây thì tiêu đề tab là chỗ duy nhất trong app không đổi theo ngôn ngữ, và luật F8 không quét file `.ts` nên không có gì bắt.

- **Vì sao một `TitleStrategy` ở `core/i18n/` thay vì mỗi page tự gọi `Title.setTitle()`** — Router đã gom `title` của route; một chiến lược là một chỗ dịch và một chỗ nối hậu tố thương hiệu. Page tự đặt thì có page quên, và page quên hiện tiêu đề của page trước. Hậu tố lấy từ `CORE_BRANDING.name` để `core/` không biết tên sản phẩm ([`fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §2.5).

- **Vì sao `CoreTitleStrategy` dịch lại khi đổi ngôn ngữ** — Router chỉ gọi `updateTitle` lúc điều hướng, nên không có bước này thì tab giữ tiêu đề ngôn ngữ cũ tới lần điều hướng kế — và tab là thứ người dùng nhìn khi chuyển qua lại giữa nhiều tab.

Route chi tiết trong `QUAN_TRI_ROUTES` ([`fe-routing-guard.md`](../../../quy-uoc/fe-routing-guard.md) §2.2), cùng khuôn với route danh sách:

```typescript
  {
    path: 'nguoi-dung/:id',
    canActivate: [permissionGuard('core.user.read')],
    loadComponent: () =>
      import('./nguoi-dung/pages/chi-tiet/chi-tiet-nguoi-dung.page').then((m) => m.ChiTietNguoiDungPage),
    title: 'nguoiDung.chiTiet',
  },
```

### 2.3 Route con và `noShell`

Cách làm sai thường gặp là để chúng trong khung rồi ẩn sidebar bằng CSS. Sai vì: khung vẫn dựng, vẫn gọi API menu, và người chưa đăng nhập vẫn phát sinh một request 401 mỗi lần mở màn đăng nhập.

Không gác màn đổi mật khẩu bắt buộc thì nó **vào được bất cứ lúc nào** — người đã đổi xong vẫn mở lại được một màn nói rằng họ phải đổi, và bấm Quay lại sau khi đổi xong là quay đúng vào đó.

## 3. Guard theo permission, KHÔNG theo role

—

### 3.1 Quyết định

Vì sao, nói bằng hệ quả cụ thể: kiểm theo tên role nghĩa là mỗi lần khách hàng muốn "cho nhóm trưởng phòng xem được màn này", đội phải sửa code, build lại và triển khai lại. Kiểm theo permission thì đó là một thao tác cấu hình trong màn phân quyền. Chi phí ban đầu cao hơn — phải khai một danh mục permission và một màn gán quyền — và đó là toàn bộ cái giá.

Ở dự án tiền nhiệm có ba tên role khai cứng trong mã nguồn. Hệ quả không phải là code xấu, mà là **Core không mang đi được**: sản phẩm tiếp theo có cơ cấu tổ chức khác sẽ thừa kế ba cái tên vô nghĩa với nó, và mọi câu lệnh kiểm tra dựa trên chúng.

### 3.2 `authGuard`

- **Vì sao trả `UrlTree`** — `UrlTree` để router hủy điều hướng cũ và chuyển sang cái mới trong **một** chu kỳ; cách kia tạo hai lần điều hướng chồng nhau, và trong vài trường hợp lịch sử trình duyệt còn giữ lại trang bị chặn — bấm Back là quay về đúng chỗ vừa bị cấm.

- **Vì sao kiểm `returnUrl`** — Không kiểm thì đó là một lỗ chuyển hướng ra ngoài — kẻ tấn công gửi một liên kết đăng nhập kèm `returnUrl` trỏ sang site của họ.

### 3.3 `permissionGuard`

- **Vì sao Topbar đọc tên từ phiên** — hai nguồn cho một cái tên thì có lúc lệch, và lệch ở chỗ người dùng nhìn thấy đầu tiên sau khi bấm Lưu. Cùng lý do, không màn nào gọi hồ sơ chỉ để lấy `tenantName`, `preferredLanguage` hay `isSystemOperator` — chúng đã ở trong phiên.

- **Vì sao `lamMoiPhien()` không dùng chung lời gọi `me` đang chạy** — Lời gọi đó có thể đã rời trình duyệt trước khi hồ sơ lưu xong và mang về tên cũ; huỷ nó rồi gửi lại.

- **Vì sao `lamMoiPhien()` trả `Promise<void>`** — Màn đổi mật khẩu bắt buộc điều hướng ngay sau khi làm mới phiên; điều hướng khi `me` chưa về là guard đọc cờ cũ và đẩy người vừa đổi xong quay lại chính màn đó. Promise xong cả khi lời gọi lỗi hay bị huỷ, để nơi chờ không treo.

- **Vì sao `goiLaiMe()` không xử lý lỗi** — Lỗi của lời gọi `me` đã đi qua `errorInterceptor` như mọi request: toast, hoặc đường hết phiên nếu là 401.

- **Vì sao `canNapLaiMenu` là cờ riêng, sống qua lời gọi bị huỷ** — Một 403 đến trong lúc `lamMoiPhien()` đang chạy vẫn được nạp lại menu khi `me` về.

- **Vì sao `MenuStore` không inject `AuthService`** — Hai service gốc inject lẫn nhau là vòng DI; Angular ném `NG0200`.

- **Vì sao nạp lại menu cùng bước** — Menu do server lọc theo quyền ([`../contracts/meta-menu.md`](../../../contracts/meta-menu.md)); thay tập quyền mà giữ menu cũ thì directive đã ẩn nút, còn sidebar vẫn mời vào màn vừa mất quyền.

- **Vì sao `Set`** — Một màn hình danh sách có thể hỏi quyền cho từng dòng; với mảng thì đó là phép quét tuyến tính nhân với số dòng, chạy lại mỗi lần đổi phát hiện.

- **Vì sao về màn 403** — Đưa về trang chủ khiến người dùng nghĩ mình bấm nhầm, thử lại, rồi lại bị đưa về — vòng lặp không có thông tin. Màn 403 nói rõ "không có quyền" và cho một đường đi tiếp.

Khối import của `AuthService` ([`fe-routing-guard.md`](../../../quy-uoc/fe-routing-guard.md) §3.3):

```typescript
// core/auth/auth.service.ts — phần import
import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription, finalize, map } from 'rxjs';
import type { ApiResult } from '../http/api-result.model';
import { sangNguoiDungHienTai } from '../http/nguoi-dung-hien-tai.mapper';
import type { PhienDto } from '../http/phien.dto';
import { unwrapData } from '../http/unwrap';
import { MenuStore } from '../menu/menu.store';
import type { NguoiDungHienTai } from './nguoi-dung-hien-tai.model';
```

### 3.4 Directive `appHasPermission`

- **Khoá tự chế thì sao** — Khoá tự chế không gây lỗi: `coQuyen()` trả `false` và nút biến mất với **mọi** người, kể cả tài khoản đủ quyền — deny-by-default làm chuỗi sai trông y hệt chuỗi đúng của một người thiếu quyền.

- **Ẩn nút là gì** — Nó là phép lịch sự với người dùng — đừng bày ra thứ họ bấm vào sẽ nhận lỗi. Người biết dùng công cụ phát triển vẫn gọi được API. Vì vậy trường không được phép xem thì BE không gửi — không phải FE ẩn nó đi.

- **Cú pháp `*` và luật F9** — F9 cấm `*ngIf`/`*ngFor`/`*ngSwitch`, không cấm directive cấu trúc của app; cổng quét đúng tên ba chỉ thị cũ nên `*appHasPermission` không bị báo nhầm.

### 3.5 `systemOperatorGuard` — gác bằng CỜ, không bằng ma trận quyền

Cùng khuôn với hai guard trên và cùng lý do: trả `UrlTree` chứ không `navigate()` rồi `return false` ([`fe-routing-guard.md`](../../../quy-uoc/fe-routing-guard.md) §3.2), và đích là màn 403 chứ không phải trang chủ ([`fe-routing-guard.md`](../../../quy-uoc/fe-routing-guard.md) §3.3).

- **Vì sao không có directive cho cờ** — Thêm một directive kiểu `appHasPermission` cho cờ này là dựng một đường gác thứ hai cho thứ đã có một đường. Vào được màn là dùng được mọi thao tác trên màn. Cờ không nằm trong `permissions` nên cũng không có khoá quyền nào để truyền vào `permissionGuard`.

- **Cờ tắt hiện ở menu, không ở guard** — Menu do máy chủ lọc ([`../../../contracts/meta-menu.md`](../../../contracts/meta-menu.md)) nên tài khoản thường không thấy mục `/he-thong`; guard bắt người gõ thẳng URL.

### 3.6 `AuthService` — khởi động và thiết lập phiên

- **Vì sao hai `GET` chạy song song** — Token chống giả mạo và `me` không phụ thuộc nhau: `me` là `GET`, không cần token. Chạy tuần tự là cộng hai độ trễ mạng vào thời gian trắng màn hình trước khung hình đầu tiên, không đổi lại gì ([`fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §2.6: bước độc lập thì chạy song song).

- **Vì sao `catchError` trong từng nhánh, không bọc ngoài `forkJoin`** — `forkJoin` huỷ mọi nhánh còn lại ngay khi một nhánh lỗi. `me` trả 401 là chuyện bình thường của người chưa đăng nhập; không bắt ngay trong nhánh thì 401 đó huỷ luôn lời gọi token, và thao tác ghi đầu tiên sau đăng nhập bị `CSRF_REJECTED`. Bọc ngoài chỉ cứu được app khỏi treo, không cứu được nhánh kia.

- **Vì sao đăng nhập xong không gọi `me`** — Response `login` đã là **cùng DTO** với `me` ([`../../../contracts/auth.md`](../../../contracts/auth.md) §5). Gọi thêm một lần là một request thừa trên đường tới màn đầu tiên, và là một cơ hội để hai nguồn cho cùng một phiên lệch nhau trong khoảng giữa hai response.

- **Vì sao `napPhienKhoiDong()` tách khỏi `goiLaiMe()`** — `goiLaiMe()` cố ý không chạy khi chưa đăng nhập (nó phục vụ *làm mới* một phiên đang có). Khởi động là lần duy nhất gọi `me` với phiên còn trống, và là lần duy nhất mang `BO_QUA_HET_PHIEN`; gộp hai đường vào một hàm là để cờ đó lọt sang các lần làm mới giữa phiên.

## 4. Thứ tự guard — không tuỳ tiện

- **Đảo thứ tự guard thì sao** — Đảo thứ tự 1 và 3 thì người chưa đăng nhập bị đưa tới màn 403 thay vì màn đăng nhập — một thông điệp sai và một đường cụt.

- **Vì sao thứ tự khai là luật** — Angular chạy `canActivate` theo thứ tự khai và dừng ở guard đầu tiên từ chối.

### 4.1 `unsavedChangesGuard` — hỏi trước khi mất dữ liệu

Đây là thứ dự án tiền nhiệm đã có và làm đúng — giữ lại.

- **Vì sao chỉ một bộ khoá i18n cho hộp hỏi** — Hộp hỏi là **một** hộp dùng chung cho mọi màn, nên câu chữ của nó cũng chỉ có một bộ khoá; mục khai khoá tồn tại để không màn nào dựng bộ khoá thứ hai cho cùng một hộp. Miền `chung.` vì câu này thật sự dùng ở nhiều miền. `severity` là `ask` vì rời trang là xác nhận thường, không phải thao tác phá huỷ.

## 5. Luồng bắt buộc đổi mật khẩu lần đầu

Đây là guard dễ quên nhất, và quên nó là một lỗ hổng thật: tài khoản do quản trị viên tạo có mật khẩu tạm, và nếu người dùng vào thẳng được màn hình khác thì mật khẩu tạm đó sống mãi.

### 5.1 Luồng

—

### 5.2 Guard

—

### 5.3 Bốn điều phải đúng, thiếu một là hỏng

- **Điều 1 — cờ từ FE thì sao** — FE tự nhớ "đã đổi rồi" thì tải lại trang là mất, hoặc tệ hơn: người dùng tự đặt lại được.

- **Điều 2 — áp lẻ tẻ thì sao** — Áp lẻ tẻ thì màn quên áp chính là đường vòng.

- **Điều 3 — không gác khi cờ tắt thì sao** — một màn "chỉ vào được khi BE yêu cầu" mà không ai gác thì chỉ còn là một câu mô tả. Không loại trừ chính màn đó khi cờ bật thì guard tự đẩy về chính nó và trình duyệt treo ở vòng lặp điều hướng.

- **Điều 4 — điều hướng trước khi làm mới thì sao** — Điều hướng trước khi làm mới thì guard đọc cờ cũ và đẩy người dùng quay lại màn vừa hoàn thành.

### 5.4 Cờ bật giữa phiên

- **Thiếu `runGuardsAndResolvers` thì sao, và cái giá** — thiếu nó, router không chạy lại guard của route không đổi tham số, và lần điều hướng ở bước 2 kết thúc mà không đi đâu — người dùng ở lại một màn mà mọi thao tác trả 403, không toast, không lời giải thích. Cái giá: `authGuard` và `mustChangePasswordGuard` chạy lại ở mọi lần điều hướng trong khung, kể cả lần chỉ đổi query param — hai lần đọc signal.

- **Vì sao đích do guard trả** — Một chỗ thứ hai tự điều hướng là bản sao thứ hai của luật đó, gồm cả điều kiện loại trừ chính màn đổi mật khẩu.

- **Vì sao cần đường riêng cho cờ bật giữa phiên** — Guard chỉ chạy lúc chuyển route, nên cờ bật sau khi phiên đã nạp thì guard không thấy.

## 6. `CORE_ROUTES` — `core/` giữ luật, app cấp đường dẫn

- **Vì sao token không có giá trị mặc định** — một đường dẫn mặc định biến "app quên khai" thành một điều hướng tới route không tồn tại, và triệu chứng là màn hình trắng lúc hết phiên — thời điểm khó chẩn đoán nhất.

## 7. State trên URL — bộ lọc thuộc về query param

—

### 7.1 Luật

Ba việc hỏng ngay khi trạng thái chỉ sống trong `signal()`:

| Người dùng làm gì | Trạng thái trong signal | Trạng thái trên URL |
| --- | --- | --- |
| Tải lại trang (F5) | Mất hết bộ lọc | Giữ nguyên |
| Gửi link cho đồng nghiệp | Người kia thấy màn hình khác | Thấy đúng cái đang thấy |
| Bấm Back sau khi mở chi tiết | Về danh sách đã reset | Về đúng trang, đúng bộ lọc |

Việc thứ hai là việc quan trọng nhất và ít được nghĩ tới nhất: "gửi cho tôi cái link đang lọc như thế" là thao tác người dùng làm hằng ngày, và không có nó thì họ mô tả bằng lời rồi người nhận dựng lại sai.

### 7.2 Khuôn

—

### 7.3 Luật của mẫu này

—

### 7.4 Cái gì KHÔNG lên URL

—

## 8. 401 và 403 — xử lý ở tầng nào

Đây là chỗ dễ làm hai lần hoặc không lần nào: 401 do interceptor xử lý nên guard không bắn toast cho nó; ngược lại interceptor không điều hướng cho 403.

**Vì sao 403 không điều hướng còn 401 thì có:** 401 nghĩa là *toàn bộ phiên* không dùng được nữa — ở lại màn hình hiện tại là vô nghĩa vì mọi request sau đều hỏng. 403 nghĩa là *một thao tác cụ thể* bị từ chối; phần còn lại của màn hình vẫn dùng được, và đá người dùng đi chỗ khác sẽ làm mất dữ liệu họ đang nhập. Tập quyền vừa làm mới áp ngay cho directive ẩn nút, còn guard áp nó ở lần điều hướng kế tiếp. `PASSWORD_CHANGE_REQUIRED` là 403 nói về *cả phiên* nên có dẫn tới đổi màn — nhưng bước đổi màn thuộc guard; interceptor chỉ làm mới phiên ([`fe-routing-guard.md`](../../../quy-uoc/fe-routing-guard.md) §5.4).

Vì sao không làm hộp thoại đăng nhập lại tại chỗ để giữ form: nó phải xử lý ca **người đăng nhập lại là tài khoản khác**, và một form do người A nhập bị gửi đi dưới danh tính người B là lỗi nặng hơn nhiều so với việc phải nhập lại. Giữ một đường xử lý duy nhất cho 401 cũng là thứ làm bảng trên còn đúng.

## 9. Checklist thêm một màn hình mới

—

## 10. Đối chiếu luật

—
