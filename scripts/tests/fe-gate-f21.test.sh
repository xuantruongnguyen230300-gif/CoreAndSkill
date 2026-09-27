#!/usr/bin/env bash
# scripts/tests/fe-gate-f21.test.sh — canary tự động cho luật F21 của scripts/fe-gate.sh.
#
# Dựng fixture TẠM (mktemp -d) đóng vai src/FE giả, KHÔNG đụng src/FE thật, rồi gọi THẲNG
# scripts/fe-gate.sh qua biến môi trường FE. Ca được chấm theo riêng khối output của section F21 —
# cùng cách fe-gate-f3.test.sh.
#
# Mỗi mẫu dò có ít nhất một ca đỏ riêng: `navigate([...])`, `createUrlTree([...])`,
# `navigateByUrl(...)`, `parseUrl(...)` với đường dẫn viết cứng mở đầu bằng `/`, và bản prettier bẻ
# dòng của lời gọi — F21 so trên nội dung cả tệp.
#
# Chạy từ gốc repo:
#     bash scripts/tests/fe-gate-f21.test.sh
#
# Thoát 0 = mọi ca đúng kỳ vọng · 1 = có ca sai kỳ vọng · 2 = không chạy được (thiếu script).

set -uo pipefail

T="$(cd "$(dirname "$0")" && pwd)"
REPO="$(cd "$T/../.." && pwd)"
GATE="$REPO/scripts/fe-gate.sh"

[ -f "$GATE" ] || { echo "ABORT — không thấy $GATE"; exit 2; }
cd "$REPO" || exit 2

NFAIL=0
DIR=""

make_fixture() {
  DIR="$(mktemp -d)" || { echo "ABORT — mktemp -d thất bại"; exit 2; }
  mkdir -p "$DIR/src/app/core/guards" "$DIR/src/app/platform/a"
  printf 'export const routes = [];\n' > "$DIR/src/app/app.routes.ts"
}

# tep <đường dẫn tương đối dưới src/app/> <nội dung>
tep() {
  mkdir -p "$(dirname "$DIR/src/app/$1")"
  printf '%s\n' "$2" > "$DIR/src/app/$1"
}

check() {
  local name="$1" expect="$2" out sec actual
  out="$(FE="$DIR" bash "$GATE" 2>&1)"
  rm -rf "$DIR"
  sec="$(printf '%s\n' "$out" | awk '/== F21 /{f=1; print; next} /== F[0-9]/ || /fe-gate[.]sh (PASS|FAIL)/ {f=0} f')"
  if printf '%s\n' "$sec" | grep -q 'FAIL'; then
    actual=do
  elif printf '%s\n' "$sec" | grep -q 'OK'; then
    actual=xanh
  else
    actual=khong-thay-section
  fi
  if [ "$actual" = "$expect" ]; then
    printf '  \033[32mOK\033[0m    %-78s F21=%s (kỳ vọng %s)\n' "$name" "$actual" "$expect"
  else
    printf '  \033[31mFAIL\033[0m  %-78s F21=%s (kỳ vọng %s)\n' "$name" "$actual" "$expect"
    printf '%s\n' "$sec"
    NFAIL=$((NFAIL + 1))
  fi
}

G=core/guards/x.guard.ts

printf '\n\033[1m== canary F21 — gọi thẳng scripts/fe-gate.sh qua fixture tạm ==\033[0m\n'

make_fixture; tep "$G" "void this.router.navigate(['/dang-nhap']);"
check "navigate(['/…']) -> FAIL" do

make_fixture; tep "$G" "void this.router.navigate(
      ['/dang-nhap'],
      { queryParams: { returnUrl } },
    );"
check "prettier bẻ: navigate(⏎ ['/…'] -> FAIL" do

make_fixture; tep "$G" "return router.createUrlTree(['/khong-co-quyen']);"
check "createUrlTree(['/…']) -> FAIL" do

make_fixture; tep "$G" "return router.createUrlTree(
      [\"/dang-nhap\"],
      { queryParams: { returnUrl: state.url } },
    );"
