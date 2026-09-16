---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `docs/` — nguồn tri thức DUY NHẤT của CoreAndSkill

> **Đây là mục lục cấp cao nhất.** Không tìm thấy chủ đề trong bảng định tuyến của agent → tra ở đây rồi mở **đúng một** file. Đừng đọc cả thư mục.
>
> Ranh giới ba khu: `.claude/` giữ **quy trình**, `docs/` giữ **quy tắc**, `spec/` giữ **nghiệp vụ theo feature**. Luật đầy đủ ở [`.claude/CLAUDE.md`](../.claude/CLAUDE.md) §2–§3. Khi có thay đổi: **nội dung vào `docs/`**, `.claude/` chỉ sửa khi **đường dẫn** đổi.

## ⚠️ Trạng thái repo — đọc trước tiên

**Repo này đang ở giai đoạn 1: chưa có `src/`.**

Vì vậy **mọi mô tả kiến trúc trong `docs/` đều là `📐 ĐÍCH ĐẾN — CHƯA THI CÔNG`**, không phải mô tả hiện trạng. Đây là trạng thái đúng theo thiết kế: `src/` sẽ được xây ở giai đoạn 2 và phải bám theo những tài liệu này.

Điều kiện chuyển sang giai đoạn 2, và ai lật nhãn trạng thái: [`adr/0030-dieu-kien-chuyen-giai-doan-2.md`](adr/0030-dieu-kien-chuyen-giai-doan-2.md).

Hệ quả: gần như mọi file mang `verified: chua-doi-chieu`. Đó là giá trị **trung thực** — chưa có source để đối chiếu. Xem [`.claude/CLAUDE.md`](../.claude/CLAUDE.md) §4 và §9.

## Tra theo chủ đề

| Đang làm gì | Đọc file nào |
| --- | --- |
| **Layer rule, dependency, project layout BE** | [`quy-uoc/be-architecture.md`](quy-uoc/be-architecture.md) |
| **Entity, soft delete, Value Object, concurrency** | [`quy-uoc/be-entity-domain.md`](quy-uoc/be-entity-domain.md) |
| **Command/Query, Handler, Validator, `Result<T>`, `ErrorDescriptor`** | [`quy-uoc/be-cqrs-handler.md`](quy-uoc/be-cqrs-handler.md) |
| **Controller, envelope, `Result` → HTTP, rate limit, phân quyền** | [`quy-uoc/be-api-controller.md`](quy-uoc/be-api-controller.md) |
| **Repository, query, index, N+1, cache** | [`quy-uoc/be-performance.md`](quy-uoc/be-performance.md) |
| **Tầng `core`/`shared`/`platform`/`modules`, cấu trúc một feature FE** | [`quy-uoc/fe-architecture.md`](quy-uoc/fe-architecture.md) |
| **Gọi API, ranh giới DTO/model, mapper** | [`quy-uoc/fe-api-client.md`](quy-uoc/fe-api-client.md) |
| **Control flow Angular, form, responsive, style theo token, bọc PrimeNG** | [`quy-uoc/fe-ui-conventions.md`](quy-uoc/fe-ui-conventions.md) |
| **Route, lazy-load, guard, state trên URL** | [`quy-uoc/fe-routing-guard.md`](quy-uoc/fe-routing-guard.md) |
| **Cái gì được commit: artifact build, secret, file sinh tự động** | [`quy-uoc/repo-artifact.md`](quy-uoc/repo-artifact.md) |
| **Chấm review: cái gì là finding, cái gì không** | [`quy-uoc/tieu-chi-review.md`](quy-uoc/tieu-chi-review.md) |
| **Ranh giới Core ↔ Module, khi nào tách module** | [`kien-truc-core-module.md`](kien-truc-core-module.md) |
| **Luật nào được ép bằng gì** | [`RULES.md`](RULES.md) |
| **Nội dung nào thuộc file nào — nguồn đối chiếu duy nhất** | [`OWNERSHIP.md`](OWNERSHIP.md) |
| **Giao diện: layout, câu chữ, token, component, ảnh màn hình** | [`Design/`](Design/) — nguồn UI **duy nhất** |
| **Hợp đồng API một endpoint cụ thể** | [`contracts/`](contracts/) |
| **Schema `core`: bảng, cột, index** | [`database/schema-core.md`](database/schema-core.md) |
| **Ai sở hữu migration, schema theo module** | [`database/migration-policy.md`](database/migration-policy.md) |
| **Chạy script DB: đường dẫn, thứ tự, phát hiện DB lệch model** | [`database/script-runbook.md`](database/script-runbook.md) |
| **"Core đủ chưa, còn thiếu mảng nào"** | [`wiki-core/be/01-core-components.md`](wiki-core/be/01-core-components.md) §Áp dụng |
| **Lộ trình thi công: pha nào làm gì, khi nào một pha đóng** | [`wiki-core/be/trien-khai/00-lo-trinh-tong-the.md`](wiki-core/be/trien-khai/00-lo-trinh-tong-the.md) · [`wiki-core/fe/trien-khai/00-lo-trinh-tong-the.md`](wiki-core/fe/trien-khai/00-lo-trinh-tong-the.md) |
| **Kiến thức nền về core (chuẩn chung, không riêng dự án)** | [`wiki-core/`](wiki-core/) |
| **Một quyết định kiến trúc được đưa ra vì sao** | [`adr/`](adr/) |
| **Một sự cố đã xảy ra thế nào, cổng nào lẽ ra phải bắt** | [`audit/`](audit/) |

