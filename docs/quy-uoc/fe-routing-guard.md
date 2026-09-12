---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Route và Guard — điều hướng, phân quyền, trạng thái trên URL

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`. File này là quy ước thi công cho tầng điều hướng của `src/FE`.
>
> Phân quyền phía FE **chỉ để dựng giao diện đúng**, không phải để bảo vệ dữ liệu. Bảo vệ thật nằm ở BE ([`be-api-controller.md`](be-api-controller.md)). Một guard bị vượt qua chỉ được phép dẫn tới một màn hình rỗng và một lỗi 403 từ server — không bao giờ dẫn tới dữ liệu.

---

## 1. Bản đồ route

```
/                          → chuyển hướng theo trạng thái phiên
/dang-nhap                 → không có khung app (noShell)
/quen-mat-khau             → noShell
/doi-mat-khau-bat-buoc     → noShell, chỉ vào được khi BE yêu cầu
│
└── (trong khung app, sau authGuard)
    /trang-chu
    /ho-so
    /quan-tri
    │   ├── nguoi-dung          permission: 'core.user.read'
    │   ├── vai-tro             permission: 'core.role.read'
    │   ├── phan-quyen          permission: 'core.permission.read'
    │   └── menu                permission: 'core.menu.read'
    └── /<ten-module>/...       route nghiệp vụ, lazy theo module
    /khong-co-quyen             → 403 hiển thị được
    /**                          → 404
```

> 🚨 **Đoạn URL viết tiếng Việt không dấu; khoá phân quyền thì KHÔNG.** Hai thứ này
> trông giống nhau ở sơ đồ trên nhưng thuộc hai hệ khác nhau: đoạn URL là chuỗi người dùng
> nhìn thấy, còn khoá phân quyền là **dữ liệu trong database** mà BE so khớp từng ký tự. Bản
> trước của sơ đồ này dùng `core.nguoi-dung.xem`, `core.vai-tro.xem` … — một bộ khoá **không
> tồn tại** ở phía BE. Guard theo bộ đó thì FE ẩn sạch mọi mục menu, kể cả với tài khoản đủ
> quyền, và không có lỗi nào bật ra.
>
> 📖 Danh mục khoá đầy đủ, và là nguồn duy nhất:
> [`../database/schema-core.md`](../database/schema-core.md) §5.2. FE **không** tự đặt khoá mới.

### 1.1 Quy ước đặt tên đoạn URL

| Luật | Ví dụ |
| --- | --- |
| Chữ thường, **không dấu**, nối bằng gạch ngang | `/quan-tri/nguoi-dung` |
| Danh từ số ít cho một bản ghi, danh từ chung cho danh sách | `/quan-tri/nguoi-dung/:id` |
| Không đưa động từ vào URL, trừ hành động không có tài nguyên tương ứng | `/dang-nhap` được; `/quan-tri/xoa-nguoi-dung` **không** |
| Không lồng sâu quá ba cấp | `/quan-tri/nguoi-dung/:id` là giới hạn |
| Đoạn URL là **tiếng Việt không dấu**, khớp ngôn ngữ của miền nghiệp vụ | nhất quán với tên thư mục feature |

URL là thứ người dùng nhìn thấy, gửi cho nhau, và đánh dấu lại. Đổi URL là breaking change với người dùng — cân nhắc như đổi hợp đồng API.

---

## 2. Lazy-load theo tầng

### 2.1 `app.routes.ts` chỉ khai `loadChildren`

```typescript
// app.routes.ts
export const APP_ROUTES: Routes = [
  {
    path: '',
    canActivate: [authGuard, mustChangePasswordGuard],
    loadComponent: () => import('./shared/layout/khung-app.component').then((m) => m.KhungAppComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'trang-chu' },
      {
        path: 'trang-chu',
        loadChildren: () => import('./platform/trang-chu/trang-chu.routes').then((m) => m.TRANG_CHU_ROUTES),
      },
      {
        path: 'quan-tri',
        loadChildren: () => import('./platform/quan-tri/quan-tri.routes').then((m) => m.QUAN_TRI_ROUTES),
      },
      // Route nghiệp vụ — mỗi module một dòng, không import component trực tiếp.
      // Thêm module mới còn phải thêm tên vào BUSINESS_MODULES: fe-architecture.md §4.4.
    ],
  },
  {
    path: '',
    loadChildren: () => import('./platform/xac-thuc/xac-thuc.routes').then((m) => m.XAC_THUC_ROUTES),
  },
  { path: 'khong-co-quyen', loadComponent: () => import('./platform/loi/khong-co-quyen.page').then((m) => m.KhongCoQuyenPage) },
  { path: '**', loadComponent: () => import('./platform/loi/khong-tim-thay.page').then((m) => m.KhongTimThayPage) },
];
```

**`app.routes.ts` không được import component của feature trực tiếp.** Một `import` tĩnh kéo cả feature vào bundle khởi động, và triệu chứng duy nhất là bundle to dần — không lỗi, không cảnh báo, không ai để ý cho tới lúc ngân sách bundle (luật F14) đỏ.

```bash
# PASS khi không in ra dòng nào ngoài loadComponent/loadChildren.
grep -nE "^import .* from '\./(platform|modules)/" src/FE/src/app/app.routes.ts
```

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
  {
    path: 'nguoi-dung/:id',
    canActivate: [permissionGuard('core.user.read')],
    loadComponent: () =>
      import('./nguoi-dung/pages/chi-tiet/chi-tiet-nguoi-dung.page').then((m) => m.ChiTietNguoiDungPage),
    title: 'nguoiDung.chiTiet',
  },
];
```

**Guard đặt trong route của feature, không rải ở `app.routes.ts`.** Lý do là câu hỏi *"màn này ai vào được"* phải trả lời được bằng cách mở đúng một file — file của feature. Đặt guard ở cấp cha thì thêm một màn hình mới sẽ thừa hưởng guard mà người viết không hề biết, và tệ hơn là thừa hưởng **thiếu** guard.

**`title` giữ KHOÁ i18n, không giữ câu.** Chiến lược đặt tiêu đề trang trong `core/` dịch khoá đó — nếu để câu thẳng ở đây thì tiêu đề tab là chỗ duy nhất trong app không đổi theo ngôn ngữ, và luật F8 không quét file `.ts` nên không có gì bắt.

### 2.3 Route con và `noShell`

Hai màn xác thực không có khung app: không sidebar, không thanh trên, không menu. Chúng nằm ở nhánh route **thứ hai** của `app.routes.ts` (§2.1) — nhánh không bọc trong component khung.

Cách làm sai thường gặp là để chúng trong khung rồi ẩn sidebar bằng CSS. Sai vì: khung vẫn dựng, vẫn gọi API menu, và người chưa đăng nhập vẫn phát sinh một request 401 mỗi lần mở màn đăng nhập.

---

## 3. Guard theo permission, KHÔNG theo role

### 3.1 Quyết định

> **Không có hằng số role nào trong code — cả BE lẫn FE.** Role là dữ liệu trong cơ sở dữ liệu; phân quyền kiểm bằng **permission**. Xem [`../adr/0005-permission-based.md`](../adr/0005-permission-based.md).

Vì sao, nói bằng hệ quả cụ thể: kiểm theo tên role nghĩa là mỗi lần khách hàng muốn "cho nhóm trưởng phòng xem được màn này", đội phải sửa code, build lại và triển khai lại. Kiểm theo permission thì đó là một thao tác cấu hình trong màn phân quyền. Chi phí ban đầu cao hơn — phải khai một danh mục permission và một màn gán quyền — và đó là toàn bộ cái giá.

Ở dự án tiền nhiệm có ba tên role khai cứng trong mã nguồn. Hệ quả không phải là code xấu, mà là **Core không mang đi được**: sản phẩm tiếp theo có cơ cấu tổ chức khác sẽ thừa kế ba cái tên vô nghĩa với nó, và mọi câu lệnh kiểm tra dựa trên chúng.

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

  // Giữ đường quay lại: sau khi đăng nhập, người dùng về đúng chỗ họ định tới.
  return router.createUrlTree([routes.dangNhap], {
    queryParams: { returnUrl: state.url },
  });
};
```

**Trả `UrlTree` chứ không gọi `router.navigate()` rồi `return false`.** `UrlTree` để router hủy điều hướng cũ và chuyển sang cái mới trong **một** chu kỳ; cách kia tạo hai lần điều hướng chồng nhau, và trong vài trường hợp lịch sử trình duyệt còn giữ lại trang bị chặn — bấm Back là quay về đúng chỗ vừa bị cấm.

`returnUrl` phải được kiểm khi dùng lại: chỉ chấp nhận đường dẫn nội bộ bắt đầu bằng `/` và không bắt đầu bằng `//`. Không kiểm thì đó là một lỗ chuyển hướng ra ngoài — kẻ tấn công gửi một liên kết đăng nhập kèm `returnUrl` trỏ sang site của họ.

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
// core/auth/auth.service.ts — phần liên quan
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly _nguoiDung = signal<NguoiDungHienTai | null>(null);

  readonly nguoiDung = this._nguoiDung.asReadonly();
  readonly daDangNhap = computed(() => this._nguoiDung() !== null);

  private readonly tapQuyen = computed(() => new Set(this._nguoiDung()?.quyen ?? []));

  coQuyen(ma: string): boolean {
    return this.tapQuyen().has(ma);
  }
}
```

**Dùng `Set` chứ không `Array.includes`.** Một màn hình danh sách có thể hỏi quyền cho từng dòng; với mảng thì đó là phép quét tuyến tính nhân với số dòng, chạy lại mỗi lần đổi phát hiện.

**Chuyển hướng tới màn 403 chứ không tới trang chủ.** Đưa về trang chủ khiến người dùng nghĩ mình bấm nhầm, thử lại, rồi lại bị đưa về — vòng lặp không có thông tin. Màn 403 nói rõ "không có quyền" và cho một đường đi tiếp.

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
<app-nut
  *appHasPermission="'core.user.write'"
  [nhan]="'chung.them' | translate"
  (bam)="moFormTao()"
/>
```

Chuỗi truyền vào phải là một khoá **có thật** trong danh mục quyền ([`../database/schema-core.md`](../database/schema-core.md) §5.2). Khoá tự chế không gây lỗi: `coQuyen()` trả `false` và nút biến mất với **mọi** người, kể cả tài khoản đủ quyền — deny-by-default làm chuỗi sai trông y hệt chuỗi đúng của một người thiếu quyền.

> Cú pháp `*` ở đây là **directive cấu trúc tự viết**, không phải chỉ thị cũ của Angular. Luật F9 cấm `*ngIf`/`*ngFor`, không cấm directive cấu trúc của app — cổng quét đúng tên ba chỉ thị cũ nên không báo nhầm.

**Ẩn nút KHÔNG phải là phân quyền.** Nó là phép lịch sự với người dùng — đừng bày ra thứ họ bấm vào sẽ nhận lỗi. Người biết dùng công cụ phát triển vẫn gọi được API. Bảo vệ thật ở BE, và guard phía FE cũng chỉ là lớp trải nghiệm.

Hệ quả thực tế: **không được dùng directive này để giấu dữ liệu nhạy cảm đã tải về.** Nếu một trường không được phép xem, BE không gửi trường đó — không phải FE ẩn nó đi.

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
| 3' | `systemOperatorGuard` | **Thay cho** `permissionGuard` ở khu quản trị hệ thống, không cộng thêm vào: khu đó gác bằng **cờ** `isSystemOperator`, không bằng ma trận quyền ([`../adr/0017-khu-quan-tri-he-thong.md`](../adr/0017-khu-quan-tri-he-thong.md)). Hai đường gác không bao giờ chạy chung một route |
| 4 | `unsavedChangesGuard` | Chạy lúc **rời** route, không phải lúc vào. Hỏi trước khi bỏ thay đổi chưa lưu — §4.1 |

Angular chạy `canActivate` theo thứ tự khai và dừng ở guard đầu tiên từ chối. Đảo thứ tự 1 và 3 thì người chưa đăng nhập bị đưa tới màn 403 thay vì màn đăng nhập — một thông điệp sai và một đường cụt.

---

### 4.1 `unsavedChangesGuard` — hỏi trước khi mất dữ liệu

Một guard **dùng chung ở Core**, không phải mỗi màn tự làm. Màn hình chỉ khai *"tôi đang có thay đổi chưa lưu"*; phần hỏi và phần chặn nằm ở một chỗ.

| Luật | Vì sao |
| --- | --- |
| Chỉ hỏi khi giá trị **thật sự đổi**, không hỏi khi người dùng chỉ chạm vào ô rồi bỏ đi | Hỏi thừa vài lần là người dùng bấm "rời đi" theo phản xạ, và lần thứ mười họ mất dữ liệu thật |
| Sau khi lưu thành công phải **xoá cờ thay đổi** | Quên bước này thì lưu xong vẫn bị hỏi, và người dùng học được rằng hộp thoại này vô nghĩa |
| Câu hỏi nói rõ **mất gì**, không hỏi chung chung | *"Rời trang? Thay đổi chưa lưu sẽ mất"* — không phải *"Bạn có chắc không?"* |
| Áp cho cả điều hướng trong app lẫn đóng tab | Hai đường khác nhau về kỹ thuật; thiếu một đường là thủng một nửa |

Đây là thứ dự án tiền nhiệm đã có và làm đúng — giữ lại.


## 5. Luồng bắt buộc đổi mật khẩu lần đầu

Đây là guard dễ quên nhất, và quên nó là một lỗ hổng thật: tài khoản do quản trị viên tạo có mật khẩu tạm, và nếu người dùng vào thẳng được màn hình khác thì mật khẩu tạm đó sống mãi.

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

  if (!auth.phaiDoiMatKhau()) {
    return true;
  }

  // Đang ở chính màn đổi mật khẩu thì cho qua — nếu không sẽ lặp vô hạn.
  if (state.url.startsWith(routes.doiMatKhauBatBuoc)) {
    return true;
  }

  return router.createUrlTree([routes.doiMatKhauBatBuoc]);
};
```

### 5.3 Bốn điều phải đúng, thiếu một là hỏng

1. **Cờ đến từ BE, không từ trạng thái phía FE.** FE tự nhớ "đã đổi rồi" thì tải lại trang là mất, hoặc tệ hơn: người dùng tự đặt lại được.
2. **Guard áp cho toàn bộ nhánh trong khung app**, không chỉ vài màn. Áp lẻ tẻ thì màn quên áp chính là đường vòng.
3. **Chính màn đổi mật khẩu phải được loại trừ**, nếu không guard tự đẩy về chính nó và trình duyệt treo ở vòng lặp điều hướng.
4. **Sau khi đổi xong, cờ phải được làm mới từ BE** trước khi điều hướng tiếp. Điều hướng trước khi làm mới thì guard đọc cờ cũ và đẩy người dùng quay lại màn vừa hoàn thành.

Luồng nghiệp vụ đầy đủ (hạn mật khẩu tạm, số lần sai, khoá tài khoản) ở [`../contracts/auth.md`](../contracts/auth.md) và [`../wiki-core/fe/07-auth-identity.md`](../wiki-core/fe/07-auth-identity.md).

---

## 6. `CORE_ROUTES` — `core/` giữ luật, app cấp đường dẫn

Guard nằm trong `core/`, mà `core/` mang đi được sang dự án khác. Vì vậy nó **không được khai cứng** đường dẫn của dự án này.

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

Token **không có giá trị mặc định**, cùng lý do đã nói ở [`fe-architecture.md`](fe-architecture.md) §2.5: một đường dẫn mặc định biến "app quên khai" thành một điều hướng tới route không tồn tại, và triệu chứng là màn hình trắng lúc hết phiên — thời điểm khó chẩn đoán nhất.

```bash
# core/ không được biết đường dẫn route của dự án. PASS khi không in ra dòng nào.
grep -rnE "navigate\(\s*\[\s*'/" src/FE/src/app/core --include='*.ts' | grep -v '\.spec\.ts'
```

---

## 7. State trên URL — bộ lọc thuộc về query param

### 7.1 Luật

> **Bộ lọc, phân trang và sắp xếp của một trang danh sách phải nằm trên query param.**

Ba việc hỏng ngay khi trạng thái chỉ sống trong `signal()`:

| Người dùng làm gì | Trạng thái trong signal | Trạng thái trên URL |
| --- | --- | --- |
| Tải lại trang (F5) | Mất hết bộ lọc | Giữ nguyên |
| Gửi link cho đồng nghiệp | Người kia thấy màn hình khác | Thấy đúng cái đang thấy |
| Bấm Back sau khi mở chi tiết | Về danh sách đã reset | Về đúng trang, đúng bộ lọc |

Việc thứ hai là việc quan trọng nhất và ít được nghĩ tới nhất: "gửi cho tôi cái link đang lọc như thế" là thao tác người dùng làm hằng ngày, và không có nó thì họ mô tả bằng lời rồi người nhận dựng lại sai.

### 7.2 Khuôn

```typescript
export class DanhSachNguoiDungPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(NguoiDungService);

  // URL là NGUỒN. Signal chỉ là bản đọc của URL, không phải bản thứ hai.
  private readonly query = toSignal(
    this.route.queryParamMap.pipe(map(docQuery)),
    { initialValue: QUERY_MAC_DINH },
  );

  protected readonly items = signal<NguoiDung[]>([]);

  constructor() {
    effect(() => {
      // Bước ánh xạ TƯỜNG MINH: trạng thái đọc từ URL -> tham số truy vấn của hợp đồng.
      this.service.danhSach(sangPageQuery(this.query())).subscribe((trang) => this.items.set(trang.items));
    });
  }

  /** Mọi thay đổi bộ lọc đi qua URL, không set thẳng vào signal. */
  protected doiLoc(thayDoi: Partial<NguoiDungUrlState>): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { ...thayDoi, trang: 1 }, // đổi bộ lọc thì về trang 1
      queryParamsHandling: 'merge',
      replaceUrl: true, // không nhồi lịch sử: mỗi ký tự gõ vào ô tìm kiếm không phải một bước Back
    });
  }
}

