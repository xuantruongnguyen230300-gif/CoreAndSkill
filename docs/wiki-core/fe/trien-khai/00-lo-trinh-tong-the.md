---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Lộ trình thi công Core FE — tổng thể

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Repo đang ở giai đoạn 1, chưa có `src/`. Toàn bộ lộ trình dưới đây là **kế hoạch cho giai đoạn 2**, không phải mô tả tiến độ.
>
> Các file `fe/01-…17-…` trả lời *"một Core FE tốt gồm gì và vì sao"*. Thư mục này trả lời *"làm theo thứ tự nào, và mỗi bước xong thì có gì chạy được"*.

---

## 1. Bốn pha

| Pha | Tên | Xong thì có gì **chạy được** |
| --- | --- | --- |
| **F0** | Nền móng | App build được, cấu trúc bốn tầng, ranh giới ESLint bật, envelope + chuỗi interceptor hoạt động: gọi một endpoint lỗi thật → hiện đúng thông điệp của BE |
| **F1** | Design token | Token từ `Design/` vào code; thư viện UI render đúng màu token; không còn màu literal nào trong SCSS component |
| **F2** | Auth + routing | Đăng nhập bằng cookie chạy thật; guard chặn đúng; tài khoản buộc đổi mật khẩu bị ép đúng chỗ; menu dựng theo permission; hồ sơ cá nhân đọc và lưu được |
| **F3** | Màn quản trị Core | Quản trị người dùng, vai trò và phân quyền chạy đủ vòng đời qua HTTP thật; lỗi validation bind đúng từng ô |

**Cổng chạy song song từ F0**, không phải một pha riêng ở cuối. Xem [`05-gate.md`](05-gate.md).

**Sau F3 — chưa cần một pha riêng, nhưng phải nằm trong kế hoạch:** khu thông báo trong ứng dụng (thành phần A16 ở [`../01-core-components.md`](../01-core-components.md) §2), component tải tệp, và nút xuất dữ liệu trên lưới. Ba thứ này ăn khớp với pha **B4** phía BE ([`../../be/trien-khai/05-b4-tep-nhap-xuat-thong-bao.md`](../../be/trien-khai/05-b4-tep-nhap-xuat-thong-bao.md)) — làm sau khi các endpoint tương ứng chạy thật, không làm trước bằng dữ liệu giả.

**Khu quản trị đơn vị** (`/he-thong/don-vi` theo bản đồ route ở [`../../../quy-uoc/fe-routing-guard.md`](../../../quy-uoc/fe-routing-guard.md) §1, tài khoản vận hành, [`../../../contracts/tenants.md`](../../../contracts/tenants.md)) — **sau F3, đóng khi B3 xong.** Dựng được ngay sau F3 trên dữ liệu giả đúng card, vì nó dùng lại khuôn lưới và form của F3 và chỉ khác ở đường gác — cờ vận hành thay vì ma trận quyền. Đóng khi endpoint của card đó chạy thật ở pha B3 ([`../../be/trien-khai/04-b3-van-hanh.md`](../../be/trien-khai/04-b3-van-hanh.md)).

**Module mẫu** — sau F3, ở dự án hạ nguồn đầu tiên, **không** ở repo Core: §4.1.

Mỗi pha có một file riêng với định nghĩa hoàn thành và danh sách nghiệm thu:
[`01-f0-nen-mong.md`](01-f0-nen-mong.md) · [`02-f1-design-token.md`](02-f1-design-token.md) · [`03-f2-auth-routing.md`](03-f2-auth-routing.md) · [`04-f3-man-quan-tri.md`](04-f3-man-quan-tri.md)

---

## 2. Nguyên tắc chi phối cả bốn pha

