#!/usr/bin/env bash
# scripts/tests/fe-gate-f36.test.sh — canary tự động cho luật F36 của scripts/fe-gate.sh.
#
# Chạy trên BẢN SAO của code thật (scripts/tests/_fixture-that.sh), cắm đúng một vi phạm mỗi ca,
# gọi THẲNG scripts/fe-gate.sh qua biến FE, chấm theo riêng khối output của section F36.
#
# Hai chiều đỏ được canh riêng, vì chúng hỏng theo hai cách khác nhau:
#   (a) hai chuỗi xuất hiện NGOÀI tệp cấp seam — ai đó "sửa lại cho đúng thói quen Angular";
#   (b) một trong hai chuỗi biến mất KHỎI seam — luật vẫn "không có vi phạm" nhưng đã hết hiệu lực,
#       và pipe date/number im lặng rơi về khuôn Anh-Mỹ. Đây là chiều mà một cổng chỉ đếm vi phạm
#       sẽ xanh rỗng (05-gate.md §8.2), nên nó có ca riêng ở dưới.
#
# Chạy từ gốc repo:
#     bash scripts/tests/fe-gate-f36.test.sh
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
SEAM=src/app/core/config/core-i18n.ts
CAU_HINH=src/app/app.config.ts

printf '\n\033[1m== canary F36 — bản sao code thật, gọi thẳng scripts/fe-gate.sh ==\033[0m\n'

dung_ban_sao
cham F36 "bản sao nguyên vẹn -> PASS" xanh

# --- chiều (a): hai chuỗi xuất hiện ngoài seam
dung_ban_sao
sed -i "s|    provideZonelessChangeDetection(),|    { provide: LOCALE_ID, useValue: 'vi' },\n    provideZonelessChangeDetection(),|" "$DIR/$CAU_HINH"
cham F36 "app.config.ts cấp lại LOCALE_ID (đúng hình dạng trước ADR-0063) -> FAIL" do

dung_ban_sao
printf '\nregisterLocaleData(localeVi);\n' >> "$DIR/src/app/core/i18n/dong-bo-ngon-ngu.ts"
cham F36 "một tệp core khác gọi registerLocaleData ngoài seam -> FAIL" do

dung_ban_sao
printf '\n// TODO: nhớ cấp LOCALE_ID ở đây\n' >> "$DIR/$CAU_HINH"
cham F36 "chú thích nhắc LOCALE_ID ở tệp khác cũng đỏ — bản kể lại là nguồn thứ hai -> FAIL" do

# --- chiều (b): chuỗi biến mất khỏi seam
dung_ban_sao
sed -i 's/LOCALE_ID/MA_NGON_NGU/g' "$DIR/$SEAM"
cham F36 "seam thôi cấp LOCALE_ID (0 khớp ở mọi nơi) -> FAIL, không phải 'hết vi phạm'" do

dung_ban_sao
sed -i 's/registerLocaleData/dangKyDuLieuLocale/g' "$DIR/$SEAM"
cham F36 "seam thôi đăng ký dữ liệu locale (0 khớp ở mọi nơi) -> FAIL" do

# --- không báo sai
dung_ban_sao; cam src/app/platform/zz/zz.page.spec.ts "registerLocaleData(localeVi); const x = LOCALE_ID;"
cham F36 "*.spec.ts ngoài seam dùng hai chuỗi -> PASS (phạm vi là mã chạy thật)" xanh

dung_ban_sao; cam src/app/core/config/zz.ts "const MA_LOCALE_IDX = 3; export function dangKyLocale() {}"
cham F36 "định danh chỉ CHỨA chuỗi (MA_LOCALE_IDX) không bị tính -> PASS" xanh

# --- cổng không chạy được là ĐỎ, không phải xanh
dung_ban_sao; rm -f "$DIR/$SEAM"
cham F36 "thiếu hẳn tệp cấp seam -> FAIL" do

dung_ban_sao; rm -rf "$DIR/src/app"
cham F36 "không có src/app để quét -> FAIL" do

ket_thuc fe-gate-f36.test.sh
