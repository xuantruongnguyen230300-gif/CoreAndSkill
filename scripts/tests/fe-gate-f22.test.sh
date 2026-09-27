#!/usr/bin/env bash
# scripts/tests/fe-gate-f22.test.sh — canary tự động cho luật F22 của scripts/fe-gate.sh.
#
# T12:cap-xanh-do — mọi ca kỳ vọng XANH có một ca kỳ vọng ĐỎ đi qua cùng cơ chế dò, khác đúng một vi
# phạm được cắm (RULES.md §8 luật T12); chú thích ngay trên mỗi ca xanh nói cặp của nó ở đâu.
#
# F22 hỏng theo BỐN cách khác hẳn nhau, nên canh riêng bốn nửa (nửa (3) và (4) từ ADR-0081 — đếm
# DÒNG MÃ, dòng thô vượt chỉ NOTE):
#
#   (1) PHÍA CÂY TỆP — một tệp vượt ngưỡng mà cổng không thấy. Mỗi LOẠI trong bảng §5 một ca, và ca
#       phải chứng minh cổng gọi ĐÍCH DANH loại đó cùng tên tệp — "có FAIL ở đâu đó trong section"
#       không đủ: cổng đỏ vì một loại khác trông y hệt cổng đỏ đúng chỗ.
#
#   (2) PHÍA BẢNG CHỦ — cổng giữ một BẢN SAO con số, hoặc một bản sao danh sách loại. Nửa này KHÔNG
#       ca nào của nửa (1) bắt được: một cổng viết cứng 400 và liệt kê tay sáu loại vẫn qua sạch
#       nửa (1). Chỉ SỬA BẢNG rồi xem cổng có đổi hành vi theo không mới bắt được — đúng lớp lỗi
#       DEBT F34 nêu đích danh ("đọc từ đó, không viết cứng hai nơi").
#
#   (3) PHÍA BỘ BÓC — phân loại dòng (trống / chú thích / mã) lệch khỏi đoạn "Phân loại một dòng"
#       ở §5. Mỗi quy tắc của đoạn đó một cặp ca, dựng sao cho bộ bóc SAI theo đúng chiều quy tắc
#       ấy chặn thì cho kết quả ngược: tệp nằm ĐÚNG ngưỡng tính bằng mã, cộng các dòng đang xét —
#       chú thích thì xanh (kèm NOTE về dòng thô), mã thì đỏ. Ca đỏ phân định nhất là hai quy tắc
#       "không theo dõi": khối mở GIỮA dòng và dấu chú thích nằm trong chuỗi giữa dòng — một bộ bóc
#       "thông minh hơn §5" đếm thiếu ở đó và xanh.
#
#   (4) PHÍA CÚ PHÁP — cổng đọc cú pháp chú thích theo đuôi LÚC CHẠY từ gạch đầu dòng "Cú pháp theo
#       đuôi tệp" ở §5; câu chữ gạch đó là đầu vào của cổng. Cùng khuôn nửa (2): sửa gạch đó trong
#       repo giả thì cổng phải đổi hành vi theo — thêm dấu dòng cho một đuôi, bỏ một đuôi, mất hẳn
#       gạch. Thêm một ca đọc gạch THẬT bằng phép đọc của test và so với cú pháp mà các ca nửa (3)
#       giả định: §5 đổi thì ca đó chỉ ra phải sửa nửa (3) cùng lượt, thay vì để nửa (3) đỏ khó hiểu.
#
# Ca nửa (2) chạy script từ một REPO GIẢ — thư mục tạm chứa bản sao `scripts/fe-gate.sh` và bản sao
# `docs/quy-uoc/fe-architecture.md`. Cổng `cd` về gốc repo của chính nó rồi đọc bảng theo đường dẫn
# tương đối, nên bản sao bảng trong repo giả là thứ nó đọc. docs/ thật không bị chạm.
#
# Fixture của nửa (1) là cây TỐI GIẢN dựng TỪ CHÍNH BẢNG: mỗi dòng bảng một tệp dài ĐÚNG BẰNG ngưỡng
# cứng của dòng đó. Nền xanh đó vừa là ca biên "bằng ngưỡng thì chưa vượt", vừa làm mỗi ca sau chỉ
# đổi đúng một thứ. Fixture không viết cứng đường dẫn thư mục nào của app đang chạy — nó tự sinh
# `src/app/zz/` cho mẫu tên trần và `src/<thư mục>/` cho mẫu có thư mục, nên việc dời thư mục bên
# trong src/FE/src/app không ảnh hưởng tới test này.
#
# Phép đọc bảng của test CỐ Ý khác phép của cổng: test đọc theo VỊ TRÍ cột (ô thứ ba là ngưỡng cứng),
# cổng đọc theo TÊN cột. Dùng chung parser thì parser sai cả hai bên vẫn xanh.
#
# Chạy từ gốc repo:
#     bash scripts/tests/fe-gate-f22.test.sh
#
# Thoát 0 = mọi ca đúng kỳ vọng · 1 = có ca sai kỳ vọng · 2 = không chạy được.

