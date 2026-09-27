---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0106 — `IFileStorage` là seam dự án thay được: dự án đăng ký bản cài của mình trước `AddCore`, Core đăng ký bản đĩa cục bộ bằng `TryAddSingleton`

> **Trạng thái:** Đã chấp nhận (2026-09-25) · Bổ sung bởi ADR-0107 (2026-09-25) — [`0107-rootpath-chi-kiem-khi-dung-ban-luu-cua-core.md`](0107-rootpath-chi-kiem-khi-dung-ban-luu-cua-core.md)

## Bối cảnh

Tài liệu nói lớp trừu tượng `IFileStorage` tồn tại để đổi được nơi lưu:

- [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §1.1: *"đổi nơi lưu là đổi **một** bản cài"*.
- [`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md) §8: đĩa cục bộ không dùng chung được giữa hai instance, và đó là *"lý do phổ biến nhất buộc phải đổi nơi lưu"*.

Không chỗ nào nói **ai** đổi, và đổi **bằng cách nào**. Theo [`0016-phan-phoi-core-bang-clone.md`](0016-phan-phoi-core-bang-clone.md), dự án hạ nguồn không sửa vùng Core. Vậy dự án chỉ đổi được bằng cách đăng ký bản cài của mình.

Kiến trúc sư đối chiếu mã ngày 2026-09-25:

- Core đăng ký bằng `AddSingleton`: `src/BE/Core/CoreAndSkill.Core.Infrastructure/DependencyInjection/CoreInfrastructureServiceCollectionExtensions.cs`, chuỗi `services.AddSingleton<IFileStorage, LocalFileStorage>();`.
  - Dự án đăng ký **trước** `AddCore` — đúng thứ tự dòng của module ở [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §3 — thì bản của Core đăng ký sau và thắng, **im lặng**.
  - Dự án đăng ký **sau** `AddCore` thì bản của dự án thắng, nhưng chỉ nhờ quy tắc *"đăng ký sau thắng"*, và trong container có hai bản.
- Hai seam khác mà dự án được thay đã dùng khuôn đúng: `INotificationPreferences` và `INotificationTemplateRenderer` đăng ký bằng `TryAddSingleton` (`src/BE/Core/CoreAndSkill.Core.Application/DependencyInjection.cs`, chuỗi `services.TryAddSingleton<INotificationPreferences,`).
- `Core:File:RootPath` bắt buộc **vô điều kiện**: `[Required]` trên `CoreFileOptions.RootPath`, cộng `CoreFileOptionsValidator` kiểm thư mục có thật, ghi được, nằm ngoài thư mục ứng dụng. Chỉ `LocalFileStorage` đọc khoá đó.
- Mọi chỗ dùng `IFileStorage` đều nhận nó qua DI hoặc qua một DI scope, không chỗ nào `new` bản cài.

## Quyết định

Người dùng chốt ngày 2026-09-25, phương án (a):

1. **`IFileStorage` là seam dự án thay được.** Dự án muốn nơi lưu khác thì hiện thực đủ interface — kể cả nhóm kho tạm và `ListAsync` mà việc bảo trì kho tệp dùng — rồi đăng ký bằng `AddSingleton` **trước** `AddCore`, cùng chỗ với dòng của module. Vòng đời `Singleton` theo [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §5.2.
2. **Core đăng ký bản đĩa cục bộ bằng `TryAddSingleton`**, nên nhường bản của dự án. Việc đổi mã thuộc `backend-expert`, ở đợt code — nợ **B23**.
3. Khuôn này giống hai seam thông báo ở trên. Seam nào khác trở thành *"dự án thay được"* thì phải có ADR riêng. ADR này không mở rộng khuôn cho seam nào khác.

## Câu hỏi còn mở — chờ người dùng

### `Core:File:RootPath` có còn bắt buộc khi dự án đã thay bản cài?

Hôm nay, dự án thay bản cài **vẫn phải** khai một `Core:File:RootPath` hợp lệ: thư mục có thật, ghi được, ngoài thư mục ứng dụng. Nếu không, tiến trình không khởi động, dù không gì đọc khoá đó.

| | Giữ bắt buộc như hôm nay | Chỉ bắt buộc khi bản cài hiệu lực là bản đĩa cục bộ của Core (kiến trúc sư khuyến nghị) |
| --- | --- | --- |
| Mã | Không đổi | Phép kiểm `RootPath` — cả `[Required]` lẫn validator — chỉ đăng ký cùng `LocalFileStorage`. `AddCore` thấy `IFileStorage` đã có thì bỏ qua cả hai |
| Dự án thay bản cài | Phải tạo một thư mục không dùng tới, chỉ để qua phép kiểm | Không phải khai gì thừa |
| Rủi ro | Người vận hành tưởng thư mục đó đang chứa tệp | Luật A8 (*mọi `Options` bắt buộc có `ValidateOnStart`*) cần hiểu là *bắt buộc khi thành phần dùng nó có mặt*. Cổng A8 phải biết ngoại lệ đó |

Tài liệu ghi đúng hiện trạng: **giữ bắt buộc**, cho tới khi có câu trả lời.

## Phương án đã cân nhắc và vì sao loại

### Phương án B — Chỉ Core đổi bản cài; dự án không thay

**Được:** không có seam dự án thay được; một đường duy nhất.

**Mất:** dự án cần nhiều instance thì phải chờ Core có bản cài mới, hoặc sửa vùng Core, trái ADR-0016. Nhu cầu đổi nơi lưu là của từng sản phẩm, không phải của Core.

**Vì sao loại:** người dùng chọn (a).

### Phương án C — Giữ `AddSingleton`, tài liệu dạy dự án đăng ký **sau** `AddCore`

**Được:** không đổi mã.

**Mất:**

- Hai bản trong container. Mọi chỗ nhận `IEnumerable<IFileStorage>`, nếu có ngày xuất hiện, sẽ thấy cả hai.
- Thứ tự ngược với dòng của module (trước `AddCore`), nên `Program.cs` có hai quy tắc thứ tự.
- Đặt nhầm chỗ thì bản của dự án bị đè im lặng.

**Vì sao loại:** cái đúng dựa vào một quy tắc ngầm của container, và cái sai không báo gì.

## Hệ quả

### Tích cực

- Dự án đổi nơi lưu mà không chạm vùng Core.
- Cùng khuôn với hai seam thông báo, nên người viết `Program.cs` chỉ nhớ một quy tắc: bản thay của dự án đứng trước `AddCore`.

### Tiêu cực

- **Bản cài của dự án phải đúng cả những hợp đồng không nằm trong chữ ký.** Khoá do hệ thống sinh, kho tạm có hạn dùng, `OpenAsync` trả `Result` cho *"không có tệp"* ([`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md) §1). Chưa có bộ test hợp đồng dùng chung nào để dự án chạy cho bản cài của mình — cùng khoảng trống với B18.
- **Đến khi mã đổi xong, khuôn đã chốt chưa chạy được.** Dự án làm đúng tài liệu — đăng ký trước — thì bị đè im lặng. Tài liệu ghi 🚧 cho tới khi B23 xong.
- **`RootPath` vẫn bắt buộc** cho tới khi câu hỏi mở được trả lời.

