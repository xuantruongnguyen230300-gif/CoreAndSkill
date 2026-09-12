---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Kiến trúc Frontend — bốn tầng và hàng rào giữ chúng

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Repo đang ở giai đoạn 1: chưa có `src/`. Toàn bộ file này mô tả thứ `src/FE` **phải trở thành**, không phải mô tả hiện trạng.
>
> Lý do đằng sau ranh giới Core ↔ Module (chung cho cả BE lẫn FE) nằm ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md). File này không lặp lại phần lý luận đó — nó trả lời câu *"thi công thế nào và cái gì canh"*.

---

## 1. Bốn tầng — và một chiều duy nhất — định nghĩa gốc

```
modules/     ← nghiệp vụ riêng từng dự án. Lazy-loaded. Mỗi module một domain.
   ↓
platform/    ← màn hình Core: đăng nhập, đổi mật khẩu, quản trị người dùng, phân quyền
   ↓
shared/      ← thứ dùng chung KHÔNG phải hạ tầng singleton: component dumb, directive, pipe, model
   ↓
core/        ← tầng đáy: interceptor, auth, guard, envelope HTTP, i18n, theme, menu, toast
```

**Chiều phụ thuộc chỉ đi xuống.** Một tầng được import mọi tầng dưới nó; không tầng nào được import lên trên. `core/` là tầng đáy — nó không import bất cứ thứ gì từ ba tầng còn lại.

Ngang hàng thì tuỳ tầng:

| Tầng | Import ngang hàng |
| --- | --- |
| `core/` | Được — `core/auth` dùng `core/http` là bình thường |
| `shared/` | Được |
| `platform/` | Được — màn hình Core lắp vào nhau là chuyện thường |
| `modules/` | **CẤM.** `modules/A` không import `modules/B` (luật F2) |

`modules/` là tầng duy nhất bị cấm import ngang, vì đó là chỗ nghiệp vụ của các domain khác nhau sống cạnh nhau. Hai màn hình Core dính nhau thì tệ nhất là khó tách; hai module nghiệp vụ dính nhau thì mất luôn khả năng bỏ một module ra khỏi sản phẩm — thứ mà cả mô hình modular monolith sinh ra để giữ.

### 1.1 Cây thư mục cấp `src/FE/` — cái gì nằm NGOÀI `src/app/`

```
src/FE/
├── public/                 # tài sản tĩnh copy nguyên trạng vào bundle, ra thẳng GỐC SITE
│   ├── favicon.ico
│   ├── fonts/              # woff2 tự host — không gọi CDN font lúc chạy
│   └── i18n/               # bảng dịch vi.json / en.json, nạp lúc chạy
├── src/
│   ├── environments/       # cấu hình COMPILE-TIME: apiBaseUrl, production
│   ├── styles/             # token global + style toàn cục — xem fe-ui-conventions.md
│   ├── index.html
│   ├── main.ts
│   └── app/                # bốn tầng, xem §2
├── angular.json
├── eslint.config.js        # hàng rào ranh giới — §4
├── package.json
└── tsconfig*.json
```

Đừng tạo thư mục thứ năm ở cấp `src/FE/` mà không trả lời được: *"thứ này cần lúc build hay lúc chạy?"*

| | `src/environments/*.ts` | `public/*.json` |
| --- | --- | --- |
| Giá trị chốt lúc | **build** | **runtime**, fetch lúc khởi động |
| Đổi giá trị cần | build lại | thay file, không build lại |
| Dùng cho | `apiBaseUrl`, cờ `production` | bảng dịch |

**Không dựng cả hai cơ chế cho cùng một giá trị.** Hai nguồn cấu hình cho một giá trị là cách chắc chắn nhất để chúng lệch nhau, và chỗ lệch chỉ lộ ra ở môi trường mà không ai ngồi debug.

> **FE không có "dữ liệu runtime" theo nghĩa của BE.** Trình duyệt không ghi file lên đĩa server. Mọi thứ trong `src/FE/` đều là tài sản của source và đều vào git — trừ những gì [`repo-artifact.md`](repo-artifact.md) loại trừ.

---

## 2. Mỗi tầng chứa gì, cấm chứa gì

### 2.1 `core/` — hạ tầng toàn app

```
core/
├── auth/            # trạng thái phiên, đăng nhập/đăng xuất, permission signal
├── config/          # InjectionToken seam: CORE_ROUTES, CORE_BRANDING, CORE_I18N
├── guards/          # authGuard, permissionGuard, mustChangePasswordGuard
├── http/            # envelope model, hàm CRUD dùng chung, unwrap, mapper lỗi
├── interceptors/    # thứ tự khai ở fe-api-client.md, KHÔNG tuỳ tiện
├── i18n/            # cấu hình ngx-translate, loader, service đổi ngôn ngữ
├── menu/            # menu động theo quyền
├── theme/           # preset PrimeNG, chuyển sáng/tối
└── toast/           # service phát toast (component hiển thị nằm ở shared/)
```

| Được chứa | **Cấm** chứa |
| --- | --- |
| Service `providedIn: 'root'`, sống suốt vòng đời app | Component có template người dùng nhìn thấy |
| Interceptor, guard, resolver | Bất cứ import nào từ `shared/`, `platform/`, `modules/` |
| Kiểu và hàm thuần dùng bởi hạ tầng | Tên sản phẩm, đường dẫn route cụ thể, bảng màu của một dự án — chúng vào qua **seam** (§2.5) |
| `InjectionToken` khai seam cấu hình | Logic riêng của đúng một feature |

**Phép thử `core/` vs `shared/services/`:** *bỏ hết màn hình đi thì service này còn nghĩa gì không?* Còn → `core/`. Không → `shared/services/`.

