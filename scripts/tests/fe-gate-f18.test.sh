#!/usr/bin/env bash
# scripts/tests/fe-gate-f18.test.sh — canary tự động cho luật F18 của scripts/fe-gate.sh (không đọc `data` của envelope
# bằng `!`, `as`, `??`).
#
# Chạy trên BẢN SAO của code thật (scripts/tests/_fixture-that.sh), gọi THẲNG scripts/fe-gate.sh qua biến FE, chấm ĐÍCH
# DANH theo riêng khối output của section F18: ca đỏ phải nêu đúng tệp bị cắm; ca xanh phải in số tệp đã quét — bằng
# chứng cổng đã đọc được đầu vào, không xanh rỗng (RULES.md §8 luật T6).
#
# Mỗi dạng bị cấm ở fe-api-client.md §1.2 có một ca đỏ riêng; ca xanh là cửa duy nhất `unwrapData`, và *.spec.ts nằm
# ngoài phạm vi (lệnh gốc ở §1.2 loại *.spec.ts).
#
# T12:cap-xanh-do — mọi ca kỳ vọng XANH có một ca kỳ vọng ĐỎ đi qua cùng cơ chế dò, khác đúng một vi phạm được cắm
# (RULES.md §8 luật T12); chú thích "cặp đỏ:" ngay trên mỗi ca xanh nói cặp ở đâu.
#
# Chạy từ gốc repo:
#     bash scripts/tests/fe-gate-f18.test.sh
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
ca() { cham_dot_bien F18 "$@"; }
XANH_RONG='cổng sẽ xanh rỗng'
SVC=src/app/platform/zz/services/zz.service.ts
DOC_CHAM="    return this.http.get<ApiResult<HoSoDto>>('/core/ho-so').pipe(map((res) => res.data!));"

printf '\n\033[1m== canary F18 — bản sao code thật, gọi thẳng scripts/fe-gate.sh ==\033[0m\n'

# ---------------------------------------------------------------- nền
# Cặp đỏ: ca "res.data!" ngay dưới — cùng bản sao, thêm đúng một service đọc data bằng dạng bị cấm.
dung_ban_sao
ca "bản sao nguyên vẹn -> PASS, có in số tệp đã quét" xanh "tệp đã quét" "!$XANH_RONG"

dung_ban_sao; cam "$SVC" "$DOC_CHAM"
ca "res.data! -> FAIL, nêu đúng tệp" do "services/zz.service.ts:1:" "!$XANH_RONG"

# Cặp đỏ: ca "res.data!" ở trên — cùng tệp, cùng dòng, `(res) => res.data!` thay bằng `unwrapData`.
dung_ban_sao; cam "$SVC" "    return this.http.get<ApiResult<HoSoDto>>('/core/ho-so').pipe(map(unwrapData));"
ca "map(unwrapData) — cửa duy nhất -> PASS" xanh "tệp đã quét"

dung_ban_sao; cam "$SVC" "    return this.http.get<ApiResult<HoSoDto>>('/core/ho-so').pipe(map((res) => res.data as HoSoDto));"
ca "res.data as T -> FAIL" do "services/zz.service.ts:1:"

dung_ban_sao; cam "$SVC" "    return this.http.get<ApiResult<HoSoDto[]>>('/core/ho-so').pipe(map((res) => res.data ?? []));"
ca "res.data ?? [] -> FAIL" do "services/zz.service.ts:1:"

# Cặp đỏ: ca "res.data!" ở trên — CÙNG dòng, tệp *.spec.ts cạnh service thay vì chính service.
dung_ban_sao; cam src/app/platform/zz/services/zz.service.spec.ts "$DOC_CHAM"
ca "*.spec.ts đọc res.data! -> PASS (spec ngoài phạm vi)" xanh "tệp đã quét"

# ---------------------------------------------------------------- chốt tập rỗng (T6)
# Cặp xanh: "bản sao nguyên vẹn" ở đầu tệp.
dung_ban_sao; find "$DIR/src" -name '*.ts' -not -name '*.spec.ts' -delete
ca "không còn tệp *.ts nào ngoài *.spec.ts -> FAIL (không xanh rỗng)" do "$XANH_RONG" "!OK    "

ket_thuc fe-gate-f18.test.sh
