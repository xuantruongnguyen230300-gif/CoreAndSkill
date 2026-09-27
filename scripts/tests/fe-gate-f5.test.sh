#!/usr/bin/env bash
# scripts/tests/fe-gate-f5.test.sh — canary tự động cho luật F5 của scripts/fe-gate.sh.
#
# Chạy trên BẢN SAO của code thật (scripts/tests/_fixture-that.sh): allowlist F5 trỏ tới đường dẫn
# thật, nên một fixture tối giản sẽ đỏ vì "đường dẫn không tồn tại" chứ không vì vi phạm được cắm.
# Mỗi ca cắm đúng một vi phạm vào bản sao, gọi THẲNG scripts/fe-gate.sh qua biến FE, và chấm theo
# riêng khối output của section F5.
#
# Chạy từ gốc repo:
#     bash scripts/tests/fe-gate-f5.test.sh
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
PRIME="import { ButtonModule } from 'primeng/button';"

printf '\n\033[1m== canary F5 — bản sao code thật, gọi thẳng scripts/fe-gate.sh ==\033[0m\n'

dung_ban_sao
cham F5 "bản sao nguyên vẹn -> PASS" xanh

dung_ban_sao; cam src/app/platform/zz/zz.page.ts "$PRIME"
cham F5 "platform/…/*.page.ts import primeng/ -> FAIL" do

dung_ban_sao; cam src/app/shared/components/zz/zz.component.ts "$PRIME"
cham F5 "shared/components/ import primeng/ -> FAIL" do

dung_ban_sao; cam src/app/core/http/zz.ts "$PRIME"
cham F5 "core/ ngoài theme/ và i18n/ import primeng/ -> FAIL" do

dung_ban_sao; cam src/app/shared/ui/zz/zz.component.ts "$PRIME"
cham F5 "shared/ui/ import primeng/ (nằm trong allowlist) -> PASS" xanh

dung_ban_sao; cam src/app/core/i18n/zz.ts "import { PrimeNG } from 'primeng/config';"
cham F5 "core/i18n/ import primeng/config (nằm trong allowlist) -> PASS" xanh

dung_ban_sao; cam src/app/platform/zz/zz.page.spec.ts "$PRIME"
cham F5 "*.spec.ts ngoài allowlist import primeng/ -> PASS (phạm vi là mã chạy thật)" xanh

dung_ban_sao; rm -rf "$DIR/src/app/core/theme"
cham F5 "allowlist trỏ tới đường dẫn đã bị xoá -> FAIL (cổng khoá chỗ trống là cổng mù)" do

dung_ban_sao; find "$DIR/src/app" -name '*.ts' -delete
cham F5 "không còn tệp *.ts nào để quét -> FAIL (không xanh rỗng)" do

ket_thuc fe-gate-f5.test.sh