Service quản lý phiên đăng nhập vẫn có nghĩa khi chưa vẽ màn hình nào → `core/`. Service nhớ trạng thái đóng/mở của sidebar thì không → `shared/services/`.

Đây là chỗ đặt nhầm nhiều nhất, và đặt nhầm thì gãy luật F1 ngay: một service hạ tầng lỡ nằm ở `shared/` sẽ bị `core/` import ngược lên. Ở dự án tiền nhiệm đã xảy ra đúng ca này — interceptor trong `core/` import service toast từ `shared/services/`. Cách sửa là chuyển service xuống `core/`, giữ component hiển thị ở `shared/` và cho nó import ngược xuống `core/` (đúng chiều được phép).

### 2.2 `shared/` — dùng chung nhưng không phải hạ tầng

```
shared/
├── ui/           # BỌC thư viện UI. Mỏng, không biết nghiệp vụ, không biết API.
├── components/   # dumb UI tái dùng > 1 feature, ghép TỪ shared/ui/
├── forms/        # hạ tầng form dùng chung: gắn fieldErrors vào control, hiển thị lỗi
├── directives/   # directive dùng chung: appHasPermission, appAutofocus
├── pipes/        # pipe thuần
├── models/       # kiểu dùng chung giữa nhiều feature — KHÔNG phải DTO
└── services/     # service TRẠNG THÁI UI: không gọi HTTP, không giữ phiên
```

`ui/` và `components/` là **hai** thư mục, không phải một: ranh giới giữa chúng, và lý do tách, ở [`../wiki-core/fe/05-component-library.md`](../wiki-core/fe/05-component-library.md) §3. Trộn chúng lại thì lớp bọc dần mang logic và mất đúng tính chất khiến nó tồn tại.

> ⚠️ **Thư mục vắng mặt trong sơ đồ này là thư mục không cổng nào canh.** Hai thư mục `ui/` và `forms/` từng bị thiếu ở đây trong khi tài liệu khác yêu cầu chúng — mà cổng F5 khoá theo đường dẫn `shared/ui/**`: một cổng canh một đường dẫn không có trong sơ đồ là cổng **luôn xanh vì không gì rơi vào vùng nó canh**. Thêm thư mục con vào `shared/` thì thêm dòng ở đây trước.

| Được chứa | **Cấm** chứa |
| --- | --- |
| Component chỉ nhận `input()` và phát `output()` | Component tự inject service lấy dữ liệu (luật F11) |
| Bọc component thư viện UI trong `ui/` — xem [`../wiki-core/fe/05-component-library.md`](../wiki-core/fe/05-component-library.md) §2 | Gọi `HttpClient` |
| Kiểu dùng chung giữa nhiều feature | DTO — DTO thuộc feature, xem [`fe-api-client.md`](fe-api-client.md) |
| | Import từ `platform/` hoặc `modules/` |

> `shared/` là một phần của **CoreBase** — nó đi theo khi mang nền tảng sang dự án khác. Vì vậy nó không được biết tên sản phẩm, không được biết bảng màu cụ thể, không được biết route nghiệp vụ nào.

### 2.3 `platform/` — Màn hình Core — định nghĩa gốc

Màn hình có nghĩa với **mọi** sản phẩm dựng trên nền tảng: đăng nhập, quên mật khẩu, đổi mật khẩu bắt buộc, hồ sơ cá nhân, quản trị người dùng, quản trị vai trò, phân quyền, quản trị đơn vị (khu hệ thống).

**Trang chủ** cũng thuộc `platform/`, nhưng khác các màn trên ở một điểm: Core chỉ cấp **trang chào tối giản** — tên đơn vị, lời chào, lối tắt tới các màn người dùng có quyền. Nó không gọi endpoint riêng nào, chỉ dùng lại thông tin phiên và menu.

Dự án dựng trên Core hầu như luôn muốn thay nó bằng một **bảng tổng hợp có số liệu**. Vì vậy trang chủ đi qua seam `CORE_HOME` (§2.5): dự án cấp component của mình, **không** sửa route và không sửa khung shell. Core không đoán hộ dự án cần số liệu gì — số liệu là nghiệp vụ của ngành. Core cấp **component** biểu đồ ([`../Design/Components/Chart.md`](../Design/Components/Chart.md)); riêng **thư viện** biểu đồ vẫn thuộc nhóm thêm-khi-cần và nạp theo yêu cầu ([`../wiki-core/fe/01-core-components.md`](../wiki-core/fe/01-core-components.md) §3, [`../adr/0019-ba-component-nang-thuoc-core.md`](../adr/0019-ba-component-nang-thuoc-core.md) ràng buộc 1).

Mỗi màn ở trên có một card trong [`../contracts/`](../contracts/) — đó là danh sách kiểm được bằng máy, còn câu trên là câu văn. **Quản trị menu KHÔNG nằm trong danh sách**: ở v1 menu là dữ liệu seed, lý do ở [`../contracts/meta-menu.md`](../contracts/meta-menu.md) §2.

Cấu trúc con giống hệt `modules/` (§3). Khác biệt duy nhất là **ý nghĩa**, không phải hình dạng.

Khi phân vân đặt một feature vào `platform/` hay `modules/`, hỏi: *"màn này có ý nghĩa với sản phẩm tiếp theo dựng trên nền tảng này không?"* Có → `platform/`. Không → `modules/`.

Đặt nhầm về phía `platform/` đắt hơn đặt nhầm về phía `modules/`: một màn nghiệp vụ lọt vào `platform/` sẽ đi theo nền tảng sang mọi dự án sau, mang theo cả khái niệm mà dự án đó không có. Khi phân vân, chọn `modules/` — nâng lên sau rẻ hơn gỡ xuống.

### 2.4 `modules/` — nghiệp vụ riêng dự án

```
modules/
├── <ten-module-a>/
└── <ten-module-b>/
```

