#!/usr/bin/env bash
# lib.sh — hàm dùng chung của các hook. Nạp bằng `. .claude/hooks/lib.sh` SAU khi
# đã cd về gốc repo; không chạy riêng.
#
# Không jq, không grep -P. awk chạy LC_ALL=C: mỗi byte là một ký tự, nên chuỗi
# UTF-8 hỏng hay emoji bốn byte không làm awk bỏ cuộc giữa chừng.

# top_str <khoá>  (stdin: payload JSON)
# In giá trị chuỗi của <khoá> ở TẦNG NGOÀI CÙNG của object, còn nguyên dạng thoát
# JSON. Không có khoá đó ở tầng ngoài, giá trị không phải chuỗi, hay JSON cụt:
# không in gì, thoát 1.
#
# Vì sao không dùng một mẫu sed `.*"agent_type"...` — hai cách hỏng, cả hai đã
# xảy ra thật với payload của harness:
#   1. Payload có mảng `background_tasks` của phiên cha; mỗi việc nền trong đó
#      mang khoá `"agent_type"` RIÊNG, đứng SAU khoá tầng ngoài. Mẫu tham lam bám
#      lần xuất hiện CUỐI, tức agent_type của một việc nền khác.
#   2. Trên Git Bash (MSYS2), sed ở locale UTF-8 không khớp `.` với byte cuối của
#      ký tự UTF-8 bốn byte (emoji như 📐 trong last_assistant_message). `.*` đuôi
#      dừng trước byte đó, và phần còn lại của dòng dính vào sau \1.
# Ở đây: đi qua từng chuỗi JSON trọn vẹn (bỏ qua cặp thoát \x) và đếm độ sâu
# ngoặc — chỉ nhận khoá ở độ sâu 1, không phụ thuộc thứ tự khoá.
top_str() {
  LC_ALL=C awk -v want="$1" '
    function skip_ws(k) { while (k <= n && index(" \t\r\n", substr(s, k, 1)) > 0) k++; return k }
    function str_end(k,   d) { # k = vị trí dấu nháy mở; trả vị trí dấu nháy đóng, 0 nếu cụt
      k++
      while (k <= n) {
        d = substr(s, k, 1)
        if (d == "\\") { k += 2; continue }
        if (d == "\"") return k
        k++
      }
      return 0
    }
    { s = s $0 "\n" }
    END {
      n = length(s); depth = 0; i = 1
      while (i <= n) {
        c = substr(s, i, 1)
        if (c == "\"") {
          j = str_end(i); if (j == 0) exit 1
          k = skip_ws(j + 1)
          if (depth == 1 && substr(s, k, 1) == ":" && substr(s, i + 1, j - i - 1) == want) {
            k = skip_ws(k + 1)
            if (substr(s, k, 1) != "\"") exit 1
            m = str_end(k); if (m == 0) exit 1
            printf "%s", substr(s, k + 1, m - k - 1)
            exit 0
          }
          i = j + 1
          continue
        }
        if (c == "{" || c == "[") depth++
        else if (c == "}" || c == "]") depth--
        i++
      }
      exit 1
    }'
}

# phien_dir  (stdin: payload JSON)
# In thư mục trạng thái RIÊNG của phiên, tính từ gốc repo. Khoá là `session_id`
# tầng ngoài. Subagent mang session_id của phiên cha (payload thật, Claude Code
# 2.1.282), nên dấu do subagent ghi thuộc về phiên cha — đúng thứ hook Stop của
# phiên cha cần thấy.
#
# Chỉ giữ [A-Za-z0-9_-]: không dấu chấm, không gạch chéo — một session_id lạ không
# được thành `..` và trỏ ra ngoài thư mục phiên. Không đọc được thì mọi dấu dồn về
# một thư mục chung: hỏng theo hướng chặn thừa, không theo hướng mất dấu.
phien_dir() {
  local sid
  sid=$(top_str session_id | LC_ALL=C tr -cd 'A-Za-z0-9_-' | cut -c1-80)
  [ -n "$sid" ] || sid=khong-ro-phien
  printf '.claude/.state/phien/%s' "$sid"
}

