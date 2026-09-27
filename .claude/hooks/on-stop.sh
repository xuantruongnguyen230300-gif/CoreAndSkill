#!/usr/bin/env bash
# on-stop.sh — hook Stop. Chạy khi agent kết thúc lượt.
#
# Đây là chỗ cổng được cưỡng chế THẬT. Hook do harness chạy, nó không quên.
#
# Thứ tự, mỗi bước chỉ chạy khi bước trước không chặn:
#   0. còn agent nền đang chạy  -> thoát, không làm gì: phiên đang CHỜ, chưa dừng
#   1. cổng tài liệu            -> chỉ chạy khi có gì mới hơn lần xanh cuối;
#                                  đỏ -> chặn, lặp tới khi xanh
#   2. khối core-paths đọc lỗi  -> chặn một lần mỗi phiên (chỉ khi có src/)
#   3. phiên đã chạm Core       -> đòi core-reviewer (chỉ khi có src/)
#   4. sửa src/                 -> NHẮC cổng build/test, không chặn; CI chặn
#
# Dấu chung ở .claude/.state/, dấu riêng của phiên ở .claude/.state/phien/<phiên>/
# — xem đầu on-edit.sh.
#
# Không phụ thuộc `jq`. Luôn in JSON hợp lệ ra stdout khi muốn chặn lượt.

set -uo pipefail
export LC_ALL=en_US.UTF-8
cd "$(dirname "$0")/../.." || exit 0

STATE=".claude/.state"
payload=$(cat 2>/dev/null || printf '{}')

# Ky tu dieu khien THO trong chuoi JSON la khong hop le (RFC 8259). Harness tu
# choi parse thi quyet dinh `block` bi bo — tuc LUOT KET THUC TRONG KHI CONG
# DANG DO. Repo co file .md dung CRLF, va thong diep nhung noi dung lay tu file.
json_escape() { printf '%s' "$1" | tr -d '\015' | tr '\011' ' ' | sed 's|\\|\\\\|g; s|"|\\"|g' | sed ':a;N;$!ba;s|\n|\\n|g'; }
block() { printf '{"decision":"block","reason":"%s"}\n' "$(json_escape "$1")"; }

# Harness dat co nay khi hook Stop da chan mot lan trong chuoi hien tai. CHI dung
# no cho nhung lan chan ma agent KHONG tu sua duoc (rc=2, khoi core-paths loi).
# Cong do la mot su that, khong phai loi nhac — no chan lai du co da bat.
REPEAT=0
case "$payload" in
  *'"stop_hook_active":true'*|*'"stop_hook_active": true'*) REPEAT=1 ;;
esac

# Thư viện hỏng thì mọi bước dưới đây mất — phải nói ra, không im lặng thoát 0.
if ! . .claude/hooks/lib.sh 2>/dev/null; then
  [ "$REPEAT" -eq 1 ] && exit 0
  block "Hook Stop không nạp được .claude/hooks/lib.sh — lượt này cổng tài liệu và hàng rào core-reviewer KHÔNG chạy. Báo người dùng; đừng sửa hook để qua."
  exit 0
fi

mkdir -p "$STATE" 2>/dev/null || exit 0
P=$(printf '%s' "$payload" | phien_dir)
mkdir -p "$P" 2>/dev/null || exit 0
touch "$P" 2>/dev/null

# ---------------------------------------------------------------- 0. agent nền
# Phiên chính kết thúc lượt trong lúc agent nền còn chạy là đang CHỜ thông báo,
# chưa dừng hẳn. Chặn lúc này chỉ ép nó hỏi thăm agent hay ngủ chờ rồi thử dừng
# lại — vòng lặp tốn nhất của hook cũ. Mọi dấu giữ nguyên; lần kết thúc lượt đầu
# tiên không còn agent nền nào chạy sẽ kiểm đủ. Payload không có danh sách hoặc
# đọc không ra thì coi như không có agent nền: hỏng theo hướng chặn.
if printf '%s' "$payload" | nen_dang_chay; then
  exit 0
fi

# Dọn thư mục phiên không hoạt động quá 7 ngày. Mỗi hook ghé qua đều chạm mtime
# thư mục phiên, nên một phiên còn sống không bao giờ bị dọn.
find "$STATE/phien" -mindepth 1 -maxdepth 1 -type d -mtime +7 -exec rm -rf {} + 2>/dev/null

