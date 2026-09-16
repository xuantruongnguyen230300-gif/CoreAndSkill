---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 18. Triển khai và vận hành backend

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`.
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
| Bí mật **không** nằm trong repo, kể cả tệp cấu hình theo môi trường | [`../../quy-uoc/repo-artifact.md`](../../quy-uoc/repo-artifact.md) §6 |
| Ở môi trường thật, bí mật đến từ **nguồn ngoài cây làm việc** do nền tảng cấp — biến môi trường hoặc kho bí mật | Đổi bí mật không cần build lại, và artifact rò ra ngoài không mang bí mật theo |
| Thứ tự ưu tiên của framework: tệp mặc định → tệp theo môi trường → **biến môi trường thắng** | Người vận hành ghi đè được mọi giá trị mà không sửa tệp đã đóng gói |
| Thiếu một giá trị bắt buộc thì app **không khởi động** — ở **mọi** môi trường, kể cả máy dev | [`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md) §4 — hỏng lúc triển khai, không hỏng lúc người dùng chạm tới |

Danh sách giá trị bắt buộc **không** chép vào đây — nó là tập các lớp cấu hình có kiểm lúc khởi động, đọc bằng lệnh khi có `src/`:

```bash
grep -rn 'ValidateOnStart' src/BE --include='*.cs'
```

## 3. Proxy đứng trước API

Có proxy thì khai danh sách proxy tin cậy; không có thì để trống. Vị trí bước này và hai cách hỏng: [`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md) §3.1, ràng buộc 4.

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
| **Cơ chế job nền** | ✅ `BackgroundService` của .NET, không thư viện — chốt 2026-09-15 | Sau seam `IBackgroundJobScheduler` ([`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md) §1.1). Xem lại (Quartz.NET) chỉ khi cần lịch cron do người dùng cấu hình |
| **Triển khai không gián đoạn** | ❌ chưa | Cần nhiều instance — điều kiện ở ADR-0014 |
