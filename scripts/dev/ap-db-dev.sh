#!/usr/bin/env bash
# scripts/dev/ap-db-dev.sh — dựng database CỤC BỘ của máy dev: bước 1 và 2 của docs/database/script-runbook.md §6.
#
# 🛑 NGƯỜI DÙNG CHẠY. Agent chỉ viết, không chạy: lệnh chạm database là việc của người dùng.
# 🛑 CHỈ máy dev, Postgres trên chính máy này. Không chạy trên máy chủ dùng chung hay bản cài thật —
#    tệp database/dev/tao-db-local.sql mang mật khẩu công khai trên GitHub (runbook §6).
#
#   bash scripts/dev/ap-db-dev.sh
#
# Làm, theo đúng runbook — không thêm lệnh nào ngoài các lệnh ở đó:
#   1. database/dev/tao-db-local.sql bằng tài khoản quản trị cụm, nối vào database `postgres`
#      — bỏ qua khi database ứng dụng đã có (dựng lại từ đầu: runbook §6, script không xoá gì).
#   2. Câu kiểm "đang ở đâu, là ai", kiểm gói unaccent, rồi áp TỪNG script còn thiếu bằng coreandskill_owner,
#      -v ON_ERROR_STOP=1, dừng ở tệp lỗi đầu tiên (runbook §3.3 bước 2, §3.4).
#   Sau đó so checksum đã ghi trong database với tệp trong repo (runbook §3.5) và in các bước còn lại.
# KHÔNG làm bước 3 (khối cấp quyền, runbook §3.6): khối đó chỉ có một nguồn là runbook — chép vào đây
# thì hai bản sẽ lệch. Script in lệnh và chỗ đọc.
#
# Khác lệnh runbook đúng hai chỗ, cùng lý do Windows:
#   - PGCLIENTENCODING=UTF8: script là UTF-8; psql trên Windows mặc định lấy bảng mã của console.
#   - Bỏ BOM UTF-8 ở đầu script trước khi đưa cho psql (bản chép tạm), như test tích hợp làm — chuỗi
#     `WithoutUtf8Bom` ở src/BE/Tests/CoreAndSkill.Core.IntegrationTests/Support/PostgresFixture.cs.
#     Checksum không đổi: giá trị lưu nằm sẵn trong khối ghi lịch sử của script, phép so ở cuối đọc tệp gốc.
#
# Biến (mặc định = bảng "Biến của §3.3" ở runbook §6): PGHOST, PGADMIN, PGDATABASE.
# Mật khẩu: hỏi một lần cho tài khoản quản trị cụm. Mật khẩu coreandskill_owner đọc từ tao-db-local.sql
# (nguồn duy nhất của giá trị dev công khai đó), không chép vào đây.
# Thoát 0 = xong bước 1–2, checksum khớp · 1 = DUNG (đọc dòng DUNG) · 2 = không chạy được.
set -uo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT" || exit 2
: "${PGHOST:=localhost}" "${PGADMIN:=postgres}" "${PGDATABASE:=coreandskill_dev}"
export PGHOST PGDATABASE
export PGCLIENTENCODING=UTF8
OWNER=coreandskill_owner
TAO_DB=database/dev/tao-db-local.sql

dung() { echo "DUNG — $1"; exit 1; }

command -v psql >/dev/null 2>&1 \
  || { echo "ABORT — psql không có trong PATH: docs/wiki-core/be/ly-do/script-runbook.md §6"; exit 2; }
[ -f "$TAO_DB" ] || { echo "ABORT — không thấy $TAO_DB"; exit 2; }
ls database/scripts/core/*.sql >/dev/null 2>&1 || { echo "ABORT — database/scripts/core/ không có script nào"; exit 2; }
case "$PGHOST" in
  localhost|127.0.0.1|::1) ;;
  *) echo "ABORT — PGHOST='$PGHOST' không phải máy này. Script chỉ cho database cục bộ (runbook §6)."; exit 2 ;;
esac

OWNER_PW="$(sed -n "s/^ALTER ROLE $OWNER PASSWORD '\([^']*\)';.*/\1/p" "$TAO_DB" | tr -d '\r' | head -n1)"
[ -n "$OWNER_PW" ] || { echo "ABORT — không đọc được mật khẩu $OWNER trong $TAO_DB"; exit 2; }

echo "== Database dev: $PGDATABASE trên $PGHOST =="
printf 'Mật khẩu tài khoản quản trị cụm (%s): ' "$PGADMIN"
read -rs ADMIN_PW; echo
admin() { PGPASSWORD="$ADMIN_PW" psql -U "$PGADMIN" -d postgres -v ON_ERROR_STOP=1 "$@"; }
owner() { PGPASSWORD="$OWNER_PW" psql -U "$OWNER" -d "$PGDATABASE" -v ON_ERROR_STOP=1 "$@"; }