set -uo pipefail

T="$(cd "$(dirname "$0")" && pwd)"
REPO="$(cd "$T/../.." && pwd)"
[ -f "$REPO/scripts/fe-gate.sh" ] || { echo "ABORT — không thấy scripts/fe-gate.sh"; exit 2; }
[ -d "$REPO/src/FE/src/app" ] || { echo "ABORT — không thấy src/FE/src/app để chép"; exit 2; }
cd "$REPO" || exit 2
# shellcheck source=_fixture-that.sh
. "$T/_fixture-that.sh"

NFAIL=0
REPO2=""
GATE="$REPO/scripts/fe-gate.sh"
BANG_THAT="$REPO/docs/quy-uoc/fe-architecture.md"
[ -f "$BANG_THAT" ] || { echo "ABORT — không thấy $BANG_THAT"; exit 2; }

# ---------------------------------------------------------------- đọc bảng (phép đọc của TEST)
# bang_loai <tệp bảng> — in "<mẫu tên tệp><TAB><ngưỡng cứng>" cho mỗi dòng ngưỡng.
# Nhận dòng theo HÌNH DẠNG (ô đầu là một mẫu trong backtick, ô thứ ba là một số bọc **), không theo
# số mục — đổi số mục §5 không làm test mù.
bang_loai() {
  LC_ALL=C awk -F'|' '/^\| `/ && $4 ~ /\*\*[0-9]+\*\*/ {
      m = $2; sub(/^[^`]*`/, "", m); sub(/`.*$/, "", m)
      c = $4; gsub(/[^0-9]/, "", c)
      if (m != "" && c != "") print m "\t" c }' "$1"
}

SO_LOAI=$(bang_loai "$BANG_THAT" | grep -c .)
[ "$SO_LOAI" -gt 0 ] || { echo "ABORT — không đọc được dòng ngưỡng nào từ $BANG_THAT"; exit 2; }

# ---------------------------------------------------------------- dựng fixture
# dong <n> — in n DÒNG MÃ. Từ ADR-0081 cổng đếm dòng mã, nên dòng độn phải là mã ở CẢ ba cú pháp
# (.ts, .scss, .html): một định danh trần — không mở đầu bằng dấu chú thích nào. Nội dung vẫn tránh
# nhuốm màu luật khác (F6 hex, F8 dấu thanh, F37 hằng *_MS).
dong() { LC_ALL=C awk -v n="$1" 'BEGIN { for (i = 1; i <= n; i++) print "zz_canary_" i ";" }'; }

# duong_dan <mẫu> — đường dẫn tương đối dưới $DIR của tệp canary cho mẫu đó
duong_dan() {
  case "$1" in
    */*) printf 'src/%s/%s\n' "${1%/*}" "$(printf '%s' "${1##*/}" | sed 's/\*/zz-canary/')" ;;
    *)   printf 'src/app/zz/%s\n' "$(printf '%s' "$1" | sed 's/\*/zz-canary/')" ;;
  esac
}

# dung_toi_gian — DIR = cây tối giản, mỗi dòng bảng đúng một tệp dài ĐÚNG BẰNG ngưỡng cứng
dung_toi_gian() {
  DIR="$(mktemp -d)" || { echo "ABORT — mktemp -d thất bại"; exit 2; }
  local m n f
  while IFS=$'\t' read -r m n; do
    [ -n "$m" ] || continue
    f="$DIR/$(duong_dan "$m")"
    mkdir -p "$(dirname "$f")"
    dong "$n" > "$f"
  done < <(bang_loai "$BANG_THAT")
}

# dung_repo_gia — REPO2 = repo giả (bản sao script + bản sao bảng chủ); GATE trỏ vào script sao
dung_repo_gia() {
  REPO2="$(mktemp -d)" || { echo "ABORT — mktemp -d thất bại"; exit 2; }
  mkdir -p "$REPO2/scripts" "$REPO2/docs/quy-uoc"
  cp "$REPO/scripts/fe-gate.sh" "$REPO2/scripts/fe-gate.sh"
  cp "$BANG_THAT" "$REPO2/docs/quy-uoc/fe-architecture.md"
  BANG_GIA="$REPO2/docs/quy-uoc/fe-architecture.md"
  GATE="$REPO2/scripts/fe-gate.sh"
}

# them_dong_bang <dòng markdown> — chèn một dòng vào CUỐI bảng ngưỡng của BẢN SAO
them_dong_bang() {
  LC_ALL=C awk -v d="$1" '{ a[NR] = $0; if ($0 ~ /^\| `/ && $0 ~ /\*\*[0-9]+\*\*/) last = NR }
    END { for (i = 1; i <= NR; i++) { print a[i]; if (i == last) print d } }' "$BANG_GIA" > "$BANG_GIA.tmp" \
    && mv "$BANG_GIA.tmp" "$BANG_GIA"
}

