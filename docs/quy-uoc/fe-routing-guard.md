---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Route và Guard — điều hướng, phân quyền, trạng thái trên URL

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`. File này là quy ước thi công cho tầng điều hướng của `src/FE`.
>
> Phân quyền phía FE **chỉ để dựng giao diện đúng**; bảo vệ thật nằm ở BE ([`be-api-controller.md`](be-api-controller.md)). Guard bị vượt qua chỉ được dẫn tới màn hình rỗng và 403 từ server — không bao giờ tới dữ liệu.

---

## 1. Bản đồ route

```
/                          → chuyển hướng theo trạng thái phiên
/dang-nhap                 → không có khung app (noShell)
/doi-mat-khau-bat-buoc     → noShell, authGuard + mustChangePasswordGuard — chỉ vào được khi BE yêu cầu
│
└── (trong khung app platform/shell, sau authGuard)
    /trang-chu
    /ho-so
    /quan-tri
    │   ├── nguoi-dung          permission: 'core.user.read'
    │   ├── vai-tro             permission: 'core.role.read'
    │   └── phan-quyen          permission: 'core.permission.read'
    /he-thong
    │   └── don-vi              systemOperatorGuard — cờ isSystemOperator, không phải permission
    /<ten-module>/...           route nghiệp vụ, lazy theo module
    /khong-co-quyen             403 — trong khung app
    └── /**                     404 — trong khung app, luôn là route cuối
```

Khu `/he-thong` (quản trị đơn vị của tài khoản vận hành) gác bằng **cờ**, không bằng ma trận quyền — thứ tự guard ở §4, hợp đồng ở [`../contracts/tenants.md`](../contracts/tenants.md).

**Trang 403 và 404 nằm TRONG khung app.** Chưa đăng nhập thì `authGuard` của nhánh khung đưa về màn đăng nhập kèm `returnUrl` — kể cả khi URL gõ sai.

> 🚨 **Đoạn URL viết tiếng Việt không dấu; khoá phân quyền thì KHÔNG.**
>
> 📖 Danh mục khoá đầy đủ, và là nguồn duy nhất:
> [`../database/schema-core.md`](../database/schema-core.md) §5.2. FE **không** tự đặt khoá mới.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-routing-guard.md`](../wiki-core/fe/ly-do/fe-routing-guard.md) §1

### 1.1 Quy ước đặt tên đoạn URL

| Luật | Ví dụ |
| --- | --- |
| Chữ thường, **không dấu**, nối bằng gạch ngang | `/quan-tri/nguoi-dung` |
| Danh từ số ít cho một bản ghi, danh từ chung cho danh sách | `/quan-tri/nguoi-dung/:id` |
| Không đưa động từ vào URL, trừ hành động không có tài nguyên tương ứng | `/dang-nhap` được; `/quan-tri/xoa-nguoi-dung` **không** |
| Không lồng sâu quá ba cấp | `/quan-tri/nguoi-dung/:id` là giới hạn |
| Đoạn URL là **tiếng Việt không dấu**, khớp ngôn ngữ của miền nghiệp vụ | nhất quán với tên thư mục feature |

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-routing-guard.md`](../wiki-core/fe/ly-do/fe-routing-guard.md) §1.1

---

## 2. Lazy-load theo tầng

### 2.1 `app.routes.ts` chỉ khai `loadChildren`

```typescript
// app.routes.ts
export const APP_ROUTES: Routes = [
  // Nhánh xác thực khai TRƯỚC nhánh khung app; xac-thuc.routes.ts KHÔNG khai path '' hay '**'.
  {
    path: '',
    loadChildren: () => import('./platform/xac-thuc/xac-thuc.routes').then((m) => m.XAC_THUC_ROUTES),
  },
  {
    path: '',
    canActivate: [authGuard, mustChangePasswordGuard],
    // Cho AuthService chạy lại guard của URL hiện tại khi cờ buộc đổi mật khẩu bật giữa phiên (§5.4).
    runGuardsAndResolvers: 'always',
    loadComponent: () => import('./platform/shell/shell.component').then((m) => m.ShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'trang-chu' },
      {
        path: 'trang-chu',
        loadChildren: () => import('./platform/trang-chu/trang-chu.routes').then((m) => m.TRANG_CHU_ROUTES),
      },
      // 'quan-tri', 'he-thong': cùng khuôn loadChildren — ly-do §2.1
      // Route nghiệp vụ — mỗi module một dòng, không import component trực tiếp.
      // Thêm module mới còn phải thêm tên vào BUSINESS_MODULES: fe-architecture.md §4.4.

      // Trang lỗi nằm TRONG khung (§1). '**' là dòng cuối.
      { path: 'khong-co-quyen', title: 'trangLoi.khongCoQuyen.tieuDe', loadComponent: () => import('./platform/loi/khong-co-quyen.page').then((m) => m.KhongCoQuyenPage) },
      { path: '**', title: 'trangLoi.khongTimThay.tieuDe', loadComponent: () => import('./platform/loi/khong-tim-thay.page').then((m) => m.KhongTimThayPage) },
    ],
  },
];
```

**`app.routes.ts` không được import component của feature trực tiếp.**

```bash
# Luật F20 — app.routes.ts không import tĩnh component của feature.
# PASS khi không in ra dòng nào ngoài loadComponent/loadChildren.
[ -f src/FE/src/app/app.routes.ts ] || { echo "F20: không có src/FE/src/app/app.routes.ts để quét"; exit 1; }
grep -nE "^import .* from '\./(platform|modules)/" src/FE/src/app/app.routes.ts
```

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-routing-guard.md`](../wiki-core/fe/ly-do/fe-routing-guard.md) §2.1

### 2.2 Mỗi feature một `<feature>.routes.ts`

```typescript
// platform/quan-tri/quan-tri.routes.ts
export const QUAN_TRI_ROUTES: Routes = [
  {
    path: 'nguoi-dung',
    canActivate: [permissionGuard('core.user.read')],
    loadComponent: () =>
      import('./nguoi-dung/pages/danh-sach/danh-sach-nguoi-dung.page').then((m) => m.DanhSachNguoiDungPage),
    title: 'nguoiDung.tieuDe',
  },
  // 'nguoi-dung/:id' cùng khuôn — ly-do §2.2
];
```

**Guard đặt trong route của feature, không rải ở `app.routes.ts`.**

**`title` giữ KHOÁ i18n, không giữ câu.** `core/i18n/core-title.strategy.ts` — `CoreTitleStrategy extends TitleStrategy` — dịch khoá đó rồi nối hậu tố ` · ` + `CORE_BRANDING.name` ([`fe-architecture.md`](fe-architecture.md) §2.5); khoá vắng ⇒ chỉ `CORE_BRANDING.name`. Đăng ký ở `app.config.ts`: `{ provide: TitleStrategy, useClass: CoreTitleStrategy }`. Ngôn ngữ đổi lúc chạy ([`../wiki-core/fe/08-i18n.md`](../wiki-core/fe/08-i18n.md) §7) thì `CoreTitleStrategy` dịch lại tiêu đề của route hiện tại.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-routing-guard.md`](../wiki-core/fe/ly-do/fe-routing-guard.md) §2.2

### 2.3 Route con và `noShell`

Hai màn xác thực không có khung app; chúng nằm ở nhánh route **đầu tiên** của `app.routes.ts` (§2.1), không bọc trong component khung.

**Không khung app KHÔNG có nghĩa là không guard.** Hai màn ở nhánh này gác khác nhau (§1):

```typescript
// platform/xac-thuc/xac-thuc.routes.ts
export const XAC_THUC_ROUTES: Routes = [
  {
    // Màn đăng nhập KHÔNG có authGuard: nơi duy nhất người chưa đăng nhập được vào.
    path: 'dang-nhap',
    loadComponent: () => import('./pages/dang-nhap/dang-nhap.page').then((m) => m.DangNhapPage),
    title: 'xacThuc.dangNhap.tieuDe',
  },
  {
    // authGuard: chưa đăng nhập thì chưa có cờ để đọc. mustChangePasswordGuard: cờ tắt thì đẩy về đích sau đăng nhập (§5.2).
    path: 'doi-mat-khau-bat-buoc',
    canActivate: [authGuard, mustChangePasswordGuard],
    loadComponent: () =>
      import('./pages/doi-mat-khau-bat-buoc/doi-mat-khau-bat-buoc.page').then((m) => m.DoiMatKhauBatBuocPage),
    title: 'xacThuc.doiMatKhauBatBuoc.tieuDe',
  },
];
```

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-routing-guard.md`](../wiki-core/fe/ly-do/fe-routing-guard.md) §2.3

---

## 3. Guard theo permission, KHÔNG theo role

### 3.1 Quyết định

> **Không có hằng số role nào trong code — cả BE lẫn FE.** Role là dữ liệu trong cơ sở dữ liệu; phân quyền kiểm bằng **permission**. Xem [`../adr/0005-permission-based.md`](../adr/0005-permission-based.md).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-routing-guard.md`](../wiki-core/fe/ly-do/fe-routing-guard.md) §3.1

### 3.2 `authGuard`

```typescript
// core/guards/auth.guard.ts
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const routes = inject(CORE_ROUTES);

  if (auth.daDangNhap()) {
    return true;
  }

  // Giữ đường quay lại sau đăng nhập.
  return router.createUrlTree([routes.dangNhap], {
    queryParams: { returnUrl: state.url },
  });
};
```

**Trả `UrlTree` chứ không gọi `router.navigate()` rồi `return false`.**

`returnUrl` phải được kiểm khi dùng lại: chỉ chấp nhận đường dẫn nội bộ bắt đầu bằng `/` và không bắt đầu bằng `//`.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-routing-guard.md`](../wiki-core/fe/ly-do/fe-routing-guard.md) §3.2

### 3.3 `permissionGuard`

```typescript
// core/guards/permission.guard.ts
export function permissionGuard(...quyenCanCo: string[]): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);
    const routes = inject(CORE_ROUTES);

    // Mọi quyền trong danh sách đều phải có (VÀ, không phải HOẶC).
    // Cần ngữ nghĩa HOẶC thì khai một permission mới ở BE, đừng nới guard này.
    return quyenCanCo.every((q) => auth.coQuyen(q))
      ? true
      : router.createUrlTree([routes.khongCoQuyen]);
  };
}
```

```typescript
// core/auth/auth.service.ts — phần liên quan; khối import ở ly-do §3.3
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  /** MenuStore KHÔNG inject AuthService — vòng DI (NG0200). */
  private readonly menu = inject(MenuStore);
  private readonly _nguoiDung = signal<NguoiDungHienTai | null>(null);
  private goiMe: Subscription | null = null;
  private canNapLaiMenu = false;

  readonly nguoiDung = this._nguoiDung.asReadonly();
  readonly daDangNhap = computed(() => this._nguoiDung() !== null);
  readonly phaiDoiMatKhau = computed(() => this._nguoiDung()?.mustChangePassword ?? false);
  /** Cờ vận hành hệ thống — ĐƯỜNG GÁC RIÊNG, không nằm trong tập quyền (contracts/auth.md §3, §5). */
  readonly laVanHanhHeThong = computed(() => this._nguoiDung()?.isSystemOperator ?? false);

  private readonly tapQuyen = computed(() => new Set(this._nguoiDung()?.quyen ?? []));

  coQuyen(ma: string): boolean {
    return this.tapQuyen().has(ma);
  }

  /** errorInterceptor gọi khi gặp 403 FORBIDDEN (fe-api-client.md §2.2). */
  lamMoiQuyen(): void {
    this.canNapLaiMenu = true;
    if (this.goiMe === null) {
      void this.goiLaiMe();
    }
  }

  /** Bốn nơi gọi: màn hồ sơ sau khi lưu; errorInterceptor khi 403 PASSWORD_CHANGE_REQUIRED (§5.4); màn đổi mật khẩu bắt buộc (§5.3); khung ứng dụng sau khi đổi ngôn ngữ (Design/Screens/00). Huỷ lời gọi đang chạy rồi gửi lại. */
  lamMoiPhien(): Promise<void> {
    return this.goiLaiMe();
  }

  /** SessionExpiryHandler gọi (fe-api-client.md §2.5). Dọn gì, giữ gì: wiki-core/fe/07-auth-identity.md §7.2. */
  donPhien(): void {
    this.goiMe?.unsubscribe();
    this.canNapLaiMenu = false;
    this._nguoiDung.set(null);
  }

  /** Thay TOÀN BỘ người dùng hiện tại bằng `me`. Promise xong khi lời gọi kết thúc — về, lỗi, hay bị huỷ. */
  private goiLaiMe(): Promise<void> {
    if (!this.daDangNhap()) {
      return Promise.resolve();
    }
    this.goiMe?.unsubscribe();
    return new Promise((xong) => {
      this.goiMe = this.http
        .get<ApiResult<PhienDto>>('/core/auth/me')
        .pipe(map(unwrapData), finalize(() => { this.goiMe = null; xong(); }))
        .subscribe({
          next: (d) => {
            const nguoiDung = sangNguoiDungHienTai(d);
            this._nguoiDung.set(nguoiDung);
            if (this.canNapLaiMenu) {
              this.canNapLaiMenu = false;
              this.menu.lamMoi(d.id);
            }
            if (nguoiDung.mustChangePassword) {
              // Chạy lại guard của URL hiện tại — mustChangePasswordGuard trả đích (§5.4). Không tự chọn đường dẫn.
              void this.router.navigateByUrl(this.router.url, { onSameUrlNavigation: 'reload' });
            }
          },
          error: () => undefined,
        });
    });
  }
}
```

**Lưu hồ sơ xong thì gọi `lamMoiPhien()`.** Topbar đọc tên từ phiên (`nguoiDung()`), không từ response của `PUT` hồ sơ ([`../contracts/profile.md`](../contracts/profile.md) §2). Nơi gọi mà điều hướng ngay sau đó thì `await` nó — màn đổi mật khẩu bắt buộc (§5.3); nơi không điều hướng thì `void` nó — interceptor (§5.4).

**Phiên mang cả đơn vị, ngôn ngữ ưa thích và cờ vận hành hệ thống.** `nguoiDung()` giữ `tenantCode`, `tenantName`, `preferredLanguage`, `isSystemOperator` của DTO phiên ([`../contracts/auth.md`](../contracts/auth.md) §3, §5); mapper `sangNguoiDungHienTai` chép sang model. Khung đọc `tenantName` ở đây; ngôn ngữ sau đăng nhập đọc `preferredLanguage` ([`../wiki-core/fe/08-i18n.md`](../wiki-core/fe/08-i18n.md) §7); `systemOperatorGuard` đọc `isSystemOperator` (§3.5). Không màn nào gọi hồ sơ chỉ để lấy các giá trị này.

**`lamMoiQuyen()` nạp lại menu trong cùng bước với tập quyền.** Cờ `canNapLaiMenu` sống qua lời gọi bị huỷ.

**Dùng `Set` chứ không `Array.includes`.**

**Chuyển hướng tới màn 403 chứ không tới trang chủ.**

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-routing-guard.md`](../wiki-core/fe/ly-do/fe-routing-guard.md) §3.3

