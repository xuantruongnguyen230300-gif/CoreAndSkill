---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Kiến trúc Frontend — bốn tầng và hàng rào giữ chúng

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Repo đang ở giai đoạn 1, chưa có `src/`: file này mô tả thứ `src/FE` **phải trở thành**, không phải hiện trạng.
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
├── http/            # envelope model, hàm CRUD dùng chung, unwrap, mapper lỗi
├── interceptors/    # thứ tự khai ở fe-api-client.md, KHÔNG tuỳ tiện
├── i18n/            # cấu hình ngx-translate, loader, service đổi ngôn ngữ
├── list/            # trạng thái màn danh sách: GridQuery, ListStateStore — §2.8
├── menu/            # menu động theo quyền
├── theme/           # preset PrimeNG, chuyển sáng/tối — cơ chế ở wiki-core/fe/04 §7
└── toast/           # service giữ hàng đợi toast (component Toast bọc PrimeNG nên nằm ở shared/ui/)
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
├── forms/        # hạ tầng form dùng chung: gắn fieldErrors vào control, hiển thị lỗi
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
| | Import từ `platform/` hoặc `modules/` |
| | Import `components/` từ bên trong `ui/` — chiều ngược, đoạn ngay trên; luật F24 (§4.6) |

> `shared/` là một phần của **CoreBase**, đi theo khi mang nền tảng sang dự án khác: nó không được biết tên sản phẩm, bảng màu cụ thể hay route nghiệp vụ nào.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §2.2

### 2.3 `platform/` — Màn hình Core — định nghĩa gốc

Màn hình có nghĩa với **mọi** sản phẩm dựng trên nền tảng: đăng nhập, đổi mật khẩu bắt buộc, hồ sơ cá nhân, quản trị người dùng, quản trị vai trò, phân quyền, quản trị đơn vị (khu hệ thống).

**Trang chủ** cũng thuộc `platform/`, nhưng Core chỉ cấp **trang chào tối giản** — tên đơn vị, lời chào, lối tắt tới các màn người dùng có quyền; không gọi endpoint riêng, chỉ dùng lại thông tin phiên và menu.

Trang chủ đi qua seam `CORE_HOME` (§2.5): dự án cấp component của mình, **không** sửa route và không sửa khung shell. Core cấp **component** biểu đồ ([`../Design/Components/Chart.md`](../Design/Components/Chart.md)); riêng **thư viện** biểu đồ thuộc nhóm thêm-khi-cần, nạp theo yêu cầu ([`../wiki-core/fe/01-core-components.md`](../wiki-core/fe/01-core-components.md) §3, [`../adr/0019-ba-component-nang-thuoc-core.md`](../adr/0019-ba-component-nang-thuoc-core.md) ràng buộc 1).

Mỗi màn ở trên có một card trong [`../contracts/`](../contracts/). **Quản trị menu KHÔNG nằm trong danh sách**: ở v1 menu là dữ liệu seed ([`../contracts/meta-menu.md`](../contracts/meta-menu.md) §2).

Cấu trúc con giống hệt `modules/` (§3); khác biệt duy nhất là **ý nghĩa**.

`platform/` giữ thêm hai thư mục không phải feature:

| Thư mục | Chứa gì |
| --- | --- |
| `platform/shell/` | **Khung ứng dụng — smart.** Inject phiên và menu, rồi truyền dữ liệu xuống `Sidebar` và `Topbar` qua `input()`. Hai component đó **dumb**, tự dựng ở `shared/components/` — luật F11 áp cho chúng như mọi component khác, không ngoại lệ |
| `platform/config/` | Seam `CORE_SCREEN_EXT` (§2.7). Đặt ở đây vì kiểu của nó đến từ `shared/ui/`, mà `core/` không được import `shared/` (luật F1) |

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
| `CORE_HOME` | Component của **trang chủ**. Core cấp một trang chào tối giản; dự án thay bằng bảng tổng hợp của ngành mình mà không sửa route, không sửa shell — §2.3 |
| `CORE_I18N` | Danh sách ngôn ngữ, ngôn ngữ mặc định, và các nguồn tệp dịch theo thứ tự nạp | [`../wiki-core/fe/08-i18n.md`](../wiki-core/fe/08-i18n.md) §2.3 |
| `API_BASE_URL` | Base URL của API, cấp từ `environment.apiBaseUrl` | [`fe-api-client.md`](fe-api-client.md) §2.1 |
| Bảng màu | **Không đi qua DI.** Giá trị nằm ở `src/styles/_tokens.scss` theo `Design/`; preset PrimeNG ở `core/theme/` chỉ đọc `var(--color-*)` | [`../wiki-core/fe/04-design-token-system.md`](../wiki-core/fe/04-design-token-system.md) §7 |
| `CORE_SCREEN_EXT` | **Phần mở rộng của từng màn Core** — cột, hành động dòng, trường lọc mà dự án thêm vào. Token nằm ở `platform/config/`, **không** ở `core/` | §2.7 |

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
export interface LanguageOption { code: string; nativeName: string }  // code: mã BCP 47; tên viết bằng CHÍNH ngôn ngữ đó

export interface CoreI18n {
  readonly languages: ReadonlyArray<LanguageOption>; // một mục ⇒ LanguageSwitcher không render
  readonly defaultLanguage: string;                  // MÃ — phải là `code` của một mục trong languages
  readonly sources: readonly string[];               // đường dẫn thư mục tệp dịch, tầng sau ghi đè tầng trước
}

export const CORE_I18N = new InjectionToken<CoreI18n>('CORE_I18N');
```

**Token cố ý không có `factory` mặc định.**

Phép thử cho seam mới: *"giá trị này có đổi khi dựng sản phẩm khác trên cùng nền tảng không?"* Có → seam. Không → để trong `core/`.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §2.5

### 2.6 Composition root — nơi mọi seam được nối lại

Composition root phía FE (tương ứng "host mỏng" ở `be-architecture.md` §3) là **file cấu hình app**, nơi khai toàn bộ provider gốc.

**Luật:** `useExisting` khi hai token phải trỏ về **cùng một** thể hiện; `useClass` chỉ khi thật sự cần thể hiện thứ hai. Mặc định là `useExisting`.

**Luật:** mỗi bước khởi tạo chạy trước khi app dựng phải khai rõ **nó phụ thuộc bước nào**. Hai bước độc lập chạy song song được.

> 📖 Danh sách interceptor và thứ tự: [`fe-api-client.md`](fe-api-client.md) — file này không chép lại.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §2.6

---

### 2.7 Seam cho MÀN Core — định nghĩa gốc

Hợp đồng `CORE_SCREEN_EXT`:

```typescript
// platform/config/core-screen-ext.ts — hợp đồng, KHÔNG có giá trị mặc định; khối import ở ly-do §2.7
/** Phần một dự án được phép THÊM vào một màn Core. Không có gì cho phép BỚT. */
export interface ScreenExtension<T = unknown> {
  readonly columns?: ReadonlyArray<DataColumnDef<T>>;   // nối vào SAU cột của Core
  readonly rowActions?: ReadonlyArray<UiMenuItem>;      // nối vào cột hành động
  readonly filterFields?: ReadonlyArray<FilterField>;   // nối vào FilterPanel
  readonly toolbarSlot?: TemplateRef<unknown>;          // chèn vào nhóm hành động của Toolbar
}

/** Khoá là mã MÀN DANH SÁCH Core: 'users' · 'roles' · 'permissions' · 'tenants' */
export const CORE_SCREEN_EXT =
  new InjectionToken<Readonly<Record<string, ScreenExtension>>>('CORE_SCREEN_EXT');
