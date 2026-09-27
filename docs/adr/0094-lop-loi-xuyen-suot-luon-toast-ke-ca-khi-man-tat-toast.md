---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0094 — Lỗi 5xx và 403 `CORE.AUTH.*` luôn toast kèm `traceId`, kể cả khi request tắt toast; cờ tắt toast chỉ áp cho lỗi của màn

> **Trạng thái:** Đã chấp nhận (2026-09-25)

## Bối cảnh

Hai tệp luật FE mô tả khác nhau cùng một ca.

- [`../quy-uoc/fe-routing-guard.md`](../quy-uoc/fe-routing-guard.md) §8 khai: 403 `CORE.AUTH.FORBIDDEN` thì `errorInterceptor` toast kèm `traceId`; 403 `CORE.AUTH.ORIGIN_REJECTED` thì toast chung kèm `traceId`. Không có điều kiện nào.
- [`../quy-uoc/fe-api-client.md`](../quy-uoc/fe-api-client.md) §2.2 cho `FORBIDDEN` xuống nhánh toast cuối. Nhánh đó tắt khi request mang cờ `BO_QUA_TOAST_LOI`. Mọi mã khác rơi xuống nhánh cuối, gồm cả 500, cũng theo cờ.

Code theo vế thứ hai. Kiến trúc sư đọc `src/FE/src/app/core/interceptors/error.interceptor.ts` ngày 2026-09-25: nhánh cuối chỉ toast khi request không mang cờ, và điều kiện đó áp cho mọi mã rơi xuống.

Cờ không hiếm. [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) §6.2 buộc mọi lời gọi ghi của form dùng `applyFormFailure` mang cờ, để lỗi không hiện hai lần. Số tệp đang mang cờ đếm bằng lệnh, không chép ở đây:

```bash
grep -rln BO_QUA_TOAST_LOI src/FE/src/app --include='*.ts' | grep -v '\.spec\.ts'
```

Kiến trúc sư đọc code và thấy hệ quả sau. Lỗi này là suy từ code, chưa ai quan sát ở môi trường chạy thật. Một form mang cờ nhận 500 thì màn hiện câu của BE trong khu lỗi của nó. Câu đó bảo người dùng *"báo mã theo dõi cho quản trị"* (`CommonErrors.Unexpected` ở `src/BE/Core/CoreAndSkill.Core.Application/Common/CommonErrors.cs`). Nhưng không màn nào hiện mã theo dõi. `traceId` chỉ tới người dùng qua `ToastService.loi(summary, traceId)`. Người dùng được bảo báo một mã mà họ không nhìn thấy. [`../contracts/auth.md`](../contracts/auth.md) §11 đã khai cho `CORE.SYSTEM.UNEXPECTED`: *"Toast lỗi hệ thống kèm `traceId`"*. Code đang trái hợp đồng đó ở mọi request mang cờ.

Kiến trúc sư đề xuất cho §8 thắng và dựng một lớp lỗi xuyên suốt, không để cờ tắt được lớp đó.

## Quyết định

Người dùng chốt ngày 2026-09-25:

1. **Lớp lỗi xuyên suốt gồm mọi phản hồi 5xx, và 403 mang mã `CORE.AUTH.*`, trừ `CORE.AUTH.PASSWORD_CHANGE_REQUIRED`.** Mã loại trừ đó đã có nhánh riêng. `errorInterceptor` **luôn** toast lớp này kèm `traceId`, kể cả khi request mang `BO_QUA_TOAST_LOI`.
2. **Khu lỗi của màn không hiện lỗi thuộc lớp này.** Toast đã nói thay.
3. **`BO_QUA_TOAST_LOI` chỉ tắt toast cho lỗi của màn.** Mất kết nối, 429 và 409 `CORE.CONCURRENCY.CONFLICT` vẫn theo cờ.
4. **Tệp dịch có khoá `loi.CORE.SYSTEM.UNEXPECTED`, và câu của khoá đó không nhúng `traceId`.** Toast gắn mã riêng.
5. **`fe-routing-guard.md` §8 giữ nguyên.** `fe-api-client.md` §2.2 và đoạn *"Toast và banner không đi cùng nhau"* ở `fe-ui-conventions.md` §6.2 sửa theo.

Định nghĩa của lớp nằm ở đúng một chỗ: `fe-api-client.md` §2.2. Tệp này không liệt kê lại.

## Phương án đã cân nhắc và vì sao loại

### Phương án B — Cờ thắng ở mọi nơi, sửa §8 cho khớp §2.2

**Được:** một luật đơn giản: có cờ thì không toast. Màn toàn quyền với lỗi của request nó gửi.

**Mất:** màn nào mang cờ cũng phải tự hiện `traceId` cho 5xx, và tự báo thiếu quyền theo cùng một cách với mọi màn khác. Hôm nay không màn nào làm việc đó. Mỗi màn mới cũng phải nhớ làm. Nghĩa vụ rải ra nhiều màn, thay vì nằm ở một interceptor.

**Vì sao loại:** `traceId` là thứ duy nhất người dùng mang theo được khi báo lỗi hệ thống. Để nó phụ thuộc việc từng màn nhớ làm đúng thì nó sẽ mất ở màn đầu tiên quên. Đó đúng là tình trạng code hôm nay.

### Phương án C — Khu lỗi của màn tự hiện `traceId`

