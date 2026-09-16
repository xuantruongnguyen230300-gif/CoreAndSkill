#!/usr/bin/env bash
# run-tests.sh — bộ test payload cho các hook ở .claude/hooks/.
#
# Chạy từ gốc repo:
#     bash .claude/hooks/tests/run-tests.sh        # in ca SAI + tổng
#     bash .claude/hooks/tests/run-tests.sh -v     # in mọi ca
#
# Thoát 0 = mọi ca OK · 1 = có ca SAI · 2 = không chạy được (thiếu node/git).
#
# Mỗi nhóm dựng một repo giả trong thư mục tạm của hệ thống (mktemp -d), chép
# hook vào đó rồi gọi hook bằng payload JSON giống harness gửi. KHÔNG chạm repo
# thật, không đọc gì ngoài .claude/hooks/*.sh và .claude/settings.json; thư mục
# tạm bị xoá khi script thoát.
#
# Thử bản hook ứng viên TRƯỚC khi thay file thật:
#     HOOKS_UNDER_TEST=<thư-mục-chứa-*.sh> bash .claude/hooks/tests/run-tests.sh
#
# Nguồn cấu hình trong repo giả:
#   - on-pretool đọc danh sách cấm từ `permissions.deny` của settings.json THẬT
#     (ca "phải chặn" kiểm chính cấu hình đang dùng, không kiểm một bản chép);
#   - on-stop cần hai dạng: "chưa gắn SubagentStop" và "đã gắn" — dựng từ
#     settings.json thật bằng cách gỡ / thêm hai khối PreToolUse, SubagentStop;
#   - tài liệu chủ khối core-paths và .gitignore là FIXTURE viết ngay trong file
#     này, để test không đổi kết quả khi tài liệu thật đang được sửa.
#
# Chạy được trên Git Bash (Windows) và Linux (CI). Ca đường dẫn Windows dùng
# `pwd -W` khi có; không có thì dùng đường dẫn POSIX tuyệt đối tương đương.

set -u
export LC_ALL=en_US.UTF-8

VERBOSE=0
[ "${1:-}" = "-v" ] && VERBOSE=1

T="$(cd "$(dirname "$0")" && pwd)"
REPO="$(cd "$T/../../.." && pwd)"
HOOKS="${HOOKS_UNDER_TEST:-$REPO/.claude/hooks}"
REALSET="$REPO/.claude/settings.json"

for need in node git; do
  command -v "$need" >/dev/null 2>&1 || { echo "ABORT — thiếu '$need' trên PATH; bộ test cần nó để dựng payload JSON / repo giả." >&2; exit 2; }
done
for f in core-paths.sh on-edit.sh on-stop.sh on-subagent-stop.sh on-pretool.sh; do
  [ -f "$HOOKS/$f" ] || { echo "ABORT — không thấy $HOOKS/$f" >&2; exit 2; }
done
[ -f "$REALSET" ] || { echo "ABORT — không thấy $REALSET" >&2; exit 2; }

TMPROOT="$(mktemp -d 2>/dev/null)" || { echo "ABORT — mktemp -d thất bại" >&2; exit 2; }
[ -n "$TMPROOT" ] && [ -d "$TMPROOT" ] || { echo "ABORT — mktemp -d không trả thư mục" >&2; exit 2; }
trap 'rm -rf "$TMPROOT"' EXIT
trap 'exit 2' INT TERM
WORK="$TMPROOT/work"
mkdir -p "$WORK"
REPORT="$TMPROOT/report.md"
: > "$REPORT"
NPASS=0; NFAIL=0
declare -A GROUPN=()

row() { # nhóm | ca | kỳ vọng | thực tế
  local ok="OK"
  if [ "$3" = "$4" ]; then NPASS=$((NPASS+1)); else ok="**SAI**"; NFAIL=$((NFAIL+1)); fi
  GROUPN[$1]=$(( ${GROUPN[$1]:-0} + 1 ))
  printf '| %s | %s | %s | %s | %s |\n' "$1" "$2" "$3" "$4" "$ok" >> "$REPORT"
}
header() { printf '\n## %s\n\n| Nhóm | Ca | Kỳ vọng | Thực tế | KQ |\n| --- | --- | --- | --- | --- |\n' "$1" >> "$REPORT"; }

# ---------------------------------------------------------------- cấu hình dựng từ settings.json thật
SOFTSET="$WORK/settings.soft.json"
HARDSET="$WORK/settings.hard.json"
node -e '
  const fs = require("fs");
  const [src, soft, hard] = process.argv.slice(1);
  const o = JSON.parse(fs.readFileSync(src, "utf8"));
  o.hooks = o.hooks || {};
  delete o.hooks.PreToolUse; delete o.hooks.SubagentStop;
  fs.writeFileSync(soft, JSON.stringify(o, null, 2) + "\n");
  o.hooks.PreToolUse = [{ matcher: "Bash|PowerShell", hooks: [{ type: "command", command: "bash .claude/hooks/on-pretool.sh", timeout: 10 }] }];
  o.hooks.SubagentStop = [{ matcher: "^core-reviewer$", hooks: [{ type: "command", command: "bash .claude/hooks/on-subagent-stop.sh", timeout: 10 }] }];
  fs.writeFileSync(hard, JSON.stringify(o, null, 2) + "\n");
' "$REALSET" "$SOFTSET" "$HARDSET" || { echo "ABORT — settings.json không phân tích được như JSON" >&2; exit 2; }

fixture_gitignore() {
cat <<'EOF'
bin/
obj/
dist/
node_modules/
.angular/
.claude/.state/
EOF
}

