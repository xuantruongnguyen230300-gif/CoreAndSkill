---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 18. Triển khai và vận hành backend

> 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Code của chủ đề này đã có một phần dưới `src/BE`, nhưng **chưa mục nào trong tệp được đối chiếu** với nó — tệp vẫn trong tầm chấm review (2026-09-24).
>
> **Máy chủ chạy Linux (chốt 2026-09-10); cách chạy, nguồn bí mật và cơ chế job nền chốt ở §7 (2026-09-15).** File này khai **nguyên tắc trung lập nền tảng** ở §1–§6 — đúng dù chạy trên máy chủ riêng, container hay dịch vụ lưu trữ; phần cụ thể chỉ nằm ở §7.
>
> Phía FE: [`../fe/17-phuc-vu-va-trien-khai.md`](../fe/17-phuc-vu-va-trien-khai.md).

---

## 1. Một artifact cho mọi môi trường

Build **một lần**, triển khai cùng một artifact lên mọi môi trường. Khác biệt giữa các môi trường chỉ nằm ở **cấu hình đọc lúc chạy**, không nằm trong bản build.

Build riêng cho từng môi trường nghĩa là thứ đã thử ở môi trường thử **không phải** thứ chạy ở môi trường thật.

## 2. Bí mật và cấu hình theo môi trường

| Luật | Vì sao |
| --- | --- |
| Bí mật **dạng rõ** **không** nằm trong repo, kể cả tệp cấu hình theo môi trường — ngoại lệ có tên duy nhất chỉ dành cho DB dev chung, không cho môi trường máy chủ nào | [`../../quy-uoc/repo-artifact.md`](../../quy-uoc/repo-artifact.md) §6, §6.4 |
| Ở môi trường thật, bí mật đến từ **nguồn ngoài cây làm việc** do nền tảng cấp — biến môi trường hoặc kho bí mật | Đổi bí mật không cần build lại, và artifact rò ra ngoài không mang bí mật theo |
| `appsettings.Development.json` **không** đi vào artifact publish | Tệp đó mang khoá `dev` và bản mã mật khẩu DB dev ([`../../adr/0098-giai-ma-o-buoc-dung-options-mot-db-dev-chung-khoa-dev-trong-git.md`](../../adr/0098-giai-ma-o-buoc-dung-options-mot-db-dev-chung-khoa-dev-trong-git.md)). Không môi trường máy chủ nào dùng khoá `dev` (luật S25) |
| Thứ tự ưu tiên của framework: tệp mặc định → tệp theo môi trường → **biến môi trường thắng** | Người vận hành ghi đè được mọi giá trị mà không sửa tệp đã đóng gói |
| Thiếu một giá trị bắt buộc thì app **không khởi động** — ở **mọi** môi trường, kể cả máy dev | [`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md) §4 — hỏng lúc triển khai, không hỏng lúc người dùng chạm tới |

Danh sách giá trị bắt buộc **không** chép vào đây — nó là tập các lớp cấu hình có kiểm lúc khởi động, đọc bằng lệnh khi có `src/`:

```bash
grep -rn 'ValidateOnStart' src/BE --include='*.cs'
```

## 3. Proxy đứng trước API

Có proxy thì khai danh sách proxy tin cậy; không có thì để trống. Vị trí bước này và hai cách hỏng: [`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md) §3.1, ràng buộc 4.

### Khoá cấu hình proxy tin cậy — định nghĩa gốc

| Khoá | Giá trị | Ví dụ |
| --- | --- | --- |
| `Core:Network:KnownProxies` | Mảng địa chỉ IP, mỗi proxy một mục | `["10.0.0.5"]` |
| `Core:Network:KnownNetworks` | Mảng dải mạng dạng CIDR | `["10.0.0.0/24"]` |

- **Hai khoá cùng rỗng hoặc vắng = không tin nguồn nào.** Header `X-Forwarded-For` / `X-Forwarded-Proto` bị bỏ qua hẳn. Đây là giá trị đúng khi app nhận kết nối trực tiếp.
- **Có ít nhất một mục** thì header chuyển tiếp chỉ được áp khi kết nối TCP đến từ đúng proxy hoặc dải đã khai. Danh sách loopback mặc định của framework **không** cộng thêm vào.
- **Mục sai dạng thì app không khởi động**, thông báo nêu tên khoá — cùng luật fail-fast ở [`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md) §4.
- Qua biến môi trường, chỉ số mảng đi sau hai dấu gạch dưới: `Core__Network__KnownProxies__0=10.0.0.5`.

> ⚠️ **Rỗng phải có nghĩa là tắt, và điều đó không tự có.** Middleware chuyển tiếp của framework chỉ đối chiếu nguồn khi ít nhất một trong hai danh sách có mục. Xoá danh sách mặc định mà vẫn bật cờ chuyển tiếp thì nó **tin mọi nguồn** — ai cũng tự đặt được IP để chọn phân vùng rate limit ([`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §6.3). Vì thế cờ chuyển tiếp chỉ bật khi đã khai ít nhất một mục.

## 4. Thứ tự triển khai một bản mới

