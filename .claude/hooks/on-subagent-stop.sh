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
# MỌI lần không ghi dấu đều để lại một dòng lý do ở
# .claude/.state/subagent-stop-error.log — payload không có agent_type, agent_type
# rỗng, hay agent_type khác core-reviewer. Matcher đã lọc ^core-reviewer$, nên tới
# được đây mà không ghi dấu là một dị thường: im lặng ở nhánh đó là để cổng biến
# mất mà không ai biết. Hook Stop trỏ tới file log khi vẫn chặn sau một lượt review.
#
# Không jq, không grep -P, tự ép locale. Luôn thoát 0: hook này không chặn gì.

set -uo pipefail
export LC_ALL=en_US.UTF-8
cd "$(dirname "$0")/../.." || exit 0

STATE=".claude/.state"
mkdir -p "$STATE" 2>/dev/null || exit 0

payload=$(cat 2>/dev/null || true)

# top_str nằm ở lib.sh — cùng một bản cho mọi hook. Nạp hỏng thì KHÔNG ghi dấu
# và để lại lý do: một lượt review không được ghi dấu là hook Stop chặn mãi mà
# không ai biết vì sao.
if ! . .claude/hooks/lib.sh 2>/dev/null; then
  printf '%s %s\n' "$(date '+%F %T' 2>/dev/null)" "không nạp được .claude/hooks/lib.sh — KHÔNG ghi dấu review." >> "$STATE/subagent-stop-error.log"
  exit 0
fi

# In an toàn vào log: chỉ ký tự in được, cắt ngắn.
shown() { printf '%s' "$1" | LC_ALL=C tr -c '[:print:]' '?' | cut -c1-80; }

log() {
  printf '%s %s\n' "$(date '+%F %T' 2>/dev/null)" "$1" >> "$STATE/subagent-stop-error.log"
}

if at=$(printf '%s' "$payload" | top_str agent_type); then
  case "$at" in
    core-reviewer)
      : > "$STATE/core-reviewed"
      rm -f "$STATE/subagent-stop-error.log"
      ;;
    '')
      log "payload SubagentStop có agent_type RỖNG — KHÔNG ghi dấu review."
      ;;
    *)
      log "payload SubagentStop có agent_type '$(shown "$at")', không phải core-reviewer — KHÔNG ghi dấu review. Matcher ^core-reviewer$ lẽ ra đã lọc payload này."
      ;;
  esac
else
  keys=$(printf '%s' "$payload" | LC_ALL=C grep -o '"[A-Za-z_]*"[[:space:]]*:' | head -20 | tr -d '\n')
  log "payload SubagentStop không có trường agent_type ở tầng ngoài (hoặc JSON cụt) — KHÔNG ghi dấu review. Khoá có trong payload: ${keys:-(không trích được khoá nào)}"
fi

exit 0