Mỗi thư mục con là **một domain nghiệp vụ**, lazy-loaded, và có tên trùng với tên khai trong mảng `BUSINESS_MODULES` của `eslint.config.js` (§4.4).

Tương ứng phía BE là một `Modules.<X>.*`; ranh giới khi nào tách module mới ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.3.

### 2.5 Seam — `core/` giữ CƠ CHẾ, app cấp DỮ LIỆU

`core/` và `shared/` mang đi được sang dự án khác. Vì vậy **mọi dữ liệu riêng của một dự án** phải đi vào chúng qua một seam, không được khai cứng bên trong.

| Seam | Cấp gì cho `core/` | Chi tiết ở |
| --- | --- | --- |
| `CORE_ROUTES` | Đường dẫn mà guard chuyển hướng tới | [`fe-routing-guard.md`](fe-routing-guard.md) |
| `CORE_BRANDING` | Tên sản phẩm, chữ tắt | mục này |
| `CORE_HOME` | Component của **trang chủ**. Core cấp một trang chào tối giản; dự án thay bằng bảng tổng hợp của ngành mình mà không sửa route, không sửa shell — §2.3 |
| `CORE_I18N` | Danh sách ngôn ngữ, ngôn ngữ mặc định, và các nguồn tệp dịch theo thứ tự nạp | [`../wiki-core/fe/08-i18n.md`](../wiki-core/fe/08-i18n.md) §2.3 |
| `API_BASE_URL` | Base URL của API, cấp từ `environment.apiBaseUrl` | [`fe-api-client.md`](fe-api-client.md) §2.1 |
| Bảng màu preset | Tham số cho preset PrimeNG | [`fe-ui-conventions.md`](fe-ui-conventions.md) |
| `CORE_SCREEN_EXT` | **Phần mở rộng của từng màn Core** — cột, hành động dòng, trường lọc mà dự án thêm vào. Xem §2.7 | mục này |

Khuôn bắt buộc, không phát minh khuôn thứ hai:

```typescript
// core/config/core-branding.ts — hợp đồng, KHÔNG có giá trị mặc định
import { InjectionToken, EnvironmentProviders, makeEnvironmentProviders } from '@angular/core';

export interface CoreBranding {
  readonly name: string;      // tên đầy đủ: hậu tố <title>, dòng chữ ở sidebar
  readonly shortName: string; // chữ tắt trong ô vuông thương hiệu
}

export const CORE_BRANDING = new InjectionToken<CoreBranding>('CORE_BRANDING');

export function provideCoreBranding(branding: CoreBranding): EnvironmentProviders {
  return makeEnvironmentProviders([{ provide: CORE_BRANDING, useValue: branding }]);
}
```

**Token cố ý không có `factory` mặc định.** Một giá trị mặc định biến "app quên khai" thành một giao diện mang tên sản phẩm **khác** — sai ở chỗ dễ thấy nhất mà không lỗi nào, không test nào bắt. Thiếu provider thì Angular ném `NG0201` ngay lần dựng đầu, tức lỗi nổ đúng lúc và đúng chỗ.

Ngoại lệ duy nhất là thứ phải dựng **trước khi injector tồn tại** — bảng màu, vì hàm cấu hình theme chạy lúc tạo object cấu hình. Cái đó là **tham số hàm**, và quên truyền là lỗi biên dịch, sớm hơn cả `NG0201`.

Khi cần seam mới, phép thử là: *"giá trị này có đổi khi dựng sản phẩm khác trên cùng nền tảng không?"* Có → seam. Không → để trong `core/`.

### 2.6 Composition root — nơi mọi seam được nối lại

`be-architecture.md` §3 khai một "host mỏng" ở phía BE. Phía FE có đúng một chỗ tương ứng: **file cấu hình app**, nơi khai toàn bộ danh sách provider gốc.

Ba thứ ở đây hỏng theo kiểu **không có thông báo lỗi nào**:

| Bẫy | Triệu chứng | Vì sao không ai thấy |
| --- | --- | --- |
| Khai một service **hai lần** dưới hai token (`useClass` cho token thứ hai thay vì `useExisting`) | Hai thể hiện tồn tại song song. Bên ghi trạng thái và bên đọc trạng thái là hai object khác nhau, nên giao diện phụ thuộc trạng thái đó **không bao giờ cập nhật** | Không lỗi, không cảnh báo. Cả hai thể hiện đều hợp lệ |
| **Thứ tự interceptor** khai sai | Xem [`fe-api-client.md`](fe-api-client.md) — thứ tự ở đó không tuỳ tiện | Request vẫn đi, chỉ đi qua sai trình tự |
| Bước khởi tạo chạy trước khi thứ nó phụ thuộc sẵn sàng | Lần tải trang đầu tiên hỏng, lần sau bình thường | Chỉ tái hiện được ở tải nguội |

**Luật:** `useExisting` khi hai token phải trỏ về **cùng một** thể hiện; `useClass` chỉ khi thật sự cần thể hiện thứ hai. Mặc định là `useExisting` — nhu cầu có hai thể hiện của một service hạ tầng gần như luôn là dấu hiệu của một nhầm lẫn.

**Luật:** mỗi bước khởi tạo chạy trước khi app dựng phải khai rõ **nó phụ thuộc bước nào**. Hai bước độc lập chạy song song được; hai bước có phụ thuộc mà khai như thể độc lập là một lỗi chỉ lộ ra khi mạng chậm.

> 📖 Danh sách interceptor và thứ tự của chúng: đọc [`fe-api-client.md`](fe-api-client.md). File này **không** chép lại danh sách đó.

---

### 2.7 Seam cho MÀN Core — định nghĩa gốc

