#!/usr/bin/env bash
# scripts/tests/fe-gate-f3.test.sh — canary tự động cho luật F3 của scripts/fe-gate.sh.
#
# Dựng fixture TẠM (mktemp -d) đóng vai src/FE giả, KHÔNG đụng src/FE thật, rồi gọi THẲNG
# scripts/fe-gate.sh qua biến môi trường FE. Fixture tối giản KHÔNG làm mọi section khác xanh
# (F5 đỏ vì allowlist trỏ tới tệp không có trong fixture), nên exit code toàn script không cô lập
# được F3 — ca được chấm theo riêng khối output của section F3: đỏ khi khối đó có dòng FAIL.
#
# Chạy từ gốc repo:
#     bash scripts/tests/fe-gate-f3.test.sh
#
# Kỳ vọng mỗi ca: 'do' (section F3 có FAIL) hoặc 'xanh' (section F3 có OK, không FAIL).
#
# Thoát 0 = mọi ca đúng kỳ vọng · 1 = có ca sai kỳ vọng · 2 = không chạy được (thiếu script).

set -uo pipefail

T="$(cd "$(dirname "$0")" && pwd)"
REPO="$(cd "$T/../.." && pwd)"
GATE="$REPO/scripts/fe-gate.sh"

[ -f "$GATE" ] || { echo "ABORT — không thấy $GATE"; exit 2; }
cd "$REPO" || exit 2

NFAIL=0

make_fixture() {
  local dir
  dir="$(mktemp -d)" || { echo "ABORT — mktemp -d thất bại"; exit 2; }
  mkdir -p "$dir/src/app/core" "$dir/src/app/platform/shell"
  printf 'export const routes = [];\n' > "$dir/src/app/app.routes.ts"
  printf 'module.exports = { BUSINESS_MODULES: [] };\n' > "$dir/eslint.boundaries.cjs"
  printf '<app-sidebar></app-sidebar><app-topbar></app-topbar><main><router-outlet></router-outlet></main>' \
    > "$dir/src/app/platform/shell/shell.component.html"
  printf '%s' "$dir"
}

# run_case <tên ca> <đuôi tệp: ts|html|…> <nội dung tệp> <kỳ vọng: do|xanh> [XOA_MA_NGUON]
# Đối số thứ năm `XOA_MA_NGUON` gỡ mọi *.ts / *.html của fixture trước khi chạy — ca chốt chống
# xanh rỗng.
run_case() {
  local name="$1" ext="$2" body="$3" expect="$4" che_do="${5:-}"
  local dir; dir="$(make_fixture)"
  printf '%s\n' "$body" > "$dir/src/app/core/canary.$ext"
  if [ "$che_do" = XOA_MA_NGUON ]; then
    find "$dir/src" \( -name '*.ts' -o -name '*.html' \) -delete
  fi

  local out f3 actual
  out="$(FE="$dir" bash "$GATE" 2>&1)"
  rm -rf "$dir"
  f3="$(printf '%s\n' "$out" | awk '/== F3 /{f=1; print; next} /== F[0-9]/{f=0} f')"
  if printf '%s\n' "$f3" | grep -q 'FAIL'; then
    actual=do
  elif printf '%s\n' "$f3" | grep -q 'OK'; then
    actual=xanh
  else
    actual=khong-thay-section
  fi

  if [ "$actual" = "$expect" ]; then
    printf '  \033[32mOK\033[0m    %-78s F3=%s (kỳ vọng %s)\n' "$name" "$actual" "$expect"
  else
    printf '  \033[31mFAIL\033[0m  %-78s F3=%s (kỳ vọng %s)\n' "$name" "$actual" "$expect"
    printf '%s\n' "$f3"
    NFAIL=$((NFAIL + 1))
  fi
}

IMP="import { x } from '../platform/shell/x';"

printf '\n\033[1m== canary F3 — gọi thẳng scripts/fe-gate.sh qua fixture tạm ==\033[0m\n'

# --- dạng KHÔNG kèm tên rule: tắt mọi rule, kể cả rule ranh giới -> FAIL
run_case "// eslint-disable-next-line không tên rule -> FAIL" ts \
  "// eslint-disable-next-line
$IMP" do
run_case "/* eslint-disable-next-line */ không tên rule -> FAIL" ts \
  "/* eslint-disable-next-line */
$IMP" do
run_case "// eslint-disable-line không tên rule (cuối dòng import) -> FAIL" ts \
  "$IMP // eslint-disable-line" do
run_case "/* eslint-disable */ toàn file -> FAIL" ts \
  "/* eslint-disable */
$IMP" do
run_case "/*eslint-disable*/ toàn file, không khoảng trắng -> FAIL" ts \
  "/*eslint-disable*/
$IMP" do
run_case "// eslint-disable-next-line -- lý do (có mô tả, vẫn không tên rule) -> FAIL" ts \
  "// eslint-disable-next-line -- tạm thời
$IMP" do
run_case "dòng CRLF: // eslint-disable-next-line\\r -> FAIL" ts \
  "$(printf '// eslint-disable-next-line\r')
$IMP" do
run_case "<!-- eslint-disable --> trong .html -> FAIL" html \
  '<!-- eslint-disable -->
<img src="a.png">' do
run_case "<!-- eslint-disable-next-line --> trong .html -> FAIL" html \
  '<!-- eslint-disable-next-line -->
<img src="a.png">' do

# --- dạng kèm tên rule trong danh sách cấm tắt -> FAIL
run_case "// eslint-disable-next-line import/no-restricted-paths -> FAIL" ts \
  "// eslint-disable-next-line import/no-restricted-paths