### 3.4 Directive `appHasPermission`

Guard bảo vệ **màn hình**; directive ẩn **phần tử**. Cả hai đọc chung một nguồn.

```typescript
// shared/directives/has-permission.directive.ts
@Directive({
  selector: '[appHasPermission]',
  standalone: true,
})
export class HasPermissionDirective {
  private readonly auth = inject(AuthService);
  private readonly tpl = inject(TemplateRef<unknown>);
  private readonly vcr = inject(ViewContainerRef);

  readonly appHasPermission = input.required<string | string[]>();

  constructor() {
    effect(() => {
      const canCo = this.appHasPermission();
      const ds = Array.isArray(canCo) ? canCo : [canCo];
      const duocPhep = ds.every((q) => this.auth.coQuyen(q));

      this.vcr.clear();
      if (duocPhep) {
        this.vcr.createEmbeddedView(this.tpl);
      }
    });
  }
}
```

```html
<app-button *appHasPermission="'core.user.write'" variant="primary" (clicked)="moFormTao()">{{ 'chung.them' | translate }}</app-button>
```

Chuỗi truyền vào phải là khoá **có thật** trong danh mục quyền ([`../database/schema-core.md`](../database/schema-core.md) §5.2).

**Ẩn nút KHÔNG phải là phân quyền.** Bảo vệ thật ở BE, và guard phía FE cũng chỉ là lớp trải nghiệm.

