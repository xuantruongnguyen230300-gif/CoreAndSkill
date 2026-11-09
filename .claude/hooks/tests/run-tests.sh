#!/usr/bin/env bash
# run-tests.sh — bộ test payload cho các hook ở .claude/hooks/.
#
# Chạy từ gốc repo:
#     bash .claude/hooks/tests/run-tests.sh        # in ca SAI + tổng
#     bash .claude/hooks/tests/run-tests.sh -v     # in mọi ca
#
# Thoát 0 = mọi ca OK · 1 = có ca SAI · 2 = không chạy được (thiếu node/git/timeout).
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
#   - on-pretool đọc danh sách cấm từ `permissions.deny` và danh sách cho phép git
#     từ các mục `Bash(git …)` của `permissions.allow` — của settings.json THẬT
#     (ca "phải chặn" / "cho qua" kiểm chính cấu hình đang dùng, không kiểm bản chép);
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

for need in node git timeout; do
  command -v "$need" >/dev/null 2>&1 || { echo "ABORT — thiếu '$need' trên PATH; bộ test cần nó để dựng payload JSON / repo giả / chạy hook dưới giới hạn giờ như harness." >&2; exit 2; }
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
  # ghi một dòng vào .claude/.state/stub-ran. Mã 1 in nguyên văn
  # .claude/.state/stub-fail nếu có (ca test tự chọn TẬP LỖI), không thì một dòng
  # FAIL mặc định. Cả ba nằm trong thư mục trạng thái — ngoài tầm quét "có gì mới
  # hơn lần xanh", nên chính việc đếm hay đổi tập lỗi không làm cổng chạy lại.
  cat > "$R/.claude/check-docs.sh" <<'EOF'
#!/usr/bin/env bash
cd "$(dirname "$0")/.." || exit 2
mkdir -p .claude/.state
echo x >> .claude/.state/stub-ran
rc=$(cat .claude/.state/stub-rc 2>/dev/null || echo 0)
if [ "$rc" = 1 ]; then
  if [ -f .claude/.state/stub-fail ]; then cat .claude/.state/stub-fail
  else echo "  FAIL  [§3] docs/x.md:1 — vi phạm giả \"có nháy\""; fi
fi
[ "$rc" = 2 ] && echo "ABORT — giả lập"
exit "$rc"
EOF
  fixture_doc > "$R/docs/kien-truc-core-module.md"
  ( cd "$R" && git init -q . >/dev/null 2>&1 )
  printf '%s' "$R"
}