# ---------------------------------------------------------------- chấm
# cham_f22 <tên ca> <do|xanh> [chuỗi bắt buộc có trong khối F22]...
# Khác `cham` của _fixture-that.sh ở hai chỗ: chạy script từ $GATE (có thể là repo giả), và kiểm
# thêm rằng khối F22 nêu ĐÍCH DANH thứ cần nêu — một FAIL không nói tên loại nào là một FAIL không
# chỉ được chỗ hỏng.
cham_f22() {
  local name="$1" expect="$2"; shift 2
  local out sec actual thieu="" s
  out="$(FE="$DIR" bash "$GATE" 2>&1)"
  rm -rf "$DIR"
  [ -n "$REPO2" ] && rm -rf "$REPO2"
  REPO2=""; GATE="$REPO/scripts/fe-gate.sh"
  sec="$(khoi_section F22 "$out")"
  if printf '%s\n' "$sec" | grep -q 'FAIL'; then
    actual=do
  elif printf '%s\n' "$sec" | grep -q 'OK'; then
    actual=xanh
  else
    actual=khong-thay-section
  fi
  for s in "$@"; do
    case "$s" in
      '!'*) printf '%s\n' "$sec" | LC_ALL=C grep -qF -- "${s#!}" \
              && thieu="$thieu    KHÔNG được có trong khối: ${s#!}"$'\n' ;;
      *)    printf '%s\n' "$sec" | LC_ALL=C grep -qF -- "$s" \
              || thieu="$thieu    thiếu trong khối: $s"$'\n' ;;
    esac
  done
  if [ "$actual" = "$expect" ] && [ -z "$thieu" ]; then
    printf '  \033[32mOK\033[0m    %-76s F22=%s (kỳ vọng %s)\n' "$name" "$actual" "$expect"
  else
    printf '  \033[31mFAIL\033[0m  %-76s F22=%s (kỳ vọng %s)\n' "$name" "$actual" "$expect"
    [ -n "$thieu" ] && printf '%s' "$thieu"
    printf '%s\n' "$sec"
    NFAIL=$((NFAIL + 1))
  fi
}

printf '\n\033[1m== canary F22 — nửa (1): phía cây tệp, fixture dựng từ bảng §5 ==\033[0m\n'

# Cặp đỏ: mỗi ca "dài MUC+1" trong vòng lặp ngay dưới — cùng cây, một tệp thêm đúng một dòng mã.
# Dòng thô = ngưỡng, không vượt, nên KHÔNG được có NOTE: NOTE chỉ in khi dòng thô VƯỢT.
dung_toi_gian
cham_f22 "mỗi loại dài ĐÚNG BẰNG ngưỡng cứng -> PASS, không NOTE (ca biên: bằng thì chưa vượt)" xanh \
  '!NOTE'

# Một ca cho MỖI dòng bảng — danh sách loại đến từ bảng, test cũng không liệt kê tay.
while IFS=$'\t' read -r MAU MUC; do
  [ -n "$MAU" ] || continue
  REL="$(duong_dan "$MAU")"
  dung_toi_gian; dong "$((MUC + 1))" > "$DIR/$REL"
  cham_f22 "$MAU dài $((MUC + 1)) dòng mã > $MUC -> FAIL đích danh loại và tệp" do \
    "$MAU vượt ngưỡng cứng $MUC dòng mã" "$REL: $((MUC + 1)) dòng mã"

  dung_toi_gian; rm -f "$DIR/$REL"
  cham_f22 "$MAU: không còn tệp nào khớp -> FAIL (không xanh rỗng)" do \
    "$MAU: không tệp nào khớp"
done < <(bang_loai "$BANG_THAT")

# `find` khớp cả thư mục, không riêng tệp. Một THƯ MỤC trùng mẫu tên từng làm chốt chống xanh rỗng
# tưởng loại đó có đầu vào, trong khi awk bỏ qua nó bằng một cảnh báo ra stderr mà không ai đọc —
# section in OK dù không tệp thật nào được đo. Ca này neo `-type f` lại.
MAU_TM=$(bang_loai "$BANG_THAT" | LC_ALL=C awk -F'\t' '$1 == "*.component.ts" { print $1 }')
dung_toi_gian; rm -f "$DIR/$(duong_dan "$MAU_TM")"; mkdir -p "$DIR/src/app/zz/gia.component.ts"
cham_f22 "loại chỉ còn một THƯ MỤC trùng mẫu tên -> FAIL (thư mục không phải tệp)" do \
  "$MAU_TM: không tệp nào khớp"

dung_toi_gian; rm -rf "$DIR/src/styles"
cham_f22 "mất hẳn thư mục gốc của một dòng bảng -> FAIL" do "không có thư mục"

dung_toi_gian; rm -rf "$DIR/src"
cham_f22 "không có \$FE/src -> FAIL" do

printf '\n\033[1m== canary F22 — nửa (2): phía bảng chủ, script chạy từ repo giả ==\033[0m\n'

