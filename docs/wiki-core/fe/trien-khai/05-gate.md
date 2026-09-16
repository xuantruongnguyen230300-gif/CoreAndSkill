---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Cổng Frontend — bảng đầy đủ

> 📐 **MỌI CỔNG TRONG FILE NÀY MANG NHÃN `ĐÍCH ĐẾN — CHƯA THI CÔNG`.**
>
> Giai đoạn 1 chưa có `src/`, chưa có `src/FE`, chưa có `scripts/fe-gate.sh`. **Không cổng FE nào đang chạy**, và không có ngoại lệ nào trong bảng dưới. Đây là trạng thái đúng theo thiết kế, không phải thiếu sót — xem [`../../../../.claude/CLAUDE.md`](../../../../.claude/CLAUDE.md) §4.
>
> **Mã luật trong file này là mã F của [`../../../RULES.md`](../../../RULES.md) — §7 cho luật đã có cổng, §10 cho luật còn nợ cổng**, không phải một hệ đánh số riêng. Hai hệ mã song song cho cùng một tập luật là nguồn lệch, và lệch ở tài liệu cổng thì không ai phát hiện được — vì bảng nào cũng "trông đúng" khi đọc một mình.

---

## 1. Vì sao FE cần cổng nhiều hơn BE

Ở BE, phần lớn luật kiến trúc được compiler ép: `Core.Domain` không tham chiếu `Core.Infrastructure` thì code trong Domain **không thể** gọi `DbContext`. Hàng rào đó chi phí bằng 0, không bao giờ hỏng, và không phụ thuộc ai nhớ luật.

FE không có cơ chế tương đương. Một thư mục là một thư mục; một import đi ngược tầng vẫn biên dịch bình thường. **Ranh giới FE chỉ tồn tại khi có người viết lint rule cho nó** — và chỉ **còn** tồn tại khi có cổng cấm gỡ lint rule đó bằng một dòng comment.

Đó là lý do bảng ở §3 dài hơn hẳn phần tương ứng của BE, và là lý do luật F3 tồn tại.

---

## 2. Hai loại cổng — phân biệt rõ ngay từ đầu

| Loại | Chạy bằng | Đặc điểm |
| --- | --- | --- |
| **Cổng script** | `bash scripts/fe-gate.sh` | Quét văn bản: tìm mẫu trong mã nguồn, đối chiếu tên file, so danh sách. Rẻ, nhanh, không cần cài gì ngoài shell |
| **Cổng công cụ** | `ng lint` · `ng build` | Dùng chính công cụ của framework. Hiểu được cấu trúc code, nhưng chậm hơn và cần môi trường build đầy đủ |

**Không gộp hai loại vào một lệnh.** Chạy riêng cho phép fail nhanh: cổng script mất vài giây và bắt phần lớn vi phạm thường gặp.

Thứ tự chạy đúng — **script → lint → test → build** — đi từ rẻ tới đắt.

---

## 3. Bảng cổng đầy đủ

> Cột "Trạng thái" là trạng thái **hôm nay**, giai đoạn 1.
>
> Cột "Ép bằng" trỏ tới **nơi giữ lệnh**. Lệnh nằm ở file quy ước chủ của luật thì script chạy đúng lệnh đó; lệnh nằm ở §8 của file này khi chưa file quy ước nào giữ nó. Trong tài liệu, mỗi lệnh chỉ có một bản.
>
> Cột "Bật ở pha" của hai bảng dưới là **nơi duy nhất** ghi pha bật của từng mã; lộ trình chỉ giữ lý do ([`00-lo-trinh-tong-the.md`](00-lo-trinh-tong-the.md) §3.3).

### 3.1 Cổng chạy bằng script

