---
name: "review-doc"
description: "Review một thay đổi tài liệu đang nằm trong working tree: đối chiếu diff với luật repo, kiểm nhãn trạng thái và neo, kiểm việc chép nội dung sang .claude/, kiểm số đếm được chép cứng, rồi chạy cổng. Chỉ báo cáo, không sửa. Dùng khi vừa sửa docs/, .claude/ hay spec/ và cần một lượt đọc hiểu trên đúng phần đổi — soát định kỳ toàn kho thì dùng /doc-audit."
argument-hint: "[phạm vi] - vd 'docs/quy-uoc' hoặc để trống để review toàn bộ thay đổi chưa commit"
---

## User Input

```text
$ARGUMENTS
```

Bạn **BẮT BUỘC** phải xem xét user input trước khi tiếp tục (nếu không rỗng).

## Mục tiêu

Cổng `check-docs.sh` là lưới **chặn hồi quy**, không phải chứng nhận chất lượng. Nó không đọc hiểu nội dung. Skill này là lượt đọc hiểu chạy trên đúng phần **vừa thay đổi** — nhỏ hơn, nên đọc kỹ được.

Khác với skill `doc-audit` (soát toàn kho, định kỳ), skill này chỉ soát **diff**: cái gì vừa được thêm hoặc sửa.

🛑 **Chỉ đọc.** Không sửa file nào, kể cả finding nhìn có vẻ hiển nhiên. Người review không sửa thứ mình review — thấy lỗi thì báo, không vá.

## Các bước thực hiện

### 1. Lấy diff — chỉ đọc, không đụng git ghi

Ba lệnh đọc được phép chạy tự do theo [`../../CLAUDE.md`](../../CLAUDE.md) §1:

```bash
git status
git diff -- docs .claude spec
git diff --stat -- docs .claude spec
```

`$ARGUMENTS` có nêu phạm vi → giới hạn đường dẫn theo đó. Rỗng → lấy toàn bộ thay đổi chưa commit.

Diff rỗng → hỏi người dùng: muốn review commit gần nhất thay vì working tree không? Nếu có, dùng lệnh đọc:

```bash
git show --stat
git show -- docs .claude spec
```

🛑 Mọi lệnh git **ghi** đều cấm. Cần một lệnh ghi để đi tiếp thì **nói rõ cần chạy lệnh gì**, người dùng tự chạy.

### 2. Đọc tiêu chí chấm

Mở [`../../../docs/quy-uoc/tieu-chi-review.md`](../../../docs/quy-uoc/tieu-chi-review.md) — file này quyết định **cái gì là finding, cái gì không**. Chấm theo nó, không chấm theo cảm giác.

Đối chiếu luật với bảng luật — các tệp `docs/RULES-*.md`, mục lục [`../../../docs/RULES.md`](../../../docs/RULES.md): mỗi finding phải nêu được **luật số mấy** bị vi phạm. Không nêu được thì đó là góp ý, không phải finding — và phải ghi vào mục riêng, đừng trộn lẫn.

### 3. Kiểm nhãn trạng thái và neo

Ba nhãn hợp lệ theo [`../../CLAUDE.md`](../../CLAUDE.md) §4; neo theo §9 của cùng tệp. Câu hiện trạng mới thêm trong diff là thứ đối chiếu được: mở đúng tệp `src/` được neo, `grep -F` chuỗi neo, rồi đọc xem code có làm đúng điều câu đó khai không. Năm dạng vi phạm cần soi:

| Dạng | Vì sao là lỗi |
| --- | --- |
| Tuyên bố hoàn thành cho thứ chưa có code | Nhãn "đã xong" được thiết kế để **không ai kiểm lại** |
| Nhãn có ngày nhưng nội dung sai | Cổng kiểm ngày, không kiểm nội dung — đây là lỗ hổng của cổng |
| Nhãn chép vào mục lục thay vì đặt ở đầu chính file | Trạng thái ở chỗ thứ hai không bao giờ được sửa cùng lúc |
| Đóng một việc tồn đọng bằng cách sửa mô tả cho khớp mong muốn | Dạng sai đắt nhất: không lỗi biên dịch, không test nào bắt |
| Neo vào `src/` sai dạng §9 | Trái §9 — lý do ở ADR mà §9 trỏ tới |

Kiểm thêm khoá `verified` của file vừa sửa: đóng dấu ngày cho một file chưa ai mở source ra so là đúng khuôn sai mà §4 cấm. `chua-doi-chieu` là giá trị trung thực cho tới khi có người đối chiếu **đúng file đó** — sự tồn tại của `src/` không lật nó.

### 4. Kiểm chép nội dung sang `.claude/`

Diff có chạm `.claude/` → đây là phần cần soi kỹ nhất, vì cổng chỉ bắt được một phần của nó.

**Phép thử một câu** ([`../../CLAUDE.md`](../../CLAUDE.md) §2): câu vừa thêm vào `.claude/` có thể trở thành **sai khi code thay đổi** không? Có → nó thuộc `docs/`.

