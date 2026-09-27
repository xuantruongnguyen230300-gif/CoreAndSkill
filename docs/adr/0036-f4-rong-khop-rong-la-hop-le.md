---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0036 — Cổng F4 coi tập rỗng khớp tập rỗng là PASS, không phải lỗi vì thiếu thư mục `modules/`

> **Trạng thái:** Đã chấp nhận (2026-09-17)

## Bối cảnh

Định nghĩa gốc của cổng F4 ở [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §4.4 được viết với tiền đề
thư mục `src/FE/src/app/modules/` **sẽ** tồn tại ở một thời điểm nào đó, và coi việc thư mục đó vắng mặt là điều
kiện lỗi phải chặn trước khi so sánh:

```bash
[ -d src/FE/src/app/modules ] || { echo "F4: không có src/FE/src/app/modules để đối chiếu"; exit 1; }
```

[ADR-0032](0032-module-mau-o-du-an-ha-nguon.md) sau đó chốt: **module mẫu không bao giờ nằm trong repo Core** — nó
là module nghiệp vụ thật đầu tiên của dự án hạ nguồn đầu tiên. Hệ quả trực tiếp, đã ghi ở
[`../wiki-core/fe/trien-khai/00-lo-trinh-tong-the.md`](../wiki-core/fe/trien-khai/00-lo-trinh-tong-the.md) §4: thư
mục `modules/` "chưa có thư mục nào ở cuối F3, và đó là đúng" — nhưng với repo Core, "chưa có" này không phải một
trạng thái tạm thời sẽ hết ở một pha sau. Nó là trạng thái **vĩnh viễn** của chính repo Core, vì Core không bao giờ
tự sinh module.

`docs/wiki-core/fe/trien-khai/01-f0-nen-mong.md` §3.1 đã lường trước đúng nửa vấn đề này cho luật F2 ("Danh sách
module rỗng ⇒ vùng này chưa có gì để ép") nhưng không giải quyết cho F4 — vì F4 được viết ra chính là để bắt tình
huống "cấu hình khai một module nhưng thư mục không có" (mục đích ghi rõ ở
[`../wiki-core/fe/trien-khai/05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) §8.2). Tác giả gốc không tính tới
trường hợp "cả hai phía cùng không có gì, và đó chính là trạng thái đúng".

`frontend-expert`, khi dựng `scripts/fe-gate.sh` ở B0/F0, phát hiện: áp nguyên văn lệnh trên vào script của
**chính repo Core** sẽ làm cổng đỏ **vĩnh viễn** ngay tại repo mà cổng này được viết ra để sau đó phân phối cho
dự án hạ nguồn qua clone ([ADR-0016](0016-phan-phoi-core-bang-clone.md)). Thay vì tự quyết, agent đó cố ý bỏ mục
F4 khỏi script, ghi lý do bằng comment tại chỗ, và leo thang cho `architect` quyết — đúng quy trình.

## Quyết định

> **Cổng F4 luôn so sánh HAI TẬP tên (tập thư mục thật với mảng `BUSINESS_MODULES`), và một cặp tập RỖNG khớp
> nhau được coi là PASS — không phải điều kiện lỗi phải chặn trước khi so sánh.** Thư mục `modules/` vắng mặt
> được đọc là "tập thư mục rỗng", không phải "không có gì để đối chiếu".

Định nghĩa gốc ở [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §4.4 sửa theo hướng này. Đúng
MỘT bản script, không rẽ nhánh theo "đây có phải repo Core hay không" — script chạy giống hệt nhau ở repo Core
(mãi mãi 0-0, mãi mãi PASS) và ở dự án hạ nguồn (so khớp thật ngay khi module đầu tiên có thư mục).

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Bỏ hẳn mục F4 khỏi `scripts/fe-gate.sh` của repo Core; chỉ bật ở dự án hạ nguồn

Đây là điều `frontend-expert` đã tạm làm trong lúc chờ quyết định.

**Được:** đơn giản, code Core không chứa logic không có gì để dùng.

**Vì sao loại:** `scripts/fe-gate.sh` là chính tệp được phân phối bằng clone theo ADR-0016, và ADR-0016 luật 1 cấm
dự án hạ nguồn sửa tệp thuộc Core. Nếu bản Core của script không có mục F4, dự án hạ nguồn nhận về một script
thiếu mục đó — muốn bật lại thì **phải sửa đúng tệp mà ADR-0016 cấm sửa**, hoặc phải nâng thay đổi lên Core rồi
kéo về (đường hợp lệ duy nhất của ADR-0016), nhưng khi đó lại quay về câu hỏi ban đầu: script ở Core có chạy được
F4 hay không. Phương án này không giải quyết vấn đề, nó chỉ dời vấn đề sang đúng chỗ ADR-0016 cấm sửa. Nó cũng đòi
một tín hiệu đáng tin để phân biệt "tôi đang chạy ở repo Core" với "tôi đang chạy ở dự án hạ nguồn" — không có tín
hiệu nào tồn tại ngoài chính câu hỏi "thư mục `modules/` có hay không", tức lại vòng về đúng chỗ xuất phát.

### Phương án B — Tạo sẵn thư mục `modules/` rỗng (kèm `.gitkeep`) ở repo Core để `-d` luôn đúng

**Được:** không phải sửa logic cổng.

**Vì sao loại:** [`../wiki-core/fe/trien-khai/01-f0-nen-mong.md`](../wiki-core/fe/trien-khai/01-f0-nen-mong.md) §2
đã cấm tường minh việc này — Git không theo dõi thư mục rỗng, nên nó biến mất ở lần clone kế tiếp và cổng lại đỏ
đúng lúc không ai đang sửa gì. Tạo thư mục giả để né một cổng là dựng thêm một trạng thái giả cho một vấn đề mà
gốc rễ nằm ở chính logic cổng.

### Phương án C — Sửa logic F4 để tập rỗng khớp tập rỗng là hợp lệ (đã chọn)

**Được:** một script duy nhất, đúng ở mọi repo dùng chung nó qua clone; không cần tín hiệu phân biệt repo nào;
không thêm trạng thái giả nào vào cây thư mục.

**Mất:** xem Hệ quả tiêu cực.

## Hệ quả

### Tích cực

- Đúng MỘT bản `scripts/fe-gate.sh`, không rẽ nhánh theo repo — khớp tinh thần ADR-0016 (Core phân phối bằng
  clone, dự án hạ nguồn không sửa tệp thuộc Core).
- F4 tự động "bật thật" đúng lúc dự án hạ nguồn tạo module đầu tiên, không cần thao tác gỡ cờ hay bật lại cổng nào
  — nhất quán với cách F2 đã được thiết kế cho cùng vấn đề (danh sách rỗng ⇒ chưa có gì để ép, không phải lỗi).
- Không thêm trạng thái giả (thư mục rỗng, biến môi trường đánh dấu repo) vào cây thư mục hay cấu hình.

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| Làm mềm một phần nguyên tắc "0 phần tử là đỏ, không phải xanh" ([`../wiki-core/fe/trien-khai/05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) §8, mục 2; cùng tinh thần T6) | Đây phải là **ngoại lệ có tên**, đúng một chỗ — allowlist của F3/F5/F11 vẫn phải đỏ khi đọc ra 0 phần tử, vì với chúng rỗng luôn là dấu hiệu nguồn đọc hỏng, không phải trạng thái nghiệp vụ hợp lệ. Nới nguyên tắc này cho các cổng khác là sai |
| F4 "mù theo thiết kế" trong suốt vòng đời của repo Core — cổng không bao giờ có cơ hội đỏ ở đây | Một cổng luôn xanh trông giống hệt cổng mù nếu người đọc không biết lý do. Phải ghi chú tại chỗ trong `scripts/fe-gate.sh` và trong [`05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) §8.2 để không ai tưởng nhầm "F4 chưa từng đỏ" là dấu hiệu cổng hỏng |
| Trách nhiệm chứng minh F4 không mù dồn hết vào dự án hạ nguồn đầu tiên | Đúng lúc thêm module đầu tiên, người thi công phải tự làm canary thật (cố ý gỡ tên khỏi `BUSINESS_MODULES` hoặc xoá thư mục, xác nhận đỏ, rồi hoàn nguyên) — mục này đã có sẵn trong định nghĩa hoàn thành của [`../wiki-core/fe/trien-khai/00-lo-trinh-tong-the.md`](../wiki-core/fe/trien-khai/00-lo-trinh-tong-the.md) §4.1, nhưng rủi ro bị bỏ qua tăng lên vì trạng thái mặc định giờ là PASS thay vì FAIL |

### Điều kiện lật quyết định

Nếu ở dự án hạ nguồn đầu tiên, F4 báo PASS trong khi thực tế đang có một ca lệch thật (module bị xoá thư mục
nhưng tên còn trong `BUSINESS_MODULES`, hoặc ngược lại) — tức phép so khớp hai tập bị chứng minh sai — thì sửa lại
đúng logic so khớp đó. **Không quay lại phương án `exit 1` khi thiếu thư mục**: ca hỏng đó nằm ở phép so sánh, còn
việc thư mục `modules/` vắng mặt tại repo Core vẫn là một trạng thái hợp lệ cần được PASS.

## Liên quan

- [ADR-0032](0032-module-mau-o-du-an-ha-nguon.md) — vì sao repo Core không bao giờ có `modules/`
- [ADR-0016](0016-phan-phoi-core-bang-clone.md) — cơ chế clone và luật không sửa tệp thuộc Core
- [`../wiki-core/fe/trien-khai/00-lo-trinh-tong-the.md`](../wiki-core/fe/trien-khai/00-lo-trinh-tong-the.md) §4, §4.1 — phạm vi "chỉ Core" và định nghĩa hoàn thành của module mẫu
- [`../wiki-core/fe/trien-khai/01-f0-nen-mong.md`](../wiki-core/fe/trien-khai/01-f0-nen-mong.md) §3.1, §3.2 — cách F2 xử lý cùng dạng vấn đề, và lý do F4 ra đời
- [`../wiki-core/fe/trien-khai/05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) §8.2 — bảng trạng thái hỏng của F4, cập nhật theo ADR này
