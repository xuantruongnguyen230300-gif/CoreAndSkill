#!/usr/bin/env bash
# on-stop.sh — hook Stop. Chạy khi agent kết thúc lượt.
#
# Đây là chỗ cổng được cưỡng chế THẬT. Hook do harness chạy, nó không quên.
#
# Thứ tự, mỗi bước chỉ chạy khi bước trước không chặn:
#   0. còn agent nền đang chạy  -> thoát, không làm gì: phiên đang CHỜ, chưa dừng
#   1. cổng tài liệu            -> chỉ chạy khi có gì mới hơn lần xanh cuối;
#                                  đỏ -> chặn MỘT lần mỗi tập lỗi mỗi phiên; dừng
#                                  lại mà tập lỗi y hệt -> không chặn, báo người
#                                  dùng, đi tiếp các bước dưới (ADR-0111 qđ 5)
#   2. khối core-paths đọc lỗi  -> chặn một lần mỗi phiên
#   3. phiên đã chạm Core       -> đòi core-reviewer: chặn MỘT lần mỗi tập tệp Core
#                                  chưa review mỗi phiên; dừng lại mà tập y hệt ->
#                                  không chặn, báo người dùng (ADR-0114 qđ 6, 13)
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

# Ký tự điều khiển THÔ trong chuỗi JSON là không hợp lệ (RFC 8259: U+0000–U+001F
# phải thoát). Harness từ chối parse thì quyết định `block` bị bỏ — tức LƯỢT KẾT
# THÚC TRONG KHI CỔNG ĐANG ĐỎ. Thông điệp nhúng nội dung lấy từ tệp và từ đầu ra
# cổng: CR của tệp CRLF, ESC lẻ không thành mã màu, byte rác trong nhật ký chạm Core.
# Tab thành dấu cách, xuống dòng thành \n, mọi byte điều khiển còn lại (0x00–0x08,
# 0x0B–0x1F) bị bỏ. LC_ALL=C: xử lý theo byte — byte dưới 0x20 không bao giờ nằm
# trong một ký tự UTF-8 nhiều byte, nên bỏ nó không cắt hỏng chữ có dấu.
json_escape() { printf '%s' "$1" | LC_ALL=C tr -d '\000-\010\013-\037' | LC_ALL=C tr '\011' ' ' | LC_ALL=C sed 's|\\|\\\\|g; s|"|\\"|g' | LC_ALL=C sed ':a;N;$!ba;s|\n|\\n|g'; }
# SYSMSG: lời báo NGƯỜI DÙNG gom từ các bước (cổng đỏ, Core chưa review — tập đã chặn
# một lần). Mọi JSON in ra từ đây mang nó theo trường systemMessage — kể cả lời chặn
# của bước sau.
SYSMSG=""
them_sysmsg() {
  if [ -n "$SYSMSG" ]; then
    SYSMSG="$SYSMSG

$1"
  else
    SYSMSG=$1
  fi
}
block() {
  if [ -n "$SYSMSG" ]; then
    printf '{"decision":"block","reason":"%s","systemMessage":"%s"}\n' "$(json_escape "$1")" "$(json_escape "$SYSMSG")"
  else
    printf '{"decision":"block","reason":"%s"}\n' "$(json_escape "$1")"
  fi
}

# Harness đặt cờ này khi hook Stop đã chặn một lần trong chuỗi hiện tại. CHỈ dùng
# nó cho những lần chặn mà agent KHÔNG tự sửa được (rc=2, khối core-paths lỗi).
# Cổng đỏ KHÔNG dùng cờ này: cờ bật sau BẤT KỲ lần chặn nào, nên nó không trả lời
# được "tập lỗi này đã chặn phiên này chưa" — dấu vân tay tập lỗi theo phiên (bước 1)
# trả lời được.
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
    rm -f "$P/gate-unrunnable" "$P/gate-fail-fp"
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
    # Mỗi lỗi là ĐÚNG MỘT dòng `FAIL [§N] …` — check-docs.sh gộp danh sách tệp, lý do
    # vào cùng dòng. Chỉ nhận dòng MỞ ĐẦU bằng FAIL: dòng OK/NOTE nhắc chữ "FAIL" không
    # phải lỗi. LC_ALL=C + grep -a: một byte UTF-8 hỏng (tên tệp lạ) làm grep ở locale
    # UTF-8 in "Binary file matches" thay cho chính các dòng lỗi.
    plain=$(printf '%s\n' "$gate_out" | LC_ALL=C sed 's/\x1b\[[0-9;]*m//g')
    fail_lines=$(printf '%s\n' "$plain" | LC_ALL=C grep -aE '^[[:space:]]*FAIL[[:space:]]')
    if [ -n "$fail_lines" ]; then
      nfail=$(printf '%s\n' "$fail_lines" | LC_ALL=C grep -ac .)
      fails=$(printf '%s\n' "$fail_lines" | head -20)
      [ "$nfail" -gt 20 ] && fails="$fails
