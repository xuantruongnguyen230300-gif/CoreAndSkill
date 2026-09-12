---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 03. Quản lý state — signal store viết tay, chưa dùng NgRx

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`; mọi mẫu dưới đây là thứ phải viết.
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
| Server state | Service store dạng signal, có khoá cache | `<feature>/state/<feature>.store.ts` |
| UI state | `signal()` trần trong page, phần cần chia sẻ link thì đặt trên URL | `<feature>/pages/…` |
| Session state | Service `providedIn: 'root'` trong `core/` | `core/auth`, `core/i18n`, `core/theme` |

> **Store sống ở `state/`, không ở `services/`.** Cấu trúc thư mục của một feature do [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §3 quyết; file này chỉ nói *store làm gì*. Hệ quả cần biết: cổng F12 (*"mọi `services/*.service.ts` có `.spec.ts` cạnh nó"*) **không** phủ file store — test cho store là yêu cầu riêng ở §9, và nó không có cổng đếm tên file nào canh.

**UI state cần chia sẻ được bằng đường link thì phải nằm trên URL, không nằm trong store.** Trang, sắp xếp, bộ lọc của một bảng thuộc nhóm này — xem [`11-grid-and-metadata.md`](11-grid-and-metadata.md) §3.

---

## 2. Vì sao KHÔNG dùng NgRx ở v1

Đây là quyết định có chủ đích, không phải bỏ sót.

### 2.1 Signals đã là cơ chế reactivity gốc

Từ Angular 17 trở đi, signal là cơ chế reactivity của chính framework, và Angular 20 chạy được zoneless dựa trên nó. Một store viết bằng `signal()` + `computed()` không phải là "giải pháp tạm" — nó dùng đúng cơ chế mà `@if`, `@for` và change detection đang dùng.

### 2.2 Cái giá của một thư viện store, nói cụ thể

| Chi phí | Cụ thể là gì |
| --- | --- |
| Học | Mỗi người vào dự án phải hiểu thêm một mô hình (action/reducer/effect hoặc store/withMethods) trước khi sửa được một màn |
| Số file mỗi thay đổi | Thêm một trường vào state chạm nhiều file thay vì một |
| Debug | Lỗi đi qua một tầng trung gian nữa; stack trace dài hơn và ít liên quan tới code mình viết |
| Bundle | Một phụ thuộc nữa phải nâng cấp đồng bộ với Angular ([`16-nen-tang-va-nang-cap.md`](16-nen-tang-va-nang-cap.md) §3) |

Cái giá này đáng trả khi state thật sự phức tạp. Ở phạm vi Core hiện tại — hai màn quản trị, một menu, một phiên — nó **chưa đáng**.

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

```typescript
// platform/quan-tri-nguoi-dung/state/user-list.store.ts
@Injectable()   // KHÔNG providedIn:'root' — store của feature sống theo route, xem §4.2
export class UserListStore {
  private readonly api = inject(UserApiService);

  // State riêng tư: chỉ store được ghi.
  private readonly _items = signal<User[]>([]);
  private readonly _total = signal(0);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);   // MÃ lỗi, không phải câu hiển thị

  // Bề mặt công khai chỉ đọc.
  readonly items = this._items.asReadonly();
  readonly total = this._total.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  readonly isEmpty = computed(() => !this._loading() && this._items().length === 0);

  async load(query: UserQuery): Promise<void> {
    this._loading.set(true);
    this._error.set(null);
    try {
      const page = await firstValueFrom(this.api.search(query));
      this._items.set(page.items);
      this._total.set(page.totalCount);
    } catch (e) {
      // Giữ MÃ lỗi; template dịch nó. Giữ câu của BE là khoá chặt vào ngôn ngữ server.
      this._error.set(e instanceof ApiFailureError ? e.body?.error.code ?? 'CORE.CLIENT.RESOURCE_LOAD_FAILED' : 'CORE.CLIENT.RESOURCE_LOAD_FAILED');
    } finally {
      this._loading.set(false);
    }
  }
}
```

### 4.1 Bốn quy tắc cứng

| Quy tắc | Vì sao |
| --- | --- |
| Bề mặt công khai chỉ đọc (`asReadonly()`) | Một `set()` gọi từ component là một đường thay đổi state không đi qua store — và nó sẽ được viết, vì nó ngắn hơn |
| Store **không** gọi `HttpClient` trực tiếp | Giữ ranh giới `services/`: mapper DTO → model sống ở service API, store chỉ giữ state |
| Component dumb (`components/`) **không** inject store | Luật F11. Component dumb nhận `input()` và phát `output()`; inject store là nó biết nghiệp vụ và hết dùng lại được |
| Một store cho một feature | Một store dùng chung cho nhiều feature không liên quan chính là "god component" ở dạng state |

### 4.2 `providedIn: 'root'` hay provide theo route

| Chọn | Khi nào | Hệ quả |
| --- | --- | --- |
| `providedIn: 'root'` | Session state, menu, theme, ngôn ngữ — thứ sống suốt phiên | Không bao giờ được dọn; mọi `effect()` trong đó sống mãi (§6) |
| Provide ở route của feature | Server state của một màn | Rời màn là store bị huỷ cùng — dữ liệu cũ không sống sót sang lần vào sau |

**Mặc định cho store feature là provide theo route.** Giữ store feature ở `root` nghĩa là quay lại màn cũ sẽ thấy dữ liệu của lần trước trong khi đang chờ tải lại — trạng thái nhập nhằng nhất để debug. Muốn giữ có chủ đích thì đó là **cache**, và cache phải có khoá và có luật vô hiệu hoá (§5).

---

## 5. Cache theo khoá và vô hiệu hoá

Cache chỉ đáng làm khi có bằng chứng một dữ liệu bị gọi lại nhiều lần mà gần như không đổi. Ở Core này, ứng viên rõ ràng nhất là **menu theo permission** và **danh mục tra cứu**.

### 5.1 Khoá cache phải chứa mọi thứ làm đổi kết quả

```typescript
// core/menu/menu.store.ts (rút gọn)
private readonly cache = new Map<string, MenuNode[]>();

private key(userId: string, lang: string): string {
  return `${userId}|${lang}`;   // đổi người dùng HOẶC đổi ngôn ngữ ⇒ khoá khác
}
```

**Bẫy:** bỏ sót một chiều trong khoá là lỗi bảo mật, không chỉ là lỗi hiển thị. Menu cache theo ngôn ngữ mà quên `userId` thì người đăng nhập sau thấy menu của người trước — tức thấy tên những màn mà họ không có quyền vào. Nguyên tắc: **khoá gồm mọi biến mà khi nó đổi, câu trả lời của server đổi.**

### 5.2 Bốn sự kiện bắt buộc xoá cache

| Sự kiện | Xoá gì | Hậu quả nếu quên |
| --- | --- | --- |
| Đăng xuất / hết phiên | **Toàn bộ** | Dữ liệu người dùng trước lộ sang người sau trên cùng máy |
| Đổi ngôn ngữ | Cache có nội dung đã dịch | Menu và nhãn nửa tiếng này nửa tiếng kia |
| Ghi thành công lên tài nguyên X | Cache của X | Người dùng lưu xong, quay lại danh sách, thấy giá trị cũ và tưởng lưu hỏng |
| Nhận 403 khi thao tác vẫn hiển thị được | Cache permission và menu | Giao diện tiếp tục hiện thứ người dùng không còn quyền |

Dòng thứ ba là nguồn lỗi phổ biến nhất trong hệ quản trị: **ghi xong không làm mới danh sách.** Cách rẻ nhất là store danh sách tự đăng ký một sự kiện `invalidate('users')` phát ra sau mỗi lần ghi, thay vì mỗi màn tự nhớ gọi `load()` lại.

### 5.3 Không đặt thời gian sống ở v1

Cache ở v1 **không có TTL**. Nó chỉ bị xoá bởi bốn sự kiện ở §5.2.

Lý do: TTL biến "dữ liệu cũ" thành một thứ **phụ thuộc thời gian**, và lỗi phụ thuộc thời gian là loại khó tái hiện nhất. Thêm TTL khi có số đo cho thấy cache đang giữ dữ liệu quá lâu trong một luồng thật, không thêm cho chắc.

---

## 6. `effect()` — hai rủi ro thường bị gộp làm một

**(1) `effect()` gọi ngoài injection context ném lỗi lúc chạy, không lúc biên dịch.** Cùng họ với bẫy `inject()` ở [`02-http-envelope.md`](02-http-envelope.md) §5.2. Hợp lệ trong constructor hoặc trong khởi tạo field; gọi trong một phương thức thường thì phải tự truyền `injector`.

**(2) `effect()` tạo trong service `providedIn: 'root'` không bao giờ tự dọn.** Đúng theo thiết kế — service đó sống bằng đời app. Nhưng nó khác hẳn `effect()` trong component (tự huỷ khi component bị huỷ). Cần dừng theo một sự kiện nghiệp vụ (ví dụ ngừng theo dõi hoạt động khi đăng xuất) thì phải giữ `EffectRef` và tự gọi `.destroy()`.

**Và một quy tắc bao trùm cả hai:** `effect()` không phải nơi để cập nhật state. Cập nhật state từ trong effect tạo ra vòng phụ thuộc khó lần, và Angular có cảnh báo riêng cho nó. Dùng `computed()` cho giá trị phái sinh; để `effect()` cho tác dụng phụ thật sự ra ngoài (ghi `localStorage`, đặt thuộc tính trên `document`, gọi API bên thứ ba).

---

## 7. Nhiều tab trình duyệt — chấp nhận không đồng bộ

Mỗi tab là một runtime Angular riêng. Sửa dữ liệu ở tab A thì store ở tab B **không hề biết**.

Ở quy mô một hệ quản trị nội bộ, đây là đánh đổi chấp nhận được: mở hai tab cùng một màn không phải luồng nghiệp vụ chính, và tự tải lại là chi phí nhỏ. **Không** làm đồng bộ đa tab ở v1.

Khi có luồng thật cần (một màn theo dõi mở song song một màn nhập liệu), cách rẻ nhất **không phải** phát toàn bộ state qua `BroadcastChannel` — làm thế thì hai tab chạy hai phiên bản code khác nhau sẽ lệch schema, và gói tin nặng hơn cả gọi lại API. Cách đúng là phát **một tín hiệu "tài nguyên X đã đổi"**, tab kia tự gọi lại `load()`.

Một điều **phải** đồng bộ giữa các tab ngay cả ở v1: **đăng xuất**. Cookie phiên dùng chung cho mọi tab, nên đăng xuất ở tab A khiến tab B đang mở trở thành một giao diện chết — mọi thao tác trả 401. Xem [`07-auth-identity.md`](07-auth-identity.md) §7.

---

## 8. Optimistic update — chỉ cho thao tác đơn giản, và luôn có ảnh chụp

Chỉ áp cho thao tác **một trường, một dòng, dễ đảo ngược** (bật/tắt, đổi thứ tự). Không áp cho thao tác nhiều bước — khi đó "rollback về đâu" không còn rõ.

Quy tắc bắt buộc: **rollback bằng ảnh chụp state trước khi đổi, không bằng cách đảo ngược thao tác lần nữa.** Giữa lúc chờ API, một thay đổi khác có thể đã xen vào; đảo ngược thủ công sẽ đưa state về một trạng thái chưa từng tồn tại, còn gán lại ảnh chụp thì luôn đúng.

---

## 9. Test store

| Test gì | Cách |
| --- | --- |
| Trạng thái tải | Gọi `load()` với service giả trả Observable chậm → `loading()` là `true` ở giữa, `false` ở cuối |
| Nhánh lỗi | Service giả ném lỗi có envelope → `error()` mang đúng **mã lỗi** của envelope, không phải chuỗi Angular sinh và không phải câu tiếng Việt viết cứng |
| Khoá cache | Gọi hai lần với hai khoá khác nhau → service bị gọi hai lần; cùng khoá → gọi một lần |
| Vô hiệu hoá | Phát sự kiện đăng xuất → lần gọi kế tiếp phải chạm service lại |

Không test `computed()` tầm thường (chỉ ánh xạ lại một field) — test đó chỉ khẳng định lại chính dòng code nó test. Xem [`06-testing-strategy.md`](06-testing-strategy.md) §5.

---

## 10. §Áp dụng — Core này có gì, cố ý thiếu gì

> 📐 Bảng dưới là **đích đến của giai đoạn 2**, không phải hiện trạng. Ý nghĩa ba ký hiệu trạng thái: [`../README.md`](../README.md) §9.

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Store viết tay bằng signal cho server state | ✅ sẽ có | Khuôn ở §4 |
| `signal()` trần cho UI state trong page | ✅ sẽ có |  |
| Session state ở `core/` | ✅ sẽ có | Người dùng, permission, ngôn ngữ, chế độ hiển thị |
| Trạng thái bảng đặt trên URL | ✅ sẽ có | [`11-grid-and-metadata.md`](11-grid-and-metadata.md) §3 |
| Cache theo khoá + bốn sự kiện vô hiệu hoá | ✅ sẽ có | §5.2 |
| Thư viện store chuyên dụng (NgRx) | ❌ chưa | Điều kiện: thoả ngưỡng ở §3 |
| Cache có thời gian sống (TTL) | ❌ chưa | Điều kiện: có số đo cho thấy cache giữ dữ liệu quá lâu trong một luồng thật (§5.3) |
| Đồng bộ dữ liệu giữa nhiều tab | ❌ chưa | Điều kiện: có luồng thật mở hai tab song song (§7). Riêng đăng xuất thì làm ngay — [`07-auth-identity.md`](07-auth-identity.md) §7.3 |
| Optimistic update | ❌ chưa | Điều kiện: có thao tác một trường, dễ đảo ngược và cảm nhận được độ trễ (§8) |
| Một store toàn cục dùng chung cho mọi feature | ❌ loại, không hoãn `K14` | §4.1 — đây là "god component" ở dạng state |
| Cập nhật state bên trong `effect()` | ❌ loại, không hoãn `K15` | §6 — dùng `computed()` cho giá trị phái sinh |

Một finding dạng *"FE thiếu X"* chỉ hợp lệ khi X mang trạng thái **✅ sẽ có** mà vắng mặt, hoặc khi điều kiện ở cột ghi chú của một dòng **❌ chưa** đã xảy ra. Dòng **❌ loại, không hoãn** chỉ đổi được bằng một ADR mới, không đổi được bằng một finding.

---

## 11. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| Đặt file store ở đâu, đặt tên thế nào | [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §3 |
| State của bảng đặt trên URL thế nào | [`11-grid-and-metadata.md`](11-grid-and-metadata.md) §3 |
| Session state và permission | [`07-auth-identity.md`](07-auth-identity.md) |
| Vì sao `OnPush` và signal ăn nhập nhau | [`13-performance.md`](13-performance.md) §2 |
| Ranh giới component dumb / smart | [`05-component-library.md`](05-component-library.md) §5 |