## Các khu — mỗi khu một vai

| Khu | Vai | Ai đọc |
| --- | --- | --- |
| [`quy-uoc/`](quy-uoc/) | **Quy ước THI CÔNG** — dự án dựng trên Core phải làm thế nào. Có code mẫu. | `backend-expert`, `frontend-expert` khi viết code |
| [`wiki-core/`](wiki-core/) | **Kiến thức nền** — một core tốt gồm gì và vì sao. Có thể vượt nhu cầu một dự án cụ thể. | `core-reviewer` khi audit; `architect` khi cân nhắc |
| [`Design/`](Design/) | **Giao diện** — token, component spec, screen spec, prompt pack. Phủ cả màn Core lẫn màn nghiệp vụ | `design-expert`, `frontend-expert` |
| [`contracts/`](contracts/) + [`database/`](database/) | **Hợp đồng & dữ liệu** — endpoint, schema, runbook | cả hai phía |
| [`luong/`](luong/) | **Luồng hoạt động** — khi người dùng làm X thì hệ thống đi qua những đâu, theo thứ tự nào, hỏng ở đâu. Sở hữu **thứ tự** và **mối nối**, không sở hữu định nghĩa nào | mọi agent trước khi thi công một chức năng Core |
| [`OWNERSHIP.md`](OWNERSHIP.md) | **Sổ chủ quyền** — nội dung nào thuộc file nào. Là đầu vào của cổng §15, không phải tài liệu để đọc hiểu | ai sắp viết một định nghĩa mới |
| [`adr/`](adr/) | **Quyết định** — vì sao chọn thế này chứ không thế kia | `architect`; ai muốn lật một quyết định |
| [`audit/`](audit/) | **Bài học** — postmortem sự cố, kết quả audit | mọi agent trước khi lặp lại một sai lầm |
| `spec/` *(ngoài `docs/`)* | **Nghiệp vụ theo feature** — `business-rules.md`, `ui-spec.md`. Đặt ngoài `docs/` là **có chủ đích** | `ba-analyst`, `backend-expert`, `frontend-expert` |

> **`docs/` giữ QUY TẮC, `spec/` giữ NGHIỆP VỤ.** Quy tắc kiến trúc/code luôn ở `docs/`, và agent phải tuân thủ chúng kể cả khi đang làm việc thuộc `spec/`.

**Khoảng cách giữa `quy-uoc/` và `wiki-core/` là cố ý.** Nó cho phép `core-reviewer` phân biệt *"lệch vì đã cố ý đơn giản hoá, đã thống nhất"* (không phải finding) với *"lệch vì thiếu sót thật"* (là finding). Đừng gộp hai khu này.

## Bảng định tuyến — cách viết để cổng đối chiếu được

Một bảng markdown không tự khai nó là bảng gì. Cổng §8 chỉ đối chiếu *"định danh ở ô chủ đề phải có trong file được trỏ tới"* cho những bảng **tự khai mình là bảng định tuyến**, và cách khai là **tiêu đề của cột cuối**.

Tập tiêu đề hợp lệ là **đầu vào của cổng**, không phải văn xuôi — đọc bằng lệnh, đừng chép tay:

