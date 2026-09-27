#!/usr/bin/env bash
# scripts/tests/fe-gate-f9.test.sh — canary tự động cho LỚP SCRIPT của luật F9 (cú pháp Angular cũ) trong
# scripts/fe-gate.sh. Lớp ESLint của cùng luật có canary riêng: scripts/tests/fe-lint-f9.test.sh.
#
# Chạy trên BẢN SAO của code thật (scripts/tests/_fixture-that.sh), gọi THẲNG scripts/fe-gate.sh qua biến FE, chấm ĐÍCH
# DANH theo riêng khối output của section F9: ca đỏ phải nêu đúng tệp bị cắm; ca xanh phải in số tệp đã quét — bằng
# chứng cổng đã đọc được đầu vào, không xanh rỗng (RULES.md §8 luật T6).
#
# Mỗi nhánh của mẫu dò (lệnh gốc: fe-ui-conventions.md §1.4) có một ca đỏ riêng: *ngIf, *ngFor, *ngSwitch, @Input(),
# @Output(), @ViewChild(, NgModule; cộng ca template nội tuyến trong .ts, ca chú thích gõ lại cú pháp cũ (§1.4: "chú
# thích gọi tên, không gõ lại"), và chốt tập rỗng.
#
# T12:cap-xanh-do — mọi ca kỳ vọng XANH có một ca kỳ vọng ĐỎ đi qua cùng cơ chế dò, khác đúng một vi phạm được cắm
# (RULES.md §8 luật T12); chú thích "cặp đỏ:" ngay trên mỗi ca xanh nói cặp ở đâu.
#
# Chạy từ gốc repo:
#     bash scripts/tests/fe-gate-f9.test.sh
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
ca() { cham_dot_bien F9 "$@"; }
XANH_RONG='cổng sẽ xanh rỗng'
HTML=src/app/platform/zz/zz.component.html
TS=src/app/platform/zz/zz.component.ts

printf '\n\033[1m== canary F9 (lớp script) — bản sao code thật, gọi thẳng scripts/fe-gate.sh ==\033[0m\n'

# ---------------------------------------------------------------- nền
# Cặp đỏ: mọi ca đỏ bên dưới — cùng bản sao, thêm đúng một tệp mang cú pháp cũ.
dung_ban_sao
ca "bản sao nguyên vẹn -> PASS, có in số tệp đã quét" xanh "tệp đã quét" "!$XANH_RONG"

# ---------------------------------------------------------------- template
# Cặp đỏ: ca *ngIf ngay dưới — cùng tệp .html, cùng nhánh, @if thay bằng *ngIf.
dung_ban_sao; cam "$HTML" '@if (hien()) { <span>{{ nhan() }}</span> }'
ca "control flow mới @if -> PASS" xanh "tệp đã quét"

dung_ban_sao; cam "$HTML" '<span *ngIf="hien()">{{ nhan() }}</span>'
ca "*ngIf trong .html -> FAIL, nêu đúng tệp" do "zz/zz.component.html:1:" "!$XANH_RONG"

dung_ban_sao; cam "$HTML" '<li *ngFor="let x of ds()">{{ x }}</li>'
ca "*ngFor trong .html -> FAIL" do "zz/zz.component.html:1:"

dung_ban_sao; cam "$HTML" '<div [ngSwitch]="kieu()"><span *ngSwitchCase="1">a</span></div>'
ca "*ngSwitchCase trong .html -> FAIL" do "zz/zz.component.html:1:"

dung_ban_sao; cam "$TS" "@Component({ selector: 'app-zz', template: '<li *ngFor=\"let x of ds()\">{{ x }}</li>' })
export class ZzComponent {}"
ca "*ngFor trong template nội tuyến của .ts -> FAIL" do "zz/zz.component.ts:1:"

dung_ban_sao; cam "$HTML" '<!-- trước đây dùng *ngIf ở đây -->'
ca "chú thích GÕ LẠI *ngIf -> FAIL (§1.4: chú thích gọi tên, không gõ lại)" do "zz/zz.component.html:1:"

# ---------------------------------------------------------------- decorator
# Cặp đỏ: ca @Input() ngay dưới — cùng tệp .ts, cùng dòng, input() thay bằng decorator.
dung_ban_sao; cam "$TS" "export class ZzComponent { readonly nhan = input<string>(''); }"
ca "input() signal -> PASS" xanh "tệp đã quét"

dung_ban_sao; cam "$TS" "export class ZzComponent { @Input() nhan = ''; }"
ca "@Input() -> FAIL, nêu đúng tệp" do "zz/zz.component.ts:1:"

dung_ban_sao; cam "$TS" "export class ZzComponent { @Output() doi = new EventEmitter<string>(); }"
ca "@Output() -> FAIL" do "zz/zz.component.ts:1:"

dung_ban_sao; cam "$TS" "export class ZzComponent { @ViewChild('o') o!: ElementRef; }"
ca "@ViewChild( -> FAIL" do "zz/zz.component.ts:1:"

dung_ban_sao; cam "$TS" "@NgModule({ declarations: [] })
export class ZzModule {}"
ca "NgModule -> FAIL" do "zz/zz.component.ts:1:"

# ---------------------------------------------------------------- chốt tập rỗng (T6)
# Cặp xanh: "bản sao nguyên vẹn" ở đầu tệp.
dung_ban_sao; find "$DIR/src/app" \( -name '*.ts' -o -name '*.html' \) -delete
ca "không còn tệp *.ts / *.html nào dưới src/app -> FAIL (không xanh rỗng)" do "$XANH_RONG" "!OK    "

ket_thuc fe-gate-f9.test.sh
