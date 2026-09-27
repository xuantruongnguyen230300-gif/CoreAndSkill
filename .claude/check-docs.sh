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
# Ba bẫy nữa, cùng đã đo trên máy thật (máy tải nặng: một spawn tới ~100ms):
#   - `$(...)` và `x=$(f ...)` cũng là spawn — trong vòng lặp thì dùng `printf -v`,
#     `$(< file)` (bash 5.2 không fork), mảng kết hợp;
#   - `while read ... < <(cmd)` đọc pipe TỪNG BYTE: hơn chục nghìn dòng tốn vài
#     giây chỉ để đọc. Dòng nhiều thì nạp bằng `_lines` (tách chuỗi, vài ms);
#   - `case "$chuỗi_dài" in *"$x"*` trong vòng lặp lớn quét lại cả chuỗi mỗi vòng
#     — tra bằng mảng kết hợp, hoặc đưa cả vòng vào một lượt awk.
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

# `[ -e ]` có nhớ. Cùng một đường dẫn được trích hàng trăm lần (§3, §4, §5), và
# mỗi lần stat trên Windows tốn đáng kể. Cây không đổi trong một lượt chạy, nên
# kết quả lần đầu là kết quả của mọi lần sau.
declare -A _EXISTS=()
exists() {
  [ -z "$1" ] && { [ -e "$1" ]; return; }
  if [ -z "${_EXISTS["$1"]+x}" ]; then
    if [ -e "$1" ]; then _EXISTS["$1"]=0; else _EXISTS["$1"]=1; fi
  fi
  return "${_EXISTS["$1"]}"
}

# Tách "$1" theo '|' đúng như `IFS='|' read -r v1 v2 ... <<< "$1"` gán cho các
# biến KHÔNG phải biến cuối: _PF[i] = trường thứ i, rỗng nếu dòng thiếu trường.
# Lấy "$2" trường đầu. Builtin thuần — here-string trong vòng lặp tốn một pipe
# mỗi vòng.
_pipe_fields() {
  local r="$1" i=1
  _PF=()
  while [ "$i" -le "$2" ]; do
    case "$r" in
      *'|'*) _PF[i]="${r%%|*}"; r="${r#*|}" ;;
      *)     _PF[i]="$r"; r="" ;;
    esac
    i=$((i+1))
  done
}

# Nạp đầu ra nhiều dòng vào MẢNG tên "$1": mọi dòng KHÁC RỖNG của "$2", đúng thứ
# tự — tức đúng những dòng mà vòng `while IFS= read -r x; do [ -z "$x" ] &&
# continue` xử lý. Tách bằng word splitting (IFS = newline, TẮT glob, `local -`
# trả lại cờ shell khi ra khỏi hàm). Trên Git Bash `read` đọc pipe TỪNG BYTE; với
# hơn chục nghìn dòng, chỉ riêng việc đọc đã tốn nhiều giây, tách chuỗi thì vài ms.
_lines() {
  local -n _dst="$1"
  local IFS=$'\n' -
  set -f
  # shellcheck disable=SC2206  # cố ý: tách theo newline, glob đã tắt
  _dst=($2)
}

# Gỡ khoảng trắng hai đầu rồi MỘT backtick mỗi đầu, ghi vào biến tên "$1".
# Cùng phép gỡ với `trim()` cũ, nhưng `printf -v` thay cho `$(...)`: không fork.
_trim_to() {
  local v="$2"
  v="${v#"${v%%[![:space:]]*}"}"; v="${v%"${v##*[![:space:]]}"}"; v="${v#\`}"; v="${v%\`}"
  printf -v "$1" '%s' "$v"
}

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
#
# Tra bằng MẢNG KẾT HỢP `HISTL`, không bằng `case "$chuỗi" in *" $f:$ln "*`:
# chuỗi nối mọi dòng miễn trừ dài hàng chục KB, và so khớp glob trên nó ở mỗi
# vòng lặp (§13 lặp hơn chục nghìn lần) từng ngốn nhiều giây nhất của cả cổng.
#
# Khoá sinh ra đúng TẬP needle mà phép so chuỗi cũ nhận: với mục "p:n", needle
# " f:n " khớp khi f = p, HOẶC f là phần đuôi của p nằm sau một dấu cách trong p
# (tên file có dấu cách). Vòng `while` dưới sinh đủ các đuôi đó. Mọi chỗ tra đều
# lấy f là phần trước dấu ':' đầu tiên, n là số dòng — nên không khớp chéo mục.
_hl=$(grep -rn 'trước ở\|trước nằm ở\|đã xoá\|đã chuyển\|đã bỏ\|không còn tồn tại\|chưa từng tồn tại\|loại khỏi repo\|check-ignore\|dự án tiền nhiệm\|đã thay thế\|sẽ được điền\|giai đoạn 2' $SCAN_DIRS --include='*.md' 2>/dev/null | cut -d: -f1,2)
declare -A HISTL=()
_hla=(); _lines _hla "$_hl"
for _e in "${_hla[@]}"; do
  _n="${_e#*:}"; _s="${_e%%:*}"
  HISTL["$_s:$_n"]=1
  while [ "${_s#* }" != "$_s" ]; do _s="${_s#* }"; HISTL["$_s:$_n"]=1; done
done
# Dạng CHUỖI, y hệt bản cũ (" a:1 b:2 … "), cho các mục làm việc trong awk: awk
# tìm chuỗi con bằng `index()` nhanh, nên ở đó giữ nguyên phép so gốc.
HIST_LINES=" "
[ -n "$_hl" ] && HIST_LINES=" ${_hl//$'\n'/ } "
unset _hl _hla _e _n _s

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
  SETTXT=$(tr -d '\n' < .claude/settings.json)
  # Chỉ lấy MẢNG deny, không lấy cả file: `allow` cũng có mục dạng
  # "Bash(git ...:*)", nên so trên cả file thì một lệnh CẤM mà chỉ nằm ở allow
  # vẫn qua cổng. Chuỗi trong deny không chứa "]", nên cắt ở "]" đầu tiên là đủ.
  DENY="${SETTXT#*\"deny\"}"
  if [ "$DENY" = "$SETTXT" ]; then DENY=""; else DENY="${DENY%%]*}"; fi
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

  # BA DẠNG cho mỗi mục cấm. `Bash(x:*)` so tiền tố của lệnh chạy qua công cụ
  # Bash — công cụ PowerShell và tiền tố `rtk` không khớp nó. Đã thử thật: một
  # lệnh cấm lọt qua cả hai đường đó. Thiếu một dạng là lệnh lọt đúng đường ấy,
  # và cổng cũ vẫn xanh vì chỉ đối chiếu dạng Bash.
  lay3=0
  rest="$DENY"
  while [ "${rest#*\"Bash(}" != "$rest" ]; do
    rest="${rest#*\"Bash(}"
    item="${rest%%\"*}"
    case "$item" in *':*)') ;; *) continue ;; esac
    x="${item%:\*)}"
    case "$x" in 'rtk '*) continue ;; esac
    lay3=$((lay3+1))
    case "$DENY" in *"\"Bash(rtk $x:*)\""*) ;; *) bad "settings.json deny có 'Bash($x:*)' nhưng thiếu 'Bash(rtk $x:*)'"; n=$((n+1)) ;; esac
    case "$DENY" in *"\"PowerShell($x:*)\""*) ;; *) bad "settings.json deny có 'Bash($x:*)' nhưng thiếu 'PowerShell($x:*)'"; n=$((n+1)) ;; esac
  done

  if [ "$lay" -eq 0 ]; then
    bad "§1 KHÔNG trích được lệnh git nào từ bảng cấm — mục này đang không kiểm gì"
  elif [ "$lay2" -eq 0 ]; then
    bad "§1 KHÔNG trích được lệnh non-git nào từ CLAUDE.md §1 — nửa sau của mục này đang không kiểm gì"
  elif [ "$lay3" -eq 0 ]; then
    bad "§1 KHÔNG trích được mục Bash(...:*) nào từ permissions.deny — phần kiểm ba dạng đang không kiểm gì"
  elif [ "$n" -eq 0 ]; then
    ok "mọi lệnh trong bảng cấm đều được deny chặn ($lay lệnh git, $lay2 lệnh khác); mỗi mục deny đủ ba dạng Bash / rtk / PowerShell ($lay3 mục)"
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
# HIỆU NĂNG: hàng nghìn link nhưng chỉ vài phần số đó là ĐÍCH KHÁC NHAU. awk tách
# link (cùng phép cắt như bản bash cũ, LC_ALL=C vì mọi mốc cắt đều là ký tự
# ASCII) và in mỗi đích một lần; bash chỉ `[ -e ]` các đích đó. Chỉ khi có đích
# chết mới chạy lượt awk thứ hai để in lỗi theo đúng thứ tự link.
#   mode=U: in "P<tab>đích" cho mỗi đích mới + "S<tab>số-link-đã-xét" ở cuối.
#   mode=B: stdin mở đầu bằng số đích chết + từng đích; in "file<tab>dòng<tab>link"
#           cho mỗi link trỏ vào đích chết.
_awk3='
  function parse(   c, rest, raw) {
    # f="${hit%%:*}"; rest="${hit#*:}"; ln="${rest%%:*}"; raw="${rest#*:}"
    c = index($0, ":");  if (c) { f = substr($0, 1, c - 1); rest = substr($0, c + 1) } else { f = $0; rest = $0 }
    c = index(rest, ":"); if (c) { ln = substr(rest, 1, c - 1); raw = substr(rest, c + 1) } else { ln = rest; raw = rest }
    # tgt="${raw#\](}"; tgt="${tgt%)}"
    tgt = raw
    if (substr(tgt, 1, 2) == "](") tgt = substr(tgt, 3)
    if (tgt != "" && substr(tgt, length(tgt)) == ")") tgt = substr(tgt, 1, length(tgt) - 1)
    # case "$tgt" in http*|mailto:*|#*|"") continue
    if (substr(tgt, 1, 4) == "http" || substr(tgt, 1, 7) == "mailto:" || substr(tgt, 1, 1) == "#" || tgt == "") return 0
    c = index(tgt, "#"); if (c) tgt = substr(tgt, 1, c - 1)
    if (tgt == "") return 0
    # File o GOC repo khong co dau "/" trong ten, nen "${f%/*}" tra ve nguyen ten
    # file chu khong phai thu muc chua no - va moi link tuong doi trong README.md
    # goc bi bao chet oan. Bay nay chi lo ra khi README.md duoc dua vao tam quet.
    if (index(f, "/")) { d = f; sub(/\/[^\/]*$/, "", d) } else d = "."
    path = d "/" tgt
    return 1
  }
  mode == "B" && NR == 1      { nm = $0 + 0; next }
  mode == "B" && NR <= 1 + nm { M[$0] = 1; next }
  $0 == ""  { next }
  !parse()  { next }
  mode == "U" { seen++; if (!(path in U)) { U[path] = 1; print "P\t" path }; next }
  mode == "B" && (path in M) { print f "\t" ln "\t" tgt }
  END { if (mode == "U") print "S\t" seen + 0 }
