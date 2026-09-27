#!/usr/bin/env bash
# scripts/tests/fe-lint-f9.test.sh — canary cho LỚP ESLint của luật F9: rule
# `@angular-eslint/template/prefer-control-flow` phải bật ở mức error trong src/FE/eslint.config.js
# (docs/quy-uoc/fe-architecture.md §4.5, bảng rule cấm tắt; docs/RULES.md §7 F9).
#
# Lớp `fe-gate.sh` của F9 chỉ grep `*ngIf|*ngFor|*ngSwitch` — nó mù trước dạng `<ng-template [ngIf]>`,
# `[ngForOf]`, `[ngSwitch]` (cùng directive, không có dấu `*`). Rule ESLint bắt cả dạng đó, ở cả
# tệp .html lẫn template nội tuyến trong .ts (`angular.processInlineTemplates`).
#
# Gọi THẲNG ESLint của src/FE với eslint.config.js thật qua `--stdin` + `--stdin-filename` — không
# để lại tệp nào, không chép lại rule (luật T1, docs/RULES.md §8). Ca đỏ phải đỏ ĐÚNG tên rule.
#
# Chạy từ gốc repo:
#     bash scripts/tests/fe-lint-f9.test.sh
#
# Thoát 0 = mọi ca đúng kỳ vọng · 1 = có ca sai kỳ vọng · 2 = không chạy được (thiếu node_modules).

set -uo pipefail

T="$(cd "$(dirname "$0")" && pwd)"
REPO="$(cd "$T/../.." && pwd)"
FE="$REPO/src/FE"
ESLINT="$FE/node_modules/eslint/bin/eslint.js"

[ -f "$ESLINT" ] || { echo "ABORT — không thấy $ESLINT (chạy npm ci trong src/FE)"; exit 2; }
[ -f "$FE/eslint.config.js" ] || { echo "ABORT — không thấy $FE/eslint.config.js"; exit 2; }
command -v node >/dev/null 2>&1 || { echo "ABORT — thiếu node trên PATH"; exit 2; }

NFAIL=0
RULE='@angular-eslint/template/prefer-control-flow'

# stdin_case <tên ca> <đường dẫn giả> <nội dung> <kỳ vọng: do|xanh>
stdin_case() {
  local name="$1" file="$2" body="$3" expect="$4" out code actual
  out="$(cd "$FE" && printf '%s\n' "$body" | node "$ESLINT" --stdin --stdin-filename "$file" 2>&1)"
  code=$?
  if [ "$code" -eq 0 ]; then
    actual=xanh
  elif printf '%s\n' "$out" | grep -qF "$RULE"; then
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

printf '\n\033[1m== canary F9 (lớp ESLint) — prefer-control-flow qua eslint --stdin ==\033[0m\n'

stdin_case "<ng-template [ngIf]> trong .html -> ĐỎ (dạng grep của fe-gate.sh không thấy)" \
  src/app/shared/ui/x/x.component.html \
  '<ng-template [ngIf]="a"><p>x</p></ng-template>' do

stdin_case "*ngIf trong .html -> ĐỎ" \
  src/app/shared/ui/x/x.component.html \
  '<p *ngIf="a">x</p>' do

stdin_case "<ng-template ngFor [ngForOf]> trong .html -> ĐỎ" \
  src/app/shared/ui/x/x.component.html \
  '<ng-template ngFor let-i [ngForOf]="ds"><p>{{ i }}</p></ng-template>' do

stdin_case "<ng-template [ngIf]> trong template nội tuyến của .ts -> ĐỎ" \
  src/app/shared/ui/x/x.component.ts \
  "import { Component } from '@angular/core';
@Component({ selector: 'app-x', template: '<ng-template [ngIf]=\"a\"><p>x</p></ng-template>' })
export class XComponent {
  a = true;
}" do

stdin_case "@if / @for trong .html -> XANH (đối chứng)" \
  src/app/shared/ui/x/x.component.html \
  '@if (a) {
  <p>x</p>
}
@for (i of ds; track i) {
  <p>{{ i }}</p>
}' xanh

printf '\n'
if [ "$NFAIL" -eq 0 ]; then
  printf '\033[32m✔ fe-lint-f9.test.sh PASS — mọi ca canary đúng kỳ vọng\033[0m\n'
  exit 0
fi
printf '\033[31m✘ fe-lint-f9.test.sh FAIL — %s ca sai kỳ vọng, xem chi tiết ở trên\033[0m\n' "$NFAIL"
exit 1
