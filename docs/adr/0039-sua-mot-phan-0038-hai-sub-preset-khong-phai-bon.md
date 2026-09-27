---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0039 — Sửa một phần ADR-0038: sub-preset thật cần hôm nay là `base` + `toast` (hai), không phải bốn

> **Trạng thái:** Đã chấp nhận (2026-09-17) · Sửa một phần [`0038-preset-primeng-tung-sub-theo-phan-dung.md`](0038-preset-primeng-tung-sub-theo-phan-dung.md)

## Bối cảnh

ADR-0038 (chấp nhận cùng ngày 2026-09-17) tự mâu thuẫn nội bộ. Mục Bối cảnh của nó xác nhận: *"`app-button` (`shared/ui/button/`) là component tự dựng, **không** bọc PrimeNG `Button`"* và ứng dụng *"chỉ thực dùng **một** component PrimeNG trực tiếp: `Toast`"* — nhưng ngay câu sau lại viết: *"Sub-preset thật đang cần chỉ khoảng **4** (`base` + `button` + `toast` + `message`, tổng ~30.9 kB đo được qua build stats)"*. Mục Quyết định #1 và #2 lặp lại con số bốn này nguyên văn: *"(hôm nay: `base`, `button`, `toast`, `message`)"*.

Hai câu này ngược nhau: nếu `app-button` không bọc PrimeNG `Button`, và không nơi nào trong repo import `primeng/message` hay dùng `p-message`, thì `button` và `message` không phải sub-preset "**thật** đang cần" — chúng đúng là loại suy đoán "chắc sẽ cần" mà chính ADR-0038 (Phương án B, mục *Vì sao loại*) đã bác bỏ khi áp cho 86 sub-preset còn lại của `Aura` gộp, và mà `kien-truc-core-module.md` §4.2 cấm áp cho phụ thuộc/hiện thực trong Core.

Xác minh lại ngày 2026-09-17, sau khi `frontend-expert` thi công `prime-preset.ts` qua hai lượt độc lập và `core-reviewer` grep toàn `src/FE/src`:

```bash
grep -rn "from 'primeng/" src/FE/src --include='*.ts'
```

chỉ trả về ba dòng: `primeng/config` (`providePrimeNG` — không phải component), `primeng/api` (`MessageService` — không phải component, là API chung mà `Toast` dùng để nhận message, không phải component `<p-message>`) và `primeng/toast` (component `Toast`, nơi duy nhất). Không có `primeng/button`, không có `primeng/message`. Đọc thẳng `src/FE/src/app/core/theme/prime-preset.ts` xác nhận nó chỉ ghép `AuraBase` + `AuraToast` — đúng **hai** sub-preset, không phải bốn; và `src/FE/src/app/shared/ui/button/button.component.ts` có comment tại chỗ: *"Tự dựng, không bọc PrimeNG (§Nền)"*.

Chạy lại `npx ng build` (từ `src/FE`) ngày 2026-09-17 cho kết quả đo thật:

```
Initial total | 411.33 kB raw | 111.15 kB estimated transfer
```

không còn cảnh báo `maximumWarning: 500kB` — thấp hơn cả mức **ước tính** ~430 kB mà ADR-0038 đưa ra dựa trên con số bốn sub-preset sai (506.93 − 76.36 kB).

Đây **không** phải một quyết định bị lật. Quyết định #1–#2 gốc của ADR-0038 — ghép sub-preset theo đúng những gì `src/FE` **thực sự** dùng trực tiếp, cấm import object `Aura` gộp, thêm luật F29 — hoàn toàn đúng và không đổi; chính quy tắc đó (đọc đúng nghĩa đen, không đọc theo ví dụ minh hoạ trong ngoặc) dẫn ra con số hai, không phải bốn. Sai chỉ nằm ở phần liệt kê minh hoạ "hôm nay cần bốn" — một lỗi đếm lúc viết, bị chính nội dung Bối cảnh của cùng ADR đó phản bác ngay trong cùng đoạn văn.

## Quyết định

1. **Ở ADR-0038, cụm "(hôm nay: `base`, `button`, `toast`, `message`)" ở mục Quyết định #1–#2, và câu "Sub-preset thật đang cần chỉ khoảng 4 (`base` + `button` + `toast` + `message`, tổng ~30.9 kB...)" ở mục Bối cảnh, đều SAI.** Con số đúng tại ngày ADR-0038 được chấp nhận là **2** (`base` + `toast`) — khớp đúng code thật đã thi công và khớp đúng câu miêu tả ứng dụng ở ngay Bối cảnh của chính 0038. ADR-0038 giữ nguyên nội dung theo đúng [`README.md`](README.md) §5; sai lệch này được ghi nhận và sửa ở đây, không sửa đè lên 0038.
2. **Quy tắc vận hành của ADR-0038 không đổi:** Quyết định #1 gốc ("sub-preset tương ứng những component PrimeNG mà `src/FE` **thực sự** dùng trực tiếp") và luật **F29** ở `RULES.md` vẫn nguyên văn, không sửa. Con số cụ thể trong ngoặc chỉ là ví dụ minh hoạ tại một thời điểm — không phải một phần của quy tắc, và ADR này không đổi quy tắc.
3. **Số liệu bundle ở ADR-0038 mục Hệ quả** ("Bundle initial giảm ước tính về ~430 kB") được thay bằng số đo thật: **411.33 kB raw / 111.15 kB estimated transfer**, đo bằng `npx ng build` ngày 2026-09-17 — số liệu này khai ở ADR này, không sửa vào 0038.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Sửa trực tiếp nội dung ADR-0038

