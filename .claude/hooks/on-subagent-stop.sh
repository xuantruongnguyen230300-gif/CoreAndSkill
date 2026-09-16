#!/usr/bin/env bash
# on-subagent-stop.sh — hook SubagentStop, matcher ^core-reviewer$.
#
# Việc duy nhất: ghi DẤU THỜI ĐIỂM một lượt core-reviewer vừa kết thúc, vào
# .claude/.state/core-reviewed. Hook Stop so mtime các file Core đã sửa với dấu
# này để biết thay đổi nào chưa có lượt review nào mới hơn.
#
# Không tin riêng matcher: chỉ ghi dấu khi payload tự khai agent_type là
# core-reviewer. Matcher sai hoặc không được áp thì hook chạy cho MỌI subagent,
# và một lượt backend-expert kết thúc sẽ được tính là "đã review".
#
# Payload không có trường agent_type -> KHÔNG ghi dấu, ghi lý do ra
# .claude/.state/subagent-stop-error.log. Hook Stop trỏ tới file đó khi vẫn chặn
# sau một lượt review — lỗi không biến mất im lặng.
#
# Không jq, không grep -P, tự ép locale. Luôn thoát 0: hook này không chặn gì.

set -uo pipefail
export LC_ALL=en_US.UTF-8
cd "$(dirname "$0")/../.." || exit 0

STATE=".claude/.state"
mkdir -p "$STATE" 2>/dev/null || exit 0

payload=$(cat 2>/dev/null || true)

# Mau BRE tham lam bam lan xuat hien CUOI; khoa that chi xuat hien mot lan, con
# chuoi "agent_type" nam trong noi dung bao cao thi da bi thoat thanh \" nen
# khong khop mau (ngay truoc dau nhay dong la dau gach cheo nguoc).
at=$(printf '%s' "$payload" | sed -n 's/.*"agent_type"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -1)

case "$at" in
  core-reviewer)
    : > "$STATE/core-reviewed"
    rm -f "$STATE/subagent-stop-error.log"
    ;;
  '')
    keys=$(printf '%s' "$payload" | grep -o '"[A-Za-z_]*"[[:space:]]*:' | head -20 | tr -d '\n')
    printf '%s payload SubagentStop không có trường agent_type — KHÔNG ghi dấu review. Khoá có trong payload: %s\n' \
      "$(date '+%F %T' 2>/dev/null)" "${keys:-(không trích được khoá nào)}" >> "$STATE/subagent-stop-error.log"
    ;;
  *) ;;
esac

exit 0
