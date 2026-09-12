---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `adr/` — sổ ghi quyết định kiến trúc

> **Khu này trả lời đúng một câu hỏi:** *vì sao lại thế này chứ không thế kia?*
>
> Mọi khu khác trong [`../README.md`](../README.md) mô tả hệ thống **phải trở thành cái gì**. Khu này giữ **lý do**. Đó là thứ mất nhanh nhất: sáu tháng sau, quy ước còn nằm trong file, nhưng những phương án đã cân nhắc và bị loại thì không ai nhớ — và người mới sẽ đề xuất lại đúng phương án đã bị loại, với đúng lập luận đã bị bác.

📖 Nguyên tắc chung về thực hành ADR: [`../wiki-core/be/08-adr-practice.md`](../wiki-core/be/08-adr-practice.md)

---

## 1. ADR là gì

Một **Architecture Decision Record** là bản ghi **một** quyết định kiến trúc, viết tại thời điểm quyết định, kèm bối cảnh lúc đó và các phương án đã bị loại.

Ba tính chất khiến nó khác một trang tài liệu thường:

| Tính chất | Nghĩa là |
| --- | --- |
| **Bất biến** | Đã viết thì không sửa nội dung. Đổi ý thì viết bản ghi mới. |
| **Có thời điểm** | Nó đúng với bối cảnh ngày viết, không tự nhận là đúng vĩnh viễn. |
| **Có phương án bị loại** | Phần giá trị nhất không phải cái đã chọn, mà là cái đã bỏ và vì sao. |

> Một ADR chỉ ca ngợi phương án đã chọn là một ADR chưa suy nghĩ đủ. Nếu không viết được mục "Hệ quả tiêu cực", nghĩa là chưa hiểu cái giá mình vừa trả.

## 2. Khi nào viết một ADR mới

Viết khi quyết định có **ít nhất một** trong các dấu hiệu sau:

- **Đắt để đảo ngược.** Đổi lại sau này tốn tuần, không tốn giờ.
- **Chạm ranh giới.** Nó thay đổi cái gì thuộc Core, cái gì thuộc module, hoặc chiều phụ thuộc giữa các tầng.
- **Có phương án thay thế hợp lý.** Có người tỉnh táo sẽ chọn khác — cần ghi vì sao ta không chọn.
- **Trái với thói quen ngành.** Người mới đọc code sẽ tưởng là sai sót và "sửa lại cho đúng".
- **Lật một ADR cũ.**

### Khi nào KHÔNG viết

| Tình huống | Ghi ở đâu thay vì ADR |
| --- | --- |
| Quy ước đặt tên, thứ tự tham số, kiểu định dạng | [`../quy-uoc/`](../quy-uoc/) |
| Chọn một thư viện tương đương với thư viện khác, đổi lại trong một buổi | Không cần ghi |
| Kể lại một sự cố đã xảy ra | [`../audit/`](../audit/) |
| Một luật cần được ép bằng cổng | [`../RULES.md`](../RULES.md) — và ADR chỉ giải thích vì sao có luật đó |
| Kế hoạch, lộ trình, ước lượng | [`../00-overview/`](../00-overview/) |

**Phép thử nhanh:** nếu sáu tháng nữa không ai hỏi *"vì sao hồi đó lại làm thế?"* thì đó không phải ADR.

## 3. Khuôn chuẩn — năm mục, không thiếu mục nào

| Mục | Phải trả lời | Bẫy thường gặp |
| --- | --- | --- |
| **Bối cảnh** | Lúc quyết định, hoàn cảnh ra sao? Ràng buộc nào có thật? | Viết bối cảnh của hôm nay thay vì của lúc đó |
| **Quyết định** | Chọn gì, ở dạng câu khẳng định ngắn | Viết lửng lơ kiểu "nên cân nhắc" — ADR không có "nên" |
| **Phương án đã cân nhắc và vì sao loại** | Từng phương án: được gì, mất gì, **vì sao loại** | Dựng bù nhìn: mô tả phương án bị loại yếu đi để phương án đã chọn thắng dễ |
| **Hệ quả** | Cả **tích cực lẫn tiêu cực**. Cái giá phải trả là gì | Chỉ liệt kê lợi ích |
| **Trạng thái** | `Đã chấp nhận (YYYY-MM-DD)` · `Đã thay thế bởi ADR-NNNN (YYYY-MM-DD)` · `Đã loại bỏ (YYYY-MM-DD)` | Để trống, hoặc không kèm ngày |