fixture_doc() {
cat <<'EOF'
# Kiến trúc Core và Module

Văn xuôi có ```khối rào ngoài mốc``` không bị đọc.

```
src/KHONG/DUOC/DOC/
```

## Đường dẫn chạm Core

<!-- core-paths:begin -->
```
# Backend
src/BE/Core/
src/BE/CoreAndSkill.Api/
src/BE/Tests/CoreAndSkill.ArchTests/

# Frontend
src/FE/src/app/core/
src/FE/src/app/shared/
src/FE/src/app/platform/
src/FE/eslint.config.js
src/FE/eslint.boundaries.cjs

# Tài liệu kích hoạt review
docs/quy-uoc/
docs/RULES.md
docs/OWNERSHIP.md
docs/kien-truc-core-module.md
```
<!-- core-paths:end -->

Hết.
EOF
}

mkrepo() { # $1 tên; in đường dẫn repo giả
  local R="$WORK/$1"
  mkdir -p "$R/.claude/hooks" "$R/docs"
  cp "$HOOKS"/*.sh "$R/.claude/hooks/"
  cp "$SOFTSET" "$R/.claude/settings.json"
  fixture_gitignore > "$R/.gitignore"
  cat > "$R/.claude/check-docs.sh" <<'EOF'
#!/usr/bin/env bash
cd "$(dirname "$0")/.." || exit 2
rc=$(cat .stub-rc 2>/dev/null || echo 0)
[ "$rc" = 1 ] && echo "  FAIL  docs/x.md:1 — vi phạm giả \"có nháy\""
[ "$rc" = 2 ] && echo "ABORT — giả lập"
exit "$rc"
EOF
  fixture_doc > "$R/docs/kien-truc-core-module.md"
  ( cd "$R" && git init -q . >/dev/null 2>&1 )
  printf '%s' "$R"
}

# payload JSON dựng bằng node — đúng cách harness tuần tự hoá, kể cả gạch chéo ngược.
# pj <event> <tool> <key> <file-chứa-giá-trị> [tool_response-file]
pj() {
  node -e '
    const fs = require("fs");
    const [ev, tool, key, vf, rf] = process.argv.slice(1);
    const o = { session_id: "s1", transcript_path: "C:\\Users\\x\\t.jsonl", cwd: "D:\\Repo",
                permission_mode: "default", hook_event_name: ev, tool_name: tool, tool_input: {} };
    o.tool_input[key] = fs.readFileSync(vf, "utf8");
    if (key === "command") o.tool_input.description = "mô tả \"command\": \"git push\"";
    if (rf) o.tool_response = { stdout: fs.readFileSync(rf, "utf8") };
    process.stdout.write(JSON.stringify(o));
  ' "$@"
}
valf() { printf '%s' "$1" > "$WORK/v.tmp"; printf '%s' "$WORK/v.tmp"; }

isjson() { # stdin -> "json" | "khong-json" | "rong"
  local s; s=$(cat)
  [ -z "$s" ] && { echo rong; return; }
  printf '%s' "$s" > "$WORK/j.tmp"
  node -e 'try{JSON.parse(require("fs").readFileSync(process.argv[1],"utf8"));console.log("json")}catch(e){console.log("khong-json")}' "$WORK/j.tmp"
}
jfield() { # $1 file json, $2 field -> giá trị
  node -e 'try{const o=JSON.parse(require("fs").readFileSync(process.argv[1],"utf8"));const v=o[process.argv[2]];console.log(v===undefined?"":String(v))}catch(e){console.log("")}' "$1" "$2"
}

###############################################################################
header core-paths.sh
R=$(mkrepo cp)
cpt() { # tên, nội dung doc, rc kỳ vọng
  printf '%s' "$2" > "$R/docs/fx.md"
  bash "$R/.claude/hooks/core-paths.sh" docs/fx.md > "$WORK/cp.out" 2> "$WORK/cp.err"
  local rc=$?
  local lines; lines=$(grep -c . "$WORK/cp.out")
  # Chỉ đếm dòng lý do của core-paths — cảnh báo locale của shell (nếu có) không tính.
  local errl; errl=$(grep -c '^core-paths:' "$WORK/cp.err")
  row core-paths "$1" "rc=$3" "rc=$rc"
  if [ "$3" -ne 0 ]; then row core-paths "$1 — có lý do ở stderr, stdout rỗng" "err=1 out=0" "err=$errl out=$lines"; fi
}
bash "$R/.claude/hooks/core-paths.sh" > "$WORK/cp.out" 2>/dev/null; rc=$?
row core-paths "khối hợp lệ (tài liệu mặc định)" "rc=0 / 12 dòng" "rc=$rc / $(grep -c . "$WORK/cp.out") dòng"
row core-paths "không lọt khối rào ngoài mốc" "0" "$(grep -c 'KHONG' "$WORK/cp.out")"
row core-paths "dòng đầu / dòng cuối" "src/BE/Core/ | docs/kien-truc-core-module.md" "$(head -1 "$WORK/cp.out") | $(tail -1 "$WORK/cp.out")"
fixture_doc | sed 's/$/\r/' > "$R/docs/crlf.md"
bash "$R/.claude/hooks/core-paths.sh" docs/crlf.md > "$WORK/cp2.out" 2>/dev/null; rc=$?
row core-paths "tài liệu CRLF" "rc=0 / giống bản LF" "rc=$rc / $(cmp -s "$WORK/cp.out" "$WORK/cp2.out" && echo giống bản LF || echo khác)"
( cd "$WORK" && bash "$R/.claude/hooks/core-paths.sh" > "$WORK/cp3.out" 2>/dev/null ); rc=$?
row core-paths "gọi từ thư mục khác" "rc=0" "rc=$rc"
bash "$R/.claude/hooks/core-paths.sh" docs/khong-co.md >/dev/null 2>&1; rc=$?
row core-paths "không thấy tài liệu" "rc=3" "rc=$rc"
cpt "không có mốc" $'# x\n```\nsrc/a/\n```\n' 4
cpt "end trước begin" $'<!-- core-paths:end -->\n<!-- core-paths:begin -->\n```\nsrc/a/\n```\n' 4
cpt "hai mốc begin" $'<!-- core-paths:begin -->\n```\nsrc/a/\n```\n<!-- core-paths:end -->\n<!-- core-paths:begin -->\n' 4
cpt "thiếu mốc end" $'<!-- core-paths:begin -->\n```\nsrc/a/\n```\n' 4
cpt "khối rào gắn ngôn ngữ" $'<!-- core-paths:begin -->\n```text\nsrc/a/\n```\n<!-- core-paths:end -->\n' 5
cpt "khối rào chưa đóng" $'<!-- core-paths:begin -->\n```\nsrc/a/\n<!-- core-paths:end -->\n' 5
cpt "chữ ngoài khối rào" $'<!-- core-paths:begin -->\nsrc/lot/\n```\nsrc/a/\n```\n<!-- core-paths:end -->\n' 5
cpt "hai khối rào" $'<!-- core-paths:begin -->\n```\nsrc/a/\n```\n```\nsrc/b/\n```\n<!-- core-paths:end -->\n' 5
cpt "không có khối rào" $'<!-- core-paths:begin -->\n\n<!-- core-paths:end -->\n' 5
cpt "khối chỉ có chú thích" $'<!-- core-paths:begin -->\n```\n# chưa có\n\n```\n<!-- core-paths:end -->\n' 6
cpt "đường dẫn tuyệt đối" $'<!-- core-paths:begin -->\n```\n/src/a/\n```\n<!-- core-paths:end -->\n' 7
cpt "đường dẫn có .." $'<!-- core-paths:begin -->\n```\nsrc/../a/\n```\n<!-- core-paths:end -->\n' 7
cpt "đường dẫn có dấu cách" $'<!-- core-paths:begin -->\n```\nsrc/a b/\n```\n<!-- core-paths:end -->\n' 7
cpt "đường dẫn Windows" $'<!-- core-paths:begin -->\n```\nsrc\\\\a\\\\\n```\n<!-- core-paths:end -->\n' 7
cpt "khoảng trắng quanh mốc và dòng" $'  <!--  core-paths:begin  -->\n```\n   src/a/   \n```\n<!-- core-paths:end -->\n' 0

###############################################################################
header on-edit.sh
R=$(mkrepo ed)
RU=$(cd "$R" && pwd)                     # /c/Users/.../ed  hoặc  /tmp/.../ed
if RW=$(cd "$R" && pwd -W 2>/dev/null) && [ -n "$RW" ]; then :; else RW="$RU"; fi
RB=${RW//\//\\}                          # C:\Users\...\ed  (Linux: \tmp\...\ed — hook tự đổi gạch chéo)
S="$R/.claude/.state"
st() { # trạng thái dấu: changed/src/core
  printf 'changed=%s src=%s core=%s' "$([ -e "$S/changed" ] && echo 1 || echo 0)" "$([ -e "$S/src-touched" ] && echo 1 || echo 0)" "$([ -s "$S/core-touched" ] && echo 1 || echo 0)"
}
reset_state() { rm -rf "$S"; }
edt() { # tên, tool, key, giá trị, kỳ vọng [tool_response]
  reset_state
  local rf=""
  if [ -n "${6:-}" ]; then printf '%s' "$6" > "$WORK/r.tmp"; rf="$WORK/r.tmp"; fi
  pj PostToolUse "$2" "$3" "$(valf "$4")" $rf | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null
  local rc=$?
  row on-edit "$1" "$5 rc=0" "$(st) rc=$rc"
}
edt "không src — Edit tuyệt đối gạch chéo ngược docs/" Edit file_path "$RB\\docs\\x.md" "changed=1 src=0 core=0"
edt "không src — Write tương đối docs/" Write file_path "docs/x.md" "changed=1 src=0 core=0"
edt "không src — Write ./spec/ tương đối" Write file_path "./spec/a/business-rules.md" "changed=1 src=0 core=0"
edt "không src — gạch chéo xuôi tuyệt đối .claude/" Edit file_path "$RW/.claude/agents/a.md" "changed=1 src=0 core=0"
edt "không src — dạng POSIX tuyệt đối docs/" Edit file_path "$RU/docs/x.md" "changed=1 src=0 core=0"
LOWW=$(printf '%s' "$RW" | tr 'A-Z' 'a-z')
edt "không src — chữ thường toàn bộ" Edit file_path "$LOWW/docs/x.md" "changed=1 src=0 core=0"
edt "không src — gạch chéo gộp đôi" Edit file_path "${RW//\//\/\/}//docs//x.md" "changed=1 src=0 core=0"
edt "không src — README.md gốc (ngoài khu)" Edit file_path "$RB\\README.md" "changed=0 src=0 core=0"
edt "không src — file bị gitignore .claude/.state/" Write file_path "$RB\\.claude\\.state\\x" "changed=0 src=0 core=0"
edt "không src — ngoài repo nhưng có \\docs\\ (hướng an toàn)" Edit file_path "D:\\Khac\\docs\\x.md" "changed=1 src=0 core=0"
edt "không src — ngoài repo, không có khu" Edit file_path "D:\\Khac\\ghi-chu.md" "changed=0 src=0 core=0"
edt "không src — NotebookEdit notebook_path" NotebookEdit notebook_path "$RB\\spec\\a.ipynb" "changed=1 src=0 core=0"
edt "không src — Bash, tool_response chứa \"file_path\"" Bash command "ls" "changed=1 src=0 core=0" '"file_path": "docs/evil.md" "notebook_path":"x"'
edt "không src — PowerShell command" PowerShell command "Get-ChildItem" "changed=1 src=0 core=0"
reset_state
printf '{"session_id":"s","tool_name":"Read","tool_input":{"pattern":"x"}}' | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null; rc=$?
row on-edit "không src — payload không file_path, không command" "changed=0 src=0 core=0 rc=0" "$(st) rc=$rc"

mkdir -p "$R/src/BE/Core/Core.Domain" "$R/src/BE/Core/obj" "$R/src/BE/Modules/A" "$R/src/FE/src/app/core" "$R/src/FE/node_modules/pkg" "$R/docs/quy-uoc"
touch -d '2 hours ago' "$R/docs/kien-truc-core-module.md"
edt "có src — Edit Core BE" Edit file_path "$RB\\src\\BE\\Core\\Core.Domain\\X.cs" "changed=1 src=1 core=1"
row on-edit "có src — core-touched ghi đường dẫn tương đối" "src/BE/Core/Core.Domain/X.cs" "$(cat "$S/core-touched" 2>/dev/null)"
edt "có src — Edit module (không Core)" Edit file_path "$RB\\src\\BE\\Modules\\A\\X.cs" "changed=1 src=1 core=0"
edt "có src — Edit docs/quy-uoc/ (Core, không src)" Edit file_path "$RB\\docs\\quy-uoc\\be-x.md" "changed=1 src=0 core=1"
edt "có src — Edit file bị gitignore trong Core (obj/)" Write file_path "$RB\\src\\BE\\Core\\obj\\x.json" "changed=0 src=0 core=0"
edt "có src — mục tệp khớp đúng tên" Write file_path "src/FE/eslint.config.js" "changed=1 src=1 core=1"
edt "có src — tên gần giống mục tệp thì không khớp" Write file_path "src/FE/eslint.config.jsx" "changed=1 src=1 core=0"
edt "có src — Edit chính tài liệu chủ" Edit file_path "$RB\\docs\\kien-truc-core-module.md" "changed=1 src=0 core=1"

reset_state
touch -d '2 hours ago' "$R/docs/kien-truc-core-module.md" "$R/.gitignore"
find "$R/src" -type f -exec touch -d '2 hours ago' {} +
printf 'a' > "$R/src/FE/src/app/core/a.ts"
printf 'b' > "$R/src/BE/Core/obj/gen.json"
printf 'c' > "$R/src/FE/node_modules/pkg/i.js"
pj PostToolUse Bash command "$(valf 'echo x > src/FE/src/app/core/a.ts')" | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null; rc=$?
row on-edit "có src — Bash vừa sửa Core FE + file gitignore" "changed=1 src=1 core=1 rc=0" "$(st) rc=$rc"
row on-edit "có src — Bash: core-touched chỉ có file không bị gitignore" "src/FE/src/app/core/a.ts" "$(tr '\n' ' ' < "$S/core-touched" 2>/dev/null | sed 's/ $//')"

reset_state
find "$R/src" "$R/docs" -type f -exec touch -d '2 hours ago' {} +
pj PostToolUse Bash command "$(valf 'git status')" | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null; rc=$?
row on-edit "có src — Bash, không file nào vừa đổi" "changed=1 src=0 core=0 rc=0" "$(st) rc=$rc"

reset_state
printf 'x' > "$R/src/BE/Modules/A/Y.cs"
pj PostToolUse Bash command "$(valf 'dotnet build')" | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null; rc=$?
row on-edit "có src — Bash vừa sửa module" "changed=1 src=1 core=0 rc=0" "$(st) rc=$rc"

reset_state
cp "$R/docs/kien-truc-core-module.md" "$WORK/doc.bak"
printf '# không còn khối\n' > "$R/docs/kien-truc-core-module.md"
edt "có src — khối Core mất: vẫn ghi changed/src" Edit file_path "$RB\\src\\BE\\Core\\Core.Domain\\X.cs" "changed=1 src=1 core=0"
row on-edit "có src — khối Core mất: ghi lý do vào core-paths-error" "có lý do" "$([ -s "$S/core-paths-error" ] && echo 'có lý do' || echo 'không')"
cp "$WORK/doc.bak" "$R/docs/kien-truc-core-module.md"
edt "có src — khối sửa lại: dấu lỗi bị xoá" Edit file_path "$RB\\src\\BE\\Core\\Core.Domain\\X.cs" "changed=1 src=1 core=1"
row on-edit "có src — khối sửa lại: không còn core-paths-error" "không" "$([ -e "$S/core-paths-error" ] && echo có || echo không)"

rm -rf "$R/src"
reset_state
printf 'x' > "$R/docs/quy-uoc/be-y.md"
pj PostToolUse Bash command "$(valf 'ls')" | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null; rc=$?
row on-edit "không src — Bash vừa sửa docs/quy-uoc/ (không đòi review)" "changed=1 src=0 core=0 rc=0" "$(st) rc=$rc"

###############################################################################
header on-stop.sh
R=$(mkrepo stop)
S="$R/.claude/.state"
soft() { cp "$SOFTSET" "$R/.claude/settings.json"; }
hard() { cp "$HARDSET" "$R/.claude/settings.json"; }
runstop() { # $1 = 0|1 stop_hook_active -> ghi out vào $WORK/stop.out
  printf '{"session_id":"s","hook_event_name":"Stop","stop_hook_active":%s}' "$([ "$1" = 1 ] && echo true || echo false)" \
    | bash "$R/.claude/hooks/on-stop.sh" > "$WORK/stop.out" 2> "$WORK/stop.err"
}
kind() { # phân loại đầu ra
  local j; j=$(isjson < "$WORK/stop.out")
  if [ "$j" = rong ]; then echo "im lặng"; return; fi
  if [ "$j" != json ]; then echo "JSON HỎNG"; return; fi
  local d; d=$(jfield "$WORK/stop.out" decision)
  if [ "$d" = block ]; then echo "block"; else [ -n "$(jfield "$WORK/stop.out" systemMessage)" ] && echo "systemMessage" || echo "json khác"; fi
}
has() { grep -q -- "$1" "$WORK/stop.out" && echo có || echo không; }
clean() { rm -rf "$S"; mkdir -p "$S"; rm -f "$R/.stub-rc"; }

soft; clean
runstop 0; row on-stop "không có dấu changed" "im lặng rc=0" "$(kind) rc=$?"
clean; : > "$S/changed"; echo 1 > "$R/.stub-rc"
runstop 0; rc=$?; row on-stop "cổng đỏ (rc=1)" "block rc=0" "$(kind) rc=$rc"
row on-stop "cổng đỏ — giữ dấu changed" "còn" "$([ -e "$S/changed" ] && echo còn || echo mất)"
runstop 1; row on-stop "cổng đỏ, stop_hook_active — vẫn chặn" "block" "$(kind)"
clean; : > "$S/changed"; echo 2 > "$R/.stub-rc"
runstop 0; row on-stop "cổng không chạy được (rc=2)" "block" "$(kind)"
runstop 1; row on-stop "rc=2 lần hai — thả, xoá changed" "im lặng / mất" "$(kind) / $([ -e "$S/changed" ] && echo còn || echo mất)"

clean; : > "$S/changed"; printf 'docs/quy-uoc/a.md\n' > "$S/core-touched"
runstop 0; row on-stop "không src, có core-touched — không nhắc" "im lặng / mất" "$(kind) / $([ -e "$S/core-touched" ] && echo còn || echo mất)"

mkdir -p "$R/src/BE/Core"; printf 'x' > "$R/src/BE/Core/A.cs"
clean; : > "$S/changed"; printf 'src/BE/Core/A.cs\n' > "$S/core-touched"
runstop 0; row on-stop "có src, CHƯA gắn SubagentStop — nhắc một lần" "block" "$(kind)"
row on-stop "nhắc một lần — nêu core-reviewer, không mời 'nói lý do'" "có/không" "$(has core-reviewer)/$(has 'lý do rồi kết thúc')"
row on-stop "nhắc một lần — xoá dấu" "mất" "$([ -e "$S/core-touched" ] && echo còn || echo mất)"
: > "$S/changed"; runstop 1; row on-stop "nhắc một lần — lần Stop sau thả" "im lặng" "$(kind)"

hard; clean; : > "$S/changed"; printf 'src/BE/Core/A.cs\n' > "$S/core-touched"
runstop 0; row on-stop "ĐÃ gắn SubagentStop, chưa có dấu review — chặn" "block" "$(kind)"
row on-stop "chặn cứng — nêu lối thoát của người dùng" "có" "$(has 'người dùng tự xoá')"
runstop 1; row on-stop "chặn cứng, stop_hook_active — vẫn chặn" "block" "$(kind)"
row on-stop "chặn cứng — giữ core-touched" "còn" "$([ -e "$S/core-touched" ] && echo còn || echo mất)"
touch -d '1 minute ago' "$R/src/BE/Core/A.cs"; : > "$S/core-reviewed"
runstop 1; row on-stop "dấu review mới hơn file — thả" "im lặng" "$(kind)"
row on-stop "dấu review mới hơn — xoá core-touched" "mất" "$([ -e "$S/core-touched" ] && echo còn || echo mất)"

clean; : > "$S/changed"; : > "$S/core-reviewed"; printf 'y' > "$R/src/BE/Core/A.cs"
printf 'src/BE/Core/A.cs\n' > "$S/core-touched"
runstop 0; row on-stop "sửa ngay sau dấu review (không ngủ) — chặn" "block" "$(kind)"

clean; : > "$S/changed"; touch -d '10 minutes ago' "$R/src/BE/Core/A.cs"; : > "$S/core-reviewed"
printf 'z' > "$R/src/BE/Core/B.cs"
printf 'src/BE/Core/A.cs\nsrc/BE/Core/B.cs\n' > "$S/core-touched"
runstop 0; row on-stop "hai file, chỉ một mới hơn dấu — chỉ liệt kê file đó" "block / B có / A không" "$(kind) / B $(has 'B.cs') / A $(has 'A.cs')"

clean; : > "$S/changed"; : > "$S/core-reviewed"; sleep 1
printf 'src/BE/Core/DaXoa.cs\n' > "$S/core-touched"
runstop 0; row on-stop "file đã xoá sau dấu review — chặn" "block" "$(kind)"
printf 'loi\n' > "$S/subagent-stop-error.log"
runstop 1; row on-stop "chặn cứng kèm log SubagentStop — trỏ tới log" "có" "$(has 'subagent-stop-error.log')"

soft; clean; : > "$S/changed"; : > "$S/src-touched"
runstop 0; row on-stop "chỉ sửa src/ — nhắc không chặn" "systemMessage" "$(kind)"
row on-stop "nhắc src/ — xoá src-touched và changed" "mất/mất" "$([ -e "$S/src-touched" ] && echo còn || echo mất)/$([ -e "$S/changed" ] && echo còn || echo mất)"

clean; : > "$S/changed"; : > "$S/src-touched"; printf 'src/BE/Core/A.cs\n' > "$S/core-touched"
runstop 0; row on-stop "chạm Core + sửa src/ — lời nhắc gộp vào lý do chặn" "block / có" "$(kind) / $(has 'Hook KHÔNG chạy cổng build/test')"

clean; : > "$S/changed"; printf 'core-paths: x: không thấy mốc\n' > "$S/core-paths-error"
runstop 0; row on-stop "khối core-paths lỗi — báo" "block" "$(kind)"
: > "$S/changed"; runstop 0; row on-stop "khối core-paths lỗi — không báo lại khi lỗi không mới" "im lặng" "$(kind)"
clean; : > "$S/changed"; printf 'core-paths: x\n' > "$S/core-paths-error"
runstop 1; row on-stop "khối core-paths lỗi, stop_hook_active — không chặn" "im lặng" "$(kind)"

rm -rf "$R/src"; hard; clean; : > "$S/changed"; printf 'src/BE/Core/A.cs\n' > "$S/core-touched"; printf 'e\n' > "$S/core-paths-error"
runstop 0; row on-stop "KHÔNG có src/, đã gắn SubagentStop — lớp 3 không bật" "im lặng" "$(kind)"

# JSON thật với lý do nhiều dòng, nháy, gạch chéo ngược, tab, CRLF
mkdir -p "$R/src/BE/Core"; soft; clean; : > "$S/changed"
printf 'src/BE/Core/"nhay"\\x\tTab.cs\r\n' > "$S/core-touched"
runstop 0; row on-stop "ký tự khó trong lý do (nháy, \\\\, tab, CR)" "block" "$(kind)"

###############################################################################
header on-subagent-stop.sh
R=$(mkrepo sub)
S="$R/.claude/.state"
sst() { rm -rf "$S"; printf '%s' "$2" | bash "$R/.claude/hooks/on-subagent-stop.sh" 2>/dev/null; local rc=$?
  row on-subagent-stop "$1" "$3" "$([ -e "$S/core-reviewed" ] && echo dấu || echo 'không dấu') log=$([ -s "$S/subagent-stop-error.log" ] && echo 1 || echo 0) rc=$rc"; }
sst "agent_type core-reviewer" '{"session_id":"s","hook_event_name":"SubagentStop","agent_type":"core-reviewer","stop_hook_active":false}' "dấu log=0 rc=0"
sst "agent_type backend-expert" '{"session_id":"s","hook_event_name":"SubagentStop","agent_type":"backend-expert"}' "không dấu log=0 rc=0"
sst "không có agent_type" '{"session_id":"s","hook_event_name":"SubagentStop","agent_id":"a1"}' "không dấu log=1 rc=0"
sst "báo cáo chứa chuỗi agent_type giả" '{"agent_type":"backend-expert","last_assistant_message":"\"agent_type\":\"core-reviewer\""}' "không dấu log=0 rc=0"
sst "stdin rỗng" '' "không dấu log=1 rc=0"

###############################################################################
header on-pretool.sh
R=$(mkrepo pre)
S="$R/.claude/.state"
pt() { # tên, tool, lệnh, PASS|BLOCK [, errlog 0|1]
  rm -f "$S/pretool-error.log"
  pj PreToolUse "$2" command "$(valf "$3")" | bash "$R/.claude/hooks/on-pretool.sh" > "$WORK/pt.out" 2> "$WORK/pt.err"
  local rc=$? res
  case "$rc" in 0) res=PASS ;; 2) res=BLOCK ;; *) res="rc=$rc" ;; esac
  [ -s "$WORK/pt.out" ] && res="$res+stdout"
  if [ -n "${5:-}" ]; then
    row on-pretool "$1" "$4 log=$5" "$res log=$([ -s "$S/pretool-error.log" ] && echo 1 || echo 0)"
  else
    row on-pretool "$1" "$4" "$res"
  fi
}
nl=$'\n'
# --- cho qua
pt 'grep -n "git commit" docs' Bash 'grep -n "git commit" docs' PASS 0
pt 'git status' Bash 'git status' PASS 0
pt 'git diff' Bash 'git diff' PASS
pt 'git log' Bash 'git log' PASS
pt 'git remote -v' Bash 'git remote -v' PASS
pt 'dotnet ef migrations add X' Bash 'dotnet ef migrations add X' PASS
pt 'npm run build' Bash 'npm run build' PASS
pt 'echo "git push"' Bash 'echo "git push"' PASS 0
pt "echo 'git reset --hard'" Bash "echo 'git reset --hard'" PASS
pt 'rtk git status' Bash 'rtk git status' PASS
pt 'git -C . status' Bash 'git -C . status' PASS
pt 'git --no-pager log --oneline | head' Bash 'git --no-pager log --oneline | head' PASS
pt 'printf "%s" "$(git status)"' Bash 'printf "%s" "$(git status)"' PASS
pt 'heredoc có dòng git push trong thân' Bash "cat <<'EOF' > f.md${nl}git push${nl}git reset --hard${nl}EOF${nl}ls" PASS 0
pt 'heredoc <<- thụt tab' Bash "cat <<-EOF > f.md${nl}	git push${nl}	EOF${nl}ls" PASS 0
pt 'git checkout-index (không phải git checkout)' Bash 'git checkout-index -a' PASS
pt 'git log --grep="push"' Bash 'git log --grep="push"' PASS
pt 'find -exec grep (không phải lệnh cấm)' Bash 'find . -name "*.md" -exec grep -l git {} \;' PASS
pt 'bash .claude/check-docs.sh' Bash 'bash .claude/check-docs.sh' PASS
pt 'chú thích # git push' Bash "ls # git push${nl}# git commit -m x" PASS
pt 'sed có ; và && trong nháy đơn' Bash "sed -n '/a/{p;q}' x && echo 'a && git push'" PASS
pt 'PowerShell: Get-ChildItem; git status' PowerShell 'Get-ChildItem; git status' PASS
pt 'PowerShell: Write-Output "git push"' PowerShell 'Write-Output "git push"' PASS
pt 'PowerShell: here-string chứa git push' PowerShell "\$x = @'${nl}git push${nl}'@${nl}Write-Output \$x" PASS 0
pt 'PowerShell: backtick là ký tự thoát' PowerShell 'Write-Output "a `"git push`" b"' PASS
# --- chặn
pt 'rtk git commit -m x' Bash 'rtk git commit -m x' BLOCK
pt 'cd a && git push' Bash 'cd a && git push' BLOCK
pt 'echo x; git reset --hard' Bash 'echo x; git reset --hard' BLOCK
pt 'git -C . commit -m x' Bash 'git -C . commit -m x' BLOCK
pt 'git -c user.name=x commit' Bash 'git -c user.name=x commit' BLOCK
pt 'FOO=1 git push' Bash 'FOO=1 git push' BLOCK
pt '& git push (PowerShell)' PowerShell '& git push' BLOCK
pt '& git push (Bash)' Bash '& git push' BLOCK
pt 'git remote add o u' Bash 'git remote add o u' BLOCK
pt 'dotnet ef database update' Bash 'dotnet ef database update' BLOCK
pt 'timeout 5 git push' Bash 'timeout 5 git push' BLOCK
pt 'nice -n 5 git push' Bash 'nice -n 5 git push' BLOCK
pt 'nohup git push' Bash 'nohup git push' BLOCK
pt 'command git push' Bash 'command git push' BLOCK
pt 'env FOO=1 git push' Bash 'env FOO=1 git push' BLOCK
pt 'git --git-dir=.git --work-tree=. commit' Bash 'git --git-dir=.git --work-tree=. commit -m x' BLOCK
pt 'git --no-pager branch' Bash 'git --no-pager branch' BLOCK
pt 'bash -c "git commit -m x"' Bash 'bash -c "git commit -m x"' BLOCK
pt 'echo "$(git push)"' Bash 'echo "$(git push)"' BLOCK
pt 'echo `git stash`' Bash 'echo `git stash`' BLOCK
pt 'nhiều dòng: git status / git push' Bash "git status${nl}git push" BLOCK
pt 'CRLF: git status / git push' Bash "git status"$'\r\n'"git push" BLOCK
pt 'git diff | git apply' Bash 'git diff | git apply' BLOCK
pt 'git push>out.txt' Bash 'git push>out.txt' BLOCK
pt 'git push 2>&1' Bash 'git push 2>&1' BLOCK
pt '/usr/bin/git push' Bash '/usr/bin/git push' BLOCK
pt 'GIT push (Windows không phân biệt hoa thường)' Bash 'GIT push' BLOCK
pt 'git.exe push (PowerShell)' PowerShell 'git.exe push' BLOCK
pt '& "C:\Program Files\Git\cmd\git.exe" push (PowerShell)' PowerShell '& "C:\Program Files\Git\cmd\git.exe" push origin' BLOCK
pt 'powershell -Command "git push"' Bash 'powershell -Command "git push"' BLOCK
pt 'cmd /c git push' Bash 'cmd /c git push' BLOCK
pt 'find . -exec git add {} \;' Bash 'find . -name "*.md" -exec git add {} \;' BLOCK
pt 'ls | xargs git rm' Bash 'ls | xargs -n 1 git rm' BLOCK
pt 'rtk proxy git push' Bash 'rtk proxy git push' BLOCK
pt 'eval git push' Bash 'eval "git push"' BLOCK
pt 'if git push; then ...' Bash 'if git push; then echo ok; fi' BLOCK
pt '(git push)' Bash '(git push)' BLOCK
pt '{ git push; }' Bash '{ git push; }' BLOCK
pt 'PowerShell: $env:X="1"; git push' PowerShell '$env:X="1"; git push' BLOCK
pt 'PowerShell: git commit -m "a;b"' PowerShell 'git commit -m "a;b"' BLOCK
pt 'npm publish' Bash 'npm publish' BLOCK
pt 'dotnet nuget push x.nupkg' Bash 'dotnet nuget push x.nupkg' BLOCK
pt 'docker push img' Bash 'docker push img' BLOCK
pt 'git stash list (khớp deny git stash)' Bash 'git stash list' BLOCK
pt 'heredoc rồi git push sau thân' Bash "cat <<EOF > f${nl}x${nl}EOF${nl}git push" BLOCK
# --- lỗi phân tích
pt 'nháy chưa đóng, không khớp — cho qua, ghi log' Bash 'echo "chua dong' PASS 1
pt 'đoạn đầu khớp, nháy sau chưa đóng — chặn, ghi log' Bash 'git push; echo "chua dong' BLOCK 1
pt 'heredoc thiếu dòng kết thúc — cho qua, ghi log' Bash "cat <<EOF${nl}x" PASS 1
pt 'PowerShell -EncodedCommand — cho qua, ghi log' Bash 'powershell -EncodedCommand ZwBpAHQA' PASS 1

rm -f "$S/pretool-error.log"
printf '{"session_id":"s","tool_name":"Bash","tool_input":{"description":"x"}}' | bash "$R/.claude/hooks/on-pretool.sh" 2>/dev/null; rc=$?
row on-pretool "payload không có command — cho qua, ghi log" "rc=0 log=1" "rc=$rc log=$([ -s "$S/pretool-error.log" ] && echo 1 || echo 0)"
rm -f "$S/pretool-error.log"
printf '' | bash "$R/.claude/hooks/on-pretool.sh" 2>/dev/null; rc=$?
row on-pretool "stdin rỗng — cho qua, ghi log" "rc=0 log=1" "rc=$rc log=$([ -s "$S/pretool-error.log" ] && echo 1 || echo 0)"
rm -f "$S/pretool-error.log"
printf '{"tool_name":"Bash","tool_input":{"command":"git push' | bash "$R/.claude/hooks/on-pretool.sh" 2>/dev/null; rc=$?
row on-pretool "payload JSON cụt — cho qua, ghi log" "rc=0 log=1" "rc=$rc log=$([ -s "$S/pretool-error.log" ] && echo 1 || echo 0)"
mv "$R/.claude/settings.json" "$WORK/set.bak"
rm -f "$S/pretool-error.log"
pj PreToolUse Bash command "$(valf 'git push')" | bash "$R/.claude/hooks/on-pretool.sh" 2>/dev/null; rc=$?
row on-pretool "không có settings.json — cho qua, ghi log" "rc=0 log=1" "rc=$rc log=$([ -s "$S/pretool-error.log" ] && echo 1 || echo 0)"
printf '{"permissions":{"deny":["Edit(/x/**)"],"allow":["Bash(git push:*)"]}}' > "$R/.claude/settings.json"
rm -f "$S/pretool-error.log"
pj PreToolUse Bash command "$(valf 'git push')" | bash "$R/.claude/hooks/on-pretool.sh" 2>/dev/null; rc=$?
row on-pretool "deny không có Bash(...) (allow có git push) — cho qua, ghi log" "rc=0 log=1" "rc=$rc log=$([ -s "$S/pretool-error.log" ] && echo 1 || echo 0)"
mv "$WORK/set.bak" "$R/.claude/settings.json"

# lý do chặn là tiếng Việt, nêu mục cấm
pj PreToolUse Bash command "$(valf 'cd x && git -C . push origin')" | bash "$R/.claude/hooks/on-pretool.sh" > /dev/null 2> "$WORK/pt.err"
row on-pretool "lý do chặn nêu mục cấm Bash(git push:*)" "có" "$(grep -q 'Bash(git push:\*)' "$WORK/pt.err" && echo có || echo không)"

# thời gian một lần gọi — chỉ in, không chấm
big=$(printf 'cat <<EOF > big.md\n'; for i in $(seq 1 1500); do printf 'dòng %s có "nháy" và git push trong thân heredoc\n' "$i"; done; printf 'EOF\n')
s0=$(date +%s%N)
pj PreToolUse Bash command "$(valf 'git status && ls -la')" | bash "$R/.claude/hooks/on-pretool.sh" 2>/dev/null
s1=$(date +%s%N)
pj PreToolUse Bash command "$(valf "$big")" | bash "$R/.claude/hooks/on-pretool.sh" 2>/dev/null; rcbig=$?
s2=$(date +%s%N)
row on-pretool "heredoc 1500 dòng chứa git push — cho qua" "rc=0" "rc=$rcbig"
TIMING="Thời gian (gồm cả node dựng payload): lệnh ngắn $(( (s1-s0)/1000000 )) ms · heredoc 1500 dòng $(( (s2-s1)/1000000 )) ms"

###############################################################################
# Mỗi nhóm phải tự chứng minh nó có ca — một nhóm không chạy ca nào mà vẫn in
# "0 SAI" là một bộ test không kiểm gì.
for g in core-paths on-edit on-stop on-subagent-stop on-pretool; do
  if [ "${GROUPN[$g]:-0}" -eq 0 ]; then
    printf '| %s | (nhóm) | có ca | không ca nào chạy | **SAI** |\n' "$g" >> "$REPORT"
    NFAIL=$((NFAIL+1))
  fi
done

if [ "$VERBOSE" -eq 1 ]; then
  cat "$REPORT"
  printf '\n%s\n' "$TIMING"
elif [ "$NFAIL" -gt 0 ]; then
  printf '| Nhóm | Ca | Kỳ vọng | Thực tế | KQ |\n| --- | --- | --- | --- | --- |\n'
  grep -F '**SAI**' "$REPORT"
fi
summary=""
for g in core-paths on-edit on-stop on-subagent-stop on-pretool; do summary="$summary $g=${GROUPN[$g]:-0}"; done
printf '\nSố ca theo nhóm:%s\n' "$summary"
printf 'Tổng: %s OK, %s SAI\n' "$NPASS" "$NFAIL"
[ "$NFAIL" -eq 0 ] || exit 1
exit 0
