---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0035 — Điều kiện 3 của ADR-0030 (hook review Core đã thử bằng payload thật) hoãn tới PR chạm Core đầu tiên của giai đoạn 2

> **Trạng thái:** Đã chấp nhận (2026-09-16) · Bổ sung [`0030-dieu-kien-chuyen-giai-doan-2.md`](0030-dieu-kien-chuyen-giai-doan-2.md)
>
> **Bổ sung 0030.** Bảy điều kiện còn lại giữ nguyên, đã có bằng chứng thật (xem Bối cảnh). ADR này chỉ thay cách kiểm của điều kiện 3, theo đúng cơ chế "Điều kiện lật quyết định" #2 mà chính 0030 đặt ra: bỏ một điều kiện thì ghi ADR mới, không lặng lẽ bỏ qua, không sửa bảng cũ.

## Bối cảnh

Ngày 2026-09-16, trước khi tạo `src/`, bảy trong tám điều kiện của [ADR-0030](0030-dieu-kien-chuyen-giai-doan-2.md) đã có bằng chứng thật:

| # | Điều kiện | Bằng chứng |
| --- | --- | --- |
| 1 | Quyết định đợt 2026-09-14 vào tài liệu | Ba lệnh kiểm của 0030 đều đúng: 10 ADR `Đã chấp nhận`, RULES đủ 13 dòng, không mã hướng khoá giữ chỗ |
| 2 | Lệnh cấm chặn đủ 3 đường | Thử trực tiếp `rtk git commit`, `git commit` sau `&&`, `git commit` qua công cụ PowerShell — **cả ba đều bị chặn**, đúng thông điệp của `on-pretool.sh` |
| 4 | CI chạy cổng + gitleaks | Push `8df1e3a` lên `origin/main` — job `gitleaks (S6)` **success**, job kiểm `src/BE`/`src/FE` **success**, hai job cổng BE/FE **skipped đúng** vì chưa có `src/BE`/`src/FE` (không phải `if: false`) |
| 5 | Mọi card hợp đồng đã gán pha | Lệnh kiểm của 0030 in rỗng |
| 6 | `auth.md`/`profile.md` = `AGREED` | `grep '^\*\*Status:\*\*'` — mọi card v1 mang `AGREED` |
| 7 | Cổng tài liệu xanh | `bash .claude/check-docs.sh` thoát `0` |
| 8 | Harness không hỏi lại lệnh dựng skeleton | Thêm `permissions.allow` cho `dotnet new/sln/add/restore`, `npx ng new/generate`; thử thật ở thư mục tạm **ngoài repo**: `dotnet new sln`, `dotnet sln add`, `dotnet add package`, `dotnet restore`, `npm install --save-dev @angular/cli` + `npx ng new --dry-run` — không lệnh nào bị hỏi lại |

**Điều kiện 3 không kiểm được theo đúng cách 0030 mô tả.** Hook `on-stop.sh` tự đọc `[ -d src ]`: chưa có `src/` thì nó **chủ động bỏ qua** toàn bộ bước chặn Core (`.claude/hooks/on-stop.sh` — nhánh *"Giai đoạn 1: không có code để core-reviewer đối chiếu. Không nhắc."*, xoá dấu `core-touched` rồi thoát). Đây là thiết kế **cố ý** của chính hook, không phải lỗi.

Muốn thử "hai nhánh" mà điều kiện 3 đòi (có dấu review → cho qua; không dấu → chặn) thì bắt buộc phải có ít nhất một file thật nằm dưới một đường dẫn trong khối `core-paths` — tức phải có `src/`. Nhưng theo chính bối cảnh của ADR-0030 (*"lệnh đầu tiên tạo ra `src/` là hành động lật giai đoạn trên thực tế"*), tạo file đó — dù chỉ để thử rồi xoá — **đã là hành động lật giai đoạn**, không phải một phép thử vô hại đứng ngoài giai đoạn 1.

## Quyết định