$IMP" do
run_case "<!-- eslint-disable-next-line @angular-eslint/template/alt-text --> -> FAIL" html \
  '<!-- eslint-disable-next-line @angular-eslint/template/alt-text -->
<img src="a.png">' do

# --- comment KHỐI nhắc tên rule cấm tắt: cấu hình nội tuyến và eslint-disable trải nhiều dòng -> FAIL.
# Mọi ca dưới đây đã thử trực tiếp bằng `npx eslint --stdin`: ESLint thoát 0 dù có vi phạm ranh giới,
# trừ ca `// eslint …` (ESLint 9 bỏ qua cấu hình nội tuyến trong comment dòng — cổng vẫn chặn, xem
# fe-architecture.md §4.5).
run_case "/* eslint import/no-restricted-paths: \"off\" */ -> FAIL" ts \
  "/* eslint import/no-restricted-paths: \"off\" */
$IMP" do
run_case "/*eslint import/no-restricted-paths:0*/ không khoảng trắng -> FAIL" ts \
  "/*eslint import/no-restricted-paths:0*/
$IMP" do
run_case "/* eslint … */ trải nhiều dòng, tên rule ở dòng sau -> FAIL" ts \
  "/* eslint
   import/no-restricted-paths: \"off\"
*/
$IMP" do
run_case "/* eslint rule-khác, rule-cấm */ tắt nhiều rule cùng lúc -> FAIL" ts \
  "/* eslint @typescript-eslint/no-explicit-any: \"off\", import/no-restricted-paths: \"off\" */
$IMP" do
run_case "dòng CRLF: /* eslint import/no-restricted-paths: 0 */\\r -> FAIL" ts \
  "$(printf '/* eslint import/no-restricted-paths: 0 */\r')
$IMP" do
run_case "// eslint import/no-restricted-paths: \"off\" (comment dòng) -> FAIL" ts \
  "// eslint import/no-restricted-paths: \"off\"
$IMP" do
run_case "<!-- eslint @angular-eslint/template/alt-text: \"off\" --> trong .html -> FAIL" html \
  '<!-- eslint @angular-eslint/template/alt-text: "off" -->
<img src="a.png">' do
run_case "<!-- eslint … --> trải nhiều dòng trong .html -> FAIL" html \
  '<!--
  eslint @angular-eslint/template/alt-text: "off"
-->
<img src="a.png">' do
run_case "<!-- eslint … --> trong template nội tuyến của .ts -> FAIL" ts \
  "@Component({ selector: 'app-x', template: \`<!-- eslint @angular-eslint/template/alt-text: \"off\" -->
<img src=\"a.png\">\` })
export class X {}" do
run_case "/* eslint-disable rule-khác,⏎ rule-cấm */ tên rule cấm ở dòng sau -> FAIL" ts \
  "/* eslint-disable @typescript-eslint/no-explicit-any,
   import/no-restricted-paths */
$IMP" do
run_case "<!-- eslint-disable-next-line⏎ rule-cấm --> trong .html -> FAIL" html \
  '<!-- eslint-disable-next-line
     @angular-eslint/template/alt-text -->
<img src="a.png">' do

# --- comment khối không nhắc rule cấm, hoặc chỉ trông giống -> PASS
run_case "/* eslint @typescript-eslint/no-explicit-any: \"off\" */ (rule ngoài danh sách) -> PASS" ts \
  "/* eslint @typescript-eslint/no-explicit-any: \"off\" */
export const y: any = 1;" xanh
run_case "/* eslint-enable import/no-restricted-paths */ (bật lại, không tắt) -> PASS" ts \
  "/* eslint-enable import/no-restricted-paths */
export const y = 1;" xanh
run_case "chú thích văn xuôi nhắc tên rule, không mang nhãn eslint -> PASS" ts \
  "/* Rule import/no-restricted-paths canh luật F1 — không tắt nó. */
export const y = 1;" xanh
run_case "chuỗi glob '/**/*.ts' không bị hiểu nhầm thành comment -> PASS" ts \
  "export const g = 'src/**/*.ts';
/* import/no-restricted-paths */
export const y = 1;" xanh

# --- dạng kèm tên rule NGOÀI danh sách cấm tắt -> PASS (đúng dòng hợp lệ đang có ở shared/ui/autocomplete)
run_case "// eslint-disable-next-line @angular-eslint/no-output-native -> PASS" ts \
  "// eslint-disable-next-line @angular-eslint/no-output-native
export const y = 1;" xanh
run_case "/* eslint-disable @typescript-eslint/no-explicit-any */ -> PASS" ts \
  "/* eslint-disable @typescript-eslint/no-explicit-any */
export const y: any = 1;" xanh
run_case "không có comment tắt nào -> PASS" ts \
  "export const y = 1;" xanh

# --- chốt chống xanh rỗng: không còn tệp *.ts / *.html nào để quét -> FAIL
run_case "không còn tệp *.ts / *.html nào để quét -> FAIL (không xanh rỗng)" md \
  "không phải mã nguồn" do XOA_MA_NGUON

printf '\n'
if [ "$NFAIL" -eq 0 ]; then
  printf '\033[32m✔ fe-gate-f3.test.sh PASS — mọi ca canary đúng kỳ vọng\033[0m\n'
  exit 0
else
  printf '\033[31m✘ fe-gate-f3.test.sh FAIL — %s ca sai kỳ vọng, xem chi tiết ở trên\033[0m\n' "$NFAIL"
  exit 1
fi