'
_g3=$(grep -rnoE '\]\([^)]+\)' $SCAN_DIRS --include='*.md' 2>/dev/null)
_miss3=()
_L=(); _lines _L "$(printf '%s\n' "$_g3" | LC_ALL=C awk -v mode=U "$_awk3")"
for _l in "${_L[@]}"; do
  case "$_l" in
    S$'\t'*) seen="${_l#S$'\t'}" ;;
    P$'\t'*) _p="${_l#P$'\t'}"; exists "$_p" || _miss3+=("$_p") ;;
  esac
done
# awk không chạy / không in dòng S -> seen không phải số -> coi như 0 -> FAIL bên dưới.
case "$seen" in ''|*[!0-9]*) seen=0 ;; esac
if [ "${#_miss3[@]}" -gt 0 ]; then
  _L=(); _lines _L "$( { printf '%s\n' "${#_miss3[@]}"; printf '%s\n' "${_miss3[@]}"; printf '%s\n' "$_g3"; } | LC_ALL=C awk -v mode=B "$_awk3")"
  for _l in "${_L[@]}"; do
    f="${_l%%$'\t'*}"; _r="${_l#*$'\t'}"; ln="${_r%%$'\t'*}"; tgt="${_r#*$'\t'}"
    bad "$f:$ln — link chết: $tgt"
    n=$((n+1))
  done
fi
unset _g3
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
_PATHS=(); _lines _PATHS "$PATHS"

section "§4  Đường dẫn được trích dẫn phải tồn tại"
n=0; seen=0
for hit in "${_PATHS[@]}"; do
  [ -z "$hit" ] && continue
  f="${hit%%:*}"; rest="${hit#*:}"; ln="${rest%%:*}"; p="${rest#*:}"
  case "$HIST_FILES" in *" $f "*) continue ;; esac
  [ -n "${HISTL["$f:$ln"]+x}" ] && continue
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
  exists "$p" && continue
  bad "$f:$ln — đường dẫn không tồn tại: $p"
  n=$((n+1))
