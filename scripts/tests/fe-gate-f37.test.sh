#!/usr/bin/env bash
# scripts/tests/fe-gate-f37.test.sh — canary tự động cho luật F37 của scripts/fe-gate.sh.
#
# Chạy trên BẢN SAO của code thật (scripts/tests/_fixture-that.sh), cắm đúng một vi phạm mỗi ca,
# gọi THẲNG scripts/fe-gate.sh qua biến FE, chấm theo riêng khối output của section F37.
#
# HAI NỬA được canh RIÊNG, vì chúng hỏng theo hai cách khác nhau:
#   (1) TRUY NGƯỢC — hằng số không nêu token nào. Dễ thấy, dễ sửa.
#   (2) ĐỐI CHIẾU GIÁ TRỊ — chú thích vẫn nêu đúng tên token, nhưng GIÁ TRỊ hai bên đã trôi khỏi
#       nhau. Nửa (1) xanh trong ca này; chỉ nửa (2) bắt được nó, và đây là lớp lỗi mà ADR-0073
#       dựng luật F37 để chặn. Ca "token đổi, hằng đứng yên" và ca "hằng đổi, token đứng yên" có
#       mặt riêng ở dưới vì chúng là hai chiều trôi khác nhau.
#
# NGOẠI LỆ CÓ TÊN (TOOLTIP_DELAY_CHUOT_MS) cũng có hai chiều hỏng: ngoại lệ thôi tự khai lý do, và
# ngoại lệ trỏ vào một hằng đã đổi tên — mục ruỗng, thứ không làm gì đỏ mà vẫn nới cổng ra.
#
# Chạy từ gốc repo:
#     bash scripts/tests/fe-gate-f37.test.sh
#
# Thoát 0 = mọi ca đúng kỳ vọng · 1 = có ca sai kỳ vọng · 2 = không chạy được.

set -uo pipefail

T="$(cd "$(dirname "$0")" && pwd)"
REPO="$(cd "$T/../.." && pwd)"
[ -f "$REPO/scripts/fe-gate.sh" ] || { echo "ABORT — không thấy scripts/fe-gate.sh"; exit 2; }
[ -d "$REPO/src/FE/src/app/shared/ui" ] || { echo "ABORT — không thấy src/FE/src/app/shared/ui để chép"; exit 2; }
cd "$REPO" || exit 2
# shellcheck source=_fixture-that.sh
. "$T/_fixture-that.sh"

NFAIL=0
TOKENS=src/styles/_tokens.scss
TOAST=src/app/shared/ui/toast/toast.component.ts
DIALOG=src/app/shared/ui/dialog/dialog.component.ts
TOOLTIP=src/app/shared/ui/tooltip/tooltip.component.ts

printf '\n\033[1m== canary F37 — bản sao code thật, gọi thẳng scripts/fe-gate.sh ==\033[0m\n'

dung_ban_sao
cham F37 "bản sao nguyên vẹn -> PASS" xanh

# --- nửa (1): truy ngược
dung_ban_sao
sed -i 's/--dur-slow/thoi luong cham/g' "$DIR/$TOAST"
cham F37 "TOAST_THOI_LUONG_VAO_MS thôi nêu tên token -> FAIL" do

dung_ban_sao
sed -i 's/--dur-slow/--dur-khongcotrongtokens/g' "$DIR/$TOAST"
cham F37 "hằng nêu một token KHÔNG có trong _tokens.scss -> FAIL" do

dung_ban_sao
sed -i 's|^export const DIALOG_THOI_LUONG_MS = 200;|\nexport const DIALOG_THOI_LUONG_MS = 200;|' "$DIR/$DIALOG"
cham F37 "chú thích không còn LIỀN TRÊN khai báo -> FAIL (cổng đọc khối ngay trên, không đọc cả tệp)" do

# --- nửa (2): đối chiếu giá trị — nửa duy nhất bắt được ca hai nơi trôi khỏi nhau
dung_ban_sao
sed -i 's/--dur-slow: 320ms;/--dur-slow: 400ms;/' "$DIR/$TOKENS"
cham F37 "token đổi giá trị, hằng đứng yên -> FAIL (nửa truy ngược vẫn xanh ở ca này)" do

dung_ban_sao
sed -i 's/export const TOAST_THOI_LUONG_VAO_MS = 320;/export const TOAST_THOI_LUONG_VAO_MS = 300;/' "$DIR/$TOAST"
cham F37 "hằng đổi giá trị, token đứng yên -> FAIL" do

dung_ban_sao
sed -i 's/--dur-base: 200ms;/--dur-base: 240ms;/' "$DIR/$TOKENS"
cham F37 "một token đổi kéo theo HAI hằng lệch (Dialog + Toast ra) -> FAIL" do

# --- ngoại lệ có tên
dung_ban_sao
sed -i 's/tham số hành vi/mot tham so/g' "$DIR/$TOOLTIP"
cham F37 "ngoại lệ thôi tự khai lý do 'tham số hành vi' -> FAIL" do

dung_ban_sao
sed -i 's/TOOLTIP_DELAY_CHUOT_MS/TOOLTIP_DELAY_CHUOT_MOI_MS/g' "$DIR/$TOOLTIP"
cham F37 "ngoại lệ có tên trỏ vào hằng đã đổi tên (mục ruỗng) -> FAIL" do

# --- chốt chống xanh rỗng: bộ đọc chết trông y hệt 'không có vi phạm'
dung_ban_sao
sed -i '/--dur-/d' "$DIR/$TOKENS"
cham F37 "đọc được 0 token --dur-* -> FAIL, không phải 'không có vi phạm'" do

dung_ban_sao
rm -f "$DIR/$TOKENS"
cham F37 "thiếu hẳn _tokens.scss -> FAIL" do

dung_ban_sao
rm -rf "$DIR/src/app/shared/ui"
cham F37 "không có shared/ui để quét -> FAIL" do

dung_ban_sao
sed -i 's/^export const \([A-Z_]*\)_MS = /const \1_MS = /' "$DIR/$TOAST" "$DIR/$DIALOG" "$DIR/$TOOLTIP"
cham F37 "đọc được 0 hằng *_MS -> FAIL, không phải PASS" do

# --- không báo sai
dung_ban_sao
cam src/app/shared/ui/zz/zz.component.spec.ts 'export const ZZ_SAI_MS = 999;'
cham F37 "*.spec.ts khai một hằng *_MS không nêu token -> PASS (phạm vi là mã chạy thật)" xanh

dung_ban_sao
cam src/app/shared/ui/zz/zz.component.ts '/**
 * Thời lượng gì đó — phản chiếu token **`--dur-fast`** (`120ms`, src/styles/_tokens.scss).
 */
export const ZZ_DUNG_MS = 120;'
cham F37 "hằng mới nêu đúng token và khớp giá trị -> PASS" xanh

dung_ban_sao
cam src/app/shared/ui/zz/zz.component.ts '/**
 * Thời lượng gì đó — phản chiếu token **`--dur-fast`** (`120ms`, src/styles/_tokens.scss).
 */
export const ZZ_LECH_MS = 130;'
cham F37 "hằng mới nêu đúng token nhưng LỆCH giá trị -> FAIL" do

dung_ban_sao
cam src/app/shared/ui/zz/zz.component.ts '/** Thoi luong gi do, khong neu token nao. */
export const ZZ_THIEU_MS = 120;'
cham F37 "hằng mới không nêu token nào -> FAIL (ngoại lệ chỉ có một tên, không mở thêm)" do

ket_thuc fe-gate-f37.test.sh
