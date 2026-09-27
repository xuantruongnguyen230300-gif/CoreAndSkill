#!/usr/bin/env bash
# scripts/tests/fe-lint-f35.test.sh — canary cho luật F35 (và F24 đi cùng khối) của `ng lint`:
# zone `sharedLayerZones` / `platformLayerZones` trong src/FE/eslint.config.js
# (docs/quy-uoc/fe-architecture.md §4.7; lý do ở wiki-core/fe/ly-do/fe-architecture.md §4.7).
#
# Cổng lint không có section riêng để chấm như fe-gate.sh, nên canary gọi THẲNG ESLint của src/FE
# với ĐÚNG eslint.config.js đang chạy thật — không chép lại zone nào (luật T1, docs/RULES.md §8).
#
# 🛑 CÁCH CANARY NÀY TỪNG CHẾT IM LẶNG — đọc trước khi sửa
#
# `import/no-restricted-paths` chỉ chạy khi resolver phân giải được import, và việc đó đòi HAI thứ
# cùng có thật trên đĩa:
#   (1) THƯ MỤC của tên tệp `--stdin-filename` — resolver lấy nó làm basedir;
#   (2) TỆP ĐÍCH mà import trỏ tới.
# Thiếu một trong hai thì rule **bỏ qua không nói gì** và ESLint thoát 0. Với ca kỳ vọng ĐỎ, cái đó
# hiện ra thành FAIL. Với ca kỳ vọng XANH thì **không ai thấy**: ca vẫn PASS, vì xanh-do-được-phép và
# xanh-do-không-phân-giải-được trông y hệt nhau. Đúng khuôn docs/audit/2026-08-23-cong-khong-ton-tai.md.
#
# Chuyện đã xảy ra thật: [ADR-0078](../../docs/adr/0078-cot-nen-giu-quyen-quyet-thu-muc-button-va-input-doi-sang-shared-components.md)
# dời `shared/ui/button/` sang `shared/components/button/`. Bản trước của tệp này gõ tay
# `src/app/shared/ui/button/canary.ts` ở hai ca và `'../../ui/button/button.component'` ở một ca:
# hai ca đỏ thành xanh (thấy được), ca xanh thứ ba thành xanh-rỗng (không thấy được).
#
# BA LỚP chống, theo thứ tự mạnh dần:
#   L1 — DẪN XUẤT mỏ neo từ cây thật lúc chạy, không gõ tay tên thư mục nào. Dời một component
#        không làm canary mù: nó tự lấy component khác đang có.
#   L2 — KHẲNG ĐỊNH mỏ neo có thật trước khi chạy, và tự tính đường dẫn import bằng `realpath`.
#        Thiếu thì ABORT (thoát 2), không chạy tiếp. Đếm sai số `../` cũng là một cách phân giải hỏng
#        im lặng, nên đường dẫn tương đối không còn được gõ tay nữa.
#   L3 — ĐỐI CHỨNG PHÂN GIẢI cho ca kỳ vọng XANH: cùng ĐÚNG tệp đích đó, nhập từ một nơi mà luật
#        CẤM (`core/` — luật F1), phải ĐỎ. Đối chứng đỏ ⇒ đích phân giải được ⇒ ca xanh kia xanh vì
#        chiều được phép, không vì resolver chết. L1 và L2 chống được lần hỏng đã biết; chỉ L3 chống
#        được LỚP hỏng, vì nó không dựa vào việc ai đó nhớ kiểm đường dẫn.
#
# Hai cách cắm vi phạm, vì resolver chỉ phân giải import tới tệp CÓ THẬT:
#   - chiều shared/ → platform/ và F24 shared/ui/ → shared/components/: `eslint --stdin` với
#     `--stdin-filename` đặt đoạn mã vào một đường dẫn trong cây thật; import trỏ tới tệp thật của
#     platform/ hoặc shared/components/. Không để lại tệp nào phải hoàn nguyên.
#   - chiều platform/ → modules/ và shared/ → modules/: repo Core KHÔNG có modules/ (ADR-0032), nên
#     dựng fixture TẠM (mktemp -d) chứa cả tệp vi phạm lẫn tệp modules/ bị import, rồi chạy ESLint
#     từ thư mục đó với `--config` trỏ về eslint.config.js thật. Với `--config` tường minh, ESLint 9
#     lấy cwd làm gốc cho `files` và cho `target`/`from` của zone, nên fixture giả được cây src/app.
#     Nhánh fixture KHÔNG dính lớp hỏng trên: nó tự dựng cả hai đầu của phép import.
#
# Mỗi ca đỏ phải đỏ ĐÚNG rule `import/no-restricted-paths` kèm ĐÚNG thông điệp của zone — một lỗi
# khác (config hỏng, parser gãy) làm exit ≠ 0 nhưng KHÔNG được tính là canary đạt. Ca xanh đối chứng
# chứng minh config nạp được trong fixture: đỏ ở ca kia không phải vì ESLint chết.
#
# Chạy từ gốc repo:
#     bash scripts/tests/fe-lint-f35.test.sh
#
# Thoát 0 = mọi ca đúng kỳ vọng · 1 = có ca sai kỳ vọng · 2 = không chạy được (thiếu node_modules,
# hoặc mỏ neo không còn thật — xem L2).

