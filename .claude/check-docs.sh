#!/usr/bin/env bash
# check-docs.sh — cổng tài liệu cho CoreAndSkill
#
# Kiểm các luật trong .claude/CLAUDE.md. Chạy từ gốc repo:
#     bash .claude/check-docs.sh
#
# Thoát 0 = PASS. Thoát 1 = có vi phạm. Thoát 2 = không chạy được (cây repo sai).
#
# ── HIỆU NĂNG — đọc trước khi sửa ──────────────────────────────────────────────
# Mỗi mục dùng ĐÚNG MỘT lệnh ngoài (grep/awk) quét toàn repo; phần phân tích bên
# trong vòng lặp chỉ dùng builtin của bash (mở rộng tham số, `case`).
#
# Trên Git Bash/Windows mỗi lần spawn tiến trình tốn ~50ms, và một cổng chậm là
# một cổng bị bỏ qua. Đừng đưa `grep`/`sed`/`wc` vào trong vòng lặp.
#
# ⚠️ PASS KHÔNG CÓ NGHĨA LÀ TÀI LIỆU ĐÚNG. Cổng chỉ bắt được thứ máy kiểm được.
#    Ba loại lỗi nó không bao giờ bắt: văn xuôi tả thứ không tồn tại; sơ đồ/cây
#    thư mục chép sai; ngày đúng nhưng nội dung sai. Xem CLAUDE.md §8.

set -uo pipefail

# Ép locale UTF-8 tường minh — không dựa vào locale của shell gọi script.
# Với LANG rỗng, `grep -P` thoát mã 2 và vòng lặp `while read` bọc ngoài chỉ
# đơn giản không nhận dòng nào — mục đó in OK mà không kiểm gì.
export LC_ALL=en_US.UTF-8
cd "$(dirname "$0")/.." || exit 2

FAIL=0
BT='`'
section() { printf '\n\033[1m== %s ==\033[0m\n' "$1"; }
bad()     { printf '  \033[31mFAIL\033[0m  %s\n' "$1"; FAIL=1; }
ok()      { printf '  \033[32mOK\033[0m    %s\n' "$1"; }
warn()    { printf '  \033[33mNOTE\033[0m  %s\n' "$1"; }

# ================================================================ §0
# "Tôi có đang xét gì không?"
#
# PASS phải phân biệt được "mọi thứ đúng" với "tôi không nhìn gì cả".
#
# Luật: MỖI MỤC PHẢI TỰ CHỨNG MINH NÓ CÓ DỮ LIỆU ĐẦU VÀO.
# Bối cảnh: docs/audit/2026-09-09-cong-no-op-va-hai-mo-hinh-nguon.md
for m in .claude/CLAUDE.md docs/README.md docs/RULES.md; do
  [ -e "$m" ] || { printf '\033[31m❌ ABORT — không thấy %s. Chạy script từ gốc repo.\033[0m\n' "$m"; exit 2; }
done
MIN_DOCS="${CHECKDOCS_MIN:-30}"
_n_doc=$(find docs -name '*.md' 2>/dev/null | wc -l)
case "$_n_doc" in
  ''|*[!0-9]*)
    printf '[31m❌ ABORT — không đếm được số file .md trong docs/ (giá trị: %s). Phép đếm hỏng; PASS lúc này vô nghĩa.[0m
' "$_n_doc"
    exit 2 ;;
esac
if [ "$_n_doc" -lt "$MIN_DOCS" ]; then
  printf '\033[31m❌ ABORT — chỉ thấy %s file .md trong docs/ (ngưỡng %s). Cây repo không đầy đủ; PASS lúc này vô nghĩa.\033[0m\n' "$_n_doc" "$MIN_DOCS"
  exit 2
fi

SCAN_DIRS="docs .claude"
[ -d spec ] && SCAN_DIRS="$SCAN_DIRS spec"
# README.md o goc repo la CUA VAO DAU TIEN cua repo va la file duy nhat git dang
# theo doi o giai doan 1 - nhung no khong nam trong khu nao ca, nen moi link chet
# trong do la link chet o cho de thay nhat ma khong mot muc nao nhin toi.
# Chi them vao SCAN_DIRS (§3, §4...), KHONG them vao SCAN_FM: README.md goc
# khong phai file docs/ nen no khong phai khai ba khoa phan loai (§12).
[ -f README.md ] && SCAN_DIRS="$SCAN_DIRS README.md"
printf 'Quét %s file .md · khu: %s\n' "$_n_doc" "$SCAN_DIRS"

# ---------------------------------------------------------------- miễn trừ
# File mang banner "TÀI LIỆU LỊCH SỬ" (CLAUDE.md §5) mô tả trạng thái QUÁ KHỨ —
# đường dẫn và mốc thời gian trong đó cố ý không còn đúng.
#
# CHỈ NHẬN BANNER Ở 30 DÒNG ĐẦU: khớp mọi dòng thì một file chủ chỉ NHẮC cụm này
# trong một ô bảng nói về file khác cũng bị miễn trừ toàn bộ.
HIST_FILES=" $(find $SCAN_DIRS -name '*.md' -print0 2>/dev/null | xargs -0 awk '
  FNR==1 { hit=0 }
  hit || FNR>30 { next }
  /TÀI LIỆU LỊCH SỬ/ { print FILENAME; hit=1 }' 2>/dev/null | tr '\n' ' ')"

# Dòng nói rõ nó đang nhắc tới thứ đã biến mất cũng được miễn trừ — nếu không,
# mọi ghi chép "vì sao ta bỏ X" đều bị báo lỗi, và người ta sẽ xoá bài học đi
# cho cổng xanh. Đó chính là hành vi luật này tồn tại để ngăn.
HIST_LINES=" $(grep -rn 'trước ở\|trước nằm ở\|đã xoá\|đã chuyển\|đã bỏ\|không còn tồn tại\|chưa từng tồn tại\|loại khỏi repo\|check-ignore\|dự án tiền nhiệm\|đã thay thế\|sẽ được điền\|giai đoạn 2' $SCAN_DIRS --include='*.md' 2>/dev/null | cut -d: -f1,2 | tr '\n' ' ')"

