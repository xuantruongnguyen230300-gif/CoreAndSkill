---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0125 — Cả thư mục `src/FE/src/styles/` vào khối `core-paths` và vùng Core, kể cả bảng màu ở `_tokens.scss`; bảng màu của dự án hạ nguồn chờ một cơ chế riêng

> **Trạng thái:** Đã chấp nhận (2026-11-06)

## Bối cảnh

Hai ADR đã để ngỏ câu hỏi *thư mục style toàn cục của FE thuộc ai*:

- [ADR-0059](0059-cong-fe-canary-va-tai-san-fe-cua-core-vao-core-paths.md) phương án C không đưa `src/FE/src/styles/` vào khối `core-paths`, vì chưa chốt nó thuộc ai. Bảng màu của dự án đi qua tệp token toàn cục; nếu tệp đó là Core thì dự án hạ nguồn không đổi được màu mà không sửa tệp Core, trái luật 1 của [ADR-0016](0016-phan-phoi-core-bang-clone.md).
- [ADR-0100](0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md) điểm 4 xếp tệp token toàn cục vào phần FE chưa phân vùng, và tự nói không trả lời câu đó.

Ngày 2026-11-06, hai ADR dời cơ chế của component Core vào chính thư mục này:

| ADR | Cơ chế dời vào `src/FE/src/styles/` | Trước đó nằm ở | Hệ quả ADR đó tự ghi |
| --- | --- | --- | --- |
| [ADR-0123](0123-chuyen-dong-component-primeng-di-bang-token-qua-css-go-hang-ms-f37-het-ap-dung.md) | Thời lượng, đường cong chuyển động của lớp bọc PrimeNG dưới `shared/ui/`: khối đè ở `_thu-vien.scss`, bậc `--dur-*`, `--ease-*` ở `_tokens.scss` — luật F62 | Hằng `*_MS` dưới `shared/ui/`, trong khối | Nửa CSS của F62 được canh yếu hơn chỗ F37 từng canh |
| [ADR-0124](0124-go-nua-provider-hoat-anh-giam-chuyen-dong-dua-vao-css-toan-cuc-va-safe-f39-het-ap-dung.md) | Giảm chuyển động của component PrimeNG: khối `@media` toàn cục ở `styles.scss` — luật F63 | Nửa provider ở `core/theme/`, trong khối | Sửa `styles.scss` không kéo lượt `core-reviewer` nào |

Kiến trúc sư đọc thư mục ngày 2026-11-06 — liệt kê tệp bằng `git ls-files src/FE/src/styles`:

| Tệp | Core dựa vào gì trong đó | Neo |
| --- | --- | --- |
| `_tokens.scss` | Bảng màu sáng và tối. **Cùng tệp:** thời lượng, đường cong, bo góc, bóng, thang `z-index`, màu biểu đồ | `src/FE/src/styles/_tokens.scss`, chuỗi `--color-brand: #1a56b8;` và chuỗi `--dur-base: 200ms;` |
| `_thu-vien.scss` | Override thư viện cho component Core | `src/FE/src/styles/_thu-vien.scss`, chuỗi `@keyframes app-drawer-mask-vao` |
| `styles.scss` | Điểm vào duy nhất của style toàn cục; khối giảm chuyển động | `src/FE/angular.json`, chuỗi `"src/styles/styles.scss"`; `src/FE/src/styles/styles.scss`, chuỗi `@media (prefers-reduced-motion: reduce)` |
| `_spacing.scss` | Giá trị điểm ngắt mà luật F58 chỉ cho khai ở đây | `src/FE/src/styles/_spacing.scss`, chuỗi `$bp-md: 900px;` |
| `_typography.scss` | Thang cỡ chữ | `src/FE/src/styles/_typography.scss`, chuỗi `--fs-md:` |

Ba điều rút ra:

**Một — không tệp nào trong thư mục chỉ chứa dữ liệu của dự án.** Bảng màu, thứ dữ liệu dự án duy nhất mà phương án C của ADR-0059 nêu, nằm chung tệp với bậc thời lượng F62 dựa vào. Tách theo tệp không cô lập được bảng màu.

**Hai — cơ chế Core đang nằm ở chỗ sửa mà không kéo review.** Gỡ `!important` của khối giảm chuyển động, hay đổi `--dur-base`, đi qua mọi cổng mà không lượt `core-reviewer` nào bị đòi.

**Ba — chưa dự án hạ nguồn nào tồn tại.** Repo chưa có tag nào: `git describe --tags --always` trả mã commit. Gốc repo không có `CORE_VERSION`. Hôm nay chưa ai thật sự cần đổi bảng màu.

