---
kind: luat
scope: core
verified: chua-doi-chieu
---

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Ba ArchTest và một integration test dưới đây canh các luật E4/E5/E6/E8 của
> [`../RULES.md`](../RULES.md) §4; tên trong bảng **chưa tồn tại** trong `src/BE` — xác nhận bằng
> lệnh tự-kiểm ở cuối §7.

# Phần chưa thi công — `migration-policy.md`

> File này giữ đặc tả cho các ArchTest mà [`migration-policy.md`](migration-policy.md) §7 và §7.1
> mô tả nhưng chưa ai viết. Khi một detector được viết xong, chuyển đúng phần của nó về lại file
> gốc (kèm bằng chứng đối chiếu) và xoá khỏi đây.

---

## 7. Cổng nào canh những luật này

| Luật | Ép bằng | Bắt được gì |
| --- | --- | --- |
| E4 `EveryMappedEntity_LivesInTheSchemaOfItsSide` | ArchTest | Entity của Core rơi vào schema module, hoặc ngược lại. Đây là lỗi im lặng: bảng vẫn dựng, vẫn chạy, chỉ sai chỗ — và chỉ lộ ra vào ngày tách module |
| E5 `NoForeignKey_CrossesSchemaBoundary` | ArchTest | Navigation property nối hai schema, tức FK vật lý xuyên ranh giới. Bắt ở tầng model; câu kiểm (4) ở [`schema-core.md`](schema-core.md) §11 bắt phần SQL viết tay |
| E6 `EveryMigration_LivesIn_ItsOwningProject` | ArchTest | Migration chạm schema `core` nằm trong project module ngoài ngoại lệ khoá quyền ([`migration-policy.md`](migration-policy.md) §1), hoặc ngược lại — tức chính ca hai chủ sở hữu mà §1.1 của file đó mô tả |
| E8 `Startup_Fails_When_PendingMigrationsExist` | Integration test, PostgreSQL thật | App khởi động được trong khi DB thật còn thiếu migration. Xem [`script-runbook.md`](script-runbook.md) §5 |

Các dòng trên nằm trong bảng §4 của [`../RULES.md`](../RULES.md). Riêng E8 đã có một lớp không
database; tên ở bảng này là phần còn nợ — hàng E8 của `RULES.md` và của [`../DEBT.md`](../DEBT.md).

Kiểm bằng một lệnh phủ **mọi** tên trong bảng, không phải một tên — hai con số chép tay không khớp
nhau thì lệnh đi kèm chỉ chứng minh được đúng một dòng:

```bash
grep -rnE 'EveryMappedEntity_LivesInTheSchemaOfItsSide|NoForeignKey_CrossesSchemaBoundary|EveryMigration_LivesIn_ItsOwningProject|Startup_Fails_When_PendingMigrationsExist' src/BE --include='*.cs'
```

PASS của câu *"chưa ai viết"* là lệnh in **rỗng** (đo lại 2026-09-27, cùng kết quả rỗng đo ngày
2026-09-20). In ra dòng nào thì đúng detector đó đã tồn tại, và hàng tương ứng ở
[`../RULES.md`](../RULES.md) §4 phải đổi trạng thái — đừng để lệnh và bảng nói khác nhau.

Khi viết chúng, nhớ luật T1: **mỗi detector phải có test kiểm chính detector đó**. Một ArchTest
xanh vì nó không quét gì cả là tệ hơn không có ArchTest, vì nó tạo cảm giác được bảo vệ.

### 7.1 E6 kiểm gì cho cụ thể

Detector đọc từng file migration, tìm tên schema xuất hiện trong nó, rồi đối chiếu với project
chứa file:

| File migration nằm ở | Được phép chạm schema |
| --- | --- |
| `Core.Infrastructure` | `core` — và chỉ `core` |
| `Modules.<X>.Infrastructure` | `<x>`; với `core` **chỉ** đúng dạng của ngoại lệ khoá quyền ở [`migration-policy.md`](migration-policy.md) §1 |

Một migration của module chạm bảng `core` **ngoài** ngoại lệ đó là dấu hiệu module đang "sửa hộ" Core,
và bản vá Core lần sau sẽ đè lên nó hoặc xung đột với nó.

Ngoại lệ đi bằng SQL viết tay trong migration — dạng `ON CONFLICT … DO NOTHING` không có trong thao tác
chèn dữ liệu dựng sẵn của EF — nên phần này của detector buộc phải đọc văn bản lệnh SQL của migration
module: mọi tham chiếu `core.` chỉ được nằm trong câu `INSERT` vào hai bảng đã nêu, kèm
`ON CONFLICT … DO NOTHING`. Lời cảnh báo ngay dưới áp nguyên văn cho phần đó.

> ⚠️ **Bài học khi thi công detector này:** ở dự án tiền nhiệm, một detector cùng loại quét **văn
> bản nguồn** thay vì cấu trúc, nên code phải viết vòng để né nó — đuôi vẫy chó. Ưu tiên đọc
> model EF (`IModel` của từng `DbContext`) thay vì `grep` chuỗi trong file `.cs`. Nếu buộc phải
> quét văn bản, detector phải có test chứng minh nó bỏ qua comment và định danh.