# Dòng nằm TRONG khối code có rào (```). Một pattern `grep` viết trong khối lệnh
# không phải là một tuyên bố về hiện trạng — nó là công cụ đi tìm tuyên bố đó.
# Không có miễn trừ này thì chính skill dò vi phạm sẽ bị báo là vi phạm.
CODEBLOCK=" $(find $SCAN_DIRS -name '*.md' -print0 2>/dev/null | xargs -0 awk '
  FNR==1 { inblk=0 }
  /^[ \t]*```/ { inblk = !inblk; print FILENAME ":" FNR; next }
  inblk { print FILENAME ":" FNR }' 2>/dev/null | tr '\n' ' ')"

# ================================================================ §1
# Bảng cấm git trong CLAUDE.md §1 phải khớp permissions.deny trong settings.json.
# §1 tự tuyên bố lệnh cấm "được cưỡng chế bằng máy" — nếu văn bản liệt kê một
# lệnh mà deny không chặn, câu đó thành lời hứa suông.
section "§1  Bảng cấm trong CLAUDE.md §1 phải khớp settings.json"
if [ ! -e .claude/settings.json ]; then
  bad "không thấy .claude/settings.json — không kiểm được §1"
else
  # Chỉ gỡ xuống dòng, KHÔNG gỡ dấu cách: chuỗi cần so khớp là "Bash(git add:"
  # — gỡ dấu cách biến nó thành "Bash(gitadd:" và mọi lệnh đều báo sai.
  DENY=$(tr -d '\n' < .claude/settings.json)
  n=0; lay=0
  while IFS= read -r cmd; do
    [ -z "$cmd" ] && continue
    case "$cmd" in *' '*) continue ;; esac
    lay=$((lay+1))
    # Nhan CA hai dang: "Bash(git tag:*)" va "Bash(git remote add:*)". Khong
    # duoc mien tru mot lenh cung trong script — mot ngoai le khong khai o dau
    # ca la mot ngoai le khong ai kiem lai.
    case "$DENY" in *"Bash(git $cmd:"*|*"Bash(git $cmd "*) continue ;; esac
    bad "CLAUDE.md §1 cấm 'git $cmd' nhưng settings.json deny KHÔNG chặn"
    n=$((n+1))
  done < <(grep -m1 'CẤM' .claude/CLAUDE.md | grep -oP "$BT\\K[a-z-]+(?=$BT)")
  # Phan NON-GIT cua §1. CLAUDE.md §1 liet ke them mot loat lenh pha huy khong
  # phai git; thieu doi chieu thi mot dong them vao van ban ma quen them vao
  # settings.json se khong ai biet.
  lay2=0
  while IFS= read -r cmd; do
    [ -z "$cmd" ] && continue
    # Chi xet lenh NHIEU TU. Mot tu don la ten lenh git da duoc vong lap tren
    # xet roi (vi du `remote` trong chinh cau van giai thich).
    case "$cmd" in *' '*) ;; *) continue ;; esac
    # Bo dang co CO: `git remote -v` chi doc, no duoc phep va khong duoc deny.
    case "$cmd" in *' -'*) continue ;; esac
    lay2=$((lay2+1))
    case "$DENY" in *"Bash($cmd:"*) continue ;; esac
    bad "CLAUDE.md §1 cấm '$cmd' nhưng settings.json deny KHÔNG chặn"
    n=$((n+1))
  done < <({ grep -m1 'chặn thêm' .claude/CLAUDE.md; grep -m1 'dạng ghi của' .claude/CLAUDE.md; } | grep -oP "$BT\K[a-z][a-z0-9 -]+(?=$BT)")

  if [ "$lay" -eq 0 ]; then
    bad "§1 KHÔNG trích được lệnh git nào từ bảng cấm — mục này đang không kiểm gì"
  elif [ "$lay2" -eq 0 ]; then
    bad "§1 KHÔNG trích được lệnh non-git nào từ CLAUDE.md §1 — nửa sau của mục này đang không kiểm gì"
  elif [ "$n" -eq 0 ]; then
    ok "mọi lệnh trong bảng cấm đều được deny chặn ($lay lệnh git, $lay2 lệnh khác)"
  fi
fi

# ================================================================ §2
# .claude/ chỉ chứa quy trình. Code mẫu là tri thức -> thuộc docs/.
# Cho phép: bash/sh (lệnh chạy), markdown (mẫu báo cáo), text (khối $ARGUMENTS),
# và khối không gắn ngôn ngữ (sơ đồ cây thư mục, ASCII).
section "§2  .claude/ không được chứa code block ngôn ngữ lập trình"
n=0; seen=0
while IFS= read -r hit; do
  [ -z "$hit" ] && continue
  seen=$((seen+1))
  f="${hit%%:*}"; rest="${hit#*:}"; ln="${rest%%:*}"
  lang="${hit##*\`\`\`}"
  case "$lang" in bash|sh|shell|markdown|md|text|txt|diff|"") continue ;; esac
  bad "$f:$ln — code block '$lang' (tri thức thuộc docs/)"
  n=$((n+1))
done < <(grep -rn '^[[:space:]]*```[a-zA-Z]' .claude --include='*.md' 2>/dev/null)
# Fence THỤT LỀ (trong một mục danh sách) cũng là code block — neo `^``` sẽ
# để lọt một khối ```csharp thụt hai dấu cách.
if [ "$seen" -eq 0 ]; then
  bad "§2 KHÔNG trích được code block nào trong .claude/ — mục này đang không kiểm gì"
elif [ "$n" -eq 0 ]; then
  ok "không có code block ngôn ngữ lập trình trong .claude/ ($seen khối đã xét)"
fi

# ================================================================ §3
# Mọi link markdown nội bộ phải resolve được.
section "§3  Link markdown nội bộ phải resolve được"
n=0; seen=0
while IFS= read -r hit; do
  [ -z "$hit" ] && continue
  f="${hit%%:*}"; rest="${hit#*:}"; ln="${rest%%:*}"; raw="${rest#*:}"
  tgt="${raw#\](}"; tgt="${tgt%)}"
  case "$tgt" in http*|mailto:*|'#'*|'') continue ;; esac
  tgt="${tgt%%#*}"
  [ -z "$tgt" ] && continue
  seen=$((seen+1))
  # File o GOC repo khong co dau "/" trong ten, nen "${f%/*}" tra ve nguyen ten
  # file chu khong phai thu muc chua no - va moi link tuong doi trong README.md
  # goc bi bao chet oan. Bay nay chi lo ra khi README.md duoc dua vao tam quet.
  case "$f" in */*) d="${f%/*}" ;; *) d="." ;; esac
  [ -e "$d/$tgt" ] && continue
  bad "$f:$ln — link chết: $tgt"
  n=$((n+1))
done < <(grep -rnoE '\]\([^)]+\)' $SCAN_DIRS --include='*.md' 2>/dev/null)
if [ "$seen" -eq 0 ]; then
  bad "§3 không trích được link nào — mục này đang không kiểm gì"
elif [ "$n" -eq 0 ]; then
  ok "mọi link nội bộ resolve được ($seen link)"
fi

# ================================================================ §4 + §5 + §7
# Ba mục dùng CHUNG một lần quét: đường dẫn nằm trong code span.
#   §4 — đường dẫn phải tồn tại
#   §5 — không neo vào file bị gitignore loại khỏi repo
#   §7 — trích dẫn dạng file:dòng phải nằm trong file
PATHS=$(grep -rnoP "$BT\\K(docs|\\.claude|spec)/[^$BT]*(?=$BT)" $SCAN_DIRS --include='*.md' 2>/dev/null)

section "§4  Đường dẫn được trích dẫn phải tồn tại"
n=0; seen=0
while IFS= read -r hit; do
  [ -z "$hit" ] && continue
  f="${hit%%:*}"; rest="${hit#*:}"; ln="${rest%%:*}"; p="${rest#*:}"
  case "$HIST_FILES" in *" $f "*) continue ;; esac
  case "$HIST_LINES" in *" $f:$ln "*) continue ;; esac
  # MIỄN TRỪ có khai báo: docs/00-overview/ là tài liệu LẬP KẾ HOẠCH, không phải
  # quy tắc kỹ thuật (docs/README.md dán nhãn khu này là "🗄️ kế hoạch"). Mọi
  # đường dẫn nó nêu hoặc thuộc repo tiền nhiệm, hoặc thuộc cấu trúc giai đoạn 2
  # chưa dựng — "chưa tồn tại" chính là nội dung nó đang nói, không phải lỗi.
  #
  # Cái giá của miễn trừ này, nói rõ để không ai tưởng nó miễn phí: một đường dẫn
  # gõ sai trong khu kế hoạch sẽ KHÔNG bị bắt. Chấp nhận được vì khu này không
  # phải nguồn để code bám theo — nhưng nếu một ngày nó thành nguồn thì phải gỡ
  # miễn trừ này ra trước.
  case "$f" in docs/00-overview/*) continue ;; esac
  case "$p" in *'*'*|*'<'*|*'{'*|*' '*|*'…'*) continue ;; esac
  p="${p%:*[0-9]}"; p="${p%/}"
  case "$p" in *:[0-9]*) p="${p%:*}" ;; esac
  seen=$((seen+1))
  [ -e "$p" ] && continue
  bad "$f:$ln — đường dẫn không tồn tại: $p"
  n=$((n+1))
done <<< "$PATHS"
if [ "$seen" -eq 0 ]; then
  bad "§4 không trích được đường dẫn nào — mục này đang không kiểm gì"
elif [ "$n" -eq 0 ]; then
  ok "mọi đường dẫn trích dẫn đều tồn tại ($seen đường dẫn)"
fi

# §5 — CỐ Ý không báo lỗi cho file mới chưa commit: một cổng đo tiến độ commit
# là cổng người ta tắt đi. Chỉ bắt file bị loại trừ THEO THIẾT KẾ.
section "§5  Trích dẫn không neo vào file bị gitignore"
if ! git rev-parse --git-dir >/dev/null 2>&1; then
  warn "không phải git repo — §5 không kiểm gì (khai báo tường minh)"
else
  CAND=""; seen=0
  while IFS= read -r hit; do
    [ -z "$hit" ] && continue
    seen=$((seen+1))
    rest="${hit#*:}"; p="${rest#*:}"
    case "$p" in *'*'*|*'<'*|*'{'*|*' '*|*'…'*) continue ;; esac
    case "$p" in *:[0-9]*) p="${p%:*}" ;; esac
    [ -e "$p" ] || continue
    CAND="$CAND$p"$'\n'
    # §5 quét RỘNG HƠN §4, cố ý.
    #
    # §4 chỉ nhận tiền tố docs/ .claude/ spec/ — khu BẮT BUỘC tồn tại hôm nay.
    # Khu giai đoạn 2 (src/, scripts/, database/) chưa dựng, nên bắt "không tồn
    # tại" ở đó là báo sai.
    #
    # §5 thì khác: nó chỉ báo khi file THẬT SỰ TỒN TẠI và bị gitignore loại trừ
    # — không tồn tại thì bỏ qua ngay ở dòng `[ -e ]` trên. Nên mở rộng phạm vi
    # ở đây không sinh phát hiện sai nào, mà giữ được đúng lớp lỗi §5 tồn tại để
    # bắt: bằng chứng mà người thứ hai clone về không bao giờ mở được.
  done < <(grep -rnoP "$BT\\K[A-Za-z0-9_.][A-Za-z0-9_./-]*/[^$BT]*(?=$BT)" $SCAN_DIRS --include='*.md' 2>/dev/null)
  IGN=$(printf '%s' "$CAND" | sort -u | git check-ignore --stdin 2>/dev/null)
  # CANARY cho NUA SAU cua muc nay. `seen` chi chung minh nua truoc (grep trich
  # ung vien) con song. Dieu kien PASS that lai la `[ -z "$IGN" ]`, ma IGN sinh
  # tu `git check-ignore` — neu lenh do that bai thi IGN rong va muc in PASS
  # KEM MOT CON SO LON, tuc mot PASS gia trong suc thuyet phuc nhat toan script.
  CANARY_IGN=$(printf '%s\n' '.claude/.state/canary' | git check-ignore --stdin 2>/dev/null)
  # Dieu kien PASS cua muc nay la "grep khong tra ve gi" — thuan phu dinh, nen
  # no KHONG phan biet duoc "khong co vi pham" voi "phep do da hong". Bo dem
  # `seen` la thu duy nhat phan biet hai trang thai do.
  if [ "$seen" -eq 0 ]; then
    bad "§5 KHÔNG trích được ứng viên đường dẫn nào — mục này đang không kiểm gì"
  elif [ -z "$CANARY_IGN" ]; then
    bad "§5 canary hỏng — git check-ignore không nhận ra cả một đường dẫn chắc chắn bị loại trừ; nửa sau của mục này đang không kiểm gì"
  elif [ -z "$IGN" ]; then
    ok "không trích dẫn nào neo vào file bị gitignore ($seen ứng viên đã xét)"
  else
    while IFS= read -r p; do
      [ -z "$p" ] && continue
      bad "trích dẫn neo vào file bị gitignore: $p"
    done <<< "$IGN"
  fi