# nen_dang_chay  (stdin: payload Stop)
# Thoát 0 khi mảng `background_tasks` ở TẦNG NGOÀI có ít nhất một phần tử mang
# "type":"subagent" và "status":"running" — phiên chính kết thúc lượt để CHỜ agent
# nền, chưa dừng hẳn. Mọi trường hợp khác thoát 1: không có khoá, mảng rỗng, chỉ
# có việc kiểu "shell", agent đã xong, JSON cụt hay sai dạng. Hỏng theo hướng CHẶN:
# đọc không ra thì coi như không có agent nền.
#
# Chỉ tính `subagent`: một lệnh shell chạy nền không phải agent đang làm việc cho
# phiên, và cho nó hoãn chặn thì một `sleep` nền là đủ né hàng rào.
#
# Duyệt JSON như top_str: chuỗi đi trọn (kể cả cặp thoát), nên một đoạn
# `"status":"running"` nằm TRONG last_assistant_message không bao giờ được tính.
nen_dang_chay() {
  LC_ALL=C awk '
    function skip_ws(k) { while (k <= n && index(" \t\r\n", substr(s, k, 1)) > 0) k++; return k }
    function str_end(k,   d) {
      k++
      while (k <= n) {
        d = substr(s, k, 1)
        if (d == "\\") { k += 2; continue }
        if (d == "\"") return k
        k++
      }
      return 0
    }
    function val_end(k,   c, depth, j) { # k = ký tự đầu của một giá trị; trả vị trí ký tự cuối, 0 nếu cụt
      c = substr(s, k, 1)
      if (c == "\"") return str_end(k)
      if (c == "{" || c == "[") {
        depth = 0
        while (k <= n) {
          c = substr(s, k, 1)
          if (c == "\"") { j = str_end(k); if (j == 0) return 0; k = j + 1; continue }
          if (c == "{" || c == "[") depth++
          else if (c == "}" || c == "]") { depth--; if (depth == 0) return k }
          k++
        }
        return 0
      }
      while (k <= n && index(",}] \t\r\n", substr(s, k, 1)) == 0) k++
      return k - 1
    }
    { s = s $0 "\n" }
    END {
      n = length(s)
      i = skip_ws(1); if (substr(s, i, 1) != "{") exit 1
      i++; a = 0
      while (1) {
        i = skip_ws(i)
        if (substr(s, i, 1) != "\"") exit 1
        j = str_end(i); if (j == 0) exit 1
        key = substr(s, i + 1, j - i - 1)
        i = skip_ws(j + 1); if (substr(s, i, 1) != ":") exit 1
        i = skip_ws(i + 1)
        e = val_end(i); if (e == 0) exit 1
        if (key == "background_tasks") { a = i; break }
        i = skip_ws(e + 1)
        if (substr(s, i, 1) == ",") { i++; continue }
        exit 1
      }
      if (substr(s, a, 1) != "[") exit 1
      i = a + 1
      while (1) {
        i = skip_ws(i)
        c = substr(s, i, 1)
        if (c == "]") exit 1
        e = val_end(i); if (e == 0) exit 1
        if (c == "{") {
          typ = ""; st = ""; k = i + 1
          while (1) {
            k = skip_ws(k)
            if (substr(s, k, 1) == "}") break
            if (substr(s, k, 1) != "\"") exit 1
            m = str_end(k); if (m == 0) exit 1
            kk = substr(s, k + 1, m - k - 1)
            k = skip_ws(m + 1); if (substr(s, k, 1) != ":") exit 1
            k = skip_ws(k + 1)
            ve = val_end(k); if (ve == 0) exit 1
            if (substr(s, k, 1) == "\"") {
              if (kk == "type") typ = substr(s, k + 1, ve - k - 1)
              else if (kk == "status") st = substr(s, k + 1, ve - k - 1)
            }
            k = skip_ws(ve + 1)
            if (substr(s, k, 1) == ",") { k++; continue }
            if (substr(s, k, 1) == "}") break
            exit 1
          }
          if (typ == "subagent" && st == "running") exit 0
        }
        i = skip_ws(e + 1)
        if (substr(s, i, 1) == ",") { i++; continue }
        exit 1
      }
    }'
}

# nap_prune — dựng mảng PRUNE cho `find` từ chính .gitignore (dòng dạng "ten/"),
# không chép danh sách thứ hai. Tỉa chỉ để `find` không đi qua node_modules — đúng/
# sai cuối cùng vẫn do `git check-ignore` quyết. Dùng: find ... \( "${PRUNE[@]}" -false \) -prune -o ...
nap_prune() {
  PRUNE=()
  local g n
  [ -f .gitignore ] || return 0
  while IFS= read -r g; do
    g="${g%$'\r'}"
    case "$g" in */) n="${g%/}" ;; *) continue ;; esac
    case "$n" in ''|*/*|*[*?\[]*|'#'*|'!'*) continue ;; esac
    PRUNE+=( -name "$n" -o )
  done < .gitignore
}

# bo_gitignore  (stdin: đường dẫn tính từ gốc repo, mỗi dòng một)
# In lại những dòng KHÔNG bị gitignore. So theo TÊN, không theo `NR == FNR`: khi
# không dòng nào bị gitignore thì tệp tạm rỗng, `NR == FNR` đúng luôn cho stdin, và
# MỌI dòng bị coi là "bị gitignore".
bo_gitignore() {
  local tmp list
  list=$(cat)
  [ -n "$list" ] || return 0
  tmp=$(mktemp 2>/dev/null) || { printf '%s\n' "$list"; return 0; }
  printf '%s\n' "$list" | git check-ignore --stdin 2>/dev/null > "$tmp"
  printf '%s\n' "$list" | awk 'FILENAME == ARGV[1] { ig[$0] = 1; next } !($0 in ig)' "$tmp" -
  rm -f "$tmp"
}

# thay_doi_tu <mốc>
# Thoát 0 khi trong repo có ít nhất một tệp HOẶC thư mục mới hơn <mốc> mà không bị
# gitignore — bỏ qua .git và thư mục trạng thái .claude/.state. Không đoán từ chuỗi
# lệnh: hỏi hệ thống tệp, nên sửa bằng Edit, Write hay lệnh shell đều như nhau.
# Tính cả thư mục vì xoá hay đổi tên một mục cập nhật mtime thư mục chứa nó — tệp
# đã xoá không còn mtime để so.
thay_doi_tu() {
  local list
  nap_prune
  list=$(find . \( "${PRUNE[@]}" -name .git -o -path ./.claude/.state \) -prune -o -newer "$1" -print 2>/dev/null \
         | head -2000 | sed 's|^\./||')
  [ -n "$(printf '%s\n' "$list" | bo_gitignore | grep -v '^$' | head -1)" ]
}
