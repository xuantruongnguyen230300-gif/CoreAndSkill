---
kind: luat
scope: core
verified: 2026-11-07
---

# 03. Quản lý state — signal store viết tay, chưa dùng NgRx

> ✅ **CÓ THẬT** (đối chiếu cả tệp 2026-11-07, chỉ bằng đọc mã). Hạng mục có thật và hạng mục chưa: bảng ở §10. Khối mã ở §4 trích từ tệp thật, khối ở §5.1 là bản rút gọn — nhãn ngay tại mục. Quy tắc cuối ở §6 (`K15`) khớp code, kể cả ngoại lệ có tên của nó. Rà bằng `grep -rln 'effect(' src/FE/src/app --include='*.ts' --exclude='*.spec.ts'`, rồi đọc từng thân `effect()`. PASS khi chỉ thân ở `src/FE/src/app/shared/ui/toast/toast.component.ts` gọi tới một hàm ghi signal.
>
> Thi công cụ thể (đặt file ở đâu, đặt tên thế nào): [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md).

---

## 1. Ba loại state, ba cách xử lý khác nhau

Phần lớn tranh cãi về "dùng thư viện state nào" biến mất khi tách đúng ba loại này. Chúng có **vòng đời khác nhau**, nên gộp chung vào một cơ chế là tự tạo bài toán.

| Loại | Ví dụ | Nguồn sự thật | Vòng đời |
| --- | --- | --- | --- |
| **Server state** | Danh sách người dùng, chi tiết một bản ghi, ma trận quyền | **Server** — bản trên FE luôn là bản sao có thể cũ | Từ lần tải tới lần làm mới |
| **UI state** | Trang hiện tại, ô tìm kiếm, dialog đang mở, cột đang ẩn | Chính FE | Theo màn hình, thường mất khi rời trang |
| **Session state** | Người đang đăng nhập, tập permission, ngôn ngữ, chế độ sáng/tối | Server (nạp một lần) + lựa chọn của người dùng | Suốt phiên làm việc |

**Sai lầm phổ biến nhất là đối xử với server state như một biến của app.** Khi coi nó là biến, người ta cố giữ nó "luôn đúng" bằng cách đồng bộ tay sau mỗi thao tác ghi — và mỗi chỗ quên đồng bộ là một chỗ hiển thị dữ liệu cũ. Cách đúng là coi nó là **cache**: chấp nhận nó có thể cũ, và định nghĩa rõ *khi nào phải vứt đi* (§5).

