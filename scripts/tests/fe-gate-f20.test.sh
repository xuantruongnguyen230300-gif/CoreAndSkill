#!/usr/bin/env bash
# scripts/tests/fe-gate-f20.test.sh — canary tự động cho luật F20 của scripts/fe-gate.sh (app.routes.ts không
# import tĩnh component của feature dưới ./platform/ hay ./modules/ — feature chỉ vào qua loadChildren/loadComponent).
#
# Chạy trên BẢN SAO của code thật (scripts/tests/_fixture-that.sh), gọi THẲNG scripts/fe-gate.sh qua biến FE, chấm ĐÍCH
# DANH theo riêng khối output của section F20: ca đỏ nêu đúng dòng import bị cắm; ca xanh nêu câu OK của cổng và không
# mang câu FAIL — F20 chỉ đọc đúng MỘT tệp nêu tên cố định, nên chốt T6 là: tệp vắng thì đỏ (ca riêng bên dưới), và tiền
# đề canary kiểm bản sao có import động ./platform/ (tệp xanh có nội dung để dò, không phải tệp trống).
#
# Ba điều canary này ghim:
#   - import tĩnh từ ./platform/ và từ ./modules/ đều đỏ (cổng canh cả hai thư mục feature);
#   - chỉ app.routes.ts bị dò — import tĩnh ở tệp khác không làm F20 đỏ;
#   - dòng chú thích và import ngoài feature (./core/, thư viện) không bị bắt nhầm.
#
# T12:cap-xanh-do — mọi ca kỳ vọng XANH có một ca kỳ vọng ĐỎ đi qua cùng cơ chế dò, khác đúng một vi phạm được cắm
# (RULES.md §8 luật T12); chú thích "cặp đỏ:" ngay trên mỗi ca xanh nói cặp ở đâu.
#
# Chạy từ gốc repo:
#     bash scripts/tests/fe-gate-f20.test.sh
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

ROUTES=src/app/app.routes.ts
# Tiền đề (T6): app.routes.ts thật phải đang có import động ./platform/ — nếu không, ca xanh dưới đây là xanh trên tệp
# không còn gì để dò.
LC_ALL=C grep -qF "import('./platform/" "$REAL_FE/$ROUTES" \
  || { echo "ABORT — $REAL_FE/$ROUTES không còn import động ./platform/ — tiền đề của canary F20 đã đổi"; exit 2; }

NFAIL=0
ca() { cham_dot_bien F20 "$@"; }
# Tiêu đề section cũng mang câu "app.routes.ts không import…" — chuỗi chấm gồm cả nhãn OK/FAIL để chỉ khớp DÒNG KẾT QUẢ.
OK_F20='OK    app.routes.ts không import tĩnh component của feature'
FAIL_F20='FAIL  app.routes.ts import tĩnh component của feature:'
STATIC_PLATFORM="import { ZzPage } from './platform/zz/zz.page';"
STATIC_MODULES="import { ZzPage } from './modules/zz/zz.page';"

# dau_tep <dòng> — thêm một dòng vào ĐẦU app.routes.ts của bản sao, kiểm dòng đã vào
dau_tep() { X="$1" doi "$ROUTES" 's{\A}{$ENV{X}\n}' "$1"; }

printf '\n\033[1m== canary F20 — bản sao code thật, gọi thẳng scripts/fe-gate.sh ==\033[0m\n'

# ---------------------------------------------------------------- nền
# Cặp đỏ: hai ca ngay dưới — cùng bản sao, thêm đúng một import tĩnh.
dung_ban_sao
ca "bản sao nguyên vẹn -> PASS, có câu OK của cổng" xanh "$OK_F20" "!$FAIL_F20"

# ---------------------------------------------------------------- import tĩnh vào feature
dung_ban_sao; dau_tep "$STATIC_PLATFORM"
ca "import tĩnh từ ./platform/ -> FAIL, nêu đúng dòng import" do "$FAIL_F20" "$STATIC_PLATFORM" "!$OK_F20"

dung_ban_sao; dau_tep "$STATIC_MODULES"
ca "import tĩnh từ ./modules/ -> FAIL, nêu đúng dòng import" do "$FAIL_F20" "$STATIC_MODULES"

dung_ban_sao; dau_tep "import ZzPage from './platform/zz/zz.page';"
ca "import mặc định (default import) từ ./platform/ -> FAIL" do "import ZzPage from './platform/zz/zz.page';"

dung_ban_sao; dau_tep "import * as Zz from './modules/zz/zz.page';"
ca "import namespace (* as) từ ./modules/ -> FAIL" do "import * as Zz from './modules/zz/zz.page';"

# ---------------------------------------------------------------- ca trông giống vi phạm nhưng hợp lệ
# Cặp đỏ: ca "import tĩnh từ ./platform/" ở trên — cùng dòng, bỏ chú thích thành import thật.
dung_ban_sao; dau_tep "// $STATIC_PLATFORM"
ca "cùng import nằm sau // (chú thích) -> PASS" xanh "$OK_F20" "!$FAIL_F20"

# Cặp đỏ: ca "import tĩnh từ ./platform/" — cùng cú pháp import, khác đích: ./core/ không phải feature.
dung_ban_sao; dau_tep "import { ZzGuard } from './core/guards/zz.guard';"
ca "import tĩnh từ ./core/ (không phải feature) -> PASS" xanh "$OK_F20" "!$FAIL_F20"

# Cặp đỏ: ca "import tĩnh từ ./platform/" — cùng đích, nhưng là import động trong loadChildren (cách feature được phép vào).
dung_ban_sao; dau_tep "export const ZZ_ROUTES = { loadChildren: () => import('./platform/zz/zz.routes').then((m) => m.ZZ) };"
ca "feature vào bằng import('./platform/…') động -> PASS" xanh "$OK_F20" "!$FAIL_F20"

# ---------------------------------------------------------------- cổng chỉ dò app.routes.ts
# Cặp đỏ: ca "import tĩnh từ ./platform/" — cùng dòng import, đặt ở app.routes.ts.
dung_ban_sao; cam src/app/app.zz.ts "$STATIC_PLATFORM"
ca "cùng import tĩnh ./platform/ ở tệp KHÁC app.routes.ts -> PASS (F20 chỉ dò app.routes.ts)" xanh "$OK_F20" "!$FAIL_F20"

# ---------------------------------------------------------------- chốt tệp vắng (T6)
# Cặp xanh: ca "bản sao nguyên vẹn" ở đầu tệp.
dung_ban_sao; rm -f "$DIR/$ROUTES"
ca "không còn app.routes.ts -> FAIL (không xanh rỗng)" do "để quét" "!$OK_F20"

ket_thuc fe-gate-f20.test.sh
