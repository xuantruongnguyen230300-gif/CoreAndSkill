---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `quy-uoc/` — quy ước THI CÔNG của CoreAndSkill

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Repo đang ở giai đoạn 1: chỉ có `docs/` và `.claude/`,
> chưa có `src/`. Mọi quy ước trong khu này mô tả thứ `src/` **phải trở thành** ở giai
> đoạn 2, không phải mô tả code đang chạy. Không dòng nào ở đây được đọc như bằng chứng
> về hiện trạng.

---

## 1. Khu này là gì

`quy-uoc/` trả lời đúng một câu hỏi: **"tôi sắp viết code, phải viết thế nào cho đúng
luật của repo này?"**

Mỗi file trong khu là một **hợp đồng thi công**: có bảng, có code mẫu biên dịch được, có
lý do và có đánh đổi. Một senior đọc file tương ứng xong phải code được ngay, không phải
đoán và không phải hỏi lại.

Ba thứ khu này **không** làm:

| Không phải | Mà là | Ở đâu |
| --- | --- | --- |
| Kiến thức nền "một core tốt gồm gì" | Quy ước thi hành của **repo này** | [`../wiki-core/`](../wiki-core/) giữ kiến thức nền |
| Luật nghiệp vụ của một feature | Quy tắc kiến trúc và code | `spec/<feature>/` (ngoài `docs/`) giữ nghiệp vụ |
| Lý do một quyết định được chọn | Cách thi hành quyết định đó | [`../adr/`](../adr/) giữ lý do và các phương án đã loại |

---

## 2. Khác `wiki-core/` chỗ nào — và vì sao khoảng cách đó là CỐ Ý

Hai khu nói về cùng những chủ đề (entity, envelope, phân quyền, cache) nhưng ở hai độ
cao khác nhau:

| | [`../wiki-core/`](../wiki-core/) | `quy-uoc/` (khu này) |
| --- | --- | --- |
| Câu hỏi trả lời | *"Một core tốt gồm những gì, và vì sao?"* | *"Repo này thực sự làm gì, và làm thế nào?"* |
| Phạm vi | Có thể **vượt** nhu cầu của CoreAndSkill | Đúng bằng thứ đã chốt cho CoreAndSkill |
| Ví dụ cụ thể | Mô tả đầy đủ một tầng cache nhiều mức, invalidation theo tag, Redis | Ghi rõ **chưa làm cache ở v1**, chỉ khai sẵn interface |
| Ai đọc | `core-reviewer` khi audit; `architect` khi cân nhắc nâng cấp | `backend-expert`, `frontend-expert` khi đang viết code |

**Khoảng cách giữa hai khu không phải là nợ kỹ thuật cần dọn.** Nó là công cụ đo.

Khi `core-reviewer` thấy code lệch khỏi `wiki-core/`, nó phải trả lời được một câu trước
khi mở finding: *lệch này đã được ghi nhận ở `quy-uoc/` chưa?*

| Tình huống | Kết luận |
| --- | --- |
| `wiki-core/` mô tả X, `quy-uoc/` ghi rõ **"chưa làm X ở v1, vì …"**, code không có X | **Không phải finding.** Đây là đơn giản hoá có chủ đích, đã thống nhất |
| `wiki-core/` mô tả X, `quy-uoc/` **cũng** yêu cầu X, code không có X | **Là finding.** Thiếu sót thật |
| `quy-uoc/` yêu cầu X, `wiki-core/` không nhắc tới | Hợp lệ — `quy-uoc/` được phép chặt hơn kiến thức nền |

Gộp hai khu lại sẽ **phá đúng thứ chúng sinh ra để làm**: không còn chỗ nào ghi *"chúng
tôi biết cách làm đầy đủ, và đây là lý do chúng tôi cố ý chưa làm"*. Khi đó mọi lệch đều
trông giống nhau, và mọi lệch đều thành finding — kể cả những lệch đã được cân nhắc kỹ.

