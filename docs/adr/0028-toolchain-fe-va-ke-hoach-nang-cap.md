---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0028 — FE dựng trên Angular 20.3 / PrimeNG 20 / ngx-translate 18, nâng lên 21 trước khi 20 hết hỗ trợ; toolchain chốt ở F0 theo bảng tương thích chính thức

> **Trạng thái:** Đã chấp nhận (2026-09-14)

## Bối cảnh

Lúc quyết định, phía FE có năm câu hỏi về nền tảng mà tài liệu hoặc trả lời bằng một con số không có chủ, hoặc không trả lời:

| # | Câu hỏi | Tài liệu nói gì (lúc ghi ADR) |
| --- | --- | --- |
| 1 | Dựng trên phiên bản nào | Chuỗi *Angular 20.3 / PrimeNG 20 / ngx-translate 18* chép ở nhiều file sống: [`../quy-uoc/README.md`](../quy-uoc/README.md) §3 (dòng 76), [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) (dòng 7, 15, 17), [`../Design/Icons.md`](../Design/Icons.md) (dòng 17). Trong khi [`../wiki-core/fe/16-nen-tang-va-nang-cap.md`](../wiki-core/fe/16-nen-tang-va-nang-cap.md) §8 và hướng khoá `K47` **cấm** chép số phiên bản vào tài liệu. Bộ số đó là stack đã chạy thật ở dự án tiền nhiệm — không có quyết định nào của repo này chọn nó |
| 2 | Bao giờ phải nâng | Cùng file §1 (dòng 19): bản lớn mỗi sáu tháng, mỗi bản được hỗ trợ khoảng mười tám tháng. Suy ra bản 20 hết hỗ trợ khoảng **11/2026** — trùng khoảng thời gian dự kiến dựng F0–F3. §2.2 gọi mốc hết hỗ trợ là mốc cứng |
| 3 | Node, TypeScript, angular-eslint | Không file nào có. Job FE (đang tắt) trong [`../../.github/workflows/docs-gate.yml`](../../.github/workflows/docs-gate.yml) khai một phiên bản Node không kèm nguồn |
| 4 | Bộ chạy test | [`../wiki-core/fe/06-testing-strategy.md`](../wiki-core/fe/06-testing-strategy.md) §7 (dòng 161–165) chọn *bộ chạy mặc định của Angular CLI* và hoãn lựa chọn thử nghiệm; [`../wiki-core/fe/trien-khai/05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) §5 (dòng 135) gọi ChromeHeadless. Không nơi nào gọi tên bộ chạy |
| 5 | Path alias, HTTPS máy dev | Không quy ước nào khai alias; ranh giới FE dựa vào rule phân giải specifier ra đường dẫn thật, và bẫy resolver của nó hỏng im lặng ([`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §4.2, dòng 400–413). [`0015-fe-va-api-khac-nguon.md`](0015-fe-va-api-khac-nguon.md) đòi HTTPS ở mọi môi trường kể cả máy dev, nhưng chưa file nào nói FE dev server lấy chứng chỉ từ đâu |

Ràng buộc có thật: chưa có `src/FE`, nên chưa có `package.json` nào để đối chiếu; và repo không chứa nguồn nào cho bảng tương thích giữa Angular với Node, TypeScript, angular-eslint.

## Quyết định

