---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Contract card — Meta / Menu

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Mọi card trong file này mang `Status: DRAFT`.
>
> Envelope, `ErrorType` → HTTP, khuôn mã lỗi: [`README.md`](README.md). Bảng dữ liệu:
> [`../database/schema-core.md`](../database/schema-core.md) §6.

---

## 1. `GET /api/v1/core/meta/menu`

**Status:** DRAFT
**Quyền:** `[Authorize]` — chỉ cần đăng nhập, không cần permission riêng

Trả cây điều hướng mà **người dùng hiện tại** được thấy. FE gọi một lần sau khi đăng nhập.

### Response 200 — danh sách PHẲNG, không lồng cây

```json
{
  "success": true,
  "data": [
    {
      "id": "0192f3e0-0001-7000-8000-000000000001",
      "parentId": null,
      "code": "trang-chu",
      "labelKey": "menu.trang-chu",
      "icon": "pi-home",
      "route": "/trang-chu",
      "displayOrder": 10
    },
    {
      "id": "0192f3e0-0001-7000-8000-000000000002",
      "parentId": null,
      "code": "quan-tri",
      "labelKey": "menu.quan-tri",
      "icon": "pi-cog",
      "route": null,
      "displayOrder": 90
    },
    {
      "id": "0192f3e0-0001-7000-8000-000000000003",
      "parentId": "0192f3e0-0001-7000-8000-000000000002",
      "code": "quan-tri-nguoi-dung",
      "labelKey": "menu.quan-tri-nguoi-dung",
      "icon": "pi-users",
      "route": "/quan-tri/nguoi-dung",
      "displayOrder": 10
    }
  ],
  "error": null,
  "traceId": "0HNO9S8JAP586:00000040"
}
```