```

Năm luật của seam này:

1. **Chỉ THÊM, không BỚT.** Không có `hiddenColumns`, không có `removeActions`.
2. **Cột thêm nối vào SAU, không chen giữa.**
3. **Khoá là mã màn, không phải đường dẫn route.**
4. **Token không có `factory` mặc định** (cùng lý do với `CORE_BRANDING` ở §2.5), nhưng dự án **không** bắt buộc khai seam này. Màn Core đọc bằng `inject(CORE_SCREEN_EXT, { optional: true })`; kết quả `null` hoặc không có khoá của mình ⇒ chạy đúng bản gốc, không lỗi.
5. **Mọi màn danh sách Core trong `platform/` tự đọc token và tự nối phần mở rộng** theo mã màn của mình. Phạm vi seam là màn danh sách; màn hồ sơ và các màn form không có khoá. Chưa có cổng: nợ ở [`../RULES.md`](../RULES.md) §10.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §2.7

### 2.8 Trạng thái màn danh sách — định nghĩa gốc

[`../wiki-core/fe/11-grid-and-metadata.md`](../wiki-core/fe/11-grid-and-metadata.md) §3–§4 mô tả **cái gì** phải làm; mục này chốt **ai** làm và **làm thế nào**: đúng **một** mẫu, `ListStateStore` ở `core/list/`. Mọi màn danh sách — `platform/` lẫn `modules/` — dùng mẫu này; không màn nào tự viết lại vòng *đọc URL → gọi API → đổi trạng thái*.

```typescript
// core/list/grid-query.ts — khối import ở ly-do §2.8
/** Truy vấn của một màn danh sách. Mọi tên trường là TÊN TRÊN DÂY — không có bước đổi tên ở giữa (luật 2). */
export interface GridQuery extends PageQuery {
  /** Bộ lọc riêng của endpoint — mỗi khoá một tham số rời, tên theo card ở contracts/. */
  readonly filters: Readonly<Record<string, string>>;
}
```

```typescript
// core/list/list-state.store.ts — khối import ở ly-do §2.8
type ListViewState = 'idle' | 'loading' | 'error' | 'empty' | 'empty-filtered';

/** Chờ ngừng gõ. Dưới 200ms một từ gõ có dấu vẫn sinh vài lần gọi; trên 500ms ô tìm có cảm giác chậm. */
const NGUNG_GO_MS = 300;

/** Áp khi URL không mang `pageSize` — mặc định của hợp đồng (contracts/README.md §8). */
const PAGE_SIZE_MAC_DINH = 20;

/** Tên dây dùng chung cho mọi danh sách; query param nào khác là bộ lọc. `tab` là tên dùng chung: store bỏ qua (luật 8). */
const TEN_DUNG_CHUNG: readonly string[] = ['page', 'pageSize', 'sortBy', 'sortDescending', 'searchText', 'tab'];

@Injectable() // KHÔNG providedIn: 'root' — luật 7
export class ListStateStore<T> {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly injector = inject(Injector);
  private readonly destroyRef = inject(DestroyRef);

  private readonly _state = signal<ListViewState>('idle');
  private readonly _result = signal<PagedList<T> | null>(null);
  private readonly reloadTick = signal(0);
  private readonly searchInput = new Subject<string>();
  private connected = false;

  /** Bản ĐỌC của URL, không phải bản thứ hai (luật 1). */
  readonly query = toSignal(this.route.queryParamMap.pipe(map(docQuery)), { requireSync: true });
  readonly state = this._state.asReadonly();
  /** Rỗng KHÔNG mang nghĩa "không có dữ liệu" — `state` mới mang nghĩa đó. */
  readonly rows = computed<readonly T[]>(() => this._result()?.items ?? []);
  readonly totalCount = computed(() => this._result()?.totalCount ?? 0);