(+$((nfail - 20)) lỗi nữa)"
      fp_src=$fail_lines
    else
      # Mã 1 mà không dòng FAIL nào: không bao giờ gửi đi một danh sách rỗng.
      fails=$(printf '%s\n' "$plain" | head -20)
      fp_src=$plain
    fi

    # Dấu vân tay TẬP lỗi của PHIÊN này (ADR-0111 quyết định 5): mỗi tập chặn một lần.
    # Lỗi do phiên khác hay người dùng đang sửa gây ra thì phiên này không sửa được —
    # chặn lặp vì nó là vòng không đáy, kể cả khi stop_hook_active đã bật. Băm TOÀN BỘ
    # tập (không chỉ 20 dòng in ra), sắp theo byte: thêm, bớt hay đổi một lỗi bất kỳ là
    # tập mới, chặn lại. Dấu chỉ còn giá trị khi MỚI HƠN HẲN mốc xanh: một lần xanh —
    # do phiên nào chạy cũng vậy — xảy ra sau lần chặn thì tập cũ quay lại là lỗi mới.
    # Hai mốc cùng tích tắc tính là cũ: hỏng theo hướng chặn thêm một lần.
    fp=$(printf '%s\n' "$fp_src" | LC_ALL=C sort | cksum)
    old_fp=""
    if [ -f "$P/gate-fail-fp" ] && { [ ! -e "$STATE/gate-green" ] || [ "$P/gate-fail-fp" -nt "$STATE/gate-green" ]; }; then
      old_fp=$(cat "$P/gate-fail-fp" 2>/dev/null)
    fi

    if [ -n "$old_fp" ] && [ "$old_fp" = "$fp" ]; then
      # Đã chặn tập này một lần: không chặn nữa, nhưng người dùng phải thấy. Đi tiếp
      # các bước dưới — lớp chặn core-reviewer vẫn xét như thường, và lời báo đi kèm
      # mọi JSON.
      them_sysmsg "Cổng tài liệu vẫn ĐỎ với đúng tập lỗi đã chặn một lần trong phiên này — hook không chặn lại (ADR-0111 quyết định 5), lượt kết thúc khi cổng còn đỏ.

$fails

Xem đủ: bash .claude/check-docs.sh. Job check-docs trên CI vẫn đỏ nếu lỗi còn tới lúc đẩy."
    else
      printf '%s' "$fp" > "$P/gate-fail-fp" 2>/dev/null
      block "Cổng tài liệu ĐỎ — sửa trước khi kết thúc lượt.

$fails

Sửa các vi phạm trên rồi chạy lại: bash .claude/check-docs.sh
Nếu một vi phạm là báo sai, ĐỪNG nới luật cho cổng xanh — sửa phép dò bằng một dấu hiệu máy đọc được, và ghi lý do.
Vi phạm nằm ở tệp mà phiên này không đụng tới (phiên khác, người dùng đang sửa): đừng sửa hộ — nói với người dùng. Mỗi tập lỗi chỉ chặn một lần mỗi phiên: dừng lại mà tập lỗi y hệt thì hook không chặn nữa, chỉ báo người dùng; có lỗi mới thì chặn lại."
      exit 0
    fi
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
if [ -s "$STATE/core-paths-error" ] && [ "$REPEAT" -eq 0 ]; then
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
if [ -e "$P/touched-core" ]; then
  if [ ! -s "$STATE/core-touched" ]; then
    # Nhật ký đã được dọn — một lượt review mới hơn mọi file, hoặc người dùng tự xoá.
    # Dấu vân tay đi theo: tập đã chặn không còn, lần chạm Core sau là việc mới.
    rm -f "$P/touched-core" "$P/core-review-fp"
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

    # Chặn cứng (theo tập tệp, dưới đây) CHỈ khi hook SubagentStop ghi dấu review
    # đã được gắn vào settings.json. Chưa gắn mà chặn cứng thì không lượt nào thoát
    # ra được — không có gì ghi dấu review cả.
    HARD=0
    case "$(cat .claude/settings.json 2>/dev/null)" in
      *'"SubagentStop"'*on-subagent-stop.sh*) HARD=1 ;;
    esac

    how="Phiên chính gọi /core-review (agent core-reviewer qua công cụ Agent). Lời giao đúng ba trường, không kèm gì khác: Phạm vi: BE|FE, phần đổi|toàn bộ. Vòng: n/2.
