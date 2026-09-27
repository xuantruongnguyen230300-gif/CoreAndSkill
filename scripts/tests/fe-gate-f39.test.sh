#!/usr/bin/env bash
# scripts/tests/fe-gate-f39.test.sh — canary tự động cho luật F39 của scripts/fe-gate.sh (ADR-0080).
#
# Chạy trên BẢN SAO của code thật (scripts/tests/_fixture-that.sh), mỗi ca một đột biến đã kiểm là
# vào được bản sao (`doi`), gọi THẲNG scripts/fe-gate.sh qua biến FE, chấm ĐÍCH DANH theo riêng khối
# output của section F39: ca đỏ phải nêu đúng tiêu chí bị vi phạm — mỗi thông điệp của cổng mở đầu
# bằng số tiêu chí `(1)`…`(5)` — và KHÔNG nêu tiêu chí khác khi đột biến chỉ chạm một tiêu chí; ca xanh
# phải nêu đúng tệp mà cổng nhận là tệp mang hàm xuất.
#
# T12:cap-xanh-do — mọi ca kỳ vọng XANH có một ca kỳ vọng ĐỎ đi qua cùng cơ chế dò, khác đúng một vi
# phạm được cắm (RULES.md §8 luật T12); chú thích ngay trên mỗi ca xanh hoặc cặp ca nói cặp ở đâu.
#
# NĂM CA BẮT BUỘC của dòng luật (đều mang chữ "BẮT BUỘC" trong tên ca):
#   - thu dòng nối trong app.config.ts về `provideAnimationsAsync()` trần — đột biến đã đi qua 6/6
#     unit test của app.config.spec.ts ngày 2026-09-24 (ADR-0080 §Bối cảnh);
#   - xoá hai chuỗi của (5) khỏi DÒNG MÃ của tệp Core nhưng giữ nguyên chú thích;
#   - dòng nối chỉ còn trong chú thích;
#   - dời hàm xuất về app.config.ts;
#   - import tĩnh qua bí danh (`provideNoopAnimations as tatHoatAnh`).
#
# Tệp mang hàm xuất được test tìm bằng phép đọc RIÊNG (grep -rl trên cây thật), không đọc từ output
# của cổng, và không viết cứng đường dẫn — ADR-0080 để thư mục cho frontend-expert chọn.
#
# Chạy từ gốc repo:
#     bash scripts/tests/fe-gate-f39.test.sh
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
CAU_HINH=src/app/app.config.ts
XUAT='export function provideCoreAnimations('

# ---------------------------------------------------------------- phép đọc của TEST
TEP_CORE=$(cd "$REAL_FE" && grep -rlF --include='*.ts' --exclude='*.spec.ts' -- "$XUAT" src/app | sort)
[ "$(printf '%s\n' "$TEP_CORE" | grep -c .)" -eq 1 ] || {
  echo "ABORT — cần đúng MỘT tệp mang '$XUAT' dưới src/FE/src/app, thấy: '$TEP_CORE'"; exit 2; }
DONG_NOI='    provideCoreAnimations(),'
grep -qxF -- "$DONG_NOI" "$REAL_FE/$CAU_HINH" || { echo "ABORT — $CAU_HINH thật không còn dòng nối '$DONG_NOI'"; exit 2; }
DONG_IMPORT=$(grep -E '^import \{ provideCoreAnimations \} from ' "$REAL_FE/$CAU_HINH")
[ "$(printf '%s\n' "$DONG_IMPORT" | grep -c .)" -eq 1 ] || { echo "ABORT — $CAU_HINH thật không còn đúng một dòng import provideCoreAnimations"; exit 2; }

ca() { cham_dot_bien F39 "$@"; }

# Dòng import dạng bí danh — điểm vào tĩnh (ca bắt buộc của (3)) và điểm vào nạp lười (cặp xanh của nó).
IMPORT_TINH="import { provideNoopAnimations as tatHoatAnh } from '@angular/platform-browser/animations';"
IMPORT_LUOI="import { provideAnimationsAsync as chayLuoi } from '@angular/platform-browser/animations/async';"
# them_import <rel> <dòng import> — chèn dòng import lên ĐẦU tệp của bản sao (thành dòng 1)
them_import() { X="$2" doi "$1" 's{\A}{$ENV{X}\n}' "$2"; }
# doi_ve <rel đích> — nối NGUYÊN VĂN tệp Core vào cuối tệp đích rồi xoá tệp Core (cơ chế dời hàm xuất)
doi_ve() {
  mkdir -p "$DIR/$(dirname "$1")"
  cat "$DIR/$TEP_CORE" >> "$DIR/$1" && rm -f "${DIR:?}/${TEP_CORE:?}"
}

