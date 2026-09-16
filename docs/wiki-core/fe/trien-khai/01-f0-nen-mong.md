---
kind: luat
scope: core
verified: chua-doi-chieu
---

# F0 — Nền móng

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**
>
> **Định nghĩa hoàn thành:** app build xanh ở chế độ zoneless; cấu trúc tầng đúng; cấu hình ESLint ép được ranh giới (chứng minh bằng canary); gọi **một endpoint cố tình trả lỗi** (endpoint thử của pha B0 — [`00-lo-trinh-tong-the.md`](00-lo-trinh-tong-the.md) §5) → hiện đúng thông điệp của BE, không phải chuỗi rỗng, không phải `undefined`, không phải câu do framework sinh; và **ít nhất một test interceptor đã từng đỏ** trước khi code chạy đúng.

---

## 1. Dựng workspace

Tạo app Angular với style SCSS, có routing, **không** SSR ([`../17-phuc-vu-va-trien-khai.md`](../17-phuc-vu-va-trien-khai.md) §1).

Ba thứ bật ngay ở lần cấu hình đầu tiên, **không** để mặc định rồi đổi sau:

| Bật ngay | Vì sao không để sau |
| --- | --- |
| **Zoneless** | Component viết trong lúc còn chế độ cũ chưa được kiểm chứng dưới chế độ mới; gỡ sau thì lỗi lộ rải rác — [`../13-performance.md`](../13-performance.md) §2.1 |
| **`OnPush` là mặc định** | Đổi sau là rà lại mọi component đã viết |
| **Chế độ TypeScript nghiêm ngặt** | Nới thì dễ, siết lại trên codebase đã lớn thì gần như không ai làm |

### 1.1 Toolchain — chốt một lần, ở F0

> 📖 Phiên bản framework, thư viện UI, thư viện dịch và kế hoạch nâng cấp: [`../../../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md`](../../../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md). File này **không** chép số phiên bản nào ([`../16-nen-tang-va-nang-cap.md`](../16-nen-tang-va-nang-cap.md) §8).

| Hạng mục | Chốt thế nào |
| --- | --- |
| Node, TypeScript, angular-eslint | Theo **bảng tương thích chính thức** của phiên bản Angular đã chốt, tra ở angular.dev lúc dựng workspace — không lấy số từ trí nhớ hay từ dự án khác |
| Nơi giữ phiên bản | `package.json` ghim **chính xác** (không dải mở) kèm trường `engines`, và `.nvmrc` cạnh nó. Từ F0, hai tệp này là **nguồn phiên bản duy nhất**; tài liệu đọc số bằng lệnh |
| Bộ chạy test | Karma + Jasmine, trình duyệt ChromeHeadless ([`../06-testing-strategy.md`](../06-testing-strategy.md) §7) |
| Plugin ranh giới | `eslint-plugin-import` — quy tắc chặn đường dẫn của nó ép F1, F2 và F24 (§3) |
| Path alias trong `tsconfig` | **Không** dùng ở v1 |

### 1.2 HTTPS ở máy dev

FE và API khác nguồn ở mọi môi trường, nên máy dev cũng chạy HTTPS — lý do ở [`../17-phuc-vu-va-trien-khai.md`](../17-phuc-vu-va-trien-khai.md) §4. Máy chủ dev của FE dùng chính chứng chỉ dev của .NET, xuất ra dạng PEM:

```bash
# 1. Tạo và tin cậy chứng chỉ dev trên máy — một lần mỗi máy
dotnet dev-certs https --trust

# 2. Xuất cert và khoá riêng dạng PEM, khoá không đặt mật khẩu, vào src/FE/.cert/ — chạy từ gốc repo.
#    Ra hai tệp: localhost.pem (cert) và localhost.key (khoá riêng).
mkdir -p src/FE/.cert
dotnet dev-certs https --export-path "src/FE/.cert/localhost.pem" --format Pem --no-password
```