fi

# §7 — phép đo GIÁN TIẾP CỦA TÍNH TRUNG THỰC: tài liệu bịa bằng chứng thường bịa
# luôn số dòng, mà số dòng thì máy đếm được. Ở dự án tiền nhiệm, ngày thêm kiểm
# này nó lập tức tìm ra hai lời nói dối mà nhiều lượt review đã bỏ qua.
section "§7  Trích dẫn file:dòng phải nằm trong file"
declare -A LINECOUNT
n=0; seen=0
while IFS= read -r hit; do
  [ -z "$hit" ] && continue
  f="${hit%%:*}"; rest="${hit#*:}"; ln="${rest%%:*}"; ref="${rest#*:}"
  case "$ref" in *:[0-9]*) ;; *) continue ;; esac
  case "$HIST_FILES" in *" $f "*) continue ;; esac
  case "$HIST_LINES" in *" $f:$ln "*) continue ;; esac
  tf="${ref%:*}"; tl="${ref##*:}"
  case "$tl" in *[!0-9]*|'') continue ;; esac
  [ -e "$tf" ] || continue
  seen=$((seen+1))
  if [ -z "${LINECOUNT[$tf]+x}" ]; then
    LINECOUNT[$tf]=$(wc -l < "$tf")
  fi
  total=${LINECOUNT[$tf]}
  [ "$tl" -le "$total" ] && [ "$tl" -ge 1 ] && continue
  bad "$f:$ln — trích dẫn $ref nhưng file chỉ có $total dòng"
  n=$((n+1))
done <<< "$PATHS"
# 0 trích dẫn KHÔNG phải là PASS. Ở giai đoạn 1 chưa có src/ để neo nên con số
# này hợp lệ bằng 0 — nhưng mục phải NÓI RA rằng nó không kiểm gì, thay vì im
# lặng in OK. Một no-op im lặng là cách cổng chết mà không ai biết
# (docs/audit/2026-08-23-cong-khong-ton-tai.md).
if [ "$n" -eq 0 ]; then
  if [ "$seen" -eq 0 ]; then
    warn "không có trích dẫn file:dòng nào — §7 KHÔNG kiểm gì (khai báo tường minh, không phải PASS)"
  else
    ok "mọi trích dẫn file:dòng nằm trong file ($seen trích dẫn)"
  fi
fi

# ================================================================ §6
# Tuyên bố hoàn thành phải kèm ngày đối chiếu (CLAUDE.md §4).
# Dạng sai đắt nhất: không gây lỗi biên dịch, không bị test bắt, và nhãn "đã
# xong" được thiết kế để không ai kiểm lại.
#
# MIỄN TRỪ: nhắc tới nhãn ≠ tuyên bố bằng nhãn. Một dòng ĐỊNH NGHĨA nhãn ("nhãn
# `✅ CÓ THẬT` nghĩa là…") hoặc TRÍCH nó làm ví dụ xấu ("…đánh dấu `FIXED` khi
# giá trị chưa hề vào code") không phải là tuyên bố hoàn thành. Không có miễn
# trừ này thì chính tài liệu mô tả luật sẽ vi phạm luật, và người ta sẽ xoá phần
# mô tả đi cho cổng xanh — đúng hành vi luật này tồn tại để ngăn.
#
# Dấu hiệu máy đọc được: khi nhắc tới nhãn, người viết BỌC nó trong code span
# hoặc dấu ngoặc kép. Khi tuyên bố, họ viết trần. Nên: gỡ mọi đoạn được bọc ra
# khỏi dòng, rồi mới xét — còn sót nhãn nghĩa là có tuyên bố trần.
section "§6  Tuyên bố hoàn thành phải kèm ngày đối chiếu"
# `raw` đếm dòng grep TRÍCH ĐƯỢC, `seen` đếm dòng còn lại sau miễn trừ. Đừng
# gộp làm một: một biến đếm tăng ngay trước `bad` là đếm VI PHẠM, không đếm đầu
# vào, nên nó in "(0 dòng đã xét)" cả khi pattern đã hỏng.
n=0; seen=0; raw=0
while IFS= read -r hit; do
  [ -z "$hit" ] && continue
  raw=$((raw+1))
  f="${hit%%:*}"; rest="${hit#*:}"; ln="${rest%%:*}"; body="${rest#*:}"
  case "$HIST_FILES" in *" $f "*) continue ;; esac
  case "$HIST_LINES" in *" $f:$ln "*) continue ;; esac
  case "$CODEBLOCK" in *" $f:$ln "*) continue ;; esac
  case "$hit" in *20[0-9][0-9]-[01][0-9]-[0-3][0-9]*) continue ;; esac
  # Tiêu đề mục ĐẶT TÊN cho luật, không tuyên bố đã làm xong việc gì.
  case "$body" in '#'*|'> #'*) continue ;; esac
  seen=$((seen+1))
  # gỡ các đoạn trong code span và trong ngoặc kép (thẳng lẫn cong)
  s="$body"
  for q in '`' '"' '“' '”'; do
    out=""; tmp="$s"
    while [ "${tmp#*$q}" != "$tmp" ]; do
      out="$out${tmp%%$q*}"; tmp="${tmp#*$q}"
      [ "${tmp#*$q}" = "$tmp" ] && { tmp=""; break; }
      tmp="${tmp#*$q}"
    done
    s="$out$tmp"
  done
  case "$s" in
    *'ĐÃ CÓ'*|*'✅ Xong'*|*FIXED*|*'Đã bật'*|*'CÓ THẬT'*) ;;
    *) continue ;;
  esac
  bad "$f:$ln — tuyên bố hoàn thành không kèm ngày đối chiếu"
  n=$((n+1))
done < <(grep -rn 'ĐÃ CÓ\|✅ Xong\|FIXED\|Đã bật\|CÓ THẬT' $SCAN_DIRS --include='*.md' 2>/dev/null)
if [ "$raw" -eq 0 ]; then
  bad "§6 KHÔNG trích được dòng nào chứa nhãn hoàn thành — pattern hỏng, mục này đang không kiểm gì"
elif [ "$n" -eq 0 ]; then
  ok "mọi tuyên bố hoàn thành đều kèm ngày ($raw dòng trích, $seen dòng còn lại sau miễn trừ)"
fi