printf '\n\033[1m== canary F39 — bản sao code thật, gọi thẳng scripts/fe-gate.sh (tệp mang hàm xuất: %s) ==\033[0m\n' "$TEP_CORE"

# ---------------------------------------------------------------- nền + hai ca bắt buộc đầu
# Cặp đỏ: hai ca bắt buộc ngay dưới — cùng bản sao; một: dòng nối thu về provider trần; hai: hai chuỗi
# của (5) rời khỏi dòng mã, chú thích giữ nguyên.
dung_ban_sao
ca "bản sao nguyên vẹn -> PASS, và cổng nêu đúng tệp mang hàm xuất" xanh \
  "$TEP_CORE" "app.config.ts gọi provideCoreAnimations"

dung_ban_sao
X="$DONG_IMPORT" doi "$CAU_HINH" 's{^\Q$ENV{X}\E$}{import { provideAnimationsAsync } from \x27\@angular/platform-browser/animations/async\x27;}m' \
  "import { provideAnimationsAsync }"
doi "$CAU_HINH" 's{^    provideCoreAnimations\(\),$}{    provideAnimationsAsync(),}m' "    provideAnimationsAsync(),"
ca "BẮT BUỘC: dòng nối thu về provideAnimationsAsync() trần (đột biến đã lọt 6/6 unit test) -> FAIL (2)+(4)" do \
  "(2) provideAnimationsAsync( xuất hiện NGOÀI" "app.config.ts:" "(4) " "!(3) "

# Chỉ đổi những dòng KHÔNG mở đầu bằng dấu chú thích (`*`, `/*`, `//` sau khoảng trắng) — tức dòng mã
# của tệp này; chú thích giữ nguyên, và dòng kiểm ngay sau xác nhận hai chuỗi vẫn còn trong đó.
dung_ban_sao
doi "$TEP_CORE" 's{^(?![ \t]*(?:\*|/\*|//))([^\n]*)$}{my $l = $1; $l =~ s/\Q(prefers-reduced-motion: reduce)\E/(prefers-color-scheme: dark)/g; $l =~ s/\x27noop\x27/\x27animations\x27/g; $l}gme' \
  "(prefers-color-scheme: dark)"
LC_ALL=C grep -qF "'noop'" "$DIR/$TEP_CORE" && LC_ALL=C grep -qF '(prefers-reduced-motion: reduce)' "$DIR/$TEP_CORE" \
  || { printf '  \033[31mFAIL\033[0m  đột biến đã xoá cả chuỗi trong CHÚ THÍCH — ca kế tiếp không còn là ca "chỉ chú thích"\n'; NFAIL=$((NFAIL + 1)); HONG=1; }
ca "BẮT BUỘC: hai chuỗi của (5) chỉ còn trong CHÚ THÍCH, dòng mã mất cả hai -> FAIL (5)" do \
  "(5) " "thiếu '(prefers-reduced-motion: reduce)' trên DÒNG MÃ" "thiếu \"'noop'\" trên DÒNG MÃ" "!(2) "

# ---------------------------------------------------------------- (4) dòng nối trên DÒNG MÃ
# Cặp đỏ: ca bắt buộc ngay dưới — chuỗi `// provideCoreAnimations(),` có mặt trong chú thích ở cả hai
# ca; ca đỏ khác đúng một điều: dòng mã thật không còn.
dung_ban_sao
doi "$CAU_HINH" 's{^(    provideCoreAnimations\(\),)$}{    // provideCoreAnimations(), — ghi chú: dòng nối ngay dưới\n$1}m' "// provideCoreAnimations(), — ghi chú"
ca "chú thích nhắc dòng nối, dòng mã thật còn nguyên -> PASS" xanh \
  "app.config.ts gọi provideCoreAnimations"
dung_ban_sao
doi "$CAU_HINH" 's{^    provideCoreAnimations\(\),$}{    // provideCoreAnimations(),}m' "    // provideCoreAnimations(),"
ca "BẮT BUỘC: dòng nối chỉ còn trong CHÚ THÍCH -> FAIL (4) đích danh" do \
  "(4) " "không có 'provideCoreAnimations(' trên DÒNG MÃ" "!(1) " "!(2) " "!(3) "

dung_ban_sao
doi "$CAU_HINH" 's{^    provideCoreAnimations\(\),\n}{}m'
X="$DONG_IMPORT" doi "$CAU_HINH" 's{^\Q$ENV{X}\E\n}{}m'
ca "app.config.ts bỏ hẳn dòng nối và import -> FAIL (4)" do \
  "(4) " "!(2) "