1. **Áp script schema trước**, theo checklist ở [`../../database/script-runbook.md`](../../database/script-runbook.md) §7 — app không bao giờ tự áp ([`../../adr/0009-ap-schema-chay-tay.md`](../../adr/0009-ap-schema-chay-tay.md)).
2. Script tuân **mở rộng trước, thu hẹp sau** ([`13-core-data-migration.md`](13-core-data-migration.md) §4) — nên **bản app cũ vẫn chạy được trên schema mới**. Đây là điều kiện để bước 5 tồn tại.
3. Triển khai artifact mới. Readiness **đỏ** khi còn migration chưa áp — [`07-observability.md`](07-observability.md) §8.
4. Kiểm sau triển khai: readiness xanh **và một request ghi thật đi qua**. Health xanh không chứng minh cookie và antiforgery chạy — proxy khai sai vẫn cho health xanh trong khi không request ghi nào qua được.
5. **Quay lui = triển khai lại artifact trước, không đụng DB** — làm được nhờ bước 2.

**Thu hẹp schema** (xoá cột, xoá bảng cũ) là **một lần triển khai riêng**, chỉ làm sau khi bản mới đã ổn định. Gộp vào cùng lần mở rộng thì bước 5 mất: bản app cũ không còn chạy được trên schema đã thu hẹp. Quay lui ở mức schema: [`13-core-data-migration.md`](13-core-data-migration.md) §6.

v1 chạy **một instance** nên mỗi lần triển khai có gián đoạn ngắn — đã chấp nhận ở [`../../adr/0014-mot-instance-key-ring-postgres.md`](../../adr/0014-mot-instance-key-ring-postgres.md).

## 5. Sao lưu

[`10-data-retention.md`](10-data-retention.md) §7 — kèm §7.1 về bảng khoá bảo vệ dữ liệu, thứ do chính Core sinh ra.

## 6. Khi có sự cố — truy một lỗi người dùng báo

| Bước | Làm gì |
| --- | --- |
| 1 | Lấy **mã lần gọi** người dùng thấy trên màn lỗi — [`07-observability.md`](07-observability.md) §3.2 |
| 2 | Tìm log theo mã đó: mọi dòng của request đều mang nó, kể cả dòng do thư viện ghi |
| 3 | Đọc `code` trong envelope. **4xx mang mã nghiệp vụ** thường không phải sự cố — là một luật đã chặn đúng. **5xx** mới là sự cố: log có exception |
| 4 | Lỗi ở việc chạy nền, **khi Outbox đã bật**: bản ghi outbox mang mã lần gọi của request đã sinh ra nó — nối được về nguyên nhân |
| 5 | Sửa dữ liệu ở môi trường thật **chỉ** qua script theo checklist [`../../database/script-runbook.md`](../../database/script-runbook.md) §7 — không bao giờ sửa tay |
| 6 | Xong thì ghi postmortem ở [`../../audit/`](../../audit/) theo khuôn bốn phần |

## 7. §Áp dụng

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Một artifact cho mọi môi trường | ✅ sẽ có | §1 |
| Bí mật ngoài repo, không khởi động khi thiếu | ✅ sẽ có | §2 |
| Triển khai theo thứ tự schema → app, quay lui không đụng DB | ✅ sẽ có | §4 |
| **Hệ điều hành máy chủ** | ✅ Linux — chốt 2026-09-10 | |
| **Cách chạy bản thật** | ✅ Docker Compose — chốt 2026-09-15 | Một máy chủ Linux, không cloud ở v1 |
| **Nguồn bí mật ở bản thật** | ✅ Biến môi trường do Compose cấp từ tệp `.env` **ngoài repo** — chốt 2026-09-15 | Cùng khoá cấu hình với `user-secrets` ở máy dev; không Vault ở v1. Tệp `.env` không commit ([`../../quy-uoc/repo-artifact.md`](../../quy-uoc/repo-artifact.md) §6) |
| **Cơ chế job nền** | ✅ `BackgroundService` của .NET, không thư viện — chốt 2026-09-15 | Hai hình dạng, ranh giới khai ở [`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md) §1.1: chạy một lần theo yêu cầu thì qua seam `IBackgroundJobScheduler`, lặp theo chu kỳ thì `BackgroundService` riêng — [`../../adr/0047-job-dinh-ky-khong-di-qua-seam-lap-lich.md`](../../adr/0047-job-dinh-ky-khong-di-qua-seam-lap-lich.md). Xem lại (Quartz.NET) chỉ khi cần lịch cron do người dùng cấu hình |
| **Kho tệp (`Core:File:RootPath`)** | ✅ Volume có tên `coreandskill-files` gắn vào thư mục tạo sẵn trong image — chốt 2026-09-21 | Khoá bắt buộc, không mặc định, không nằm ở `appsettings.json`; khai ở `docker-compose.yml` (`Core__File__RootPath`) chứ không qua `.env` vì không phải bí mật. Máy dev đặt bằng `dotnet user-secrets set "Core:File:RootPath" "<đường dẫn tuyệt đối, ngoài thư mục ứng dụng>"` — thư mục phải có sẵn và ghi được, nếu không tiến trình không lên. Sao lưu cùng DB, phục hồi về cùng mốc: [`14-file-storage.md`](14-file-storage.md) §8 |
| **Triển khai không gián đoạn** | ❌ chưa | Cần nhiều instance — điều kiện ở ADR-0014 |
