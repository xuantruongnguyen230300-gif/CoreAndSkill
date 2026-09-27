#!/usr/bin/env bash
# scripts/tests/fe-gate-f29.test.sh — canary tự động cho luật F29 của scripts/fe-gate.sh (ADR-0056).
#
# Hai nhóm ca, cùng gọi THẲNG scripts/fe-gate.sh qua biến FE và F29_NODE_MODULES, chấm theo riêng
# khối output của section F29:
#   - gói GIẢ: dựng node_modules/primeng/fesm2022/primeng-<x>.mjs và
#     node_modules/@primeuix/themes/dist/aura/<y>/ tối giản để kiểm từng nhánh của detector —
#     thiếu sàn, vượt trần, preset gộp, đổi tên theo bảng, bao đóng hai tầng, tập rỗng, gói vắng;
#   - code THẬT: chép src/FE/src, trỏ F29_NODE_MODULES vào src/FE/node_modules đã cài, rồi gỡ một
#     sub-preset bắt buộc hoặc thêm một sub-preset ngoài trần vào prime-preset.ts của bản sao.
# Hai bảng tên đọc từ docs thật (05-gate.md §8.12) — ca gói giả dùng tên module có thật trong bảng.
#
# Chạy từ gốc repo:
#     bash scripts/tests/fe-gate-f29.test.sh
#
# Thoát 0 = mọi ca đúng kỳ vọng · 1 = có ca sai kỳ vọng · 2 = không chạy được.

set -uo pipefail

T="$(cd "$(dirname "$0")" && pwd)"
REPO="$(cd "$T/../.." && pwd)"
GATE="$REPO/scripts/fe-gate.sh"
[ -f "$GATE" ] || { echo "ABORT — không thấy $GATE"; exit 2; }
cd "$REPO" || exit 2
# shellcheck source=_fixture-that.sh
. "$T/_fixture-that.sh"

NFAIL=0
NM_THAT="$REPO/src/FE/node_modules"

# --- gói giả -------------------------------------------------------------------------------------
# gia <thư mục> — DIR = fixture src/ tối giản; NM = gói giả rỗng trong đó
gia() {
  DIR="$(mktemp -d)" || { echo "ABORT — mktemp -d thất bại"; exit 2; }
  NM="$DIR/nm"
  mkdir -p "$DIR/src/app/core/theme" "$DIR/src/app/shared/ui" "$NM/primeng/fesm2022" "$NM/@primeuix/themes/dist/aura/base"
}
# mjs <module> <module phụ thuộc…> — một tệp primeng-<module>.mjs khai import các module phụ thuộc
mjs() {
  local m="$1" d; shift
  : > "$NM/primeng/fesm2022/primeng-$m.mjs"
  for d in "$@"; do printf "import { X } from 'primeng/%s';\n" "$d" >> "$NM/primeng/fesm2022/primeng-$m.mjs"; done
}
# preset_co <tên…> — các thư mục sub-preset có trong gói giả
preset_co() { local p; for p in "$@"; do mkdir -p "$NM/@primeuix/themes/dist/aura/$p"; done; }
# dung_truc_tiep <module…> — shared/ui/x.ts import trực tiếp các module
dung_truc_tiep() { local m; for m in "$@"; do printf "import { X } from 'primeng/%s';\n" "$m"; done > "$DIR/src/app/shared/ui/x.ts"; }
# preset_ts <sub-preset…> — prime-preset.ts import các sub-preset
preset_ts() { local p; for p in "$@"; do printf "import A from '@primeuix/themes/aura/%s';\n" "$p"; done > "$DIR/src/app/core/theme/prime-preset.ts"; }

cham_gia() {
  local name="$1" expect="$2" out sec actual
  out="$(FE="$DIR" F29_NODE_MODULES="$NM" bash "$GATE" 2>&1)"
  rm -rf "$DIR"
  sec="$(khoi_section F29 "$out")"
  if printf '%s\n' "$sec" | grep -q 'FAIL'; then actual=do
  elif printf '%s\n' "$sec" | grep -q 'OK'; then actual=xanh
  else actual=khong-thay-section; fi
  if [ "$actual" = "$expect" ]; then
    printf '  \033[32mOK\033[0m    %-78s F29=%s (kỳ vọng %s)\n' "$name" "$actual" "$expect"
  else
    printf '  \033[31mFAIL\033[0m  %-78s F29=%s (kỳ vọng %s)\n' "$name" "$actual" "$expect"
    printf '%s\n' "$sec"
    NFAIL=$((NFAIL + 1))
  fi
}

# Gói giả dùng chung: dialog → button; table → paginator → select (hai tầng); datepicker ngoài mọi bao đóng
goi_chung() {
  preset_co dialog button datatable paginator select datepicker
  mjs dialog api button
  mjs button api
  mjs table api paginator
  mjs paginator api select
  mjs select api
  mjs datepicker api button
}

printf '\n\033[1m== canary F29 — gói giả ==\033[0m\n'

gia; goi_chung; dung_truc_tiep dialog api; preset_ts base dialog
cham_gia "dialog trực tiếp, preset base + dialog -> PASS" xanh