# ================================================================ §8
# Bảng định tuyến phải trỏ ĐÚNG chủ đề: ô chủ đề nêu đích danh một ĐỊNH DANH MÃ
# trong code span thì định danh đó phải có trong file đích.
# Đường dẫn đúng tới FILE SAI vẫn qua được §4; mục này bắt nó.
#
# Chỉ xét định danh trông như tên kiểu/hàm/hằng (có chữ hoa, không khoảng trắng,
# không dấu câu). Placeholder dạng {X} và văn xuôi bị loại — nếu không, mục này
# sinh ra hàng loạt phát hiện sai và người ta sẽ tắt nó đi.
section "§8  Bảng định tuyến phải trỏ đúng chủ đề"
# Mot bang markdown khong tu khai no la bang gi. Ban truoc phai DOAN bang hinh
# dang cua o cuoi, va moi phep doan deu doi bao-sai lay bo-lot: neo tien to
# `docs/` thi mu voi duong dan tuong doi; nhan "o cuoi co lien ket" thi bao sai
# bang van xuoi co kem dan chung.
#
# Nay khong doan nua: bang dinh tuyen tu khai bang TIEU DE COT CUOI. awk duoi
# day chi phat ra hang cua bang co tieu de cot cuoi nam trong tap da chot; moi
# bang khac bi bo qua truoc khi vao vong lap.
#
# Danh sach tieu de hop le la HOP DONG — them mot tieu de moi o tai lieu ma
# quen them vao day thi bang do khong duoc canh, va §8 se bao "xet 0 dinh danh".
declare -A FBODY
n=0; seen=0; rows=0
while IFS= read -r hit; do
  [ -z "$hit" ] && continue
  rows=$((rows+1))
  f="${hit%%:*}"; rest="${hit#*:}"; ln="${rest%%:*}"; body="${rest#*:}"
  case "$HIST_FILES" in *" $f "*) continue ;; esac
  case "$HIST_LINES" in *" $f:$ln "*) continue ;; esac
  case "$f" in */*) fd="${f%/*}" ;; *) fd="." ;; esac
  # Da biet chac day la hang dinh tuyen, nen duong dan o O NAO CUNG DUOC.
  targets=""; head_part=""; tmp2="$body"
  while [ "${tmp2#*\`}" != "$tmp2" ]; do
    pre="${tmp2%%\`*}"; tmp2="${tmp2#*\`}"
    span="${tmp2%%\`*}"
    [ "$span" = "$tmp2" ] && break
    tmp2="${tmp2#*\`}"
    case "$span" in
      *.md)
        if [ -e "$span" ]; then targets="$targets $span"
        elif [ -e "$fd/$span" ]; then targets="$targets $fd/$span"
        fi ;;
      *) head_part="$head_part$pre\`$span\`" ;;
    esac
  done
  [ -z "$targets" ] && continue
  tmp="$head_part"
  while [ "${tmp#*\`}" != "$tmp" ]; do
    tmp="${tmp#*\`}"
    ident="${tmp%%\`*}"
    [ "$ident" = "$tmp" ] && break
    tmp="${tmp#*\`}"
    case "$ident" in
      *' '*|*','*|*'|'*|*'{'*|*'/'*|'') continue ;;
      *[A-Z]*) ;;
      *) continue ;;
    esac
    base="${ident%%<*}"; base="${base%%(*}"
    [ ${#base} -lt 3 ] && continue
    seen=$((seen+1))
    found=0
    for tgt in $targets; do
      if [ -z "${FBODY[$tgt]+x}" ]; then FBODY[$tgt]=$(cat "$tgt"); fi
      case "${FBODY[$tgt]}" in *"$base"*) found=1; break ;; esac
    done
    [ "$found" -eq 1 ] && continue
    bad "$f:$ln — trỏ '${targets# }' nhưng không file nào nhắc '$base'"
    n=$((n+1))
  done
done < <(find $SCAN_DIRS -name '*.md' -print0 2>/dev/null | xargs -0 awk '
  function trim(x){ gsub(/^[ 	]+|[ 	]+$/,"",x); return x }
  FNR==1 { ok=0 }
  /^\|/ {
    line=$0; body=line; sub(/^\|/,"",body); sub(/\|[ 	]*$/,"",body)
    nc=split(body, c, "|"); last=trim(c[nc])
    if (last ~ /^[-: ]+$/) next
    if (ok==0) {
      if (last=="File" || last=="Đọc" || last=="Đọc file nào" || last=="Đọc theo thứ tự" || last=="Đường dẫn nguồn" || last=="Khuôn" || last=="Đích" || last=="Ở file") ok=1
      next
    }
    print FILENAME ":" FNR ":" line; next
  }
  { ok=0 }
')
if [ "$rows" -eq 0 ]; then
  bad "§8 KHÔNG trích được hàng nào từ bảng định tuyến — tập tiêu đề cột đã lệch khỏi tài liệu"
elif [ "$seen" -eq 0 ]; then
  bad "§8 trích được $rows hàng nhưng KHÔNG định danh nào — mục này đang không kiểm gì"
elif [ "$n" -eq 0 ]; then
  ok "bảng định tuyến trỏ đúng chủ đề ($rows hàng, $seen định danh đã xét)"
fi

# ================================================================ §9
# Tên file .md viết trong code span ở .claude/ phải resolve được — bắt đường dẫn
# cụt sau khi di trú file.
section "§9  Tên file .md trong .claude/ phải resolve được"
ALLMD=" $(find docs .claude -name '*.md' -printf '%f\n' 2>/dev/null | sort -u | tr '\n' ' ')"
[ -d spec ] && ALLMD="$ALLMD$(find spec -name '*.md' -printf '%f\n' 2>/dev/null | sort -u | tr '\n' ' ')"
n=0; seen=0
while IFS= read -r hit; do
  [ -z "$hit" ] && continue
  f="${hit%%:*}"; rest="${hit#*:}"; ln="${rest%%:*}"; name="${rest#*:}"
  case "$HIST_LINES" in *" $f:$ln "*) continue ;; esac
  case "$name" in */*) continue ;; esac
  seen=$((seen+1))
  case "$ALLMD" in *" $name "*) continue ;; esac
  bad "$f:$ln — không resolve được tên file: $name"
  n=$((n+1))
done < <(grep -rnoP "$BT\\K[A-Za-z0-9._-]+\\.md(?=$BT)" .claude --include='*.md' 2>/dev/null)
if [ "$seen" -eq 0 ]; then
  bad "§9 KHÔNG trích được tên file .md nào trong .claude/ — mục này đang không kiểm gì"
elif [ "$n" -eq 0 ]; then
  ok "mọi tên file .md trong .claude/ resolve được ($seen tên)"
fi

# ================================================================ §10
# Chú thích trong src/ trỏ docs/ phải tồn tại.
# GIAI ĐOẠN 1: chưa có src/. Mục này là NO-OP CÓ KHAI BÁO — nó phải NÓI RA rằng
# nó không kiểm gì, thay vì im lặng in OK. Một no-op im lặng là cách cổng chết
# mà không ai biết (docs/audit/2026-08-23-cong-khong-ton-tai.md).
section "§10 Chú thích trong src/ trỏ docs/ phải tồn tại"
if [ ! -d src ]; then
  warn "chưa có src/ — §10 KHÔNG kiểm gì (khai báo tường minh, không phải PASS)"
else
  n=0; seen=0
  while IFS= read -r hit; do
    [ -z "$hit" ] && continue
    f="${hit%%:*}"; rest="${hit#*:}"; ln="${rest%%:*}"; p="${rest#*:}"
    p="${p%.}"; p="${p%,}"; p="${p%)}"
    seen=$((seen+1))
    [ -e "$p" ] && continue
    bad "$f:$ln — chú thích trỏ docs/ không tồn tại: $p"
    n=$((n+1))
  done < <(grep -rnoP '(?<![A-Za-z0-9_/.-])docs/[A-Za-z0-9._/-]+' src --include='*.cs' --include='*.ts' --include='*.scss' --include='*.html' 2>/dev/null)
  [ "$n" -eq 0 ] && ok "mọi chú thích trong src/ trỏ docs/ đều tồn tại ($seen đường dẫn)"
fi

# ================================================================ §11
# Không được tuyên bố CÓ THẬT khi chưa có src/ để đối chiếu (CLAUDE.md §4).
section "§11 Không tuyên bố hiện trạng khi chưa có src/"
if [ -d src ]; then
  warn "đã có src/ — §11 chỉ áp ở giai đoạn 1"
