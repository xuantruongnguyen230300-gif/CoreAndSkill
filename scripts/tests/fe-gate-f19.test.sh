#!/usr/bin/env bash
# scripts/tests/fe-gate-f19.test.sh — canary tự động cho luật F19 của scripts/fe-gate.sh.
#
# Dựng fixture TẠM (mktemp -d) đóng vai src/FE giả, KHÔNG đụng src/FE thật, rồi gọi THẲNG
# scripts/fe-gate.sh qua biến môi trường FE. Fixture tối giản KHÔNG làm mọi section khác xanh,
# nên ca được chấm theo riêng khối output của section F19 — cùng cách fe-gate-f3.test.sh.
#
# Mỗi mẫu dò của F19 có ít nhất một ca đỏ riêng:
#   - mẫu (1) chuỗi mở đầu bằng `/api` ở BẤT KỲ đâu — kể cả gán vào trường `duongDan`;
#   - mẫu (2) lời gọi HTTP có đối số đầu mang tiền tố `api/` — so trên nội dung cả tệp, nên lời gọi
#     prettier bẻ sang dòng sau vẫn bị bắt. Ca `'api/…'` không có `/` đầu chỉ mẫu (2) bắt.
#
# Chạy từ gốc repo:
#     bash scripts/tests/fe-gate-f19.test.sh
#
# Kỳ vọng mỗi ca: 'do' (section F19 có FAIL) hoặc 'xanh' (section F19 có OK, không FAIL).
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
  mkdir -p "$DIR/src/app/core/http" "$DIR/src/app/platform/a/services"
  printf 'export const routes = [];\n' > "$DIR/src/app/app.routes.ts"
}

# tep <đường dẫn tương đối dưới src/> <nội dung>
tep() {
  mkdir -p "$(dirname "$DIR/src/$1")"
  printf '%s\n' "$2" > "$DIR/src/$1"
}

# check <tên ca> <kỳ vọng: do|xanh> — chạy gate trên fixture hiện tại rồi dọn fixture
check() {
  local name="$1" expect="$2" out sec actual
  out="$(FE="$DIR" bash "$GATE" 2>&1)"
  rm -rf "$DIR"
  sec="$(printf '%s\n' "$out" | awk '/== F19 /{f=1; print; next} /== F[0-9]/ || /fe-gate[.]sh (PASS|FAIL)/ {f=0} f')"
  if printf '%s\n' "$sec" | grep -q 'FAIL'; then
    actual=do
  elif printf '%s\n' "$sec" | grep -q 'OK'; then
    actual=xanh
  else
    actual=khong-thay-section
  fi
  if [ "$actual" = "$expect" ]; then
    printf '  \033[32mOK\033[0m    %-78s F19=%s (kỳ vọng %s)\n' "$name" "$actual" "$expect"
  else
    printf '  \033[31mFAIL\033[0m  %-78s F19=%s (kỳ vọng %s)\n' "$name" "$actual" "$expect"
    printf '%s\n' "$sec"
    NFAIL=$((NFAIL + 1))
  fi
}

SVC=app/platform/a/services/a.service.ts

printf '\n\033[1m== canary F19 — gọi thẳng scripts/fe-gate.sh qua fixture tạm ==\033[0m\n'

# --- mẫu (2): lời gọi HTTP, kể cả khi prettier bẻ dòng
make_fixture; tep "$SVC" "return this.http.get<X>('/api/v1/core/users');"
check "this.http.get<X>('/api/…') trên một dòng -> FAIL" do

make_fixture; tep "$SVC" "return this.http
      .get<ApiResult<PagedList<X>>>('/api/v1/core/users', { params })
      .pipe(map(unwrapData));"
check "prettier bẻ: this.http⏎ .get<…>('/api/…') -> FAIL" do

make_fixture; tep "$SVC" "return this.http.post<ApiResult<null>>(
      '/api/v1/core/users',
      payload,
    );"
check "prettier bẻ: .post<…>(⏎ '/api/…' -> FAIL" do

make_fixture; tep "$SVC" "return http.delete(\`/api/v1/core/users/\${id}\`);"
check "http.delete(\`/api/…\`) template literal, không có this. -> FAIL" do