**Được:** không cần thêm một ADR; người đọc chỉ thấy một bản duy nhất, không phải đối chiếu hai file để ra con số đúng.

**Vì sao loại:** [`README.md`](README.md) §5 cấm tuyệt đối việc sửa nội dung ADR cũ, **không có ngoại lệ** cho "lỗi đếm lúc viết" hay "phát hiện trong cùng ngày" — nguyên văn: *"KHÔNG sửa ADR cũ. Kể cả khi quyết định trong đó đã sai hoàn toàn."* Sửa đè sẽ xoá mất chính dấu vết cho thấy ADR-0038 khi viết ra đã tự mâu thuẫn ngay giữa Bối cảnh và Quyết định — bằng chứng đó có giá trị riêng cho quy trình viết ADR (xem Điều kiện lật quyết định).

### Phương án B — Không viết ADR, chỉ nêu trong báo cáo, để nguyên ADR-0038 sai trong tài liệu

**Được:** rẻ nhất, không thêm file.

**Vì sao loại:** ADR là nguồn duy nhất người sau tra "vì sao chọn thế này". Một ADR tự mâu thuẫn mà không có gì trong `docs/adr/` chú thích đúng ở đâu, sai ở đâu, sẽ tiếp tục gây hiểu lầm mỗi lần có người mở lại 0038 — có thể có người đọc thấy "button + message" trong ngoặc rồi tưởng đó là mục tiêu cần giữ, và thêm nhầm sub-preset khi chưa có component nào cần.

### Phương án C (được chọn) — ADR mới, "Sửa một phần" 0038, chỉ đổi phần liệt kê minh hoạ + số liệu bundle

Xem mục Quyết định. Quy tắc vận hành (Quyết định #1–#2 gốc của 0038, luật F29) không đổi, nên đây không phải quan hệ "Thay thế" — đúng khuôn **"Sửa một phần"** ở [`README.md`](README.md) §5.1: một phần (câu miêu tả "hôm nay cần bao nhiêu") hết đúng, phần còn lại (quy tắc) vẫn đúng nguyên vẹn.

## Hệ quả

### Tích cực

- Người đọc ADR-0038 thấy ngay ở dòng Trạng thái rằng có một ADR sửa một phần, và biết chính xác phần nào của 0038 không còn đúng — không phải tự đối chiếu code mới phát hiện ra.
- Số liệu bundle trong hồ sơ quyết định khớp số đo thật (`npx ng build`), không còn là con số suy đoán dựa trên một phép đếm sai.
- Giữ nguyên trạng ADR-0038 làm bằng chứng: một ADR tự mâu thuẫn ngay từ Bối cảnh sang Quyết định, trong cùng một đoạn văn, là dấu hiệu cụ thể cho thấy quy trình chấp nhận một ADR cần thêm một bước tự-đối-chiếu nội bộ trước khi đóng dấu "Đã chấp nhận" — mất bản gốc thì mất luôn ví dụ cụ thể để chỉ ra điều đó.

### Tiêu cực — cái giá thật

- **Từ nay đọc 0038 không đủ — phải đọc thêm dòng Trạng thái trỏ tới ADR này mới ra đúng con số.** Một người chỉ mở 0038, không theo link ở dòng Trạng thái, vẫn đọc "bốn sub-preset (base, button, toast, message)" và có thể tin đó là đúng.
- **Chi phí quy trình không tương xứng với lỗi.** Nguyên nhân gốc là một câu miêu tả sai lúc viết (không phải một quyết định kiến trúc bị đảo), nhưng luật §5 không phân biệt "lỗi đếm" với "quyết định bị lật" — cả hai đòi đúng một quy trình năm mục. Chấp nhận cái giá này để giữ đúng một quy tắc duy nhất cho ADR, không tạo ngoại lệ ngầm mà lần sau ai đó có thể lạm dụng ("chỉ là lỗi nhỏ, sửa thẳng cho nhanh").
- **Không có hành động thi công nào đi kèm.** ADR này thuần sửa hồ sơ; `prime-preset.ts` và luật F29 đã đúng từ trước, không đổi gì ở code.

### Điều kiện lật quyết định

Nếu về sau `src/FE` thật sự thêm import `primeng/button` hoặc `primeng/message` (một màn cần `p-message`, hoặc quyết định bọc PrimeNG `Button` thay `app-button`), con số "hai sub-preset" ở ADR này lại lỗi thời — đúng bản chất động của quy tắc ADR-0038 mục Quyết định #1. Lúc đó viết ADR mới ghi nhận con số hiện tại tại thời điểm đó, theo đúng cách ADR này vừa làm với 0038: không sửa đè lên ADR này, không sửa đè lên 0038.

## Liên quan

- [`0038-preset-primeng-tung-sub-theo-phan-dung.md`](0038-preset-primeng-tung-sub-theo-phan-dung.md) — ADR bị sửa một phần bởi ADR này; quy tắc vận hành (Quyết định #1–#2) không đổi.
- [`README.md`](README.md) §5, §5.1 — quy trình "Sửa một phần", áp dụng nguyên vẹn ở đây.
- [`../RULES.md`](../RULES.md) §7 F29 — luật không đổi bởi ADR này.
- [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.2 — ngưỡng "nhu cầu tương lai không tính" mà con số "bốn" sai đã vô tình vi phạm.
