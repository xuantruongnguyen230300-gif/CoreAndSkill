#!/usr/bin/env bash
# scripts/tests/fe-gate-f6.test.sh — canary tự động cho luật F6 của scripts/fe-gate.sh (hex literal trong SCSS).
#
# Chạy trên BẢN SAO của code thật (scripts/tests/_fixture-that.sh), gọi THẲNG scripts/fe-gate.sh qua biến FE, chấm ĐÍCH
# DANH theo riêng khối output của section F6: ca đỏ phải nêu đúng tệp bị cắm; ca xanh phải in số tệp đã quét — bằng
# chứng cổng đã đọc được đầu vào, không xanh rỗng (RULES.md §8 luật T6).
#
# Ba điều canary này ghim:
#   - chốt tập rỗng: không còn *.scss nào, hoặc chỉ còn đúng tệp khai token (tệp được loại trừ) → đỏ;
#   - loại trừ ĐÚNG tệp src/styles/_tokens.scss — không loại trừ cả thư mục, không loại trừ theo tên tệp
#     (fe-ui-conventions.md §3.3, 05-gate.md §8.4);
#   - quét cả style toàn cục dưới src/styles, không chỉ src/app (05-gate.md §8.4).
#
# T12:cap-xanh-do — mọi ca kỳ vọng XANH có một ca kỳ vọng ĐỎ đi qua cùng cơ chế dò, khác đúng một vi phạm được cắm
# (RULES.md §8 luật T12); chú thích "cặp đỏ:" ngay trên mỗi ca xanh nói cặp ở đâu.
#
# Chạy từ gốc repo:
#     bash scripts/tests/fe-gate-f6.test.sh
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

TOKEN=src/styles/_tokens.scss
[ -f "$REAL_FE/$TOKEN" ] || { echo "ABORT — không thấy $REAL_FE/$TOKEN — tiền đề của phép loại trừ F6 đã đổi"; exit 2; }

NFAIL=0
ca() { cham_dot_bien F6 "$@"; }
XANH_RONG='cổng sẽ xanh rỗng'

printf '\n\033[1m== canary F6 — bản sao code thật, gọi thẳng scripts/fe-gate.sh ==\033[0m\n'

# ---------------------------------------------------------------- nền
# Cặp đỏ: ca "component *.scss mang #1a2b3c" ngay dưới — cùng bản sao, thêm đúng một tệp mang hex.
dung_ban_sao
ca "bản sao nguyên vẹn -> PASS, có in số tệp đã quét" xanh "tệp đã quét" "!$XANH_RONG"

dung_ban_sao; cam src/app/platform/zz/zz.component.scss '.zz { color: #1a2b3c; }'
ca "component *.scss mang #1a2b3c -> FAIL, nêu đúng tệp" do "zz/zz.component.scss:1:" "!$XANH_RONG"

dung_ban_sao; cam src/app/platform/zz/zz.component.scss '.zz { color: #aabbccdd; }'
ca "hex 8 chữ số (#aabbccdd) -> FAIL" do "zz/zz.component.scss:1:"

dung_ban_sao; cam src/styles/zz.scss '.zz { color: #FFF; }'
ca "style toàn cục src/styles/*.scss mang #FFF -> FAIL (quét cả src/styles, không chỉ src/app)" do "styles/zz.scss:1:"

# ---------------------------------------------------------------- loại trừ đúng MỘT tệp
# Cặp đỏ: hai ca ngay dưới — cùng dòng cắm, đặt ở tệp cùng thư mục / cùng tên khác thư mục.
dung_ban_sao; X='.zz-them { --zz-mau: #1a2b3c; }' doi "$TOKEN" 's{\z}{\n$ENV{X}\n}' '--zz-mau: #1a2b3c'
ca "hex thêm vào chính src/styles/_tokens.scss -> PASS (tệp khai token được loại trừ)" xanh "tệp đã quét"

dung_ban_sao; cam src/styles/_tokens-zz.scss '.zz-them { --zz-mau: #1a2b3c; }'
ca "cùng dòng, tệp khác CÙNG thư mục styles/ -> FAIL (không loại trừ cả thư mục)" do "styles/_tokens-zz.scss:1:"

dung_ban_sao; cam src/app/zz/_tokens.scss '.zz-them { --zz-mau: #1a2b3c; }'
ca "cùng dòng, tệp CÙNG TÊN _tokens.scss ở thư mục khác -> FAIL (không loại trừ theo tên)" do "app/zz/_tokens.scss:1:"

# ---------------------------------------------------------------- nội suy SCSS không phải hex
# Cặp đỏ: ca ngay dưới — cùng tệp, cùng dòng, `#{$a}` thay bằng `#abc`.
dung_ban_sao; cam src/app/platform/zz/zz.component.scss '.zz { --x: #{$a}; }'
ca "nội suy SCSS #{\$a} -> PASS (không phải hex)" xanh "tệp đã quét"

dung_ban_sao; cam src/app/platform/zz/zz.component.scss '.zz { --x: #abc; }'
ca "cùng dòng, #abc thay nội suy -> FAIL" do "zz/zz.component.scss:1:"

# ---------------------------------------------------------------- chốt tập rỗng (T6)
# Cặp xanh của hai ca này: "bản sao nguyên vẹn" ở đầu tệp.
dung_ban_sao; find "$DIR/src" -name '*.scss' -delete
ca "không còn tệp *.scss nào -> FAIL (không xanh rỗng)" do "$XANH_RONG" "!OK    "

dung_ban_sao; find "$DIR/src" -name '*.scss' -not -path "$DIR/$TOKEN" -delete
if [ -f "$DIR/$TOKEN" ]; then
  ca "chỉ còn đúng tệp khai token (tệp được loại trừ) -> FAIL (không xanh rỗng)" do "$XANH_RONG" "!OK    "
else
  HONG=1; NFAIL=$((NFAIL + 1))
  printf '  \033[31mFAIL\033[0m  bản sao mất %s sau khi xoá — ca "chỉ còn tệp khai token" không dựng được\n' "$TOKEN"
  ca "chỉ còn đúng tệp khai token (tệp được loại trừ) -> FAIL (không xanh rỗng)" do
fi

ket_thuc fe-gate-f6.test.sh