# payload JSON dựng bằng node — đúng cách harness tuần tự hoá, kể cả gạch chéo ngược.
# pj <event> <tool> <key> <file-chứa-giá-trị> [tool_response-file]
# session_id lấy từ biến PJ_SID (mặc định s1). PJ_DUR khác rỗng: payload PostToolUse /
# PostToolUseFailure mang thêm tool_use_id và duration_ms (mili giây) — hình payload thật
# đo trên Claude Code 2.1.282 và 2.1.283 (2026-09-30): PostToolUse xếp tool_input,
# tool_response, tool_use_id, duration_ms; PostToolUseFailure (lệnh thoát khác 0) không
# có tool_response mà có tool_use_id, error, is_interrupt, duration_ms.
PJ_SID=s1
PJ_DUR=""
pj() {
  PJSID="$PJ_SID" PJDUR="$PJ_DUR" node -e '
    const fs = require("fs");
    const [ev, tool, key, vf, rf] = process.argv.slice(1);
    const o = { session_id: process.env.PJSID, transcript_path: "C:\\Users\\x\\t.jsonl", cwd: "D:\\Repo",
                permission_mode: "default", hook_event_name: ev, tool_name: tool, tool_input: {} };
    o.tool_input[key] = fs.readFileSync(vf, "utf8");
    if (key === "command") o.tool_input.description = "mô tả \"command\": \"git push\"";
    if (rf) o.tool_response = { stdout: fs.readFileSync(rf, "utf8") };
    if (ev === "PostToolUse" || ev === "PostToolUseFailure") {
      if (ev === "PostToolUseFailure") delete o.tool_response;
      o.tool_use_id = "toolu_01Test";
      if (ev === "PostToolUseFailure") { o.error = "Exit code 1\nbuild failed"; o.is_interrupt = false; }
      if (process.env.PJDUR !== "") o.duration_ms = Number(process.env.PJDUR);
    }
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
reset_state
printf '{"session_id":"s1","tool_name":"Read","tool_input":{"pattern":"x"}}' | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null; rc=$?
row on-edit "payload không file_path, không command — không dấu nào" "src=0 core=0 co=0 rc=0" "$(st) rc=$rc"

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
# Payload shell mang duration_ms như payload thật; ca "không có duration_ms" ở dưới.
PJ_DUR=1500
reset_state; lamcu
printf 'a' > "$R/src/FE/src/app/core/a.ts"
printf 'b' > "$R/src/BE/Core/obj/gen.json"
printf 'c' > "$R/src/FE/node_modules/pkg/i.js"
pj PostToolUse Bash command "$(valf 'echo x > src/FE/src/app/core/a.ts')" | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null; rc=$?
row on-edit "shell — vừa sửa Core FE + file gitignore" "src=1 core=1 co=1 rc=0" "$(st) rc=$rc"
row on-edit "shell — core-touched chỉ có file không bị gitignore" "src/FE/src/app/core/a.ts" "$(tr '\n' ' ' < "$S/core-touched" 2>/dev/null | sed 's/ $//')"

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

# ---- cửa sổ quét = khoảng của CHÍNH lời gọi, không phải "từ lần quét trước của phiên"
# Lỗi cũ: nhánh shell quét mọi tệp mới hơn mốc lần quét trước của phiên, nên thay đổi
# của phiên khác, của IDE, của người dùng giữa hai lời gọi đều bị tính cho phiên đang
# chạy lệnh — phiên chỉ chạy `ls` cũng mang cờ touched-core và bị hook Stop chặn.
# Giả lập "lời gọi trước xảy ra từ lâu" bằng cách lùi mtime MỌI thứ trong thư mục trạng
# thái phiên — kiểu mốc nào (scan-stamp của bản cũ hay mốc khác) cũng lùi theo, nên ca
# kiểm hành vi, không kiểm tên tệp mốc.
lui() { local s=$1; shift; touch -d "@$(( $(date +%s) - s ))" "$@"; }   # <giây> <tệp>...: mtime về <giây> trước
lui_phien() { [ -d "$S/phien" ] && find "$S/phien" -exec touch -d "@$(( $(date +%s) - $1 ))" {} + 2>/dev/null; return 0; }
shpost() { # <session_id> <duration_ms, rỗng = không khoá> <lệnh> [sự kiện]
  local PJ_SID=$1 PJ_DUR=$2
  pj "${4:-PostToolUse}" Bash command "$(valf "$3")" | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null
}
co_phien() { [ -e "$S/phien/$1/touched-core" ] && echo 1 || echo 0; }
trong_nk() { grep -qxF "$1" "$S/core-touched" 2>/dev/null && echo có || echo không; }

# Tái hiện ca 1 của lượt soát: s1 sửa Core, s2 chỉ chạy `ls`.
reset_state; lamcu
shpost s2 300 'ls'
lui_phien 120
printf 'k' > "$R/src/BE/Core/Core.Domain/PhienKhac.cs"; lui 40 "$R/src/BE/Core/Core.Domain/PhienKhac.cs"
shpost s1 45000 'dotnet format'
shpost s2 300 'ls'
row on-edit "cửa sổ — phiên s1 sửa Core, phiên s2 chỉ chạy ls: cờ ở s1, s2 không mang cờ" "s1=1 s2=0 nhật ký có" \
  "s1=$(co_phien s1) s2=$(co_phien s2) nhật ký $(trong_nk src/BE/Core/Core.Domain/PhienKhac.cs)"

# Tái hiện ca 2: người dùng sửa Core trong IDE giữa hai lời gọi, phiên chạy `git status`.
reset_state; lamcu
shpost s1 300 'ls'
lui_phien 120
printf 'u' > "$R/src/FE/src/app/core/ide.ts"; lui 60 "$R/src/FE/src/app/core/ide.ts"
shpost s1 400 'git status'
row on-edit "cửa sổ — tệp Core đổi GIỮA hai lời gọi (IDE, người dùng): không tính cho phiên" "src=0 core=0 co=0" "$(st)"

reset_state; lamcu
printf 't' > "$R/src/BE/Core/Core.Domain/Truoc.cs"; lui 60 "$R/src/BE/Core/Core.Domain/Truoc.cs"
shpost s1 30000 'dotnet build'
row on-edit "cửa sổ — tệp đổi 60 s trước, lời gọi chỉ dài 30 s: không tính" "src=0 core=0 co=0" "$(st)"

reset_state; lamcu
printf 'c' > "$R/src/BE/Core/Core.Domain/ChinhMinh.cs"; lui 2 "$R/src/BE/Core/Core.Domain/ChinhMinh.cs"
shpost s1 3000 'sed -i s/a/b/ src/BE/Core/Core.Domain/ChinhMinh.cs'
row on-edit "cửa sổ — lệnh shell của chính phiên ghi tệp Core: có cờ" "src=1 core=1 co=1" "$(st)"

# Hai đầu của độ trễ cho phép (DO_TRE): hook chạy 2 s sau khi lệnh 3 s xong, tệp ghi ở
# giây đầu của lệnh — vẫn phải tính; tệp đổi 20 s trước một lời gọi 0,4 s — không tính.
reset_state; lamcu
printf 'd' > "$R/src/BE/Core/Core.Domain/DauLenh.cs"; lui 5 "$R/src/BE/Core/Core.Domain/DauLenh.cs"
shpost s1 3000 'sed -i s/a/b/ src/BE/Core/Core.Domain/DauLenh.cs && sleep 3'
row on-edit "cửa sổ — hook trễ 2 s sau lệnh, tệp ghi ở đầu lệnh: vẫn tính" "src=1 core=1 co=1" "$(st)"
reset_state; lamcu
printf 'e' > "$R/src/BE/Core/Core.Domain/HaiMuoiGiay.cs"; lui 20 "$R/src/BE/Core/Core.Domain/HaiMuoiGiay.cs"
shpost s1 400 'ls'
row on-edit "cửa sổ — tệp đổi 20 s trước lời gọi 0,4 s: không tính (độ trễ cho phép chỉ vài giây)" "src=0 core=0 co=0" "$(st)"

# Lệnh chạy lâu (build, format cả solution) sửa tệp ở ĐẦU lệnh: lúc hook chạy tệp đó đã
# cũ hơn mọi cửa sổ cố định. Lời gọi đầu tiên của phiên — bản cũ không có mốc, lùi về 90 s.
reset_state; lamcu
printf 'z' > "$R/src/BE/Core/Core.Domain/Lau.cs"; lui 300 "$R/src/BE/Core/Core.Domain/Lau.cs"
shpost s1 360000 'dotnet format'
row on-edit "cửa sổ — lệnh dài 6 phút, sửa tệp Core ở phút đầu: có cờ" "src=1 core=1 co=1" "$(st)"
shpost s1 300 'ls'
row on-edit "cửa sổ — lời gọi sau không ghi lại tệp cũ hơn khoảng của nó" "1" "$(grep -c 'Lau.cs' "$S/core-touched" 2>/dev/null)"
rm -f "$R/src/BE/Core/Core.Domain/Lau.cs"

# Nhiều subagent của cùng phiên chạy song song, cùng session_id: mỗi lời gọi phải thấy
# tệp chính nó ghi, dù lời gọi kia kết thúc trước. Một mốc chung theo phiên bị lời gọi
# khác ghi đè thì A mất tệp ghi ở đầu lời gọi.
reset_state; lamcu
printf 'a' > "$R/src/BE/Core/Core.Domain/SongSongA.cs"; lui 25 "$R/src/BE/Core/Core.Domain/SongSongA.cs"
printf 'b' > "$R/src/FE/src/app/core/song-song-b.ts"; lui 2 "$R/src/FE/src/app/core/song-song-b.ts"
shpost s1 5000 'npx prettier --write src/FE/src/app/core/song-song-b.ts'
shpost s1 30000 'dotnet format src/BE/Core'
row on-edit "song song — hai lời gọi cùng phiên, mỗi lời gọi ghi một tệp Core: cả hai vào nhật ký" "A có / B có / cờ 1" \
  "A $(trong_nk src/BE/Core/Core.Domain/SongSongA.cs) / B $(trong_nk src/FE/src/app/core/song-song-b.ts) / cờ $(co_phien s1)"

# duration_ms lấy ở TẦNG NGOÀI của payload. Khoá "duration_ms" giả nằm trong chuỗi (lệnh,
# stdout) và trong object lồng, đứng cả TRƯỚC lẫn SAU khoá thật: bộ trích lấy lần xuất
# hiện đầu hay cuối đều nhận 999999 ms (16 phút) và tính tệp 60 s trước.
reset_state; lamcu
printf 's' > "$R/src/BE/Core/Core.Domain/Gia.cs"; lui 60 "$R/src/BE/Core/Core.Domain/Gia.cs"
node -e '
  const o = { session_id: "s1", hook_event_name: "PostToolUse", tool_name: "Bash",
              tool_input: { command: "echo \"\\\"duration_ms\\\":999999\"", description: "x" },
              tool_response: { stdout: "{\"duration_ms\": 999999}", meta: { duration_ms: 999999 } },
              tool_use_id: "toolu_01Test", duration_ms: 2000, extra: [{ duration_ms: 999999 }] };
  process.stdout.write(JSON.stringify(o));
' | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null
row on-edit "cửa sổ — khoá \"duration_ms\" giả trong chuỗi và object lồng: dùng khoá tầng ngoài" "src=0 core=0 co=0" "$(st)"

# Lệnh thoát khác 0 bắn PostToolUseFailure, KHÔNG bắn PostToolUse (đo thật). Payload
# không có tool_response, có error / is_interrupt / duration_ms — hook quét như thường.
reset_state; lamcu
printf 'f' > "$R/src/BE/Core/Core.Domain/Loi.cs"; lui 3 "$R/src/BE/Core/Core.Domain/Loi.cs"
shpost s1 4000 'sed -i s/a/b/ src/BE/Core/Core.Domain/Loi.cs && dotnet build' PostToolUseFailure
row on-edit "cửa sổ — payload PostToolUseFailure (lệnh thoát khác 0): quét như PostToolUse" "src=1 core=1 co=1" "$(st)"

# Payload không có duration_ms (harness khác): lùi về cửa sổ 90 giây.
reset_state; lamcu
printf 'n' > "$R/src/BE/Core/Core.Domain/Gan.cs"; lui 30 "$R/src/BE/Core/Core.Domain/Gan.cs"
printf 'o' > "$R/src/BE/Core/Core.Domain/Xa.cs"; lui 150 "$R/src/BE/Core/Core.Domain/Xa.cs"
shpost s1 '' 'ls'
row on-edit "cửa sổ — không có duration_ms: lùi về 90 s (30 s trước có, 150 s trước không)" "có / không" \
  "$(trong_nk src/BE/Core/Core.Domain/Gan.cs) / $(trong_nk src/BE/Core/Core.Domain/Xa.cs)"
rm -f "$R/src/BE/Core/Core.Domain/Gan.cs" "$R/src/BE/Core/Core.Domain/Xa.cs"

# Không mốc nào tích tụ trong thư mục phiên: chỉ còn dấu touched-core / src-touched.
shpost s1 300 'git status'; shpost s2 300 'ls'; shpost s2 800 'dotnet build' PostToolUseFailure
row on-edit "cửa sổ — sau nhiều lời gọi của hai phiên, thư mục phiên không còn tệp mốc nào" "không" \
  "$(find "$S/phien" -type f ! -name touched-core ! -name src-touched 2>/dev/null | grep -q . && echo có || echo không)"

reset_state
cp "$R/docs/kien-truc-core-module.md" "$WORK/doc.bak"
printf '# không còn khối\n' > "$R/docs/kien-truc-core-module.md"
edt "có src — khối Core mất: vẫn ghi src" Edit file_path "$RB\\src\\BE\\Core\\Core.Domain\\X.cs" "src=1 core=0 co=0"
row on-edit "có src — khối Core mất: ghi lý do vào core-paths-error" "có lý do" "$([ -s "$S/core-paths-error" ] && echo 'có lý do' || echo 'không')"
cp "$WORK/doc.bak" "$R/docs/kien-truc-core-module.md"
edt "có src — khối sửa lại: dấu lỗi bị xoá" Edit file_path "$RB\\src\\BE\\Core\\Core.Domain\\X.cs" "src=1 core=1 co=1"
row on-edit "có src — khối sửa lại: không còn core-paths-error" "không" "$([ -e "$S/core-paths-error" ] && echo có || echo không)"

reset_state; lamcu
printf 'x' > "$R/docs/quy-uoc/be-y.md"
pj PostToolUse Bash command "$(valf 'ls')" | bash "$R/.claude/hooks/on-edit.sh" 2>/dev/null; rc=$?
row on-edit "shell — vừa sửa docs/quy-uoc/ (ngoài khối Core, ngoài src/): không dấu nào" "src=0 core=0 co=0 rc=0" "$(st) rc=$rc"
PJ_DUR=""

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
# Cổng đỏ chặn MỘT lần mỗi tập lỗi mỗi phiên (ADR-0111 quyết định 5). Ca nào dùng
# "block" làm dấu hiệu "cổng đã chạy và không bị hoãn" thì cần một tập lỗi CHƯA gặp:
# mỗi lần gọi, cổng giả in một dòng FAIL khác. Kiểm hành vi, không kiểm tên tệp dấu.
LOI_N=0
moi_loi() { LOI_N=$((LOI_N+1)); mkdir -p "$S"; printf '  FAIL  [§3] docs/x.md:%s — vi phạm giả mới "có nháy"\n' "$LOI_N" > "$S/stub-fail"; }
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
runstop 1; row on-stop "cổng đỏ, dừng lần hai cùng tập lỗi (stop_hook_active) — không chặn, báo người dùng" "systemMessage" "$(kind)"
stub 0; runstop 0; row on-stop "sửa xong, cổng xanh — thả, dời mốc" "im lặng / mốc mới hơn tệp" "$(kind) / $([ "$S/gate-green" -nt "$R/docs/moi.md" ] && echo 'mốc mới hơn tệp' || echo 'mốc cũ')"

# ---- cổng đỏ vì tệp của phiên khác / người dùng: mỗi tập lỗi chặn MỘT lần mỗi phiên
# ADR-0111 quyết định 5. Lỗi cũ: phiên s2 không sửa gì vẫn bị chặn mỗi lần dừng,
# kể cả khi stop_hook_active=true — vòng chặn không đáy.
soft; clean; xanh; stub 1
printf 'n' > "$R/docs/nguoi-khac.md"
runstop 0 "" s2; row on-stop "tập lỗi — s2 không sửa gì, cổng đỏ: chặn lần đầu" "block / có" "$(kind) / $(has 'docs/x.md:1')"
runstop 1 "" s2; row on-stop "tập lỗi — s2 dừng lại, cùng tập, stop_hook_active: không chặn, báo người dùng kèm lỗi" "systemMessage / có" "$(kind) / $(has 'docs/x.md:1')"
runstop 0 "" s2; row on-stop "tập lỗi — lượt sau của s2, vẫn cùng tập: không chặn, vẫn báo" "systemMessage" "$(kind)"
runstop 0 "" s3; row on-stop "tập lỗi — phiên s3 gặp cùng tập lần đầu: chặn (dấu theo phiên)" "block" "$(kind)"
moi_loi
runstop 1 "" s2; row on-stop "tập lỗi — tập đổi (lỗi mới), kể cả stop_hook_active: chặn lại" "block" "$(kind)"
runstop 1 "" s2; row on-stop "tập lỗi — tập mới đã chặn một lần: không chặn" "systemMessage" "$(kind)"
printf '  FAIL  [§3] docs/x.md:%s — vi phạm giả mới "có nháy"\n  FAIL  [§12] docs/them.md — thiếu khoá: kind\n' "$LOI_N" > "$S/stub-fail"
runstop 1 "" s2; row on-stop "tập lỗi — tập cũ cộng thêm một lỗi: chặn lại" "block" "$(kind)"
stub 0
runstop 0 "" s2; row on-stop "tập lỗi — cổng xanh: không chặn, không báo" "im lặng" "$(kind)"
stub 1; printf 'm' > "$R/docs/nguoi-khac.md"
runstop 0 "" s2; row on-stop "tập lỗi — cùng tập quay lại sau một lần xanh: chặn lại" "block" "$(kind)"
# Lần xanh do PHIÊN KHÁC chạy (s3), rồi tập cũ quay lại: dấu của s2 có từ trước lần
# xanh đó, không được dùng để thả s2.
stub 0; printf 'g' > "$R/docs/nguoi-khac.md"; runstop 0 "" s3
stub 1; printf 'k' > "$R/docs/nguoi-khac.md"
runstop 0 "" s2; row on-stop "tập lỗi — xanh do phiên khác chạy, tập cũ quay lại: chặn lại" "block" "$(kind)"
# Tập đã chặn một lần + phiên vừa sửa src/: MỘT systemMessage mang cả lời báo cổng đỏ
# lẫn lời nhắc build/test — không lời nào nuốt lời kia.
mkdir -p "$S/phien/s2"; : > "$S/phien/s2/src-touched"
runstop 0 "" s2; row on-stop "tập lỗi đã chặn + sửa src/ — một systemMessage gồm báo cổng đỏ và nhắc build/test" "systemMessage / có / có" \
  "$(kind) / $(has 'docs/x.md:1') / $(has 'Hook KHÔNG chạy cổng build/test')"
stub 0; rm -f "$S/stub-fail"

# ---- đầu ra lỗi cổng: dòng FAIL nguyên vẹn, 20 dòng đầu, báo số còn lại
clean; xanh; stub 1
{ printf '  NOTE  dòng NOTE có chữ FAIL không phải lỗi\n'
  for i in $(seq 1 25); do printf '  FAIL  [§3] docs/nhieu.md:%s — link chết\n' "$i"; done; } > "$S/stub-fail"
printf 'n' > "$R/docs/nhieu.md"
runstop 0; row on-stop "hơn 20 lỗi — in 20 dòng đầu, báo số còn lại" "block / có / không / có" "$(kind) / $(has 'nhieu.md:20 ') / $(has 'nhieu.md:21 ') / $(has '(+5 lỗi nữa)')"
row on-stop "dòng NOTE chứa chữ FAIL không bị tính là lỗi" "không" "$(has 'dòng NOTE có chữ FAIL')"
# Ký tự điều khiển THÔ (ESC lẻ không thành mã màu, 0x01, 0x1F) trong dòng lỗi: JSON phải hợp lệ.
clean; xanh; stub 1
printf '  FAIL  [§3] docs/dk.md:1 — ESC lẻ \033 và \001 và \037 cuối\n' > "$S/stub-fail"
printf 'n' > "$R/docs/dk.md"
runstop 0; row on-stop "ký tự điều khiển thô trong dòng lỗi cổng — JSON hợp lệ" "block" "$(kind)"
stub 0; rm -f "$S/stub-fail"

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
moi_loi; runstop 0 "$BG_SHELL"; row on-stop "chỉ việc shell chạy nền — không hoãn" "block" "$(kind)"
moi_loi; runstop 0 "$BG_DONE"; row on-stop "agent nền đã xong — không hoãn" "block" "$(kind)"
moi_loi; runstop 0 "[]"; row on-stop "mảng rỗng — không hoãn" "block" "$(kind)"
moi_loi
stoppl '{"session_id":"s","hook_event_name":"Stop","stop_hook_active":false,"last_assistant_message":"{\"type\":\"subagent\",\"status\":\"running\"}","background_tasks":[]}'
row on-stop "chuỗi agent nền giả trong last_assistant_message — không hoãn" "block" "$(kind)"
moi_loi
stoppl '{"session_id":"s","hook_event_name":"Stop","background_tasks":[{"type":"subagent","status":"run'
row on-stop "payload cụt — không hoãn (hỏng theo hướng chặn)" "block" "$(kind)"
stub 0; rm -f "$S/stub-fail"

# ---- chạm Core
mkdir -p "$R/src/BE/Core"; printf 'x' > "$R/src/BE/Core/A.cs"
soft; clean; co; printf 'src/BE/Core/A.cs\n' > "$S/core-touched"
runstop 0; row on-stop "có src, CHƯA gắn SubagentStop — nhắc một lần" "block" "$(kind)"
row on-stop "nhắc một lần — nêu core-reviewer, không mời 'nói lý do'" "có/không" "$(has core-reviewer)/$(has 'lý do rồi kết thúc')"
row on-stop "nhắc một lần — dọn nhật ký và cờ" "mất/mất" "$(coma "$S/core-touched")/$(coma "$PS/touched-core")"
runstop 1; row on-stop "nhắc một lần — lần Stop sau thả" "im lặng" "$(kind)"

# Chặn cứng theo TẬP tệp Core chưa review (ADR-0114 quyết định 6, 13): mỗi tập chặn
# MỘT lần mỗi phiên; dừng lại với đúng tập đó thì chỉ báo người dùng, không đòi vòng
# review vượt giới hạn hai vòng. Lỗi cũ: chặn mỗi lần dừng tới khi có dấu review, kể
# cả sau vòng 2 — lối thoát duy nhất là người dùng xoá nhật ký. Kiểm hành vi, không
# kiểm tên tệp dấu.
hard; clean; co; printf 'src/BE/Core/A.cs\n' > "$S/core-touched"
runstop 0; row on-stop "ĐÃ gắn SubagentStop, chưa có dấu review — chặn lần đầu" "block" "$(kind)"
row on-stop "chặn cứng — nêu lối thoát của người dùng" "có" "$(has 'người dùng tự xoá .claude/.state/core-touched')"
row on-stop "chặn cứng — dặn gọi reviewer chạy nền" "có" "$(has 'chạy nền')"
row on-stop "chặn cứng — trỏ /core-review, khuôn ba trường, đủ hai vòng thì không gọi vòng ba" "có / có / có" \
  "$(has '/core-review') / $(has 'Phạm vi: BE|FE, phần đổi|toàn bộ. Vòng: n/2.') / $(has 'KHÔNG gọi vòng ba')"
runstop 1; row on-stop "tập Core — dừng lại cùng tập (stop_hook_active): không chặn, báo người dùng kèm tệp" "systemMessage / có" "$(kind) / $(has 'A.cs')"
runstop 0; row on-stop "tập Core — lượt sau, vẫn cùng tập: không chặn, vẫn báo" "systemMessage" "$(kind)"
row on-stop "tập Core đã chặn — giữ nhật ký và cờ" "còn/còn" "$(coma "$S/core-touched")/$(coma "$PS/touched-core")"
runstop 0 "$BG_RUN"; row on-stop "tập Core đã chặn, agent nền đang chạy — hoãn, giữ dấu" "im lặng / còn / còn" "$(kind) / $(coma "$S/core-touched") / $(coma "$PS/touched-core")"
runstop 0 "$BG_SHELL"; row on-stop "tập Core đã chặn, chỉ việc shell chạy nền — không hoãn, báo" "systemMessage" "$(kind)"
printf 'a2' > "$R/src/BE/Core/A.cs"; printf 'src/BE/Core/A.cs\n' >> "$S/core-touched"
runstop 1; row on-stop "tập Core — sửa thêm tệp ĐÃ có trong tập: không chặn lại" "systemMessage" "$(kind)"
printf 'b' > "$R/src/BE/Core/B.cs"; printf 'src/BE/Core/B.cs\n' >> "$S/core-touched"
runstop 1; row on-stop "tập Core — thêm một tệp Core mới, kể cả stop_hook_active: chặn lại" "block / B có / A có" "$(kind) / B $(has 'B.cs') / A $(has 'A.cs')"
runstop 1; row on-stop "tập Core — tập mới đã chặn một lần: không chặn" "systemMessage" "$(kind)"
mkdir -p "$S/phien/s3"; : > "$S/phien/s3/touched-core"
runstop 0 "" s3; row on-stop "tập Core — phiên s3 mang cờ, gặp cùng tập lần đầu: chặn (dấu theo phiên)" "block" "$(kind)"
touch -d '1 minute ago' "$R/src/BE/Core/A.cs" "$R/src/BE/Core/B.cs"; : > "$S/core-reviewed"
runstop 1; row on-stop "dấu review mới hơn file — thả" "im lặng" "$(kind)"
row on-stop "dấu review mới hơn — dọn nhật ký và cờ" "mất/mất" "$(coma "$S/core-touched")/$(coma "$PS/touched-core")"

# Một lượt review mới — vòng nào cũng vậy — làm dấu vân tay cũ hết giá trị: sau đó sửa
# lại ĐÚNG tập đã chặn thì chặn lại, để hook đòi vòng kế mà CLAUDE.md §11 đòi.
hard; clean; co; printf 'r1' > "$R/src/BE/Core/A.cs"; printf 'src/BE/Core/A.cs\n' > "$S/core-touched"
runstop 0; row on-stop "review mới — lần chặn đầu cho tập {A}" "block" "$(kind)"
runstop 1; row on-stop "review mới — dừng lại cùng tập: không chặn" "systemMessage" "$(kind)"
: > "$S/core-reviewed"
printf 'r2' > "$R/src/BE/Core/A.cs"; printf 'src/BE/Core/A.cs\n' >> "$S/core-touched"
runstop 1; row on-stop "review mới hơn lần chặn, rồi sửa lại đúng tập đó — chặn lại" "block / có" "$(kind) / $(has 'A.cs')"
runstop 1; row on-stop "review mới — sau lần chặn lại, dừng cùng tập: không chặn" "systemMessage" "$(kind)"

# Nhật ký chung, cờ theo phiên: phiên không chạm Core không bị việc của phiên khác giữ lại.
hard; clean; printf 'src/BE/Core/A.cs\n' > "$S/core-touched"
runstop 0; row on-stop "phiên KHÔNG chạm Core, nhật ký có việc chưa review — thả, giữ nhật ký" "im lặng / còn" "$(kind) / $(coma "$S/core-touched")"
mkdir -p "$S/phien/s2"; : > "$S/phien/s2/touched-core"
runstop 0 "" s2; row on-stop "phiên s2 mang cờ — chặn s2" "block" "$(kind)"
runstop 0; row on-stop "cùng lúc đó phiên s không mang cờ — vẫn thả" "im lặng" "$(kind)"
rm -f "$S/core-touched"
runstop 0 "" s2; row on-stop "người dùng xoá nhật ký — phiên mang cờ được thả, gỡ cờ" "im lặng / mất" "$(kind) / $(coma "$S/phien/s2/touched-core")"
# Tập đã chặn đi theo nhật ký: s2 chạm lại đúng tệp đó sau khi người dùng xoá nhật ký
# là việc mới — chặn lần đầu, không coi là "đã chặn".
: > "$S/phien/s2/touched-core"; printf 'src/BE/Core/A.cs\n' > "$S/core-touched"
runstop 0 "" s2; row on-stop "người dùng đã xoá nhật ký, s2 chạm lại đúng tập cũ — chặn lần đầu" "block" "$(kind)"

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
printf 'loi\n' > "$S/subagent-stop-error.log"
runstop 0; row on-stop "file Core xoá SAU dấu review (không dòng nào ghi thêm) — chặn" "block / có" "$(kind) / $(has 'DaXoa.cs')"
row on-stop "chặn cứng kèm log SubagentStop — trỏ tới log" "có" "$(has 'subagent-stop-error.log')"
runstop 1; row on-stop "cùng tập đã chặn, có log SubagentStop — lời báo người dùng trỏ tới log" "systemMessage / có" "$(kind) / $(has 'subagent-stop-error.log')"

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

# JSON thật với lý do nhiều dòng, nháy, gạch chéo ngược, tab, CRLF
soft; clean; co
printf 'src/BE/Core/"nhay"\\x\tTab.cs\r\n' > "$S/core-touched"
runstop 0; row on-stop "ký tự khó trong lý do (nháy, \\\\, tab, CR)" "block" "$(kind)"
# Ký tự điều khiển THÔ khác tab/CR/LF — 0x01, ESC, BEL, 0x0B, 0x0C, 0x1F: JSON hỏng thì
# harness bỏ quyết định chặn, tức lượt kết thúc trong im lặng.
clean; co
printf 'src/BE/Core/\001Ctl\033[0\007\013\014\037.cs\n' > "$S/core-touched"
runstop 0; row on-stop "ký tự điều khiển thô (0x01, ESC, BEL, VT, FF, 0x1F) trong lý do" "block" "$(kind)"

# Tập lỗi cổng đã chặn một lần thì cổng thôi chặn — lớp core-reviewer vẫn xét riêng:
# phiên đã chạm Core vẫn bị chặn lần đầu cho tập tệp Core, và người dùng vẫn được báo
# cổng còn đỏ. Cả hai tập đã chặn một lần: một systemMessage mang cả hai lời báo.
hard; clean; printf 'q' > "$R/src/BE/Core/A.cs"; xanh; stub 1
printf 'r' > "$R/docs/do.md"
runstop 0; row on-stop "cổng đỏ + chạm Core — lần đầu chặn vì cổng" "block / có" "$(kind) / $(has 'docs/x.md:1')"
co; printf 'src/BE/Core/A.cs\n' > "$S/core-touched"
runstop 1; row on-stop "tập lỗi đã chặn một lần, phiên chạm Core chưa review — vẫn chặn vì core-reviewer, vẫn báo cổng đỏ" "block / có / có" \
  "$(kind) / $(has 'core-reviewer') / $(jfield "$WORK/stop.out" systemMessage | grep -q 'docs/x.md:1' && echo có || echo không)"
runstop 1; row on-stop "cả tập lỗi cổng lẫn tập Core đã chặn một lần — không chặn, một systemMessage mang cả hai" "systemMessage / có / có" \
  "$(kind) / $(jfield "$WORK/stop.out" systemMessage | grep -q 'docs/x.md:1' && echo có || echo không) / $(jfield "$WORK/stop.out" systemMessage | grep -q 'src/BE/Core/A.cs' && echo có || echo không)"
stub 0; soft

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
# --- danh sách cho phép git (ADR-0111 quyết định 4): lệnh git không khớp mục
# Bash(git …) nào của permissions.allow bị chặn, kể cả khi deny không liệt kê nó.
# Bốn lệnh ghi đã tái hiện lọt qua danh sách cấm cũ, qua ba đường:
pt 'git update-ref refs/heads/main HEAD~1' Bash 'git update-ref refs/heads/main HEAD~1' BLOCK
pt 'git symbolic-ref HEAD refs/heads/x' Bash 'git symbolic-ref HEAD refs/heads/x' BLOCK
pt 'git gc --prune=now' Bash 'git gc --prune=now' BLOCK
pt 'git notes add -m x' Bash 'git notes add -m x' BLOCK
pt 'rtk git update-ref refs/heads/main HEAD~1' Bash 'rtk git update-ref refs/heads/main HEAD~1' BLOCK
pt 'cd a && git symbolic-ref HEAD refs/heads/x' Bash 'cd a && git symbolic-ref HEAD refs/heads/x' BLOCK
pt 'PowerShell: git gc --prune=now' PowerShell 'git gc --prune=now' BLOCK
pt 'PowerShell: & git notes add -m x' PowerShell '& git notes add -m x' BLOCK
# dạng gạch nối (tệp git-<lệnh> trong libexec/git-core) là cùng lệnh con
pt 'git-update-ref (dạng gạch nối)' Bash 'git-update-ref refs/heads/main HEAD~1' BLOCK
pt '/mingw64/libexec/git-core/git-gc.exe --prune=now' Bash '/mingw64/libexec/git-core/git-gc.exe --prune=now' BLOCK
# tên không có trong danh sách: alias, chữ hoa, lệnh ghi deny không liệt kê, lệnh
# đọc chưa có tên (giá ADR-0111 đã nêu), dạng ghi của remote
pt 'git st (alias)' Bash 'git st' BLOCK
pt 'git PUSH (chữ hoa — danh sách cấm cũ lọt)' Bash 'git PUSH' BLOCK
pt 'git checkout-index -a (ghi working tree)' Bash 'git checkout-index -a' BLOCK
pt 'git merge-base a b (lệnh đọc chưa có tên)' Bash 'git merge-base a b' BLOCK
pt 'git reflog (lệnh đọc chưa có tên)' Bash 'git reflog' BLOCK
pt 'git remote get-url origin (chưa có tên)' Bash 'git remote get-url origin' BLOCK
pt 'git remote -v add o u (-v đứng trước lệnh con của remote)' Bash 'git remote -v add o u' BLOCK
pt 'git remote rename a b' Bash 'git remote rename a b' BLOCK
# git trần và tuỳ chọn thông tin: chỉ in rồi thoát. help / --help mở trình xem ngoài
# (man, trình duyệt — chọn theo cấu hình git), version không có trong danh sách.
pt 'git (trần — in cách dùng)' Bash 'git' PASS 0
pt 'git --version' Bash 'git --version' PASS 0
pt 'git --version push (tuỳ chọn thông tin không đứng cuối)' Bash 'git --version push' BLOCK
pt 'git version' Bash 'git version' BLOCK
pt 'git help log' Bash 'git help log' BLOCK
pt 'git --help' Bash 'git --help' BLOCK
pt 'git log --help (git đổi thành git help log)' Bash 'git log --help' BLOCK
# từng lệnh được phép, dạng thường, qua ba đường
for c in 'status --short' 'diff --stat HEAD' 'log --oneline -5' 'show HEAD:README.md' \
         'blame -L 1,5 README.md' 'ls-files docs' 'rev-parse --show-toplevel' 'check-ignore -v bin/x' \
         'cat-file -p HEAD' 'ls-tree -r HEAD' 'grep -n TODO' 'describe --tags --always' \
         'shortlog -sn HEAD' 'remote -v' 'remote' 'remote show origin'; do
  pt "git $c" Bash "git $c" PASS
  pt "rtk git $c" Bash "rtk git $c" PASS
  pt "PowerShell: git $c" PowerShell "git $c" PASS
done
# chuyển hướng: số fd và tệp đích không phải đối số — mục khớp nguyên lệnh
# (Bash(git remote -v)) vẫn khớp, và lệnh đứng SAU chuyển hướng vẫn được xét.
pt 'git remote -v 2>&1' Bash 'git remote -v 2>&1' PASS 0
pt 'git remote -v > remotes.txt' Bash 'git remote -v > remotes.txt' PASS
pt 'PowerShell: git remote -v 2>$null' PowerShell 'git remote -v 2>$null' PASS
pt 'git log --oneline >> log.txt' Bash 'git log --oneline >> log.txt' PASS
pt '>out.txt git push (chuyển hướng đứng trước lệnh)' Bash '>out.txt git push' BLOCK
pt '2>/dev/null git push' Bash '2>/dev/null git push' BLOCK
pt 'echo x >&2; git gc' Bash 'echo x >&2; git gc' BLOCK
pt 'cat <(git show HEAD:a) — thay thế tiến trình' Bash 'cat <(git show HEAD:a)' PASS 0
pt 'cat <(git gc) — thay thế tiến trình' Bash 'cat <(git gc)' BLOCK
# tuỳ chọn ghi tệp / chạy lệnh của lệnh được phép
pt 'git diff --output=x.txt' Bash 'git diff --output=x.txt' BLOCK
pt 'git diff --output x.txt' Bash 'git diff --output x.txt' BLOCK
pt 'git diff --outp=x.txt (viết tắt)' Bash 'git diff --outp=x.txt' BLOCK
pt 'git log -p --output=x.txt' Bash 'git log -p --output=x.txt' BLOCK
pt 'git show --output=x.txt HEAD' Bash 'git show --output=x.txt HEAD' BLOCK
pt 'git shortlog --output=x.txt HEAD' Bash 'git shortlog --output=x.txt HEAD' BLOCK
pt 'rtk git diff --output=x.txt' Bash 'rtk git diff --output=x.txt' BLOCK
pt 'PowerShell: git log --output=x.txt' PowerShell 'git log --output=x.txt' BLOCK
pt 'git diff --output-indicator-new=+ (không phải --output)' Bash 'git diff --output-indicator-new=+' PASS
pt 'git diff -Oordfile (orderfile của diff, chỉ đọc)' Bash 'git diff -Oordfile' PASS
pt 'git grep -Ovim foo' Bash 'git grep -Ovim foo' BLOCK
pt 'git grep -O foo' Bash 'git grep -O foo' BLOCK
pt 'git grep -nOvim foo (cụm cờ ngắn)' Bash 'git grep -nOvim foo' BLOCK
pt 'git grep --open-files-in-pager=vim foo' Bash 'git grep --open-files-in-pager=vim foo' BLOCK
pt 'git grep --open foo (viết tắt)' Bash 'git grep --open foo' BLOCK
pt 'PowerShell: git grep -Onotepad foo' PowerShell 'git grep -Onotepad foo' BLOCK
pt 'git grep -eOops (O thuộc mẫu của -e)' Bash 'git grep -eOops' PASS
pt 'git grep -n -e Owner' Bash 'git grep -n -e Owner' PASS
# tuỳ chọn toàn cục đổi cấu hình hoặc trỏ sang repo khác
pt 'git -c core.pager=evil log' Bash 'git -c core.pager=evil log' BLOCK
pt 'git -c core.fsmonitor=evil status' Bash 'git -c core.fsmonitor=evil status' BLOCK
pt 'git -c alias.x=!sh log' Bash 'git -c alias.x=!sh log' BLOCK
pt 'git --config-env=core.pager=EVIL log' Bash 'git --config-env=core.pager=EVIL log' BLOCK
pt 'PowerShell: git -c core.pager=evil log' PowerShell 'git -c core.pager=evil log' BLOCK
pt 'git -C ../other status' Bash 'git -C ../other status' BLOCK
pt 'git -C /tmp/other log' Bash 'git -C /tmp/other log' BLOCK
pt 'git --git-dir=../o/.git log' Bash 'git --git-dir=../o/.git log' BLOCK
pt 'git --git-dir ../o/.git log' Bash 'git --git-dir ../o/.git log' BLOCK
pt 'git --work-tree=../o status' Bash 'git --work-tree=../o status' BLOCK
pt 'git --exec-path=/tmp/evil status' Bash 'git --exec-path=/tmp/evil status' BLOCK
pt 'git --bare log' Bash 'git --bare log' BLOCK
RU=$(cd "$R" && pwd)
if RW=$(cd "$R" && pwd -W 2>/dev/null) && [ -n "$RW" ]; then :; else RW="$RU"; fi
pt 'git -C <gốc repo, POSIX> status' Bash "git -C $RU status" PASS
pt 'git -C <gốc repo>/ status' Bash "git -C $RU/ status" PASS
bs='\'; RWB=${RW//\//$bs}
pt 'PowerShell: git -C "<gốc repo, gạch chéo ngược>" status' PowerShell "git -C \"$RWB\" status" PASS
pt 'git -C <gốc repo>/src status (thư mục con — có thể là repo khác)' Bash "git -C $RU/src status" BLOCK
# biến môi trường đặt ngay trước git: cùng kênh với -c / --git-dir
pt 'GIT_DIR=../o/.git git log' Bash 'GIT_DIR=../o/.git git log' BLOCK
pt 'env GIT_PAGER=evil git log' Bash 'env GIT_PAGER=evil git log' BLOCK
pt 'GIT_CONFIG_COUNT=1 GIT_CONFIG_KEY_0=core.pager GIT_CONFIG_VALUE_0=evil git log' Bash 'GIT_CONFIG_COUNT=1 GIT_CONFIG_KEY_0=core.pager GIT_CONFIG_VALUE_0=evil git log' BLOCK
pt 'HOME=/tmp/x git status' Bash 'HOME=/tmp/x git status' BLOCK
pt 'LC_ALL=C git log (mọi phép gán trước git)' Bash 'LC_ALL=C git log' BLOCK
pt 'LC_ALL=C sort (gán trước lệnh khác)' Bash 'LC_ALL=C sort x' PASS
# --- `<<` KHÔNG phải heredoc: here-string `<<<` và phép dịch bit trong số học.
# Lỗi cũ: `<<<` chỉ bị loại ở dấu `<` đầu, cặp `<<` còn lại thành heredoc; `<<` trong
# $((…)), ((…)), $[…] cũng vậy. Heredoc giả không bao giờ gặp dòng kết thúc nên mọi
# dòng sau bị bỏ như thân — lệnh cấm ở dòng sau lọt. Mỗi ca chặn có ca cho qua cùng
# cơ chế; ca cho qua đòi log=0 (bản cũ ghi "heredoc không có thân").
tab=$'\t'
pt 'here-string <<< rồi dòng sau vô hại — cho qua, không log' Bash "grep x <<< \"\$list\"${nl}ls" PASS 0
pt 'here-string <<< rồi git push dòng sau' Bash "grep x <<< \"\$list\"${nl}git push" BLOCK
pt 'done <<< "$v" rồi git reset --hard dòng sau' Bash "while read -r l; do echo \"\$l\"; done <<< \"\$v\"${nl}git reset --hard" BLOCK
pt 'here-string dính liền <<<"$v" rồi dòng sau vô hại' Bash "cat <<<\"\$v\"${nl}ls" PASS 0
pt 'here-string dính liền <<<"$v" rồi git push dòng sau' Bash "cat <<<\"\$v\"${nl}git push" BLOCK
pt 'here-string mang $(git status) — cho qua' Bash 'cat <<< "$(git status)"' PASS 0
pt 'here-string mang $(git push) — chặn' Bash 'cat <<< "$(git push)"' BLOCK
pt '$((1<<2)) rồi dòng sau vô hại — cho qua, không log' Bash "x=\$((1<<2))${nl}echo \$x" PASS 0
pt '$((1<<2)) rồi git push dòng sau' Bash "x=\$((1<<2))${nl}git push" BLOCK
pt '"$((1<<2))" trong nháy kép rồi dòng sau vô hại' Bash "echo \"\$((1<<2))\"${nl}ls" PASS 0
pt '"$((1<<2))" trong nháy kép rồi git push dòng sau' Bash "echo \"\$((1<<2))\"${nl}git push" BLOCK
pt '$(( (1<<2) + (3) )) lồng ngoặc rồi dòng sau vô hại' Bash "echo \$(( (1<<2) + (3) ))${nl}ls" PASS 0
pt '$(( (1<<2) + (3) )) lồng ngoặc rồi git push dòng sau' Bash "echo \$(( (1<<2) + (3) ))${nl}git push" BLOCK
pt '$(( $(git status) << 1 )) — lệnh con trong số học, cho qua' Bash 'echo $(( $(git status) << 1 ))' PASS 0
pt '$(( $(git push) << 1 )) — lệnh con trong số học, chặn' Bash 'echo $(( $(git push) << 1 ))' BLOCK
pt '((y = 1<<4)) rồi dòng sau vô hại' Bash "((y = 1<<4))${nl}echo \$y" PASS 0
pt '((y = 1<<4)) rồi git push dòng sau' Bash "((y = 1<<4))${nl}git push" BLOCK
pt 'for ((i=1; i<64; i<<=1)) … echo' Bash "for ((i=1; i<64; i<<=1)); do echo \$i; done${nl}ls" PASS 0
pt 'for ((i=1; i<64; i<<=1)) … git push' Bash "for ((i=1; i<64; i<<=1)); do echo \$i; done${nl}git push" BLOCK
pt '$[1<<3] rồi dòng sau vô hại' Bash "x=\$[1<<3]${nl}echo \$x" PASS 0
pt '$[1<<3] rồi git push dòng sau' Bash "x=\$[1<<3]${nl}git push" BLOCK
# bash đọc `$((…) )` / `((…) )` không đóng bằng `))` là lệnh con / subshell lồng.
pt '"$((git status) )" là lệnh con, không phải số học — cho qua' Bash 'echo "$((git status) )"' PASS 0
pt '"$((git push) )" là lệnh con, không phải số học — chặn' Bash 'echo "$((git push) )"' BLOCK
pt '((git status) ) là subshell lồng — cho qua' Bash '((git status) )' PASS 0
pt '((git push) ) là subshell lồng — chặn' Bash '((git push) )' BLOCK
# heredoc thật: giữ nguyên hành vi — thân bỏ qua, sau dấu kết thì đọc tiếp.
pt "heredoc <<'EOF' rồi git push sau dấu kết" Bash "cat <<'EOF' > f${nl}git push${nl}EOF${nl}git push" BLOCK
pt 'heredoc << "EOF" (cách, nháy kép) có git push trong thân' Bash "cat << \"EOF\" > f${nl}git push${nl}EOF${nl}ls" PASS 0
pt 'heredoc << "EOF" (cách, nháy kép) rồi git push sau dấu kết' Bash "cat << \"EOF\" > f${nl}x${nl}EOF${nl}git push" BLOCK
pt 'heredoc <<-EOF rồi git push sau dấu kết thụt tab' Bash "cat <<-EOF > f${nl}${tab}x${nl}${tab}EOF${nl}git push" BLOCK
pt 'heredoc + here-string cùng dòng, git push trong thân' Bash "cat <<EOF; grep x <<< \"\$v\"${nl}git push${nl}EOF${nl}ls" PASS 0
pt 'heredoc + here-string cùng dòng, git push sau dấu kết' Bash "cat <<EOF; grep x <<< \"\$v\"${nl}x${nl}EOF${nl}git push" BLOCK
pt 'heredoc + $((1<<2)) cùng dòng, git push trong thân' Bash "cat <<EOF; echo \$((1<<2))${nl}git push${nl}EOF${nl}ls" PASS 0
pt 'heredoc + $((1<<2)) cùng dòng, git push sau dấu kết' Bash "cat <<EOF; echo \$((1<<2))${nl}x${nl}EOF${nl}git push" BLOCK
pt 'hai heredoc một dòng, git push chỉ trong hai thân' Bash "cat <<A <<B${nl}git push${nl}A${nl}git push${nl}B${nl}ls" PASS 0
pt 'hai heredoc một dòng, git push sau dấu kết thứ hai' Bash "cat <<A <<B${nl}git push${nl}A${nl}git push${nl}B${nl}git push" BLOCK

# === lượt soát 2026-09-29: đường vòng lớp hook. Mỗi ca BLOCK kèm ca PASS sinh đôi
#     cùng hình dạng (lệnh vô hại), chứng minh quy tắc không chặn thừa.
# --- 1. tên lệnh động dẫn tới lệnh cấm
pt '1: g=git; $g push — biến giải ra git' Bash 'g=git; $g push' BLOCK
pt '1: g=ls; $g -la — biến giải ra lệnh vô hại (PASS)' Bash 'g=ls; $g -la' PASS 0
pt '1: $(echo git) push — thay thế lệnh giải ra git' Bash '$(echo git) push' BLOCK
pt '1: $(echo ls) -la — thay thế lệnh vô hại (PASS)' Bash '$(echo ls) -la' PASS 0
pt '1: `echo git` push — backtick giải ra git' Bash '`echo git` push' BLOCK
pt '1: `echo ls` -la — backtick vô hại (PASS)' Bash '`echo ls` -la' PASS 0
pt '1: PowerShell $g="git"; & $g push' PowerShell '$g="git"; & $g push' BLOCK
pt '1: PowerShell $g="ls"; & $g Get-ChildItem (PASS)' PowerShell '$g="ls"; & $g Get-ChildItem' PASS 0
pt '1: PowerShell & (Get-Command git) push' PowerShell '& (Get-Command git) push' BLOCK
pt '1: PowerShell & (Get-Command ls) (PASS)' PowerShell '& (Get-Command ls)' PASS 0
pt '1: git${IFS}push — IFS làm dấu tách' Bash 'git${IFS}push' BLOCK
pt '1: echo${IFS}hi — IFS vô hại (PASS)' Bash 'echo${IFS}hi' PASS 0
pt '1: git {push,} — mở rộng ngoặc' Bash 'git {push,}' BLOCK
pt '1: mkdir -p a/{b,c} — mở rộng ngoặc vô hại (PASS)' Bash 'mkdir -p a/{b,c}' PASS 0
pt '1: cp file{,.bak} — mở rộng ngoặc trên đối số (PASS)' Bash 'cp file{,.bak}' PASS 0
pt '1: git diff HEAD{,~1} — ngoặc trên đối số lệnh được phép (PASS)' Bash 'git diff HEAD{,~1}' PASS
pt '1: { git push; } — nhóm lệnh vẫn chặn' Bash '{ git push; }' BLOCK
pt '1: { ls; } — nhóm lệnh vô hại (PASS)' Bash '{ ls; }' PASS 0
# --- 2. chạy chuỗi như lệnh
pt '2: iex "git push" (PowerShell)' PowerShell 'iex "git push"' BLOCK
pt '2: iex "ls" (PASS)' PowerShell 'iex "ls"' PASS 0
pt '2: Invoke-Expression "git push"' PowerShell 'Invoke-Expression "git push"' BLOCK
pt '2: Invoke-Expression "Get-ChildItem" (PASS)' PowerShell 'Invoke-Expression "Get-ChildItem"' PASS 0
pt '2: echo "git push" | bash' Bash 'echo "git push" | bash' BLOCK
pt '2: echo "ls" | bash (PASS)' Bash 'echo "ls" | bash' PASS 0
pt '2: bash <<< "git push"' Bash 'bash <<< "git push"' BLOCK
pt '2: bash <<< "ls" (PASS)' Bash 'bash <<< "ls"' PASS 0
pt '2: grep x <<< "$v" — here-string cho lệnh không phải trình thông dịch (PASS)' Bash 'grep x <<< "$v"' PASS 0
pt '2: sh -s <<EOF git push EOF' Bash "sh -s <<EOF${nl}git push${nl}EOF" BLOCK
pt '2: sh -s <<EOF ls EOF (PASS)' Bash "sh -s <<EOF${nl}ls${nl}EOF" PASS 0
pt '2: cat <<EOF git push EOF — cat đọc heredoc, không chạy (PASS)' Bash "cat <<EOF${nl}git push${nl}EOF" PASS 0
pt '2: bash -c -- "git push"' Bash 'bash -c -- "git push"' BLOCK
pt '2: bash -c -- "ls" (PASS)' Bash 'bash -c -- "ls"' PASS 0
pt '2: exec -a x git push' Bash 'exec -a x git push' BLOCK
pt '2: exec -a x ls (PASS)' Bash 'exec -a x ls' PASS 0
# --- 3. lệnh bọc chưa được bóc
pt '3: setsid git push' Bash 'setsid git push' BLOCK
pt '3: setsid ls (PASS)' Bash 'setsid ls' PASS 0
pt '3: stdbuf -o0 git push' Bash 'stdbuf -o0 git push' BLOCK
pt '3: stdbuf -o0 ls (PASS)' Bash 'stdbuf -o0 ls' PASS 0
pt '3: winpty git push' Bash 'winpty git push' BLOCK
pt '3: winpty ls (PASS)' Bash 'winpty ls' PASS 0
pt '3: watch -n 1 git push' Bash 'watch -n 1 git push' BLOCK
pt '3: watch -n 1 ls (PASS)' Bash 'watch -n 1 ls' PASS 0
pt '3: flock /tmp/l git push' Bash 'flock /tmp/l git push' BLOCK
pt '3: flock /tmp/l ls (PASS)' Bash 'flock /tmp/l ls' PASS 0
pt '3: flock -c "git push" /tmp/l' Bash 'flock /tmp/l -c "git push"' BLOCK
pt '3: flock /tmp/l -c "ls" (PASS)' Bash 'flock /tmp/l -c "ls"' PASS 0
# --- 4. chạy lệnh từ trong công cụ khác
pt '4: sed -n "1e git push" f' Bash "sed -n '1e git push' README.md" BLOCK
pt '4: sed -n "1,5p" f (PASS)' Bash "sed -n '1,5p' README.md" PASS 0
pt '4: sed "1e git push" f' Bash "sed '1e git push' f" BLOCK
pt '4: sed -i "s/a/b/" f (PASS)' Bash "sed -i 's/a/b/' f" PASS 0
pt '4: sed "s/x/foo bar/" f — thay thế thường (PASS)' Bash "sed 's/x/foo bar/' f" PASS 0
pt '4: awk BEGIN system git push' Bash 'awk "BEGIN{system(\"git push\")}"' BLOCK
pt '4: awk END print | git push' Bash 'awk "END{print | \"git push\"}"' BLOCK
pt '4: awk {print} (PASS)' Bash 'awk "{print}"' PASS 0
pt '4: awk END{print} (PASS)' Bash 'awk "END{print}"' PASS 0
pt '4: awk -F, {print $2} (PASS)' Bash 'awk -F, "{print \$2}"' PASS 0
pt '4: ls | xargs git rm (git ngoài danh sách)' Bash 'ls | xargs git rm' BLOCK
pt '4: ls | xargs git (không có lệnh con, đối số từ stdin)' Bash 'ls | xargs git' BLOCK
pt '4: ls | xargs -n1 git status (git đọc, đối số phụ ở cuối) (PASS)' Bash 'ls | xargs -n1 git status' PASS
pt '4: ls | xargs grep foo (không phải git) (PASS)' Bash 'ls | xargs grep foo' PASS
# --- 5. biến môi trường git ở câu lệnh trước
pt '5: export GIT_DIR=/x' Bash 'export GIT_DIR=/tmp/x' BLOCK
pt '5: export FOO=1 (PASS)' Bash 'export FOO=1' PASS
pt '5: export PATH=$PATH:/x (PASS)' Bash 'export PATH=$PATH:/x' PASS
pt '5: export GIT_CONFIG_GLOBAL=/c' Bash 'export GIT_CONFIG_GLOBAL=/tmp/c' BLOCK
pt '5: declare -x GIT_DIR=/x' Bash 'declare -x GIT_DIR=/tmp/x' BLOCK
pt '5: PowerShell $env:GIT_DIR="/x"' PowerShell '$env:GIT_DIR="/tmp/x"' BLOCK
pt '5: PowerShell $env:GIT_DIR = "/x" (dấu cách)' PowerShell '$env:GIT_DIR = "/tmp/x"' BLOCK
pt '5: PowerShell $env:FOO="1" (PASS)' PowerShell '$env:FOO="1"' PASS
# --- 6. ghi vào thư mục được bảo vệ (.claude/.state, .git)
pt '6: touch .claude/.state/core-reviewed' Bash 'touch .claude/.state/core-reviewed' BLOCK
pt '6: touch src/x.cs (PASS)' Bash 'touch src/x.cs' PASS
pt '6: rm -f .claude/.state/core-touched' Bash 'rm -f .claude/.state/core-touched' BLOCK
pt '6: rm -rf node_modules (PASS)' Bash 'rm -rf node_modules' PASS
pt '6: echo > .claude/.state/gate-green (chuyển hướng ghi)' Bash 'echo x > .claude/.state/gate-green' BLOCK
pt '6: echo x > build/out.txt (chuyển hướng vô hại) (PASS)' Bash 'echo x > build/out.txt' PASS
pt '6: mv y .claude/.state/z' Bash 'mv y .claude/.state/z' BLOCK
pt '6: cp y .claude/.state/z' Bash 'cp y .claude/.state/z' BLOCK
pt '6: sed -i s/a/b/ .claude/.state/x' Bash 'sed -i s/a/b/ .claude/.state/x' BLOCK
pt '6: echo x > .git/config (.git)' Bash 'echo x > .git/config' BLOCK
pt '6: rm -f .git/index.lock (.git)' Bash 'rm -f .git/index.lock' BLOCK
pt '6: find .claude/.state -delete' Bash 'find .claude/.state -delete' BLOCK
pt '6: find .claude/.state -type f (đọc) (PASS)' Bash 'find .claude/.state -type f' PASS
pt '6: cat .claude/.state/core-touched (đọc) (PASS)' Bash 'cat .claude/.state/core-touched' PASS
pt '6: grep x .claude/.state/log (đọc) (PASS)' Bash 'grep x .claude/.state/log' PASS
pt '6: ls .claude/.state/ (đọc) (PASS)' Bash 'ls .claude/.state/' PASS
pt '6: .gitignore không phải .git (PASS)' Bash 'echo x > .gitignore' PASS
pt '6: .github/x không phải .git (PASS)' Bash 'echo x > .github/x.yml' PASS
pt '6: PowerShell New-Item .claude/.state/x' PowerShell 'New-Item .claude/.state/core-reviewed' BLOCK
pt '6: PowerShell Remove-Item .claude/.state/x' PowerShell 'Remove-Item .claude/.state/core-touched' BLOCK
pt '6: PowerShell Set-Content .claude/.state/x' PowerShell 'Set-Content .claude/.state/x "y"' BLOCK
pt '6: PowerShell Out-File > .claude/.state/x' PowerShell 'Get-Date | Out-File .claude/.state/x' BLOCK
pt '6: PowerShell Get-Content .claude/.state/x (đọc) (PASS)' PowerShell 'Get-Content .claude/.state/x' PASS
pt '6: PowerShell New-Item build/x (PASS)' PowerShell 'New-Item build/x.txt' PASS
# --- 6. công cụ Edit/Write/NotebookEdit ghi vào thư mục bảo vệ (matcher riêng)
pte() { # tên, tool, key, giá trị, PASS|BLOCK
  node -e '
    const [tool, key, val] = process.argv.slice(1);
    const o = { session_id:"s", hook_event_name:"PreToolUse", tool_name:tool, tool_input:{} };
    o.tool_input[key] = val;
    process.stdout.write(JSON.stringify(o));
  ' "$2" "$3" "$4" | bash "$R/.claude/hooks/on-pretool.sh" > "$WORK/pt.out" 2> "$WORK/pt.err"
  local rc=$? res
  case "$rc" in 0) res=PASS ;; 2) res=BLOCK ;; *) res="rc=$rc" ;; esac
  row on-pretool "$1" "$5" "$res"
}
pte '6E: Edit .claude/.state/x (tuyệt đối \\)' Edit file_path "$RWB\\.claude\\.state\\core-reviewed" BLOCK
pte '6E: Edit .claude/.state/x (tương đối /)' Edit file_path ".claude/.state/core-reviewed" BLOCK
pte '6E: Write .git/config' Write file_path "$RWB\\.git\\config" BLOCK
pte '6E: NotebookEdit .claude/.state/x.ipynb' NotebookEdit notebook_path ".claude/.state/x.ipynb" BLOCK
pte '6E: Edit docs/x.md (PASS)' Edit file_path "$RWB\\docs\\x.md" PASS
pte '6E: Write src/x.cs (PASS)' Write file_path "src/BE/Core/x.cs" PASS
pte '6E: Edit .gitignore (không phải .git) (PASS)' Edit file_path ".gitignore" PASS
pte '6E: Edit .github/workflows/x.yml (PASS)' Edit file_path ".github/workflows/x.yml" PASS

