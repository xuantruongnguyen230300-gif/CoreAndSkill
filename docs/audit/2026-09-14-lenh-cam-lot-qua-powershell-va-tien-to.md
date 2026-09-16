---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 2026-09-14 — Lệnh cấm lọt qua công cụ PowerShell và qua tiền tố bọc

> Phát hiện bằng một phép thử có chủ đích lên lớp chặn lệnh ở [`../../.claude/settings.json`](../../.claude/settings.json). [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §1 mô tả lớp này là *"được cưỡng chế bằng máy, không phải bằng câu văn"*.
>
> Viết theo khuôn bốn phần ở [`README.md`](README.md).

---

## 1. Hiện tượng

Ngày 2026-09-14, phiên điều phối thử lớp chặn bằng một lệnh nằm trong danh sách cấm nhưng **không gây hại được**: đẩy một gói không tồn tại lên một thư mục feed giả.

```text
dotnet nuget push __khong_ton_tai__.nupkg --source <thư mục feed giả>
```

Cùng một lệnh, ba đường chạy:

| Đường | Kết quả | Thứ đã dừng lệnh |
| --- | --- | --- |
| Công cụ Bash, lệnh nguyên văn | Bị chặn trước khi chạy | `permissions.deny` — đúng thiết kế |
| Công cụ PowerShell, lệnh nguyên văn | **Chạy** | Tệp gói không tồn tại |
| Công cụ Bash, lệnh mang tiền tố `rtk` | **Không bị chặn** | Chương trình `rtk` không có trên PATH |

Hai dòng dưới không để lại hậu quả, nhưng thứ dừng lệnh **không phải lớp chặn**: một bên là tệp vắng mặt, một bên là chương trình chưa cài. Cho tên một gói có thật, hoặc cài `rtk`, thì cả hai lệnh đi hết đường.

Đường thứ ba không phải đường hiếm. Một chỉ dẫn ở cấp máy người dùng, nằm ngoài repo, yêu cầu mọi lệnh shell mang tiền tố `rtk`. Trên một máy đã cài `rtk`, agent làm đúng chỉ dẫn đó sẽ đi vòng lớp chặn **ở mọi lệnh cấm**, mà không hề cố ý.

**Thời gian mù.** Cấu hình chặn vào repo ở commit ngày 2026-09-12 (`git log -- .claude/settings.json`). Không cơ chế nào ghi lại một lệnh cấm đã chạy qua đường nào, nên không biết được đường vòng có từng được dùng trong khoảng đó hay không.

---

## 2. Nguyên nhân gốc

**Lớp chặn so CHUỖI lệnh theo TỪNG công cụ, còn thứ cần chặn là HIỆU ỨNG của lệnh.** Hai thứ này chỉ trùng nhau khi lệnh được gõ nguyên văn và đi qua đúng công cụ đã khai.

Một mục `Bash(dotnet nuget push:*)` nói hai điều hẹp hơn vẻ ngoài của nó:

- **Chỉ công cụ Bash.** Harness có thêm một công cụ shell thứ hai, và mục chặn khai cho công cụ này không áp cho công cụ kia. Mỗi công cụ chạy lệnh là một không gian tên chặn riêng, rỗng cho tới khi có người khai.
- **Chỉ chuỗi bắt đầu đúng bằng tiền tố đó.** Bất cứ thứ gì đứng trước làm chuỗi không còn khớp, dù lệnh chạy ra vẫn y hệt: một chương trình bọc, `env`, một phép gán biến, `timeout`, hay một tuỳ chọn toàn cục như `git -C <path>`.

Phép thử chỉ chạy đúng hai đường vòng: công cụ thứ hai và tiền tố `rtk`. Các dạng bọc còn lại có cùng hình dạng nhưng **chưa được thử**.

**Vì sao không lộ sớm hơn.** Repo **có** cổng cho lớp này, và cổng xanh. `check-docs.sh` §1 (luật D12 ở [`../RULES.md`](../RULES.md)) đối chiếu bảng lệnh cấm trong `CLAUDE.md` §1 với các mục dạng `Bash(...)` trong `permissions.deny`. Nó chứng minh **hai văn bản khớp nhau**, không chứng minh **lệnh bị chặn**. Luật C4 mang dấu đang được ép, dựa trên đúng bằng chứng đó: cấu hình có tồn tại. Quy trình không có bước nào yêu cầu cố chạy một lệnh cấm qua từng đường.

---

## 3. Cách vá

Bản vá được chốt trong đợt quyết định ngày 2026-09-14. Cột cuối ghi trạng thái **lúc viết bản ghi này**, không phải trạng thái đích:

| Phần | Làm gì | Vá gốc hay triệu chứng | Lúc ghi (2026-09-14) |
| --- | --- | --- | --- |
| Deny ba dạng | Mỗi mục `Bash(x:*)` có thêm `Bash(rtk x:*)` và `PowerShell(x:*)` | **Triệu chứng.** Bịt đúng hai đường đã thử; mọi tiền tố bọc khác vẫn lọt | Đang thi công ở pha 1; chưa thấy trong `settings.json` |
| Cổng §1 kiểm ba dạng | `check-docs.sh` §1 báo đỏ khi một mục `Bash(x:*)` thiếu một trong hai bản kia | Chặn hồi quy cho phần trên, nhưng vẫn chỉ kiểm **văn bản** cấu hình | Đang thi công ở pha 1; chưa chạy trên repo thật |
| Hook `PreToolUse` | Script mới cho công cụ Bash và PowerShell: đọc tiền tố cấm từ chính các mục `Bash(...)` của `permissions.deny`, tách lệnh ghép, bóc tiền tố bọc, chuẩn hoá tuỳ chọn toàn cục của git; khớp thì chặn | Gần gốc hơn, nhưng vẫn là **danh sách chặn theo chuỗi** — xem giới hạn 2 | Viết ở pha 1; **không gắn** vào `settings.json` ở pha 1. Gắn và thử bằng lệnh thật ở pha 2 |
| Bộ test của hook | Ca phải chặn và ca phải cho qua, chạy trên payload mẫu | Chứng minh hook phân tích đúng; không chứng minh harness gọi tới hook | Đặt ngoài repo theo quyết định, nên không cổng nào chạy lại được |
| Câu mô tả ở `CLAUDE.md` §1 | Nêu lớp chặn là deny ba dạng cộng hook | — | Đang thi công ở pha 1 |

**Đã thi hành ngày 2026-09-14** — bốn dòng đầu của bảng trên không còn ở trạng thái *đang thi công*: `permissions.deny` mang đủ ba dạng cho mỗi mục cấm lệnh; `check-docs.sh` §1 kiểm ba dạng và đã chạy xanh trên repo thật; hook `PreToolUse` **đã gắn** vào khối `hooks` của [`../../.claude/settings.json`](../../.claude/settings.json) cho công cụ Bash và PowerShell — không hoãn sang pha 2; bộ payload mẫu nằm trong repo ở `.claude/hooks/tests/` và chạy lại trong CI qua job *bộ test hook* của [`../../.github/workflows/docs-gate.yml`](../../.github/workflows/docs-gate.yml). Matcher của `PostToolUse` nay khai cả công cụ PowerShell, nên chỗ hở nêu ở cuối mục này đã đóng. **Chưa thi hành:** phép thử hành vi qua từng công cụ trong một phiên thật — điều kiện 2 của [`../adr/0030-dieu-kien-chuyen-giai-doan-2.md`](../adr/0030-dieu-kien-chuyen-giai-doan-2.md), và là phần còn lại của nợ C4 ở [`../RULES.md`](../RULES.md) §10.

Hai giới hạn phải nói thẳng, vì bản vá dễ bị đọc như thể đã đóng chuyện:

1. **Hook thả khi không phân tích được.** Gặp lỗi phân tích thì hook không chặn, chỉ ghi log. Chọn vậy để một lỗi của hook không khoá mọi lệnh shell. Cái giá: một chuỗi lệnh đủ lạ để làm hỏng bộ phân tích sẽ đi qua.
2. **Danh sách chặn theo chuỗi không bao giờ kín.** Bóc được các tiền tố bọc đã biết vẫn không bóc được lệnh lồng trong một shell con (`bash -c`, `sh -c`, `powershell -Command`, `cmd /c`), lệnh ghi ra tệp script rồi chạy tệp đó, hay `eval`. Các dạng này không nằm trong danh sách bóc của bản vá, chưa được thử, và không câu nào ở đây khẳng định chúng bị chặn.

Cùng cơ chế còn một chỗ nữa, đọc được ngay trong cấu hình nhưng **chưa thử**. Matcher của hook `PostToolUse` khai công cụ sửa file và Bash, không khai PowerShell. Nhiều khả năng một lượt sửa tài liệu bằng công cụ PowerShell sẽ không ghi dấu, và cổng không chạy ở cuối lượt. Đó là đúng lỗi mà [`2026-09-09-cong-no-op-va-hai-mo-hinh-nguon.md`](2026-09-09-cong-no-op-va-hai-mo-hinh-nguon.md) đã vá cho Bash, nay quay lại khi harness có thêm công cụ.

---

## 4. Cổng nào lẽ ra phải bắt

**Có cổng, cổng chạy, nhưng không phủ ca này.** `check-docs.sh` §1 đối chiếu `CLAUDE.md` §1 với `permissions.deny` ở dạng `Bash`, và chỉ ở dạng đó. Nó không biết có công cụ shell thứ hai, không biết có tiền tố bọc. Về bản chất, nó cũng không thử hành vi chặn.

Mở rộng §1 sang ba dạng là chặn hồi quy cho phần deny, và vẫn chỉ là kiểm văn bản. Phần kiểm **hành vi** chưa có cổng nào chạy lại được, nên nó vào danh sách nợ:

> **C4 (phần còn lại)**: lệnh cấm phải bị chặn trên **mọi** đường thực thi, không chỉ trên chuỗi nguyên văn đã khai. Ép bằng gì: chưa có cổng. Xem [`../RULES.md`](../RULES.md) §10.

Áp phép thử ở [`README.md`](README.md) §8:

- **Quay ngược thời gian thì luật đó có bắt được sự cố này không?** Có, nếu bộ payload của hook chứa ca `rtk <lệnh cấm>` và phép thử hành vi được chạy qua từng công cụ.
- **Nó có bắt được một sự cố khác cùng lớp không?** Chỉ một phần. Danh sách tiền tố cấm đọc từ nguồn nên không lệch. Nhưng danh sách **công cụ chạy lệnh** và danh sách **tiền tố bọc** vẫn liệt kê tay. Một công cụ shell thứ ba mà harness thêm vào sẽ lại bắt đầu với một không gian tên chặn rỗng, và repo không có nguồn máy đọc được nào liệt kê công cụ của harness để đối chiếu. Phần đó dựa vào người đọc ghi chú phát hành của harness.

---

## 5. Bài học tổng quát

**Một lớp canh neo vào ĐƯỜNG ĐI của thao tác sẽ hở ở mọi đường tương đương mà nó không liệt kê.** Neo được vào hiệu ứng thì neo vào hiệu ứng. Không neo được, như ở đây, thì chỉ còn một cách chứng minh lớp canh có tác dụng: **cố vượt nó**, qua từng đường.

Một cổng so văn bản cấu hình với văn bản tài liệu chỉ chứng minh hai văn bản nhất quán. Nó xanh cả khi cấu hình không chặn được gì. Câu *"được cưỡng chế bằng máy"* ở `CLAUDE.md` §1 đã dựa trên sự tồn tại của cấu hình, không dựa trên một lần thử. Đó là một biến thể nhẹ của khuôn mà [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §4 cấm: tuyên bố một cơ chế đang chạy mà không ai kiểm hành vi của nó.

Cùng họ với hai bản ghi trước. [`2026-09-09-cong-no-op-va-hai-mo-hinh-nguon.md`](2026-09-09-cong-no-op-va-hai-mo-hinh-nguon.md): hook ghi dấu neo vào công cụ sửa file, nên bỏ sót mọi lượt sửa bằng lệnh shell. [`2026-09-12-cong-neo-vao-muc-thay-vi-vai-tro-cua-chuoi.md`](2026-09-12-cong-neo-vao-muc-thay-vi-vai-tro-cua-chuoi.md): phép dò neo vào mục chứa chuỗi thay vì vai của chuỗi. Cả ba đều **neo vào vật chứa thay vì vào thứ luật thật sự nói tới**. Lần này vật chứa là tên công cụ và chuỗi lệnh.