Khai `ssl`, `sslCert`, `sslKey` cho cấu hình dev của máy chủ dev trong `angular.json`: `sslCert` là `.cert/localhost.pem`, `sslKey` là `.cert/localhost.key` — đường dẫn tính từ `src/FE/`, nơi đặt `angular.json`.

🛑 **Không commit cert hay khoá riêng.** Một khoá riêng đã vào commit thì nằm trong lịch sử git — commit xoá sau đó không gỡ được nó. `src/FE/.cert/` phải bị `.gitignore` loại **trước** lần xuất đầu tiên; dòng ignore thuộc [`../../../quy-uoc/repo-artifact.md`](../../../quy-uoc/repo-artifact.md).

---

## 2. Cấu trúc bốn tầng

```
src/app/
  core/        ← tầng đáy: http, interceptor, auth, guard, i18n, theme, menu, toast
  shared/      ← ui (bọc thư viện), components, directives, models
  platform/    ← màn Core: shell, đăng nhập, đổi mật khẩu, trang đích, quản trị
  modules/     ← nghiệp vụ — CHƯA tồn tại ở F0, xem [00-lo-trinh-tong-the.md](00-lo-trinh-tong-the.md) §4
```

**Chiều phụ thuộc chỉ đi xuống.** `core/` không được import bất cứ thứ gì từ ba tầng trên.

> 📖 Cấu trúc bên trong một feature, quy ước đặt tên file: [`../../../quy-uoc/fe-architecture.md`](../../../quy-uoc/fe-architecture.md).

**Không tạo sẵn thư mục rỗng.** Tầng nào chưa có file thì chưa có thư mục. Git không theo dõi thư mục rỗng, nên thư mục tạo sẵn sẽ biến mất ở bản clone kế tiếp — và một mục nghiệm thu "đã tạo đủ bốn thư mục" sẽ không bao giờ tick đúng.

---

## 3. Ranh giới bằng ESLint

Đây là hạng mục **quan trọng nhất** của F0, và cũng là hạng mục dễ bị hoãn nhất vì nó không tạo ra thứ gì nhìn thấy được.

### 3.1 Vùng ranh giới

