---
kind: tham-chieu
scope: core
verified: chua-doi-chieu
---

# Lý do, bẫy, ví dụ mở rộng của `repo-artifact.md`

> File này giữ **lý do, bẫy, ví dụ dài** của [`docs/quy-uoc/repo-artifact.md`](../../../quy-uoc/repo-artifact.md); luật ở đó. Số mục dưới đây trùng số mục của file luật — mục không có gì dời thì ghi "—".

---

## 1. Một câu nguyên tắc

—

## 2. Bảng phân loại

Vì sao cấu hình hạ tầng (reverse proxy) **vào** repo: nó quyết định hành vi của sản phẩm khi chạy thật — nén, cache, chuyển tiếp tiền tố API. Để ngoài repo thì nó không được review như code, và không ai truy được vì sao một giá trị được đặt như vậy.

Cấu hình riêng máy (IDE của từng người) **không** vào repo vì đó là sở thích cá nhân, không phải quyết định của đội.

## 3. Lockfile — commit, không bàn lại

Không có lockfile, hai người cài cùng một `package.json` ở hai thời điểm sẽ nhận hai cây phụ thuộc khác nhau, và lỗi "chạy trên máy tôi thì được" không có cách nào truy ngược.

Lý do của hai trong ba hệ quả:

1. **Diff của lockfile rất lớn và không đọc được bằng mắt.** Đó là bình thường. Điều cần review không phải từng dòng, mà là câu hỏi *"lần cài này có nâng gói nào ngoài ý định không?"* — trả lời bằng cách xem `package.json` có đổi tương ứng hay không.
2. **Không bao giờ sửa tay lockfile** — nó là kết quả của một lệnh cài; sửa tay tạo ra một trạng thái mà không lệnh nào sinh lại được.

Phía .NET không dùng khoảng phiên bản mở (dạng "bản mới nhất") vì nó biến build hôm nay và build tháng sau thành hai thứ khác nhau mà không có gì trong repo ghi lại.

### 3.1 Phiên bản gói .NET khai ở MỘT chỗ cho cả solution

Khai phiên bản trong từng `.csproj` là khuôn hỏng chậm: với một Core nhiều project, cùng một gói xuất hiện ở nhiều file, và một lần nâng sót một file tạo ra hai phiên bản của cùng một gói trong một solution. Kết quả không phải lỗi biên dịch mà là hành vi lệch giữa hai project.

Vì sao coi cảnh báo là lỗi cho **toàn bộ** solution chứ không cho một nhóm cảnh báo: bật một phần là trạng thái tệ nhất — nó tạo cảm giác đã bật.

### 3.2 Nâng cấp và lỗ hổng phụ thuộc — luật riêng của một Core dùng lại

Với một ứng dụng đơn lẻ, một gói dính lỗ hổng là vấn đề của một sản phẩm. Với **Core dùng cho nhiều dự án**, nó là vấn đề của mọi dự án dựng trên Core — và các dự án đó không có cách nào biết, vì chúng phụ thuộc Core chứ không phụ thuộc gói kia trực tiếp.

Vì sao ba việc đó không để dự án tự lo:

| Việc | Vì sao không để dự án tự lo |
| --- | --- |
| Quét lỗ hổng phụ thuộc | Dự án hạ nguồn không thấy cây phụ thuộc của Core |
| Nâng bản vá theo lịch, không theo sự cố | Nâng khi có sự cố nghĩa là nâng dưới áp lực, và đó là lúc người ta bỏ qua bước kiểm |
| Nâng phiên bản nền tảng qua ADR | Nó đổi ràng buộc của **mọi** dự án hạ nguồn cùng lúc |

Vì sao cổng quét lỗ hổng phải làm đỏ build: cảnh báo không chặn build là cảnh báo không ai đọc.

## 4. Dữ liệu chạy — không nằm trong cây làm việc

Lý do thư mục ghi lúc chạy không được nằm trong cây source không phải là `.gitignore` — mà là:

- Đường dẫn ghi phải cấu hình được theo môi trường; một đường dẫn cứng bên trong source sẽ đi theo khi triển khai và ghi vào chỗ không nên ghi.
- Đặt trong cây làm việc thì chỉ cần một lần `git add -A` là dữ liệu thật của người dùng vào repo — và [`repo-artifact.md`](../../../quy-uoc/repo-artifact.md) §8 giải thích vì sao gỡ ra không hề dễ.

**FE không có "dữ liệu chạy" theo nghĩa này.** Trình duyệt không ghi file lên đĩa máy chủ; file người dùng chọn đi thẳng lên API. Mọi thứ trong thư mục FE đều là tài sản của source.

## 5. Ảnh trong `docs/Design/`

