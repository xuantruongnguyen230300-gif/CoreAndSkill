#!/usr/bin/env bash
# scripts/tests/_fixture-that.sh — hàm dùng chung cho canary chạy trên BẢN SAO của code thật
# (fe-gate-f5/f8/f11.test.sh). Không phải một test: không có ca nào, chỉ được `source`.
#
# Chép src/FE/src và các thư mục tệp dịch public/i18n* của src/FE ra thư mục tạm, để canary cắm
# vi phạm vào bản sao rồi gọi THẲNG scripts/fe-gate.sh qua biến FE — không chạm src/FE thật.
#
# Biến vào: REPO (gốc repo). Biến ra: DIR (thư mục tạm của lần dựng gần nhất).

REAL_FE="$REPO/src/FE"

# dung_ban_sao — DIR = một bản sao mới của src/FE/src + public/i18n*
dung_ban_sao() {
  DIR="$(mktemp -d)" || { echo "ABORT — mktemp -d thất bại"; exit 2; }
  cp -r "$REAL_FE/src" "$DIR/src"
  mkdir -p "$DIR/public"
  local tm
  for tm in "$REAL_FE"/public/i18n*; do
    [ -d "$tm" ] && cp -r "$tm" "$DIR/public/"
  done
  cp "$REAL_FE/eslint.boundaries.cjs" "$DIR/"
}

# cam <đường dẫn tương đối dưới $DIR> <nội dung> — ghi đè hoặc tạo mới một tệp trong bản sao
cam() {
  mkdir -p "$(dirname "$DIR/$1")"
  printf '%s\n' "$2" > "$DIR/$1"
}

# khoi_section <mã F> <output của gate> — in riêng khối output của một section
khoi_section() {
  printf '%s\n' "$2" | sed 's/\x1b\[[0-9;]*m//g' | awk -v m="== $1 - " 'index($0, m) {f=1; print; next}
    /^== / || /fe-gate[.]sh (PASS|FAIL)/ {f=0} f'
}

# cham <mã F> <tên ca> <kỳ vọng: do|xanh> — chạy gate trên $DIR, chấm theo khối của section, dọn $DIR
cham() {
  local ma="$1" name="$2" expect="$3" out sec actual
  out="$(FE="$DIR" bash "$REPO/scripts/fe-gate.sh" 2>&1)"
  rm -rf "$DIR"
  sec="$(khoi_section "$ma" "$out")"
  if printf '%s\n' "$sec" | grep -q 'FAIL'; then
    actual=do
  elif printf '%s\n' "$sec" | grep -q 'OK'; then
    actual=xanh
  else
    actual=khong-thay-section
  fi
  if [ "$actual" = "$expect" ]; then
    printf '  \033[32mOK\033[0m    %-78s %s=%s (kỳ vọng %s)\n' "$name" "$ma" "$actual" "$expect"
  else
    printf '  \033[31mFAIL\033[0m  %-78s %s=%s (kỳ vọng %s)\n' "$name" "$ma" "$actual" "$expect"
    printf '%s\n' "$sec"
    NFAIL=$((NFAIL + 1))
  fi
}

