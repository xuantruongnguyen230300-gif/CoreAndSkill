---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0126 — Hoãn việc chuyển bộ chạy test FE sang Vitest ra khỏi đợt nâng Angular 21: làm ở PR riêng ngay sau khi PR nâng cấp hợp nhất, muộn nhất trước khi nâng Angular 22

> **Trạng thái:** Đã chấp nhận (2026-11-06)

## Bối cảnh

[ADR-0028](0028-toolchain-fe-va-ke-hoach-nang-cap.md) quyết định 5 chốt: *Karma + Jasmine, trình duyệt ChromeHeadless; chuyển sang Vitest cùng đợt nâng lên 21*. Lý do nằm ở phương án C của ADR đó: ở bản 20, Vitest còn là thử nghiệm. Đổi bộ chạy cùng đợt nâng 21 là đổi một lần, khi nó không còn là thử nghiệm.

Đợt nâng lên 21 đang nằm trong một PR chưa hợp nhất. PR đó còn gộp ba việc khác: [ADR-0123](0123-chuyen-dong-component-primeng-di-bang-token-qua-css-go-hang-ms-f37-het-ap-dung.md) (chuyển động PrimeNG qua CSS), [ADR-0124](0124-go-nua-provider-hoat-anh-giam-chuyen-dong-dua-vao-css-toan-cuc-va-safe-f39-het-ap-dung.md) (gỡ provider hoạt ảnh) và phần observability.

Đo ngày 2026-11-06, đọc từ `src/FE`:

| Đo gì | Kết quả |
| --- | --- |
| Framework đã nâng | `src/FE/package.json`, chuỗi `"@angular/core": "21.2.26"` |
| Bộ chạy test đang cấu hình | `src/FE/angular.json`, chuỗi `"builder": "@angular/build:karma"` — vẫn Karma |
| Bộ chạy mặc định của Angular 21 | Gói `@angular/build` đã cài, tệp `src/builders/unit-test/schema.json`: builder `unit-test` có khoá `runner` mặc định `"vitest"`, nhận `"karma"` hoặc `"vitest"` |
| Karma ở upstream | Gói `karma` đã cài, tệp `README.md`: *"Karma is deprecated and is not accepting new features or general bug fixes."* Vẫn chạy trên 21: lượt nâng ghi `ng test` xanh với builder Karma ([`../wiki-core/fe/16-nen-tang-va-nang-cap.md:235`](../wiki-core/fe/16-nen-tang-va-nang-cap.md)) |