else
  # CANARY. Muc nay khong the dung bo dem dau vao: 0 dong khop la trang thai
  # DUNG o giai doan 1, nen `raw -eq 0 -> bad` se do oan.
  #
  # Nhung dieu kien PASS cua no van la thuan phu dinh, va pattern cua no chua
  # mot ky tu emoji — dung ca ma §0 (dong 44-46) ghi lai: mot muc la no-op im
  # lang suot thoi gian dai vi grep khong khop duoc emoji trong pattern.
  #
  # Canary chung minh phep do CON SONG bang cach cho no khop mot chuoi dung
  # san. Pattern hong thi canary hong truoc, va muc nay noi ra.
  if ! printf '%s\n' '✅ CÓ THẬT' | grep -q '✅ CÓ THẬT'; then
    bad "§11 canary hỏng — pattern '✅ CÓ THẬT' không khớp được cả chuỗi dựng sẵn; mục này đang không kiểm gì"
  else
  n=0; seen=0
  while IFS= read -r hit; do
    [ -z "$hit" ] && continue
    seen=$((seen+1))
    f="${hit%%:*}"; rest="${hit#*:}"; ln="${rest%%:*}"
    case "$HIST_FILES" in *" $f "*) continue ;; esac
    case "$HIST_LINES" in *" $f:$ln "*) continue ;; esac
    case "$f" in docs/00-overview/*) continue ;; esac
    bad "$f:$ln — tuyên bố 'CÓ THẬT' nhưng repo chưa có src/ để đối chiếu"
    n=$((n+1))
  done < <(grep -rn '✅ CÓ THẬT' docs --include='*.md' 2>/dev/null)
  [ "$n" -eq 0 ] && ok "không tuyên bố hiện trạng nào khi chưa có src/ (canary sống, $seen dòng khớp pattern)"
  fi
fi

# ================================================================ §12
# Mọi file docs/ và spec/ phải khai đủ kind / scope / verified.
# Ba khoá biến ba câu hỏi phải-đọc-mới-biết thành ba câu máy đọc được.
section "§12 Mọi file docs/ + spec/ khai đủ kind / scope / verified"
SCAN_FM="docs"; [ -d spec ] && SCAN_FM="docs spec"
n=0; seen=0; n_chua=0; n_khongap=0
while IFS=$'\t' read -r f k s v st; do
  [ -z "$f" ] && continue
  seen=$((seen+1))
  miss=""
  [ -z "$k" ] && miss="$miss kind"
  [ -z "$s" ] && miss="$miss scope"
  [ -z "$v" ] && miss="$miss verified"
  if [ -n "$miss" ]; then
    bad "$f — thiếu khoá:$miss"
    n=$((n+1)); continue
  fi
  case "$k" in luat|tham-chieu|quyet-dinh|lich-su) ;; *) bad "$f — kind: '$k' không hợp lệ"; n=$((n+1)) ;; esac
  case "$s" in core|du-an) ;; *) bad "$f — scope: '$s' không hợp lệ"; n=$((n+1)) ;; esac
  case "$v" in
    chua-doi-chieu) n_chua=$((n_chua+1)) ;;
    khong-ap-dung)
      # 🛑 Không tự khai được khong-ap-dung. Lý do miễn trừ phải là sự thật máy
      # kiểm được, không phải một câu tự nhận. Đây là chỗ dễ lạm dụng nhất của
      # cả ba khoá: dán nhãn này lên file khó đối chiếu là cách nhanh nhất làm
      # con số đẹp lên mà không kiểm gì cả.
      case "$k" in
        tham-chieu|lich-su) n_khongap=$((n_khongap+1)) ;;
        *)
          case "$st" in
            *"not built"*|*"chưa xây"*) n_khongap=$((n_khongap+1)) ;;
            *) bad "$f — khai 'khong-ap-dung' nhưng kind là '$k' và không khai status 'not built'"; n=$((n+1)) ;;
          esac ;;
      esac ;;
    20[0-9][0-9]-[01][0-9]-[0-3][0-9]) ;;
    *) bad "$f — verified: '$v' không hợp lệ"; n=$((n+1)) ;;
  esac
done < <(find $SCAN_FM -name '*.md' -print0 2>/dev/null | xargs -0 awk '
  function flush() { if (cur != "") print cur "\t" k "\t" s "\t" v "\t" st }
  FNR==1 { flush(); cur=FILENAME; k=""; s=""; v=""; st=""; fm=($0=="---")?1:0; next }
  fm==1 && $0=="---" { fm=2; next }
  fm==1 && /^kind:/     { sub(/^kind:[ \t]*/,     ""); k=$0 }
  fm==1 && /^scope:/    { sub(/^scope:[ \t]*/,    ""); s=$0 }
  fm==1 && /^verified:/ { sub(/^verified:[ \t]*/, ""); v=$0 }
  fm==1 && /^status:/   { st=$0 }
  END { flush() }' 2>/dev/null)
if [ "$seen" -eq 0 ]; then
  bad "§12 không thấy file nào — mục này đang không kiểm gì"
elif [ "$n" -eq 0 ]; then
  ok "mọi file khai đủ ba khoá hợp lệ ($seen file)"
fi
printf '        chưa đối chiếu: %s · không áp dụng: %s\n' "$n_chua" "$n_khongap"

# ================================================================ §13
# Trích dẫn không được trỏ vào file mang kind: lich-su.
# Bịt lỗ hổng tìm ra khi audit dự án tiền nhiệm: một chú thích trong code trỏ
# đúng vào một tài liệu đã chết. Đường dẫn TỒN TẠI nên §4 xanh, mà nội dung thì
# không còn đúng — người đi theo đường dẫn đó nhận về thông tin sai.
section "§13 Trích dẫn không trỏ vào file kind: lich-su"
# So bang DUONG DAN, KHONG bang basename. Voi basename, dan nhan lich-su len
# mot file ten pho thong (repo co nhieu file README.md va CLAUDE.md) se lam MOI
# trich dan cung ten bi bao la "tro vao tai lieu da chet" — cong do hang loat vi
# mot thay doi hop le, va cach sua nhanh nhat ma nguoi ta chon la tat muc nay di.
DEAD=" $(grep -rl '^kind:[[:space:]]*lich-su' docs --include='*.md' 2>/dev/null | sort -u | tr '\n' ' ')"
# CANARY: pattern cua muc nay dò frontmatter. 0 file lich-su la trang thai HOP LE
# o giai doan 1, nen `bad` khi rong se bao oan — nhung in `ok` mau xanh thi khong
# phan biet duoc "chua co file lich-su" voi "toi khong do duoc file nao". Canary
# chung minh phep do con song; ket qua rong thi khai bao tuong minh bang NOTE.
if ! printf '%s\n' 'kind: lich-su' | grep -q '^kind:[[:space:]]*lich-su'; then
  bad "§13 canary hỏng — pattern 'kind: lich-su' không khớp được cả chuỗi dựng sẵn; mục này đang không kiểm gì"
elif [ "$DEAD" = " " ] || [ -z "${DEAD// /}" ]; then
  warn "chưa có file nào mang kind: lich-su — §13 KHÔNG kiểm gì (khai báo tường minh, không phải PASS)"
else
  n=0; seen=0
  while IFS= read -r hit; do
    [ -z "$hit" ] && continue
    f="${hit%%:*}"; rest="${hit#*:}"; ln="${rest%%:*}"; p="${rest#*:}"
    case "$HIST_FILES" in *" $f "*) continue ;; esac
    case "$HIST_LINES" in *" $f:$ln "*) continue ;; esac
    # Gỡ vỏ bọc TRƯỚC khi so tên. So chuỗi còn nguyên dấu backtick với tên file
    # trần thì không bao giờ khớp, và mục này xanh suốt mà không kiểm gì.
    p="${p#\`}"; p="${p%\`}"          # `file.md`
    p="${p#\](}"; p="${p%)}"          # ](file.md)
    # Bo tien to "../" roi so HAU TO duong dan: mot trich dan tuong doi
    # `../../database/x.md` khop dung `docs/database/x.md`, va KHONG khop
    # `docs/other/x.md`. Chat hon basename, khong can realpath.
    base="${p%%#*}"
    while [ "${base#../}" != "$base" ]; do base="${base#../}"; done
    base="${base#./}"
    # Ten TRAN (khong co dau "/") phai duoc giai theo thu muc cua file dang
    # xet. Neu khong, mot trich dan `README.md` bat ky se khop hau to voi MOI
    # file README.md trong repo — dung ca bao sai dien rong ma muc nay tranh.
    case "$f" in */*) _fd="${f%/*}" ;; *) _fd="." ;; esac
    case "$base" in */*) ;; *) base="$_fd/$base" ;; esac
    seen=$((seen+1))
    _hit=0
    for _d in $DEAD; do case "$_d" in *"$base") _hit=1; break ;; esac; done
    [ "$_hit" -eq 1 ] || continue
    bad "$f:$ln — trích dẫn trỏ vào tài liệu đã chết (kind: lich-su): $base"
    n=$((n+1))
  done < <(grep -rnoE '\]\([^)]*\.md\)|`[A-Za-z0-9._/-]+\.md`' $SCAN_DIRS --include='*.md' 2>/dev/null)
  if [ "$seen" -eq 0 ]; then
    bad "§13 KHÔNG trích được trích dẫn nào — mục này đang không kiểm gì"
  elif [ "$n" -eq 0 ]; then
    ok "không trích dẫn nào trỏ vào tài liệu đã chết ($seen trích dẫn)"
  fi
fi

