#!/usr/bin/env bash
# scripts/tests/fe-gate-f38.test.sh — canary tự động cho luật F38 của scripts/fe-gate.sh.
#
# Chạy trên BẢN SAO của code thật (scripts/tests/_fixture-that.sh), mỗi ca một đột biến, gọi THẲNG
# scripts/fe-gate.sh qua biến FE, chấm theo riêng khối output của section F38 — và chấm ĐÍCH DANH:
# ca đỏ phải nêu đúng khoá + giá trị + dòng bị bắt, ca xanh phải nêu đúng giá trị cổng đã dò. "Có
# FAIL ở đâu đó" không phân biệt đỏ đúng chỗ với đỏ vì một chốt khác; "có OK" không phân biệt cổng
# đã dò với cổng xanh rỗng.
#
# T12:cap-xanh-do — MỌI CA XANH ĐI KÈM MỘT CA ĐỎ DÙNG CHUNG CƠ CHẾ DÒ (RULES.md §8 luật T12): cùng
# section, cùng bản sao, khác đúng một vi phạm được cắm. Ca xanh đứng một mình không chứng minh được
# gì: đột biến có thể đã không tới được chỗ cổng đọc. Cặp nào nằm ở đâu thì chú thích ngay trên ca
# xanh nói ra.
#
# Giá trị name/shortName THẬT của app.config.ts được test đọc bằng phép đọc RIÊNG, cố ý khác phép
# đọc của cổng (theo vị trí trên một dòng, không theo cú pháp) — dùng chung bộ đọc thì bộ đọc sai cả
# hai bên vẫn xanh. Dạng một dòng đổi thì test ABORT, không đoán.
#
# Mỗi đột biến được kiểm là ĐÃ VÀO bản sao trước khi chấm (hàm `doi` của _fixture-that.sh) — một sed
# không khớp gì để lại bản sao nguyên vẹn, và ca xanh trên bản sao nguyên vẹn là ca xanh rỗng.
#
# Chạy từ gốc repo:
#     bash scripts/tests/fe-gate-f38.test.sh
#
# Thoát 0 = mọi ca đúng kỳ vọng · 1 = có ca sai kỳ vọng · 2 = không chạy được.

set -uo pipefail

T="$(cd "$(dirname "$0")" && pwd)"
REPO="$(cd "$T/../.." && pwd)"
[ -f "$REPO/scripts/fe-gate.sh" ] || { echo "ABORT — không thấy scripts/fe-gate.sh"; exit 2; }
[ -f "$REPO/src/FE/src/index.html" ] || { echo "ABORT — không thấy src/FE/src/index.html để chép"; exit 2; }
[ -f "$REPO/src/FE/src/app/app.config.ts" ] || { echo "ABORT — không thấy src/FE/src/app/app.config.ts"; exit 2; }
cd "$REPO" || exit 2
# shellcheck source=_fixture-that.sh
. "$T/_fixture-that.sh"

NFAIL=0
INDEX=src/index.html
CAU_HINH=src/app/app.config.ts
APP_HTML=src/app/app.html

# ---------------------------------------------------------------- phép đọc của TEST
# Một dòng, đúng thứ tự `name` rồi `shortName`, nháy đơn — dạng hôm nay của app.config.ts thật.
DONG_GOI=$(grep -n "provideCoreBranding({ name: '[^']*', shortName: '[^']*' })" "$REAL_FE/$CAU_HINH")
TEN=$(printf '%s\n' "$DONG_GOI" | sed -n "s/.*provideCoreBranding({ name: '\([^']*\)', shortName: '\([^']*\)' }).*/\1/p")
TAT=$(printf '%s\n' "$DONG_GOI" | sed -n "s/.*provideCoreBranding({ name: '\([^']*\)', shortName: '\([^']*\)' }).*/\2/p")
[ "$(printf '%s\n' "$DONG_GOI" | grep -c .)" -eq 1 ] && [ -n "$TEN" ] && [ -n "$TAT" ] || {
  echo "ABORT — không đọc được name/shortName của $CAU_HINH thật theo dạng một dòng; dạng lời gọi đã đổi, cập nhật phép đọc của test"
  exit 2
}
grep -q '<title>.*</title>' "$REAL_FE/$INDEX" || { echo "ABORT — $INDEX thật không có thẻ <title> trên một dòng"; exit 2; }
[ -f "$REAL_FE/$APP_HTML" ] || { echo "ABORT — không thấy $APP_HTML (tệp đối chứng của ca phạm vi)"; exit 2; }

# ---------------------------------------------------------------- đột biến
# ca <tên> <do|xanh> [chuỗi]... — chấm khối F38 đích danh, trừ khi đột biến của ca không vào bản sao
ca() { cham_dot_bien F38 "$@"; }

