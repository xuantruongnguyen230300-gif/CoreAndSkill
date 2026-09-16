#!/usr/bin/env bash
# on-pretool.sh — hook PreToolUse, matcher Bash|PowerShell.
#
# Lớp chặn thứ hai cho lệnh cấm. `permissions.deny` so TIỀN TỐ của chuỗi lệnh,
# nên lọt khi lệnh bị bọc (`rtk git commit`), bị nối (`cd a && git push`), mang
# tuỳ chọn toàn cục (`git -C . commit`), hoặc chạy qua công cụ PowerShell.
#
# Danh sách lệnh cấm KHÔNG chép ở đây: đọc từ chính các mục `Bash(x:*)` trong
# `permissions.deny` của .claude/settings.json. Hai danh sách thì lệch nhau.
#
# Cách làm:
#   1. tách chuỗi lệnh thành đoạn theo && || ; | & xuống dòng ( ) { }, bỏ qua ký
#      tự nằm trong nháy, bỏ thân heredoc; đi vào $( ) và `...` (bash) như đoạn
#      riêng;
#   2. bóc tiền tố bọc: rtk, env, gán biến X=y, timeout N, nice, nohup, command,
#      exec, time, sudo, xargs; & của PowerShell là dấu tách nên tự rơi ra;
#      bash/sh -c "...", powershell -Command ..., cmd /c ..., eval, find -exec
#      được phân tích tiếp phần lệnh bên trong;
#   3. tên lệnh: bỏ thư mục, bỏ đuôi .exe, hạ chữ thường;
#   4. git: bỏ tuỳ chọn toàn cục (-C <path>, -c k=v, --git-dir=..., --work-tree=...,
#      --no-pager, ...);
#   5. chuỗi còn lại bằng hoặc bắt đầu bằng "<tiền tố cấm> " -> chặn: thoát 2,
#      lý do tiếng Việt ở stderr.
#
# Lỗi phân tích -> KHÔNG chặn vì lỗi đó, ghi .claude/.state/pretool-error.log.
# Một đoạn đã phân tích trọn và khớp thì vẫn chặn. Hook hỏng không được làm hỏng
# mọi lệnh shell của mọi agent; `permissions.deny` vẫn là lớp thứ nhất.
#
# Không jq, không grep -P, tự ép locale. awk chạy ở LC_ALL=C: mọi ký tự cần xét
# đều là ASCII, và chế độ byte không bỏ cuộc vì một chuỗi UTF-8 hỏng.

set -uo pipefail
export LC_ALL=en_US.UTF-8
cd "$(dirname "$0")/../.." || exit 0

STATE=".claude/.state"
SETTINGS=".claude/settings.json"

log_err() {
  mkdir -p "$STATE" 2>/dev/null || return 0
  printf '%s %s\n' "$(date '+%F %T' 2>/dev/null)" "$1" >> "$STATE/pretool-error.log" 2>/dev/null
  return 0
}

if [ ! -f "$SETTINGS" ]; then
  log_err "không thấy $SETTINGS — hook không biết lệnh nào bị cấm, không chặn gì"
  exit 0
fi

AWKPROG='
function err(m) { gsub(/[\t\r\n]+/, " ", m); print "ERR\t" m }
function clip(s) { gsub(/[\t\r\n]+/, " / ", s); return (length(s) > 300) ? substr(s, 1, 300) "..." : s }
function normsp(s) { gsub(/[ \t]+/, " ", s); sub(/^ /, "", s); sub(/ $/, "", s); return s }

