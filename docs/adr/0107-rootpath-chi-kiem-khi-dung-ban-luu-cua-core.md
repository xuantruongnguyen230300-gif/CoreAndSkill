---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0107 — `Core:File:RootPath` chỉ bắt buộc khi bản lưu hiệu lực là bản đĩa cục bộ của Core; luật A8 đọc là *"bắt buộc khi thành phần đọc nó có mặt"*

> **Trạng thái:** Đã chấp nhận (2026-09-25)

## Bối cảnh

[ADR-0106](0106-ifilestorage-la-seam-du-an-thay-duoc.md) chốt `IFileStorage` là seam dự án thay được. Nó để mở một câu: dự án đã thay bản cài có còn phải khai `Core:File:RootPath` không. Trong lúc chờ, tài liệu ghi *giữ bắt buộc*.

Kiến trúc sư đối chiếu mã ngày 2026-09-25. Khoá đó bị kiểm ở **hai** chỗ, cả hai vô điều kiện:

- **Thiếu khoá:** attribute trên thuộc tính `RootPath` — `src/BE/Core/CoreAndSkill.Core.Application/Configuration/CoreFileOptions.cs`, chuỗi `[Required(AllowEmptyStrings = false)]`. Lớp này đăng ký `ValidateDataAnnotations()` cùng `ValidateOnStart()` ở `src/BE/Core/CoreAndSkill.Core.Web/DependencyInjection/CoreOptionsServiceCollectionExtensions.cs`, chuỗi `services.AddOptions<CoreFileOptions>()`.
- **Thư mục hợp lệ** (tuyệt đối, có thật, ghi được, ngoài thư mục ứng dụng): `CoreFileOptionsValidator`, đăng ký ở `src/BE/Core/CoreAndSkill.Core.Infrastructure/DependencyInjection/CoreInfrastructureServiceCollectionExtensions.cs`, chuỗi `services.AddSingleton<IValidateOptions<CoreFileOptions>, CoreFileOptionsValidator>();`.

Hai điều nữa đọc được ở mã:

- Ngoài validator, chỉ `LocalFileStorage` đọc `RootPath`.
- Cùng lớp `CoreFileOptions` mang các khoá còn lại của nhóm `Core:File:*`: trần dung lượng tải lên, hạn giữ tệp tạm, chu kỳ bảo trì kho. Mã tải lên, mã nhập và job bảo trì kho đọc chúng, dù bản lưu nào đang chạy.

Luật A8 ([`../RULES.md`](../RULES.md) §3) viết *"mọi `Options` bắt buộc có đường `ValidateOnStart`"*. Nó không nói gì về trường hợp thành phần đọc khoá không có mặt.

## Quyết định

Người dùng chốt ngày 2026-09-25, theo khuyến nghị ở ADR-0106: **Core chỉ kiểm `Core:File:RootPath` khi bản lưu hiệu lực là `LocalFileStorage` của Core; dự án đã thay `IFileStorage` thì Core bỏ qua cả hai phép kiểm của khoá đó.**

Ba ràng buộc đi kèm:

1. **Một điều kiện, không hai.** Phép kiểm `RootPath`, cả thiếu khoá lẫn thư mục hợp lệ, đăng ký theo **đúng** điều kiện mà Core dùng để thêm `LocalFileStorage`, đọc một lần. `LocalFileStorage` là `internal`, nên dự án không tự thêm được nó. Vì vậy điều kiện dùng chung không để lại ca nào bản đĩa cục bộ chạy mà thiếu phép kiểm.
2. **Các khoá còn lại của `Core:File:*` giữ `ValidateOnStart` vô điều kiện.** Mọi bản lưu đều dùng chúng.
3. **Luật A8 đọc là *"bắt buộc khi thành phần đọc nó có mặt"*.** Điều kiện hợp lệ duy nhất là **sự có mặt của thành phần**, quyết ở chính nhánh đăng ký thành phần đó. Môi trường hay một cờ cấu hình không bao giờ là điều kiện hợp lệ — [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §4.3 giữ nguyên.

`backend-expert` chọn cách tách phép kiểm thiếu khoá khỏi lớp dùng chung, ở nợ B23. Có hai cách: bỏ attribute và kiểm thiếu khoá trong validator có điều kiện, hoặc tách `RootPath` sang một lớp `Options` riêng đăng ký cùng `LocalFileStorage`. **Chỉ dời validator thì chưa đủ:** attribute vẫn chặn khởi động.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Giữ bắt buộc vô điều kiện, như hôm nay

**Được:** không đổi mã. A8 giữ dạng câu tuyệt đối.

**Mất:** dự án thay bản cài phải tạo một thư mục ghi được mà không gì dùng. Người vận hành thấy khoá `RootPath` trong cấu hình sẽ tưởng tệp nằm ở đó. Khi đó việc sao lưu kho tệp cùng DB ([`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md) §8) sao lưu một thư mục rỗng, và không ai thấy sai.

**Vì sao loại:** người dùng chọn miễn. Cái giá của A trả ở mọi dự án thay bản cài, mãi mãi. Cái giá của phương án đã chọn trả một lần, ở Core.

### Phương án B — Miễn bằng một cờ cấu hình

Ví dụ một khoá bật/tắt bản lưu cục bộ: tắt thì bỏ qua phép kiểm `RootPath`.

**Được:** dễ đọc trong tệp cấu hình. Không phụ thuộc thứ tự đăng ký.

**Mất:** có hai nguồn sự thật cho cùng một câu hỏi — cờ và đăng ký DI. Cờ tắt mà dự án chưa đăng ký bản thay thì `LocalFileStorage` chạy không có `RootPath` hợp lệ. Lỗi lộ ra ở lần tải tệp đầu tiên, đúng thứ §4 của `be-architecture.md` tồn tại để chặn.

**Vì sao loại:** hỏng theo hướng im lặng.

### Phương án C — Validator tự hỏi container bản lưu đang là bản nào

Validator vẫn đăng ký vô điều kiện. Lúc kiểm, nó resolve `IFileStorage` và bỏ qua nếu kết quả không phải `LocalFileStorage`.

**Được:** đúng cả khi dự án đặt dòng đăng ký sau `AddCore`, cách đặt mà ADR-0106 đã loại.

**Mất:** validator phải nhận `IServiceProvider` và tự resolve một singleton trong lúc kiểm options (service locator). Nó kiểm lúc chạy một sự thật đã biết từ lúc đăng ký.

**Vì sao loại:** phức tạp hơn nhánh đăng ký mà chỉ thêm đúng một ca. Ca đó lại là cách đặt đã bị loại.

## Hệ quả

### Tích cực

- Dự án thay bản cài không khai khoá thừa. Không còn thư mục mồi nhử nào để người vận hành sao lưu nhầm.
- Luật A8 có câu trả lời cho ca trước đây nó không nói tới, kèm tiêu chí cho điều kiện hợp lệ.

### Tiêu cực

- **A8 mất dạng câu tuyệt đối.** Cổng A8 (📐) chỉ kiểm được *có đường* `ValidateOnStart`. Nó không kiểm được *điều kiện có đúng không*. Điều kiện chỉ có test hai chiều của nợ B23 canh, và test đó chỉ phủ `RootPath`. Một `Options` sau này tự miễn bằng điều kiện lỏng hơn chỉ bị review bắt.
- **Dự án đặt bản thay sau `AddCore` thì vẫn bị đòi `RootPath`.** Thông điệp nêu tên khoá chứ không nêu chỗ đặt sai. Tiến trình dừng hẳn nên lỗi không bị bỏ qua, nhưng thông điệp dẫn người đọc đi sai hướng.
- **Cả hai cách thi công đều có giá.** Tách lớp thì nhóm khoá `Core:File:*` nằm ở hai lớp. Giữ một lớp thì phép kiểm thiếu khoá phải viết tay trong validator thay cho một attribute.
- Tới khi B23 xong, tài liệu nói *"miễn"* mà mã vẫn đòi. Các chỗ đó mang dấu 🚧 Lệch.

### Rút lui nếu sai

Đăng ký lại phép kiểm `RootPath` vô điều kiện: trả attribute, hoặc bỏ điều kiện quanh validator — một chỗ trong mã. Không dữ liệu nào phụ thuộc. Dự án đã bỏ khoá thì tiến trình không lên và nêu tên khoá, tức hỏng ồn chứ không hỏng im. Dòng A8 về lại dạng tuyệt đối bằng một ADR mới.

### Dấu hiệu quyết định này bắt đầu sai

- Một `Options` thứ hai xin miễn theo câu A8 mới, và điều kiện của nó không phải nhánh đăng ký thành phần.
- Tiến trình khởi động với `LocalFileStorage` mà `RootPath` không bị kiểm. Khi đó điều kiện đã tách làm hai.

## Sáu câu hỏi trước khi chấp nhận

| # | Câu hỏi | Trả lời |
| --- | --- | --- |
| 1 | Vấn đề thật, đã đo chưa | Đọc được ở mã: hai phép kiểm vô điều kiện cho một khoá chỉ `LocalFileStorage` đọc. **Chưa có dự án nào** thay bản cài, nên chưa ai trả cái giá đó thật |
| 2 | Phương án đơn giản hơn, vì sao không đủ | Phương án A. Nó bắt mọi dự án thay bản cài giữ một thư mục mồi nhử |
| 3 | Chi phí vận hành thêm | Không. Dự án thay bản cài bớt một khoá |
| 4 | Ai bảo trì | `backend-expert` giữ nhánh đăng ký; `test-engineer` giữ test hai chiều |
| 5 | Rút lui thế nào | Đăng ký lại phép kiểm vô điều kiện, một chỗ — mục *Rút lui nếu sai* |
| 6 | Có buộc Core biết nghiệp vụ không | Không |

## Thi công và kiểm

| Ai | Việc |
| --- | --- |
| `backend-expert` | Đăng ký phép kiểm `RootPath` theo cùng điều kiện với `LocalFileStorage` — nợ B23, vòng 2 của đợt code, cùng lượt đổi `TryAddSingleton` của ADR-0106 |
| `test-engineer` | Hai test, cả hai đều thiếu `RootPath`. Không đăng ký gì: tiến trình không khởi động, thông điệp nêu tên khoá. Có một `IFileStorage` giả đăng ký trước `AddCore`: khởi động được. Canary: đưa phép kiểm ra ngoài điều kiện làm test thứ hai đỏ; gỡ phép kiểm làm test thứ nhất đỏ |

## Liên quan

- [`0106-ifilestorage-la-seam-du-an-thay-duoc.md`](0106-ifilestorage-la-seam-du-an-thay-duoc.md) — ADR này trả lời câu hỏi mở của 0106; mọi phần khác của 0106 giữ nguyên hiệu lực
- [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §4.3
- [`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md) §1, §2