**Không được dùng directive này để giấu dữ liệu nhạy cảm đã tải về** — trường không được phép xem thì BE không gửi.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-routing-guard.md`](../wiki-core/fe/ly-do/fe-routing-guard.md) §3.4

### 3.5 `systemOperatorGuard` — gác bằng CỜ, không bằng ma trận quyền

Khu `/he-thong` (§1) là đường gác duy nhất **không** đi qua ma trận quyền: nó đọc cờ `isSystemOperator` của phiên ([`../adr/0017-khu-quan-tri-he-thong.md`](../adr/0017-khu-quan-tri-he-thong.md), [`../contracts/auth.md`](../contracts/auth.md) §3, §5). Cờ **không** nằm trong `permissions`, nên `permissionGuard` không thay được.

```typescript
// core/guards/system-operator.guard.ts
export const systemOperatorGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const routes = inject(CORE_ROUTES);

  return auth.laVanHanhHeThong() ? true : router.createUrlTree([routes.khongCoQuyen]);
};
```

**Không có directive song song cho cờ này.** Khu `/he-thong` không phân quyền theo từng nút; mọi endpoint của khu gác bằng cùng một cờ ([`../contracts/tenants.md`](../contracts/tenants.md)).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-routing-guard.md`](../wiki-core/fe/ly-do/fe-routing-guard.md) §3.5