| Vùng | Luật | Trạng thái ở F0 |
| --- | --- | --- |
| `core/` là tầng đáy — không import ngược lên `shared/`, `platform/`, `modules/` | F1 | Bật ngay, và **kiểm được ngay** |
| `modules/<A>/` không import nội bộ `modules/<B>/` | F2 | Danh sách module rỗng ⇒ vùng này chưa có gì để ép |
| `shared/ui/` không import `shared/components/` | F24 | Bật ngay — `shared/ui/` có mặt từ F0 (bọc `Toast`, §4); canary theo §3.3 với một file trong mỗi thư mục. Khuôn cấu hình: [`../../../quy-uoc/fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §4.6 |

### 3.2 Bẫy của vùng thứ hai — đọc trước khi cấu hình

Quy tắc chặn đường dẫn của ESLint đòi danh sách vùng có **ít nhất một** phần tử. Truyền một mảng rỗng làm ESLint chết ngay lúc nạp cấu hình, và toàn bộ `ng lint` đỏ vì một lý do không liên quan gì tới code.

Cách xử lý đúng: **bỏ hẳn khối cấu hình đó khi danh sách module rỗng**, và ghi lý do ngay tại chỗ.

Mảng tên module khai ở `src/FE/eslint.boundaries.cjs`; `eslint.config.js` `require` tệp đó để sinh vùng, và cổng F4 đọc **cùng tệp** — [`../../../quy-uoc/fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §4.3–§4.4.

Cách xử lý **sai** — và đã xảy ra thật ở dự án tiền nhiệm: để lại tên của những module không còn tồn tại. Khi đó quy tắc không phân giải nổi đường dẫn đích nên **không chặn được gì**, nhưng cấu hình vẫn *trông như* đang chạy. Đúng kiểu "cổng xanh vì không kiểm gì cả".

Hệ quả: cần một cổng riêng kiểm rằng danh sách module trong cấu hình **khớp thư mục thật** — đó là luật F4, xem [`05-gate.md`](05-gate.md).

### 3.3 Chứng minh bằng canary

Một cấu hình ranh giới chưa được chứng minh là một cấu hình chưa biết có chạy không.

Phép thử, làm ngay khi vừa cấu hình xong:

1. Thêm tạm một import từ `shared/` vào một file trong `core/`.
2. Chạy lint → **phải đỏ**, và phải đỏ với đúng thông điệp đã khai.
3. Hoàn nguyên.

Không làm bước này thì không có gì phân biệt "quy tắc đang chạy" với "quy tắc không phân giải được đường dẫn nên im lặng cho qua".

### 3.4 Cấm `eslint-disable` cho quy tắc ranh giới

ESLint bỏ qua được bằng một dòng comment. Nghĩa là **không có F3 thì F1 và F2 chỉ là gợi ý** ([`../../../RULES.md`](../../../RULES.md) §7).

Cổng F3 quét chính xác danh sách quy tắc ranh giới, không quét mọi `eslint-disable` — có những quy tắc mà tắt cục bộ là hợp lý. Cổng **đọc** danh sách quy tắc cấm tắt từ bảng chủ ở [`../../../quy-uoc/fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §4.5 lúc chạy, không giữ bản sao ([`05-gate.md`](05-gate.md) §8.1).

---

## 4. `core/http` — thứ viết trước tiên

Mọi service sau này đều đi qua đây, nên sai ở đây là sai lan ra toàn app.

| File | Việc |
| --- | --- |
| `core/http/api-result.model.ts` | `ApiResult<T>` khớp 1:1 hợp đồng BE; chứa luôn kiểu lỗi có envelope và hàm đọc envelope an toàn |
| `core/http/unwrap.ts` | Một cửa duy nhất để lấy `data` ra khỏi envelope |
| `core/interceptors/*.interceptor.ts` | Chuỗi interceptor, đúng thứ tự khai ở file chủ |
| `core/toast/toast.service.ts` | Nơi duy nhất hiện thông báo chung |

> 📖 **Chuỗi interceptor gồm những gì, thứ tự nào, tên file nào — và định nghĩa `ApiResult<T>`: [`../../../quy-uoc/fe-api-client.md`](../../../quy-uoc/fe-api-client.md) §1 và §2. Bốn cái bẫy đã trả giá thật khi tiêu thụ envelope: [`../02-http-envelope.md`](../02-http-envelope.md) §5. Chép từ đó, đừng viết lại từ trí nhớ.**

**`ToastService` thuộc `core/`, không thuộc `shared/`.** Đây là chỗ vi phạm F1 hay xảy ra nhất: interceptor ở `core/` cần hiện thông báo, và nơi hiện thông báo bị đặt ở `shared/` — thế là `core/` import ngược. Cách đúng: **service** hạ tầng ở `core/toast/`, **component** hiển thị ở `shared/ui/toast/` — `Toast` mang cột Nền "bọc PrimeNG" ([`../05-component-library.md`](../05-component-library.md) §3) — và nó import ngược lại từ `core/` (đúng chiều được phép).

---

## 5. Thứ tự viết

```
1. ApiResult<T> — thuần khai báo kiểu
        │
        ▼
2. Interceptor dịch lỗi + ToastService
        │
        ▼
3. Test interceptor — CHO NÓ ĐỎ TRƯỚC
   (khẳng định theo hình dạng sai, chạy thấy fail, rồi sửa cho xanh)
        │
        ▼
4. Các interceptor còn lại + đăng ký đúng thứ tự
        │
        ▼
5. Cấu hình ESLint ranh giới + canary (§3.3)
```

**Bước 3 không được bỏ.** Một test viết xong xanh ngay từ đầu không chứng minh được gì — nó có thể đang xanh vì khẳng định sai. Xem [`../06-testing-strategy.md`](../06-testing-strategy.md) §8.

---

## 6. Ba thứ hay bị làm sai ở F0

**(1) Đăng ký interceptor sai thứ tự.** Thứ tự là ràng buộc thật ([`../../../quy-uoc/fe-api-client.md`](../../../quy-uoc/fe-api-client.md) §2). Triệu chứng khi sai: request đi ra thiếu base URL hoặc thiếu cookie phiên, và lỗi trông như lỗi của BE.

**(2) Ép kiểu cho qua ở chỗ đọc envelope.** Không phải mọi phản hồi lỗi đều có envelope (mất mạng, proxy trả HTML). Ép kiểu là cách nhanh nhất để có một `undefined` hiển thị lên màn hình.

**(3) Coi cấu hình lint là việc dọn dẹp cuối pha.** Bật cuối pha nghĩa là mọi file viết trong pha đó chưa từng bị kiểm, và lần bật đầu tiên sẽ cho một danh sách vi phạm dài tới mức người ta muốn tắt bớt quy tắc.

---

## 7. Nghiệm thu F0

- [ ] Build xanh; app chạy zoneless; chế độ TypeScript nghiêm ngặt bật
- [ ] Node, TypeScript, angular-eslint chốt theo bảng tương thích chính thức của phiên bản Angular đã chốt; ghim chính xác trong `package.json` (có `engines`) và `.nvmrc` (§1.1)
- [ ] Không tài liệu nào chép số phiên bản vừa chốt — đọc bằng lệnh ở [`../16-nen-tang-va-nang-cap.md`](../16-nen-tang-va-nang-cap.md) §8
- [ ] `ng test` chạy Karma + Jasmine trên ChromeHeadless ở chế độ không giám sát
- [ ] Máy chủ dev của FE chạy HTTPS bằng chứng chỉ dev đã tin cậy; không cert hay khoá riêng nào nằm trong commit (§1.2)
- [ ] Cấu trúc tầng đúng; **không** có thư mục rỗng nào được tạo sẵn
- [ ] `ApiResult<T>` khớp hợp đồng BE, xác nhận bằng **một lần gọi thật** hoặc tài liệu API — không đoán
- [ ] Gọi endpoint thử của B0 ở nhánh lỗi → thông báo hiện đúng `message` của BE
- [ ] Ngắt mạng giữa chừng → app hiện câu dự phòng, không văng exception
- [ ] Lỗi đi ra khỏi interceptor mang theo envelope đã bóc và `traceId` (bẫy spread prototype)
- [ ] Có ít nhất một test interceptor đã chứng minh là **từng đỏ**
- [ ] Canary ranh giới: import ngược từ `core/` lên `shared/` → lint **đỏ**; hoàn nguyên → xanh
- [ ] Khối cấu hình vùng module được bỏ hẳn (không để tên module không tồn tại)
- [ ] Cổng F3 chặn được `eslint-disable` cho quy tắc ranh giới — kiểm bằng canary tương tự
- [ ] `ToastService` nằm ở `core/`, component hiển thị ở `shared/`

---

## 8. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| Envelope và bốn cái bẫy | [`../02-http-envelope.md`](../02-http-envelope.md) |
| Cấu trúc feature, đặt tên | [`../../../quy-uoc/fe-architecture.md`](../../../quy-uoc/fe-architecture.md) |
| Toàn bộ cổng | [`05-gate.md`](05-gate.md) |
| Pha kế tiếp | [`02-f1-design-token.md`](02-f1-design-token.md) |
| Vì sao giữ cấu trúc thư mục | [`../../../adr/0007-fe-giu-cau-truc-thu-muc.md`](../../../adr/0007-fe-giu-cau-truc-thu-muc.md) |