# ---------------------------------------------------------------- bước 1
CO_DB="$(admin -At -c "SELECT 1 FROM pg_database WHERE datname = '$PGDATABASE'")" \
  || dung "không nối được vào database postgres bằng $PGADMIN — sai mật khẩu, hay Postgres chưa chạy"
if [ "$CO_DB" = "1" ]; then
  echo ">>> Bước 1: database $PGDATABASE đã có — bỏ qua. Dựng lại từ đầu: runbook §6 'Dựng lại từ đầu khi lỡ tay'."
else
  echo ">>> Bước 1: $TAO_DB"
  admin -f "$TAO_DB" || dung "bước 1 hỏng — đọc lỗi ở trên"
fi

# ---------------------------------------------------------------- bước 2
echo ">>> Câu kiểm 'đang ở đâu, là ai'"
O="$(owner -At -c 'SELECT current_database(), current_user, inet_server_addr()')" \
  || dung "không nối được vào $PGDATABASE bằng $OWNER"
echo "    $O"
case "$O" in
  "$PGDATABASE|$OWNER|"*) ;;
  *) dung "mong đợi '$PGDATABASE|$OWNER|…', nhận '$O'" ;;
esac

[ "$(owner -At -c "SELECT count(*) FROM pg_available_extensions WHERE name = 'unaccent'")" = "1" ] \
  || dung "cụm thiếu gói contrib (unaccent) — runbook §3.3 'Cụm phải có ICU và gói contrib'"

DA_AP="$(owner -At -c "SELECT script_name FROM core.schema_script_history" 2>/dev/null | tr -d '\r' || true)"

TMPD="$(mktemp -d)" || exit 2
trap 'rm -rf "$TMPD"' EXIT
ap_tep() { # ap_tep <tệp> — bản chép đã bỏ BOM, giữ tên tệp để lỗi của psql nêu đúng tên
  local f="$1" c="$TMPD/$(basename "$1")"
  if [ "$(head -c 3 "$f" | od -An -tx1 | tr -d ' \n')" = "efbbbf" ]; then tail -c +4 "$f" > "$c"; else cp "$f" "$c"; fi
  echo ">>> $f"
  owner -f "$c" || dung "tại $f — đọc lỗi, sửa, dựng lại từ đầu (runbook §3.3 bước 2)"
}

SO_AP=0
# Core trước, module sau; trong mỗi bên theo thứ tự tên. Vòng module chỉ chạy cho module CÓ thư mục script.
for f in database/scripts/core/*.sql database/scripts/modules/*/*.sql; do
  [ -f "$f" ] || continue
  if printf '%s\n' "$DA_AP" | grep -qxF "$(basename "$f")"; then continue; fi
  ap_tep "$f"; SO_AP=$((SO_AP + 1))
done
echo ">>> Bước 2: áp $SO_AP script."

# ---------------------------------------------------------------- checksum (runbook §3.5)
echo ">>> So checksum: database ↔ tệp trong repo"
LECH=0; SO_SO=0
while IFS=',' read -r name db_hash; do
  [ -n "$name" ] || continue
  SO_SO=$((SO_SO + 1))
  file="$(find database/scripts -name "$name" | head -n1)"
  if [ -z "$file" ]; then echo "    LECH: $name — database có, repo không có"; LECH=$((LECH + 1)); continue; fi
  offset="$(grep -abm1 '^-- Ghi nhận đã áp' "$file" | cut -d: -f1)"
  real_hash="$(head -c "$offset" "$file" | sha256sum | cut -d' ' -f1)"
  [ "$db_hash" = "$real_hash" ] || { echo "    LECH: $name (DB=$db_hash, file=$real_hash)"; LECH=$((LECH + 1)); }
done < <(owner -At -F',' -c "SELECT script_name, checksum_sha256 FROM core.schema_script_history" | tr -d '\r')
[ "$SO_SO" -gt 0 ] || dung "core.schema_script_history rỗng — không có gì để so, bước 2 chưa ghi lịch sử"
[ "$LECH" -eq 0 ] || dung "$LECH script lệch checksum — runbook §3.2"
echo "    khớp $SO_SO script."

cat <<EOF

Xong bước 1–2. Còn lại — docs/quy-uoc/chay-may-dev.md §2:
  Bước 3 — khối cấp quyền và câu nghiệm thu ở docs/database/script-runbook.md §3.6, chạy bằng $OWNER.
           Trong Git Bash này: export PGHOST=$PGHOST PGDATABASE=$PGDATABASE PGCLIENTENCODING=UTF8
           rồi dán nguyên khối 'psql ... <<'SQL' ... SQL' của §3.6.
  Bước 4 — thư mục kho tệp: bash scripts/dev/chuan-bi-may-dev.sh
  Bước 5 — (cd src/BE && dotnet run --project CoreAndSkill.Api -- core bootstrap)
EOF