### 3.6 `AuthService` — khởi động và thiết lập phiên — định nghĩa gốc

Hai đường thiết lập phiên, mỗi đường một hàng; không có đường thứ ba.

| Lúc | Làm gì |
| --- | --- |
| **Khởi động app** — `provideAppInitializer` ở `app.config.ts` ([`fe-architecture.md`](fe-architecture.md) §2.6) | `forkJoin` hai `GET` **song song**: `XsrfTokenStore.lamMoi()` ([`fe-api-client.md`](fe-api-client.md) §2.1) và `/core/auth/me` mang `BO_QUA_HET_PHIEN` ([`fe-api-client.md`](fe-api-client.md) §2.5). App render sau khi **cả hai** về |
| `me` trả 200 | `_nguoiDung.set(sangNguoiDungHienTai(d))` |
| `me` trả 401 | Chưa đăng nhập: giữ `null`, không toast, không điều hướng — `authGuard` lo phần còn lại (§3.2) |
| Một nhánh lỗi | `catchError` **trong từng nhánh** của `forkJoin`, trả `null`; app vẫn render. Lỗi khác 401 đã đi qua `errorInterceptor` |
| **Đăng nhập trả 200** | Hai việc **không phụ thuộc thứ tự**, cả hai xong trước khi điều hướng: `_nguoiDung.set(...)` từ **DTO phiên trong response `login`** — cùng kiểu với `me` ([`../contracts/auth.md`](../contracts/auth.md) §5), **không gọi `me`**; và `XsrfTokenStore.lamMoi()` ([`../luong/D1-dang-nhap.md`](../luong/D1-dang-nhap.md) bước 7–8) |
| Phiên vừa thiết lập (cả hai đường) | Áp `preferredLanguage` theo [`../wiki-core/fe/08-i18n.md`](../wiki-core/fe/08-i18n.md) §7 |