# ---------------------------------------------------------------- 1. cổng tài liệu
# Chạy khi chưa từng xanh, hoặc khi có tệp/thư mục mới hơn lần xanh cuối — hỏi
# hệ thống tệp, không đoán từ chuỗi lệnh. Mốc xanh đặt TRƯỚC khi cổng chạy: file
# sửa trong lúc cổng đang chạy vẫn mới hơn mốc, nên lần sau chạy lại.
need=0
if [ ! -e "$STATE/gate-green" ] || thay_doi_tu "$STATE/gate-green"; then need=1; fi
# Cổng không chạy được (rc=2) đã báo cho phiên này, và cây chưa đổi từ đó: không
# chạy lại, không báo lại — chặn lặp vì một điều kiện agent không tự sửa được là
# vòng không đáy.
if [ "$need" -eq 1 ] && [ -e "$P/gate-unrunnable" ] && ! thay_doi_tu "$P/gate-unrunnable"; then need=0; fi

if [ "$need" -eq 1 ]; then
  tmp="$STATE/gate-green.tmp.$$"
  : > "$tmp"
  gate_out=$(bash .claude/check-docs.sh 2>&1)
  gate_rc=$?
  if [ "$gate_rc" -eq 0 ]; then
    mv -f "$tmp" "$STATE/gate-green"
    rm -f "$P/gate-unrunnable"
  else
    rm -f "$tmp"
  fi

  if [ "$gate_rc" -eq 2 ]; then
    # rc=2 la "cong KHONG CHAY DUOC" (sai thu muc, cay repo thieu) — dieu kien agent
    # khong tu sua duoc. Chan lan dau de noi ra; lan hai tro di la vong khong day.
    if [ "$REPEAT" -eq 1 ]; then
      : > "$P/gate-unrunnable"
      exit 0
    fi
    raw=$(printf '%s' "$gate_out" | sed 's/\x1b\[[0-9;]*m//g' | head -20)
    block "Cổng tài liệu KHÔNG CHẠY ĐƯỢC (mã thoát 2) — đây KHÔNG phải vi phạm tài liệu.

$raw

Đọc nguyên văn ở trên: thường là chạy sai thư mục, hoặc cây repo không đầy đủ. Đừng đi sửa tài liệu."
    exit 0
  fi

  if [ "$gate_rc" -ne 0 ]; then
    fails=$(printf '%s' "$gate_out" | sed 's/\x1b\[[0-9;]*m//g' | grep 'FAIL' | head -20)
    # Khong bao gio gui di mot danh sach trong.
    [ -z "$fails" ] && fails=$(printf '%s' "$gate_out" | head -20)
    block "Cổng tài liệu ĐỎ — không được kết thúc lượt khi cổng chưa xanh.

$fails

Sửa các vi phạm trên rồi chạy lại: bash .claude/check-docs.sh
Nếu một vi phạm là báo sai, ĐỪNG nới luật cho cổng xanh — sửa phép dò bằng một dấu hiệu máy đọc được, và ghi lý do."
    exit 0
  fi
fi

# Lời nhắc cổng build/test — gắn vào bất kỳ thông điệp nào in ra từ đây trở đi.
reminder=""
if [ -f "$P/src-touched" ]; then
  reminder="Lượt này đã sửa src/. Hook KHÔNG chạy cổng build/test — chạy đủ các lệnh ở mục \"Trước khi coi việc là xong\" trong file agent thi công của khu vừa sửa. CI chặn khi cổng đỏ."
fi
with_reminder() {
  if [ -n "$reminder" ]; then
    printf '%s\n\n%s' "$1" "$reminder"
    rm -f "$P/src-touched"
  else
    printf '%s' "$1"
  fi
}

