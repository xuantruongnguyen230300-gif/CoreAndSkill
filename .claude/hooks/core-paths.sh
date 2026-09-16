#!/usr/bin/env bash
# core-paths.sh — đọc khối "đường dẫn chạm Core" từ tài liệu chủ.
#
# In ra mỗi dòng một đường dẫn tính từ gốc repo. Script này KHÔNG giữ danh sách
# nào: nguồn duy nhất là khối nằm giữa hai mốc `core-paths:begin` và
# `core-paths:end` trong docs/kien-truc-core-module.md. Hook, agent và skill chỉ
# trỏ tới khối đó. Một danh sách thứ hai ở đây sẽ lệch khỏi khối — người sửa chỉ
# sửa một chỗ, và không có gì báo (.claude/CLAUDE.md §5).
#
# Hợp đồng định dạng mà script đọc:
#   - đúng MỘT mốc begin và MỘT mốc end, begin đứng trước;
#   - giữa hai mốc là đúng MỘT khối rào ``` KHÔNG gắn ngôn ngữ, ngoài nó chỉ có
#     dòng trống;
#   - trong khối: mỗi dòng một đường dẫn tính từ gốc repo; thư mục kết thúc bằng
#     "/"; dòng trống và dòng bắt đầu "#" bị bỏ qua.
#
# Mã thoát — lỗi LUÔN kèm một dòng lý do ở stderr. Một parser im lặng trả về
# danh sách rỗng là cách hook "chạm Core" chết mà không ai biết.
#   0  đọc được, stdout có ít nhất một đường dẫn
#   3  không thấy tài liệu chủ
#   4  mốc begin/end thiếu, lặp, hoặc sai thứ tự
#   5  khối rào thiếu, gắn ngôn ngữ, chưa đóng, có hơn một khối, hoặc có chữ ngoài khối
#   6  khối không có đường dẫn nào
#   7  có dòng không phải đường dẫn hợp lệ tính từ gốc repo
#
# Dùng (từ bất kỳ đâu):  bash .claude/hooks/core-paths.sh [tài-liệu-tính-từ-gốc-repo]
#
# Cùng khuôn hai hook còn lại: không jq, không grep -P, tự ép locale. awk chạy ở
# LC_ALL=C — mốc và ký tự cần xét đều là ASCII, và chế độ byte không bao giờ bỏ
# cuộc giữa chừng vì một chuỗi UTF-8 hỏng trong phần văn xuôi của tài liệu.

set -uo pipefail
export LC_ALL=en_US.UTF-8
cd "$(dirname "$0")/../.." || exit 3

DOC="${1:-docs/kien-truc-core-module.md}"
if [ ! -f "$DOC" ]; then
  printf 'core-paths: không thấy %s\n' "$DOC" >&2
  exit 3
fi

LC_ALL=C awk '
  function fail(c, m) { if (code == 0) { code = c; msg = m } }
  BEGIN { st = 0; nb = 0; ne = 0; nf = 0; open = 0; n = 0; code = 0; msg = "" }
  { sub(/\r$/, "") }
  /^[ \t]*<!--[ \t]*core-paths:begin[ \t]*-->[ \t]*$/ {
    nb++
    if (nb > 1)  fail(4, "mốc core-paths:begin xuất hiện hơn một lần (dòng " FNR ")")
    if (st == 2) fail(4, "mốc core-paths:begin nằm sau mốc end (dòng " FNR ")")
    st = 1; next
  }
  /^[ \t]*<!--[ \t]*core-paths:end[ \t]*-->[ \t]*$/ {
    ne++
    if (ne > 1)  fail(4, "mốc core-paths:end xuất hiện hơn một lần (dòng " FNR ")")
    if (st != 1) fail(4, "mốc core-paths:end không có mốc begin đứng trước (dòng " FNR ")")
    if (open)    fail(5, "khối rào chưa đóng trước mốc end (dòng " FNR ")")
    st = 2; next
  }
  st != 1 { next }
  /^[ \t]*```/ {
    if (!open) {
      nf++
      if (nf > 1) fail(5, "có hơn một khối rào giữa hai mốc (dòng " FNR ")")
      lang = $0; sub(/^[ \t]*`+/, "", lang); gsub(/[ \t]/, "", lang)
      if (lang != "") fail(5, "khối rào gắn ngôn ngữ \"" lang "\" — hợp đồng là khối không gắn ngôn ngữ (dòng " FNR ")")
      open = 1
    } else {
      open = 0
    }
    next
  }
  {
    line = $0; gsub(/^[ \t]+|[ \t]+$/, "", line)
    if (!open) {
      if (line != "") fail(5, "có chữ ngoài khối rào giữa hai mốc (dòng " FNR "): " line)
      next
    }
    if (line == "" || line ~ /^#/) next
    if (line ~ /[ \t\\*?<>|:"`]/ || line ~ /^\// || line ~ /^\.\// || line ~ /(^|\/)\.\.(\/|$)/ || line ~ /\/\//) {
      fail(7, "dòng " FNR " không phải đường dẫn hợp lệ tính từ gốc repo: " line)
      next
    }
    out[++n] = line
  }
  END {
    if (code == 0 && nb == 0) fail(4, "không thấy mốc <!-- core-paths:begin -->")
    if (code == 0 && ne == 0) fail(4, "không thấy mốc <!-- core-paths:end -->")
    if (code == 0 && nf == 0) fail(5, "không có khối rào nào giữa hai mốc")
    if (code == 0 && n == 0)  fail(6, "khối không có đường dẫn nào")
    if (code != 0) { printf "core-paths: %s: %s\n", FILENAME, msg > "/dev/stderr"; exit code }
    for (i = 1; i <= n; i++) print out[i]
  }
' "$DOC"
