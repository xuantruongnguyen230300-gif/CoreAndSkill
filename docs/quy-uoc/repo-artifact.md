---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Cái gì được vào repo — artifact, dữ liệu chạy, file sinh, secret

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Repo đang ở giai đoạn 1: chưa có `src/`. Quy ước dưới đây áp cho `src/` khi nó ra đời, và áp **ngay** cho phần `docs/` đã có.

---

## 1. Một câu nguyên tắc

> **Repo giữ NGUỒN. Mọi thứ dựng lại được từ nguồn thì không vào repo.**

Phép thử một dòng: *"Xoá file này đi, chạy một lệnh dựng, nó có quay lại y hệt không?"*

- Có → **không** commit.
- Không, và nó cần cho người khác chạy được dự án → **commit**.
- Không, nhưng nó là dữ liệu do lúc chạy sinh ra → **không** commit, và phải nằm ngoài cây làm việc hoặc bị loại trừ tường minh.

---

## 2. Bảng phân loại

| Nhóm | Ví dụ | Vào repo | Vì sao |
| --- | --- | --- | --- |
| **Nguồn** | `.cs`, `.ts`, `.html`, `.scss`, `.csproj`, `angular.json` | ✔ | Là repo |
| **Cấu hình dựng** | `eslint.config.js`, `tsconfig*.json`, `Directory.Build.props` | ✔ | Quyết định cách build; thiếu thì mỗi máy build ra một kiểu |
| **Lockfile** | `package-lock.json` | ✔ | Xem §3 |
| **Bảng dịch** | `public/i18n/*.json` | ✔ | Nguồn, không phải kết quả |
| **Tài liệu và ảnh minh hoạ** | `docs/**`, ảnh trong [`../Design/`](../Design/) | ✔ | Xem §5 |
| **Artifact build** | thư mục output của .NET và của Angular | ✘ | Dựng lại được |
| **Thư viện tải về** | thư mục gói của trình quản lý gói | ✘ | Dựng lại được từ lockfile |
| **Dữ liệu chạy** | file người dùng tải lên, log, cơ sở dữ liệu tệp | ✘ | Xem §4 |
| **Secret** | chuỗi kết nối, khoá ký, mật khẩu, khoá riêng của chứng chỉ HTTPS dev | ✘ | Xem §6 |
| **Cấu hình riêng máy** | file cấu hình của IDE cho từng người | ✘ | Sở thích cá nhân |
| **Cấu hình hạ tầng** | cấu hình reverse proxy phục vụ app | ✔ | Quyết định hành vi của sản phẩm khi chạy thật; phải được review như code |
| **File sinh tự động** | file `.g.cs`, kiểu sinh từ OpenAPI | tuỳ | Xem §7 |

> 📖 Lý do, bẫy, ví dụ mở rộng: [`repo-artifact.md`](../wiki-core/be/ly-do/repo-artifact.md) §2

---

## 3. Lockfile — commit, không bàn lại

**`package-lock.json` vào repo.** Ba hệ quả phải chấp nhận:

1. Diff của lockfile rất lớn và không đọc được bằng mắt — review bằng câu hỏi *"lần cài này có nâng gói nào ngoài ý định không?"*, đối chiếu với `package.json`.
2. **Không bao giờ sửa tay lockfile.**
3. **Xung đột lockfile không giải bằng cách chọn một bên.** Lấy `package.json` đã hợp nhất rồi chạy lại lệnh cài để sinh lockfile mới.

Phía .NET, vai trò tương đương do phiên bản gói khai tường minh ở `Directory.Packages.props` đảm nhiệm (§3.1) — **không** dùng khoảng phiên bản mở (dạng "bản mới nhất").

> 📖 Lý do, bẫy, ví dụ mở rộng: [`repo-artifact.md`](../wiki-core/be/ly-do/repo-artifact.md) §3

### 3.1 Phiên bản gói .NET khai ở MỘT chỗ cho cả solution