set -uo pipefail

T="$(cd "$(dirname "$0")" && pwd)"
REPO="$(cd "$T/../.." && pwd)"
FE="$REPO/src/FE"
ESLINT="$FE/node_modules/eslint/bin/eslint.js"
CONFIG="$FE/eslint.config.js"

[ -f "$ESLINT" ] || { echo "ABORT — không thấy $ESLINT (chạy npm ci trong src/FE)"; exit 2; }
[ -f "$CONFIG" ] || { echo "ABORT — không thấy $CONFIG"; exit 2; }
command -v node >/dev/null 2>&1 || { echo "ABORT — thiếu node trên PATH"; exit 2; }
command -v realpath >/dev/null 2>&1 || { echo "ABORT — thiếu realpath; L2 không tính được đường dẫn import"; exit 2; }

NFAIL=0
RULE='import/no-restricted-paths'

# ---------------------------------------------------------------- L1: mỏ neo dẫn xuất
# neo_component <thư mục dưới src/app> — tệp *.component.ts đầu tiên (thứ tự byte) dưới thư mục đó.
# Lấy "tệp đầu tiên" chứ không gọi tên một component cụ thể: mỗi component đều có thể dời đi, còn
# việc tầng đó có ÍT NHẤT MỘT component thì chính kiến trúc bảo đảm.
neo_component() {
  find "$FE/src/app/$1" -name '*.component.ts' -not -name '*.spec.ts' 2>/dev/null | LC_ALL=C sort | head -1
}

UI_TEP="$(neo_component shared/ui)"
CPN_TEP="$(neo_component shared/components)"
PLAT_TEP="$(find "$FE/src/app/platform" -name '*.ts' -not -name '*.spec.ts' 2>/dev/null | LC_ALL=C sort | head -1)"
CORE_DIR="$(find "$FE/src/app/core" -mindepth 1 -maxdepth 1 -type d 2>/dev/null | LC_ALL=C sort | head -1)"

[ -n "$UI_TEP" ]   || { echo "ABORT — không thấy *.component.ts nào dưới src/app/shared/ui/; canary F24 mất mỏ neo"; exit 2; }
[ -n "$CPN_TEP" ]  || { echo "ABORT — không thấy *.component.ts nào dưới src/app/shared/components/; canary F24 mất mỏ neo"; exit 2; }
[ -n "$PLAT_TEP" ] || { echo "ABORT — không thấy *.ts nào dưới src/app/platform/; canary F35 mất đích import"; exit 2; }
[ -n "$CORE_DIR" ] || { echo "ABORT — không thấy thư mục con nào dưới src/app/core/; L3 mất chỗ đứng đối chứng"; exit 2; }

UI_DIR="$(dirname "$UI_TEP")"
CPN_DIR="$(dirname "$CPN_TEP")"

# ---------------------------------------------------------------- chấm
# cham <tên ca> <kỳ vọng: do|xanh> <chuỗi thông điệp phải có khi đỏ> <output> <exit>
cham() {
  local name="$1" expect="$2" msg="$3" out="$4" code="$5" actual
  if [ "$code" -eq 0 ]; then
    actual=xanh
  elif printf '%s\n' "$out" | grep -qF "$RULE" && printf '%s\n' "$out" | grep -qF "$msg"; then
    actual=do
  else
    actual="loi-khac"
  fi
  if [ "$actual" = "$expect" ]; then
    printf '  \033[32mOK\033[0m    %-78s %s (kỳ vọng %s)\n' "$name" "$actual" "$expect"
  else
    printf '  \033[31mFAIL\033[0m  %-78s %s (kỳ vọng %s)\n' "$name" "$actual" "$expect"
    printf '%s\n' "$out" | sed 's/^/        /'
    NFAIL=$((NFAIL + 1))
  fi
}

