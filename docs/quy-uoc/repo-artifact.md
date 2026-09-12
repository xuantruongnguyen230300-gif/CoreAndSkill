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

Phép thử một dòng, áp được cho mọi file đang phân vân:

*"Xoá file này đi, chạy một lệnh dựng, nó có quay lại y hệt không?"*

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
| **Secret** | chuỗi kết nối, khoá ký, mật khẩu | ✘ | Xem §6 |
| **Cấu hình riêng máy** | file cấu hình của IDE cho từng người | ✘ | Sở thích cá nhân, không phải quyết định của đội |
| **Cấu hình hạ tầng** | cấu hình reverse proxy phục vụ app | ✔ | Nó quyết định hành vi của sản phẩm khi chạy thật — nén, cache, chuyển tiếp tiền tố API. Để ngoài repo thì nó không được review như code, và không ai truy được vì sao một giá trị được đặt như vậy |
| **File sinh tự động** | file `.g.cs`, kiểu sinh từ OpenAPI | tuỳ | Xem §7 |

---

## 3. Lockfile — commit, không bàn lại

**`package-lock.json` vào repo.** Không có nó, hai người cài cùng một `package.json` ở hai thời điểm sẽ nhận hai cây phụ thuộc khác nhau, và lỗi "chạy trên máy tôi thì được" không có cách nào truy ngược.

Ba hệ quả phải chấp nhận:

1. **Diff của lockfile rất lớn và không đọc được bằng mắt.** Đó là bình thường. Điều cần review không phải từng dòng, mà là câu hỏi *"lần cài này có nâng gói nào ngoài ý định không?"* — trả lời bằng cách xem `package.json` có đổi tương ứng hay không.
2. **Không bao giờ sửa tay lockfile.** Nó là kết quả của một lệnh cài; sửa tay tạo ra một trạng thái mà không lệnh nào sinh lại được.
3. **Xung đột lockfile không giải bằng cách chọn một bên.** Giải bằng cách lấy `package.json` đã hợp nhất rồi chạy lại lệnh cài để sinh lockfile mới.

Phía .NET, vai trò tương đương do phiên bản gói khai tường minh trong `.csproj` đảm nhiệm — **không** dùng khoảng phiên bản mở (dạng "bản mới nhất"), vì nó biến build hôm nay và build tháng sau thành hai thứ khác nhau mà không có gì trong repo ghi lại.

### 3.1 Phiên bản gói .NET khai ở MỘT chỗ cho cả solution

Khai phiên bản trong từng `.csproj` là khuôn hỏng chậm: với một Core nhiều project, cùng một gói xuất hiện ở nhiều file, và một lần nâng sót một file tạo ra hai phiên bản của cùng một gói trong một solution. Kết quả không phải lỗi biên dịch mà là hành vi lệch giữa hai project.

**Luật:** phiên bản khai tập trung ở **`Directory.Packages.props`** (Central Package Management của NuGet) cấp solution; `.csproj` chỉ nêu **tên** gói, không nêu phiên bản.

Kèm theo, khai ở **`Directory.Build.props`** cấp solution: coi cảnh báo biên dịch là lỗi cho **toàn bộ** solution, không chỉ cho một nhóm cảnh báo. Bật một phần là trạng thái tệ nhất — nó tạo cảm giác đã bật.

### 3.2 Nâng cấp và lỗ hổng phụ thuộc — luật riêng của một Core dùng lại

Với một ứng dụng đơn lẻ, một gói dính lỗ hổng là vấn đề của một sản phẩm. Với **Core dùng cho nhiều dự án**, nó là vấn đề của mọi dự án dựng trên Core — và các dự án đó không có cách nào biết, vì chúng phụ thuộc Core chứ không phụ thuộc gói kia trực tiếp.

Vì vậy ba việc dưới đây thuộc về Core, không thuộc về từng dự án:

| Việc | Nhịp | Vì sao không để dự án tự lo |
| --- | --- | --- |
| Quét lỗ hổng phụ thuộc, cả .NET lẫn npm | Mỗi lần CI chạy | Dự án hạ nguồn không thấy cây phụ thuộc của Core |
| Nâng bản vá trong dòng phiên bản hiện tại | Theo lịch, không theo sự cố | Nâng khi có sự cố nghĩa là nâng dưới áp lực, và đó là lúc người ta bỏ qua bước kiểm |
| Nâng phiên bản nền tảng (.NET, Angular) | Một quyết định có ADR | Nó đổi ràng buộc của **mọi** dự án hạ nguồn cùng lúc |

