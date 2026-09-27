#!/usr/bin/env bash
# on-edit.sh — hook PostToolUse (Edit|Write|NotebookEdit|Bash|PowerShell).
#
# Việc duy nhất: GHI DẤU chạm code. KHÔNG chạy cổng. Cổng tài liệu cũng không cần
# dấu từ đây: hook Stop tự hỏi hệ thống tệp xem có gì mới hơn lần cổng xanh cuối.
#
# Dấu, ở .claude/.state/ (bị gitignore), chỉ ghi khi src/ tồn tại:
#   core-touched                 nhật ký CHUNG các file chạm Core, mọi phiên ghi vào.
#                                Review phủ cả cây, nên nhật ký là một sự thật của repo
#   phien/<phiên>/touched-core   phiên NÀY đã chạm Core — hook Stop chỉ chặn phiên có cờ
#   phien/<phiên>/src-touched    phiên này đã sửa src/ — hook Stop nhắc cổng build/test
#   phien/<phiên>/scan-stamp     mốc lần quét trước của nhánh lệnh shell
#
# "Chạm Core" đọc từ khối máy đọc trong docs/kien-truc-core-module.md qua
# core-paths.sh. File này không giữ danh sách đường dẫn Core nào.
#
# Không phụ thuộc `jq` (máy này không có). Không dùng `grep -P`.
# Luôn thoát 0: một hook ghi dấu mà làm hỏng lượt là một hook người ta tắt đi.

set -uo pipefail
# Ép locale, VÀ không dùng `grep -P`. Hai lớp cho cùng một bẫy: trên Git Bash
# với LANG rỗng, `grep -P` thoát mã 2 kèm "supports only unibyte and UTF-8
# locales". Trong một hook, lỗi đó KHÔNG hiện ra đâu cả — dấu đơn giản không
# được ghi.
export LC_ALL=en_US.UTF-8
cd "$(dirname "$0")/../.." || exit 0
. .claude/hooks/lib.sh || exit 0

STATE=".claude/.state"

payload=$(cat)
P=$(printf '%s' "$payload" | phien_dir)

# Cat bo phan `tool_response` TRUOC khi trich bat cu thu gi. Payload cua hook
# chua ca ket qua tra ve cua cong cu, va ket qua do co the chua nguyen van
# nhung chuoi ma cac mau duoi day dang tim. Moi mau BRE deu tham lam, nen
# `.*"file_path"` bam vao lan xuat hien CUOI CUNG trong ca payload.
payload=${payload%%'"tool_response"'*}

# Hai lệnh sed trích đường dẫn chạy ở LC_ALL=C — BẮT BUỘC, không phải tuỳ chọn.
# Trên Git Bash (MSYS2), sed ở locale UTF-8 không khớp `.` với byte CUỐI của một
# ký tự UTF-8 bốn byte (emoji: 📐 📖 🚧 🛑 — tài liệu repo dùng chúng khắp nơi, nên
# old_string/new_string/content của Edit/Write mang chúng thường xuyên). Khi đó
# `.*` ở đuôi mẫu dừng ngay trước byte đó, lệnh `s` chỉ thay phần đã khớp, và cả
# phần còn lại của payload — bắt đầu bằng byte mồ côi 0x90/0x96/0xA7... — dính vào
# sau \1. Kết quả là một "đường dẫn" kiểu `docs/x.md<byte> Lý do…","new_string":…`
# ghi vào core-touched, không bao giờ tồn tại trên đĩa. Ở LC_ALL=C mỗi byte là
# một ký tự, `.` khớp mọi byte, mẫu phủ trọn dòng.
f=$(printf '%s' "$payload" | LC_ALL=C sed -n 's/.*"file_path"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -1)
# NotebookEdit khai `notebook_path`, KHÔNG khai `file_path`.
[ -z "$f" ] && f=$(printf '%s' "$payload" | LC_ALL=C sed -n 's/.*"notebook_path"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -1)

mode=""
if [ -n "$f" ]; then
  mode=edit
else
  # Không có file_path nào để trích ⇒ đây là một lệnh shell.
  case "$payload" in *'"command"'*) mode=shell ;; *) exit 0 ;; esac
fi

# ---------------------------------------------------------------- chỉ từ giai đoạn 2
# Dấu src-touched và core-touched chỉ có nghĩa khi có code. Giai đoạn 1 dừng ở
# đây — parser khối Core không chạy, nên một khối đang được viết dở ở docs/
# không ảnh hưởng gì tới lượt nào.
[ -d src ] || exit 0

mkdir -p "$P" 2>/dev/null || exit 0
# Mốc hoạt động của phiên: hook Stop dọn thư mục phiên không hoạt động lâu ngày
# theo mtime của chính thư mục, nên mỗi lần ghé qua đều chạm nó.
touch "$P" 2>/dev/null

# Khối "đường dẫn chạm Core". Đọc lỗi thì GHI LÝ DO ra dấu riêng để hook Stop
# nói ra — không lặng lẽ coi như "không chạm Core".
list=$(bash .claude/hooks/core-paths.sh 2>"$STATE/core-paths-error.tmp")
rc=$?
if [ "$rc" -ne 0 ]; then
  [ -s "$STATE/core-paths-error.tmp" ] || printf 'core-paths.sh thoát mã %s mà không in lý do\n' "$rc" > "$STATE/core-paths-error.tmp"
  mv -f "$STATE/core-paths-error.tmp" "$STATE/core-paths-error" 2>/dev/null
  list=""
else
  rm -f "$STATE/core-paths-error.tmp" "$STATE/core-paths-error"
fi

# Ghi một dòng vào nhật ký chung VÀ bật cờ của phiên — luôn đi cùng nhau.
ghi_core() { printf '%s\n' "$1" >> "$STATE/core-touched"; : > "$P/touched-core"; }

