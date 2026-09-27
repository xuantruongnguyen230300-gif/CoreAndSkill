---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Kiến trúc Frontend — bốn tầng và hàng rào giữ chúng

> 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG** (đối chiếu 2026-09-21, **chỉ §2.8 và §3**; phần còn lại chưa ai đối chiếu):
>
> | Có thật hôm nay | Sẽ thành |
> | --- | --- |
> | `src/FE/src/app/core/list/list-state.store.ts` — `connect(`, `TEN_DUNG_CHUNG`; luật 9 §2.8 (đối chiếu 2026-09-24): `// Luật 9:`, và `list-state.store.spec.ts` cạnh nó có `describe('luật 9` | — |
> | `src/FE/src/app/platform/he-thong/don-vi/services/don-vi.mapper.ts` — `mapDonVi` ở `services/`; `state/` có ở ba feature của `platform/`, ví dụ `src/FE/src/app/platform/he-thong/don-vi/state/tao-don-vi.store.ts` — `class TaoDonViStore` ([ADR-0049](../adr/0049-trang-thai-quy-trinh-hop-thoai-o-state-khong-o-services.md)) | — |
> | §2.6 luật F39 **đã khớp về chỗ đặt** (đọc mã 2026-09-24): `src/FE/src/app/core/theme/core-animations.ts` có `export function provideCoreAnimations(`; `src/FE/src/app/app.config.ts` chỉ còn dòng nối `provideCoreAnimations(),`; ngoài `*.spec.ts`, không tệp nào khác dưới `src/FE/src/app` khớp `provideAnimationsAsync(` | — cổng: [`../RULES.md`](../RULES.md) F39 |
> | 🚧 §2.3 `platform/loi/` (chốt 2026-09-25): hai page nằm thẳng dưới `src/FE/src/app/platform/loi/` — `khong-co-quyen.page.ts`, `khong-tim-thay.page.ts` — ngoài mọi `pages/`, nên F10 không quét | Dời vào `loi/pages/khong-co-quyen/`, `loi/pages/khong-tim-thay/`; hai `loadComponent` ở `app.routes.ts` đổi cùng lượt |
>
> Lý do đằng sau ranh giới Core ↔ Module (chung cho cả BE lẫn FE): [`../kien-truc-core-module.md`](../kien-truc-core-module.md).

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

**Chiều phụ thuộc chỉ đi xuống.** Một tầng được import mọi tầng dưới nó; không tầng nào được import lên trên. `core/` là tầng đáy — không import gì từ ba tầng còn lại.

Ngang hàng thì tuỳ tầng:

| Tầng | Import ngang hàng |
| --- | --- |
| `core/` | Được — `core/auth` dùng `core/http` là bình thường |
| `shared/` | Được |
| `platform/` | Được — màn hình Core lắp vào nhau là chuyện thường |
| `modules/` | **CẤM.** `modules/A` không import `modules/B` (luật F2) |

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §1

### 1.1 Cây thư mục cấp `src/FE/` — cái gì nằm NGOÀI `src/app/`

```
src/FE/
├── public/                 # tài sản tĩnh copy nguyên trạng vào bundle, ra thẳng GỐC SITE
│   ├── favicon.ico
│   ├── fonts/              # woff2 tự host — không gọi CDN font lúc chạy
│   ├── i18n/               # bảng dịch của Core, nạp lúc chạy — v1 chỉ vi.json (wiki-core/fe/08-i18n.md §2.3)
│   └── i18n-app/           # bảng dịch của dự án, nạp sau và ghi đè được khoá Core — v1 chỉ vi.json
├── src/
│   ├── environments/       # cấu hình COMPILE-TIME: apiBaseUrl, production
│   ├── styles/             # token global + style toàn cục — xem fe-ui-conventions.md
│   ├── index.html
│   ├── main.ts
│   └── app/                # bốn tầng, xem §2
├── .nvmrc                  # phiên bản Node của môi trường build — cùng package.json là nguồn phiên bản
├── angular.json
├── eslint.boundaries.cjs   # mảng BUSINESS_MODULES — eslint.config.js và cổng F4 cùng đọc, §4.3–§4.4
├── eslint.config.js        # hàng rào ranh giới — §4
├── package.json
└── tsconfig*.json
```

Không tạo thư mục thứ năm ở cấp `src/FE/` khi chưa trả lời được: *"thứ này cần lúc build hay lúc chạy?"*

| | `src/environments/*.ts` | `public/*.json` |
| --- | --- | --- |
| Giá trị chốt lúc | **build** | **runtime**, fetch lúc khởi động |
| Đổi giá trị cần | build lại | thay file, không build lại |
| Dùng cho | `apiBaseUrl`, cờ `production` | bảng dịch |

**Không dựng cả hai cơ chế cho cùng một giá trị.**

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §1.1

---

## 2. Mỗi tầng chứa gì, cấm chứa gì

### 2.1 `core/` — hạ tầng toàn app

```
core/
├── auth/            # trạng thái phiên, đăng nhập/đăng xuất, permission signal
├── config/          # InjectionToken seam: CORE_ROUTES, CORE_BRANDING, CORE_I18N
├── guards/          # authGuard, permissionGuard, mustChangePasswordGuard
├── http/            # envelope model, hàm CRUD dùng chung, unwrap, mapper lỗi, dịch mã lỗi (dich-loi.ts)
├── interceptors/    # thứ tự khai ở fe-api-client.md, KHÔNG tuỳ tiện
├── i18n/            # cấu hình ngx-translate, loader, service đổi ngôn ngữ
├── list/            # trạng thái màn danh sách: GridQuery, ListStateStore — §2.8
├── menu/            # menu động theo quyền
├── theme/           # preset PrimeNG, chuyển sáng/tối — cơ chế ở wiki-core/fe/04 §7
├── toast/           # service giữ hàng đợi toast (component Toast bọc PrimeNG nên nằm ở shared/ui/)
└── unsaved-changes/ # hỏi-và-chặn khi rời màn còn thay đổi chưa lưu (fe-routing-guard.md §4.1); hộp thoại ở platform/shell/
```

| Được chứa | **Cấm** chứa |
| --- | --- |
| Service `providedIn: 'root'`, sống suốt vòng đời app | Component có template người dùng nhìn thấy |
| Interceptor, guard, resolver | Bất cứ import nào từ `shared/`, `platform/`, `modules/` |
| Kiểu và hàm thuần dùng bởi hạ tầng | Tên sản phẩm, đường dẫn route cụ thể, bảng màu của một dự án — chúng vào qua **seam** (§2.5) |
| `InjectionToken` khai seam cấu hình | Logic riêng của đúng một feature |

