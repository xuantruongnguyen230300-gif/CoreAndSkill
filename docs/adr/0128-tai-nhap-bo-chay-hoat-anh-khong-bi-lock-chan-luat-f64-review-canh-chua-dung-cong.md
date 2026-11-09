---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0128 — Gỡ `@angular/animations` khỏi `package.json` không chặn được việc đưa bộ chạy hoạt ảnh trở lại, vì gói còn trong lock dưới dạng phụ thuộc ngang hàng tuỳ chọn: chính sách thành luật F64 do review canh, chưa dựng cổng

> **Trạng thái:** Đã chấp nhận (2026-11-07)

## Bối cảnh

Quyết định 4 của [ADR-0124](0124-go-nua-provider-hoat-anh-giam-chuyen-dong-dua-vao-css-toan-cuc-va-safe-f39-het-ap-dung.md) gỡ `@angular/animations` khỏi `dependencies` của `src/FE/package.json`, rồi giao cho bước đó một vai: *"Từ đó, đưa bộ chạy của Angular trở lại là thêm phụ thuộc ngoài — một dấu hiệu bắt buộc viết ADR. Thủ tục đó thay cho cổng F39, không tốn section nào."* Ba chỗ khác của ADR đó dựa trên cùng tiền đề này:

- Phương án D — luật có cổng cấm `@angular/animations` — bị loại, một phần vì *"Quyết định 4 đạt cùng mục đích bằng một thủ tục đã có"*.
- Quyết định 5 không dựng luật nào thay F39.
- Hàng *Tái nhập bộ chạy của Angular không có cổng* ở *Hệ quả tiêu cực* tả đường lọt là *"một lượt cài lại gói rồi thêm `provideAnimationsAsync()` trần"*.

Tiền đề chung là thế này: gỡ khỏi `package.json` thì gói rời cây cài đặt. Muốn import lại thì phải cài lại, và lần cài đó là lúc thủ tục bắt được.

Ngày 2026-11-07, lượt thi công của `frontend-expert` chạy bước 4: `src/FE/package.json` không còn khai gói. Nhưng gói vẫn nằm trong `node_modules`, và `npm ls` cho đường `@angular/platform-browser@21.2.26 → @angular/animations@21.2.26`. `architect` đối chiếu cùng ngày:

| Đo gì | Kết quả |
| --- | --- |
| Lock của app | `src/FE/package-lock.json` còn mục `"node_modules/@angular/animations"`, mang `"optional": true` và `"peer": true` |
| Ai kéo gói | Chỉ `@angular/platform-browser`. Gói này khai `@angular/animations` ở `peerDependencies` và đánh dấu `optional` ở `peerDependenciesMeta` — cùng tệp lock, mục `"node_modules/@angular/platform-browser"`. Không mục nào khác trong lock nhắc tới gói |
| Vì sao còn. Tái hiện trong thư mục tạm của phiên, ngoài cây repo — npm 10.8.2, chế độ chỉ ghi lock (`--package-lock-only`) | (1) Dựng lock mới từ một `package.json` không khai gói: **không** có mục. (2) Khai gói, rồi `npm uninstall @angular/animations`: mục **ở lại**, mang đúng hai cờ như lock của app. (3) Chạy tiếp `npm prune`: mục vẫn ở lại. (4) Gỡ đích danh mục khỏi lock rồi `npm install`: npm **không** thêm lại, chạy lần thứ hai cũng vậy |
| Hệ quả cho mã | Không gì buộc người tái nhập phải đụng tới `package.json`. Với cây cài đặt hôm nay, một `import` từ `@angular/animations` hay `@angular/platform-browser/animations` vẫn biên dịch và chạy được |
| Ai thấy một lần tái nhập | `src/FE/src/app/core/`, `shared/`, `platform/` thuộc khối `core-paths`, nên sửa ở đó kéo một lượt `core-reviewer`. `src/FE/src/app/app.config.ts` — chỗ người ta sẽ khai provider hoạt ảnh — cùng `src/FE/package.json` và `src/FE/package-lock.json` thì không thuộc khối đó ([`../kien-truc-core-module.md`](../kien-truc-core-module.md), khối `core-paths`) |

