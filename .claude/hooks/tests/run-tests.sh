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
#   - nhóm settings.json chạy NGUYÊN chuỗi lệnh hook của settings.json thật;
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
for f in lib.sh core-paths.sh on-edit.sh on-stop.sh on-subagent-stop.sh on-pretool.sh; do
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
GROUPS_ALL="core-paths on-edit on-stop on-subagent-stop on-pretool settings"

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
  o.hooks.PreToolUse = [{ matcher: "Bash|PowerShell", hooks: [{ type: "command", command: "bash \"$CLAUDE_PROJECT_DIR/.claude/hooks/on-pretool.sh\"", timeout: 10 }] }];
  o.hooks.SubagentStop = [{ matcher: "^core-reviewer$", hooks: [{ type: "command", command: "bash \"$CLAUDE_PROJECT_DIR/.claude/hooks/on-subagent-stop.sh\"", timeout: 10 }] }];
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
src/BE/Tests/CoreAndSkill.ArchTests/

# Frontend
src/FE/src/app/core/
src/FE/src/app/shared/
src/FE/src/app/platform/
src/FE/eslint.config.js
src/FE/eslint.boundaries.cjs

# Cổng FE và canary của nó — mục ngoài mọi khu src/ docs/ database/
scripts/fe-gate.sh
scripts/tests/

# Dữ liệu schema/seed của Core
database/scripts/core/

# Tệp chứa chính khối này
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
  # Cổng giả: mã thoát đọc từ .claude/.state/stub-rc (mặc định 0), mỗi lần chạy
  # ghi một dòng vào .claude/.state/stub-ran. Cả hai nằm trong thư mục trạng
  # thái — ngoài tầm quét "có gì mới hơn lần xanh", nên chính việc đếm không làm
  # cổng chạy lại.
  cat > "$R/.claude/check-docs.sh" <<'EOF'
