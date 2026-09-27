#!/usr/bin/env bash
# scripts/tests/fe-gate-f10.test.sh — canary tự động cho luật F10 của scripts/fe-gate.sh (components/ và pages/ không
# import DTO trực tiếp).
#
# Chạy trên BẢN SAO của code thật (scripts/tests/_fixture-that.sh), gọi THẲNG scripts/fe-gate.sh qua biến FE, chấm ĐÍCH
# DANH theo riêng khối output của section F10: ca đỏ phải nêu đúng tệp VÀ dòng bị cắm; ca xanh phải in số tệp đã quét —
# bằng chứng cổng đã đọc được đầu vào, không xanh rỗng (RULES.md §8 luật T6).
#
# Hai phép dò của F10, mỗi phép có ca đỏ riêng:
#   (1) theo TÊN — định danh kết thúc bằng `Dto`;
#   (2) theo TỆP — đường nhập trỏ tới tệp `*.dto`: `from '…dto'` (import, export … from, nhập nhiều tên bị prettier bẻ
#       dòng), `import('…dto')` (kể cả khi bị bẻ dòng), `import '…dto'`. Kiểu dây không mang tên …Dto (…Payload) chỉ (2)
#       thấy — ca BẮT BUỘC của core-reviewer: `import type { CapNhatHoSoPayload } from '../../models/ho-so.dto';` trong một
#       page đã lọt qua bản chỉ có (1).
# Phạm vi: services/ và core/ ĐƯỢC đọc DTO (fe-api-client.md §4.1: `services/` hoặc `core/<mảng>/`), *.spec.ts ngoài phạm vi
# (05-gate.md §8.7) — mỗi điều có ca xanh cặp với ca đỏ cùng dòng nhập đặt ở pages/.
#
# T12:cap-xanh-do — mọi ca kỳ vọng XANH có một ca kỳ vọng ĐỎ đi qua cùng cơ chế dò, khác đúng một vi phạm được cắm
# (RULES.md §8 luật T12); chú thích "cặp đỏ:" ngay trên mỗi ca xanh nói cặp ở đâu.
#
# Chạy từ gốc repo:
#     bash scripts/tests/fe-gate-f10.test.sh
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
ca() { cham_dot_bien F10 "$@"; }
XANH_RONG='cổng sẽ xanh rỗng'

PAGE=src/app/platform/ho-so/pages/zz/zz.page.ts
COMP=src/app/platform/quan-tri/nguoi-dung/components/zz/zz.component.ts
NHAP_PAYLOAD="import type { CapNhatHoSoPayload } from '../../models/ho-so.dto';"

printf '\n\033[1m== canary F10 — bản sao code thật, gọi thẳng scripts/fe-gate.sh ==\033[0m\n'

# ---------------------------------------------------------------- nền
# Cặp đỏ: ca BẮT BUỘC ngay dưới — cùng bản sao, thêm đúng một page nhập kiểu dây từ tệp *.dto.
dung_ban_sao
ca "bản sao nguyên vẹn -> PASS, có in số tệp đã quét" xanh "tệp đã quét" "!$XANH_RONG"

# ---------------------------------------------------------------- (2) theo tệp — ca của core-reviewer và cặp của nó
dung_ban_sao; cam "$PAGE" "$NHAP_PAYLOAD"
ca "BẮT BUỘC: page nhập …Payload từ ../../models/ho-so.dto -> FAIL (tên không mang Dto)" do \
  "pages/zz/zz.page.ts:1:" "!$XANH_RONG"

# Cặp đỏ: ca BẮT BUỘC ở trên — cùng page, cùng chỗ, dòng nhập trỏ tệp *.model thay vì *.dto.
dung_ban_sao; cam "$PAGE" "import type { HoSo } from '../../models/ho-so.model';"
ca "page nhập model từ ../../models/ho-so.model -> PASS" xanh "tệp đã quét"

# Cặp đỏ: ca BẮT BUỘC ở trên — CÙNG dòng nhập, tệp đặt ở services/ thay vì pages/.
dung_ban_sao; cam src/app/platform/ho-so/services/zz/zz.service.ts "$NHAP_PAYLOAD"
ca "services/ nhập …Payload từ tệp *.dto -> PASS (services/ được phép)" xanh "tệp đã quét"

# Cặp đỏ: ca BẮT BUỘC ở trên — CÙNG dòng nhập, cùng thư mục pages/zz/, tệp *.spec.ts thay vì tệp chạy thật.
dung_ban_sao; cam src/app/platform/ho-so/pages/zz/zz.page.spec.ts "$NHAP_PAYLOAD"
ca "*.spec.ts trong pages/ nhập tệp *.dto -> PASS (spec ngoài phạm vi, 05-gate.md §8.7)" xanh "tệp đã quét"