Bảng *Bối cảnh* của ADR-0124 đo `peerDependencies` của `primeng` mà không đo `peerDependencies` của `@angular/platform-browser`. Phép đo đó **đã sót ngay từ lúc viết**. Luật D45 ở [`../RULES-DOCS.md`](../RULES-DOCS.md) quy định phép đo đúng phải sống ở ADR mới, nên ADR-0124 giữ nguyên nội dung.

**Chưa đo** hai điều. Một: hành vi ở chế độ cài thật, khi có `node_modules`. Hai: một import thử có làm `ng build` đỏ khi gói vắng hẳn hay không. Đọc tĩnh gói đã cài thì cả hai lối vào hoạt ảnh của `@angular/platform-browser` đều nạp `@angular/animations/browser`: một lối bằng import tĩnh, lối `async` bằng import động với chuỗi hằng. Vì vậy khi gói vắng, bộ đóng gói nhiều khả năng không phân giải được. Đó là suy luận, chưa phải phép đo.

## Quyết định

`architect` chốt, theo quyết định của người dùng ở [ADR-0114](0114-bo-so-no-luat-ve-rules-viec-ton-sang-issues-review-khong-sinh-viec-stop-chan-review-mot-lan-go-nhanh-chua-co-src.md): chỉ dựng cổng khi có vi phạm thật lặp lại.

1. **Câu "thủ tục thay cho cổng F39" ở quyết định 4 của ADR-0124 không còn là một rào.** Hôm nay không gì buộc một lần tái nhập phải đi qua `package.json`. Phần còn lại của quyết định 4 — gỡ gói khỏi `dependencies` — vẫn giữ, và đã thi công.
2. **Chính sách giữ nguyên, dù gói có sẵn trong cây cài đặt, và thành luật F64 mang `🕳️`.** Mã dưới `src/FE/src` và `src/FE/e2e`, kể cả spec, không import `@angular/animations` hay `@angular/platform-browser/animations`. Đưa bộ chạy của Angular trở lại vẫn là thêm phụ thuộc ngoài: khai gói ở `package.json` và viết ADR. Ô *Ép bằng gì* của F64 ghi đúng cỡ chỗ hở. Điều *"không dựng luật"* ở quyết định 5 của ADR-0124 hết hiệu lực. Điều *"không section, không canary"* vẫn còn.
3. **Lượt này không dựng cổng và không đổi `src/`.** Chưa có lần tái nhập nào. Phương án C — gỡ mục dư khỏi lock — được hoãn, không bị loại.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Không viết gì, hoặc chỉ ghi một dòng nhật ký

Đây là **phương án đơn giản nhất**.

**Được:** không thêm tệp nào.

**Mất:** người đọc ADR-0124 tiếp tục tin vào một rào không có thật, và phương án D của ADR đó trông như đã bị loại đủ lý do.

**Vì sao loại:** luật D45. Mục *Hệ quả* lỗi thời và phép đo sót ở §Bối cảnh phải sửa bằng ADR mới. Nhật ký chỉ giữ quyết định nhỏ, không giữ một phép đo sửa lại ADR.

### Phương án A′ — Chỉ ghi chỗ hở bằng văn xuôi ở `fe-architecture.md` §2.6, không thêm dòng luật

**Được:** bảng luật không thêm mã nào. Đúng chữ quyết định 5 của ADR-0124.

**Mất:** có một luật mà câu *"ép bằng gì"* không có câu trả lời ở chỗ người ta tra. Luật D45 nói câu hỏi đó tra ở các bảng `RULES-*.md`, không tra ở `adr/`.

**Vì sao loại:** [`README.md`](README.md) §8 đòi mọi luật do một ADR sinh ra phải có dòng trong bảng luật. ADR-0114 quyết định 9 đòi luật chưa có cổng mang `🕳️` để chỗ hở nhìn thấy được. Quyết định 5 của ADR-0124 bỏ dòng luật chỉ vì đã giao việc đó cho thủ tục ở quyết định 4. Thủ tục không chặn được thì lý do đó cũng mất.

### Phương án B — Chặn bằng lint: `no-restricted-imports` cho hai tiền tố, đặt trong `src/FE/eslint.config.js`

**Được:** máy chặn ở `ng lint`, ở mọi chỗ — kể cả `app.config.ts` và `modules/`, những nơi `core-reviewer` không thấy. Viết thì rẻ: một mục cấu hình.