# ---------------------------------------------------------------- L2: dựng ca từ hai đường dẫn THẬT
# stdin_case <tên ca> <thư mục nguồn tuyệt đối> <tệp đích tuyệt đối> <kỳ vọng> <thông điệp>
# Đặt một `canary.ts` ảo vào <thư mục nguồn> và nhập <tệp đích>. Đường dẫn tương đối do realpath
# tính, không gõ tay. Thiếu một trong hai đầu ⇒ ABORT, vì khi đó rule bỏ qua im lặng và mọi kết
# quả của ca đều vô nghĩa — kể cả kết quả "xanh".
stdin_case() {
  local name="$1" srcdir="$2" target="$3" expect="$4" msg="$5" rel out code file
  [ -d "$srcdir" ] || { echo "ABORT — thư mục nguồn không tồn tại: ${srcdir#$FE/} (rule sẽ bỏ qua im lặng)"; exit 2; }
  [ -f "$target" ] || { echo "ABORT — tệp đích không tồn tại: ${target#$FE/} (rule sẽ bỏ qua im lặng)"; exit 2; }
  rel="$(realpath --relative-to="$srcdir" "$target")" || { echo "ABORT — realpath không tính được đường dẫn tương đối"; exit 2; }
  rel="${rel%.ts}"
  case "$rel" in ../*|./*) ;; *) rel="./$rel" ;; esac
  file="${srcdir#$FE/}/canary.ts"
  out="$(cd "$FE" && printf "import '%s';\n" "$rel" | node "$ESLINT" --stdin --stdin-filename "$file" 2>&1)"
  code=$?
  cham "$name" "$expect" "$msg" "$out" "$code"
}

# fixture_case <tên ca> <đường dẫn tệp vi phạm dưới src/app> <nội dung> <kỳ vọng> <thông điệp>
# Fixture luôn có src/app/modules/kho/kho.service.ts để import tới nó phân giải được.
fixture_case() {
  local name="$1" file="$2" body="$3" expect="$4" msg="$5" dir out code
  dir="$(mktemp -d)" || { echo "ABORT — mktemp -d thất bại"; exit 2; }
  mkdir -p "$dir/src/app/modules/kho" "$dir/$(dirname "src/app/$file")"
  printf 'export const KHO = 1;\n' > "$dir/src/app/modules/kho/kho.service.ts"
  printf '%s\n' "$body" > "$dir/src/app/$file"
  out="$(cd "$dir" && node "$ESLINT" --no-config-lookup --config "$CONFIG" "src/app/$file" 2>&1)"
  code=$?
  rm -rf "$dir"
  cham "$name" "$expect" "$msg" "$out" "$code"
}

printf '\n\033[1m== canary F35/F24 — gọi thẳng ESLint với eslint.config.js thật ==\033[0m\n'
printf '  mỏ neo dẫn xuất lúc chạy (L1): ui=%s · components=%s · platform=%s · core=%s\n' \
  "${UI_DIR#$FE/src/app/}" "${CPN_DIR#$FE/src/app/}" "${PLAT_TEP#$FE/src/app/}" "${CORE_DIR#$FE/src/app/}"

# --- chiều shared/ → platform/ (F35), trên cây thật
stdin_case "shared/components import platform/ -> ĐỎ" \
  "$CPN_DIR" "$PLAT_TEP" do "shared/ không được import platform/"

stdin_case "shared/ui import platform/ -> ĐỎ (zone F35 áp cả cây shared/)" \
  "$UI_DIR" "$PLAT_TEP" do "shared/ không được import platform/"

# --- F24 vẫn phải đỏ sau khi gộp vào khối shared/** (bẫy "khối sau thay khối trước", ly-do §4.6)
stdin_case "shared/ui import shared/components -> ĐỎ (F24 còn nguyên sau khi gộp khối)" \
  "$UI_DIR" "$CPN_TEP" do "shared/ui/ không được import shared/components/"

# --- L3: đối chứng phân giải cho ca xanh ngay dưới.
# ĐÚNG tệp đích của ca xanh, nhập từ core/ — F1 cấm chiều này, nên ca này PHẢI đỏ. Nó đỏ thì đích
# phân giải được, và chỉ khi đó cái xanh của ca sau mới có nghĩa. Ca này xanh = resolver đã chết và
# ca sau đang xanh rỗng.
stdin_case "ĐỐI CHỨNG L3: core/ import chính tệp shared/ui đó -> ĐỎ (F1) ⇒ đích phân giải được" \
  "$CORE_DIR" "$UI_TEP" do "core/ không được import shared/"

# --- đối chứng trên cây thật: shared/components import shared/ui là chiều ĐÚNG
stdin_case "shared/components import shared/ui -> XANH (chiều cho phép, đã có L3 bảo chứng)" \
  "$CPN_DIR" "$UI_TEP" xanh ""

# --- chiều platform/ → modules/ (F35), fixture tạm
fixture_case "platform/ import modules/ -> ĐỎ (fixture có modules/kho thật)" \
  platform/x/x.page.ts \
  "import { KHO } from '../../modules/kho/kho.service';
export const x = KHO;" \
  do "platform/ không được import modules/"

fixture_case "shared/ import modules/ -> ĐỎ (fixture có modules/kho thật)" \
  shared/services/canary.ts \
  "import { KHO } from '../../modules/kho/kho.service';
export const x = KHO;" \
  do "shared/ không được import modules/"

# --- đối chứng trong fixture: config nạp được, tệp sạch xanh
fixture_case "platform/ không import modules/ -> XANH (đối chứng: config nạp được trong fixture)" \
  platform/x/x.page.ts \
  "export const x = 1;" \
  xanh ""

printf '\n'
if [ "$NFAIL" -eq 0 ]; then
  printf '\033[32m✔ fe-lint-f35.test.sh PASS — mọi ca canary đúng kỳ vọng\033[0m\n'
  exit 0
fi
printf '\033[31m✘ fe-lint-f35.test.sh FAIL — %s ca sai kỳ vọng, xem chi tiết ở trên\033[0m\n' "$NFAIL"
exit 1