MUC_CT=$(bang_loai "$BANG_THAT" | LC_ALL=C awk -F'\t' '$1 == "*.component.ts" { print $2 }')
MUC_PAGE=$(bang_loai "$BANG_THAT" | LC_ALL=C awk -F'\t' '$1 == "*.page.ts" { print $2 }')
[ -n "$MUC_CT" ] && [ -n "$MUC_PAGE" ] || { echo "ABORT — bảng §5 không còn dòng *.component.ts / *.page.ts"; exit 2; }

# Ca quyết định của DEBT F34: NỚI ngưỡng ở bảng thì tệp vừa vượt phải thành hợp lệ. Một cổng viết
# cứng con số vẫn đỏ ở ca này.
# Cặp đỏ: ca ngay dưới — cùng repo giả, cùng tệp vượt, bảng KHÔNG nới.
dung_toi_gian; dung_repo_gia
dong "$((MUC_CT + 1))" > "$DIR/$(duong_dan '*.component.ts')"
LC_ALL=C sed -i "s/| \*\*$MUC_CT\*\* |/| **9000** |/" "$BANG_GIA"
cham_f22 "bảng NỚI ngưỡng -> tệp vừa vượt thành hợp lệ, PASS (cổng không giữ bản sao số)" xanh \
  "*.component.ts<=9000"

dung_toi_gian; dung_repo_gia
dong "$((MUC_CT + 1))" > "$DIR/$(duong_dan '*.component.ts')"
cham_f22 "repo giả, bảng GIỮ NGUYÊN, cùng tệp vượt -> FAIL (cặp của ca NỚI)" do \
  "*.component.ts vượt ngưỡng cứng $MUC_CT dòng mã"

# Chiều ngược lại: SIẾT ngưỡng ở bảng thì cây tệp đứng yên phải thành vi phạm.
dung_toi_gian; dung_repo_gia
LC_ALL=C sed -i "s/| \*\*$MUC_PAGE\*\* |/| **10** |/" "$BANG_GIA"
cham_f22 "bảng SIẾT ngưỡng -> cây tệp đứng yên thành vi phạm, FAIL" do \
  "vượt ngưỡng cứng 10 dòng"

# Cột đọc theo TÊN, không theo chỉ số: đặt một số bọc ** vào ô "Ngưỡng mềm" — cổng đọc nhầm cột sẽ đỏ.
# Cặp đỏ: ca ngay dưới — cùng dòng bảng, cùng số **5**, đặt vào ô Ngưỡng CỨNG.
dung_toi_gian; dung_repo_gia
LC_ALL=C sed -i 's/^\(| `\*\.component\.ts` (dumb) |\) 150 |/\1 **5** |/' "$BANG_GIA"
cham_f22 "số bọc ** ở cột Ngưỡng MỀM -> PASS (cổng đọc cột theo tên, không theo vị trí)" xanh \
  "*.component.ts<=$MUC_CT"

dung_toi_gian; dung_repo_gia
LC_ALL=C sed -i "s/^\(| \`\*\.component\.ts\` (dumb) | 150 |\) \*\*$MUC_CT\*\* |/\1 **5** |/" "$BANG_GIA"
cham_f22 "cùng **5** đặt vào ô Ngưỡng CỨNG -> FAIL (cặp của ca cột mềm)" do \
  "*.component.ts vượt ngưỡng cứng 5 dòng mã"

dung_toi_gian; dung_repo_gia
LC_ALL=C sed -i 's/^## 5\. Ngưỡng kích thước file/## 5. Kich thuoc file/' "$BANG_GIA"
cham_f22 "mất tiêu đề mục bảng ngưỡng -> FAIL (không đọc được thì không xanh)" do \
  "không đọc được dòng ngưỡng nào"

dung_toi_gian; dung_repo_gia
LC_ALL=C sed -i 's/| Ngưỡng mềm | Ngưỡng cứng |/| Ngưỡng mềm | Tran cung |/' "$BANG_GIA"
cham_f22 "đổi tên cột 'Ngưỡng cứng' -> FAIL (không đoán cột thay bảng)" do \
  "không đọc được dòng ngưỡng nào"

dung_toi_gian; dung_repo_gia
LC_ALL=C sed -i "s/| \*\*$MUC_PAGE\*\* |/| bon tram |/" "$BANG_GIA"
cham_f22 "ô 'Ngưỡng cứng' không phải số -> FAIL đích danh dòng bảng" do \
  "*.page.ts: cột 'Ngưỡng cứng' không phải số"

# Danh sách LOẠI cũng đến từ bảng: thêm một dòng mới thì cổng phải đo loại mới ngay. Đuôi của loại
# mới là `.ts` — đuôi ĐÃ có cú pháp ở §5 — để ca này đo đúng việc "bảng thêm loại", không vấp chốt
# cú pháp ở ca kế tiếp.
dung_toi_gian; dung_repo_gia
them_dong_bang '| `*.zzcanary.ts` | 1 | **2** | dòng canary do fe-gate-f22.test.sh chèn vào BẢN SAO |'
mkdir -p "$DIR/src/app/zz"; dong 3 > "$DIR/src/app/zz/moi.zzcanary.ts"
cham_f22 "bảng có dòng loại MỚI + tệp vượt -> FAIL đích danh loại mới" do \
  "*.zzcanary.ts vượt ngưỡng cứng 2 dòng mã" "moi.zzcanary.ts: 3 dòng mã"

