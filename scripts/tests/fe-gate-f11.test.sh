#!/usr/bin/env bash
# scripts/tests/fe-gate-f11.test.sh — canary tự động cho luật F11 của scripts/fe-gate.sh.
#
# Chạy trên BẢN SAO của code thật (scripts/tests/_fixture-that.sh), cắm đúng một vi phạm mỗi ca,
# gọi THẲNG scripts/fe-gate.sh qua biến FE, chấm theo riêng khối output của section F11.
#
# Chạy từ gốc repo:
#     bash scripts/tests/fe-gate-f11.test.sh
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
TEP=src/app/shared/components/zz/zz.component.ts

printf '\n\033[1m== canary F11 — bản sao code thật, gọi thẳng scripts/fe-gate.sh ==\033[0m\n'

dung_ban_sao
cham F11 "bản sao nguyên vẹn -> PASS" xanh

dung_ban_sao; cam "$TEP" "private readonly http = inject(HttpClient);"
cham F11 "component dumb inject(HttpClient) -> FAIL" do

dung_ban_sao; cam "$TEP" "private readonly svc = inject<NguoiDungService>(NguoiDungService);"
cham F11 "inject<T>(Service) -> FAIL" do

dung_ban_sao; cam "$TEP" "private readonly d = inject(DestroyRef); private readonly h = inject(HttpClient);"
cham F11 "token được tha và service dữ liệu trên CÙNG một dòng -> FAIL" do

dung_ban_sao; cam "$TEP" "private readonly d = inject(DestroyRef);
private readonly ctl = inject(NgControl, { self: true, optional: true });
private readonly el = inject<ElementRef<HTMLElement>>(ElementRef);"
cham F11 "chỉ inject token trong bảng được tha (kể cả kèm tuỳ chọn, kể cả inject<T>) -> PASS" xanh

dung_ban_sao; cam src/app/shared/components/zz/zz.component.spec.ts "TestBed.inject(HttpClient); inject(HttpClient);"
cham F11 "*.spec.ts dưới shared/components/ -> PASS (phạm vi là mã chạy thật)" xanh

dung_ban_sao; cam src/app/shared/ui/zz/zz.component.ts "private readonly http = inject(HttpClient);"
cham F11 "shared/ui/ nằm ngoài phạm vi câu luật F11 -> PASS" xanh

dung_ban_sao; rm -rf "$DIR/src/app/shared/components"
cham F11 "không có thư mục shared/components/ -> FAIL (không xanh rỗng)" do

dung_ban_sao; find "$DIR/src/app/shared/components" -name '*.ts' -delete
cham F11 "shared/components/ không còn tệp *.ts nào -> FAIL (không xanh rỗng)" do

ket_thuc fe-gate-f11.test.sh