| Field | Kiểu | Ghi chú |
| --- | --- | --- |
| `id` | uuid | |
| `parentId` | uuid \| null | `null` = mục gốc. Trỏ tới một mục **cũng** có `parentId: null` — cây đúng một cấp |
| `code` | string | Khoá ổn định, dùng cho `@for` `track` phía FE |
| `labelKey` | string | **Khoá i18n**, không phải nhãn — xem §1.2 |
| `icon` | string \| null | Class icon THẬT (`pi-home`), FE dùng thẳng làm class CSS |
| `route` | string \| null | `null` cho mục cha (chỉ đóng/mở, không điều hướng) |
| `displayOrder` | int | Thứ tự trong cùng một cấp |

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.AUTH.NOT_AUTHENTICATED` | `Unauthorized` | 401 | Chưa đăng nhập. **JSON sạch, không 302 redirect** |
| `CORE.AUTH.PASSWORD_CHANGE_REQUIRED` | `Forbidden` | 403 | Đang ở trạng thái bắt buộc đổi mật khẩu ([`auth.md`](auth.md) §1.2) |

### 1.1 Vì sao PHẲNG chứ không lồng sẵn cây

| | Phẳng | Lồng sẵn |
| --- | --- | --- |
| Hình dạng response | Một mảng, một kiểu phần tử | Kiểu đệ quy — mỗi phần tử có thể chứa mảng cùng kiểu |
| Đổi từ 1 cấp lên 2 cấp | **Không đổi hợp đồng**; chỉ FE đổi hàm dựng cây | Đổi kiểu response, tức breaking change |
| FE phải làm gì | Một hàm `buildMenuTree()` chạy một lần | Không phải làm gì |

Chi phí phía FE là một hàm ngắn, chạy đúng một lần sau đăng nhập. Đổi lại, hợp đồng ổn định
trước một thay đổi cấu trúc gần như chắc chắn sẽ tới.

> Ở dự án tiền nhiệm, bản đầu BE trả **cây lồng sẵn** trong khi FE đã code sẵn theo shape phẳng.
> Lệch chỉ được phát hiện ở một lượt audit chéo, và BE phải sửa theo FE. Card này chốt shape
> **trước** khi cả hai bên viết dòng nào — đó là toàn bộ lý do khu `contracts/` tồn tại.

### 1.2 `labelKey` là khoá i18n, KHÔNG phải nhãn

BE trả `"menu.quan-tri"`, không trả `"Quản trị hệ thống"`.

**Vì sao:** người dùng đổi ngôn ngữ **ngay trong app**, không tải lại trang. Một câu tiếng Việt
do BE trả về là chuỗi **đã cố định trước khi** người dùng chọn ngôn ngữ, và không có gì ở FE tra
ngược lại được. Luật R8 ([`../RULES.md`](../RULES.md) §5) cấm câu hiển thị nằm trong BE, luật F8
cấm chữ tiếng Việt trong template FE — cả hai cùng chỉ về một chỗ: **bảng dịch**.

Thiếu bản dịch cho một khoá thì FE hiện chính khoá đó. Xấu, nhưng **nhìn là biết thiếu gì** — tốt
hơn hẳn một câu tiếng Việt lọt vào giao diện tiếng Anh mà không ai để ý.

📖 [`../wiki-core/fe/08-i18n.md`](../wiki-core/fe/08-i18n.md)

### 1.3 `icon` trả THẲNG class CSS, không phải khoá trừu tượng

FE dùng nguyên `icon` làm class, chỉ thay bằng một icon mặc định khi BE trả `null`. **Không** có
bảng ánh xạ khoá → class ở FE.

Bảng ánh xạ đó là một nguồn thứ hai: thêm một icon phải sửa hai chỗ, và ngày quên chỗ thứ hai thì
menu hiện ra không có icon mà không có lỗi nào. Ở dự án tiền nhiệm, cơ chế ánh xạ này đã tồn tại
rồi bị gỡ bỏ vì đúng lý do đó.

Đánh đổi: BE biết tên một class CSS. Chấp nhận được — nó là một chuỗi **dữ liệu trong database**,
do người seed menu đặt, không phải một hằng số trong `Core.*`.

### 1.4 Lọc theo quyền — thứ tự quyết định

Đúng ba bước, không có nhánh nào khác. Định nghĩa đầy đủ ở
[`../database/schema-core.md`](../database/schema-core.md) §6.3:

| Bước | Điều kiện trên mục menu | Kết quả |
| --- | --- | --- |
| 1 | Có quyền yêu cầu | Hiện ⇔ người dùng có quyền đó. **Bỏ qua hoàn toàn danh sách vai trò** |
| 2 | Không có quyền yêu cầu, nhưng có gán vai trò | Hiện ⇔ người dùng thuộc một trong các vai trò đó |
| 3 | Không có cả hai | Hiện cho **mọi** người dùng đã đăng nhập |

**Bước 1 thắng tuyệt đối.** Nếu hai cơ chế cùng có hiệu lực — giao hoặc hợp — sẽ có ca một mục ẩn
đi mà không ai chỉ ra được dòng dữ liệu nào gây ra, vì cả hai đều "đúng một nửa".

> **Menu là HIỂN THỊ, không phải phân quyền.** Mục menu ẩn đi không chặn được ai gõ thẳng URL, và
> cũng không chặn được ai gọi thẳng API. Chặn thật nằm ở attribute kiểm quyền trên endpoint. Ẩn
> một mục menu mà quên gắn quyền cho endpoint tương ứng là một lỗ hổng, không phải một lớp bảo vệ.

### 1.5 Mục cha luôn được kéo vào khi có con hiện

> **Nếu bất kỳ con nào của một mục cha được hiện, mục cha PHẢI có mặt trong response** — kể cả
> khi bản thân mục cha bị quy tắc §1.4 loại.

Bắt buộc, không phải tuỳ chọn: FE dựng cây từ `parentId`, nên thiếu dòng cha thì con thành **mồ
côi** — hàm dựng cây không tìm thấy cha, coi nó là mục gốc, và nó xuất hiện sai vị trí trên
sidebar. Triệu chứng nhìn thấy là một mục con nằm chình ình ở cấp cao nhất.

Ví dụ với dữ liệu ở §1: một người **chỉ** có `core.user.read` sẽ thấy `quan-tri-nguoi-dung`
(bước 1 cho qua). Mục cha `quan-tri` được kéo vào **vì có con hiện**, dù chính nó có thể đang bị
ghim cho một vai trò mà người này không thuộc.

### 1.6 🪤 Mục menu mồ côi do module đã gỡ

Menu do module đóng góp mang `moduleKey` khác `null`. Gỡ một module khỏi solution **không** tự
xoá dòng menu của nó khỏi database.

Triệu chứng: sidebar hiện một mục, bấm vào thì route không tồn tại nên FE **âm thầm** đưa về
trang chủ. Người dùng không nhận được thông báo nào và không hiểu chuyện gì vừa xảy ra. Ở dự án
tiền nhiệm, đúng ba mục như vậy tồn tại trong nhiều tuần.

Hai đường xử lý, chọn một và ghi vào tài liệu triển khai của dự án:

| Đường | Cách | Đánh đổi |
| --- | --- | --- |
| Dọn khi gỡ module | Script gỡ module xoá mềm mọi mục menu mang `moduleKey` của nó | Cần nhớ chạy. Module quay lại thì seeder của nó dựng lại |
| Lọc lúc chạy | Host khai danh sách module đang lắp; query menu bỏ qua mục có `moduleKey` không thuộc danh sách | Không phải nhớ gì, nhưng dữ liệu rác vẫn nằm trong database |

**Chốt (2026-09-10): lọc lúc chạy.** Host khai danh sách module đang lắp; truy vấn menu bỏ qua
mục có `moduleKey` không thuộc danh sách đó. Lý do: nó không phụ thuộc vào việc ai đó nhớ chạy một
script — câu *"cơ chế nào không phụ thuộc trí nhớ"* là câu quyết định ở gần như mọi lựa chọn trong
repo này.

Hệ quả phải giữ nguyên, không "dọn cho sạch": dữ liệu menu của module đã gỡ **vẫn nằm trong
database**. Lắp module trở lại thì mục cũ hiện lại — đúng thứ ta muốn. Dọn dữ liệu thừa là việc
vận hành, không phải điều kiện để menu hiển thị đúng.

---

## 2. Quản trị menu — 📐 chưa có endpoint

Ở v1, menu là **dữ liệu seed**: Core cung cấp cơ chế, dự án cung cấp danh sách mục qua một seam
mà host khai, và dữ liệu vào database qua bước seed
([`../database/script-runbook.md`](../database/script-runbook.md) §6).

**Không có `POST`/`PUT`/`DELETE` cho mục menu ở v1.** Lý do:

| Lý do | Chi tiết |
| --- | --- |
| Menu gắn chặt với **route của FE** | Thêm một mục menu trỏ tới route chưa tồn tại tạo ra đúng triệu chứng §1.6. Route là code, nên menu nên đi cùng vòng đời của code |
| Cấu hình vòng đời code không nên sửa qua UI | Nó không đi qua review, không đi qua CI, và không có ở môi trường dev — tức không tái hiện được |
| Chưa có nhu cầu thật | Ngưỡng ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.2: chỉ đưa vào Core khi từ hai module trở lên cần |

Cái **được phép** sửa qua UI là **ai thấy mục nào** — đó là ma trận phân quyền
([`permissions.md`](permissions.md)), không phải bản thân cây menu.

Khi có nhu cầu thật, endpoint quản trị menu sẽ thêm vào file này với `Status: DRAFT` và một ADR
đi kèm.

---

## 3. Metadata lưới và form — 📐 NGOÀI PHẠM VI v1

> **Chốt (2026-09-10): giữ NGOÀI v1.** Hai endpoint dưới đây ghi ra để định hình sớm, không phải
> để thi công.
>
> Điều kiện xem lại: có đủ nhiều màn danh sách lặp cùng một khuôn tới mức viết tay từng cột thành
> gánh nặng thật. Trước ngưỡng đó, lớp mô tả này chỉ thêm một tầng gián tiếp mà mọi tuỳ biến nhỏ
> đều phải luồn qua.

Ý tưởng: BE mô tả **cấu hình hiển thị** của một lưới hoặc một form, FE dựng giao diện từ mô tả đó
thay vì viết tay từng cột.

📖 Nền tảng:
[`../wiki-core/be/03-metadata-driven-design.md`](../wiki-core/be/03-metadata-driven-design.md) ·
[`../wiki-core/fe/11-grid-and-metadata.md`](../wiki-core/fe/11-grid-and-metadata.md)

### 3.1 `GET /api/v1/core/meta/grid/{key}`

**Status:** DRAFT · 📐 chưa thuộc v1
**Quyền:** `[Authorize]`

```json
{
  "success": true,
  "data": {
    "key": "core.user",
    "defaultSortBy": "userName",
    "defaultSortDescending": false,
    "defaultPageSize": 20,
    "columns": [
      { "field": "userName",  "labelKey": "user.field.userName",  "type": "text",  "sortable": true,  "width": 180 },
      { "field": "fullName",  "labelKey": "user.field.fullName",  "type": "text",  "sortable": true,  "width": 220 },
      { "field": "isLocked",  "labelKey": "user.field.status",    "type": "badge", "sortable": false, "width": 120 },
      { "field": "createdAt", "labelKey": "user.field.createdAt", "type": "date",  "sortable": true,  "width": 160 }
    ]
  },
  "error": null,
  "traceId": "0HNO9S8JAP586:00000041"
}
```

`sortable: true` của một cột **phải** khớp allowlist `sortBy` của endpoint danh sách tương ứng
([`users.md`](users.md) §3). Lệch nhau nghĩa là UI cho bấm sắp xếp một cột mà API trả 400.

### 3.2 `GET /api/v1/core/meta/form/{key}`

**Status:** DRAFT · 📐 chưa thuộc v1
**Quyền:** `[Authorize]`

```json
{
  "success": true,
  "data": {
    "key": "core.user.create",
    "fields": [
      { "name": "userName", "labelKey": "user.field.userName", "type": "text",  "required": true, "maxLength": 256 },
      { "name": "email",    "labelKey": "user.field.email",    "type": "email", "required": true, "maxLength": 256 },
      { "name": "fullName", "labelKey": "user.field.fullName", "type": "text",  "required": true, "maxLength": 200 },
      { "name": "roleIds",  "labelKey": "user.field.roles",    "type": "multiselect", "required": false,
        "optionsEndpoint": "/api/v1/core/permissions/matrix" }
    ]
  },
  "error": null,
  "traceId": "0HNO9S8JAP586:00000042"
}
```

`name` là tên property **PascalCase-hoá** khi tra `fieldErrors` (`userName` → `fieldErrors["UserName"]`)
— xem luật casing ở [`README.md`](README.md) §4.

### Lỗi (cả hai)

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.META.KEY_NOT_FOUND` | `NotFound` | 404 | `{key}` không có trong danh mục metadata |
| `CORE.AUTH.NOT_AUTHENTICATED` | `Unauthorized` | 401 | |