| Nguyên tắc | Nghĩa cụ thể |
| --- | --- |
| **Mỗi pha kết thúc bằng thứ chạy được** | Không phải "một thư mục đầy file". Định nghĩa hoàn thành của mỗi pha là một hành vi kiểm được bằng tay |
| **Luật kiến trúc phải có máy kiểm** | Cổng sinh ra cùng lúc với luật, không sinh ra sau — luật chưa có cổng nằm ở [`../../../RULES.md`](../../../RULES.md) §10 |
| **Một luật, một nguồn** | Envelope định nghĩa đúng một chỗ; mọi nơi khác import lại — [`../../../../.claude/CLAUDE.md`](../../../../.claude/CLAUDE.md) §5 |
| **Viết đúng ngay từ đầu, không dọn nợ sau** | Bốn pha này là xây mới. Không có giai đoạn "đồng bộ" hay "dọn nợ" nào, vì không có gì để đồng bộ ngược |

Nguyên tắc cuối là lý do lộ trình này khác lộ trình của một dự án cải tạo. Ở dự án tiền nhiệm, có hẳn một giai đoạn dọn hardcode màu — và món nợ đó tự tái sinh hai lần vì chưa có cổng. Ở đây, cổng bật **trước** khi có màn hình nào để vi phạm nó.

---

## 3. Thứ tự phụ thuộc — và vì sao đúng thứ tự đó

```
F0 ──► F1 ──► F2 ──► F3
 └──────┴──────┴──────┴──► Cổng (bật dần từ F0)
```

### 3.1 F1 trước F2

Màn đăng nhập là màn đầu tiên có giao diện thật. Dựng nó khi chưa có token nghĩa là sẽ chọn màu tại chỗ rồi sửa lại sau — đúng thứ nợ mà cổng F6/F7 sinh ra để chặn.

Ngoại lệ nhỏ có thể chấp nhận: dựng khung màn đăng nhập ở cuối F1 khi token đã sẵn, để F2 chỉ còn phần logic.

### 3.2 F2 trước F3

Các màn quản trị của F3 đều nằm sau guard permission. Không có auth thì **không kiểm chứng được** chúng bị chặn đúng hay không — mà "bị chặn đúng" chính là phần dễ sai nhất của chúng.

### 3.3 Cổng không đợi tới cuối

| Cổng bật ở | Nhóm luật | Vì sao đúng lúc đó |
| --- | --- | --- |
| F0 | Ranh giới tầng, cú pháp, ranh giới DTO, cách đọc envelope, spec cho service | Cấu trúc bốn tầng và `core/http` vừa dựng — luật có đối tượng để ép ngay |
| F1 | Màu literal, component dumb | Token và component dùng chung vừa có, luật mới bắt đầu có nghĩa |
| F2 | Chữ tiếng Việt trong template | i18n vừa có |
| F3 | Ngân sách bundle | Lúc này mới có đủ màn để đo một con số có nghĩa |
| Sau F3 | Ranh giới giữa các module | Module nghiệp vụ đầu tiên ra đời — ở dự án hạ nguồn (§4.1) |

Mã luật cụ thể và pha bật của **từng** mã: cột "Bật ở pha" ở [`05-gate.md`](05-gate.md) §3 — nơi duy nhất giữ ánh xạ đó. Bảng này chỉ giữ lý do.

Nguyên tắc đằng sau bảng này: **tính năng trước, cổng ngay sau** — không viết cổng cho thứ chưa tồn tại, và cũng không để tính năng chạy một thời gian dài rồi mới thêm cổng.

---

## 4. Phạm vi — chỉ Core

