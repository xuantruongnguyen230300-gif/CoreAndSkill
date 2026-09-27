#!/usr/bin/env bash
# scripts/tests/fe-gate-f27.test.sh — canary tự động cho luật F27 của scripts/fe-gate.sh.
#
# Dựng fixture TẠM (mktemp -d) đóng vai src/FE giả, KHÔNG đụng src/FE thật, rồi gọi THẲNG
# scripts/fe-gate.sh (không chép lại logic F27) qua biến môi trường FE để trỏ gate vào fixture.
# Đúng tinh thần T1 (docs/RULES.md §8) — kiểm chính detector đang chạy thật, không kiểm bản sao.
#
# Fixture tối giản KHÔNG làm mọi section khác xanh (F5 đỏ vì allowlist trỏ tới tệp không có trong
# fixture, F25 đỏ vì fixture không có màn danh sách nào), nên exit code toàn script không cô lập
# được F27 — ca được chấm theo riêng khối output của section F27, cùng cách fe-gate-f3.test.sh.
# "exit" trong bảng ca dưới đây là exit của RIÊNG section F27: 1 = khối có FAIL, 0 = khối có OK.
#
# Chạy từ gốc repo:
#     bash scripts/tests/fe-gate-f27.test.sh
#
# Thoát 0 = mọi ca đúng kỳ vọng · 1 = có ca sai kỳ vọng · 2 = không chạy được (thiếu script).

set -uo pipefail

T="$(cd "$(dirname "$0")" && pwd)"
REPO="$(cd "$T/../.." && pwd)"
GATE="$REPO/scripts/fe-gate.sh"

[ -f "$GATE" ] || { echo "ABORT — không thấy $GATE"; exit 2; }

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
  printf 'module.exports = { BUSINESS_MODULES: [] };\n' > "$dir/eslint.boundaries.cjs"
  printf '%s' "$dir"
}

# run_case <tên ca> <tạo file: yes/no> <nội dung shell.component.html> <exit code kỳ vọng>
run_case() {
  local name="$1" create="$2" html="$3" expect="$4"
  local dir; dir="$(make_fixture)"
  if [ "$create" = "yes" ]; then
    printf '%s' "$html" > "$dir/src/app/platform/shell/shell.component.html"
  fi

  local out actual
  out="$(FE="$dir" bash "$GATE" 2>&1)"
  rm -rf "$dir"
  actual="$(section_exit F27 "$out")"

  if [ "$actual" -eq "$expect" ]; then
    printf '  \033[32mOK\033[0m    %-70s exit=%s (kỳ vọng %s)\n' "$name" "$actual" "$expect"
  else
    printf '  \033[31mFAIL\033[0m  %-70s exit=%s (kỳ vọng %s)\n' "$name" "$actual" "$expect"
    printf '%s\n' "$out" | sed -n '/== F27/,/^$/p'
    NFAIL=$((NFAIL + 1))
  fi
}

printf '\n\033[1m== canary F27 — gọi thẳng scripts/fe-gate.sh qua fixture tạm ==\033[0m\n'

run_case "vi phạm thật: <nav class=sidebar>/<header class=topbar> dựng tay, không gọi app-sidebar/app-topbar -> FAIL" \
  yes \
  '<nav class="sidebar"><ul><li><a>x</a></li></ul></nav><header class="topbar" role="banner"><span>y</span></header><main><router-outlet></router-outlet></main>' \
  1

run_case "đã sửa đúng: chỉ còn <app-sidebar>/<app-topbar>, không còn markup tay -> PASS" \
  yes \
  '<app-sidebar [items]="menu"></app-sidebar><app-topbar [user]="nguoiDung"></app-topbar><main><router-outlet></router-outlet></main>' \
  0

run_case "nửa vá: đã thêm app-sidebar/app-topbar nhưng còn sót <nav class=sidebar> cũ -> FAIL" \
  yes \
  '<app-sidebar></app-sidebar><app-topbar></app-topbar><nav class="sidebar">rác còn sót</nav>' \
  1

run_case "thiếu app-topbar: có app-sidebar nhưng chưa gọi app-topbar -> FAIL" \
  yes \
  '<app-sidebar></app-sidebar><main><router-outlet></router-outlet></main>' \
  1

# Cố ý giữ nguyên <app-sidebar>/<app-topbar> hợp lệ trong ca này — khác ca "vi phạm thật" ở trên,
# ca này CHỈ được phép FAIL nhờ đúng nhánh regex thứ nhất bắt được <nav>/<header> dù attribute bị
# đảo thứ tự; nếu thiếu app-sidebar/app-topbar, ca sẽ FAIL bất kể regex thứ nhất có hoạt động hay
# không — tức không còn cách ly được cái nó tuyên bố kiểm (từng phát hiện thật lúc viết test này).
run_case "đảo thứ tự attribute: class không phải attribute đầu của <nav> vẫn phải bị bắt -> FAIL" \
  yes \
  '<app-sidebar></app-sidebar><app-topbar></app-topbar><nav [attr.aria-label]="x" class="sidebar"></nav>' \
  1

run_case "không báo sai: div tên gần giống 'sidebar' không phải thẻ nav/header vi phạm -> PASS" \
  yes \
  '<app-sidebar></app-sidebar><app-topbar></app-topbar><div class="sidebar-wrapper">không phải vi phạm</div>' \
  0

run_case "rỗng: shell.component.html trống -> FAIL (thiếu app-sidebar/app-topbar)" \
  yes \
  '' \
  1

run_case "thiếu file: shell.component.html không tồn tại -> FAIL" \
  no \
  '' \
  1

printf '\n'
if [ "$NFAIL" -eq 0 ]; then
  printf '\033[32m✔ fe-gate-f27.test.sh PASS — mọi ca canary đúng kỳ vọng\033[0m\n'
  exit 0
else
  printf '\033[31m✘ fe-gate-f27.test.sh FAIL — %s ca sai kỳ vọng, xem chi tiết ở trên\033[0m\n' "$NFAIL"
  exit 1
fi
