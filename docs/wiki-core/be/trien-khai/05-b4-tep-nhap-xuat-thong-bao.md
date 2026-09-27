---
kind: luat
scope: core
verified: chua-doi-chieu
---

# B4 — Tệp, nhập/xuất, thông báo

> 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG** (đối chiếu 2026-09-21, **chỉ các dòng dưới đây**; `verified:` giữ `chua-doi-chieu`). Mọi đường dẫn tính từ `src/BE/`.
>
> | Có thật hôm nay | Sẽ thành |
> | --- | --- |
> | Tệp: `Core/CoreAndSkill.Core.Web/Controllers/FilesController.cs` (`Download`), dọn tệp ở `Core/CoreAndSkill.Core.Infrastructure/Files/FileMaintenanceHostedService.cs` (`RunOnceAsync`) | Chiều DB (`FilesDatabaseTests`, `RequiresDocker`) chạy xanh ở CI |
> | Nhập: `Core/CoreAndSkill.Core.Application/Import/ImportJobExecutor.cs` (`SaveFailureFileAsync`); xuất và thông báo đi qua `Tests/CoreAndSkill.Core.IntegrationTests/Web/B4ExportEndpointTests.cs`, `B4JobsAndNotificationsEndpointTests.cs` | Như trên, cho `ImportJobDatabaseTests`, `ExportUsersDatabaseTests`, `NotificationsDatabaseTests` |
> | Outbox: `Tests/CoreAndSkill.Core.IntegrationTests/Outbox/OutboxDatabaseTests.cs` (`Commit_WritesTheBusinessRowAndExactlyOneOutboxRow_Together`) — **chưa chạy** | Chạy xanh ở CI có Docker |
>
> Mục §4 **không** tick ở đây: mỗi mục ghi test chạy được hôm nay và test còn chờ Docker.
>
> **Định nghĩa hoàn thành:** đính kèm một tệp vào một bản ghi rồi tải lại được, và người không có quyền thì không tải được; xuất một danh sách theo đúng bộ lọc đang xem; một sự kiện nghiệp vụ sinh ra một thông báo tới đúng người nhận, qua Outbox.

---

## 1. Dựng gì ở pha này

| Thứ | File chủ | Hợp đồng |
| --- | --- | --- |
| Lưu trữ tệp, vòng đời tệp, phục vụ có kiểm quyền | [`../14-file-storage.md`](../14-file-storage.md) | [`../../../contracts/files.md`](../../../contracts/files.md) |
| Nhập và xuất dữ liệu | [`../15-import-export.md`](../15-import-export.md) | [`../../../contracts/exports.md`](../../../contracts/exports.md) |
| Việc chạy nền — bảng `core.job`, theo dõi qua `jobId` | [`../../../database/schema-core.md`](../../../database/schema-core.md) §9.8 | [`../../../contracts/jobs.md`](../../../contracts/jobs.md) |
| Sự kiện, Outbox, thông báo trong ứng dụng và email | [`../12-notifications.md`](../12-notifications.md) | [`../../../contracts/notifications.md`](../../../contracts/notifications.md) |
| Bảo mật khi nhận tệp | [`../09-security-beyond-auth.md`](../09-security-beyond-auth.md) §9 | |

---

## 2. Thứ tự viết

1. **Lưu trữ tệp** trước, vì xuất dữ liệu tạo ra tệp và thông báo có thể đính kèm tệp.
2. **Outbox và tiến trình phát**, kiểm chứng bằng một sự kiện giả trước khi có kênh thật.
3. **Thông báo trong ứng dụng**, rồi mới tới kênh email — kênh trong ứng dụng không phụ thuộc hạ tầng ngoài nên hỏng ở đâu thấy ngay ở đó.
4. **Xuất dữ liệu** theo bộ lọc đang xem, dùng chung chỗ dựng truy vấn với màn danh sách.
5. **Nhập dữ liệu**, phần khó nhất: báo lỗi theo từng dòng và huỷ thay đổi đang theo dõi sau mỗi dòng lỗi.

---

## 3. Ba thứ hay bị làm sai ở B4

