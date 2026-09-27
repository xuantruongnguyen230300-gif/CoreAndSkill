---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0078 — Cột "Nền" giữ nguyên quyền quyết thư mục; `Button` và `Input` dời sang `shared/components/`, `shared/ui/` không được định nghĩa lại

> **Trạng thái:** Đã chấp nhận (2026-09-23)

## Bối cảnh

Lượt soát FE ngày 2026-09-23 báo về một mâu thuẫn giữa hai tệp `kind: luat`: [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) §2.1 nói component *tự dựng* nằm ở `shared/components/`, trong khi [`../Design/Components/Button.md`](../Design/Components/Button.md) khai `Nền: tự dựng` rồi neo vào một tệp dưới `shared/ui/`. Đề nghị kèm theo là *"chốt một chiều"* — hoặc dời code, hoặc định nghĩa lại `shared/ui/`.

Đối chiếu lại cùng ngày cho một bức tranh khác, và sự khác biệt đó quyết định câu trả lời: **không có hai luật đối lập.** Có **một** luật, khai nhất quán ở bốn chỗ, và **hai** component không theo nó.

Chuỗi thẩm quyền, đọc từ nguồn xuống:

- [`../Design/COMPONENTS.md`](../Design/COMPONENTS.md) §4 là nguồn. Cột **"Nền"** nhận đúng hai giá trị, `bọc PrimeNG` hoặc `tự dựng`. Ở đó `Button` là `tự dựng`, `Input` là `tự dựng`, `Check` là `bọc PrimeNG`.
- [`../wiki-core/fe/05-component-library.md`](../wiki-core/fe/05-component-library.md) §3 khai quy tắc ánh xạ và khai luôn chiều thẩm quyền, thành câu: `bọc PrimeNG` → `shared/ui/<tên>/`, `tự dựng` → `shared/components/<tên>/`, *"cột đó là nguồn, thư mục đuổi theo"*, *"muốn đổi thư mục của một component thì đổi cột 'Nền' ở `Design/` trước"*.
- [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) §2.1 và [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §2.2 nói cùng chiều.
- Allowlist của luật **F5** ở [`../wiki-core/fe/05-component-library.md`](../wiki-core/fe/05-component-library.md) §2.4 — bảng đó là nguồn duy nhất của allowlist và đã có dòng đăng ký trong [`../OWNERSHIP.md`](../OWNERSHIP.md) — cho `src/app/shared/ui/**` được import `primeng/*` và cấm `src/app/shared/components/**`.

Phép đếm trên cây `src/FE/src/app/shared/` ngày 2026-09-23: hai mươi bảy thư mục component, đối chiếu từng cái với cột "Nền". **Hai mươi lăm khớp. Hai lệch:** `button` và `input`, cả hai khai `tự dựng` mà nằm dưới `shared/ui/`. Không có ca lệch nào khác, và không có ca nào lệch theo chiều ngược lại.

`Button.md` và `Input.md` cùng mang `kind: luat`, cùng khai `Nền: tự dựng, không bọc PrimeNG`, và cùng có một dòng `Component: src/FE/src/app/shared/ui/…` dưới banner `🚧`. Dòng đó là **lời khai hiện trạng**, không phải một luật thứ hai — nó ghi đúng chỗ code đang nằm. Nhưng nó ghi chỗ sai như một sự thật bình thường, không đánh dấu là lệch, nên đọc nhanh thì trông hệt như một luật đối lập. Đó là cách một vi phạm trở nên vô hình.

Ba phép đo nữa, cùng ngày, trên mã nguồn:

- **Chỗ đặt sai đang tắt một cổng đang xanh.** Allowlist F5 cấp quyền theo **cây**: `src/app/shared/ui/**`. `button` và `input` nằm trong cây đó, nên thêm một dòng `import { ButtonModule } from 'primeng/button';` vào `button.component.ts` **không làm F5 đỏ**. Câu `không bọc PrimeNG` của `Button.md` vì thế hôm nay **không có cổng nào ép**, dù cổng ép đúng câu đó tồn tại, đã chạy và đang xanh. Đây là chuyện có thật hôm nay, không phải rủi ro tương lai.
- **Và nó khoá một chiều import.** Zone `sharedUiZones` (luật **F24**, [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §4.6) cấm `shared/ui/**` import `shared/components/**`. Đứng ở `shared/ui/`, `Button` và `Input` vĩnh viễn không dùng được component tự dựng nào. Ngày cần đến, người sửa gặp lint đỏ và lối ra dễ nhất trong tay họ là tắt rule — thứ [ADR-0007](0007-fe-giu-cau-truc-thu-muc.md) cấm.
- **`Check` đúng chỗ, và banner đang nói ngược.** `src/FE/src/app/shared/ui/check/check.component.ts` có `import { CheckboxModule } from 'primeng/checkbox';` và `import { RadioButtonModule } from 'primeng/radiobutton';`; `Check.md` khai `bọc PrimeNG`. Banner của [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) lại liệt `Check` vào nhóm *"lệch chỗ đặt"* cùng `Input`, và **không** nhắc `Button`. Làm theo banner đó là đưa một tệp có import `primeng/*` ra khỏi allowlist F5 — cổng đỏ ngay.

Một chướng ngại đã biết cho việc dời: `src/FE/src/app/shared/ui/types.ts` có `import type { SegmentOption } from './input/input.component';`, dùng cho trường `options` của `FilterField`. Dời `input` mà không xử lý dòng đó biến nó thành một import từ `shared/ui/` vào `shared/components/`, tức F24 đỏ.

Một câu sai nữa nằm cùng tệp: §6.1 của [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) xếp `app-check` và `app-date-picker` vào nhóm *"control tự dựng ở `shared/components/`"*, trong khi cột "Nền" khai cả hai là `bọc PrimeNG`. Luật đăng ký `ControlValueAccessor` không phụ thuộc tầng — `check.component.ts` cài `ControlValueAccessor` và nằm ở `shared/ui/`.

## Quyết định

`architect` chốt ba điều:

1. **Cột "Nền" ở [`../Design/COMPONENTS.md`](../Design/COMPONENTS.md) §4 giữ nguyên vai nguồn duy nhất quyết thư mục.** `shared/ui/` **không** được định nghĩa lại thành *"component lá"* hay bất kỳ khái niệm hình dạng nào. `shared/ui/` là **tầng được phép import `primeng/*`**, và định nghĩa đó do allowlist F5 giữ, không do trực giác về độ phức tạp của component.
2. **`Button` và `Input` dời sang `shared/components/`. `Check` ở lại `shared/ui/`. Không component nào khác dời.** Sau lượt dời, `src/FE/src/app/shared/ui/types.ts` không được còn phụ thuộc nào vào `shared/components/`, và F24 phải xanh **không** bằng một `eslint-disable`.
3. **Ba câu tài liệu khai sai được sửa theo.** Banner của [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) (nêu đúng hai ca lệch, không nêu `Check`) và câu §6.1 xếp `app-check`/`app-date-picker` sai tầng — `architect` sửa ngay trong lượt này. Hai dòng neo ở [`../Design/Components/Button.md`](../Design/Components/Button.md) và [`../Design/Components/Input.md`](../Design/Components/Input.md) **chỉ đổi khi code đã dời**: neo phải mô tả chỗ thật, nên sửa trước là dựng một lời khai sai theo chiều ngược lại.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Định nghĩa lại `shared/ui/` là "component lá", giữ code nguyên

Đây là phương án *"sửa luật"* mà lượt soát đặt lên bàn, và là phương án đơn giản hơn: không dòng code nào đổi.

**Được:** không churn. Lập luận nghe thuận tai — `Button` và `Input` là lá, chúng không ghép gì từ `shared/ui/`, nên đứng ở tầng mô tả là *"ghép TỪ `shared/ui/`"* thì trái tên gọi.

**Vì sao loại:** hai lý do đo được.

Thứ nhất, `shared/ui/` không phải một khái niệm về **hình dạng** component; nó là một **đặc quyền**. Đúng cây đó được import `primeng/*`, và F5 là cổng giữ điều đó. Định nghĩa lại theo "lá" thì `IconButton`, `Badge`, `Avatar`, `SkeletonLoader`, `ProgressBar`, `FilterChip` cũng là lá — mỗi cái chuyển vào là thêm một tệp mà F5 thôi canh. Phương án "không chạm code" trên thực tế chạm nhiều code hơn hẳn phương án dời hai thư mục, và nó trả bằng thứ đắt hơn: phạm vi mà một cổng đang canh.

Thứ hai, nó đảo chiều thẩm quyền mà [`../wiki-core/fe/05-component-library.md`](../wiki-core/fe/05-component-library.md) §3 đã khai thành câu. Sau khi đảo, không còn phép thử máy kiểm được nào cho *"component này thuộc thư mục nào"* — chỉ còn phán đoán "lá hay không lá", mà `FormRow`, `Card`, `ConfirmDialog` nằm đúng giữa. Đổi một quy tắc đọc-được-bằng-máy lấy một quy tắc phải tranh luận là đi lùi, kể cả khi hôm nay nó rẻ hơn.

### Phương án B — Giữ nguyên cả hai bên, ghi một ngoại lệ có tên cho `Button` và `Input`

**Được:** rẻ nhất trong ba phương án. Không dời thư mục, không sửa import, không sửa neo.

**Vì sao loại:** ngoại lệ rơi đúng vào hai component **được dùng nhiều nhất** — gần như mọi màn trong `platform/` import ít nhất một trong hai. Một ngoại lệ đặt ở chỗ đông người qua lại là ngoại lệ sẽ được chép, và người chép sẽ chép cả hình dạng lẫn chỗ đặt mà không đọc lý do. Nó cũng không gỡ được thứ đang thật: câu `không bọc PrimeNG` của `Button.md` vẫn không có cổng nào ép.

### Phương án C — Dời cả `Check` theo, đúng như banner đang mô tả

**Vì sao loại:** sai phép đo, và cái sai đó nằm ngay trong banner. `check.component.ts` import `primeng/checkbox` và `primeng/radiobutton`; dời nó ra khỏi `shared/ui/` làm F5 đỏ ở lần chạy kế tiếp. Banner khai `Check` là ca lệch — câu đó sai, và quyết định 3 sửa nó. Đây là ví dụ cho vì sao **không** nên đọc banner như nguồn: nó ghi lại nhận định của một lượt đối chiếu, không ghi lại luật.

## Hệ quả

### Tích cực

- Câu `Nền: tự dựng, không bọc PrimeNG` của `Button.md` và `Input.md` từ nay **được cổng F5 ép thật**, không chỉ được viết ra. Đây là thứ duy nhất trong ADR này đổi ngay hành vi của một cổng đang chạy.
- Quy tắc "cột Nền → thư mục" đúng trở lại cho toàn bộ cây `shared/`, nên nó lại là một phép thử máy kiểm được: một cổng sau này chỉ cần đọc hai bảng, không cần mang theo danh sách ngoại lệ.
- Mẫu mã ở §6.1 của [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) chú `shared/components/input/input.component.ts` trở thành đúng mà không phải sửa mẫu — đường dẫn đó hôm nay không tồn tại, nên một agent thi công theo mẫu sẽ tạo tệp thứ hai.

### Tiêu cực

- **Churn nhìn thấy được, không mang lại tính năng nào.** Hai thư mục dời, và mọi tệp import chúng phải sửa đường dẫn: đếm ngày 2026-09-23, năm tệp trong `shared/components/` đang import `../../ui/button/button.component`, và gần hai chục tệp trong `platform/` import `ButtonComponent` hoặc `InputComponent`. Một diff rộng như vậy dễ trộn lẫn với thay đổi thật, và người review sẽ lướt qua nó.
- **Một quyết định nhỏ bị đẩy sang `frontend-expert` mà ADR này cố ý không chốt:** chỗ đặt của `SegmentOption` và `FilterField` sau khi `input` dời. `FilterPanel` mang `Nền: tự dựng`, nên có thể `FilterField` vốn không thuộc tệp kiểu của `shared/ui/` ngay từ đầu — nhưng đó là một câu hỏi về ranh giới kiểu dùng chung, và quyết nó ở đây, không có phép đo, là đoán.
- **Cột "Nền" trở thành thứ đổi rất đắt.** Đổi một ô trong `Design/COMPONENTS.md` từ nay kéo theo một lượt dời thư mục và một lượt sửa import. Đó là cái giá của việc để `Design/` cầm quyền; nó chấp nhận được, nhưng người sửa ô đó phải biết trước, và hôm nay không câu nào ở `COMPONENTS.md` nói điều đó.
- **Neo trong `Button.md` và `Input.md` sai trong khoảng giữa** — từ khi ADR này được chấp nhận tới khi code dời xong. Không cổng nào bắt: luật **D41** và **D43** ([`../DEBT.md`](../DEBT.md)) đều chưa có cổng, và `check-docs.sh` không bao giờ `grep` một chuỗi trong `src/`. Khoảng này chỉ đóng được bằng việc `frontend-expert` làm quyết định 2 và phần neo của quyết định 3 trong **cùng một lượt**.

### Dấu hiệu quyết định này bắt đầu sai

Một component thứ ba mang `Nền: tự dựng` mà có lý do kỹ thuật **thật sự** buộc nó nằm trong `shared/ui/`. Lúc đó tiêu chí phân tầng đã không còn trùng với tiêu chí cấp quyền import, và cả quy tắc lẫn allowlist phải xét lại cùng lúc — không phải bằng một ngoại lệ.

## Liên quan

- [`../wiki-core/fe/05-component-library.md`](../wiki-core/fe/05-component-library.md) §2.4 và §3 — allowlist F5 và quy tắc ánh xạ "Nền" → thư mục.
- [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §2.2, §4.6 — sơ đồ `shared/` và zone `sharedUiZones` của luật F24.
- [`0007-fe-giu-cau-truc-thu-muc.md`](0007-fe-giu-cau-truc-thu-muc.md) — ranh giới FE ép bằng ESLint, và lệnh cấm `eslint-disable`.
- [`0037-f2-dung-du-component-core-khong-hoan-ngam.md`](0037-f2-dung-du-component-core-khong-hoan-ngam.md) — khuôn "không hoãn ngầm", cùng lý do quyết định 3 không cho sửa neo trước khi code dời.