# ================================================================ §14
# Mọi luật trong docs/RULES.md phải khai cột "ép bằng gì".
# Một luật trả lời "bằng niềm tin" thì không phải luật — nó là gợi ý, và phải
# nằm ở §10 Danh sách nợ, nhìn thấy được, chứ không trộn vào bảng luật.
section "§14 Mọi luật trong RULES.md khai cột ép bằng gì"
n=0; seen=0
while IFS= read -r line; do
  [ -z "$line" ] && continue
  case "$line" in *'| MUST'*|*'| SHOULD'*) ;; *) continue ;; esac
  seen=$((seen+1))
  IFS='|' read -r _ c_id _ _ c_enf _ <<< "$line"
  enf="${c_enf#"${c_enf%%[![:space:]]*}"}"; enf="${enf%"${enf##*[![:space:]]}"}"
  id="${c_id//[[:space:]]/}"
  case "$enf" in ''|'—'|'-'|'?')
    bad "RULES.md — luật $id không khai cột 'ép bằng gì' (phải chuyển sang §10 Danh sách nợ)"
    n=$((n+1)) ;;
  esac
done < <(grep -h '^|' docs/RULES.md 2>/dev/null)
if [ "$seen" -eq 0 ]; then
  bad "§14 không trích được luật nào từ RULES.md — mục này đang không kiểm gì"
elif [ "$n" -eq 0 ]; then
  ok "mọi luật đều khai cột ép bằng gì ($seen luật)"
fi

# ================================================================ §15
# Một nội dung — một nguồn đối chiếu duy nhất (luật D13).
#
# Đọc bảng chủ quyền ở docs/OWNERSHIP.md §3 và kiểm: chuỗi định danh của mỗi
# định nghĩa chỉ được xuất hiện ở ĐÚNG file chủ của nó.
#
# Khuôn hỏng nó chặn: hai ba file cùng `kind: luat` định nghĩa cùng một thứ và
# nói khác nhau. Cổng khớp chuỗi không so nội dung hai file với nhau; mục này
# thu hẹp khoảng đó lại cho những định nghĩa đã đăng ký.
#
# Sổ đăng ký LÀ đầu vào của cổng, nên nó không lệch khỏi thứ nó ép được.
section "§15 Một nội dung — một nguồn (bảng chủ quyền)"
OWN="docs/OWNERSHIP.md"
if [ ! -e "$OWN" ]; then
  bad "không thấy $OWN — không kiểm được §15"