| Sai | Hậu quả |
| --- | --- |
| **Phục vụ tệp bằng đường tĩnh** | Ai có đường dẫn là tải được, phân quyền vô nghĩa — [`../14-file-storage.md`](../14-file-storage.md) |
| **Ghi Outbox ở một giao dịch khác với thay đổi nghiệp vụ** | Hai thứ không còn cùng số phận, và đó là toàn bộ lý do Outbox tồn tại — [`../12-notifications.md`](../12-notifications.md) §2.1 |
| **Lưu câu thông báo đã ghép sẵn** | Đổi ngôn ngữ không đổi được thông báo cũ, sửa lỗi chính tả không sửa được cái đã gửi — [`../12-notifications.md`](../12-notifications.md) §5 |

---

## 4. Nghiệm thu B4

Ghi chú dưới mỗi mục: **chạy được** = test chạy không cần Docker; **chờ Docker** = test `RequiresDocker`, chưa chạy lần nào.

- [ ] Tải một tệp lên, tải lại được; tài khoản không có quyền → **không** tải được, kể cả khi có đường dẫn.
  Chạy được: `Download_WhenAttached_FollowsThePermissionOfTheOwnerRecord`, `Download_IsNeverServedByAStaticPath_TheStorageKeyUnderTheHostRootIsNotReachable`. Chờ Docker: `AnUnattachedFile_IsReadableByItsUploaderOnly`.
- [ ] Tệp vượt giới hạn dung lượng hoặc sai kiểu → bị từ chối kèm mã lỗi trong card.
  Chạy được: `Upload_FarOverTheLimit_IsRefusedByContentLength_BeforeTheBodyIsRead_WithTooLarge`, `Upload_AnExecutableRenamedToPdf_IsRejected`.
- [ ] Xuất một danh sách đang lọc → tệp chứa **đúng** tập dòng của bộ lọc đó, và có dòng nhật ký kiểm toán.
  Chạy được (kho giả): `Export_WritesOneAuditRow_WithFilterAndRowCount_AndNoPersonalData`. Chờ Docker: `TheExportedSet_IsExactlyWhatTheListShows_InTheSameOrder`.
- [ ] Nhập một tệp có một dòng sai ở giữa → request trả `jobId`; `GET /api/v1/core/jobs/{id}` báo đúng số dòng, các dòng hợp lệ khác không bị dở dang.
  Chờ Docker: `MiddleErrorRow_IsNotInTheDatabase_AndTheRowsAroundItAre`.
- [ ] Tệp vượt `Core:Import:MaxRows` → 422 ngay, không có bản ghi `core.job` nào được tạo.
  Chạy được: `Handle_MoreRowsThanTheCap_Is422_WithActualAndLimit_AndCreatesNothing`. Chờ Docker: `MoreRowsThanTheCap_IsRefusedWith422Semantics_AndNoJobNoOutboxRowNoTempFile`.
- [ ] Một dòng outbox `dead` → `/health/ready` trả `Degraded`; `core outbox-replay --id` đưa nó về `pending` và có dòng nhật ký kiểm toán.
  Chạy được: `OneDeadRow_IsDegraded_NoMatterHowFresh`, `ValidId_ReplaysExactlyThatRow_AndSucceeds`. Chờ Docker: `Replay_ResetsADeadRowToPending_AndWritesOneAuditRow`.
- [ ] Một sự kiện nghiệp vụ → có bản ghi Outbox trong **cùng** giao dịch; tắt tiến trình phát thì bản ghi vẫn nằm đó chờ.
  Chạy được: `Stage_TurnsARecordedEventIntoOneOutboxRow_InTheSameChangeSet`. Chờ Docker: `Commit_WritesTheBusinessRowAndExactlyOneOutboxRow_Together`, `Rollback_WritesNeitherTheBusinessRowNorTheOutboxRow`.
- [ ] Thông báo hiện đúng ngôn ngữ của người nhận, không phải ngôn ngữ của người gây ra sự kiện.
  Chạy được: `Email_UsesTheRecipientsPreferredLanguage_NotTheInitiatorsAndNotTheRequests`. Chờ Docker: `Email_UsesTheRecipientsPreferredLanguage_NotThePublishersOrTheDefault`.

---

## 5. Sau B4

Core đã đủ cho một module nghiệp vụ đầu tiên. Module mẫu dựng ở dự án hạ nguồn đầu tiên, không ở repo Core — [`00-lo-trinh-tong-the.md`](00-lo-trinh-tong-the.md) §5. Thủ tục thêm thành phần vào Core: [`../01-core-components.md`](../01-core-components.md) §6.
