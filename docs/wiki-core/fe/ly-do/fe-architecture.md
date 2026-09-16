---
kind: tham-chieu
scope: core
verified: chua-doi-chieu
---

# Lý do, bẫy, ví dụ mở rộng — `fe-architecture.md`

> File này giữ **lý do, bẫy, ví dụ dài** của [`fe-architecture.md`](../../../quy-uoc/fe-architecture.md); luật ở đó. Số mục dưới đây trùng số mục của file luật; mục không có gì dời ghi "—".

---

## 1. Bốn tầng — và một chiều duy nhất

`modules/` là tầng duy nhất bị cấm import ngang, vì đó là chỗ nghiệp vụ của các domain khác nhau sống cạnh nhau. Hai màn hình Core dính nhau thì tệ nhất là khó tách; hai module nghiệp vụ dính nhau thì mất luôn khả năng bỏ một module ra khỏi sản phẩm — thứ mà cả mô hình modular monolith sinh ra để giữ.

### 1.1 Cây thư mục cấp `src/FE/` — cái gì nằm NGOÀI `src/app/`

- **Vì sao không dựng hai cơ chế cho một giá trị** — Hai nguồn cấu hình cho một giá trị là cách chắc chắn nhất để chúng lệch nhau, và chỗ lệch chỉ lộ ra ở môi trường mà không ai ngồi debug.

> **FE không có "dữ liệu runtime" theo nghĩa của BE.** Trình duyệt không ghi file lên đĩa server. Mọi thứ trong `src/FE/` đều là tài sản của source và đều vào git — trừ những gì [`repo-artifact.md`](../../../quy-uoc/repo-artifact.md) loại trừ.

## 2. Mỗi tầng chứa gì, cấm chứa gì

—

### 2.1 `core/` — hạ tầng toàn app

Service quản lý phiên đăng nhập vẫn có nghĩa khi chưa vẽ màn hình nào → `core/`. Service nhớ trạng thái đóng/mở của sidebar thì không → `shared/services/`.

Đây là chỗ đặt nhầm nhiều nhất, và đặt nhầm thì gãy luật F1 ngay: một service hạ tầng lỡ nằm ở `shared/` sẽ bị `core/` import ngược lên. Ở dự án tiền nhiệm đã xảy ra đúng ca này — interceptor trong `core/` import service toast từ `shared/services/`. Cách sửa là chuyển service xuống `core/`, giữ component hiển thị ở `shared/` và cho nó import ngược xuống `core/` (đúng chiều được phép).

### 2.2 `shared/` — dùng chung nhưng không phải hạ tầng

- **Vì sao `ui/` và `components/` là hai thư mục** — Trộn chúng lại thì lớp bọc dần mang logic và mất đúng tính chất khiến nó tồn tại.

- **Vì sao `ui/` không import `components/`** — Đi ngược chiều thì lớp bọc kéo theo component tự dựng, hai thư mục dính thành một khối, và việc tách chúng mất nghĩa.

> **Thư mục vắng mặt trong sơ đồ** — Hai thư mục `ui/` và `forms/` từng bị thiếu ở đây trong khi tài liệu khác yêu cầu chúng — mà cổng F5 khoá theo đường dẫn `shared/ui/**`: một cổng canh một đường dẫn không có trong sơ đồ là cổng **luôn xanh vì không gì rơi vào vùng nó canh**.

### 2.3 `platform/` — Màn hình Core

Đặt nhầm về phía `platform/` đắt hơn đặt nhầm về phía `modules/`: một màn nghiệp vụ lọt vào `platform/` sẽ đi theo nền tảng sang mọi dự án sau, mang theo cả khái niệm mà dự án đó không có. Khi phân vân, chọn `modules/` — nâng lên sau rẻ hơn gỡ xuống.

- **Vì sao trang chủ đi qua seam `CORE_HOME`** — Dự án dựng trên Core hầu như luôn muốn thay trang chào bằng một **bảng tổng hợp có số liệu**. Core không đoán hộ dự án cần số liệu gì — số liệu là nghiệp vụ của ngành — nên chỉ cấp cơ chế thay component, không cấp nội dung.

### 2.4 `modules/` — nghiệp vụ riêng dự án

—

### 2.5 Seam — `core/` giữ CƠ CHẾ, app cấp DỮ LIỆU