done
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
  # HIỆU NĂNG: awk đếm ứng viên, tách đường dẫn (cùng phép cắt như bản bash cũ;
  # LC_ALL=C vì mọi mốc cắt là ký tự ASCII và "…" so theo byte UTF-8 nguyên văn)
  # rồi in mỗi đường dẫn MỘT lần, theo thứ tự gặp đầu. Bash chỉ `[ -e ]` các
  # đường dẫn đó. Bỏ bản trùng không đổi kết quả — ứng viên đi qua `sort -u`
  # ngay sau. Ứng viên gom vào MẢNG: nối chuỗi `CAND="$CAND$p"` qua hàng nghìn
  # vòng là chép lại cả chuỗi mỗi vòng.
  CAND=(); seen=0
  _L=(); _lines _L "$(grep -rnoP "$BT\\K[A-Za-z0-9_.][A-Za-z0-9_./-]*/[^$BT]*(?=$BT)" $SCAN_DIRS --include='*.md' 2>/dev/null | LC_ALL=C awk '
    $0 == "" { next }
    {
      seen++
      # rest="${hit#*:}"; p="${rest#*:}"
      c = index($0, ":");  rest = c ? substr($0, c + 1) : $0
      c = index(rest, ":"); p = c ? substr(rest, c + 1) : rest
      # case "$p" in *"*"*|*"<"*|*"{"*|*" "*|*"…"*) continue
      if (index(p, "*") || index(p, "<") || index(p, "{") || index(p, " ") || index(p, "…")) next
      # case "$p" in *:[0-9]*) p="${p%:*}"
      if (p ~ /:[0-9]/) { match(p, /:[^:]*$/); p = substr(p, 1, RSTART - 1) }
      if (!(p in U)) { U[p] = 1; print "P\t" p }
    }
    END { print "S\t" seen + 0 }')"
  for _l in "${_L[@]}"; do
    case "$_l" in
      S$'\t'*) seen="${_l#S$'\t'}" ;;
      P$'\t'*) p="${_l#P$'\t'}"
        # Đường dẫn ra ngoài repo (`../…`, tuyệt đối) không thể bị .gitignore của repo này loại
        # trừ, và chỉ MỘT cái cũng làm `git check-ignore --stdin` chết cả lô (`outside repository`).
        case "$p" in ..|../*|/*) continue ;; esac
        exists "$p" && CAND+=("$p") ;;
    esac
  done
  # awk không chạy / không in dòng S -> seen không phải số -> coi như 0 -> FAIL bên dưới.
  case "$seen" in ''|*[!0-9]*) seen=0 ;; esac
  # §5 quét RỘNG HƠN §4, cố ý.
  #
  # §4 chỉ nhận tiền tố docs/ .claude/ spec/ — khu BẮT BUỘC tồn tại hôm nay.
  # Khu giai đoạn 2 (src/, scripts/, database/) chưa dựng, nên bắt "không tồn
  # tại" ở đó là báo sai.
  #
  # §5 thì khác: nó chỉ báo khi file THẬT SỰ TỒN TẠI và bị gitignore loại trừ
  # — không tồn tại thì bỏ qua ngay ở dòng `exists` (tức `[ -e ]`) trên. Nên mở
  # rộng phạm vi ở đây không sinh phát hiện sai nào, mà giữ được đúng lớp lỗi §5
  # tồn tại để bắt: bằng chứng mà người thứ hai clone về không bao giờ mở được.
  if [ "${#CAND[@]}" -gt 0 ]; then
    IGN=$(printf '%s\n' "${CAND[@]}" | sort -u | git check-ignore --stdin 2>/dev/null); IGN_RC=$?
  else
    IGN=$(printf '%s' '' | sort -u | git check-ignore --stdin 2>/dev/null); IGN_RC=$?
  fi
  # `git check-ignore`: 0 = có đường dẫn bị loại trừ, 1 = không có, lớn hơn = lệnh chết giữa
  # chừng. Khi chết, IGN rỗng KHÔNG có nghĩa "không vi phạm" — canary dưới không bắt được ca
  # này vì nó chạy riêng một đường dẫn chắc chắn hợp lệ.
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
  elif [ "$IGN_RC" -gt 1 ]; then
    bad "§5 git check-ignore thoát lỗi (mã $IGN_RC): $(printf '%s\n' "${CAND[@]}" | sort -u | git check-ignore --stdin 2>&1 >/dev/null | head -1) — nửa sau của mục này đang không kiểm gì"
  elif [ -z "$IGN" ]; then
    ok "không trích dẫn nào neo vào file bị gitignore ($seen ứng viên đã xét)"
  else
    while IFS= read -r p; do
      [ -z "$p" ] && continue
      bad "trích dẫn neo vào file bị gitignore: $p — trích ở $(grep -rnF -- "$BT$p" $SCAN_DIRS --include='*.md' 2>/dev/null | cut -d: -f1,2 | head -3 | paste -sd' ' -). Người clone về không mở được tệp này: trỏ vào tệp có trong git; tệp trạng thái chạy thì tả bằng lời như CLAUDE.md §8"
    done <<< "$IGN"
  fi
fi

# §7 — phép đo GIÁN TIẾP CỦA TÍNH TRUNG THỰC: tài liệu bịa bằng chứng thường bịa
# luôn số dòng, mà số dòng thì máy đếm được. Ở dự án tiền nhiệm, ngày thêm kiểm
# này nó lập tức tìm ra hai lời nói dối mà nhiều lượt review đã bỏ qua.
section "§7  Trích dẫn file:dòng phải nằm trong file"
declare -A LINECOUNT
# Hai lượt. Lượt 1 lọc trích dẫn và gom file đích; MỘT lệnh `wc -l` đếm dòng mọi
# file đích thường cùng lúc (mỗi `$(wc -l < f)` là hai tiến trình). Lượt 2 so và
# báo theo đúng thứ tự trích dẫn. File đích không phải file thường (thư mục...)
# hoặc không có dòng trong đầu ra gộp thì đếm từng cái như cũ.
n=0; seen=0
_r7=(); _wc7=(); declare -A _wcq7=()
for hit in "${_PATHS[@]}"; do
  [ -z "$hit" ] && continue
  f="${hit%%:*}"; rest="${hit#*:}"; ln="${rest%%:*}"; ref="${rest#*:}"
  case "$ref" in *:[0-9]*) ;; *) continue ;; esac
  case "$HIST_FILES" in *" $f "*) continue ;; esac
  # ADR có số ghi quyết định TẠI THỜI ĐIỂM viết, và D45 cấm sửa nội dung nó: số dòng
  # ADR trích là lịch sử, không bắt cập nhật khi tệp đích đổi (người dùng chốt 2026-09-27).
  case "$f" in docs/adr/[0-9]*) continue ;; esac
  [ -n "${HISTL["$f:$ln"]+x}" ] && continue
  tf="${ref%:*}"; tl="${ref##*:}"
  case "$tl" in *[!0-9]*|'') continue ;; esac
  exists "$tf" || continue
  seen=$((seen+1))
  _r7+=("$hit")
  if [ -f "$tf" ] && [ -z "${_wcq7["$tf"]+x}" ]; then _wcq7["$tf"]=1; _wc7+=("$tf"); fi
done
if [ "${#_wc7[@]}" -gt 0 ]; then
  _wco=(); _lines _wco "$(wc -l -- "${_wc7[@]}" 2>/dev/null)"
  # Nhiều file thì dòng cuối là "total" — bỏ, để một file tên "total" không bị ghi đè.
  [ "${#_wc7[@]}" -gt 1 ] && [ "${#_wco[@]}" -gt 0 ] && unset '_wco[-1]'
  for _l in "${_wco[@]}"; do
    _l="${_l#"${_l%%[! ]*}"}"
    case "$_l" in *' '?*) ;; *) continue ;; esac
    case "${_l%% *}" in ''|*[!0-9]*) continue ;; esac
    LINECOUNT["${_l#* }"]="${_l%% *}"
  done
fi
for hit in "${_r7[@]}"; do
  f="${hit%%:*}"; rest="${hit#*:}"; ln="${rest%%:*}"; ref="${rest#*:}"
  tf="${ref%:*}"; tl="${ref##*:}"
  if [ -z "${LINECOUNT[$tf]+x}" ]; then
    LINECOUNT[$tf]=$(wc -l < "$tf")
  fi
  total=${LINECOUNT[$tf]}
  [ "$tl" -le "$total" ] && [ "$tl" -ge 1 ] && continue
  bad "$f:$ln — trích dẫn $ref nhưng file chỉ có $total dòng"
  n=$((n+1))
done
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
  [ -n "${HISTL["$f:$ln"]+x}" ] && continue
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
_L=(); _lines _L "$(find $SCAN_DIRS -name '*.md' -print0 2>/dev/null | xargs -0 awk '
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
')"
for hit in "${_L[@]}"; do
  [ -z "$hit" ] && continue
  rows=$((rows+1))
  f="${hit%%:*}"; rest="${hit#*:}"; ln="${rest%%:*}"; body="${rest#*:}"
  case "$HIST_FILES" in *" $f "*) continue ;; esac
  [ -n "${HISTL["$f:$ln"]+x}" ] && continue
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
        if exists "$span"; then targets="$targets $span"
        elif exists "$fd/$span"; then targets="$targets $fd/$span"
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
      # `$(< f)`, không `$(cat f)`: cùng nội dung, nhưng bash 5.2 đọc thẳng file
      # mà không fork — `$(cat)` là hai tiến trình mỗi file đích.
      if [ -z "${FBODY[$tgt]+x}" ]; then FBODY[$tgt]=$(< "$tgt"); fi
      case "${FBODY[$tgt]}" in *"$base"*) found=1; break ;; esac
    done
    [ "$found" -eq 1 ] && continue
    bad "$f:$ln — trỏ '${targets# }' nhưng không file nào nhắc '$base'"
    n=$((n+1))
  done
done
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
  [ -n "${HISTL["$f:$ln"]+x}" ] && continue
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
elif ! git rev-parse --git-dir >/dev/null 2>&1; then
  warn "không phải git repo — §10 KHÔNG kiểm gì vì không lọc được file bị gitignore (khai báo tường minh)"
else
  n=0; seen=0
  # Chỉ quét file KHÔNG bị gitignore. node_modules/, bin/, obj/, dist/, .angular/
  # chứa hàng nghìn file sinh ra: không ai viết chú thích trỏ docs/ trong đó, và
  # quét chúng làm cổng chậm tới mức người ta bỏ chạy. Danh sách loại trừ là
  # chính .gitignore — hỏi git, không chép tên thư mục vào đây.
  _L=(); _lines _L "$(git ls-files -z -co --exclude-standard -- src 2>/dev/null | grep -zE '\.(cs|ts|scss|html)$' | xargs -0 -r grep -HnoP '(?<![A-Za-z0-9_/.-])docs/[A-Za-z0-9._/-]+' 2>/dev/null)"
  for hit in "${_L[@]}"; do
    [ -z "$hit" ] && continue
    f="${hit%%:*}"; rest="${hit#*:}"; ln="${rest%%:*}"; p="${rest#*:}"
    p="${p%.}"; p="${p%,}"; p="${p%)}"
    seen=$((seen+1))
    exists "$p" && continue
    bad "$f:$ln — chú thích trỏ docs/ không tồn tại: $p"
    n=$((n+1))
  done
  if [ "$seen" -eq 0 ]; then
    warn "không có chú thích nào trong src/ trỏ docs/ — §10 KHÔNG kiểm gì (khai báo tường minh, không phải PASS)"
  elif [ "$n" -eq 0 ]; then
    ok "mọi chú thích trong src/ trỏ docs/ đều tồn tại ($seen đường dẫn)"
  fi
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
    [ -n "${HISTL["$f:$ln"]+x}" ] && continue
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
  # HIỆU NĂNG: mục này xét hơn chục nghìn trích dẫn và không cần hỏi hệ thống
  # file, nên cả vòng xét chạy trong MỘT lượt awk (vòng bash từng tốn nhiều giây).
  # awk đọc từ stdin, theo thứ tự: số mục DEAD, từng mục DEAD, HIST_FILES,
  # HIST_LINES, rồi đầu ra grep. Nó in "B<tab>file<tab>dòng<tab>đích" cho mỗi vi
  # phạm (đúng thứ tự trích dẫn) và "S<tab>số-đã-xét" ở cuối; câu báo lỗi ghép ở
  # bash bên dưới. LC_ALL=C: mọi phép cắt đều cắt theo ký tự ASCII (: ` ]( ) # / ../)
  # nên cắt theo byte cho cùng kết quả với cắt theo ký tự của bash.
  # `$DEAD` được tách + mở rộng glob MỘT lần ở đây, đúng phép `for _d in $DEAD` cũ.
  _dw=(); for _d in $DEAD; do _dw+=("$_d"); done
  _L=(); _lines _L "$( {
      printf '%s\n' "${#_dw[@]}"
      [ "${#_dw[@]}" -gt 0 ] && printf '%s\n' "${_dw[@]}"
      printf '%s\n' "$HIST_FILES" "$HIST_LINES"
      grep -rnoE '\]\([^)]*\.md\)|`[A-Za-z0-9._/-]+\.md`' $SCAN_DIRS --include='*.md' 2>/dev/null
    } | LC_ALL=C awk '
    NR == 1       { nd = $0 + 0; next }
    NR <= 1 + nd  { D[NR - 1] = $0; next }
    NR == 2 + nd  { hf = $0; next }
    NR == 3 + nd  { hl = $0; next }
    $0 == ""      { next }
    {
      # f="${hit%%:*}"; rest="${hit#*:}"; ln="${rest%%:*}"; p="${rest#*:}"
      hit = $0
      c = index(hit, ":");  if (c) { f = substr(hit, 1, c - 1); rest = substr(hit, c + 1) } else { f = hit; rest = hit }
      c = index(rest, ":"); if (c) { ln = substr(rest, 1, c - 1); p = substr(rest, c + 1) } else { ln = rest; p = rest }
      # miễn trừ: file lịch sử, dòng lịch sử — cùng phép tìm chuỗi con như bản bash
      if (index(hf, " " f " ")) next
      if (index(hl, " " f ":" ln " ")) next
      # Gỡ vỏ bọc TRƯỚC khi so tên. So chuỗi còn nguyên dấu backtick với tên file
      # trần thì không bao giờ khớp, và mục này xanh suốt mà không kiểm gì.
      if (substr(p, 1, 1) == "`") p = substr(p, 2)                               # `file.md`
      if (p != "" && substr(p, length(p)) == "`") p = substr(p, 1, length(p) - 1)
      if (substr(p, 1, 2) == "](") p = substr(p, 3)                              # ](file.md)
      if (p != "" && substr(p, length(p)) == ")") p = substr(p, 1, length(p) - 1)
      # Bo tien to "../" roi so HAU TO duong dan: mot trich dan tuong doi
      # `../../database/x.md` khop dung `docs/database/x.md`, va KHONG khop
      # `docs/other/x.md`. Chat hon basename, khong can realpath.
      c = index(p, "#"); base = c ? substr(p, 1, c - 1) : p
      while (substr(base, 1, 3) == "../") base = substr(base, 4)
      if (substr(base, 1, 2) == "./") base = substr(base, 3)
      # Ten TRAN (khong co dau "/") phai duoc giai theo thu muc cua file dang
      # xet. Neu khong, mot trich dan `README.md` bat ky se khop hau to voi MOI
      # file README.md trong repo — dung ca bao sai dien rong ma muc nay tranh.
      if (index(f, "/")) { fd = f; sub(/\/[^\/]*$/, "", fd) } else fd = "."
      if (!index(base, "/")) base = fd "/" base
      seen++
      lb = length(base)
      for (m = 1; m <= nd; m++) {
        d = D[m]; ld = length(d)
        if (ld >= lb && substr(d, ld - lb + 1) == base) { print "B\t" f "\t" ln "\t" base; break }
      }
    }
    END { print "S\t" seen + 0 }')"
  for _l in "${_L[@]}"; do
    case "$_l" in
      S$'\t'*) seen="${_l#S$'\t'}" ;;
      B$'\t'*)
        _r="${_l#B$'\t'}"; f="${_r%%$'\t'*}"; _r="${_r#*$'\t'}"; ln="${_r%%$'\t'*}"; base="${_r#*$'\t'}"
        bad "$f:$ln — trích dẫn trỏ vào tài liệu đã chết (kind: lich-su): $base"
        n=$((n+1)) ;;
    esac
  done
  # awk không chạy / không in dòng S -> seen không phải số -> coi như 0 -> FAIL bên dưới.
  case "$seen" in ''|*[!0-9]*) seen=0 ;; esac
  if [ "$seen" -eq 0 ]; then
    bad "§13 KHÔNG trích được trích dẫn nào — mục này đang không kiểm gì"
  elif [ "$n" -eq 0 ]; then
    ok "không trích dẫn nào trỏ vào tài liệu đã chết ($seen trích dẫn)"
  fi
fi

# ================================================================ §14
# Mọi luật trong docs/RULES.md và docs/RULES-*.md phải khai cột "ép bằng gì".
# Một luật trả lời "bằng niềm tin" thì không phải luật — nó là gợi ý, và phải
# nằm ở docs/DEBT.md, nhìn thấy được, chứ không trộn vào bảng luật.
section "§14 Mọi luật trong RULES.md khai cột ép bằng gì"
n=0; seen=0
_L=(); _lines _L "$(grep -h '^|' docs/RULES.md docs/RULES-*.md 2>/dev/null)"
for line in "${_L[@]}"; do
  [ -z "$line" ] && continue
  case "$line" in *'| MUST'*|*'| SHOULD'*) ;; *) continue ;; esac
  seen=$((seen+1))
  _pipe_fields "$line" 5; c_id="${_PF[2]}"; c_enf="${_PF[5]}"
  enf="${c_enf#"${c_enf%%[![:space:]]*}"}"; enf="${enf%"${enf##*[![:space:]]}"}"
  id="${c_id//[[:space:]]/}"
  case "$enf" in ''|'—'|'-'|'?')
    bad "RULES.md — luật $id không khai cột 'ép bằng gì' (phải chuyển sang docs/DEBT.md)"
    n=$((n+1)) ;;
  esac
done
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
  # HIỆU NĂNG: bản trước chạy một `grep -rlF` quét trọn docs/ cho MỖI dòng sổ,
  # cộng bốn năm tiến trình phụ mỗi dòng — mục chậm nhất cả cổng. Nay ba lượt:
  #   1. đọc sổ, tách cột bằng builtin (không `$(trim)`, không here-string);
  #   2. MỘT lượt `grep -F -f` + awk qua docs/, tìm mọi chuỗi cùng lúc;
  #   3. báo theo đúng thứ tự dòng sổ, cùng câu chữ, cùng điều kiện.
  # awk chạy LC_ALL=C: `index()` so byte, và chuỗi UTF-8 hợp lệ khớp theo byte
  # đúng khi và chỉ khi nó khớp theo ký tự — cùng kết quả với `grep -F`.
  # Chuỗi bắt đầu bằng '-' thì `grep` đọc nó thành CỜ; lượt 2 không mô phỏng được
  # điều đó, nên dòng ấy đi đúng đường `grep` cũ, từng dòng một.
  n=0; seen=0
  _t15=(); _o15=(); _s15=(); _k15=(); _sl15=(); _ix15=()
  while IFS= read -r line; do
    case "$line" in '|'*'|'*'|'*) ;; *) continue ;; esac
    case "$line" in *'---'*|*'Chủ đề'*) continue ;; esac
    _pipe_fields "$line" 4
    # gỡ khoảng trắng và backtick
    _trim_to topic "${_PF[2]}"; _trim_to owner "${_PF[3]}"; _trim_to sig "${_PF[4]}"
    [ -z "$sig" ] || [ -z "$owner" ] && continue
    # Bo qua moi bang KHAC nam trong pham vi §3 (bang giai thich, bang doi
    # chieu). Dong so THAT luon co cot file chu bat dau bang `docs/` — do la
    # dau hieu may doc duoc; nhan bat ky dong `| a | b | c |` nao la bao sai.
    case "$owner" in docs/*) ;; *) continue ;; esac
    seen=$((seen+1))
    _j=${#_k15[@]}
    _t15[_j]="$topic"; _o15[_j]="$owner"; _s15[_j]="$sig"
    if [ ! -e "$owner" ]; then _k15[_j]=miss
    else
      case "$sig" in
        -*) _k15[_j]=grep ;;
        *)  _k15[_j]=scan; _ix15[${#_sl15[@]}]=$_j; _sl15+=("$sig") ;;
      esac
    fi
  done < <(sed -n '/^## 3\./,/^## 4\./p' "$OWN")
  declare -A _h15=() _c15=()
  if [ "${#_sl15[@]}" -gt 0 ]; then
    # Lượt 2 gồm hai tầng, cùng quét docs/ theo đúng cách bản cũ quét
    # (`grep -r docs --include='*.md'`):
    #   - `grep -F -f` lọc ra mọi dòng chứa ÍT NHẤT MỘT chuỗi (tập cha đúng: dòng
    #     nào chứa một chuỗi thì chắc chắn qua lọc). `-a` để file "nhị phân" vẫn
    #     in dòng thật, không in câu "Binary file … matches".
    #   - awk xét từng dòng còn lại với TỪNG chuỗi bằng `index()` — phép khớp
    #     chính xác; in "chỉ-số<tab>file" một lần cho mỗi cặp (chuỗi, file).
    # Danh sách chuỗi đi vào awk qua stdin (dòng đầu = số chuỗi), không qua biến
    # môi trường: Windows giới hạn cỡ một biến môi trường.
    _L=(); _lines _L "$( {
        printf '%s\n' "${#_sl15[@]}"; printf '%s\n' "${_sl15[@]}"
        grep -rHaF -f <(printf '%s\n' "${_sl15[@]}") docs --include='*.md' 2>/dev/null
      } | LC_ALL=C awk '
      NR == 1      { ns = $0 + 0; next }
      NR <= 1 + ns { S[NR - 1] = $0; next }
      {
        c = index($0, ":"); fn = substr($0, 1, c - 1); line = substr($0, c + 1)
        for (i = 1; i <= ns; i++)
          if (!((i, fn) in got) && index(line, S[i])) { got[i, fn] = 1; print (i - 1) "\t" fn }
      }')"
    for _l in "${_L[@]}"; do
      _i="${_l%%$'\t'*}"; _fp="${_l#*$'\t'}"
      # BỎ QUA chính sổ đăng ký (nó buộc phải chứa chuỗi đó).
      [ "$_fp" = "$OWN" ] && continue
      _j=${_ix15[_i]}
      _h15[$_j]="${_h15[$_j]:-}$_fp"$'\n'; _c15[$_j]=$(( ${_c15[$_j]:-0} + 1 ))
    done
  fi
  for ((_j = 0; _j < ${#_k15[@]}; _j++)); do
    topic="${_t15[_j]}"; owner="${_o15[_j]}"; sig="${_s15[_j]}"
    if [ "${_k15[_j]}" = miss ]; then
      bad "OWNERSHIP: file chủ không tồn tại — $owner (chủ đề: $topic)"
      n=$((n+1)); continue
    fi
    # Đếm file chứa chuỗi, BỎ QUA chính sổ đăng ký (nó buộc phải chứa chuỗi đó).
    if [ "${_k15[_j]}" = grep ]; then
      hits=$(grep -rlF "$sig" docs --include='*.md' 2>/dev/null | grep -v "^$OWN$" | sort -u)
      cnt=$(printf '%s' "$hits" | grep -c . || true)
    else
      cnt="${_c15[$_j]:-0}"
      case "$cnt" in
        0) hits="" ;;
        1) hits="${_h15[$_j]%$'\n'}" ;;
        # Đếm lại SAU `sort -u`, như bản cũ đếm — không dùng số trước khi lọc trùng.
        *) hits=$(printf '%s' "${_h15[$_j]}" | sort -u)
           _hl15=(); _lines _hl15 "$hits"; cnt=${#_hl15[@]} ;;
      esac
    fi
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
  # Tách SIGS thành mảng MỘT lần; here-string trong vòng lặp tốn một pipe mỗi mốc.
  # (_lines bỏ dòng rỗng — vòng dưới vốn bỏ qua chuỗi rỗng.)
  _sigs16=(); _lines _sigs16 "$SIGS"
  n=0; seen=0
  while IFS= read -r hit; do
    [ -z "$hit" ] && continue
    f="${hit%%:*}"; rest="${hit#*:}"; ln="${rest%%:*}"; body="${rest#*:}"
    case "$f" in "$OWN") continue ;; esac
    case "$HIST_FILES" in *" $f "*) continue ;; esac
    [ -n "${HISTL["$f:$ln"]+x}" ] && continue
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
    for sig in "${_sigs16[@]}"; do
      [ -z "$sig" ] && continue
      case "$body" in *"$sig"*) hit_ok=1; break ;; esac
    done
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
# Gom danh sách trước, rồi MỘT lệnh `grep -H` cho mọi file (qua xargs), thay vì
# một `grep` mỗi file. `grep` đi file theo đúng thứ tự tham số, nên đầu ra vẫn
# theo thứ tự `sort` rồi thứ tự dòng — y như vòng lặp cũ.
_f17=()
while IFS= read -r f; do
  case "$f" in docs/audit/*|docs/adr/*) continue ;; esac
  case "$HIST_FILES" in *" $f "*) continue ;; esac
  scanned=$((scanned+1))
  _f17+=("$f")
done < <(find docs .claude spec -name '*.md' 2>/dev/null | sort)
if [ "${#_f17[@]}" -gt 0 ]; then
  while IFS= read -r hit; do
    [ -z "$hit" ] && continue
    # grep 3.0 báo file nhị phân bằng MỘT dòng không có "file:" đứng đầu. Bản cũ
    # chạy grep trên từng file nên ghép "$f:" + nguyên dòng đó; giữ y như vậy.
    case "$hit" in
      "Binary file "*" matches") f="${hit#Binary file }"; f="${f% matches}"; ln17="$hit" ;;
      *) f="${hit%%:*}"; ln17="${hit#*:}"; ln17="${ln17%%:*}" ;;
    esac
    bad "$f:$ln17 — kể lại bản trước. Bản cũ nằm trong lịch sử git; bài học vào docs/audit/"
    n=$((n+1))
  done < <(printf '%s\0' "${_f17[@]}" | xargs -0 grep -HnE "$HISTPAT" 2>/dev/null)
fi
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
# Phan con lai cua D27 van thuoc docs/DEBT.md: `core-reviewer` va nguoi doc bat.
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
    *'`K-MOI`'*)
      # Ma GIU CHO: huong khoa moi chua duoc cap so. Van la vi pham — ma khong
      # co so thi khong dem duoc lo trong — nhung phai noi dung ten vi pham, va
      # khong duoc dua chuoi khong phai so vao phep so sanh so ben duoi (ban
      # truoc do bash thoat loi "integer expression expected" giua muc).
      bad "$hit — hướng khoá mang mã giữ chỗ ${BT}K-MOI${BT}, chưa cấp số ${BT}K##${BT}. Cấp mã kế tiếp sau mã lớn nhất (RULES.md D32)"; n=$((n+1)) ;;
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
    case "$v" in ''|*[!0-9]*) bad "mã hướng khoá không phải số: $c"; n=$((n+1)); continue ;; esac
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
    # Hai lệnh `grep -Hc` cho CẢ khu, thay vì hai lệnh mỗi file. `-c` đếm dòng
    # khớp; "mục 5 có mặt" = đếm > 0, đúng điều `grep -q` cũ trả lời. File grep
    # không đọc được thì không có dòng đếm — rơi về rỗng / 0, như bản cũ.
    declare -A _s6=() _q5=()
    while IFS= read -r _l; do _s6["${_l%:*}"]="${_l##*:}"; done < <(grep -HcE '^## [1-6]\. ' "$@")
    while IFS= read -r _l; do _q5["${_l%:*}"]="${_l##*:}"; done < <(grep -Hc '^## 5\. Quan hệ với đơn vị' "$@")
    for f in "$@"; do
      s6="${_s6[$f]:-}"
      [ "$s6" -eq 6 ] || { bad "$f — có $s6/6 mục bắt buộc. Khuôn sáu mục ở docs/luong/README.md §3"; n=$((n+1)); }
      [ "${_q5[$f]:-0}" -gt 0 ] || { bad "$f — thiếu mục 'Quan hệ với đơn vị'. Trả lời một trong ba: thuộc đơn vị / dùng chung toàn hệ / không áp dụng kèm lý do"; n=$((n+1)); }
    done
    [ "$n" -eq 0 ] && ok "mục lục khớp thư mục ($n_file luồng), mỗi luồng đủ sáu mục và khai quan hệ với đơn vị"
  fi
fi

# ================================================================ §23
# Luat D36. Bo luat cua moi agent co nguong co.
#
# Bang dinh tuyen cua mot agent co hai phan, khai bang TIEU DE MUC:
#   - Muc co chu "Bộ luật": phan agent LUON doc (checklist RULES-* + tong quan).
#     Tong co cua ca phan nay la corpus mot luot phai ganh -> co nguong.
#     Tieu de mang "phạm vi BE" / "phạm vi FE" thi tinh rieng tung pham vi
#     (core-reviewer: mot luot = mot pham vi); phan khong mang pham vi la chung.
#   - Muc co chu "Tra cứu": mo khi viec cham dung chu de -> khong cong.
# Corpus = chung + max(BE, FE). Nguong do nguoi dung chot 2026-09-27 (ADR-0109):
# 120 KB cho moi agent, ke ca core-reviewer.
#
# Mot agent khong co muc "Bộ luật" nao la mot agent khong khai corpus -> FAIL,
# vi "khong do" khong duoc phep tron voi "dat nguong".
section "§23 Bộ luật của mỗi agent không vượt ngưỡng cỡ"
CORPUS_MAX=$((120*1024)); CORPUS_MAX_REVIEWER=$((120*1024))
_rows=$(awk '
  FNR==1 { mode="none"; scope="chung"; a=FILENAME; sub(/.*\//,"",a); sub(/\.md$/,"",a) }
  /^#+ / { mode="none"; scope="chung"
           if ($0 ~ /Bộ luật/) { mode="luat"; if ($0 ~ /phạm vi BE/) scope="BE"; else if ($0 ~ /phạm vi FE/) scope="FE" }
           next }
  mode=="luat" && /^\|/ {
    line=$0
    while (match(line, /`[^`]*\.md`/)) { print a, scope, substr(line, RSTART+1, RLENGTH-2); line=substr(line, RSTART+RLENGTH) }
  }' .claude/agents/*.md 2>/dev/null | sort -u)
declare -A CSIZE
while read -r sz p; do [ -n "$p" ] && CSIZE[$p]=$sz; done < <(printf '%s\n' "$_rows" | awk '{print $3}' | sort -u | xargs wc -c 2>/dev/null)
declare -A CCHUNG CBE CFE
while read -r a sc p; do
  [ -z "$p" ] && continue
  sz="${CSIZE[$p]:-0}"
  case "$sc" in
    BE) CBE[$a]=$(( ${CBE[$a]:-0} + sz )) ;;
    FE) CFE[$a]=$(( ${CFE[$a]:-0} + sz )) ;;
    *)  CCHUNG[$a]=$(( ${CCHUNG[$a]:-0} + sz )) ;;
  esac
done < <(printf '%s\n' "$_rows")
n=0; seen=0; summary=""
for af in .claude/agents/*.md; do
  a="${af##*/}"; a="${a%.md}"
  chung="${CCHUNG[$a]:-0}"; be="${CBE[$a]:-0}"; fe="${CFE[$a]:-0}"
  if [ "$((chung+be+fe))" -eq 0 ]; then
    bad "$af — không có mục 'Bộ luật' nào trỏ tới file .md: corpus của agent này không đo được"
    n=$((n+1)); continue
  fi
  seen=$((seen+1))
  big=$be; [ "$fe" -gt "$big" ] && big=$fe
  corpus=$((chung+big)); max=$CORPUS_MAX
  [ "$a" = "core-reviewer" ] && max=$CORPUS_MAX_REVIEWER
  summary="$summary $a=$(((corpus+1023)/1024))KB"
  if [ "$corpus" -gt "$max" ]; then
    bad "$af — bộ luật $(((corpus+1023)/1024)) KB vượt ngưỡng $((max/1024)) KB (chung $(((chung+1023)/1024)), BE $(((be+1023)/1024)), FE $(((fe+1023)/1024))). Rút file luật hoặc chuyển hàng sang mục 'Tra cứu' — không nới ngưỡng"
    n=$((n+1))
  fi