**Phép thử `core/` vs `shared/services/`:** *bỏ hết màn hình đi thì service này còn nghĩa gì không?* Còn → `core/`. Không → `shared/services/`.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §2.1

### 2.2 `shared/` — dùng chung nhưng không phải hạ tầng

```
shared/
├── ui/           # BỌC thư viện UI. Mỏng, không biết nghiệp vụ, không biết API.
├── components/   # dumb UI tái dùng > 1 feature, ghép TỪ shared/ui/
├── forms/        # hạ tầng form dùng chung: gắn fieldErrors vào control, hiển thị lỗi, banner lỗi chung, focus ô sai (chủ: fe-ui-conventions.md §6.2)
├── directives/   # directive dùng chung: appHasPermission, appAutofocus
├── pipes/        # pipe thuần
├── models/       # kiểu dùng chung giữa nhiều feature — KHÔNG phải DTO
└── services/     # service TRẠNG THÁI UI: không gọi HTTP, không giữ phiên
```

`ui/` và `components/` là **hai** thư mục: ranh giới và lý do tách ở [`../wiki-core/fe/05-component-library.md`](../wiki-core/fe/05-component-library.md) §3.

**Chiều phụ thuộc bên trong `shared/`: `components/` được import `ui/`; `ui/` KHÔNG import `components/`.** Lớp bọc cần hiện một component tự dựng thì nhận nó qua **slot** là input `TemplateRef`, và màn ghép component đó vào — `DataTable` với `emptyTemplate`, `loadingTemplate`, `errorTemplate` ([`../Design/Components/DataTable.md`](../Design/Components/DataTable.md); phần template ghép ba slot ở [`ly-do/fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §2.8). Chiều này có cổng: luật F24, zone ESLint ở §4.6.

> ⚠️ **Thư mục vắng mặt trong sơ đồ này là thư mục không cổng nào canh.** Thêm thư mục con vào `shared/` thì thêm dòng ở đây trước.

| Được chứa | **Cấm** chứa |
| --- | --- |
| Component chỉ nhận `input()` và phát `output()` | Component tự inject service lấy dữ liệu (luật F11 — token được tha: [`../wiki-core/fe/trien-khai/05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) §8.8) |
| Bọc component thư viện UI trong `ui/` — xem [`../wiki-core/fe/05-component-library.md`](../wiki-core/fe/05-component-library.md) §2 | Gọi `HttpClient` |
| Kiểu dùng chung giữa nhiều feature | DTO — DTO thuộc feature, xem [`fe-api-client.md`](fe-api-client.md) |
| | Import từ `platform/` hoặc `modules/` — luật F35 (§4.7) |
| | Import `components/` từ bên trong `ui/` — chiều ngược, đoạn ngay trên; luật F24 (§4.6) |

> `shared/` là một phần của **CoreBase**, đi theo khi mang nền tảng sang dự án khác: nó không được biết tên sản phẩm, bảng màu cụ thể hay route nghiệp vụ nào.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §2.2

### 2.3 `platform/` — Màn hình Core — định nghĩa gốc

Màn hình có nghĩa với **mọi** sản phẩm dựng trên nền tảng: đăng nhập, đổi mật khẩu bắt buộc, hồ sơ cá nhân, quản trị người dùng, quản trị vai trò, phân quyền, quản trị đơn vị (khu hệ thống).

**Trang chủ** cũng thuộc `platform/`, nhưng Core chỉ cấp **trang chào tối giản** — tên đơn vị, lời chào, lối tắt tới các màn người dùng có quyền; không gọi endpoint riêng, chỉ dùng lại thông tin phiên và menu.

Trang chủ đi qua seam `CORE_HOME` (§2.5): dự án cấp **hàm nạp lười** component của mình, **không** sửa route và không sửa khung shell. Route trang chủ của Core đọc token ngay trong `loadComponent` (khuôn ở §2.5); không khai thì trang chào của Core. Core cấp **component** biểu đồ ([`../Design/Components/Chart.md`](../Design/Components/Chart.md)); riêng **thư viện** biểu đồ thuộc nhóm thêm-khi-cần, nạp theo yêu cầu ([`../wiki-core/fe/01-core-components.md`](../wiki-core/fe/01-core-components.md) §3, [`../adr/0019-ba-component-nang-thuoc-core.md`](../adr/0019-ba-component-nang-thuoc-core.md) ràng buộc 1).

Mỗi màn ở trên có một card trong [`../contracts/`](../contracts/). **Quản trị menu KHÔNG nằm trong danh sách**: ở v1 menu là dữ liệu seed ([`../contracts/meta-menu.md`](../contracts/meta-menu.md) §2).

Cấu trúc con giống hệt `modules/` (§3); khác biệt duy nhất là **ý nghĩa**.

`platform/` giữ thêm ba thư mục không phải feature:

| Thư mục | Chứa gì |
| --- | --- |
| `platform/loi/` | Hai trang lỗi trong khung, `khong-co-quyen` và `khong-tim-thay` — không `*.routes.ts`, không service. Page vẫn nằm ở `loi/pages/<ten-page>/` như §3, để F10 quét tới (🚧 bảng đầu file) |
| `platform/shell/` | **Khung ứng dụng — smart.** Inject phiên và menu, rồi truyền dữ liệu xuống `Sidebar` và `Topbar` qua `input()`. Hai component đó **dumb**, tự dựng ở `shared/components/` — luật F11 áp cho chúng như mọi component khác, không ngoại lệ |
| `platform/config/` | Seam `CORE_SCREEN_EXT` (§2.7). Đặt ở đây vì kiểu dòng của nó là model của màn `platform/`, mà `core/` không được import `platform/` (luật F1) |

Phân vân `platform/` hay `modules/`, hỏi: *"màn này có ý nghĩa với sản phẩm tiếp theo dựng trên nền tảng này không?"* Có → `platform/`. Không → `modules/`.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §2.3

### 2.4 `modules/` — nghiệp vụ riêng dự án

```
modules/
├── <ten-module-a>/
└── <ten-module-b>/
```

