---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0057 — Seam FE chỉ mang giá trị dựng được ở composition root: `CORE_HOME` là hàm nạp lười, `CORE_SCREEN_EXT` chỉ còn cột dạng `value(row)`

> **Trạng thái:** Đã chấp nhận (2026-09-22)

## Bối cảnh

Người dùng chốt thi công seam `CORE_HOME` ngay. `frontend-expert` dừng lại vì luật chỉ có một dòng mô tả ở [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §2.3 và §2.5, chưa có hợp đồng: chưa rõ kiểu của token, tệp đặt token, có hàm `provide…` hay không, và route trang chủ đọc token bằng cách nào. Route khai tĩnh nên không `inject()` được ở chỗ khai.

Cùng lượt đó, lượt soát FE nêu bốn lỗ trong hợp đồng `CORE_SCREEN_EXT` ở §2.7. Kiến trúc sư đối chiếu với `src/FE` ngày 2026-09-22:

| Đo gì | Kết quả |
| --- | --- |
| Ai đọc `rowActions`, `filterFields` | Không màn nào. `grep` chỉ thấy hai trường này ở `src/FE/src/app/platform/config/core-screen-ext.ts`, tức nơi khai. Dự án khai vào thì không có gì xảy ra, cũng không có lỗi nào |
| `rowActions` nhận được cú bấm không | Không. `UiMenuItem` (`src/FE/src/app/shared/ui/types.ts`) không có hàm xử lý và không mang dòng được bấm |
| `filterFields` vẽ ở đâu | `FilterPanel` và `Drawer` đều chưa dựng |
| Cột thêm cấp ô bằng gì | `DataColumnDef.cell` là `TemplateRef` **bắt buộc** trong `src/FE/src/app/shared/ui/data-table/data-table.component.ts` (`readonly cell: TemplateRef`), nhưng tuỳ chọn ở [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) §9. `header` là chuỗi đã dịch |
| Dự án khai token ở đâu | Ở cấu hình app, tức composition root. Ở đó không có view nào, nên không dựng được `TemplateRef`. `cell` và `toolbarSlot` vì vậy **không dự án nào cấp được** |
| Khoá `'permissions'` | Màn ma trận phân quyền không dùng `ListStateStore` nên không phải màn danh sách. Cổng F25 cũng không coi nó là màn danh sách |
| Router có chạy `loadComponent` trong ngữ cảnh tiêm không | Có. `node_modules/@angular/router/fesm2022/router2.mjs` (bản `20.3.31` đang cài) gọi `runInInjectionContext(injector, () => route.loadComponent())` |

Repo Core không có dự án hạ nguồn nào ([`0032-module-mau-o-du-an-ha-nguon.md`](0032-module-mau-o-du-an-ha-nguon.md)), nên chưa có nơi nào dùng thật seam này.

## Quyết định

Kiến trúc sư chốt năm điểm. Hợp đồng chi tiết nằm ở [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §2.5 và §2.7. ADR này chỉ ghi lý do.

1. **Mọi giá trị đi qua seam FE phải dựng được ở composition root.** Không `TemplateRef`, không component import tĩnh. Chuỗi hiển thị đi dưới dạng **khoá i18n**, màn Core dịch khi dựng.
2. **`CORE_HOME` mang một hàm nạp lười** cùng hình dạng với `loadComponent`. Token khai ở `core/config/`, kèm `provideCoreHome(load)`. Route trang chủ của Core gọi `inject(CORE_HOME, { optional: true })` ngay trong `loadComponent`. Không có token thì route dùng trang chào của Core. Không có component bọc.
3. **Cột thêm vào màn danh sách Core có kiểu `ScreenExtColumn<T>`.** Tiêu đề là khoá i18n, ô là `value(row)` trả chuỗi đã định dạng, và cột không sắp xếp được. Token có kiểu theo từng mã màn: `users`, `roles`, `tenants`. Dự án cấp token qua `provideCoreScreenExt(factory)`, và factory chạy trong ngữ cảnh tiêm.
4. **`rowActions`, `filterFields`, `toolbarSlot` rút khỏi hợp đồng từ hôm nay.** Khi dự án hạ nguồn đầu tiên cần một trong ba thì thêm lại bằng một ADR mới, ở dạng callback và không dùng `TemplateRef`.
5. **`ColumnDef` (§9) có thêm `value?: (row) => string`.** Mỗi cột có đúng một trong hai: `cell` hoặc `value`. `DataTable` vẽ `value` thành chữ khi cột không có `cell`.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — `CORE_HOME` mang thẳng `Type<unknown>`

**Được:** kiểu đơn giản nhất. Route chỉ cần `loadComponent: () => inject(CORE_HOME)`.

**Mất:** để có `Type` thì cấu hình app phải import tĩnh component trang chủ. Component đó cùng mọi thứ nó kéo theo (bảng tổng hợp, component biểu đồ) sẽ vào **bundle khởi động**. Điều này đi ngược luật lazy-load của route ([`../quy-uoc/fe-routing-guard.md`](../quy-uoc/fe-routing-guard.md), F20), ăn vào ngân sách F14, và làm hỏng tinh thần ràng buộc 1 của [`0019-ba-component-nang-thuoc-core.md`](0019-ba-component-nang-thuoc-core.md).

**Vì sao loại:** bảng tổng hợp là thứ nặng nhất dự án sẽ đặt vào đây, và nó phải nằm ở chunk tải chậm.

### Phương án B — Component bọc có `NgComponentOutlet`

**Được:** không phụ thuộc việc router chạy `loadComponent` trong ngữ cảnh tiêm.

**Mất:** thêm một component chỉ để chuyển tiếp. Component đó tự giữ trạng thái đang tải và lỗi tải. Trang chủ thật bị lồng thêm một tầng DOM.

**Vì sao loại:** router bản đang dùng đã cho `inject()` trong `loadComponent`, nên component bọc không giải thêm vấn đề nào.

### Phương án C — Dự án tự sửa route trang chủ

**Vì sao loại:** vi phạm luật #1 của [`0016-phan-phoi-core-bang-clone.md`](0016-phan-phoi-core-bang-clone.md), vì dự án không sửa tệp thuộc Core. Đây cũng chính là lý do seam tồn tại.

### Phương án D — Giữ `rowActions` và chốt ngay hợp đồng `onSelect(row)`

**Được:** dự án có sẵn điểm mở rộng hành động dòng mà không phải chờ Core.

**Mất:** ba màn Core phải dựng một cột hành động mà [`../Design/Screens/10-nguoi-dung.md`](../Design/Screens/10-nguoi-dung.md) nói màn Core không có, và chưa có spec giao diện nào cho cột đó. Hình dạng callback cũng phải đoán khi chưa có ai dùng: chỉ điều hướng, mở hộp thoại, hay cần kiểm quyền.

**Vì sao loại:** số nơi cần hôm nay là 0, dưới ngưỡng ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.2. Thêm một trường tuỳ chọn vào sau này không phá dự án nào, nên để hợp đồng đợi nhu cầu thật là rẻ. Ngược lại, giữ một trường khai mà không màn nào đọc đúng là kiểu hỏng *"im lặng không mở rộng được"* mà §2.7 muốn tránh.

### Phương án E — Ô của cột thêm là component (`NgComponentOutlet` với input `row`)

**Được:** ô vẽ được link, badge, nút.

**Mất:** mỗi màn danh sách phải dựng thêm một đường vẽ component động. Component import tĩnh ở cấu hình app lại quay về vấn đề bundle của phương án A.

**Vì sao loại:** ví dụ gốc của seam ([`../wiki-core/fe/ly-do/fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §2.7) là cột *"Mã nhân viên"*, tức một chuỗi. Thêm `component` sau này là thay đổi cộng thêm.

### Phương án F — Giữ `Readonly<Record<string, ScreenExtension<unknown>>>`

**Vì sao loại:** khoá gõ sai (`'user'`) bị bỏ qua mà không ai biết. Mỗi màn cũng phải ép kiểu `as ScreenExtension<X>` để đọc dòng. Kiểu theo từng mã màn cộng với hàm `provide…` có kiểu biến cả hai lỗi này thành lỗi biên dịch.

## Hệ quả

### Tích cực

- `frontend-expert` có đủ bốn điểm của `CORE_HOME` để thi công.
- Hợp đồng seam không còn trường nào khai ra mà không có tác dụng. Mọi trường còn lại đều có một màn đọc nó, và F25 canh việc đọc.
- Khoá màn gõ sai và kiểu dòng sai đều thành lỗi biên dịch ở dự án hạ nguồn.
- Trang chủ của dự án nằm ở chunk tải chậm.

### Tiêu cực

- **Cột của dự án chỉ vẽ được chữ**: không link, không badge, không nút. Dữ liệu riêng của dự án phải tới qua service mà factory `inject()`. Core không cấp kênh dữ liệu nào cho cột thêm.
- **Hành động dòng, trường lọc và nút trên thanh công cụ không còn điểm mở rộng.** Nhu cầu đầu tiên của một dự án hạ nguồn cho bất kỳ thứ nào trong ba thứ này sẽ thành một PR Core, một ADR và một tag, chứ không thành một dòng cấu hình.
- **Model FE của ba màn Core (`NguoiDung`, `VaiTro`, `DonVi`) thành bề mặt công khai.** Đổi tên một trường là gãy biên dịch ở dự án hạ nguồn. Lỗi lộ ra rõ ràng, nhưng vẫn là thay đổi phá vỡ và phải ghi khi phát hành.
- **Quên khai `CORE_HOME` không báo lỗi.** Dự án sẽ thấy trang chào của Core thay cho bảng tổng hợp của mình. Điều này lệch khỏi luật *"token không có giá trị mặc định"* ở §2.5. Kiến trúc sư chấp nhận vì trang chào không mang tên sản phẩm nào sai.
- **Route trang chủ dựa vào một hành vi của router**, là chạy `loadComponent` trong ngữ cảnh tiêm. Nếu một lần nâng Angular bỏ hành vi đó thì trang chủ nổ lỗi ngữ cảnh tiêm ngay khi vào. Test route của `platform/trang-chu/` phải bắt được ca này, vì không cổng nào khác bắt.
- **Cột thêm không sắp xếp được.** `sortBy` chỉ nhận allowlist của endpoint Core (nợ B3 ở [`../RULES.md`](../RULES.md) §10), và endpoint đó không biết cột của dự án.
- **`DataTable` có thêm một đường vẽ (`value`) mà hôm nay chỉ seam dùng.**

### Dấu hiệu quyết định này bắt đầu sai

- Dự án hạ nguồn đầu tiên cần ô dạng link hoặc badge ngay ở cột đầu tiên nó thêm. Khi đó phương án E phải được xét lại.
- Nhiều dự án cùng xin hành động dòng. Khi đó đến lúc viết hợp đồng callback, lần này có người dùng thật để đối chiếu.
- Một dự án dựng lại màn danh sách Core trong `modules/` chỉ vì cột dạng chữ không đủ. Đó là fork vô hình mà [`../wiki-core/fe/ly-do/fe-architecture.md`](../wiki-core/fe/ly-do/fe-architecture.md) §2.7 cảnh báo, và nó nghĩa là seam đã quá hẹp.

### Rút lui nếu sai

Chưa dự án hạ nguồn nào dùng seam, nên hôm nay đảo quyết định chỉ tốn công sửa ba màn và tài liệu. Về sau:

- **Nới hợp đồng**, tức thêm `rowActions` dạng callback hoặc thêm `component` cho ô, là thay đổi cộng thêm. Viết ADR mới, thêm trường, sửa ba màn, mở rộng canary F25. Dự án đang dùng không phải sửa gì.
- **Đổi `CORE_HOME` sang `Type`** phá vỡ mọi dự án đã khai. Mỗi dự án sửa một dòng `provideCoreHome`, và phải ghi vào ghi chú phát hành của tag Core.

## Liên quan

- [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §2.3, §2.5, §2.7: hợp đồng, là nguồn duy nhất.
- [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) §9: `ColumnDef` có thêm `value`.
- [`0016-phan-phoi-core-bang-clone.md`](0016-phan-phoi-core-bang-clone.md): lý do dự án chỉ mở rộng qua seam.
- Chưa có spec màn trang chủ trong [`../Design/Screens/`](../Design/Screens/). Seam không cần spec đó, nhưng phần *"lối tắt tới các màn người dùng có quyền"* của trang chào Core thì cần.