| Mã | Nội dung | Ép bằng | Bật ở pha | Trạng thái |
| --- | --- | --- | --- | --- |
| **F3** | `eslint-disable` bị cấm cho **danh sách quy tắc ranh giới** | `fe-gate.sh` — đọc danh sách từ bảng chủ lúc chạy, §8.1 | F0 | 📐 ĐÍCH ĐẾN — CHƯA THI CÔNG |
| **F4** | Mảng module của cấu hình ranh giới **khớp thư mục `modules/` thật** | `fe-gate.sh` — đọc `src/FE/eslint.boundaries.cjs`, §8.2 | F0 | 📐 ĐÍCH ĐẾN — CHƯA THI CÔNG |
| **F5** | Component nghiệp vụ không import trực tiếp thư viện UI — đi qua `shared/ui/` | `fe-gate.sh` — allowlist theo **đường dẫn**, đọc từ bảng chủ lúc chạy, §8.3 | F1 | 📐 ĐÍCH ĐẾN — CHƯA THI CÔNG |
| **F6** | Không hex color literal trong SCSS — trừ đúng tệp khai token | `fe-gate.sh` — §8.4 | F1 | 📐 ĐÍCH ĐẾN — CHƯA THI CÔNG |
| **F7** | Không `rgb()`/`rgba()` literal trong SCSS — trừ dạng đọc token pha alpha | `fe-gate.sh` — lệnh ở [`../../../quy-uoc/fe-ui-conventions.md`](../../../quy-uoc/fe-ui-conventions.md) §3.2; bẫy §8.5 | F1 | 📐 ĐÍCH ĐẾN — CHƯA THI CÔNG |
| **F8** | Template `.html` không chứa chữ tiếng Việt | `fe-gate.sh` — lệnh ở [`../../../quy-uoc/fe-ui-conventions.md`](../../../quy-uoc/fe-ui-conventions.md) §5.2; bẫy §8.6 | F2 | 📐 ĐÍCH ĐẾN — CHƯA THI CÔNG |
| **F9** | Không còn cú pháp Angular lỗi thời | `fe-gate.sh` — lệnh ở [`../../../quy-uoc/fe-ui-conventions.md`](../../../quy-uoc/fe-ui-conventions.md) §1.4 | F0 | 📐 ĐÍCH ĐẾN — CHƯA THI CÔNG |
| **F10** | `components/` và `pages/` không import DTO trực tiếp | `fe-gate.sh` — lệnh ở [`../../../quy-uoc/fe-api-client.md`](../../../quy-uoc/fe-api-client.md) §4.3; bẫy §8.7 | F0 | 📐 ĐÍCH ĐẾN — CHƯA THI CÔNG |
| **F11** | `components/` không inject service lấy dữ liệu | `fe-gate.sh` — không tha đường dẫn nào; allowlist **token** đọc từ bảng chủ lúc chạy, §8.8 | F1 | 📐 ĐÍCH ĐẾN — CHƯA THI CÔNG |
| **F12** | Mọi `*.service.ts` có `.spec.ts` cạnh nó | `fe-gate.sh` — đối chiếu tên file, §8.9 | F0 | 📐 ĐÍCH ĐẾN — CHƯA THI CÔNG |
| **F18** | Không đọc `data` của envelope bằng các dạng bị cấm | `fe-gate.sh` — lệnh ở [`../../../quy-uoc/fe-api-client.md`](../../../quy-uoc/fe-api-client.md) §1.2 | F0 | 📐 ĐÍCH ĐẾN — CHƯA THI CÔNG |
| **F19** | Lời gọi HTTP trong service không mang tiền tố của base URL | `fe-gate.sh` — lệnh ở [`../../../quy-uoc/fe-api-client.md`](../../../quy-uoc/fe-api-client.md) §2.1 | F0 | 📐 ĐÍCH ĐẾN — CHƯA THI CÔNG |
| **F20** | `app.routes.ts` không import tĩnh component của feature | `fe-gate.sh` — lệnh ở [`../../../quy-uoc/fe-routing-guard.md`](../../../quy-uoc/fe-routing-guard.md) §2.1 | F0 | 📐 ĐÍCH ĐẾN — CHƯA THI CÔNG |
| **F21** | `core/` không điều hướng tới đường dẫn route viết cứng | `fe-gate.sh` — lệnh ở [`../../../quy-uoc/fe-routing-guard.md`](../../../quy-uoc/fe-routing-guard.md) §6 | F0 | 📐 ĐÍCH ĐẾN — CHƯA THI CÔNG |
| **F22** | File không vượt ngưỡng cứng về số dòng | `fe-gate.sh` — lệnh ở [`../../../quy-uoc/fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §5 | F0 | 📐 ĐÍCH ĐẾN — CHƯA THI CÔNG |
| **F23** | Các tệp ngôn ngữ có cùng tập khoá, kiểm **từng tầng** `public/i18n/` và `public/i18n-app/` riêng | `fe-gate.sh` — lệnh ở [`../../../quy-uoc/fe-ui-conventions.md`](../../../quy-uoc/fe-ui-conventions.md) §5.3; thư mục vắng hay một tệp thì thoát 0, hành vi khai ở đó | F2 — cùng lúc với F8 | 📐 ĐÍCH ĐẾN — CHƯA THI CÔNG |

### 3.2 Cổng chạy bằng công cụ

| Mã | Nội dung | Ép bằng | Bật ở pha | Trạng thái |
| --- | --- | --- | --- | --- |
| **F1** | `core/` không import ngược lên `shared/` / `platform/` / `modules/` | `ng lint` — quy tắc chặn đường dẫn, vùng tầng đáy | F0 | 📐 ĐÍCH ĐẾN — CHƯA THI CÔNG |
| **F2** | `modules/<A>/` không import nội bộ `modules/<B>/` | `ng lint` — quy tắc chặn đường dẫn, vùng module sinh từ mảng module | Cấu hình từ F0; **ép thật** khi có module nghiệp vụ đầu tiên — sau F3, ở dự án hạ nguồn ([`00-lo-trinh-tong-the.md`](00-lo-trinh-tong-the.md) §4.1) | 📐 ĐÍCH ĐẾN — CHƯA THI CÔNG |
| **F24** | `shared/ui/` không import `shared/components/` | `ng lint` — quy tắc chặn đường dẫn, vùng lớp bọc ([`../../../quy-uoc/fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §4.6) | F0 | 📐 ĐÍCH ĐẾN — CHƯA THI CÔNG |
| **F13** | Mọi `@for` có `track` | `ng build` — `@for` thiếu `track` là lỗi biên dịch template, §8.10 | F0 | 📐 ĐÍCH ĐẾN — CHƯA THI CÔNG |
| **F14** | Bundle không vượt ngân sách đã khai | `ng build` — ngân sách trong cấu hình build | F3 | 📐 ĐÍCH ĐẾN — CHƯA THI CÔNG |

