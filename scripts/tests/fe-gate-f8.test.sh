#!/usr/bin/env bash
# scripts/tests/fe-gate-f8.test.sh — canary tự động cho luật F8 của scripts/fe-gate.sh.
#
# F8 có hai vế, cùng một câu luật "mọi câu đến từ i18n":
#   (1) template .html không chứa chữ tiếng Việt;
#   (2) mọi khoá dịch viết literal trong src/app có trong tệp dịch vi.json — khoá thiếu thì người
#       dùng thấy chuỗi khoá thô, tức câu đó KHÔNG đến từ i18n.
#
# Chạy trên BẢN SAO của code thật (scripts/tests/_fixture-that.sh), cắm đúng một vi phạm mỗi ca,
# gọi THẲNG scripts/fe-gate.sh qua biến FE, chấm theo riêng khối output của section F8.
#
# Chạy từ gốc repo:
#     bash scripts/tests/fe-gate-f8.test.sh
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
HTML=src/app/platform/zz/zz.page.html
TS=src/app/platform/zz/zz.page.ts

printf '\n\033[1m== canary F8 — bản sao code thật, gọi thẳng scripts/fe-gate.sh ==\033[0m\n'

dung_ban_sao
cham F8 "bản sao nguyên vẹn -> PASS" xanh

# --- vế (1): chữ tiếng Việt trong template
dung_ban_sao; cam "$HTML" "<p>Xin chào</p>"
cham F8 "<p>Xin chào</p> trong .html -> FAIL" do

dung_ban_sao; cam "$HTML" "<!-- Ghi chú tiếng Việt,
     trải hai dòng -->
<p>{{ 'chung.luu' | translate }}</p>"
cham F8 "chữ tiếng Việt chỉ nằm trong chú thích HTML -> PASS" xanh

dung_ban_sao; find "$DIR/src/app" -name '*.html' -delete
cham F8 "không còn tệp *.html nào để quét -> FAIL (không xanh rỗng)" do

# --- vế (2): khoá dịch literal phải có trong vi.json
dung_ban_sao; cam "$HTML" "<p>{{ 'khongCo.khoaNay' | translate }}</p>"
cham F8 "'khongCo.khoaNay' | translate trong .html -> FAIL" do

dung_ban_sao; cam "$HTML" "<p>{{
  'khongCo.khoaNay'
    | translate: { a: 1 }
}}</p>"
cham F8 "khoá và pipe bị prettier bẻ sang dòng khác nhau -> FAIL" do

dung_ban_sao; cam "$TS" "const cau = this.translate.instant(
  'khongCo.khoaNay',
  { a: 1 },
);"
cham F8 "translate.instant(⏎ 'khongCo.khoaNay' trong .ts -> FAIL" do

dung_ban_sao; cam "$TS" "this.translate.stream(\"khongCo.khoaNay\").subscribe();"
cham F8 "translate.stream(\"…\") nháy kép -> FAIL" do

dung_ban_sao; cam "$TS" "@Component({ template: \`<p>{{ 'khongCo.khoaNay' | translate }}</p>\` })
export class Zz {}"
cham F8 "khoá thiếu trong template nội tuyến của .ts -> FAIL" do

dung_ban_sao; cam src/app/platform/zz/zz.routes.ts "export const r = [{ path: '', title: 'khongCo.tieuDe' }];"
cham F8 "title: 'khongCo.tieuDe' trong *.routes.ts -> FAIL" do

dung_ban_sao; cam "$HTML" "<p>{{ 'chung.luu' | translate }}</p>"
cham F8 "'chung.luu' có trong vi.json -> PASS" xanh

dung_ban_sao; cam src/app/platform/zz/zz.page.spec.ts "translate.instant('khongCo.khoaNay');"
cham F8 "*.spec.ts nhắc khoá không có -> PASS (phạm vi là mã chạy thật)" xanh

dung_ban_sao; mkdir -p "$DIR/public/i18n-app"; printf '{ "duAn": { "moi": "Mới" } }\n' > "$DIR/public/i18n-app/vi.json"
cam "$HTML" "<p>{{ 'duAn.moi' | translate }}</p>"
cham F8 "khoá chỉ có ở tầng dự án public/i18n-app/vi.json -> PASS" xanh

dung_ban_sao; rm -f "$DIR/public/i18n/vi.json"
cham F8 "không có public/i18n/vi.json -> FAIL" do

dung_ban_sao; printf '{ "chung": ' > "$DIR/public/i18n/vi.json"
cham F8 "vi.json hỏng cú pháp -> FAIL (không coi là tập khoá rỗng)" do

dung_ban_sao
find "$DIR/src/app" \( -name '*.ts' -o -name '*.html' \) -not -path '*/zz/*' -delete
cam "$HTML" "<p>{{ bien | translate }}</p>"
cham F8 "không trích được khoá literal nào -> FAIL (không xanh rỗng)" do

ket_thuc fe-gate-f8.test.sh