dung_toi_gian; dung_repo_gia
them_dong_bang '| `*.zzcanary.ts` | 1 | **2** | dòng canary do fe-gate-f22.test.sh chèn vào BẢN SAO |'
cham_f22 "bảng có dòng loại MỚI nhưng không tệp nào khớp -> FAIL (không xanh rỗng)" do \
  "*.zzcanary.ts: không tệp nào khớp"

# §5: "Đuôi nào có ở cột Loại file mà chưa có ở đây thì cổng đỏ, không đoán." Tệp của loại mới chỉ
# một dòng, dưới ngưỡng — cổng mà đoán cú pháp (hay đếm thô) sẽ xanh.
dung_toi_gian; dung_repo_gia
them_dong_bang '| `*.zzcanary` | 1 | **2** | dòng canary do fe-gate-f22.test.sh chèn vào BẢN SAO |'
mkdir -p "$DIR/src/app/zz"; dong 1 > "$DIR/src/app/zz/moi.zzcanary"
cham_f22 "bảng có đuôi CHƯA có cú pháp chú thích ở §5 -> FAIL đích danh, không đoán" do \
  "*.zzcanary: đuôi '.zzcanary' chưa có cú pháp chú thích"

# Miễn trừ `*.spec.ts` (câu ngay dưới bảng §5) hôm nay KHÔNG có chỗ thi hành trên bảng thật: không
# mẫu nào trong bảng khớp một tên kết thúc bằng `.spec.ts`, nên một ca cắm tệp spec vào bảng thật sẽ
# xanh vì lý do sai. Ca này thêm dòng `*.ts` vào BẢN SAO bảng để miễn trừ có chỗ chạy thật — không
# có nó thì dòng `-not -name '*.spec.ts'` của cổng là code chưa ai chạy qua.
dung_toi_gian; dung_repo_gia
them_dong_bang '| `*.ts` | 1 | **2** | dòng canary do fe-gate-f22.test.sh chèn vào BẢN SAO |'
dong 9999 > "$DIR/src/app/zz/mien-tru.spec.ts"
cham_f22 "dòng bảng *.ts: .spec.ts 9999 dòng được tha, tệp .ts thường vẫn bị bắt" do \
  "*.ts vượt ngưỡng cứng 2 dòng mã" '!mien-tru.spec.ts'

dung_toi_gian; dung_repo_gia
them_dong_bang '| `DOC-KHONG-PHAI-MAU` | 1 | **2** | dòng canary do fe-gate-f22.test.sh chèn vào BẢN SAO |'
cham_f22 "dòng bảng không dịch được thành mẫu tên tệp -> FAIL, không bỏ qua im lặng" do \
  "không dịch được thành mẫu tên tệp"

dung_toi_gian; dung_repo_gia; rm -f "$BANG_GIA"
cham_f22 "mất hẳn bảng chủ -> FAIL" do "không thấy bảng ngưỡng"

printf '\n\033[1m== canary F22 — nửa (3): bộ bóc, mỗi quy tắc phân loại dòng ở §5 một cặp ==\033[0m\n'

MUC_HTML=$(bang_loai "$BANG_THAT" | LC_ALL=C awk -F'\t' '$1 == "*.html" { print $2 }')
MUC_SCSS=$(bang_loai "$BANG_THAT" | LC_ALL=C awk -F'\t' '$1 == "*.scss" { print $2 }')
[ -n "$MUC_HTML" ] && [ -n "$MUC_SCSS" ] || { echo "ABORT — bảng §5 không còn dòng *.html / *.scss"; exit 2; }
TS_REL="$(duong_dan '*.component.ts')"; N="$MUC_CT"
HTML_REL="$(duong_dan '*.html')"; NH="$MUC_HTML"
SCSS_REL="$(duong_dan '*.scss')"; NS="$MUC_SCSS"

# them <rel> <dòng>... — nối mỗi đối số thành một dòng vào cuối tệp (cây tối giản: tệp đang ĐÚNG ngưỡng)
them() { local f="$DIR/$1"; shift; printf '%s\n' "$@" >> "$f"; }
# viet <rel> <n> <dòng>... — ghi lại tệp: n dòng mã, rồi các dòng cho sau
viet() { local f="$DIR/$1" n="$2"; shift 2; { dong "$n"; printf '%s\n' "$@"; } > "$f"; }

# -- `*.ts`: dòng bắt đầu bằng `//` là chú thích; `f(); // x` là mã.
# Cặp đỏ: ca ngay dưới — cùng hai dòng nối, dòng thứ hai có mã đứng trước `//`.
dung_toi_gian; them "$TS_REL" '// a' '  // b'
cham_f22 "ts: dòng mở đầu bằng // là chú thích -> PASS, NOTE thô $((N + 2)) mã $N" xanh \
  "NOTE" "$TS_REL: thô $((N + 2)) > $N, mã $N"