# ---------------------------------------------------------------- (1) hàm xuất nằm dưới core/
# Cùng cơ chế cho cặp (`doi_ve`). Cặp đỏ: ca bắt buộc ngay dưới — tệp đích là app.config.ts (ngoài
# core/) thay cho một tệp mới dưới core/.
DICH_CORE=src/app/core/hoat-anh-zz/chon-hoat-anh.ts
dung_ban_sao; doi_ve "$DICH_CORE"
ca "hàm xuất dời sang tệp khác DƯỚI core/ -> PASS, cổng nêu ĐƯỜNG MỚI (tìm theo tên hàm)" xanh \
  "$DICH_CORE" "!$TEP_CORE"
dung_ban_sao; doi_ve "$CAU_HINH"
ca "BẮT BUỘC: hàm xuất dời về app.config.ts (phương án B ADR-0080 đã loại) -> FAIL (1) đích danh" do \
  "(1) " "app.config.ts mang 'export function provideCoreAnimations(' nhưng nằm NGOÀI" "!(2) " "!(4) " "!(5) "

# Miễn trừ của (2) bám TỆP mang hàm xuất, không bám đường dẫn cũ — cặp đỏ thứ hai của ca xanh "dời
# sang tệp khác dưới core/": cùng lần dời, cắm lại đúng một lời gọi provideAnimationsAsync( vào ĐƯỜNG CŨ.
dung_ban_sao; doi_ve "$DICH_CORE"
cam "$TEP_CORE" 'export const zz = () => provideAnimationsAsync();'
ca "dời tệp Core + một lời gọi ở ĐƯỜNG CŨ -> FAIL (2) (miễn trừ không bám đường dẫn)" do \
  "(2) provideAnimationsAsync( xuất hiện NGOÀI" "$TEP_CORE:"

# ---------------------------------------------------------------- (2) provideAnimationsAsync( chỉ ở tệp đó, trên dòng mã
# Cặp đỏ: ca ngay dưới — cùng lời gọi nối vào cùng tệp, không có `// ` đứng đầu nên là dòng mã.
dung_ban_sao; printf '// Xem provideAnimationsAsync() trong tệp của Core.\n' >> "$DIR/$CAU_HINH"
ca "chú thích nhắc provideAnimationsAsync() ngoài tệp Core -> PASS (dò trên dòng mã)" xanh \
  "$TEP_CORE"
dung_ban_sao; printf 'export const zzXem = provideAnimationsAsync();\n' >> "$DIR/$CAU_HINH"
ca "cùng lời gọi là DÒNG MÃ ngoài tệp Core -> FAIL (2)" do \
  "(2) provideAnimationsAsync( xuất hiện NGOÀI" "app.config.ts:"

THU_MUC_CORE="$(dirname "$TEP_CORE")"
dung_ban_sao
printf 'export const zzHoatAnh = () => provideAnimationsAsync();\n' >> "$DIR/$THU_MUC_CORE/zz-ke-ben.ts"
ca "provideAnimationsAsync( ở tệp KHÁC cùng thư mục với tệp Core -> FAIL (2) (miễn trừ theo tệp, không theo thư mục)" do \
  "(2) provideAnimationsAsync( xuất hiện NGOÀI" "zz-ke-ben.ts:"

# ---------------------------------------------------------------- (3) điểm vào tĩnh
# Cùng cơ chế cho cặp: chèn một import BÍ DANH lên đầu tệp Core, gọi bí danh ở nhánh vắng matchMedia.
# Cặp đỏ: ca bắt buộc ngay dưới — khác đúng điểm vào: `…/animations` thay cho `…/animations/async`.
dung_ban_sao
them_import "$TEP_CORE" "$IMPORT_LUOI"
doi "$TEP_CORE" 's{return provideAnimationsAsync\(\x27noop\x27\);}{return chayLuoi(\x27noop\x27);}' "return chayLuoi('noop');"
ca "bí danh từ …/animations/async (điểm vào nạp lười) -> PASS" xanh \
  "$TEP_CORE"
dung_ban_sao
them_import "$TEP_CORE" "$IMPORT_TINH"
doi "$TEP_CORE" 's{return provideAnimationsAsync\(\x27noop\x27\);}{return tatHoatAnh();}' "return tatHoatAnh();"
ca "BẮT BUỘC: import tĩnh qua bí danh (provideNoopAnimations as tatHoatAnh) -> FAIL (3) đích danh" do \
  "(3) dòng mã nhắc ĐIỂM VÀO TĨNH" "$TEP_CORE:1:" "!(2) " "!(5) "