| Câu vừa thêm | Kết luận |
| --- | --- |
| "Xong việc chạm Core thì gọi `core-reviewer`" | Quy trình — thuộc `.claude/` |
| "Sửa envelope thì đọc file quy ước controller trước" | Trỏ đường — thuộc `.claude/` |
| Một quy tắc về kiểu trả về của handler | Tri thức — **finding**, phải nằm ở `docs/` |
| Một con số về số lượng project | Tri thức — **finding** |

Ba dạng vi phạm cụ thể:

- **Code block ngôn ngữ lập trình trong `.claude/`.** Cổng bắt được dạng này, nhưng vẫn liệt kê trong báo cáo nếu có.
- **Văn xuôi chép lại nội dung `docs/`.** Cổng **không** bắt được. Soi mọi đoạn mới dài hơn một dòng trong `.claude/`: nó đang mô tả *quy trình* hay đang giải thích *nội dung* của một file `docs/`?
- **Dòng trỏ đường kèm tóm tắt.** Dạng chuẩn là **một dòng, không tóm tắt kèm** ([`../../CLAUDE.md`](../../CLAUDE.md) §3). Một dòng trỏ đường kèm hai câu giải thích chính là bản sao thứ hai đang được sinh ra.

Cũng kiểm chiều cập nhật: các gạch đầu dòng ở §3 của file luật repo nói khi nào `.claude/` được đổi, và không trường hợp nào cho chép nội dung. Nếu diff sửa `docs/` **và** thêm nội dung vào `.claude/` cùng lúc, hỏi người dùng vì sao.

### 5. Kiểm số đếm được bằng lệnh

Luật ở [`../../CLAUDE.md`](../../CLAUDE.md) §6: không chép vào tài liệu thứ đếm được bằng lệnh. Bảng liệt kê tay sẽ luôn mục ruỗng.

Soi trong diff các dạng: số lượng file, số lượng test, số lượng cổng, số lượng component, danh sách "còn N chỗ" hay danh sách file vi phạm liệt kê tay.

```bash
git diff -- docs .claude | grep -nE '^\+.*[0-9]+ (file|project|test|cổng|mục|component|chỗ)'
```

Thay đúng nếu bắt được: một **lệnh** cộng **tiêu chí PASS**, không phải một con số.

> 📖 Ngoại lệ — con số là giá trị đã chốt kèm nguồn quyết định: hàng D14 ở `docs/RULES-DOCS.md`

### 6. Chạy cổng

```bash
bash .claude/check-docs.sh
```

Ghi kết quả vào báo cáo. **PASS không có nghĩa là tài liệu đúng** — ba loại lỗi cổng không bao giờ bắt được nêu ở [`../../CLAUDE.md`](../../CLAUDE.md) §8. Nói lại điều đó trong mọi báo cáo.

Cổng FAIL → liệt kê từng mục fail; đó là finding mức chặn, không cần bàn thêm.

### 7. Viết báo cáo

Theo mục Đầu ra. Mỗi finding neo theo [`../../CLAUDE.md`](../../CLAUDE.md) §9. Không neo được thì không phải finding — nó là cảm giác.

## Dừng lại và hỏi khi

1. **Diff rỗng.** Hỏi người dùng muốn review gì — working tree hay commit gần nhất.
2. **Diff chạm cả `docs/` lẫn `.claude/` với nội dung tương tự nhau ở hai bên.** Đây là dấu hiệu bản sao đang sinh ra — hỏi, đừng tự kết luận bên nào là bản gốc.
3. **Một finding đòi phải chọn file chủ giữa hai file.** Đó là quyết định của người dùng: báo cả hai vị trí, không chọn hộ; quy trình sau khi chọn ở `docs/OWNERSHIP.md` §5.
4. **Người dùng yêu cầu sửa các finding ngay trong lượt này.** Nói rõ lượt review không sửa. Finding đòi sửa nội dung luật hay quy ước → `architect`, giao kèm mã finding; sửa cơ học (đường dẫn, số §, neo, nhãn) → phiên chính — `docs/adr/0111-git-danh-sach-cho-phep-cong-docs-chan-mot-lan-architect-truoc-thi-cong-theo-dau-hieu.md` quyết định 1. `tech-writer` chỉ viết tài liệu bàn giao.
5. **Cần lệnh git ghi** — xem [`../../CLAUDE.md`](../../CLAUDE.md) §1.

## Đầu ra

Mức theo `docs/quy-uoc/tieu-chi-review.md` §2, khuôn theo §6 của cùng tệp. Ô *Phạm vi* ghi thêm nguồn diff (working tree hay commit gần nhất) và kết quả `check-docs.sh`. Thêm hai mục:

- *Kết quả theo nhóm kiểm*, ngay sau *Kết luận*: một dòng cho mỗi bước 3–6 — kết luận và bằng chứng.
- *Góp ý không phải finding*: điều không nêu được luật bị vi phạm (bước 2).

Mỗi finding ghi người sửa theo mục 4 của *Dừng lại và hỏi khi*.

## Trước khi coi là xong

1. Diff đã lấy bằng lệnh git **chỉ đọc**; không chạy lệnh git ghi nào.
2. Mỗi finding nêu được luật bị vi phạm; không nêu được thì đã chuyển sang mục góp ý.
3. Đã chạy `bash .claude/check-docs.sh` và ghi kết quả.
4. Báo cáo có mục **Lỗ mù**.
5. Không sửa bất kỳ file nào.
