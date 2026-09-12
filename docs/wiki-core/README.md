---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `wiki-core/` — kiến thức nền: một Core tốt gồm gì và vì sao

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Repo đang ở giai đoạn 1, chưa có `src/`. Mọi mô tả kỹ thuật trong khu này là thứ Core **phải trở thành**, không phải mô tả hiện trạng.

---

## 1. Vai của khu này

Khu `wiki-core/` trả lời đúng một câu hỏi:

> **Một Core tầm trung, dùng lại được qua nhiều dự án, thì cần gồm những gì — và vì sao thứ đó cần thiết?**

Nó là **chuẩn đối chiếu**. Khi `core-reviewer` audit và hỏi *"Core này đã đủ chưa, còn thiếu mảng nào"*, nó mở khu này. Khi `architect` cân nhắc thêm một abstraction, nó mở khu này để xem thứ đó đã được cân nhắc và **cố ý loại** chưa.

Vì là kiến thức nền, khu này **được phép vượt nhu cầu của một dự án cụ thể**. Một mục ở đây mô tả cách làm đúng ở quy mô lớn hơn hiện tại không phải là lỗi — miễn là nó nói rõ ngưỡng nào thì cần.

## 2. Khác `quy-uoc/` chỗ nào — và vì sao khoảng cách đó là cố ý

| | [`../quy-uoc/`](../quy-uoc/) | `wiki-core/` (khu này) |
| --- | --- | --- |
| Trả lời câu hỏi | **Dự án này thi công thế nào** | **Một core tốt gồm gì và vì sao** |
| Ràng buộc | Là luật cho code — lệch là finding | **Mục §Áp dụng và Nhóm A ràng buộc** — vắng mặt là finding (§9). Thân bài giải thích thì không: nó được phép mô tả cách làm ở quy mô lớn hơn dự án này |
| Phạm vi | Đúng bằng thứ Core này làm | Có thể rộng hơn thứ Core này làm |
| Code mẫu | Đầy đủ, đủ để code theo | Ngắn, chỉ để minh hoạ ý |
| Ai đọc | `backend-expert`, `frontend-expert` khi viết code | `core-reviewer` khi audit, `architect` khi cân nhắc |

**Khoảng cách giữa hai khu là công cụ, không phải nợ.** Nó cho người review phân biệt hai thứ mà nếu gộp lại thì không phân biệt được:

| Loại lệch | Là finding? | Dấu hiệu |
| --- | --- | --- |
| **Lệch vì đã cố ý đơn giản hoá** | ❌ Không | `wiki-core/` mô tả thứ rộng hơn, và có một mục §Áp dụng (hoặc một ADR) nói rõ dự án này chưa làm phần nào, vì sao |
| **Lệch vì thiếu sót thật** | ✅ Có | `quy-uoc/` yêu cầu X, code không có X — hoặc `wiki-core/` xếp X vào Nhóm A mà không ai từng quyết định bỏ |

Gộp hai khu lại thì mọi thứ Core chưa làm đều trông như thiếu sót, và người review sẽ mở hàng loạt finding cho những thứ đã được cân nhắc và loại có lý do. Chuyện đó đủ ồn để làm cả bộ review mất tin cậy.