```bash
grep -n 'last=="File"' .claude/check-docs.sh
```

Ba hệ quả cho người viết:

- Bảng nào muốn được canh thì đặt tiêu đề cột cuối theo tập đó. Đường dẫn khi ấy nằm ở **ô nào cũng được**.
- Bảng **văn xuôi** có kèm liên kết (bảng lý do, bảng nhật ký quyết định) cố ý **không** dùng tiêu đề trong tập — nếu không, định danh ở cột đầu sẽ bị đối chiếu với một file chỉ được nhắc làm dẫn chứng.
- 🛑 **Giới hạn đã biết:** đổi tiêu đề một bảng sang chữ ngoài tập sẽ **âm thầm** đưa bảng đó ra khỏi tầm canh. Cổng chỉ đỏ khi **mọi** bảng đều rơi ra ngoài. Đây là nợ có ý thức, ghi ở [`RULES.md`](RULES.md) §10.
- Bảng định tuyến của một **agent** còn tách hai phần theo **tiêu đề mục**: mục *Bộ luật* (cộng dồn, có ngưỡng cỡ) và mục *Tra cứu* (mở đúng một file, không cộng). Luật D36 ở [`RULES.md`](RULES.md) §1; cổng §23 đọc thẳng tiêu đề mục. Muốn thêm một file vào bảng của agent thì hỏi trước: *agent phải tuân nó ở mọi việc, hay chỉ mở khi chủ đề chạm tới?* — câu trả lời quyết định phần.

---

## Bảng trạng thái — ở CẤP KHU, không ở cấp file

| Khu | Trạng thái |
| --- | --- |
| [`quy-uoc/`](quy-uoc/) | 📐 **ĐÍCH ĐẾN** — quy ước cho `src/` sẽ xây ở giai đoạn 2 |
| [`wiki-core/`](wiki-core/) | ✅ sống — kiến thức nền, không phụ thuộc có `src/` hay chưa |
| [`Design/`](Design/) | ✅ sống — nguồn UI duy nhất |
| [`contracts/`](contracts/) | 📐 **ĐÍCH ĐẾN** — đọc `Status:` ở đầu **từng** card |
| [`database/`](database/) | 📐 **ĐÍCH ĐẾN** |
| [`luong/`](luong/) | 📐 **ĐÍCH ĐẾN** — mục lục khai đủ luồng |
| [`adr/`](adr/) | ✅ sống |
| [`audit/`](audit/) | ✅ sống |
| [`00-overview/`](00-overview/) | 🗄️ **lịch sử** — kế hoạch lập ở giai đoạn 1, không còn hiệu lực; lộ trình thi công tra ở bảng *Tra theo chủ đề* |

> 🛑 **Bảng này cố ý KHÔNG gắn nhãn cho từng file.** Trạng thái của một file đọc ở **đầu chính file đó**, cùng khoá `verified:` ở frontmatter.
>
> Lý do: ở dự án tiền nhiệm, mục lục từng gắn nhãn vào bảy dòng file. Một lần đối chiếu tìm ra **năm trong bảy** đã sai — trạng thái được chép ra chỗ thứ hai, và chỗ thứ hai không bao giờ được sửa cùng lúc. Đúng khuôn [`.claude/CLAUDE.md`](../.claude/CLAUDE.md) §5 cấm.

## Đếm bằng lệnh, đừng chép số

Theo [`.claude/CLAUDE.md`](../.claude/CLAUDE.md) §6, không chép số lượng file vào bất kỳ tài liệu nào:

```bash
find docs -name '*.md' | wc -l                    # tổng số file
grep -rL '^kind:' docs --include='*.md'           # file thiếu frontmatter
grep -rl 'verified: chua-doi-chieu' docs          # file chưa đối chiếu source
```

## Trước khi coi một thay đổi tài liệu là xong

```bash
bash .claude/check-docs.sh
```

Gate chặn hồi quy: đường dẫn trích dẫn tồn tại, link resolve, tuyên bố hoàn thành có ngày, `.claude/` không lẫn tri thức, mọi file khai đủ ba khoá.

**PASS không có nghĩa là nội dung đúng** — xem [`.claude/CLAUDE.md`](../.claude/CLAUDE.md) §8 để biết ba loại lỗi gate không bao giờ bắt được.