  constructor() {
    // Cơ chế 1 và 2: chờ ngừng gõ, bỏ giá trị trùng; thay URL, không nhồi lịch sử (luật 5).
    this.searchInput
      .pipe(debounceTime(NGUNG_GO_MS), map((s) => s.trim()), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((s) => this.ghiUrl({ searchText: s === '' ? null : s, page: null }, true));
  }

  /** Màn gọi ĐÚNG MỘT lần, truyền hàm tải của service mình. Store không biết endpoint. */
  connect(load: (query: GridQuery) => Observable<PagedList<T>>): this {
    if (this.connected) {
      throw new Error('ListStateStore.connect() chỉ được gọi một lần cho mỗi màn');
    }
    this.connected = true;
    combineLatest([
      toObservable(this.query, { injector: this.injector }).pipe(distinctUntilChanged(cungTruyVan)),
      toObservable(this.reloadTick, { injector: this.injector }),
    ])
      .pipe(
        tap(() => this._state.set('loading')),
        // Cơ chế 3: request mới huỷ request cũ.
        switchMap(([q]) =>
          load(q).pipe(
            tap((trang) => {
              this._result.set(trang);
              this._state.set(trang.totalCount > 0 ? 'idle' : coDieuKien(q) ? 'empty-filtered' : 'empty');
            }),
            // errorInterceptor đã hiển thị lỗi; ở đây chỉ đổi trạng thái.
            catchError(() => {
              this._state.set('error');
              return EMPTY;
            }),
          ),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe();
    return this;
  }

  setPage(page: number, pageSize?: number): void {
    const patch: Params = { page: page === 1 ? null : page };
    if (pageSize !== undefined) {
      patch['pageSize'] = pageSize === PAGE_SIZE_MAC_DINH ? null : pageSize;
    }
    this.ghiUrl(patch, false);
  }

  setSort(sortBy: string | null, sortDescending: boolean): void {
    this.ghiUrl({ sortBy, sortDescending: sortBy !== null && sortDescending ? true : null }, false);
  }

  setSearchText(searchText: string): void {
    this.searchInput.next(searchText);
  }

  /** `null` gỡ điều kiện khỏi URL. Đổi lọc thì về trang 1 (luật 3). */
  setFilters(filters: Readonly<Record<string, string | null>>): void {
    this.ghiUrl({ ...filters, page: null }, false);
  }

  reload(): void {
    this.reloadTick.update((n) => n + 1);
  }

  /** `null` gỡ tham số — giá trị mặc định không nằm trên URL. */
  private ghiUrl(patch: Params, replaceUrl: boolean): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: patch,
      queryParamsHandling: 'merge',
      replaceUrl,
    });
  }
}

function docQuery(p: ParamMap): GridQuery {
  const filters: Record<string, string> = {};
  for (const k of p.keys) {
    const v = p.get(k);
    if (v !== null && !TEN_DUNG_CHUNG.includes(k)) {
      filters[k] = v;
    }
  }
  return {
    page: soNguyenDuong(p.get('page')) ?? 1,
    pageSize: soNguyenDuong(p.get('pageSize')) ?? PAGE_SIZE_MAC_DINH,
    sortBy: p.get('sortBy') ?? undefined,
    sortDescending: p.get('sortDescending') === 'true' ? true : undefined,
    searchText: p.get('searchText') ?? undefined,
    filters,
  };
}

/** Sai dạng thì bỏ (luật 2). */
function soNguyenDuong(raw: string | null): number | undefined {
  const n = raw === null ? Number.NaN : Number(raw);
  return Number.isInteger(n) && n >= 1 ? n : undefined;
}

function cungTruyVan(a: GridQuery, b: GridQuery): boolean {
  return JSON.stringify(a) === JSON.stringify(b);
}

