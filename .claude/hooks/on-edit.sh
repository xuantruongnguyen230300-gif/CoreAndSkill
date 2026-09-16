#!/usr/bin/env bash
# on-edit.sh — hook PostToolUse (Edit|Write|NotebookEdit|Bash|PowerShell).
#
# Việc duy nhất: GHI DẤU. KHÔNG chạy cổng ở đây — cổng chạy một lần ở hook Stop.
#
# Ba dấu, đều ở .claude/.state/ (bị gitignore):
#   changed       file thuộc khu cần canh vừa đổi -> hook Stop chạy cổng tài liệu
#   src-touched   src/ vừa đổi                    -> hook Stop nhắc cổng build/test
#   core-touched  danh sách file chạm Core        -> hook Stop đòi core-reviewer
# Hai dấu sau chỉ ghi khi src/ tồn tại.
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
# được ghi, và cổng ở hook Stop không bao giờ chạy.
export LC_ALL=en_US.UTF-8
cd "$(dirname "$0")/../.." || exit 0

STATE=".claude/.state"
mkdir -p "$STATE" 2>/dev/null || exit 0

payload=$(cat)

# Cat bo phan `tool_response` TRUOC khi trich bat cu thu gi. Payload cua hook
# chua ca ket qua tra ve cua cong cu, va ket qua do co the chua nguyen van
# nhung chuoi ma cac mau duoi day dang tim. Moi mau BRE deu tham lam, nen
# `.*"file_path"` bam vao lan xuat hien CUOI CUNG trong ca payload.
payload=${payload%%'"tool_response"'*}

f=$(printf '%s' "$payload" | sed -n 's/.*"file_path"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -1)
# NotebookEdit khai `notebook_path`, KHÔNG khai `file_path`.
[ -z "$f" ] && f=$(printf '%s' "$payload" | sed -n 's/.*"notebook_path"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -1)

# ---------------------------------------------------------------- chuẩn hoá đường dẫn
# Harness gửi đường dẫn tuyệt đối dạng Windows, gạch chéo ngược nhân đôi trong
# JSON: "D:\\Manager\\CoreAndSkill\\docs\\x.md". So khớp trên chuỗi đó mà không
# chuẩn hoá thì khớp nhầm cả file NGOÀI repo có "docs/" trong đường dẫn, và
# không bỏ được file bị gitignore vì `git check-ignore` cần đường dẫn tương đối.
REL=""
to_rel() {
  local p="$1" low r root
  while :; do case "$p" in *//*) p="${p//\/\//\/}" ;; *) break ;; esac; done
  while :; do case "$p" in ./*) p="${p#./}" ;; *) break ;; esac; done
  case "$p" in
    /*|[A-Za-z]:/*) ;;
    *) REL="$p"; return 0 ;;
  esac
  # Windows không phân biệt hoa thường, và ký tự ổ đĩa có lúc viết thường.
  low=$(printf '%s' "$p" | tr 'A-Z' 'a-z')
  for root in "$ROOT_W" "$ROOT_U"; do
    [ -n "$root" ] || continue
    r=$(printf '%s' "$root" | tr 'A-Z' 'a-z')
    case "$low" in "$r"/*) REL="${p:$((${#root}+1))}"; return 0 ;; esac
  done
  return 1
}

in_zone() {
  # $1 = đường dẫn tương đối (REL) hoặc rỗng; $2 = đường dẫn gốc đã chuẩn hoá gạch chéo
  if [ -n "$1" ]; then
    case "$1" in docs/*|.claude/*|spec/*|src/*) return 0 ;; esac
    return 1
  fi
  case "$2" in */docs/*|*/.claude/*|*/spec/*|*/src/*) return 0 ;; esac
  return 1
}

mode=""
if [ -n "$f" ]; then
  mode=edit
  BSL=$(printf '\134')
  # `tr` thay TUNG ky tu nen xu ly duoc ca dang nhan doi lan dang don. Truyen
  # "$BSL$BSL": mot backslash don o cuoi tap ky tu lam tr canh bao.
  f=$(printf '%s' "$f" | tr "$BSL$BSL" "//")
  ROOT_U=$(pwd)
  ROOT_W=$(pwd -W 2>/dev/null || true)
  if to_rel "$f"; then
    in_zone "$REL" "$f" || exit 0
    # File bị gitignore (bin/, obj/, node_modules/, .claude/.state/...) không
    # phải tài liệu, không phải mã nguồn được commit: không có gì để canh.
    git check-ignore -q -- "$REL" 2>/dev/null && exit 0
  else
    # Đường dẫn tuyệt đối không nhận ra được là nằm dưới gốc repo (dạng tên
    # ngắn 8.3, tiền tố lạ...). Hỏng theo hướng AN TOÀN: khớp chuỗi con như cũ.
    # Ghi dấu thừa tốn ~2 giây chạy cổng; bỏ sót thì cổng biến mất im lặng.
    in_zone "" "$f" || exit 0
  fi
  : > "$STATE/changed"
else
  # Không có file_path nào để trích ⇒ đây là một lệnh shell.
  #
  # GHI DẤU KHÔNG ĐIỀU KIỆN, cố ý. Sửa file bằng lệnh shell không đi qua
  # Edit/Write; phân biệt lệnh đọc với lệnh ghi cần parse dòng lệnh, mà parse sai
  # theo chiều "không phải lệnh ghi" thì cổng biến mất im lặng.
  case "$payload" in *'"command"'*) ;; *) exit 0 ;; esac
  mode=shell
  : > "$STATE/changed"
fi

# ---------------------------------------------------------------- chỉ từ giai đoạn 2
# Dấu src-touched và core-touched chỉ có nghĩa khi có code. Giai đoạn 1 dừng ở
# đây — parser khối Core không chạy, nên một khối đang được viết dở ở docs/
# không ảnh hưởng gì tới lượt nào.
[ -d src ] || exit 0

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

if [ "$mode" = edit ]; then
  if [ -n "$REL" ]; then
    case "$REL" in src/*) : > "$STATE/src-touched" ;; esac
  else
    case "$f" in */src/*) : > "$STATE/src-touched" ;; esac
  fi
  [ -n "$list" ] || exit 0
  hit=0
  while IFS= read -r e; do
    [ -z "$e" ] && continue
    if [ -n "$REL" ]; then
      case "$e" in
        */) case "$REL" in "$e"*) hit=1 ;; esac ;;
        *)  [ "$REL" = "$e" ] && hit=1 ;;
      esac
    else
      case "$f" in *"/$e"*) hit=1 ;; esac
    fi
    [ "$hit" -eq 1 ] && break
  done <<< "$list"
  [ "$hit" -eq 1 ] && printf '%s\n' "${REL:-$f}" >> "$STATE/core-touched"
  exit 0
