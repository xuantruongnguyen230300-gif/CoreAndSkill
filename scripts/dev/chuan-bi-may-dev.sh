#!/usr/bin/env bash
# scripts/dev/chuan-bi-may-dev.sh — chuẩn bị máy dev, KHÔNG chạm database.
# Hướng dẫn chủ: docs/quy-uoc/chay-may-dev.md (thứ tự các bước, bảng triệu chứng). Chạy trong Git Bash.
#
#   bash scripts/dev/chuan-bi-may-dev.sh --kiem-tra   # chỉ đọc và in — không tạo, không cài gì
#   bash scripts/dev/chuan-bi-may-dev.sh              # kiểm như trên, rồi làm phần còn thiếu:
#        chứng chỉ HTTPS dev (docs/wiki-core/fe/17-phuc-vu-va-trien-khai.md §4.2), thư mục kho tệp
#        (docs/database/script-runbook.md §6 bước 4), npm ci trong src/FE
#
# Không gọi psql, không gọi dotnet run: phần database là scripts/dev/ap-db-dev.sh — NGƯỜI DÙNG chạy.
# Giá trị không chép vào đây: đọc từ tệp nguồn lúc chạy (global.json, .nvmrc, docker-compose.yml,
# appsettings.Development.json) — đổi tệp nguồn thì script theo.
# Thoát 0 = đủ · 1 = còn THIEU · 2 = không chạy được.
set -uo pipefail

KIEM_TRA=0
case "${1:-}" in
  --kiem-tra) KIEM_TRA=1 ;;
  "") ;;
  *) echo "Dùng: bash scripts/dev/chuan-bi-may-dev.sh [--kiem-tra]"; exit 2 ;;
esac

ROOT="${DEV_ROOT:-$(cd "$(dirname "$0")/../.." && pwd)}"
GLOBAL_JSON="$ROOT/src/BE/global.json"
NVMRC="$ROOT/src/FE/.nvmrc"
COMPOSE="$ROOT/docker-compose.yml"
APPSET="$ROOT/src/BE/CoreAndSkill.Api/appsettings.Development.json"
CERT_DIR="$ROOT/src/FE/.cert"
for f in "$GLOBAL_JSON" "$NVMRC" "$COMPOSE" "$APPSET"; do
  [ -f "$f" ] || { echo "ABORT — không thấy ${f#"$ROOT"/}"; exit 2; }
done

NTHIEU=0
ok()    { printf '  OK     %s\n' "$1"; }
canh()  { printf '  CANH   %s\n' "$1"; }
thieu() { printf '  THIEU  %s\n' "$1"; NTHIEU=$((NTHIEU + 1)); }
lam()   { printf '  LAM    %s\n' "$1"; }
gop()   { tr '\n\r\t' '   ' < "$1"; }

echo "== Máy dev — điều kiện cần =="

# ---- Git Bash: mọi khối lệnh của hướng dẫn viết cho bash
case "$(uname -s)" in
  MINGW*|MSYS*) ok "Git Bash ($(uname -s))" ;;
  *) canh "không phải Git Bash ($(uname -s)) — máy không chạy Windows thì đè Core:File:RootPath (docs/database/script-runbook.md §6)" ;;
esac

# ---- .NET SDK: chạy `dotnet --version` TRONG src/BE để dotnet đọc global.json; thiếu SDK khớp thì lệnh hỏng
SDK_CAN="$(gop "$GLOBAL_JSON" | sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')"
if ! command -v dotnet >/dev/null 2>&1; then
  thieu "dotnet không có trong PATH — cài .NET SDK theo src/BE/global.json ($SDK_CAN)"
elif SDK_CO="$(cd "$ROOT/src/BE" && dotnet --version 2>/dev/null)"; then
  ok ".NET SDK $SDK_CO (src/BE/global.json: $SDK_CAN)"
else
  thieu ".NET SDK: không có bản khớp src/BE/global.json ($SDK_CAN) — chạy 'cd src/BE && dotnet --version' để đọc lỗi"
fi

# ---- Node: .nvmrc là bản CI dùng; engines của package.json là miền npm chấp nhận
NODE_CAN="$(tr -d ' \r\n' < "$NVMRC")"
if ! command -v node >/dev/null 2>&1; then
  thieu "node không có trong PATH — cài Node theo src/FE/.nvmrc ($NODE_CAN)"
else
  NODE_CO="$(node -v 2>/dev/null | tr -d 'v\r')"
  if [ "$NODE_CO" = "$NODE_CAN" ]; then ok "Node $NODE_CO (src/FE/.nvmrc)"
  else canh "Node $NODE_CO khác src/FE/.nvmrc ($NODE_CAN) — CI dùng .nvmrc; đọc khoá engines của src/FE/package.json"; fi
  command -v npm >/dev/null 2>&1 && ok "npm $(npm -v 2>/dev/null | tr -d '\r')" || thieu "npm không có trong PATH"
fi

# ---- PostgreSQL: bản theo docker-compose.yml. Chỉ kiểm psql có trong PATH; KHÔNG nối vào cụm nào
PG_CAN="$(sed -n 's/^[[:space:]]*image:[[:space:]]*postgres:\([0-9][0-9]*\).*/\1/p' "$COMPOSE" | head -n1)"
if command -v psql >/dev/null 2>&1; then
  PG_CO="$(psql --version 2>/dev/null | sed -n 's/.* \([0-9][0-9]*\)\.[0-9].*/\1/p' | head -n1)"
  if [ -n "$PG_CAN" ] && [ "$PG_CO" = "$PG_CAN" ]; then ok "psql bản $PG_CO (docker-compose.yml: postgres:$PG_CAN)"
  else canh "psql bản '${PG_CO:-?}', docker-compose.yml ghim postgres:${PG_CAN:-?}"; fi