Vì sao ảnh là ngoại lệ của quy tắc "không commit file nhị phân": [`Design/`](../../../Design/) là nguồn giao diện duy nhất ([`.claude/CLAUDE.md`](../../../../.claude/CLAUDE.md) §7). Một tài liệu thiết kế trỏ tới ảnh nằm ngoài repo là một tài liệu chết ngay khi liên kết đó hỏng — và liên kết ngoài repo luôn hỏng, chỉ là sớm hay muộn.

Vì sao có ba ràng buộc đi kèm — để ngoại lệ này không thành cửa sau:

| Ràng buộc | Vì sao |
| --- | --- |
| Ảnh đã nén, định dạng web | Ảnh gốc từ công cụ thiết kế nặng gấp nhiều lần và không ai xem trực tiếp |
| Không commit file nguồn của công cụ thiết kế | Chúng là artifact của một công cụ, không đọc được bằng diff, và có bản gốc ở nơi khác |
| Ảnh thay thế thì ghi đè cùng tên | Nếu không, tài liệu trỏ vào ảnh cũ mà không ai biết; lịch sử nằm trong git |

## 6. Secret và cấu hình theo môi trường

### 6.1 Luật

Vì sao allowlist của gitleaks chỉ chứa placeholder rõ ràng: một mục allowlist rộng là một lối cho secret thật đi qua cùng với placeholder.

**Bundle FE là văn bản công khai.** Mọi giá trị trong nó đọc được bằng cách mở công cụ phát triển của trình duyệt. Không có kỹ thuật nào làm nó thành bí mật — làm rối mã nguồn cũng không.

### 6.2 File cấu hình mặc định vẫn phải vào repo

Cám dỗ là loại trừ hẳn mọi file cấu hình cho an toàn. Đừng: người mới clone về sẽ không biết ứng dụng cần những khoá nào, và sẽ phát hiện từng khoá một qua từng lần chạy lỗi.

Vì sao mẫu loại trừ chừa đúng `appsettings.Development.json`: để việc tạo file môi trường khác **không thể** commit nhầm.

**Vì sao `appsettings.Development.json` vào repo.** `ValidateOnStart` bật ở mọi môi trường ([`be-architecture.md`](../../../quy-uoc/be-architecture.md) §4.3): máy vừa clone về phải khởi động được với phần không bí mật đã có sẵn, và chỉ tự đặt phần bí mật. Bí mật đi qua `user-secrets` nên không bao giờ nằm trong tệp; gitleaks (S6) quét cả tệp này.

### 6.3 Máy mới clone về cần tạo gì

Danh sách này phải sống trong tài liệu chứ không trong đầu người đã cài xong — vì chính người đó là người duy nhất không cần nó.

Bí mật của máy dev nằm ở kho secret của công cụ phát triển, tức **không nằm trong cây làm việc** — không thể commit nhầm ngay cả khi mẫu loại trừ bị sửa hỏng.

> **Vì sao máy dev cũng chạy HTTPS:** [`adr/0015-fe-va-api-khac-nguon.md`](../../../adr/0015-fe-va-api-khac-nguon.md) chốt dev chạy **đúng hình dạng của thật** để các bẫy cookie chéo nguồn lộ ra ở máy dev, không phải ở lần triển khai đầu tiên — và vì FE `http` mà API `https` là **khác site**, cookie phiên `SameSite=Lax` không được gửi. Thiếu chứng chỉ, thiếu HTTPS ở máy chủ dev của FE, hoặc thiếu origin trong allowlist CORS thì triệu chứng là *đăng nhập thành công rồi vẫn nhận 401* — không thông báo lỗi nào chỉ ra nguyên nhân.

## 7. File sinh tự động

Vì sao file sinh mỗi lần build không vào repo: lặp lại được; commit là commit một bản sao cũ. Vì sao file sinh theo yêu cầu vào repo: người clone về không chạy lệnh sinh đó, và không có nó thì không build được.

Dòng thứ ba của dấu `<auto-generated>` — *"sửa nguồn rồi chạy lại lệnh"* — là dòng quan trọng nhất và cũng là dòng hay bị bỏ nhất. Một cảnh báo "đừng sửa tay" mà không nói **sửa ở đâu thay thế** sẽ bị bỏ qua, vì người đang cần sửa vẫn phải sửa một chỗ nào đó.

Không có phép kiểm `git diff --exit-code` sau khi sinh lại, một file sinh bị sửa tay sẽ sống sót vô thời hạn và mọi lần sinh lại sau đó đều "vô tình" ghi đè mất thay đổi của ai đó.

## 8. `.gitignore` không cứu được file ĐÃ track — đây là cái bẫy

