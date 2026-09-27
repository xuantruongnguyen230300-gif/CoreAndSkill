---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0056 — Tập sub-preset PrimeNG bắt buộc gồm cả component con mà component được import thật sự dựng bên trong, không chỉ component import trực tiếp

> **Trạng thái:** Đã chấp nhận (2026-09-22)

## Bối cảnh

[`0038-preset-primeng-tung-sub-theo-phan-dung.md`](0038-preset-primeng-tung-sub-theo-phan-dung.md) chốt rằng `prime-preset.ts` chỉ import sub-preset tương ứng những component PrimeNG mà `src/FE` **thực sự dùng trực tiếp**. Luật F29 ở [`../RULES.md`](../RULES.md) chép đúng phạm vi đó. [`0039-sua-mot-phan-0038-hai-sub-preset-khong-phai-bon.md`](0039-sua-mot-phan-0038-hai-sub-preset-khong-phai-bon.md) giữ nguyên quy tắc.

Ngày 2026-09-22, `frontend-expert` chứng minh bằng test rằng phạm vi "trực tiếp" để lọt một lỗi. Kiến trúc sư đối chiếu cùng ngày:

- `src/FE/src/app/core/theme/prime-preset.spec.ts` dựng thật từng component và khẳng định biến theme của component con có giá trị. Các ca trong test: `button (nút đóng Dialog)` với biến `--p-button-padding-x`, `select (ô số dòng Paginator)`, `chip (Autocomplete multiple)`, `inputtext (ô nhập Autocomplete đơn)`. Chú thích đầu tệp nêu cơ chế: biến `--p-<con>-*` chỉ sinh ra khi preset có `components.<con>`. Thiếu thì CSS tĩnh của component con tham chiếu một biến rỗng. Giao diện vẫn chạy, theme sai, và không có lỗi nào báo.
- Không tệp nào trong `src/FE/src` import `primeng/button`, `primeng/select`, `primeng/chip` hay `primeng/inputtext`. Theo chữ của F29, bốn sub-preset đó là thừa. Theo test, thiếu chúng là hỏng.
- Gói `primeng` (bản ghim trong `src/FE/package.json`), sau khi cài, khai phụ thuộc giữa các component bằng lệnh import. `primeng-dialog.mjs` import `primeng/button`. `primeng-autocomplete.mjs` import `primeng/chip` và `primeng/inputtext`. `primeng-table.mjs` import thêm `primeng/datepicker`, `primeng/inputnumber`, `primeng/badge`, `primeng/selectbutton` cho bộ lọc cột, là những thứ `src/FE` chưa dựng.

## Quyết định

Kiến trúc sư chốt:

1. Tập sub-preset **bắt buộc** gồm: `base`, sub-preset của mọi component PrimeNG được import trực tiếp, và sub-preset của mọi component con mà các component đó **thật sự dựng ra** trong cách `src/FE` dùng chúng hôm nay.
2. Tập sub-preset **được phép** không vượt quá: `base` cộng bao đóng phụ thuộc khai trong gói PrimeNG của các component import trực tiếp. Sub-preset nằm ngoài tập này là rác.
3. Cấm import object `Aura` gộp. Điều này giữ nguyên như 0038.
4. Cổng dự kiến có hai lớp:
   - **Script**, là một section F29 trong `scripts/fe-gate.sh`. Nó dựng tập *trực tiếp* từ các lệnh `import ... from 'primeng/<x>'` trong `src/FE/src` và dựng tập *được phép* bằng cách đọc các lệnh import `primeng/<y>` trong tệp `primeng-<x>.mjs` của gói `primeng` đã cài. Nó đỏ khi thiếu sub-preset của một component trực tiếp, khi có sub-preset nằm ngoài tập được phép, và khi có import `Aura` gộp. Hai bảng khai một chỗ, lệnh đọc bảng chứ không chép: bảng tên module PrimeNG khác tên sub-preset (ví dụ `table` ứng với `datatable`), và bảng module không phải component (`api`, `config`…).
   - **Test**, theo khuôn `prime-preset.spec.ts`. Mỗi component con thật sự được dựng có một ca khẳng định biến `--p-<con>-*` của nó có giá trị. Đây là lớp duy nhất canh vế *"thật sự dựng ra"*.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Giữ chữ của F29: chỉ tính component import trực tiếp