Mỗi thư mục con là **một domain nghiệp vụ**, lazy-loaded, tên trùng với tên khai trong mảng `BUSINESS_MODULES` của `eslint.boundaries.cjs` (§4.3–§4.4).

Tương ứng phía BE là một `Modules.<X>.*`; ranh giới khi nào tách module mới ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.3.

### 2.5 Seam — `core/` giữ CƠ CHẾ, app cấp DỮ LIỆU

`core/` và `shared/` mang đi được sang dự án khác, nên **mọi dữ liệu riêng của một dự án** phải đi vào chúng qua seam, không được khai cứng bên trong.

| Seam | Cấp gì cho `core/` | Chi tiết ở |
| --- | --- | --- |
| `CORE_ROUTES` | Đường dẫn mà guard chuyển hướng tới | [`fe-routing-guard.md`](fe-routing-guard.md) |
| `CORE_BRANDING` | Tên sản phẩm, chữ tắt | mục này |
| `CORE_HOME` | **Hàm nạp lười** component trang chủ; không khai ⇒ trang chào của Core | §2.3, khuôn ngay dưới |
| `CORE_I18N` | Danh sách ngôn ngữ, ngôn ngữ mặc định, và các nguồn tệp dịch theo thứ tự nạp | [`../wiki-core/fe/08-i18n.md`](../wiki-core/fe/08-i18n.md) §2.3 |
| `API_BASE_URL` | Base URL của API, cấp từ `environment.apiBaseUrl` | [`fe-api-client.md`](fe-api-client.md) §2.1 |
| Bảng màu | **Không đi qua DI.** Giá trị nằm ở `src/styles/_tokens.scss` theo `Design/`; preset PrimeNG ở `core/theme/` chỉ đọc `var(--color-*)` | [`../wiki-core/fe/04-design-token-system.md`](../wiki-core/fe/04-design-token-system.md) §7 |
| `CORE_SCREEN_EXT` | **Cột dự án thêm vào màn danh sách Core.** Token nằm ở `platform/config/`, **không** ở `core/` | §2.7 |

Khuôn bắt buộc, không phát minh khuôn thứ hai:

```typescript
// core/config/core-branding.ts — hợp đồng, KHÔNG có giá trị mặc định; khối import ở ly-do §2.5
export interface CoreBranding {
  readonly name: string;      // tên đầy đủ: hậu tố <title>, dòng chữ ở sidebar
  readonly shortName: string; // chữ tắt trong ô vuông thương hiệu
}

export const CORE_BRANDING = new InjectionToken<CoreBranding>('CORE_BRANDING');

export function provideCoreBranding(branding: CoreBranding): EnvironmentProviders {
  return makeEnvironmentProviders([{ provide: CORE_BRANDING, useValue: branding }]);
}

// core/config/core-i18n.ts — cùng khuôn; nguồn tệp dịch theo thứ tự nạp: wiki-core/fe/08-i18n.md §2.3
/** Khai ở core vì CORE_I18N cấp nó (F1); shared/ui/types re-export cho LanguageSwitcher — fe-ui-conventions.md §9. */
export interface LanguageOption {
  readonly code: string;        // mã BCP 47
  readonly nativeName: string;  // tên viết bằng CHÍNH ngôn ngữ đó
  readonly localeData: unknown; // import tĩnh '@angular/common/locales/<mã>' ở composition root — ADR-0063
}

export interface CoreI18n {
  readonly languages: ReadonlyArray<LanguageOption>; // một mục ⇒ LanguageSwitcher không render
  readonly defaultLanguage: string;                  // MÃ — phải là `code` của một mục trong languages
  readonly sources: readonly string[];               // đường dẫn thư mục tệp dịch, tầng sau ghi đè tầng trước
}

export const CORE_I18N = new InjectionToken<CoreI18n>('CORE_I18N');

/** Luật F36: LOCALE_ID và registerLocaleData CHỈ ở đây — fe-ui-conventions.md §5.5. */
export function provideCoreI18n(i18n: CoreI18n): EnvironmentProviders {
  for (const ngonNgu of i18n.languages) {
    registerLocaleData(ngonNgu.localeData, ngonNgu.code); // MỌI ngôn ngữ khai, không riêng mặc định
  }
  return makeEnvironmentProviders([
    { provide: CORE_I18N, useValue: i18n },
    { provide: LOCALE_ID, useValue: i18n.defaultLanguage },
  ]);
}

// core/config/core-home.ts — cùng khuôn; hàm nạp lười, không phải Type (ADR-0057)
export type CoreHomeLoader = () => Promise<Type<unknown>>;

export const CORE_HOME = new InjectionToken<CoreHomeLoader>('CORE_HOME');

export function provideCoreHome(load: CoreHomeLoader): EnvironmentProviders {
  return makeEnvironmentProviders([{ provide: CORE_HOME, useValue: load }]);
}

// platform/trang-chu/trang-chu.routes.ts — `loadComponent` chạy trong ngữ cảnh tiêm
const TRANG_CHAO_CORE: CoreHomeLoader = () =>
  import('./pages/trang-chu/trang-chu.page').then((m) => m.TrangChuPage);

export const TRANG_CHU_ROUTES: Routes = [
  {
    path: '',
    title: 'trangChu.tieuDe',
    loadComponent: () => (inject(CORE_HOME, { optional: true }) ?? TRANG_CHAO_CORE)(),
  },
];
```

**Token cố ý không có `factory` mặc định.** `CORE_HOME` cũng vậy nhưng đọc bằng `{ optional: true }` (ly-do §2.5). Route trang chủ phải có test chạy `loadComponent` thật.

Phép thử cho seam mới: *"giá trị này có đổi khi dựng sản phẩm khác trên cùng nền tảng không?"* Có → seam. Không → để trong `core/`.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §2.5

### 2.6 Composition root — nơi mọi seam được nối lại

Composition root phía FE (tương ứng "host mỏng" ở `be-architecture.md` §3) là **file cấu hình app**, nơi khai toàn bộ provider gốc.

**Luật:** `useExisting` khi hai token phải trỏ về **cùng một** thể hiện; `useClass` chỉ khi thật sự cần thể hiện thứ hai. Mặc định là `useExisting`.

**Luật:** mỗi bước khởi tạo chạy trước khi app dựng phải khai rõ **nó phụ thuộc bước nào**. Hai bước độc lập chạy song song được.

