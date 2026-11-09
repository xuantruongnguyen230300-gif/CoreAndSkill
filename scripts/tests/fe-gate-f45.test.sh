#!/usr/bin/env bash
# scripts/tests/fe-gate-f45.test.sh — canary tự động cho luật F45 của scripts/fe-gate.sh (mã cho phản hồi
# không envelope chỉ chọn ở core/http/; ba mã CORE.CLIENT.(NO_CONNECTION|SERVER_UNAVAILABLE|INVALID_RESPONSE)
# không xuất hiện ngoài core/http/).
#
# Chạy trên BẢN SAO của code thật (scripts/tests/_fixture-that.sh), gọi THẲNG scripts/fe-gate.sh qua biến FE,
# chấm ĐÍCH DANH theo riêng khối output của section F45: ca đỏ nêu đúng tệp vi phạm; ca xanh in số tệp đã quét
# (bằng chứng cổng đã đọc đầu vào, không xanh rỗng — RULES.md §8 luật T6).
#
# T12:cap-xanh-do — mỗi ca XANH có một ca ĐỎ cùng cơ chế dò, khác đúng một vi phạm; chú thích "cặp đỏ:" /
# "cặp xanh:" ngay trên ca nói cặp ở đâu.
#
# Chạy từ gốc repo:
#     bash scripts/tests/fe-gate-f45.test.sh
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
ca() { cham_dot_bien F45 "$@"; }
XANH_RONG='cổng sẽ xanh rỗng'
STORE=src/app/platform/zz/zz.store.ts
TRONG_HTTP=src/app/core/http/zz-http.ts

printf '\n\033[1m== canary F45 — bản sao code thật, gọi thẳng scripts/fe-gate.sh ==\033[0m\n'

# Cặp đỏ: ca "store giả mang literal" ngay dưới — cùng bản sao.
dung_ban_sao
ca "bản sao nguyên vẹn -> PASS, có in số tệp đã quét" xanh "tệp đã quét" "!$XANH_RONG"

# ---------------------------------------------------------------- store giả dưới platform/
# Cặp: ca đỏ và ca xanh ngay dưới — cùng store, đỏ khi tự tra khoá, xanh khi gọi hàm của core/http/.
dung_ban_sao
cam "$STORE" "export class ZzStore { cau(t: { instant(k: string): string }) { return t.instant('loi.CORE.CLIENT.NO_CONNECTION'); } }"
ca "store giả dưới platform/ mang literal loi.CORE.CLIENT.NO_CONNECTION -> FAIL, nêu đúng tệp" do "platform/zz/zz.store.ts" "!$XANH_RONG"

dung_ban_sao
cam "$STORE" "import { cauLoiKhongPhaiApi } from '../../core/http/dich-loi';
export class ZzStore { cau(t: never) { return cauLoiKhongPhaiApi(t); } }"
ca "cùng store gọi cauLoiKhongPhaiApi của core/http/ -> PASS" xanh "tệp đã quét" "!platform/zz/zz.store.ts"

# ---------------------------------------------------------------- hai mã còn lại + mã trơn (bỏ tiền tố loi.)
for ma in SERVER_UNAVAILABLE INVALID_RESPONSE; do
  dung_ban_sao
  cam "$STORE" "export const ZZ = 'loi.CORE.CLIENT.$ma';"
  ca "literal loi.CORE.CLIENT.$ma dưới platform/ -> FAIL" do "platform/zz/zz.store.ts"
done

# Cặp xanh: ca "bản sao nguyên vẹn" (không tệp nào ngoài core/http/ mang mã trơn).
dung_ban_sao
cam "$STORE" "export const ZZ: string = 'CORE.CLIENT.NO_CONNECTION';"
ca "mã TRƠN CORE.CLIENT.NO_CONNECTION (không tiền tố loi.) dưới platform/ -> FAIL" do "platform/zz/zz.store.ts"

# ---------------------------------------------------------------- literal trong core/http/ được phép
# Cặp đỏ: ca ngay dưới — cùng literal, đặt ở core/ nhưng ngoài core/http/.
dung_ban_sao
cam "$TRONG_HTTP" "export const ZZ = 'loi.CORE.CLIENT.NO_CONNECTION';"
ca "literal nằm TRONG core/http/ -> PASS (nơi duy nhất được tra)" xanh "tệp đã quét" "!zz-http.ts"

dung_ban_sao
cam src/app/core/zz-ngoai-http.ts "export const ZZ = 'loi.CORE.CLIENT.NO_CONNECTION';"
ca "cùng literal ở core/ nhưng NGOÀI core/http/ -> FAIL" do "core/zz-ngoai-http.ts"

# ---------------------------------------------------------------- literal VALIDATION_* và spec được phép
# Cặp đỏ: ca "store giả mang literal" ở trên (cùng nơi đặt, khác mã).
dung_ban_sao
cam "$STORE" "export const ZZ = 'loi.CORE.CLIENT.VALIDATION_REQUIRED';"
ca "loi.CORE.CLIENT.VALIDATION_* dưới platform/ -> PASS (chỉ ba mã bị cấm)" xanh "tệp đã quét" "!platform/zz/zz.store.ts"

dung_ban_sao
cam src/app/platform/zz/zz.store.spec.ts "const k = 'loi.CORE.CLIENT.NO_CONNECTION';"
ca "literal trong *.spec.ts dưới platform/ -> PASS (spec không bị quét)" xanh "tệp đã quét" "!zz.store.spec.ts"

# ---------------------------------------------------------------- chốt tập rỗng (T6)
# Cặp xanh: "bản sao nguyên vẹn" ở đầu tệp.
dung_ban_sao; find "$DIR/src/app" -name '*.ts' -not -path '*/core/http/*' -delete
ca "không còn tệp *.ts nào ngoài core/http/ -> FAIL (không xanh rỗng)" do "$XANH_RONG" "!OK    "

ket_thuc fe-gate-f45.test.sh