done
if [ "$seen" -eq 0 ] && [ "$n" -eq 0 ]; then
  bad "§23 không thấy file agent nào — mục này đang không kiểm gì"
elif [ "$n" -eq 0 ]; then
  ok "bộ luật mọi agent dưới ngưỡng ($seen agent):$summary"
fi

# ================================================================ §24
# Luat D37. Van xuoi chep lap — mot dong dai xuat hien o nhieu file.
#
# §15/§16/§19/§20 chi bat ban sao cua thu DA DANG KY (moc, kieu, token).
# Muc nay bat dang con lai: mot cau van >= 100 ky tu duoc chep nguyen van sang
# file khac. Hai file thi co the la trung hop (checklist, cau mo ta trang thai)
# -> NOTE; tu ba file tro len la mot boilerplate dang song o nhieu cho -> FAIL:
# chon mot file chu, cac file khac tro duong. FAIL chi khi cum co file docs/
# (noi dung); cum thuan .claude/ (van ban quy trinh) chi NOTE — nhin thay, khong chan.
#
# Mien tru (deu la thu KHONG phai noi dung): dong bang, blockquote, tieu de,
# code block, dong tho dau (ASCII/so do), dong mang nhan trang thai (bat buoc
# o moi file), dong tro duong (co link va phan chu con lai ngan), file lich su.
section "§24 Văn xuôi không được chép nguyên văn sang file khác"
_dup=$(find docs .claude -name '*.md' -print0 2>/dev/null | xargs -0 awk -v hist="$HIST_FILES" '
  FNR==1 { inblk=0; skip=(index(hist, " " FILENAME " ")>0) }
  skip { next }
  FNR<=6 && /^kind: lich-su/ { skip=1; next }
  /^[ \t]*```/ { inblk=!inblk; next }
  inblk { next }
  { line=$0; sub(/[ \t]+$/,"",line) }
  line ~ /^(\||>|#|---|[ \t])/ { next }
  line ~ /[│┌└├┐┘┤─┬┴▼►]/ { next }
  line ~ /ĐÍCH ĐẾN — CHƯA THI CÔNG|ĐÃ CHỐT — ĐANG THI CÔNG|CÓ THẬT/ { next }
  {
    bare=line; gsub(/\[[^]]*\]\([^)]*\)/,"",bare); gsub(/`[^`]*`/,"",bare)
    if (index(line,"](")>0 && length(bare)<60) next
    if (length(line) < 100) next
    print FILENAME "\t" line
  }' | sort -u | awk -F'\t' '{ n[$2]++; f[$2]=f[$2] " " $1 } END { for (k in n) if (n[k]>1) print n[k] "\t" k "\t" f[k] }' | sort -rn)