Sáu seam ở §2.5 đều ở mức **toàn app**: tên sản phẩm, ngôn ngữ, base URL, trang chủ. Không seam nào chạm tới **một màn cụ thể**, và đó là một lỗ hổng thật:

> Một dự án hạ nguồn cần thêm cột *"Mã nhân viên"* vào danh sách người dùng của Core có đúng hai đường: **sửa file trong `platform/`** — vi phạm [`../adr/0016-phan-phoi-core-bang-clone.md`](../adr/0016-phan-phoi-core-bang-clone.md) luật #1 — hoặc **dựng một màn danh sách người dùng thứ hai** trong `modules/`. Cả hai đều là fork, chỉ khác hình thức.

Đường thứ hai nguy hiểm hơn vì nó **vô hình**: nó không sửa file nào của Core nên không cổng nào thấy, nhưng bản vá Core lần sau sẽ không bao giờ tới được màn đã nhân đôi.

`CORE_SCREEN_EXT` đóng đường đó:

```typescript
// core/config/core-screen-ext.ts — hợp đồng, KHÔNG có giá trị mặc định
import { InjectionToken, TemplateRef } from '@angular/core';
import { DataColumnDef, FilterField, UiMenuItem } from '../../shared/ui/types';

/** Phần một dự án được phép THÊM vào một màn Core. Không có gì cho phép BỚT. */
export interface ScreenExtension<T = unknown> {
  readonly columns?: ReadonlyArray<DataColumnDef<T>>;   // nối vào SAU cột của Core
  readonly rowActions?: ReadonlyArray<UiMenuItem>;      // nối vào cột hành động
  readonly filterFields?: ReadonlyArray<FilterField>;   // nối vào FilterPanel
  readonly toolbarSlot?: TemplateRef<unknown>;          // chèn vào nhóm hành động của Toolbar
}

/** Khoá là mã màn Core: 'users' · 'roles' · 'permissions' · 'tenants' · 'profile' */
export const CORE_SCREEN_EXT =
  new InjectionToken<Readonly<Record<string, ScreenExtension>>>('CORE_SCREEN_EXT');
```

Bốn luật của seam này, và cả bốn đều có lý do:

1. **Chỉ THÊM, không BỚT.** Không có `hiddenColumns`, không có `removeActions`. Một dự án giấu cột "Trạng thái" khỏi danh sách người dùng là giấu một thông tin mà luồng khoá tài khoản của Core dựa vào — và lỗi đó chỉ lộ khi có người hỏi *"sao tài khoản này đăng nhập được"*. Cần bớt thì đó là dấu hiệu màn Core sai, và sửa ở Core.
2. **Cột thêm nối vào SAU, không chen giữa.** Cho chọn vị trí nghĩa là Core phải giữ một thứ tự cột ổn định mãi mãi; nối sau thì Core thêm cột mới mà không phá bố cục của ai.
3. **Khoá là mã màn, không phải đường dẫn route.** Route đổi được qua `CORE_ROUTES`; mã màn thì không. Khoá theo route là khoá vào một thứ đã có seam riêng để đổi.
4. **Token không có `factory` mặc định** — cùng lý do với `CORE_BRANDING` ở §2.5. Nhưng khác một điểm: dự án **không** bắt buộc khai seam này. Không khai thì màn Core chạy đúng bản gốc, và đó là mặc định đúng.

**Cái giá, nói thẳng:** mỗi màn Core phải tự đọc token này và tự nối phần mở rộng vào — tức là một đoạn mã lặp ở mọi màn `platform/`. Nếu quên ở một màn thì màn đó **im lặng không mở rộng được**, và dự án hạ nguồn sẽ phát hiện bằng cách thấy cột mình khai không hiện ra. Đây là chỗ cần một mục kiểm khi có `src/`: mọi màn danh sách trong `platform/` phải đọc `CORE_SCREEN_EXT`.

### 2.8 Trạng thái màn danh sách — định nghĩa gốc

[`../wiki-core/fe/11-grid-and-metadata.md`](../wiki-core/fe/11-grid-and-metadata.md) §3–§4 mô tả rất kỹ **cái gì** phải làm: giữ truy vấn, đồng bộ URL, quy tắc *"đổi lọc thì về trang 1"*, và ba cơ chế chống gọi dồn dập. Nhưng cả [`../Design/Components/Toolbar.md`](../Design/Components/Toolbar.md) lẫn [`../Design/Components/DataTable.md`](../Design/Components/DataTable.md) đều đẩy việc đó sang *"trang cha"* — và **không file nào nói trang cha là ai**.

Hệ quả nếu để nguyên: người dựng màn danh sách **đầu tiên** buộc phải quyết, vì không viết được màn nào mà không quyết. Quyết đó thành tiền lệ cho mọi màn sau, và không ai duyệt nó.

Tầng này là **smart** — nó biết route, biết HTTP. Vì vậy nó **không** nằm được ở bất kỳ component nào trong [`../Design/COMPONENTS.md`](../Design/COMPONENTS.md) §3, nơi mọi component đều dumb. Nó thuộc `core/`.

```typescript
// core/list/grid-query.ts
export interface GridQuery {
  readonly page: number;                              // đếm từ 1
  readonly pageSize: number;
  readonly sortBy: string | null;                     // tên trường, theo contracts/
  readonly sortDir: 'asc' | 'desc' | null;
  readonly keyword: string;                           // ô tìm của Toolbar
  readonly filters: Readonly<Record<string, unknown>>;// kết quả từ FilterPanel
}

/**
 * 🛑 `sortBy`/`sortDir` KHÔNG phải tên tuỳ chọn. Đó là tên trên dây, khai ở
 * `docs/contracts/README.md` §8. `DataTable` nhận đúng hai tên này; việc quy đổi
 * sang dạng `1 | -1` của PrimeNG nằm TRONG lớp bọc, không rò ra ngoài.
 */

/** Hợp đồng của tầng giữ trạng thái danh sách. Một thực thể cho MỘT màn. */
export interface ListStateStore {
  readonly query: Signal<GridQuery>;
  readonly state: Signal<'idle' | 'loading' | 'error' | 'empty' | 'empty-filtered'>;

  setPage(page: number, pageSize?: number): void;
  setSort(field: string | null, order: 1 | -1 | null): void;
  setKeyword(keyword: string): void;                  // tự chống gọi dồn dập
  setFilters(filters: Record<string, unknown>): void; // tự đưa page về 1
  reload(): void;
}
```

