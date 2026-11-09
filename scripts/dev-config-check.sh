#!/usr/bin/env bash
# scripts/dev-config-check.sh — cổng Q41: cụm giá trị cấu hình dev của BE và FE phải khớp nhau và khớp giá trị ĐÃ GHIM.
# Ba cặp:
#   1. cổng API : launchSettings.json `applicationUrl` <-> environment.development.ts `apiBaseUrl` (ghim 7100)
#   2. origin FE: angular.json serve (ssl, cổng) <-> appsettings.Development.json `Core:Auth:AllowedOrigins` (ghim 4200)
#   3. tenant   : environment.development.ts `tenantCode` <-> appsettings.Development.json `Core:Bootstrap:FirstTenantCode`
# Đổi một chỗ mà không đổi chỗ kia, hay đổi cổng khỏi giá trị ghim, đều đỏ — thông điệp nêu tệp + giá trị lệch.
# Đổi cổng có chủ ý: sửa PIN_* dưới đây CÙNG LÚC với các tệp nguồn.
#
# Biến vào: DEV_CFG_ROOT (gốc chứa src/BE, src/FE; mặc định gốc repo) — canary trỏ vào bản chép ở thư mục tạm.
# Thoát 0 = xanh · 1 = có lệch · 2 = không chạy được (thiếu tệp đầu vào).
# Chạy: bash scripts/dev-config-check.sh      (Git Bash + Linux; không grep -P)
set -uo pipefail
export LC_ALL=C

PIN_API_PORT=7100
PIN_FE_PORT=4200
PIN_API_ORIGIN="https://localhost:$PIN_API_PORT"
PIN_FE_ORIGIN="https://localhost:$PIN_FE_PORT"

ROOT="${DEV_CFG_ROOT:-$(cd "$(dirname "$0")/.." && pwd)}"
LAUNCH="$ROOT/src/BE/CoreAndSkill.Api/Properties/launchSettings.json"
APPSET="$ROOT/src/BE/CoreAndSkill.Api/appsettings.Development.json"
ENVDEV="$ROOT/src/FE/src/environments/environment.development.ts"
ANGULAR="$ROOT/src/FE/angular.json"
for f in "$LAUNCH" "$APPSET" "$ENVDEV" "$ANGULAR"; do
  [ -f "$f" ] || { echo "ABORT — không thấy ${f#"$ROOT"/}"; exit 2; }
done
rel() { printf '%s' "${1#"$ROOT"/}"; }

NFAIL=0
ok()  { printf '  OK    %s\n' "$1"; }
bad() { printf '  FAIL  %s\n' "$1"; NFAIL=$((NFAIL + 1)); }
# gop <tệp> — cả tệp thành một dòng (cho phép mảng JSON xuống dòng)
gop() { tr '\n\r\t' '   ' < "$1"; }

echo "== dev-config-check (Q41) =="

# ---------------------------------------------------------------- 1. cổng API
APP_URL="$(gop "$LAUNCH" | sed -n 's/.*"applicationUrl"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')"
API_BASE="$(sed -n "s/.*apiBaseUrl:[[:space:]]*'\([^']*\)'.*/\1/p" "$ENVDEV" | head -n1)"
[ -n "$APP_URL" ]  || bad "$(rel "$LAUNCH"): không đọc được applicationUrl"
[ -n "$API_BASE" ] || bad "$(rel "$ENVDEV"): không đọc được apiBaseUrl"
if [ -n "$APP_URL" ] && [ -n "$API_BASE" ]; then
  API_ORIGIN="$(printf '%s' "$API_BASE" | sed -n 's#^\(https\{0,1\}://[^/]*\).*#\1#p')"
  if [ "$API_ORIGIN" = "$PIN_API_ORIGIN" ]; then ok "apiBaseUrl của FE dev = $PIN_API_ORIGIN (đã ghim)"
  else bad "$(rel "$ENVDEV"): apiBaseUrl có origin '$API_ORIGIN', ghim là '$PIN_API_ORIGIN'"; fi
  case ";$(printf '%s' "$APP_URL" | tr -d ' ');" in
    *";$PIN_API_ORIGIN;"*) ok "applicationUrl của BE chứa $PIN_API_ORIGIN (đã ghim)" ;;
    *) bad "$(rel "$LAUNCH"): applicationUrl '$APP_URL' không chứa '$PIN_API_ORIGIN' (ghim)" ;;
  esac
  case ";$(printf '%s' "$APP_URL" | tr -d ' ');" in
    *";$API_ORIGIN;"*) ok "origin của apiBaseUrl nằm trong applicationUrl" ;;
    *) bad "$(rel "$ENVDEV") apiBaseUrl origin '$API_ORIGIN' không có trong applicationUrl '$APP_URL' của $(rel "$LAUNCH")" ;;
  esac