**Luật F39:** composition root **không** khai provider hoạt ảnh của Angular. Nó gọi `provideCoreAnimations()` của `core/`, và hàm đó tự chọn bộ chạy theo cài đặt giảm chuyển động của hệ điều hành. Nửa provider của giảm chuyển động là nghĩa vụ trợ năng của các component Core, nên thân của nó phải nằm trong `core-paths` — cùng lý do `LOCALE_ID` đi qua `provideCoreI18n` ([`../adr/0063-locale-id-den-tu-seam-core-i18n.md`](../adr/0063-locale-id-den-tu-seam-core-i18n.md)). Chọn API nào và vì sao: [`../adr/0080-nua-provider-giam-chuyen-dong-nap-luoi-va-song-o-core.md`](../adr/0080-nua-provider-giam-chuyen-dong-nap-luoi-va-song-o-core.md).

> 📖 Danh sách interceptor và thứ tự: [`fe-api-client.md`](fe-api-client.md) — file này không chép lại.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §2.6

---

### 2.7 Seam cho MÀN Core — định nghĩa gốc

Hợp đồng `CORE_SCREEN_EXT`:

```typescript
// platform/config/core-screen-ext.ts — hợp đồng, KHÔNG có giá trị mặc định; khối import ở ly-do §2.7
/** Một cột dự án THÊM vào màn danh sách Core. Khai ở composition root — không có view, nên không TemplateRef. */
export interface ScreenExtColumn<T> {
  readonly key: string;                   // duy nhất trong màn, không trùng khoá cột của Core
  readonly headerKey: string;             // KHOÁ i18n — màn Core dịch khi dựng cột; không phải chuỗi đã dịch
  readonly value: (row: T) => string;     // chuỗi ĐÃ định dạng; đọc signal thì ô tự cập nhật
  readonly width?: string;
  readonly align?: 'start' | 'end';
  readonly hideBelow?: 'xs' | 'sm' | 'md' | 'lg';
  readonly priority?: 'high' | 'low';
}                                         // không `sortable`: sortBy chỉ nhận allowlist của endpoint Core

/** Phần một dự án được phép THÊM vào một màn Core. Không có gì cho phép BỚT. */
export interface ScreenExtension<T> {
  readonly columns?: ReadonlyArray<ScreenExtColumn<T>>; // nối vào SAU cột của Core
}

/** Khoá là mã MÀN DANH SÁCH Core, kiểu dòng theo từng màn — khoá gõ sai là lỗi biên dịch. */
export interface CoreScreenExtensions {
  readonly users?: ScreenExtension<NguoiDung>;
  readonly roles?: ScreenExtension<VaiTro>;
  readonly tenants?: ScreenExtension<DonVi>;
}

export const CORE_SCREEN_EXT = new InjectionToken<CoreScreenExtensions>('CORE_SCREEN_EXT');

/** `factory` chạy trong ngữ cảnh tiêm — `value(row)` đọc được service của dự án. */
export function provideCoreScreenExt(factory: () => CoreScreenExtensions): EnvironmentProviders {
  return makeEnvironmentProviders([{ provide: CORE_SCREEN_EXT, useFactory: factory }]);
}
```

Màn Core đổi mỗi `ScreenExtColumn` thành một `DataColumnDef` có `header` đã dịch và `value` ([`fe-ui-conventions.md`](fe-ui-conventions.md) §9).

> 📐 Phần chưa thi công: [fe-architecture-chua-thi-cong.md](fe-architecture-chua-thi-cong.md)

Luật của seam này:

1. **Chỉ THÊM, không BỚT.** Không có `hiddenColumns`, không có `removeActions`.
2. **Cột thêm nối vào SAU cột dữ liệu của Core, không chen giữa.** Ngoại lệ có tên: *cột hành động* vẫn đứng cuối ([`../Design/Components/DataTable.md`](../Design/Components/DataTable.md)), cột dự án ngay trước nó.
3. **Khoá là mã màn, không phải đường dẫn route.**
4. **Token không có `factory` mặc định** (cùng lý do với `CORE_BRANDING` ở §2.5), nhưng dự án **không** bắt buộc khai seam này. Màn Core đọc bằng `inject(CORE_SCREEN_EXT, { optional: true })?.<mã màn>`, không ép kiểu; kết quả `null` hoặc không có khoá của mình ⇒ chạy đúng bản gốc, không lỗi.
5. **Mọi màn danh sách Core trong `platform/` tự đọc token và tự nối phần mở rộng** theo mã màn của mình. Phạm vi seam là màn danh sách; màn hồ sơ và các màn form không có khoá. Cổng: F25 ở [`../RULES.md`](../RULES.md) §7.
6. **Khoá cột trùng là lỗi cấu hình — màn ném `Error` khi khởi tạo**, ở dev lẫn production: cột dự án trùng khoá cột Core, hoặc hai cột dự án trùng nhau. Thông báo nêu mã màn và khoá. Không bỏ qua cột, không đè cột Core. Phép kiểm ở **một** chỗ cho mọi màn: `platform/config/cot-mo-rong.ts` — [ADR-0084](../adr/0084-khoa-cot-mo-rong-trung-thi-man-core-nem-loi-khi-khoi-tao.md).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §2.7

### 2.8 Trạng thái màn danh sách — định nghĩa gốc

[`../wiki-core/fe/11-grid-and-metadata.md`](../wiki-core/fe/11-grid-and-metadata.md) §3–§4 mô tả **cái gì** phải làm; mục này chốt **ai** làm và **làm thế nào**: đúng **một** mẫu, `ListStateStore` ở `core/list/`. Mọi màn danh sách — `platform/` lẫn `modules/` — dùng mẫu này; không màn nào tự viết lại vòng *đọc URL → gọi API → đổi trạng thái*.

Hiện thực nằm ở tệp nguồn, tài liệu không giữ bản sao: `src/FE/src/app/core/list/grid-query.ts` — `export interface GridQuery extends PageQuery` (bộ lọc riêng của endpoint ở `filters`, mỗi khoá một tham số rời); `src/FE/src/app/core/list/list-state.store.ts` — `export class ListStateStore<T>`, hằng số `NGUNG_GO_MS` và `PAGE_SIZE_MAC_DINH` kèm lý do ngay cạnh. Store phát `query` (bản đọc của URL), `state` (`idle` · `loading` · `error` · `empty` · `empty-filtered` — cái cuối khi đang có từ khoá hoặc bộ lọc), `rows`, `totalCount`; màn đổi truy vấn qua `setPage`, `setSort`, `setSearchText`, `setFilters`, `reload`. `connect()` gọi đúng một lần mỗi màn — gọi lại thì ném lỗi. Giá trị mặc định không nằm trên URL. Lỗi tải chỉ đổi `state` sang `error`; hiện lỗi là việc của `errorInterceptor`. `rows` rỗng không có nghĩa *không có dữ liệu* — `state` mới mang nghĩa đó.