Một file đã được commit thì thêm mẫu loại trừ **không** làm gì cả: nó vẫn được theo dõi, mọi thay đổi vẫn vào commit sau, và nội dung cũ vẫn nằm trong lịch sử. Đây là chỗ mọi người tưởng đã xử lý xong mà chưa.

Vì sao viết lại lịch sử không thay thế được việc đổi khoá: bản sao có thể đã ở trên máy người khác, trong bản clone, hoặc trong bộ nhớ đệm của dịch vụ lưu trữ.

## 9. Tổ chức `.gitignore`

**Vì sao không gom hết vào một file gốc:** mẫu loại trừ đặt cạnh thứ nó loại trừ thì người sửa cây con đó đọc được ngay; gom vào gốc thì file gốc dài dần và mỗi dòng mất ngữ cảnh. Đổi lại, phải chấp nhận quy tắc không mẫu nào lặp ở hai file — trùng lặp thì sửa một chỗ không chạm chỗ kia.

Vì sao mỗi mẫu không hiển nhiên phải có chú thích: một `.gitignore` toàn mẫu trần là file mà sáu tháng sau không ai dám xoá dòng nào.

## 10. Kiểm bằng lệnh, không bằng bảng liệt kê

Lệnh 3 là lệnh có giá trị nhất và ít được chạy nhất: nó bắt trường hợp một thư mục output **chưa** bị track (nên lệnh 1 xanh) nhưng cũng **chưa** được khai loại trừ — tức đang chờ một lần `git add` diện rộng để lọt vào.

## 11. Cảnh báo — tài liệu không được neo vào file bị loại trừ

Đây là luật dễ vi phạm mà không nhận ra, vì trên máy người viết **file đó tồn tại**. Liên kết mở được, đường dẫn đúng, mọi thứ trông bình thường. Người thứ hai clone về thì không có file đó, và tài liệu trỏ vào hư không.

Cổng tài liệu chỉ bắt được đường dẫn nó nhận ra là đường dẫn — một câu văn xuôi mô tả "xem trong thư mục output" thì không. Trách nhiệm còn lại thuộc về người viết và người review.

## 12. Tên tệp phân biệt hoa thường — máy dev Windows, máy chủ Linux

Hệ quả của việc Windows coi hai tên chỉ khác hoa thường là **một** tệp còn Linux coi là **hai**: một đường dẫn viết lệch hoa thường chạy trơn tru suốt quá trình phát triển, qua mọi lần chạy thử, rồi **hỏng ở đúng lần triển khai đầu tiên** — nơi đắt nhất để phát hiện.

Vì sao không có hai tệp chỉ khác nhau ở hoa thường: trên Windows chúng là một tệp, nên chỉ một bản sống sót khi clone — mất tệp mà không có lỗi nào. Vì sao FE chữ thường mà C# vẫn `PascalCase`: điều bắt buộc là **nhất quán**, không phải cùng một kiểu cho hai ngôn ngữ.

Triệu chứng điển hình của lớp mù thứ nhất (tệp tài nguyên mà trình biên dịch không nhìn tới): giao diện hiện nguyên chuỗi khoá thay vì câu tiếng Việt, vì code hỏi `VI.json` trong khi tệp trên đĩa là `vi.json`. Không lỗi, không cảnh báo — chỉ là mọi câu chữ biến mất.

## 13. Checklist trước khi đề nghị commit

—

## 14. Nhánh và pull request

Vì sao mục này tồn tại: [`adr/0011-ci-github-actions.md`](../../../adr/0011-ci-github-actions.md) khai rằng cổng chạy trên mỗi pull request, và một cổng gắn vào một quy trình không ai mô tả là một cổng không ai chạy.

Vì sao từng luật: không đẩy thẳng lên nhánh chính để nhánh chính luôn triển khai được; đặt tên nhánh theo việc vì nhánh sống càng lâu càng khó hợp nhất; không viết lại lịch sử của nhánh đã đẩy lên vì người khác đã kéo về rồi — viết lại là buộc họ tự dọn.

Repo chưa khai một dịch vụ lưu trữ mã cụ thể, nên mục này cố ý nói bằng khái niệm chung: nó đúng dù dùng dịch vụ nào.

## 15. Phiên bản Core và cách một dự án lấy Core

### 15.1 Tệp `CORE_VERSION` ở gốc repo dự án

—

### 15.2 Đánh tag ở repo Core

—

### 15.3 Kéo một bản Core mới về dự án

—

### 15.4 Dự án cần đổi một hành vi của Core

Sửa thẳng tệp trong `Core/` của bản clone là con đường một chiều: từ lần đó trở đi, mọi lần kéo bản vá đều phải xử lý xung đột tay, và thực tế là sẽ không ai kéo nữa.