**Mất:** thêm một cổng trong khối `core-paths`, kèm cái giá repo này trả cho mỗi cổng: một mục trong danh sách cấm tắt của F3 và một canary. Tất cả chỉ để chặn một vi phạm chưa từng xảy ra.

**Vì sao loại:** ADR-0114. ADR-0124 loại phương án D vì hai lý do. Lý do thứ hai — quyết định 4 đạt cùng mục đích — nay đã sai. Lý do thứ nhất — ADR-0114 — vẫn nguyên, và một mình nó là đủ.

### Phương án C — Gỡ mục dư khỏi lock

**Được:** có lại đúng cái rào mà quyết định 4 của ADR-0124 tưởng đã có. Phép đo (4) cho thấy npm không tự thêm lại mục. Không cổng, không canary, về sau không có gì phải bảo trì.

**Mất:** một lượt `frontend-expert` chạm `src/FE/package-lock.json` và sửa đích danh một mục, vì cả `npm uninstall` lẫn `npm prune` đều không gỡ được nó — phép đo (2), (3). Sau đó phải cài lại và chạy lại `ng build`, `ng test`, `ng lint`. Hai điểm chưa đo: chế độ cài thật, và import thử có làm build đỏ không. Rào này cũng mong manh: nếu sau này một gói phụ thuộc cứng vào `@angular/animations`, gói đó sẽ kéo nó về mà `package.json` của app không đổi một dòng.

**Vì sao hoãn, không loại:** đây là cách rẻ nhất để có lại rào. Nhưng nó là một việc `src/` mới, sinh ra sau lượt thi công cuối của đợt. Mở thêm lượt hay không, và mở lúc nào, là ưu tiên của người dùng. Kích hoạt: *Điều kiện xét lại* 1, hoặc khi người dùng chọn làm.

## Hệ quả

### Tích cực

