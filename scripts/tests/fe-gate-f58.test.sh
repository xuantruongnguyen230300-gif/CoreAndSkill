#!/usr/bin/env bash
# scripts/tests/fe-gate-f58.test.sh — canary tự động cho luật F58 của scripts/fe-gate.sh (đặc tả:
# docs/wiki-core/fe/trien-khai/05-gate.md §8.16).
#
# Chạy trên BẢN SAO của code thật (scripts/tests/_fixture-that.sh), mỗi ca một thay đổi đã kiểm là vào
# được bản sao (`doi`), gọi THẲNG scripts/fe-gate.sh qua biến FE, chấm ĐÍCH DANH theo riêng khối output
# của section F58. Không sửa code sản phẩm để thử.
#
# T12:cap-xanh-do — ba ca xanh X1–X3 và mười một ca đỏ D1–D11:
#   X1 nguyên vẹn · X2 đổi $bp-* VÀ hằng theo (cổng đọc giá trị từ _spacing.scss, không ghim số) ·
#   X3 hai dòng giống vi phạm mà không phải (setProperty('max-width'), subpathLen).
#   Cặp đỏ của X2 là D1 (đổi $bp-*, giữ hằng — ca F-01); cặp đỏ của X3 là D6 và D7 (cùng nơi đặt, đúng
#   dòng vi phạm thật).
#
# Chạy từ gốc repo:
#     bash scripts/tests/fe-gate-f58.test.sh
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
SP=src/styles/_spacing.scss
GUONG=src/app/core/theme/diem-ngat.ts
[ -f "$REAL_FE/$GUONG" ] || { echo "ABORT — không thấy $GUONG"; exit 2; }
grep -qF "TRUY_VAN_DUOI_BP_XS = '(max-width: 479.98px)';" "$REAL_FE/$GUONG" || { echo "ABORT — $GUONG thật không còn hằng TRUY_VAN_DUOI_BP_XS = 479.98px để làm nền cho ca"; exit 2; }
grep -qx '\$bp-xs: 480px;' "$REAL_FE/$SP" || { echo "ABORT — $SP thật không còn dòng '\$bp-xs: 480px;'"; exit 2; }

ca() { cham_dot_bien F58 "$@"; }

printf '\n\033[1m== canary F58 — bản sao code thật, gọi thẳng scripts/fe-gate.sh ==\033[0m\n'

# ---------------------------------------------------------------- ca xanh
dung_ban_sao
ca "X1 không đổi -> OK, cổng nêu số hằng và số biến đã đọc" xanh \
  "2 hằng của diem-ngat.ts khớp bảng" "5 biến đã đọc"

# Cặp đỏ: D1 — cùng đổi $bp-xs, hằng đứng yên.
dung_ban_sao
doi "$SP" 's{^\$bp-xs: 480px;}{\$bp-xs: 500px;}m' '$bp-xs: 500px;'
doi "$GUONG" 's{479\.98px}{499.98px}' '499.98px'
ca "X2 đổi \$bp-xs và hằng theo -> OK (cổng đọc giá trị từ _spacing.scss)" xanh \
  "2 hằng của diem-ngat.ts khớp bảng"

# Cặp đỏ: D6 và D7 — cùng tệp, đúng dòng vi phạm thật.
dung_ban_sao
cam src/app/shared/zz/zz.ts "el.style.setProperty('max-width', '1px');
const subpathLen = 3;"
ca "X3 setProperty('max-width') và subpathLen dưới shared/ -> OK" xanh \
  "2 hằng của diem-ngat.ts khớp bảng"

# ---------------------------------------------------------------- ca đỏ
dung_ban_sao
doi "$SP" 's{^\$bp-xs: 480px;}{\$bp-xs: 481px;}m' '$bp-xs: 481px;'
ca "D1 đổi \$bp-xs, giữ nguyên hằng (ca F-01) -> FAIL nêu tên hằng" do \
  "TRUY_VAN_DUOI_BP_XS" "đòi 480.98" "sửa hằng ở tệp phản chiếu, đừng sửa _spacing.scss"

dung_ban_sao
doi "$GUONG" 's{479\.98px}{479px}' "'(max-width: 479px)'"
ca "D2 hằng DUOI mang số nguyên 479 thay cho 479.98 -> FAIL" do \
  "(3) " "TRUY_VAN_DUOI_BP_XS" "mang 479"

dung_ban_sao
doi "$GUONG" 's{\$bp-xs}{\$bp-sm}' '$bp-sm'
ca "D3 chú thích hằng …_BP_XS nêu \$bp-sm -> FAIL" do \
  "(3) " "nêu \$bp-sm nhưng hậu tố tên là \$bp-xs"

dung_ban_sao
doi "$GUONG" 's{^/\*\*[^\n]*\$bp-xs[^\n]*\n}{}m'
ca "D4 xoá khối chú thích liền trên một hằng -> FAIL" do \
  "(3) " "không có khối '/** … */' liền trên"

dung_ban_sao
printf 'export const BP_XS_PX = 1;\n' >> "$DIR/$GUONG"
ca "D5 thêm \`export const BP_XS_PX = 1;\` vào tệp phản chiếu -> FAIL" do \
  "(3) dòng mã của tệp phản chiếu không đúng khuôn" "BP_XS_PX"

dung_ban_sao
cam src/app/shared/zz/zz.ts "export const zz = window.matchMedia('(max-width: 1px)');"
ca "D6 matchMedia('(max-width: 1px)') trong .ts dưới shared/ -> FAIL (4)" do \
  "(4) " "shared/zz/zz.ts:1:"

dung_ban_sao
cam src/app/shared/zz/zz.ts "const BP_MD = 1;"
ca "D7 const BP_MD = 1; trong .ts dưới shared/ -> FAIL (5)" do \
  "(5) " "shared/zz/zz.ts:1:"

dung_ban_sao
cam src/app/shared/zz/zz.component.scss "@use 'spacing' as s;
.zz {
  @media (max-width: #{s.\$bp-xs - 1px}) {}
}"
ca "D8 @media (max-width: #{s.\$bp-xs - 1px}) {} trong .scss dưới app/ -> FAIL (6)" do \
  "(6) " "shared/zz/zz.component.scss:3:"

dung_ban_sao
doi "$SP" 's{\$nguong - 0\.02px}{\$nguong - 1px}' '$nguong - 1px'
ca "D9 phép trừ của duoi-man-hinh đổi sang 1px -> FAIL (2)" do \
  "(2) " "duoi-man-hinh" "@media (max-width: \$nguong - 0.02px) {"

dung_ban_sao
rm -f "${DIR:?}/${GUONG:?}"
ca "D10 xoá tệp phản chiếu -> FAIL (3)" do \
  "(3) không có" "diem-ngat.ts"

dung_ban_sao
doi "$SP" 's{^\$bp-[^\n]*\n}{}mg'
ca "D11 xoá mọi dòng \$bp-* của _spacing.scss -> FAIL (1)" do \
  "(1) " "không có dòng"

ket_thuc fe-gate-f58.test.sh