**Được:** luật đơn giản, script so được hai tập mà không cần biết gì về bên trong PrimeNG.

**Vì sao loại:** test đã chứng minh luật này cho ra một preset thiếu biến. Lỗi đó không báo ở đâu cả. Một luật có cổng mà cổng xanh trên một giao diện sai theme là luật tệ hơn không có luật.

### Phương án B — Tập bắt buộc bằng bao đóng phụ thuộc khai trong gói PrimeNG

**Được:** tính được hoàn toàn bằng máy, không cần test dựng, tự cập nhật khi nâng PrimeNG.

**Vì sao loại:** bao đóng đó lớn hơn thứ được dựng. Riêng `table` đã kéo theo `datepicker`, `inputnumber`, `badge`, `selectbutton`, là những component `src/FE` chưa dựng ở đâu. Bắt buộc chúng là quay lại đúng loại suy đoán *"chắc sẽ cần"* mà 0038 đã bác. Bao đóng này vẫn có ích làm **trần**, đó là quyết định 2.

### Phương án C — Import `Aura` gộp

**Được:** không bao giờ thiếu biến.

**Vì sao loại:** 0038 đã loại vì ngân sách bundle, và lý do đó không đổi.

## Hệ quả

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| **Vế "thật sự dựng ra" chỉ có test canh, và chỉ canh những ca đã viết** | Bật một tuỳ chọn mới của một component, ví dụ bộ lọc cột của bảng, có thể dựng thêm một component con. Không ai viết ca test cho nó thì preset thiếu biến và mọi cổng vẫn xanh. Lớp bắt được ca này là review |
| **Script phụ thuộc vào cấu trúc tệp của gói PrimeNG** | Đường `fesm2022/primeng-<x>.mjs` là chi tiết đóng gói, không phải hợp đồng. Nâng PrimeNG có thể đổi nó. Script phải đỏ khi không đọc được tệp nào, không được xanh rỗng |
| **Hai bảng khai tay** | Bảng tên khác nhau và bảng module không phải component sẽ cũ đi khi nâng PrimeNG. Cổng chỉ báo lệch khi lệch làm thiếu một sub-preset |
| **Bundle lớn hơn mức 0039 đo** | Thêm component con là thêm sub-preset. Chưa đo lại |

### Tích cực

- Luật F29 nói đúng thứ nó muốn canh: không thiếu biến theme và không mang rác.
- Trần của quyết định 2 máy tính được, nên sub-preset dư có cổng bắt.

### Điều gì là dấu hiệu quyết định này bắt đầu sai

- Một lỗi theme ở component con được phát hiện bằng mắt trong khi cổng xanh. Nghĩa là lớp test không theo kịp cách dùng, và việc cần làm là tìm một cách dò tự động vế *"thật sự dựng"*.
- Script F29 đỏ sau mỗi lần nâng PrimeNG vì cấu trúc gói đổi. Nghĩa là trần nên lấy từ nguồn khác.

### Rút lui nếu sai

Mọi thứ là tài liệu, script và test. Không đổi lược đồ, không sinh dữ liệu. Quay về phạm vi trực tiếp là sửa lại luật F29 và gỡ các ca test component con, nhưng làm vậy là chấp nhận lại lỗi theme im lặng đã chứng minh. Chuyển sang phương án B là đổi tập bắt buộc thành trần hiện tại và chấp nhận bundle lớn hơn.

## Liên quan

- [`0038-preset-primeng-tung-sub-theo-phan-dung.md`](0038-preset-primeng-tung-sub-theo-phan-dung.md): quyết định bị **sửa một phần**. Chữ *"thực sự dùng trực tiếp"* ở quyết định 1 và phạm vi chỉ-trực-tiếp của F29 ở quyết định 2 hết hiệu lực. Phần còn lại giữ nguyên: ghép từng sub-preset, cấm `Aura` gộp, không nâng ngân sách bundle
- [`0039-sua-mot-phan-0038-hai-sub-preset-khong-phai-bon.md`](0039-sua-mot-phan-0038-hai-sub-preset-khong-phai-bon.md): con số "hai" ở đó là con số của phạm vi trực tiếp tại ngày đó
- [`../RULES.md`](../RULES.md) luật F29, §7 và §10