1. Mỗi lượt MỘT phạm vi — BE hoặc FE, không bao giờ cả hai. Chạm cả hai thì hai lượt.
2. KHÔNG gửi tóm tắt việc vừa làm hay danh sách tệp — chỉ ba trường trên.
3. Không sửa gì trong phạm vi đang soát lúc nó chạy.
4. Phạm vi đã đủ hai vòng (2/2) thì KHÔNG gọi vòng ba: hỏi người dùng — sửa tiếp, chuyển thành issue hay chấp nhận — rồi dừng lại.
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
        rm -f "$STATE/core-touched" "$P/touched-core" "$P/core-review-fp"
      else
        pend_show=$(printf '%s' "$pending" | head -15)
        npend=$(printf '%s' "$pending" | LC_ALL=C grep -ac .)
        [ "$npend" -gt 15 ] && pend_show="$pend_show
(+$((npend - 15)) tệp nữa)"
        diag=""
        [ -s "$STATE/subagent-stop-error.log" ] && diag="

Đã chạy core-reviewer mà vẫn còn tệp chưa review: đọc .claude/.state/subagent-stop-error.log — hook SubagentStop không ghi được dấu review. Báo người dùng."

        # Dấu vân tay TẬP tệp Core chưa review của PHIÊN này (ADR-0114 quyết định 6,
        # 13): mỗi tập chặn một lần. Băm ĐƯỜNG DẪN, không băm mtime — sửa thêm một tệp
        # đã có trong tập không làm tập mới; thêm một tệp là tập mới, chặn lại. Băm
        # TOÀN BỘ tập (không chỉ 15 dòng in ra), sắp theo byte. Dấu chỉ còn giá trị
        # khi MỚI HƠN HẲN dấu core-reviewed: một lượt review — dù vòng nào — xảy ra sau
        # lần chặn thì tập còn lại là việc của vòng kế, chặn lại. Hai mốc cùng tích
        # tắc tính là cũ: hỏng theo hướng chặn thêm một lần. Không dùng
        # stop_hook_active: cờ đó bật sau BẤT KỲ lần chặn nào, kể cả của cổng tài liệu.
        cfp=$(printf '%s' "$pending" | LC_ALL=C sort -u | cksum)
        old_cfp=""
        if [ -f "$P/core-review-fp" ] && { [ ! -e "$STATE/core-reviewed" ] || [ "$P/core-review-fp" -nt "$STATE/core-reviewed" ]; }; then
          old_cfp=$(cat "$P/core-review-fp" 2>/dev/null)
        fi

        if [ -n "$old_cfp" ] && [ "$old_cfp" = "$cfp" ]; then
          # Đã chặn tập này một lần: không đòi vòng review nữa — có thể phạm vi đã đủ
          # hai vòng (.claude/CLAUDE.md §11). Người dùng phải thấy; nhật ký và cờ giữ
          # nguyên, nên phiên kế tiếp chạm Core gặp lại tập này. Đi tiếp bước 4.
          them_sysmsg "Core vẫn còn tệp chưa qua core-reviewer — đúng tập đã chặn một lần trong phiên này; hook không chặn lại (ADR-0114 quyết định 6), lượt kết thúc khi Core chưa review.

File chưa được review:
$pend_show

Soát: /core-review BE hoặc /core-review FE. Bỏ qua review là quyết định của người dùng: xoá .claude/.state/core-touched.$diag"
        else
          printf '%s' "$cfp" > "$P/core-review-fp" 2>/dev/null
          block "$(with_reminder "Core đã đổi sau lượt core-reviewer gần nhất. Hook chặn MỘT lần cho tập tệp dưới đây trong phiên này: dừng lại với đúng tập này thì hook không chặn nữa, chỉ báo người dùng; có tệp Core mới chưa review, hoặc có lượt review mới rồi sửa tiếp, thì chặn lại.

File chưa được review:
$pend_show

$how

Không tự tạo, sửa hay xoá file nào trong .claude/.state/ để qua hook. Bỏ qua lượt review là quyết định của người dùng: người dùng tự xoá .claude/.state/core-touched.$diag")"
          exit 0
        fi
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
  msg=$reminder
  [ -n "$SYSMSG" ] && msg="$SYSMSG

$reminder"
  printf '{"systemMessage":"%s"}\n' "$(json_escape "$msg")"
  exit 0
fi

# Cổng đỏ với tập lỗi đã chặn một lần: không chặn, nhưng không im lặng.
if [ -n "$SYSMSG" ]; then
  printf '{"systemMessage":"%s"}\n' "$(json_escape "$SYSMSG")"
fi

exit 0