| # | Hạng mục | Chốt |
| --- | --- | --- |
| 1 | Framework và thư viện | **Angular 20.3, PrimeNG 20, ngx-translate 18** cho F0–F3 |
| 2 | Nâng bản lớn | Lên **Angular 21 trước khi bản 20 hết hỗ trợ**. Ngày chính xác đối chiếu lịch hỗ trợ chính thức ở angular.dev khi lập kế hoạch nâng. Thứ tự và ràng buộc theo [`../wiki-core/fe/16-nen-tang-va-nang-cap.md`](../wiki-core/fe/16-nen-tang-va-nang-cap.md) §2.3 và §3 |
| 3 | Node, TypeScript, angular-eslint | **Chốt ở F0 theo bảng tương thích chính thức của Angular cho bản 20.3** (angular.dev). ADR này **không** ghi số. Nghiệm thu F0 có mục kiểm việc đó |
| 4 | Nguồn phiên bản sau F0 | **Duy nhất** `package.json` (ghim chính xác, khai `engines`) và `.nvmrc`. Tài liệu sống đọc số bằng lệnh (`K47`) |
| 5 | Bộ chạy test | **Karma + Jasmine**, trình duyệt **ChromeHeadless**. Chuyển sang **Vitest cùng đợt nâng lên 21** |
| 6 | Lint import | **`eslint-plugin-import`** |
| 7 | Path alias | **Không dùng ở v1** — import nội bộ bằng đường dẫn tương đối |
| 8 | HTTPS máy dev | Chứng chỉ phát triển của .NET: `dotnet dev-certs https --trust`, xuất PEM bằng `--export-path … --format Pem`; `angular.json` khai `ssl`, `sslCert`, `sslKey`; tệp chứng chỉ **không** commit. Thao tác ghi ở [`../wiki-core/fe/trien-khai/01-f0-nen-mong.md`](../wiki-core/fe/trien-khai/01-f0-nen-mong.md) §1 |

Sau ADR này, tài liệu sống **không** chép lại bảng trên — chỗ cần nói phiên bản trỏ về đây.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Dựng thẳng trên Angular 21 ngay từ F0

**Được:** chưa có code nên đây là lúc nhảy rẻ nhất; không phải nâng một bản lớn chỉ vài tháng sau khi dựng; có Vitest từ đầu. Đây là phương án một người tỉnh táo sẽ chọn.

**Vì sao loại:** nó đổi **ba** thứ cùng lúc — framework, thư viện UI, bộ chạy test — trên một bộ spec giao diện và quy ước FE viết theo PrimeNG 20. Đó là đúng điều §2.3 cấm (*không gộp một lần*). Tương thích của PrimeNG và ngx-translate với bản 21 **chưa được đối chiếu ở đâu trong repo**; nhảy trước khi đối chiếu là đi đúng hướng `K46` khoá. Và tổ hợp 20.3 / PrimeNG 20 / ngx-translate 18 là tổ hợp **duy nhất** có bằng chứng đã chạy thật. Loại **có điều kiện** — xem điều kiện lật 3.

### Phương án B — Ghi luôn số Node, TypeScript, angular-eslint vào ADR này

**Được:** F0 không phải tra gì.

**Vì sao loại:** repo không có nguồn cho các số đó, nên ghi vào là bịa. Một con số bịa trong một ADR còn tệ hơn trong một file thường: ADR không sửa nội dung được, nên số sai nằm đó mãi, mang dáng vẻ một quyết định.

### Phương án C — Vitest ngay trên Angular 20

**Vì sao loại:** [`../wiki-core/fe/06-testing-strategy.md`](../wiki-core/fe/06-testing-strategy.md) §7 đã hoãn nó ở bản 20 vì còn ở dạng thử nghiệm. Đổi bộ chạy cùng đợt nâng 21 là đổi một lần, khi nó không còn là thử nghiệm.

### Phương án D — Path alias cho các tầng

**Được:** import ngắn; dời thư mục không đổi specifier.