fi

# ---------------------------------------------------------------- 2. origin FE
# Khối "serve" của angular.json: từ `"serve": {` đến khối anh em kế tiếp.
SERVE="$(gop "$ANGULAR" | sed -n 's/.*"serve"[[:space:]]*:[[:space:]]*{\(.*\)/\1/p' | sed "s/\"configurations\".*//")"
[ -n "$SERVE" ] || bad "$(rel "$ANGULAR"): không thấy khối serve"
# Cấu hình `development` (defaultConfiguration của serve) không được ghi đè port/ssl của khối options.
SERVE_DEV="$(gop "$ANGULAR" | sed -n 's/.*"serve"[[:space:]]*:[[:space:]]*{\(.*\)/\1/p' | sed -n 's/.*"development"[[:space:]]*:[[:space:]]*{\([^}]*\)}.*/\1/p')"
if printf '%s' "$SERVE_DEV" | grep -q '"port"\|"ssl"'; then
  bad "$(rel "$ANGULAR"): serve.configurations.development ghi đè port/ssl — cổng không còn đọc được từ options"
fi
SERVE_PORT="$(printf '%s' "$SERVE" | sed -n 's/.*"port"[[:space:]]*:[[:space:]]*\([0-9]*\).*/\1/p')"
[ -n "$SERVE_PORT" ] || SERVE_PORT=4200   # mặc định của dev-server khi không khai "port"
if [ "$SERVE_PORT" = "$PIN_FE_PORT" ]; then ok "angular.json serve dùng cổng $PIN_FE_PORT (đã ghim)"
else bad "$(rel "$ANGULAR"): serve.port = $SERVE_PORT, ghim là $PIN_FE_PORT"; fi
if printf '%s' "$SERVE" | grep -q '"ssl"[[:space:]]*:[[:space:]]*true'; then ok "angular.json serve bật ssl (origin là https)"
else bad "$(rel "$ANGULAR"): serve không có \"ssl\": true — origin FE không còn là $PIN_FE_ORIGIN"; fi

ORIGINS="$(gop "$APPSET" | sed -n 's/.*"AllowedOrigins"[[:space:]]*:[[:space:]]*\[\([^]]*\)\].*/\1/p')"
[ -n "$ORIGINS" ] || bad "$(rel "$APPSET"): không đọc được Core:Auth:AllowedOrigins"
case ",$(printf '%s' "$ORIGINS" | tr -d ' "')," in
  *",$PIN_FE_ORIGIN,"*) ok "AllowedOrigins chứa $PIN_FE_ORIGIN" ;;
  *) bad "$(rel "$APPSET"): AllowedOrigins [$ORIGINS] không chứa '$PIN_FE_ORIGIN' (origin của dev-server, $(rel "$ANGULAR"))" ;;
esac

# ---------------------------------------------------------------- 3. tenant dev
FE_TENANT="$(sed -n "s/.*tenantCode:[[:space:]]*'\([^']*\)'.*/\1/p" "$ENVDEV" | head -n1)"
BE_TENANT="$(gop "$APPSET" | sed -n 's/.*"FirstTenantCode"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')"
if [ -z "$FE_TENANT" ] || [ -z "$BE_TENANT" ]; then
  bad "không đọc được tenantCode ($(rel "$ENVDEV"): '$FE_TENANT') hoặc FirstTenantCode ($(rel "$APPSET"): '$BE_TENANT')"
elif [ "$FE_TENANT" = "$BE_TENANT" ]; then ok "tenantCode FE dev = FirstTenantCode BE = $FE_TENANT"
else bad "$(rel "$ENVDEV") tenantCode='$FE_TENANT' ≠ $(rel "$APPSET") Core:Bootstrap:FirstTenantCode='$BE_TENANT'"; fi

if [ "$NFAIL" -eq 0 ]; then echo "dev-config-check.sh PASS"; exit 0; fi
echo "dev-config-check.sh FAIL ($NFAIL lệch)"; exit 1