# ---------------------------------------------------------------- 2. khối core-paths lỗi
# on-edit.sh ghi lý do vào dấu chung này khi không đọc được khối. Khi đó lượt này
# KHÔNG được kiểm là có chạm Core hay không — phải nói ra, không im lặng.
# Dấu `.reported` của PHIÊN giữ "đã báo lỗi này cho phiên này"; on-edit viết lại
# dấu lỗi mỗi lần đọc hỏng, nên lỗi còn nguyên thì lượt người dùng sau vẫn được báo.
if [ -d src ] && [ -s "$STATE/core-paths-error" ] && [ "$REPEAT" -eq 0 ]; then
  if [ ! -e "$P/core-paths-error.reported" ] || [ "$STATE/core-paths-error" -nt "$P/core-paths-error.reported" ]; then
    : > "$P/core-paths-error.reported"
    err=$(head -5 "$STATE/core-paths-error")
    block "$(with_reminder "Hook không đọc được khối đường dẫn chạm Core — lượt này KHÔNG được kiểm là có chạm Core hay không.

$err

Khối nằm ở docs/kien-truc-core-module.md, giữa hai mốc core-paths:begin và core-paths:end. Sửa khối cho đúng hợp đồng định dạng ghi ở đầu .claude/hooks/core-paths.sh. Nếu khối đúng mà vẫn lỗi thì lỗi nằm ở hook — báo người dùng, đừng sửa hook để qua.")"
    exit 0
  fi
fi

# ---------------------------------------------------------------- 3. chạm Core
# Nhật ký core-touched là CHUNG (review phủ cả cây); chỉ phiên mang cờ
# touched-core mới bị chặn — phiên không đụng Core không bị việc của phiên khác
# giữ lại. Việc Core dở dang của một phiên đã chết vẫn nằm trong nhật ký, nên
# phiên kế tiếp chạm Core phải review cả phần đó.
if [ ! -d src ]; then
  # Giai đoạn 1: không có code để core-reviewer đối chiếu. Không nhắc.
  rm -f "$STATE/core-touched" "$P/touched-core"
elif [ -e "$P/touched-core" ]; then
  if [ ! -s "$STATE/core-touched" ]; then
    # Nhật ký đã được dọn — một lượt review mới hơn mọi file, hoặc người dùng tự xoá.
    rm -f "$P/touched-core"
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
3. Không sửa gì trong lúc nó chạy.
Gọi nó chạy nền rồi kết thúc lượt: hook không chặn khi còn agent nền đang chạy."

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
          # Đường dẫn không còn trên đĩa: file (hoặc cả thư mục chứa nó) đã bị xoá,
          # hoặc là một dòng hỏng chưa bao giờ là đường dẫn thật. Không còn mtime
          # của chính nó. Mốc thay thế: mtime của THƯ MỤC TỔ TIÊN GẦN NHẤT CÒN TỒN
          # TẠI — xoá một mục con cập nhật mtime thư mục chứa nó, nên mốc đó không
          # sớm hơn lúc xoá. Dấu review mới hơn hẳn mốc đó => việc xoá xảy ra trước
          # lượt review. Mốc chỉ có thể MUỘN hơn lúc xoá (thư mục còn đổi vì việc
          # khác), tức hỏng theo hướng chặn thừa, không bao giờ theo hướng bỏ sót.
          #
          # KHÔNG so với mtime của chính core-touched: đó là lúc ghi dòng mới nhất
          # của BẤT KỲ file nào, không phải lúc file này biến mất.
          d=$p
          while :; do
            case "$d" in */*) d=${d%/*} ;; *) d=. ;; esac
            [ -n "$d" ] || d=/
            [ -d "$d" ] && break
          done
          [ "$STATE/core-reviewed" -nt "$d" ] || pending="$pending$p"$'\n'
        fi
      done < <(LC_ALL=C sort -u "$STATE/core-touched")

      if [ -z "$pending" ]; then
        rm -f "$STATE/core-touched" "$P/touched-core"
      else
        diag=""
        [ -s "$STATE/subagent-stop-error.log" ] && diag="

Đã chạy core-reviewer mà vẫn bị chặn: đọc .claude/.state/subagent-stop-error.log — hook SubagentStop không ghi được dấu review. Báo người dùng."
        block "$(with_reminder "Core đã đổi sau lượt core-reviewer gần nhất. Hook chặn mỗi lần phiên kết thúc lượt mà không còn agent nền nào chạy, cho tới khi có một lượt review mới hơn các file dưới đây.

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
      rm -f "$STATE/core-touched" "$P/touched-core"
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
  rm -f "$P/src-touched"
  printf '{"systemMessage":"%s"}\n' "$(json_escape "$reminder")"
  exit 0
fi

exit 0
