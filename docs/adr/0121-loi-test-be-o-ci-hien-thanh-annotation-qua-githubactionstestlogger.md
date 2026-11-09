---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0121 — Lỗi test BE ở CI hiện thành annotation của GitHub qua gói `GitHubActionsTestLogger`

> **Trạng thái:** Đã chấp nhận (2026-10-06)
>
> Người dùng chốt ngày 2026-10-06 (đợt rà đồng bộ, câu Q31). Kiến trúc sư chốt phiên bản và phạm vi. Không đổi trạng thái ADR nào.

## Bối cảnh

Job `backend-gate` đã đỏ chín ngày liền mà không ai biết test nào đỏ. Lý do: kết quả `dotnet test` chỉ nằm trong log của job, mà GitHub chỉ cho xem log khi đã đăng nhập — người dùng không mở, còn agent trong repo này đọc GitHub qua API công khai, không đăng nhập. Annotation của một lượt chạy thì đọc được qua API công khai (`check-runs/<id>/annotations`) và hiện ngay ở trang tóm tắt của lượt chạy.

Hiện trạng ngày 2026-10-06, đã mở ra đối chiếu:

| Chỗ | Nói gì | Neo |
| --- | --- | --- |
| Hai bước test | `dotnet test` chạy hai lần — nhóm không cần Docker, rồi nhóm `RequiresDocker` — không có `--logger` nào | `.github/workflows/docs-gate.yml`, chuỗi `--filter "Category!=RequiresDocker"` và chuỗi `--filter "Category=RequiresDocker" \| tee` |
| Chốt chống xanh rỗng | Bước test DB đọc dòng tổng kết của logger console (`Passed!`/`Failed!` kèm `Total:`) | `.github/workflows/docs-gate.yml`, chuỗi `grep -qE '(Passed\|Failed)!.*Total: *[1-9]'` |
| Bộ chạy test | xUnit 2 qua VSTest | `src/BE/Directory.Packages.props`, chuỗi `<PackageVersion Include="xunit" Version="2.9.3" />` |

Gói tra trên nuget.org ngày 2026-10-06: `GitHubActionsTestLogger` bản mới nhất `3.0.5` (phát hành 2026-07-25), tác giả Oleksii Holub (Tyrrrz), giấy phép MIT, có đích `net10.0`, không phụ thuộc gói nào. Với VSTest, bật bằng `--logger GitHubActions`; gói ghi annotation cho từng test đỏ và một bản tóm tắt vào trang của lượt chạy.

## Quyết định

Người dùng chọn thêm gói. Kiến trúc sư chốt:

1. **`GitHubActionsTestLogger` `3.0.5`**, ghim chính xác ở `src/BE/Directory.Packages.props` trong nhóm *Test*. **Mọi** project test BE tham chiếu nó, kèm `PrivateAssets="all"` — kể cả project test của module sau này: VSTest không tìm thấy logger trong một assembly test thì lượt chạy của project đó hỏng. Khai ở một chỗ chung cho mọi project test thì project mới không quên được; khai từng `.csproj` thì người thêm project phải nhớ.
2. **Cả hai bước `dotnet test` của `backend-gate` thêm `--logger GitHubActions`.** Logger console giữ nguyên, nên chốt chống xanh rỗng của bước test DB vẫn đọc được dòng tổng kết — người sửa workflow chạy thử và xác nhận điều đó, không giả định.
3. **Chỉ phía BE.** Test FE (`ng test`) không thuộc quyết định này.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Tải tệp kết quả TRX lên làm artifact

**Được:** không thêm gói nào; giữ kết quả đầy đủ.
**Mất:** tải artifact cũng phải đăng nhập — đúng rào cản đang có.
**Vì sao loại:** không giải được vấn đề.

### Phương án B — Không làm gì, người dùng tự đọc log

**Được:** không đổi gì.
**Mất:** lặp lại chín ngày đỏ không ai biết; agent không chẩn đoán được CI.
**Vì sao loại:** vấn đề đã quan sát thật, không phải dự đoán.

### Phương án C — Tự viết bước workflow đọc TRX rồi in `::error::`

**Được:** không phụ thuộc bên thứ ba.
**Mất:** một đoạn script phân tích XML phải bảo trì, phải có canary riêng để không xanh rỗng — đúng việc gói đã làm và đã có người bảo trì.
**Vì sao loại:** chi phí viết và nuôi cao hơn một dòng `PackageVersion`, cho cùng kết quả.

## Hệ quả

### Tích cực

- Test đỏ ở CI hiện tên và thông điệp ngay ở trang tóm tắt, đọc được qua API công khai — người dùng và agent đều thấy mà không cần đăng nhập.
- Không đổi cách chạy test ở máy dev: không có `--logger GitHubActions` thì gói không làm gì.

### Tiêu cực

- **Thêm một phụ thuộc bên thứ ba do một người bảo trì.** Ghim chính xác thì tụt bản dần; nâng là việc tay. Gói ngừng phát hành cho bản .NET mới thì CI mất annotation, không mất kết quả test.
- **Annotation phơi thông điệp lỗi của test ra trang công khai** — trang đó vốn đã công khai với repo công khai, nhưng thông điệp khẳng định có thể in giá trị dữ liệu test. Test đã dùng giá trị giả ([`../../.gitleaks.toml`](../../.gitleaks.toml) và `.gitleaksignore` liệt kê chuỗi giả trong test); giá trị thật không được xuất hiện trong khẳng định.
- **Mỗi project test mới phải mang gói** — quên thì lượt chạy của chính project đó hỏng trên CI, với thông báo về logger chứ không về test. Khai ở một chỗ chung (quyết định 1) chặn ca này.
- **Lỗi test FE vẫn chỉ nằm trong log** — quyết định này không phủ `ng test`.

### Rút lui nếu sai

Bỏ dòng `PackageVersion`, các tham chiếu, và hai cờ `--logger GitHubActions` — một lượt, không sinh dữ liệu nào. Bản tóm tắt cũ trên các lượt chạy đã qua vẫn còn trên GitHub tới khi lượt chạy hết hạn lưu.

### Dấu hiệu quyết định này bắt đầu sai

- Gói không theo kịp bản .NET hay bản VSTest repo dùng — annotation vắng trong lượt chạy có test đỏ.
- Repo chuyển sang Microsoft.Testing.Platform hay xUnit 3 — cách bật đổi (`--report-github`), xét lại cấu hình.

## Liên quan

- Workflow: [`../../.github/workflows/docs-gate.yml`](../../.github/workflows/docs-gate.yml), job `backend-gate`
- Hai bước test và vì sao tách: [`../wiki-core/be/04-testing-strategy.md`](../wiki-core/be/04-testing-strategy.md) §4.2
- Công cụ test BE: [`../wiki-core/be/04-testing-strategy.md`](../wiki-core/be/04-testing-strategy.md) §7