#!/usr/bin/env bash
cd "$(dirname "$0")/.." || exit 2
mkdir -p .claude/.state
echo x >> .claude/.state/stub-ran
rc=$(cat .claude/.state/stub-rc 2>/dev/null || echo 0)
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
# session_id lấy từ biến PJ_SID (mặc định s1).
PJ_SID=s1
pj() {
  PJSID="$PJ_SID" node -e '
    const fs = require("fs");
    const [ev, tool, key, vf, rf] = process.argv.slice(1);
    const o = { session_id: process.env.PJSID, transcript_path: "C:\\Users\\x\\t.jsonl", cwd: "D:\\Repo",
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
coma() { [ -e "$1" ] && echo còn || echo mất; }

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
# KHÔNG neo vào SỐ đường dẫn: khối core-paths mọc thêm mục mỗi lần định nghĩa
# Core mở rộng, nên một con số ở đây đỏ vì lý do KHÔNG phải lỗi, và lối sửa tự
# nhiên là tăng số lên mà không nghĩ — ca test tụt thành con dấu cao su.
# Neo bằng: rc=0, tập KHÁC RỖNG (chống xanh-rỗng), và sự có mặt của một mục cụ thể.
cp_n=$(grep -c . "$WORK/cp.out")
row core-paths "khối hợp lệ (tài liệu mặc định)" "rc=0 / khác rỗng" "rc=$rc / $([ "$cp_n" -ge 1 ] && echo 'khác rỗng' || echo RỖNG)"
row core-paths "mục ngoài src/ không rơi ra im lặng" "có" "$(grep -qx 'database/scripts/core/' "$WORK/cp.out" && echo 'có' || echo 'KHÔNG')"
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
PS="$S/phien/s1"                         # thư mục phiên của payload pj/pje (session_id s1)
st() { # dấu: src-touched của phiên / nhật ký core-touched chung / cờ touched-core của phiên
  printf 'src=%s core=%s co=%s' "$([ -e "$PS/src-touched" ] && echo 1 || echo 0)" "$([ -s "$S/core-touched" ] && echo 1 || echo 0)" "$([ -e "$PS/touched-core" ] && echo 1 || echo 0)"
}
reset_state() { rm -rf "$S"; }
lamcu() { find "$R" -path "$R/.git" -prune -o -exec touch -d '2 hours ago' {} + 2>/dev/null; }
edt() { # tên, tool, key, giá trị, kỳ vọng [tool_response]
  reset_state
  local rf=""
  if [ -n "${6:-}" ]; then printf '%s' "$6" > "$WORK/r.tmp"; rf="$WORK/r.tmp"; fi
  pj PostToolUse "$2" "$3" "$(valf "$4")" $rf | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null
  local rc=$?
  row on-edit "$1" "$5 rc=0" "$(st) rc=$rc"
}
# Giai đoạn 1: chưa có src/ thì không có gì để canh — kể cả tài liệu. Cổng tài liệu
# không cần dấu nào từ hook này: hook Stop tự hỏi hệ thống tệp.
edt "không src — Edit docs/: không dấu nào" Edit file_path "$RB\\docs\\x.md" "src=0 core=0 co=0"
edt "không src — Edit tài liệu chủ khối: không dấu nào" Edit file_path "$RB\\docs\\kien-truc-core-module.md" "src=0 core=0 co=0"
edt "không src — PowerShell command" PowerShell command "Get-ChildItem" "src=0 core=0 co=0"
row on-edit "không src — không tạo thư mục phiên" "không" "$([ -d "$PS" ] && echo có || echo không)"
reset_state
printf '{"session_id":"s1","tool_name":"Read","tool_input":{"pattern":"x"}}' | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null; rc=$?
row on-edit "không src — payload không file_path, không command" "src=0 core=0 co=0 rc=0" "$(st) rc=$rc"

mkdir -p "$R/src/BE/Core/Core.Domain" "$R/src/BE/Core/obj" "$R/src/BE/Modules/A" "$R/src/FE/src/app/core" "$R/src/FE/node_modules/pkg" "$R/docs/quy-uoc" "$R/database/scripts/core" "$R/scripts/tests"
printf 'g' > "$R/scripts/fe-gate.sh"
lamcu
edt "có src — Edit Core BE" Edit file_path "$RB\\src\\BE\\Core\\Core.Domain\\X.cs" "src=1 core=1 co=1"
row on-edit "có src — core-touched ghi đường dẫn tương đối" "src/BE/Core/Core.Domain/X.cs" "$(cat "$S/core-touched" 2>/dev/null)"
edt "có src — Edit module (không Core)" Edit file_path "$RB\\src\\BE\\Modules\\A\\X.cs" "src=1 core=0 co=0"
edt "có src — Edit docs/quy-uoc/: sửa luật không đòi review code" Edit file_path "$RB\\docs\\quy-uoc\\be-x.md" "src=0 core=0 co=0"
edt "có src — Edit file bị gitignore trong Core (obj/)" Write file_path "$RB\\src\\BE\\Core\\obj\\x.json" "src=0 core=0 co=0"
edt "có src — mục tệp khớp đúng tên" Write file_path "src/FE/eslint.config.js" "src=1 core=1 co=1"
edt "có src — tên gần giống mục tệp thì không khớp" Write file_path "src/FE/eslint.config.jsx" "src=1 core=0 co=0"
edt "có src — Edit chính tài liệu chủ khối" Edit file_path "$RB\\docs\\kien-truc-core-module.md" "src=0 core=1 co=1"
edt "có src — Edit database/scripts/core/" Edit file_path "$RB\\database\\scripts\\core\\0002__core__seed.sql" "src=0 core=1 co=1"
row on-edit "có src — core-touched ghi đường dẫn database/" "database/scripts/core/0002__core__seed.sql" "$(cat "$S/core-touched" 2>/dev/null)"
# Lỗ cũ: nhánh Edit/Write lọc khu vực (docs/ .claude/ spec/ src/ database/) TRƯỚC
# khi so với khối, nên mục scripts/ rơi ra im lặng. Bản hook cũ cho core=0 ở đây.
edt "có src — Edit scripts/fe-gate.sh (mục ngoài mọi khu cũ)" Edit file_path "$RB\\scripts\\fe-gate.sh" "src=0 core=1 co=1"
edt "có src — Write dưới scripts/tests/ (mục thư mục)" Write file_path "scripts/tests/fe-gate-x.test.sh" "src=0 core=1 co=1"
# Chuẩn hoá đường dẫn — neo vào mục Core là tài liệu chủ khối.
edt "chuẩn hoá — gạch chéo xuôi tuyệt đối" Edit file_path "$RW/docs/kien-truc-core-module.md" "src=0 core=1 co=1"
edt "chuẩn hoá — dạng POSIX tuyệt đối" Edit file_path "$RU/docs/kien-truc-core-module.md" "src=0 core=1 co=1"
LOWW=$(printf '%s' "$RW" | tr 'A-Z' 'a-z')
edt "chuẩn hoá — chữ thường toàn bộ" Edit file_path "$LOWW/docs/kien-truc-core-module.md" "src=0 core=1 co=1"
edt "chuẩn hoá — gạch chéo gộp đôi" Edit file_path "${RW//\//\/\/}//docs//kien-truc-core-module.md" "src=0 core=1 co=1"
edt "chuẩn hoá — ./ tương đối" Write file_path "./docs/kien-truc-core-module.md" "src=0 core=1 co=1"
row on-edit "chuẩn hoá — core-touched ghi đường dẫn tương đối" "docs/kien-truc-core-module.md" "$(cat "$S/core-touched" 2>/dev/null)"
edt "README.md gốc (ngoài khối)" Edit file_path "$RB\\README.md" "src=0 core=0 co=0"
edt "file bị gitignore .claude/.state/" Write file_path "$RB\\.claude\\.state\\x" "src=0 core=0 co=0"
edt "ngoài repo nhưng có \\docs\\kien-truc-core-module.md (hướng an toàn)" Edit file_path "D:\\Khac\\docs\\kien-truc-core-module.md" "src=0 core=1 co=1"
edt "ngoài repo, không khớp mục nào" Edit file_path "D:\\Khac\\ghi-chu.md" "src=0 core=0 co=0"
edt "NotebookEdit notebook_path trong Core" NotebookEdit notebook_path "$RB\\src\\FE\\src\\app\\core\\a.ipynb" "src=1 core=1 co=1"

# Phiên: cờ touched-core và src-touched thuộc thư mục của phiên đã sửa; nhật ký
# core-touched là chung.
reset_state
PJ_SID=s2
pj PostToolUse Edit file_path "$(valf "$RB\\src\\BE\\Core\\Core.Domain\\X.cs")" | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null
PJ_SID=s1
row on-edit "phiên — cờ nằm ở thư mục của phiên đã sửa, nhật ký chung" "s2=1 s1=0 chung=1" \
  "s2=$([ -e "$S/phien/s2/touched-core" ] && echo 1 || echo 0) s1=$([ -e "$PS/touched-core" ] && echo 1 || echo 0) chung=$([ -s "$S/core-touched" ] && echo 1 || echo 0)"
reset_state
PJ_SID='../../x'
pj PostToolUse Edit file_path "$(valf "$RB\\src\\BE\\Core\\Core.Domain\\X.cs")" | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null
PJ_SID=s1
row on-edit "phiên — session_id lạ không trỏ ra ngoài thư mục phiên" "phien/x có / ngoài không" \
  "phien/x $([ -e "$S/phien/x/touched-core" ] && echo có || echo không) / ngoài $([ -e "$S/touched-core" ] || [ -e "$R/.claude/touched-core" ] && echo có || echo không)"
reset_state
printf '{"hook_event_name":"PostToolUse","tool_name":"Edit","tool_input":{"file_path":"src/BE/Core/Core.Domain/X.cs"}}' | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null
row on-edit "phiên — payload không có session_id: dồn về một thư mục chung" "có" "$([ -e "$S/phien/khong-ro-phien/touched-core" ] && echo có || echo không)"

# Payload Edit/Write có nội dung chứa ký tự UTF-8 BỐN byte (emoji). Trên Git Bash,
# sed ở locale UTF-8 không khớp `.` với byte cuối của ký tự đó, nên mẫu trích
# file_path để lại đuôi payload dính sau đường dẫn: core-touched nhận dòng
# `docs/x.md<0x96> Lý do…","new_string":…` — không bao giờ tồn tại trên đĩa.
# Hình payload đúng như harness gửi: file_path đứng đầu tool_input, rồi
# old_string/new_string (Edit) hoặc content (Write), rồi tool_response.
# Trên Linux (CI) bản cũ cũng xanh ở ca này — lỗi chỉ tái hiện trên MSYS2, đúng
# môi trường hook chạy thật.
pje() { # <tool> <file_path> <file-chứa-nội-dung>
  node -e '
    const fs = require("fs");
    const [tool, fp, vf] = process.argv.slice(1);
    const v = fs.readFileSync(vf, "utf8");
    const o = { session_id: "s1", transcript_path: "C:\\Users\\x\\t.jsonl", cwd: "D:\\Repo",
                permission_mode: "default", hook_event_name: "PostToolUse", tool_name: tool };
    if (tool === "Write") {
      o.tool_input = { file_path: fp, content: v };
      o.tool_response = { type: "update", filePath: fp, content: v };
    } else {
      o.tool_input = { file_path: fp, old_string: v, new_string: v + " sửa", replace_all: false };
      o.tool_response = { filePath: fp, oldString: v, newString: v + " sửa" };
    }
    o.tool_use_id = "toolu_1";
    process.stdout.write(JSON.stringify(o));
  ' "$@"
}
ctouched() { LC_ALL=C tr -c '[:print:]\n' '?' < "$S/core-touched" 2>/dev/null | tr '\n' ' ' | sed 's/ $//' | cut -c1-90; }
EMO_DOC=$'> \xF0\x9F\x93\x96 Lý do, bẫy, ví dụ mở rộng: [`be-x.md`](../wiki-core/be/ly-do/be-x.md) §6.3 "nháy"\n\n---\n\n## 7. \xF0\x9F\x93\x90 Khi endpoint chưa tồn tại'
reset_state
pje Edit "$RB\\docs\\kien-truc-core-module.md" "$(valf "$EMO_DOC")" | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null; rc=$?
row on-edit "có src — Edit, old/new_string chứa emoji 4 byte: core-touched đúng đường dẫn" "docs/kien-truc-core-module.md rc=0" "$(ctouched) rc=$rc"
reset_state
pje Write "$RB\\src\\FE\\src\\app\\core\\a.ts" "$(valf "$EMO_DOC")" | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null; rc=$?
row on-edit "có src — Write, content chứa emoji 4 byte: core-touched đúng đường dẫn" "src/FE/src/app/core/a.ts rc=0" "$(ctouched) rc=$rc"

# ---- nhánh lệnh shell
reset_state; lamcu
printf 'a' > "$R/src/FE/src/app/core/a.ts"
printf 'b' > "$R/src/BE/Core/obj/gen.json"
printf 'c' > "$R/src/FE/node_modules/pkg/i.js"
pj PostToolUse Bash command "$(valf 'echo x > src/FE/src/app/core/a.ts')" | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null; rc=$?
row on-edit "shell — vừa sửa Core FE + file gitignore" "src=1 core=1 co=1 rc=0" "$(st) rc=$rc"
row on-edit "shell — core-touched chỉ có file không bị gitignore" "src/FE/src/app/core/a.ts" "$(tr '\n' ' ' < "$S/core-touched" 2>/dev/null | sed 's/ $//')"
row on-edit "shell — đặt mốc quét của phiên" "có" "$([ -e "$PS/scan-stamp" ] && echo có || echo không)"

reset_state; lamcu
pj PostToolUse Bash command "$(valf 'git status')" | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null; rc=$?
row on-edit "shell — không file nào vừa đổi" "src=0 core=0 co=0 rc=0" "$(st) rc=$rc"

reset_state; lamcu
printf 'x' > "$R/src/BE/Modules/A/Y.cs"
pj PostToolUse Bash command "$(valf 'dotnet build')" | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null; rc=$?
row on-edit "shell — vừa sửa module" "src=1 core=0 co=0 rc=0" "$(st) rc=$rc"

lamcu
edt "shell — tool_response chứa \"file_path\" vẫn là nhánh shell" Bash command "ls" "src=0 core=0 co=0" '"file_path": "src/BE/Core/evil.cs" "notebook_path":"x"'

# Mục core-paths NGOÀI src/: nhánh shell dò bằng `find` trên tập roots dựng riêng,
# chỉ nhận mục có thật trên đĩa — chỗ một mục có thể rơi ra im lặng.
reset_state; lamcu
printf 'x' > "$R/database/scripts/core/0005__core__x.sql"
pj PostToolUse Bash command "$(valf 'psql -f database/scripts/core/0005__core__x.sql')" \
  | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null; rc=$?
row on-edit "shell — vừa sửa database/scripts/core/" "src=0 core=1 co=1 rc=0" "$(st) rc=$rc"
row on-edit "shell — core-touched ghi đường dẫn database/" \
  "database/scripts/core/0005__core__x.sql" "$(cat "$S/core-touched" 2>/dev/null)"
reset_state; lamcu
printf 'h' > "$R/scripts/fe-gate.sh"
pj PostToolUse Bash command "$(valf 'sed -i s/a/b/ scripts/fe-gate.sh')" | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null; rc=$?
row on-edit "shell — vừa sửa scripts/fe-gate.sh" "src=0 core=1 co=1 rc=0" "$(st) rc=$rc"

# Mục core-paths KHÔNG có trên đĩa: một mục thiếu KHÔNG được kéo theo cả phép quét —
# `find` gặp root không tồn tại trả mã khác 0, và một bản thoát sớm vì mã đó làm
# mọi dấu biến mất im lặng.
reset_state; lamcu
mv "$R/database" "$WORK/database.bak"
printf 'y' > "$R/src/BE/Core/Core.Domain/Z.cs"
pj PostToolUse Bash command "$(valf 'dotnet build')" | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null; rc=$?
row on-edit "shell — mục core-paths thiếu trên đĩa, phần còn lại vẫn quét" \
  "src=1 core=1 co=1 rc=0" "$(st) rc=$rc"
row on-edit "shell — mục thiếu trên đĩa KHÔNG ghi dấu lỗi (đúng hiện trạng, là chỗ hở)" \
  "không" "$([ -e "$S/core-paths-error" ] && echo có || echo không)"
mv "$WORK/database.bak" "$R/database"
rm -f "$R/src/BE/Core/Core.Domain/Z.cs"

# Mốc lần quét trước, không phải cửa sổ 90 giây: một lệnh chạy lâu (build, format cả
# solution) sửa file ở ĐẦU lệnh thì lúc hook chạy file đó đã quá 90 giây. Bản cũ
# dùng `-newermt '-90 seconds'` nên bỏ sót ở ca này.
reset_state; lamcu
pj PostToolUse Bash command "$(valf 'ls')" | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null
touch -d '10 minutes ago' "$PS/scan-stamp"
printf 'z' > "$R/src/BE/Core/Core.Domain/Lau.cs"; touch -d '5 minutes ago' "$R/src/BE/Core/Core.Domain/Lau.cs"
pj PostToolUse Bash command "$(valf 'dotnet format')" | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null; rc=$?
row on-edit "shell — file sửa 5 phút trước, sau mốc quét trước (lệnh chạy lâu)" "src=1 core=1 co=1 rc=0" "$(st) rc=$rc"
pj PostToolUse Bash command "$(valf 'ls')" | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null
row on-edit "shell — quét lại ngay sau đó không ghi trùng dòng" "1" "$(grep -c 'Lau.cs' "$S/core-touched" 2>/dev/null)"
rm -f "$R/src/BE/Core/Core.Domain/Lau.cs"

reset_state
cp "$R/docs/kien-truc-core-module.md" "$WORK/doc.bak"
printf '# không còn khối\n' > "$R/docs/kien-truc-core-module.md"
edt "có src — khối Core mất: vẫn ghi src" Edit file_path "$RB\\src\\BE\\Core\\Core.Domain\\X.cs" "src=1 core=0 co=0"
row on-edit "có src — khối Core mất: ghi lý do vào core-paths-error" "có lý do" "$([ -s "$S/core-paths-error" ] && echo 'có lý do' || echo 'không')"
cp "$WORK/doc.bak" "$R/docs/kien-truc-core-module.md"
edt "có src — khối sửa lại: dấu lỗi bị xoá" Edit file_path "$RB\\src\\BE\\Core\\Core.Domain\\X.cs" "src=1 core=1 co=1"
row on-edit "có src — khối sửa lại: không còn core-paths-error" "không" "$([ -e "$S/core-paths-error" ] && echo có || echo không)"

rm -rf "$R/src"
reset_state
printf 'x' > "$R/docs/quy-uoc/be-y.md"
pj PostToolUse Bash command "$(valf 'ls')" | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null; rc=$?
row on-edit "không src — Bash vừa sửa docs/quy-uoc/: không dấu nào" "src=0 core=0 co=0 rc=0" "$(st) rc=$rc"

###############################################################################
header on-stop.sh
R=$(mkrepo stop)
S="$R/.claude/.state"
PS="$S/phien/s"
soft() { cp "$SOFTSET" "$R/.claude/settings.json"; }
hard() { cp "$HARDSET" "$R/.claude/settings.json"; }
runstop() { # $1 = 0|1 stop_hook_active; $2 = mảng background_tasks JSON (trống = không có khoá); $3 = session_id (mặc định s)
  local bg=""
  [ -n "${2:-}" ] && bg=",\"background_tasks\":$2"
  printf '{"session_id":"%s","hook_event_name":"Stop","stop_hook_active":%s%s}' "${3:-s}" "$([ "$1" = 1 ] && echo true || echo false)" "$bg" \
    | bash "$R/.claude/hooks/on-stop.sh" > "$WORK/stop.out" 2> "$WORK/stop.err"
}
stoppl() { printf '%s' "$1" | bash "$R/.claude/hooks/on-stop.sh" > "$WORK/stop.out" 2> "$WORK/stop.err"; }
kind() { # phân loại đầu ra
  local j; j=$(isjson < "$WORK/stop.out")
  if [ "$j" = rong ]; then echo "im lặng"; return; fi
  if [ "$j" != json ]; then echo "JSON HỎNG"; return; fi
  local d; d=$(jfield "$WORK/stop.out" decision)
  if [ "$d" = block ]; then echo "block"; else [ -n "$(jfield "$WORK/stop.out" systemMessage)" ] && echo "systemMessage" || echo "json khác"; fi
}
has() { grep -q -- "$1" "$WORK/stop.out" && echo có || echo không; }
ran() { if [ -f "$S/stub-ran" ]; then grep -c . "$S/stub-ran"; else echo 0; fi; }
stub() { mkdir -p "$S"; echo "$1" > "$S/stub-rc"; }
clean() { rm -rf "$S"; mkdir -p "$S"; }
lamcu() { find "$R" \( -path "$R/.git" -o -path "$S" \) -prune -o -exec touch -d '1 hour ago' {} + 2>/dev/null; }
xanh() { lamcu; : > "$S/gate-green"; rm -f "$S/stub-ran"; }   # cổng vừa xanh, không gì mới hơn
co() { mkdir -p "$PS"; : > "$PS/touched-core"; }               # phiên s đã chạm Core
BG_RUN='[{"id":"a1","type":"subagent","status":"running","description":"Sửa [Core] {x} \"nháy\"","agent_type":"backend-expert"}]'
BG_SHELL='[{"id":"b1","type":"shell","status":"running","description":"sleep","command":"sleep 30"}]'
BG_DONE='[{"id":"a1","type":"subagent","status":"completed","description":"x","agent_type":"backend-expert"}]'
BG_MIX='[{"id":"b1","type":"shell","status":"running","command":"x"},{"id":"a2","status":"running","description":"y ] } [ \\\"","type":"subagent"}]'

# ---- cổng tài liệu: chạy khi có gì mới hơn lần xanh cuối
soft; clean
runstop 0; row on-stop "chưa từng xanh — chạy cổng, xanh thì im lặng" "im lặng / chạy 1 / có mốc" "$(kind) / chạy $(ran) / $([ -e "$S/gate-green" ] && echo 'có mốc' || echo 'không mốc')"
xanh
runstop 0; row on-stop "không gì mới hơn lần xanh — không chạy cổng" "im lặng / chạy 0" "$(kind) / chạy $(ran)"
xanh; stub 1; printf 'x' > "$R/docs/moi.md"
runstop 0; rc=$?; row on-stop "tài liệu mới hơn lần xanh, cổng đỏ" "block rc=0" "$(kind) rc=$rc"
row on-stop "cổng đỏ — không dời mốc xanh" "mốc cũ hơn tệp" "$([ "$R/docs/moi.md" -nt "$S/gate-green" ] && echo 'mốc cũ hơn tệp' || echo 'mốc đã dời')"
runstop 1; row on-stop "cổng đỏ, stop_hook_active — vẫn chặn" "block" "$(kind)"
stub 0; runstop 0; row on-stop "sửa xong, cổng xanh — thả, dời mốc" "im lặng / mốc mới hơn tệp" "$(kind) / $([ "$S/gate-green" -nt "$R/docs/moi.md" ] && echo 'mốc mới hơn tệp' || echo 'mốc cũ')"

xanh; stub 2; printf 'y' > "$R/docs/moi2.md"
runstop 0; row on-stop "cổng không chạy được (rc=2)" "block" "$(kind)"
runstop 1; row on-stop "rc=2 lần hai — thả" "im lặng" "$(kind)"
rm -f "$S/stub-ran"
runstop 0; row on-stop "rc=2 đã báo, cây chưa đổi — không chạy lại, không báo lại" "im lặng / chạy 0" "$(kind) / chạy $(ran)"
printf 'z' > "$R/docs/moi3.md"
runstop 0; row on-stop "rc=2, cây đổi sau lần báo — chạy lại, báo lại" "block" "$(kind)"

stub 0; printf 'd' > "$R/docs/se-xoa.md"; xanh
rm -f "$R/docs/se-xoa.md"
runstop 0; row on-stop "xoá một tài liệu sau lần xanh — chạy cổng (mtime thư mục)" "chạy 1" "chạy $(ran)"

mkdir -p "$R/node_modules/p"; xanh
printf 'o' > "$R/node_modules/p/i.js"; printf 's' > "$S/rac"
runstop 0; row on-stop "chỉ tệp bị gitignore / trong .claude/.state mới hơn — không chạy cổng" "chạy 0" "chạy $(ran)"

# ---- agent nền: phiên đang chờ, chưa dừng — không chạy cổng, không chặn
xanh; stub 1; printf 'b' > "$R/docs/bg.md"
runstop 0 "$BG_RUN"; row on-stop "agent nền đang chạy — không chạy cổng, không chặn" "im lặng / chạy 0" "$(kind) / chạy $(ran)"
runstop 0 "$BG_MIX"; row on-stop "agent nền đứng sau việc shell, mô tả có ngoặc và nháy — vẫn hoãn" "im lặng" "$(kind)"
stoppl '{"session_id":"86a5182e-27be-4a95-bb48-9f8fafb6c46a","transcript_path":"C:\\Users\\x\\86a5.jsonl","cwd":"C:\\Users\\x\\probe-stop","prompt_id":"9991045a","permission_mode":"default","hook_event_name":"Stop","stop_hook_active":false,"last_assistant_message":"launched","background_tasks":[{"id":"a5d062c89d6777f71","type":"subagent","status":"running","description":"Sleep 30 seconds and reply","agent_type":"general-purpose"}],"session_crons":[]}'
row on-stop "payload thật 2.1.282 có agent nền đang chạy — hoãn" "im lặng" "$(kind)"
runstop 0 "$BG_SHELL"; row on-stop "chỉ việc shell chạy nền — không hoãn" "block" "$(kind)"
runstop 0 "$BG_DONE"; row on-stop "agent nền đã xong — không hoãn" "block" "$(kind)"
runstop 0 "[]"; row on-stop "mảng rỗng — không hoãn" "block" "$(kind)"
stoppl '{"session_id":"s","hook_event_name":"Stop","stop_hook_active":false,"last_assistant_message":"{\"type\":\"subagent\",\"status\":\"running\"}","background_tasks":[]}'
row on-stop "chuỗi agent nền giả trong last_assistant_message — không hoãn" "block" "$(kind)"
stoppl '{"session_id":"s","hook_event_name":"Stop","background_tasks":[{"type":"subagent","status":"run'
row on-stop "payload cụt — không hoãn (hỏng theo hướng chặn)" "block" "$(kind)"
stub 0

# ---- chạm Core
hard; clean; co; printf 'docs/x.md\n' > "$S/core-touched"
runstop 0; row on-stop "không src, có nhật ký và cờ — không nhắc, dọn cả hai" "im lặng / mất / mất" "$(kind) / $(coma "$S/core-touched") / $(coma "$PS/touched-core")"

mkdir -p "$R/src/BE/Core"; printf 'x' > "$R/src/BE/Core/A.cs"
soft; clean; co; printf 'src/BE/Core/A.cs\n' > "$S/core-touched"
runstop 0; row on-stop "có src, CHƯA gắn SubagentStop — nhắc một lần" "block" "$(kind)"
row on-stop "nhắc một lần — nêu core-reviewer, không mời 'nói lý do'" "có/không" "$(has core-reviewer)/$(has 'lý do rồi kết thúc')"
row on-stop "nhắc một lần — dọn nhật ký và cờ" "mất/mất" "$(coma "$S/core-touched")/$(coma "$PS/touched-core")"
runstop 1; row on-stop "nhắc một lần — lần Stop sau thả" "im lặng" "$(kind)"

hard; clean; co; printf 'src/BE/Core/A.cs\n' > "$S/core-touched"
runstop 0; row on-stop "ĐÃ gắn SubagentStop, chưa có dấu review — chặn" "block" "$(kind)"
row on-stop "chặn cứng — nêu lối thoát của người dùng" "có" "$(has 'người dùng tự xoá .claude/.state/core-touched')"
row on-stop "chặn cứng — dặn gọi reviewer chạy nền" "có" "$(has 'chạy nền')"
runstop 1; row on-stop "chặn cứng, stop_hook_active — vẫn chặn" "block" "$(kind)"
row on-stop "chặn cứng — giữ nhật ký và cờ" "còn/còn" "$(coma "$S/core-touched")/$(coma "$PS/touched-core")"
runstop 0 "$BG_RUN"; row on-stop "chặn cứng, agent nền đang chạy — hoãn, giữ dấu" "im lặng / còn / còn" "$(kind) / $(coma "$S/core-touched") / $(coma "$PS/touched-core")"
runstop 0 "$BG_SHELL"; row on-stop "chặn cứng, chỉ việc shell chạy nền — chặn" "block" "$(kind)"
touch -d '1 minute ago' "$R/src/BE/Core/A.cs"; : > "$S/core-reviewed"
runstop 1; row on-stop "dấu review mới hơn file — thả" "im lặng" "$(kind)"
row on-stop "dấu review mới hơn — dọn nhật ký và cờ" "mất/mất" "$(coma "$S/core-touched")/$(coma "$PS/touched-core")"

# Nhật ký chung, cờ theo phiên: phiên không chạm Core không bị việc của phiên khác giữ lại.
hard; clean; printf 'src/BE/Core/A.cs\n' > "$S/core-touched"
runstop 0; row on-stop "phiên KHÔNG chạm Core, nhật ký có việc chưa review — thả, giữ nhật ký" "im lặng / còn" "$(kind) / $(coma "$S/core-touched")"
mkdir -p "$S/phien/s2"; : > "$S/phien/s2/touched-core"
runstop 0 "" s2; row on-stop "phiên s2 mang cờ — chặn s2" "block" "$(kind)"
runstop 0; row on-stop "cùng lúc đó phiên s không mang cờ — vẫn thả" "im lặng" "$(kind)"
rm -f "$S/core-touched"
runstop 0 "" s2; row on-stop "người dùng xoá nhật ký — phiên mang cờ được thả, gỡ cờ" "im lặng / mất" "$(kind) / $(coma "$S/phien/s2/touched-core")"

clean; co; : > "$S/core-reviewed"; printf 'y' > "$R/src/BE/Core/A.cs"
printf 'src/BE/Core/A.cs\n' > "$S/core-touched"
runstop 0; row on-stop "sửa ngay sau dấu review (không ngủ) — chặn" "block" "$(kind)"

clean; co; touch -d '10 minutes ago' "$R/src/BE/Core/A.cs"; : > "$S/core-reviewed"
printf 'z' > "$R/src/BE/Core/B.cs"
printf 'src/BE/Core/A.cs\nsrc/BE/Core/B.cs\n' > "$S/core-touched"
runstop 0; row on-stop "hai file, chỉ một mới hơn dấu — chỉ liệt kê file đó" "block / B có / A không" "$(kind) / B $(has 'B.cs') / A $(has 'A.cs')"

# Đường dẫn KHÔNG còn trên đĩa. Mốc so là mtime của thư mục tổ tiên gần nhất còn
# tồn tại (xoá một mục con cập nhật mtime thư mục chứa nó) — KHÔNG phải mtime của
# chính core-touched, vốn là lúc ghi dòng mới nhất của BẤT KỲ file nào.
clean; co; printf 'd' > "$R/src/BE/Core/DaXoa.cs"
printf 'src/BE/Core/DaXoa.cs\n' > "$S/core-touched"
touch -d '5 minutes ago' "$R/src/BE/Core/DaXoa.cs" "$R/src/BE/Core" "$S/core-touched"
: > "$S/core-reviewed"; sleep 1
rm -f "$R/src/BE/Core/DaXoa.cs"
runstop 0; row on-stop "file Core xoá SAU dấu review (không dòng nào ghi thêm) — chặn" "block / có" "$(kind) / $(has 'DaXoa.cs')"
printf 'loi\n' > "$S/subagent-stop-error.log"
runstop 1; row on-stop "chặn cứng kèm log SubagentStop — trỏ tới log" "có" "$(has 'subagent-stop-error.log')"

clean; co; mkdir -p "$R/src/BE/Core/Sub"; printf 'd' > "$R/src/BE/Core/Sub/X.cs"
printf 'src/BE/Core/Sub/X.cs\n' > "$S/core-touched"
touch -d '5 minutes ago' "$R/src/BE/Core/Sub/X.cs" "$R/src/BE/Core/Sub" "$R/src/BE/Core" "$S/core-touched"
: > "$S/core-reviewed"; sleep 1
rm -rf "$R/src/BE/Core/Sub"
runstop 0; row on-stop "cả thư mục chứa file Core xoá SAU dấu review — chặn" "block / có" "$(kind) / $(has 'Sub/X.cs')"

clean; co; printf 'd' > "$R/src/BE/Core/DaXoa2.cs"
printf 'src/BE/Core/DaXoa2.cs\n' > "$S/core-touched"
rm -f "$R/src/BE/Core/DaXoa2.cs"
touch -d '5 minutes ago' "$R/src/BE/Core" "$S/core-touched"
: > "$S/core-reviewed"
runstop 0; row on-stop "file Core xoá TRƯỚC dấu review — thả, dọn nhật ký" "im lặng / mất" "$(kind) / $(coma "$S/core-touched")"

# Dòng hỏng đúng hình lỗi emoji cũ của on-edit (đường dẫn + byte mồ côi 0x90 + đuôi
# payload), rồi core-touched được ghi THÊM sau lượt review.
clean; co; mkdir -p "$R/docs/quy-uoc"
touch -d '10 minutes ago' "$R/src/BE/Core/A.cs"
printf 'src/BE/Core/A.cs\ndocs/quy-uoc/fe-architecture.md\x90 [ADR-0064](../adr/0064-x.md))","new_string":"y\n' > "$S/core-touched"
touch -d '5 minutes ago' "$R/docs/quy-uoc"
: > "$S/core-reviewed"; sleep 1
printf 'src/BE/Core/A.cs\n' >> "$S/core-touched"
runstop 0; row on-stop "dòng hỏng + core-touched ghi thêm sau review — thả" "im lặng / mất" "$(kind) / $(coma "$S/core-touched")"

# ---- nhắc build/test, theo phiên
soft; clean; mkdir -p "$PS"; : > "$PS/src-touched"
runstop 0; row on-stop "chỉ sửa src/ — nhắc không chặn" "systemMessage" "$(kind)"
row on-stop "nhắc src/ — xoá src-touched của phiên" "mất" "$(coma "$PS/src-touched")"
clean; mkdir -p "$S/phien/s2"; : > "$S/phien/s2/src-touched"
runstop 0; row on-stop "src-touched của phiên khác — không nhắc phiên này" "im lặng" "$(kind)"
clean; co; : > "$PS/src-touched"; printf 'src/BE/Core/A.cs\n' > "$S/core-touched"
runstop 0; row on-stop "chạm Core + sửa src/ — lời nhắc gộp vào lý do chặn" "block / có" "$(kind) / $(has 'Hook KHÔNG chạy cổng build/test')"

# ---- khối core-paths lỗi: báo một lần mỗi phiên
clean; printf 'core-paths: x: không thấy mốc\n' > "$S/core-paths-error"
runstop 0; row on-stop "khối core-paths lỗi — báo" "block" "$(kind)"
runstop 0; row on-stop "khối core-paths lỗi — không báo lại cùng phiên khi lỗi không mới" "im lặng" "$(kind)"
runstop 0 "" s2; row on-stop "khối core-paths lỗi — phiên khác vẫn được báo" "block" "$(kind)"
clean; printf 'core-paths: x\n' > "$S/core-paths-error"
runstop 1; row on-stop "khối core-paths lỗi, stop_hook_active — không chặn" "im lặng" "$(kind)"

rm -rf "$R/src"; hard; clean; co; printf 'src/BE/Core/A.cs\n' > "$S/core-touched"; printf 'e\n' > "$S/core-paths-error"
runstop 0; row on-stop "KHÔNG có src/, đã gắn SubagentStop — lớp 3 không bật" "im lặng" "$(kind)"

# JSON thật với lý do nhiều dòng, nháy, gạch chéo ngược, tab, CRLF
mkdir -p "$R/src/BE/Core"; soft; clean; co
printf 'src/BE/Core/"nhay"\\x\tTab.cs\r\n' > "$S/core-touched"
runstop 0; row on-stop "ký tự khó trong lý do (nháy, \\\\, tab, CR)" "block" "$(kind)"

# ---- thư viện mất, dọn thư mục phiên cũ
mv "$R/.claude/hooks/lib.sh" "$WORK/lib.bak"; clean
runstop 0; row on-stop "lib.sh mất — chặn và nói ra" "block / có" "$(kind) / $(has 'lib.sh')"
runstop 1; row on-stop "lib.sh mất, stop_hook_active — thả" "im lặng" "$(kind)"
mv "$WORK/lib.bak" "$R/.claude/hooks/lib.sh"
clean; mkdir -p "$S/phien/cu"; touch -d '10 days ago' "$S/phien/cu"
runstop 0; row on-stop "thư mục phiên không hoạt động quá 7 ngày bị dọn, phiên đang chạy giữ" "mất / còn" "$(coma "$S/phien/cu") / $(coma "$PS")"

###############################################################################
header on-subagent-stop.sh
R=$(mkrepo sub)
S="$R/.claude/.state"
sst() { rm -rf "$S"; printf '%s' "$2" | bash "$R/.claude/hooks/on-subagent-stop.sh" 2>/dev/null; local rc=$?
  row on-subagent-stop "$1" "$3" "$([ -e "$S/core-reviewed" ] && echo dấu || echo 'không dấu') log=$([ -s "$S/subagent-stop-error.log" ] && echo 1 || echo 0) rc=$rc"; }
sst "agent_type core-reviewer" '{"session_id":"s","hook_event_name":"SubagentStop","agent_type":"core-reviewer","stop_hook_active":false}' "dấu log=0 rc=0"
# Không ghi dấu thì PHẢI để lại lý do — kể cả khi agent_type chỉ đơn giản là khác.
# Matcher ^core-reviewer$ đã lọc; tới được đây với tên khác là dị thường.
sst "agent_type backend-expert — không dấu, CÓ log" '{"session_id":"s","hook_event_name":"SubagentStop","agent_type":"backend-expert"}' "không dấu log=1 rc=0"
row on-subagent-stop "log nêu tên agent_type lạ" "có" "$(grep -q "'backend-expert'" "$S/subagent-stop-error.log" 2>/dev/null && echo có || echo không)"
sst "không có agent_type" '{"session_id":"s","hook_event_name":"SubagentStop","agent_id":"a1"}' "không dấu log=1 rc=0"
sst "báo cáo chứa chuỗi agent_type giả" '{"agent_type":"backend-expert","last_assistant_message":"\"agent_type\":\"core-reviewer\""}' "không dấu log=1 rc=0"
sst "stdin rỗng" '' "không dấu log=1 rc=0"
sst "agent_type rỗng" '{"hook_event_name":"SubagentStop","agent_type":""}' "không dấu log=1 rc=0"
sst "agent_type không phải chuỗi" '{"hook_event_name":"SubagentStop","agent_type":null}' "không dấu log=1 rc=0"
sst "JSON cụt" '{"hook_event_name":"SubagentStop","agent_type":"core-rev' "không dấu log=1 rc=0"

# Hình payload THẬT, bắt từ Claude Code 2.1.280 (2026-09-24): thứ tự khoá như dưới,
# last_assistant_message là UTF-8 thô (không thoát \u), và background_tasks liệt kê
# mọi subagent đang chạy nền của phiên cha — mỗi mục mang khoá agent_type RIÊNG,
# đứng SAU khoá tầng ngoài. Kể cả chính subagent đang dừng cũng có mặt ở đó.
ssp() { # <agent_type tầng ngoài> <last_assistant_message đã thoát JSON> <background_tasks JSON>
  printf '{"session_id":"s","transcript_path":"C:\\\\Users\\\\x\\\\t.jsonl","cwd":"D:\\\\Repo","prompt_id":"p","permission_mode":"default","agent_id":"a1","agent_type":"%s","hook_event_name":"SubagentStop","stop_hook_active":false,"agent_transcript_path":"C:\\\\Users\\\\x\\\\subagents\\\\agent-a1.jsonl","last_assistant_message":"%s","background_tasks":%s,"session_crons":[]}' "$1" "$2" "$3"
}
E4=$'\xF0\x9F\x93\x90'   # 📐 U+1F4D0 — ký tự UTF-8 bốn byte
BG_SELF='{"id":"a1","type":"subagent","status":"running","description":"Soát Core phạm vi BE","agent_type":"core-reviewer"}'
BG_OTHER='{"id":"a2","type":"subagent","status":"running","description":"Spec màn hình","agent_type":"design-expert"}'
# Trên Git Bash bản sed cũ trả `core-reviewer<0x90> ĐÍCH ĐẾN…` — không khớp nhánh nào, thoát im lặng.
sst "payload thật — last_assistant_message chứa emoji 4 byte" "$(ssp core-reviewer "Đã bàn giao. $E4 ĐÍCH ĐẾN còn 2 mục, \\\"nháy\\\"" "[$BG_SELF]")" "dấu log=0 rc=0"
# Mẫu tham lam bám agent_type CUỐI — của design-expert đang chạy nền.
sst "payload thật — background_tasks có subagent khác chạy nền" "$(ssp core-reviewer "Đã bàn giao." "[$BG_SELF,$BG_OTHER]")" "dấu log=0 rc=0"
# Chiều nguy hiểm: một agent khác dừng trong lúc core-reviewer chạy nền — bản cũ ghi dấu "đã review".
sst "tầng ngoài backend-expert, background_tasks có core-reviewer" "$(ssp backend-expert "Xong." "[$BG_SELF]")" "không dấu log=1 rc=0"
sst "khoá tầng ngoài đứng SAU background_tasks và chuỗi có emoji" "{\"background_tasks\":[$BG_OTHER],\"last_assistant_message\":\"\\\"agent_type\\\":\\\"x\\\" $E4\",\"agent_type\":\"core-reviewer\"}" "dấu log=0 rc=0"
mv "$R/.claude/hooks/lib.sh" "$WORK/lib.bak"
sst "lib.sh mất — không dấu, CÓ log" '{"session_id":"s","hook_event_name":"SubagentStop","agent_type":"core-reviewer"}' "không dấu log=1 rc=0"
mv "$WORK/lib.bak" "$R/.claude/hooks/lib.sh"

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
# Chuỗi lệnh hook THẬT trong settings.json. Harness chạy lệnh hook ở thư mục làm
# việc HIỆN TẠI của phiên — thư mục đó đổi theo mỗi lần `cd` — nên một lệnh viết
# bằng đường dẫn tương đối (`bash .claude/hooks/x.sh`) hỏng im lặng ngay khi phiên
# đứng ở thư mục con: lớp chặn git thứ hai, việc ghi dấu và cổng tài liệu cùng mất.
# $CLAUDE_PROJECT_DIR thì harness đặt cố định ở gốc dự án.
header settings.json
R=$(mkrepo cfg)
cp "$REALSET" "$R/.claude/settings.json"
RU=$(cd "$R" && pwd)
if RW=$(cd "$R" && pwd -W 2>/dev/null) && [ -n "$RW" ]; then :; else RW="$RU"; fi
mkdir -p "$R/src/FE/src/app"
cmds=$(node -e '
  const o = JSON.parse(require("fs").readFileSync(process.argv[1], "utf8"));
  for (const [ev, arr] of Object.entries(o.hooks || {}))
    for (const m of arr) for (const h of (m.hooks || []))
      if (h.type === "command") console.log(ev + "\t" + h.command);
' "$REALSET")
n=0
while IFS=$'\t' read -r ev cmd; do
  [ -z "$ev" ] && continue
  n=$((n+1))
  case "$cmd" in *'$CLAUDE_PROJECT_DIR'*|*'${CLAUDE_PROJECT_DIR}'*) neo=có ;; *) neo=không ;; esac
  row settings "$ev — lệnh neo vào \$CLAUDE_PROJECT_DIR" "có" "$neo"
  case "$ev" in
    PreToolUse)   pl='{"session_id":"s","hook_event_name":"PreToolUse","tool_name":"Bash","tool_input":{"command":"git status"}}' ;;
    PostToolUse)  pl='{"session_id":"s","hook_event_name":"PostToolUse","tool_name":"Bash","tool_input":{"command":"ls"}}' ;;
    SubagentStop) pl='{"session_id":"s","hook_event_name":"SubagentStop","agent_type":"core-reviewer"}' ;;
    *)            pl='{"session_id":"s","hook_event_name":"Stop","stop_hook_active":false}' ;;
  esac
  ( cd "$R/src/FE/src/app" && printf '%s' "$pl" | CLAUDE_PROJECT_DIR="$RW" bash -c "$cmd" > "$WORK/cfg.out" 2> "$WORK/cfg.err" ); rc=$?
  nf=$(grep -c 'No such file' "$WORK/cfg.err")
  row settings "$ev — chạy được khi phiên đứng ở thư mục con" "rc=0 thiếu-tệp=0" "rc=$rc thiếu-tệp=$nf"
done <<< "$cmds"
row settings "settings.json thật có lệnh hook để kiểm" "có" "$([ "$n" -ge 1 ] && echo có || echo không)"
# Canary: dạng tương đối cũ, cùng thư mục con — phải hỏng, tức hai loại ca trên có răng.
( cd "$R/src/FE/src/app" && printf '{}' | CLAUDE_PROJECT_DIR="$RW" bash -c 'bash .claude/hooks/on-stop.sh' >/dev/null 2>&1 ); rc=$?
row settings "canary — lệnh tương đối cũ, đứng ở thư mục con thì hỏng" "rc=127" "rc=$rc"

###############################################################################
# Mỗi nhóm phải tự chứng minh nó có ca — một nhóm không chạy ca nào mà vẫn in
# "0 SAI" là một bộ test không kiểm gì.
for g in $GROUPS_ALL; do
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
for g in $GROUPS_ALL; do summary="$summary $g=${GROUPN[$g]:-0}"; done
printf '\nSố ca theo nhóm:%s\n' "$summary"
printf 'Tổng: %s OK, %s SAI\n' "$NPASS" "$NFAIL"
[ "$NFAIL" -eq 0 ] || exit 1
exit 0