**Luật:** phiên bản khai tập trung ở **`Directory.Packages.props`** (Central Package Management của NuGet) cấp solution; `.csproj` chỉ nêu **tên** gói, không nêu phiên bản. Đây là luật **A14** ở [`../RULES.md`](../RULES.md), ép bằng chính NuGet: một `Version=` còn sót trong `.csproj` là lỗi NU1008, build đỏ.

Kèm theo, khai ở **`Directory.Build.props`** cấp solution: coi cảnh báo biên dịch là lỗi cho **toàn bộ** solution, không chỉ cho một nhóm cảnh báo. Ngoại lệ duy nhất là CS8524, tắt riêng — lý do ở [`be-api-controller.md`](be-api-controller.md) §1.2.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`repo-artifact.md`](../wiki-core/be/ly-do/repo-artifact.md) §3.1

### 3.2 Nâng cấp và lỗ hổng phụ thuộc — luật riêng của một Core dùng lại

Ba việc dưới đây thuộc về Core, không thuộc về từng dự án:

| Việc | Nhịp |
| --- | --- |
| Quét lỗ hổng phụ thuộc, cả .NET lẫn npm | Mỗi lần CI chạy |
| Nâng bản vá trong dòng phiên bản hiện tại | Theo lịch, không theo sự cố |
| Nâng phiên bản nền tảng (.NET, Angular) | Một quyết định có ADR |

**Cổng:** lệnh quét lỗ hổng của cả hai hệ sinh thái chạy trong CI và **làm đỏ build** khi có lỗ hổng ở mức cao.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`repo-artifact.md`](../wiki-core/be/ly-do/repo-artifact.md) §3.2

---

## 4. Dữ liệu chạy — không nằm trong cây làm việc