_seen=$(find docs .claude -name '*.md' 2>/dev/null | wc -l)
n=0; w=0
while IFS=$'\t' read -r cnt text files; do
  [ -z "$cnt" ] && continue
  short="${text:0:90}"
  case "$files" in *" docs/"*) indocs=1 ;; *) indocs=0 ;; esac
  if [ "$cnt" -ge 3 ] && [ "$indocs" -eq 1 ]; then
    bad "$cnt file cùng một câu — chọn một file chủ, còn lại trỏ đường:$files ↦ \"$short…\""
    n=$((n+1))
  else
    warn "$cnt file cùng một câu:$files ↦ \"$short…\""
    w=$((w+1))
  fi
done < <(printf '%s\n' "$_dup")
if [ "$_seen" -lt "$MIN_DOCS" ]; then
  bad "§24 chỉ thấy $_seen file — mục này đang không kiểm gì"
elif [ "$n" -eq 0 ]; then
  ok "không câu nào trong docs/ bị chép sang ≥3 file ($_seen file đã quét, $w cụm ở dạng NOTE)"
fi

# ================================================================ §25
# Luat D38. File luat qua co thi tach ly do — canh bao mem.
#
# §23 canh TONG bo luat cua mot agent; muc nay canh TUNG file luat, de phinh lo
# ra o dung file truoc khi §23 do. Nguong 50 KB: cac file luat lon nhat sau dot
# tach 2026-09-16 nam o 46–50 KB. Vuot nguong khong chan — NOTE, vi them mot
# luat quan trong van phai duoc phep; cach dung la doi phan "vi sao / bay / vi
# du mo rong" sang wiki-core/{be,fe}/ly-do/<cung ten>.md, khong bo luat.
section "§25 File luật vượt cỡ thì tách lý do (cảnh báo mềm)"
LUAT_MAX=$((50*1024))
# schema-core.md khong tinh: no la bang dinh nghia (cot, index, SQL kiem), khong phai van xuoi
# luat; co cua no do so bang quyet, va §23 van canh no trong bo luat cua core-reviewer.
set -- docs/quy-uoc/*.md docs/kien-truc-core-module.md docs/database/script-runbook.md
n_luat=0; w=0
while read -r sz f; do
  [ -z "$f" ] && continue
  n_luat=$((n_luat+1))
  if [ "$sz" -gt "$LUAT_MAX" ]; then
    warn "$f — $(((sz+1023)/1024)) KB > $((LUAT_MAX/1024)) KB: dời 'vì sao / bẫy / ví dụ mở rộng' sang wiki-core/*/ly-do/ (luật D38)"
    w=$((w+1))
  fi