| Loại | Cơ chế ở Core này | Nơi khai |
| --- | --- | --- |
| Server state | Service store dạng signal. Cache có khoá chỉ khi cache có chủ đích (§5) — hôm nay chỉ menu; `ListStateStore` không cache, chỉ bỏ qua truy vấn trùng | Màn danh sách: `ListStateStore` ở `core/list/` ([`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.8) · khác: `<feature>/state/*.store.ts` |
| UI state | `signal()` trần trong page, phần cần chia sẻ link thì đặt trên URL. Một *quy trình* (hộp thoại + form + gửi + xử lý lỗi) làm page vượt ngưỡng cỡ file thì lên `state/*.store.ts` — không lên `services/` ([`../../adr/0049-trang-thai-quy-trinh-hop-thoai-o-state-khong-o-services.md`](../../adr/0049-trang-thai-quy-trinh-hop-thoai-o-state-khong-o-services.md)) | `<feature>/pages/…` · `<feature>/state/…` |
| Session state | Service `providedIn: 'root'` trong `core/` | `core/auth`, `core/i18n`, `core/theme` |

> **Store sống ở `state/`, không ở `services/`.** Cấu trúc thư mục của một feature do [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §3 quyết; file này chỉ nói *store làm gì*. Hệ quả cần biết: cổng F12 (*"mọi `*.service.ts` có `.spec.ts` cạnh nó"*) **không** phủ file store — test cho store là yêu cầu riêng ở §9, luật F32, và chưa cổng nào canh.

**UI state cần chia sẻ được bằng đường link thì phải nằm trên URL, không nằm trong store.** Trang, sắp xếp, bộ lọc của một bảng thuộc nhóm này — xem [`11-grid-and-metadata.md`](11-grid-and-metadata.md) §3.

---

## 2. Vì sao KHÔNG dùng NgRx ở v1

Đây là quyết định có chủ đích, không phải bỏ sót.

### 2.1 Signals đã là cơ chế reactivity gốc

Signal là cơ chế reactivity của chính framework, và phiên bản Angular đã chốt ([`../../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md`](../../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md)) chạy được zoneless dựa trên nó. Một store viết bằng `signal()` + `computed()` không phải là "giải pháp tạm" — nó dùng đúng cơ chế mà `@if`, `@for` và change detection đang dùng.

### 2.2 Cái giá của một thư viện store, nói cụ thể

| Chi phí | Cụ thể là gì |
| --- | --- |
| Học | Mỗi người vào dự án phải hiểu thêm một mô hình (action/reducer/effect hoặc store/withMethods) trước khi sửa được một màn |
| Số file mỗi thay đổi | Thêm một trường vào state chạm nhiều file thay vì một |
| Debug | Lỗi đi qua một tầng trung gian nữa; stack trace dài hơn và ít liên quan tới code mình viết |
| Bundle | Một phụ thuộc nữa phải nâng cấp đồng bộ với Angular ([`16-nen-tang-va-nang-cap.md`](16-nen-tang-va-nang-cap.md) §3) |

Cái giá này đáng trả khi state thật sự phức tạp. Ở phạm vi Core hiện tại — vài màn quản trị, một menu, một phiên — nó **chưa đáng**.

### 2.3 Điều quan trọng: quyết định này rẻ để lật

Store viết tay ở §4 có cùng hình dạng bề mặt với một store thư viện: state riêng tư, `computed()` phái sinh, phương thức thay đổi. Nếu sau này thêm thư viện, phần phải viết lại nằm gọn trong file store, không lan ra component.

Đây là tiêu chí chọn quan trọng nhất khi hoãn một quyết định: **hoãn được thứ rẻ để lật, không hoãn thứ đắt để lật.**

---

## 3. Ba ngưỡng để cân nhắc thêm thư viện store — định nghĩa gốc

Ngưỡng khai ở đây và chỉ ở đây; tài liệu khác trỏ về mục này thay vì nhắc lại con số. **Thêm khi thoả ít nhất hai trong ba** — không thêm vì "dự án khác cũng có":

1. **Từ ba nơi trở lên cùng đọc và ghi một state**, và đường đi của dữ liệu đã khó vẽ ra trên giấy.
2. **Cần undo/redo, hoặc cần xem lại lịch sử thay đổi state** để tìm lỗi — đây là chỗ devtools của thư viện store thật sự ăn tiền.
3. **Có luồng bất đồng bộ chồng nhau cần huỷ lẫn nhau** (gõ tìm kiếm liên tục kèm nhiều request chờ), và việc tự quản lý bằng `switchMap` đã rải ra nhiều file.

Chưa thoả thì store viết tay là lựa chọn đúng, không phải lựa chọn tạm.

---

## 4. Khuôn store viết tay

**Màn danh sách không tự viết store** — dùng `ListStateStore`: [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.8.

> ✅ **CÓ THẬT** (đối chiếu 2026-11-06): khối dưới trích `src/FE/src/app/platform/he-thong/don-vi/state/tao-don-vi.store.ts`, chuỗi `export class TaoDonViStore` — store của một quy trình hộp thoại ([ADR-0049](../../adr/0049-trang-thai-quy-trinh-hop-thoai-o-state-khong-o-services.md)). Tệp thật là nguồn; `// …` là chỗ lược.

```typescript
/**
 * Trạng thái + quy trình hộp "Tạo đơn vị" (danh-sach-don-vi.page) — tách khỏi page để giữ
 * *.page.ts dưới ngưỡng dòng (fe-architecture.md §5, luật F22). Store cấp ở `providers` của page
 * (ADR-0049) nên chết cùng màn; `moHop()` reset lại trạng thái mỗi lần mở. `HopTaoDonViComponent`
 * KHÔNG biết store này: page đọc nó rồi truyền GIÁ TRỊ xuống bằng `input()` và nghe `output()` để
 * gọi hàm ở đây.
 */
@Injectable()
export class TaoDonViStore {
  private readonly service = inject(DonViService);
  // … translate, fb; form sáu ô kèm Validators

  private readonly _hienThi = signal(false);
  readonly hienThi = this._hienThi.asReadonly();
  private readonly _dangLuu = signal(false);
  readonly dangLuu = this._dangLuu.asReadonly();
  private readonly _daGui = signal(false);
  readonly daGui = this._daGui.asReadonly();
  private readonly _loiChung = signal<string | null>(null);
  readonly loiChung = this._loiChung.asReadonly();
  // … hienThiXacNhanHuy, lanGuiSai cùng khuôn; loiTruong (computed), chuanHoaMa(), dongHop()

  moHop(): void {
    this.form.reset({ /* … sáu ô về '' */ });
    this._daGui.set(false);
    this._loiChung.set(null);
    this._hienThi.set(true);
  }

  /** `thanhCong` chạy khi tạo xong — page lo Toast + `list.reload()`; store này không biết `ListStateStore`. */
  gui(thanhCong: (dv: DonVi) => void): void {
    // Chặn gửi hai lần (Enter ngầm, bấm đúp): lần hai sẽ nhận CODE_DUPLICATE giả.
    if (this.dangLuu()) return;
    // … chuẩn hoá mã; form không hợp lệ thì đánh dấu chạm và dừng
    const gt = this.form.getRawValue();
    this._dangLuu.set(true);
    this.service
      .tao(gt)
      .pipe(finalize(() => this._dangLuu.set(false)))
      .subscribe({
        next: (dv) => {
          this._hienThi.set(false);
          thanhCong(dv);
        },
        error: (err: unknown) => this.xuLyLoi(err),
      });
  }
}
```

**Quy tắc 1 có lệnh tự rà** — trường công khai khởi tạo bằng `signal` hay `linkedSignal` trong mọi `*.store.ts`. PASS khi không in dòng nào (đối chiếu 2026-11-06: không in gì). Lọt khai báo bẻ nhiều dòng và store không mang đuôi `.store.ts`; đây là lệnh tự rà, không phải cổng — review canh.

```bash
grep -rnE '^\s*(public\s+)?(readonly\s+)?[A-Za-z_]\w*\s*(:\s*[^=]+)?=\s*(signal|linkedSignal)\b' --include='*.store.ts' src/FE/src/app
```

### 4.1 Bốn quy tắc cứng

| Quy tắc | Vì sao |
| --- | --- |
| Bề mặt công khai chỉ đọc (`asReadonly()`) | Một `set()` gọi từ component là một đường thay đổi state không đi qua store — và nó sẽ được viết, vì nó ngắn hơn |
| Store **không** gọi `HttpClient` trực tiếp | Giữ ranh giới `services/`: mapper DTO → model sống ở service API, store chỉ giữ state |
| Component dumb (`components/`) **không** inject store, **không** import tệp store, **không** nhận store qua `input()` | Luật F11 cho `shared/components/` (phần `inject(`), F33 cho `components/` của feature (mọi đường nhập store). Component dumb nhận **giá trị** qua `input()` và phát `output()`; nhận một store — dù qua `input()` — là nó biết nghiệp vụ và hết dùng lại được, và F11 không thấy đường đó |
| **Không dùng chung một thực thể store giữa các feature** | Một thực thể store nhiều feature cùng đọc, ghi chính là "god component" ở dạng state: hai feature không tách được nữa. Một feature được có nhiều store |

Dùng lại *lớp* store của feature khác bằng một thực thể riêng không phạm quy tắc 4 — trạng thái không vượt ranh giới feature. Ca đang có: `src/FE/src/app/platform/quan-tri/nguoi-dung/state/tao-nguoi-dung.store.ts`, chuỗi `readonly timVaiTro = new TraCuuVaiTroStore(this.vaiTroService, this.translate);` — lớp của feature `vai-tro`, thực thể riêng của hộp tạo người dùng (ngoại lệ `new` ở [ADR-0049](../../adr/0049-trang-thai-quy-trinh-hop-thoai-o-state-khong-o-services.md) điều 2).

### 4.2 `providedIn: 'root'` hay `providers` của page

| Chọn | Khi nào | Hệ quả |
| --- | --- | --- |
| `providedIn: 'root'` | Session state, menu, theme, ngôn ngữ — thứ sống suốt phiên | Không bao giờ được dọn; mọi `effect()` trong đó sống mãi (§6) |
| `providers` của page mà route trỏ tới | Store của một màn: `ListStateStore` (luật 7 ở [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.8), store quy trình hộp thoại ([ADR-0049](../../adr/0049-trang-thai-quy-trinh-hop-thoai-o-state-khong-o-services.md) điều 2) | Page huỷ thì store huỷ cùng — dữ liệu cũ không sống sót sang lần vào sau |

**Mặc định cho store feature là `providers` của page** — hoặc của component thông minh nằm trong page; store chết cùng component cấp nó. Giữ store feature ở `root` nghĩa là quay lại màn cũ sẽ thấy dữ liệu của lần trước trong khi đang chờ tải lại — trạng thái nhập nhằng nhất để debug. Muốn giữ có chủ đích thì đó là **cache**, và cache phải có khoá và có luật vô hiệu hoá (§5).

**Bẫy — `providers` trong cấu hình `Route` không cho hệ quả trên.** Router của Angular 21 không huỷ injector của route khi rời màn; huỷ tự động là tính năng thử nghiệm phải bật (`withExperimentalAutoCleanupInjectors`, từ 21.1), và Core không bật — `src/FE/src/app/app.config.ts`, chuỗi `provideRouter(routes),`. Store khai ở đó sống suốt vòng đời ứng dụng. ADR-0049 điều 2 còn ghi *"(hoặc route)"*: với router hôm nay, nhánh đó không đạt điều *"chết cùng màn"* mà chính điều 2 đặt ra. Chưa store nào dùng nó — `grep -rn 'providers:' src/FE/src/app --include='*.routes.ts'` không in gì (đối chiếu 2026-11-06).

---

## 5. Cache theo khoá và vô hiệu hoá

Cache chỉ đáng làm khi có bằng chứng một dữ liệu bị gọi lại nhiều lần mà gần như không đổi. Ở Core này, ứng viên rõ ràng nhất là **menu theo permission** và **danh mục tra cứu**.

### 5.1 Khoá cache phải chứa mọi thứ làm đổi kết quả

> ✅ **CÓ THẬT** về ý (đối chiếu 2026-11-05): `src/FE/src/app/core/menu/menu.store.ts`, chuỗi `private khoaCache(userId: string): string {` — khoá gồm người dùng và ngôn ngữ. Khối dưới rút gọn, không trích nguyên văn: tên hàm, chữ ký và dấu ngăn khác tệp thật.

```typescript
// core/menu/menu.store.ts (rút gọn)
private readonly cache = new Map<string, MenuNode[]>();

private key(userId: string, lang: string): string {
  return `${userId}|${lang}`;   // đổi người dùng HOẶC đổi ngôn ngữ ⇒ khoá khác
}
```

**Bẫy:** bỏ sót một chiều trong khoá là lỗi bảo mật, không chỉ là lỗi hiển thị. Menu cache theo ngôn ngữ mà quên `userId` thì người đăng nhập sau thấy menu của người trước — tức thấy tên những màn mà họ không có quyền vào. Nguyên tắc: **khoá gồm mọi biến mà khi nó đổi, câu trả lời của server đổi.**

### 5.2 Bốn sự kiện làm bản sao hết hiệu lực

Hai phản ứng khác nhau, không gọi chung một tên. **Vô hiệu hoá**: bỏ mục khỏi cache để lần đọc sau đi server. **Nạp lại**: gọi server ngay cho dữ liệu đang hiển thị. Dữ liệu có cache mà đang hiện thì cần cả hai, theo thứ tự đó; dữ liệu không cache — màn danh sách qua `ListStateStore` — chỉ có nạp lại.

| Sự kiện | Phản ứng | Hậu quả nếu quên |
| --- | --- | --- |
| Đăng xuất / hết phiên | Vô hiệu hoá **toàn bộ** cache | Dữ liệu người dùng trước lộ sang người sau trên cùng máy |
| Đổi ngôn ngữ | Cache có nội dung đã dịch: khoá chứa ngôn ngữ (§5.1) thì mục cũ không còn bị trúng; khoá thiếu ngôn ngữ thì vô hiệu hoá. Thứ đang hiển thị: nạp lại | Menu và nhãn nửa tiếng này nửa tiếng kia |
| Ghi thành công lên tài nguyên X | X có cache thì vô hiệu hoá; X đang hiển thị thì nạp lại | Người dùng lưu xong, quay lại danh sách, thấy giá trị cũ và tưởng lưu hỏng |
| Nhận 403 khi thao tác vẫn hiển thị được | Vô hiệu hoá cache permission và menu, rồi nạp lại | Giao diện tiếp tục hiện thứ người dùng không còn quyền |

Dòng thứ ba là nguồn lỗi phổ biến nhất trong hệ quản trị: **ghi xong không làm mới danh sách.** `ListStateStore` sống theo page (§4.2), nên quay lại danh sách từ màn khác luôn tải mới; ca còn lại là ghi ngay trên màn danh sách — store quy trình gọi callback thành công, page gọi `list.reload()` trong đó (khuôn §4). Một tín hiệu chung kiểu *"tài nguyên X đã đổi"* chỉ cần khi X đang hiển thị ở chỗ không giữ tham chiếu tới store của nó — chưa có ca.

> Hôm nay (đối chiếu 2026-11-06): cache duy nhất của Core là menu. Đăng xuất vô hiệu hoá hết — `src/FE/src/app/core/menu/menu.store.ts`, chuỗi `this.cache.clear();`. 403 `CORE.AUTH.FORBIDDEN` làm mới quyền, rồi bỏ mục cache của người dùng và nạp lại menu — `src/FE/src/app/core/interceptors/error.interceptor.ts`, chuỗi `auth.lamMoiQuyen();`; `src/FE/src/app/core/auth/auth.service.ts`, chuỗi `this.menu.thuLai(d.id);`; `src/FE/src/app/core/menu/menu.store.ts`, chuỗi `this.cache.delete(this.khoaCache(userId));`. Đổi ngôn ngữ: v1 một ngôn ngữ, chưa có đường đổi; khoá đã chứa ngôn ngữ. Ghi thành công: nạp lại, không có cache để vô hiệu hoá — màn gọi `reload()` của `ListStateStore`, ví dụ `src/FE/src/app/platform/he-thong/don-vi/pages/danh-sach/danh-sach-don-vi.page.ts`, chuỗi `this.list.reload();`.

### 5.3 Không đặt thời gian sống ở v1

Cache ở v1 **không có TTL**. Nó chỉ bị vô hiệu hoá bởi các sự kiện ở §5.2.

Lý do: TTL biến "dữ liệu cũ" thành một thứ **phụ thuộc thời gian**, và lỗi phụ thuộc thời gian là loại khó tái hiện nhất. Thêm TTL khi có số đo cho thấy cache đang giữ dữ liệu quá lâu trong một luồng thật, không thêm cho chắc.

---

## 6. `effect()` — hai rủi ro thường bị gộp làm một

**(1) `effect()` gọi ngoài injection context ném lỗi lúc chạy, không lúc biên dịch.** Cùng họ với bẫy `inject()` ở [`02-http-envelope.md`](02-http-envelope.md) §5.2. Hợp lệ trong constructor hoặc trong khởi tạo field; gọi trong một phương thức thường thì phải tự truyền `injector`.

**(2) `effect()` tạo trong service `providedIn: 'root'` không bao giờ tự dọn.** Đúng theo thiết kế — service đó sống bằng đời app. Nhưng nó khác hẳn `effect()` trong component (tự huỷ khi component bị huỷ). Cần dừng theo một sự kiện nghiệp vụ (ví dụ ngừng theo dõi hoạt động khi đăng xuất) thì phải giữ `EffectRef` và tự gọi `.destroy()`.

**Và một quy tắc bao trùm cả hai:** `effect()` không phải nơi để cập nhật state. Cập nhật state từ trong effect tạo ra vòng phụ thuộc khó lần, và Angular không chặn: từ bản 19, ghi signal trong `effect()` được phép mặc định (`allowSignalWrites` thành tuỳ chọn bỏ) — chỉ review canh luật này. Giá trị phái sinh dùng `computed()`; phái sinh mà vẫn phải ghi được — tự về giá trị đầu khi một nguồn đổi — dùng `linkedSignal()`; ghi vào `FormControl` theo một input hay tham số route thì đi qua `ngOnChanges` hoặc subscription có `takeUntilDestroyed`. Để `effect()` cho tác dụng phụ thật sự ra ngoài (ghi `localStorage`, đặt thuộc tính trên `document`, gọi API bên thứ ba).

**Một ngoại lệ có tên** (người dùng chốt 2026-11-07): `ToastComponent` rút hàng đợi của `ToastService` ngay trong `effect()` — `src/FE/src/app/shared/ui/toast/toast.component.ts`, chuỗi `untracked(() => this.toast.layHet())`. `layHet()` trả hàng đợi về rồi ghi `_hangDoi.set([])` — `src/FE/src/app/core/toast/toast.service.ts`, chuỗi `this._hangDoi.set([]);`. Đây là giao thức hộp thư *nhận một lần* giữa `core/` và lớp bọc thư viện, không phải state phái sinh. Thứ được ghi là việc *đã lấy*, không phải một giá trị tính từ nguồn, nên `computed()` hay `linkedSignal()` không diễn đạt được. Biên hẹp: ngoại lệ chỉ phủ đúng lời gọi đó. Mọi `effect()` khác ghi signal đều nằm ngoài ngoại lệ, kể cả một hàng đợi khác rút theo cùng khuôn. Ngoại lệ mới nào cũng ghi vào đây. Các phương án đã loại: dòng 2026-11-07 ở [`../../adr/nhat-ky-quyet-dinh.md`](../../adr/nhat-ky-quyet-dinh.md).

---

## 7. Nhiều tab trình duyệt — chấp nhận không đồng bộ

Mỗi tab là một runtime Angular riêng. Sửa dữ liệu ở tab A thì store ở tab B **không hề biết**.

Ở quy mô một hệ quản trị nội bộ, đây là đánh đổi chấp nhận được: mở hai tab cùng một màn không phải luồng nghiệp vụ chính, và tự tải lại là chi phí nhỏ. **Không** làm đồng bộ đa tab ở v1.

Khi có luồng thật cần (một màn theo dõi mở song song một màn nhập liệu), cách rẻ nhất **không phải** phát toàn bộ state qua `BroadcastChannel` — làm thế thì hai tab chạy hai phiên bản code khác nhau sẽ lệch schema, và gói tin nặng hơn cả gọi lại API. Cách đúng là phát **một tín hiệu "tài nguyên X đã đổi"**, tab kia tự tải lại.

Một điều **phải** đồng bộ giữa các tab ngay cả ở v1: **đăng xuất**. Cookie phiên dùng chung cho mọi tab, nên đăng xuất ở tab A khiến tab B đang mở trở thành một giao diện chết — mọi thao tác trả 401. Xem [`07-auth-identity.md`](07-auth-identity.md) §7.

---

## 8. Optimistic update — chỉ cho thao tác đơn giản, và luôn có ảnh chụp

Chỉ áp cho thao tác **một trường, một dòng, dễ đảo ngược** (bật/tắt, đổi thứ tự). Không áp cho thao tác nhiều bước — khi đó "rollback về đâu" không còn rõ.

Quy tắc bắt buộc: **rollback bằng ảnh chụp state trước khi đổi, không bằng cách đảo ngược thao tác lần nữa.** Giữa lúc chờ API, một thay đổi khác có thể đã xen vào; đảo ngược thủ công sẽ đưa state về một trạng thái chưa từng tồn tại, còn gán lại ảnh chụp thì luôn đúng.

---

## 9. Test store

| Test gì | Cách |
| --- | --- |
| Trạng thái tải | Hàm tải với service giả trả Observable chậm → trạng thái là *đang tải* ở giữa, rời trạng thái đó khi xong — như `state()` của `ListStateStore` |
| Nhánh lỗi | Store server state: service giả ném lỗi có envelope → trạng thái là *lỗi*, không phải *rỗng*; store giữ tối đa mã lỗi, không giữ câu hay đối tượng lỗi — như `errorCode()`, luật 10 ở [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.8. Store quy trình hộp thoại: hàng cuối |
| Khoá cache | Gọi hai lần với hai khoá khác nhau → service bị gọi hai lần; cùng khoá → gọi một lần |
| Vô hiệu hoá | Phát sự kiện đăng xuất → lần gọi kế tiếp phải chạm service lại |
| Store quy trình hộp thoại | Mở → trạng thái được đặt lại; gửi thành công → cờ mở về `false` và callback chạy; **mỗi** mã lỗi mà store xử lý riêng có một ca; lỗi không kèm `fieldErrors` vẫn ra thông báo chung, không im lặng |

Không test `computed()` tầm thường (chỉ ánh xạ lại một field) — test đó chỉ khẳng định lại chính dòng code nó test. Xem [`06-testing-strategy.md`](06-testing-strategy.md) §5.

---

## 10. §Áp dụng — Core này có gì, cố ý thiếu gì

> Đối chiếu với `src/` ngày 2026-11-07, chỉ bằng đọc mã: hàng ghi ✅ CÓ THẬT có neo ở cột *Ghi chú*; hàng `❌` cũng đã đối chiếu là chưa có trong code. Ý nghĩa ký hiệu: [`../README.md`](../README.md) §9.

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Store viết tay bằng signal cho server state | ✅ CÓ THẬT 2026-11-06 | Màn danh sách: `ListStateStore` ([`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.8); khuôn ở §4 là store quy trình hộp thoại. Neo: `src/FE/src/app/core/list/list-state.store.ts` (chuỗi `export class ListStateStore<T>`) |
| `signal()` trần cho UI state trong page | ✅ CÓ THẬT 2026-11-06 | Neo: `src/FE/src/app/platform/ho-so/pages/ho-so/ho-so.page.ts` (chuỗi `protected readonly dangLuuThongTin = signal(false);`) |
| Session state ở `core/` | ✅ CÓ THẬT 2026-11-06 | Người dùng, permission, ngôn ngữ, chế độ hiển thị. Neo: `src/FE/src/app/core/auth/auth.service.ts` (chuỗi `readonly nguoiDung = this._nguoiDung.asReadonly();`), `src/FE/src/app/core/i18n/dong-bo-ngon-ngu.ts` (chuỗi `export class DongBoNgonNgu`), `src/FE/src/app/core/theme/theme.service.ts` (chuỗi `readonly preference = this._preference.asReadonly();`) |
| Trạng thái bảng đặt trên URL | ✅ CÓ THẬT 2026-11-06 | [`11-grid-and-metadata.md`](11-grid-and-metadata.md) §3. Neo: `src/FE/src/app/core/list/list-state.store.ts` (chuỗi `toSignal(this.route.queryParamMap.pipe(map(docQuery))`) |
| Cache theo khoá; phản ứng với bốn sự kiện ở §5.2 | ✅ CÓ THẬT 2026-11-06 | Cache duy nhất là menu; đổi ngôn ngữ chưa có đường đổi ở v1, khoá đã chứa ngôn ngữ. Từng sự kiện và neo ở khối *Hôm nay* cuối §5.2 |
| Thư viện store chuyên dụng (NgRx) | ❌ chưa | Điều kiện: thoả ngưỡng ở §3 |
| Cache có thời gian sống (TTL) | ❌ chưa | Điều kiện: có số đo cho thấy cache giữ dữ liệu quá lâu trong một luồng thật (§5.3) |
| Đồng bộ dữ liệu giữa nhiều tab | ❌ chưa | Điều kiện: có luồng thật mở hai tab song song (§7). Riêng đăng xuất thì làm ngay — [`07-auth-identity.md`](07-auth-identity.md) §7.3; có thật: `src/FE/src/app/core/auth/tab-session-broadcast.service.ts` (chuỗi `export const KHOA_BAO_TAB_PHIEN_KET_THUC`) |
| Optimistic update | ❌ chưa | Điều kiện: có thao tác một trường, dễ đảo ngược và cảm nhận được độ trễ (§8) |
| Một thực thể store dùng chung giữa các feature | ❌ loại, không hoãn `K14` | §4.1 quy tắc 4 — đây là "god component" ở dạng state |
| Cập nhật state bên trong `effect()` | ❌ loại, không hoãn `K15` | §6 — `computed()`, `linkedSignal()` cho giá trị phái sinh; một ngoại lệ có tên, rút hàng đợi `Toast`, cũng ở §6. Ba chỗ từng lệch nay dùng `linkedSignal`: `src/FE/src/app/shared/ui/dialog/dialog.component.ts` và `src/FE/src/app/shared/ui/drawer/drawer.component.ts`, chuỗi `protected readonly hienThi = linkedSignal(() => this.open());`; `src/FE/src/app/shared/components/confirm-dialog/confirm-dialog.component.ts`, chuỗi `protected readonly daDanhDau = linkedSignal<boolean, boolean>({` |

> Cách đọc ba ký hiệu của bảng trên — và khi nào *"FE thiếu X"* là finding: [`../README.md`](../README.md) §9.

---

## 11. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| Đặt file store ở đâu, đặt tên thế nào | [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §3 |
| State của bảng đặt trên URL thế nào | [`11-grid-and-metadata.md`](11-grid-and-metadata.md) §3 |
| Session state và permission | [`07-auth-identity.md`](07-auth-identity.md) |
| Vì sao `OnPush` và signal ăn nhập nhau | [`13-performance.md`](13-performance.md) §2 |
| Ranh giới component dumb / smart | [`05-component-library.md`](05-component-library.md) §5 |
