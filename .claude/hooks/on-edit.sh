#!/usr/bin/env bash
# on-edit.sh — hook PostToolUse trên Edit|Write.
#
# Việc duy nhất: GHI DẤU rằng một file thuộc khu cần canh vừa bị sửa.
# KHÔNG chạy cổng ở đây — cổng mất khoảng 2 giây, và chạy nó sau mỗi lần sửa
# file làm mỗi lượt chậm đi hàng chục giây. Cổng chạy một lần ở hook Stop.
#
# Không phụ thuộc `jq` (máy này không có). Chỉ dùng grep/sed.
#
# Luôn thoát 0: một hook ghi dấu mà làm hỏng lượt là một hook người ta tắt đi.

set -uo pipefail
# Ép locale, VÀ không dùng `grep -P`. Hai lớp cho cùng một bẫy: trên Git Bash
# với LANG rỗng, `grep -P` thoát mã 2 kèm "supports only unibyte and UTF-8
# locales". Trong một hook, lỗi đó KHÔNG hiện ra đâu cả — dấu đơn giản không
# được ghi, và cổng ở hook Stop không bao giờ chạy. Bắt được lúc pipe-test;
# nếu không thì nó đã im lặng vô hiệu hoá cả hai hook.
export LC_ALL=en_US.UTF-8
cd "$(dirname "$0")/../.." || exit 0

STATE=".claude/.state"
mkdir -p "$STATE" 2>/dev/null || exit 0

payload=$(cat)