# tieu_de_them <chuỗi> — nối chuỗi vào CUỐI nội dung thẻ <title> của bản sao
tieu_de_them() { X="$1" doi "$INDEX" 's{<title>(.*?)</title>}{<title>$1 $ENV{X}</title>}s' "$1</title>"; }

# goi_thanh <văn bản> — thay NGUYÊN dòng lời gọi provideCoreBranding({ của bản sao bằng văn bản khác
goi_thanh() { X="$1" doi "$CAU_HINH" 's{^[ \t]*provideCoreBranding\(\{[^\n]*\n}{$ENV{X}\n}m' "${2:-$1}"; }

printf '\n\033[1m== canary F38 — bản sao code thật, gọi thẳng scripts/fe-gate.sh (name=%s, shortName=%s) ==\033[0m\n' "$TEN" "$TAT"

# ---------------------------------------------------------------- nền
# Cặp đỏ: ca "name trong <title>" ngay dưới — cùng bản sao, chỉ thêm đúng tên vào thẻ.
dung_ban_sao
ca "bản sao nguyên vẹn -> PASS, và cổng nêu cả hai giá trị đã dò" xanh \
  "name='$TEN'" "shortName='$TAT'"

# ---------------------------------------------------------------- vi phạm trong index.html
dung_ban_sao; tieu_de_them "· $TEN"
ca "<title> mang name -> FAIL đích danh khoá, giá trị và dòng <title>" do \
  "mang name '$TEN'" "<title>" "!mang shortName"

dung_ban_sao; tieu_de_them "$TAT"
ca "<title> mang shortName đứng riêng -> FAIL đích danh shortName" do \
  "mang shortName '$TAT'" "<title>" "!mang name"

dung_ban_sao
X="$TEN" doi "$INDEX" 's{(<meta charset[^>]*>)}{$1\n    <meta name="application-name" content="$ENV{X}" />}' "application-name"
ca "<head> mang name ở một thẻ meta, ngoài <title> -> FAIL (luật phủ cả <head>)" do \
  "mang name '$TEN'" "application-name"

# Cặp đối chứng: ca "cùng dòng nối vào app.html" ở nhóm phạm vi bên dưới.
dung_ban_sao; printf '<!-- %s -->\n' "$TEN" >> "$DIR/$INDEX"
ca "chú thích HTML nối cuối index.html mang name -> FAIL (chú thích cũng đi tới trình duyệt)" do \
  "mang name '$TEN'" "<!-- $TEN -->"

# ---------------------------------------------------------------- ranh giới khớp nguyên từ
# Cặp đỏ: ca ngay dưới — cùng chỗ cắm (cuối <title>), chỉ khác một ký tự chữ dính sau mã tắt.
# Đây là ca phân định grep -w với grep -F: bản sao thật đã mang 'CSP' trong chú thích, nhưng ca này
# không dựa vào chú thích đó — nó tự dựng ranh giới.
dung_ban_sao; tieu_de_them "${TAT}P"
ca "shortName chỉ là CHUỖI CON của một từ dài hơn (${TAT}P) -> PASS, và vẫn đã dò shortName" xanh \
  "shortName='$TAT'"

dung_ban_sao; tieu_de_them "$TAT ·"
ca "shortName đứng riêng ở đúng chỗ đó -> FAIL" do \
  "mang shortName '$TAT'"

# ---------------------------------------------------------------- cổng đi theo dự án hạ nguồn
ACME="Acme Kho"; ACME_TAT="AK"
GOI_ACME="    provideCoreBranding({ name: '$ACME', shortName: '$ACME_TAT' }),"

dung_ban_sao; goi_thanh "$GOI_ACME"; tieu_de_them "· $ACME"
ca "dự án hạ nguồn đổi name, <title> mang name MỚI -> FAIL đích danh name mới" do \
  "mang name '$ACME'"

# Cặp đỏ: ca ngay trên — cùng lời gọi hạ nguồn, cùng chỗ cắm, chỉ khác tên nào được cắm.
dung_ban_sao; goi_thanh "$GOI_ACME"; tieu_de_them "· $TEN"
ca "dự án hạ nguồn, <title> mang tên CỦA CORE -> PASS (chuỗi cấm không viết cứng trong cổng)" xanh \
  "name='$ACME'" "shortName='$ACME_TAT'" "!name='$TEN'"

# ---------------------------------------------------------------- hình dạng lời gọi — bộ đọc của cổng
GOI_BE_DONG="    provideCoreBranding({
      name: '$TEN',
      shortName: '$TAT',
    }),"
dung_ban_sao; goi_thanh "$GOI_BE_DONG" "      name: '$TEN',"; tieu_de_them "· $TEN"
ca "lời gọi bị prettier bẻ nhiều dòng + name trong <title> -> FAIL" do \
  "mang name '$TEN'"