done < <(wc -c "$@" 2>/dev/null | grep -v ' total$')
if [ "$n_luat" -eq 0 ]; then
  bad "§25 không đo được file luật nào — mục này đang không kiểm gì"
else
  ok "đã đo $n_luat file luật, $w file trên $((LUAT_MAX/1024)) KB (NOTE, không chặn)"
fi

section "§26 Khối core-paths thật phải đọc được"
# Khối này là NGUỒN DUY NHẤT của câu hỏi "thay đổi này có chạm Core không", và
# hook on-edit.sh đọc thẳng nó. Bộ test hook chạy core-paths.sh trên một FIXTURE
# riêng (cố ý — để test không đổi kết quả khi tài liệu thật đang sửa dở), nên
# trước mục này KHÔNG cổng nào chạy nó trên docs/kien-truc-core-module.md: khối
# bị rào sai, bị tách đôi, hay lọt một dòng văn xuôi thì mọi cổng vẫn PASS và
# định nghĩa "cái gì thuộc Core" biến mất trong im lặng.
#
# Chỉ tiêu thụ MÃ THOÁT. Không chép danh sách, không đếm số dòng — hai thứ đó có
# chủ ở chính khối, và một bản đếm ở đây sẽ mục ruỗng (§6).
cp_out=$(bash .claude/hooks/core-paths.sh 2>&1)
cp_rc=$?
if [ "$cp_rc" -ne 0 ]; then
  bad "core-paths.sh thoát mã $cp_rc — khối trong docs/kien-truc-core-module.md không đọc được"
  printf '%s\n' "$cp_out" | sed 's/^/        /'
