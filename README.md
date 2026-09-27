# CoreAndSkill

**Bộ khung dùng chung** cho các hệ thống nghiệp vụ tầm trung: một Core kỹ thuật mang đi được sang mọi dự án, cộng một tầng agent và tài liệu để việc phát triển bám đúng chuẩn.

Ba tài sản gắn chặt với nhau:

| Khu | Vai | Vào đâu để đọc |
| --- | --- | --- |
| **`docs/`** | **Nguồn tri thức duy nhất.** Kiến trúc, quy ước code, hợp đồng API, schema, thiết kế giao diện. Code và agent đều phải phục tùng. | [`docs/README.md`](docs/README.md) |
| **`.claude/`** | Bộ agent và skill: phân tích nghiệp vụ như BA, code như senior, review độc lập, sinh test, viết tài liệu. | [`.claude/README.md`](.claude/README.md) |
| **`spec/`** | Nghiệp vụ theo từng feature — không đi theo Core sang dự án khác. | [`spec/README.md`](spec/README.md) |
| **`src/`** | Source Core và các module. Đã có trên đĩa, **chưa vào git** — xem Trạng thái. | — |

---

## ⚠️ Trạng thái: 🚧 ĐÃ CHỐT — ĐANG THI CÔNG

**`src/` đã có trên đĩa nhưng chưa vào git.** Tám điều kiện chuyển sang giai đoạn 2 ở [`docs/adr/0030-dieu-kien-chuyen-giai-doan-2.md`](docs/adr/0030-dieu-kien-chuyen-giai-doan-2.md) đã đạt — điều kiện 3 hoãn hợp lệ theo cơ chế thay thế ở [`docs/adr/0035-hoan-dieu-kien-3-den-pr-cham-core-dau-tien.md`](docs/adr/0035-hoan-dieu-kien-3-den-pr-cham-core-dau-tien.md) (chứng minh ở PR đầu tiên chạm một đường dẫn `core-paths`, chưa xảy ra). `architect` đối chiếu tám điều kiện ngày 2026-09-16; nhãn này lật cùng đợt.

Điều kiện chuyển sang giai đoạn 2, và ai lật nhãn trạng thái: [`docs/adr/0030-dieu-kien-chuyen-giai-doan-2.md`](docs/adr/0030-dieu-kien-chuyen-giai-doan-2.md).

**Nguồn duy nhất của mục này** — cái gì có trên đĩa, cái gì trong git, cổng nào vì thế chưa chạy: [`docs/README.md`](docs/README.md) mục *Trạng thái repo*. Dưới đây chỉ là hệ quả cho người mới mở repo.

Hệ quả cần biết khi đọc:

- Mọi mô tả kiến trúc trong `docs/` vẫn mang nhãn `📐 ĐÍCH ĐẾN — CHƯA THI CÔNG` hoặc `verified: chua-doi-chieu` cho tới khi có người đối chiếu trực tiếp đúng file đó với `src/` — **riêng lẻ, không hàng loạt**. Nhãn ở mục này chỉ nói *quyết định chuyển giai đoạn đã chốt*, không xác nhận nội dung từng file đã đối chiếu.
- Lớp 3 chặn Core (hook `Stop` khi có `src/`) **chưa được thử bằng payload thật** — chỉ chứng minh sống ở PR đầu tiên chạm Core, xem ADR-0035.
- Nhóm skill sinh code (`/core-new-module`…) cố ý chưa viết: một skill scaffold cần một module mẫu đã chạy được để sao chép.

---

## Stack

| Lớp | Công nghệ |
| --- | --- |
| Backend | .NET 10 · ASP.NET Core · MediatR · FluentValidation |
| Dữ liệu | EF Core · PostgreSQL |
| Danh tính | ASP.NET Core Identity · phiên bằng cookie · antiforgery hai lớp |
| Frontend | Angular standalone + signals · PrimeNG · ngx-translate — phiên bản và kế hoạch nâng cấp: [`docs/adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md`](docs/adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md) |
| Kiến trúc | Modular Monolith — Core tách project theo tầng + N module, mỗi module một schema riêng. Danh sách project: [`docs/kien-truc-core-module.md`](docs/kien-truc-core-module.md) §2 |

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

Cổng chạy được trên máy bất kỳ lúc nào, từ gốc repo:

```bash
bash .claude/check-docs.sh
```

Nó kiểm những gì máy kiểm được: đường dẫn trích dẫn có tồn tại, link có resolve, trích dẫn `file:dòng` có nằm trong file, tuyên bố hoàn thành có kèm ngày, `.claude/` có lẫn tri thức không, mọi file có khai đủ ba khoá phân loại không.

CI chạy cổng này trên mỗi pull request — xem [`.github/workflows/docs-gate.yml`](.github/workflows/docs-gate.yml).

> ⚠️ **PASS không có nghĩa là tài liệu ĐÚNG.** Cổng không đọc hiểu nội dung. Ba loại lỗi nó không bao giờ bắt được — văn xuôi tả thứ không tồn tại, sơ đồ chép sai, ngày đúng nhưng nội dung sai — liệt kê ở [`.claude/CLAUDE.md`](.claude/CLAUDE.md) §8.

Cổng backend và frontend **đã có** trong [`.github/workflows/docs-gate.yml`](.github/workflows/docs-gate.yml), bật theo điều kiện `src/BE` / `src/FE` có mặt trong cây checkout. Vì `src/` chưa vào git nên chúng **chưa chạy lần nào** — đừng trích kết quả của chúng như thể đã có.

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
