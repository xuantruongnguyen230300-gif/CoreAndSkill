#!/usr/bin/env bash
# scripts/tests/fe-gate-f12.test.sh — canary tự động cho luật F12 của scripts/fe-gate.sh (mọi *.service.ts có .spec.ts
# cạnh nó).
#
# Chạy trên BẢN SAO của code thật (scripts/tests/_fixture-that.sh), gọi THẲNG scripts/fe-gate.sh qua biến FE, chấm ĐÍCH
# DANH theo riêng khối output của section F12: ca đỏ phải nêu đúng service thiếu spec; ca xanh phải in số service đã đối
# chiếu — bằng chứng cổng đã đọc được đầu vào, không xanh rỗng (RULES.md §8 luật T6).
#
# Ghim: chốt tập rỗng (không còn *.service.ts nào → đỏ); spec phải nằm CẠNH service, không phải ở thư mục khác
# (05-gate.md §8.9); service ở core/ cũng bị đối chiếu, không chỉ thư mục services/ (05-gate.md §8.9).
#
# T12:cap-xanh-do — mọi ca kỳ vọng XANH có một ca kỳ vọng ĐỎ đi qua cùng cơ chế dò, khác đúng một vi phạm được cắm
# (RULES.md §8 luật T12); chú thích "cặp đỏ:" ngay trên mỗi ca xanh nói cặp ở đâu.
#
# Chạy từ gốc repo:
#     bash scripts/tests/fe-gate-f12.test.sh
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

# Service thật có spec cạnh nó — test tự tìm trên cây thật, không viết cứng đường dẫn.
SVC_THAT=$(cd "$REAL_FE" && find src/app -name '*.service.ts' | sort | while IFS= read -r f; do
  [ -f "${f%.ts}.spec.ts" ] && { printf '%s\n' "$f"; break; }
done)
[ -n "$SVC_THAT" ] || { echo "ABORT — không thấy *.service.ts thật nào có spec cạnh nó dưới $REAL_FE/src/app"; exit 2; }

NFAIL=0
ca() { cham_dot_bien F12 "$@"; }
XANH_RONG='cổng sẽ xanh rỗng'
SVC=src/app/core/zz/zz.service.ts

printf '\n\033[1m== canary F12 — bản sao code thật, gọi thẳng scripts/fe-gate.sh (service thật dùng làm mẫu: %s) ==\033[0m\n' "$SVC_THAT"

# ---------------------------------------------------------------- nền
# Cặp đỏ: ca ngay dưới — cùng bản sao, xoá đúng một tệp spec thật.
dung_ban_sao
ca "bản sao nguyên vẹn -> PASS, có in số service đã đối chiếu" xanh "service đã đối chiếu" "!$XANH_RONG"

dung_ban_sao; rm -f "$DIR/${SVC_THAT%.ts}.spec.ts"
ca "xoá spec cạnh một service thật -> FAIL, nêu đúng service" do "$SVC_THAT" "!$XANH_RONG"

# ---------------------------------------------------------------- service mới ở core/
# Cặp đỏ: hai ca ngay dưới — cùng service, spec vắng / spec ở thư mục khác.
dung_ban_sao; cam "$SVC" 'export class ZzService {}'; cam src/app/core/zz/zz.service.spec.ts "describe('ZzService', () => {});"
ca "service mới ở core/ có spec cạnh nó -> PASS" xanh "service đã đối chiếu"

dung_ban_sao; cam "$SVC" 'export class ZzService {}'
ca "service mới ở core/ không có spec -> FAIL (core/ cũng bị đối chiếu)" do "core/zz/zz.service.ts"

dung_ban_sao; cam "$SVC" 'export class ZzService {}'; cam src/app/core/zz/__tests__/zz.service.spec.ts "describe('ZzService', () => {});"
ca "spec nằm ở thư mục khác, không cạnh service -> FAIL" do "core/zz/zz.service.ts"

# ---------------------------------------------------------------- chốt tập rỗng (T6)
# Cặp xanh: "bản sao nguyên vẹn" ở đầu tệp.
dung_ban_sao; find "$DIR/src/app" -name '*.service.ts' -delete
ca "không còn tệp *.service.ts nào -> FAIL (không xanh rỗng)" do "$XANH_RONG" "!OK    "

ket_thuc fe-gate-f12.test.sh
