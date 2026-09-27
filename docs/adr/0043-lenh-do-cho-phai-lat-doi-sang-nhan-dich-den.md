---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0043 — Lệnh dò "chỗ phải lật" của ADR-0030 đổi đầu vào từ chữ "giai đoạn 1" sang **nhãn `📐 ĐÍCH ĐẾN`**, và tách làm hai tầng; không thêm khoá frontmatter mới

> **Trạng thái:** Đã chấp nhận (2026-09-20) · Bổ sung [`0030-dieu-kien-chuyen-giai-doan-2.md`](0030-dieu-kien-chuyen-giai-doan-2.md) · Bổ sung bởi [ADR-0045](0045-cho-src-sua-dinh-nghia-khong-them-ky-hieu-thu-tu.md) (2026-09-20) — tầng B cố ý không phủ [`../RULES.md`](../RULES.md); ADR-0045 lo riêng cột *Chặn bởi* của §10 tệp đó, hai tầng giữ nguyên · Bổ sung bởi [ADR-0069](0069-tach-so-no-khoi-rules-md.md) (2026-09-23) — danh sách loại trừ của tầng B nhận thêm `docs/DEBT.md`, nơi sổ nợ chuyển tới
>
> **Bổ sung 0030.** Tám điều kiện, bảng *Chỗ phải lật*, mục *Không lật* và mục *Chuyển chủ sang code* của 0030 giữ nguyên hiệu lực. ADR này chỉ thay **khối lệnh** ở cuối mục *Chỗ phải lật* — đoạn 0030 dùng để tìm "các chỗ còn lại nhắc giai đoạn 1".

## Bối cảnh

Ngày 2026-09-16, `architect` lật nhãn giai đoạn theo bảng *Chỗ phải lật* của [ADR-0030](0030-dieu-kien-chuyen-giai-doan-2.md). Năm chỗ liệt kê tay trong bảng đó đã lật xong. Bước sau bảng — *"Các chỗ còn lại nhắc giai đoạn 1 **không** liệt kê tay. Tìm bằng lệnh, mở từng file, quyết lật hay giữ"* — **dừng giữa chừng**, và đợt review BE ngày 2026-09-20 tìm ra năm nhóm tài liệu vẫn khẳng định `src/` không tồn tại trong khi code đã có trên đĩa.

Nguyên nhân là **chính khối lệnh đó**. Nó dò chuỗi `giai đoạn 1\|Giai đoạn 1`. Chạy lại ngày 2026-09-20 nó in 20 file — và bỏ sót đúng những file sai nặng nhất, vì chúng không chứa chuỗi đó:

| File bị bỏ sót | Nó viết gì thay vì "giai đoạn 1" |
| --- | --- |
| [`../database/migration-policy.md`](../database/migration-policy.md) | *"Chưa có `src/`, chưa có migration nào tồn tại"* — trong khi thư mục `Migrations/` có 4 migration kèm `CoreDbContextModelSnapshot.cs`, và `database/scripts/core/` có 4 script |
| [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) và bốn file `be-*.md` cạnh nó | *"`src/BE` **phải trở thành** ở giai đoạn 2"* / *"sẽ xây ở giai đoạn 2"* — nhắc **giai đoạn 2**, không nhắc giai đoạn 1 |

Nói cách khác: lệnh dò theo **chữ chỉ giai đoạn**, còn thứ hỏng là **lời khẳng định về hiện trạng**. Hai tập đó chỉ giao nhau một phần, và phần không giao chính là phần nguy hiểm — một file nói "sẽ xây ở giai đoạn 2" đang khẳng định hiện trạng mạnh y như một file nói "giai đoạn 1 chưa có `src/`", nhưng lệnh cũ mù với nó.

Ba phép đo chạy ngày 2026-09-20 trên repo này, dùng để chọn phương án:

| Đầu vào dò | Số file in ra |
| --- | --- |
| `giai đoạn 1\|Giai đoạn 1` (lệnh cũ) | 20 |
| Câu khẳng định `src/` vắng mặt (`chưa có \`src/\`` và biến thể) | 65 — vẫn **bỏ sót** cả năm file `be-*.md` |
| Nhãn `📐 ĐÍCH ĐẾN` | 161 — **chứa** cả năm file `be-*.md` |

