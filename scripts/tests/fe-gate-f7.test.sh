#!/usr/bin/env bash
# scripts/tests/fe-gate-f7.test.sh — canary tự động cho luật F7 của scripts/fe-gate.sh (rgb()/rgba() literal trong SCSS).
#
# Chạy trên BẢN SAO của code thật (scripts/tests/_fixture-that.sh), gọi THẲNG scripts/fe-gate.sh qua biến FE, chấm ĐÍCH
# DANH theo riêng khối output của section F7: ca đỏ phải nêu đúng tệp bị cắm; ca xanh phải in số tệp đã quét — bằng
# chứng cổng đã đọc được đầu vào, không xanh rỗng (RULES.md §8 luật T6).
#
# Ba điều canary này ghim:
#   - chốt tập rỗng: không còn *.scss nào dưới src/app → đỏ;
#   - miễn trừ theo CÚ PHÁP `rgb(var(--x) / a)`, không theo giá trị (fe-ui-conventions.md §3.2);
#   - bẫy 05-gate.md §8.5: dòng vừa có dạng được tha vừa có literal trần vẫn đỏ — cổng gỡ dạng được tha TRƯỚC rồi mới hỏi.
#
# T12:cap-xanh-do — mọi ca kỳ vọng XANH có một ca kỳ vọng ĐỎ đi qua cùng cơ chế dò, khác đúng một vi phạm được cắm
# (RULES.md §8 luật T12); chú thích "cặp đỏ:" ngay trên mỗi ca xanh nói cặp ở đâu.
#
# Chạy từ gốc repo:
#     bash scripts/tests/fe-gate-f7.test.sh
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
ca() { cham_dot_bien F7 "$@"; }
XANH_RONG='cổng sẽ xanh rỗng'
SCSS=src/app/platform/zz/zz.component.scss

printf '\n\033[1m== canary F7 — bản sao code thật, gọi thẳng scripts/fe-gate.sh ==\033[0m\n'

# ---------------------------------------------------------------- nền
# Cặp đỏ: ca "component *.scss mang rgb(0 0 0)" ngay dưới — cùng bản sao, thêm đúng một tệp mang literal.
dung_ban_sao
ca "bản sao nguyên vẹn -> PASS, có in số tệp đã quét" xanh "tệp đã quét" "!$XANH_RONG"

dung_ban_sao; cam "$SCSS" '.zz { color: rgb(0 0 0); }'
ca "component *.scss mang rgb(0 0 0) -> FAIL, nêu đúng tệp" do "zz/zz.component.scss:1:" "!$XANH_RONG"

dung_ban_sao; cam "$SCSS" '.zz { color: rgba(0, 0, 0, 0.1); }'
ca "component *.scss mang rgba(0, 0, 0, 0.1) -> FAIL" do "zz/zz.component.scss:1:"

# ---------------------------------------------------------------- miễn trừ theo cú pháp
# Cặp đỏ: ca ngay dưới — cùng tệp, cùng dòng, `var(--color-overlay-rgb)` thay bằng giá trị `0 0 0`.
dung_ban_sao; cam "$SCSS" '.zz { color: rgb(var(--color-overlay-rgb) / 0.5); }'
ca "dạng đọc token pha alpha rgb(var(--x) / a) -> PASS" xanh "tệp đã quét"

dung_ban_sao; cam "$SCSS" '.zz { color: rgb(0 0 0 / 0.5); }'
ca "cùng dòng, giá trị literal thay var(--x) -> FAIL (không miễn trừ theo giá trị)" do "zz/zz.component.scss:1:"

# Bẫy §8.5 — cặp xanh của ca này: "dạng đọc token pha alpha" ở trên (cùng tệp, dòng này chỉ thêm một literal trần).
dung_ban_sao; cam "$SCSS" '.zz { box-shadow: 0 0 0 1px rgb(var(--color-overlay-rgb) / 0.5), 0 1px 2px rgba(0, 0, 0, 0.1); }'
ca "BẪY §8.5: một dòng có cả dạng được tha lẫn literal trần -> FAIL" do "zz/zz.component.scss:1:"

# ---------------------------------------------------------------- chốt tập rỗng (T6)
# Cặp xanh: "bản sao nguyên vẹn" ở đầu tệp.
dung_ban_sao; find "$DIR/src/app" -name '*.scss' -delete
ca "không còn tệp *.scss nào dưới src/app -> FAIL (không xanh rỗng)" do "$XANH_RONG" "!OK    "

ket_thuc fe-gate-f7.test.sh