**Vì sao loại ở v1:** rule ranh giới phải phân giải alias ra đường dẫn thật mới so được vùng cấm, tức thêm một tầng cấu hình resolver — và bẫy resolver ở [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §4.2 là loại hỏng **trông y hệt đang chạy sạch**. Canary của F0 phải chứng minh thêm một cấu hình nữa, cho một lợi ích chưa đo được. Xét lại ở điều kiện lật 4.

### Phương án E — Proxy máy dev thay cho chứng chỉ

**Vì sao loại:** [`0015-fe-va-api-khac-nguon.md`](0015-fe-va-api-khac-nguon.md) đã loại — nó giấu đúng nhóm lỗi cookie chéo nguồn cần thấy sớm.

### Phương án F — Giữ chuỗi phiên bản trong tài liệu sống

**Vì sao loại:** `K47`. Bộ số đã có ở ít nhất ba file sống cùng lúc — đúng hình dạng hai nguồn sẽ lệch.

## Hệ quả

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| **Nâng lên 21 là việc chắc chắn, và đến sớm** | Nó rơi vào ngay sau — hoặc giữa — F0–F3, trên một codebase vừa dựng, và **gồm cả đổi bộ chạy test**. Nếu F0–F3 kéo qua mốc hết hỗ trợ, đợt nâng chen vào giữa một pha |
| **Công viết cho Karma + Jasmine là công viết một lần rồi bỏ** | Cấu hình test, và có thể một phần spec, sẽ viết lại ở đợt nâng |
| **F0 gánh thêm một bước tra bảng tương thích** | Tra sai, hoặc mỗi người tra một lần ra một kết quả, thì máy dev và CI lệch nhau. `engines` và `.nvmrc` chỉ có tác dụng khi công cụ đọc chúng |
| **Không alias** | Đường dẫn tương đối dài ở tầng sâu; dời một thư mục là sửa mọi import trỏ vào nó |
| **Chứng chỉ dev** | Mỗi máy xuất PEM một lần; chứng chỉ hết hạn phải xuất lại; hành vi tin cậy chứng chỉ khác nhau giữa hệ điều hành; tệp khoá riêng nằm trên đĩa máy dev ngoài repo, và phải có luật bỏ qua để không lọt vào commit. Cái giá này lặp lại với mỗi người mới, cộng thêm vào cái giá ADR-0015 đã ghi |

### Tích cực

- Tài liệu sống hết chép số phiên bản; **một** nguồn phiên bản, đọc được bằng lệnh.
- Không con số nào trong repo thiếu nguồn.
- Tổ hợp khởi đầu là tổ hợp có bằng chứng chạy thật.
- HTTPS máy dev dùng đúng công cụ phía BE đã có, nên FE và API trên máy dev dùng chung một nguồn chứng chỉ.

### Điều kiện lật quyết định

1. **Bảng tương thích tra ở F0 cho thấy tổ hợp 20.3 không chạy được với phiên bản Node còn được hỗ trợ**, hoặc không còn nhận bản vá.
2. **Gần tới mốc hết hỗ trợ mà PrimeNG hoặc ngx-translate chưa có bản cho 21.** ADR mới chọn giữa ghi đè gói bắc cầu ([`../wiki-core/fe/16-nen-tang-va-nang-cap.md`](../wiki-core/fe/16-nen-tang-va-nang-cap.md) §3) và chạy quá mốc có thời hạn.
3. **Lúc F0 bắt đầu, thời gian hỗ trợ còn lại của bản 20 ngắn hơn thời gian dự kiến của F0–F3**, và cả PrimeNG lẫn ngx-translate đã có bản cho 21. Lúc đó dựng thẳng trên 21 (phương án A) rẻ hơn dựng rồi nâng ngay.
4. **Đường dẫn tương đối gây lỗi ranh giới đo được**, hoặc dời thư mục thành việc thường xuyên → xét lại alias.

## Liên quan

- [`../wiki-core/fe/16-nen-tang-va-nang-cap.md`](../wiki-core/fe/16-nen-tang-va-nang-cap.md) — chính sách nâng cấp, `K46`, `K47`
- [`../wiki-core/fe/trien-khai/01-f0-nen-mong.md`](../wiki-core/fe/trien-khai/01-f0-nen-mong.md) — nơi chốt toolchain và nghiệm thu
- [`../wiki-core/fe/06-testing-strategy.md`](../wiki-core/fe/06-testing-strategy.md) §7 — bộ chạy test
- [`0015-fe-va-api-khac-nguon.md`](0015-fe-va-api-khac-nguon.md) — vì sao HTTPS ở máy dev
- [`0007-fe-giu-cau-truc-thu-muc.md`](0007-fe-giu-cau-truc-thu-muc.md) — ranh giới FE ép bằng ESLint
