#!/usr/bin/env bash
# on-pretool.sh — hook PreToolUse. HAI matcher, cùng gọi tệp này (settings.json):
#   - Bash|PowerShell → phân tích chuỗi lệnh (phần chính bên dưới);
#   - Edit|Write|NotebookEdit → đọc file_path/notebook_path, chặn nếu nhắm vùng bảo
#     vệ (§6 dưới: .claude/.state, .git, tệp cấu hình git toàn cục). Dồn chung một tệp
#     để dùng lại bộ đọc JSON và hàm chuẩn hoá đường dẫn `protpath` (không tách script
#     thứ hai).
#
# Trên từng đoạn của chuỗi lệnh:
#   - lệnh cấm: lớp thứ hai sau `permissions.deny`. deny so TIỀN TỐ của chuỗi lệnh,
#     nên lọt khi lệnh bị bọc (`rtk git commit`), bị nối (`cd a && git push`), mang
#     tuỳ chọn toàn cục (`git -C . commit`), hoặc chạy qua công cụ PowerShell;
#   - git theo DANH SÁCH CHO PHÉP (ADR-0111 quyết định 4): lệnh git không khớp mục
#     `Bash(git …)` nào của `permissions.allow` bị chặn. Đây là lớp chặn CHÍNH cho
#     lệnh git ghi mà deny không liệt kê — deny không diễn đạt được "mọi lệnh trừ …".
#     Mục có `:*` khớp tiền tố theo từ, mục không có `:*` khớp nguyên lệnh, như
#     harness. Lệnh được phép vẫn bị chặn khi mang tuỳ chọn ghi tệp hay chạy chương
#     trình ngoài (`--output`, `git grep -O`, `--help`), tuỳ chọn toàn cục đổi cấu
#     hình hay repo (`-c`, `--config-env`, `-C` trỏ nơi khác gốc repo, `--git-dir`,
#     `--work-tree`, `--exec-path=`, `--bare`), hoặc phép gán biến môi trường đứng
#     ngay trước git (cùng kênh với `-c`: GIT_CONFIG_*, GIT_DIR, HOME…).
#
# Các đường vòng đã bịt thêm (lượt soát 2026-09-29) — mỗi quy tắc kèm ca PASS ở
# .claude/hooks/tests/run-tests.sh chứng minh không chặn lệnh vô hại cùng hình dạng:
#   1. tên lệnh động: `$g`/`${g}` giải theo phép gán đã thấy, `$(echo git)` và
#      `` `echo git` `` ở vị trí tên lệnh, PowerShell `& $g`, `& (Get-Command git)`,
#      `git${IFS}push`, `git {push,}` (mở rộng ngoặc → lệnh con không có tên);
#   2. chạy chuỗi như lệnh: PowerShell `iex`/`Invoke-Expression`; trình thông dịch
#      đọc stdin — `echo … | bash`, `bash <<< …`, `sh -s <<EOF … EOF`; `bash -c --`;
#      `exec -a`;
#   3. lệnh bọc: setsid, stdbuf, winpty, watch, flock (ngoài env/timeout/nice/nohup);
#   4. lệnh trong công cụ khác: sed lệnh `e`; awk system()/pipe/getline; git dưới
#      xargs không có lệnh con;
#   5. biến môi trường quyết định git đọc cấu hình nào (danh sách ở homevar(): GIT_*,
#      HOME, XDG_CONFIG_HOME, USERPROFILE, HOMEDRIVE, HOMEPATH) đổi ở câu lệnh riêng:
#      export/declare/typeset/readonly/local, phép gán đứng một mình (`HOME=/x`),
#      PowerShell `$env:X=`, `Set-Item`/`New-Item` trên ổ Env:, `setx`;
#   6. ghi vùng bảo vệ — .claude/.state, .git, và tệp cấu hình git toàn cục
#      (~/.gitconfig, ~/.config/git/…, $XDG_CONFIG_HOME/git/…, với mọi neo thư mục nhà
#      ở HOMERE): lệnh ghi (touch/rm/mv/cp/tee/sed -i, cmdlet PowerShell và bí danh)
#      có đối số ở đó, chuyển hướng `>` tới đó, và công cụ Edit/Write/NotebookEdit ghi
#      vào đó. Lệnh chỉ ĐỌC (cat ~/.gitconfig, Get-Content, Get-Item Env:HOME) cho qua.
# ĐƯỜNG HOOK KHÔNG THẤY — chú thích này là nguồn duy nhất của danh sách (ADR-0114
# quyết định 11; luật C4 ở docs/RULES-DOCS.md). Lệnh cấm, hay lệnh ghi vào thư mục
# bảo vệ, đi qua đường mà hook PreToolUse không đọc được. Hook chỉ đọc phần TĨNH của
# chuỗi lệnh trong một lời gọi công cụ, hay đường dẫn đích của công cụ ghi tệp. Các
# đường dưới đặt lệnh cấm vào chỗ đó không chứa, hoặc chỉ biết được lúc chạy (đối
# chiếu hook 2026-09-30).
#   (1) Tên lệnh hay tuỳ chọn sinh lúc chạy: mở rộng ngoặc ở vị trí tên lệnh
#       (`{git,} push`); biến gán từ thay thế lệnh (`g=$(which git); $g push`); tuỳ
#       chọn ẩn sau biến hay thay thế lệnh (`git log $opt`, `git log $(…)`) — lệnh con
#       được phép nên lọt, dù giá trị mang tuỳ chọn ghi tệp.
#   (2) Lệnh nằm trong tệp: ghi lệnh vào tệp rồi chạy tệp (`echo git push > s.sh;
#       bash s.sh`); chương trình awk đọc từ tệp (`awk -f <tệp>`).
#   (3) Nguồn ống dẫn không tĩnh cho trình thông dịch: `curl … | bash`,
#       `printf '%s' "$v" | bash`, nhiều tầng như `echo x | rev | bash`; `… | pwsh -`.
#   (4) Mã của trình khác: tiến trình con do `node`, `python` sinh; PowerShell
#       `Start-Process git -ArgumentList push`; `-EncodedCommand` — hook cho qua, chỉ
#       ghi log.
#   (5) Cấu hình git. Kênh trong repo đã đóng: `git config` bị chặn — không có trong
#       danh sách cho phép, có trong `deny`; ghi vào `.git/` bị chặn cả qua lệnh shell
#       lẫn Edit, Write, NotebookEdit; `git <alias>` bị danh sách cho phép chặn, vì git
#       không cho alias che lệnh có sẵn. Tệp cấu hình git của người dùng
#       (`~/.gitconfig`, `~/.config/git/`, `$XDG_CONFIG_HOME/git/`) và các biến quyết
#       định nó (`HOME`, `XDG_CONFIG_HOME`, `USERPROFILE`, `HOMEDRIVE`, `HOMEPATH`,
#       `GIT_*`) đã vào vùng bảo vệ (2026-10-01). Còn hở — một lệnh đọc được phép sẽ
#       chạy chương trình theo cấu hình bị sửa (`core.fsmonitor`, `diff.external`):
#       đích sinh lúc chạy (`f=~/.gitconfig; echo x >> $f`, PowerShell
#       `Join-Path $HOME .gitconfig`); đích tương đối khi thư mục hiện tại là nhà
#       (`cd ~ && echo x >> .gitconfig`); ghi qua thư mục cha (`cp -r d/git ~/.config/`,
#       `tar -xf a.tar -C ~`); thân `cmd /c` đọc theo luật bash; gọi .NET trong
#       PowerShell (`[IO.File]::WriteAllText(…)`,
#       `[Environment]::SetEnvironmentVariable(…)`); chương trình ghi ngoài tập lệnh ghi
#       (xcopy, robocopy, trình soạn thảo); tệp cấu hình hệ thống (`/etc/gitconfig`,
#       `%PROGRAMDATA%\Git\config`); `unset HOME`; và — có chủ ý — gán `HOME=` đứng
#       trước một lệnh không phải git mà chính tiến trình đó gọi git (npm script,
#       make). Phân tích đủ mọi đường là tự viết lại một shell, và vẫn sót đường kế
#       tiếp.
# Chấp nhận, không bịt thêm. Lớp bắt được là người đọc bản ghi phiên. Một trong các
# đường trên trở thành cách làm thường ngày là dấu hiệu phải xem lại cách chặn —
# không phải thêm một mẫu vào hook.
# Chi tiết thêm của chính hook này: ghi qua thư mục cha còn rsync, unzip; tệp cấu hình
# hệ thống còn <Git>/etc/gitconfig; thân `cmd /c` đọc theo luật bash nên
# `%USERPROFILE%\.gitconfig` mất gạch chéo ngược. Gán biến ĐỨNG TRƯỚC lệnh không phải
# git (`HOME=/tmp npm …`) cho qua vì phạm vi gói trong đúng lệnh đó, không đổi cấu
# hình git về sau; chặn nó là chặn mọi HOME= tạm thời.
#
# Không danh sách nào chép ở đây: lệnh cấm đọc từ các mục `Bash(x:*)` của
# `permissions.deny`, lệnh git được phép từ các mục `Bash(git …)` của
# `permissions.allow` trong .claude/settings.json. Hai danh sách thì lệch nhau.
# deny thắng allow. Đọc được settings.json mà allow không có mục git nào thì mọi
# lệnh git bị chặn (mặc định chặn), kèm log.
#
# Cách làm:
#   1. tách chuỗi lệnh thành đoạn theo && || ; | & xuống dòng ( ) { }, bỏ qua ký
#      tự nằm trong nháy, bỏ thân heredoc; đi vào $( ) và `...` (bash) như đoạn
#      riêng; `<<<` (here-string) và `<<` trong số học $((…)) ((…)) $[…] KHÔNG
#      phải heredoc — nội dung số học được phân tích như lệnh, không nhận heredoc;
#      chuyển hướng: số fd dính liền trước dấu và tệp đích sau dấu không phải đối số;
#   2. bóc tiền tố bọc: rtk, env, gán biến X=y, timeout N, nice, nohup, command,
#      exec, time, sudo, xargs; & của PowerShell là dấu tách nên tự rơi ra;
#      bash/sh -c "...", powershell -Command ..., cmd /c ..., eval, find -exec
#      được phân tích tiếp phần lệnh bên trong;
#   3. tên lệnh: bỏ thư mục, bỏ đuôi .exe, hạ chữ thường; `git-<lệnh>` (dạng gạch
#      nối trong libexec/git-core) đọc như `git <lệnh>`;
#   4. git: bỏ tuỳ chọn toàn cục (-C <path>, -c k=v, --git-dir=..., --no-pager, ...)
#      để khớp, nhưng ghi nhận tuỳ chọn nguy hiểm;
#   5. chuỗi còn lại bằng hoặc bắt đầu bằng "<tiền tố cấm> " -> chặn; với git thêm
#      các phép ở trên. Chặn: thoát 2, lý do tiếng Việt ở stderr.
#
# Lỗi phân tích -> KHÔNG chặn vì lỗi đó, ghi .claude/.state/pretool-error.log.
# Một đoạn đã phân tích trọn và khớp thì vẫn chặn. Hook hỏng không được làm hỏng
# mọi lệnh shell của mọi agent; `permissions.deny` vẫn là lớp thứ nhất.
#
# Không jq, không grep -P, tự ép locale. awk chạy ở LC_ALL=C: mọi ký tự cần xét
# đều là ASCII, và chế độ byte không bỏ cuộc vì một chuỗi UTF-8 hỏng.
#
# Thời gian phải TUYẾN TÍNH theo độ dài lệnh: harness giết hook quá `timeout` của
# khối hooks rồi cho lệnh đi tiếp, nên hook chậm = lệnh cấm lọt. Trong vòng lặp
# không chép đuôi chuỗi (`substr(s, i)`): tìm bằng cửa sổ cố định (findat), giải
# mã JSON bằng split, nối chuỗi theo khúc chứ không theo từng ký tự; không gsub trên
# cả lệnh (gsub của gawk tốn theo số chỗ khớp x độ dài chuỗi).