Người dùng chốt ngày 2026-11-06, sau khi được nêu rõ hệ quả với bảng màu.

## Quyết định

**Người dùng chốt: cả thư mục `src/FE/src/styles/` vào nhóm Frontend của khối `core-paths`.** Kiến trúc sư ghi phần đi kèm:

1. **Khối `core-paths` ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §10 thêm một dòng thư mục**, kèm một hàng ở bảng *Nguồn của từng nhóm*. Ghi dạng thư mục, không liệt kê tệp: partial mới thêm vào thư mục tự thuộc khối.
2. **Cả thư mục thuộc vùng Core, kể cả bảng màu ở `_tokens.scss`.** ADR-0100 điểm 3 nói mọi đường dẫn trong khối thuộc vùng Core, nên luật A17 áp: dự án hạ nguồn không sửa tệp nào trong thư mục. ADR này trả lời phần *tệp token toàn cục* của ADR-0100 điểm 4 và của ADR-0059 phương án C. Phần `src/FE/` còn lại ngoài khối — composition root `app.config.ts`, `angular.json`, `environments/` — vẫn chưa phân vùng.
3. **Bảng màu của dự án chờ một cơ chế riêng, và hôm nay chưa có.** Dự án hạ nguồn cần màu thương hiệu khác thì đi theo [`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md) §15.4, hàng *chưa có seam, nhưng nhu cầu chung*: dựng cơ chế ở repo Core, đánh tag, kéo về. Dự án không sửa `_tokens.scss` của bản clone. **Kích hoạt:** dự án hạ nguồn đầu tiên cần màu khác bảng màu Core. ADR này không chọn hình dạng cơ chế. Chưa dự án nào cho biết nó cần đổi tới đâu: chỉ màu thương hiệu, cả thang trung tính, cả theme tối hay màu biểu đồ.
4. **Ô *Ép bằng gì* của F62, F63 đổi:** nửa CSS nay do `core-reviewer` canh. Không cổng máy nào thêm, nên trạng thái `🕳️` của hai luật giữ nguyên ([ADR-0114](0114-bo-so-no-luat-ve-rules-viec-ton-sang-issues-review-khong-sinh-viec-stop-chan-review-mot-lan-go-nhanh-chua-co-src.md)).
5. **ADR này bổ sung ADR-0059 và ADR-0100.** Cả hai giữ nguyên hiệu lực. ADR-0123 và ADR-0124 không đổi trạng thái: thứ ADR này giải là hệ quả tiêu cực chúng tự ghi, không phải quyết định của chúng.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Liệt kê từng tệp vào khối, trừ `_tokens.scss`

**Được:**

- `_thu-vien.scss` và `styles.scss`, hai tệp mang khối đè F62 và khối giảm chuyển động F63, vào khối.
- `_tokens.scss` để ngỏ cho dự án, nên không cần cơ chế bảng màu nào.

**Mất:**

- `_tokens.scss` không chỉ chứa màu. Bậc `--dur-*`, `--ease-*`, mà ADR-0123 đặt làm nguồn duy nhất của thời lượng nhóm B, nằm ở đó. Đổi `--dur-base` là đổi cùng lúc `Dialog`, `Drawer`, ba lớp nổi và backdrop, mà không lượt review nào bị đòi. Cùng tệp còn thang `z-index`, bo góc, bóng.
- Khối liệt kê từng tệp thì partial mới thêm vào thư mục mặc định đứng ngoài khối. Lỗ mở ra theo chiều im lặng.

**Vì sao loại:** để hở khỏi review đúng bậc thời lượng mà khối đè F62 trỏ vào.

### Phương án B — Không thêm gì vào khối

Đây là **phương án đơn giản nhất**.

**Được:** không đổi gì. Dự án hạ nguồn không bị cấm sửa style toàn cục. Không thêm lượt review nào.

**Mất:** nửa CSS của F62, F63 chỉ còn người duyệt PR canh, nếu tình cờ đọc tới; không lượt `core-reviewer` nào được giao. Hai cơ chế từng nằm trong khối, là hằng `*_MS` và nửa provider, rời khối chỉ vì nền đổi từ TypeScript sang CSS.

**Vì sao loại:** mức canh của hai cơ chế Core không nên tụt chỉ vì chúng đổi ngôn ngữ.

### Phương án C — Đưa cả thư mục vào khối, đồng thời tách bảng màu ra một tệp thuộc vùng dự án ngay lượt này

Kiến trúc sư thêm phương án này lúc viết ADR. Nó không được trình riêng cho người dùng. Lý do loại trùng với điều kiện kích hoạt người dùng đã chốt.

**Được:** không có quãng nào dự án hạ nguồn bị chặn đổi màu.

**Mất:**

- Đây là dựng cơ chế bảng màu trước khi có dự án nào cần. Phải đoán ranh giới (thang thương hiệu, thang trung tính, theme tối, màu biểu đồ) và cách nạp (tệp ghi đè nạp sau, biến SCSS, preset).
- Đụng luật *literal màu chỉ ở `_tokens.scss`* ([`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) §3.3). Cổng hex của `scripts/fe-gate.sh` loại trừ đúng tệp đó, chuỗi `-not -path "$FE/src/styles/_tokens.scss"`, nên phải sửa cổng cùng lượt.