# ---- JSON: giải mã một chuỗi bắt đầu ở s[i] == "; kết quả ở JV, vị trí sau nháy đóng ở JI
function jstr(s, i,    out, rest, c, h) {
  out = ""; i++
  while (1) {
    rest = substr(s, i)
    if (!match(rest, /["\\]/)) return 0
    out = out substr(rest, 1, RSTART - 1)
    i += RSTART - 1
    c = substr(s, i, 1)
    if (c == DQ) { JV = out; JI = i + 1; return 1 }
    c = substr(s, i + 1, 1)
    if (c == "n") out = out "\n"
    else if (c == "t") out = out "\t"
    else if (c == "r") out = out "\r"
    else if (c == "b" || c == "f") out = out " "
    else if (c == "u") { h = substr(s, i + 2, 4); out = out uchr(h); i += 4 }
    else if (c == "") return 0
    else out = out c
    i += 2
  }
}
function uchr(h,    n, k, d) {
  n = 0; h = tolower(h)
  for (k = 1; k <= 4; k++) { d = index("0123456789abcdef", substr(h, k, 1)); if (d == 0) return "?"; n = n * 16 + d - 1 }
  if (n == 10) return "\n"
  if (n == 9) return "\t"
  if (n == 13) return "\r"
  if (n >= 32 && n < 127) return sprintf("%c", n)
  return "?"
}
# ---- JSON: làm phẳng; mỗi giá trị chuỗi vào V[đường, thứ tự], số lượng ở C[đường]
function jparse(s,    L, i, c, sp, ty, ky, ek, path, j) {
  L = length(s); i = 1; sp = 0
  while (i <= L) {
    c = substr(s, i, 1)
    if (c == DQ) {
      if (!jstr(s, i)) return 0
      i = JI
      if (sp > 0 && ty[sp] == "{" && ek[sp]) { ky[sp] = JV; ek[sp] = 0; continue }
      path = ""
      for (j = 1; j <= sp; j++) path = path (ty[j] == "{" ? "." ky[j] : "[]")
      C[path]++; V[path, C[path]] = JV
      continue
    }
    if (c == "{" || c == "[") { sp++; ty[sp] = c; ek[sp] = (c == "{"); ky[sp] = ""; i++; continue }
    if (c == "}" || c == "]") { if (sp == 0) return 0; sp--; i++; continue }
    if (c == ",") { if (sp > 0 && ty[sp] == "{") ek[sp] = 1; i++; continue }
    i++
  }
  return (sp == 0)
}

# ---- tách lệnh
function pushw(words, w) { words[++words[0]] = w }
function joinw(words, a, b,    s, k) { s = ""; for (k = a; k <= b; k++) s = s (k > a ? " " : "") words[k]; return s }
function bname(w) { sub(/.*[\/\\]/, "", w); w = tolower(w); sub(/\.exe$/, "", w); return w }

function analyze(cmd, ps, depth,    sC, sL, sP, sPS) {
  if (depth > 6) { PERR = "lệnh lồng quá sâu"; return }
  sC = CMD; sL = LEN; sP = P; sPS = PSM
  CMD = cmd; LEN = length(cmd); P = 1; PSM = ps; HDN[depth] = 0
  scan("", depth)
  CMD = sC; LEN = sL; P = sP; PSM = sPS
}

function heredoc(depth,    strip, d, c) {
  strip = 0
  if (substr(CMD, P, 1) == "-") { strip = 1; P++ }
  while (P <= LEN && (substr(CMD, P, 1) == " " || substr(CMD, P, 1) == "\t")) P++
  d = ""
  while (P <= LEN) {
    c = substr(CMD, P, 1)
    if (c ~ /[ \t\r\n;&|<>()]/) break
    if (c != SQ && c != DQ && c != BS) d = d c
    P++
  }
  if (d == "") { PERR = "heredoc không có dấu kết thúc"; return }
  HDN[depth]++; HDD[depth, HDN[depth]] = d; HDS[depth, HDN[depth]] = strip
}
function skipheredocs(depth,    k, line, e, d) {
  for (k = 1; k <= HDN[depth]; k++) {
    d = HDD[depth, k]
    while (1) {
      if (P > LEN) { PERR = "heredoc thiếu dòng kết thúc " d; HDN[depth] = 0; return }
      e = index(substr(CMD, P), "\n")
      if (e == 0) { line = substr(CMD, P); P = LEN + 1 } else { line = substr(CMD, P, e - 1); P += e }
      sub(/\r$/, "", line)
      if (HDS[depth, k]) sub(/^\t+/, "", line)
      if (line == d) break
    }
  }
  HDN[depth] = 0
}

function scan(stop, depth,    words, word, inw, c, c2, mode, e) {
  words[0] = 0; word = ""; inw = 0; mode = 0
  while (P <= LEN) {
    c = substr(CMD, P, 1)
    if (mode == 1) {
      if (c == SQ) {
        if (PSM && substr(CMD, P + 1, 1) == SQ) { word = word SQ; P += 2; continue }
        mode = 0
      } else word = word c
      P++; continue
    }
    if (mode == 2) {
      if (c == DQ) { mode = 0; P++; continue }
      if (!PSM && c == BS) {
        c2 = substr(CMD, P + 1, 1)
        if (c2 == DQ || c2 == BS || c2 == "$" || c2 == BT) { word = word c2; P += 2; continue }
        if (c2 == "\n") { P += 2; continue }
        word = word c; P++; continue
      }
      if (PSM && c == BT) { word = word substr(CMD, P + 1, 1); P += 2; continue }
      if (c == "$" && substr(CMD, P + 1, 1) == "(") { P += 2; scan(")", depth); continue }
      if (!PSM && c == BT) { P++; scan(BT, depth); continue }
      word = word c; P++; continue
    }
    if (stop != "" && c == stop) {
      P++
      if (inw) pushw(words, word)
      evalseg(words, depth)
      return
    }
    if (c == "#" && !inw) { while (P <= LEN && substr(CMD, P, 1) != "\n") P++; continue }
    if (c == SQ) { mode = 1; inw = 1; P++; continue }
    if (c == DQ) { mode = 2; inw = 1; P++; continue }
    if (!PSM && c == "$" && substr(CMD, P + 1, 1) == SQ) { mode = 1; inw = 1; P += 2; continue }
    if (!PSM && c == "$" && substr(CMD, P + 1, 1) == "{") {
      e = index(substr(CMD, P), "}")
      if (e == 0) { PERR = "thiếu dấu đóng }"; P = LEN + 1; continue }
      word = word substr(CMD, P, e); inw = 1; P += e; continue
    }
    if (!PSM && c == BS) {
      c2 = substr(CMD, P + 1, 1); P += 2
      if (c2 != "\n") { word = word c2; inw = 1 }
      continue
    }
    if (PSM && c == BT) {
      c2 = substr(CMD, P + 1, 1); P += 2
      if (c2 != "\n" && c2 != "\r") { word = word c2; inw = 1 }
      continue
    }
    if (c == "$" && substr(CMD, P + 1, 1) == "(") { P += 2; scan(")", depth); inw = 1; continue }
    if (!PSM && c == BT) { P++; scan(BT, depth); inw = 1; continue }
    if (PSM && c == "@" && (substr(CMD, P + 1, 1) == SQ || substr(CMD, P + 1, 1) == DQ) && substr(CMD, P + 2, 1) ~ /[\r\n]/) {
      c2 = substr(CMD, P + 1, 1)
      e = index(substr(CMD, P + 2), "\n" c2 "@")
      if (e == 0) { PERR = "here-string chưa đóng"; P = LEN + 1; continue }
      P = P + 2 + e + 2; inw = 1; continue
    }
    if (!PSM && c == "<" && substr(CMD, P + 1, 1) == "<" && substr(CMD, P + 2, 1) != "<") {
      if (inw) { pushw(words, word); word = ""; inw = 0 }
      P += 2; heredoc(depth); continue
    }
    if (c == " " || c == "\t" || c == "<" || c == ">") {
      if (inw) { pushw(words, word); word = ""; inw = 0 }
      P++; continue
    }
    if (c == ";" || c == "&" || c == "|" || c == "\n" || c == "\r" || c == "(" || c == ")" || c == "{" || c == "}") {
      if (inw) { pushw(words, word); word = ""; inw = 0 }
      evalseg(words, depth); delete words; words[0] = 0
      P++
      if (c == "\n" && HDN[depth] > 0) skipheredocs(depth)
      continue
    }
    word = word c; inw = 1; P++
  }
  if (mode != 0) PERR = "dấu nháy chưa đóng"
  if (stop != "") PERR = "thiếu dấu đóng " stop
  if (inw) pushw(words, word)
  evalseg(words, depth)
  if (HDN[depth] > 0) { PERR = "heredoc không có thân"; HDN[depth] = 0 }
}

function evalseg(words, depth,    n, i, w, lw, cmdn, j, k, t, sub_, norm) {
  n = words[0]
  if (n == 0) return
  i = 1
  while (i <= n) {
    w = words[i]; lw = bname(w)
    if (w ~ /^[A-Za-z_][A-Za-z0-9_]*=/) { i++; continue }
    if (lw == "rtk") { i++; if (i <= n && (words[i] == "proxy" || words[i] == "err" || words[i] == "test" || words[i] == "summary")) i++; continue }
    if (lw == "nohup" || lw == "exec" || lw == "builtin" || lw == "!" || lw == "if" || lw == "then" || lw == "else" || lw == "elif" || lw == "do" || lw == "while" || lw == "until" || lw == "coproc") { i++; continue }
    if (lw == "command" || lw == "time") { i++; while (i <= n && words[i] ~ /^-/) i++; continue }
    if (lw == "sudo") { i++; while (i <= n && words[i] ~ /^-/) { if (words[i] ~ /^-[ugCDhpRrTt]$/) i++; i++ } continue }
    if (lw == "env") {
      i++
      while (i <= n && (words[i] ~ /^-/ || words[i] ~ /^[A-Za-z_][A-Za-z0-9_]*=/)) {
        if (words[i] == "-S" || words[i] == "--split-string") { if (i < n) analyze(joinw(words, i + 1, n), PSM, depth + 1); return }
        if (words[i] == "-u" || words[i] == "-C" || words[i] == "--unset" || words[i] == "--chdir") i++
        i++
      }
      continue
    }
    if (lw == "timeout") { i++; while (i <= n && words[i] ~ /^-/) { if (words[i] == "-s" || words[i] == "-k" || words[i] == "--signal" || words[i] == "--kill-after") i++; i++ } if (i <= n) i++; continue }
    if (lw == "nice") { i++; while (i <= n && words[i] ~ /^-/) { if (words[i] == "-n" || words[i] == "--adjustment") i++; i++ } continue }
    if (lw == "xargs") { i++; while (i <= n && words[i] ~ /^-/) { if (words[i] ~ /^-[IiLlnPsdaE]$/) i++; i++ } continue }
    break
  }
  if (i > n) return
  cmdn = bname(words[i])

  if (cmdn == "eval") { if (i < n) analyze(joinw(words, i + 1, n), PSM, depth + 1); return }
  if (cmdn == "bash" || cmdn == "sh" || cmdn == "zsh" || cmdn == "dash" || cmdn == "ksh") {
    for (j = i + 1; j <= n; j++) {
      if (words[j] ~ /^-[A-Za-z]*c[A-Za-z]*$/) { if (j < n) analyze(words[j + 1], 0, depth + 1); return }
      if (words[j] !~ /^-/) break
    }
  }
  if (cmdn == "powershell" || cmdn == "pwsh") {
    for (j = i + 1; j <= n; j++) {
      t = tolower(words[j])
      if (t == "-encodedcommand" || t == "-enc" || t == "-e" || t == "-ec") { PERR = "không phân tích được lệnh PowerShell mã hoá (-EncodedCommand)"; return }
      if (t == "-c" || (length(t) >= 4 && index("-command", t) == 1)) { if (j < n) analyze(joinw(words, j + 1, n), 1, depth + 1); return }
    }
  }
  if (cmdn == "cmd") {
    for (j = i + 1; j <= n; j++) { t = tolower(words[j]); if (t == "/c" || t == "/k") { if (j < n) analyze(joinw(words, j + 1, n), 0, depth + 1); return } }
  }
  if (cmdn == "find") {
    for (j = i + 1; j <= n; j++) {
      if (words[j] == "-exec" || words[j] == "-execdir" || words[j] == "-ok" || words[j] == "-okdir") {
        delete sub_; sub_[0] = 0
        for (k = j + 1; k <= n && words[k] != ";" && words[k] != "+"; k++) pushw(sub_, words[k])
        evalseg(sub_, depth + 1)
        j = k
      }
    }
  }

  norm = cmdn; j = i + 1
  if (cmdn == "git") {
    while (j <= n) {
      w = words[j]
      if (w == "-C" || w == "-c" || w == "--git-dir" || w == "--work-tree" || w == "--namespace" || w == "--config-env" || w == "--super-prefix" || w == "--attr-source" || w == "--list-cmds") { j += 2; continue }
      if (w ~ /^--(git-dir|work-tree|namespace|config-env|super-prefix|attr-source|exec-path|list-cmds)=/) { j++; continue }
      if (w == "--no-pager" || w == "-p" || w == "--paginate" || w == "-P" || w == "--bare" || w == "--no-replace-objects" || w == "--literal-pathspecs" || w == "--glob-pathspecs" || w == "--noglob-pathspecs" || w == "--icase-pathspecs" || w == "--no-optional-locks" || w == "--no-lazy-fetch" || w == "--no-advice") { j++; continue }
      break
    }
  }
  for (; j <= n; j++) norm = norm " " words[j]
  for (k = 1; k <= NP; k++) {
    if (norm == PFX[k] || index(norm, PFX[k] " ") == 1) {
      if (HIT == "") { HIT = 1; HITP = PFX[k]; HITSEG = norm }
      return
    }
  }
}

BEGIN { RS = "\001"; SQ = "\047"; DQ = "\""; BS = "\\"; BT = "`"; HIT = ""; PERR = ""; NP = 0 }
FILENAME == SET { SETTXT = SETTXT $0; next }
{ PAYTXT = PAYTXT $0 }
END {
  if (!jparse(SETTXT)) err("settings.json không phân tích được như JSON")
  for (j = 1; j <= C[".permissions.deny[]"]; j++) {
    d = V[".permissions.deny[]", j]
    if (d !~ /^Bash\(.*:\*\)$/) continue
    p = normsp(substr(d, 6, length(d) - 8))
    if (p == "") continue
    sp1 = index(p " ", " ")
    PFX[++NP] = tolower(substr(p, 1, sp1 - 1)) substr(p, sp1)
  }
  if (NP == 0) { err("không trích được mục Bash(...:*) nào từ permissions.deny — hook không chặn gì"); exit 0 }
  delete V; delete C
  if (!jparse(PAYTXT)) err("payload không phân tích trọn vẹn như JSON")
  tool = V[".tool_name", 1]
  if (C[".tool_input.command"] < 1) { err("payload không có tool_input.command (tool_name=" tool ")"); exit 0 }
  analyze(V[".tool_input.command", 1], (tool == "PowerShell") ? 1 : 0, 0)
  if (PERR != "") err(PERR " — lệnh: " clip(V[".tool_input.command", 1]))
  if (HIT != "") print "BLOCK\t" HITP "\t" clip(HITSEG)
  exit 0
}
'

out=$(LC_ALL=C awk -v SET="$SETTINGS" "$AWKPROG" "$SETTINGS" - 2>&1)
rc=$?
if [ "$rc" -ne 0 ]; then
  log_err "bộ phân tích thoát mã $rc — không chặn: $(printf '%s' "$out" | head -3 | tr '\r\n' '  ')"
  exit 0
fi

TAB=$'\t'
blockline=""
while IFS= read -r line; do
  case "$line" in
    '') ;;
    "ERR$TAB"*)   log_err "${line#ERR$TAB}" ;;
    "BLOCK$TAB"*) [ -z "$blockline" ] && blockline="${line#BLOCK$TAB}" ;;
    *)            log_err "dòng lạ từ bộ phân tích: $line" ;;
  esac
done <<< "$out"

[ -n "$blockline" ] || exit 0

pfx="${blockline%%$TAB*}"
seg="${blockline#*$TAB}"
printf '%s\n' \
  "Lệnh bị chặn bởi hook PreToolUse (.claude/hooks/on-pretool.sh)." \
  "" \
  "Đoạn lệnh: $seg" \
  "Khớp mục cấm: Bash($pfx:*) trong permissions.deny của .claude/settings.json." \
  "" \
  "Lệnh git ghi và lệnh phá huỷ là việc của người dùng (.claude/CLAUDE.md §1). Dừng lại và nói rõ cần chạy lệnh gì; người dùng tự chạy rồi bảo tiếp tục. Đừng viết lại lệnh cho lọt qua hook." >&2
exit 2