📖 Luật đầy đủ về ba khu tri thức: [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §2.

---

## 3. Stack đã chốt

| Lớp | Công nghệ | Ghi chú ràng buộc |
| --- | --- | --- |
| Runtime | **.NET 10** (LTS), ASP.NET Core | Build không warning ([`../RULES.md`](../RULES.md) T4) |
| Mediator | **MediatR** | Đúng hai pipeline behavior ở v1 — xem [`be-cqrs-handler.md`](be-cqrs-handler.md). Dòng phiên bản được khoá: [`../adr/0031-khoa-mediatr-12-5.md`](../adr/0031-khoa-mediatr-12-5.md) |
| Validation | **FluentValidation** | Validator trả về lỗi qua `Result`, không ném exception |
| ORM | **EF Core + PostgreSQL (Npgsql)** | Concurrency token dùng `xmin`, không dùng recipe SQL Server |
| Identity | **ASP.NET Core Identity**, phiên bằng **Cookie** (KHÔNG JWT) | `AppUser`/`AppRole` chỉ sống trong `Core.Infrastructure` |
| Chống CSRF | Cookie `SameSite=Lax` + antiforgery **hai lớp** (kiểm `Origin` + token) | `Lax` chỉ chặn site khác; subdomain khác cùng tên miền gốc vẫn là cùng site — nên vẫn cần hai lớp |
| Frontend | **Angular** standalone + signals, **PrimeNG**, `@ngx-translate` | Xem [`fe-architecture.md`](fe-architecture.md). Phiên bản, toolchain và kế hoạch nâng cấp: [`../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md`](../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md) — tài liệu sống không chép số phiên bản |
| Cache | Redis — **hoãn tới v2** | Khai interface trước, chưa có implementation — xem [`be-performance.md`](be-performance.md) |
| Message broker | **KHÔNG có ở v1** | Outbox + hosted service thay thế |

Layout project và ranh giới Core ↔ Module: [`../kien-truc-core-module.md`](../kien-truc-core-module.md).

---

## 4. Đang làm gì → đọc file nào

**Đọc đúng một file.** Đừng đọc cả thư mục — mỗi file dưới đây là file chủ của chủ đề
của nó, và những file còn lại chỉ trỏ về nó.

| Đang làm gì | Đọc |
| --- | --- |
| Tạo project mới, quyết định file này nằm ở tầng nào, viết `Program.cs`, đăng ký DI, thêm `IOptions<T>` | [`be-architecture.md`](be-architecture.md) |
| Viết entity mới, `BaseEntity`, Value Object, soft delete, concurrency token, ranh giới Identity | [`be-entity-domain.md`](be-entity-domain.md) |
| Viết Command/Query/Handler/Validator, dùng `Result<T>`, khai `ErrorDescriptor`, phân trang `PagedList<T>` | [`be-cqrs-handler.md`](be-cqrs-handler.md) |
| Viết controller, ánh xạ `Result` → HTTP, hình dạng envelope, ba attribute phân quyền endpoint, allowlist ẩn danh, rate limit, cookie phiên, CORS/antiforgery | [`be-api-controller.md`](be-api-controller.md) |
| Viết repository, tối ưu query, đặt index, chống N+1, phân trang keyset, cân nhắc cache | [`be-performance.md`](be-performance.md) |
| Đặt file FE vào `core/` hay `shared/` hay `platform/` hay `modules/` | [`fe-architecture.md`](fe-architecture.md) |
| Gọi API từ FE, ranh giới DTO ↔ model, mapper | [`fe-api-client.md`](fe-api-client.md) |
| Dựng UI, form, style theo token, bọc PrimeNG | [`fe-ui-conventions.md`](fe-ui-conventions.md) |
| Khai route, lazy-load, guard, giữ state trên URL | [`fe-routing-guard.md`](fe-routing-guard.md) |
| Quyết định cái gì được commit: artifact build, secret, file sinh tự động | [`repo-artifact.md`](repo-artifact.md) |
| Chấm review: cái gì là finding, cái gì không | [`tieu-chi-review.md`](tieu-chi-review.md) |

Không thấy chủ đề của mình ở bảng trên → tra mục lục cấp trên: [`../README.md`](../README.md).

Mỗi file luật ở đây chỉ giữ **luật, bảng, chữ ký, ví dụ tối thiểu**. Phần "vì sao, bẫy, ví dụ mở rộng" nằm ở file **cùng tên** trong `../wiki-core/be/ly-do/` hoặc `../wiki-core/fe/ly-do/`, cùng số mục § — mục lục ở [`../wiki-core/README.md`](../wiki-core/README.md) §4 và §5. Cần lý do mới mở; thi công thì không cần.

---

## 5. Bốn quy tắc áp cho MỌI file trong khu này

### 5.1 Nói được VÌ SAO, và nói được ĐÁNH ĐỔI

Một quy ước không trả lời được *"vì sao"* thì không ai theo, và người đầu tiên gặp bất
tiện sẽ bỏ nó. Một quy ước không nói *"đánh đổi gì"* thì người sau sẽ lật nó mà không
biết mình đang trả lại cái giá nào.

Chỗ của "vì sao" và "đánh đổi" là **file lý do cùng tên** trong `../wiki-core/{be,fe}/ly-do/`,
cùng số mục §. File luật giữ câu luật và một dòng trỏ sang đó — để agent thi công không
phải gánh phần giải thích ở mỗi lượt.

### 5.2 Bẫy đã biết phải nói thẳng

Chỗ nào có một cách viết **trông đúng mà sai**, file luật nêu đích danh **câu cấm**, và
file lý do nêu cách viết sai đó cùng hậu quả. Ba bẫy đắt nhất kế thừa từ dự án tiền
nhiệm:

| Bẫy | Ở file |
| --- | --- |
| Cầu nối lỗi dựng bằng reflection — chữ ký đổi thì mọi lỗi nghiệp vụ thành `NullReferenceException` | câu cấm: [`be-api-controller.md`](be-api-controller.md) §1 · diễn giải: [`../wiki-core/be/ly-do/be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §1.3 |
| Concurrency token dùng API của SQL Server trên PostgreSQL — check vô hiệu **im lặng** | câu cấm: [`be-entity-domain.md`](be-entity-domain.md) §6 · diễn giải: [`../wiki-core/be/ly-do/be-entity-domain.md`](../wiki-core/be/ly-do/be-entity-domain.md) §6 |
| Rate limit chọn sai overload — hạn mức đăng nhập áp cho **toàn hệ thống** thay vì mỗi IP | câu cấm: [`be-api-controller.md`](be-api-controller.md) §6.2 · diễn giải: [`../wiki-core/be/ly-do/be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §6.2 |

📖 Postmortem đầy đủ: [`../audit/`](../audit/).

### 5.3 Mọi luật phải nối được với một cổng

Mỗi quy ước ở khu này ứng với ít nhất một dòng trong [`../RULES.md`](../RULES.md), và
dòng đó khai **ép bằng gì**. Quy ước không có cổng vẫn được viết, nhưng phải nằm ở §9
của `RULES.md` — nhìn thấy được là nợ, không trộn lẫn vào luật đã có cổng.

Khi thêm một quy ước mới vào khu này: thêm dòng tương ứng vào `RULES.md` **cùng lượt**.

### 5.4 Comment trong code tối thiểu — lịch sử ở `docs/audit/`

Code mẫu trong khu này cố ý ít comment. Lý do một thiết kế được chọn thuộc về
[`../adr/`](../adr/); lịch sử một sự cố thuộc về [`../audit/`](../audit/). Nhồi cả hai
vào comment sinh ra file code mà hơn nửa số dòng không phải code — và phần đó không bao
giờ được cập nhật khi code đổi.

Comment được giữ lại chỉ cho một loại: **cảnh báo chống sửa nhầm** ở chỗ mà cách viết
đúng trông như một lỗi. Quyết định đầy đủ: [`../adr/0010-comment-toi-thieu.md`](../adr/0010-comment-toi-thieu.md).

---

## 6. Trước khi coi một thay đổi trong khu này là xong

```bash
bash .claude/check-docs.sh
```

Gate kiểm được: đường dẫn tồn tại, link resolve, mọi file khai đủ ba khoá frontmatter,
tuyên bố hoàn thành có ngày đối chiếu.

Gate **không** kiểm được: code mẫu có biên dịch không, quy ước có mâu thuẫn nhau không,
một mục có mô tả thứ chưa ai xây không. Ba loại đó thuộc về `core-reviewer` và người đọc
— xem [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §8.