# Hình dạng của core/auth (fe-api-client.md §4.2): kiểu dây trong tệp *.dto.ts, service cùng thư mục nhập nó.
# Cặp đỏ: ca ngay dưới — cùng tên, cùng tệp *.dto đích, nơi nhập là một page.
dung_ban_sao
cam src/app/core/auth/zz-doi-mat-khau.dto.ts 'export interface DoiMatKhauPayload { matKhauMoi: string; }'
cam src/app/core/auth/zz-doi-mat-khau.service.ts "import { DoiMatKhauPayload } from './zz-doi-mat-khau.dto';"
ca "core/auth/*.service.ts nhập tệp *.dto cùng thư mục -> PASS (core/ được phép)" xanh "tệp đã quét"

dung_ban_sao
cam src/app/core/auth/zz-doi-mat-khau.dto.ts 'export interface DoiMatKhauPayload { matKhauMoi: string; }'
cam "$PAGE" "import { DoiMatKhauPayload } from '../../../../core/auth/zz-doi-mat-khau.dto';"
ca "page nhập DoiMatKhauPayload từ core/auth/*.dto -> FAIL" do "pages/zz/zz.page.ts:1:"

dung_ban_sao; cam "$COMP" "import { TaoNguoiDungPayload } from '../../models/nguoi-dung.dto';"
ca "components/ nhập …Payload từ tệp *.dto -> FAIL" do "components/zz/zz.component.ts:1:"

dung_ban_sao; cam "$COMP" "import {
  CapNhatNguoiDungPayload,
  TaoNguoiDungPayload,
} from '../../models/nguoi-dung.dto';"
ca "nhập nhiều tên bị prettier bẻ dòng -> FAIL, nêu dòng mang from" do "components/zz/zz.component.ts:4:"

dung_ban_sao; cam "$COMP" "export type { TaoNguoiDungPayload } from '../../models/nguoi-dung.dto';"
ca "components/ tái xuất (export … from) tệp *.dto -> FAIL" do "components/zz/zz.component.ts:1:"

dung_ban_sao; cam "$PAGE" 'import type { CapNhatHoSoPayload } from "../../models/ho-so.dto";'
ca "nháy kép quanh đường nhập tệp *.dto -> FAIL" do "pages/zz/zz.page.ts:1:"

dung_ban_sao; cam "$PAGE" "type GuiLen = import(
  '../../models/ho-so.dto'
).CapNhatHoSoPayload;"
ca "kiểu nhập nội tuyến import('…dto') bị bẻ dòng -> FAIL" do "pages/zz/zz.page.ts:1:"

dung_ban_sao; cam "$PAGE" "import '../../models/ho-so.dto';"
ca "nhập trần import '…dto' -> FAIL" do "pages/zz/zz.page.ts:1:"

# ---------------------------------------------------------------- (1) theo tên — giữ nguyên phép dò cũ
# Cặp đỏ: ca ngay dưới — cùng page, cùng dòng, kiểu HoSo thay bằng HoSoDto.
dung_ban_sao; cam "$PAGE" "export class ZzPage { readonly hoSo = signal<HoSo | null>(null); }"
ca "page dùng kiểu model HoSo -> PASS" xanh "tệp đã quét"

dung_ban_sao; cam "$PAGE" "export class ZzPage { readonly hoSo = signal<HoSoDto | null>(null); }"
ca "page dùng tên …Dto, không có dòng nhập -> FAIL (phép dò theo tên)" do "pages/zz/zz.page.ts:1:"

# ---------------------------------------------------------------- chốt tập rỗng (T6)
# Cặp xanh của hai ca này: "bản sao nguyên vẹn" ở đầu tệp.
dung_ban_sao; find "$DIR/src/app" -name '*.ts' -path '*/components/*' -delete; find "$DIR/src/app" -name '*.ts' -path '*/pages/*' -delete
ca "không còn tệp *.ts nào dưới components/ hoặc pages/ -> FAIL (không xanh rỗng)" do "$XANH_RONG" "!OK    "

dung_ban_sao; find "$DIR/src/app" -name '*.dto.ts' -delete
ca "không còn tệp *.dto.ts nào -> FAIL (phép dò theo đường nhập không còn gì để thấy)" do "tệp *.dto.ts" "!OK    "

ket_thuc fe-gate-f10.test.sh