# cham_dich_danh <mã F> <tên ca> <do|xanh> [chuỗi]... — như `cham`, cộng kiểm khối output của section
# nêu ĐÍCH DANH từng chuỗi truyền vào (so theo byte, `grep -F`). Chuỗi mở đầu bằng '!' thì KHÔNG được
# có trong khối. Lý do tồn tại: "có FAIL ở đâu đó trong section" không phân biệt cổng đỏ đúng chỗ với
# cổng đỏ vì một chốt khác; và "có OK" không phân biệt cổng đã dò với cổng xanh rỗng — ca xanh truyền
# chuỗi chứng minh cổng đã đọc được thứ nó phải dò.
cham_dich_danh() {
  local ma="$1" name="$2" expect="$3"; shift 3
  local out sec actual thieu="" s
  out="$(FE="$DIR" bash "$REPO/scripts/fe-gate.sh" 2>&1)"
  rm -rf "$DIR"
  sec="$(khoi_section "$ma" "$out")"
  if printf '%s\n' "$sec" | grep -q 'FAIL'; then
    actual=do
  elif printf '%s\n' "$sec" | grep -q 'OK'; then
    actual=xanh
  else
    actual=khong-thay-section
  fi
  for s in "$@"; do
    case "$s" in
      '!'*) printf '%s\n' "$sec" | LC_ALL=C grep -qF -- "${s#!}" \
              && thieu="$thieu    KHÔNG được có trong khối: ${s#!}"$'\n' ;;
      *)    printf '%s\n' "$sec" | LC_ALL=C grep -qF -- "$s" \
              || thieu="$thieu    thiếu trong khối: $s"$'\n' ;;
    esac
  done
  if [ "$actual" = "$expect" ] && [ -z "$thieu" ]; then
    printf '  \033[32mOK\033[0m    %-78s %s=%s (kỳ vọng %s)\n' "$name" "$ma" "$actual" "$expect"
  else
    printf '  \033[31mFAIL\033[0m  %-78s %s=%s (kỳ vọng %s)\n' "$name" "$ma" "$actual" "$expect"
    [ -n "$thieu" ] && printf '%s' "$thieu"
    printf '%s\n' "$sec"
    NFAIL=$((NFAIL + 1))
  fi
}

# doi <rel dưới $DIR> <chương trình perl chạy trên CẢ tệp> [dấu] — áp một đột biến lên bản sao rồi
# KIỂM nó đã vào: tệp phải đổi, và nếu có [dấu] thì dấu phải có trong tệp sau đột biến. Một sed/perl
# không khớp gì để lại bản sao nguyên vẹn — ca xanh trên bản sao nguyên vẹn là ca xanh rỗng, ca đỏ
# thì đỏ vì lý do khác. Không vào: ghi FAIL, đặt HONG=1 để `cham_dot_bien` bỏ chấm ca đó.
# Giá trị cắm vào nên đi qua biến môi trường (X, Y…) để khỏi thoát nháy trong chương trình perl.
HONG=0
doi() {
  local f="$DIR/$1"
  cp "$f" "$f.truoc-dot-bien" || { HONG=1; NFAIL=$((NFAIL + 1)); return; }
  LC_ALL=C perl -0777 -pe "$2" "$f.truoc-dot-bien" > "$f"
  if cmp -s "$f" "$f.truoc-dot-bien" || { [ -n "${3:-}" ] && ! LC_ALL=C grep -qF -- "$3" "$f"; }; then
    printf '  \033[31mFAIL\033[0m  đột biến KHÔNG vào được bản sao %s (dấu: "%s")\n' "$1" "${3:-}"
    NFAIL=$((NFAIL + 1)); HONG=1
  fi
  rm -f "$f.truoc-dot-bien"
}

# cham_dot_bien <mã F> <tên ca> <do|xanh> [chuỗi]... — `cham_dich_danh`, trừ khi đột biến của ca đã
# không vào được bản sao (HONG=1): khi đó dọn $DIR và báo ca bị bỏ chấm.
cham_dot_bien() {
  if [ "$HONG" -eq 1 ]; then
    rm -rf "$DIR"; HONG=0
    printf '  \033[31mFAIL\033[0m  %-78s (bỏ chấm — đột biến không vào)\n' "$2"
    return
  fi
  cham_dich_danh "$@"
}

# ket_thuc <tên tệp test> — in tổng kết và thoát theo NFAIL
ket_thuc() {
  printf '\n'
  if [ "$NFAIL" -eq 0 ]; then
    printf '\033[32m✔ %s PASS — mọi ca canary đúng kỳ vọng\033[0m\n' "$1"
    exit 0
  fi
  printf '\033[31m✘ %s FAIL — %s ca sai kỳ vọng, xem chi tiết ở trên\033[0m\n' "$1" "$NFAIL"
  exit 1
}