# --- 7. tệp cấu hình git TOÀN CỤC (~/.gitconfig, ~/.config/git/, $XDG_CONFIG_HOME/git/)
#     và biến môi trường quyết định tệp đó (HOME / XDG_CONFIG_HOME / USERPROFILE).
#     Lỗ 2026-09-30: sửa các tệp này thì một lệnh git ĐƯỢC PHÉP (status, diff) chạy
#     chương trình ngoài; đổi HOME đổi luôn tệp git đọc. Mỗi ca BLOCK có ca PASS
#     sinh đôi cùng hình dạng (đọc, hoặc ghi vào anh em không phải cấu hình git).
# 7a. ghi tệp cấu hình git qua lệnh shell — mọi dạng neo thư mục nhà.
pt '7a: echo x > ~/.gitconfig' Bash 'echo x > ~/.gitconfig' BLOCK
pt '7a: echo x > ~/notes.txt (nhà, không phải cấu hình) (PASS)' Bash 'echo x > ~/notes.txt' PASS
pt '7a: echo x > $HOME/.gitconfig' Bash 'echo x > $HOME/.gitconfig' BLOCK
pt '7a: echo x > $HOME/.bashrc (PASS)' Bash 'echo x > $HOME/.bashrc' PASS
pt '7a: cp evil ~/.config/git/config' Bash 'cp evil ~/.config/git/config' BLOCK
pt '7a: cp x ~/.config/nvim/init.vim (.config không phải git) (PASS)' Bash 'cp x ~/.config/nvim/init.vim' PASS
pt '7a: tee ~/.config/git/attributes (đối số dưới thư mục git)' Bash 'tee ~/.config/git/attributes' BLOCK
pt '7a: echo x > $XDG_CONFIG_HOME/git/config' Bash 'echo x > $XDG_CONFIG_HOME/git/config' BLOCK
pt '7a: echo x > $XDG_CONFIG_HOME/nvim/init.vim (PASS)' Bash 'echo x > $XDG_CONFIG_HOME/nvim/init.vim' PASS
pt '7a: touch /c/Users/me/.gitconfig (POSIX Windows)' Bash 'touch /c/Users/me/.gitconfig' BLOCK
pt '7a: touch /c/Users/me/project.txt (PASS)' Bash 'touch /c/Users/me/project.txt' PASS
pt '7a: rm -f ~/.gitconfig (xoá)' Bash 'rm -f ~/.gitconfig' BLOCK
pt '7a: cp evil "C:\Users\me\.gitconfig" (backslash trong nháy kép)' Bash 'cp evil "C:\Users\me\.gitconfig"' BLOCK
pt '7a: echo x >> $USERPROFILE/.gitconfig (biến Windows trong Git Bash)' Bash 'echo x >> $USERPROFILE/.gitconfig' BLOCK
pt '7a: echo x >> $USERPROFILE/notes.txt (PASS)' Bash 'echo x >> $USERPROFILE/notes.txt' PASS
pt '7a: echo x >> /home/u/.config/git/config (nhà Linux)' Bash 'echo x >> /home/u/.config/git/config' BLOCK
pt '7a: echo x >> /home/u/notes.txt (PASS)' Bash 'echo x >> /home/u/notes.txt' PASS
pt '7a: cat ~/.gitconfig (đọc) (PASS)' Bash 'cat ~/.gitconfig' PASS 0
pt '7a: ls ~/.config/git/ (đọc) (PASS)' Bash 'ls ~/.config/git/' PASS
pt '7a: grep x ~/.gitconfig (đọc) (PASS)' Bash 'grep x ~/.gitconfig' PASS
# 7b. ghi tệp cấu hình git qua cmdlet PowerShell.
pt '7b: Set-Content C:\Users\me\.gitconfig' PowerShell 'Set-Content C:\Users\me\.gitconfig "x"' BLOCK
pt '7b: Set-Content C:\Users\me\notes.txt (PASS)' PowerShell 'Set-Content C:\Users\me\notes.txt "x"' PASS
pt '7b: Out-File $env:USERPROFILE\.gitconfig' PowerShell 'Get-Date | Out-File $env:USERPROFILE\.gitconfig' BLOCK
pt '7b: Remove-Item $HOME\.gitconfig' PowerShell 'Remove-Item $HOME\.gitconfig' BLOCK
pt '7b: Set-Content $env:USERPROFILE\.config\git\config' PowerShell 'Set-Content $env:USERPROFILE\.config\git\config "x"' BLOCK
pt '7b: Get-Content C:\Users\me\.gitconfig (đọc) (PASS)' PowerShell 'Get-Content C:\Users\me\.gitconfig' PASS
# bí danh PowerShell của Copy-Item / Move-Item / Rename-Item — áp cho MỌI vùng bảo vệ.
pt '7b: copy evil $env:USERPROFILE\.gitconfig (bí danh Copy-Item)' PowerShell 'copy evil $env:USERPROFILE\.gitconfig' BLOCK
pt '7b: copy a.txt b.txt (PASS)' PowerShell 'copy a.txt b.txt' PASS
pt '7b: move evil .claude/.state/core-reviewed (bí danh Move-Item)' PowerShell 'move evil .claude/.state/core-reviewed' BLOCK
pt '7b: move a.txt b.txt (PASS)' PowerShell 'move a.txt b.txt' PASS
pt '7b: ren .git\HEAD x (bí danh Rename-Item)' PowerShell 'ren .git\HEAD x' BLOCK
pt '7b: ren a.txt b.txt (PASS)' PowerShell 'ren a.txt b.txt' PASS
# 7c. công cụ Edit/Write/NotebookEdit ghi tệp cấu hình git.
pte '7c: Edit ~/.gitconfig' Edit file_path "~/.gitconfig" BLOCK
pte '7c: Write ~/.config/git/config' Write file_path "~/.config/git/config" BLOCK
pte '7c: Edit $XDG_CONFIG_HOME/git/config' Edit file_path "\$XDG_CONFIG_HOME/git/config" BLOCK
pte '7c: Write C:\Users\me\.gitconfig' Write file_path "C:\\Users\\me\\.gitconfig" BLOCK
pte '7c: NotebookEdit ~/.config/git/x.ipynb' NotebookEdit notebook_path "~/.config/git/x.ipynb" BLOCK
pte '7c: Edit ~/.config/nvim/init.vim (PASS)' Edit file_path "~/.config/nvim/init.vim" PASS
pte '7c: Edit ~/.bashrc (PASS)' Edit file_path "~/.bashrc" PASS
pte '7c: Write C:\Users\me\notes.txt (PASS)' Write file_path "C:\\Users\\me\\notes.txt" PASS
# 7d. đổi biến môi trường quyết định tệp cấu hình git đọc.
pt '7d: export HOME=/tmp/x' Bash 'export HOME=/tmp/x' BLOCK
pt '7d: export XDG_CONFIG_HOME=/tmp/c' Bash 'export XDG_CONFIG_HOME=/tmp/c' BLOCK
pt '7d: declare -x HOME=/tmp/x' Bash 'declare -x HOME=/tmp/x' BLOCK
pt '7d: readonly HOME=/tmp/x' Bash 'readonly HOME=/tmp/x' BLOCK
pt '7d: HOME=/tmp/x (câu riêng)' Bash 'HOME=/tmp/x' BLOCK
pt '7d: XDG_CONFIG_HOME=/tmp/c (câu riêng)' Bash 'XDG_CONFIG_HOME=/tmp/c' BLOCK
pt '7d: HOME=/tmp/x; git status (đổi HOME rồi git)' Bash 'HOME=/tmp/x; git status' BLOCK
pt '7d: export HOMEDIR=/tmp (tên khác, PASS)' Bash 'export HOMEDIR=/tmp' PASS
pt '7d: export XDG_DATA_HOME=/tmp (không phải CONFIG, PASS)' Bash 'export XDG_DATA_HOME=/tmp' PASS
pt '7d: HOMEDIR=/tmp (câu riêng, tên khác, PASS)' Bash 'HOMEDIR=/tmp' PASS
pt '7d: HOME=/tmp/x npm install (gán trước lệnh không phải git — phạm vi gói, PASS)' Bash 'HOME=/tmp/x npm install' PASS
pt '7d: PowerShell $env:HOME="/tmp"' PowerShell '$env:HOME="/tmp"' BLOCK
pt '7d: PowerShell $env:USERPROFILE="C:\tmp"' PowerShell '$env:USERPROFILE="C:\tmp"' BLOCK
pt '7d: PowerShell $env:XDG_CONFIG_HOME="/tmp"' PowerShell '$env:XDG_CONFIG_HOME="/tmp"' BLOCK
pt '7d: PowerShell $env:HOME = "/tmp" (dấu cách)' PowerShell '$env:HOME = "/tmp"' BLOCK
# Git for Windows dựng HOME từ HOMEDRIVE+HOMEPATH (rồi USERPROFILE) khi HOME không
# đặt — trường hợp thường gặp trong PowerShell.
pt '7d: PowerShell $env:HOMEDRIVE="D:"' PowerShell '$env:HOMEDRIVE="D:"' BLOCK
pt '7d: PowerShell $env:homepath="\x" (chữ thường)' PowerShell '$env:homepath="\x"' BLOCK
pt '7d: PowerShell $env:MYHOME="C:\x" (tên khác, PASS)' PowerShell '$env:MYHOME="C:\x"' PASS
pt '7d: GIT_DIR=/tmp/x (câu riêng)' Bash 'GIT_DIR=/tmp/x' BLOCK
pt '7d: MY_DIR=/tmp/x (câu riêng, PASS)' Bash 'MY_DIR=/tmp/x' PASS
pt '7d: home=/tmp/x (bash phân biệt hoa thường — biến khác, PASS)' Bash 'home=/tmp/x' PASS
pt '7d: setx HOME C:/x (biến người dùng lưu bền)' Bash 'setx HOME C:/x' BLOCK
pt '7d: setx FOO 1 (PASS)' Bash 'setx FOO 1' PASS
pt '7d: PowerShell setx xdg_config_home C:\x' PowerShell 'setx xdg_config_home C:\x' BLOCK
pt '7d: PowerShell Set-Item Env:HOME C:\x' PowerShell 'Set-Item Env:HOME C:\x' BLOCK
pt '7d: PowerShell Set-Item Env:FOO 1 (PASS)' PowerShell 'Set-Item Env:FOO 1' PASS
pt '7d: PowerShell New-Item -Path Env: -Name USERPROFILE -Value x' PowerShell 'New-Item -Path Env: -Name USERPROFILE -Value x' BLOCK
pt '7d: PowerShell Get-Item Env:HOME (đọc) (PASS)' PowerShell 'Get-Item Env:HOME' PASS

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
# Hình settings.json lạ. Danh sách cho phép git là mặc định-chặn: đọc được settings.json
# mà không có mục Bash(git …) nào thì mọi lệnh git bị chặn, không phải mọi lệnh cho qua.
printf '{"permissions":{"deny":["Edit(/x/**)"],"allow":["Bash(git status:*)"]}}' > "$R/.claude/settings.json"
pt 'deny không có Bash(...) — git ngoài allow vẫn chặn, ghi log' Bash 'git push' BLOCK 1
pt 'deny không có Bash(...) — git status có trong allow, cho qua' Bash 'git status' PASS 1
printf '{"permissions":{"deny":["Bash(npm publish:*)"],"allow":["Bash(ls:*)"]}}' > "$R/.claude/settings.json"
pt 'allow không có mục Bash(git …) — git status bị chặn, ghi log' Bash 'git status' BLOCK 1
pt 'allow không có mục Bash(git …) — lệnh không phải git cho qua' Bash 'ls -la' PASS 1
printf '{"permissions":{"deny":["Bash(git push:*)"],"allow":["Bash(git push:*)","Bash(git status:*)"]}}' > "$R/.claude/settings.json"
pt 'git push có ở cả deny lẫn allow — deny thắng' Bash 'git push' BLOCK
printf '{"permissions":{"deny":["Bash(npm publish:*)"],"allow":["Bash(git:*)","Bash(git status:*)"]}}' > "$R/.claude/settings.json"
pt 'allow có Bash(git:*) — không nhận mục không có lệnh con, chặn, ghi log' Bash 'git push' BLOCK 1
mv "$WORK/set.bak" "$R/.claude/settings.json"