**F2 bật lúc nào — đọc theo file chủ** ([`../../../quy-uoc/fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §4.3–§4.4). Vùng module sinh ra từ mảng tên module; mảng rỗng thì khối cấu hình bị bỏ hẳn ([`01-f0-nen-mong.md`](01-f0-nen-mong.md) §3.2). Vì vậy từ F0 tới hết F3, repo Core **không có** module nào và F2 **chưa ép gì** — thứ canh khoảng đó là F4. F2 bắt đầu ép khi tên module đầu tiên vào mảng, và được chứng minh bằng canary theo bước 4 của quy trình thêm module ở file chủ.

### 3.3 Luật có mã nhưng chưa có cổng — F15, F16, F17

Các luật dưới đây có mã và có dòng riêng trong bảng nợ ở [`../../../RULES.md`](../../../RULES.md) §10; chúng **không** phải luật vô danh.

| Mã | Luật | Khai chi tiết ở | Vì sao chưa chạy được |
| --- | --- | --- | --- |
| **F15** | Sàn coverage FE theo tầng | [`../06-testing-strategy.md`](../06-testing-strategy.md) §4 | Cần `src/FE`, bộ chạy test và ngưỡng khai trong cấu hình test |
| **F16** | Mọi khoá i18n FE tra cứu khớp một mã lỗi BE đã khai | [`../08-i18n.md`](../08-i18n.md) §5.3 | Cần cả hai phía tồn tại để đối chiếu hai chiều |
| **F17** | Template đạt bộ quy tắc tiếp cận đã chọn | [`../15-accessibility.md`](../15-accessibility.md) §6 | Phần lớn tiêu chí cần render thật, không quét tĩnh được |

Chúng nằm ở đây, **không** nằm trong §3.1–§3.2: một dòng trong bảng cổng đọc như một cổng đang canh, trong khi chưa có gì canh cả. Cách ép dự kiến của từng luật ghi ở cột tương ứng của `RULES.md` §10. Bộ test vẫn chạy ở bước 3 của §5; thứ còn nợ là **sàn** coverage và bộ quy tắc tiếp cận, không phải việc chạy test.

> 📌 **Tên section trong `scripts/fe-gate.sh` mang mã luật F, không mang một hệ mã thứ hai.** Lý do: hai hệ số song song cho cùng một tập luật thì mỗi bảng đều "trông đúng" khi đọc một mình, và lệch chỉ lộ ra khi có người mở cả hai — tức gần như không bao giờ.

---

## 4. 🛑 CẢNH BÁO — script cổng KHÔNG phải toàn bộ cổng FE

> ### Chạy mỗi `fe-gate.sh` rồi tuyên bố "cổng xanh" là **sai**, và đó là cách hỏng **đã xảy ra thật**.

Bảng §3 chia làm hai vì một lý do vận hành, không phải vì trình bày: **script chỉ phủ nhóm 3.1.** Mọi luật ở nhóm 3.2 — trong đó có cả F1, luật ranh giới quan trọng nhất — **không** nằm trong script và sẽ không bao giờ đỏ dù có vi phạm, nếu người chạy chỉ gọi script.

### 4.1 Chuyện đã xảy ra ở dự án tiền nhiệm

Tài liệu hướng dẫn chạy một script cổng trong khi **file đó không tồn tại trên đĩa**.

Cách nó hỏng mới là điều đáng nhớ: khối lệnh gồm bốn dòng. Dòng đầu gọi script và báo `No such file or directory`. **Ba dòng sau chạy bình thường.** Người chạy thấy ba đầu ra xanh và kết luận cổng đã xanh.

Kết quả: ba mục cổng **không có gì canh** suốt một thời gian dài, trong khi tài liệu vẫn ghi bình thường. Một luật màu trong số đó được dọn tay hai lần và tự tái sinh cả hai lần.

📖 [`../../../audit/2026-08-23-cong-khong-ton-tai.md`](../../../audit/2026-08-23-cong-khong-ton-tai.md)

### 4.2 Ba bài học rút thành luật

| Bài học | Thành luật gì ở repo này |
| --- | --- |
| "Có tài liệu về cổng" ≠ "có cổng" | Mọi cổng phải khai **lệnh chạy được**, không chỉ mô tả |
| "Có script" ≠ "cổng đã chạy" | Phải kiểm **sự tồn tại** của script trước khi chạy nó (§5) |
| Một lệnh hỏng giữa chuỗi không dừng chuỗi | Không nối các lệnh cổng bằng dấu chấm phẩy; dùng nối có điều kiện, hoặc chạy từng lệnh và đọc từng kết quả |

### 4.3 Kiểm chính cổng, không chỉ chạy cổng

Luật T1 của [`../../../RULES.md`](../../../RULES.md) — *"mọi detector phải có test kiểm chính detector đó"* — áp cho phía FE dưới dạng **canary**:

> Trước khi tin một mục cổng, hãy làm nó **đỏ** một lần: thêm tạm một vi phạm, chạy cổng, thấy đỏ đúng chỗ, rồi hoàn nguyên.

Không làm bước này thì không phân biệt được "cổng xanh vì sạch" với "cổng xanh vì mù". Hai trạng thái đó cho ra cùng một đầu ra.

Ví dụ thật của một cổng mù: mẫu tìm DTO ở cổng F10 phải là dạng **không** có ranh giới từ ở đầu. Tên DTO thật luôn có ký tự chữ ngay trước phần `Dto`, nên một mẫu đòi ranh giới từ ở đầu **không bao giờ khớp** — cổng xanh vĩnh viễn, không phải vì mã nguồn sạch.

---

## 5. Lệnh chạy

```bash
# 0. Kiểm script có tồn tại — bước này KHÔNG được bỏ, xem §4.1
ls scripts/fe-gate.sh

# 1. Cổng script (nhóm 3.1) — rẻ nhất, chạy trước
bash scripts/fe-gate.sh

# 2. Cổng lint (F1, F2, F24)
cd src/FE && npx ng lint

# 3. Bộ test — Karma + Jasmine trên ChromeHeadless; sàn coverage F15 còn là nợ (§3.3)
npx ng test --watch=false --browsers=ChromeHeadless

# 4. Build — F13 (lỗi biên dịch template) và ngân sách bundle F14
npx ng build
```

**Đọc từng kết quả.** Các lệnh trên cố ý viết thành từng dòng riêng chứ không nối bằng dấu chấm phẩy — nối như vậy là tái tạo đúng chế độ hỏng ở §4.1.

Nếu môi trường chưa trỏ được tới trình duyệt, bước 3 hỏng với thông báo không tìm thấy trình duyệt. Đó là **lỗi môi trường**, không phải cổng đỏ — nhưng cũng **không** phải cổng xanh.

> 📖 Bộ chạy test và toolchain đã chốt: [`../../../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md`](../../../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md). File này không chép số phiên bản.

---

## 6. Chạy ở đâu — máy dev VÀ CI, không chỉ một trong hai

Repo này **có CI** ([`../../../adr/0011-ci-github-actions.md`](../../../adr/0011-ci-github-actions.md)), và đó là điểm khác quan trọng so với dự án tiền nhiệm — nơi cổng phụ thuộc hoàn toàn vào trí nhớ người chạy.

| Nơi chạy | Vai | Không thay được nhau vì |
| --- | --- | --- |
| **Máy dev, trước khi commit** | Phản hồi nhanh; sửa ngay lúc còn nhớ code | Chờ CI cho một lỗi màu literal là lãng phí |
| **CI, trên mỗi PR** | Không phụ thuộc trí nhớ ai | Máy dev có thể quên, có thể cấu hình khác, có thể bỏ qua |

**Cả hai đều cần.** Chỉ có CI thì vòng phản hồi dài và người ta commit thử nhiều lần; chỉ có máy dev thì quay lại đúng điều kiện đã sinh ra vấn đề ban đầu — cổng tồn tại nhưng không ai gọi nó.

Hai ràng buộc cho phần CI:

1. **CI chạy đúng các lệnh ở §5, đúng thứ tự đó**, và fail ở bất kỳ lệnh nào là fail cả job.
2. **Bước kiểm sự tồn tại của script không được bỏ trên CI.** Một script thiếu phải làm CI đỏ, không được để nó trượt qua thành một dòng cảnh báo.

---

## 7. Đếm mục cổng bằng lệnh, đừng chép số

Theo [`../../../../.claude/CLAUDE.md`](../../../../.claude/CLAUDE.md) §6, tài liệu không được chép số đếm được. Số mục trong script đọc bằng:

```bash
grep -c '^section ' scripts/fe-gate.sh
```

Số đó phải khớp số dòng ở bảng §3.1. **Lệch là một dấu hiệu**, và lệch theo hai chiều mang hai nghĩa khác nhau:

| Lệch | Nghĩa |
| --- | --- |
| Script có ít mục hơn bảng | Một luật đã khai nhưng chưa có gì ép — đúng thứ mà bảng này sinh ra để lộ ra |
| Script có nhiều mục hơn bảng | Có cổng đang chạy mà không luật nào khai — phải bổ sung vào [`../../../RULES.md`](../../../RULES.md) §7 hoặc gỡ |

Chiều thứ hai nguy hiểm hơn vẻ ngoài: một cổng không có luật tương ứng là một cổng không ai biết vì sao nó tồn tại, và nó sẽ bị gỡ ở lần đầu tiên nó cản đường ai đó.

---

## 8. Ghi chú thi công cho từng cổng dễ sai

Hai nguyên tắc cho **mọi** section của script:

1. **Đọc định nghĩa từ file chủ lúc chạy, không chép vào script.** Khi một cổng cần một danh sách — quy tắc cấm tắt, allowlist đường dẫn, mảng module — script đọc nó từ đúng nơi giữ định nghĩa. Một mảng chép vào script là bản sao thứ hai, và bản không ai nhớ sẽ nói dối ([`../../../../.claude/CLAUDE.md`](../../../../.claude/CLAUDE.md) §5).
2. **Đọc được 0 phần tử là đỏ, không phải xanh.** Tiêu đề bảng đổi chữ, file chủ đổi chỗ, thư mục đích không tồn tại — cả ba đều khiến phép đọc trả rỗng, và một cổng coi rỗng là sạch thì xanh vì mù (§4.3). Mỗi section kiểm đầu vào không rỗng và thư mục đích tồn tại trước khi quét.

Mọi lệnh dưới đây chạy từ gốc repo, trên Git Bash, không dùng `grep -P`.

### 8.1 F3 — cấm `eslint-disable` cho quy tắc ranh giới

Quét **đúng danh sách quy tắc ranh giới**, không quét mọi `eslint-disable` — có quy tắc mà tắt cục bộ là hợp lý.

> 📖 **Danh sách quy tắc cấm tắt: bảng ở [`../../../quy-uoc/fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §4.5**, dưới tiêu đề mở đầu bằng `Danh sách rule cấm tắt`. Thêm một rule vào bộ ranh giới là sửa bảng đó; script tự thấy ở lần chạy sau.

```bash
# Luật F3 — PASS khi lệnh cuối không in dòng nào.
BANG=docs/quy-uoc/fe-architecture.md
RULES=$(awk '/Danh sách rule cấm tắt/ { f = 1; next }
             /^#/ { f = 0 }
             f && /^\| `/' "$BANG" | cut -d'`' -f2 | paste -sd'|' -)
[ -n "$RULES" ] || { echo "F3: không đọc được danh sách rule từ $BANG"; exit 1; }
[ -d src/FE/src ] || { echo "F3: không có src/FE/src để quét"; exit 1; }
grep -rnE "eslint-disable(-next-line|-line)?.*($RULES)" src/FE/src --include='*.ts' --include='*.html'
```

Quy ước của bảng chủ mà lệnh dựa vào: mỗi dòng rule có cột đầu là **một** tên rule trong dấu backtick; dòng không mở đầu bằng backtick bị bỏ qua.

Mẫu phủ cả ba dạng tắt theo tên — cho cả file, cho dòng kế tiếp, cho chính dòng đó — và dùng `.*` giữa `eslint-disable` với tên rule, vì một comment tắt nhiều rule liệt kê tên theo thứ tự bất kỳ. Dạng tắt **toàn file không kèm tên rule** cần mẫu riêng: lệnh ở file chủ, cùng §4.5.

**Không có F3 thì F1 và F2 chỉ là gợi ý** — vì cả hai đều gỡ được bằng một dòng comment.

### 8.2 F4 — mảng module khớp thư mục thật

Vì sao cần: quy tắc chặn đường dẫn của ESLint đòi danh sách vùng có ít nhất một phần tử, nên khi chưa có module nào thì **cả khối cấu hình phải bị bỏ hẳn** ([`01-f0-nen-mong.md`](01-f0-nen-mong.md) §3.2).

Mảng tên module khai ở `src/FE/eslint.boundaries.cjs`; `eslint.config.js` `require` tệp đó để sinh vùng. F4 đọc **cùng tệp** đó — không đọc `eslint.config.js`, không chép mảng. Lệnh: [`../../../quy-uoc/fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §4.4.

Điều đó tạo ra hai trạng thái hỏng, và F4 bắt cả hai:

| Trạng thái hỏng | Hậu quả |
| --- | --- |
| Cấu hình liệt kê một module **không tồn tại** | Quy tắc không phân giải nổi đường dẫn đích ⇒ không chặn được gì, nhưng **trông như đang chạy** |
| Thư mục module tồn tại nhưng **không** có trong cấu hình | Module đó không bị ràng buộc ranh giới nào |

Cả hai đều là "cổng xanh vì không kiểm gì cả" — dạng hỏng mà toàn bộ file này tồn tại để chống.

### 8.3 F5 — allowlist theo đường dẫn

Đường dẫn được phép chạm thư viện UI khai ở **đúng một chỗ**: bảng ở [`../05-component-library.md`](../05-component-library.md) §2.4, dưới tiêu đề mở đầu bằng `Allowlist import thư viện UI`. Script đọc các dòng ✔ của bảng đó lúc chạy:

```bash
# Luật F5 — PASS khi lệnh cuối không in dòng nào.
BANG=docs/wiki-core/fe/05-component-library.md
MAU=$(awk '/Allowlist import thư viện UI/ { f = 1; next }
           /^#/ { f = 0 }
           f && /^\| `/ && /✔/' "$BANG" \
      | cut -d'`' -f2 | sed -e 's#/\*\*$#/#' -e 's#^#^src/FE/#' | paste -sd'|' -)
[ -n "$MAU" ] || { echo "F5: không đọc được allowlist từ $BANG"; exit 1; }
[ -d src/FE/src/app ] || { echo "F5: không có src/FE/src/app để quét"; exit 1; }
grep -rn "from 'primeng/" src/FE/src/app --include='*.ts' | grep -v '\.spec\.ts:' | grep -vE "$MAU"
```

Quy ước của bảng chủ mà lệnh dựa vào: cột đầu là **một** đường dẫn trong dấu backtick, tính từ `src/FE/`; thư mục kết thúc bằng `/**`, tệp ghi đúng tên. Đổi quy ước đó là sửa lệnh cùng lượt.

Allowlist theo **đường dẫn cụ thể**, không theo mẫu tên file — một allowlist dạng "mọi file có `ui` trong tên" tha nhầm bất cứ file nào ai đó đặt tên khéo.

Trước khi tin mục này: mỗi đường dẫn trong allowlist phải có mặt trong sơ đồ thư mục ở [`../../../quy-uoc/fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §2. Cổng khoá một đường dẫn không tồn tại là cổng luôn xanh vì không có gì rơi vào vùng nó canh.

### 8.4 F6 — hex literal trong SCSS

Nơi duy nhất được giữ literal màu là tệp khai token ([`../../../quy-uoc/fe-ui-conventions.md`](../../../quy-uoc/fe-ui-conventions.md) §3.3). Cổng loại trừ **đúng tệp đó**, không loại trừ cả thư mục, và quét cả style toàn cục chứ không chỉ `src/app`.

```bash
# Luật F6 — PASS khi không in ra dòng nào.
[ -d src/FE/src ] || { echo "F6: không có src/FE/src để quét"; exit 1; }
grep -rnE '#([0-9a-fA-F]{3,4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})\b' src/FE/src --include='*.scss' \
  | grep -v '^src/FE/src/styles/_tokens\.scss:'
```

Mẫu khớp bốn độ dài mã hex hợp lệ và không khớp nội suy SCSS dạng `#{…}`. Nó **có** khớp một bộ chọn id trông giống mã hex; gặp ca đó thì xử lý theo §9 — loại trừ một đường dẫn cụ thể kèm lý do, không nới mẫu.

### 8.5 F7 — miễn trừ theo CÚ PHÁP, không theo giá trị

Chỉ tha đúng một dạng: đọc token ra rồi pha alpha. **Không** tha theo giá trị, kể cả những giá trị trông vô hại — lý do đầy đủ ở [`../04-design-token-system.md`](../04-design-token-system.md) §6.2.

**Bẫy khi hiện thực:** phải **gỡ dạng được phép ra khỏi dòng trước, rồi mới hỏi lại phần còn lại**. Nếu chỉ loại bỏ những dòng *có chứa* dạng hợp lệ, một dòng vừa có dạng hợp lệ vừa có một literal trần sẽ được tha nhầm nguyên dòng.

### 8.6 F8 — dò dấu thanh, và bẫy số dòng

Cách dò và ba giới hạn: [`../08-i18n.md`](../08-i18n.md) §10.

Bẫy phải tránh: khi xoá chú thích HTML trước lúc dò, **phải thay chúng bằng đúng số ký tự xuống dòng mà chúng chiếm**. Xoá trắng làm luồng ngắn lại và số dòng báo ra lệch so với file thật — người sửa mở nhầm chỗ, không thấy gì, rồi kết luận cổng báo bậy và bỏ qua nó.

Một cổng chỉ sai số dòng thôi cũng đủ để mất hết uy tín.

### 8.7 F10 — mẫu tìm DTO

Xem §4.3. Và **loại trừ `*.spec.ts`** — đây là **phạm vi đúng** của luật, không phải một ngoại lệ: F10 bảo vệ đường code chạy thật, còn spec giả lập tầng HTTP thì bắt buộc phải dựng payload đúng hình dạng wire, tức phải nói bằng DTO.

### 8.8 F11 — component dumb: không tha đường dẫn, chỉ tha token

Layout shell không phải ngoại lệ: phần smart ở `platform/shell`, còn `Sidebar` và `Topbar` là component dumb nhận dữ liệu qua `input()` ([`../05-component-library.md`](../05-component-library.md) §5.2). Cổng này **không tha đường dẫn nào**.

Thứ được tha là **token**: component dumb vẫn phải chạm DOM và vòng đời của chính nó, và các token dưới đây không lấy dữ liệu. Cổng đọc bảng này lúc chạy; thêm một token là thêm một dòng kèm lý do, không sửa lệnh.

#### Token được inject trong component dumb — định nghĩa gốc

| Token | Vì sao không lấy dữ liệu |
| --- | --- |
| `ElementRef` | Phần tử gốc của chính component — đo kích thước, đặt focus |
| `DestroyRef` | Dọn đăng ký khi component huỷ |
| `ChangeDetectorRef` | Báo vòng phát hiện thay đổi của chính component |
| `Renderer2` | Thao tác DOM qua lớp trừu tượng của Angular |
| `DOCUMENT` | Tài liệu gốc — gắn và gỡ bộ nghe sự kiện nằm ngoài phần tử gốc |
| `NgControl` | Control cài `ControlValueAccessor` đọc `invalid && touched` của control chủ trên chính phần tử: `inject(NgControl, { self: true, optional: true })` — `self` để không với lên control của form cha, `optional` cho ca không gắn control. Cách đăng ký CVA đi cùng: [`../../../quy-uoc/fe-ui-conventions.md`](../../../quy-uoc/fe-ui-conventions.md) §6.1 |

```bash
# Luật F11 — PASS khi lệnh cuối không in dòng nào.
BANG=docs/wiki-core/fe/trien-khai/05-gate.md
TOKEN=$(awk '/Token được inject trong component dumb/ { f = 1; next }
             /^#/ { f = 0 }
             f && /^\| `/' "$BANG" | cut -d'`' -f2 | paste -sd'|' -)
[ -n "$TOKEN" ] || { echo "F11: không đọc được allowlist token từ $BANG"; exit 1; }
[ -d src/FE/src/app ] || { echo "F11: không có src/FE/src/app để quét"; exit 1; }
grep -rnE 'inject(<[^()]*>)?\(' src/FE/src/app --include='*.ts' | grep '/shared/components/' | grep -v '\.spec\.ts:' \
  | sed -E "s/inject(<[^()]*>)?\( *($TOKEN)(<[^()]*>)?( *, *\{[^()]*\})? *\)//g" \
  | grep -E 'inject(<[^()]*>)?\('
```

Lệnh **gỡ dạng được tha ra khỏi dòng trước, rồi mới hỏi phần còn lại** — cùng bẫy ở §8.5: loại cả dòng có chứa một token được tha thì một dòng vừa `inject(DestroyRef)` vừa inject một service dữ liệu sẽ được tha nhầm nguyên dòng. Dạng được tha: `inject(Token)`, `inject(Token<…>)`, `inject<…>(Token)`, và mỗi dạng kèm object tuỳ chọn `inject(Token, { … })` — tuỳ chọn không đổi token nào được lấy.

Quét đúng `shared/components/` — **phạm vi câu luật F11 ở [`../../../RULES.md`](../../../RULES.md) §7**, không rộng hơn. Thư mục `shared/ui/` không nằm trong phạm vi: lớp bọc thư viện không phải component dumb theo nghĩa của F11.

🛑 **Lỗ mù đã biết, ghi ra để không ai tưởng F11 phủ hết.** [`../../../quy-uoc/fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §3.1 áp cùng cấm đoán cho `components/` của **từng feature** (`platform/<feature>/components/`, `modules/<feature>/components/`), nhưng câu luật F11 chỉ nói `shared/components/` nên cổng không quét chúng. Một cổng quét rộng hơn câu luật là cổng chặn thứ không luật nào cấm — [`../../../RULES.md`](../../../RULES.md) §7 phải nới câu luật trước, rồi lệnh trên nới theo cùng lượt. Cho tới lúc đó, component dumb của feature do review người canh.

Hai giới hạn phải biết khi thi công:

| Giới hạn | Hệ quả |
| --- | --- |
| Mẫu bắt `inject(`, **không** bắt inject qua tham số constructor | Một component inject bằng constructor đi qua cổng. Hoặc thêm mẫu cho dạng đó, hoặc ghi lỗ mù ngay trong script |
| Mẫu so theo **dòng**: lời gọi xuống dòng giữa `inject(` và tên token, hay giữa token và object tuỳ chọn | Cổng đỏ nhầm, theo chiều an toàn. Viết lời gọi trên một dòng |

### 8.9 F12 — spec cạnh service

```bash
# Luật F12 — PASS khi không in ra dòng nào. In đích danh từng service thiếu spec.
[ -d src/FE/src/app ] || { echo "F12: không có src/FE/src/app để quét"; exit 1; }
find src/FE/src/app -name '*.service.ts' | while IFS= read -r f; do
  [ -f "${f%.ts}.spec.ts" ] || echo "$f"
done
```

Quét mọi `*.service.ts`, không chỉ trong thư mục `services/`: service hạ tầng ở `core/` — phiên, toast — mang sàn coverage cao nhất ([`../06-testing-strategy.md`](../06-testing-strategy.md) §4) mà không nằm trong thư mục tên `services/`.

Giới hạn: cổng kiểm **sự tồn tại** của spec, không kiểm nội dung (§10).

### 8.10 F13 — `track` do compiler ép

`@for` thiếu `track` không biên dịch được, nên `ng build` là cổng — không cần quy tắc lint riêng, và không có gì để tắt bằng một dòng comment.

Thứ compiler **không** bắt: khoá `track` có ổn định không. `track $index` vẫn biên dịch, dù nó gần như vô hiệu hoá `track` khi danh sách sắp xếp lại ([`../05-component-library.md`](../05-component-library.md) §6). Phần đó thuộc review.

### 8.11 F14 — lỗ mù đã biết

Ngân sách thường chỉ khai cho phần khởi động; **lazy chunk không bị ràng buộc gì**. Một chunk lazy lớn hơn cả bundle khởi động vẫn đi qua cổng im lặng — điều này đã xảy ra thật ở dự án tiền nhiệm.

Khai ngân sách cho lazy chunk ngay khi có lazy chunk đầu tiên. Không khai được thì **phải ghi lỗ mù đó ra ở chính file này**, để không ai tưởng F14 phủ hết.

---

## 9. Nguyên tắc khi sửa hoặc thêm cổng

| Nguyên tắc | Vì sao |
| --- | --- |
| **Tính năng trước, cổng ngay sau** | Không viết cổng cho thứ chưa tồn tại — nó sẽ là một mục luôn xanh và không ai biết nó có chạy không |
| **Không nới rule chung để tha một chỗ** | Loại trừ một **đường dẫn cụ thể** kèm lý do. Rule lỏng thì lần sau không ai biết vì sao nó lỏng |
| **Mỗi cổng mới phải có canary** | §4.3 |
| **Mỗi cổng mới phải có mã luật** | Cổng không có luật sẽ bị gỡ ở lần đầu nó cản đường ai đó |
| **Ghi lý do ngay trong script** | Người gặp cổng đỏ sẽ đọc script trước khi đọc tài liệu |

---

## 10. Điều cổng KHÔNG bao giờ bắt được

Cổng chỉ bắt thứ máy kiểm được. Bốn loại lỗi nó không bao giờ bắt, ghi ra để không ai nhầm "cổng xanh" với "code đúng":

1. **Logic sai nhưng cú pháp đúng.** Một guard chặn nhầm nhóm người dùng đi qua mọi cổng.
2. **Câu chữ sai.** Một thông báo hứa hiệu lực tức thì trong khi thao tác có độ trễ ([`04-f3-man-quan-tri.md`](04-f3-man-quan-tri.md) §3.2).
3. **Trải nghiệm hỏng.** Focus đi sai chỗ, bảng nhảy khi tải, lỗi hiện không đúng ô.
4. **Test viết cho có.** Cổng F12 kiểm **sự tồn tại** của file spec, không kiểm nội dung bên trong.

Ba loại đầu thuộc về danh sách nghiệm thu của từng pha — và đó là lý do các danh sách đó phải đi **bằng tay**, không bằng cách đọc lại code.

---

## 11. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| Bảng luật đầy đủ, cả BE lẫn FE | [`../../../RULES.md`](../../../RULES.md) §7 |
| Sự cố cổng không tồn tại | [`../../../audit/2026-08-23-cong-khong-ton-tai.md`](../../../audit/2026-08-23-cong-khong-ton-tai.md) |
| Vì sao cổng bật ở từng pha | [`00-lo-trinh-tong-the.md`](00-lo-trinh-tong-the.md) §3.3 |
| Cấu hình ESLint ranh giới | [`01-f0-nen-mong.md`](01-f0-nen-mong.md) §3 |
| Bộ chạy test và toolchain | [`../../../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md`](../../../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md) |
| Ngưỡng coverage | [`../06-testing-strategy.md`](../06-testing-strategy.md) §4 |
| Ngân sách bundle | [`../13-performance.md`](../13-performance.md) §4 |
| Cổng tài liệu của giai đoạn 1 | [`../../../../.claude/CLAUDE.md`](../../../../.claude/CLAUDE.md) §8 |