```typescript
// app.config.ts — phần khởi động
provideAppInitializer(() => {
  const xsrf = inject(XsrfTokenStore);
  const auth = inject(AuthService);
  return forkJoin([
    xsrf.lamMoi().pipe(catchError(() => of(null))),
    auth.napPhienKhoiDong().pipe(catchError(() => of(null))),
  ]);
}),
```

`napPhienKhoiDong()` là lời gọi `me` duy nhất chạy khi `daDangNhap()` còn `false`; `goiLaiMe()` (§3.3) không dùng cho khởi động.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-routing-guard.md`](../wiki-core/fe/ly-do/fe-routing-guard.md) §3.6

---

## 4. Thứ tự guard — không tuỳ tiện

```typescript
canActivate: [authGuard, mustChangePasswordGuard, permissionGuard('core.user.read')]
```

| Thứ tự | Guard | Vì sao ở vị trí này |
| --- | --- | --- |
| 1 | `authGuard` | Chưa đăng nhập thì mọi câu hỏi sau đều vô nghĩa, và hỏi quyền của một người dùng null là lỗi |
| 2 | `mustChangePasswordGuard` | Người đang bị buộc đổi mật khẩu chưa được coi là dùng hệ thống bình thường |
| 3 | `permissionGuard` | Cuối cùng mới hỏi quyền cho màn hình cụ thể |
| 3' | `systemOperatorGuard` | **Thay cho** `permissionGuard` ở khu quản trị hệ thống, không cộng thêm vào: khu đó gác bằng **cờ** `isSystemOperator`, không bằng ma trận quyền (§3.5, [`../adr/0017-khu-quan-tri-he-thong.md`](../adr/0017-khu-quan-tri-he-thong.md)). Hai đường gác không bao giờ chạy chung một route |
| 4 | `unsavedChangesGuard` | Chạy lúc **rời** route, không phải lúc vào. Hỏi trước khi bỏ thay đổi chưa lưu — §4.1 |

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-routing-guard.md`](../wiki-core/fe/ly-do/fe-routing-guard.md) §4

---

### 4.1 `unsavedChangesGuard` — hỏi trước khi mất dữ liệu

Một guard **dùng chung ở Core**; màn chỉ khai *"tôi đang có thay đổi chưa lưu"*, phần hỏi và chặn nằm ở một chỗ.

| Luật | Vì sao |
| --- | --- |
| Chỉ hỏi khi giá trị **thật sự đổi**, không hỏi khi người dùng chỉ chạm vào ô rồi bỏ đi | Hỏi thừa vài lần là người dùng bấm "rời đi" theo phản xạ, và lần thứ mười họ mất dữ liệu thật |
| Sau khi lưu thành công phải **xoá cờ thay đổi** | Quên bước này thì lưu xong vẫn bị hỏi, và người dùng học được rằng hộp thoại này vô nghĩa |
| Câu hỏi nói rõ **mất gì**, không hỏi chung chung | *"Rời trang? Thay đổi chưa lưu sẽ mất"* — không phải *"Bạn có chắc không?"* |
| Áp cho cả điều hướng trong app lẫn đóng tab | Hai đường khác nhau về kỹ thuật; thiếu một đường là thủng một nửa |