else
  canh "psql không có trong PATH — cần cho scripts/dev/ap-db-dev.sh; cách thêm: docs/wiki-core/be/ly-do/script-runbook.md §6 (dùng DBeaver/pgAdmin thì bỏ qua)"
fi

# ---- Chứng chỉ HTTPS dev: tin cậy trên máy + hai tệp PEM cho ng serve
CERT_TIN=0
if command -v dotnet >/dev/null 2>&1 && dotnet dev-certs https --check --trust >/dev/null 2>&1; then
  CERT_TIN=1; ok "chứng chỉ HTTPS dev của .NET có và được máy tin cậy"
else
  thieu "chứng chỉ HTTPS dev chưa có hoặc chưa được tin cậy (17-phuc-vu-va-trien-khai.md §4.2)"
fi
if [ -f "$CERT_DIR/localhost.pem" ] && [ -f "$CERT_DIR/localhost.key" ]; then
  ok "src/FE/.cert/localhost.pem và localhost.key có"
else
  thieu "src/FE/.cert/localhost.pem hoặc localhost.key chưa có — ng serve không lên HTTPS"
fi

# ---- Thư mục kho tệp: đường dẫn ở Core:File:RootPath; Core không tự tạo
ROOT_PATH="$(gop "$APPSET" | sed -n 's/.*"File"[[:space:]]*:[[:space:]]*{[^}]*"RootPath"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')"
if [ -z "$ROOT_PATH" ]; then
  thieu "không đọc được Core:File:RootPath trong appsettings.Development.json"
elif [ -d "$ROOT_PATH" ]; then
  ok "thư mục kho tệp có: $ROOT_PATH"
else
  thieu "thư mục kho tệp chưa có: $ROOT_PATH — thiếu thì BE không khởi động"
fi

# ---- Thư viện FE
if [ -d "$ROOT/src/FE/node_modules" ]; then ok "src/FE/node_modules có (npm ci đã chạy ít nhất một lần)"
else thieu "src/FE/node_modules chưa có — npm ci"; fi

# ---- Cụm cổng / origin / mã đơn vị dev (luật T14) — chỉ đọc bốn tệp cấu hình
if [ -f "$ROOT/scripts/dev-config-check.sh" ]; then
  if bash "$ROOT/scripts/dev-config-check.sh" >/dev/null 2>&1; then ok "cụm cấu hình dev BE ↔ FE khớp (scripts/dev-config-check.sh)"
  else thieu "cụm cấu hình dev BE ↔ FE lệch — chạy 'bash scripts/dev-config-check.sh' để đọc chỗ lệch"; fi
fi

if [ "$KIEM_TRA" -eq 1 ]; then
  echo
  if [ "$NTHIEU" -eq 0 ]; then echo "KIEM TRA: đủ."; exit 0; fi
  echo "KIEM TRA: $NTHIEU mục THIEU — chạy lại không có --kiem-tra để làm phần script làm được."; exit 1
fi

# ================================================================ làm phần còn thiếu
echo
echo "== Làm phần còn thiếu =="
command -v dotnet >/dev/null 2>&1 || { echo "DUNG — chưa có dotnet, cài trước rồi chạy lại."; exit 1; }
cd "$ROOT" || exit 2

# Chứng chỉ — đúng hai lệnh của 17-phuc-vu-va-trien-khai.md §4.2, chạy từ gốc repo.
if [ "$CERT_TIN" -eq 0 ]; then
  lam "dotnet dev-certs https --trust (Windows hỏi xác nhận — bấm Yes)"
  dotnet dev-certs https --trust || { echo "DUNG — tạo/tin cậy chứng chỉ hỏng."; exit 1; }
fi
if [ ! -f src/FE/.cert/localhost.pem ] || [ ! -f src/FE/.cert/localhost.key ]; then
  lam "xuất chứng chỉ PEM vào src/FE/.cert/ (thư mục bị .gitignore loại — không commit)"
  mkdir -p src/FE/.cert
  dotnet dev-certs https --export-path "src/FE/.cert/localhost.pem" --format Pem --no-password \
    || { echo "DUNG — xuất chứng chỉ hỏng."; exit 1; }
fi

if [ -n "$ROOT_PATH" ] && [ ! -d "$ROOT_PATH" ]; then
  lam "tạo thư mục kho tệp $ROOT_PATH"
  mkdir -p "$ROOT_PATH" || { echo "DUNG — không tạo được $ROOT_PATH."; exit 1; }
fi

command -v npm >/dev/null 2>&1 || { echo "DUNG — chưa có npm."; exit 1; }
lam "npm ci trong src/FE (đọc package-lock.json)"
(cd src/FE && npm ci) || { echo "DUNG — npm ci hỏng."; exit 1; }

echo
echo "Xong phần không chạm database. Tiếp theo — docs/quy-uoc/chay-may-dev.md §2:"
echo "  database: bash scripts/dev/ap-db-dev.sh   (người dùng chạy)"
echo "  BE:       dotnet run --project src/BE/CoreAndSkill.Api"
echo "  FE:       cd src/FE && npm start"