else
  n=0; seen=0
  while IFS= read -r line; do
    case "$line" in '|'*'|'*'|'*) ;; *) continue ;; esac
    case "$line" in *'---'*|*'Chủ đề'*) continue ;; esac
    IFS='|' read -r _ c_topic c_owner c_sig _ <<< "$line"
    # gỡ khoảng trắng và backtick
    trim() { local v="$1"; v="${v#"${v%%[![:space:]]*}"}"; v="${v%"${v##*[![:space:]]}"}"; v="${v#\`}"; v="${v%\`}"; printf '%s' "$v"; }
    topic=$(trim "$c_topic"); owner=$(trim "$c_owner"); sig=$(trim "$c_sig")
    [ -z "$sig" ] || [ -z "$owner" ] && continue
    # Bo qua moi bang KHAC nam trong pham vi §3 (bang giai thich, bang doi
    # chieu). Dong so THAT luon co cot file chu bat dau bang `docs/` — do la
    # dau hieu may doc duoc; nhan bat ky dong `| a | b | c |` nao la bao sai.
    case "$owner" in docs/*) ;; *) continue ;; esac
    seen=$((seen+1))
    if [ ! -e "$owner" ]; then
      bad "OWNERSHIP: file chủ không tồn tại — $owner (chủ đề: $topic)"
      n=$((n+1)); continue
    fi
    # Đếm file chứa chuỗi, BỎ QUA chính sổ đăng ký (nó buộc phải chứa chuỗi đó).
    hits=$(grep -rlF "$sig" docs --include='*.md' 2>/dev/null | grep -v "^$OWN$" | sort -u)
    cnt=$(printf '%s' "$hits" | grep -c . || true)
    if [ "$cnt" -eq 0 ]; then
      bad "OWNERSHIP: chuỗi định danh KHÔNG xuất hiện ở đâu cả — '$sig' (chủ đề: $topic). Dòng này đang không canh gì"
      n=$((n+1))
    elif [ "$cnt" -gt 1 ]; then
      bad "OWNERSHIP: '$topic' được định nghĩa ở $cnt file, chủ quyền thuộc $owner —"
      while IFS= read -r h; do [ -n "$h" ] && printf '          %s\n' "$h"; done <<< "$hits"
      n=$((n+1))
    elif [ "$hits" != "$owner" ]; then
      bad "OWNERSHIP: '$topic' định nghĩa ở $hits nhưng sổ khai chủ là $owner"
      n=$((n+1))
    fi
  done < <(sed -n '/^## 3\./,/^## 4\./p' "$OWN")
  if [ "$seen" -eq 0 ]; then
    bad "§15 không trích được dòng nào từ bảng chủ quyền — mục này đang không kiểm gì"
  elif [ "$n" -eq 0 ]; then
    ok "mỗi định nghĩa đã đăng ký chỉ có một nguồn ($seen chủ đề)"
  fi
fi

# ================================================================ §16
# CHIEU NGUOC LAI CUA §15.
#
# §15 di tu SO DANG KY ra tai lieu: moi chuoi da dang ky chi duoc o mot file.
# No khong bat duoc ca nguoc lai — mot dinh nghia duoc danh dau la "dinh nghia
# goc" nhung KHONG CO DONG NAO trong so. Chuoi do khong ai canh, va ban sao dau
# tien cua no se khong lam do cong.
#
# Vi vay quy uoc danh moc tro thanh mot HOP DONG HAI CHIEU:
#   - Viet mot dinh nghia moi  -> gan moc "— dinh nghia goc" vao tieu de
#   - Gan moc                  -> BAT BUOC co mot dong trong OWNERSHIP.md §3
# Cong lo phan con lai. So dang ky vi the TU DUY TRI, khong phu thuoc vao viec
# ai do nho them dong.
section "§16 Mốc 'định nghĩa gốc' phải có dòng trong sổ chủ quyền"
if [ ! -e "$OWN" ]; then
  bad "không thấy $OWN — không kiểm được §16"
else
  # Tap chuoi da dang ky, doc tu chinh §3 cua so.
  # Tap chuoi da dang ky, doc tu chinh §3 cua so.
  #
  # BO LOC BAT BUOC — giong het §15: chi nhan dong co cot FILE CHU bat dau bang
  # `docs/`. Thieu no, mot bang van xuoi trong pham vi §3 bien mot tu thuong
  # thanh "chuoi dinh danh"; vi phep so la khop CHUOI CON, moi moc chua tu do
  # deu di lot §16 — tuc D22 bi vo hieu bang dung mot tu.
  #
  # Lop thu hai: sig ngan hon 12 ky tu bi loai. Mot dinh danh that luon dai;
  # mot tu don lot vao day luon la rac.
  SIGS=$(sed -n '/^## 3\./,/^## 4\./p' "$OWN" | awk -F'|' 'NF>4 {
      o=$3; g=$4;
      gsub(/^[ 	]+|[ 	]+$/,"",o); gsub(/`/,"",o);
      gsub(/^[ 	]+|[ 	]+$/,"",g); gsub(/^`|`$/,"",g);
      if (o ~ /^docs\//) print g
    }' | awk 'length($0) >= 12')
  n=0; seen=0
  while IFS= read -r hit; do
    [ -z "$hit" ] && continue
    f="${hit%%:*}"; rest="${hit#*:}"; ln="${rest%%:*}"; body="${rest#*:}"
    case "$f" in "$OWN") continue ;; esac
    case "$HIST_FILES" in *" $f "*) continue ;; esac
    case "$HIST_LINES" in *" $f:$ln "*) continue ;; esac
    # Nhac toi moc ≠ gan moc. Mot dong luat viet "moc `— dinh nghia goc`" trong
    # code span la dang NOI VE quy uoc, khong phai dang khai mot dinh nghia.
    # Cung phep phan biet ma §6 dung cho nhan trang thai: go moi doan trong code
    # span ra truoc, con sot moc nghia la moc that.
    stripped=""; tmp="$body"
    while [ "${tmp#*\`}" != "$tmp" ]; do
      stripped="$stripped${tmp%%\`*}"; tmp="${tmp#*\`}"
      [ "${tmp#*\`}" = "$tmp" ] && { tmp=""; break; }
      tmp="${tmp#*\`}"
    done
    stripped="$stripped$tmp"
    case "$stripped" in *'— định nghĩa gốc'*) ;; *) continue ;; esac
    seen=$((seen+1))
    hit_ok=0
    while IFS= read -r sig; do
      [ -z "$sig" ] && continue
      case "$body" in *"$sig"*) hit_ok=1; break ;; esac
    done <<< "$SIGS"
    [ "$hit_ok" -eq 1 ] && continue
    bad "$f:$ln — mốc 'định nghĩa gốc' KHÔNG có dòng nào trong OWNERSHIP.md §3; không cổng nào canh nó"
    n=$((n+1))
  done < <(grep -rn -- '— định nghĩa gốc' docs --include='*.md' 2>/dev/null)
  if [ "$seen" -eq 0 ]; then
    bad "§16 KHÔNG trích được mốc nào — mục này đang không kiểm gì (quy ước đánh mốc đã chết, hoặc pattern hỏng)"
  elif [ "$n" -eq 0 ]; then
    ok "mọi mốc 'định nghĩa gốc' đều đã đăng ký ($seen lần xuất hiện)"
  fi
fi

# ================================================================ §17
# Luat D24. Chi do cum GAN NHU KHONG BAO GIO hop le trong file noi dung; mo rong
# tap cum la bat dau chan nham cau hop le ("chay ban cu", "da tung thu hoi").
# Cach dien dat khac lot qua - lop chan goc la CLAUDE.md §3 va review.
section "§17 File nội dung không kể lại bản trước của chính nó"
HISTPAT='Bản trước|LẬT 20[0-9]{2}-|SỬA 20[0-9]{2}-|Gỡ 20[0-9]{2}-'
scanned=0; n=0
while IFS= read -r f; do
  case "$f" in docs/audit/*|docs/adr/*) continue ;; esac
  case "$HIST_FILES" in *" $f "*) continue ;; esac
  scanned=$((scanned+1))
  while IFS= read -r hit; do
    [ -z "$hit" ] && continue
    bad "$f:${hit%%:*} — kể lại bản trước. Bản cũ nằm trong lịch sử git; bài học vào docs/audit/"
    n=$((n+1))
  done < <(grep -nE "$HISTPAT" "$f" 2>/dev/null)
done < <(find docs .claude spec -name '*.md' 2>/dev/null | sort)
if [ "$scanned" -eq 0 ]; then
  bad "§17 không quét được file nào — mục này đang không kiểm gì"
elif [ "$n" -eq 0 ]; then
  ok "không file nội dung nào kể lại bản trước ($scanned file)"
fi

# ================================================================ §18
# Luat D28 — lat cat may kiem duoc cua D27 (ADR-0019 rang buoc 3).
#
# D27 day du la "xoa phan nghiep vu ma spec van con nghia" — doc hieu, khong
# quet tinh duoc. Muc nay KHONG co tham vong do ca D27. No do dung mot lat cat
# co hinh dang: tu vung nganh nam o VI TRI DINH DANH — trong code span hoac
# trong code block. Do la cho tu do khong con xoa duoc: no la ten bien the, ten
# prop, ten truong trong chu ky kieu. Xoa di thi spec doi nghia.
#
# Van xuoi va o bang thi KHONG bi xet, ke ca trong muc Bien the/Trang thai/API:
# "loc chung tu theo khoang" la mot VI DU, va vi du la thu lam spec de hieu.
# Xem docs/audit/2026-09-12-cong-neo-vao-muc-thay-vi-vai-tro-cua-chuoi.md.
#
# Phan con lai cua D27 van thuoc §10 RULES.md: `core-reviewer` va nguoi doc bat.
section "§18 Từ vựng nghiệp vụ không nằm ở vị trí định danh trong Components/"
BIZPAT='chứng từ|dự toán|kỳ kế toán|hạch toán|kho[áó] sổ|bảng lương|định khoản'
BIZPAT="$BIZPAT"'|chung[ _]?[Tt]u|du[ _]?[Tt]oan|ky[ _]?[Kk]e[ _]?[Tt]oan'
BIZPAT="$BIZPAT"'|hach[ _]?[Tt]oan|khoa[ _]?[Ss]o|bang[ _]?[Ll]uong|dinh[ _]?[Kk]hoan'
# CANARY. Dieu kien PASS cua muc nay la thuan phu dinh va pattern chua ky tu co
# dau — dung ca ma §0 ghi lai. Pattern hong thi canary hong truoc.
if ! printf '%s\n' 'kyKeToan hạch toán' | grep -qE "$BIZPAT"; then
  bad "§18 canary hỏng — pattern không khớp được cả chuỗi dựng sẵn; mục này đang không kiểm gì"
else
  n=0; seg=0
  while IFS= read -r line; do
    case "$line" in
      SEG:*) seg="${line#SEG:}" ;;
      ?*)    bad "$line"; n=$((n+1)) ;;
    esac
  done < <(awk -v pat="$BIZPAT" '
    function look(txt, where) {
      if (txt ~ pat) {
        printf "%s:%d — từ vựng nghiệp vụ ở %s. Đây là chỗ nó ăn vào mô hình: tên biến thể, tên prop, tên trường. Component của Core không được biết nghiệp vụ (RULES.md D28, ADR-0019)\n", FILENAME, FNR, where
      }
    }
    FNR == 1 { fence = 0 }
    /^[ \t]*```/ { fence = !fence; next }
    {
      if (fence) { seg++; look($0, "trong code block") }
      else {
        k = split($0, part, "`")
        for (i = 2; i <= k; i += 2) { seg++; look(part[i], "trong code span") }
      }
    }
    END { printf "SEG:%d\n", seg }
  ' docs/Design/Components/*.md 2>/dev/null)
  case "$seg" in
    ''|*[!0-9]*) bad "§18 không đếm được đoạn định danh nào — phép đếm hỏng, PASS lúc này vô nghĩa" ;;
    0)           bad "§18 xét 0 đoạn định danh trong docs/Design/Components/ — mục này đang không kiểm gì" ;;
    *)           [ "$n" -eq 0 ] && ok "không từ vựng nghiệp vụ nào ở vị trí định danh (canary sống, $seg đoạn đã xét)" ;;
  esac
fi

# ================================================================ §19
# Luat D29. Mot kieu cong khai chi duoc khai o MOT cho trong toan docs/.
#
# CO Y BO `export class`: moi `export class` trong docs/ deu la VI DU minh hoa
# (component, service, page Angular). `DanhSachNguoiDungPage` xuat hien o hai
# file quy uoc la mot vi du chay xuyen suot — do la uu diem, khong phai loi.
# Bat ca do se ep nguoi viet doi ten vi du de lam vui long may do; xem bai hoc
# o docs/audit/2026-09-12-cong-neo-vao-muc-thay-vi-vai-tro-cua-chuoi.md.
#
# Be mat can canh la HOP DONG: interface / type / enum / const.
section "§19 Mỗi kiểu công khai chỉ được khai một lần trong toàn docs/"
n=0; seen=0
while IFS= read -r line; do
  case "$line" in
    SEEN:*) seen="${line#SEEN:}" ;;
    ?*)     bad "$line"; n=$((n+1)) ;;
  esac
done < <(grep -rnE '^[[:space:]]*export (interface|type|const|enum) [A-Za-z_][A-Za-z0-9_]*' docs --include='*.md' 2>/dev/null | awk -F: '
  {
    if (match($0, /export (interface|type|const|enum) [A-Za-z_][A-Za-z0-9_]*/)) {
      nm = substr($0, RSTART, RLENGTH)
      sub(/^export (interface|type|const|enum) /, "", nm)
      loc = $1 ":" $2
      where[nm] = (nm in cnt) ? where[nm] ", " loc : loc
      cnt[nm]++
      total++
    }
  }
  END {
    for (k in cnt)
      if (cnt[k] > 1)
        printf "%s khai %d lần: %s — một kiểu công khai chỉ được có MỘT chỗ khai; chỗ kia rút còn dòng trỏ đường (RULES.md D29)\n", k, cnt[k], where[k]
    printf "SEEN:%d\n", total
  }
')
case "$seen" in
  ''|*[!0-9]*) bad "§19 không đếm được khai báo nào — phép đếm hỏng, PASS lúc này vô nghĩa" ;;
  0)           bad "§19 xét 0 khai báo export trong docs/ — mục này đang không kiểm gì" ;;
  *)           [ "$n" -eq 0 ] && ok "mỗi kiểu công khai chỉ khai một lần ($seen khai báo đã xét)" ;;
esac

# ================================================================ §20
# Luat D30. GIA TRI cua mot token chi song o docs/Design/DESIGN.md.
#
# Cam GAN GIA TRI LITERAL, khong cam dong `--x:` noi chung: `--brand: var(--y)`
# la bi danh, khong mang gia tri; `--color-bg: <gia tri o DESIGN.md>` la khung
# minh hoa. Ca hai deu hop le. Thu phai co mot nguon la CON SO.
#
# Mien tru docs/adr/: mot ADR ghi lai quyet dinh TAI THOI DIEM do, ke ca gia tri.
section "§20 Giá trị token chỉ được khai ở Design/DESIGN.md"
TOKPAT='^[[:space:]]*--[a-z0-9-]+:[[:space:]]*(#[0-9a-fA-F]{3,8}|rgba?\([0-9]|[0-9])'
# CANARY kep. Dieu kien PASS thuan phu dinh, nen mot pattern hong hoac mot pham
# vi quet hong deu cho ra "OK" gia.
if ! printf '  --color-bg: #f4f6fa;\n' | grep -qE "$TOKPAT"; then
  bad "§20 canary pattern hỏng — không khớp được dòng gán dựng sẵn; mục này đang không kiểm gì"