Thư mục mà ứng dụng ghi vào lúc chạy (file tải lên, log, ảnh sinh ra) **không được** đặt bên trong thư mục source. Khuôn đúng: đường dẫn gốc lưu trữ khai trong cấu hình, mặc định trỏ ra ngoài cây làm việc. Chi tiết phía server ở [`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md).

FE không có "dữ liệu chạy" theo nghĩa này — mọi thứ trong thư mục FE đều là tài sản của source.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`repo-artifact.md`](../wiki-core/be/ly-do/repo-artifact.md) §4

---

## 5. Ảnh trong `docs/Design/`

Ảnh màn hình và ảnh minh hoạ trong [`../Design/`](../Design/) **vào repo** — ngoại lệ có chủ đích với quy tắc "không commit file nhị phân", vì [`../Design/`](../Design/) là nguồn giao diện duy nhất ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §7). Ba ràng buộc đi kèm:

| Ràng buộc |
| --- |
| Ảnh đã nén, định dạng web (PNG cho ảnh chụp giao diện, SVG cho sơ đồ) |
| Không commit file nguồn của công cụ thiết kế |
| Ảnh thay thế thì **ghi đè cùng tên**, không thêm hậu tố phiên bản |

> 📖 Lý do, bẫy, ví dụ mở rộng: [`repo-artifact.md`](../wiki-core/be/ly-do/repo-artifact.md) §5

---

## 6. Secret và cấu hình theo môi trường

### 6.1 Luật

> **Không secret nào trong source, và không secret nào trong bundle FE** (luật S6).

| Nơi | Được chứa | Cấm |
| --- | --- | --- |
| File cấu hình mặc định của server (vào repo) | Cấu trúc khoá, giá trị không nhạy cảm, giá trị mặc định an toàn | Mọi giá trị thật của môi trường chạy |
| `appsettings.Development.json` của server (**vào** repo) | Giá trị **không bí mật** cho máy lập trình viên — ví dụ origin FE trong allowlist CORS, mã đơn vị của lệnh bootstrap | Mọi bí mật: chuỗi kết nối mang mật khẩu, khoá ký, mật khẩu tài khoản |
| File cấu hình của môi trường khác (**không** vào repo) | Chuỗi kết nối, khoá ký | — |
| Kho secret của công cụ phát triển (`user-secrets`, ngoài cây làm việc) | Mọi bí mật của máy lập trình viên | — |
| Biến môi trường lúc triển khai | Như trên, cho môi trường chạy | — |
| `environments/*.ts` của FE | `apiBaseUrl`, cờ `production` | Bất cứ thứ gì lộ ra là phải đổi |

**Cổng của S6:** gitleaks chạy trong CI **ngay từ giai đoạn 1**, trên toàn repo kể cả `docs/`. Allowlist của nó (`.gitleaks.toml` ở gốc repo) chỉ chứa **placeholder rõ ràng** trong tài liệu, không chứa mẫu nới cho mã nguồn.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`repo-artifact.md`](../wiki-core/be/ly-do/repo-artifact.md) §6.1

### 6.2 File cấu hình mặc định vẫn phải vào repo

Khuôn đúng: **file mặc định vào repo với cấu trúc đầy đủ và giá trị rỗng hoặc vô hại; `appsettings.Development.json` vào repo với giá trị không bí mật của máy dev**. File của môi trường khác bị loại trừ theo mẫu tên, và mẫu đó chừa đúng `appsettings.Development.json`. Bí mật đi qua `user-secrets`, không bao giờ nằm trong tệp; gitleaks (S6) quét cả tệp này.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`repo-artifact.md`](../wiki-core/be/ly-do/repo-artifact.md) §6.2

### 6.3 Máy mới clone về cần tạo gì

| Cần tạo | Ở đâu | Lấy giá trị từ đâu |
| --- | --- | --- |
| Bí mật của máy dev — chuỗi kết nối, mật khẩu hai tài khoản của lệnh bootstrap | kho secret của công cụ phát triển (`user-secrets`) | tự đặt; tên khoá của lệnh bootstrap: [`../database/script-runbook.md`](../database/script-runbook.md) §3.3 |
| Cơ sở dữ liệu cục bộ + áp schema | máy cá nhân | [`../database/script-runbook.md`](../database/script-runbook.md) |
| Đơn vị hệ thống, đơn vị nghiệp vụ đầu tiên và hai tài khoản đầu tiên | cơ sở dữ liệu cục bộ | Lệnh bootstrap — **cùng lệnh** với bản cài thật; mật khẩu đọc từ kho secret của công cụ phát triển — [`../database/script-runbook.md`](../database/script-runbook.md) |
| Thư viện của FE | thư mục FE | lệnh cài, đọc từ lockfile |
| Chứng chỉ HTTPS phát triển được máy tin cậy, và bản xuất cho máy chủ dev của FE | kho chứng chỉ của máy cá nhân; tệp xuất nằm ở `src/FE/.cert/` — thư mục bị `.gitignore` loại trừ, không bao giờ commit (lệnh kiểm 5 ở §10) | Lệnh và khai báo trong `angular.json`: [`../wiki-core/fe/trien-khai/01-f0-nen-mong.md`](../wiki-core/fe/trien-khai/01-f0-nen-mong.md) §1 |
| Origin của FE trong allowlist CORS của API — **đủ scheme và cổng** | có sẵn trong `appsettings.Development.json` — chỉ kiểm lại cho khớp | cổng mà máy chủ dev của FE thật sự chạy |

Máy dev cũng chạy HTTPS — [`../adr/0015-fe-va-api-khac-nguon.md`](../adr/0015-fe-va-api-khac-nguon.md).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`repo-artifact.md`](../wiki-core/be/ly-do/repo-artifact.md) §6.3

---

## 7. File sinh tự động

| Loại | Vào repo |
| --- | --- |
| Sinh **mỗi lần build** bởi chính chuỗi build | ✘ |
| Sinh **theo yêu cầu** bằng một lệnh chạy tay (kiểu từ hợp đồng API, mã từ lược đồ) | ✔ |

Với loại thứ hai, hai điều bắt buộc:

**Một — mọi file sinh phải mang dấu ở dòng đầu:**

```text
// <auto-generated>
//     File này do <lệnh sinh> tạo ra. ĐỪNG SỬA TAY.
//     Sửa nguồn rồi chạy lại lệnh: <lệnh chính xác>
// </auto-generated>
```

**Hai — cổng phải kiểm file sinh còn khớp nguồn.** Chạy lại lệnh sinh, rồi kiểm cây làm việc còn sạch không:

```bash
<lệnh sinh>
git diff --exit-code   # PASS khi không có thay đổi
```

> 📖 Lý do, bẫy, ví dụ mở rộng: [`repo-artifact.md`](../wiki-core/be/ly-do/repo-artifact.md) §7

---

## 8. `.gitignore` không cứu được file ĐÃ track — đây là cái bẫy

`.gitignore` chỉ có tác dụng với file **git chưa từng biết tới**.

| Việc vừa làm | Kết quả thật |
| --- | --- |
| Thêm mẫu vào `.gitignore` | File đã track: **không đổi gì cả** |
| Thêm mẫu + gỡ khỏi chỉ mục | File thôi được theo dõi từ commit này trở đi |
| Cả hai bước trên | Nội dung cũ **vẫn còn trong lịch sử** — với secret, coi như đã lộ |

**Với secret đã lỡ commit, xử lý duy nhất đúng là: coi nó đã lộ và đổi giá trị.** Viết lại lịch sử không bao giờ thay thế được việc đổi khoá.

> Mọi lệnh git ghi thuộc về người dùng, không phải agent ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §1). Khi phát hiện file cần gỡ khỏi chỉ mục, agent **báo lệnh cần chạy** và dừng lại.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`repo-artifact.md`](../wiki-core/be/ly-do/repo-artifact.md) §8

---

## 9. Tổ chức `.gitignore`

Một file gốc cho thứ chung; mỗi cây con một file riêng cho thứ đặc thù.

```
.gitignore              # thứ chung: cấu hình IDE, file tạm của hệ điều hành
src/BE/.gitignore       # output build .NET, cấu hình theo môi trường
src/FE/.gitignore       # thư mục gói, output build, cache công cụ
```

Hai quy tắc: **không mẫu nào lặp ở hai file**; mỗi mẫu không hiển nhiên phải có chú thích một dòng nói nó loại trừ cái gì.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`repo-artifact.md`](../wiki-core/be/ly-do/repo-artifact.md) §9

---

## 10. Kiểm bằng lệnh, không bằng bảng liệt kê

Theo [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §6, không chép danh sách file vi phạm vào tài liệu. Kiểm bằng lệnh:

```bash
# 1. Artifact build lọt vào chỉ mục — PASS khi in ra 0
git ls-files | grep -cE '/(bin|obj|dist|node_modules)/' || true

# 2. File cấu hình của môi trường khác Development lọt vào chỉ mục — PASS khi in ra 0
#    appsettings.Development.json được phép: nó vào repo và không chứa bí mật (§6.1)
git ls-files | grep -E 'appsettings\.[A-Za-z]+\.json$' | grep -vc 'appsettings\.Development\.json$' || true

# 3. Đường dẫn lẽ ra bị loại trừ nhưng .gitignore không khai — PASS khi không in gì
git status --porcelain --ignored=no | grep -E '/(bin|obj|dist|node_modules)/' || true

# 4. File lớn bất thường mới vào — PASS khi danh sách chỉ chứa ảnh của docs/Design/
git ls-files -s | awk '{print $4}' | xargs -I{} du -k {} 2>/dev/null | awk '$1 > 500'

# 5. Thư mục chứng chỉ HTTPS dev bị loại trừ, và chưa tệp nào trong đó vào chỉ mục
#    PASS khi dòng đầu in ra src/FE/.cert/probe.pem và dòng sau in ra 0
git check-ignore src/FE/.cert/probe.pem
git ls-files | grep -c '^src/FE/\.cert/' || true
```

> 📖 Lý do, bẫy, ví dụ mở rộng: [`repo-artifact.md`](../wiki-core/be/ly-do/repo-artifact.md) §10

---

## 11. Cảnh báo — tài liệu không được neo vào file bị loại trừ

> **Luật D5:** trích dẫn trong tài liệu không được trỏ vào file bị `.gitignore` loại khỏi repo.

| Dạng vi phạm | Ví dụ | Thay bằng |
| --- | --- | --- |
| Trích dẫn file cấu hình môi trường để minh hoạ khoá cấu hình | trỏ vào file cấu hình riêng của máy | Trích file cấu hình **mặc định** đã vào repo |
| Trỏ vào file bên trong thư mục thư viện tải về | dẫn chứng hành vi của một gói | Nêu tên gói và phiên bản, kèm lệnh để người đọc tự mở |
| Trỏ vào output build để chứng minh kết quả | dẫn chứng bundle | Nêu lệnh dựng và tiêu chí PASS |

Cổng tài liệu kiểm luật này tự động; phần cổng không nhận ra là đường dẫn thuộc về người viết và người review ([`tieu-chi-review.md`](tieu-chi-review.md)).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`repo-artifact.md`](../wiki-core/be/ly-do/repo-artifact.md) §11

---

## 12. Tên tệp phân biệt hoa thường — máy dev Windows, máy chủ Linux

Máy lập trình viên chạy Windows, máy chủ chạy Linux ([`../wiki-core/be/18-trien-khai-va-van-hanh.md`](../wiki-core/be/18-trien-khai-va-van-hanh.md) §7); Windows coi `NguoiDung.ts` và `nguoi-dung.ts` là **một** tệp, Linux coi là **hai**.

| Luật | Chi tiết |
| --- | --- |
| Đường dẫn viết trong code khớp **chính xác** hoa thường với tên tệp trên đĩa | Áp cho cả import mã nguồn lẫn đường dẫn tài nguyên |
| Không có hai tệp chỉ khác nhau ở hoa thường | Chỉ một bản sống sót khi clone trên Windows |
| Tên tệp phía FE viết **chữ thường, nối bằng gạch ngang** | C# giữ `PascalCase` theo quy ước .NET |

Ba lớp chặn, và mỗi lớp có chỗ mù riêng:

| Lớp | Bắt được | Không bắt được |
| --- | --- | --- |
| `forceConsistentCasingInFileNames` trong `tsconfig` | Import TypeScript lệch hoa thường | Tệp dịch, ảnh, mọi tài nguyên trình biên dịch không nhìn tới |
| Build trong CI **trên Linux** | Mọi đường dẫn được phân giải lúc build | Đường dẫn ghép chuỗi lúc chạy |
| Đọc và review | Phần còn lại | — |

> 📖 Lý do, bẫy, ví dụ mở rộng: [`repo-artifact.md`](../wiki-core/be/ly-do/repo-artifact.md) §12

---

## 13. Checklist trước khi đề nghị commit

- [ ] Không có file nào trong nhóm "✘" của bảng §2 nằm trong thay đổi.
- [ ] Nếu có file cấu hình mới: giá trị thật nằm ngoài repo, file mặc định có đủ cấu trúc khoá (§6.2).
- [ ] Nếu có file sinh tự động: mang dấu "đừng sửa tay" kèm lệnh sinh (§7).
- [ ] Nếu `package.json` đổi: lockfile đổi theo, và không sửa tay (§3).
- [ ] Nếu thêm ảnh vào [`../Design/`](../Design/): đã nén, đúng định dạng, ghi đè đúng tên cũ (§5).
- [ ] Nếu thêm mẫu vào `.gitignore`: đã kiểm file tương ứng chưa bị track (§8).
- [ ] Nếu tài liệu có trích dẫn mới: không trỏ vào file bị loại trừ (§11).
- [ ] Chạy `bash .claude/check-docs.sh` nếu có sửa `.md`.

---

## 14. Nhánh và pull request

Mục này định nghĩa *"pull request"* mà [`../adr/0011-ci-github-actions.md`](../adr/0011-ci-github-actions.md) gắn cổng vào.

| Luật | Chi tiết |
| --- | --- |
| **Nhánh chính luôn ở trạng thái triển khai được** | Không đẩy thẳng lên nhánh chính. Mọi thay đổi đi qua một nhánh việc rồi hợp nhất bằng pull request |
| **Một nhánh việc = một việc** | Đặt tên theo việc, không theo người |
| **Cổng chạy trên pull request, và đỏ thì không hợp nhất** | Cổng tài liệu ở giai đoạn 1; thêm cổng BE và FE ở giai đoạn 2 |
| **Không viết lại lịch sử của nhánh đã đẩy lên** | Người khác đã kéo về rồi |
| **Tag phát hành Core đánh trên nhánh chính**, sau khi cổng xanh | §15 |

> 🛑 **Mọi lệnh git ghi ở đây là của người dùng**, không phải của agent ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §1). Agent dừng lại và nói rõ cần chạy lệnh gì.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`repo-artifact.md`](../wiki-core/be/ly-do/repo-artifact.md) §14

---

## 15. Phiên bản Core và cách một dự án lấy Core

Quyết định nền: [`../adr/0016-phan-phoi-core-bang-clone.md`](../adr/0016-phan-phoi-core-bang-clone.md). Mục này chỉ nói **thao tác**.

### 15.1 Tệp `CORE_VERSION` ở gốc repo dự án

Ba dòng, không hơn: tag Core đang dùng, mã commit đầy đủ của tag đó, ngày kéo về. Cập nhật **cùng lúc** với lần kéo — một tệp nói sai còn tệ hơn không có tệp.

### 15.2 Đánh tag ở repo Core

Tag theo dạng `core-vMAJOR.MINOR.PATCH`, tăng theo đúng nghĩa quen thuộc: `PATCH` cho sửa lỗi không đổi hợp đồng, `MINOR` cho thêm khả năng mà bản cũ vẫn chạy, `MAJOR` cho thay đổi phá vỡ. Cùng lúc đánh tag, ghi một mục trong `CHANGELOG.md` ở gốc repo Core; mục của thay đổi phá vỡ **bắt buộc** có phần *"dự án phải làm gì"*.

### 15.3 Kéo một bản Core mới về dự án

Repo dự án giữ thêm một remote trỏ về repo Core. Trình tự: đọc `CHANGELOG.md` trước → kéo về theo **tag**, không theo nhánh → xử lý xung đột (nếu có xung đột trong `Core/` thì luật 1 của ADR-0016 đã bị vi phạm từ trước) → chạy cổng và bộ test → cập nhật `CORE_VERSION`.

> 🛑 Các lệnh git ghi ở bước này là **của người dùng**, không phải của agent ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §1). Agent dừng lại và nói rõ cần chạy lệnh gì.

### 15.4 Dự án cần đổi một hành vi của Core

| Tình huống | Làm gì |
| --- | --- |
| Có seam sẵn | Cấp giá trị qua seam. Không sửa tệp Core |
| Chưa có seam, nhưng nhu cầu chung | Thêm seam **ở repo Core**, đánh tag, rồi kéo về dự án |
| Nhu cầu chỉ của riêng dự án này | Viết trong module của dự án. Không nâng lên Core — ngưỡng ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.2 |

Không sửa thẳng tệp trong `Core/` của bản clone.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`repo-artifact.md`](../wiki-core/be/ly-do/repo-artifact.md) §15.4