Năm luật của tầng này. Bốn luật đầu là thứ mỗi màn sẽ tự làm sai một kiểu nếu không khai ra:

1. **Truy vấn sống trên URL, và URL là nguồn sự thật.** Mở lại một link phải ra đúng danh sách đó — kèm trang, sắp xếp, từ khoá và bộ lọc. Không phải để chia sẻ link cho đẹp, mà vì người dùng bấm Quay lại của trình duyệt sau khi xem một bản ghi và mong về đúng chỗ cũ.
2. **Đổi bộ lọc hoặc từ khoá thì `page` về 1.** Không về 1 thì người dùng đang ở trang 7, lọc lại còn 2 trang, và thấy một danh sách rỗng — họ đọc đó là *"không có dữ liệu"*.
3. **Ba cơ chế chống gọi dồn dập, không phải một.** Chờ ngừng gõ (`setKeyword`), bỏ qua truy vấn trùng truy vấn trước, và **huỷ kết quả của request cũ khi request mới đã về**. Thiếu cơ chế thứ ba thì gõ nhanh sẽ thấy danh sách nhảy về kết quả của chuỗi đã gõ xong từ lâu — lỗi chỉ hiện khi mạng chậm, nên nó qua được mọi lần thử ở máy lập trình viên.
4. **`state` là một biến, không phải bốn cờ** — cùng lý do đã ghi ở `DataTable.md`: bốn cờ cho phép biểu diễn tổ hợp vô nghĩa.
5. **Một thực thể cho một màn, cấp ở cấp route.** Cấp ở gốc app thì hai màn danh sách mở song song sẽ ghi đè truy vấn của nhau.

🛑 **Tầng này KHÔNG gọi HTTP.** Nó giữ truy vấn và phát tín hiệu; màn hình theo dõi `query` rồi tự gọi service của mình. Cho nó gọi HTTP nghĩa là nó phải biết endpoint, và lúc đó nó không dùng lại được cho màn thứ hai.

## 3. Cấu trúc một feature

Áp cho **cả** `platform/<feature>/` lẫn `modules/<feature>/`.

```
<platform|modules>/<feature>/
├── <feature>.routes.ts             # lazy route riêng của feature
├── pages/<ten-page>/               # SMART — route target
│   ├── <ten-page>.page.ts
│   ├── <ten-page>.page.html
│   └── <ten-page>.page.scss
├── components/<ten-component>/     # DUMB — chỉ input()/output()
├── services/
│   ├── <feature>.service.ts        # gọi API
│   ├── <feature>.service.spec.ts   # luật F12: mọi service có spec cạnh nó
│   └── <feature>.mapper.ts         # DTO ↔ model
├── models/
│   ├── <feature>.model.ts          # model của màn hình
│   └── <feature>.dto.ts            # hình dạng trên dây — chỉ services/ được import
└── state/ (tuỳ chọn)               # signal store khi state đủ phức tạp
```

### 3.1 Bảng trách nhiệm — quy tắc cứng

| Thư mục | Được phép | Cấm |
| --- | --- | --- |
| `pages/*` | inject service/store, giữ signal của màn hình, điều hướng, đọc query param | gọi `HttpClient` trực tiếp; import DTO (luật F10) |
| `components/*` | nhận `input()`, phát `output()`, render | inject service lấy dữ liệu (luật F11); biết HTTP; import DTO |
| `services/*` | gọi API, map DTO ↔ model, hủy/retry | giữ trạng thái UI (mở/đóng dialog, tab đang chọn) |
| `state/*.store.ts` | `signal`/`computed`, điều phối service | render, đụng DOM |
| `models/*` | type, interface, hằng số của miền | logic, gọi hàm |

**Vì sao `components/` bị cấm inject data service:** một component dumb có thể đặt ở bất kỳ đâu, test bằng cách truyền input, và tái dùng ở màn hình thứ hai mà không kéo theo gì. Ngay khi nó tự lấy dữ liệu, ba tính chất đó mất cùng lúc — và cách duy nhất để tái dùng nó ở màn khác là thêm tham số điều kiện, tức bắt đầu vòng xoáy god component.

Nếu thấy mình đang muốn inject service vào một component trong `components/`, đó là dấu hiệu nó nên là một page (smart), không phải component.

### 3.2 Khi nào cần `state/`

**Không** tạo store mặc định cho mọi feature. Chỉ thêm khi có ít nhất một trong:

- Nhiều page/component trong cùng feature cần đọc-ghi chung một state.
- State phải derive qua nhiều bước `computed()` lồng nhau.
- Cần giữ cache giữa các lần điều hướng qua lại.

Feature một page, state cục bộ → khai `signal()` thẳng trong page. Bọc một store cho state chỉ một nơi dùng là thêm một lớp gián tiếp không đổi lấy gì.

> 📖 **Store viết tay bằng `signal()`, chưa dùng NgRx ở v1; ba ngưỡng để cân nhắc thêm thư viện; và khuôn store viết tay: đọc [`../wiki-core/fe/03-state-management.md`](../wiki-core/fe/03-state-management.md) §2, §3 và §4.** Đó là file chủ.

---