function coDieuKien(q: GridQuery): boolean {
  return q.searchText !== undefined || Object.keys(q.filters).length > 0;
}
```

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

Page **không** có `catchError`, không `subscribe`, không `try/catch` — luật xử lý lỗi của màn ở [`fe-api-client.md`](fe-api-client.md) §3.

**Phân trang bind lên `DataTable`; màn không dựng thêm dải phân trang thứ hai.** Chủ hợp đồng phân trang là `DataTable` ([`../Design/Components/DataTable.md`](../Design/Components/DataTable.md)); [`../Design/Components/Pagination.md`](../Design/Components/Pagination.md) giữ hình thức và accessibility của dải đó.

Luật của tầng này:

1. **Truy vấn sống trên URL, và URL là nguồn sự thật.** Mọi thay đổi đi qua setter rồi lên URL; không set thẳng vào signal, và không ghi lại URL sau khi tải xong (bẫy vòng lặp ở [`../wiki-core/fe/11-grid-and-metadata.md`](../wiki-core/fe/11-grid-and-metadata.md) §3.3).
2. **Tên trên URL = tên trên dây = tên trong `GridQuery`.** Query param đọc từ URL được kiểm: sai dạng thì bỏ; đúng dạng mà vượt khoảng của hợp đồng thì BE trả 400 và màn vào `error` — không vá âm thầm ([`../contracts/README.md`](../contracts/README.md) §8).
3. **Đổi bộ lọc hoặc từ khoá thì `page` về 1.**
4. **Ba cơ chế chống gọi dồn dập, cả ba ở store.** Chờ ngừng gõ (`setSearchText`), bỏ qua truy vấn trùng, và **huỷ kết quả của request cũ khi request mới đã đi**. `Toolbar` phát `searchChanged` ở mọi lần gõ, không chờ bên trong. **Ngưỡng chờ là hằng số `NGUNG_GO_MS` của store**, lý do ghi ngay cạnh hằng số — không phải input của `Toolbar`, không phải tham số của `connect()`.
5. **`replaceUrl` cho thay đổi liên tục, không cho thay đổi rời rạc.** Gõ vào ô tìm thay URL tại chỗ; đổi trang, sắp xếp, bộ lọc thì thêm một bước lịch sử.
6. **`state` là một biến, không phải bốn cờ** (lý do ở `DataTable.md`).
7. **Một thực thể cho một màn, cấp ở cấp route — trong `providers` của chính page mà route trỏ tới.**
8. **`tab` là tên dùng chung, không phải bộ lọc.** Store bỏ qua nó (`TEN_DUNG_CHUNG`), không gửi lên API; `queryParamsHandling: 'merge'` giữ nó khi store ghi URL. Trang cha tự ghi và đọc `tab` qua `Router` / `queryParamMap` của route; màn không có store cũng đọc thẳng `queryParamMap` — không cấp `ListStateStore` chỉ để có `tab` ([`../Design/Components/Tabs.md`](../Design/Components/Tabs.md)).

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
| `components/*` | nhận `input()`, phát `output()`, render | inject service lấy dữ liệu (luật F11); biết HTTP; import DTO |
| `services/*` | gọi API, map DTO ↔ model, hủy/retry | giữ trạng thái UI (mở/đóng dialog, tab đang chọn) |
| `state/*.store.ts` | `signal`/`computed`, điều phối service | render, đụng DOM |
| `models/*` | type, interface, hằng số của miền | logic, gọi hàm |

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

Bốn luật đi thành một bộ, thiếu một thì những cái kia mất hiệu lực:

| Luật | Nội dung | Nếu thiếu |
| --- | --- | --- |
| **F1** | `core/` không import ngược lên | Tầng đáy không còn là tầng đáy |
| **F2** | `modules/A` không import `modules/B` | Module dính nhau, không bỏ ra được |
| **F3** | Cấm `eslint-disable` cho danh sách rule ranh giới | **F1 và F2 chỉ còn là gợi ý** |
| **F4** | `BUSINESS_MODULES` khớp thư mục `modules/` thật | F2 thành no-op im lặng |

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §4.1

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
# Luật F4 — BUSINESS_MODULES khớp thư mục modules/ thật.
# PASS khi hai danh sách giống hệt nhau (diff không in dòng nào).
# Thiếu thư mục hoặc thiếu tệp khai mảng thì ĐỎ — không nuốt lỗi bằng 2>/dev/null.
[ -d src/FE/src/app/modules ] || { echo "F4: không có src/FE/src/app/modules để đối chiếu"; exit 1; }
[ -f src/FE/eslint.boundaries.cjs ] || { echo "F4: không có src/FE/eslint.boundaries.cjs"; exit 1; }
diff <(ls -1 src/FE/src/app/modules | sort) \
     <(node -p "require('./src/FE/eslint.boundaries.cjs').BUSINESS_MODULES.join('\n')" | sort)
```

> **Không được** chép danh sách module vào script cổng ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §5).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §4.4

### 4.5 Cấm `eslint-disable` cho rule ranh giới — luật F3

#### Danh sách rule cấm tắt — định nghĩa gốc

Bảng này là **Danh sách rule cấm tắt — định nghĩa gốc**, nguồn duy nhất của tập rule mà `eslint-disable` không được chạm tới. `scripts/fe-gate.sh` đọc bảng lúc chạy, không giữ mảng thứ hai — lệnh ở [`../wiki-core/fe/trien-khai/05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) §8.1; tài liệu khác **không** chép lại bảng, chỉ trỏ về mục này ([`../OWNERSHIP.md`](../OWNERSHIP.md) §2).

| Rule | Canh luật |
| --- | --- |
| `import/no-restricted-paths` | F1, F2, F24 |
| `@angular-eslint/template/prefer-control-flow` | F9 |
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

Dạng tắt **toàn file không kèm tên rule** không có chuỗi để đọc từ bảng, nên cần mẫu riêng:

```bash
# Luật F3 — dạng tắt TOÀN FILE không kèm tên rule; cũng cấm, vì nó tắt luôn rule ranh giới.
# PASS khi không in ra dòng nào.
[ -d src/FE/src ] || { echo "F3: không có src/FE/src để quét"; exit 1; }
grep -rnE 'eslint-disable\s*\*/' src/FE/src --include='*.ts'
```

Ba điểm phải giữ khi thi công cổng:

1. **Quét cả ba dạng** `eslint-disable`, `eslint-disable-next-line`, `eslint-disable-line`.
2. **Quét cả dạng tắt toàn file.**
3. **Cổng phải có test của chính nó.** Viết một file canary chứa đúng dòng bị cấm, chạy cổng, xác nhận đỏ, rồi xoá.

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

Nối vào config cho file trong `shared/ui/` bằng một khối cùng khuôn §4.2 — `files: ['src/app/shared/ui/**/*.ts']`, `zones: sharedUiZones`; khối đầy đủ ở ly-do §4.6.

> ⚠️ **Khối này không được chồng `files` lên khối của F1 hay F2.** Cần canh thêm một chiều cho cây đã có khối thì nối zone vào mảng của khối đó.

**Canary bắt buộc khi thi công**, cùng cách §4.2: tạm import một component từ `shared/components/` vào một file trong `shared/ui/`, thấy lint đỏ đúng thông điệp, rồi hoàn nguyên.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §4.6

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

```bash
# Luật F22 — *.page.ts không vượt ngưỡng cứng. PASS khi không in ra dòng nào.
[ -d src/FE/src/app ] || { echo "F22: không có src/FE/src/app để quét"; exit 1; }
find src/FE/src/app -name '*.page.ts' -not -name '*.spec.ts' \
  -exec awk 'END { if (NR > 400) print FILENAME ": " NR }' {} \;
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

| Luật | Nội dung | Ép bằng | Mục |
| --- | --- | --- | --- |
| F1 | `core/` không import ngược lên | ESLint `coreLayerZones` | §4.2 |
| F2 | `modules/A` không import `modules/B` | ESLint `moduleBoundaryZones` | §4.3 |
| F3 | Cấm `eslint-disable` cho rule ranh giới | Cổng FE | §4.5 |
| F4 | `BUSINESS_MODULES` khớp thư mục thật | Cổng FE | §4.4 |
| F10 | `components/`/`pages/` không import DTO | Cổng FE | [`fe-api-client.md`](fe-api-client.md) |
| F11 | `components/` không inject data service | Cổng FE | §3.1 |
| F12 | Mọi service có `.spec.ts` | Cổng FE | §3 |
| F22 | `*.page.ts` không vượt ngưỡng cứng | Cổng FE | §5 |
| F24 | `shared/ui/` không import `shared/components/` | ESLint `sharedUiZones` | §4.6 |

Bảng đầy đủ mọi luật kèm cột "ép bằng gì": [`../RULES.md`](../RULES.md) §7.