make_fixture; tep "$SVC" "return this.http
      .put<ApiResult<null>>('api/v1/core/users', payload);"
check "đối số đầu 'api/…' không có / đầu (chỉ mẫu 2 bắt) -> FAIL" do

# --- mẫu (1): chuỗi /api ở mọi chỗ
make_fixture; tep "$SVC" "private readonly duongDan = '/api/v1/core/users';"
check "gán '/api/…' vào trường duongDan -> FAIL" do

make_fixture; tep "$SVC" "private readonly duongDan = \"/api/v1/core/users\";"
check "gán \"/api/…\" (nháy kép) -> FAIL" do

make_fixture; tep "app/core/http/goc.ts" "export const GOC = '/api';"
check "chuỗi đúng bằng '/api' -> FAIL" do

# --- không phải vi phạm
make_fixture; tep "$SVC" "private readonly duongDan = '/core/users';
return this.http
      .get<ApiResult<X>>(this.duongDan, { params });"
check "đường dẫn ngắn '/core/…' -> PASS" xanh

make_fixture; tep "$SVC" "/** Request của \`POST /api/v1/core/auth/login\` — contracts/auth.md §3. */
export interface X {}"
check "chú thích nhắc /api/ nhưng không mở chuỗi bằng /api -> PASS" xanh

make_fixture; tep "environments/environment.ts" "export const environment = { apiBaseUrl: 'https://localhost:7100/api/v1' };"
check "base URL tuyệt đối ở environments/ -> PASS" xanh

make_fixture; tep "$SVC" "export const x = 1;"; tep "app/platform/a/services/a.service.spec.ts" "httpMock.expectOne('/api/v1/core/users');"
check "*.spec.ts nhắc '/api/' không bị tính -> PASS" xanh

make_fixture; find "$DIR/src" -name '*.ts' -delete
check "không còn tệp *.ts nào để quét -> FAIL (không xanh rỗng)" do

# --- ca trên code THẬT: mọi tệp gọi HTTP của src/FE, giữ nguyên cách prettier đã bẻ dòng
REAL_APP="$REPO/src/FE/src/app"
REAL_HTTP=$(grep -rlE "\.(get|post|put|patch|delete)<" "$REAL_APP" --include='*.ts' | grep -v '\.spec\.ts$')
if [ -z "$REAL_HTTP" ]; then
  printf '  \033[31mFAIL\033[0m  không tìm thấy tệp gọi HTTP thật dưới %s để làm canary\n' "$REAL_APP"
  NFAIL=$((NFAIL + 1))
else
  copy_real() {
    local f
    while IFS= read -r f; do
      mkdir -p "$(dirname "$DIR/src/app/${f#"$REAL_APP"/}")"
      cp "$f" "$DIR/src/app/${f#"$REAL_APP"/}"
    done <<< "$REAL_HTTP"
  }
  make_fixture; copy_real
  check "tệp gọi HTTP thật, nguyên vẹn -> PASS" xanh

  # Đổi mọi đường dẫn '/core/…' thành 'api/v1/core/…' — không có '/' đầu nên mẫu (1) KHÔNG thấy;
  # chỉ mẫu (2) trên lời gọi đã bị prettier bẻ dòng mới bắt được.
  make_fixture; copy_real
  find "$DIR/src" -name '*.ts' -exec sed -i "s#'/core/#'api/v1/core/#g" {} +
  if grep -rq "'api/v1/core/" "$DIR/src"; then
    check "tệp HTTP thật, đường dẫn đổi sang 'api/v1/core/…' -> FAIL" do
  else
    rm -rf "$DIR"
    printf '  \033[31mFAIL\033[0m  không có chuỗi %s nào trong tệp thật để đổi — canary không cắm được\n' "'/core/"
    NFAIL=$((NFAIL + 1))
  fi
fi

printf '\n'
if [ "$NFAIL" -eq 0 ]; then
  printf '\033[32m✔ fe-gate-f19.test.sh PASS — mọi ca canary đúng kỳ vọng\033[0m\n'
  exit 0
else
  printf '\033[31m✘ fe-gate-f19.test.sh FAIL — %s ca sai kỳ vọng, xem chi tiết ở trên\033[0m\n' "$NFAIL"
  exit 1
fi
