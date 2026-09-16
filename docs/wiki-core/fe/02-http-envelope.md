---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 02. Envelope HTTP — hình dạng dữ liệu giữa BE và FE

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`; mọi đoạn code dưới đây là thứ phải viết, không phải thứ đang chạy.
>
> **Hợp đồng chính thức của từng endpoint nằm ở [`../../contracts/README.md`](../../contracts/README.md).** File này mô tả *hình dạng chung* và *cách FE tiêu thụ nó*. Khi hai bên lệch nhau, `contracts/` thắng và file này phải sửa.
>
> Phía BE dựng envelope này ở đâu và theo luật nào: [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) · catalog mã lỗi: [`../be/16-i18n-va-ma-loi.md`](../be/16-i18n-va-ma-loi.md).

---

## 1. Vì sao có envelope, thay vì trả thẳng dữ liệu

Trả thẳng `{ id, name }` với 200 và `{ error: "..." }` với 400 là cách đơn giản nhất — và nó hỏng ở đúng ba chỗ:

| Vấn đề | Không envelope | Có envelope |
| --- | --- | --- |
| Nối log hai phía | FE không biết request của mình mang `traceId` nào ở BE | `traceId` có trong **mọi** phản hồi, kể cả thành công |
| Lỗi nghiệp vụ vs lỗi kỹ thuật | Cả hai đều là 400, phân biệt bằng đọc chuỗi | Mã lỗi nghiệp vụ phân biệt bằng máy |
| Lỗi theo từng ô nhập | Mỗi endpoint tự chọn một hình dạng | `fieldErrors` một hình dạng duy nhất cho toàn hệ |

Cái giá phải trả là mỗi lời gọi mang thêm vài trăm byte và một tầng bóc vỏ. Đổi lại, FE có **đúng một** chỗ hiểu hình dạng phản hồi — và đó là điều kiện để có xử lý lỗi tập trung ở §4.

---

## 2. Hình dạng envelope — file này KHÔNG khai lại

> 📖 **Hình dạng trên dây (kiểu C#, ba khối JSON mẫu, chỗ dựng duy nhất, luật casing): đọc [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §2.**
>
> 📖 **Kiểu TypeScript phía FE (`ApiResult<T>`, `ApiError`, `ApiFieldError`) và chỗ khai chúng: đọc [`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §1.**

Đọc sai hình dạng — trường lỗi ở gốc envelope, cờ thành công tên khác, hay trường không có trên dây — hỏng ba chỗ:

| Chỗ hỏng | Triệu chứng |
| --- | --- |
| Hàm nhận diện envelope hỏi một cờ không tồn tại trong payload | Hàm **luôn** trả `null` ⇒ mọi lỗi hiện câu dự phòng, `traceId` không bao giờ tới màn hình |
| Khoá dịch dựng từ một trường ở gốc envelope | `undefined` trong khoá ⇒ toast trống |
| Điều kiện kiểm lỗi từng ô ở gốc envelope | Luôn sai ⇒ lỗi validation rơi xuống toast, đúng thứ §4 cấm |

Cả ba đều là JSON hợp lệ ở hai phía, nên **không có lỗi biên dịch và không test hình dạng nào đỏ**. Đó là lý do hình dạng chỉ được khai ở một chỗ: hai bản khai thì bản không ai sửa sẽ nói dối ([`../../OWNERSHIP.md`](../../OWNERSHIP.md)).

### 2.1 Ba tính chất của hình dạng đó, và vì sao chúng có mặt

| Tính chất | Vì sao |
| --- | --- |
| Cờ thành công là field riêng, không suy ra từ `data` | Có thao tác thành công mà không trả dữ liệu (xoá, đổi mật khẩu). Suy ra từ `data !== null` thì mọi thao tác như thế bị coi là lỗi |
| Toàn bộ phần lỗi gom trong **một** object con | Sau một câu kiểm cờ thành công, TypeScript biết chắc object đó có mặt — không còn `?.` rải khắp nơi, và không còn envelope "nửa lỗi nửa thành công" |
| `traceId` có cả khi thành công | Lỗi hiển thị sai dữ liệu không phải lỗi HTTP. Không có `traceId` ở nhánh thành công thì đúng loại lỗi đó không truy được |

### 2.2 Quy ước khoá của `fieldErrors` — chỗ dễ hỏng im lặng nhất

