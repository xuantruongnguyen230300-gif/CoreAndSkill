---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Contract card — Permissions (phân quyền)

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Mọi card trong file này mang `Status: DRAFT`.
>
> Envelope, `ErrorType` → HTTP, khuôn mã lỗi: [`README.md`](README.md). Bảng dữ liệu:
> [`../database/schema-core.md`](../database/schema-core.md) §5.

---

## 1. Mô hình — và thứ KHÔNG có trong file này

```text
người dùng ──< vai trò >──< quyền >── tài nguyên
```

**Không endpoint nào trong file này dựa trên một hằng số vai trò.** Không có
`?role=SuperAdmin`, không có nhánh `if (role == "Admin")`, không có danh sách vai trò cố định
trong schema request. Vai trò là **dữ liệu**: mọi endpoint đọc chúng từ bảng vai trò, và số lượng
lẫn tên gọi do dự án đặt.

Đây là hệ quả trực tiếp của [`../adr/0005-permission-based.md`](../adr/0005-permission-based.md)
và luật S1/S2 ở [`../RULES.md`](../RULES.md) §6.

> **Đây là chỗ lệch lớn nhất so với dự án tiền nhiệm.** Ở đó, ma trận có đúng ba cột
> `SuperAdmin`/`Admin`/`User` khai cứng trong validator — thêm một vai trò thứ tư là sửa code
> backend. Ở đây, thêm vai trò là thêm một dòng trong bảng.

**Quyền dùng trong file này:** `core.permission.read` (đọc) và `core.permission.write` (ghi).
Ý nghĩa từng khoá và danh mục đầy đủ:
[`../database/schema-core.md`](../database/schema-core.md) §5.2 — card không chép lại danh mục
([`../OWNERSHIP.md`](../OWNERSHIP.md) §2).

---

## 2. Deny-by-default — và hệ quả của nó

> **Không có dòng cấp quyền nghĩa là KHÔNG được phép.**

Hai hệ quả phải thiết kế quanh, không phải phát hiện sau:

