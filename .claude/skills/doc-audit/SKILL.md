---
name: "doc-audit"
description: "Soát định kỳ thứ cổng check-docs.sh không bắt được vì cần đọc hiểu hoặc cần chạy code: hai tệp cùng làm chủ một chủ đề, tệp mồ côi, tuyên bố hiện trạng lệch src/, luật không ai mô tả cách thi công và quy ước không luật nào ép, cổng BE/FE khai mà không chạy. Chạy trong subagent đọc-chỉ, chỉ báo cáo. Dùng khi soát định kỳ, trước khi chốt giai đoạn, hoặc sau một đợt sửa nhiều tệp docs/ — soát một diff thì dùng /review-doc."
argument-hint: "[phạm vi] - 'BE', 'FE', một thư mục như 'docs/quy-uoc', hoặc để trống để soát toàn bộ"
context: fork
agent: Explore
model: opus
---

Phạm vi lượt này: `$ARGUMENTS`

Bạn chạy một lượt soát định kỳ, đọc-chỉ. Bạn không thấy hội thoại đã gọi skill: mọi điều cần biết nằm ở đây và ở các tệp được trỏ. Đây là lượt soát, không phải lượt tìm nhanh — mục nào đem ra đối chiếu thì đọc trọn mục đó.

## Trước khi soát

1. Đọc `.claude/CLAUDE.md` — agent `Explore` không tự nạp nó. Các mục dùng nhiều nhất ở lượt này: §4, §5, §6, §9.
2. Chốt phạm vi. Rỗng: toàn bộ `docs/`, mọi `docs/RULES-*.md`, cả hai cổng. `BE` hoặc `FE`: bảng luật và quy ước của phía đó — tệp nào giữ phía nào đọc ở `docs/RULES.md` và bảng *Tra theo chủ đề* của `docs/README.md` — cùng cổng của phía đó. Một thư mục: phép 1–3 trong thư mục đó; bỏ phép 4–5 và ghi vào *Lỗ mù*.
3. Không làm lại việc của cổng: thiếu khoá phân loại, link hay đường dẫn chết, trích dẫn vào tệp `kind: lich-su`, luật để trống cột ép đều do `check-docs.sh` bắt. Mục cổng đang có: `grep -n '^section ' .claude/check-docs.sh`.

## Phép 1 — hai tệp cùng làm chủ một chủ đề

Phép này tìm phần diễn đạt lại mà cổng §15 không bắt — giới hạn của cổng: `docs/OWNERSHIP.md` §6. So tiêu đề cấp 1 trước, không đổ tiêu đề cấp 2 của cả kho vào context:

```bash
find docs -name '*.md' -not -path 'docs/adr/*' -not -path 'docs/audit/*' -not -path 'docs/00-overview/*' | xargs grep -H '^# '
```

Cặp nào nghi trùng thì `grep -n '^## '` đúng hai tệp đó, mở mục nghi trùng và đọc. Finding nêu cả hai vị trí; hướng sửa theo quy trình ở `docs/OWNERSHIP.md` §5. Không chọn tệp chủ — việc đó ghi vào *Cần xác minh* cho người dùng.

## Phép 2 — tệp mồ côi

So đường dẫn đầy đủ sau khi giải đường tương đối, không so tên tệp: kho có nhiều tệp trùng tên. Link tới thư mục tính là trỏ tới `README.md` của thư mục đó.

Cột `awk` gọi qua biến `-v`, không viết dạng đô-la kèm chữ số: cơ chế truyền tham số của skill thay dạng đó trước khi lệnh chạy.

```bash
T=$(mktemp -d)
{ grep -rnoE '\]\([^)#]+(\.md|/)' docs .claude spec README.md --include='*.md' \
    | awk -F: -v F=1 -v M=3 '{ d=$F; if (!sub(/\/[^\/]*$/, "", d)) d="."; t=$M; sub(/^\]\(/, "", t); sub(/\/$/, "/README.md", t); print $F "\t" d "/" t }'
  grep -rnoE '`(docs|spec)/[^`]+\.md' docs .claude spec README.md --include='*.md' \
    | awk -F: -v F=1 -v M=3 '{ t=$M; sub(/^`/, "", t); print $F "\t" t }'
} > "$T/cap"
cut -f2 "$T/cap" | xargs -d '\n' realpath -m --relative-to=. | paste <(cut -f1 "$T/cap") - \
  | awk -F'\t' -v A=1 -v B=2 '$A != $B { print $B }' | sort -u > "$T/duoc-tro"
