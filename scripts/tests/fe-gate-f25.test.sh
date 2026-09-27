#!/usr/bin/env bash
# scripts/tests/fe-gate-f25.test.sh — canary tự động cho luật F25 của scripts/fe-gate.sh.
#
# Dựng fixture TẠM (mktemp -d) đóng vai src/FE giả, KHÔNG đụng src/FE thật, rồi gọi THẲNG
# scripts/fe-gate.sh qua biến môi trường FE. Fixture tối giản KHÔNG làm mọi section khác xanh,
# nên ca được chấm theo riêng khối output của section F25 — cùng cách fe-gate-f3.test.sh.
#
# Hai nhóm ca:
#   - ca tổng hợp: page viết tay trong fixture, kiểm từng nhánh của detector;
#   - ca trên code THẬT: chép các màn danh sách thật từ src/FE vào fixture — nguyên vẹn phải xanh,
#     gỡ dòng đọc token ở một màn phải đỏ. Nhóm này bắt trường hợp detector chỉ đúng với fixture.
#
# Chạy từ gốc repo:
#     bash scripts/tests/fe-gate-f25.test.sh
#
# Kỳ vọng mỗi ca: 'do' (section F25 có FAIL) hoặc 'xanh' (section F25 có OK, không FAIL).
#
# Thoát 0 = mọi ca đúng kỳ vọng · 1 = có ca sai kỳ vọng · 2 = không chạy được (thiếu script).

set -uo pipefail

T="$(cd "$(dirname "$0")" && pwd)"
REPO="$(cd "$T/../.." && pwd)"
GATE="$REPO/scripts/fe-gate.sh"
REAL_PLATFORM="$REPO/src/FE/src/app/platform"

[ -f "$GATE" ] || { echo "ABORT — không thấy $GATE"; exit 2; }
cd "$REPO" || exit 2

NFAIL=0
DIR=""

make_fixture() {
  DIR="$(mktemp -d)" || { echo "ABORT — mktemp -d thất bại"; exit 2; }
  mkdir -p "$DIR/src/app/platform"
}

# page <đường dẫn tương đối dưới platform/> <nội dung>
page() {
  mkdir -p "$(dirname "$DIR/src/app/platform/$1")"
  printf '%s\n' "$2" > "$DIR/src/app/platform/$1"
}

# check <tên ca> <kỳ vọng: do|xanh> — chạy gate trên fixture hiện tại rồi dọn fixture
check() {
  local name="$1" expect="$2" out sec actual
  out="$(FE="$DIR" bash "$GATE" 2>&1)"
  rm -rf "$DIR"
  sec="$(printf '%s\n' "$out" | awk '/== F25 /{f=1; print; next} /== F[0-9]/ || /fe-gate[.]sh (PASS|FAIL)/ {f=0} f')"
  if printf '%s\n' "$sec" | grep -q 'FAIL'; then
    actual=do
  elif printf '%s\n' "$sec" | grep -q 'OK'; then
    actual=xanh
  else
    actual=khong-thay-section
  fi
  if [ "$actual" = "$expect" ]; then
    printf '  \033[32mOK\033[0m    %-78s F25=%s (kỳ vọng %s)\n' "$name" "$actual" "$expect"
  else
    printf '  \033[31mFAIL\033[0m  %-78s F25=%s (kỳ vọng %s)\n' "$name" "$actual" "$expect"
    printf '%s\n' "$sec"
    NFAIL=$((NFAIL + 1))
  fi
}

DOC="providers: [ListStateStore]"
DOC_EXT="$DOC
readonly moRong = inject(CORE_SCREEN_EXT, { optional: true })?.['users'];"

printf '\n\033[1m== canary F25 — gọi thẳng scripts/fe-gate.sh qua fixture tạm ==\033[0m\n'

# --- ca tổng hợp
make_fixture; page a/pages/ds/ds-a.page.ts "$DOC_EXT"
check "màn danh sách đọc CORE_SCREEN_EXT -> PASS" xanh

make_fixture; page a/pages/ds/ds-a.page.ts "$DOC"
check "màn danh sách KHÔNG đọc CORE_SCREEN_EXT -> FAIL" do