Nhãn `📐 ĐÍCH ĐẾN — CHƯA THI CÔNG` là một trong ba nhãn trạng thái mà [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §4 bắt buộc. Nó là **từ vựng có kiểm soát**, không phải văn xuôi tự do: mọi file khẳng định "chưa thi công" đều buộc phải mang đúng nhãn này. Đó là dấu hiệu máy đọc được **đã tồn tại sẵn** — lệnh cũ chỉ không dùng nó.

## Quyết định

1. **Khối lệnh ở cuối mục *Chỗ phải lật* của ADR-0030 thay bằng khối hai tầng ghi ở mục *Lệnh thay thế* dưới đây.** Đầu vào của nó là nhãn `📐 ĐÍCH ĐẾN`, không phải chữ "giai đoạn 1".
2. **Tầng A là tập giao "mâu thuẫn chứng minh được": file mang `📐 ĐÍCH ĐẾN` **và** được một file mã nguồn thật dưới `src/` hoặc `database/` trích dẫn.** Một file mã trích dẫn tài liệu làm chuẩn trong khi tài liệu đó tự khai là chưa ai viết — đó là mâu thuẫn máy chứng minh được, không phải phỏng đoán. Tầng A làm trước.
3. **Tầng B là toàn bộ file mang `📐 ĐÍCH ĐẾN`** — tồn đọng đầy đủ, làm nhiều lượt.
4. **Cả hai tầng in ra *danh sách phải mở đọc*, không phải *danh sách phải sửa*.** Giữ nguyên nguyên tắc của ADR-0030:75 — một câu nói về giai đoạn 1 như **một mốc đã qua** thì giữ được, và một file mô tả phần code **thật sự chưa có** (`src/FE/modules/` chẳng hạn) thì giữ nguyên `📐` là đúng.
5. **Không thêm khoá frontmatter thứ tư.** Ba khoá `kind` / `scope` / `verified` giữ nguyên.

### Lệnh thay thế

```bash
# PASS khi in ra DANH SÁCH PHẢI MỞ ĐỌC. Rỗng nghĩa là đã dọn hết, không phải "không có việc".
[ -d src ] || { echo "NOTE: chưa có src/ — mục này không xét gì (T6)"; exit 0; }

# (1) file docs/ còn mang nhãn 📐 ĐÍCH ĐẾN — TẦNG B, tồn đọng đầy đủ
grep -rl '📐 \*\*ĐÍCH ĐẾN\|📐 ĐÍCH ĐẾN' docs --include='*.md' \
  | grep -v '^docs/adr/\|^docs/audit/\|^docs/00-overview/\|^docs/RULES.md$' \
  | sort -u > /tmp/f7-dichden.txt

# (2) file docs/ mà mã nguồn thật trích dẫn; lọc bỏ đường dẫn không tồn tại trong repo này
grep -rhIo --include='*.cs' --include='*.ts' --include='*.html' --include='*.scss' \
     --include='*.sql' --include='*.slnx' --include='*.props' \
     --exclude-dir=node_modules --exclude-dir=bin --exclude-dir=obj --exclude-dir=dist \
     'docs/[A-Za-z0-9_./-]*\.md' src/ database/ 2>/dev/null \
  | sort -u | while read -r p; do [ -f "$p" ] && echo "$p"; done > /tmp/f7-cited.txt

echo "== TẦNG A — mâu thuẫn chứng minh được, làm trước =="
comm -12 /tmp/f7-dichden.txt /tmp/f7-cited.txt
echo "== TẦNG B — tồn đọng đầy đủ =="
cat /tmp/f7-dichden.txt
```

Bốn điều kiện loại trừ ở bước (1) giữ nguyên lý do của ADR-0030: `adr/` và `audit/` là bản ghi lịch sử, `00-overview/` mang `kind: lich-su`, và [`../RULES.md`](../RULES.md) nằm ở mục *Không lật* của chính 0030 — mỗi dòng `📐` của nó chuyển trạng thái khi cổng **của chính nó** chạy.

Bốn thư mục loại trừ ở bước (2) (`node_modules`, `bin`, `obj`, `dist`) là bắt buộc, không phải tối ưu: chạy thử không có chúng, lệnh in 310 đường dẫn thay vì 68 — phần lớn là tài liệu **của thư viện bên thứ ba**, vốn cũng nằm dưới một thư mục tên `docs/` nên khớp đúng mẫu. Bước lọc `[ -f "$p" ]` là lớp chặn thứ hai cho cùng loại nhiễu: đường dẫn không tồn tại trong repo này thì bị bỏ.

### Đã chạy thử

Ngày 2026-09-20, trên repo này — **số đo tại thời điểm ra quyết định, không phải hiện trạng**: tầng A in **33** file, tầng B in **161** file. Cả hai khác rỗng — kỷ luật **T6** ([`../RULES.md`](../RULES.md)) đòi chứng minh tập đầu vào khác rỗng, vì một lệnh in rỗng vì hỏng và một lệnh in rỗng vì đã sạch trông giống hệt nhau.

Tầng A bắt được cả bốn nhóm tài liệu mà đợt review BE ngày 2026-09-20 tìm ra bằng tay: `migration-policy.md`, `contracts/README.md`, bốn trong năm file `quy-uoc/be-*.md`, và hai file `wiki-core/be/`.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Giữ cách dò văn xuôi, chỉ nới mẫu cho rộng ra

Thêm `chưa có \`src/\`` và `giai đoạn 2` vào mẫu cũ rồi lọc tay.

**Được:** sửa đúng một dòng, không đụng gì khác. Đây là hướng thứ nhất mà lượt review đề xuất.

**Vì sao loại:** đã đo, và nó **vẫn mù đúng chỗ cũ**. Mẫu `chưa có \`src/\`` in 65 file nhưng bỏ sót cả năm file `quy-uoc/be-*.md` — chính nhóm mà lượt review chấm nặng nhất, vì `ApiEnvelope.cs` và `ResultToHttpMapper.cs` khớp từng field với `be-api-controller.md`. Thêm `giai đoạn 2` vào để vá chỗ đó thì tập kết quả nhảy lên 82 file **và** kéo theo dương tính giả thật sự: mọi file lộ trình nhắc "giai đoạn 2" như một mốc kế hoạch. Nới một mẫu văn xuôi để bắt thêm một khuôn câu là trò đuổi bắt không có điểm dừng — khuôn câu thứ ba sẽ xuất hiện, và lần sau không ai biết nó đã bị bỏ sót.

### Phương án B — Khai một khoá frontmatter mới, kiểu `giai-doan:` hoặc `da-doi-chieu-src:`

**Được:** dấu hiệu máy đọc được, tường minh, không phụ thuộc cách hành văn. Đây là hướng thứ hai mà lượt review đề xuất, và lượt review nói đúng rằng nó bền hơn việc dò văn xuôi.

**Vì sao loại:** ba lý do cộng lại.

Thứ nhất, **nó là bản sao của một thứ đã có chủ.** Nhãn `📐` / `🚧` / `✅` ở đầu file đã trả lời đúng câu hỏi đó rồi, và [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §4 đã bắt buộc nó. Thêm một khoá nữa là tạo nguồn thứ hai cho cùng một sự thật — đúng thứ §5 tồn tại để ngăn, và hai nguồn sẽ lệch nhau vì người sửa banner không nhớ sửa frontmatter.

Thứ hai, **nó là lời tự khai, và không gì kiểm được lời tự khai.** Đây đúng cơ chế hỏng mà §9 đã cảnh báo cho `verified: khong-ap-dung`: một khoá tự nhận là cách nhanh nhất làm con số đẹp lên mà không kiểm gì. Tầng A của phương án đã chọn thì ngược lại — nó đối chiếu **hai nguồn độc lập** (nhãn trong tài liệu, trích dẫn trong mã nguồn) nên không file nào tự khai mình sạch được.

Thứ ba, **chi phí trả trước cho một việc xảy ra một lần.** Khoá mới phải điền cho 161 file, cần một mục cổng mới canh, và tồn tại vĩnh viễn — trong khi việc nó phục vụ là một lần chuyển giai đoạn.

### Phương án C — Chỉ dùng tầng A, bỏ tầng B

**Được:** 33 file là danh sách làm được trong một lượt; báo cáo trông gọn.

**Vì sao loại:** tầng A **cố ý** hẹp. Nó chỉ thấy file mà mã nguồn có trích dẫn, nên nó bỏ sót [`../quy-uoc/tieu-chi-review.md`](../quy-uoc/tieu-chi-review.md) và [`../README.md`](../README.md) — hai file mà lượt review chấm là finding thật. Bỏ tầng B là lặp lại **đúng lỗi của lệnh cũ**: một lệnh in ra con số nhỏ dễ chịu rồi khiến người chạy tin rằng đã xong. Lệnh cũ in 20 và đợt lật đã dừng giữa chừng vì tin vào con số đó.

### Phương án D — Sửa thẳng khối lệnh trong ADR-0030

**Vì sao loại:** [`README.md`](README.md) §5 cấm sửa nội dung ADR cũ, và ADR-0030 đã có tiền lệ được bổ sung đúng cách bởi [ADR-0035](0035-hoan-dieu-kien-3-den-pr-cham-core-dau-tien.md). Sửa đè thì sáu tháng sau không còn gì cho biết lệnh cũ đã mù ở đâu — mà chính chỗ mù đó là bài học đắt nhất của lượt này.

## Hệ quả

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| **Tồn đọng thật lớn hơn nhiều lần con số cũ, và nay nhìn thấy được** | Đừng chép số vào đây — chạy khối lệnh ở mục *Lệnh thay thế*; tiêu chí đọc: tầng B dài hơn tầng A nhiều lần, và cả hai dài hơn hẳn 20 file mà lệnh cũ in ra. Lệnh này không làm việc nặng thêm — nó cho thấy việc vốn đã nặng. Nhưng hệ quả vận hành có thật: đợt lật không đóng được trong một lượt, và bất kỳ ai chạy lệnh cũng sẽ thấy một danh sách dài chưa đóng |
| **Tầng B nhiễu cao, và nhiễu đó không giảm được bằng máy** | Phần lớn file trong tầng B mang `📐` **đúng** — `src/FE/modules/` chưa tồn tại thật. Phân biệt "nhãn sai" với "nhãn đúng" đòi người mở file đọc. Lệnh không thay được việc đó, chỉ thu hẹp chỗ phải nhìn |
| **Tầng A phụ thuộc thói quen trích dẫn tài liệu trong mã nguồn** | Nó chỉ thấy file được một chú thích trong `src/` nhắc tên. Thói quen đó **không có cổng nào ép** (§10 của cổng chỉ kiểm chú thích đã có trỏ đúng chỗ, không ép phải có chú thích). Mã nguồn viết không trích dẫn thì tầng A không thấy, và nó lặng lẽ hẹp lại mà không báo gì |
| **Tầng A chạy trên cây thư mục, nên vô hiệu trên checkout CI sạch** | `src/`, `database/` hiện **chưa vào git**. Trên một checkout sạch, nhánh `[ -d src ]` cho ra `NOTE`, tầng A rỗng. Cùng nguyên nhân với điểm (a) của **D39** ở [`../RULES.md`](../RULES.md). Đây là lệnh chạy tay, không phải cổng, nên hôm nay chưa gây hại — nhưng nếu sau này có người nâng nó thành mục cổng mà quên điều này thì nó im lặng PASS |
| **Một file có thể đã lật đúng mà vẫn nằm trong danh sách** | [`../database/schema-core.md`](../database/schema-core.md) đã lật sang `🚧` ở đầu file nhưng vẫn in ra ở tầng A, vì các **mục** bên trong còn giữ `📐` một cách có chủ đích. Đúng như thiết kế — đây là *danh sách phải mở đọc* — nhưng nó nghĩa là danh sách không bao giờ co về rỗng một cách gọn gàng |

### Tích cực

- Đầu vào của lệnh là **từ vựng nhãn có kiểm soát** mà §4 đã bắt buộc, không phải cách hành văn tự do của từng người viết.
- Tầng A đối chiếu hai nguồn độc lập, nên nó phát hiện mâu thuẫn chứ không nhận lời tự khai.
- Không có khoá frontmatter thứ tư, không có nguồn sự thật thứ hai về trạng thái file.
- Lệnh tự khai là không xét gì khi chưa có `src/`, thay vì im lặng in rỗng (kỷ luật **T6**).

### Điều kiện lật quyết định

1. **Tầng A bắt đầu trả về rỗng trong khi tầng B vẫn dài.** Nghĩa là thói quen trích dẫn tài liệu trong mã nguồn đã mất, và tầng A hết tác dụng phân loại. Lúc đó cần một dấu hiệu khác, không phải nới mẫu.
2. **Có người nâng khối lệnh này thành một mục cổng.** Lúc đó phải xử lý được cả cây có `src/` lẫn cây không có, theo đúng **D39**; quyết định lại bằng ADR mới.
3. **Xuất hiện một nhãn trạng thái thứ tư ngoài ba nhãn của §4.** Đầu vào của lệnh đổi theo, và ADR này viết lại.

## Liên quan

- [`0030-dieu-kien-chuyen-giai-doan-2.md`](0030-dieu-kien-chuyen-giai-doan-2.md) — mục *Chỗ phải lật*, khối lệnh mà ADR này thay
- [`0035-hoan-dieu-kien-3-den-pr-cham-core-dau-tien.md`](0035-hoan-dieu-kien-3-den-pr-cham-core-dau-tien.md) — tiền lệ bổ sung 0030 bằng ADR mới
- [`../RULES.md`](../RULES.md) — **T6** (tập đầu vào khác rỗng), **D34** (khối lệnh khai tiêu chí PASS), **D39** (lệnh dò `src/` trên cây chưa vào git)
- [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §4 (ba nhãn trạng thái) · §9 (ba khoá frontmatter)