find docs -name '*.md' | sort | comm -23 - "$T/duoc-tro"
echo "da xet: $(wc -l < "$T/cap") trich dan"
```

PASS: ngoài dòng `da xet:` không in gì. `da xet: 0` là FAIL — bộ lọc đã ngừng bắt trích dẫn. Tệp `kind: lich-su` cố ý gỡ khỏi mọi bảng không phải finding. Tệp mồ côi có nội dung dày thì ghi vào *Cần xác minh*, không đề xuất xoá.

## Phép 3 — tuyên bố hiện trạng lệch `src/`

Neo vào `src/` là đường dẫn cộng một chuỗi tìm được trong tệp, không số dòng: `.claude/CLAUDE.md` §9, `docs/adr/0046-neo-trich-dan-vao-src-bang-chuoi-tim-duoc.md`. Phạm vi là một thư mục thì thay `docs` trong các lệnh dưới bằng thư mục đó.

```bash
grep -rnoE '`src/[^` *<{]+`' docs --include='*.md' | grep -vE '^docs/(adr|audit|00-overview)/' \
  | while IFS=: read -r f l p; do p=${p//\`/}; [ -e "${p%%:*}" ] || echo "$f:$l $p"; done
grep -rnE '`src/[^` ]+:[0-9]+' docs --include='*.md' | grep -vE '^docs/(adr|audit|00-overview)/'
grep -rn '✅ CÓ THẬT\|✅ Xong\|FIXED\|Đã bật' docs --include='*.md' | grep -vE '^docs/(adr|audit|00-overview)/'
```

- Lệnh 1 — đường dẫn không tồn tại: đọc dòng đó. Nằm dưới nhãn `📐`, là ví dụ, hay là đường dẫn tương đối trong một phía thì không phải finding; khai là hiện trạng thì là finding.
- Lệnh 2 — mỗi dòng in ra là một neo bằng số dòng.
- Lệnh 3 — mỗi dòng: mở tệp `src/` được neo, `grep -F` chuỗi neo, rồi đọc xem code có làm đúng điều tuyên bố không. Ngày hợp lệ không chứng minh nội dung đúng.

## Phép 4 — luật ↔ quy ước, hai chiều

- **Luật treo.** Mỗi dòng luật ở `docs/RULES-*.md` trong phạm vi: có tệp quy ước mô tả đủ để làm theo không? Tra bảng *Tra theo chủ đề* của `docs/README.md`, mở đúng một tệp mỗi chủ đề. Kết luận PASS, PARTIAL kèm phần thiếu, hoặc MISSING.
- **Quy ước không luật nào ép.** Câu mệnh lệnh trong tệp quy ước — phải, không được, luôn, cấm, MUST — mà không có dòng ở `RULES-*`: thiếu dòng luật là finding; luật chưa có cổng mà ô trạng thái không nói rõ là finding; gợi ý mang giọng luật ghi vào *Không phải finding*. Không phân được là luật hay gợi ý thì ghi *Cần xác minh*.
- Luật mang `📐`: miễn đúng phần `docs/quy-uoc/tieu-chi-review.md` §3.2 cho miễn.

## Phép 5 — cổng khai mà không chạy

Chạy khi phạm vi rỗng, `BE` hoặc `FE`.

- Chạy lệnh ở `docs/RULES.md` mục *Tự rà cột "Trạng thái" — luật D39*; tiêu chí PASS ghi ngay dưới lệnh.
- BE: chạy ArchTests theo `docs/wiki-core/be/04-testing-strategy.md` §4.2; máy không có Docker thì dùng lối loại trừ tường minh ở đó và ghi nhóm bị loại vào *Lỗ mù*.
- FE: chạy theo `docs/wiki-core/fe/trien-khai/05-gate.md` §5; đọc §4 của cùng tệp trước khi kết luận cổng FE xanh. Bước 4a chạy được — tệp tạm của nó nằm ngoài repo. Bước 4b chép tệp vào `src/`, trái ràng buộc cuối skill này: không chạy trong lượt soát, ghi vào *Lỗ mù*.
- Test hay section có tên ở cột *Ép bằng gì* của một luật `✅` mà đỏ hoặc không tồn tại là finding. Bước nào không chạy được thì ghi *Lỗ mù*; không tuyên bố xanh cho bước chưa chạy.

## Báo cáo

- Mức theo `docs/quy-uoc/tieu-chi-review.md` §2, khuôn theo §6 của cùng tệp. Thêm mục cuối *Lỗ mù*: phép nào không chạy, phạm vi nào bỏ, vì sao.
- Mọi finding có neo: tới `docs/` bằng `tệp:dòng`, tới `src/` bằng đường dẫn cộng chuỗi. Không neo được thì ghi *Cần xác minh*.
- Mỗi finding ghi người sửa: sửa nội dung luật hay quy ước → `architect`; sửa cơ học (đường dẫn, số §, neo, nhãn) → phiên chính — `docs/adr/0111-git-danh-sach-cho-phep-cong-docs-chan-mot-lan-architect-truoc-thi-cong-theo-dau-hieu.md` quyết định 1.
- Không hỏi giữa chừng: điều cần người dùng quyết ghi vào *Cần xác minh*.
- Chỉ báo cáo: không sửa tệp nào, không tạo tệp trong repo — tệp tạm chỉ trong thư mục do `mktemp -d` tạo. Không chạy lệnh git ghi (`.claude/CLAUDE.md` §1).