Ba khoá frontmatter theo [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §9: `kind: quyet-dinh` · `scope: core` · `verified: chua-doi-chieu`.

## 4. Đánh số và đặt tên file

```text
NNNN-slug-tieng-viet-khong-dau.md
```

| Quy tắc | Chi tiết |
| --- | --- |
| Số | Bốn chữ số, tăng dần, **không bao giờ dùng lại** kể cả khi một ADR bị loại bỏ |
| Slug | Chữ thường, không dấu, nối bằng gạch ngang, mô tả **quyết định** chứ không mô tả chủ đề |
| Đổi tên | **Không.** Tên file đã có người link tới; đổi tên là làm gãy link |
| Số tiếp theo | Đếm bằng lệnh, không nhớ bằng đầu |

```bash
ls docs/adr/[0-9]*.md | tail -1        # ADR mới nhất → số tiếp theo
grep -l 'Đã thay thế bởi' docs/adr/*.md # các quyết định đã bị lật
```

Slug tốt: `0005-permission-based`. Slug tệ: `0005-phan-quyen` — nó nói chủ đề, không nói quyết định, nên không phân biệt được với một ADR khác cũng về phân quyền.

## 5. Cách lật một quyết định cũ

🛑 **KHÔNG sửa ADR cũ.** Kể cả khi quyết định trong đó đã sai hoàn toàn.

Quy trình đúng, ba bước:

1. Viết **ADR mới** với số tiếp theo. Mục "Bối cảnh" của nó nói rõ: quyết định nào đang bị lật, và **cái gì đã đổi** khiến lập luận cũ không còn đúng.
2. Ở ADR cũ, sửa **duy nhất dòng Trạng thái** thành `Đã thay thế bởi ADR-NNNN (YYYY-MM-DD)`, kèm một dòng link. Không đụng vào phần nội dung.
3. Cập nhật bảng mục lục ở file này.

**Vì sao nghiêm khắc thế:** ADR là bản ghi lịch sử. Sửa nội dung nó là **xoá mất lý do người ta từng nghĩ thế** — và mất luôn khả năng học từ chỗ lập luận cũ hỏng. Một ADR bị lật vẫn còn giá trị: nó cho biết phương án nào đã thử, thất bại ở đâu, và điều kiện nào đã đổi. Sửa đè lên thì lần sau có người lại đề xuất đúng phương án đó, và không còn gì để đối chiếu.

Hệ quả cần chấp nhận: đọc khu này phải đọc theo **trạng thái**, không đọc theo số thứ tự. Một ADR mang trạng thái "Đã thay thế" vẫn nằm nguyên trong thư mục — đó là chủ ý, không phải rác chưa dọn.

## 6. Mục lục

Trạng thái và ngày chấp nhận của từng ADR nằm ở **chính file ADR đó** — không chép sang bảng này ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §6). Đếm và soát bằng lệnh:

```bash
grep -H '^> \*\*Trạng thái:\*\*' docs/adr/[0-9]*.md
```

