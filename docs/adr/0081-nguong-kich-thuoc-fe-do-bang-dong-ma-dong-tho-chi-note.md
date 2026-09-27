---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0081 — Ngưỡng kích thước tệp FE đo bằng số dòng MÃ; số dòng thô vượt ngưỡng cứng chỉ in `NOTE`

> **Trạng thái:** Đã chấp nhận (2026-09-24)

## Bối cảnh

Bảng ngưỡng ở [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §5 là đầu vào của cổng **F22**. Ngày 2026-09-24 cổng đó mở rộng từ riêng `*.page.ts` sang **mọi** loại tệp trong bảng, và nó đếm **dòng thô**: dòng trống và dòng chú thích tính như mã.

`frontend-expert` vừa tách `src/FE/src/app/shared/ui/tooltip/tooltip.component.ts` từ 272 xuống 244 dòng để lọt ngưỡng cứng 250, rồi đo thấy 244 dòng đó gồm 22 dòng trống, 93 dòng chú thích và 129 dòng mã. `architect` đo lại khớp, rồi đo toàn bộ `src/FE/src/app` cùng ngày:

| Nhóm | Tỉ lệ dòng chú thích |
| --- | --- |
| Lớp bọc thư viện ở `shared/ui/` | 32–53% (`toast` 53%, `dialog` 43%, `tooltip` 38%, `check` 32%) |
| `*.page.ts` | 3–10% |

Đếm thô vì vậy cho lớp bọc một ngân sách thực tế nhỏ hơn hẳn trang, và nó phạt đúng thứ repo cần ở lớp bọc: chú thích giải thích vì sao thư viện cư xử lạ. Với cổng mới, `tooltip` còn **6 dòng**: một chú thích dài hơn thế làm cổng đỏ, và lối ra rẻ nhất lúc đó là xoá chú thích.

Hôm đó không tệp nào vượt ngưỡng cứng, dù đếm theo cách nào. Riêng ngưỡng mềm của `*.page.ts`: đếm thô có 5/11 tệp vượt, đếm mã chỉ còn 1.

## Quyết định

**Người dùng chốt:** cổng F22 **đỏ** khi số **dòng mã** vượt ngưỡng cứng; số **dòng thô** vượt ngưỡng cứng mà dòng mã không vượt thì cổng chỉ in **`NOTE`**. Không con số nào trong bảng đổi.

**`architect` chốt ba điều đi kèm:**

1. **Cả hai cột ngưỡng — mềm và cứng — đọc theo số dòng mã.** Ngưỡng mềm không có cổng, nhưng để nó đo bằng một thước khác ngưỡng cứng thì cùng một tệp có hai câu trả lời cho câu *"đã tới lúc tách chưa"*.
2. **Định nghĩa phân loại dòng nằm ở §5 của `fe-architecture.md`, và đó là nguồn duy nhất.** Bộ bóc trong cổng dựng theo đúng đoạn đó. Hai quy tắc giữ cho bộ bóc đơn giản:
   - **Dấu mở chú thích chỉ có hiệu lực khi đứng đầu dòng.** Nhờ vậy bộ bóc khỏi phải hiểu chuỗi ký tự, và `'https://…'` hay `` `a /* b` `` giữa dòng không bao giờ mở chú thích.
   - **Mọi chỗ mơ hồ nghiêng về đếm là mã.** Một khối mở giữa dòng sau mã không được theo dõi, nên các dòng bên trong nó tính là mã. Sai theo chiều này chỉ làm cổng chặt hơn, không bao giờ lỏng hơn.
3. **Canary của bộ bóc có một ca cho mỗi quy tắc phân loại ở §5**, kèm ca đỏ cặp đôi theo luật **T12** ([`../RULES.md`](../RULES.md) §8). Việc dựng thuộc `test-engineer`.

## Phương án đã cân nhắc và vì sao loại

### Phương án 1 — Giữ đếm thô

**Được:** một con số, bộ dò không cần hiểu cú pháp chú thích, không có điểm mù.

**Vì sao loại:** phép đo ở §Bối cảnh cho thấy nó không trung lập — nó đánh thuế chú thích, và nhóm tệp bị đánh thuế nặng nhất là nhóm cần chú thích nhất. Với F22 phủ mọi loại tệp, cái thuế đó thành một áp lực có thật lên từng lần viết thêm một câu giải thích.

### Phương án 2 — Ngưỡng riêng cho `shared/ui/`

**Được:** máy kiểm được — cây `shared/ui/` đã có định nghĩa qua allowlist F5.

**Vì sao loại:** `shared/ui/` sẽ mang nghĩa thứ hai (*"được ngân sách lớn hơn"*) bên cạnh nghĩa duy nhất mà [ADR-0078](0078-cot-nen-giu-quyen-quyet-thu-muc-button-va-input-doi-sang-shared-components.md) vừa giữ cho nó (*"được import `primeng/*`"*). Nó cũng không giúp gì cho một tệp nhiều chú thích nằm ở chỗ khác.

### Phương án 3 — Chỉ đếm dòng mã, không in gì về dòng thô

**Được:** đơn giản hơn phương án đã chọn một nhánh.

**Vì sao loại:** về đường đỏ nó **y hệt** phương án đã chọn — cái nới dưới đây là của cả hai. Khác biệt duy nhất là phương án này nới **trong im lặng**: một tệp phình ra bằng chú thích, hay bằng mã bị ghi thành chú thích, không để lại dấu vết nào trong output của cổng. `NOTE` giữ cho khoản nới đó nhìn thấy được.

## Hệ quả

### Tích cực

- Viết thêm chú thích ở lớp bọc không còn đẩy tệp tới cổng đỏ.
- Mọi tệp có dòng thô vượt ngưỡng cứng đều hiện ra trong output, kể cả khi cổng xanh.

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| **Đường đỏ nới ra cho tệp nhiều chú thích, dù không con số nào đổi** | Ở tỉ lệ của `tooltip` — 129 dòng mã trên 244 dòng thô — ngưỡng cứng 250 dòng mã tương đương khoảng 470 dòng thô. Đây là chủ đích, nhưng nó là một khoản nới thật, và nó lớn nhất ở đúng nhóm tệp ít người đọc lại nhất |
| **Mã bị ghi thành chú thích không bị tính** | Một khối mã chết để lại dưới dạng chú thích đi qua cổng. Lớp bắt nó là `NOTE` và người đọc; [ADR-0010](0010-comment-toi-thieu.md) vẫn áp |
| **Bộ bóc là một phép dò có điểm mù** | Một dòng của chuỗi nhiều dòng (template literal) bắt đầu bằng dấu mở chú thích bị đếm là chú thích; nếu dòng đó mở khối mà không đóng thì các dòng sau cũng bị đếm theo, tới dấu đóng kế tiếp. Hôm nay component dùng `templateUrl`/`styleUrl` nên ca này hiếm, nhưng không ai canh nó |
| **Hai con số phải giải thích thay vì một** | Người đọc output phải biết `NOTE` về dòng thô **không** phải cảnh báo cần sửa |
| **Thêm một loại tệp vào bảng nay tốn thêm một bước** | Đuôi tệp chưa có cú pháp chú thích khai ở §5 làm cổng đỏ — đúng như mong muốn, nhưng người thêm dòng bảng phải biết |

### Dấu hiệu quyết định này bắt đầu sai

1. Một `NOTE` về dòng thô chỉ vào một tệp mà phần chênh là **mã chết ghi thành chú thích**, không phải lời giải thích.
2. Điểm mù template literal xảy ra trong một tệp thật.
3. Một tệp vượt ngưỡng mềm về dòng thô từ lâu mà không ai tách, vì đếm mã nó vẫn dưới ngưỡng — tức ngưỡng mềm thôi là lúc dừng lại tự hỏi.

## Liên quan

- [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §5 — bảng ngưỡng và định nghĩa phân loại dòng.
- [`../RULES.md`](../RULES.md) §7 — luật F22; §8 — luật T12.
- [`0078-cot-nen-giu-quyen-quyet-thu-muc-button-va-input-doi-sang-shared-components.md`](0078-cot-nen-giu-quyen-quyet-thu-muc-button-va-input-doi-sang-shared-components.md) — nghĩa duy nhất của `shared/ui/`.
- [`0010-comment-toi-thieu.md`](0010-comment-toi-thieu.md) — chú thích tối thiểu, vẫn áp.