# lý do chặn là tiếng Việt, nêu đúng nguồn chặn
pj PreToolUse Bash command "$(valf 'cd x && git -C . push origin')" | bash "$R/.claude/hooks/on-pretool.sh" > /dev/null 2> "$WORK/pt.err"
row on-pretool "lý do chặn nêu mục cấm Bash(git push:*)" "có" "$(grep -q 'Bash(git push:\*)' "$WORK/pt.err" && echo có || echo không)"
pj PreToolUse Bash command "$(valf 'git gc --prune=now')" | bash "$R/.claude/hooks/on-pretool.sh" > /dev/null 2> "$WORK/pt.err"
row on-pretool "lý do chặn lệnh ngoài danh sách nêu permissions.allow và CLAUDE.md §1" "có" "$(grep -q 'permissions.allow' "$WORK/pt.err" && grep -q 'CLAUDE.md §1' "$WORK/pt.err" && echo có || echo không)"
pj PreToolUse Bash command "$(valf 'git diff --output=x.txt')" | bash "$R/.claude/hooks/on-pretool.sh" > /dev/null 2> "$WORK/pt.err"
row on-pretool "lý do chặn tuỳ chọn nêu chính tuỳ chọn --output=x.txt" "có" "$(grep -q -- '--output=x.txt' "$WORK/pt.err" && echo có || echo không)"