set -uo pipefail
export LC_ALL=en_US.UTF-8
cd "$(dirname "$0")/../.." || exit 0

STATE=".claude/.state"
SETTINGS=".claude/settings.json"
# Gốc repo ở hai dạng — `git -C <gốc repo>` không trỏ sang repo khác.
ROOTU=$(pwd)
ROOTW=$(pwd -W 2>/dev/null) || ROOTW=""

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
function clip(s) { if (length(s) > 300) s = substr(s, 1, 300) "..."; gsub(/[\t\r\n]+/, " / ", s); return s }
function normsp(s) { gsub(/[ \t]+/, " ", s); sub(/^ /, "", s); sub(/ $/, "", s); return s }

# ---- JSON: giải mã một chuỗi bắt đầu ở s[i] == "; kết quả ở JV, vị trí sau nháy đóng ở JI.
# Tìm nháy đóng qua cửa sổ 256 byte (không chép đuôi chuỗi), rồi giải mã cả chuỗi một lần.
function jstr(s, i,    st, w, e) {
  st = ++i
  while (1) {
    w = substr(s, i, 256)
    if (w == "") return 0
    e = match(w, /["\\]/)
    if (e == 0) { i += length(w); continue }
    i += e - 1
    if (substr(s, i, 1) == DQ) break
    if (substr(s, i + 1, 1) == "") return 0
    i += 2
  }
  JV = substr(s, st, i - st); JI = i + 1
  if (index(JV, BS)) JV = junesc(JV)
  return 1
}
# Tách theo \ (split tuyến tính; gsub của gawk tốn theo số chỗ khớp x độ dài chuỗi):
# mỗi mảnh sau mảnh đầu mở bằng ký tự thoát; mảnh rỗng là \\ và mảnh kế sau nó là văn
# bản thường, không mở chuỗi thoát. Nối vào buf ngắn rồi đổ sang out theo khúc 4 KB, để
# awk không tối ưu phép nối (mawk) vẫn chép tuyến tính.
function junesc(r,    n, pc, k, p, c, buf, out) {
  n = split(r, pc, /\\/); buf = pc[1]; out = ""
  for (k = 2; k <= n; k++) {
    p = pc[k]
    if (p == "") { k++; buf = buf BS pc[k] }
    else {
      c = substr(p, 1, 1)
      if (c == "u") buf = buf uchr(substr(p, 2, 4)) substr(p, 6)
      else {
        if (c == "n") c = "\n"
        else if (c == "t") c = "\t"
        else if (c == "r") c = "\r"
        else if (c == "b" || c == "f") c = " "
        buf = buf c substr(p, 2)
      }
    }
    if (length(buf) > 4096) { out = out buf; buf = "" }
  }
  return out buf
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
# words["rd"] = 1: từ kế tiếp là tệp đích của chuyển hướng — bỏ, không phải đối số.
# Mỗi đoạn lệnh bắt đầu bằng mảng mới (delete words), nên cờ không tràn sang đoạn sau.
function pushw(words, w) {
  if (words["rd"]) { words["rd"] = 0; return }
  if (words["rdout"]) { words["rdout"] = 0; if (protpath(w)) hit("write", w, w); return }
  if (words["hswant"]) { words["hswant"] = 0; words["hs"] = w; return }
  words[++words[0]] = w
}
function joinw(words, a, b,    s, k) { s = ""; for (k = a; k <= b; k++) s = s (k > a ? " " : "") words[k]; return s }
function bname(w) { sub(/.*[\/\\]/, "", w); w = tolower(w); sub(/\.exe$/, "", w); return w }

# nohd = 1: nội dung số học — `<<` là phép dịch bit, không mở heredoc.
function analyze(cmd, ps, depth, nohd,    sC, sL, sP, sPS, sN) {
  if (depth > 6) { PERR = "lệnh lồng quá sâu"; return }
  sC = CMD; sL = LEN; sP = P; sPS = PSM; sN = NOHD
  CMD = cmd; LEN = length(cmd); P = 1; PSM = ps; NOHD = nohd; HDN[depth] = 0
  scan("", depth)
  CMD = sC; LEN = sL; P = sP; PSM = sPS; NOHD = sN
}

# Vị trí đầu tiên >= p của chuỗi t trong CMD, 0 nếu không có. Cửa sổ cố định có phần
# gối đầu length(t)-1: tổng số byte chép tuyến tính theo độ dài lệnh.
function findat(t, p,    n, e) {
  n = length(t) - 1
  while (p <= LEN) {
    e = index(substr(CMD, p, 512 + n), t)
    if (e) return p + e - 1
    p += 512
  }
  return 0
}
# Nháy kép đóng tính từ p (bỏ qua ký tự sau \), 0 nếu không có.
function dqend(p,    c) {
  for (; p <= LEN; p++) { c = substr(CMD, p, 1); if (c == BS) p++; else if (c == DQ) return p }
  return 0
}
# Số học: p ngay sau dấu mở, lvl = số ngoặc đang mở (2 cho "((", 1 cho "$[").
# Trả vị trí dấu đóng đầu tiên của "))" (hoặc "]"), 0 nếu không phải số học. Như bash:
# "((" / "$((" mà ngoặc trong đóng không liền ")" là subshell / lệnh con lồng nhau.
function arithend(p, lvl, op, cl,    c) {
  for (; p <= LEN; p++) {
    c = substr(CMD, p, 1)
    if (c == SQ) { p = findat(SQ, p + 1); if (!p) return 0 }
    else if (c == DQ) { p = dqend(p + 1); if (!p) return 0 }
    else if (c == BS) p++
    else if (c == op) lvl++
    else if (c == cl) {
      lvl--
      if (op == "(" && lvl == 1) return (substr(CMD, p + 1, 1) == ")") ? p : 0
      if (lvl == 0) return p
    }
  }
  return 0
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
# Bỏ thân heredoc. Khi đoạn lệnh là một trình thông dịch đọc stdin (HDWANT do evalseg
# bật), GOM thân lại rồi phân tích như một lệnh — `sh -s <<EOF … EOF` chạy thân đó.
# Không có HDWANT thì không gom: thân heredoc lớn (cat <<EOF) vẫn bỏ qua tuyến tính.
function skipheredocs(depth,    k, line, e, d, cap, body) {
  cap = HDWANT[depth]; body = ""
  for (k = 1; k <= HDN[depth]; k++) {
    d = HDD[depth, k]
    while (1) {
      if (P > LEN) { PERR = "heredoc thiếu dòng kết thúc " d; HDN[depth] = 0; HDWANT[depth] = 0; return }
      e = findat("\n", P)
      if (e == 0) { line = substr(CMD, P); P = LEN + 1 } else { line = substr(CMD, P, e - P); P = e + 1 }
      sub(/\r$/, "", line)
      if (HDS[depth, k]) sub(/^\t+/, "", line)
      if (line == d) break
      if (cap) body = body line "\n"
    }
  }
  HDN[depth] = 0
  if (cap) { HDWANT[depth] = 0; analyze(body, HDWANTPS[depth], depth + 1) }
}

function scan(stop, depth,    words, word, inw, c, c2, mode, e, w, isout, dup, r, inner, q, nm) {
  words[0] = 0; word = ""; inw = 0; mode = 0
  while (P <= LEN) {
    c = substr(CMD, P, 1)
    if (mode == 1) {
      e = findat(SQ, P)
      if (e == 0) { word = word substr(CMD, P); P = LEN + 1; continue }
      word = word substr(CMD, P, e - P); P = e + 1
      if (PSM && substr(CMD, P, 1) == SQ) { word = word SQ; P++; continue }
      mode = 0; continue
    }
    if (mode == 2) {
      if (c != DQ && c != BS && c != BT && c != "$") {
        w = substr(CMD, P, 256); e = match(w, /["\\`$]/)
        if (e == 0) e = length(w) + 1
        word = word substr(w, 1, e - 1); P += e - 1; continue
      }
      if (c == DQ) { mode = 0; P++; continue }
      if (!PSM && c == BS) {
        c2 = substr(CMD, P + 1, 1)
        if (c2 == DQ || c2 == BS || c2 == "$" || c2 == BT) { word = word c2; P += 2; continue }
        if (c2 == "\n") { P += 2; continue }
        word = word c; P++; continue
      }
      if (PSM && c == BT) { word = word substr(CMD, P + 1, 1); P += 2; continue }
      if (!PSM && c == "$" && substr(CMD, P + 1, 2) == "((" && (e = arithend(P + 3, 2, "(", ")"))) {
        analyze(substr(CMD, P + 3, e - P - 3), 0, depth + 1, 1); P = e + 2; continue
      }
      if (c == "$" && substr(CMD, P + 1, 1) == "(") { P += 2; scan(")", depth); continue }
      if (!PSM && c == BT) { P++; scan(BT, depth); continue }
      word = word c; P++; continue
    }
    if (stop != "" && c == stop) {
      P++
      if (inw) pushw(words, word)
      SEG_SEP = ""; evalseg(words, depth)
      return
    }
    if (c == "#" && !inw) { while (P <= LEN && substr(CMD, P, 1) != "\n") P++; continue }
    if (c == SQ) { mode = 1; inw = 1; P++; continue }
    if (c == DQ) { mode = 2; inw = 1; P++; continue }
    if (!PSM && c == "$" && substr(CMD, P + 1, 1) == SQ) { mode = 1; inw = 1; P += 2; continue }
    if (!PSM && c == "$" && substr(CMD, P + 1, 1) == "{") {
      e = findat("}", P)
      if (e == 0) { PERR = "thiếu dấu đóng }"; P = LEN + 1; continue }
      inner = substr(CMD, P + 2, e - P - 2)
      # ${IFS} là dấu tách từ (git${IFS}push -> git push), không phải ký tự của từ.
      if (inner == "IFS") { if (inw) { pushw(words, word); word = ""; inw = 0 } P = e + 1; continue }
      word = word substr(CMD, P, e - P + 1); inw = 1; P = e + 1; continue
    }
    if (!PSM && c == "$" && substr(CMD, P + 1, 3) == "IFS" && substr(CMD, P + 4, 1) !~ /[A-Za-z0-9_]/) {
      if (inw) { pushw(words, word); word = ""; inw = 0 }
      P += 4; continue
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
    if (!PSM && c == "$" && substr(CMD, P + 1, 2) == "((" && (e = arithend(P + 3, 2, "(", ")"))) {
      analyze(substr(CMD, P + 3, e - P - 3), 0, depth + 1, 1); P = e + 2; inw = 1; continue
    }
    if (!PSM && c == "$" && substr(CMD, P + 1, 1) == "[" && (e = arithend(P + 2, 1, "[", "]"))) {
      analyze(substr(CMD, P + 2, e - P - 2), 0, depth + 1, 1); P = e + 1; inw = 1; continue
    }
    if (c == "$" && substr(CMD, P + 1, 1) == "(") {
      # $(echo git) ở vị trí TÊN lệnh: giải kết quả tĩnh rồi tiêm vào làm tên lệnh.
      if (!inw && words[0] == 0 && (e = parenend(P + 2))) {
        r = echoresolve(substr(CMD, P + 2, e - P - 2))
        if (r != "") { injectwords(words, r); P = e + 1; word = ""; inw = 0; continue }
      }
      P += 2; scan(")", depth); inw = 1; continue
    }
    if (!PSM && c == BT) {
      if (!inw && words[0] == 0 && (e = findat(BT, P + 1))) {
        r = echoresolve(substr(CMD, P + 1, e - P - 1))
        if (r != "") { injectwords(words, r); P = e + 1; word = ""; inw = 0; continue }
      }
      P++; scan(BT, depth); inw = 1; continue
    }
    if (PSM && c == "@" && (substr(CMD, P + 1, 1) == SQ || substr(CMD, P + 1, 1) == DQ) && substr(CMD, P + 2, 1) ~ /[\r\n]/) {
      c2 = substr(CMD, P + 1, 1)
      e = findat("\n" c2 "@", P + 2)
      if (e == 0) { PERR = "here-string chưa đóng"; P = LEN + 1; continue }
      P = e + 3; inw = 1; continue
    }
    if (!PSM && c == "(" && substr(CMD, P + 1, 1) == "(" && (e = arithend(P + 2, 2, "(", ")"))) {
      if (inw) { pushw(words, word); word = ""; inw = 0 }
      SEG_SEP = ""; evalseg(words, depth); delete words; words[0] = 0
      analyze(substr(CMD, P + 2, e - P - 2), 0, depth + 1, 1); P = e + 2; continue
    }
    # PowerShell toán tử gọi `& ( … )`: & (Get-Command git) push -> chạy git push.
    if (PSM && c == "&" && substr(CMD, P + 1, 1) != "&") {
      q = P + 1
      while (q <= LEN && (substr(CMD, q, 1) == " " || substr(CMD, q, 1) == "\t")) q++
      if (substr(CMD, q, 1) == "(" && (e = parenend(q + 1))) {
        inner = substr(CMD, q + 1, e - q - 1)
        nm = psgetcmd(inner)
        if (inw) { pushw(words, word); word = ""; inw = 0 }
        SEG_SEP = ""; evalseg(words, depth); delete words; words[0] = 0
        if (nm != "") pushw(words, nm); else analyze(inner, 1, depth + 1)
        P = e + 1; continue
      }
    }
    # `{` là nhóm lệnh chỉ khi ở đầu đoạn và theo sau là khoảng trắng/`;`; còn lại là
    # ký tự của từ (mở rộng ngoặc `git {push,}` giữ `{push,}` thành một từ -> lệnh con
    # git không có tên -> chặn). `}` đứng riêng là đóng nhóm.
    if (c == "{" && !inw && words[0] == 0 && substr(CMD, P + 1, 1) ~ /[ \t\r\n;]/) {
      SEG_SEP = ""; evalseg(words, depth); delete words; words[0] = 0; P++; continue
    }
    if (c == "}" && !inw) {
      SEG_SEP = ""; evalseg(words, depth); delete words; words[0] = 0; P++; continue
    }
    # here-string `<<<`: thân là MỘT từ. Đánh dấu để bắt lấy — nếu đoạn là trình
    # thông dịch (bash <<< "git push") thì evalseg phân tích thân đó như một lệnh.
    if (!PSM && c == "<" && substr(CMD, P + 1, 2) == "<<") {
      if (inw) { pushw(words, word); word = ""; inw = 0 }
      P += 3; words["hswant"] = 1; continue
    }
    if (!PSM && !NOHD && c == "<" && substr(CMD, P + 1, 1) == "<") {
      if (inw) { pushw(words, word); word = ""; inw = 0 }
      P += 2; heredoc(depth); continue
    }
    # Chuyển hướng > >> >| >& < <& <>: số fd dính liền trước dấu (2>, PowerShell *>)
    # và tệp đích sau dấu không phải đối số — không bỏ thì `git remote -v 2>&1` không
    # khớp mục nguyên lệnh, và `>f git push` đọc thành lệnh tên "f". `<(` / `>(` là
    # thay thế tiến trình: không có tệp đích, phần trong ngoặc là một đoạn lệnh.
    if (c == "<" || c == ">") {
      if (inw) { if (word !~ /^[0-9]+$/ && !(PSM && word == "*")) pushw(words, word); word = ""; inw = 0 }
      isout = (c == ">"); dup = 0
      P++
      c2 = substr(CMD, P, 1)
      if (c2 == "&") { dup = 1; P++ }
      else if (c2 == ">" || c2 == "|") P++
      else if (c == "<" && c2 == ">") { isout = 1; P++ }
      # Tệp đích của chuyển hướng GHI (> >> >| <>) — chặn nếu nhắm thư mục bảo vệ (§6).
      # `>&fd` là nhân bản fd, không phải tệp. `<` là đọc.
      if (substr(CMD, P, 1) != "(") { if (isout && !dup) words["rdout"] = 1; else words["rd"] = 1 }
      continue
    }
    if (c == " " || c == "\t") {
      if (inw) { pushw(words, word); word = ""; inw = 0 }
      P++; continue
    }
    if (c == ";" || c == "&" || c == "|" || c == "\n" || c == "\r" || c == "(" || c == ")") {
      if (inw) { pushw(words, word); word = ""; inw = 0 }
      SEG_SEP = c; evalseg(words, depth); delete words; words[0] = 0
      P++
      if (c == "\n" && HDN[depth] > 0) skipheredocs(depth)
      continue
    }
    word = word c; inw = 1; P++
  }
  if (mode != 0) PERR = "dấu nháy chưa đóng"
  if (stop != "") PERR = "thiếu dấu đóng " stop
  if (inw) pushw(words, word)
  SEG_SEP = ""; evalseg(words, depth)
  if (HDN[depth] > 0) { PERR = "heredoc không có thân"; HDN[depth] = 0 }
}

# Ghi lần chặn đầu tiên: loại, chi tiết, đoạn lệnh.
function hit(kind, det, seg) { if (HIT == "") { HIT = kind; HITP = det; HITSEG = seg } }
function denyhit(norm,    k) {
  for (k = 1; k <= NP; k++) if (norm == PFX[k] || index(norm, PFX[k] " ") == 1) return k
  return 0
}
function allowhit(norm,    k) {
  for (k = 1; k <= NGA; k++) {
    if (norm == GA[k]) return k
    if (!GX[k] && index(norm, GA[k] " ") == 1) return k
  }
  return 0
}
# `-C <p>` chỉ được khi p là "." hoặc chính gốc repo (dạng POSIX hay ổ đĩa Windows,
# gạch chéo nào cũng được, bỏ gạch cuối). Thư mục con cũng chặn: có thể là repo khác.
function normpath(p) {
  gsub(/\\/, "/", p)
  while (length(p) > 1 && substr(p, length(p)) == "/") p = substr(p, 1, length(p) - 1)
  return (ROOTW != "") ? tolower(p) : p
}
function samerepo(p) {
  p = normpath(p)
  return (p == "." || p == normpath(ROOTU) || (ROOTW != "" && p == normpath(ROOTW)))
}
# Tuỳ chọn của lệnh git được phép mà ghi tệp hoặc chạy chương trình ngoài. Chặn cả
# dạng viết tắt tiền tố (parse-options của git nhận) và cả sau `--` — thận trọng hơn git.
function gitdanger(w, sub_,    o, k, c) {
  if (w == "--help") return w
  o = w; k = index(o, "="); if (k) o = substr(o, 1, k - 1)
  # --output=<tệp>: mọi lệnh qua setup_revisions (log, show, diff, shortlog…) mở tệp ghi.
  if (length(o) >= 4 && index("--output", o) == 1) return w
  if (sub_ == "grep") {
    if (length(o) >= 4 && index("--open-files-in-pager", o) == 1) return w
    # cụm cờ ngắn: -O ở bất kỳ vị trí nào trước một cờ nhận đối số (e f A B C m)
    if (w ~ /^-[^-]/) {
      for (k = 2; k <= length(w); k++) {
        c = substr(w, k, 1)
        if (c == "O") return w
        if (index("efABCm", c)) break
      }
    }
  }
  return ""
}

# ---- các đường vòng (lượt soát 2026-09-29, mở rộng 2026-09-30)
# Đường dẫn nhắm thư mục trạng thái của harness (.claude/.state), .git, hay tệp cấu
# hình git TOÀN CỤC của người dùng. Chuẩn hoá gạch chéo, hạ chữ thường (Windows), cả
# sau `x=` (dạng dd of=...). `.gitignore`, `.github` KHÔNG khớp vì `.git` phải theo
# sau bởi `/` hoặc hết chuỗi; `.gitconfig` cũng KHÔNG khớp `.git` vì lý do đó — nó do
# gitcfg() bắt riêng.
# gitcfg(q): q đã chuẩn hoá. Nhận tệp cấu hình git toàn cục — sửa nó thì một lệnh git
# ĐƯỢC PHÉP (status, diff) chạy chương trình ngoài (core.fsmonitor, diff.external,
# textconv…). Ba dạng, kèm mọi neo tuyệt đối tương ứng của thư mục nhà:
#   - $XDG_CONFIG_HOME/git/… (biến trỏ thẳng vào thư mục cấu hình);
#   - <nhà>/.gitconfig (tệp cấu hình toàn cục);
#   - <nhà>/.config/git/… (thư mục cấu hình git mặc định trong thư mục nhà).
# <nhà>: xem HOMERE ở BEGIN (~, $HOME, $USERPROFILE, $env:…, %USERPROFILE%, dạng
#   POSIX /<ổ>/users/<tên> của Git Bash, dạng ổ đĩa <ổ>:/users/<tên>, /home/<tên>).
function gitcfg(q) {
  if (q ~ ("^(\\$\\{?xdg_config_home\\}?|\\$env:xdg_config_home)/git(/|$)")) return 1
  if (q ~ (HOMERE "/\\.gitconfig$")) return 1
  if (q ~ (HOMERE "/\\.config/git(/|$)")) return 1
  return 0
}
# homevar(nm): biến môi trường quyết định git đọc cấu hình nào — GIT_* (cấu hình/repo
# trực tiếp); HOME, XDG_CONFIG_HOME (vị trí tệp cấu hình toàn cục); USERPROFILE,
# HOMEDRIVE, HOMEPATH (Git for Windows dựng HOME từ chúng khi HOME không đặt — trường
# hợp thường gặp trong PowerShell). Một danh sách cho mọi nhánh: export/declare, gán
# câu riêng, $env:, setx, Set-Item Env:. Tên so đúng chữ; bên gọi tự hạ/nâng chữ khi
# nền tảng không phân biệt (PowerShell, setx).
function homevar(nm) {
  return (nm ~ /^GIT_/ || nm == "HOME" || nm == "XDG_CONFIG_HOME" || nm == "USERPROFILE" || nm == "HOMEDRIVE" || nm == "HOMEPATH")
}
function protpath(p,    q, a) {
  q = p; gsub(/\\/, "/", q); q = tolower(q)
  while (q ~ /^\.\//) sub(/^\.\//, "", q)
  gsub(/\/\/+/, "/", q)
  if (q ~ /(^|\/)\.claude\/\.state(\/|$)/) return 1
  if (q ~ /(^|\/)\.git(\/|$)/) return 1
  if (gitcfg(q)) return 1
  if (index(q, "=")) { a = substr(q, index(q, "=") + 1)
    if (a ~ /(^|\/)\.claude\/\.state(\/|$)/ || a ~ /(^|\/)\.git(\/|$)/ || gitcfg(a)) return 1 }
  return 0
}
# Tên lệnh động: `$var` / `${var}` (bash và PowerShell) giải ra giá trị đã ghi ở GV.
function resolvetok(w,    name) {
  name = ""
  if (w ~ /^\$\{[A-Za-z_][A-Za-z0-9_]*\}$/) name = substr(w, 3, length(w) - 3)
  else if (w ~ /^\$[A-Za-z_][A-Za-z0-9_]*$/) name = substr(w, 2)
  if (name != "" && (name in GV)) return GV[name]
  return ""
}
# Kết quả TĨNH của `echo`/`printf` (chỉ khi không còn phép mở rộng nào): dùng khi
# một thay thế lệnh $(...) / `...` đứng ở vị trí TÊN lệnh, hoặc echo|trình-thông-dịch.
function echoresolve(s,    t, rest) {
  if (length(s) > 4096) return ""
  t = s; sub(/^[ \t]+/, "", t); sub(/[ \t]+$/, "", t)
  if (t ~ /[;|&`$\n(){}<>]/) return ""
  if (t ~ /^echo[ \t]/) rest = substr(t, 5)
  else if (t ~ /^printf[ \t]/) rest = substr(t, 7)
  else return ""
  gsub(SQ, "", rest); gsub(DQ, "", rest)
  sub(/^[ \t]+/, "", rest); sub(/^-[neE]+[ \t]+/, "", rest)
  sub(/^[ \t]+/, "", rest); sub(/[ \t]+$/, "", rest)
  return rest
}
# Vị trí `)` đóng cho `$(` hoặc `(` mở tại p (đã qua dấu mở), tôn trọng nháy và lồng.
function parenend(p,    c, d) {
  d = 1
  while (p <= LEN) {
    c = substr(CMD, p, 1)
    if (c == SQ) { p = findat(SQ, p + 1); if (!p) return 0; p++; continue }
    if (c == DQ) { p = dqend(p + 1); if (!p) return 0; p++; continue }
    if (c == BS) { p += 2; continue }
    if (c == "(") d++
    else if (c == ")") { d--; if (d == 0) return p }
    p++
  }
  return 0
}
function injectwords(words, r,    m, a, k) {
  m = split(r, a, /[ \t]+/)
  for (k = 1; k <= m; k++) if (a[k] != "") pushw(words, a[k])
}
# PowerShell `Get-Command <tên>` / `gcm <tên>` → <tên> (đích của toán tử gọi `&`).
function psgetcmd(s,    t, low, m) {
  t = s; sub(/^[ \t]+/, "", t); sub(/[ \t]+$/, "", t); low = tolower(t)
  if (low ~ /^get-command[ \t]/) t = substr(t, 12)
  else if (low ~ /^gcm[ \t]/) t = substr(t, 5)
  else return ""
  sub(/^[ \t]+/, "", t); sub(/[ \t].*$/, "", t)
  gsub(SQ, "", t); gsub(DQ, "", t)
  return t
}
function pipeout(words, i, n,    j, s) {
  s = ""; j = i + 1
  while (j <= n && words[j] ~ /^-[neE]+$/) j++
  for (; j <= n; j++) s = s (s == "" ? "" : " ") words[j]
  return s
}
# Lệnh ghi có đối số dưới thư mục bảo vệ → chặn. Đối số bắt đầu bằng `-` bỏ qua.
function checkwrite(words, start, n,    j, w) {
  for (j = start; j <= n; j++) {
    w = words[j]
    if (w ~ /^-/) continue
    if (protpath(w)) { hit("write", w, joinw(words, 1, n)); return }
  }
}
# Lệnh `e` của GNU sed (chạy shell): địa chỉ rồi `e` rồi lệnh. Đặt EXC = lệnh trích
# được (chuỗi rỗng nghĩa là có `e`-exec nhưng không bóc được lệnh → chặn thẳng).
function sedexec(w,    m) {
  EXC = ""
  if (match(w, /(^|[;{}0-9$/])e[ \t]+/)) { EXC = substr(w, RSTART + RLENGTH); return 1 }
  if (w ~ /(^|[;{}0-9$/])e[ \t]*$/) return 1
  if (w ~ /\/e[;}]*$/) return 1
  return 0
}
# Chương trình awk: trích lệnh shell trong system("…"), print | "…", "…" | getline;
# và ghi tệp print > "đường-dẫn-bảo-vệ". Một lượt trái-sang-phải, tuyến tính.
function awkexec(prog, depth,    L, i, c, st, str, j, k, prevpipe, prevredir, sysnext) {
  L = length(prog); i = 1; prevpipe = 0; prevredir = 0; sysnext = 0
  while (i <= L) {
    c = substr(prog, i, 1)
    if (c == DQ) {
      st = i + 1; i++
      while (i <= L) { c = substr(prog, i, 1); if (c == BS) { i += 2; continue } if (c == DQ) break; i++ }
      str = substr(prog, st, i - st); i++
      if (sysnext || prevpipe) analyze(str, 0, depth + 1)
      else if (prevredir) { if (protpath(str)) hit("write", str, "awk print > " str) }
      else {
        j = i; while (j <= L && substr(prog, j, 1) ~ /[ \t]/) j++
        if (substr(prog, j, 1) == "|") { k = j + 1; while (k <= L && substr(prog, k, 1) ~ /[ \t]/) k++
          if (substr(prog, k, 7) == "getline") analyze(str, 0, depth + 1) }
      }
      sysnext = 0; prevpipe = 0; prevredir = 0; continue
    }
    if (c == "|") { prevpipe = 1; prevredir = 0; i++; continue }
    if (c == ">") { prevredir = 1; prevpipe = 0; i++; continue }
    if (c ~ /[ \t]/) { i++; continue }
    if (c == "s" && substr(prog, i, 6) == "system") { j = i + 6
      while (j <= L && substr(prog, j, 1) ~ /[ \t]/) j++
      if (substr(prog, j, 1) == "(") sysnext = 1; i = j; continue }
    prevpipe = 0; prevredir = 0; i++
  }
}

function evalseg(words, depth,    n, i, w, lw, cmdn, j, k, t, sub_, norm, envw, rv, sep, pin, eqp, nm, w1, script, daenv) {
  n = words[0]
  sep = SEG_SEP; SEG_SEP = ""
  pin = PIPEIN; PIPEIN = ""
  XARGS = 0
  if (n == 0) return

  # PowerShell: phép gán biến và biến môi trường ($env:) là câu lệnh riêng. Ghi giá
  # trị vào GV để `& $g …` sau đó giải được; $env:GIT_* thì chặn (§5).
  if (PSM) {
    w1 = words[1]
    if (w1 ~ /^\$env:/) {
      eqp = index(w1, "="); if (eqp) nm = substr(w1, 6, eqp - 6); else nm = substr(w1, 6)
      nm = toupper(nm)
      # Biến đổi cấu hình git mà lệnh git đọc sau đó — danh sách ở homevar().
      if (homevar(nm)) { hit("gitenv", "$env:" nm, joinw(words, 1, n)); return }
      return
    }
    if (w1 ~ /^\$[A-Za-z_][A-Za-z0-9_]*=/) { eqp = index(w1, "="); GV[substr(w1, 2, eqp - 2)] = substr(w1, eqp + 1); return }
    if (w1 ~ /^\$[A-Za-z_][A-Za-z0-9_]*$/ && n >= 3 && words[2] == "=") { GV[substr(w1, 2)] = words[3]; return }
  }

  i = 1; envw = ""; daenv = ""
  while (i <= n) {
    w = words[i]; lw = bname(w)
    if (w ~ /^[A-Za-z_][A-Za-z0-9_]*=/) { envw = w; eqp = index(w, "="); nm = substr(w, 1, eqp - 1); GV[nm] = substr(w, eqp + 1); if (homevar(nm)) daenv = w; i++; continue }
    if (lw == "rtk") { i++; if (i <= n && (words[i] == "proxy" || words[i] == "err" || words[i] == "test" || words[i] == "summary")) i++; continue }
    if (lw == "nohup" || lw == "builtin" || lw == "!" || lw == "if" || lw == "then" || lw == "else" || lw == "elif" || lw == "do" || lw == "while" || lw == "until" || lw == "coproc") { i++; continue }
    if (lw == "exec") { i++; while (i <= n && words[i] ~ /^-/) { if (words[i] == "-a") i++; i++ } continue }
    if (lw == "command" || lw == "time") { i++; while (i <= n && words[i] ~ /^-/) i++; continue }
    if (lw == "sudo") { i++; while (i <= n && words[i] ~ /^-/) { if (words[i] ~ /^-[ugCDhpRrTt]$/) i++; i++ } continue }
    if (lw == "env") {
      i++
      while (i <= n && (words[i] ~ /^-/ || words[i] ~ /^[A-Za-z_][A-Za-z0-9_]*=/)) {
        if (words[i] == "-S" || words[i] == "--split-string") { if (i < n) analyze(joinw(words, i + 1, n), PSM, depth + 1); return }
        if (words[i] ~ /^[A-Za-z_][A-Za-z0-9_]*=/) envw = words[i]
        if (words[i] == "-u" || words[i] == "-C" || words[i] == "--unset" || words[i] == "--chdir") i++
        i++
      }
      continue
    }
    if (lw == "timeout") { i++; while (i <= n && words[i] ~ /^-/) { if (words[i] == "-s" || words[i] == "-k" || words[i] == "--signal" || words[i] == "--kill-after") i++; i++ } if (i <= n) i++; continue }
    if (lw == "nice") { i++; while (i <= n && words[i] ~ /^-/) { if (words[i] == "-n" || words[i] == "--adjustment") i++; i++ } continue }
    # lệnh bọc khác (§3): setsid, stdbuf, winpty, watch — bóc tuỳ chọn rồi phân tích lệnh trong.
    if (lw == "setsid" || lw == "winpty") { i++; while (i <= n && words[i] ~ /^-/) i++; continue }
    if (lw == "stdbuf") { i++; while (i <= n && words[i] ~ /^-/) { if (words[i] ~ /^-[ioe]$/) i++; i++ } continue }
    if (lw == "watch") { i++; while (i <= n && words[i] ~ /^-/) { if (words[i] ~ /^-[ndt]$/ || words[i] == "--interval") i++; i++ } continue }
    if (lw == "flock") {
      i++
      while (i <= n && words[i] ~ /^-/) {
        if (words[i] == "-c" || words[i] == "--command") { i++; if (i <= n) analyze(words[i], PSM, depth + 1); return }
        if (words[i] ~ /^-[wE]$/ || words[i] == "--timeout" || words[i] == "--conflict-exit-code") i++
        i++
      }
      if (i <= n && words[i] !~ /^-/) i++
      if (i <= n && (words[i] == "-c" || words[i] == "--command")) { i++; if (i <= n) analyze(words[i], PSM, depth + 1); return }
      continue
    }
    if (lw == "xargs") { XARGS = 1; i++; while (i <= n && words[i] ~ /^-/) { if (words[i] ~ /^-[IiLlnPsdaE]$/) i++; i++ } continue }
    break
  }
  # Gán biến của homevar() (HOME=, XDG_CONFIG_HOME=, GIT_*=…) là CÂU RIÊNG (không có
  # lệnh theo sau): giá trị TỒN tại trong shell — HOME luôn được export — và đổi tệp
  # cấu hình git mà lệnh git đọc ở câu sau (lỗ 2026-09-30).
  # Gán đứng NGAY TRƯỚC một lệnh (HOME=/tmp git… hay HOME=/tmp npm…) chỉ áp cho đúng
  # lệnh đó: trước git thì nhánh env-trước-git đã chặn; trước lệnh khác thì phạm vi
  # gói gọn, không đổi cấu hình git về sau — cho qua (như LC_ALL=C sort), tránh chặn thừa.
  if (i > n) {
    if (daenv != "") { hit("gitenv", daenv, joinw(words, 1, n)); return }
    return
  }
  rv = resolvetok(words[i]); if (rv != "") words[i] = rv
  cmdn = bname(words[i])

  if (cmdn == "eval") { if (i < n) analyze(joinw(words, i + 1, n), PSM, depth + 1); return }
  if (PSM && (cmdn == "iex" || cmdn == "invoke-expression")) {
    if (i < n) analyze(joinw(words, i + 1, n), 1, depth + 1); else if (pin != "") analyze(pin, 1, depth + 1)
    return
  }
  if (cmdn == "bash" || cmdn == "sh" || cmdn == "zsh" || cmdn == "dash" || cmdn == "ksh") {
    script = 0
    for (j = i + 1; j <= n; j++) {
      if (words[j] ~ /^-[A-Za-z]*c[A-Za-z]*$/) { k = j + 1; while (k <= n && words[k] == "--") k++; if (k <= n) analyze(words[k], 0, depth + 1); return }
      if (words[j] == "--") { if (j < n) script = 1; break }
      if (words[j] !~ /^-/) { script = 1; break }
    }
    if (!script) {
      # trình thông dịch đọc lệnh từ stdin: ống dẫn (echo|bash), here-string, heredoc.
      if (pin != "") analyze(pin, 0, depth + 1)
      if (("hs" in words) && words["hs"] != "") analyze(words["hs"], 0, depth + 1)
      if (HDN[depth] > 0) { HDWANT[depth] = 1; HDWANTPS[depth] = 0 }
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
      } else if (words[j] == "-delete") { checkwrite(words, i + 1, n); return }
    }
  }

  # §5: export/declare một biến GIT_* ở câu lệnh riêng đổi hành vi git chạy sau đó.
  # §5: export/declare một biến của homevar() đổi cấu hình git đọc ở câu sau.
  if (cmdn == "export" || cmdn == "declare" || cmdn == "typeset" || cmdn == "readonly" || cmdn == "local") {
    for (j = i + 1; j <= n; j++) {
      w = words[j]; if (w ~ /^-/) continue
      if (index(w, "=")) { eqp = index(w, "="); nm = substr(w, 1, eqp - 1); GV[nm] = substr(w, eqp + 1); if (homevar(nm)) { hit("gitenv", nm, joinw(words, i, n)); return } }
      else if (homevar(w)) { hit("gitenv", w, joinw(words, i, n)); return }
    }
    return
  }
  # §5: setx (biến người dùng/hệ thống Windows, lưu bền cho tiến trình về sau) — tên
  # biến không phân biệt hoa thường.
  if (cmdn == "setx") {
    for (j = i + 1; j <= n; j++) { w = words[j]; if (w ~ /^[\/-]/) continue; if (homevar(toupper(w))) { hit("gitenv", w, joinw(words, i, n)); return } }
  }
  # §5: PowerShell ghi qua ổ Env: (Set-Item Env:HOME x, New-Item -Path Env: -Name HOME).
  # Chỉ cmdlet ghi; Get-Item/Get-ChildItem Env: là đọc, cho qua.
  if (PSM && (index(PWRITE, " " cmdn " ") || cmdn == "set-item" || cmdn == "si")) {
    t = 0
    for (j = i + 1; j <= n; j++) if (tolower(words[j]) ~ /^env:/) t = 1
    if (t) for (j = i + 1; j <= n; j++) {
      w = words[j]; if (w ~ /^-/) continue
      nm = toupper(w); sub(/^ENV:[\\\/]?/, "", nm)
      if (homevar(nm)) { hit("gitenv", "Env:" nm, joinw(words, i, n)); return }
    }
  }
  # §4: sed lệnh `e` (chạy shell) và sed -i (ghi tệp bảo vệ).
  if (cmdn == "sed") {
    for (j = i + 1; j <= n; j++) {
      w = words[j]; if (w == "-i" || w ~ /^-i/ || w ~ /^--in-place/) continue
      if (sedexec(w)) { if (EXC != "") analyze(EXC, 0, depth + 1); else hit("write", "sed e-command", joinw(words, i, n)); return }
    }
    for (j = i + 1; j <= n; j++) if (words[j] == "-i" || words[j] ~ /^-i/ || words[j] ~ /^--in-place/) { checkwrite(words, i + 1, n); break }
  }
  # §4: chương trình awk gọi system()/pipe/getline hay ghi tệp bảo vệ.
  if (cmdn == "awk" || cmdn == "gawk" || cmdn == "mawk") {
    for (j = i + 1; j <= n; j++) { w = words[j]; if (w ~ /^-/) continue; awkexec(w, depth) }
  }
  # §6: lệnh ghi tệp có đối số dưới thư mục bảo vệ (.claude/.state, .git).
  if (index(BWRITE, " " cmdn " ") || index(PWRITE, " " cmdn " ")) { checkwrite(words, i + 1, n); return }

  if (cmdn == "git" || cmdn ~ /^git-./) { gitseg(words, i, n, cmdn, envw); return }
  # echo|trình-thông-dịch: nhớ kết quả tĩnh của echo/printf cho đoạn kế qua ống dẫn.
  if (sep == "|" && (cmdn == "echo" || cmdn == "printf")) PIPEIN = pipeout(words, i, n)
  norm = cmdn
  for (j = i + 1; j <= n; j++) norm = norm " " words[j]
  if (k = denyhit(norm)) hit("deny", PFX[k], norm)
}

# Đoạn lệnh git: words[i] là `git` (hoặc `git-<lệnh>`), n là từ cuối.
function gitseg(words, i, n, cmdn, envw,    j, w, g, sub_, norm, k, d) {
  g = ""; j = i + 1
  if (cmdn == "git") {
    while (j <= n) {
      w = words[j]
      if (w == "-C") { if (g == "" && !samerepo(words[j + 1])) g = w " " words[j + 1]; j += 2; continue }
      if (w == "-c" || w == "--config-env" || w == "--git-dir" || w == "--work-tree") { if (g == "") g = w " " words[j + 1]; j += 2; continue }
      if (w ~ /^--(git-dir|work-tree|config-env|exec-path)=/ || w == "--bare") { if (g == "") g = w; j++; continue }
      if (w == "--namespace" || w == "--super-prefix" || w == "--attr-source" || w == "--list-cmds") { j += 2; continue }
      if (w ~ /^--(namespace|super-prefix|attr-source|list-cmds)=/) { j++; continue }
      if (w == "--no-pager" || w == "-p" || w == "--paginate" || w == "-P" || w == "--no-replace-objects" || w == "--literal-pathspecs" || w == "--glob-pathspecs" || w == "--noglob-pathspecs" || w == "--icase-pathspecs" || w == "--no-optional-locks" || w == "--no-lazy-fetch" || w == "--no-advice") { j++; continue }
      break
    }
    norm = "git"
  } else norm = "git " substr(cmdn, 5)
  for (k = j; k <= n; k++) norm = norm " " words[k]

  if (k = denyhit(norm)) { hit("deny", PFX[k], norm); return }
  if (envw != "") { hit("env", envw, joinw(words, i, n)); return }
  if (g != "") { hit("global", g, joinw(words, i, n)); return }
  # git dưới xargs không có lệnh con: đối số đến từ stdin, không biết trước — chặn (§4).
  if (norm == "git") { if (XARGS) hit("xargs", "git", joinw(words, i, n)); return }
  if (cmdn == "git") {
    sub_ = words[j]
    # tuỳ chọn thông tin: git in rồi thoát
    if (j == n && (sub_ == "--version" || sub_ == "-v" || sub_ == "--exec-path" || sub_ == "--html-path" || sub_ == "--man-path" || sub_ == "--info-path")) return
    k = j + 1
  } else { sub_ = substr(cmdn, 5); k = i + 1 }
  if (!allowhit(norm)) { hit("allow", "git " sub_, joinw(words, i, n)); return }
  for (; k <= n; k++) if ((d = gitdanger(words[k], sub_)) != "") { hit("option", d, joinw(words, i, n)); return }
}

BEGIN { RS = "\001"; SQ = "\047"; DQ = "\""; BS = "\\"; BT = "`"; HIT = ""; PERR = ""; NP = 0; NGA = 0
  PIPEIN = ""; SEG_SEP = ""; XARGS = 0; EXC = ""
  # Neo thư mục nhà cho gitcfg(): ~ · $HOME/${HOME} · $USERPROFILE/${USERPROFILE} ·
  # $env:HOME/$env:USERPROFILE · %USERPROFILE% · /<ổ>/users/<tên> · <ổ>:/users/<tên> ·
  # /home/<tên> · /users/<tên> · /root. Đã hạ chữ và đổi \ -> /.
  HOMERE = "^(~|\\$\\{?home\\}?|\\$\\{?userprofile\\}?|\\$env:home|\\$env:userprofile|%userprofile%|/[a-z]/users/[^/]+|[a-z]:/users/[^/]+|/home/[^/]+|/users/[^/]+|/root)"
  # Lệnh GHI tệp: có đối số là đường dẫn dưới thư mục bảo vệ thì chặn (§6). Bao hai
  # phía dấu cách để khớp nguyên từ. PowerShell dùng tên và bí danh của cmdlet.
  BWRITE = " touch rm rmdir mkdir mv cp tee dd truncate ln install shred chmod chown chgrp unlink mktemp rsync "
  PWRITE = " new-item remove-item set-content add-content clear-content out-file copy-item move-item rename-item set-itemproperty clear-item ni ri rd del erase sc ac clc cpi mi rni copy move ren "
}
FILENAME == SET { SETTXT = SETTXT $0; next }
{ PAYTXT = PAYTXT $0 }
END {
  if (!jparse(SETTXT)) { err("settings.json không phân tích được như JSON — không chặn gì"); exit 0 }
  for (j = 1; j <= C[".permissions.deny[]"]; j++) {
    d = V[".permissions.deny[]", j]
    if (d !~ /^Bash\(.*:\*\)$/) continue
    p = normsp(substr(d, 6, length(d) - 8))
    if (p == "") continue
    sp1 = index(p " ", " ")
    PFX[++NP] = tolower(substr(p, 1, sp1 - 1)) substr(p, sp1)
  }
  if (NP == 0) err("không trích được mục Bash(...:*) nào từ permissions.deny — chỉ còn danh sách cho phép git")
  # Danh sách cho phép git: mục Bash(git <lệnh con> …) có hoặc không có :*. Mục không
  # có lệnh con (Bash(git:*)) hay có * giữa chừng không được nhận — nhận thì cả danh
  # sách thành "mọi lệnh git".
  for (j = 1; j <= C[".permissions.allow[]"]; j++) {
    d = V[".permissions.allow[]", j]
    if (d !~ /^Bash\(git([ :]|\))/) continue
    p = substr(d, 6, length(d) - 6); ex = 1
    if (p ~ /:\*$/) { ex = 0; p = substr(p, 1, length(p) - 2) }
    p = normsp(p)
    if (p !~ /^git [^ ]/ || index(p, "*")) { err("mục allow " d " không nhận làm danh sách cho phép git"); continue }
    GA[++NGA] = p; GX[NGA] = ex
  }
  if (NGA == 0) err("không trích được mục Bash(git …) nào từ permissions.allow — mọi lệnh git bị chặn")
  delete V; delete C
  if (!jparse(PAYTXT)) err("payload không phân tích trọn vẹn như JSON")
  tool = V[".tool_name", 1]
  # Công cụ ghi tệp (matcher Edit|Write|NotebookEdit): đọc đường dẫn, chặn nếu nhắm
  # thư mục trạng thái harness (.claude/.state) hay .git. Lỗi đọc -> cho qua + log.
  if (tool == "Edit" || tool == "Write" || tool == "NotebookEdit" || tool == "MultiEdit") {
    fp = (C[".tool_input.file_path"] >= 1) ? V[".tool_input.file_path", 1] : ((C[".tool_input.notebook_path"] >= 1) ? V[".tool_input.notebook_path", 1] : "")
    if (fp == "") { err("payload " tool " không có file_path/notebook_path"); exit 0 }
    if (protpath(fp)) print "BLOCK\ttoolwrite\t" clip(fp) "\t" clip(tool " " fp)
    exit 0
  }
  if (C[".tool_input.command"] < 1) { err("payload không có tool_input.command (tool_name=" tool ")"); exit 0 }
  analyze(V[".tool_input.command", 1], (tool == "PowerShell") ? 1 : 0, 0)
  if (PERR != "") err(PERR " — lệnh: " clip(V[".tool_input.command", 1]))
  if (HIT != "") print "BLOCK\t" HIT "\t" clip(HITP) "\t" clip(HITSEG)
  exit 0
}
'

out=$(LC_ALL=C awk -v SET="$SETTINGS" -v ROOTU="$ROOTU" -v ROOTW="$ROOTW" "$AWKPROG" "$SETTINGS" - 2>&1)
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

kind="${blockline%%$TAB*}"; rest="${blockline#*$TAB}"
det="${rest%%$TAB*}"; seg="${rest#*$TAB}"
case "$kind" in
  deny)   why="Khớp mục cấm: Bash($det:*) trong permissions.deny của .claude/settings.json." ;;
  allow)  why="Lệnh '$det' không có trong danh sách cho phép git — các mục Bash(git …) trong permissions.allow của .claude/settings.json, bảng ở .claude/CLAUDE.md §1. Lệnh đọc chưa có tên: xin người dùng thêm vào danh sách, hoặc để người dùng tự chạy." ;;
  option) why="Tuỳ chọn '$det' ghi tệp hoặc chạy chương trình ngoài — bị chặn cả khi lệnh git được phép (.claude/CLAUDE.md §1)." ;;
  global) why="Tuỳ chọn toàn cục '$det' đổi cấu hình git hoặc trỏ git sang repo khác gốc repo — bị chặn cả khi lệnh con được phép (.claude/CLAUDE.md §1)." ;;
  env)    why="Phép gán '$det' đứng ngay trước git có thể đổi cấu hình hoặc repo của git — bỏ phép gán rồi chạy lại (.claude/CLAUDE.md §1)." ;;
  gitenv) why="Biến môi trường '$det' đổi cấu hình hoặc repo cho lệnh git 'được phép' chạy sau đó — bỏ nó đi (.claude/CLAUDE.md §1)." ;;
  xargs)  why="git dưới xargs không có lệnh con: đối số tới từ stdin, không biết trước — có thể là lệnh ghi. Nêu rõ lệnh con git (đọc) hoặc để người dùng chạy (.claude/CLAUDE.md §1)." ;;
  write)  why="Lệnh ghi/xoá nhắm vùng bảo vệ — thư mục trạng thái harness (.claude/.state), .git, hoặc tệp cấu hình git toàn cục (~/.gitconfig, ~/.config/git/, \$XDG_CONFIG_HOME/git/) — đích '$det'. Agent không tạo/sửa/xoá gì trong đó (.claude/CLAUDE.md §1, §8). Lệnh chỉ ĐỌC (ls, cat, grep) thì được." ;;
  toolwrite) why="Ghi vào '$det' — thuộc vùng bảo vệ (.claude/.state, .git, hoặc tệp cấu hình git toàn cục); agent không tạo/sửa/xoá gì trong đó (.claude/CLAUDE.md §1, §8)." ;;
  *)      why="Lý do: $kind $det." ;;
esac
printf '%s\n' \
  "Lệnh bị chặn bởi hook PreToolUse (.claude/hooks/on-pretool.sh)." \
  "" \
  "Đoạn lệnh: $seg" \
  "$why" \
  "" \
  "Lệnh git ghi và lệnh phá huỷ là việc của người dùng (.claude/CLAUDE.md §1). Dừng lại và nói rõ cần chạy lệnh gì; người dùng tự chạy rồi bảo tiếp tục. Đừng viết lại lệnh cho lọt qua hook." >&2
exit 2