Màn danh sách dùng mẫu này — phần `.ts` dưới đây; phần template (bind `Toolbar`, `DataTable` và ba slot) ở [`ly-do/fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §2.8:

```typescript
// platform/quan-tri/nguoi-dung/pages/danh-sach/danh-sach-nguoi-dung.page.ts — khối import ở ly-do §2.8
@Component({
  selector: 'app-danh-sach-nguoi-dung',
  standalone: true,
  imports: [TranslateModule, ToolbarComponent, DataTableComponent, EmptyStateComponent, SkeletonLoaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ListStateStore], // luật 7 — một thực thể cho MỘT màn
  templateUrl: './danh-sach-nguoi-dung.page.html',
})
export class DanhSachNguoiDungPage {
  private readonly service = inject(NguoiDungService);

  protected readonly list = inject<ListStateStore<NguoiDung>>(ListStateStore)
    .connect((q) => this.service.danhSach(q));
}
```

Luồng tải qua `connect()`: page **không** `catchError`, `subscribe`, `try/catch` — [`fe-api-client.md`](fe-api-client.md) §3; tra cứu phụ: §6.1.

**Phân trang bind lên `DataTable`; màn không dựng thêm dải phân trang thứ hai.** Chủ hợp đồng phân trang là `DataTable` ([`../Design/Components/DataTable.md`](../Design/Components/DataTable.md)); [`../Design/Components/Pagination.md`](../Design/Components/Pagination.md) giữ hình thức và accessibility của dải đó.

Luật của tầng này:

1. **Truy vấn sống trên URL, và URL là nguồn sự thật.** Mọi thay đổi đi qua setter rồi lên URL; không set thẳng vào signal, và không ghi lại URL sau khi tải xong (bẫy vòng lặp ở [`../wiki-core/fe/11-grid-and-metadata.md`](../wiki-core/fe/11-grid-and-metadata.md) §3.3) — ngoại lệ duy nhất là luật 9.
2. **Tên trên URL = tên trên dây = tên trong `GridQuery`.** Query param đọc từ URL được kiểm: sai dạng thì bỏ; đúng dạng mà vượt khoảng của hợp đồng thì BE trả 400 và màn vào `error` — không vá âm thầm ([`../contracts/README.md`](../contracts/README.md) §8).
3. **Đổi bộ lọc hoặc từ khoá thì `page` về 1.**
4. **Ba cơ chế chống gọi dồn dập, cả ba ở store.** Chờ ngừng gõ (`setSearchText`), bỏ qua truy vấn trùng, và **huỷ kết quả của request cũ khi request mới đã đi**. `Toolbar` phát `searchChanged` ở mọi lần gõ, không chờ bên trong. **Ngưỡng chờ là hằng số `NGUNG_GO_MS` của store**, lý do ghi ngay cạnh hằng số — không phải input của `Toolbar`, không phải tham số của `connect()`.
5. **`replaceUrl` cho thay đổi liên tục, không cho thay đổi rời rạc.** Gõ vào ô tìm thay URL tại chỗ; đổi trang, sắp xếp, bộ lọc thì thêm một bước lịch sử.
6. **`state` là một biến, không phải bốn cờ** (lý do ở `DataTable.md`).
7. **Một thực thể cho một màn, cấp ở cấp route — trong `providers` của chính page mà route trỏ tới.**
8. **`tab` là tên dùng chung, không phải bộ lọc.** Store bỏ qua nó (`TEN_DUNG_CHUNG`), không gửi lên API; `queryParamsHandling: 'merge'` giữ nó khi store ghi URL. Trang cha tự ghi và đọc `tab` qua `Router` / `queryParamMap` của route; màn không có store cũng đọc thẳng `queryParamMap` — không cấp `ListStateStore` chỉ để có `tab` ([`../Design/Components/Tabs.md`](../Design/Components/Tabs.md)).
9. **Trang vượt trang cuối thì về trang cuối.** `items` rỗng, `totalCount > 0`, `page > 1` ⇒ store ghi `page = min(page − 1, ⌈totalCount / pageSize⌉)` bằng `replaceUrl`, không đổi `state` khỏi `loading`, không giữ kết quả rỗng. `page` giảm ngặt nên dừng chắc chắn. Test đơn vị của store khoá ca này. Chốt 2026-09-24.

🛑 **Tầng này KHÔNG inject `HttpClient` và không biết endpoint.** Màn truyền hàm tải của service mình qua `connect()`; mapper DTO → model vẫn nằm ở service ([`fe-api-client.md`](fe-api-client.md) §4).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §2.8

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
| `components/*` | nhận `input()`, phát `output()`, render | inject service lấy dữ liệu (luật F11); biết HTTP; import DTO; import/nhận store (F33) |
| `services/*` | chỉ `*.service.ts`, `*.mapper.ts`: gọi API, map DTO ↔ model, hủy/retry | giữ trạng thái UI (mở/đóng dialog, tab đang chọn); tệp khác đuôi (F31) |
| `state/*.store.ts` | `signal`/`computed`, điều phối service, quy trình hộp thoại ([ADR-0049](../adr/0049-trang-thai-quy-trinh-hop-thoai-o-state-khong-o-services.md)); có `*.store.spec.ts` (F32) | render, đụng DOM |
| `models/*` | type, interface, hằng số của miền | logic, gọi hàm, mapper |

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §3.1

### 3.2 Khi nào cần `state/`

**Không** tạo store mặc định cho mọi feature. Chỉ thêm khi có ít nhất một trong:

- Nhiều page/component trong cùng feature cần đọc-ghi chung một state.
- State phải derive qua nhiều bước `computed()` lồng nhau.
- Cần giữ cache giữa các lần điều hướng qua lại.

Feature một page, state cục bộ → khai `signal()` thẳng trong page.

> 📖 Store viết tay bằng `signal()`, chưa dùng NgRx ở v1; ngưỡng cân nhắc thư viện; khuôn store: [`../wiki-core/fe/03-state-management.md`](../wiki-core/fe/03-state-management.md) §2–§4 (file chủ).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §3.2

---

## 4. Ranh giới ép bằng ESLint — mục quan trọng nhất của file này

### 4.1 Vì sao mục này dài hơn mọi mục khác

> 📖 Lý do, bẫy, ví dụ mở rộng — gồm bảng "bốn luật đi thành một bộ, thiếu một thì mất hiệu lực": [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §4.1

### 4.2 Zone `coreLayerZones` — `core/` là tầng đáy

```typescript
// eslint.config.js
// Luật F1 — core/ là tầng đáy, không import ngược lên ba tầng trên.
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

Nối vào config cho file trong `core/`:

```typescript
{
  files: ['src/app/core/**/*.ts'],
  plugins: { import: importPlugin },
  settings: {
    // Phải khai `.ts` — thiếu thì rule không phân giải được import và im lặng không chặn gì.
    'import/resolver': { node: { extensions: ['.ts', '.js'] } },
  },
  rules: {
    'import/no-restricted-paths': ['error', { zones: coreLayerZones }],
  },
}
```

**Canary bắt buộc khi thi công:** viết một file vi phạm cố ý (import từ `platform/` vào `core/`), xác nhận lint đỏ đúng thông điệp, rồi hoàn nguyên (luật T1, [`../RULES.md`](../RULES.md) §8).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §4.2

### 4.3 Zone `moduleBoundaryZones` — module không import chéo

```javascript
// eslint.boundaries.cjs — nơi DUY NHẤT khai tên module nghiệp vụ; eslint.config.js và cổng F4 cùng đọc (§4.4).
// Thêm module mới → thêm ĐÚNG tên thư mục vào mảng này.
module.exports = { BUSINESS_MODULES: [] };
```

```typescript
// eslint.config.js — không viết tay một zone mới; zone sinh ra từ mảng.
const { BUSINESS_MODULES } = require('./eslint.boundaries.cjs');

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

