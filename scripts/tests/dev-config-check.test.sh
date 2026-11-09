#!/usr/bin/env bash
# scripts/tests/dev-config-check.test.sh — canary của scripts/dev-config-check.sh (Q41).
# Mỗi ca chạy cổng trên BẢN CHÉP của bốn tệp nguồn ở thư mục tạm (DEV_CFG_ROOT), không chạm tệp thật;
# ca đỏ chấm ĐÍCH DANH: thông điệp phải nêu đúng tệp + giá trị lệch (không chỉ "có FAIL ở đâu đó").
#
# T12:cap-xanh-do — mỗi cặp có một ca XANH (bản chép nguyên vẹn, hoặc đổi ĐỒNG BỘ cả hai chỗ) và một ca
# ĐỎ khác đúng một chỗ; chú thích "cặp:" nêu ca đối ứng.
#
# Chạy từ gốc repo:  bash scripts/tests/dev-config-check.test.sh
# Thoát 0 = mọi ca đúng kỳ vọng · 1 = có ca sai kỳ vọng · 2 = không chạy được.
set -uo pipefail
export LC_ALL=C

T="$(cd "$(dirname "$0")" && pwd)"
REPO="$(cd "$T/../.." && pwd)"
GATE="$REPO/scripts/dev-config-check.sh"
[ -f "$GATE" ] || { echo "ABORT — không thấy scripts/dev-config-check.sh"; exit 2; }
NFAIL=0
P_LAUNCH=src/BE/CoreAndSkill.Api/Properties/launchSettings.json
P_APPSET=src/BE/CoreAndSkill.Api/appsettings.Development.json
P_ENVDEV=src/FE/src/environments/environment.development.ts
P_ANGULAR=src/FE/angular.json

DIR=""
dung() { # bản chép mới của bốn tệp nguồn
  DIR="$(mktemp -d)" || { echo "ABORT — mktemp"; exit 2; }
  local f
  for f in "$P_LAUNCH" "$P_APPSET" "$P_ENVDEV" "$P_ANGULAR"; do
    [ -f "$REPO/$f" ] || { echo "ABORT — không thấy $f"; exit 2; }
    mkdir -p "$DIR/$(dirname "$f")"; cp "$REPO/$f" "$DIR/$f"
  done
}
# sua <tệp> <sed-expr> — sửa bản chép và bắt buộc có thay đổi thật (canary không được sửa hụt)
sua() {
  local before after
  before="$(cksum < "$DIR/$1")"; sed -i "$2" "$DIR/$1"; after="$(cksum < "$DIR/$1")"
  [ "$before" != "$after" ] || { echo "ABORT — sed '$2' không đổi gì ở $1 (canary hỏng)"; exit 2; }
}
# ca <tên> <xanh|do> [chuỗi phải có trong output; '!chuỗi' = không được có]...
ca() {
  local name="$1" expect="$2" out rc actual s thieu=""; shift 2
  out="$(DEV_CFG_ROOT="$DIR" bash "$GATE" 2>&1)"; rc=$?
  rm -rf "$DIR"
  case "$rc" in 0) actual=xanh ;; 1) actual=do ;; *) actual="loi-$rc" ;; esac
  for s in "$@"; do
    case "$s" in
      '!'*) printf '%s\n' "$out" | grep -qF -- "${s#!}" && thieu="$thieu  KHÔNG được có: ${s#!}"$'\n' ;;
      *)    printf '%s\n' "$out" | grep -qF -- "$s" || thieu="$thieu  thiếu: $s"$'\n' ;;
    esac
  done
  if [ "$actual" = "$expect" ] && [ -z "$thieu" ]; then
    printf '  \033[32mOK\033[0m    %s (%s)\n' "$name" "$actual"
  else
    printf '  \033[31mFAIL\033[0m  %s — %s, kỳ vọng %s\n%s%s\n' "$name" "$actual" "$expect" "$thieu" "$out"
    NFAIL=$((NFAIL + 1))
  fi
}

printf '\n== canary dev-config-check — bản chép ở thư mục tạm ==\n'

# Cặp đỏ: mọi ca "do" bên dưới — cùng bản chép, khác đúng một chỗ.
dung
ca "bản chép nguyên vẹn -> PASS, có in dòng OK của cả ba cặp" xanh "dev-config-check.sh PASS" "apiBaseUrl của FE dev" "angular.json serve bật ssl" "tenantCode FE dev" "!FAIL"