Chi phí chuyển, đếm bằng lệnh chạy trong `src/FE`. Không chép số, theo [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §6:

| Hạng mục | Vì sao tốn công | Đếm bằng |
| --- | --- | --- |
| Spec dùng API riêng của Jasmine | `jasmine.*`, `spyOn`, `createSpyObj` không có trong Vitest dưới cùng tên | `grep -rlE 'jasmine\.\|(^\|[^.])spyOn\(\|createSpyObj' src --include='*.spec.ts' \| wc -l`, so với `find src -name '*.spec.ts' \| wc -l` |
| Spec đo bố cục thật | Đọc `getComputedStyle`, `getBoundingClientRect` của DOM có CSS thật. Môi trường DOM giả không tính style, nên các spec này cần browser mode của Vitest. ADR-0123 quyết định 5 thêm loại spec này cho mỗi lớp bọc | `grep -rlE 'getBoundingClientRect\|getComputedStyle' src --include='*.spec.ts' \| wc -l` |
| Sàn coverage theo tầng, luật F15 | Viết dưới dạng một reporter riêng của Karma, không phải cấu hình | `src/FE/karma.conf.js`, chuỗi `const SAN_NHANH` và chuỗi `'reporter:coverage-floor'` |
| Bước CI | Hai bước gọi thẳng cờ của Karma | `.github/workflows/docs-gate.yml`, chuỗi `name: 3. ng test` và chuỗi `name: 3b. ng test --code-coverage`; cả hai chạy `--browsers=ChromeHeadless` |

Lý do *phải cùng đợt* ở [`../wiki-core/fe/06-testing-strategy.md`](../wiki-core/fe/06-testing-strategy.md) §7 không còn đúng. Đoạn *Vitest không dùng ở v1* viện rằng *đổi bộ chạy và đổi framework trong hai đợt tách rời là hai lần viết lại cấu hình test*. Lượt nâng 21 giữ nguyên `karma.conf.js` mà `ng test` vẫn xanh. Vậy tách hai đợt chỉ tốn **một** lần viết lại cấu hình test, không phải hai.

Người dùng chốt ngày 2026-11-06: hoãn.

## Quyết định

**Người dùng chốt: việc chuyển bộ chạy test sang Vitest không làm trong PR nâng Angular 21.** Nó làm ở một PR riêng, mở ngay sau khi PR hiện tại hợp nhất, và **muộn nhất trước khi nâng Angular 22**. Kiến trúc sư ghi phần đi kèm:

1. **ADR-0028 tạm hoãn một phần.** Chỉ vế *"chuyển sang Vitest cùng đợt nâng lên 21"* của quyết định 5 bị hoãn. Vế *Karma + Jasmine, ChromeHeadless* của quyết định 5 còn áp dụng, cùng mọi quyết định khác của ADR-0028 — ADR này không chạm vào chúng. Trong lúc hoãn, Karma + Jasmine là bộ chạy hợp lệ trên Angular 21.
2. **PR Vitest mở bằng một ADR mới, và ADR đó lật phần hoãn.** Dòng trạng thái của ADR-0028 nhận thêm đoạn `· Hết hoãn bởi ADR-NNNN` ([`README.md`](README.md) §5.1). Chuyển sang Vitest là thêm phụ thuộc ngoài, một dấu hiệu bắt buộc viết ADR theo [`README.md`](README.md) §2. ADR đó chốt phần ADR này cố ý không chốt: chuyển một bước hay qua builder `unit-test` với `runner: "karma"`, nhà cung cấp browser mode, chỗ đặt sàn F15 sau khi bỏ reporter của Karma, và hai bước CI `3`, `3b`.
3. **Mốc muộn nhất là trước khi nâng Angular 22.** Lượt nâng lên 22 không bắt đầu khi phần hoãn còn treo. Lý do là nguyên tắc *không đổi framework và bộ chạy test trong cùng một đợt*, mà phương án A của ADR-0028 dùng để loại việc nhảy thẳng lên 21.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Chuyển ngay trong PR hiện tại, đúng nguyên văn ADR-0028 quyết định 5

Đây là **phương án đơn giản nhất về thủ tục**: không ADR mới, không phần hoãn nào phải theo dõi.

**Được:**

- ADR-0028 được thi hành nguyên văn.
- Không có quãng nào app chạy test trên một bộ chạy đã deprecated.
- Một lượt review cho toàn bộ đợt nâng.

**Mất:** một spec đỏ trong PR này có bốn nguồn khả dĩ: framework mới, CSS chuyển động của ADR-0123, việc gỡ provider của ADR-0124, hay chính bộ chạy mới. Hai trong số đó trực tiếp dựa vào `ng test`:

- ADR-0124 quyết định 3 lấy `ng test` không có provider hoạt ảnh làm **phép thử động duy nhất** cho tiền đề của nó. Một spec đỏ vì `NG05105` nghĩa là tiền đề sai. Đổi bộ chạy cùng lượt thì một lần đỏ không còn quy được về tiền đề hay về bộ chạy.
- ADR-0123 quyết định 5 viết spec đọc giá trị đã tính của CSS, và dựa vào việc Karma nạp `styles.scss` (`src/FE/angular.json`, mục `test`). Đổi bộ chạy cùng lượt thì cả cách nạp style lẫn môi trường DOM đổi đúng lúc spec đó ra đời.

**Vì sao loại:** PR đã lớn. Tách bộ chạy ra thì mỗi lần hỏng còn quy được về một nguyên nhân.

### Phương án B — Hoãn không hạn, chuyển khi Karma thật sự hỏng

**Được:** không phải lên lịch. Công chuyển chỉ trả khi buộc phải trả.

**Mất:** Karma không nhận sửa lỗi nữa. Lúc nó hỏng — với một bản Chrome mới, hay khi builder Karma bị gỡ khỏi một bản Angular — thì việc chuyển thành việc gấp, đúng kiểu bị buộc gộp với một đợt nâng. Mỗi spec viết theo API Jasmine trong lúc chờ là thêm một spec phải chuyển. Nợ chờ một sự kiện ở bên thứ ba là nợ không có ngày, cùng lập luận ADR-0123 phương án C.

**Vì sao loại:** mốc *trước Angular 22* giữ được nguyên tắc không gộp đổi framework với đổi bộ chạy, mà vẫn tách PR.

## Hệ quả

### Tích cực

- PR nâng 21 giữ được `ng test` trên bộ chạy cũ làm thước đo cố định. Spec đỏ trong PR đó quy về thay đổi của framework, của ADR-0123 hay của ADR-0124, không về bộ chạy.
- Phép thử động của ADR-0124 quyết định 3 giữ nguyên nghĩa.
- PR Vitest đứng riêng, nên diff của nó chỉ là bộ chạy, cấu hình và spec. Hỏng thì hoàn riêng được.

### Tiêu cực

| Cái giá | Nghĩa là |
| --- | --- |
| **Test chạy trên một bộ chạy đã deprecated trong một quãng không có ngày cố định** | Mốc là *"ngay sau khi PR hiện tại hợp nhất"*, không phải một ngày. Karma hỏng giữa quãng đó thì `ng test` đỏ vì lý do không thuộc mã |
| **Spec viết trong quãng hoãn tăng việc chuyển** | Mọi spec mới dùng `spyOn`, `jasmine.*` thêm vào danh sách phải chuyển. Spec đo giá trị đã tính của ADR-0123 quyết định 5 chuyển muộn hơn, và đòi browser mode |
| **Mốc *"trước Angular 22"* không có cổng** | Không máy nào chặn một lượt nâng 22 khi phần hoãn còn treo. Thứ canh duy nhất là dòng trạng thái của ADR-0028 và hàng Vitest ở [`../wiki-core/fe/06-testing-strategy.md`](../wiki-core/fe/06-testing-strategy.md) §10, với điều kiện người lập kế hoạch nâng đọc tới chúng |
| **ADR-0028 lệch với mã trong quãng hoãn** | Một người chỉ đọc quyết định 5 sẽ tin Vitest đã về cùng 21. Dòng trạng thái là thứ duy nhất báo phần hoãn |
| **Một lượt ADR và review nữa** | PR Vitest cần ADR riêng (quyết định 2) và lượt soát riêng, thay vì đi chung đợt nâng |

### Rút lui nếu sai

**Dấu hiệu sai:**

- Karma hay builder Karma hỏng trước khi PR Vitest mở.
- PR hiện tại hợp nhất mà PR Vitest không mở, và phần hoãn trôi.
- Kế hoạch nâng 22 được lập khi phần hoãn còn treo.

**Quy trình:**

1. **Karma hỏng trước PR Vitest:** mở PR Vitest ngay, kèm ADR lật phần hoãn theo quyết định 2. Không vá Karma.
2. **Quyết định không chuyển sang Vitest nữa:** viết ADR mới, sửa một phần ADR-0028 quyết định 5, và nêu bộ chạy thay thế. ADR này mang thêm trạng thái tương ứng.
3. **Muốn chuyển ngay, trước khi PR hiện tại hợp nhất:** ADR lật phần hoãn vẫn bắt buộc. Thứ tự giữa hai PR do người dùng chốt.

Không dữ liệu nào sinh ra, không migration. Chi phí rút lui chính là chi phí chuyển, vốn đã phải trả.

## Sáu câu hỏi trước khi chấp nhận

| # | Câu hỏi | Trả lời |
| --- | --- | --- |
| 1 | Vấn đề thật, đã đo chưa | PR gộp bốn việc, hai trong số đó dựa vào `ng test`, là quan sát được. Cỡ PR đếm bằng `git status --short \| wc -l`. **Chưa đo:** chuyển sang Vitest có thật sự làm spec đỏ hay không. Lý do hoãn là giữ khả năng quy nguyên nhân, không phải một lần hỏng đã xảy ra |
| 2 | Phương án đơn giản hơn, vì sao không đủ | Phương án A, làm nguyên văn ADR-0028. Nó làm phép thử động của ADR-0124 mất nghĩa |
| 3 | Chi phí vận hành thêm | Theo dõi một phần hoãn không có cổng; một ADR và một lượt review riêng cho PR Vitest; số spec phải chuyển tăng theo thời gian hoãn |
| 4 | Ai bảo trì | PR Vitest: `frontend-expert` thi công; `test-engineer` chuyển sàn F15 và hai bước CI; `architect` viết ADR lật phần hoãn. Mốc *"trước 22"*: người lập kế hoạch nâng bản lớn kế tiếp |
| 5 | Rút lui thế nào | Ba trường hợp ở mục *Rút lui nếu sai* |
| 6 | Có buộc Core biết nghiệp vụ không | Không. Đây là toolchain |

## ADR cũ — phần nào còn hiệu lực

| ADR | Còn hiệu lực | Tạm hoãn |
| --- | --- | --- |
| [0028](0028-toolchain-fe-va-ke-hoach-nang-cap.md) | Quyết định 1–4, 6–8. Quyết định 5: vế *Karma + Jasmine, ChromeHeadless* | Quyết định 5: vế *chuyển sang Vitest cùng đợt nâng lên 21* — tới khi ADR của PR Vitest lật phần hoãn |

## Việc thi công

**`architect` — `docs/`, đã làm cùng lượt viết ADR này:**

- Dòng trạng thái của ADR-0028.
- Hàng *Vitest* ở §10 và đoạn *Vitest* ở §7 của [`../wiki-core/fe/06-testing-strategy.md`](../wiki-core/fe/06-testing-strategy.md).

Không việc mã nào trong PR hiện tại. PR Vitest có ADR riêng và việc thi công riêng.

## Liên quan

- [`0028-toolchain-fe-va-ke-hoach-nang-cap.md`](0028-toolchain-fe-va-ke-hoach-nang-cap.md) — ADR được tạm hoãn một phần.
- [`0123-chuyen-dong-component-primeng-di-bang-token-qua-css-go-hang-ms-f37-het-ap-dung.md`](0123-chuyen-dong-component-primeng-di-bang-token-qua-css-go-hang-ms-f37-het-ap-dung.md) quyết định 5 · [`0124-go-nua-provider-hoat-anh-giam-chuyen-dong-dua-vao-css-toan-cuc-va-safe-f39-het-ap-dung.md`](0124-go-nua-provider-hoat-anh-giam-chuyen-dong-dua-vao-css-toan-cuc-va-safe-f39-het-ap-dung.md) quyết định 3 — hai phép đo dựa vào `ng test`.
- [`../wiki-core/fe/06-testing-strategy.md`](../wiki-core/fe/06-testing-strategy.md) §7, §10 — bộ chạy test.
- [`../wiki-core/fe/16-nen-tang-va-nang-cap.md`](../wiki-core/fe/16-nen-tang-va-nang-cap.md) §4, §11 — kiểm sau mỗi lần nâng, kế hoạch nâng kế tiếp.