dung_toi_gian; them "$TS_REL" '// a' '  f(); // b'
cham_f22 "ts: mã rồi // cuối dòng là dòng MÃ -> FAIL" do \
  "$TS_REL: $((N + 1)) dòng mã"

# -- khối mở và đóng trên HAI dòng khác nhau; dòng đóng còn mã sau `*/` là mã.
# Cặp đỏ: ca ngay dưới — cùng khối, dòng đóng có thêm mã.
dung_toi_gian; them "$TS_REL" '/**' ' * a' ' */'
cham_f22 "ts: khối /** */ trải ba dòng là chú thích -> PASS, NOTE" xanh \
  "NOTE" "$TS_REL: thô $((N + 3)) > $N, mã $N"
dung_toi_gian; them "$TS_REL" '/**' ' * a' ' */ f();'
cham_f22 "ts: dòng đóng khối còn mã sau */ là dòng MÃ -> FAIL" do \
  "$TS_REL: $((N + 1)) dòng mã"

# -- khối mở và đóng trên CÙNG dòng: không còn gì ngoài chú thích thì là chú thích; `/* a */ f();` là mã.
# Cặp đỏ: ca ngay dưới.
dung_toi_gian; them "$TS_REL" '/* a */'
cham_f22 "ts: /* a */ đứng riêng là chú thích -> PASS, NOTE" xanh \
  "NOTE" "$TS_REL: thô $((N + 1)) > $N, mã $N"
dung_toi_gian; them "$TS_REL" '/* a */ f();'
cham_f22 "ts: /* a */ f(); là dòng MÃ (§5 nêu đích danh) -> FAIL" do \
  "$TS_REL: $((N + 1)) dòng mã"

# -- khối mở GIỮA dòng sau mã KHÔNG được theo dõi: ba dòng dưới đây đều là mã. Ca đỏ là ca phân định:
# một bộ bóc theo dõi khối đó đếm được N-1 và xanh.
# Cặp đỏ: ca ngay dưới — cùng ba dòng, thêm đúng một dòng mã độn.
dung_toi_gian; viet "$TS_REL" "$((N - 3))" 'f(); /* a' ' * b' ' */'
cham_f22 "ts: khối mở giữa dòng + 2 dòng sau, tổng đúng $N dòng mã -> PASS" xanh \
  '!NOTE'
dung_toi_gian; viet "$TS_REL" "$((N - 2))" 'f(); /* a' ' * b' ' */'
cham_f22 "ts: khối mở giữa dòng không được theo dõi, hai dòng sau là MÃ -> FAIL" do \
  "$TS_REL: $((N + 1)) dòng mã"

# -- `//` nằm trong chuỗi giữa dòng (URL) không phải chú thích.
# Cặp đỏ: ca ngay dưới — cùng dòng, bỏ `// ` đứng đầu.
dung_toi_gian; them "$TS_REL" "// const u = 'https://vi.du/a';"
cham_f22 "ts: URL trong một dòng chú thích // -> PASS, NOTE" xanh \
  "NOTE" "$TS_REL: thô $((N + 1)) > $N, mã $N"
dung_toi_gian; them "$TS_REL" "const u = 'https://vi.du/a';"
cham_f22 "ts: chuỗi chứa https:// giữa dòng là dòng MÃ -> FAIL" do \
  "$TS_REL: $((N + 1)) dòng mã"

# -- `/*` nằm trong chuỗi giữa dòng không mở khối. Ca đỏ phân định: bộ bóc mở khối ở đó sẽ nuốt dòng
# `f();` kế tiếp, đếm được N và xanh.
# Cặp đỏ: ca ngay dưới — cùng hai dòng, thêm một dòng mã độn.
dung_toi_gian; viet "$TS_REL" "$((N - 2))" "const g = 'src/*';" 'f();'
cham_f22 "ts: chuỗi chứa /* giữa dòng + dòng sau, tổng đúng $N dòng mã -> PASS" xanh \
  '!NOTE'
dung_toi_gian; viet "$TS_REL" "$((N - 1))" "const g = 'src/*';" 'f();'
cham_f22 "ts: /* trong chuỗi giữa dòng KHÔNG mở khối, dòng sau vẫn là mã -> FAIL" do \
  "$TS_REL: $((N + 1)) dòng mã"

# -- dòng trống: chỉ khoảng trắng, kể cả `\r`.
# Cặp đỏ: ca ngay dưới — dòng cuối có thêm một ký tự.
dung_toi_gian; them "$TS_REL" '' '   ' $'\r' $'\t \r'
cham_f22 "ts: dòng rỗng, toàn khoảng trắng, chỉ \\r -> không phải mã, PASS, NOTE" xanh \
  "NOTE" "$TS_REL: thô $((N + 4)) > $N, mã $N"
