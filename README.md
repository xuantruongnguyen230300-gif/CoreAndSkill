# CoreAndSkill

**Bộ khung dùng chung** cho các hệ thống nghiệp vụ tầm trung: một Core kỹ thuật mang đi được sang mọi dự án, cộng một tầng agent và tài liệu để việc phát triển bám đúng chuẩn.

Ba tài sản gắn chặt với nhau:

| Khu | Vai | Vào đâu để đọc |
| --- | --- | --- |
| **`docs/`** | **Nguồn tri thức duy nhất.** Kiến trúc, quy ước code, hợp đồng API, schema, thiết kế giao diện. Code và agent đều phải phục tùng. | [`docs/README.md`](docs/README.md) |
| **`.claude/`** | Bộ agent và skill: phân tích nghiệp vụ như BA, code như senior, review độc lập, sinh test, viết tài liệu. | [`.claude/README.md`](.claude/README.md) |
| **`spec/`** | Nghiệp vụ theo từng feature — không đi theo Core sang dự án khác. | [`spec/README.md`](spec/README.md) |
| **`src/`** | Source Core và các module. **Chưa tồn tại** — xem Trạng thái. | — |

---

## ⚠️ Trạng thái: giai đoạn 1

**Repo hiện chỉ có `docs/` và `.claude/`. Chưa có `src/`.**

Đây là trạng thái theo thiết kế, không phải dở dang: `src/` sẽ được xây ở giai đoạn 2 và **bám theo `docs/`**. Viết tài liệu trước cho phép quyết định kiến trúc được đưa ra lúc còn rẻ nhất để đảo.

Hệ quả cần biết khi đọc:

- Mọi mô tả kiến trúc trong `docs/` mang nhãn `📐 ĐÍCH ĐẾN — CHƯA THI CÔNG`.
- Gần như mọi file mang `verified: chua-doi-chieu` — chưa có source để đối chiếu. Đó là giá trị **trung thực**, không phải nợ.
- Nhóm skill sinh code (`/core-new-module`…) cố ý chưa viết: một skill scaffold cần một module mẫu đã chạy được để sao chép.

---

## Stack

| Lớp | Công nghệ |
| --- | --- |
| Backend | .NET 10 · ASP.NET Core · MediatR · FluentValidation |
| Dữ liệu | EF Core · PostgreSQL |
| Danh tính | ASP.NET Core Identity · phiên bằng cookie · antiforgery hai lớp |
| Frontend | Angular 20 standalone + signals · PrimeNG · ngx-translate |
| Kiến trúc | Modular Monolith — Core 5 project + N module, mỗi module một schema riêng |

Lý do của từng lựa chọn, kèm phương án đã loại và cái giá phải trả: [`docs/adr/`](docs/adr/).

---

## Bắt đầu từ đâu

| Bạn là | Đọc theo thứ tự |
| --- | --- |
| Người mới vào dự án | [`docs/README.md`](docs/README.md) → [`docs/kien-truc-core-module.md`](docs/kien-truc-core-module.md) → khu `quy-uoc/` của phần bạn làm |
| Người sắp viết code BE | [`docs/quy-uoc/be-architecture.md`](docs/quy-uoc/be-architecture.md) → [`be-cqrs-handler.md`](docs/quy-uoc/be-cqrs-handler.md) → [`be-api-controller.md`](docs/quy-uoc/be-api-controller.md) |
| Người sắp viết code FE | [`docs/quy-uoc/fe-architecture.md`](docs/quy-uoc/fe-architecture.md) → [`fe-ui-conventions.md`](docs/quy-uoc/fe-ui-conventions.md) → [`docs/Design/DESIGN.md`](docs/Design/DESIGN.md) |
| Người muốn biết luật nào bị ép bằng gì | [`docs/RULES.md`](docs/RULES.md) |
| Người muốn hiểu vì sao quyết định thế này | [`docs/adr/README.md`](docs/adr/README.md) |
| Người vừa gặp sự cố | [`docs/audit/README.md`](docs/audit/README.md) |

---

## Cổng

Giai đoạn 1 có **một** cổng, chạy từ gốc repo:

```bash
bash .claude/check-docs.sh
```

Nó kiểm những gì máy kiểm được: đường dẫn trích dẫn có tồn tại, link có resolve, trích dẫn `file:dòng` có nằm trong file, tuyên bố hoàn thành có kèm ngày, `.claude/` có lẫn tri thức không, mọi file có khai đủ ba khoá phân loại không.

CI chạy cổng này trên mỗi pull request — xem [`.github/workflows/docs-gate.yml`](.github/workflows/docs-gate.yml).

> ⚠️ **PASS không có nghĩa là tài liệu ĐÚNG.** Cổng không đọc hiểu nội dung. Ba loại lỗi nó không bao giờ bắt được — văn xuôi tả thứ không tồn tại, sơ đồ chép sai, ngày đúng nhưng nội dung sai — liệt kê ở [`.claude/CLAUDE.md`](.claude/CLAUDE.md) §8.

Cổng backend và frontend sẽ được thêm ở giai đoạn 2. Chúng **chưa tồn tại**; đừng nhắc tới chúng như thể đã có.

---

## Nguyên tắc chi phối repo

Bốn điều quyết định cách mọi thứ ở đây được viết. Luật đầy đủ ở [`.claude/CLAUDE.md`](.claude/CLAUDE.md).

1. **`.claude/` giữ quy trình, `docs/` giữ tri thức.** Phép thử: *`.claude/` không được chứa câu nào có thể trở thành SAI khi code thay đổi.* Nội dung đi vào `docs/`; `.claude/` chỉ nhận đường dẫn.
2. **Một chủ đề, một file chủ.** Hai file cùng mô tả một thứ thì chúng sẽ lệch nhau, và không ai sửa cả hai cùng lúc.
3. **Không chép vào tài liệu thứ đếm được bằng lệnh.** Bảng liệt kê tay luôn mục ruỗng — thay bằng lệnh kèm tiêu chí đạt.
4. **Mọi luật phải trả lời được "ép bằng cái gì".** Trả lời "bằng niềm tin" thì nó là gợi ý, không phải luật — và chỗ của nó là danh sách nợ, nhìn thấy được.

---

## Git

Agent **không** chạy lệnh git làm thay đổi trạng thái repo — điều này được cưỡng chế bằng cấu hình harness, không bằng câu văn. Commit, push, tạo nhánh là việc của người dùng.
