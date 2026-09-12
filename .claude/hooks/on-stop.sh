#!/usr/bin/env bash
# on-stop.sh — hook Stop. Chạy khi agent kết thúc lượt.
#
# Đây là chỗ cổng được cưỡng chế THẬT. Trước đó, việc chạy cổng và việc gọi
# core-reviewer phụ thuộc vào agent NHỚ làm — mà đó lại đúng là thứ tầng kiểm
# tra sinh ra để thay thế. Hook do harness chạy, nó không quên.
#
# Không phụ thuộc `jq`. Luôn in JSON hợp lệ ra stdout khi muốn chặn lượt.

set -uo pipefail
export LC_ALL=en_US.UTF-8
cd "$(dirname "$0")/../.." || exit 0

STATE=".claude/.state"
payload=$(cat 2>/dev/null || printf '{}')

# Chong lap vo han: harness dat co nay khi hook Stop da chan mot lan.
#
# Ban truoc `exit 0` NGAY tai day — tuc bo qua CA CONG, khong chi bo qua loi
# nhac. Hau qua: luot 1 cong do -> chan -> agent sua (co the sua sai) -> dung
# luot -> co bat -> hook thoat -> LUOT KET THUC VOI CONG DANG DO. Cau "khong
# the ket thuc mot luot voi cong dang do" sai o dung lan quan trong nhat.
#
# Cong do KHONG phai mot loi nhac, no la mot su that. Loi nhac chan mot lan la
# du; su that thi khong.
REPEAT=0
case "$payload" in
  *'"stop_hook_active":true'*|*'"stop_hook_active": true'*) REPEAT=1 ;;
esac

# Không sửa gì thuộc khu cần canh thì không có việc gì để làm.
[ -f "$STATE/changed" ] || exit 0

# Ky tu dieu khien THO trong chuoi JSON la khong hop le (RFC 8259). Harness tu
# choi parse thi quyet dinh `block` bi bo — tuc LUOT KET THUC TRONG KHI CONG
# DANG DO, dung dieu CLAUDE.md §8 khang dinh khong the xay ra. Repo co 26 file
# .md dung CRLF, va nhieu thong diep bad() nhung noi dung lay tu dong file.
json_escape() { printf '%s' "$1" | tr -d '\015' | tr '\011' ' ' | sed 's|\\|\\\\|g; s|"|\\"|g' | sed ':a;N;$!ba;s|\n|\\n|g'; }

# ---------------------------------------------------------------- cổng tài liệu
gate_out=$(bash .claude/check-docs.sh 2>&1)
gate_rc=$?

if [ "$gate_rc" -eq 2 ]; then
  # rc=2 phan lon la dieu kien agent KHONG tu sua duoc (sai thu muc, cay repo
  # thieu file moc). Chan lan dau de noi ra; chan lan hai tro di la vong khong
  # day, va nguoi dung khong thoat duoc bang agent vi `git restore`/`checkout`
  # nam trong permissions.deny. Ly do bien minh o dau file ("cong do la mot su
  # that") KHONG ap cho rc=2: day khong phai su that ve tai lieu.
  if [ "$REPEAT" -eq 1 ]; then
    rm -f "$STATE/changed"
    exit 0
  fi
  # rc=2 la "cong KHONG CHAY DUOC" (sai thu muc, cay repo thieu) — khac han
  # rc=1 la "cong chay va tim thay vi pham". Thong diep ABORT khong chua chu
  # FAIL, nen ban truoc loc ra RONG roi gui cho agent mot danh sach trong kem
  # cau "sua cac vi pham tren": khong co vi pham nao o tren de sua, va nguyen
  # nhan that thi bi loc mat.
  raw=$(printf '%s' "$gate_out" | sed 's/\x1b\[[0-9;]*m//g' | head -20)
  reason="Cổng tài liệu KHÔNG CHẠY ĐƯỢC (mã thoát 2) — đây KHÔNG phải vi phạm tài liệu.

$raw

Đọc nguyên văn ở trên: thường là chạy sai thư mục, hoặc cây repo không đầy đủ. Đừng đi sửa tài liệu."
  printf '{"decision":"block","reason":"%s"}\n' "$(json_escape "$reason")"
  exit 0
fi