- **Vì sao token không có `factory` mặc định** — Một giá trị mặc định biến "app quên khai" thành một giao diện mang tên sản phẩm **khác** — sai ở chỗ dễ thấy nhất mà không lỗi nào, không test nào bắt. Thiếu provider thì Angular ném `NG0201` ngay lần dựng đầu, tức lỗi nổ đúng lúc và đúng chỗ.

**Bảng màu không phải seam trong TypeScript.** Preset ở `core/theme/` đọc `var(--color-*)`, nên màu của dự án đi vào qua file token toàn cục — không qua tham số hàm, không qua token DI nào. Cơ chế: [`../wiki-core/fe/04-design-token-system.md`](../../../wiki-core/fe/04-design-token-system.md) §7.

Khối import của `core/config/core-branding.ts` ([`fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §2.5):

```typescript
import { InjectionToken, EnvironmentProviders, makeEnvironmentProviders } from '@angular/core';
```

### 2.6 Composition root — nơi mọi seam được nối lại

- **Vì sao mặc định là `useExisting`** — Nhu cầu có hai thể hiện của một service hạ tầng gần như luôn là dấu hiệu của một nhầm lẫn.

- **Vì sao bước khởi tạo phải khai phụ thuộc** — Hai bước có phụ thuộc mà khai như thể độc lập là một lỗi chỉ lộ ra khi mạng chậm.

Ba thứ ở đây hỏng theo kiểu **không có thông báo lỗi nào**:

| Bẫy | Triệu chứng | Vì sao không ai thấy |
| --- | --- | --- |
| Khai một service **hai lần** dưới hai token (`useClass` cho token thứ hai thay vì `useExisting`) | Hai thể hiện tồn tại song song. Bên ghi trạng thái và bên đọc trạng thái là hai object khác nhau, nên giao diện phụ thuộc trạng thái đó **không bao giờ cập nhật** | Không lỗi, không cảnh báo. Cả hai thể hiện đều hợp lệ |
| **Thứ tự interceptor** khai sai | Xem [`fe-api-client.md`](../../../quy-uoc/fe-api-client.md) — thứ tự ở đó không tuỳ tiện | Request vẫn đi, chỉ đi qua sai trình tự |
| Bước khởi tạo chạy trước khi thứ nó phụ thuộc sẵn sàng | Lần tải trang đầu tiên hỏng, lần sau bình thường | Chỉ tái hiện được ở tải nguội |

### 2.7 Seam cho MÀN Core

Các seam khác ở [`fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §2.5 đều ở mức **toàn app**: tên sản phẩm, ngôn ngữ, base URL, trang chủ. Không seam nào chạm tới **một màn cụ thể**, và đó là một lỗ hổng thật:

> Một dự án hạ nguồn cần thêm cột *"Mã nhân viên"* vào danh sách người dùng của Core có đúng hai đường: **sửa file trong `platform/`** — vi phạm [`../adr/0016-phan-phoi-core-bang-clone.md`](../../../adr/0016-phan-phoi-core-bang-clone.md) luật #1 — hoặc **dựng một màn danh sách người dùng thứ hai** trong `modules/`. Cả hai đều là fork, chỉ khác hình thức.

Đường thứ hai nguy hiểm hơn vì nó **vô hình**: nó không sửa file nào của Core nên không cổng nào thấy, nhưng bản vá Core lần sau sẽ không bao giờ tới được màn đã nhân đôi.

**Vì sao ở `platform/config/`, không ở `core/config/`.** Cột, hành động dòng, trường lọc là kiểu công khai của thư viện UI, sống ở `shared/` — chủ của chúng là [`fe-ui-conventions.md`](../../../quy-uoc/fe-ui-conventions.md) §9. Token đặt ở `core/` thì phải import `shared/`, tức gãy luật F1 ngay dòng `import` đầu tiên. Người đọc token này là màn Core ở `platform/`, nên token ở cùng tầng với người đọc nó.

- **Luật 1 — chỉ THÊM, không BỚT** — Một dự án giấu cột "Trạng thái" khỏi danh sách người dùng là giấu một thông tin mà luồng khoá tài khoản của Core dựa vào — và lỗi đó chỉ lộ khi có người hỏi *"sao tài khoản này đăng nhập được"*. Cần bớt thì đó là dấu hiệu màn Core sai, và sửa ở Core.

- **Luật 2 — nối vào SAU** — Cho chọn vị trí nghĩa là Core phải giữ một thứ tự cột ổn định mãi mãi; nối sau thì Core thêm cột mới mà không phá bố cục của ai.

- **Luật 3 — khoá là mã màn** — Route đổi được qua `CORE_ROUTES`; mã màn thì không. Khoá theo route là khoá vào một thứ đã có seam riêng để đổi.

- **Vì sao phạm vi là màn danh sách, không có khoá cho màn hồ sơ hay màn form** — Cột, hành động dòng, trường lọc là ba thứ chỉ tồn tại ở màn danh sách. Muốn thêm một ô vào màn form là thêm một trường dữ liệu, tức đổi hợp đồng API và schema — việc đó không giải được bằng một seam giao diện, và một khoá cho màn form trong token chỉ hứa thứ seam không làm được.

**Cái giá, nói thẳng:** mỗi màn Core phải tự đọc token này và tự nối phần mở rộng vào — tức là một đoạn mã lặp ở mọi màn `platform/`. Nếu quên ở một màn thì màn đó **im lặng không mở rộng được**, và dự án hạ nguồn sẽ phát hiện bằng cách thấy cột mình khai không hiện ra. Đây là chỗ cần một mục kiểm khi có `src/`: mọi màn danh sách trong `platform/` phải đọc `CORE_SCREEN_EXT`.

Khối import của `platform/config/core-screen-ext.ts` ([`fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §2.7):

```typescript
import { InjectionToken, TemplateRef } from '@angular/core';
import { DataColumnDef, FilterField, UiMenuItem } from '../../shared/ui/types';
```

### 2.8 Trạng thái màn danh sách

- **Vì sao tầng này không nằm trong [`../Design/COMPONENTS.md`](../../../Design/COMPONENTS.md) §3** — Tầng này là smart (biết route); mọi component ở đó dumb, chỉ phát sự kiện; store nhận sự kiện và ghi lên URL.

Khối import của `GridQuery`, `ListStateStore` và `DanhSachNguoiDungPage` ([`fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §2.8):

```typescript
// core/list/grid-query.ts — phần import
import type { PageQuery } from '../http/paged.model';
```

```typescript
// core/list/list-state.store.ts — phần import
import { DestroyRef, Injectable, Injector, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, Params, Router } from '@angular/router';
import {
  EMPTY, Observable, Subject, catchError, combineLatest, debounceTime,
  distinctUntilChanged, map, switchMap, tap,
} from 'rxjs';
import type { PagedList } from '../http/paged.model';
import type { GridQuery } from './grid-query';
```

```typescript
// platform/quan-tri/nguoi-dung/pages/danh-sach/danh-sach-nguoi-dung.page.ts — phần import
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { ListStateStore } from '../../../../../core/list/list-state.store';
import { EmptyStateComponent } from '../../../../../shared/components/empty-state/empty-state.component';
import { SkeletonLoaderComponent } from '../../../../../shared/components/skeleton-loader/skeleton-loader.component';
import { ToolbarComponent } from '../../../../../shared/components/toolbar/toolbar.component';
import { DataTableComponent } from '../../../../../shared/ui/data-table/data-table.component';
import type { NguoiDung } from '../../models/nguoi-dung.model';
import { NguoiDungService } from '../../services/nguoi-dung.service';
```

Template của màn:

```html
<!-- Biến thể Toolbar chọn theo SỐ TRƯỜNG LỌC, không theo số bản ghi
     (../Design/Templates/ListScreen.md §3). Màn này có hai trường lọc — "Vai trò" và
     "Trạng thái" — nên là `full` với khu lọc nằm thẳng trong dải, không có panel. -->
<app-toolbar
  variant="full"
  [searchValue]="list.query().searchText ?? ''"
  (searchChanged)="list.setSearchText($event)"
>
  <!-- Khu lọc 1–2 trường vào qua slot nội dung, đặt ngay sau ô tìm (Toolbar.md §API). -->
</app-toolbar>
<!-- Phân trang vào ra qua API của DataTable: ở biến thể `paged`, lớp bọc vẽ dải phân trang
     ngay dưới khung bảng và màn KHÔNG đặt thêm một Pagination nào — DataTable.md, đoạn mở đầu. -->
<app-data-table
  rowKey="id"
  [rows]="list.rows()"
  [state]="list.state()"
  [totalRecords]="list.totalCount()"
  [page]="list.query().page"
  [pageSize]="list.query().pageSize"
  [sortBy]="list.query().sortBy ?? null"
  [sortDescending]="list.query().sortDescending ?? false"
  (pageChange)="list.setPage($event.page, $event.pageSize)"
  (sortChange)="list.setSort($event.sortBy, $event.sortDescending)"
  [emptyTemplate]="trong"
  [loadingTemplate]="dangTai"
  [errorTemplate]="loi"
/>

<!-- Context của emptyTemplate mang ca rỗng; màn KHÔNG đọc lại `state` để suy ra ca đó. -->
<ng-template #trong let-ca>
  @if (ca === 'empty-filtered') {
    <app-empty-state
      variant="no-results"
      size="compact"
      [title]="'nguoiDung.khongKetQua.tieuDe' | translate"
      [description]="'nguoiDung.khongKetQua.moTa' | translate"
    />
  } @else {
    <app-empty-state
      variant="first-use"
      size="compact"
      [title]="'nguoiDung.trong.tieuDe' | translate"
      [description]="'nguoiDung.trong.moTa' | translate"
    />
  }
</ng-template>

<ng-template #dangTai>
  <!-- Khuôn dạng hàng: tên preset theo SkeletonLoader.md -->
  <app-skeleton-loader variant="group" />
</ng-template>

<ng-template #loi>
  <app-empty-state
    variant="error"
    size="compact"
    [title]="'nguoiDung.loi.taiThatBai' | translate"
    [actionLabel]="'chung.thuLai' | translate"
    (actionClicked)="list.reload()"
  />
</ng-template>
```

- **Giải thích template của màn mẫu** — Mẫu lược bỏ `PageHeader`, cột, chip, hai ô lọc trong slot, và nút xoá lọc của khối không khớp. Ba slot trạng thái là input `TemplateRef` của `DataTable`: `DataTable` quyết **khi nào** hiện slot nào, màn quyết **hiện gì** — `EmptyState`, `SkeletonLoader` là component tự dựng mà lớp bọc không được import ([`fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §2.2). Nút "Thử lại" nằm trong `errorTemplate` và gọi thẳng `list.reload()`. **Khoá i18n trong mẫu là khoá thật của màn**, lấy từ bảng Câu chữ ở [`../Design/Screens/10-nguoi-dung.md`](../../../Design/Screens/10-nguoi-dung.md) — spec màn là nguồn của câu chữ và của khoá, mục này không đặt khoá riêng. API đầy đủ của từng component ở [`../Design/Components/Toolbar.md`](../../../Design/Components/Toolbar.md) và [`../Design/Components/DataTable.md`](../../../Design/Components/DataTable.md).

- **Vì sao không dựng dải phân trang thứ hai** — Đặt thêm một `app-pagination` ở màn là hai đường cùng phát một sự kiện đổi trang, và `setPage` bị gọi hai lần cho một cú bấm.

- **Luật 1 — URL là nguồn sự thật** — Mở lại một link phải ra đúng danh sách đó — kèm trang, sắp xếp, từ khoá và bộ lọc — vì người dùng bấm Quay lại sau khi xem một bản ghi và mong về đúng chỗ cũ.

- **Luật 2 — tên trên URL = tên trên dây** — một bước đổi tên là một chỗ để một tham số sai tên đi ra khỏi trình duyệt — BE không nhận ra nó, **áp mặc định**, và người dùng bấm trang 7 nhận về trang 1.

- **Luật 3 — đổi lọc thì về trang 1** — Không về 1 thì người dùng đang ở trang 7, lọc lại còn 2 trang, và thấy một danh sách rỗng — họ đọc đó là *"không có dữ liệu"*.

- **Luật 4 — vì sao cần huỷ request cũ** — Thiếu cơ chế thứ ba thì gõ nhanh sẽ thấy danh sách nhảy về kết quả của chuỗi đã gõ xong từ lâu — lỗi chỉ hiện khi mạng chậm. Kết quả về muộn không được ghi đè kết quả mới.

- **Luật 4 — vì sao `Toolbar` không chờ bên trong, và ngưỡng chờ là hằng số của store** — Thêm một lớp chờ bên trong `Toolbar` là cộng dồn hai độ trễ. Ngưỡng nằm ở store chứ không phải input của `Toolbar` hay tham số của `connect()` để mọi màn danh sách chờ cùng một khoảng — người dùng không phải học lại nhịp gõ ở mỗi màn.

- **Luật 5 — `replaceUrl`** — Sai chiều nào cũng khó chịu: một bên là bấm Quay lại mười lần mới thoát ô tìm, bên kia là mất khả năng quay lại trang trước.

- **Luật 6 — `state` là một biến** — Bốn cờ cho phép biểu diễn tổ hợp vô nghĩa; cùng lý do đã ghi ở [`../Design/Components/DataTable.md`](../../../Design/Components/DataTable.md).

- **Luật 7 — một thực thể cho một màn** — Cấp ở gốc app thì hai màn danh sách mở song song ghi đè truy vấn của nhau, và quay lại màn cũ thấy trạng thái của lần trước. Khai trên page thì store chết cùng màn.

- **Vì sao store không biết endpoint** — Biết endpoint thì store không dùng lại được cho màn thứ hai. Query param là dữ liệu người dùng gõ được — `?page=-5` sẽ tới — nên `soNguyenDuong` bỏ giá trị sai dạng.

## 3. Cấu trúc một feature

—

### 3.1 Bảng trách nhiệm — quy tắc cứng

**Vì sao `components/` bị cấm inject data service:** một component dumb có thể đặt ở bất kỳ đâu, test bằng cách truyền input, và tái dùng ở màn hình thứ hai mà không kéo theo gì. Ngay khi nó tự lấy dữ liệu, ba tính chất đó mất cùng lúc — và cách duy nhất để tái dùng nó ở màn khác là thêm tham số điều kiện, tức bắt đầu vòng xoáy god component.

Nếu thấy mình đang muốn inject service vào một component trong `components/`, đó là dấu hiệu nó nên là một page (smart), không phải component.

### 3.2 Khi nào cần `state/`

- **Vì sao không bọc store cho state một nơi dùng** — Bọc một store cho state chỉ một nơi dùng là thêm một lớp gián tiếp không đổi lấy gì.

## 4. Ranh giới ép bằng ESLint — mục quan trọng nhất của file này

—

### 4.1 Vì sao mục này dài hơn mọi mục khác

Phía BE, ranh giới tầng do **compiler** ép: `Core.Domain` không có `ProjectReference` tới `Core.Infrastructure` thì code trong Domain *không thể* gọi `DbContext`, dù người viết có muốn. Hàng rào đó chi phí bằng 0 và không bao giờ hỏng.

Phía FE, quyết định đã chốt là **giữ cấu trúc thư mục** trong một app Angular duy nhất, không tách Angular workspace nhiều library, không dùng Nx ([`../adr/0007-fe-giu-cau-truc-thu-muc.md`](../../../adr/0007-fe-giu-cau-truc-thu-muc.md)). Hệ quả trực tiếp: **không có compiler nào ép ranh giới tầng.** Một import trỏ ngược lên tầng trên vẫn biên dịch sạch sẽ.

Hàng rào duy nhất còn lại là ESLint, và **nó tắt được bằng một dòng comment** — vì vậy F1–F4 đi thành một bộ.

### 4.2 Zone `coreLayerZones` — `core/` là tầng đáy

> ⚠️ **Bẫy resolver.** `import/no-restricted-paths` phải phân giải specifier ra đường dẫn file thật mới so được với `target`/`from`. Quên khai `extensions` có `.ts` thì rule không nổ, không cảnh báo, và **trông y hệt như đang chạy sạch**. Khi thi công, viết một file canary vi phạm cố ý và xác nhận lint đỏ — luật T1 ở [`../RULES.md`](../../../RULES.md) áp cho cả detector phía FE.

### 4.3 Zone `moduleBoundaryZones` — module không import chéo

> ⚠️ **Cảnh báo từ dự án tiền nhiệm — bài học đắt nhất của mục này.**
>
> Ở dự án tiền nhiệm, mảng `BUSINESS_MODULES` để rỗng (đúng theo cách hiểu "chưa có module nghiệp vụ nào"), nên cả block rule bị bỏ hẳn theo đúng nhánh điều kiện ở trên. Hệ quả: **ranh giới module phía FE chưa từng được kiểm chứng một lần nào.** Luật vẫn nằm trong tài liệu, cấu hình vẫn có mặt, và mọi thứ trông như đang chạy.
>
> Đó không phải lỗi của người viết cấu hình — mảng rỗng là lựa chọn đúng khi chưa có module, vì zone trỏ vào thư mục không tồn tại thì cũng chẳng chặn được gì mà lại trông như đang chặn. Vấn đề là **không có gì nhắc khi module đầu tiên ra đời**. Luật F4 và [`fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §4.4 tồn tại để lấp đúng khoảng trống đó.

- **Vì sao khối rule phải bị bỏ hẳn khi mảng rỗng** — Schema của `import/no-restricted-paths` đòi `zones` có tối thiểu một phần tử. Truyền mảng rỗng làm ESLint chết ngay lúc nạp config ("Invalid Options") — toàn bộ lệnh lint đỏ vì một lý do không liên quan gì tới code đang sửa.

### 4.4 Quy trình thêm một module mới — bốn bước, không bỏ bước nào

- **Vì sao cổng F4 không nuốt lỗi** — Nuốt lỗi bằng `2>/dev/null` biến "chưa có gì để đối chiếu" thành "hai danh sách rỗng khớp nhau" — cổng xanh vì mù.

- **Vì sao mảng ở tệp riêng và không chép vào script cổng** — Tệp `.cjs` riêng để cổng `require` được mà không nạp cả cấu hình ESLint. Cổng đọc mảng từ **chính** tệp mà cấu hình ESLint dùng rồi so với thư mục thật. Hai bản sao thì bản không ai nhớ sẽ nói dối.

### 4.5 Cấm `eslint-disable` cho rule ranh giới — luật F3

Không có luật này thì F1 và F2 chỉ là gợi ý: bất kỳ ai gặp lint đỏ đều có thể viết một dòng comment và đi tiếp, và diff của dòng đó trông vô hại trong review.

> **Vì sao ánh xạ rule → luật phải đúng tên** — Ghi một rule chỉ "trông có vẻ liên quan" thì hai hệ quả cùng lúc: cổng cấm tắt một rule **ngoài** bộ ranh giới, và rule thật sự ép luật đó **không** có tên trong danh sách — nên đúng một dòng `eslint-disable` gỡ được nó, hợp lệ theo chính bảng này.

> **Vì sao không giữ bản lệnh thứ hai** — một bản chép tên rule vào lệnh là đúng thứ bảng trên sinh ra để thay thế: thêm một rule vào bảng mà lệnh không tự thấy thì cổng xanh vì mù.

- **Vì sao tên rule F17 phải chốt theo phiên bản angular-eslint cài thật** — Một tên lệch khỏi phiên bản đó thì cổng quét một chuỗi không bao giờ xuất hiện và xanh vì mù.

- **Vì sao F13 không cần dòng trong bảng** — `@for` thiếu `track` là lỗi biên dịch template; không có comment nào tắt được một lỗi biên dịch.

- **Vì sao quét cả ba dạng** — Chỉ quét một dạng thì hai dạng kia đi lọt, và cổng xanh vì mù chứ không vì sạch.

- **Vì sao quét cả dạng tắt toàn file** — Dạng này tắt *mọi* rule, gồm cả rule ranh giới, mà không chứa tên rule nào để grep — phải bắt bằng mẫu riêng.

- **Vì sao cổng phải có test của chính nó** — Một cổng hỏng âm thầm tệ hơn không có cổng: nó tạo cảm giác được bảo vệ. Xem [`../audit/2026-08-23-cong-khong-ton-tai.md`](../../../audit/2026-08-23-cong-khong-ton-tai.md).

Đánh đổi của F3 nói thẳng: nó **sẽ** gây khó chịu đúng vào lúc người ta đang vội. Đó là chủ đích. Một hàng rào chỉ có tác dụng khi việc vượt qua nó đắt hơn việc đi vòng.

### 4.6 Zone `sharedUiZones` — `shared/ui/` không import `shared/components/` — luật F24

- **Vì sao cần zone riêng** — F1 chỉ canh `core/`, nên một import từ `shared/ui/` sang `shared/components/` không chạm zone nào của F1 hay F2.

> **Bẫy flat config — khối sau thay khối trước** — Trong flat config, hai khối cùng khai `import/no-restricted-paths` cho một file thì tuỳ chọn của khối sau **thay** tuỳ chọn của khối trước, không cộng dồn: zone của khối trước biến mất và lint vẫn xanh. Các khối hiện có canh những cây rời nhau — `core/`, `modules/`, `shared/ui/` — nên không chồng lên nhau.

Canary theo bẫy resolver ở [`fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §4.2: tạm import một component từ `shared/components/` vào một file trong `shared/ui/`, thấy lint đỏ đúng thông điệp, rồi hoàn nguyên. Tắt rule này bằng comment bị F3 chặn — cùng tên rule với F1 và F2 ([`fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §4.5).

Khối config nối `sharedUiZones` vào file trong `shared/ui/` ([`fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §4.6):

```typescript
{
  files: ['src/app/shared/ui/**/*.ts'],
  plugins: { import: importPlugin },
  settings: {
    'import/resolver': { node: { extensions: ['.ts', '.js'] } },
  },
  rules: {
    'import/no-restricted-paths': ['error', { zones: sharedUiZones }],
  },
}
```

## 5. Ngưỡng kích thước file

Không có ngưỡng thì không có thời điểm nào để tách, và mọi file đều lớn dần cho tới lúc không ai đọc nổi. Ở dự án tiền nhiệm, một file style vượt một nghìn dòng và một page vượt năm trăm dòng — cả hai đều không phải do một lần viết ẩu, mà do hai mươi lần thêm "chỉ một chỗ nữa".

**Vì sao ngưỡng của `.scss` chặt nhất:** style là loại file duy nhất mà việc thêm dòng gần như không có chi phí nhận thức tại thời điểm thêm — không ai phải đọc lại 200 dòng trên để viết dòng 201. Đó chính là lý do nó phình nhanh nhất và là chỗ hex trần sinh sôi.

- **Vì sao không cắt đôi file rồi import lại** — Hai file gọi nhau mà không có ranh giới ý nghĩa thì tệ hơn một file dài.

- **Vì sao không áp cho `*.spec.ts`** — Test dài là bình thường; tách test theo số dòng là phản tác dụng.

## 6. So sánh với phương án đã loại

Ba phương án được cân nhắc cho ranh giới FE. Quyết định và lý do đầy đủ ở [`../adr/0007-fe-giu-cau-truc-thu-muc.md`](../../../adr/0007-fe-giu-cau-truc-thu-muc.md); bảng dưới là bản tóm để không phải mở ADR mỗi lần tranh luận.

| | **Thư mục + ESLint** (đã chọn) | Angular workspace nhiều library | Nx |
| --- | --- | --- | --- |
| Ranh giới ép bằng | ESLint `import/no-restricted-paths` | `tsconfig` path + build riêng từng library | Nx module boundary (cũng là ESLint) + project graph |
| Bỏ qua được bằng comment | **Có** — nên phải có F3 | Khó hơn: import sai đường thì library không build được | Có, cùng cơ chế ESLint |
| Chi phí dựng ban đầu | Gần như 0 | Cao: mỗi library một file đóng gói, một build target | Cao: thêm workspace tool, cache, generator |
| Chi phí mỗi lần thêm feature | Tạo thư mục | Tạo library, khai path, sửa build config | Chạy generator — nhưng phải hiểu generator |
| Thời gian build | Một app, build một lần | N library build tuần tự, chậm hơn rõ rệt | Nhanh nhờ cache, nếu cache hoạt động |
| Số công cụ đội phải học thêm | 0 | Angular library, công cụ đóng gói | Nx CLI, project graph, cấu hình cache |
| Tách một tầng ra package npm sau này | Phải làm thủ công | Sẵn sàng | Sẵn sàng |

**Được gì khi chọn thư mục:** đội không phải học công cụ mới, build một lần, thêm feature là tạo thư mục. Với một hệ tầm trung và một đội không lớn, đó là phần lớn giá trị.

**Mất gì:** mất hàng rào của compiler. Đây là cái giá thật, không phải cái giá trên giấy — nó có nghĩa là ranh giới FE phụ thuộc vào một script cổng còn chạy được, chứ không phải vào một đồ thị tham chiếu không thể sai.

**Vì vậy đánh đổi này chỉ hợp lệ kèm điều kiện:** F3 và F4 phải có cổng thật, và cổng phải có test của chính nó. Bỏ điều kiện đó thì phương án đã chọn không còn là "đơn giản hơn" mà là "không có ranh giới".

**Khi nào lật lại quyết định:** khi số module nghiệp vụ đủ lớn để build một app trở nên chậm tới mức cản trở vòng lặp sửa-thử, hoặc khi có nhu cầu thật phát hành một tầng ra ngoài repo dưới dạng package. Cả hai đều là ngưỡng quan sát được, không phải cảm giác — và cả hai đều phải ghi vào ADR trước khi động vào cấu trúc.

## 7. Checklist thêm một feature mới

—

## 8. Đối chiếu — luật nào ở đâu

—