**Được:** lỗi chỉ hiện ở một chỗ, ngay trong ngữ cảnh người dùng đang nhìn.

**Mất:** `applyFormFailure` phải trả thêm `traceId` bên cạnh câu. Mọi chỗ gọi phải đổi. `NoticeBanner` cần một vị trí cho mã, tức là việc của `docs/Design/`. Các khu lỗi không đi qua `applyFormFailure` (khu lỗi của `AuthCard`, trạng thái lỗi của `Sidebar`, lớp nổi của `Autocomplete`) mỗi chỗ phải làm riêng. 403 `CORE.AUTH.*` vẫn hiện khác nhau theo màn.

**Vì sao loại:** chi phí lan tới hợp đồng một hàm dùng chung, một component Design, và mọi khu lỗi đặc thù. Lợi ích là bỏ một toast, và lớp lỗi này vốn không thuộc riêng màn nào.

### Phương án D — Chỉ 5xx luôn toast, 403 `CORE.AUTH.*` vẫn theo cờ

**Được:** hẹp hơn. Phủ đúng ca mất `traceId`.

**Mất:** §8 vẫn mâu thuẫn §2.2 cho `FORBIDDEN` và `ORIGIN_REJECTED`, nên phải sửa §8. Thiếu quyền vẫn hiện toast ở màn này, banner ở màn khác. `FORBIDDEN` kéo theo `lamMoiQuyen()`, tức làm mới tập quyền và menu của cả khung. Đó là việc của khung, không phải của form vừa gửi.

**Vì sao loại:** giải nửa vấn đề và để nguyên mâu thuẫn giữa hai tệp luật.

## Hệ quả

### Tích cực

- `traceId` luôn tới người dùng khi hệ thống hỏng, bất kể màn đặt cờ gì.
- Hai tệp luật hết nói khác nhau. Một chỗ quyết, ở interceptor.
- Màn mới không phải nhớ gì cho lớp này.

### Tiêu cực

| Cái giá | Nghĩa là |
| --- | --- |
| **Màn mất quyền tự lo 5xx và 403 `CORE.AUTH.*`** | Màn muốn hiện lỗi tại chỗ vẫn nhận toast. Screen spec đang cho một mã của lớp vào khu lỗi của màn thì nay trái quyết định này. Ca đã thấy: màn đăng nhập cho `CSRF_REJECTED` sau lần gửi lại vào khu lỗi của `AuthCard`. `design-expert` phải rà các screen spec |
| **Hai nửa phải đi cùng nhau, và nửa thứ hai rải ở nhiều chỗ** | Interceptor toast ở một chỗ. Khu lỗi bỏ qua lớp này ở từng màn. Màn nào quên thì lỗi hiện hai lần, một toast và một banner. Chưa có cổng: nợ **F41** ở [`../DEBT.md`](../DEBT.md) |
| **Lớp định nghĩa theo tiền tố, nên mã mới tự vào lớp** | Một mã 403 `CORE.AUTH.*` mới, thêm sau này, tự động luôn toast. Mã cần nhánh riêng như `PASSWORD_CHANGE_REQUIRED` thì người thêm mã phải khai loại trừ ở §2.2. Không gì nhắc họ làm việc đó |
| **5xx không mang envelope không có `traceId`** | Proxy trả trang HTML cho 502 hay 504 thì không có envelope, nên không có mã để hiện. Câu dùng cho ca này **chưa chốt**. Mất kết nối, tức không có phản hồi nào, là nhánh khác và vẫn theo cờ |
| **BE hỏng hàng loạt thì toast dồn** | Một trang gửi nhiều request song song mà cả loạt ra 500 thì mỗi request một toast. Request không mang cờ vốn đã vậy. Nay thêm cả request mang cờ |

### Rút lui nếu sai

Quyết định chỉ nằm ở FE, không sinh dữ liệu. Rút lui gồm ba bước. Cho hàm nhận diện lớp dùng chung (tên do `frontend-expert` đặt lúc thi công) trả `false` cho mọi lỗi: interceptor và khu lỗi quay về hành vi theo cờ. Gỡ đoạn định nghĩa lớp ở `fe-api-client.md` §2.2, và câu tương ứng ở `fe-ui-conventions.md` §6.2. Viết ADR mới lật ADR này, rồi sửa §8 của `fe-routing-guard.md` cho khớp. Không có bước dữ liệu nào.

### Dấu hiệu quyết định này bắt đầu sai

- Người dùng báo thấy cùng một lỗi hai lần: một màn đã quên nửa thứ hai.
- Một màn có lý do thật để hiện 5xx tại chỗ mà không muốn toast. Khi đó lớp cần một lối thoát có tên, không phải thêm cờ thứ hai.
- Ba mã 403 `CORE.AUTH.*` trở lên cần nhánh riêng. Khi đó định nghĩa theo tiền tố đang bắt nhầm nhiều hơn bắt đúng.

## Liên quan

- [`../quy-uoc/fe-api-client.md`](../quy-uoc/fe-api-client.md) §2.2: định nghĩa lớp, thứ tự nhánh
- [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) §6.2: khu lỗi của màn
- [`../quy-uoc/fe-routing-guard.md`](../quy-uoc/fe-routing-guard.md) §8: giữ nguyên
- [`../contracts/auth.md`](../contracts/auth.md) §11: mã dùng chung và việc FE làm
- [`../DEBT.md`](../DEBT.md) F41: luật chưa có cổng