1. **Điều kiện 3 của ADR-0030 hoãn** — không bắt buộc kiểm trước khi tạo `src/` lần đầu.
2. **Điều kiện thay thế:** điều kiện 3 coi là đã đạt khi **PR đầu tiên chạm tới một đường dẫn Core thật** (theo khối `core-paths` ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md)) tự nhiên đi qua đúng hai nhánh mà 0030 mô tả — thiếu dấu `core-reviewed` mới hơn thì lượt bị chặn; có dấu mới hơn thì lượt qua — và người dùng xác nhận đã thấy đúng hai nhánh đó xảy ra ở PR đó.
3. **Cho tới lúc đó, lớp 3 (chặn cứng của hook `Stop`) là lưới an toàn CHƯA được chứng minh sống** — chỉ có lớp 1 (agent thi công tự báo `CẦN CORE-REVIEW`, [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §8) là cơ chế đã có bằng chứng vận hành thật (182 ca test hook mô phỏng, không phải payload thật trong phiên làm việc thật). Đây không phải điều kiện lỏng hơn — là bối cảnh phải nhớ khi đọc kết quả lượt chạm Core đầu tiên: nếu lớp 3 không chặn đúng như kỳ vọng, đó là lúc phát hiện, không phải trước đó.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Tạo một file giả dưới `src/` ngay bây giờ để thử, rồi xoá

**Được:** đóng điều kiện 3 ngay, đúng nghĩa đen của ADR-0030.

**Vì sao loại:** máy đọc `[ -d src ]`, không đọc "file này là giả hay thật". Tạo file — dù trống, dù xoá ngay sau — vẫn là **chính hành động** mà ADR-0030 gọi là lật giai đoạn trên thực tế, đúng lúc chưa đủ điều kiện còn lại để lật có chủ đích. Người dùng đã từ chối phương án này khi được hỏi trực tiếp ngày 2026-09-16.

### Phương án B — Dựng một repo Git riêng, tách biệt, chỉ để thử hook

**Được:** không đụng trạng thái `src/` của repo thật.

**Vì sao loại:** hook đọc đường dẫn tương đối từ gốc **repo đang chạy trong đó** (`cd "$(dirname "$0")/../.."`), và đọc khối `core-paths` từ chính `docs/kien-truc-core-module.md` của repo đó. Một repo thử riêng không có cùng `.claude/settings.json`, cùng khối `core-paths`, cùng lịch sử `.state` — kết quả thử ở đó không chứng minh được gì cho repo này.

### Phương án C — Bỏ hẳn điều kiện 3

**Vì sao loại:** điều kiện 3 bảo vệ đúng loại lỗi hỏng-im-lặng nghiêm trọng nhất trong toàn bộ thiết kế: `core-reviewer` bị bỏ qua mà không ai biết, vì "cổng tài liệu vẫn xanh". Bỏ hẳn là nới an toàn để đi nhanh hơn — đúng hành vi mà [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §4 cấm.

## Hệ quả

### Tiêu cực

| Cái giá | Nghĩa là |
| --- | --- |
| Lớp 3 chưa được chứng minh sống trước khi có code | PR chạm Core đầu tiên gánh thêm vai trò "phép thử hook", ngoài vai trò nghiệp vụ của chính nó |
| Nếu lớp 3 hỏng, phát hiện muộn hơn ý định ban đầu của ADR-0030 | Bù lại bằng lớp 1 (agent tự báo) và bằng việc người dùng biết trước để tự ý quan sát kỹ PR đó |

### Tích cực

- Không có `src/` giả, không có trạng thái nửa-vời trong lịch sử repo.
- Giai đoạn 2 bắt đầu bằng đúng công việc thật đầu tiên, không phải một thao tác dàn dựng.
- Người dùng biết trước, tường minh, rằng lớp 3 cần được quan sát kỹ ở đúng lượt đầu tiên chạm Core — không phải một giả định ngầm.

## Liên quan

- [`0030-dieu-kien-chuyen-giai-doan-2.md`](0030-dieu-kien-chuyen-giai-doan-2.md) — tám điều kiện gốc, mục *Điều kiện lật quyết định* #2 là cơ sở của ADR này.