#### Khoá i18n của hộp hỏi

Ba spec màn trỏ về đây ([`../Design/Screens/00-khung-ung-dung.md`](../Design/Screens/00-khung-ung-dung.md), [`../Design/Screens/03-ho-so-ca-nhan.md`](../Design/Screens/03-ho-so-ca-nhan.md), [`../Design/Screens/12-ma-tran-phan-quyen.md`](../Design/Screens/12-ma-tran-phan-quyen.md)). Khoá theo [`fe-ui-conventions.md`](fe-ui-conventions.md) §5.3, miền `chung.`.

| Chỗ dùng | Input của [`../Design/Components/ConfirmDialog.md`](../Design/Components/ConfirmDialog.md) | Khoá |
| --- | --- | --- |
| Tiêu đề — "Rời trang?" | `title` | `chung.roiTrang.tieuDe` |
| Mô tả — nói rõ mất gì | `message` | `chung.roiTrang.moTa` |
| Nút xác nhận rời đi | `confirmLabel` | `chung.roiTrang.xacNhan` |
| Nút ở lại | `cancelLabel` để `null` → hộp dùng nhãn huỷ chung | `chung.huy` |

`severity` là `ask`. Câu hiển thị thuộc bảng dịch; mục này chỉ khai **khoá**.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-routing-guard.md`](../wiki-core/fe/ly-do/fe-routing-guard.md) §4.1

## 5. Luồng bắt buộc đổi mật khẩu lần đầu

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-routing-guard.md`](../wiki-core/fe/ly-do/fe-routing-guard.md) §5

### 5.1 Luồng

```
Đăng nhập
   │
   ├─ BE trả về hồ sơ có cờ "phải đổi mật khẩu" = true
   │      │
   │      ▼
   │   /doi-mat-khau-bat-buoc   ← mọi route khác đều bị guard đẩy về đây
   │      │  đổi thành công → BE hạ cờ
   │      ▼
   └─▶ returnUrl (hoặc /trang-chu)
```

### 5.2 Guard

```typescript
// core/guards/must-change-password.guard.ts
export const mustChangePasswordGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const routes = inject(CORE_ROUTES);

  const dangVaoManDoiMatKhau = state.url.startsWith(routes.doiMatKhauBatBuoc);

  if (!auth.phaiDoiMatKhau()) {
    // Không bị ép đổi mà vẫn gõ thẳng URL màn đó → đẩy về đích sau đăng nhập.
    return dangVaoManDoiMatKhau ? router.createUrlTree([routes.sauDangNhap]) : true;
  }

  // Đang ở chính màn đổi mật khẩu thì cho qua.
  return dangVaoManDoiMatKhau ? true : router.createUrlTree([routes.doiMatKhauBatBuoc]);
};
```

Guard này gác **hai chiều** nên có mặt ở cả hai nhánh route: nhánh khung app (§2.1) đẩy người bị ép vào màn đổi mật khẩu; nhánh xác thực (§2.3) đẩy người **không** bị ép ra khỏi màn đó.

### 5.3 Bốn điều phải đúng, thiếu một là hỏng

1. **Cờ đến từ BE, không từ trạng thái phía FE.**
2. **Guard áp cho toàn bộ nhánh trong khung app**, không chỉ vài màn.
3. **Chính màn đổi mật khẩu phải được loại trừ khi cờ BẬT, và phải bị chặn khi cờ TẮT** — cùng một guard, nhánh ngược lại (§5.2).
4. **Sau khi đổi xong, cờ phải được làm mới từ BE** trước khi điều hướng tiếp: màn `await auth.lamMoiPhien()` (§3.3) rồi mới điều hướng.

Ngôn ngữ người dùng chọn trên màn đổi mật khẩu bắt buộc được ghi vào hồ sơ sau khi đổi xong: [`../wiki-core/fe/08-i18n.md`](../wiki-core/fe/08-i18n.md) §7.