Khối rule phải **bị bỏ hẳn** khi mảng rỗng:

```typescript
// 🛑 `zones` phải có TỐI THIỂU một phần tử — mảng rỗng làm ESLint chết lúc nạp config ("Invalid Options").
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

Zone chỉ áp cho `src/app/modules/**` — **không** áp cho `platform/`: màn hình Core được phép lắp vào nhau (chủ đích).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §4.3

### 4.4 Quy trình thêm một module mới — bốn bước, không bỏ bước nào

1. Tạo `src/app/modules/<ten-module>/` theo cấu trúc §3.
2. **Thêm tên module vào mảng `BUSINESS_MODULES` trong `eslint.boundaries.cjs`.**
3. Đăng ký lazy route trong `app.routes.ts` — xem [`fe-routing-guard.md`](fe-routing-guard.md).
4. Chạy cổng FE và xác nhận zone thật sự chặn: tạm viết một import chéo, thấy lint đỏ, rồi xoá.

Bước 2 hay bị quên; cổng phải bắt:

```bash
# Luật F4 — BUSINESS_MODULES khớp thư mục modules/ thật. PASS khi hai TẬP tên giống hệt nhau; rỗng khớp rỗng là PASS
# (ADR-0036). Kiểm exit code của `node -p` TRƯỚC khi diff — cấu hình hỏng không được lẫn với tập rỗng.
[ -f src/FE/eslint.boundaries.cjs ] || { echo "F4: không có src/FE/eslint.boundaries.cjs"; exit 1; }
if [ -d src/FE/src/app/modules ]; then FOLDERS=$(ls -1 src/FE/src/app/modules | sort); else FOLDERS=""; fi
CONFIG_RAW=$(node -p "require(require('path').resolve(process.argv[1])).BUSINESS_MODULES.join('\n')" \
  src/FE/eslint.boundaries.cjs 2>&1)
[ $? -eq 0 ] || { echo "F4: eslint.boundaries.cjs lỗi khi đọc BUSINESS_MODULES:"; printf '%s\n' "$CONFIG_RAW"; exit 1; }
CONFIG=$(printf '%s\n' "$CONFIG_RAW" | sort)
diff <(printf '%s\n' "$FOLDERS") <(printf '%s\n' "$CONFIG")
```

> 📖 Vì sao tập rỗng là PASS và là ngoại lệ có tên của riêng F4: [`../adr/0036-f4-rong-khop-rong-la-hop-le.md`](../adr/0036-f4-rong-khop-rong-la-hop-le.md).

> **Không được** chép danh sách module vào script cổng ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §5).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §4.4

### 4.5 Cấm `eslint-disable` cho rule ranh giới — luật F3

#### Danh sách rule cấm tắt — định nghĩa gốc

Bảng này là **Danh sách rule cấm tắt — định nghĩa gốc**, nguồn duy nhất của tập rule mà `eslint-disable` không được chạm tới. `scripts/fe-gate.sh` đọc bảng lúc chạy, không giữ mảng thứ hai — lệnh ở [`../wiki-core/fe/trien-khai/05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) §8.1; tài liệu khác **không** chép lại bảng, chỉ trỏ về mục này ([`../OWNERSHIP.md`](../OWNERSHIP.md) §2).

| Rule | Canh luật |
| --- | --- |
| `import/no-restricted-paths` | F1, F2, F24, F35 |
| `@angular-eslint/template/prefer-control-flow` | F9 (lớp hai) — bật mức `error` trong khối `files: ['**/*.html']` của `src/FE/eslint.config.js`; bắt cả `<ng-template [ngIf]>` mà mẫu grep của F9 không thấy. Canary `scripts/tests/fe-lint-f9.test.sh`, đối chiếu 2026-09-23 |
| `@angular-eslint/template/alt-text` | F17 |
| `@angular-eslint/template/click-events-have-key-events` | F17 |
| `@angular-eslint/template/elements-content` | F17 |
| `@angular-eslint/template/interactive-supports-focus` | F17 |
| `@angular-eslint/template/label-has-associated-control` | F17 |
| `@angular-eslint/template/mouse-events-have-key-events` | F17 |
| `@angular-eslint/template/no-autofocus` | F17 |
| `@angular-eslint/template/no-distracting-elements` | F17 |
| `@angular-eslint/template/role-has-required-aria` | F17 |
| `@angular-eslint/template/table-scope` | F17 |
| `@angular-eslint/template/valid-aria` | F17 |
| Rule ranh giới bổ sung khi có | — |