elif printf '  --brand: var(--blue-600);\n' | grep -qE "$TOKPAT"; then
  bad "§20 canary pattern quá rộng — nó bắt cả bí danh var(); sẽ báo sai"
elif ! grep -rlq -- '--color-bg' docs --include='*.md' 2>/dev/null; then
  bad "§20 canary phạm vi hỏng — không với tới docs/; mục này đang không kiểm gì"
else
  n=0
  while IFS= read -r hit; do
    [ -z "$hit" ] && continue
    f="${hit%%:*}"
    case "$f" in docs/Design/DESIGN.md|docs/adr/*) continue ;; esac
    bad "$hit — gán giá trị cho token ngoài file chủ. Giá trị chỉ sống ở docs/Design/DESIGN.md; chỗ khác dùng bí danh var() hoặc khung minh hoạ (RULES.md D30)"
    n=$((n+1))
  done < <(grep -rnE "$TOKPAT" docs --include='*.md' 2>/dev/null)
  [ "$n" -eq 0 ] && ok "không giá trị token nào nằm ngoài file chủ (canary kép sống)"
fi

# ================================================================ §21
# Luat D32. Mot huong da khoa (`❌ loai, khong hoan`) phai mang ma `K##`.
#
# VI SAO: 49 huong dang bi khoa — Redis, message broker, NgRx, SSR, Nx, man
# chon tenant... Chung la thu bi lat AM THAM. Da xay ra that: them `Chart` la
# lat bon cau nhu vay ma khong ai nhan ra, phai viet ADR-0019 sau khi su da roi.
#
# Ma cap TUAN TU, KHONG tai su dung, KHONG danh so lai. Nho vay viec mot huong
# bien mat tro thanh mot LO TRONG trong day so — thu may dem duoc.
#
# HINH DANG: chi hang bang co ❌ o o THU HAI tro di moi la mot huong khoa.
# ❌ o o DAU la dong DINH NGHIA ky hieu (wiki-core/README §9); ❌ trong van xuoi
# la cau NHAC ky hieu. Ca hai deu khong duoc cap ma — 18 ca nhu vay da doc tay
# tung cai truoc khi chot phep do nay.
section "§21 Hướng đã khoá phải mang mã, và không biến mất trong im lặng"
n=0; decl=" "; ndecl=0
while IFS= read -r hit; do
  [ -z "$hit" ] && continue
  case "$hit" in
    *'`K'*)
      c="${hit##*\`K}"; c="K${c%%\`*}"
      case "$decl" in
        *" $c "*) bad "mã $c khai ở hai chỗ — mã hướng khoá không được tái sử dụng: $hit"; n=$((n+1)) ;;
        *) decl="$decl$c "; ndecl=$((ndecl+1)) ;;
      esac ;;
    *) bad "$hit — hướng khoá thiếu mã ${BT}K##${BT}. Cấp mã kế tiếp sau mã lớn nhất; không dùng lại mã cũ (RULES.md D32)"; n=$((n+1)) ;;
  esac
done < <(grep -rnE '^\|.+\| *❌ \*{0,2}loại, không hoãn' docs --include='*.md' 2>/dev/null)

if [ "$ndecl" -eq 0 ]; then
  bad "§21 xét 0 hướng khoá — mục này đang không kiểm gì"
else
  # mã lớn nhất đang khai
  max=0
  for c in $decl; do
    v="${c#K}"; case "$v" in 0*) v="${v#0}" ;; esac
    [ "$v" -gt "$max" ] && max="$v"
  done
  # mã được ADR trích dẫn = đã lật CÓ ghi chép, hợp lệ khi vắng mặt
  # Mien tru CHI khi mot ADR noi thang la LAT ma do — dang: lat `K##`.
  # Trich dan CUNG CO (ADR nhac lai mot huong VAN CON hieu luc) khong duoc
  # mien tru; neu khong, moi ma tung duoc nhac o bat ky ADR nao deu co the
  # bien mat trong im lang va cong tu rong di.
  cited=" $(grep -rhoE 'lật `K[0-9]{2}`' docs/adr --include='*.md' 2>/dev/null | grep -oE 'K[0-9]{2}' | sort -u | tr '
' ' ')"
  i=1
  while [ "$i" -le "$max" ]; do
    printf -v c 'K%02d' "$i"
    case "$decl" in
      *" $c "*) : ;;
      *) case "$cited" in
           *" $c "*) : ;;
           *) bad "$c không còn dòng khai nào, và không ADR nào nói là LẬT nó — một hướng đã khoá vừa biến mất trong im lặng. Lật thì viết ADR có cụm \"lật\" kèm mã, đừng xoá dòng"; n=$((n+1)) ;;
         esac ;;
    esac
    i=$((i+1))
  done
  [ "$n" -eq 0 ] && ok "mọi hướng khoá đều có mã, không trùng, không lỗ trống ($ndecl mã, cao nhất K$(printf '%02d' "$max"))"
fi

# ================================================================ §22
# Luat D33. Khu luong/ co ba rang buoc may kiem duoc.
#
# 1) Muc luc khong lech khoi thu muc — cung khuon COMPONENTS.md, va lan nay
#    duoc cong ep thay vi de trong van xuoi (bai hoc 2026-09-11).
# 2) Moi file luong co du SAU muc bat buoc.
# 3) Moi file luong khai muc 5 "Quan he voi don vi".
#
# Rang buoc 3 la ly do khu nay ra doi: RULES.md §9 xep ro du lieu giua hai don
# vi la rui ro nghiem trong nhat toan he, va no hong IM LANG. Bat moi luong tu
# tra loi cau do bien mot khoang im lang thanh mot cau kiem duoc.
#
# Phep dem neo vao HANG BANG co lien ket, nen khoi lenh trong README (bat dau
# bang `ls`/`grep`) khong tu dem chinh no.
section "§22 Khu luồng — mục lục khớp thư mục, và mỗi luồng khai quan hệ với đơn vị"
if [ ! -d docs/luong ]; then
  warn "chưa có docs/luong/ — bỏ qua §22"
else
  set -- docs/luong/[A-Z][0-9]*.md
  if [ "$#" -eq 0 ] || [ ! -e "$1" ]; then
    bad "§22 không thấy file luồng nào trong docs/luong/ — mục này đang không kiểm gì"
  else
    n_file=$#
    n_row=$(grep -cE '^\| `[A-Z][0-9]+` \| \[' docs/luong/README.md 2>/dev/null)
    n=0
    if [ "$n_file" -ne "$n_row" ]; then
      bad "mục lục khu luồng lệch khỏi thư mục: $n_file file, $n_row dòng có liên kết. Thêm file thì thêm dòng (RULES.md D33)"
      n=$((n+1))
    fi
    for f in "$@"; do
      s6=$(grep -cE '^## [1-6]\. ' "$f")
      [ "$s6" -eq 6 ] || { bad "$f — có $s6/6 mục bắt buộc. Khuôn sáu mục ở docs/luong/README.md §3"; n=$((n+1)); }
      grep -q '^## 5\. Quan hệ với đơn vị' "$f" || { bad "$f — thiếu mục 'Quan hệ với đơn vị'. Trả lời một trong ba: thuộc đơn vị / dùng chung toàn hệ / không áp dụng kèm lý do"; n=$((n+1)); }
    done
    [ "$n" -eq 0 ] && ok "mục lục khớp thư mục ($n_file luồng), mỗi luồng đủ sáu mục và khai quan hệ với đơn vị"
  fi
fi

# ================================================================ kết luận
printf '\n'
if [ "$FAIL" -eq 0 ]; then
  printf '\033[32m✅ PASS — tài liệu đồng bộ.\033[0m\n'
  printf '\033[33m   Nhắc: PASS không có nghĩa là nội dung ĐÚNG. Cổng chỉ bắt được thứ máy\n'
  printf '   kiểm được. Ba loại lỗi nó không bao giờ bắt: văn xuôi tả thứ không tồn\n'
  printf '   tại, sơ đồ chép sai, ngày đúng nhưng nội dung sai. Xem CLAUDE.md §8.\033[0m\n'
  exit 0
else
  printf '\033[31m❌ FAIL — có vi phạm ở trên.\033[0m\n'
  exit 1
fi
