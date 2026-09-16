#!/usr/bin/env bash
# on-stop.sh — hook Stop. Chạy khi agent kết thúc lượt.
#
# Đây là chỗ cổng được cưỡng chế THẬT. Hook do harness chạy, nó không quên.
#
# Thứ tự, mỗi bước chỉ chạy khi bước trước không chặn:
#   1. cổng tài liệu            đỏ -> chặn, lặp tới khi xanh
#   2. khối core-paths đọc lỗi  -> chặn một lần (chỉ khi có src/)
#   3. chạm Core                -> đòi core-reviewer (chỉ khi có src/)
#   4. sửa src/                 -> NHẮC cổng build/test, không chặn; CI chặn
#
# Không phụ thuộc `jq`. Luôn in JSON hợp lệ ra stdout khi muốn chặn lượt.

set -uo pipefail
export LC_ALL=en_US.UTF-8
cd "$(dirname "$0")/../.." || exit 0

STATE=".claude/.state"
payload=$(cat 2>/dev/null || printf '{}')

# Harness dat co nay khi hook Stop da chan mot lan trong chuoi hien tai. CHI dung
# no cho nhung lan chan ma agent KHONG tu sua duoc (rc=2, khoi core-paths loi).
# Cong do la mot su that, khong phai loi nhac — no chan lai du co da bat.
REPEAT=0
case "$payload" in
  *'"stop_hook_active":true'*|*'"stop_hook_active": true'*) REPEAT=1 ;;
esac

# Không sửa gì thuộc khu cần canh thì không có việc gì để làm.
[ -f "$STATE/changed" ] || exit 0

# Ky tu dieu khien THO trong chuoi JSON la khong hop le (RFC 8259). Harness tu
# choi parse thi quyet dinh `block` bi bo — tuc LUOT KET THUC TRONG KHI CONG
# DANG DO. Repo co file .md dung CRLF, va thong diep nhung noi dung lay tu file.
json_escape() { printf '%s' "$1" | tr -d '\015' | tr '\011' ' ' | sed 's|\\|\\\\|g; s|"|\\"|g' | sed ':a;N;$!ba;s|\n|\\n|g'; }
block() { printf '{"decision":"block","reason":"%s"}\n' "$(json_escape "$1")"; }

# ---------------------------------------------------------------- 1. cổng tài liệu
gate_out=$(bash .claude/check-docs.sh 2>&1)
gate_rc=$?

if [ "$gate_rc" -eq 2 ]; then
  # rc=2 la "cong KHONG CHAY DUOC" (sai thu muc, cay repo thieu) — dieu kien agent
  # khong tu sua duoc. Chan lan dau de noi ra; lan hai tro di la vong khong day.
  if [ "$REPEAT" -eq 1 ]; then
    rm -f "$STATE/changed"
    exit 0
  fi
  raw=$(printf '%s' "$gate_out" | sed 's/\x1b\[[0-9;]*m//g' | head -20)
  block "Cổng tài liệu KHÔNG CHẠY ĐƯỢC (mã thoát 2) — đây KHÔNG phải vi phạm tài liệu.

$raw

Đọc nguyên văn ở trên: thường là chạy sai thư mục, hoặc cây repo không đầy đủ. Đừng đi sửa tài liệu."
  exit 0
fi

if [ "$gate_rc" -ne 0 ]; then
  # Giữ nguyên dấu: lượt sau vẫn phải chạy lại cổng cho tới khi xanh.
  fails=$(printf '%s' "$gate_out" | sed 's/\x1b\[[0-9;]*m//g' | grep 'FAIL' | head -20)
  # Khong bao gio gui di mot danh sach trong.
  [ -z "$fails" ] && fails=$(printf '%s' "$gate_out" | head -20)
  block "Cổng tài liệu ĐỎ — không được kết thúc lượt khi cổng chưa xanh.

$fails

Sửa các vi phạm trên rồi chạy lại: bash .claude/check-docs.sh
Nếu một vi phạm là báo sai, ĐỪNG nới luật cho cổng xanh — sửa phép dò bằng một dấu hiệu máy đọc được, và ghi lý do."
  exit 0
fi

# Lời nhắc cổng build/test — gắn vào bất kỳ thông điệp nào in ra từ đây trở đi.
reminder=""
if [ -f "$STATE/src-touched" ]; then
  reminder="Lượt này đã sửa src/. Hook KHÔNG chạy cổng build/test — chạy đủ các lệnh ở mục \"Trước khi coi việc là xong\" trong file agent thi công của khu vừa sửa. CI chặn khi cổng đỏ."
fi
with_reminder() {
  if [ -n "$reminder" ]; then
    printf '%s\n\n%s' "$1" "$reminder"
    rm -f "$STATE/src-touched"
  else
    printf '%s' "$1"
  fi
}

# ---------------------------------------------------------------- 2. khối core-paths lỗi
# on-edit.sh ghi lý do vào dấu này khi không đọc được khối. Khi đó lượt này
# KHÔNG được kiểm là có chạm Core hay không — phải nói ra, không im lặng.
# Dấu `.reported` giữ "đã báo lỗi này"; on-edit viết lại dấu lỗi mỗi lần đọc
# hỏng, nên lỗi còn nguyên thì lượt người dùng sau vẫn được báo lại.
if [ -d src ] && [ -s "$STATE/core-paths-error" ] && [ "$REPEAT" -eq 0 ]; then
  if [ ! -e "$STATE/core-paths-error.reported" ] || [ "$STATE/core-paths-error" -nt "$STATE/core-paths-error.reported" ]; then
    : > "$STATE/core-paths-error.reported"
    err=$(head -5 "$STATE/core-paths-error")
    block "$(with_reminder "Hook không đọc được khối đường dẫn chạm Core — lượt này KHÔNG được kiểm là có chạm Core hay không.