> 📖 **Luật casing (payload camelCase, khoá của `fieldErrors` PascalCase, cơ chế ở BE, và vì sao "sửa cho nhất quán" sẽ phá nó **im lặng**): đọc [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §2.3.**

Tra thẳng khoá BE gửi mà không chuyển đổi thì, với khoá PascalCase thật sự đi trên dây, phép tra **không tìm thấy gì, không báo gì**: form hiện lỗi chung, ô nhập sạch sẽ, người dùng không biết sửa chỗ nào.

Vì vậy bước chuyển khoá → tên control là **bắt buộc** và nằm ở đúng một chỗ: hàm gắn lỗi vào form ([`09-forms-validation.md`](09-forms-validation.md) §4).

Phép thử khi thi công: một request cố tình sai một trường → mở tab Network → khoá trong `fieldErrors` phải khớp **tên property của DTO phía BE**, và ô nhập tương ứng phải sáng lên lỗi. Một trong hai vế sai là hỏng, dù màn hình trông vẫn ổn.

---

## 3. HTTP status vẫn mang nghĩa

Ví dụ payload cho ba trường hợp (thành công, lỗi nghiệp vụ, lỗi validation) nằm ở [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §2.1 — file này không chép lại chúng.

**Envelope không thay thế status:** proxy, log truy cập và trình duyệt đều đọc status. Trả 200 kèm một cờ thất bại trong thân là kiểu thiết kế làm mọi công cụ hạ tầng mù — đừng làm.

---

## 4. Xử lý lỗi tập trung ở interceptor, không rải bắt lỗi khắp service

### 4.1 Vì sao

Nếu mỗi service tự bắt lỗi, ba thứ xảy ra và cả ba đều không lộ ra ngay:

1. **Câu chữ khác nhau giữa các màn** — cùng một lỗi 403 chỗ thì nói "Bạn không có quyền", chỗ thì nói "Đã có lỗi xảy ra".
2. **Một service quên bắt** thì lỗi rơi vào `console` và người dùng thấy một màn đứng im.
3. **Không có chỗ nào để thêm hành vi chung** — muốn tự động đăng xuất khi 401 thì phải sửa mọi service.

Nguyên tắc: **một request lỗi được xử lý đúng một lần, ở interceptor.** Service chỉ xử lý lỗi khi nó cần làm gì đó *khác* với mặc định — và khi đó nó phải tự tắt hành vi mặc định một cách tường minh.

### 4.2 Chuỗi interceptor và thứ tự

> 📖 **Chuỗi interceptor FE (gồm những gì, đúng thứ tự nào, đặt ở thư mục nào) và khuôn của từng cái: đọc [`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §2.**

**Thứ tự là ràng buộc thật, không phải chi tiết trình bày.** Interceptor Angular chạy theo thứ tự khai cho chiều đi và theo thứ tự **ngược lại** cho chiều về. Interceptor dịch lỗi đứng cuối để nó là lớp trong cùng ở chiều đi và lớp ngoài cùng ở chiều về — tức nó thấy lỗi của mọi tầng phía trong.

Đừng chép số lượng interceptor vào bất kỳ tài liệu nào ([`../../../.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §6). Đọc mảng tại chỗ:

```bash
grep -n 'withInterceptors' src/FE/src/app/app.config.ts
```

### 4.3 Khuôn interceptor lỗi và hàm đọc envelope an toàn

> 📖 **Mã của interceptor lỗi, hàm đọc envelope an toàn, và cờ tắt toast theo từng request: đọc [`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §1 và §2.2.**

Hai tính chất của hàm đọc envelope mà mọi nơi tiêu thụ phải chịu được, ghi ở đây vì chúng là *cách dùng*, không phải định nghĩa:

1. **Trả `null` là trường hợp bình thường**, không phải lỗi lập trình: mất mạng, reverse proxy trả HTML, hoặc BE chết trước khi kịp dựng envelope. Ép kiểu cho qua ở đây là cách nhanh nhất để có một `undefined` hiển thị lên màn hình.
2. **Nó nhận diện envelope bằng đúng tên field có thật trên dây.** Hỏi một tên khác thì hàm **luôn** trả `null`, mọi lỗi hiện câu dự phòng, và `traceId` không bao giờ tới màn hình — hỏng toàn phần mà không có một dòng lỗi nào.

### 4.4 Cách một service tự lo lỗi của mình

Tắt toast mặc định cho **đúng một request** bằng `HttpContext`, không bằng một cờ toàn cục: cờ toàn cục nghĩa là hai request chạy song song sẽ giẫm lên nhau, và không có gì báo. Tên token và cách dùng ở [`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §2.2.

Trường hợp thường gặp nhất — lỗi validation có `fieldErrors` — **không cần** cờ này: interceptor đã tự không bắn toast cho chúng, vì người dùng đang nhìn cái form.

---

## 5. Bốn cái bẫy đã trả giá thật

### 5.1 Spread `HttpErrorResponse` phá prototype

```typescript
return throwError(() => ({ ...error, envelope }));   // ❌
```

Spread một instance class chỉ chép **thuộc tính own**. Kết quả là một object thường: mất prototype, mất getter, và mọi chỗ sau đó kiểm `error instanceof HttpErrorResponse` đều trả `false`. Không có lỗi biên dịch, không có lỗi lúc chạy — chỉ có một nhánh `if` im lặng không bao giờ vào.

Khuôn ở file chủ tránh hẳn cái bẫy này bằng cách **không** cố nhét envelope vào lỗi gốc: interceptor ném ra một lớp lỗi riêng mang theo envelope đã bóc ([`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §2.2). Bẫy vẫn ghi ở đây vì cám dỗ "chỉ thêm một thuộc tính vào lỗi cho tiện" quay lại ở mọi dự án — và nếu làm, phải gán lên chính instance, không spread.

### 5.2 `inject()` gọi bên trong `catchError`

```typescript
return next(req).pipe(
  catchError((error) => {
    const toast = inject(ToastService);   // ❌ ném lỗi lúc chạy, chỉ khi có lỗi HTTP
    ...
  }),
);
```

`inject()` chỉ hợp lệ trong injection context. Thân hàm interceptor là injection context; **callback của `catchError` thì không** — nó chạy sau, ở một tick khác. Lỗi này đặc biệt độc vì nó chỉ nổ ở **đúng nhánh xử lý lỗi**: mọi thứ chạy tốt cho tới lần đầu có lỗi thật, và lúc đó lỗi thật bị thay bằng một lỗi khác.

### 5.3 Đọc `HttpErrorResponse.message` thay vì câu dựng từ envelope

`HttpErrorResponse.message` là chuỗi Angular tự sinh, đại loại `Http failure response for /api/v1/core/users: 409 Conflict`. Nó **không** phải câu nghiệp vụ. Hiển thị nó lên màn hình là biến một thông báo có ích thành một dòng kỹ thuật vô nghĩa với người dùng — và đó đúng là lỗi mà cả envelope này sinh ra để tránh.

Thứ tự đọc đúng: câu FE **dịch từ mã lỗi** trước, câu BE gửi kèm làm đường lùi, rồi mới tới câu dự phòng chung cho trường hợp không có envelope ([`08-i18n.md`](08-i18n.md) §5.4). `HttpErrorResponse.message` không nằm trong ba bậc đó.

### 5.4 Đặt tên kiểu envelope trong tài liệu khác với tên trong code

Ở dự án tiền nhiệm, tài liệu nhắc tới một kiểu tên `ApiHttpError` trong nhiều file, còn code khai một tên khác. Ai chép mẫu từ tài liệu đều nhận lỗi biên dịch, và cái tên ma đó sống qua nhiều lượt sửa vì mỗi file chỉ sửa một nửa.

Ở repo này: **tên kiểu envelope phía FE chỉ được khai ở [`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §1**, và trong code ở đúng một file thuộc `core/http/`. File khác — kể cả file này — nhắc tới thì trỏ đường, không chép định nghĩa ([`../../../.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §5).

Chính file này từng vi phạm đúng luật đó: nó khai một hình dạng envelope riêng **và** tuyên bố mình là nơi duy nhất khai hình dạng ấy, trong khi một file FE khác khai một hình dạng khác cho **cùng một đường dẫn file code**. Hai bản khai cùng trỏ vào một file nguồn là dấu hiệu chắc chắn nhất của chủ quyền chưa được chốt.

---

## 6. Ranh giới DTO ↔ model

| Khái niệm | Đuôi tên | Sống ở | Ai được import |
| --- | --- | --- | --- |
| DTO — hình dạng **wire**, do BE quyết | `UserDto` | `<feature>/services/` | Chỉ `services/` và `*.spec.ts` |
| Model — hình dạng **app**, do FE quyết | `User` | `<feature>/models/` | Mọi tầng |

Luật F10 ([`../../RULES.md`](../../RULES.md)) cấm `components/` và `pages/` import DTO. Lý do: TypeScript bị xoá lúc chạy, nên khi BE đổi tên một field, **không có gì báo** — chỉ có một ô trống trên màn hình. Ép DTO đi qua một mapper trong `services/` tạo ra đúng một chỗ để lỗi đó lộ ra.

Chi tiết mapper và quy ước gọi API: [`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md).

> ⚠️ **Bẫy của cổng F10 khi thi công:** mẫu tìm kiếm phải là `Dto\b`, **không** phải `\bDto\b`. Tên DTO thật luôn có ký tự chữ ngay trước `Dto`, nên không có word boundary ở đầu — mẫu có `\b` ở đầu sẽ không bao giờ khớp, và cổng xanh vì mù chứ không vì sạch. Cùng bẫy này đã dính ở dự án tiền nhiệm và được phát hiện bằng canary.

---

## 7. Kiểm chứng envelope khi thi công

- [ ] Gọi một endpoint cố tình trả lỗi nghiệp vụ → toast hiện câu FE dịch từ mã lỗi, và khi chưa có bản dịch thì hiện câu BE gửi kèm — không phải chuỗi rỗng, không phải `undefined`, không phải câu do Angular sinh
- [ ] Một request lỗi validation → mỗi khoá trong `fieldErrors` gắn được vào đúng một control sau bước chuyển khoá, và **mã lỗi từng ô được dịch**, không hiện thô
- [ ] Ngắt mạng giữa chừng → hàm đọc envelope trả `null`, app hiện câu dự phòng, không văng exception
- [ ] Lỗi đi ra khỏi interceptor mang theo envelope đã bóc và `traceId` đọc được ở nơi bắt (bẫy §5.1)
- [ ] Test interceptor đã **từng đỏ** trước khi xanh — một test xanh ngay từ đầu không chứng minh được gì
- [ ] `traceId` hiển thị được ở màn lỗi chung, để người dùng đọc cho người hỗ trợ

---

## 8. §Áp dụng — Core này có gì, cố ý thiếu gì

> 📐 Bảng dưới là **đích đến của giai đoạn 2**, không phải hiện trạng. Ý nghĩa ba ký hiệu trạng thái: [`../README.md`](../README.md) §9.

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Envelope `ApiResult<T>` khớp hình dạng ở file chủ | ✅ sẽ có | Kiểu khai đúng **một** chỗ: [`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §1 |
| Chuỗi interceptor đúng thứ tự | ✅ sẽ có | Pha F0 — danh sách ở [`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §2 |
| Xử lý lỗi tập trung + cờ tắt thông báo theo từng request | ✅ sẽ có | §4.4 |
| `traceId` có mặt kể cả khi thành công | ✅ sẽ có | Phụ thuộc BE ghi `traceId` cho cả nhánh thành công |
| Bước chuyển khoá `fieldErrors` → tên control, ở đúng một chỗ | ✅ sẽ có | §2.2 — khoá giữ PascalCase trên dây, luật ở file chủ BE |
| Ranh giới DTO ↔ model + mapper | ✅ sẽ có | Luật F10 |
| Tự động thử lại theo tín hiệu từ BE | ❌ chưa | Envelope **không** có trường nào nói "gọi lại được". Điều kiện: có endpoint thật sự hay timeout tạm thời, có số đo cho thấy thử lại ăn thua, và BE bổ sung tín hiệu vào hợp đồng |
| Hàng đợi request khi ngoại tuyến | ❌ chưa | Điều kiện: có người dùng thật làm việc ở nơi mất mạng (B10 ở [`01-core-components.md`](01-core-components.md)) |
| Bao lỗi thủ công ở từng service | ❌ loại, không hoãn `K12` | §4.1 — mỗi service tự xử lý lỗi là ba vấn đề cùng lúc |
| Trả 200 kèm cờ thất bại trong thân | ❌ loại, không hoãn `K13` | §3 — làm mọi công cụ hạ tầng mù |

> Cách đọc ba ký hiệu của bảng trên — và khi nào *"FE thiếu X"* là finding: [`../README.md`](../README.md) §9.

---

## 9. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| Hợp đồng từng endpoint | [`../../contracts/README.md`](../../contracts/README.md) |
| BE dựng envelope theo luật nào | [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) |
| Kiểu envelope phía FE | [`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §1 |
| Mã lỗi và tham số đặt tên | [`../be/16-i18n-va-ma-loi.md`](../be/16-i18n-va-ma-loi.md) · [`08-i18n.md`](08-i18n.md) |
| Bind `fieldErrors` vào form | [`09-forms-validation.md`](09-forms-validation.md) |
| `traceId` dùng để nối log thế nào | [`10-observability.md`](10-observability.md) |
| Quy ước gọi API và mapper | [`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) |