# "Sửa cho gọn" ở composition root: dòng nối thành provideNoopAnimations() kèm import của nó.
dung_ban_sao
X="$DONG_IMPORT" doi "$CAU_HINH" 's{^\Q$ENV{X}\E$}{import { provideNoopAnimations } from \x27\@angular/platform-browser/animations\x27;}m' \
  "import { provideNoopAnimations }"
doi "$CAU_HINH" 's{^    provideCoreAnimations\(\),$}{    provideNoopAnimations(),}m' "    provideNoopAnimations(),"
ca "\"sửa cho gọn\": app.config.ts dùng provideNoopAnimations() -> FAIL (3) + (4)" do \
  "(3) dòng mã nhắc ĐIỂM VÀO TĨNH" "app.config.ts:" "(4) "

# Dạng không gian tên, nháy kép, dạng module — cùng một phép dò theo điểm vào.
# Cặp xanh: ca ngay dưới — cùng nội dung trong *.spec.ts cùng thư mục (phạm vi là mã không phải spec).
NS_MODULE='import * as hoatAnh from "@angular/platform-browser/animations";
export const zzModule = [hoatAnh.NoopAnimationsModule, hoatAnh.BrowserAnimationsModule];'
dung_ban_sao; cam src/app/core/zz/zz.ts "$NS_MODULE"
ca "import cả không gian tên (nháy kép) + dạng module -> FAIL (3)" do \
  "(3) dòng mã nhắc ĐIỂM VÀO TĨNH" "zz/zz.ts:1:"
dung_ban_sao; cam src/app/core/zz/zz.spec.ts "$NS_MODULE"
ca "cùng nội dung trong *.spec.ts -> PASS (phạm vi là mã không phải spec)" xanh \
  "$TEP_CORE"

# Cặp xanh: ca ngay dưới — cùng câu import nối vào cùng tệp, nằm trong chú thích.
dung_ban_sao; printf '%s\n' "$IMPORT_TINH" >> "$DIR/$CAU_HINH"
ca "import điểm vào tĩnh là dòng mã ở app.config.ts -> FAIL (3)" do \
  "(3) dòng mã nhắc ĐIỂM VÀO TĨNH" "app.config.ts:"
dung_ban_sao; printf '// %s\n' "$IMPORT_TINH" >> "$DIR/$CAU_HINH"
ca "cùng câu import nằm trong chú thích -> PASS (dò trên dòng mã)" xanh \
  "$TEP_CORE"

# ---------------------------------------------------------------- (5) và các chốt chống xanh rỗng
dung_ban_sao
doi "$TEP_CORE" 's{\Qexport function provideCoreAnimations(\E}{export function provideHoatAnhCore(}' "export function provideHoatAnhCore("
ca "hàm xuất đổi tên -> FAIL chốt 'không tìm ra tệp', không phải 'không có vi phạm'" do \
  "không tìm ra tệp nào"

dung_ban_sao
doi "$TEP_CORE" 's{provideAnimationsAsync\(}{taoBoChay(}g' "taoBoChay("
ca "tệp Core không còn chuỗi provideAnimationsAsync( nào -> FAIL chốt thứ hai" do \
  "khớp 'provideAnimationsAsync('" "!(5) "

dung_ban_sao
doi "$TEP_CORE" 's{\Q(prefers-reduced-motion: reduce)\E}{(prefers-color-scheme: dark)}g' "(prefers-color-scheme: dark)"
ca "tệp Core không còn '(prefers-reduced-motion: reduce)' ở bất cứ đâu -> FAIL (5)" do \
  "thiếu '(prefers-reduced-motion: reduce)'" "!thiếu \"'noop'\""

dung_ban_sao
doi "$TEP_CORE" 's{\x27noop\x27}{\x27animations\x27}g' "'animations'"
ca "tệp Core không còn 'noop' ở bất cứ đâu -> FAIL (5)" do \
  "thiếu \"'noop'\"" "!thiếu '(prefers-reduced-motion"

dung_ban_sao; rm -f "${DIR:?}/${CAU_HINH:?}"
ca "thiếu hẳn app.config.ts -> FAIL" do \
  "không kiểm được composition root"

dung_ban_sao; rm -rf "${DIR:?}/src/app"
ca "không có src/app để quét -> FAIL" do \
  "để quét"

ket_thuc fe-gate-f39.test.sh