1. **Ma trận rỗng nghĩa là mọi endpoint có kiểm quyền trả 403 cho mọi người** — trừ tài khoản
   mang `has_permission_bypass`. Đây là trạng thái của một đơn vị chưa vai trò nào được cấp quyền.
   Danh mục quyền thì không chờ ai nạp: nó vào database **bằng migration idempotent**, cùng lượt
   áp schema, không có script nạp riêng
   ([`../database/migration-policy.md`](../database/migration-policy.md) §4.1). Khoá khai trong
   code qua seam ([`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §1.1), và tiến
   trình ứng dụng **không** ghi danh mục.
2. **Danh sách hàng của ma trận KHÔNG được dựng từ bảng cấp quyền.** Nếu dựng từ đó, quyền chưa
   cấp cho ai sẽ **biến mất khỏi UI** — và không còn đường nào cấp nó nữa. Hàng luôn dựng từ danh
   mục quyền; ô chưa cấp hiện ra là ô trống, không phải hàng vắng mặt.

Điểm 2 là loại lỗi tự nhốt mình: hệ thống vẫn chạy, màn hình vẫn mở, chỉ là một tính năng không
bao giờ bật lên được và không ai đoán ra vì sao.

---

## 3. Phiên bản ma trận — chống ghi đè khi hai người sửa cùng lúc

### 3.1 Hai sự cố mà cơ chế này chặn

| Sự cố | Chuyện gì xảy ra nếu không có gì chặn |
| --- | --- |
| **Ghi đè lẫn nhau** | A và B cùng mở màn phân quyền. A tick thêm hai ô rồi lưu. B (đang xem bản cũ) lưu sau — payload của B **không có** hai ô đó, và ghi đè xoá chúng. A không biết gì cả |
| **Tải thiếu rồi lưu** | Màn hình tải được một phần danh mục vì lỗi mạng giữa chừng. Người dùng bấm Lưu. Payload thiếu phần chưa tải ⇒ **thu hồi sạch** phần đó |

Cả hai đều không sinh lỗi nào. Chúng chỉ hiện ra khi có người phát hiện quyền của mình biến mất —
thường là nhiều ngày sau, và không ai nối được nó với thao tác nào.

### 3.2 Cơ chế

> 📖 **Token đi trên dây theo khuôn chung của mọi endpoint ghi — `GET` trả `version`, `PUT` gửi
> lại trong body, lệch ⇒ 409 không ghi gì: đọc
> [`../wiki-core/be/06-concurrency-control.md`](../wiki-core/be/06-concurrency-control.md) §6.3.**
> Card này không tả lại; bảng dưới chỉ giữ phần **riêng** của ma trận.

| # | Luật riêng của ma trận |
| --- | --- |
| 1 | `version` là băm của **cả tập** (§3.3), không phải token của một dòng |
| 2 | Lệch `version` trả mã **riêng** `CORE.PERMISSION.VERSION_MISMATCH` (§6), không phải mã dùng chung — vì xung đột ở đây là xung đột của tập |
| 3 | `PUT` phải **phủ đủ** mọi quyền trong danh mục. Thiếu bất kỳ phần tử nào ⇒ **400**, **không ghi gì** |

**Luật 3 mạnh hơn hẳn "cấm mảng rỗng".** Một payload tải được 4 trên 7 quyền vẫn thu hồi 3 quyền
kia, mà một phép kiểm `NotEmpty()` không thấy gì bất thường ở payload đó.

Nó cũng **đổi ngữ nghĩa** một cách có chủ đích: vắng mặt một phần tử **không** còn nghĩa là "thu
hồi sạch quyền đó" — nay là **lỗi của client**. Muốn thu hồi sạch thì gửi phần tử đó với
`roleIds: []`.

> **Một ngữ nghĩa mà SỰ VẮNG MẶT mang ý nghĩa phá huỷ là ngữ nghĩa không an toàn**, vì mọi lỗi
> tải thiếu — mạng chập, phân trang sai, ghép mảng hỏng — đều trở thành một lệnh xoá hợp lệ.

### 3.3 `version` tính thế nào

Băm trên tập cặp (vai trò, quyền) **đã sắp thứ tự ổn định**, lấy từ chính bảng cấp quyền. Không
thêm cột, không thêm bảng.

Ba tính chất, mỗi cái đều là lý do chọn cách này thay vì một token lưu sẵn:

| Tính chất | Vì sao quan trọng |
| --- | --- |
| Không có trạng thái phụ | Không có cột nào để quên cập nhật, không có gì để lệch với dữ liệu thật |
| Phát hiện được **mọi** thay đổi | Kể cả thay đổi do một câu `UPDATE` chạy tay trên database |
| Tính lại được bất cứ lúc nào | Không phụ thuộc lịch sử; database khôi phục từ backup vẫn cho token đúng |

> 🪤 **Hàm băm KHÔNG được `IgnoreQueryFilters()`.** Ghi đè ma trận xoá **mềm** dòng cũ
> ([`../database/schema-core.md`](../database/schema-core.md) §3.3), nên nếu băm tính cả dòng đã
> xoá mềm, nó đổi giá trị sau **mỗi** lần lưu — kể cả khi trạng thái nhìn thấy được không đổi.
> Hệ quả: hai người lưu **tuần tự** (không hề tranh chấp) vẫn nhận 409 ở lần thứ hai, và không ai
> hiểu vì sao.

> 📖 Vì sao token cấp **tập hợp** chứ không phải concurrency token theo dòng:
> [`../wiki-core/be/06-concurrency-control.md`](../wiki-core/be/06-concurrency-control.md).
> Tóm tắt: đây là thao tác ghi đè cả tập, nên xung đột là xung đột của **tập**, không của dòng.

---

## 4. `GET /api/v1/core/permissions`

**Status:** DRAFT
**Quyền:** `core.permission.read`

Danh mục quyền khả dụng của hệ thống — **không** phải ma trận. FE dùng nó để dựng bộ lọc, và để
kiểm tra một mã quyền có tồn tại không.

### Response 200

```json
{
  "success": true,
  "data": [
    {
      "id": "0192f3d0-0001-7000-8000-000000000001",
      "code": "core.user.read",
      "resourceKey": "core.user",
      "action": "read",
      "nameKey": "permission.core.user.read",
      "isSystem": true,
      "moduleKey": null
    },
    {
      "id": "0192f3d0-0001-7000-8000-000000000002",
      "code": "core.user.write",
      "resourceKey": "core.user",
      "action": "write",
      "nameKey": "permission.core.user.write",
      "isSystem": true,
      "moduleKey": null
    }
  ],
  "error": null,
  "traceId": "c80f017bf6454f2342aec6c1998aab6e"
}
```

| Field | Ghi chú |
| --- | --- |
| `code` | Khuôn `<resourceKey>.<action>`. Đây là chuỗi mà attribute kiểm quyền nhận |
| `nameKey` | **Khoá i18n**, không phải câu tiếng Việt. FE tra ra câu theo ngôn ngữ đang chọn |
| `isSystem` | Quyền do Core khai; không xoá được qua UI |
| `moduleKey` | **Suy từ `permission_resource.module_key`**, tra qua `resource_key` — bảng `core.permission` **không** có cột này ([`../database/schema-core.md`](../database/schema-core.md) §5.1, §5.2). `null` = của Core; khác `null` = do module đó đóng góp |

### Lỗi

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.FORBIDDEN` | Thiếu `core.permission.read` |

### Ghi chú

**Không phân trang.** Danh mục quyền là danh mục kỹ thuật, kích thước cỡ hàng chục tới hàng trăm,
và FE cần **toàn bộ** để dựng ma trận. Phân trang ở đây chỉ tạo ra một đường FE tải thiếu — tức
đúng sự cố thứ hai ở §3.1.

**`nameKey` là khoá, không phải nhãn** — luật R8 cấm câu hiển thị nằm trong BE. Nhãn thiếu bản
dịch thì FE hiện chính khoá đó, và người đọc biết ngay phải bổ sung bản dịch nào.

---

## 5. `GET /api/v1/core/permissions/matrix`

**Status:** DRAFT
**Quyền:** `core.permission.read`

Ma trận đầy đủ: hàng = quyền, cột = vai trò.

### Response 200

```json
{
  "success": true,
  "data": {
    "roles": [
      { "id": "0192f3c2-1111-7000-8000-000000000001", "name": "Quản trị hệ thống", "isSystem": true },
      { "id": "0192f3c2-1111-7000-8000-000000000002", "name": "Nhân viên", "isSystem": false }
    ],
    "rows": [
      {
        "permissionId": "0192f3d0-0001-7000-8000-000000000001",
        "code": "core.user.read",
        "resourceKey": "core.user",
        "resourceNameKey": "resource.core.user",
        "nameKey": "permission.core.user.read",
        "grantedRoleIds": ["0192f3c2-1111-7000-8000-000000000001"]
      },
      {
        "permissionId": "0192f3d0-0001-7000-8000-000000000002",
        "code": "core.user.write",
        "resourceKey": "core.user",
        "resourceNameKey": "resource.core.user",
        "nameKey": "permission.core.user.write",
        "grantedRoleIds": []
      }
    ],
    "version": "sha256:7f3a1c9e"
  },
  "error": null,
  "traceId": "431c80978b79e341f1008b198d1cfb86"
}
```

Ba tính chất FE được phép dựa vào — mỗi cái đều phải có test chốt:

| Tính chất | Vì sao |
| --- | --- |
| `roles` liệt kê **mọi** vai trò trong database | Kể cả vai trò chưa cấp quyền nào. Ma trận rỗng vẫn đủ cột để vẽ lưới |
| `rows` liệt kê **mọi** quyền trong danh mục | Kể cả quyền chưa cấp cho ai (`grantedRoleIds: []`) — xem §2 |
| `version` **luôn** có mặt | Thiếu nó, `PUT` không gọi được. Xem §3 |

`resourceNameKey` là **khoá i18n của tài nguyên**, lấy từ `permission_resource.name_key`
([`../database/schema-core.md`](../database/schema-core.md) §5.1) và tra qua `resource_key`. Nó có
mặt để màn hình gom hàng theo tài nguyên mà **không** phải hiện chuỗi kỹ thuật `core.user` cho người
dùng đọc; `resourceKey` vẫn giữ vai khoá gom nhóm.

### Lỗi

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.FORBIDDEN` | Thiếu `core.permission.read` |

---

## 6. `PUT /api/v1/core/permissions/matrix`

**Status:** DRAFT
**Quyền:** `core.permission.write`

**Ghi đè toàn bộ** ma trận theo đúng nội dung gửi lên. Không phải patch từng phần.

### Request

```json
{
  "version": "sha256:7f3a1c9e",
  "entries": [
    {
      "permissionId": "0192f3d0-0001-7000-8000-000000000001",
      "roleIds": ["0192f3c2-1111-7000-8000-000000000001", "0192f3c2-1111-7000-8000-000000000002"]
    },
    {
      "permissionId": "0192f3d0-0001-7000-8000-000000000002",
      "roleIds": ["0192f3c2-1111-7000-8000-000000000001"]
    }
  ]
}
```

| Field | Bắt buộc | Ghi chú |
| --- | --- | --- |
| `version` | ✔ | Token nhận từ `GET` gần nhất. Thiếu ⇒ 409, vì `null` không bao giờ khớp |
| `entries` | ✔ | **Phải phủ đủ** mọi `permissionId` trong danh mục (§3.2 luật 3) |
| `entries[].roleIds` | ✔ | `[]` là hợp lệ và nghĩa là "không vai trò nào có quyền này" |

### Response 200

```json
{
  "success": true,
  "data": { "version": "sha256:b104d2af" },
  "error": null,
  "traceId": "4bc61c0135830e5f663567b22dcf5c40"
}
```

**Trả về `version` MỚI** để FE lưu thay ngay mà không phải gọi lại `GET`. Không có nó, thao tác
lưu lần thứ hai liên tiếp luôn nhận 409 — và người dùng sẽ học được rằng phải tải lại trang sau
mỗi lần lưu.

### Lỗi

| `code` | `type` | HTTP | Khi nào | Khoá `fieldErrors` |
| --- | --- | ---: | --- | --- |
| `CORE.PERMISSION.ENTRIES_INCOMPLETE` | `Validation` | 400 | `entries` thiếu bất kỳ quyền nào của danh mục (kể cả `[]`) | `Entries` |
| `CORE.PERMISSION.DUPLICATE_ENTRY` | `Validation` | 400 | Cùng một `permissionId` xuất hiện từ hai lần trở lên | `Entries` |
| `CORE.PERMISSION.NOT_FOUND` | `BusinessRule` | 422 | `permissionId` không tồn tại. `messageParams` nêu đích danh id lạ | — |
| `CORE.PERMISSION.ROLE_NOT_FOUND` | `BusinessRule` | 422 | `roleId` không tồn tại. `messageParams` nêu đích danh id lạ | — |
| `CORE.PERMISSION.SYSTEM_ROLE_CANNOT_LOSE_WRITE` | `BusinessRule` | 422 | Payload gỡ `core.permission.write` khỏi một vai trò `is_system` | — |
| `CORE.PERMISSION.VERSION_MISMATCH` | `Conflict` | 409 | `version` thiếu hoặc lệch với trạng thái database | — |

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.VALIDATION.FAILED` | `entries` là `null`. Kèm `fieldErrors["Entries"]` |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.CSRF_REJECTED` | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.ORIGIN_REJECTED` | Header `Origin` ngoài allowlist |
| `CORE.AUTH.FORBIDDEN` | Thiếu `core.permission.write` |

> **Một id trong payload trỏ tới bản ghi không tồn tại là `BusinessRule` 422, KHÔNG kèm
> `fieldErrors`.** `Validation` theo định nghĩa là thứ tính được **từ payload, không cần DB**, mà
> *"quyền này / vai trò này có tồn tại không"* thì bắt buộc phải đọc DB — bảng phân loại ở
> [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §3.2 xếp ca này vào
> `BusinessRule`, và [`users.md`](users.md) §3 dùng đúng cách xếp đó cho `roleId`. Hệ quả cho FE:
> hai mã này không bind được vào một ô nhập; chỗ chỉ ra id nào sai là `messageParams`.

### Ghi chú — mọi nhánh lỗi phải chặn TRƯỚC khi ghi

> **Một mã lỗi trả về mà bảng vẫn bị xoá là vô nghĩa.** Nếu phép kiểm nằm ở handler **sau** khi
> lệnh xoá hàng loạt đã chạy, client nhận lỗi nhưng dữ liệu đã mất — kết quả tệ nhất trong mọi
> kết quả.

Vì vậy toàn bộ phép kiểm ở bảng trên nằm ở **validator**, chạy trước handler, hoặc ở đầu handler
trước bất kỳ thao tác ghi nào. Phép thử nghiệm thu phải kiểm **cả hai vế**: mã lỗi đúng **và**
bảng không đổi một dòng nào — đọc bảng qua
`IgnoreQueryFilters([CoreQueryFilters.SoftDeleteKey])` để thấy được cả dòng đã xoá mềm. Dạng
**không tham số** bị luật **B6** ([`../RULES.md`](../RULES.md)) cấm tuyệt đối: nó bỏ luôn cả bộ
lọc đơn vị.

> **`permissionId` lạ là 422, không phải 500.** Không chặn trước tầng ghi thì nó đi xuống và vỡ
> khoá ngoại — client nhận lỗi hệ thống cho một lỗi hoàn toàn thuộc về mình. Ở dự án tiền nhiệm,
> đúng ca này từng xảy ra ở hai endpoint và phải vá cả hai.

> **`permissionId` trùng lặp cũng là 400.** Handler dựng `Dictionary` từ `entries` sẽ ném
> `ArgumentException` trên khoá trùng ⇒ 500. Và đây **không** phải ca giả định: `PUT` bắt buộc
> gửi lại **toàn bộ** ma trận, nên một lỗi ghép mảng ở FE (nối kết quả hai lần tải, hoặc `push`
> dòng vừa sửa thay vì thay thế nó) sinh ra đúng payload này.
>
> Khoá `fieldErrors` của ca trùng là `Entries` **không có chỉ số** — cố ý: lỗi thuộc về quan hệ
> *giữa* các phần tử, không quy được cho một dòng cụ thể. `messageParams` nêu đích danh mã quyền
> bị lặp để người dùng biết sửa chỗ nào.

### Ghi đè xoá MỀM, không `DELETE`

Thao tác ghi đè xoá mềm dòng cũ rồi thêm dòng mới. Hai hệ quả:

1. Unique index phải là index **một phần** — nếu không, cấp lại một quyền đã từng thu hồi sẽ vỡ
   khoá trùng `23505` ([`../database/schema-core.md`](../database/schema-core.md) §3.3).
2. Hàm băm `version` phải bỏ qua dòng đã xoá mềm — §3.3.

Hai điều này đi cặp: làm đúng một, sai một, thì lỗi vẫn xảy ra, chỉ đổi triệu chứng.

**Hiệu lực với phiên đang chạy:** ma trận mới áp từ request kế tiếp của mọi người mang vai trò bị
đổi — [`users.md`](users.md) §7.

---

## 7. `GET /api/v1/core/permissions/matrix/by-resource`

**Status:** DRAFT
**Quyền:** `core.permission.read`

**Cùng dữ liệu với §5, gom theo tài nguyên.** Dành cho màn hình lưới gọn: hàng = tài nguyên, mỗi
hàng có sẵn các hành động của nó.

### Response 200

```json
{
  "success": true,
  "data": {
    "roles": [
      { "id": "0192f3c2-1111-7000-8000-000000000001", "name": "Quản trị hệ thống", "isSystem": true },
      { "id": "0192f3c2-1111-7000-8000-000000000002", "name": "Nhân viên", "isSystem": false }
    ],
    "resources": [
      {
        "resourceKey": "core.user",
        "nameKey": "resource.core.user",
        "moduleKey": null,
        "permissions": [
          {
            "permissionId": "0192f3d0-0001-7000-8000-000000000001",
            "action": "read",
            "grantedRoleIds": ["0192f3c2-1111-7000-8000-000000000001"]
          },
          {
            "permissionId": "0192f3d0-0001-7000-8000-000000000002",
            "action": "write",
            "grantedRoleIds": []
          }
        ]
      }
    ],
    "version": "sha256:7f3a1c9e"
  },
  "error": null,
  "traceId": "5bf5d123edc71e0fb22159bd83e670b4"
}
```

`resources` liệt kê **mọi** tài nguyên trong danh mục, kể cả tài nguyên chưa có quyền nào được
cấp — cùng lý do §2.

`version` là **cùng một token** với §5, tính trên cùng một tập dữ liệu. Đọc ở đây rồi ghi bằng §6
là hợp lệ.

### Lỗi

Giống §5.

### Ghi chú — vì sao KHÔNG có `PUT .../by-resource`

Cấu trúc này là **một cách nhìn khác của cùng một dữ liệu**, không phải một ma trận thứ hai. Nếu
có hai endpoint ghi vào cùng một bảng, mọi bất biến ở §3 và §6 phải được thi hành **hai lần** ở
hai chỗ — và ngày một lập trình viên thêm một luật vào một chỗ mà quên chỗ kia là ngày cơ chế
chống ghi đè có một lỗ mà không cổng nào bắt được.

Ở dự án tiền nhiệm, hai màn phân quyền có hai cặp `GET`/`PUT` riêng, và một lỗi hoàn toàn giống
nhau (`ToDictionary` trên khoá trùng ⇒ 500) đã phải vá ở **cả hai** — vì phát hiện ở một chỗ
không tự dẫn tới chỗ kia.

**Một đường ghi, nhiều cách đọc.** FE của màn theo tài nguyên gọi `GET .../by-resource` để vẽ,
rồi trải phẳng ra `entries` khi lưu bằng `PUT /api/v1/core/permissions/matrix`.

---

## 8. Rủi ro rollout — đọc trước khi bật kiểm quyền lần đầu

> ⚠️ **Bật kiểm quyền trên một database chưa có dòng cấp quyền nào làm MỌI người bị 403 hàng
> loạt**, kể cả những thao tác họ vẫn làm được trước đó.

Đường an toàn, đúng thứ tự:

| # | Bước | Vì sao |
| --- | --- | --- |
| 1 | Áp script migration — danh mục quyền vào database **cùng lượt**, không có bước nạp riêng | Danh mục có mặt thì màn hình mới vẽ được hàng |
| 2 | **Cấp đủ** quyền cho các vai trò hiện có, theo đúng thứ họ đang làm được | Giữ nguyên hành vi trước khi có kiểm quyền |
| 3 | Bật kiểm quyền trên endpoint | Không ai mất quyền ở bước này |
| 4 | **Thu hẹp** dần qua màn phân quyền | Người vận hành thấy trước hệ quả mỗi lần siết |

**Làm ngược lại — bật kiểm quyền khi chưa cấp gì rồi cấp dần — nghĩa là mở cho người dùng một hệ thống mà không ai làm
được gì**, và mỗi lần cấp thêm một quyền là một lần phải giải thích với một người đang bị chặn.

### 8.1 🪤 Thêm quyền mới cho một tính năng mới

Thêm một `[RequirePermission("core.bao-cao.read")]` mà **không** thêm dòng tương ứng vào migration
seed danh mục nghĩa là attribute hỏi một khoá không có trong database, và deny-by-default trả 403
cho **mọi** người ở đúng tính năng vừa thêm — kể cả người đã được giao việc đó.

Test CI đối chiếu **hai chiều** hằng số khoá ↔ dòng seed trong migration (luật B7) chặn ca này
trước khi triển khai. Khoá, dòng migration và endpoint đổi **cùng lượt** —
[`../database/migration-policy.md`](../database/migration-policy.md) §8 liệt kê danh mục việc.

Khoá của **module** đi cùng đường: dòng seed nằm trong migration của chính module, theo ngoại lệ có
tên của luật E6 ([`../database/migration-policy.md`](../database/migration-policy.md) §4.1), và
test B7 phủ cả khoá của module.

### 8.2 Dòng cấp trỏ tới quyền đã xoá mềm

Bảng cấp quyền trỏ tới danh mục bằng khoá ngoại, nên một quyền xoá **cứng** không để lại dòng cấp
mồ côi. Nhưng nếu ai đó **xoá mềm** một quyền mà không dọn dòng cấp của nó, ma trận sẽ có ô trỏ
tới một quyền không còn hiện trên UI — vô hại về mặt an ninh (không endpoint nào hỏi tới nó)
nhưng là rác.

Câu tìm rác:

```sql
SELECT rp.id, p.code
FROM   core.role_permission rp
JOIN   core.permission      p ON p.id = rp.permission_id
WHERE  rp.is_deleted = false
  AND  p.is_deleted  = true;
```

Mong đợi: 0 dòng. Có dòng ⇒ handler xoá quyền chưa dọn hết dòng cấp; đó là bug cần sửa, không
phải chuyện dọn tay một lần.

---

## 9. Nghiệm thu — năm phép thử

| # | Phép thử | PASS |
| --- | --- | --- |
| 1 | `PUT` với `entries: []` | **400**, và bảng **không đổi một dòng nào** |
| 2 | `PUT` thiếu đúng một `permissionId` | **400** |
| 3 | `GET` rồi `PUT` ngay với `version` vừa nhận | **200**, và response mang `version` mới |
| 4 | Hai `PUT` liên tiếp dùng **cùng một** `version` | Lần đầu 200, lần sau **409**, và lần sau không ghi gì |
| 5 | Lưu hai lần cùng một cặp (vai trò, quyền) | Không vỡ khoá trùng `23505` |

Phép thử 1 phải kiểm **cả hai vế** — mã 400 mà bảng vẫn bị xoá thì vô nghĩa, và đó đúng là thứ
xảy ra nếu ai đó chặn ở handler **sau** khi lệnh xoá đã chạy.

Phép thử 5 là phép thử của index một phần: nó đỏ ngay nếu mệnh đề `WHERE is_deleted = false` bị
bỏ sót ở bất kỳ đâu.
