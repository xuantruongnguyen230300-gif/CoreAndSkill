---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0059 — Cổng FE, canary của nó, bảng dịch tầng Core và `index.html` vào khối `core-paths`

> **Trạng thái:** Đã chấp nhận (2026-09-22) · Bổ sung bởi [ADR-0100](0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md) (2026-09-25)

## Bối cảnh

Khối `core-paths` ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §10 trả lời câu hỏi *"thay đổi này có chạm Core không"*. Phía FE, khối liệt kê ba tầng `core/`, `shared/`, `platform/` và hai tệp ESLint. Lượt soát FE ngày 2026-09-22 nêu bốn đường dẫn nằm ngoài khối. Kiến trúc sư đối chiếu cùng ngày:

| Đường dẫn | Là gì | Không thuộc khối thì sao |
| --- | --- | --- |
| `scripts/fe-gate.sh` | Cổng script của mọi luật FE ở [`../RULES.md`](../RULES.md) §7 có trạng thái `✅`. `bash scripts/fe-gate.sh` xanh ngày 2026-09-22 | Nới một mẫu dò là tắt một luật Core, mà không lượt `core-reviewer` nào bị đòi |
| `scripts/tests/` | Canary của cổng trên, bốn tệp `fe-gate-f*.test.sh`, cả bốn thoát 0 ngày 2026-09-22. Job `frontend-gate` của CI chạy chúng ở bước 1b | Sửa canary cho khớp một cổng đã nới là cách làm cổng xanh mà không ai thấy |
| `src/FE/public/i18n/` | Bảng dịch tầng Core. Dự án ghi đè ở `public/i18n-app/` ([`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §1.1) | Câu chữ của mọi màn Core đổi mà không lượt soát nào |
| `src/FE/src/index.html` | Chứa script nội tuyến áp theme trước khung hình đầu ([`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) §3.5). Hash CSP của script phụ thuộc từng ký tự | Một cơ chế của Core nằm ngoài tầm canh |

`on-edit.sh` đọc khối này qua **hai** nhánh, và [`0044-database-scripts-core-vao-khoi-core-paths.md`](0044-database-scripts-core-vao-khoi-core-paths.md) đã đo hai nhánh đó. Đọc lại tệp ngày 2026-09-22:

- Nhánh lệnh shell dựng gốc quét từ chính khối, nên dùng được `scripts/` ngay.
- Nhánh Edit/Write lọc khu vực trước, bằng hàm `in_zone`. Hàm đó nhận `docs/`, `.claude/`, `spec/`, `src/` và `database/`, **không** nhận `scripts/`.

Hai đường dẫn dưới `src/FE/` không vướng lỗ này.

## Quyết định

Kiến trúc sư chốt:

1. Bốn đường dẫn trên vào khối `core-paths`. Hai đường dẫn dưới `src/FE/` vào nhóm Frontend. `scripts/fe-gate.sh` và `scripts/tests/` thành một nhóm riêng. Mỗi đường dẫn có một hàng trong bảng *Nguồn của từng nhóm*.
2. Lỗ của nhánh Edit/Write với `scripts/` ghi ngay dưới khối, kèm điều kiện đóng. Không dán nhãn "đã canh" cho một đường mà phép đo nói là chưa canh.
3. Việc thêm `scripts/` vào `in_zone` giao ra ngoài lượt này, cho người giữ `.claude/hooks/`. Điều kiện nghiệm thu: trong bộ test hook, một payload Edit nhắm vào `scripts/fe-gate.sh` phải sinh dấu chạm Core.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Chỉ thêm hai đường dẫn dưới `src/FE/`, để `scripts/` ngoài khối

**Được:** khối không khai rộng hơn thứ hook canh được.

**Vì sao loại:** [`0044-database-scripts-core-vao-khoi-core-paths.md`](0044-database-scripts-core-vao-khoi-core-paths.md) đã loại đúng lập luận này. Khối là định nghĩa *cái gì thuộc Core*, không phải bản kê *cái gì hook bắt được*. Cổng của luật Core cũng thuộc Core, như ArchTests phía BE đã thuộc khối.

### Phương án B — Thêm cả thư mục `scripts/`

**Vì sao loại:** `scripts/` là thư mục chung ở gốc repo. Dự án hạ nguồn sẽ đặt script tiện ích của mình ở đó, và những script ấy không phải cổng Core. Khai cả thư mục thì mỗi lần sửa một script như vậy đều bị đòi review Core mà không có lý do.

### Phương án C — Thêm luôn `src/FE/src/styles/` và cấu hình app

**Vì sao loại:** chưa chốt được chúng thuộc ai. Bảng màu của dự án đi qua tệp token toàn cục ([`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §2.5). Nếu tệp đó là Core thì dự án hạ nguồn không đổi được màu mà không sửa tệp Core, và điều đó đụng tới [`0016-phan-phoi-core-bang-clone.md`](0016-phan-phoi-core-bang-clone.md). Đây là một câu hỏi riêng, không gộp vào lượt này.

## Hệ quả

### Tích cực

- Sửa cổng FE, canary của nó, câu chữ tầng Core hay script theme bằng lệnh shell từ nay bị đòi `core-reviewer`, cùng mức với sửa `src/FE/src/app/core/`.
- Khối khớp với cách CI đối xử với `scripts/tests/`: một bước bắt buộc của job `frontend-gate`.

### Tiêu cực

- **Lỗ ở nhánh Edit/Write với `scripts/` còn mở**, và Edit là đường hay dùng nhất. Từ hôm nay cho tới khi `in_zone` được sửa, khối khai rộng hơn thứ hook thi hành được.
- Nhánh lệnh shell thêm hai gốc để `find` quét, cho mỗi lệnh shell.
- **`public/i18n/` vào khối thì mọi lần thêm câu chữ cho một màn Core đều đòi review Core.** Việc đó đúng, nhưng làm dài thêm vòng thi công của màn Core.
- `scripts/` và `src/` chưa vào git (đối chiếu ở [`../README.md`](../README.md) *Trạng thái repo*). Khối khai đường dẫn cho những tệp mà CI chưa từng thấy.

### Rút lui nếu sai

Gỡ dòng khỏi khối, gỡ hàng khỏi bảng *Nguồn của từng nhóm*, rồi chạy `bash .claude/hooks/tests/run-tests.sh`. Không có dữ liệu nào phải xử lý, vì dấu `core-touched` chỉ là trạng thái chạy.

## Liên quan

- [`0044-database-scripts-core-vao-khoi-core-paths.md`](0044-database-scripts-core-vao-khoi-core-paths.md): cùng khuôn, cùng lỗ ở nhánh Edit/Write.
- [`../wiki-core/fe/trien-khai/05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) §5: thứ tự lệnh cổng FE, gồm bước canary.