## 4. Ranh giới ép bằng ESLint — mục quan trọng nhất của file này

### 4.1 Vì sao mục này dài hơn mọi mục khác

Phía BE, ranh giới tầng do **compiler** ép: `Core.Domain` không có `ProjectReference` tới `Core.Infrastructure` thì code trong Domain *không thể* gọi `DbContext`, dù người viết có muốn. Hàng rào đó chi phí bằng 0 và không bao giờ hỏng.

Phía FE, quyết định đã chốt là **giữ cấu trúc thư mục** trong một app Angular duy nhất, không tách Angular workspace nhiều library, không dùng Nx ([`../adr/0007-fe-giu-cau-truc-thu-muc.md`](../adr/0007-fe-giu-cau-truc-thu-muc.md)). Hệ quả trực tiếp: **không có compiler nào ép ranh giới tầng.** Một import trỏ ngược lên tầng trên vẫn biên dịch sạch sẽ.

Hàng rào duy nhất còn lại là ESLint. Và ESLint có một đặc tính phải xử lý: **nó tắt được bằng một dòng comment.** Vì vậy bốn luật đi thành một bộ, thiếu một cái thì những cái kia mất hiệu lực:

| Luật | Nội dung | Nếu thiếu |
| --- | --- | --- |
| **F1** | `core/` không import ngược lên | Tầng đáy không còn là tầng đáy |
| **F2** | `modules/A` không import `modules/B` | Module dính nhau, không bỏ ra được |
| **F3** | Cấm `eslint-disable` cho danh sách rule ranh giới | **F1 và F2 chỉ còn là gợi ý** |
| **F4** | `BUSINESS_MODULES` khớp thư mục `modules/` thật | F2 thành no-op im lặng |

### 4.2 Zone `coreLayerZones` — `core/` là tầng đáy

```typescript
// eslint.config.js
// Luật F1 — core/ là tầng đáy. Mọi tầng khác được phụ thuộc vào core/,
// nhưng core/ không được import ngược lên bất kỳ tầng nào trong ba tầng trên.
const coreLayerZones = [
  {
    target: './src/app/core',
    from: './src/app/shared',
    message:
      'core/ không được import shared/ — core/ là tầng đáy. Hạ tầng dùng chung cho cả ' +
      'core/ lẫn shared/ thì đưa THẲNG vào core/, không đặt ở shared/ rồi import ngược.',
  },
  {
    target: './src/app/core',
    from: './src/app/platform',
    message: 'core/ không được import platform/ — core/ là tầng đáy.',
  },
  {
    target: './src/app/core',
    from: './src/app/modules',
    message: 'core/ không được import modules/ — core/ là tầng đáy.',
  },
];
```

Nối vào config, áp cho đúng file trong `core/`:

```typescript
{
  files: ['src/app/core/**/*.ts'],
  plugins: { import: importPlugin },
  settings: {
    // Resolver mặc định chỉ thử `.js`/`.json`. Import nội bộ TypeScript không có
    // phần mở rộng, nên phải khai thêm `.ts` — thiếu dòng này rule không phân giải
    // nổi đường dẫn và im lặng không chặn gì.
    'import/resolver': { node: { extensions: ['.ts', '.js'] } },
  },
  rules: {
    'import/no-restricted-paths': ['error', { zones: coreLayerZones }],
  },
}
```

> ⚠️ **Bẫy resolver.** `import/no-restricted-paths` phải phân giải specifier ra đường dẫn file thật mới so được với `target`/`from`. Quên khai `extensions` có `.ts` thì rule không nổ, không cảnh báo, và **trông y hệt như đang chạy sạch**. Khi thi công, viết một file canary vi phạm cố ý và xác nhận lint đỏ — luật T1 ở [`../RULES.md`](../RULES.md) áp cho cả detector phía FE.

### 4.3 Zone `moduleBoundaryZones` — module không import chéo

```typescript
// Thêm module nghiệp vụ mới → thêm ĐÚNG tên thư mục vào mảng này.
// Không viết tay một zone mới; zone sinh ra từ mảng.
const BUSINESS_MODULES = [];

const moduleBoundaryZones = BUSINESS_MODULES.map((moduleName) => ({
  target: `./src/app/modules/${moduleName}`,
  from: './src/app/modules',
  except: [`./${moduleName}`],
  message:
    `modules/${moduleName}/ không được import nội bộ một module nghiệp vụ khác. ` +
    `Chỉ được import từ core/, shared/, platform/. Cần dùng chung logic thì nâng lên ` +
    `shared/ (dumb UI, service tái dùng) hoặc core/ (hạ tầng toàn app).`,
}));
```

Và — đây là chỗ có bẫy — khối rule phải **bị bỏ hẳn** khi mảng rỗng:

```typescript
// 🛑 Schema của `import/no-restricted-paths` đòi `zones` có TỐI THIỂU một phần tử.
// Truyền mảng rỗng làm ESLint chết ngay lúc nạp config ("Invalid Options") — toàn bộ
// lệnh lint đỏ vì một lý do không liên quan gì tới code đang sửa.
...(moduleBoundaryZones.length > 0
  ? [
      {
        files: ['src/app/modules/**/*.ts'],
        plugins: { import: importPlugin },
        settings: {
          'import/resolver': { node: { extensions: ['.ts', '.js'] } },
        },
        rules: {
          'import/no-restricted-paths': ['error', { zones: moduleBoundaryZones }],
        },
      },
    ]
  : []),
```

Zone chỉ áp cho `src/app/modules/**` — **không** áp cho `platform/`. Màn hình Core được phép lắp vào nhau; đó là chủ đích, không phải sót.