if [ "$gate_rc" -ne 0 ]; then
  # Giữ nguyên dấu: lượt sau vẫn phải chạy lại cổng cho tới khi xanh.
  fails=$(printf '%s' "$gate_out" | sed 's/\x1b\[[0-9;]*m//g' | grep 'FAIL' | head -20)
  # Chan cuoi: rc=1 ma khong loc ra dong FAIL nao thi in nguyen output. Khong
  # bao gio gui di mot danh sach trong.
  [ -z "$fails" ] && fails=$(printf '%s' "$gate_out" | head -20)
  reason="Cổng tài liệu ĐỎ — không được kết thúc lượt khi cổng chưa xanh.

$fails

Sửa các vi phạm trên rồi chạy lại: bash .claude/check-docs.sh
Nếu một vi phạm là báo sai, ĐỪNG nới luật cho cổng xanh — sửa phép dò bằng một dấu hiệu máy đọc được, và ghi lý do."
  printf '{"decision":"block","reason":"%s"}\n' "$(json_escape "$reason")"
  exit 0
fi

# ---------------------------------------------------------------- nhắc review Core
# `-s` chứ không `-f`: chỉ nhắc khi dấu THẬT SỰ có nội dung.
#
# Bản đầu dùng `-f` (chỉ cần file tồn tại) và đã sinh ra một lời nhắc với danh
# sách file TRỐNG — người đọc được bảo "lượt này chạm Core" mà không biết chạm
# vào đâu, nên không kiểm lại được lời nhắc có đúng không. Một cảnh báo không
# nêu được bằng chứng thì lần thứ hai người ta sẽ bỏ qua nó.
if [ -s "$STATE/core-touched" ]; then
  # LC_ALL=C: so sanh theo byte, khong bao gio thoat loi vi ky tu khong hop le.
  # Da xay ra that - `sort` bo cuoc giua chung va danh sach file ra RONG.
  files=$(LC_ALL=C sort -u "$STATE/core-touched" 2>&1 | head -15)
  # Loc ra rong trong khi dau CO noi dung nghia la buoc loc dang an mat bang
  # chung. Thong diep cu chi noi "loi cua hook" roi vut noi dung di - nen lan
  # sau khong ai truy duoc nguyen nhan. Nay in nguyen byte tho da thoat, co
  # gioi han, de ca chuoi hong lan ly do hong deu con lai.
  if [ -z "$files" ]; then
    raw=$(od -c "$STATE/core-touched" 2>/dev/null | head -8)
    files="(buoc loc tra ve rong trong khi dau co $(wc -c < "$STATE/core-touched") byte — day la LOI CUA HOOK.
Byte tho de chan doan:
$raw
Hay bao lai cho nguoi dung thay vi bo qua: mot dau khong doc duoc nghia la lan sau canh bao nay se sai im lang.)"
  fi
  # KHONG dung `stop_hook_active` lam bo nho "da nhac". Co do do harness bat sau
  # BAT KY lan chan nao — ke ca lan chan vi cong do. Chuoi that: luot 1 cham Core
  # va de cong do -> chan (giu ca hai dau) -> agent sua cho xanh -> dung luot, co
  # bat -> nhanh Core -> xoa dau va exit 0 => LOI NHAC BIEN MAT, dung luot can no
  # nhat. Ban truoc cua chinh file nay co dong do; no sai o CA HAI ca no voi toi.
  #
  # Bo nho "da nhac" da co san va dung hon: dau `core-touched` bi xoa NGAY DUOI,
  # truoc khi chan. Lan Stop sau khong con dau thi khong con nhac.
  # Xoá dấu TRƯỚC khi chặn — nhắc đúng một lần, không lặp.
  rm -f "$STATE/core-touched"
  rm -f "$STATE/changed"
  reason="Lượt này đã chạm tới Core. Cổng tài liệu xanh, nhưng cổng chỉ bắt được thứ máy kiểm được — nó KHÔNG đọc hiểu nội dung.

File đã sửa:
$files

Gọi agent core-reviewer qua công cụ Agent, với ba ràng buộc bắt buộc:
1. Chỉ MỘT phạm vi mỗi lượt — BE hoặc FE, không bao giờ cả hai.
2. KHÔNG gửi tóm tắt việc vừa làm — chỉ nói phạm vi cần soát. Nhận tóm tắt thì nó chỉ xác nhận lại thiên kiến của người vừa viết.
3. Không sửa gì trong lúc nó chạy.

Nếu lượt này thật sự không cần review (ví dụ chỉ sửa lỗi chính tả), nói rõ lý do rồi kết thúc."
  printf '{"decision":"block","reason":"%s"}\n' "$(json_escape "$reason")"
  exit 0
fi

rm -f "$STATE/changed"
exit 0