Luồng nghiệp vụ đầy đủ: [`../contracts/auth.md`](../contracts/auth.md), [`../wiki-core/fe/07-auth-identity.md`](../wiki-core/fe/07-auth-identity.md).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-routing-guard.md`](../wiki-core/fe/ly-do/fe-routing-guard.md) §5.3

### 5.4 Cờ bật giữa phiên

Cờ bật **sau** khi phiên đã nạp: BE chặn bằng 403 `CORE.AUTH.PASSWORD_CHANGE_REQUIRED` ([`../contracts/auth.md`](../contracts/auth.md) §1.2), FE đi đúng một đường:

| Bước | Ai | Làm gì |
| --- | --- | --- |
| 1 | `errorInterceptor` | Gặp mã đó → gọi `AuthService.lamMoiPhien()`. Không toast, không điều hướng ([`fe-api-client.md`](fe-api-client.md) §2.2) |
| 2 | `AuthService` | `me` về mang `mustChangePassword: true` → yêu cầu router chạy lại guard của **chính URL hiện tại** (`onSameUrlNavigation: 'reload'`). Không chọn đích (§3.3) |
| 3 | `mustChangePasswordGuard` | Đọc `phaiDoiMatKhau()` mới, trả `UrlTree` sang màn đổi mật khẩu bắt buộc (§5.2) |

Bước 3 chỉ chạy khi nhánh khung app khai `runGuardsAndResolvers: 'always'` (§2.1).

**Đích do guard trả, không do interceptor hay `AuthService` tự chọn** (§5.3 điều 3).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-routing-guard.md`](../wiki-core/fe/ly-do/fe-routing-guard.md) §5.4

---

## 6. `CORE_ROUTES` — `core/` giữ luật, app cấp đường dẫn

Guard nằm trong `core/`, mang đi được sang dự án khác, nên **không được khai cứng** đường dẫn của dự án này.

```typescript
// core/config/core-routes.ts
export interface CoreRoutes {
  readonly dangNhap: string;
  readonly doiMatKhauBatBuoc: string;
  readonly khongCoQuyen: string;
  readonly sauDangNhap: string;
}

export const CORE_ROUTES = new InjectionToken<CoreRoutes>('CORE_ROUTES');

export function provideCoreRoutes(routes: CoreRoutes): EnvironmentProviders {
  return makeEnvironmentProviders([{ provide: CORE_ROUTES, useValue: routes }]);
}
```

Token **không có giá trị mặc định** (lý do: [`fe-architecture.md`](fe-architecture.md) §2.5).

```bash
# Luật F21 — core/ không được biết đường dẫn route của dự án. PASS khi không in ra dòng nào.
[ -d src/FE/src/app/core ] || { echo "F21: không có src/FE/src/app/core để quét"; exit 1; }
grep -rnE "navigate\(\s*\[\s*'/" src/FE/src/app/core --include='*.ts' | grep -v '\.spec\.ts'
```

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-routing-guard.md`](../wiki-core/fe/ly-do/fe-routing-guard.md) §6

---

## 7. State trên URL — bộ lọc thuộc về query param

### 7.1 Luật

> **Bộ lọc, phân trang và sắp xếp của một trang danh sách phải nằm trên query param.**

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-routing-guard.md`](../wiki-core/fe/ly-do/fe-routing-guard.md) §7.1

### 7.2 Khuôn

> 📖 Một mẫu duy nhất cho mọi màn danh sách — `ListStateStore` ở `core/list/`: [`fe-architecture.md`](fe-architecture.md) §2.8. Query param mang đúng tên tham số trên dây (`page`, `pageSize`, `sortBy`, `sortDescending`, `searchText`, bộ lọc theo card) — không có hàm đổi tên.

### 7.3 Luật của mẫu này

> 📖 URL là nguồn, kiểm tham số đọc từ URL, đổi lọc thì về trang 1, `replaceUrl` cho thay đổi liên tục, chống gọi dồn dập: luật của tầng ở [`fe-architecture.md`](fe-architecture.md) §2.8.

### 7.4 Cái gì KHÔNG lên URL

| Không lên URL | Vì sao |
| --- | --- |
| Trạng thái mở/đóng của dialog | Đưa lên URL thì tải lại trang mở ra một dialog không có ngữ cảnh |
| Nội dung form đang nhập dở | Dữ liệu lọt vào lịch sử trình duyệt và log của proxy |
| Bất cứ thứ gì nhạy cảm | URL bị ghi lại ở nhiều nơi ngoài tầm kiểm soát |
| Trạng thái thu gọn sidebar | Thuộc về sở thích người dùng, không thuộc về màn hình này |

---

## 8. 401 và 403 — xử lý ở tầng nào

Ranh giới dứt khoát:

| Tình huống | Ai xử lý | Làm gì | Ai **không** xử lý |
| --- | --- | --- | --- |
| **401 từ một request API** (phiên hết hạn giữa chừng) | `errorInterceptor` → `SessionExpiryHandler` | Dọn trạng thái người dùng, báo tab khác, điều hướng về màn đăng nhập kèm `returnUrl` — một lần tới lần đăng nhập kế ([`fe-api-client.md`](fe-api-client.md) §2.5). Không toast — điều hướng đã là thông điệp | Guard: nó chỉ chạy lúc chuyển route, không thấy request nào |
| **Chưa đăng nhập, gõ thẳng URL** | `authGuard` | Trả `UrlTree` về màn đăng nhập kèm `returnUrl` | Interceptor: chưa có request nào phát sinh |
| **403 `CORE.AUTH.FORBIDDEN` từ một request API** | `errorInterceptor` | Toast "không có quyền" kèm `traceId`, rồi làm mới tập quyền và nạp lại menu qua `AuthService.lamMoiQuyen()` (§3.3, [`fe-api-client.md`](fe-api-client.md) §2.2). **Không** điều hướng | Guard |
| **403 `CORE.AUTH.PASSWORD_CHANGE_REQUIRED` từ một request API** | `errorInterceptor` → `AuthService` → `mustChangePasswordGuard` | Interceptor gọi `lamMoiPhien()`, không toast; `me` về mang cờ thì router chạy lại guard của URL hiện tại và guard trả `UrlTree` sang màn đổi mật khẩu bắt buộc (§5.4) | Màn; interceptor không điều hướng |
| **403 `CORE.AUTH.ORIGIN_REJECTED` từ một request API** | `errorInterceptor` | Toast chung kèm `traceId`. Không gửi lại, không làm mới quyền ([`fe-api-client.md`](fe-api-client.md) §2.2) | Guard, màn |
| **403 mang mã nghiệp vụ từ một request API** | Màn gọi endpoint | Hiển thị theo mã khai ở card của endpoint. Interceptor không toast, không làm mới quyền ([`fe-api-client.md`](fe-api-client.md) §2.2) | Interceptor, guard |
| **Không có permission cho màn hình** | `permissionGuard` | Trả `UrlTree` về màn 403 — trong khung app (§1) | Interceptor |

**Chống toast trùng:** guard không bao giờ bắn toast cho 401; interceptor không điều hướng cho 403. Mỗi ô trong bảng có đúng một chủ.

**Chốt (2026-09-10): 401 luôn điều hướng về màn đăng nhập, kể cả khi form đang nhập dở** — dữ liệu chưa lưu mất.

Cảnh báo sớm trước khi hết phiên: v1 **chưa có** — [`../wiki-core/fe/07-auth-identity.md`](../wiki-core/fe/07-auth-identity.md) §7.5.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-routing-guard.md`](../wiki-core/fe/ly-do/fe-routing-guard.md) §8

---

## 9. Checklist thêm một màn hình mới

- [ ] Đoạn URL theo quy ước §1.1 — chữ thường, không dấu, không động từ.
- [ ] Route khai trong `<feature>.routes.ts`, không trong `app.routes.ts`.
- [ ] `loadComponent` / `loadChildren`, không `import` tĩnh.
- [ ] `title` giữ **khoá i18n**, không giữ câu.
- [ ] Guard đủ và đúng thứ tự (§4): `authGuard` → `mustChangePasswordGuard` → `permissionGuard`.
- [ ] Permission dùng trong guard đã tồn tại trong danh mục quyền phía BE ([`../contracts/permissions.md`](../contracts/permissions.md)).
- [ ] Nút/thao tác cần quyền đã bọc directive `appHasPermission`.
- [ ] Endpoint mà card khai 403 mang mã nghiệp vụ → màn hiển thị được mã đó; interceptor không toast cho nó (§8).
- [ ] Nếu là trang danh sách: dùng `ListStateStore` — bộ lọc, phân trang, sắp xếp nằm trên query param mang tên dây (§7).
- [ ] Có đường ra khi không có dữ liệu và khi tải thất bại — hai trạng thái khác nhau.
- [ ] Nếu là module mới: đã thêm tên vào `BUSINESS_MODULES` ([`fe-architecture.md`](fe-architecture.md) §4.4).

---

## 10. Đối chiếu luật

| Luật | Nội dung | Mục |
| --- | --- | --- |
| S2 | Phân quyền kiểm bằng permission, không bằng tên role | §3.1 |
| F14 | Bundle không vượt ngân sách | §2.1 |
| F20 | `app.routes.ts` không import tĩnh component của feature | §2.1 |
| F21 | `core/` không khai cứng đường dẫn route của dự án | §6 |

Bảng đầy đủ: [`../RULES.md`](../RULES.md). Quyết định gốc: [`../adr/0005-permission-based.md`](../adr/0005-permission-based.md). Nền: [`../wiki-core/fe/07-auth-identity.md`](../wiki-core/fe/07-auth-identity.md) · [`../wiki-core/fe/14-security.md`](../wiki-core/fe/14-security.md).