# Cặp đỏ: ca ngay trên — cùng lời gọi bẻ dòng, chỉ khác <title> có mang tên hay không.
dung_ban_sao; goi_thanh "$GOI_BE_DONG" "      name: '$TEN',"
ca "lời gọi bẻ nhiều dòng, index.html sạch -> PASS, và vẫn đọc ra đủ hai giá trị" xanh \
  "name='$TEN'" "shortName='$TAT'"

dung_ban_sao; goi_thanh "    provideCoreBranding({ name: \"$TEN\", shortName: \"$TAT\" }),"; tieu_de_them "· $TEN"
ca "lời gọi dùng nháy kép + name trong <title> -> FAIL" do \
  "mang name '$TEN'"

dung_ban_sao; goi_thanh "    provideCoreBranding({ shortName: '$TAT', name: '$TEN' }),"; tieu_de_them "$TAT · $TEN"
ca "lời gọi đảo thứ tự khoá + cả hai giá trị trong <title> -> FAIL nêu cả hai" do \
  "mang name '$TEN'" "mang shortName '$TAT'"

dung_ban_sao; goi_thanh "    provideCoreBranding({ name: 'Core\\'s Kho', shortName: '$TAT' }),"; tieu_de_them "· Core's Kho"
ca "name có nháy thoát (Core\\'s Kho) + dạng đã bỏ thoát trong <title> -> FAIL" do \
  "mang name 'Core's Kho'"

VIET="Quản Lý Kho"
dung_ban_sao; goi_thanh "    provideCoreBranding({ name: '$VIET', shortName: 'QLK' }),"; tieu_de_them "· $VIET"
ca "name có dấu tiếng Việt + đúng chuỗi đó trong <title> -> FAIL (so theo byte UTF-8)" do \
  "mang name '$VIET'"

# ---------------------------------------------------------------- chốt chống xanh rỗng
dung_ban_sao; goi_thanh "    provideBranding({ name: '$TEN', shortName: '$TAT' }),"
ca "không còn lời gọi provideCoreBranding( nào -> FAIL, không phải 'không có vi phạm'" do \
  "không thấy lời gọi provideCoreBranding("

dung_ban_sao; goi_thanh "    // provideCoreBranding({ name: '$TEN', shortName: '$TAT' }),"; tieu_de_them "· $TEN"
ca "lời gọi chỉ còn trong CHÚ THÍCH -> FAIL chốt, không đọc giá trị đã chết" do \
  "không thấy lời gọi provideCoreBranding(" "!mang name"

dung_ban_sao
goi_thanh "    // cũ: provideCoreBranding({ name: '$TEN', shortName: '$TAT' }),
    provideCoreBranding(BRANDING_DU_AN)," "provideCoreBranding(BRANDING_DU_AN)"
ca "đối số không phải object literal, bản cũ nằm trong chú thích -> FAIL cả hai khoá" do \
  "của 'name'" "của 'shortName'" "!name='$TEN'"

dung_ban_sao; goi_thanh "    provideCoreBranding({ name: '$TEN', shortName: TEN_TAT }),"; tieu_de_them "$TAT"
ca "shortName không phải literal -> FAIL đích danh shortName (dò một nửa là xanh rỗng một nửa)" do \
  "của 'shortName'" "!của 'name'"

dung_ban_sao; goi_thanh "    provideCoreBranding({ name: '', shortName: '$TAT' }),"
ca "name rỗng -> FAIL đích danh name, không phải mẫu rỗng khớp mọi dòng" do \
  "của 'name'" "!của 'shortName'"

dung_ban_sao; goi_thanh "    provideCoreBranding({ name: '   ', shortName: '$TAT' }),"
ca "name toàn khoảng trắng -> FAIL như name rỗng" do \
  "của 'name'"

dung_ban_sao; rm -f "$DIR/$INDEX"
ca "thiếu hẳn index.html -> FAIL" do \
  "index.html — không có gì để dò"

dung_ban_sao; rm -f "$DIR/$CAU_HINH"
ca "thiếu hẳn app.config.ts -> FAIL" do \
  "app.config.ts — không đọc được tên sản phẩm"

# ---------------------------------------------------------------- phạm vi
# Cặp đỏ: ca ngay dưới — cùng dòng đã nối vào app.html, cắm thêm đúng một vi phạm: dòng đó vào index.html.
dung_ban_sao; printf '<!-- %s -->\n' "$TEN" >> "$DIR/$APP_HTML"
ca "dòng mang name nối vào app.html (không phải index.html) -> PASS (phạm vi là index.html)" xanh \
  "name='$TEN'"

dung_ban_sao; printf '<!-- %s -->\n' "$TEN" >> "$DIR/$APP_HTML"; printf '<!-- %s -->\n' "$TEN" >> "$DIR/$INDEX"
ca "cùng dòng ở app.html VÀ index.html -> FAIL, chỉ nêu index.html" do \
  "mang name '$TEN'" "index.html mang name" "!app.html"

ket_thuc fe-gate-f38.test.sh