gia; goi_chung; dung_truc_tiep dialog; preset_ts base dialog button
cham_gia "thêm button — component con nằm trong bao đóng của dialog -> PASS" xanh

gia; goi_chung; dung_truc_tiep dialog; preset_ts base
cham_gia "thiếu sub-preset của component trực tiếp (dialog) -> FAIL" do

gia; goi_chung; dung_truc_tiep dialog; preset_ts dialog
cham_gia "thiếu base -> FAIL" do

gia; goi_chung; dung_truc_tiep dialog; preset_ts base dialog datepicker
cham_gia "datepicker ngoài bao đóng của dialog (rác) -> FAIL" do

gia; goi_chung; dung_truc_tiep table; preset_ts base datatable
cham_gia "table trực tiếp đổi tên sang datatable theo bảng -> PASS" xanh

gia; goi_chung; dung_truc_tiep table; preset_ts base table
cham_gia "preset dùng tên module 'table' thay vì 'datatable' -> FAIL" do

gia; goi_chung; dung_truc_tiep table; preset_ts base datatable select
cham_gia "select nằm ở tầng thứ hai của bao đóng (table→paginator→select) -> PASS" xanh

gia; goi_chung; dung_truc_tiep dialog; preset_ts base dialog
printf "import Aura from '@primeuix/themes/aura';\n" >> "$DIR/src/app/core/theme/prime-preset.ts"
cham_gia "import Aura gộp -> FAIL" do

gia; goi_chung; dung_truc_tiep dialog; preset_ts base dialog
printf "export const p = import('@primeuix/themes/lara');\n" > "$DIR/src/app/core/theme/khac.ts"
cham_gia "import động một preset gộp ở tệp khác -> FAIL" do

gia; goi_chung; dung_truc_tiep dialog khonglaai; preset_ts base dialog
cham_gia "module lạ: không có sub-preset, không có trong bảng -> FAIL" do

gia; goi_chung; rm -f "$NM/primeng/fesm2022/primeng-dialog.mjs"; dung_truc_tiep dialog; preset_ts base dialog
cham_gia "không đọc được primeng-dialog.mjs của module trực tiếp -> FAIL" do

gia; goi_chung; mjs button; dung_truc_tiep dialog; preset_ts base dialog button
cham_gia "module lá: primeng-button.mjs không import primeng/ nào -> PASS (không chết giữa chừng)" xanh

gia; goi_chung; dung_truc_tiep api config; preset_ts base
cham_gia "chỉ import module không có sub-preset — sàn chỉ còn base -> PASS" xanh

gia; goi_chung; : > "$DIR/src/app/shared/ui/x.ts"; preset_ts base dialog
cham_gia "không có import primeng/ trực tiếp nào -> FAIL (không xanh rỗng)" do

gia; goi_chung; dung_truc_tiep dialog; printf "export const x = 1;\n" > "$DIR/src/app/core/theme/prime-preset.ts"
cham_gia "prime-preset.ts không import sub-preset nào -> FAIL (không xanh rỗng)" do

gia; goi_chung; dung_truc_tiep dialog; preset_ts base dialog; rm -rf "$NM/primeng"
cham_gia "chưa cài gói primeng -> FAIL (không có gì để đọc thì không xanh)" do

gia; goi_chung; dung_truc_tiep dialog; preset_ts base dialog
printf "import { ButtonModule } from 'primeng/datepicker';\n" > "$DIR/src/app/shared/ui/x.spec.ts"
cham_gia "*.spec.ts import primeng/ không mở rộng sàn -> PASS" xanh

printf '\n\033[1m== canary F29 — bản sao code thật, gói thật đã cài ==\033[0m\n'

if [ ! -d "$NM_THAT/primeng/fesm2022" ]; then
  printf '  \033[31mFAIL\033[0m  chưa cài gói thật ở %s — không chạy được nhóm ca này\n' "$NM_THAT"
  NFAIL=$((NFAIL + 1))
else
  that() { dung_ban_sao; NM="$NM_THAT"; }
  PRESET_BS=src/app/core/theme/prime-preset.ts

  that
  cham_gia "bản sao nguyên vẹn -> PASS" xanh

  that; sed -i "/@primeuix\/themes\/aura\/dialog'/d" "$DIR/$PRESET_BS"
  if grep -q "aura/dialog'" "$DIR/$PRESET_BS"; then
    rm -rf "$DIR"; printf '  \033[31mFAIL\033[0m  không gỡ được import aura/dialog — canary không cắm được\n'; NFAIL=$((NFAIL + 1))
  else
    cham_gia "gỡ import sub-preset dialog (component trực tiếp) -> FAIL" do
  fi

  that; sed -i "1i import AuraGalleria from '@primeuix/themes/aura/galleria';" "$DIR/$PRESET_BS"
  cham_gia "thêm sub-preset galleria — ngoài bao đóng của mọi component đang dùng -> FAIL" do

  that; sed -i "1i import AuraDatepicker from '@primeuix/themes/aura/datepicker';" "$DIR/$PRESET_BS"
  cham_gia "thêm datepicker — nằm trong bao đóng của table (trần), chưa dựng -> PASS" xanh
fi

ket_thuc fe-gate-f29.test.sh