**Vì sao loại:** trả chi phí chắc chắn cho một nhu cầu chưa ai có.

## Hệ quả

### Tích cực

- Sửa bất kỳ tệp nào dưới `src/FE/src/styles/` từ nay kéo lượt `core-reviewer`, cùng mức sửa `shared/ui/`. Nửa CSS của F62 và F63 lần đầu có người được giao canh.
- Partial mới dưới thư mục tự thuộc khối, không cần ai nhớ thêm dòng.
- Hàng *Chưa phân vùng* ở [`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md) §15.5 hẹp lại. Một trong hai mục mà ADR-0100 điểm 4 nêu tên có câu trả lời.

### Tiêu cực

| Cái giá | Nghĩa là |
| --- | --- |
| **Dự án hạ nguồn không có đường hợp lệ để đổi màu thương hiệu** | Từ hôm nay tới khi cơ chế bảng màu được dựng. Dự án đầu tiên cần màu riêng phải chờ một lượt ở repo Core: thiết kế cơ chế, đánh tag, kéo về. Gấp thì cám dỗ là sửa thẳng `_tokens.scss` của bản clone. Làm vậy là vi phạm A17, và mỗi lần kéo bản Core sau đó xung đột ở tệp hay đổi nhất của style Core. Người dùng nhận cái giá này khi chốt |
| **Một giá trị chỉ đúng cho một sản phẩm nằm trong vùng Core** | Bảng màu là màu của sản phẩm Core, không phải cơ chế. Đây là một dấu hiệu Core phình: *tham số trong Core mà giá trị chỉ đúng cho một dự án*. Nguyên tắc *Core giữ cơ chế, app cấp dữ liệu qua seam* ở [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §2.5 có một ngoại lệ ghi tên, cho tới khi cơ chế bảng màu về |
| **Dự án hạ nguồn chưa có chỗ cho style toàn cục của riêng mình** | `styles.scss` là điểm vào khai trong `angular.json`, và thuộc vùng Core. Module cần một override thư viện toàn cục thì chưa có chỗ quy định. Thêm tệp vào mảng `styles` của `angular.json`, tệp còn chưa phân vùng, là một đường khả dĩ. ADR này không chốt nó |
| **Mọi chỉnh sửa style toàn cục ở repo Core kéo một lượt `core-reviewer`** | Kể cả chỉnh một màu hay một khoảng cách theo spec `docs/Design/`. Thư mục là nơi mọi quyết định token của `design-expert` đi vào mã, nên vòng thi công token thêm một lượt review mỗi lần |
| **Review không phải cổng** | F62, F63 vẫn `🕳️`. `core-reviewer` đọc diff, không chạy trình duyệt. Gỡ `!important` của khối giảm chuyển động chỉ bị bắt nếu người soát biết ràng buộc F63 |
| **Nhánh lệnh shell của hook thêm một gốc để quét** | Cùng cái giá ADR-0059 đã ghi cho mỗi đường dẫn thêm vào khối |

### Rút lui nếu sai

1. Gỡ dòng `src/FE/src/styles/` khỏi khối, gỡ hàng của nó ở bảng *Nguồn của từng nhóm*.
2. Chạy `bash .claude/hooks/tests/run-tests.sh` và `bash .claude/check-docs.sh`.
3. Đưa thư mục trở lại hàng *Chưa phân vùng* ở `repo-artifact.md` §15.5. Ô *Ép bằng gì* của F62, F63 quay về câu *nằm ngoài `core-paths`*. Hàng *Bảng màu* ở `fe-architecture.md` §2.5 bỏ câu vùng Core.
4. **Rút lui một phần, khi chỉ bảng màu là vấn đề:** dựng cơ chế bảng màu sớm, giữ thư mục trong khối. Phương án A không phải lối rút lui, vì nó mở lại chỗ hở của bậc `--dur-*`.

Không dữ liệu nào phải xử lý: dấu `core-touched` chỉ là trạng thái chạy. Dự án hạ nguồn tuân A17 thì không có thay đổi nào trong thư mục để gỡ. Chi phí: một lượt `architect`.

### Dấu hiệu quyết định này bắt đầu sai

- Dự án hạ nguồn đầu tiên sửa `_tokens.scss` của bản clone thay vì chờ cơ chế. Luật mà dự án vi phạm ngay tuần đầu sẽ bị bỏ qua, đúng lập luận ADR-0100 phương án A. Lúc đó dựng cơ chế bảng màu ngay, không nới A17.
- Phần lớn lượt `core-reviewer` FE chỉ soát giá trị token thuần trình bày. Giá review vượt giá trị nó mang lại.
- `_thu-vien.scss` bắt đầu nhận override cho component chỉ một module dùng. Style của nghiệp vụ đang lọt vào vùng Core.

## Sáu câu hỏi trước khi chấp nhận

| # | Câu hỏi | Trả lời |
| --- | --- | --- |
| 1 | Vấn đề thật, đã đo chưa | Đo bằng đọc: ADR-0123, ADR-0124 dời hai cơ chế Core vào thư mục; khối `core-paths` đọc ngày 2026-11-06 không có dòng nào phủ `src/FE/src/styles/`. **Chưa có vi phạm thật nào**; lý do là cơ chế vừa dời, không phải một lần hỏng đã xảy ra |
| 2 | Phương án đơn giản hơn, vì sao không đủ | Phương án B, không thêm gì. Nó để nửa CSS của F62, F63 không ai được giao canh |
| 3 | Chi phí vận hành thêm | Một lượt `core-reviewer` cho mỗi lần sửa style toàn cục ở repo Core. Một lượt thiết kế và dựng cơ chế bảng màu, khi kích hoạt |
| 4 | Ai bảo trì | Dòng trong khối: `architect`. Nội dung thư mục: `frontend-expert` thi công, giá trị theo `docs/Design/` do `design-expert` giữ, `core-reviewer` soát. Cơ chế bảng màu: **chưa có chủ**; `architect` nhận khi kích hoạt |
| 5 | Rút lui thế nào | Bốn bước ở mục *Rút lui nếu sai* |
| 6 | Có buộc Core biết nghiệp vụ không | Không biết nghiệp vụ. Nhưng **có** giữ một giá trị chỉ đúng cho một sản phẩm: bảng màu. Đó là cái giá ghi ở *Tiêu cực*, có điều kiện gỡ ở quyết định 3 |

## ADR cũ — phần nào còn hiệu lực

| ADR | Còn hiệu lực | Hết hiệu lực |
| --- | --- | --- |
| [0059](0059-cong-fe-canary-va-tai-san-fe-cua-core-vao-core-paths.md) | Toàn bộ. Phương án C có câu trả lời cho phần `styles/`; phần *cấu hình app* vẫn mở | — |
| [0100](0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md) | Toàn bộ. Điểm 4: phần *tệp token toàn cục* có câu trả lời; composition root vẫn mở | — |

## Việc thi công

**`architect` — `docs/`, đã làm cùng lượt viết ADR này:**

- Dòng thư mục và hàng nguồn ở khối `core-paths`, [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §10.
- Hàng *Chưa phân vùng* ở [`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md) §15.5.
- Hàng *Bảng màu* ở [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §2.5.
- Ô *Ép bằng gì* của F62, F63 ở [`../RULES-FE.md`](../RULES-FE.md).

Không việc mã nào. Không việc nào cho `test-engineer`: bộ test hook dựng một khối giả riêng, không đọc khối thật (ADR-0100, *Bối cảnh*).

## Liên quan

- [`0059-cong-fe-canary-va-tai-san-fe-cua-core-vao-core-paths.md`](0059-cong-fe-canary-va-tai-san-fe-cua-core-vao-core-paths.md) · [`0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md`](0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md) — hai ADR được bổ sung.
- [`0123-chuyen-dong-component-primeng-di-bang-token-qua-css-go-hang-ms-f37-het-ap-dung.md`](0123-chuyen-dong-component-primeng-di-bang-token-qua-css-go-hang-ms-f37-het-ap-dung.md) · [`0124-go-nua-provider-hoat-anh-giam-chuyen-dong-dua-vao-css-toan-cuc-va-safe-f39-het-ap-dung.md`](0124-go-nua-provider-hoat-anh-giam-chuyen-dong-dua-vao-css-toan-cuc-va-safe-f39-het-ap-dung.md) — lý do của quyết định.
- [`0016-phan-phoi-core-bang-clone.md`](0016-phan-phoi-core-bang-clone.md) — luật 1, nay đọc qua A17.
- [`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md) §15.4, §15.5 — thủ tục khi chưa có seam; bảng vùng.