check "prettier bẻ: createUrlTree(⏎ [\"/…\"] nháy kép -> FAIL" do

make_fixture; tep "$G" "void this.router.navigateByUrl('/trang-chu');"
check "navigateByUrl('/…') -> FAIL" do

make_fixture; tep "$G" "void this.router.navigateByUrl(\`/trang-chu/\${id}\`);"
check "navigateByUrl(\`/…\`) template literal -> FAIL" do

make_fixture; tep "$G" "return new RedirectCommand(router.parseUrl('/dang-nhap'));"
check "parseUrl('/…') -> FAIL" do

# --- không phải vi phạm
make_fixture; tep "$G" "return router.createUrlTree([routes.dangNhap], { queryParams: { returnUrl: state.url } });
void this.router.navigate([this.routes.dangNhap]);
void this.router.navigateByUrl(this.router.url, { onSameUrlNavigation: 'reload' });
void this.router.navigate([], { queryParams: q });"
check "đường dẫn đến từ seam / URL hiện tại / mảng rỗng -> PASS" xanh

make_fixture; tep "$G" "export const x = 1;"; tep "platform/a/a.page.ts" "void this.router.navigate(['/nguoi-dung']);"
check "platform/ điều hướng cứng không thuộc phạm vi F21 -> PASS" xanh

make_fixture; tep "$G" "export const x = 1;"; tep "core/guards/x.guard.spec.ts" "expect(router.navigateByUrl('/dang-nhap'));"
check "*.spec.ts trong core/ không bị tính -> PASS" xanh

make_fixture; find "$DIR/src/app/core" -name '*.ts' -delete
check "core/ không còn tệp *.ts nào -> FAIL (không xanh rỗng)" do

# --- ca trên code THẬT: mọi tệp trong core/ gọi Router bằng đường dẫn từ seam CORE_ROUTES
REAL_CORE="$REPO/src/FE/src/app/core"
REAL_DH=$(grep -rlE "(navigate|createUrlTree)\(\[(this\.)?routes\." "$REAL_CORE" --include='*.ts' | grep -v '\.spec\.ts$')
if [ -z "$REAL_DH" ]; then
  printf '  \033[31mFAIL\033[0m  không tìm thấy lời gọi Router đọc CORE_ROUTES dưới %s để làm canary\n' "$REAL_CORE"
  NFAIL=$((NFAIL + 1))
else
  copy_real() {
    local f
    while IFS= read -r f; do
      mkdir -p "$(dirname "$DIR/src/app/core/${f#"$REAL_CORE"/}")"
      cp "$f" "$DIR/src/app/core/${f#"$REAL_CORE"/}"
    done <<< "$REAL_DH"
  }
  make_fixture; copy_real
  check "lời gọi Router thật trong core/, nguyên vẹn -> PASS" xanh

  while IFS= read -r f; do
    rel="${f#"$REAL_CORE"/}"
    make_fixture; copy_real
    # Thay đường dẫn đọc từ seam bằng đường dẫn viết cứng — đúng thứ F21 cấm.
    sed -i -E "s#\[(this\.)?routes\.[A-Za-z]+\]#['/viet-cung']#g" "$DIR/src/app/core/$rel"
    if grep -q "'/viet-cung'" "$DIR/src/app/core/$rel"; then
      check "core/$rel: seam thay bằng '/viet-cung' -> FAIL" do
    else
      rm -rf "$DIR"
      printf '  \033[31mFAIL\033[0m  core/%s không có chỗ đọc routes.* để thay — canary không cắm được\n' "$rel"
      NFAIL=$((NFAIL + 1))
    fi
  done <<< "$REAL_DH"
fi

printf '\n'
if [ "$NFAIL" -eq 0 ]; then
  printf '\033[32m✔ fe-gate-f21.test.sh PASS — mọi ca canary đúng kỳ vọng\033[0m\n'
  exit 0
else
  printf '\033[31m✘ fe-gate-f21.test.sh FAIL — %s ca sai kỳ vọng, xem chi tiết ở trên\033[0m\n' "$NFAIL"
  exit 1
fi