| ADR | Quyết định một dòng |
| --- | --- |
| [0001](0001-modular-monolith.md) | Một solution, một process, N module — Modular Monolith, không microservices, không monolith phẳng |
| [0002](0002-core-5-project.md) | Core tách thành `Domain` · `Application` · `Infrastructure` · `Web` · `Contracts`; host chỉ là composition root |
| [0003](0003-result-thuan.md) | Domain và Application trả `Result<T>` cho lỗi nghiệp vụ; exception chỉ dành cho lỗi ngoài dự kiến |
| [0004](0004-giu-aspnet-identity.md) | Giữ ASP.NET Core Identity, khoanh `AppUser`/`AppRole` trong `Core.Infrastructure` |
| [0005](0005-permission-based.md) | Không hằng số role trong Core — phân quyền bằng permission, role là dữ liệu trong DB |
| [0006](0006-pipeline-behavior.md) | Đúng hai pipeline behavior ở v1: `Validation` và `Transaction` |
| [0007](0007-fe-giu-cau-truc-thu-muc.md) | FE giữ `core/ shared/ platform/ modules/` là thư mục; ranh giới ép bằng ESLint + cấm `eslint-disable` |
| [0008](0008-core-so-huu-migration.md) | Core sở hữu migration của schema `core`; module sở hữu migration của schema mình |
| [0009](0009-ap-schema-chay-tay.md) | Không auto-migrate — sinh script cho người vận hành, kèm cơ chế chặn khởi động khi DB lệch model |
| [0010](0010-comment-toi-thieu.md) | Comment trong code tối thiểu; lịch sử sự cố và lý do sâu sống ở [`../audit/`](../audit/) và khu này |
| [0011](0011-ci-github-actions.md) | Có CI chạy đủ cổng trên mỗi PR — cổng chạy tay là cổng có thể bị bỏ |
| [0012](0012-giai-doan-1-chi-docs.md) | Giai đoạn 1 chỉ xây `docs/` + `.claude/`; `src/` làm sau và bám theo tài liệu |
| [0013](0013-multi-tenant.md) | Multi-tenant bằng cột phân biệt — nhiều cơ quan chung một bản cài, cách ly tuyệt đối, một tài khoản một tenant |
| [0014](0014-mot-instance-key-ring-postgres.md) | Chạy một instance; bộ khoá bảo vệ dữ liệu để trong PostgreSQL; chưa dùng Redis |
| [0017](0017-khu-quan-tri-he-thong.md) | Khu quản trị hệ thống: tài khoản vận hành thấy **danh sách đơn vị**, không thấy dữ liệu nghiệp vụ của đơn vị nào |
| [0016](0016-phan-phoi-core-bang-clone.md) | Dự án hạ nguồn lấy Core bằng **clone**, phiên bản bằng **tag**; dự án không sửa tệp thuộc `Core/` |
| [0015](0015-fe-va-api-khac-nguon.md) | FE và API phục vụ ở **hai origin khác nhau** ở mọi môi trường; dev bật CORS và HTTPS như thật, không dùng proxy dev để giấu ranh giới |
| [0018](0018-thang-trung-tinh-sang-va-theme-mac-dinh.md) | Thang trung tính sáng nhấc lên một bậc; viền chỉ nhạt được tới `#7f8da3` vì ngưỡng chặn là viền **trên nền trang**; theme mặc định là `light`, không theo hệ điều hành |
| [0019](0019-ba-component-nang-thuoc-core.md) | Biểu đồ, lưới nhập liệu và chọn khoảng ngày **thuộc Core** — lật bốn luật cũ. Kèm ba ràng buộc: thư viện biểu đồ không vào bundle khởi động, cả ba nằm ở nhánh tải chậm, và không mang từ vựng nghiệp vụ |
| [0020](0020-seed-dev-ba-dieu-kien-va-dau-nhan-dang.md) | Seed dev đi bằng **lệnh riêng**, chạy chỉ khi **ba điều kiện** cùng đúng; hàng dữ liệu nó tạo mang định danh cố định mà môi trường thật **từ chối khởi động** khi thấy; chính sách mật khẩu **không đổi theo môi trường** |
| [0021](0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md) | `has_permission_bypass` và `is_system_operator` là **hai cột boolean loại trừ nhau**, không phải một enum; đơn vị hệ thống nhận ra bằng cột `is_system`, không bằng định danh cố định |
| [0022](0022-seed-dev-khong-co-duong-code-rieng.md) | **Lật [0020](0020-seed-dev-ba-dieu-kien-va-dau-nhan-dang.md).** Không có đường code nào chứa sẵn thông tin tài khoản: một tệp `.sql` trong `src/BE/` lo **dữ liệu** dev, **lệnh bootstrap đã có** lo tài khoản với mật khẩu từ `user-secrets`, một script bọc gọi cả hai |

