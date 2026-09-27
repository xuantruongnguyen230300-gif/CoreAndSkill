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

Viết khi quyết định có **ít nhất một** trong các dấu hiệu sau (người dùng chốt 2026-09-27):

- **Đổi ranh giới tầng.** Cái gì thuộc Core, cái gì thuộc module, chiều phụ thuộc giữa các tầng.
- **Thêm phụ thuộc ngoài.** Thư viện, dịch vụ hay hạ tầng mới.
- **Đổi hợp đồng API hoặc DB.** Envelope, route, mã lỗi công khai, schema.
- **Đổi quy trình của repo.** Cổng, hook, cách giao việc cho agent.
- **Lật một ADR cũ.**

Không mang dấu hiệu nào ở trên là **quyết định nhỏ**: một dòng ở [`nhat-ky-quyet-dinh.md`](nhat-ky-quyet-dinh.md), rồi sửa thẳng tệp chủ của nội dung.

### Khi nào KHÔNG viết

| Tình huống | Ghi ở đâu thay vì ADR |
| --- | --- |
| Quyết định nhỏ — chọn giữa hai cách sửa, chốt câu chữ, chốt một giá trị | Một dòng ở [`nhat-ky-quyet-dinh.md`](nhat-ky-quyet-dinh.md) |
| Quy ước đặt tên, thứ tự tham số, kiểu định dạng | [`../quy-uoc/`](../quy-uoc/) |
| Chọn một thư viện tương đương với thư viện khác, đổi lại trong một buổi | Không cần ghi |
| Kể lại một sự cố đã xảy ra | [`../audit/`](../audit/) |
| Một luật cần được ép bằng cổng | [`../RULES.md`](../RULES.md) — và ADR chỉ giải thích vì sao có luật đó |
| Kế hoạch, lộ trình | [`../wiki-core/be/trien-khai/00-lo-trinh-tong-the.md`](../wiki-core/be/trien-khai/00-lo-trinh-tong-the.md) · [`../wiki-core/fe/trien-khai/00-lo-trinh-tong-the.md`](../wiki-core/fe/trien-khai/00-lo-trinh-tong-the.md) |

**Phép thử nhanh:** nếu sáu tháng nữa không ai hỏi *"vì sao hồi đó lại làm thế?"* thì đó không phải ADR.

## 3. Khuôn chuẩn — năm mục, không thiếu mục nào

| Mục | Phải trả lời | Bẫy thường gặp |
| --- | --- | --- |
| **Bối cảnh** | Lúc quyết định, hoàn cảnh ra sao? Ràng buộc nào có thật? | Viết bối cảnh của hôm nay thay vì của lúc đó |
| **Quyết định** | Chọn gì, ở dạng câu khẳng định ngắn | Viết lửng lơ kiểu "nên cân nhắc" — ADR không có "nên" |
| **Phương án đã cân nhắc và vì sao loại** | Từng phương án: được gì, mất gì, **vì sao loại** | Dựng bù nhìn: mô tả phương án bị loại yếu đi để phương án đã chọn thắng dễ |
| **Hệ quả** | Cả **tích cực lẫn tiêu cực**. Cái giá phải trả là gì | Chỉ liệt kê lợi ích |
| **Trạng thái** | `Đã chấp nhận (YYYY-MM-DD)` · `Đã thay thế bởi ADR-NNNN (YYYY-MM-DD)` · `Đã loại bỏ (YYYY-MM-DD)`. Khi một ADR sau chỉ đổi **một phần** hoặc **thêm vào**: `Đã chấp nhận (YYYY-MM-DD) · Sửa một phần bởi ADR-NNNN (YYYY-MM-DD)` · `Đã chấp nhận (YYYY-MM-DD) · Bổ sung bởi ADR-NNNN (YYYY-MM-DD)` — xem §5.1 | Để trống, hoặc không kèm ngày |

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
grep -l 'Sửa một phần bởi\|Bổ sung bởi' docs/adr/*.md # các quyết định còn hiệu lực nhưng đã có ADR sau chạm vào
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

### 5.1 Khi ADR mới không lật trọn ADR cũ

Không phải quyết định mới nào cũng xoá hiệu lực của quyết định cũ. Ba quan hệ, ba dòng trạng thái:

| Quan hệ | Nghĩa | Dòng trạng thái của ADR cũ |
| --- | --- | --- |
| **Thay thế** | Không phần nào của ADR cũ còn hiệu lực | `Đã thay thế bởi ADR-NNNN (YYYY-MM-DD)` |
| **Sửa một phần** | Một phần ADR cũ hết hiệu lực; phần còn lại vẫn đúng | `Đã chấp nhận (ngày cũ) · Sửa một phần bởi ADR-NNNN (YYYY-MM-DD)` |
| **Bổ sung** | ADR cũ vẫn đúng nguyên vẹn; ADR mới thêm vào nó | `Đã chấp nhận (ngày cũ) · Bổ sung bởi ADR-NNNN (YYYY-MM-DD)` |

Thủ tục vẫn là ba bước ở trên, với hai điều kiện cho hai quan hệ sau:

1. **ADR mới nói rõ phần nào của ADR cũ còn hiệu lực.** Người đọc ADR cũ không có cách nào khác để biết.
2. **ADR cũ chỉ đổi dòng trạng thái, kèm một dòng link.** Nội dung giữ nguyên — kể cả đoạn đã hết hiệu lực.

Ngày trong ngoặc sau `ADR-NNNN` là ngày chấp nhận của **ADR mới**.

## 6. Mục lục

Trạng thái và ngày chấp nhận của từng ADR nằm ở **chính file ADR đó** — không chép sang bảng này ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §6). Đếm và soát bằng lệnh:

```bash
grep -H '^> \*\*Trạng thái:\*\*' docs/adr/[0-9]*.md
```

> 📖 Bảng mục lục đầy đủ — một dòng mỗi ADR, cập nhật mỗi khi có ADR mới: đọc [`muc-luc.md`](muc-luc.md)

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