**Các dòng F17 liệt kê theo bộ rule tiếp cận cho template của angular-eslint** ([`../wiki-core/fe/15-accessibility.md`](../wiki-core/fe/15-accessibility.md) §6, [`fe-ui-conventions.md`](fe-ui-conventions.md) §8). **Tên chốt khi F0 theo phiên bản angular-eslint cài thật** — đối chiếu bảng với bộ rule của phiên bản đó, cùng lượt dựng cấu hình lint.

**F13 không có dòng ở bảng này** — `ng build` ép nó.

> ⚠️ **Ánh xạ rule → luật phải đúng tên rule, không đúng "trông có vẻ liên quan".**

> 📖 Lệnh quét dạng tắt **theo tên rule**: [`../wiki-core/fe/trien-khai/05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) §8.1 — mục này **không** giữ bản lệnh thứ hai

Dạng tắt **không kèm tên rule** — cả ba từ khoá, ở cả ba kiểu comment `//`, `/* */`, `<!-- -->` — không có chuỗi để đọc từ bảng, nên cần mẫu riêng:

```bash
# Luật F3 — dạng tắt KHÔNG kèm tên rule; cũng cấm, vì nó tắt mọi rule, kể cả rule ranh giới.
# Sau từ khoá chỉ còn khoảng trắng rồi hết dòng, `*/`, `-->` hoặc phần mô tả `-- ...`.
# PASS khi không in ra dòng nào.
[ -d src/FE/src ] || { echo "F3: không có src/FE/src để quét"; exit 1; }
grep -rnE 'eslint-disable(-next-line|-line)?[[:space:]]*($|\*/|-->|--[[:space:]])' \
  src/FE/src --include='*.ts' --include='*.html'
```

Hai mẫu theo dòng — mẫu trên và mẫu theo tên rule ở [`05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) §8.1 — đều mù trước hai dạng tắt khác mà ESLint vẫn nhận. Một là **cấu hình nội tuyến**: `/* eslint import/no-restricted-paths: "off" */` trong `.ts`, `<!-- eslint … -->` trong `.html` và trong template nội tuyến của `.ts`. Hai là `eslint-disable` dạng khối **trải nhiều dòng**, tên rule cấm nằm ở dòng sau. Thử bằng `npx eslint --stdin` thì cả hai đều thoát 0 dù có import vi phạm ranh giới. Mẫu riêng cho chúng đọc **cả comment**, không đọc theo dòng:

```bash
# Luật F3 — comment mang nhãn `eslint` hoặc `eslint-disable…` nhắc tên một rule cấm tắt ở BẤT KỲ
# dòng nào của nó: `/* */`, `<!-- -->` (kể cả trong .ts) và `//`. $RULES là tập đọc từ bảng trên
# bằng lệnh ở 05-gate.md §8.1. PASS khi không in ra dòng nào.
[ -n "$RULES" ] || { echo "F3: RULES rỗng — chạy lệnh đọc bảng trước"; exit 1; }
[ -d src/FE/src ] || { echo "F3: không có src/FE/src để quét"; exit 1; }
find src/FE/src \( -name '*.ts' -o -name '*.html' \) -print0 \
  | F3_RULES="$RULES" xargs -0 -r perl -0777 -ne '
      my $cam = join "|", map { quotemeta } split /\|/, $ENV{F3_RULES};
      for my $mau (qr{/\*\s*eslint(?:-disable(?:-next-line|-line)?)?(?=\s|\*/)(.*?)(?:\*/|\z)}s,
                   qr{<!--\s*eslint(?:-disable(?:-next-line|-line)?)?(?=\s|-->)(.*?)(?:-->|\z)}s,
                   qr{//[ \t]*eslint(?:-disable(?:-next-line|-line)?)?(?=\s)([^\n]*)}) {
        while (/$mau/g) {
          my ($than, $vt) = ($1, $-[0]);
          next unless $than =~ /$cam/;
          my $dong = 1 + (substr($_, 0, $vt) =~ tr/\n//);
          my ($txt) = substr($_, $vt) =~ /\A([^\r\n]*)/;
          print "$ARGV:$dong:$txt\n";
        }
      }'
```

Ba điểm phải giữ khi thi công cổng:

1. **Quét cả ba dạng** `eslint-disable`, `eslint-disable-next-line`, `eslint-disable-line` — **và** dạng cấu hình nội tuyến `eslint <rule>: …`.
2. **Quét cả dạng tắt toàn file.**
3. **Cổng phải có test của chính nó.** Viết một file canary chứa đúng dòng bị cấm, chạy cổng, xác nhận đỏ, rồi xoá. Bộ canary giữ lại: `scripts/tests/fe-gate-f3.test.sh`.

**Khi thật sự cần một ngoại lệ:** không tắt rule — sửa thiết kế (nâng thứ dùng chung lên `shared/` hoặc `core/`, hoặc cho hai module nói chuyện qua tầng dưới). Nếu ranh giới sai chứ không phải code sai thì **sửa ADR**, không sửa comment: mở [`../adr/0007-fe-giu-cau-truc-thu-muc.md`](../adr/0007-fe-giu-cau-truc-thu-muc.md), ghi lý do, chốt lại luật, rồi sửa cấu hình cho khớp.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §4.5

### 4.6 Zone `sharedUiZones` — `shared/ui/` không import `shared/components/` — luật F24

Cùng rule, cùng plugin với §4.2:

```typescript
// eslint.config.js
// Luật F24 — shared/ui/ là lớp bọc thư viện; nó không import component tự dựng ở shared/components/.
const sharedUiZones = [
  {
    target: './src/app/shared/ui',
    from: './src/app/shared/components',
    message:
      'shared/ui/ không được import shared/components/. Lớp bọc cần hiện một component tự dựng ' +
      'thì nhận nó qua input TemplateRef, và màn ghép component đó vào.',
  },
];
```

Mảng này nối vào khối `shared/**` của §4.7 cùng `sharedLayerZones`, không đứng khối riêng; khối đầy đủ ở ly-do §4.6.

> ⚠️ **Khối này không được chồng `files` lên khối của F1 hay F2.** Cần canh thêm một chiều cho cây đã có khối thì nối zone vào mảng của khối đó.

**Canary bắt buộc khi thi công**, cùng cách §4.2: tạm import một component từ `shared/components/` vào một file trong `shared/ui/`, thấy lint đỏ đúng thông điệp, rồi hoàn nguyên.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §4.6

### 4.7 Zone `sharedLayerZones` và `platformLayerZones` — hai tầng giữa chỉ nhìn xuống — luật F35

`shared/` không import `platform/` hay `modules/`; `platform/` không import `modules/` (§1, §2.2). Cùng rule với §4.2:

```typescript
// eslint.config.js — luật F35
const sharedLayerZones = ['platform', 'modules'].map((tang) => ({
  target: './src/app/shared',
  from: `./src/app/${tang}`,
  message: `shared/ không được import ${tang}/ (fe-architecture.md §4.7).`,
}));

const platformLayerZones = [
  {
    target: './src/app/platform',
    from: './src/app/modules',
    message: 'platform/ không được import modules/ (fe-architecture.md §4.7).',
  },
];
```

Hai khối: `files: ['src/app/shared/**/*.ts']` với `zones: [...sharedLayerZones, ...sharedUiZones]` — **thay** khối riêng của `shared/ui/` (cảnh báo §4.6) — và `files: ['src/app/platform/**/*.ts']` với `platformLayerZones`.

**Canary:** chiều `shared/` bằng `npx eslint --stdin`; chiều `platform/` → `modules/` bằng fixture tạm theo khuôn `scripts/tests/` — ly-do §4.7.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §4.7

---

## 5. Ngưỡng kích thước file

| Loại file | Ngưỡng mềm | Ngưỡng cứng | Tách thế nào khi vượt |
| --- | --- | --- | --- |
| `*.page.ts` (smart) | 250 | **400** | Tách khối UI ra `components/` con; đẩy logic gọi API xuống `services/`; state phức tạp thì lên `state/` |
| `*.component.ts` (dumb) | 150 | **250** | Component dumb vượt ngưỡng gần như luôn là hai component bị dính; tách theo trục "phần nào tái dùng riêng được" |
| `*.service.ts` | 200 | **300** | Tách mapper ra file riêng; tách theo nhóm endpoint nếu service phục vụ nhiều thực thể |
| `*.html` | 150 | **250** | Template dài là triệu chứng, không phải bệnh — cắt cùng lúc với file `.ts` của nó |
| `*.scss` (component) | 100 | **200** | Style component vượt ngưỡng nghĩa là đang tự vẽ lại thứ đã có trong `shared/`, hoặc đang override thư viện — xem [`fe-ui-conventions.md`](fe-ui-conventions.md) |
| `styles/*.scss` (global) | 200 | **300** | Tách theo nhóm: token màu · typography · spacing · override thư viện |

**Ngưỡng mềm** là lúc dừng lại tự hỏi; **ngưỡng cứng** là lúc cổng đỏ.

**Cả hai cột ngưỡng đo bằng số DÒNG MÃ** ([`../adr/0081-nguong-kich-thuoc-fe-do-bang-dong-ma-dong-tho-chi-note.md`](../adr/0081-nguong-kich-thuoc-fe-do-bang-dong-ma-dong-tho-chi-note.md)). Cổng F22 đếm hai số cho mỗi tệp: **dòng mã** vượt ngưỡng cứng là **FAIL**; **dòng thô** — mọi dòng của tệp — vượt ngưỡng cứng mà dòng mã không vượt thì chỉ in `NOTE`.

**Phân loại một dòng — định nghĩa gốc.** Bộ bóc của cổng dựng theo đúng đoạn này:

- **Dòng trống:** chỉ chứa khoảng trắng, kể cả `\r`.
- **Dòng chú thích:** không trống, và sau khi bỏ khoảng trắng hai đầu thì **hoặc** bắt đầu bằng dấu chú thích dòng, **hoặc** thuộc một chú thích khối mà **dòng mở của khối bắt đầu bằng dấu mở khối**. Khối kéo dài tới dòng đầu tiên chứa dấu đóng. Dòng mở và dòng đóng chỉ là dòng chú thích khi ngoài phần chú thích không còn ký tự nào khác khoảng trắng — `/* a */ f();` là dòng mã.
- **Dòng mã:** mọi dòng không trống còn lại — kể cả `f(); // x`, và kể cả dòng nằm trong một khối mở **giữa dòng** sau mã: bộ bóc không theo dõi khối đó.
- **Cú pháp theo đuôi tệp:** `*.ts` — dòng `//`, khối `/*` … `*/` (gồm `/**`). `*.scss` — dòng `//`, khối `/*` … `*/`. `*.html` — không có chú thích dòng, khối `<!--` … `-->`. Đuôi nào có ở cột *Loại file* mà chưa có ở đây thì cổng **đỏ**, không đoán.

Mọi chỗ mơ hồ nghiêng về đếm là **mã**: dấu chú thích chỉ có hiệu lực khi đứng đầu dòng, nên dấu nằm trong chuỗi ký tự giữa dòng không mở được chú thích. Điểm mù đã biết theo chiều ngược lại: một dòng của chuỗi nhiều dòng bắt đầu bằng dấu mở chú thích bị đếm là chú thích.

**Bảng này là đầu vào của cổng F22** ([`../RULES.md`](../RULES.md) §7). Cổng đọc cả danh sách loại tệp lẫn cột *Ngưỡng cứng* từ đây lúc chạy, nên sửa một ô là đổi cổng, và không nơi nào khác giữ con số. Hai cột *Loại file* và *Ngưỡng cứng* được tìm theo **tên**: đổi tên cột, bỏ dạng `**N**` của ô ngưỡng, hay thêm một dòng mà mẫu tên không dịch được thành `*.ext` hoặc `thư-mục/*.ext` đều làm cổng đỏ chứ không bị bỏ qua.

```bash
# Luật F22 — PASS khi section F22 in OK.
bash scripts/fe-gate.sh
```

Ba điều ngưỡng này **không** làm:

- Không khuyến khích tách file bằng cách cắt đôi giữa chừng rồi import lại.
- Không áp cho `*.spec.ts`.
- Không bao giờ chấp nhận bản `-v2` song song một component hay service. Sửa tại chỗ; lịch sử nằm trong git.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §5

---

## 6. So sánh với phương án đã loại

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §6

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

> 📖 Bảng đầy đủ mọi luật kèm cột "ép bằng gì": [`../RULES.md`](../RULES.md) §7.