# ---------------------------------------------------------------- cặp 1: cổng API
# Cặp xanh: ca "nguyên vẹn" ở đầu.
dung; sua "$P_ENVDEV" 's#localhost:7100#localhost:49542#'
ca "FE đổi apiBaseUrl sang 49542, BE vẫn 7100 -> FAIL, nêu tệp FE + giá trị" do "environment.development.ts" "https://localhost:49542" "dev-config-check.sh FAIL"
dung; sua "$P_LAUNCH" 's#localhost:7100#localhost:49542#'
ca "BE đổi applicationUrl sang 49542, FE vẫn 7100 -> FAIL, nêu launchSettings.json" do "launchSettings.json" "49542"
# Cặp đỏ: hai ca trên — ở đây đổi ĐỒNG BỘ nhưng khỏi ghim vẫn đỏ (ghim nằm trong cổng, không chỉ so hai chỗ).
dung; sua "$P_LAUNCH" 's#localhost:7100#localhost:49542#'; sua "$P_ENVDEV" 's#localhost:7100#localhost:49542#'
ca "đổi ĐỒNG BỘ cả hai sang 49542 -> FAIL vì khỏi giá trị ghim 7100" do "ghim là 'https://localhost:7100'" "!không có trong applicationUrl"
dung; sua "$P_LAUNCH" 's#"https://localhost:7100"#"https://localhost:7100;http://localhost:5100"#'
ca "applicationUrl thêm http://localhost:5100 mà vẫn chứa 7100 -> PASS (FE chỉ cần origin nằm trong danh sách)" xanh "dev-config-check.sh PASS"

# ---------------------------------------------------------------- cặp 2: origin FE
# ADR-0129: BE dev nhận mọi cổng https://localhost, cổng không đối chiếu AllowedOrigins nữa — ca xanh này canh
# cho cổng không bị thêm lại phép đó (danh sách thiếu mục 4200 hay còn mục http:// đều không làm đỏ).
dung; sua "$P_APPSET" 's#"https://localhost:4200", ##'; sua "$P_APPSET" 's#https://localhost:7100#http://localhost:7100#'
ca "AllowedOrigins không còn mục 4200 và còn mục http:// -> PASS (cổng không đọc AllowedOrigins)" xanh "dev-config-check.sh PASS" "!AllowedOrigins"
dung; sua "$P_ANGULAR" 's#"ssl": true,#"ssl": true, "port": 4400,#'
ca "angular.json serve thêm port 4400 -> FAIL, nêu angular.json + 4400" do "angular.json" "serve.port = 4400"
dung; sua "$P_ANGULAR" 's#"ssl": true,#"ssl": false,#'
ca "angular.json serve tắt ssl -> FAIL (origin không còn là https)" do "angular.json" "ssl"
dung; sua "$P_ANGULAR" 's#"buildTarget": "fe:build:development"#"buildTarget": "fe:build:development", "port": 4500#'
ca "cấu hình development của serve ghi đè port -> FAIL" do "ghi đè port/ssl"
# Cặp xanh: ca "nguyên vẹn" — cấu hình e2e (port 4300, ssl false) nằm ngoài khối options nên KHÔNG làm đỏ.
dung
ca "cấu hình e2e giữ port 4300/ssl false (khối configurations) -> PASS" xanh "angular.json serve dùng cổng 4200"

# ---------------------------------------------------------------- cặp 3: tenant
dung; sua "$P_ENVDEV" "s#tenantCode: 'DEMO'#tenantCode: 'KHAC'#"
ca "FE tenantCode=KHAC, BE FirstTenantCode=DEMO -> FAIL, nêu hai giá trị" do "'KHAC'" "'DEMO'" "FirstTenantCode"
dung; sua "$P_ENVDEV" "s#tenantCode: 'DEMO'#tenantCode: 'KHAC'#"; sua "$P_APPSET" 's#"FirstTenantCode": "DEMO"#"FirstTenantCode": "KHAC"#'
ca "đổi ĐỒNG BỘ tenant cả hai sang KHAC -> PASS" xanh "tenantCode FE dev = FirstTenantCode BE = KHAC"

# ---------------------------------------------------------------- đầu vào thiếu
dung; rm -f "$DIR/$P_APPSET"
ca "thiếu appsettings.Development.json -> thoát 2, không xanh rỗng" "loi-2" "ABORT"

printf '\n'
if [ "$NFAIL" -eq 0 ]; then printf '\033[32m✔ dev-config-check.test.sh PASS — mọi ca canary đúng kỳ vọng\033[0m\n'; exit 0; fi
printf '\033[31m✘ dev-config-check.test.sh FAIL — %s ca sai kỳ vọng\033[0m\n' "$NFAIL"; exit 1