elif [ -z "$cp_out" ]; then
  bad "core-paths.sh thoát 0 nhưng in ra RỖNG — không đường dẫn nào được coi là chạm Core"
else
  ok "khối core-paths đọc được ($(printf '%s\n' "$cp_out" | grep -c .) đường dẫn)"
fi

section "§27 Nhãn 📐 cấp tệp không được đứng trên một neo trỏ tới code có thật (D42)"
# Nhãn 📐 cấp tệp MIỄN TRỪ CẢ TỆP khỏi tầm chấm review (quy-uoc/tieu-chi-review.md
# §3.2), và không cơ chế nào buộc nó được xét lại khi code về. Cơ chế đó đã giấu
# một lỗ bảo mật thật ở docs/contracts/. Luật D42 + ADR-0048.
#
# Hai nửa ĐỘC LẬP. Nửa nào không thấy thư mục mã nguồn của mình thì khai NOTE rồi
# PASS — im lặng PASS và báo đỏ hàng loạt trên cây không có mã nguồn sai như nhau
# (cùng điều kiện (c) của D39).
#
# 🛑 LC_ALL=C, KHÔNG dùng LC_ALL=en_US.UTF-8 của đầu file. Trên Git Bash, awk dưới
# locale đó KHÔNG khớp được 📐 và "ĐÍCH ĐẾN": nó thoát 0 và in RỖNG. Đã đo — cả
# hai nửa cùng báo "0 tệp mang nhãn" trong khi thực tế có hàng chục. Cùng họ bẫy
# với grep -P mà đầu file cảnh báo, chỉ khác công cụ. Các mẫu ở đây là chuỗi byte
# UTF-8 nguyên văn nên so theo byte (LC_ALL=C) mới đúng.
d42_fail=0
d42_tmp="${TMPDIR:-/tmp}/d42.$$"
mkdir -p "$d42_tmp" 2>/dev/null || { bad "§27 không tạo được thư mục tạm"; d42_fail=1; }

# CANARY CHO CHÍNH PHÉP DÒ NHÃN — không phải cho phép so.
# Hai chốt T6 bên dưới (số route thật, số cặp tên component) canh hai BỘ ĐỌC KHÁC.
# Khi phép dò 📐 chết thì cả hai vẫn khác rỗng, tập tệp-mang-nhãn về 0, và mục này
# in OK — đúng khuôn "xanh vì bộ đọc hỏng" mà nó sinh ra để chống. Đã xảy ra thật
# một lần trong phiên viết mục này, do locale.
# Dò trên một chuỗi dựng tại chỗ nên chốt này đúng kể cả khi repo hết sạch vi phạm.
if [ "$(printf '> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**\n' | LC_ALL=C awk '/📐/ && /ĐÍCH ĐẾN/ { n++ } END { print n+0 }')" != "1" ]; then
  bad "§27 phép dò nhãn 📐 KHÔNG khớp được một banner dựng sẵn — bộ dò chết, mọi số 0 bên dưới là giả"
  d42_fail=1
fi

