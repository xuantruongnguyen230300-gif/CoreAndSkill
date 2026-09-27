#!/usr/bin/env bash
# scripts/tests/fe-gate-f4.test.sh — canary tự động cho luật F4 của scripts/fe-gate.sh.
#
# Dựng fixture TẠM (mktemp -d) đóng vai src/FE giả, KHÔNG đụng src/FE thật, rồi gọi THẲNG
# scripts/fe-gate.sh (không chép lại logic F4) qua biến môi trường FE để trỏ gate vào fixture.
# Đúng tinh thần T1 (docs/RULES.md §8) — kiểm chính detector đang chạy thật, không kiểm bản sao.
#
# Fixture tối giản KHÔNG làm mọi section khác xanh (F5 đỏ vì allowlist trỏ tới tệp không có trong
# fixture, F25 đỏ vì fixture không có màn danh sách nào), nên exit code toàn script không cô lập
# được F4 — ca được chấm theo riêng khối output của section F4, cùng cách fe-gate-f3.test.sh.
# "exit" trong bảng ca dưới đây là exit của RIÊNG section F4: 1 = khối có FAIL, 0 = khối có OK.
#
# Chạy từ gốc repo:
#     bash scripts/tests/fe-gate-f4.test.sh
#
# Thoát 0 = mọi ca đúng kỳ vọng · 1 = có ca sai kỳ vọng · 2 = không chạy được (thiếu node/script).

set -uo pipefail

T="$(cd "$(dirname "$0")" && pwd)"
REPO="$(cd "$T/../.." && pwd)"
GATE="$REPO/scripts/fe-gate.sh"

[ -f "$GATE" ] || { echo "ABORT — không thấy $GATE"; exit 2; }
command -v node >/dev/null 2>&1 || { echo "ABORT — thiếu node trên PATH, cần cho F4"; exit 2; }

NFAIL=0

# section_exit <mã section> <output của gate> — "exit code" của RIÊNG một section: 1 khi khối
# output của section có dòng FAIL, 0 khi có OK mà không FAIL, 99 khi không thấy section.
section_exit() {
  local sec
  sec="$(printf '%s
' "$2" | awk -v m="== $1 " 'index($0, m) {f=1; print; next} /== F[0-9]/ || /fe-gate[.]sh (PASS|FAIL)/ {f=0} f')"
  if printf '%s
' "$sec" | grep -q 'FAIL'; then echo 1
  elif printf '%s
' "$sec" | grep -q 'OK'; then echo 0
  else echo 99
  fi
}


make_fixture() {
  local dir
  dir="$(mktemp -d)" || { echo "ABORT — mktemp -d thất bại"; exit 2; }
  mkdir -p "$dir/src/app/core"
  mkdir -p "$dir/src/app/platform/shell"
  printf 'export const routes = [];\n' > "$dir/src/app/app.routes.ts"
  printf '<app-sidebar></app-sidebar><app-topbar></app-topbar>' > "$dir/src/app/platform/shell/shell.component.html"
  printf '%s' "$dir"
}

# run_case <tên ca> <nội dung eslint.boundaries.cjs> <danh sách thư mục con của modules/, cách nhau bởi dấu cách; rỗng = không tạo modules/> <exit code kỳ vọng>
run_case() {
  local name="$1" boundaries="$2" mod_dirs="$3" expect="$4"
  local dir; dir="$(make_fixture)"
  printf '%s' "$boundaries" > "$dir/eslint.boundaries.cjs"
  if [ -n "$mod_dirs" ]; then
    local m
    for m in $mod_dirs; do mkdir -p "$dir/src/app/modules/$m"; done
  fi

  local out actual
  out="$(FE="$dir" bash "$GATE" 2>&1)"
  rm -rf "$dir"
  actual="$(section_exit F4 "$out")"

  if [ "$actual" -eq "$expect" ]; then
    printf '  \033[32mOK\033[0m    %-55s exit=%s (kỳ vọng %s)\n' "$name" "$actual" "$expect"
  else
    printf '  \033[31mFAIL\033[0m  %-55s exit=%s (kỳ vọng %s)\n' "$name" "$actual" "$expect"
    printf '%s\n' "$out" | sed -n '/== F4/,/^$/p'
    NFAIL=$((NFAIL + 1))
  fi
}

printf '\n\033[1m== canary F4 — gọi thẳng scripts/fe-gate.sh qua fixture tạm ==\033[0m\n'

run_case "rỗng khớp rỗng -> PASS (ADR-0036)" \
  "module.exports = { BUSINESS_MODULES: [] };" \
  "" 0

run_case "khớp thật, khác rỗng -> PASS" \
  "module.exports = { BUSINESS_MODULES: ['ke-toan'] };" \
  "ke-toan" 0

run_case "lệch thật: khai module nhưng không có thư mục -> FAIL" \
  "module.exports = { BUSINESS_MODULES: ['ke-toan'] };" \
  "" 1

run_case "lệch thật: có thư mục nhưng không khai -> FAIL" \
  "module.exports = { BUSINESS_MODULES: [] };" \
  "ke-toan" 1

run_case "cấu hình hỏng: sai tên field -> FAIL (không phải PASS-vì-rỗng)" \
  "module.exports = { BUSSINESS_MODULES_TYPO: ['ke-toan'] };" \
  "" 1

run_case "cấu hình hỏng: cú pháp JS sai -> FAIL" \
  "module.exports = { BUSINESS_MODULES: [ ;" \
  "" 1

printf '\n'
if [ "$NFAIL" -eq 0 ]; then
  printf '\033[32m✔ fe-gate-f4.test.sh PASS — mọi ca canary đúng kỳ vọng\033[0m\n'
  exit 0
else
  printf '\033[31m✘ fe-gate-f4.test.sh FAIL — %s ca sai kỳ vọng, xem chi tiết ở trên\033[0m\n' "$NFAIL"
  exit 1
fi