**Cổng:** lệnh quét lỗ hổng của cả hai hệ sinh thái chạy trong CI và **làm đỏ build** khi có lỗ hổng ở mức cao. Cảnh báo không chặn build là cảnh báo không ai đọc.

---

## 4. Dữ liệu chạy — không nằm trong cây làm việc

Thư mục mà ứng dụng ghi vào lúc chạy (file tải lên, log, ảnh sinh ra) **không được** đặt bên trong thư mục source. Lý do không phải là `.gitignore` — mà là:

- Đường dẫn ghi phải cấu hình được theo môi trường; một đường dẫn cứng bên trong source sẽ đi theo khi triển khai và ghi vào chỗ không nên ghi.
- Đặt trong cây làm việc thì chỉ cần một lần `git add -A` là dữ liệu thật của người dùng vào repo — và §8 giải thích vì sao gỡ ra không hề dễ.

Khuôn đúng: đường dẫn gốc lưu trữ khai trong cấu hình, mặc định trỏ ra ngoài cây làm việc. Chi tiết phía server ở [`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md).

> **FE không có "dữ liệu chạy" theo nghĩa này.** Trình duyệt không ghi file lên đĩa máy chủ; file người dùng chọn đi thẳng lên API. Mọi thứ trong thư mục FE đều là tài sản của source.

---

## 5. Ảnh trong `docs/Design/`

Ảnh màn hình và ảnh minh hoạ trong [`../Design/`](../Design/) **vào repo**. Đây là ngoại lệ có chủ đích với quy tắc "không commit file nhị phân", và lý do rất cụ thể: [`../Design/`](../Design/) là nguồn giao diện duy nhất ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §7). Một tài liệu thiết kế trỏ tới ảnh nằm ngoài repo là một tài liệu chết ngay khi liên kết đó hỏng — và liên kết ngoài repo luôn hỏng, chỉ là sớm hay muộn.

Ba ràng buộc đi kèm để ngoại lệ này không thành cửa sau:

| Ràng buộc | Vì sao |
| --- | --- |
| Ảnh đã nén, định dạng web (PNG cho ảnh chụp giao diện, SVG cho sơ đồ) | Ảnh gốc từ công cụ thiết kế nặng gấp nhiều lần và không ai xem trực tiếp |
| Không commit file nguồn của công cụ thiết kế | Chúng là artifact của một công cụ, không đọc được bằng diff, và có bản gốc ở nơi khác |
| Ảnh thay thế thì **ghi đè cùng tên**, không thêm hậu tố phiên bản | Nếu không, tài liệu trỏ vào ảnh cũ mà không ai biết; lịch sử nằm trong git |

---

## 6. Secret và cấu hình theo môi trường

### 6.1 Luật

> **Không secret nào trong source, và không secret nào trong bundle FE** (luật S6).

| Nơi | Được chứa | Cấm |
| --- | --- | --- |
| File cấu hình mặc định của server (vào repo) | Cấu trúc khoá, giá trị không nhạy cảm, giá trị mặc định an toàn | Mọi giá trị thật của môi trường chạy |
| File cấu hình theo môi trường (**không** vào repo) | Chuỗi kết nối, khoá ký | — |
| Kho secret của công cụ phát triển (ngoài cây làm việc) | Như trên, cho máy lập trình viên | — |
| Biến môi trường lúc triển khai | Như trên, cho môi trường chạy | — |
| `environments/*.ts` của FE | `apiBaseUrl`, cờ `production` | Bất cứ thứ gì lộ ra là phải đổi |

**Bundle FE là văn bản công khai.** Mọi giá trị trong nó đọc được bằng cách mở công cụ phát triển của trình duyệt. Không có kỹ thuật nào làm nó thành bí mật — làm rối mã nguồn cũng không.

### 6.2 File cấu hình mặc định vẫn phải vào repo

Cám dỗ là loại trừ hẳn mọi file cấu hình cho an toàn. Đừng: người mới clone về sẽ không biết ứng dụng cần những khoá nào, và sẽ phát hiện từng khoá một qua từng lần chạy lỗi.

Khuôn đúng: **file mặc định vào repo với cấu trúc đầy đủ và giá trị rỗng hoặc vô hại**; file theo môi trường bị loại trừ theo mẫu tên. Mẫu loại trừ phải khớp đúng khuôn tên của file môi trường, để việc tạo file đó **không thể** commit nhầm.

### 6.3 Máy mới clone về cần tạo gì

Danh sách này phải sống trong tài liệu chứ không trong đầu người đã cài xong — vì chính người đó là người duy nhất không cần nó.

| Cần tạo | Ở đâu | Lấy giá trị từ đâu |
| --- | --- | --- |
| File cấu hình môi trường phát triển của server | cạnh file cấu hình mặc định | người phụ trách môi trường |
| Cơ sở dữ liệu cục bộ + áp schema | máy cá nhân | [`../database/script-runbook.md`](../database/script-runbook.md) |
| Thư viện của FE | thư mục FE | lệnh cài, đọc từ lockfile |
| Chứng chỉ HTTPS phát triển, được máy tin cậy | kho chứng chỉ của máy cá nhân | `dotnet dev-certs https --trust` |
| Máy chủ dev của FE chạy HTTPS | lệnh chạy dev của FE | `ng serve --ssl` |
| Origin của FE trong allowlist CORS của API — **đủ scheme và cổng** | file cấu hình môi trường phát triển của server | cổng mà máy chủ dev của FE thật sự chạy |

Cách an toàn hơn cho khoản đầu: dùng kho secret của công cụ phát triển, tức giá trị **không nằm trong cây làm việc** nên không thể commit nhầm ngay cả khi mẫu loại trừ bị sửa hỏng.

> **Vì sao máy dev cũng chạy HTTPS:** [`../adr/0015-fe-va-api-khac-nguon.md`](../adr/0015-fe-va-api-khac-nguon.md) chốt dev chạy **đúng hình dạng của thật** để các bẫy cookie chéo nguồn lộ ra ở máy dev, không phải ở lần triển khai đầu tiên — và vì FE `http` mà API `https` là **khác site**, cookie phiên `SameSite=Lax` không được gửi. Thiếu một trong ba dòng cuối bảng thì triệu chứng là *đăng nhập thành công rồi vẫn nhận 401* — không thông báo lỗi nào chỉ ra nguyên nhân.

---

## 7. File sinh tự động

Có hai loại, và chúng có luật ngược nhau:

| Loại | Vào repo | Vì sao |
| --- | --- | --- |
| Sinh **mỗi lần build** bởi chính chuỗi build | ✘ | Lặp lại được; commit là commit một bản sao cũ |
| Sinh **theo yêu cầu** bằng một lệnh chạy tay (kiểu từ hợp đồng API, mã từ lược đồ) | ✔ | Người clone về không chạy lệnh sinh đó, và không có nó thì không build được |

Với loại thứ hai, hai điều bắt buộc:

**Một — mọi file sinh phải mang dấu ở dòng đầu:**

```text
// <auto-generated>
//     File này do <lệnh sinh> tạo ra. ĐỪNG SỬA TAY.
//     Sửa nguồn rồi chạy lại lệnh: <lệnh chính xác>
// </auto-generated>
```

Dòng thứ ba là dòng quan trọng nhất và cũng là dòng hay bị bỏ nhất. Một cảnh báo "đừng sửa tay" mà không nói **sửa ở đâu thay thế** sẽ bị bỏ qua, vì người đang cần sửa vẫn phải sửa một chỗ nào đó.

**Hai — cổng phải kiểm file sinh còn khớp nguồn.** Chạy lại lệnh sinh, rồi kiểm cây làm việc còn sạch không:

```bash
<lệnh sinh>
git diff --exit-code   # PASS khi không có thay đổi
```

Không có phép kiểm này, một file sinh bị sửa tay sẽ sống sót vô thời hạn và mọi lần sinh lại sau đó đều "vô tình" ghi đè mất thay đổi của ai đó.

---

## 8. `.gitignore` không cứu được file ĐÃ track — đây là cái bẫy

`.gitignore` chỉ có tác dụng với file **git chưa từng biết tới**. Một file đã được commit thì thêm mẫu loại trừ **không** làm gì cả: nó vẫn được theo dõi, mọi thay đổi vẫn vào commit sau, và nội dung cũ vẫn nằm trong lịch sử.

Đây là chỗ mọi người tưởng đã xử lý xong mà chưa:

| Việc vừa làm | Kết quả thật |
| --- | --- |
| Thêm mẫu vào `.gitignore` | File đã track: **không đổi gì cả** |
| Thêm mẫu + gỡ khỏi chỉ mục | File thôi được theo dõi từ commit này trở đi |
| Cả hai bước trên | Nội dung cũ **vẫn còn trong lịch sử** — với secret, coi như đã lộ |

**Với secret đã lỡ commit, xử lý duy nhất đúng là: coi nó đã lộ và đổi giá trị.** Viết lại lịch sử là việc phải cân nhắc riêng và không bao giờ thay thế được việc đổi khoá — bản sao có thể đã ở trên máy người khác, trong bản clone, hoặc trong bộ nhớ đệm của dịch vụ lưu trữ.

> Mọi lệnh git ghi thuộc về người dùng, không phải agent ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §1). Khi phát hiện file cần gỡ khỏi chỉ mục, agent **báo lệnh cần chạy** và dừng lại.

---

## 9. Tổ chức `.gitignore`

Một file gốc cho thứ chung; mỗi cây con một file riêng cho thứ đặc thù.

```
.gitignore              # thứ chung: cấu hình IDE, file tạm của hệ điều hành
src/BE/.gitignore       # output build .NET, cấu hình theo môi trường
src/FE/.gitignore       # thư mục gói, output build, cache công cụ
```

**Vì sao không gom hết vào một file gốc:** mẫu loại trừ đặt cạnh thứ nó loại trừ thì người sửa cây con đó đọc được ngay; gom vào gốc thì file gốc dài dần và mỗi dòng mất ngữ cảnh. Đổi lại, phải chấp nhận một quy tắc: **không mẫu nào lặp ở hai file** — trùng lặp thì sửa một chỗ không chạm chỗ kia.

Mỗi mẫu không hiển nhiên phải có chú thích một dòng nói nó loại trừ cái gì. Một `.gitignore` toàn mẫu trần là file mà sáu tháng sau không ai dám xoá dòng nào.

---

## 10. Kiểm bằng lệnh, không bằng bảng liệt kê

Theo [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §6, không chép danh sách file vi phạm vào tài liệu — bảng liệt kê tay sẽ mục ruỗng. Kiểm bằng lệnh:

```bash
# 1. Artifact build lọt vào chỉ mục — PASS khi in ra 0
git ls-files | grep -cE '/(bin|obj|dist|node_modules)/' || true

# 2. File cấu hình theo môi trường lọt vào chỉ mục — PASS khi in ra 0
git ls-files | grep -cE 'appsettings\.[A-Za-z]+\.json$' || true

# 3. Đường dẫn lẽ ra bị loại trừ nhưng .gitignore không khai — PASS khi không in gì
git status --porcelain --ignored=no | grep -E '/(bin|obj|dist|node_modules)/' || true

# 4. File lớn bất thường mới vào — PASS khi danh sách chỉ chứa ảnh của docs/Design/
git ls-files -s | awk '{print $4}' | xargs -I{} du -k {} 2>/dev/null | awk '$1 > 500'
```

Lệnh 3 là lệnh có giá trị nhất và ít được chạy nhất: nó bắt trường hợp một thư mục output **chưa** bị track (nên lệnh 1 xanh) nhưng cũng **chưa** được khai loại trừ — tức đang chờ một lần `git add` diện rộng để lọt vào.

---

## 11. Cảnh báo — tài liệu không được neo vào file bị loại trừ

> **Luật D5:** trích dẫn trong tài liệu không được trỏ vào file bị `.gitignore` loại khỏi repo.

Đây là luật dễ vi phạm mà không nhận ra, vì trên máy người viết **file đó tồn tại**. Liên kết mở được, đường dẫn đúng, mọi thứ trông bình thường. Người thứ hai clone về thì không có file đó, và tài liệu trỏ vào hư không.

Ba dạng vi phạm hay gặp:

| Dạng | Ví dụ | Thay bằng |
| --- | --- | --- |
| Trích dẫn file cấu hình môi trường để minh hoạ khoá cấu hình | trỏ vào file cấu hình riêng của máy | Trích file cấu hình **mặc định** đã vào repo |
| Trỏ vào file bên trong thư mục thư viện tải về | dẫn chứng hành vi của một gói | Nêu tên gói và phiên bản, kèm lệnh để người đọc tự mở |
| Trỏ vào output build để chứng minh kết quả | dẫn chứng bundle | Nêu lệnh dựng và tiêu chí PASS |

Cổng tài liệu kiểm luật này tự động. Nhưng cổng chỉ bắt được đường dẫn nó nhận ra là đường dẫn — một câu văn xuôi mô tả "xem trong thư mục output" thì không. Trách nhiệm còn lại thuộc về người viết và người review ([`tieu-chi-review.md`](tieu-chi-review.md)).

---

## 12. Tên tệp phân biệt hoa thường — máy dev Windows, máy chủ Linux

Windows coi `NguoiDung.ts` và `nguoi-dung.ts` là **một** tệp; Linux coi là **hai**. Máy lập trình
viên chạy Windows, máy chủ chạy Linux
([`../wiki-core/be/18-trien-khai-va-van-hanh.md`](../wiki-core/be/18-trien-khai-va-van-hanh.md) §7).

Hệ quả: một đường dẫn viết lệch hoa thường chạy trơn tru suốt quá trình phát triển, qua mọi lần
chạy thử, rồi **hỏng ở đúng lần triển khai đầu tiên** — nơi đắt nhất để phát hiện.

| Luật | Chi tiết |
| --- | --- |
| Đường dẫn viết trong code khớp **chính xác** hoa thường với tên tệp trên đĩa | Áp cho cả import mã nguồn lẫn đường dẫn tài nguyên |
| Không có hai tệp chỉ khác nhau ở hoa thường | Trên Windows chúng là một tệp, nên chỉ một bản sống sót khi clone — mất tệp mà không có lỗi nào |
| Tên tệp phía FE viết **chữ thường, nối bằng gạch ngang** | C# giữ `PascalCase` theo quy ước .NET; điều bắt buộc là **nhất quán**, không phải cùng một kiểu cho hai ngôn ngữ |

Ba lớp chặn, và mỗi lớp có chỗ mù riêng:

| Lớp | Bắt được | Không bắt được |
| --- | --- | --- |
| `forceConsistentCasingInFileNames` trong `tsconfig` | Import TypeScript lệch hoa thường | Tệp dịch, ảnh, mọi tài nguyên trình biên dịch không nhìn tới |
| Build trong CI **trên Linux** | Mọi đường dẫn được phân giải lúc build | Đường dẫn ghép chuỗi lúc chạy |
| Đọc và review | Phần còn lại | — |

Triệu chứng điển hình của lớp mù thứ nhất: giao diện hiện nguyên chuỗi khoá thay vì câu tiếng
Việt, vì code hỏi `VI.json` trong khi tệp trên đĩa là `vi.json`. Không lỗi, không cảnh báo — chỉ
là mọi câu chữ biến mất.

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

[`../adr/0011-ci-github-actions.md`](../adr/0011-ci-github-actions.md) khai rằng cổng chạy trên mỗi pull request. Mục này định nghĩa cái *"pull request"* đó, vì một cổng gắn vào một quy trình không ai mô tả là một cổng không ai chạy.

| Luật | Chi tiết |
| --- | --- |
| **Nhánh chính luôn ở trạng thái triển khai được** | Không đẩy thẳng lên nhánh chính. Mọi thay đổi đi qua một nhánh việc rồi hợp nhất bằng pull request |
| **Một nhánh việc = một việc** | Đặt tên theo việc, không theo người. Nhánh sống càng lâu càng khó hợp nhất |
| **Cổng chạy trên pull request, và đỏ thì không hợp nhất** | Cổng tài liệu ở giai đoạn 1; thêm cổng BE và FE ở giai đoạn 2 |
| **Không viết lại lịch sử của nhánh đã đẩy lên** | Người khác đã kéo về rồi; viết lại là buộc họ tự dọn |
| **Tag phát hành Core đánh trên nhánh chính**, sau khi cổng xanh | §15 |

> 🛑 **Mọi lệnh git ghi ở đây là của người dùng**, không phải của agent ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §1). Agent dừng lại và nói rõ cần chạy lệnh gì.

Repo chưa khai một dịch vụ lưu trữ mã cụ thể, nên mục này cố ý nói bằng khái niệm chung: nó đúng dù dùng dịch vụ nào.

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

Sửa thẳng tệp trong `Core/` của bản clone là con đường một chiều: từ lần đó trở đi, mọi lần kéo bản vá đều phải xử lý xung đột tay, và thực tế là sẽ không ai kéo nữa.