# Đổi PascalCase -> kebab. Viết tay vì gsub của awk KHÔNG có tham chiếu ngược:
# gsub(/(.)([A-Z])/, "\\1-\\2") nuốt ký tự và cho "aut-ard" thay vì "auth-card".
d42_kebab='function kebab(s,   i, c, out) {
  out = ""
  for (i = 1; i <= length(s); i++) {
    c = substr(s, i, 1)
    if (i > 1 && c >= "A" && c <= "Z") out = out "-"
    out = out c
  }
  return tolower(out)
}'

# ---- nửa contracts: route ở tiêu đề card ↔ action dưới Controllers/
if [ ! -d src/BE ]; then
  warn "không có src/BE — nửa contracts/ KHÔNG xét gì (không phải PASS)"
else
  # TẬP A — nhớ tệp nào mang 📐 trong 15 dòng đầu, rồi thu route tĩnh ở tiêu đề
  # card của chính tệp đó. Mẫu cố ý không nhận '{': route có tham số là card
  # khuôn, không phải lời khai về một endpoint cụ thể (D42).
  LC_ALL=C awk '
    FNR == 1 { mark = 0 }
    FNR <= 15 && /📐/ && /ĐÍCH ĐẾN/ { mark = 1 }
    mark && /^## / {
      s = $0
      while (match(s, /(GET|POST|PUT|PATCH|DELETE) \/[A-Za-z0-9\/._-]+/)) {
        r = substr(s, RSTART, RLENGTH)
        sub(/\/+$/, "", r)
        print tolower(r) "\t" FILENAME
        s = substr(s, RSTART + RLENGTH)
      }
    }
  ' docs/contracts/*.md 2>/dev/null | sort -u > "$d42_tmp/a"

  # TẬP B — ghép [Route(...)] cấp controller với [HttpVerb("...")] cấp action.
  LC_ALL=C awk '
    FNR == 1 { base = "" }
    /\[Route\("/ { if (match($0, /\[Route\("[^"]*"/)) base = substr($0, RSTART + 8, RLENGTH - 9) }
    /\[Http(Get|Post|Put|Patch|Delete)/ {
      if (!match($0, /\[Http(Get|Post|Put|Patch|Delete)/)) next
      verb = tolower(substr($0, RSTART + 5, RLENGTH - 5))
      suffix = ""
      if (match($0, /\[Http(Get|Post|Put|Patch|Delete)\("[^"]*"/)) {
        t = substr($0, RSTART, RLENGTH); sub(/^[^"]*"/, "", t); sub(/"$/, "", t); suffix = t
      }
      p = base; if (suffix != "") p = p "/" suffix
      sub(/\/+$/, "", p)
      print verb " /" tolower(p)
    }
  ' $(find src/BE -name '*Controller.cs' -not -path '*/bin/*' -not -path '*/obj/*' 2>/dev/null) 2>/dev/null \
    | sort -u > "$d42_tmp/b"

  d42_files=$(cut -f2 "$d42_tmp/a" | sort -u | grep -c .)
  d42_routes=$(grep -c . "$d42_tmp/b")

  # Chốt T6 — cổng xanh vì bộ đọc hỏng và cổng xanh vì hết vi phạm trông giống hệt nhau.
  if [ "$d42_routes" -eq 0 ]; then
    bad "§27 đọc được 0 route thật dưới src/BE — bộ đọc hỏng, cổng đang xanh rỗng"; d42_fail=1
  fi

  # Canary — một route chắc chắn có thật phải bị chính phép so này bắt được.
  head -1 "$d42_tmp/b" > "$d42_tmp/canary"
  if [ -s "$d42_tmp/canary" ] && [ -z "$(comm -12 "$d42_tmp/canary" "$d42_tmp/b")" ]; then
    bad "§27 canary trượt — phép so không bắt được một route có thật"; d42_fail=1
  fi

  cut -f1 "$d42_tmp/a" | sort -u > "$d42_tmp/a-routes"
  comm -12 "$d42_tmp/a-routes" "$d42_tmp/b" > "$d42_tmp/hit"
  while IFS= read -r r; do
    [ -z "$r" ] && continue
    owner=$(LC_ALL=C awk -F'\t' -v k="$r" '$1 == k { print $2; exit }' "$d42_tmp/a")
    bad "$owner mang 📐 cấp tệp nhưng route \"$r\" đã có action thật dưới src/BE (D42)"
    d42_fail=1
  done < "$d42_tmp/hit"

  ok "nửa contracts/: $d42_files tệp mang nhãn đã xét, $d42_routes route thật đọc được"
fi

# ---- nửa Design: tên component ↔ tệp component dạng kebab dưới src/FE
if [ ! -d src/FE ]; then
  warn "không có src/FE — nửa Design/ KHÔNG xét gì (không phải PASS)"
else
  find src/FE/src -name '*.component.ts' -not -path '*/node_modules/*' 2>/dev/null \
    | LC_ALL=C awk -F/ '{ n = $NF; sub(/\.component\.ts$/, "", n); if ($(NF-1) == n) print n "\t" $0 }' \
    | sort -u > "$d42_tmp/comp"

  LC_ALL=C awk "$d42_kebab"'
    FNR == 1 { mark = 0 }
    FNR <= 15 && /📐/ && /ĐÍCH ĐẾN/ && !mark {
      mark = 1
      n = FILENAME; sub(/.*\//, "", n); sub(/\.md$/, "", n)
      print kebab(n) "\t" FILENAME
    }
  ' docs/Design/Components/*.md 2>/dev/null | sort -u > "$d42_tmp/spec"

  # Chốt T6 cho phép đổi tên: đếm MỌI cặp tên khớp được, kể cả tệp không mang nhãn.
  # Đổi quy ước đặt tên ở src/FE làm nửa này câm lặng, và số 0 ở đây là dấu hiệu
  # duy nhất phân biệt "hết vi phạm" với "bộ đổi tên hỏng".
  ls docs/Design/Components/*.md 2>/dev/null \
    | LC_ALL=C awk -F/ "$d42_kebab"'{ n = $NF; sub(/\.md$/, "", n); print kebab(n) }' \
    | sort -u > "$d42_tmp/allspec"
  cut -f1 "$d42_tmp/comp" | sort -u > "$d42_tmp/compnames"
  d42_pairs=$(comm -12 "$d42_tmp/allspec" "$d42_tmp/compnames" | grep -c .)
  d42_dfiles=$(grep -c . "$d42_tmp/spec")

  if [ "$d42_pairs" -eq 0 ]; then
    bad "§27 khớp được 0 cặp tên component — phép đổi tên hỏng, nửa Design đang xanh rỗng"; d42_fail=1
  fi

  # Tra tên -> tệp component bằng mảng dựng MỘT lần từ "$d42_tmp/comp", thay vì
  # một `awk` mỗi tệp spec. Giữ đúng nghĩa `awk '$1 == k { print $2; exit }'`:
  # dòng ĐẦU TIÊN có cột 1 bằng k thắng, lấy cột 2.
  declare -A _comp27=()
  while IFS= read -r _l; do
    _k="${_l%%$'\t'*}"
    if [ "$_k" = "$_l" ]; then _v=""; else _v="${_l#*$'\t'}"; _v="${_v%%$'\t'*}"; fi
    [ -n "$_k" ] && [ -z "${_comp27["$_k"]+x}" ] && _comp27["$_k"]="$_v"
  done < "$d42_tmp/comp"
  while IFS=$'\t' read -r k f; do
    [ -z "$k" ] && continue
    comp="${_comp27["$k"]:-}"
    [ -z "$comp" ] && continue
    bad "$f mang 📐 cấp tệp nhưng component đã có thật: $comp (D42)"
    d42_fail=1
  done < "$d42_tmp/spec"

  ok "nửa Design/: $d42_dfiles tệp mang nhãn đã xét, $d42_pairs cặp tên khớp được"
fi

rm -rf "$d42_tmp" 2>/dev/null
[ "$d42_fail" -eq 0 ] && ok "không tệp nào mang 📐 cấp tệp trên một neo trỏ tới code có thật"

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