/** Trạng thái ĐỌC TỪ URL. Tên field = tên query param trên thanh địa chỉ. */
interface NguoiDungUrlState {
  trang: number;
  coMoiTrang: number;
  sapXep: string;
  giamDan: boolean;
  tuKhoa?: string;
}

const QUERY_MAC_DINH: NguoiDungUrlState = { trang: 1, coMoiTrang: 20, sapXep: 'hoTen', giamDan: false };

function docQuery(p: ParamMap): NguoiDungUrlState {
  return {
    trang: soDuong(p.get('trang'), QUERY_MAC_DINH.trang),
    coMoiTrang: soDuong(p.get('coMoiTrang'), QUERY_MAC_DINH.coMoiTrang),
    sapXep: p.get('sapXep') ?? QUERY_MAC_DINH.sapXep,
    giamDan: p.get('giamDan') === 'true',
    tuKhoa: p.get('tuKhoa') ?? undefined,
  };
}

/** Chỗ DUY NHẤT đổi trạng thái URL thành tham số truy vấn của hợp đồng API. */
function sangPageQuery(s: NguoiDungUrlState): PageQuery {
  return {
    page: s.trang,
    pageSize: s.coMoiTrang,
    sortBy: s.sapXep,
    sortDescending: s.giamDan,
    keyword: s.tuKhoa,
  };
}
```

🛑 **Kiểu đọc-từ-URL KHÔNG được đưa thẳng vào hàm dựng query string.** Tên field ở đây là tên **query param trên thanh địa chỉ**, đặt theo ngôn ngữ giao diện; tên tham số API là thứ khác, do [`../contracts/README.md`](../contracts/README.md) §8 quyết. Bỏ bước ánh xạ thì request đi ra mang tên tham số của URL, BE **không nhận ra chúng, áp mặc định**, và người dùng đang ở trang 7 luôn nhận về trang 1 — không lỗi, không cảnh báo, không có gì trên màn hình nói rằng có chuyện gì đó vừa xảy ra. Đặt tên kiểu khác nhau cho hai vai trò là thứ khiến sai lầm đó **không viết ra được**.

Kiểu `PageQuery`: [`fe-api-client.md`](fe-api-client.md) §5.1.

### 7.3 Năm luật của mẫu này

1. **URL là nguồn duy nhất; signal là bản đọc.** Giữ cả hai làm nguồn ghi thì chúng lệch nhau, và triệu chứng là bấm Back xong màn hình hiện dữ liệu của bộ lọc mới với thanh lọc của bộ lọc cũ.
2. **Mọi tham số đọc từ URL phải được kiểm và có giá trị lùi.** Query param là dữ liệu người dùng gõ được — `?trang=-5` hoặc `?coMoiTrang=999999` sẽ tới. Hàm đọc phải chặn, không phải BE chặn hộ.
3. **Đổi bộ lọc thì đưa về trang 1.** Không làm thì người dùng đang ở trang 7, gõ từ khoá mới, và nhận về một trang rỗng — trông hệt như "không có kết quả".
4. **Dùng `replaceUrl: true` cho thay đổi liên tục** (gõ vào ô tìm kiếm), và không dùng nó cho thay đổi rời rạc (đổi trang, đổi cột sắp xếp). Sai chiều nào cũng khó chịu: một bên là bấm Back mười lần mới thoát khỏi ô tìm kiếm, bên kia là mất khả năng quay lại trang trước.
5. **Ánh xạ URL → tham số API nằm ở đúng một hàm.** Rải nó ra nhiều màn thì mỗi màn đặt tên tham số theo trí nhớ, và chỗ sai không bao giờ báo lỗi.

### 7.4 Cái gì KHÔNG lên URL

| Không lên URL | Vì sao |
| --- | --- |
| Trạng thái mở/đóng của dialog | Đưa lên URL thì tải lại trang mở ra một dialog không có ngữ cảnh |
| Nội dung form đang nhập dở | Dữ liệu lọt vào lịch sử trình duyệt và log của proxy |
| Bất cứ thứ gì nhạy cảm | URL bị ghi lại ở nhiều nơi ngoài tầm kiểm soát |
| Trạng thái thu gọn sidebar | Thuộc về sở thích người dùng, không thuộc về màn hình này |

---

## 8. 401 và 403 — xử lý ở tầng nào

Đây là chỗ dễ làm hai lần hoặc không lần nào. Ranh giới dứt khoát:

| Tình huống | Ai xử lý | Làm gì | Ai **không** xử lý |
| --- | --- | --- | --- |
| **401 từ một request API** (phiên hết hạn giữa chừng) | `errorInterceptor` | Điều hướng về màn đăng nhập kèm `returnUrl`. Không toast — điều hướng đã là thông điệp | Guard: nó chỉ chạy lúc chuyển route, không thấy request nào |
| **Chưa đăng nhập, gõ thẳng URL** | `authGuard` | Trả `UrlTree` về màn đăng nhập kèm `returnUrl` | Interceptor: chưa có request nào phát sinh |
| **403 từ một request API** | `errorInterceptor` | Toast "không có quyền" kèm `traceId`. **Không** điều hướng | Guard |
| **Không có permission cho màn hình** | `permissionGuard` | Trả `UrlTree` về màn 403 | Interceptor |

**Vì sao 403 không điều hướng còn 401 thì có:** 401 nghĩa là *toàn bộ phiên* không dùng được nữa — ở lại màn hình hiện tại là vô nghĩa vì mọi request sau đều hỏng. 403 nghĩa là *một thao tác cụ thể* bị từ chối; phần còn lại của màn hình vẫn dùng được, và đá người dùng đi chỗ khác sẽ làm mất dữ liệu họ đang nhập.

**Chống toast trùng:** 401 do interceptor xử lý, nên guard không bao giờ được bắn toast cho 401 — và ngược lại, interceptor không được điều hướng cho 403. Mỗi ô trong bảng trên có đúng một chủ.

**Chốt (2026-09-10): 401 luôn điều hướng về màn đăng nhập, kể cả khi form đang nhập dở** — dữ liệu chưa lưu mất.

Vì sao không làm hộp thoại đăng nhập lại tại chỗ để giữ form: nó phải xử lý ca **người đăng nhập lại là tài khoản khác**, và một form do người A nhập bị gửi đi dưới danh tính người B là lỗi nặng hơn nhiều so với việc phải nhập lại. Giữ một đường xử lý duy nhất cho 401 cũng là thứ làm bảng trên còn đúng.

Lớp giảm thiệt hại nằm **trước** đó, không nằm ở đây: cảnh báo sắp hết phiên — [`../wiki-core/fe/07-auth-identity.md`](../wiki-core/fe/07-auth-identity.md) §7.5.

---

## 9. Checklist thêm một màn hình mới

- [ ] Đoạn URL theo quy ước §1.1 — chữ thường, không dấu, không động từ.
- [ ] Route khai trong `<feature>.routes.ts`, không trong `app.routes.ts`.
- [ ] `loadComponent` / `loadChildren`, không `import` tĩnh.
- [ ] `title` giữ **khoá i18n**, không giữ câu.
- [ ] Guard đủ và đúng thứ tự (§4): `authGuard` → `mustChangePasswordGuard` → `permissionGuard`.
- [ ] Permission dùng trong guard đã tồn tại trong danh mục quyền phía BE ([`../contracts/permissions.md`](../contracts/permissions.md)).
- [ ] Nút/thao tác cần quyền đã bọc directive `appHasPermission`.
- [ ] Nếu là trang danh sách: bộ lọc, phân trang, sắp xếp nằm trên query param (§7).
- [ ] Có đường ra khi không có dữ liệu và khi tải thất bại — hai trạng thái khác nhau.
- [ ] Nếu là module mới: đã thêm tên vào `BUSINESS_MODULES` ([`fe-architecture.md`](fe-architecture.md) §4.4).

---

## 10. Đối chiếu luật

| Luật | Nội dung | Mục |
| --- | --- | --- |
| S2 | Phân quyền kiểm bằng permission, không bằng tên role | §3.1 |
| F14 | Bundle không vượt ngân sách | §2.1 |

Bảng đầy đủ: [`../RULES.md`](../RULES.md). Quyết định gốc: [`../adr/0005-permission-based.md`](../adr/0005-permission-based.md). Nền: [`../wiki-core/fe/07-auth-identity.md`](../wiki-core/fe/07-auth-identity.md) · [`../wiki-core/fe/14-security.md`](../wiki-core/fe/14-security.md).