dung_toi_gian; them "$TS_REL" '' '   ' $'\r' $'\t x\r'
cham_f22 "ts: dòng có một ký tự giữa khoảng trắng và \\r là dòng MÃ -> FAIL" do \
  "$TS_REL: $((N + 1)) dòng mã"

# -- xuống dòng CRLF: dấu đóng khối đứng trước `\r` vẫn đóng khối, dòng vẫn là chú thích.
# Cặp đỏ: ca ngay dưới — dòng đóng có thêm mã.
dung_toi_gian; them "$TS_REL" $'// a\r' $'/*\r' $' * b\r' $' */\r'
cham_f22 "ts: chú thích dòng và khối với xuống dòng CRLF -> PASS, NOTE" xanh \
  "NOTE" "$TS_REL: thô $((N + 4)) > $N, mã $N"
dung_toi_gian; them "$TS_REL" $'// a\r' $'/*\r' $' * b\r' $' */ f();\r'
cham_f22 "ts: CRLF, dòng đóng khối còn mã -> FAIL" do \
  "$TS_REL: $((N + 1)) dòng mã"

# -- tệp TOÀN chú thích: 0 dòng mã, dòng thô vượt xa ngưỡng. Tệp vẫn phải được đo (hiện trong NOTE
# với mã 0) — một bộ bóc bỏ rơi tệp 0 dòng mã sẽ không in gì về nó.
# Cặp đỏ: ca ngay dưới — cùng tệp, nối thêm N+1 dòng mã (vi phạm được cắm).
toan_chu_thich() { { echo '/**'; LC_ALL=C awk -v n="$1" 'BEGIN { for (i = 1; i <= n; i++) print " * x" }'; echo ' */'; } > "$DIR/$TS_REL"; }
dung_toi_gian; toan_chu_thich "$((N + 5))"
cham_f22 "ts: tệp toàn chú thích (thô $((N + 7))) -> PASS, NOTE nêu tệp với mã 0" xanh \
  "NOTE" "$TS_REL: thô $((N + 7)) > $N, mã 0"
dung_toi_gian; toan_chu_thich "$((N + 5))"; dong "$((N + 1))" >> "$DIR/$TS_REL"
cham_f22 "ts: tệp toàn chú thích + N+1 dòng mã -> FAIL" do \
  "$TS_REL: $((N + 1)) dòng mã"

# -- `*.html`: khối `<!--` … `-->`; KHÔNG có chú thích dòng.
# Cặp đỏ: ca ngay dưới — cùng khối, dòng đóng có thêm thẻ.
dung_toi_gian; them "$HTML_REL" '<!--' '  a' '-->'
cham_f22 "html: khối <!-- --> trải ba dòng là chú thích -> PASS, NOTE" xanh \
  "NOTE" "$HTML_REL: thô $((NH + 3)) > $NH, mã $NH"
dung_toi_gian; them "$HTML_REL" '<!--' '  a' '--> <p>x</p>'
cham_f22 "html: dòng đóng khối còn thẻ sau --> là dòng MÃ -> FAIL" do \
  "$HTML_REL: $((NH + 1)) dòng mã"

# Cặp đỏ: ca ngay dưới — cùng chỗ nối, dấu // thay cho <!-- -->.
dung_toi_gian; them "$HTML_REL" '<!-- a -->'
cham_f22 "html: <!-- a --> đứng riêng là chú thích -> PASS, NOTE" xanh \
  "NOTE" "$HTML_REL: thô $((NH + 1)) > $NH, mã $NH"
dung_toi_gian; them "$HTML_REL" '// a'
cham_f22 "html: // KHÔNG phải chú thích ở html (§5) -> dòng MÃ, FAIL" do \
  "$HTML_REL: $((NH + 1)) dòng mã"

# -- `*.scss`: dòng `//` và khối `/* */`.
# Cặp đỏ: ca ngay dưới — dòng khối có thêm một luật CSS sau `*/`.
dung_toi_gian; them "$SCSS_REL" '// a' '/* b */'
cham_f22 "scss: // và /* */ là chú thích -> PASS, NOTE" xanh \
  "NOTE" "$SCSS_REL: thô $((NS + 2)) > $NS, mã $NS"
dung_toi_gian; them "$SCSS_REL" '// a' '/* b */ .x {}'
cham_f22 "scss: /* b */ .x {} là dòng MÃ -> FAIL" do \
  "$SCSS_REL: $((NS + 1)) dòng mã"

printf '\n\033[1m== canary F22 — nửa (4): cú pháp chú thích đọc lúc chạy từ gạch "Cú pháp theo đuôi tệp" ở §5 ==\033[0m\n'