| Trong phạm vi | Ngoài phạm vi |
| --- | --- |
| Bốn tầng `core/ shared/ platform/ modules/` | Bất kỳ màn nghiệp vụ nào |
| Màn Core ở `platform/` — danh sách ở [`../../../quy-uoc/fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §2.3 | Nội dung của `modules/` |
| Hạ tầng: envelope, interceptor, guard, i18n, theme, menu | Biểu đồ, upload, xuất dữ liệu ([`../01-core-components.md`](../01-core-components.md) §3) |

**`modules/` chưa có thư mục nào ở cuối F3, và đó là đúng.** Thư mục đó ra đời cùng module nghiệp vụ đầu tiên — ở dự án hạ nguồn, không ở repo Core (§4.1). Đừng tạo sẵn thư mục rỗng: Git không theo dõi thư mục rỗng, nên nó biến mất ở bản clone kế tiếp và tạo ra một mục nghiệm thu không bao giờ tick đúng.

Điều này có một hệ quả trực tiếp lên cổng F2 và F4 — xem [`05-gate.md`](05-gate.md).

### 4.1 Sau F3 — module mẫu ở dự án hạ nguồn đầu tiên

> 📖 Quyết định và phương án đã loại: [`../../../adr/0032-module-mau-o-du-an-ha-nguon.md`](../../../adr/0032-module-mau-o-du-an-ha-nguon.md).

Repo Core **không** chứa module mẫu. Module mẫu là module nghiệp vụ đầu tiên của dự án hạ nguồn đầu tiên dựng trên Core. Skill scaffold module viết ở dự án đó, chạy thật trên chính module đó, rồi mới đưa về Core.

**Định nghĩa hoàn thành:**

- [ ] Module nằm ở `modules/<tên-module>/` của dự án hạ nguồn, cấu trúc theo [`../../../quy-uoc/fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §3
- [ ] Dự án hạ nguồn không sửa file nào của Core ([`../../../adr/0016-phan-phoi-core-bang-clone.md`](../../../adr/0016-phan-phoi-core-bang-clone.md)); phần cần mở rộng ở màn Core đi qua seam ([`../../../quy-uoc/fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §2.5, §2.7)
- [ ] Tên module có trong mảng module của cấu hình ranh giới; cổng F4 xanh; vùng F2 chứng minh bằng canary theo [`../../../quy-uoc/fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §4.4
- [ ] Ít nhất một màn của module chạy qua HTTP thật tới endpoint của module phía BE
- [ ] Toàn bộ cổng FE ([`05-gate.md`](05-gate.md)) xanh ở dự án hạ nguồn
- [ ] Skill scaffold sinh lại được khung module đó; phần mang nghiệp vụ của dự án đã gỡ, rồi skill được đưa về repo Core theo đường của [`../../../adr/0016-phan-phoi-core-bang-clone.md`](../../../adr/0016-phan-phoi-core-bang-clone.md)

---

## 5. Phụ thuộc ra ngoài FE

| Pha | Cần gì từ ngoài | Chặn hay không chặn |
| --- | --- | --- |
| F0 | Hình dạng envelope đã chốt ở [`../../../contracts/README.md`](../../../contracts/README.md) | **Chặn** — sai envelope là sai lan ra toàn app |
| F0 | Endpoint thử của pha B0 — trả cả nhánh thành công lẫn nhánh lỗi ([`../../be/trien-khai/01-b0-nen-mong.md`](../../be/trien-khai/01-b0-nen-mong.md)) | Chặn phần **đóng** pha: định nghĩa hoàn thành của F0 gọi đúng endpoint đó ở nhánh lỗi |
| F1 | Token trong [`../../../Design/DESIGN.md`](../../../Design/DESIGN.md) | **Chặn** |
| F2 | [`../../../contracts/auth.md`](../../../contracts/auth.md), [`../../../contracts/profile.md`](../../../contracts/profile.md), [`../../../contracts/meta-menu.md`](../../../contracts/meta-menu.md) | Chặn phần **đóng** pha, không chặn phần dựng |
| F3 | [`../../../contracts/users.md`](../../../contracts/users.md), [`../../../contracts/roles.md`](../../../contracts/roles.md), [`../../../contracts/permissions.md`](../../../contracts/permissions.md) | Như trên |
| Sau F3 — khu quản trị đơn vị | [`../../../contracts/tenants.md`](../../../contracts/tenants.md) | Như trên — đóng khi B3 xong |

Card nào thuộc pha BE nào: [`../../be/trien-khai/00-lo-trinh-tong-the.md`](../../be/trien-khai/00-lo-trinh-tong-the.md) §1 — file đó giữ ánh xạ, bảng này chỉ nêu card FE cần.

**Ý nghĩa của "chặn phần đóng, không chặn phần dựng":** khi hình dạng request và response đã cố định trong hợp đồng, FE dựng được ngay trên dữ liệu giả đúng hợp đồng. Đổi sang endpoint thật sau đó là đổi một cấu hình. Đợi BE xong mới bắt đầu là mất trắng khoảng thời gian đó.

Nhưng **đóng** một pha thì cần endpoint thật chạy — vì đúng những thứ dữ liệu giả không mô phỏng được (cookie, XSRF, độ trễ, hình dạng lỗi thật) là những thứ hay hỏng nhất.

---

## 6. Cái gì KHÔNG nằm trong lộ trình này

| Không làm ở F0–F3 | Lý do và nơi ghi |
| --- | --- |
| Store chuyên dụng | [`../03-state-management.md`](../03-state-management.md) §2 |
| Thư viện biểu đồ | [`../12-charting.md`](../12-charting.md) §2 |
| Gửi lỗi runtime ra ngoài | [`../10-observability.md`](../10-observability.md) §4 |
| Đo Web Vitals tự động | [`../10-observability.md`](../10-observability.md) §6.2 |
| Bộ test E2E | [`../06-testing-strategy.md`](../06-testing-strategy.md) §6 |
| Cấu hình runtime | [`../17-phuc-vu-va-trien-khai.md`](../17-phuc-vu-va-trien-khai.md) §5 |

Danh sách này là **quyết định**, không phải việc tồn đọng. Mỗi dòng có điều kiện mở lại ghi ở file tương ứng.

---

## 7. Cách dùng bộ tài liệu này khi thi công

1. Mở file của pha đang làm, đọc **định nghĩa hoàn thành** trước khi viết dòng code đầu tiên.
2. Mỗi khi file pha trỏ sang một file `fe/*.md`, mở file đó — chép mẫu từ đó, **đừng viết lại từ trí nhớ**.
3. Chạy cổng liên tục, không đợi tới cuối pha.
4. Đóng pha bằng cách đi hết danh sách nghiệm thu **bằng tay**, không bằng cách đọc lại code.

Điểm 4 quan trọng hơn vẻ ngoài của nó. Phần lớn mục nghiệm thu trong bộ này là những thứ **không** có test tự động nào bắt được: focus đi đúng chỗ, câu chữ đúng, cookie thật sự được gửi. Đọc code không thay được việc bấm thử.

---

## 8. "Xong một pha" nghĩa là gì — và không nghĩa là gì

| Xong nghĩa là | Xong **không** nghĩa là |
| --- | --- |
| Định nghĩa hoàn thành ở đầu file pha đã đạt, kiểm bằng tay | Mọi file dự kiến đã được tạo |
| Toàn bộ danh sách nghiệm thu đã tick, từng mục một | Code trông giống tài liệu |
| Cổng của pha đó bật và đã được chứng minh bằng canary | Cổng chạy xanh (xanh có thể vì mù — [`05-gate.md`](05-gate.md) §4.3) |
| Test của phần mới đã từng đỏ trước khi xanh | Có file test |

**Không "đóng" một pha bằng cách dời mục chưa xong sang pha sau.** Nếu buộc phải dời, mục đó phải được ghi ra tường minh như một khoản nợ, kèm điều kiện xử lý — theo đúng tinh thần [`../../../RULES.md`](../../../RULES.md) §10.

Lý do luật này tồn tại: một mục nghiệm thu bị dời im lặng sẽ không bao giờ được làm. Nó không nằm trong pha nào cả, và không ai chịu trách nhiệm cho nó.

---

## 9. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| Core FE gồm gì | [`../01-core-components.md`](../01-core-components.md) |
| Toàn bộ cổng FE | [`05-gate.md`](05-gate.md) |
| Luật nào ép bằng gì | [`../../../RULES.md`](../../../RULES.md) §7 |
| Thi công một feature | [`../../../quy-uoc/fe-architecture.md`](../../../quy-uoc/fe-architecture.md) |
| Ranh giới Core ↔ Module | [`../../../kien-truc-core-module.md`](../../../kien-truc-core-module.md) §6 |
| Mục lục khu kiến thức nền | [`../../README.md`](../../README.md) |