make_fixture; page a/pages/ds/ds-a.page.ts "$DOC_EXT"; page b/pages/ds/ds-b.page.ts "$DOC"
check "hai màn danh sách, một màn quên -> FAIL" do

make_fixture; page a/pages/ds/ds-a.page.ts "$DOC
readonly moRong = inject(
  CORE_SCREEN_EXT,
  { optional: true },
)?.['users'];"
check "prettier bẻ dòng giữa inject( và CORE_SCREEN_EXT -> PASS" xanh

make_fixture; page a/pages/ds/ds-a.page.ts \
  "$(printf '%s\r\nreadonly moRong = inject(\r\n\tCORE_SCREEN_EXT,\r\n  { optional: true },\r\n)?.[%s];\r' \
    "$DOC" "'users'")"
check "tệp CRLF, bẻ dòng và thụt TAB giữa inject( và CORE_SCREEN_EXT -> PASS" xanh

make_fixture; page a/pages/ds/ds-a.page.ts "$DOC_EXT"; page ho-so/pages/ho-so.page.ts "export class HoSoPage {}"
check "màn KHÔNG phải danh sách (không ListStateStore) không bị đòi token -> PASS" xanh

make_fixture; page a/pages/ds/ds-a.page.ts "$DOC_EXT"; page a/pages/ds/ds-a.page.spec.ts "$DOC"
check "*.spec.ts nhắc ListStateStore không bị tính là màn -> PASS" xanh

make_fixture; page ho-so/pages/ho-so.page.ts "export class HoSoPage {}"
check "không tìm thấy màn danh sách nào -> FAIL (không xanh rỗng)" do

make_fixture; rmdir "$DIR/src/app/platform"
check "không có thư mục platform/ -> FAIL" do

# --- ca trên code THẬT
REAL_PAGES=$(find "$REAL_PLATFORM" -name '*.page.ts' -not -name '*.spec.ts' \
  -exec grep -l 'ListStateStore' {} + 2>/dev/null)
if [ -z "$REAL_PAGES" ]; then
  printf '  \033[31mFAIL\033[0m  không tìm thấy màn danh sách thật dưới %s để làm canary\n' "$REAL_PLATFORM"
  NFAIL=$((NFAIL + 1))
else
  copy_real() {
    local f
    while IFS= read -r f; do
      mkdir -p "$(dirname "$DIR/src/app/platform/${f#"$REAL_PLATFORM"/}")"
      cp "$f" "$DIR/src/app/platform/${f#"$REAL_PLATFORM"/}"
    done <<< "$REAL_PAGES"
  }

  make_fixture; copy_real
  check "màn danh sách thật, nguyên vẹn -> PASS" xanh

  while IFS= read -r f; do
    rel="${f#"$REAL_PLATFORM"/}"
    make_fixture; copy_real
    # Gỡ việc đọc token: đổi tên token ở dòng inject — tệp vẫn là một màn danh sách hợp lệ về hình.
    sed -i 's/inject(CORE_SCREEN_EXT/inject(KHONG_DOC_TOKEN/' "$DIR/src/app/platform/$rel"
    if grep -q 'inject(KHONG_DOC_TOKEN' "$DIR/src/app/platform/$rel"; then
      check "gỡ inject(CORE_SCREEN_EXT ở $rel -> FAIL" do
    else
      rm -rf "$DIR"
      printf '  \033[31mFAIL\033[0m  %s không có dòng inject(CORE_SCREEN_EXT trên một dòng để gỡ — canary không cắm được\n' "$rel"
      NFAIL=$((NFAIL + 1))
    fi
  done <<< "$REAL_PAGES"
fi

printf '\n'
if [ "$NFAIL" -eq 0 ]; then
  printf '\033[32m✔ fe-gate-f25.test.sh PASS — mọi ca canary đúng kỳ vọng\033[0m\n'
  exit 0
else
  printf '\033[31m✘ fe-gate-f25.test.sh FAIL — %s ca sai kỳ vọng, xem chi tiết ở trên\033[0m\n' "$NFAIL"
  exit 1
fi