- Chỗ hở được ghi đúng cỡ, ở chỗ người viết code tra: dòng F64 và câu luật ở [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §2.6.
- Đợt này không thêm cổng nào, không thêm việc `src/` nào.
- ADR-0124 vẫn đứng. Tiền đề sai chỉ chạm tới cái giá của nó, không chạm tới lý do gỡ nửa provider.

### Tiêu cực

| Cái giá | Nghĩa là |
| --- | --- |
| **Không gì chặn một lần tái nhập** | Không có lỗi biên dịch, không có cổng, cũng không cần cài lại gói. Thêm một dòng `provideAnimationsAsync()` vào `app.config.ts` không kéo lượt `core-reviewer` nào, vì tệp đó nằm ngoài `core-paths`. Chỉ người duyệt PR thấy được |
| **F64 sống nhờ trí nhớ** | Lệnh rà ở ô *Ép bằng gì* của F64 là lệnh tự rà, không phải cổng — chỉ chạy khi có người nhớ chạy |
| **Rào mà ADR-0124 hứa nay chỉ còn trên giấy** | Ai đã đọc ADR-0124 mà chưa đọc ADR này sẽ tin điều ngược lại. Đường báo duy nhất là dòng trạng thái của ADR-0124 trỏ sang đây |
| **Bảng luật thêm một mã** | F64 là luật thứ hai về cùng một bộ chạy, sau F39 đã hết áp dụng. Người đọc phải phân biệt: F39 nói provider khai ở đâu, F64 nói không được đưa bộ chạy trở lại |
| **Phép đo của ADR này chỉ chạy ở chế độ chỉ ghi lock** | Kết luận *"npm không tự thêm lại"* chưa được kiểm với `node_modules` thật |

### Rút lui nếu sai

**Dấu hiệu sai:** một lần tái nhập lọt tới nhánh chính. Hoặc một lượt nâng cho thấy gói đã rời cây cài đặt mà không ai gỡ — khi đó câu *"gói có sẵn"* của ADR này đã lỗi thời.

**Quy trình:**

1. **Lần tái nhập đầu tiên lọt** (vi phạm thật): viết ADR mới, chọn giữa B và C. Chọn C nếu gói vẫn chỉ do `@angular/platform-browser` kéo. Chọn B nếu đã có gói khác phụ thuộc cứng vào nó. B giữ mã F64 và đổi trạng thái của dòng đó.
2. **Người dùng chọn C trước khi có vi phạm:** giao `frontend-expert` theo phương án C, kèm hai phép đo còn thiếu. Đạt khi `npm ls @angular/animations` chạy trong `src/FE` ra rỗng, và một import thử ở nhánh nháp làm `ng build` đỏ. Không đạt thì người dùng hoàn lock bằng git, ADR này giữ nguyên. Đạt thì ô *Ép bằng gì* của F64 ghi rào biên dịch kèm ngày, và nhật ký quyết định thêm một dòng — C là phương án hoãn của chính ADR này, không phải lật nó.
3. **Gói rời cây cài đặt vì một lượt nâng:** sửa ô *Ép bằng gì* của F64 cho khớp, kèm ngày.

Không sinh dữ liệu nào, không có migration. Chi phí rút lui: một lượt `frontend-expert` (C) hoặc một lượt `test-engineer` (B).

### Điều kiện xét lại

1. **Một lần tái nhập lọt**, dù phát hiện ở review hay sau đó: quy trình 1 ở trên.
2. **Mỗi lượt nâng `@angular/*`:** người làm lượt nâng chạy `npm ls @angular/animations` trong `src/FE` và ghi kết quả vào báo cáo của lượt — cùng chỗ với điều kiện xét lại 1 của ADR-0124.
3. **Angular 23 gỡ `provideAnimationsAsync`** (ADR-0124, bảng *Bối cảnh*, dòng *Hạn dùng*): xem lại `@angular/platform-browser` còn khai gói là phụ thuộc ngang hàng hay không.

## ADR cũ — phần nào còn hiệu lực

| ADR | Còn hiệu lực | Hết hiệu lực |
| --- | --- | --- |
| [0124](0124-go-nua-provider-hoat-anh-giam-chuyen-dong-dua-vao-css-toan-cuc-va-safe-f39-het-ap-dung.md) | Mọi phần trừ những chỗ ở cột bên phải. Riêng quyết định 4 vẫn còn phần gỡ gói khỏi `dependencies` | Quyết định 4: câu *"Từ đó, đưa bộ chạy … Thủ tục đó thay cho cổng F39, không tốn section nào"* — không còn là rào. Quyết định 5: điều *"không dựng luật"*; điều *"không section, không canary"* còn. Phương án D: lý do *"Quyết định 4 đạt cùng mục đích"*; lý do ADR-0114 còn. *Hệ quả tiêu cực*, hàng *Tái nhập bộ chạy của Angular không có cổng*: đường lọt không cần *"cài lại gói"* — bảng *Tiêu cực* ở trên thay cho hàng đó |

## Việc thi công

Không có việc `src/`. `architect` sửa `docs/` ngay trong lượt viết ADR này: dòng F64 ở [`../RULES-FE.md`](../RULES-FE.md); câu luật §2.6 của [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md); dòng trạng thái của ADR-0124; [`muc-luc.md`](muc-luc.md).

Phần của `design-expert` ở [`../Design/DESIGN.md`](../Design/DESIGN.md) §7 — hàng *provider hoạt ảnh của Angular* ở bảng đầu tệp, và hàng loại 2 ở bảng *Giảm chuyển động* — đang dựa vào quyết định 4 của ADR-0124 như một rào. `architect` chỉ báo, không sửa.

## Liên quan

- [`0124-go-nua-provider-hoat-anh-giam-chuyen-dong-dua-vao-css-toan-cuc-va-safe-f39-het-ap-dung.md`](0124-go-nua-provider-hoat-anh-giam-chuyen-dong-dua-vao-css-toan-cuc-va-safe-f39-het-ap-dung.md) — quyết định bị sửa một phần.
- [`0114-bo-so-no-luat-ve-rules-viec-ton-sang-issues-review-khong-sinh-viec-stop-chan-review-mot-lan-go-nhanh-chua-co-src.md`](0114-bo-so-no-luat-ve-rules-viec-ton-sang-issues-review-khong-sinh-viec-stop-chan-review-mot-lan-go-nhanh-chua-co-src.md) — vì sao chưa dựng cổng.
- [`../RULES-FE.md`](../RULES-FE.md) §7 — F39 (hết áp dụng), F63, F64.
- [`../RULES-DOCS.md`](../RULES-DOCS.md) — D45: vì sao phép đo đúng sống ở ADR mới.