# Cổng đọc cú pháp từ §5: thêm dấu chú thích dòng `//` cho `*.html` trong BẢN SAO bảng thì `// a` ở một
# tệp html thành chú thích. Một cổng viết cứng cú pháp vẫn đếm nó là mã và đỏ.
# Cặp đỏ: ca ngay dưới — cùng repo giả, cùng dòng `// a`, gạch §5 giữ nguyên.
dung_toi_gian; dung_repo_gia
LC_ALL=C sed -i 's/`\*\.html` — không có chú thích dòng, khối/`*.html` — dòng `\/\/`, khối/' "$BANG_GIA"
them "$HTML_REL" '// a'
cham_f22 "gạch §5 khai thêm dòng // cho *.html -> // a ở html thành chú thích, PASS, NOTE" xanh \
  "NOTE" "$HTML_REL: thô $((NH + 1)) > $NH, mã $NH"
dung_toi_gian; dung_repo_gia
them "$HTML_REL" '// a'
cham_f22 "repo giả, gạch §5 giữ nguyên, cùng // a ở html -> dòng MÃ, FAIL" do \
  "$HTML_REL: $((NH + 1)) dòng mã"

dung_toi_gian; dung_repo_gia
LC_ALL=C sed -i 's/`\*\.scss` — dòng `\/\/`, khối `\/\*` … `\*\/`\. //' "$BANG_GIA"
cham_f22 "gạch §5 bỏ đoạn *.scss -> FAIL đích danh cả hai dòng bảng *.scss, không đoán" do \
  "*.scss: đuôi '.scss' chưa có cú pháp chú thích" "styles/*.scss: đuôi '.scss' chưa có cú pháp chú thích"

dung_toi_gian; dung_repo_gia
LC_ALL=C sed -i '/^- \*\*Cú pháp theo đuôi tệp:\*\*/d' "$BANG_GIA"
cham_f22 "mất hẳn gạch 'Cú pháp theo đuôi tệp' -> FAIL chốt, không phải đếm theo cú pháp đoán" do \
  "không đọc được gạch 'Cú pháp theo đuôi tệp'"

# Phép đọc của TEST (viết riêng, khác phép đọc của cổng): tách gạch THẬT thành từng đoạn "`*.đuôi` — …"
# rồi so với ĐÚNG cú pháp mà các ca nửa (3) ở trên giả định. §5 đổi (thêm đuôi, đổi dấu) thì ca này
# đỏ và chỉ ra phải sửa nửa (3) cùng lượt.
cu_phap_5() {
  LC_ALL=C perl -ne 'next unless /^- \*\*C\S+ ph\S+p theo \S+ t\S+p:\*\*/;
    while (/`\*\.(\w+)` \S+ (.*?)(?=`\*\.\w+` \S+ |$)/g) {
      my ($d, $s) = ($1, $2);
      my ($l) = $s =~ /d\S+ng `([^`]+)`/;
      my ($o, $c) = $s =~ /kh\S+i `([^`]+)` \S+ `([^`]+)`/;
      print "$d|", ($l // ""), "|", ($o // ""), "|", ($c // ""), "\n" }' "$BANG_THAT" | sort
}
DOC_CU_PHAP="$(cu_phap_5 | tr '\n' ' ')"
KY_VONG_CU_PHAP="html||<!--|--> scss|//|/*|*/ ts|//|/*|*/ "
if [ "$DOC_CU_PHAP" = "$KY_VONG_CU_PHAP" ]; then
  printf '  \033[32mOK\033[0m    %-76s §5=%s\n' "cú pháp §5 thật khớp cú pháp các ca nửa (3) giả định" "$DOC_CU_PHAP"
else
  printf '  \033[31mFAIL\033[0m  %-76s\n' "cú pháp chú thích ở §5 KHÁC cú pháp các ca nửa (3) giả định"
  printf '    §5 đọc ra      : %s\n    nửa (3) giả định: %s\n' "$DOC_CU_PHAP" "$KY_VONG_CU_PHAP"
  printf '    -> cổng đã theo §5; sửa các ca nửa (3) của tệp này cho khớp cú pháp mới\n'
  NFAIL=$((NFAIL + 1))
fi

printf '\n\033[1m== canary F22 — nền: bản sao code thật ==\033[0m\n'

# Cặp đỏ: ca ngay dưới — cùng bản sao, một *.page.ts thật được nối thêm dòng mã tới vượt ngưỡng.
dung_ban_sao
cham_f22 "bản sao code thật nguyên vẹn -> PASS" xanh \
  "*.page.ts<=$MUC_PAGE"

TRANG_THAT=$(cd "$REAL_FE" && find src/app -type f -name '*.page.ts' | sort | head -1)
[ -n "$TRANG_THAT" ] || { echo "ABORT — không thấy *.page.ts nào trong src/FE/src/app"; exit 2; }
dung_ban_sao; dong "$((MUC_PAGE + 1))" >> "$DIR/$TRANG_THAT"
cham_f22 "bản sao thật, $TRANG_THAT nối thêm $((MUC_PAGE + 1)) dòng mã -> FAIL đích danh" do \
  "*.page.ts vượt ngưỡng cứng $MUC_PAGE dòng mã" "$TRANG_THAT: "

ket_thuc fe-gate-f22.test.sh