# ---------------------------------------------------------------- nhánh Edit/Write
if [ "$mode" = edit ]; then
  # Harness gửi đường dẫn tuyệt đối dạng Windows, gạch chéo ngược nhân đôi trong
  # JSON: "D:\\Manager\\CoreAndSkill\\docs\\x.md". So khớp trên chuỗi đó mà không
  # chuẩn hoá thì khớp nhầm cả file NGOÀI repo, và không bỏ được file bị gitignore
  # vì `git check-ignore` cần đường dẫn tương đối.
  BSL=$(printf '\134')
  # `tr` thay TUNG ky tu nen xu ly duoc ca dang nhan doi lan dang don. Truyen
  # "$BSL$BSL": mot backslash don o cuoi tap ky tu lam tr canh bao.
  f=$(printf '%s' "$f" | tr "$BSL$BSL" "//")
  while :; do case "$f" in *//*) f="${f//\/\//\/}" ;; *) break ;; esac; done
  while :; do case "$f" in ./*) f="${f#./}" ;; *) break ;; esac; done
  REL=""
  case "$f" in
    /*|[A-Za-z]:/*)
      # Windows không phân biệt hoa thường, và ký tự ổ đĩa có lúc viết thường.
      low=$(printf '%s' "$f" | tr 'A-Z' 'a-z')
      for root in "$(pwd -W 2>/dev/null || true)" "$(pwd)"; do
        [ -n "$root" ] || continue
        r=$(printf '%s' "$root" | tr 'A-Z' 'a-z')
        case "$low" in "$r"/*) REL="${f:$((${#root}+1))}"; break ;; esac
      done
      ;;
    *) REL="$f" ;;
  esac

  if [ -n "$REL" ]; then
    # File bị gitignore (bin/, obj/, node_modules/, .claude/.state/...) không phải
    # mã nguồn được commit: không có gì để canh.
    git check-ignore -q -- "$REL" 2>/dev/null && exit 0
    case "$REL" in src/*) : > "$P/src-touched" ;; esac
  else
    # Đường dẫn tuyệt đối không nhận ra được là nằm dưới gốc repo (dạng tên ngắn
    # 8.3, tiền tố lạ...). Hỏng theo hướng AN TOÀN: khớp chuỗi con.
    case "$f" in */src/*) : > "$P/src-touched" ;; esac
  fi

  # So với MỌI mục của khối, không lọc theo khu vực trước — một bộ lọc khu vực
  # đứng trước là cách nhóm scripts/ từng rơi khỏi nhánh này mà không ai biết.
  [ -n "$list" ] || exit 0
  while IFS= read -r e; do
    [ -z "$e" ] && continue
    if [ -n "$REL" ]; then
      case "$e" in
        */) case "$REL" in "$e"*) ghi_core "$REL"; exit 0 ;; esac ;;
        *)  [ "$REL" = "$e" ] && { ghi_core "$REL"; exit 0; } ;;
      esac
    else
      case "$f" in *"/$e"*) ghi_core "$f"; exit 0 ;; esac
    fi
  done <<< "$list"
  exit 0
fi

# ---------------------------------------------------------------- nhánh lệnh shell
# Không đoán từ chuỗi lệnh. Hỏi HỆ THỐNG TỆP xem file nào đã đổi từ lần quét
# trước của phiên: không parse gì, không nhầm lệnh đọc với lệnh ghi, và dấu ghi
# ra là đường dẫn thật.
#
# Mốc lần quét trước, KHÔNG phải một cửa sổ cố định: một lệnh chạy lâu hơn cửa sổ
# (build, format cả solution) sửa file ở đầu lệnh thì file đó đã "cũ" lúc hook này
# chạy. Mốc mới đặt TRƯỚC khi quét, nên file đổi trong lúc quét được lần sau thấy.
# Phiên chưa có mốc thì lùi về 90 giây. Các subagent song song của một phiên dùng
# chung mốc; một lần quét của agent này thấy file agent kia vừa sửa và ghi vào
# cùng dấu của phiên — không có khe hở giữa hai lần quét.
nap_prune
roots=(src)
while IFS= read -r e; do
  [ -z "$e" ] && continue
  case "$e" in src/*) continue ;; esac
  [ -e "${e%/}" ] && roots+=( "${e%/}" )
done <<< "$list"

tmp="$P/scan-stamp.tmp.$$"
: > "$tmp" 2>/dev/null || exit 0
if [ -e "$P/scan-stamp" ]; then newer=( -newer "$P/scan-stamp" ); else newer=( -newermt '-90 seconds' ); fi
recent=$(find "${roots[@]}" \( "${PRUNE[@]}" -false \) -prune -o -type f "${newer[@]}" -print 2>/dev/null | head -5000)
mv -f "$tmp" "$P/scan-stamp" 2>/dev/null || rm -f "$tmp"
[ -n "$recent" ] || exit 0

recent=$(printf '%s\n' "$recent" | bo_gitignore)
[ -n "$recent" ] || exit 0

case "$recent" in src/*|*$'\n'src/*) : > "$P/src-touched" ;; esac

[ -n "$list" ] || exit 0
core=$(printf '%s\n' "$recent" | CORE_LIST="$list" awk '
  BEGIN { n = split(ENVIRON["CORE_LIST"], E, "\n") }
  {
    for (i = 1; i <= n; i++) {
      e = E[i]; if (e == "") continue
      if (substr(e, length(e)) == "/") { if (index($0, e) == 1) { print; break } }
      else if ($0 == e) { print; break }
    }
  }')
if [ -n "$core" ]; then
  printf '%s\n' "$core" >> "$STATE/core-touched"
  : > "$P/touched-core"
fi

exit 0