> ⚠️ **Cảnh báo từ dự án tiền nhiệm — bài học đắt nhất của mục này.**
>
> Ở dự án tiền nhiệm, mảng `BUSINESS_MODULES` để rỗng (đúng theo cách hiểu "chưa có module nghiệp vụ nào"), nên cả block rule bị bỏ hẳn theo đúng nhánh điều kiện ở trên. Hệ quả: **ranh giới module phía FE chưa từng được kiểm chứng một lần nào.** Luật vẫn nằm trong tài liệu, cấu hình vẫn có mặt, và mọi thứ trông như đang chạy.
>
> Đó không phải lỗi của người viết cấu hình — mảng rỗng là lựa chọn đúng khi chưa có module, vì zone trỏ vào thư mục không tồn tại thì cũng chẳng chặn được gì mà lại trông như đang chặn. Vấn đề là **không có gì nhắc khi module đầu tiên ra đời**. Luật F4 và §4.4 tồn tại để lấp đúng khoảng trống đó.

### 4.4 Quy trình thêm một module mới — bốn bước, không bỏ bước nào

1. Tạo `src/app/modules/<ten-module>/` theo cấu trúc §3.
2. **Thêm tên module vào mảng `BUSINESS_MODULES` trong `eslint.config.js`.**
3. Đăng ký lazy route trong `app.routes.ts` — xem [`fe-routing-guard.md`](fe-routing-guard.md).
4. Chạy cổng FE và xác nhận zone thật sự chặn: tạm viết một import chéo, thấy lint đỏ, rồi xoá.

Bước 2 là bước bị quên. Cổng phải bắt — nguyên tắc của phép kiểm:

```bash
# Luật F4 — BUSINESS_MODULES khớp thư mục modules/ thật.
# PASS khi hai danh sách giống hệt nhau (diff không in dòng nào).
diff <(ls -1 src/FE/src/app/modules 2>/dev/null | sort) \
     <(node -p "require('./src/FE/eslint.config.js').BUSINESS_MODULES.join('\n')" | sort)
```

> Cách phơi mảng ra để script đọc được (export riêng, hay parse cấu hình) sẽ chốt khi thi công cổng ở giai đoạn 2. Điều **không** đổi là nguyên tắc: cổng đọc mảng từ chính file cấu hình rồi so với thư mục thật. **Không được** chép danh sách module vào script cổng — hai bản sao thì bản không ai nhớ sẽ nói dối ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §5).

### 4.5 Cấm `eslint-disable` cho rule ranh giới — luật F3

Không có luật này thì F1 và F2 chỉ là gợi ý: bất kỳ ai gặp lint đỏ đều có thể viết một dòng comment và đi tiếp, và diff của dòng đó trông vô hại trong review.

#### Danh sách rule cấm tắt — định nghĩa gốc

Bảng này là **Danh sách rule cấm tắt — định nghĩa gốc**, nguồn duy nhất của tập rule mà `eslint-disable` không được chạm tới. `scripts/fe-gate.sh` khai một mảng cùng nội dung và chú thích trỏ về đây; tài liệu khác **không** chép lại bảng — chúng trỏ về mục này ([`../OWNERSHIP.md`](../OWNERSHIP.md) §2).

| Rule | Canh luật |
| --- | --- |
| `import/no-restricted-paths` | F1, F2 |
| `@angular-eslint/template/use-track-by-function` | F13 |
| `@angular-eslint/template/prefer-control-flow` | F9, F13 |
| Rule ranh giới bổ sung khi có | — |

> ⚠️ **Ánh xạ rule → luật phải đúng tên rule, không đúng "trông có vẻ liên quan".** Ghi một rule chỉ "trông có vẻ liên quan" thì hai hệ quả cùng lúc: cổng cấm tắt một rule **ngoài** bộ ranh giới, và rule thật sự ép luật đó **không** có tên trong danh sách — nên đúng một dòng `eslint-disable` gỡ được nó, hợp lệ theo chính bảng này.

Cổng quét mọi dạng comment tắt rule, không chỉ một dạng:

```bash
# Luật F3 — không được tắt rule ranh giới bằng comment.
# PASS khi không in ra dòng nào.
grep -rnE 'eslint-disable(-next-line|-line)?[^\n]*import/no-restricted-paths' \
  src/FE/src --include='*.ts' --include='*.html'

# Dạng tắt TOÀN FILE không kèm tên rule — cũng cấm, vì nó tắt luôn rule ranh giới
# mà không chứa chuỗi nào để grep theo tên.
grep -rnE 'eslint-disable\s*\*/' src/FE/src --include='*.ts'
```

Ba điểm phải giữ khi thi công cổng này:

1. **Quét cả ba dạng** `eslint-disable`, `eslint-disable-next-line`, `eslint-disable-line`. Chỉ quét một dạng thì hai dạng kia đi lọt, và cổng xanh vì mù chứ không vì sạch.
2. **Quét cả dạng tắt toàn file.** Dạng này tắt *mọi* rule, gồm cả rule ranh giới, mà không chứa tên rule nào để grep — phải bắt bằng mẫu riêng.
3. **Cổng phải có test của chính nó.** Viết một file canary chứa đúng dòng bị cấm, chạy cổng, xác nhận đỏ, rồi xoá. Một cổng hỏng âm thầm tệ hơn không có cổng: nó tạo cảm giác được bảo vệ. Xem [`../audit/2026-08-23-cong-khong-ton-tai.md`](../audit/2026-08-23-cong-khong-ton-tai.md).

**Khi thật sự cần một ngoại lệ:** không tắt rule. Sửa thiết kế — nâng thứ dùng chung lên `shared/` hoặc `core/`, hoặc để hai module nói chuyện qua tầng dưới thay vì import thẳng. Nếu tin rằng ranh giới sai chứ không phải code sai, đường đi là **sửa ADR**, không phải sửa comment: mở [`../adr/0007-fe-giu-cau-truc-thu-muc.md`](../adr/0007-fe-giu-cau-truc-thu-muc.md), ghi lý do, chốt lại luật, rồi sửa cấu hình cho khớp.