## 7. Khuôn rỗng để chép

```markdown
---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-NNNN — <quyết định, viết ở dạng câu khẳng định>

> **Trạng thái:** Đã chấp nhận (YYYY-MM-DD)

## Bối cảnh

<Hoàn cảnh lúc quyết định. Ràng buộc có thật. Nếu có bằng chứng từ dự án
tiền nhiệm thì kể bằng văn xuôi, không trích dẫn dạng đường-dẫn-kèm-số-dòng.>

## Quyết định

<Một tới ba câu khẳng định. Không "nên", không "cân nhắc".>

## Phương án đã cân nhắc và vì sao loại

### Phương án A — <tên>
**Được:** … **Mất:** … **Vì sao loại:** …

## Hệ quả

### Tích cực
### Tiêu cực

## Liên quan
```

Ba lỗi hay gặp khi điền khuôn này:

1. **Mục "Phương án đã cân nhắc" chỉ có một phương án.** Nếu thật sự không có phương án nào khác, đây không phải quyết định — nó là ràng buộc, và ràng buộc thì ghi ở [`../RULES.md`](../RULES.md).
2. **Mục "Hệ quả tiêu cực" viết dưới dạng lợi ích trá hình** ("hơi tốn công lúc đầu nhưng về sau rất đáng"). Hệ quả tiêu cực phải là thứ ta thật sự mất và không lấy lại được bằng nỗ lực.
3. **Chép nội dung của một file `quy-uoc/` vào ADR.** ADR nói *vì sao*, `quy-uoc/` nói *làm thế nào*. Chép sang là tạo bản sao thứ hai — và bản sao thứ hai không bao giờ được sửa cùng lúc với bản gốc.

## 8. Quan hệ giữa ADR và luật có cổng

Một ADR **không tự nó ép được gì**. Nó chỉ giải thích. Phần ép nằm ở [`../RULES.md`](../RULES.md), cột *"Ép bằng gì"*.

| Vai | ADR | `RULES.md` |
| --- | --- | --- |
| Trả lời | *Vì sao chọn thế này* | *Vi phạm thì cái gì bắt được* |
| Thay đổi khi | Bối cảnh đổi → viết ADR mới | Có cổng mới, hoặc luật đổi mức |
| Đọc bởi | Người muốn lật quyết định | Cổng, và người viết code |

Nguyên tắc kèm theo: **mỗi khi một ADR sinh ra một luật, luật đó phải xuất hiện ở `RULES.md` kèm cột "Ép bằng gì".** Nếu chưa có cổng nào ép được, luật đó thuộc danh sách nợ ở §10 của file đó — nhìn thấy được, không lờ đi. Một quyết định kiến trúc không có cổng nào canh sẽ trôi trong vài tháng, và không ai biết lúc nào nó bắt đầu trôi.

## 9. Liên quan

| Đọc gì | Khi nào |
| --- | --- |
| [`../wiki-core/be/08-adr-practice.md`](../wiki-core/be/08-adr-practice.md) | Muốn hiểu thực hành ADR như một chuẩn chung, không riêng repo này |
| [`../RULES.md`](../RULES.md) | Muốn biết một quyết định ở đây được **ép bằng cổng nào** |
| [`../audit/`](../audit/) | Muốn biết một quyết định ở đây được sinh ra từ **sự cố nào** |
| [`../kien-truc-core-module.md`](../kien-truc-core-module.md) | Muốn thấy hình dạng cuối cùng mà các quyết định này hợp thành |