### Rút lui nếu sai

Đổi về `AddSingleton` và ghi lại rằng dự án không thay. Không dữ liệu nào phụ thuộc cách đăng ký. Tệp đã lưu bằng bản cài của dự án thì thuộc về dự án đó.

### Dấu hiệu quyết định này bắt đầu sai

- Nhiều seam khác được xin *"cho dự án thay"* theo cùng lối, mỗi cái không có ADR.
- Một bản cài của dự án làm hỏng việc bảo trì kho tệp. Đó là dấu hiệu thiếu bộ test hợp đồng.

## Sáu câu hỏi trước khi chấp nhận

| # | Câu hỏi | Trả lời |
| --- | --- | --- |
| 1 | Vấn đề thật, đã đo chưa | Lệch giữa tài liệu (*"đổi một bản cài"*) và mã (`AddSingleton`), đọc được bằng mắt. **Chưa có dự án nào** thay bản cài |
| 2 | Phương án đơn giản hơn, vì sao không đủ | Phương án C, không đổi mã, đơn giản hơn. Nó dựa vào quy tắc ngầm của container và sai thì không báo |
| 3 | Chi phí vận hành thêm | Không |
| 4 | Ai bảo trì | Core giữ `TryAddSingleton` và phép kiểm; dự án giữ bản cài của mình |
| 5 | Rút lui thế nào | Đổi lại một dòng đăng ký |
| 6 | Có buộc Core biết nghiệp vụ không | Không |

## Thi công và kiểm

| Ai | Việc |
| --- | --- |
| `backend-expert` | Đổi đăng ký `IFileStorage` sang `TryAddSingleton` — nợ B23 |
| `test-engineer` | Test: host đăng ký một `IFileStorage` giả **trước** `AddCore` thì mọi chỗ resolve ra bản giả; không đăng ký gì thì ra `LocalFileStorage`. Canary: đổi dòng đăng ký của Core về `AddSingleton` phải làm test đỏ |
| `backend-expert` | Sau khi người dùng trả lời câu hỏi mở, nếu chọn khuyến nghị: dời phép kiểm `RootPath` vào nhánh đăng ký `LocalFileStorage` |

## Liên quan

- [`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md) §1, §8
- [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §1.1, §5.2
- [`0016-phan-phoi-core-bang-clone.md`](0016-phan-phoi-core-bang-clone.md)
- [`0014-mot-instance-key-ring-postgres.md`](0014-mot-instance-key-ring-postgres.md) — một instance ở v1