Đánh đổi của F3 nói thẳng: nó **sẽ** gây khó chịu đúng vào lúc người ta đang vội. Đó là chủ đích. Một hàng rào chỉ có tác dụng khi việc vượt qua nó đắt hơn việc đi vòng.

---

## 5. Ngưỡng kích thước file

Không có ngưỡng thì không có thời điểm nào để tách, và mọi file đều lớn dần cho tới lúc không ai đọc nổi. Ở dự án tiền nhiệm, một file style vượt một nghìn dòng và một page vượt năm trăm dòng — cả hai đều không phải do một lần viết ẩu, mà do hai mươi lần thêm "chỉ một chỗ nữa".

| Loại file | Ngưỡng mềm | Ngưỡng cứng | Tách thế nào khi vượt |
| --- | --- | --- | --- |
| `*.page.ts` (smart) | 250 | **400** | Tách khối UI ra `components/` con; đẩy logic gọi API xuống `services/`; state phức tạp thì lên `state/` |
| `*.component.ts` (dumb) | 150 | **250** | Component dumb vượt ngưỡng gần như luôn là hai component bị dính; tách theo trục "phần nào tái dùng riêng được" |
| `*.service.ts` | 200 | **300** | Tách mapper ra file riêng; tách theo nhóm endpoint nếu service phục vụ nhiều thực thể |
| `*.html` | 150 | **250** | Template dài là triệu chứng, không phải bệnh — cắt cùng lúc với file `.ts` của nó |
| `*.scss` (component) | 100 | **200** | Style component vượt ngưỡng nghĩa là đang tự vẽ lại thứ đã có trong `shared/`, hoặc đang override thư viện — xem [`fe-ui-conventions.md`](fe-ui-conventions.md) |
| `styles/*.scss` (global) | 200 | **300** | Tách theo nhóm: token màu · typography · spacing · override thư viện |

**Ngưỡng mềm** là lúc dừng lại tự hỏi; **ngưỡng cứng** là lúc cổng đỏ.

```bash
# PASS khi không in ra dòng nào.
find src/FE/src/app -name '*.page.ts' -not -name '*.spec.ts' \
  -exec awk 'END { if (NR > 400) print FILENAME ": " NR }' {} \;
```

Ba điều ngưỡng này **không** làm, để tránh hiểu nhầm:

- Không khuyến khích tách file bằng cách cắt đôi giữa chừng rồi import lại. Hai file gọi nhau mà không có ranh giới ý nghĩa thì tệ hơn một file dài.
- Không áp cho `*.spec.ts`. Test dài là bình thường; tách test theo số dòng là phản tác dụng.
- Không bao giờ chấp nhận bản `-v2` song song một component hay service. Sửa tại chỗ; lịch sử nằm trong git.

**Vì sao ngưỡng của `.scss` chặt nhất:** style là loại file duy nhất mà việc thêm dòng gần như không có chi phí nhận thức tại thời điểm thêm — không ai phải đọc lại 200 dòng trên để viết dòng 201. Đó chính là lý do nó phình nhanh nhất và là chỗ hex trần sinh sôi.

---

## 6. So sánh với phương án đã loại

Ba phương án được cân nhắc cho ranh giới FE. Quyết định và lý do đầy đủ ở [`../adr/0007-fe-giu-cau-truc-thu-muc.md`](../adr/0007-fe-giu-cau-truc-thu-muc.md); bảng dưới là bản tóm để không phải mở ADR mỗi lần tranh luận.

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

---

## 7. Checklist thêm một feature mới

- [ ] Chọn đúng tầng: `platform/` nếu có nghĩa với mọi sản phẩm, `modules/` nếu không. Phân vân → `modules/`.
- [ ] Nếu là module mới: thêm tên vào `BUSINESS_MODULES` (§4.4) và xác nhận zone thật sự chặn.
- [ ] Tạo cấu trúc con đủ `pages/`, `components/`, `services/`, `models/` (§3).
- [ ] `<feature>.routes.ts` export hằng route; `app.routes.ts` chỉ `loadChildren` — [`fe-routing-guard.md`](fe-routing-guard.md).
- [ ] Guard đặt trong route của feature, không rải ở `app.routes.ts`.
- [ ] DTO chỉ nằm trong `models/` và chỉ `services/` import — [`fe-api-client.md`](fe-api-client.md).
- [ ] Mỗi service có `.spec.ts` cạnh nó (luật F12).
- [ ] Không chuỗi tiếng Việt nào trong template (luật F8).
- [ ] Không hex, không `rgb()`/`rgba()` trần trong SCSS (luật F6, F7).
- [ ] Chạy cổng FE, xanh mới coi là xong.

---

## 8. Đối chiếu — luật nào ở đâu

| Luật | Nội dung | Ép bằng | Mục |
| --- | --- | --- | --- |
| F1 | `core/` không import ngược lên | ESLint `coreLayerZones` | §4.2 |
| F2 | `modules/A` không import `modules/B` | ESLint `moduleBoundaryZones` | §4.3 |
| F3 | Cấm `eslint-disable` cho rule ranh giới | Cổng FE | §4.5 |
| F4 | `BUSINESS_MODULES` khớp thư mục thật | Cổng FE | §4.4 |
| F10 | `components/`/`pages/` không import DTO | Cổng FE | [`fe-api-client.md`](fe-api-client.md) |
| F11 | `components/` không inject data service | Cổng FE | §3.1 |
| F12 | Mọi service có `.spec.ts` | Cổng FE | §3 |

Bảng đầy đủ mọi luật kèm cột "ép bằng gì": [`../RULES.md`](../RULES.md) §7.