# Cat bo phan `tool_response` TRUOC khi trich bat cu thu gi. Payload cua hook
# chua ca ket qua tra ve cua cong cu, va ket qua do co the chua nguyen van
# nhung chuoi ma cac mau duoi day dang tim - ke ca noi dung cua chinh
# settings.json. Moi mau BRE deu tham lam, nen `.*"command"` bam vao lan xuat
# hien CUOI CUNG trong ca payload chu khong phai lan dau.
payload=${payload%%'"tool_response"'*}
# Đường dẫn trong JSON dùng dấu gạch chéo ngược đã nhân đôi trên Windows.
# Chuẩn hoá về gạch chéo xuôi trước khi so khớp.
f=$(printf '%s' "$payload" | sed -n 's/.*"file_path"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -1)

# NotebookEdit khai `notebook_path`, KHÔNG khai `file_path` — thiếu nhánh này
# thì sửa notebook không bao giờ ghi được dấu.
[ -z "$f" ] && f=$(printf '%s' "$payload" | sed -n 's/.*"notebook_path"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -1)

if [ -n "$f" ]; then
  # `tr` thay TUNG ky tu nen xu ly duoc ca dang nhan doi lan dang don, va
  # khong can viet mot mau sed chua backslash doi - mau do de bi hong khi file
  # duoc sinh ra qua mot lop truyen trung gian.
  BSL=$(printf '\134')
  # Truyen "$BSL$BSL" chu khong phai "$BSL": mot backslash don o cuoi tap ky tu
  # lam tr canh bao "unescaped backslash at end of string is not portable".
  f=$(printf '%s' "$f" | tr "$BSL$BSL" "//")
  # Khu cần canh. Khớp cả đường dẫn tuyệt đối lẫn tương đối.
  case "$f" in
    *docs/*|*.claude/*|*spec/*|*src/*) ;;
    *) exit 0 ;;
  esac
  : > "$STATE/changed"
  mark="$f"
else
  # Không có file_path nào để trích ⇒ đây là một lệnh Bash.
  #
  # Sửa file bằng Bash (`sed -i`, heredoc, `>`, script python) KHÔNG đi qua
  # Edit/Write. Không ghi dấu ở nhánh này thì một lượt sửa docs/ bằng Bash kết
  # thúc mà cổng không chạy — câu "không thể kết thúc lượt với cổng đỏ"
  # (CLAUDE.md §8) sai trên thực tế.
  #
  # GHI DẤU KHÔNG ĐIỀU KIỆN, cố ý. Không thử phân biệt lệnh đọc với lệnh ghi:
  # phép thử đó cần parse dòng lệnh, mà parse sai theo chiều "không phải lệnh
  # ghi" thì cổng biến mất im lặng. Đánh dấu thừa tốn ~2 giây chạy cổng ở cuối
  # lượt. Chọn hướng hỏng an toàn.
  case "$payload" in *'"command"'*) ;; *) exit 0 ;; esac
  : > "$STATE/changed"

  # Chỉ để nhận diện chạm Core. Cắt ở dấu nháy ĐẦU TIÊN, không phải cuối cùng:
  # payload của hook chứa cả `tool_response`, nên một `.*` tham lam sẽ nuốt
  # nguyên khối JSON vào dấu — và on-stop.sh in nguyên nội dung dấu đó ra.
  # KHONG doan tu chuoi lenh nua.
  #
  # Ban truoc trich duong dan tu chinh cau lenh roi so voi danh sach khu Core.
  # No danh dau ca lenh CHI DOC: mot `grep docs/quy-uoc/...` bi tinh la cham
  # Core va doi mot luot review day du. Mot canh bao doi review cho mot lenh doc
  # la canh bao se bi bo qua tu lan thu hai — dung che do hong ma on-stop.sh
  # canh bao o cho khac.
  #
  # Thay bang bang chung khong can doan: hoi HE THONG TEP xem file nao trong khu
  # Core VUA THAY DOI. Cach nay khong parse gi, khong nham lenh doc voi lenh ghi,
  # va dau ghi ra la DUONG DAN THAT thay vi mot doan lenh bi cat cut.
  mark=""
  # docs/OWNERSHIP.md la DAU VAO TRUC TIEP cua cong §15 va §16: go mot dong
  # khoi so la go mot lop bao ve vinh vien, va cong van xanh vi no chi kiem
  # nhung dong con lai. Sua no phai kich hoat review nhu sua RULES.md.
  for p in docs/quy-uoc docs/RULES.md docs/OWNERSHIP.md docs/kien-truc-core-module.md src/BE/Core src/FE/src/app/core src/FE/src/app/shared; do
    [ -e "$p" ] || continue
    found=$(find "$p" -type f -newermt '-90 seconds' 2>/dev/null | head -8)
    [ -n "$found" ] && mark="$mark$found
"
  done
fi

# Cham Core thi ghi them mot dau rieng — hook Stop dung no de nhac goi
# core-reviewer.
#
# CHI AP TU GIAI DOAN 2. Chinh dinh nghia cua core-reviewer la "doi chieu code
# THAT voi quy tac trong docs/". Giai doan 1 chua co src/, nen mot luot sua tai
# lieu khong cho no thu gi de doi chieu — loi nhac luc do doi mot viec khong
# lam duoc, va no ban ra sau MOI lan cham docs/quy-uoc/.
#
# Hau qua da xay ra that: moi luot sua tai lieu ket thuc bang mot lan chan, va
# viec chay agent lien tuc de dap ung loi nhac do sinh ra chinh su phinh to ma
# ca ban rule nay ton tai de ngan.
[ -d src ] || exit 0

case "$mark" in
  *src/BE/Core/*|*src/FE/src/app/core/*|*src/FE/src/app/shared/*|\
  *docs/quy-uoc/*|*docs/kien-truc-core-module.md*|*docs/RULES.md*|*docs/OWNERSHIP.md*)
    # Dau bay gio LUON la duong dan that (ASCII), khong con la doan lenh bi cat
    # cut - nen khong con nguy co sinh chuoi UTF-8 hong lam `sort` bo cuoc.
    printf '%s\n' "$mark" | grep -v '^[[:space:]]*$' >> "$STATE/core-touched" ;;
esac

exit 0