# --- lệnh lớn: thời gian phải tuyến tính theo độ dài lệnh.
# Harness giết hook quá `timeout` của khối hooks (10 s) rồi cho lệnh đi tiếp, nên hook
# chậm theo bậc hai = lệnh cấm đứng sau một heredoc lớn lọt lớp hai. Bản cũ chép đuôi
# chuỗi trong vòng lặp (giải mã JSON, bỏ thân heredoc): ~300 KB mất ~17 s trên Git Bash.
# Hook chạy dưới `timeout 10` như harness; trần thời gian là trần rộng, không phải số đo.
mkbig() { # tệp, KB, heredoc|nhay-don, đuôi lệnh
  node -e '
    const [f, kb, kind, tail] = process.argv.slice(1);
    let body = "", i = 0;
    while (Buffer.byteLength(body) < kb * 1024) body += "dòng " + (i++) + " có \"nháy\" và `code` \\ git push \u{1F4D0}\n";
    const cmd = kind === "heredoc" ? "cat <<EOF > big.md\n" + body + "EOF\n" + tail
                                   : "node -e \x27" + body + "\x27\n" + tail;
    require("fs").writeFileSync(f, JSON.stringify({ session_id: "s", hook_event_name: "PreToolUse",
      tool_name: "Bash", tool_input: { command: cmd, description: "lệnh lớn" } }));
  ' "$@"
}
ptbig() { # tên, tệp payload, PASS|BLOCK, trần ms
  local s0 s1 rc res ms
  s0=$(date +%s%N)
  timeout 10 bash "$R/.claude/hooks/on-pretool.sh" < "$2" > /dev/null 2>&1; rc=$?
  s1=$(date +%s%N)
  case "$rc" in 0) res=PASS ;; 2) res=BLOCK ;; 124) res="HẾT GIỜ — harness cho lọt" ;; *) res="rc=$rc" ;; esac
  ms=$(( (s1 - s0) / 1000000 ))
  row on-pretool "$1" "$3 / dưới $4 ms" "$res / $([ "$ms" -lt "$4" ] && echo "dưới $4 ms" || echo "$ms ms")"
}
mkbig "$WORK/big300-push.json" 300 heredoc 'git push'
mkbig "$WORK/big300-ls.json" 300 heredoc 'ls'
mkbig "$WORK/big600-push.json" 600 heredoc 'git push'
mkbig "$WORK/sq300-push.json" 300 nhay-don 'git push'
ptbig 'heredoc ~300 KB rồi ls — cho qua, kịp giờ' "$WORK/big300-ls.json" PASS 3000
ptbig 'heredoc ~300 KB rồi git push — chặn, kịp giờ' "$WORK/big300-push.json" BLOCK 3000
ptbig 'heredoc ~600 KB rồi git push — chặn, kịp giờ' "$WORK/big600-push.json" BLOCK 6000
ptbig "node -e '…~300 KB…' rồi git push — chặn, kịp giờ" "$WORK/sq300-push.json" BLOCK 3000

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