### 3.3 Ba câu phải trả lời trước khi chốt

Ghi ra vì đây là chỗ dễ xây quá tay nhất trong cả hệ thống:

| # | Câu hỏi | Vì sao quan trọng |
| --- | --- | --- |
| 1 | Metadata là **dữ liệu trong database** hay **hằng số trong code**? | Trong database thì sửa được lúc chạy nhưng thoát khỏi review và CI. Trong code thì ngược lại. Không có đáp án đúng chung — nhưng phải chọn **một** |
| 2 | `required`/`maxLength` ở đây có phải nguồn của validate không? | Nếu **có**, nó phải sinh ra cùng lúc với validator BE, nếu không hai bên sẽ lệch. Nếu **không**, nó chỉ là gợi ý UI và phải nói rõ thế — người đọc sẽ mặc định hiểu là nguồn |
| 3 | Metadata có **lọc theo quyền** không? | Ẩn một cột theo quyền là hợp lý; nhưng nếu API danh sách vẫn trả cột đó thì dữ liệu đã ra khỏi server rồi, và ẩn ở FE không phải bảo vệ |

> **Rủi ro lớn nhất của hướng metadata-driven: nó hấp dẫn quá mức.** Rất dễ trượt từ *"mô tả cột
> của lưới"* sang *"mô tả toàn bộ giao diện"*, và điểm cuối của đường đó là một ngôn ngữ lập
> trình bằng JSON — không gõ kiểu, không debug được, không test được, và mọi trường hợp riêng
> phải thêm một cờ mới.
>
> Ranh giới đề xuất: metadata mô tả **cột và ô nhập**. Điều kiện hiển thị, luật nghiệp vụ, thao
> tác — viết bằng code.