**Hệ quả cho người viết:** đừng chép nội dung `quy-uoc/` vào đây. Chỗ nào thuộc thi công — chữ ký class, thứ tự đăng ký DI, cách viết một handler — thì **link sang**, theo luật một-chủ-đề-một-file-chủ ở [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §5.

## 3. Nguyên tắc Nhóm A / Nhóm B

Khu này không phải checklist bắt buộc. Mọi thành phần được xếp vào một trong hai nhóm:

- **Nhóm A** — thiếu là Core không dùng được. Làm ngay từ đầu.
- **Nhóm B** — nên có, thêm khi có nhu cầu thật. Làm sớm là chi phí không đổi lấy gì.

Danh sách đầy đủ, kèm ngưỡng "khi nào Nhóm B thành cần thiết", ở [`be/01-core-components.md`](be/01-core-components.md). **Đọc mục §Áp dụng của file đó trước khi đề xuất thêm bất kỳ abstraction nào** — nhiều thứ ở đó đã được cân nhắc và loại, kèm lý do.

## 4. Mục lục — Backend

| Chủ đề đang cần | File |
| --- | --- |
| Core gồm thành phần nào, cái nào bắt buộc, cái nào cố ý chưa làm | [`be/01-core-components.md`](be/01-core-components.md) |
| Phiên đăng nhập, cookie vs JWT, permission-based, tài khoản bootstrap | [`be/02-identity-auth.md`](be/02-identity-auth.md) |
| Menu động, cấu hình cột lưới, form sinh từ metadata | [`be/03-metadata-driven-design.md`](be/03-metadata-driven-design.md) |
| Kim tự tháp test, ArchTest, meta-test cho detector, Testcontainers | [`be/04-testing-strategy.md`](be/04-testing-strategy.md) |
| Nhất quán dữ liệu giữa module khi không có distributed transaction | [`be/05-cross-module-consistency.md`](be/05-cross-module-consistency.md) |
| Hai người sửa cùng một bản ghi, xử lý xung đột, khi nào cần khoá bi quan | [`be/06-concurrency-control.md`](be/06-concurrency-control.md) |
| Log, trace id, metric, health check, OpenTelemetry | [`be/07-observability.md`](be/07-observability.md) |
| Bốn sai lầm khi viết ADR, ADR cho quyết định **không làm** | [`be/08-adr-practice.md`](be/08-adr-practice.md) |
| Chống dò tài khoản, gán tràn thuộc tính, header bảo mật, quản lý bí mật, upload độc hại | [`be/09-security-beyond-auth.md`](be/09-security-beyond-auth.md) |
| Soft delete, lưu bao lâu, xoá cứng, audit trail, quyền được quên | [`be/10-data-retention.md`](be/10-data-retention.md) |
| Kỷ luật đo trước khi tối ưu, ngưỡng đáng nghi, nghẽn ở tầng kết nối và transaction | [`be/11-performance-caching.md`](be/11-performance-caching.md) |
| Domain event vs integration event vs notification, Outbox, retry | [`be/12-notifications.md`](be/12-notifications.md) |
| Ai sở hữu migration, schema theo module, seed và backfill | [`be/13-core-data-migration.md`](be/13-core-data-migration.md) |
| Lưu file, đường dẫn qua cấu hình, quét file độc hại, dọn file mồ côi | [`be/14-file-storage.md`](be/14-file-storage.md) |
| Đọc/ghi CSV và Excel, lỗi từng dòng, export theo bộ lọc | [`be/15-import-export.md`](be/15-import-export.md) |
| Mã lỗi + tham số đặt tên, catalog mã, `fieldErrors`, định dạng theo văn hoá | [`be/16-i18n-va-ma-loi.md`](be/16-i18n-va-ma-loi.md) |
| Nhiều đơn vị dùng chung một bản cài: `ITenantScoped`, bộ lọc toàn cục, `TenantId` | [`be/17-multi-tenant.md`](be/17-multi-tenant.md) |
| Triển khai một bản mới, bí mật theo môi trường, quay lui, truy một sự cố | [`be/18-trien-khai-va-van-hanh.md`](be/18-trien-khai-va-van-hanh.md) |
| Cấu hình khác nhau theo từng đơn vị, thứ tự ưu tiên, ai khai khoá | [`be/19-cau-hinh-theo-don-vi.md`](be/19-cau-hinh-theo-don-vi.md) |
| Sinh số phiếu, số văn bản không trùng và không nhảy | [`be/20-sinh-ma-nghiep-vu.md`](be/20-sinh-ma-nghiep-vu.md) |

**Lộ trình thi công BE** (thứ tự làm, không phải lý thuyết): bắt đầu ở [`be/trien-khai/00-lo-trinh-tong-the.md`](be/trien-khai/00-lo-trinh-tong-the.md).
| "Class này nằm file nào" | [`be/tra-cuu-file-class.md`](be/tra-cuu-file-class.md) — **rỗng có chủ đích ở giai đoạn 1** |

## 5. Mục lục — Frontend

| Chủ đề đang cần | File |
| --- | --- |
| FE core gồm gì, Nhóm A/B phía FE | [`fe/01-core-components.md`](fe/01-core-components.md) |
| Vì sao có envelope, bốn cái bẫy khi tiêu thụ nó | [`fe/02-http-envelope.md`](fe/02-http-envelope.md) |
| Quản lý state bằng signal, khi nào cần store | [`fe/03-state-management.md`](fe/03-state-management.md) |
| Design token: từ khu Design xuống SCSS và preset thư viện UI | [`fe/04-design-token-system.md`](fe/04-design-token-system.md) |
| Thư viện component dùng chung, các trạng thái bắt buộc | [`fe/05-component-library.md`](fe/05-component-library.md) |
| Test FE: cái gì đáng test trước | [`fe/06-testing-strategy.md`](fe/06-testing-strategy.md) |
| Phía FE của phiên cookie, guard, người dùng hiện tại | [`fe/07-auth-identity.md`](fe/07-auth-identity.md) |
| Bản dịch, đổi ngôn ngữ tại chỗ, ghép câu từ mã lỗi BE | [`fe/08-i18n.md`](fe/08-i18n.md) |
| Form, validation, hiển thị lỗi theo từng ô nhập | [`fe/09-forms-validation.md`](fe/09-forms-validation.md) |
| Log phía client, nối trace id với BE | [`fe/10-observability.md`](fe/10-observability.md) |
| Lưới dữ liệu và hợp đồng metadata với BE | [`fe/11-grid-and-metadata.md`](fe/11-grid-and-metadata.md) |
| Biểu đồ | [`fe/12-charting.md`](fe/12-charting.md) |
| Hiệu năng FE, bundle budget, lazy-load | [`fe/13-performance.md`](fe/13-performance.md) |
| Bảo mật FE: sanitize, CSP, secret trong bundle | [`fe/14-security.md`](fe/14-security.md) |
| Accessibility | [`fe/15-accessibility.md`](fe/15-accessibility.md) |
| Nhịp nâng cấp framework và thư viện UI | [`fe/16-nen-tang-va-nang-cap.md`](fe/16-nen-tang-va-nang-cap.md) |
| Phục vụ static file, SPA fallback, cache header | [`fe/17-phuc-vu-va-trien-khai.md`](fe/17-phuc-vu-va-trien-khai.md) |

**Lộ trình thi công FE** (thứ tự làm, không phải lý thuyết): bắt đầu ở [`fe/trien-khai/00-lo-trinh-tong-the.md`](fe/trien-khai/00-lo-trinh-tong-the.md).

## 6. Cách dùng theo vai

| Vai | Cách đọc |
| --- | --- |
| `core-reviewer` | Mở đúng file theo chủ đề đang audit. Trước khi mở một finding, kiểm mục §Áp dụng của file đó — thứ bạn thấy thiếu có thể đã được cố ý loại |
| `architect` | Đọc trước khi chốt một quyết định chạm Core. Nếu quyết định lệch khỏi khu này, ghi ADR ở [`../adr/`](../adr/) chứ đừng sửa khu này cho khớp |
| `backend-expert` / `frontend-expert` | Đọc khi cần **lý do**. Khi cần **cách làm**, đi thẳng [`../quy-uoc/`](../quy-uoc/) |
| `test-engineer` | [`be/04-testing-strategy.md`](be/04-testing-strategy.md) và [`fe/06-testing-strategy.md`](fe/06-testing-strategy.md) |

**Đừng đọc cả thư mục.** Corpus mà một agent bị buộc nạp là thứ giết nó giữa chừng — lý do đầy đủ ở [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §3.

## 7. Ba khoá phân loại — đọc trước khi đối chiếu

Mọi file trong khu này khai `kind` / `scope` / `verified` ở frontmatter. Ý nghĩa ba khoá ở [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §9.

Hai điều cần nhớ khi dùng khu này:

1. **Chỉ file `kind: luat` mới là thứ code phải tuân.** File `kind: tham-chieu` là bảng tra hoặc mô tả một dự án khác — đối chiếu code với nó sẽ sinh ra finding cho những yêu cầu chưa bao giờ là luật ở đây.
2. **`verified: chua-doi-chieu` ở giai đoạn 1 là giá trị đúng**, không phải việc tồn đọng. Chưa có `src/` thì không ai đối chiếu được với cái gì.
3. **`kind` là khoá CẤP FILE, và phần lớn file khu này trộn hai loại câu** — thân bài giải thích (không ràng buộc) và §Áp dụng (ràng buộc). Khoá cấp file không diễn đạt được sự trộn đó, nên nó lấy giá trị của **câu mạnh nhất trong file**: có một câu ràng buộc thì cả file mang `kind: luat`. Phân biệt câu nào ràng buộc thì đọc §Áp dụng và bốn ký hiệu ở §9 — **đừng suy từ `kind` ra**.

> 🛑 **Đừng đọc dòng "Ràng buộc" ở §2 rồi kết luận cả khu là tham chiếu.** Ở repo này `kind: tham-chieu` có nghĩa hẹp — *bảng tra hoặc mô tả một dự án khác* (điểm 1 ở trên) — không có nghĩa "kiến thức nền có thể lệch". Hạ cả khu xuống nhãn đó sẽ mở khoá `verified: khong-ap-dung` cho toàn bộ file ở đây (cổng §12 chỉ chấp nhận giá trị đó khi `kind` là `tham-chieu`/`lich-su`), và sáu "định nghĩa gốc" đã đăng ký ở [`../OWNERSHIP.md`](../OWNERSHIP.md) sẽ nằm trong file tự khai là không ràng buộc.

```bash
grep -rl '^kind: luat' docs/wiki-core --include='*.md'
grep -rl '^kind: tham-chieu' docs/wiki-core --include='*.md'
```

## 8. Khu này KHÔNG chứa gì

| Thứ | Thuộc về |
| --- | --- |
| Cách viết một handler, chữ ký class, thứ tự đăng ký DI | [`../quy-uoc/`](../quy-uoc/) |
| Luật nào ép bằng cổng nào | [`../RULES.md`](../RULES.md) |
| Ranh giới Core ↔ Module, layout project | [`../kien-truc-core-module.md`](../kien-truc-core-module.md) |
| Vì sao chốt phương án này chứ không phương án kia | [`../adr/`](../adr/) |
| Sự cố đã xảy ra thế nào | [`../audit/`](../audit/) |
| Màu, khoảng cách, câu chữ trên giao diện | [`../Design/DESIGN.md`](../Design/DESIGN.md) |
| Nghiệp vụ của một feature | `spec/` (đặt ngoài `docs/`, có chủ đích) |

Khi một mục ở khu này bắt đầu dài ra thành hướng dẫn thi công, đó là dấu hiệu nó thuộc `quy-uoc/`. Cắt nó sang, để lại một dòng trỏ đường.

## 9. Bốn ký hiệu bạn sẽ gặp trong khu này

| Ký hiệu | Nghĩa | Cách phản ứng |
| --- | --- | --- |
| 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG** | Mô tả thứ Core phải trở thành | Đây là mặc định ở giai đoạn 1. Không phải nợ |
| ✅ **sẽ có** trong bảng §Áp dụng | Hạng mục nằm trong phạm vi v1 | Vắng mặt khi có `src/` là finding |
| ❌ **chưa** trong bảng §Áp dụng | Cố ý hoãn, kèm điều kiện kích hoạt | Đề xuất làm ngay phải chứng minh điều kiện đã xảy ra |
| ❌ **loại, không hoãn** `K##` | Một hướng đã bị bác, không phải một việc xếp sau | Muốn lật thì viết ADR **trích mã đó**, không mở finding |

Phân biệt hai dòng cuối là điểm dễ nhầm nhất khi đọc khu này. *"Chưa"* là câu hỏi về **thời điểm**; *"loại"* là câu hỏi về **hướng đi**, và nó chỉ đổi được bằng một quyết định mới có ghi chép.

### 9.1 Mã hướng khoá `K##` — định nghĩa gốc

Mỗi dòng `❌ loại, không hoãn` mang một mã `K##` **ngay sau ký hiệu**. Bốn luật:

| # | Luật | Vì sao |
| --- | --- | --- |
| 1 | Mã cấp **tuần tự**, lấy số kế tiếp sau mã lớn nhất đang có | Để dãy số liền mạch có nghĩa |
| 2 | **Không tái sử dụng**, **không đánh số lại** | Mã là danh tính; đánh số lại làm mọi trích dẫn cũ trỏ sang hướng khác mà vẫn resolve |
| 3 | Lật một hướng thì **viết ADR trích mã đó**, không xoá dòng | Dòng còn đó kèm ADR thì người sau đọc được cả hướng cũ lẫn lý do lật |
| 4 | Chỉ **hàng bảng** mới mang mã. Dòng định nghĩa ký hiệu (bảng §9) và câu văn xuôi **nhắc** ký hiệu thì không | Một phép dò không phân biệt được hai thứ đó sẽ cấp mã cho chính định nghĩa của nó |

**Vì sao cần mã, trong khi phần còn lại của `docs/` cố ý giữ văn xuôi:** hướng khoá là loại câu **bị lật trong im lặng**. Thêm một component là lật bốn câu như vậy mà không ai nhận ra — đã xảy ra, và ADR-0019 phải viết sau khi sự đã rồi. Mã biến việc một hướng biến mất thành một **lỗ trống trong dãy số**, tức thứ máy đếm được.

Cổng: [`../../.claude/check-docs.sh`](../../.claude/check-docs.sh) §21 — thiếu mã, trùng mã, hoặc một mã biến mất mà không ADR nào trích dẫn thì đỏ. Luật **D32** ở [`../RULES.md`](../RULES.md).

> Số mã hiện có **đếm bằng lệnh**, đừng chép vào đây:
>
> ```bash
> grep -rhoE '`K[0-9]{2}`' docs --include='*.md' | sort -u | wc -l
> ```

## 10. Khi khu này và `quy-uoc/` nói khác nhau

Sẽ có lúc hai khu mô tả cùng một thứ theo hai mức chi tiết khác nhau. Thứ tự phân xử:

| Câu hỏi đang hỏi | Nguồn có thẩm quyền |
| --- | --- |
| Code phải viết thế nào | [`../quy-uoc/`](../quy-uoc/) |
| Luật này được ép bằng cổng nào | [`../RULES.md`](../RULES.md) |
| Vì sao chọn thế này | [`../adr/`](../adr/) |
| Một core tốt thì nên có gì | `wiki-core/` |

Nếu hai bên **mâu thuẫn thật** — không phải khác mức chi tiết, mà nói ngược nhau — đó là vi phạm luật một-chủ-đề-một-file-chủ. Cách xử lý: chọn một file làm chủ, file kia rút còn một dòng trỏ đường, và ghi lại việc đó. **Không giữ cả hai cho chắc** — hai bản sao thì chúng sẽ lệch tiếp, và lần sau người đọc trúng bản sai sẽ không biết mình đọc trúng bản sai.