$err

Khối nằm ở docs/kien-truc-core-module.md, giữa hai mốc core-paths:begin và core-paths:end. Sửa khối cho đúng hợp đồng định dạng ghi ở đầu .claude/hooks/core-paths.sh. Nếu khối đúng mà vẫn lỗi thì lỗi nằm ở hook — báo người dùng, đừng sửa hook để qua.")"
    exit 0
  fi
fi

# ---------------------------------------------------------------- 3. chạm Core
# `-s` chứ không `-f`: chỉ xét khi dấu THẬT SỰ có nội dung — một lời nhắc với
# danh sách file trống thì không ai kiểm lại được, và lần sau bị bỏ qua.
if [ -s "$STATE/core-touched" ]; then
  if [ ! -d src ]; then
    # Giai đoạn 1: không có code để core-reviewer đối chiếu. Không nhắc.
    rm -f "$STATE/core-touched"
  else
    # LC_ALL=C: so sanh theo byte, khong bao gio thoat loi vi ky tu khong hop le.
    files=$(LC_ALL=C sort -u "$STATE/core-touched" 2>&1 | head -15)
    if [ -z "$files" ]; then
      raw=$(od -c "$STATE/core-touched" 2>/dev/null | head -8)
      files="(buoc loc tra ve rong trong khi dau co $(wc -c < "$STATE/core-touched") byte — day la LOI CUA HOOK.
Byte tho de chan doan:
$raw
Hay bao lai cho nguoi dung thay vi bo qua.)"
    fi

    # Chặn cứng (lặp tới khi có dấu review) CHỈ khi hook SubagentStop ghi dấu
    # đó đã được gắn vào settings.json. Chưa gắn mà chặn cứng thì không lượt
    # nào thoát ra được — không có gì ghi dấu review cả.
    HARD=0
    case "$(cat .claude/settings.json 2>/dev/null)" in
      *'"SubagentStop"'*on-subagent-stop.sh*) HARD=1 ;;
    esac

    how="Phiên chính gọi agent core-reviewer qua công cụ Agent, ba ràng buộc bắt buộc:
1. Mỗi lượt MỘT phạm vi — BE hoặc FE, không bao giờ cả hai. Chạm cả hai thì hai lượt.
2. KHÔNG gửi tóm tắt việc vừa làm — chỉ nói phạm vi cần soát.
3. Không sửa gì trong lúc nó chạy."

    if [ "$HARD" -eq 1 ]; then
      # "Đã review" chỉ khi dấu review MỚI HƠN HẲN file. Hai mốc bằng nhau
      # (cùng một tích tắc đồng hồ) tính là CHƯA review: bỏ sót một sửa đổi là
      # hỏng im lặng, còn chặn thừa một lần thì lượt review sau ghi dấu mới hơn.
      pending=""
      while IFS= read -r p; do
        [ -z "$p" ] && continue
        if [ ! -e "$STATE/core-reviewed" ]; then
          pending="$pending$p"$'\n'
        elif [ -e "$p" ]; then
          [ "$STATE/core-reviewed" -nt "$p" ] || pending="$pending$p"$'\n'
        else
          # File đã bị xoá: không còn mtime để so; dùng thời điểm ghi dấu.
          [ "$STATE/core-reviewed" -nt "$STATE/core-touched" ] || pending="$pending$p"$'\n'
        fi
      done < <(LC_ALL=C sort -u "$STATE/core-touched")

      if [ -z "$pending" ]; then
        rm -f "$STATE/core-touched"
      else
        diag=""
        [ -s "$STATE/subagent-stop-error.log" ] && diag="

Đã chạy core-reviewer mà vẫn bị chặn: đọc .claude/.state/subagent-stop-error.log — hook SubagentStop không ghi được dấu review. Báo người dùng."
        block "$(with_reminder "Core đã đổi sau lượt core-reviewer gần nhất. Hook sẽ chặn MỖI LẦN kết thúc lượt cho tới khi có một lượt review mới hơn các file dưới đây.

File chưa được review:
$(printf '%s' "$pending" | head -15)

$how

Không tự tạo, sửa hay xoá file nào trong .claude/.state/ để qua hook. Bỏ qua lượt review là quyết định của người dùng: người dùng tự xoá .claude/.state/core-touched.$diag")"
        exit 0
      fi
    else
      # Chưa gắn hook SubagentStop: nhắc đúng một lần. Xoá dấu TRƯỚC khi chặn —
      # KHÔNG dùng `stop_hook_active` làm bộ nhớ "đã nhắc", vì cờ đó bật sau BẤT
      # KỲ lần chặn nào, kể cả lần chặn vì cổng đỏ.
      rm -f "$STATE/core-touched" "$STATE/changed"
      block "$(with_reminder "Lượt này đã chạm tới Core (theo khối đường dẫn ở docs/kien-truc-core-module.md). Cổng tài liệu xanh, nhưng cổng chỉ bắt được thứ máy kiểm được — nó KHÔNG đọc hiểu nội dung.

File đã sửa:
$files

$how")"
      exit 0
    fi
  fi
fi

# ---------------------------------------------------------------- 4. nhắc build/test
# Chỉ NHẮC: cổng build/test mất hàng phút và cần môi trường đầy đủ; chặn lượt
# vì nó là biến hook thành thứ người ta tắt đi. CI là nơi chặn.
if [ -n "$reminder" ]; then
  rm -f "$STATE/src-touched" "$STATE/changed"
  printf '{"systemMessage":"%s"}\n' "$(json_escape "$reminder")"
  exit 0
fi

rm -f "$STATE/changed"
exit 0