fi

# ---------------------------------------------------------------- lệnh shell
# Không đoán từ chuỗi lệnh. Hỏi HỆ THỐNG TỆP xem file nào VỪA THAY ĐỔI: không
# parse gì, không nhầm lệnh đọc với lệnh ghi, và dấu ghi ra là đường dẫn thật.
#
# Tên thư mục cần tỉa đọc từ chính .gitignore (dòng dạng "ten/"), không chép
# danh sách thứ hai. Tỉa chỉ để `find` không đi qua node_modules — đúng/sai
# cuối cùng vẫn do `git check-ignore` quyết ở dưới.
prune=()
if [ -f .gitignore ]; then
  while IFS= read -r g; do
    g="${g%$'\r'}"
    case "$g" in */) n="${g%/}" ;; *) continue ;; esac
    case "$n" in ''|*/*|*[*?\[]*|'#'*|'!'*) continue ;; esac
    prune+=( -name "$n" -o )
  done < .gitignore
fi

roots=(src)
while IFS= read -r e; do
  [ -z "$e" ] && continue
  case "$e" in src/*) continue ;; esac
  [ -e "${e%/}" ] && roots+=( "${e%/}" )
done <<< "$list"

recent=$(find "${roots[@]}" \( "${prune[@]}" -false \) -prune -o -type f -newermt '-90 seconds' -print 2>/dev/null | head -200)
[ -n "$recent" ] || exit 0

printf '%s\n' "$recent" | git check-ignore --stdin 2>/dev/null > "$STATE/ignored.tmp"
# So theo TÊN FILE, không theo `NR == FNR`: khi không file nào bị gitignore thì
# ignored.tmp rỗng, `NR == FNR` đúng luôn cho stdin, và MỌI file vừa sửa bị coi
# là "bị gitignore" — dấu không bao giờ được ghi. Bắt được bằng test payload.
recent=$(printf '%s\n' "$recent" | awk 'FILENAME == ARGV[1] { ig[$0] = 1; next } !($0 in ig)' "$STATE/ignored.tmp" -)
rm -f "$STATE/ignored.tmp"
[ -n "$recent" ] || exit 0

case "$recent" in src/*|*$'\n'src/*) : > "$STATE/src-touched" ;; esac

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
[ -n "$core" ] && printf '%s\n' "$core" >> "$STATE/core-touched"

exit 0
