#!/usr/bin/env bash
# scripts/fe-gate.sh — cổng SCRIPT của FE (nhóm §3.1 trong docs/wiki-core/fe/trien-khai/05-gate.md)
#
# Chạy từ gốc repo:
#     bash scripts/fe-gate.sh
#
# Thoát 0 = PASS. Thoát 1 = có vi phạm. Thoát 2 = không chạy được (cây repo sai).
#
# 🛑 Đây KHÔNG phải toàn bộ cổng FE — chỉ nhóm chạy bằng script. Nhóm chạy bằng công cụ
# (`ng lint`, `ng build`, `ng test`) PHẢI chạy riêng, đúng thứ tự script → lint → test → build
# — xem docs/wiki-core/fe/trien-khai/05-gate.md §5.
#
# Mỗi section mang ĐÚNG mã luật F của nó — không phải một hệ mã thứ hai (05-gate.md, đầu file).
#
# Phạm vi hôm nay (pha F0 + F1 + F8 của F2) + một nợ §10 đã đóng: F3, F4, F5, F6, F7, F8, F9, F10,
# F11, F12, F18, F19, F20, F21, F22, F25, F27, F29, F36, F37, F38, F39. Canary: scripts/tests/fe-gate-f<mã>.test.sh.
#
# F25 cùng loại với F27 bên dưới: một khoản nợ ở RULES.md §10, đóng khi màn danh sách Core đầu
# tiên đọc seam CORE_SCREEN_EXT (fe-architecture.md §2.7 luật 5). Canary:
# scripts/tests/fe-gate-f25.test.sh.
#
# F27 không nằm trong bảng "Bật ở pha" của 05-gate.md §3.1 — nó là một khoản nợ ở RULES.md §10,
# đóng trực tiếp vì hình dạng máy đọc được đã có sẵn ngay (🔧 làm được ngay), không đợi cổng lên
# theo pha. Phát hiện lần đầu bởi core-reviewer ở PR F2; quyết định remediation ở
# adr/0037-f2-dung-du-component-core-khong-hoan-ngam.md. Đọc `05-gate.md` sau khi F27 được thêm ở
# đây có thể chưa khớp — tài liệu đó là lộ trình theo pha, không phải điều kiện để đóng một khoản
# nợ đã có hình dạng máy đọc được; đồng bộ bảng đó là việc của người giữ nội dung docs/, không phải
# việc của script này.
#
# F4 so hai TẬP tên (BUSINESS_MODULES với thư mục src/app/modules/ thật) — tập RỖNG khớp tập
# RỖNG là PASS, không phải lỗi: đó là trạng thái đúng vĩnh viễn của chính repo Core (không bao giờ
# có modules/, ADR-0032) và trạng thái tạm thời của một dự án hạ nguồn trước module đầu tiên.
# Đây là ngoại lệ có tên riêng của F4 — KHÔNG suy rộng sang cổng khác. Lý do đầy đủ và nghĩa vụ
# canary khi dự án hạ nguồn thêm module đầu tiên: adr/0036-f4-rong-khop-rong-la-hop-le.md.
#
# F5, F8, F11, F22 đọc allowlist/ngưỡng/lệnh từ bảng chủ lúc chạy (05-gate.md §8.3, §8.6, §8.8;
# fe-ui-conventions.md §5.2; quy-uoc/fe-architecture.md §5). Mỗi section đỏ khi đọc ra 0 phần tử
# hoặc 0 tệp đầu vào — chốt chống "xanh rỗng" (05-gate.md §8.2: ngoại lệ tập rỗng là của riêng F4).
# F6, F7, F9, F10, F12, F18 không đọc bảng chủ nào nhưng cũng đếm tập tệp mình sẽ hỏi và đỏ khi bằng 0 (05-gate.md §8,
# nguyên tắc 2; RULES.md §8 luật T6) — "OK" chỉ in kèm số tệp đã quét.
#
# Các mã "Bật ở pha F0" còn lại theo bảng — F1, F13, F24 — là cổng CÔNG CỤ (`ng lint`/`ng build`),
# không thuộc script này (05-gate.md §2).
#
# Biến FE cho phép override thư mục FE gốc (mặc định src/FE) — dùng bởi
# scripts/tests/fe-gate-f4.test.sh để trỏ script này vào một fixture tạm, gọi THẲNG script
# thật thay vì chép lại logic. KHÔNG đổi hành vi mặc định khi chạy bình thường.

set -uo pipefail
cd "$(dirname "$0")/.." || exit 2

FE="${FE:-src/FE}"
FAIL=0
section() { printf '\n\033[1m== %s ==\033[0m\n' "$1"; }
bad()     { printf '  \033[31mFAIL\033[0m  %s\n' "$1"; FAIL=1; }
ok()      { printf '  \033[32mOK\033[0m    %s\n' "$1"; }
# note — thông tin KHÔNG làm cổng đỏ (F22: dòng thô vượt mà dòng mã không vượt — ADR-0081). Chữ in ra
# không được chứa "OK" hay "FAIL": canary chấm section bằng hai chữ đó.
note()    { printf '  \033[33mNOTE\033[0m  %s\n' "$1"; }

# quet_ca_tep <mẫu perl> — đọc danh sách tệp (ngăn bằng NUL) từ stdin, so mẫu trên NỘI DUNG CẢ TỆP
# (`\s` khớp cả xuống dòng) và in `tệp:dòng:dòng chứa chỗ khớp`. Dùng cho luật mà prettier bẻ một
# lời gọi ra nhiều dòng (F19, F21) — grep theo dòng mù trước lời gọi đã bị bẻ. Mẫu dùng `\x27` `\x22`
# `\x60` cho ba loại nháy để khỏi thoát nháy trong shell.
quet_ca_tep() {
  MAU="$1" xargs -0 -r perl -0777 -ne '
    while (/$ENV{MAU}/g) {
      my $vt = $-[0];
      my $dong = 1 + (substr($_, 0, $vt) =~ tr/\n//);
      my $dau = rindex($_, "\n", $vt - 1) + 1;
      my ($txt) = substr($_, $dau) =~ /\A([^\r\n]*)/;
      print "$ARGV:$dong:$txt\n";
    }'
}

[ -d "$FE" ] || { printf '\033[31m❌ ABORT — không thấy %s. Chạy script từ gốc repo.\033[0m\n' "$FE"; exit 2; }

# ================================================================ F3
section "F3 - cấm eslint-disable cho quy tắc ranh giới"
BANG=docs/quy-uoc/fe-architecture.md
if [ ! -f "$BANG" ]; then
  bad "không thấy $BANG"
elif [ ! -d "$FE/src" ]; then
  bad "không có $FE/src để quét"
else
  RULES=$(awk '/Danh sách rule cấm tắt/ { f = 1; next }
               /^#/ { f = 0 }
               f && /^\| `/' "$BANG" | cut -d'`' -f2 | paste -sd'|' -)
  SO_TEP=$(find "$FE/src" \( -name '*.ts' -o -name '*.html' \) | wc -l)
  if [ -z "$RULES" ]; then
    bad "không đọc được danh sách rule cấm tắt từ $BANG"
  elif [ "$SO_TEP" -eq 0 ]; then
    bad "không có tệp *.ts / *.html nào trong $FE/src để quét — cổng sẽ xanh rỗng"
  else
    HITS=$(grep -rnE "eslint-disable(-next-line|-line)?.*($RULES)" "$FE/src" --include='*.ts' --include='*.html')
    # Dạng KHÔNG kèm tên rule tắt mọi rule, kể cả rule ranh giới — mẫu ở fe-architecture.md §4.5.
    # Sau từ khoá chỉ còn khoảng trắng rồi hết dòng, `*/`, `-->` hoặc phần mô tả `-- ...`.
    HITS_WHOLE=$(grep -rnE 'eslint-disable(-next-line|-line)?[[:space:]]*($|\*/|-->|--[[:space:]])' \
      "$FE/src" --include='*.ts' --include='*.html')
    # Comment mang nhãn `eslint` (cấu hình nội tuyến `/* eslint <rule>: "off" */`) hoặc `eslint-disable…`
    # nhắc tên rule cấm ở BẤT KỲ dòng nào của comment. Hai mẫu dòng ở trên không thấy dạng này, và
    # ESLint thoát 0 với nó — đã thử bằng `npx eslint --stdin`. Quét cả `<!-- -->` trong .ts vì
    # template nội tuyến cũng nhận comment đó. Lệnh gốc: fe-architecture.md §4.5.
    HITS_KHOI=$(find "$FE/src" \( -name '*.ts' -o -name '*.html' \) -print0 \
      | F3_RULES="$RULES" xargs -0 -r perl -0777 -ne '
          my $cam = join "|", map { quotemeta } split /\|/, $ENV{F3_RULES};
          for my $mau (qr{/\*\s*eslint(?:-disable(?:-next-line|-line)?)?(?=\s|\*/)(.*?)(?:\*/|\z)}s,
                       qr{<!--\s*eslint(?:-disable(?:-next-line|-line)?)?(?=\s|-->)(.*?)(?:-->|\z)}s,
                       qr{//[ \t]*eslint(?:-disable(?:-next-line|-line)?)?(?=\s)([^\n]*)}) {
            while (/$mau/g) {
              my ($than, $vt) = ($1, $-[0]);
              next unless $than =~ /$cam/;
              my $dong = 1 + (substr($_, 0, $vt) =~ tr/\n//);
              my ($txt) = substr($_, $vt) =~ /\A([^\r\n]*)/;
              print "$ARGV:$dong:$txt\n";
            }
          }')
    if [ -n "$HITS" ] || [ -n "$HITS_WHOLE" ] || [ -n "$HITS_KHOI" ]; then
      bad "eslint-disable hoặc cấu hình nội tuyến chạm rule ranh giới:"
      [ -n "$HITS" ] && printf '%s\n' "$HITS"
      [ -n "$HITS_WHOLE" ] && printf '%s\n' "$HITS_WHOLE"
      [ -n "$HITS_KHOI" ] && printf '%s\n' "$HITS_KHOI"
    else
      ok "không có eslint-disable hay cấu hình nội tuyến nào chạm rule ranh giới ($SO_TEP tệp đã quét)"
    fi
  fi
fi

# ================================================================ F4
section "F4 - BUSINESS_MODULES khớp thư mục modules/ thật (tập rỗng khớp tập rỗng là PASS — ADR-0036)"
BOUNDARIES_CJS="$FE/eslint.boundaries.cjs"
if [ ! -f "$BOUNDARIES_CJS" ]; then
  bad "không có $BOUNDARIES_CJS"
else
  if [ -d "$FE/src/app/modules" ]; then
    FOLDERS=$(ls -1 "$FE/src/app/modules" | sort)
  else
    FOLDERS=""
  fi
  # Truyền đường dẫn qua argv[1] rồi resolve bằng path.resolve — chạy đúng dù $BOUNDARIES_CJS
  # là đường dẫn tương đối (bình thường) hay tuyệt đối (khi FE bị override, xem test F4).
  CONFIG_RAW=$(node -p "require(require('path').resolve(process.argv[1])).BUSINESS_MODULES.join('\n')" \
    "$BOUNDARIES_CJS" 2>&1)
  NODE_EXIT=$?
  if [ "$NODE_EXIT" -ne 0 ]; then
    bad "$BOUNDARIES_CJS lỗi khi đọc BUSINESS_MODULES (node exit $NODE_EXIT) — không phải tập rỗng hợp lệ:"
    printf '%s\n' "$CONFIG_RAW"
  else
    CONFIG=$(printf '%s\n' "$CONFIG_RAW" | sort)
    DIFF=$(diff <(printf '%s\n' "$FOLDERS") <(printf '%s\n' "$CONFIG"))
    if [ -n "$DIFF" ]; then
      bad "BUSINESS_MODULES lệch với thư mục modules/ thật:"; printf '%s\n' "$DIFF"
    else
      ok "BUSINESS_MODULES khớp thư mục modules/ thật (kể cả khi cả hai đều rỗng)"
    fi
  fi
fi

# ================================================================ F5
section "F5 - chỉ đường dẫn trong allowlist được import primeng/ (allowlist đọc từ bảng chủ)"
BANG=docs/wiki-core/fe/05-component-library.md
if [ ! -f "$BANG" ]; then
  bad "không thấy $BANG"
elif [ ! -d "$FE/src/app" ]; then
  bad "không có $FE/src/app để quét"
else
  # Lệnh gốc: docs/wiki-core/fe/trien-khai/05-gate.md §8.3. Cột đầu của bảng tính từ src/FE/.
  DUONG_DAN=$(awk '/Allowlist import thư viện UI/ { f = 1; next }
                   /^#/ { f = 0 }
                   f && /^\| `/ && /✔/' "$BANG" | cut -d'`' -f2)
  MAU=$(printf '%s\n' "$DUONG_DAN" | sed -e 's#/\*\*$#/#' -e "s#^#^$FE/#" | paste -sd'|' -)
  SO_TEP=$(find "$FE/src/app" -name '*.ts' -not -name '*.spec.ts' | wc -l)
  if [ -z "$DUONG_DAN" ]; then
    # Rỗng ở đây là nguồn đọc hỏng, không phải trạng thái hợp lệ (05-gate.md §8.2, ngoại lệ chỉ của F4).
    bad "không đọc được allowlist từ $BANG"
  elif [ "$SO_TEP" -eq 0 ]; then
    bad "không có tệp *.ts nào trong $FE/src/app để quét — cổng sẽ xanh rỗng"
  else
    # Allowlist khoá đường dẫn không tồn tại là cổng luôn xanh (05-gate.md §8.3).
    THIEU=""
    while IFS= read -r p; do
      [ -e "$FE/${p%/\*\*}" ] || THIEU="$THIEU  $p"$'\n'
    done <<< "$DUONG_DAN"
    HITS=$(grep -rn "from 'primeng/" "$FE/src/app" --include='*.ts' | grep -v '\.spec\.ts:' | grep -vE "$MAU")
    if [ -n "$THIEU" ]; then
      bad "allowlist F5 trỏ tới đường dẫn không tồn tại:"; printf '%s' "$THIEU"
    fi
    if [ -n "$HITS" ]; then
      bad "import primeng/ ngoài allowlist:"; printf '%s\n' "$HITS"
    fi
    [ -z "$THIEU" ] && [ -z "$HITS" ] && ok "mọi import primeng/ nằm trong allowlist ($SO_TEP tệp đã quét)"
  fi
fi

# ================================================================ F6
section "F6 - không hex color literal trong SCSS (trừ tệp khai token)"
if [ ! -d "$FE/src" ]; then
  bad "không có $FE/src để quét"
else
  # Đầu vào là tập *.scss SẼ bị hỏi — tệp khai token được loại trừ nên không tính: còn mỗi nó thì cổng không hỏi gì.
  SO_TEP=$(find "$FE/src" -name '*.scss' -not -path "$FE/src/styles/_tokens.scss" | wc -l)
  if [ "$SO_TEP" -eq 0 ]; then
    bad "không có tệp *.scss nào ngoài tệp khai token trong $FE/src để quét — cổng sẽ xanh rỗng"
  else
    HITS=$(grep -rnE '#([0-9a-fA-F]{3,4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})\b' "$FE/src" --include='*.scss' \
      | grep -v "^$FE/src/styles/_tokens\.scss:")
    if [ -n "$HITS" ]; then
      bad "hex literal ngoài tệp khai token:"; printf '%s\n' "$HITS"
    else
      ok "không có hex literal nào ngoài $FE/src/styles/_tokens.scss ($SO_TEP tệp đã quét)"
    fi
  fi
fi

# ================================================================ F7
section "F7 - không rgb()/rgba() literal trong SCSS (trừ dạng đọc token pha alpha)"
if [ ! -d "$FE/src/app" ]; then
  bad "không có $FE/src/app để quét"
else
  # Bẫy 04-design-token-system.md §6.3: GỠ dạng được phép ra khỏi dòng TRƯỚC, rồi mới hỏi lại
  # phần còn lại — nếu chỉ loại cả dòng có chứa dạng hợp lệ, một dòng vừa có rgb(var(--x)/a) hợp
  # lệ vừa có một literal trần sẽ được tha nhầm nguyên dòng.
  SO_TEP=$(find "$FE/src/app" -name '*.scss' | wc -l)
  if [ "$SO_TEP" -eq 0 ]; then
    bad "không có tệp *.scss nào trong $FE/src/app để quét — cổng sẽ xanh rỗng"
  else
    HITS=$(grep -rnE 'rgba?\(' "$FE/src/app" --include='*.scss' \
      | sed -E 's/rgba?\( *var\(--[A-Za-z0-9_-]+\) *\/[^)]*\)//g' \
      | grep -E 'rgba?\(')
    if [ -n "$HITS" ]; then
      bad "rgb()/rgba() literal (không phải dạng đọc token pha alpha):"; printf '%s\n' "$HITS"
    else
      ok "mọi rgb()/rgba() trong $FE/src/app đều ở dạng đọc token pha alpha ($SO_TEP tệp đã quét)"
    fi
  fi
fi

# ================================================================ F8
section "F8 - mọi câu đến từ i18n: template không chứa chữ tiếng Việt, khoá dịch literal có trong vi.json"
if [ ! -d "$FE/src/app" ]; then
  bad "không có $FE/src/app để quét"
else
  # Lệnh gốc: docs/quy-uoc/fe-ui-conventions.md §5.2. Chú thích HTML thay bằng ĐÚNG số dòng nó
  # chiếm để số dòng báo ra khớp tệp thật (05-gate.md §8.6). Ép LC_ALL=C.UTF-8: ở locale C, grep so
  # lớp ký tự theo BYTE và bắt nhầm ký tự UTF-8 khác có chung byte.
  SO_TEP=$(find "$FE/src/app" -name '*.html' | wc -l)
  if [ "$SO_TEP" -eq 0 ]; then
    bad "không có tệp *.html nào trong $FE/src/app để quét — cổng sẽ xanh rỗng"
  else
    HITS=$(find "$FE/src/app" -name '*.html' | while IFS= read -r f; do
      perl -0777 -pe 's{<!--.*?-->}{ "\n" x (() = ($& =~ /\n/g)) }gse' "$f" \
        | LC_ALL=C.UTF-8 grep -nE '[àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđ]' \
        | sed "s|^|$f:|"
    done)
    if [ -n "$HITS" ]; then
      bad "template chứa chữ tiếng Việt — đưa vào tệp dịch:"; printf '%s\n' "$HITS"
    else
      ok "không template nào chứa chữ tiếng Việt ($SO_TEP tệp đã quét)"
    fi
  fi

  # Vế thứ hai của cùng câu luật "mọi câu đến từ i18n": khoá dịch viết LITERAL trong src/app phải có
  # trong tệp dịch vi.json — thiếu thì người dùng thấy chuỗi khoá thô, tức câu đó không đến từ i18n.
  # Tập khoá có = hợp của mọi tầng public/i18n*/vi.json (08-i18n.md §2.3), kể cả nút trung gian.
  # Khoá dùng = `'a.b' | translate`, `translate.instant|stream|get('a.b')` và `title: 'a.b'` của
  # *.routes.ts, so trên nội dung cả tệp vì prettier bẻ khoá và pipe ra hai dòng. Khoá dựng từ biến
  # KHÔNG thấy được (08-i18n.md §9). Lệnh gốc: 05-gate.md §8.6. Canary: scripts/tests/fe-gate-f8.test.sh.
  VI_JSON=$(find "$FE/public" -path "$FE/public/i18n*/vi.json" 2>/dev/null | sort)
  if [ ! -f "$FE/public/i18n/vi.json" ]; then
    bad "không có $FE/public/i18n/vi.json — không có tập khoá để đối chiếu"
  else
    KHOA_CO=$(for j in $VI_JSON; do
      node -e 'const f=(o,p)=>Object.entries(o).flatMap(([k,v])=>v!==null&&typeof v==="object"?[p+k,...f(v,p+k+".")]:[p+k]);
process.stdout.write(f(JSON.parse(require("fs").readFileSync(process.argv[1],"utf8")),"").join("\n")+"\n")' "$j" \
        || echo "__LOI_DOC__:$j"
    done 2>/dev/null | sort -u)
    LOI_DOC=$(printf '%s\n' "$KHOA_CO" | grep '^__LOI_DOC__:' | cut -d: -f2-)
    KHOA_DUNG=$(find "$FE/src/app" \( -name '*.ts' -o -name '*.html' \) -not -name '*.spec.ts' -print0 \
      | xargs -0 -r perl -0777 -ne '
          my $k = qr{[A-Za-z][\w-]*(?:\.[\w-]+)+};
          while (/([\x27"])($k)\1\s*\|\s*translate\b/g) { print "$2\n" }
          while (/\btranslate\s*\.\s*(?:instant|stream|get)\s*\(\s*([\x27"])($k)\1/g) { print "$2\n" }
          if ($ARGV =~ /\.routes\.ts$/) { while (/\btitle\s*:\s*([\x27"])($k)\1/g) { print "$2\n" } }' \
      | sort -u)
    SO_KHOA=$(printf '%s' "$KHOA_DUNG" | grep -c .)
    if [ -n "$LOI_DOC" ]; then
      bad "tệp dịch không đọc được thành JSON — không coi là tập khoá rỗng:"; printf '%s\n' "$LOI_DOC"
    elif [ "$SO_KHOA" -eq 0 ]; then
      bad "không trích được khoá dịch literal nào từ $FE/src/app — mẫu trích đã mục? (cổng sẽ xanh rỗng)"
    else
      THIEU=$(comm -23 <(printf '%s\n' "$KHOA_DUNG") <(printf '%s\n' "$KHOA_CO"))
      if [ -n "$THIEU" ]; then
        bad "khoá dịch dùng trong src/app nhưng không có trong tệp dịch — người dùng sẽ thấy khoá thô:"
        printf '%s\n' "$THIEU" | while IFS= read -r k; do
          grep -rnF "$k" "$FE/src/app" --include='*.ts' --include='*.html' | grep -v '\.spec\.ts:' \
            | head -3 | sed "s|^|  $k ← |"
        done
      else
        ok "mọi khoá dịch literal có trong tệp dịch ($SO_KHOA khoá đã so)"
      fi
    fi
  fi
fi

# ================================================================ F9
section "F9 - không còn cú pháp Angular cũ"
if [ ! -d "$FE/src/app" ]; then
  bad "không có $FE/src/app để quét"
else
  SO_TEP=$(find "$FE/src/app" \( -name '*.ts' -o -name '*.html' \) | wc -l)
  if [ "$SO_TEP" -eq 0 ]; then
    bad "không có tệp *.ts / *.html nào trong $FE/src/app để quét — cổng sẽ xanh rỗng"
  else
    HITS=$(grep -rnE '\*ngIf|\*ngFor|\*ngSwitch|@Input\(\)|@Output\(\)|@ViewChild\(|NgModule' \
      "$FE/src/app" --include='*.ts' --include='*.html')
    if [ -n "$HITS" ]; then
      bad "còn cú pháp Angular cũ:"; printf '%s\n' "$HITS"
    else
      ok "không còn cú pháp Angular cũ ($SO_TEP tệp đã quét)"
    fi
  fi
fi

# ================================================================ F10
section "F10 - components/ và pages/ không import DTO trực tiếp"
if [ ! -d "$FE/src/app" ]; then
  bad "không có $FE/src/app để quét"
else
  # Lệnh gốc: fe-api-client.md §4.3 — cùng tập tệp (mã chạy thật dưới một thư mục components/ hoặc pages/, không
  # *.spec.ts: 05-gate.md §8.7), cùng hai phép dò, cùng chốt tập rỗng. Mọi kiểu trong *.dto.ts là DTO, kể cả …Payload;
  # người đọc DTO là services/ hoặc core/<mảng>/ (§4.1) — hai nơi đó nằm ngoài tập tệp, nên không đỏ ở đó.
  #   (1) theo TÊN — định danh kết thúc bằng `Dto`;
  #   (2) theo TỆP — đường nhập trỏ tới một tệp `*.dto`; kiểu dây không mang tên …Dto chỉ (2) thấy.
  # Hai chốt tập rỗng như lệnh gốc: không còn tệp nào trong tập; không còn tệp *.dto.ts nào dưới src/app — khi đó (2)
  # không còn gì để thấy. Section RỘNG HƠN lệnh gốc ở hai chỗ, mỗi chỗ chỉ thêm vi phạm thật (cùng khuôn F19): (2) bắt
  # cả `import('…dto')` (kiểu nhập nội tuyến) và `import '…dto'` bên cạnh `from '…dto'`; và so trên nội dung cả tệp vì
  # prettier bẻ đối số của `import(` sang dòng sau. Canary: scripts/tests/fe-gate-f10.test.sh.
  TEP_F10=$(find "$FE/src/app" -name '*.ts' -not -name '*.spec.ts' | grep -E '/(components|pages)/')
  SO_TEP=$(printf '%s' "$TEP_F10" | grep -c .)
  SO_DTO=$(find "$FE/src/app" -name '*.dto.ts' | wc -l)
  if [ "$SO_TEP" -eq 0 ]; then
    bad "không có tệp *.ts nào dưới components/ hoặc pages/ trong $FE/src/app để quét — cổng sẽ xanh rỗng"
  elif [ "$SO_DTO" -eq 0 ]; then
    bad "không có tệp *.dto.ts nào trong $FE/src/app — phép dò theo đường nhập không còn gì để thấy (cổng sẽ xanh rỗng)"
  else
    MAU_NHAP_DTO='\b(?:from|import)\s*\(?\s*[\x27\x22][^\x27\x22\n]*\.dto[\x27\x22]'
    HITS=$( { printf '%s\n' "$TEP_F10" | tr '\n' '\0' | xargs -0 -r grep -HnE 'Dto\b'
              printf '%s\n' "$TEP_F10" | tr '\n' '\0' | quet_ca_tep "$MAU_NHAP_DTO"
            } | sort -u)
    if [ -n "$HITS" ]; then
      bad "DTO lọt vào components/ hoặc pages/ (theo tên …Dto hoặc theo đường nhập tệp *.dto):"; printf '%s\n' "$HITS"
    else
      ok "không có DTO nào trong components/ hoặc pages/ ($SO_TEP tệp đã quét; $SO_DTO tệp *.dto.ts trong src/app)"
    fi
  fi
fi

# ================================================================ F11
section "F11 - shared/components/ không inject service lấy dữ liệu (token được tha đọc từ bảng chủ)"
BANG=docs/wiki-core/fe/trien-khai/05-gate.md
if [ ! -f "$BANG" ]; then
  bad "không thấy $BANG"
elif [ ! -d "$FE/src/app/shared/components" ]; then
  bad "không có $FE/src/app/shared/components để quét"
else
  # Lệnh gốc: 05-gate.md §8.8. Gỡ dạng được tha ra khỏi dòng TRƯỚC rồi mới hỏi phần còn lại — loại
  # cả dòng thì `inject(DestroyRef)` đứng cạnh một inject service sẽ tha nhầm nguyên dòng.
  # Lỗ mù đã biết (05-gate.md §8.8): không bắt inject qua tham số constructor.
  TOKEN=$(awk '/Token được inject trong component dumb/ { f = 1; next }
               /^#/ { f = 0 }
               f && /^\| `/' "$BANG" | cut -d'`' -f2 | paste -sd'|' -)
  SO_TEP=$(find "$FE/src/app/shared/components" -name '*.ts' -not -name '*.spec.ts' | wc -l)
  if [ -z "$TOKEN" ]; then
    bad "không đọc được allowlist token từ $BANG"
  elif [ "$SO_TEP" -eq 0 ]; then
    bad "không có tệp *.ts nào trong $FE/src/app/shared/components để quét — cổng sẽ xanh rỗng"
  else
    HITS=$(grep -rnE 'inject(<[^()]*>)?\(' "$FE/src/app" --include='*.ts' | grep '/shared/components/' \
      | grep -v '\.spec\.ts:' \
      | sed -E "s/inject(<[^()]*>)?\( *($TOKEN)(<[^()]*>)?( *, *\{[^()]*\})? *\)//g" \
      | grep -E 'inject(<[^()]*>)?\(')
    if [ -n "$HITS" ]; then
      bad "component dumb inject thứ ngoài allowlist token:"; printf '%s\n' "$HITS"
    else
      ok "shared/components/ chỉ inject token được tha ($SO_TEP tệp đã quét)"
    fi
  fi
fi

# ================================================================ F12
section "F12 - mọi *.service.ts có .spec.ts cạnh nó"
if [ ! -d "$FE/src/app" ]; then
  bad "không có $FE/src/app để quét"
else
  SO_TEP=$(find "$FE/src/app" -name '*.service.ts' | wc -l)
  if [ "$SO_TEP" -eq 0 ]; then
    bad "không có tệp *.service.ts nào trong $FE/src/app để đối chiếu — cổng sẽ xanh rỗng"
  else
    MISSING=""
    while IFS= read -r f; do
      [ -f "${f%.ts}.spec.ts" ] || MISSING="$MISSING$f"$'\n'
    done < <(find "$FE/src/app" -name '*.service.ts')
    if [ -n "$MISSING" ]; then
      bad "service thiếu spec:"; printf '%s' "$MISSING"
    else
      ok "mọi *.service.ts đều có .spec.ts cạnh nó ($SO_TEP service đã đối chiếu)"
    fi
  fi
fi

# ================================================================ F18
section "F18 - không đọc data của envelope bằng dạng bị cấm (!, as, ??)"
if [ ! -d "$FE/src" ]; then
  bad "không có $FE/src để quét"
else
  SO_TEP=$(find "$FE/src" -name '*.ts' -not -name '*.spec.ts' | wc -l)
  if [ "$SO_TEP" -eq 0 ]; then
    bad "không có tệp *.ts nào (ngoài *.spec.ts) trong $FE/src để quét — cổng sẽ xanh rỗng"
  else
    HITS=$(grep -rnE '\.data\s*(!|as |\?\?)' "$FE/src" --include='*.ts' | grep -v '\.spec\.ts')
    if [ -n "$HITS" ]; then
      bad "đọc data bằng dạng bị cấm:"; printf '%s\n' "$HITS"
    else
      ok "không có chỗ nào đọc data bằng dạng bị cấm ($SO_TEP tệp đã quét)"
    fi
  fi
fi

# ================================================================ F19
section "F19 - đường dẫn HTTP trong service không mang tiền tố base URL"
if [ ! -d "$FE/src" ]; then
  bad "không có $FE/src để quét"
else
  # Hai mẫu, so trên nội dung cả tệp:
  #   (1) chuỗi mở đầu bằng `/api` ở BẤT KỲ đâu — đường dẫn còn đi qua trường như `duongDan`
  #       trước khi tới lời gọi, nên chỉ nhìn lời gọi là mù;
  #   (2) đối số đầu của một lời gọi HTTP mang tiền tố `api/`, có hay không có `/` đầu — prettier
  #       bẻ `this.http` / `.get<…>(` / đường dẫn ra ba dòng, nên so theo dòng là mù.
  # Lệnh gốc ở 05-gate.md §8.13 chỉ khớp `this.http.<verb><` trên một dòng — hẹp hơn hai mẫu này.
  # Canary: scripts/tests/fe-gate-f19.test.sh.
  SO_TEP=$(find "$FE/src" -name '*.ts' -not -name '*.spec.ts' | grep -c .)
  if [ "$SO_TEP" -eq 0 ]; then
    bad "không có tệp *.ts nào trong $FE/src để quét — cổng sẽ xanh rỗng"
  else
    MAU_CHUOI='[\x27\x22\x60]/api(?=[/\x27\x22\x60])'
    MAU_LOI_GOI='\.\s*(?:get|post|put|patch|delete|head|options|request|jsonp)\s*(?:<[^()]*?>)?\s*\(\s*[\x27\x22\x60]/?api/'
    HITS=$( { find "$FE/src" -name '*.ts' -not -name '*.spec.ts' -print0 | quet_ca_tep "$MAU_CHUOI"
              find "$FE/src" -name '*.ts' -not -name '*.spec.ts' -print0 | quet_ca_tep "$MAU_LOI_GOI"
            } | sort -u)
    if [ -n "$HITS" ]; then
      bad "đường dẫn HTTP mang tiền tố base URL:"; printf '%s\n' "$HITS"
    else
      ok "không có đường dẫn HTTP nào mang tiền tố base URL ($SO_TEP tệp đã quét)"
    fi
  fi
fi

# ================================================================ F20
section "F20 - app.routes.ts không import tĩnh component của feature"
ROUTES_FILE="$FE/src/app/app.routes.ts"
if [ ! -f "$ROUTES_FILE" ]; then
  bad "không có $ROUTES_FILE để quét"
else
  HITS=$(grep -nE "^import .* from '\./(platform|modules)/" "$ROUTES_FILE")
  if [ -n "$HITS" ]; then
    bad "app.routes.ts import tĩnh component của feature:"; printf '%s\n' "$HITS"
  else
    ok "app.routes.ts không import tĩnh component của feature"
  fi
fi

# ================================================================ F21
section "F21 - core/ không điều hướng bằng đường dẫn route viết cứng"
if [ ! -d "$FE/src/app/core" ]; then
  bad "không có $FE/src/app/core để quét"
else
  # Bốn API của Router nhận đường dẫn: `navigate([…])`, `createUrlTree([…])` (guard trả UrlTree),
  # `navigateByUrl(…)`, `parseUrl(…)` (RedirectCommand). Đỏ khi phần tử/đối số đầu là chuỗi mở đầu
  # bằng `/`. So trên nội dung cả tệp — prettier bẻ `navigate(` và mảng đường dẫn ra hai dòng khi
  # lời gọi kèm tuỳ chọn. Lệnh gốc ở 05-gate.md §8.14 chỉ khớp `navigate(['/` trên một dòng —
  # hẹp hơn mẫu này. Canary: scripts/tests/fe-gate-f21.test.sh.
  SO_TEP=$(find "$FE/src/app/core" -name '*.ts' -not -name '*.spec.ts' | grep -c .)
  if [ "$SO_TEP" -eq 0 ]; then
    bad "không có tệp *.ts nào trong $FE/src/app/core để quét — cổng sẽ xanh rỗng"
  else
    MAU_DIEU_HUONG='\b(?:(?:navigate|createUrlTree)\s*\(\s*\[\s*|(?:navigateByUrl|parseUrl)\s*\(\s*)[\x27\x22\x60]/'
    HITS=$(find "$FE/src/app/core" -name '*.ts' -not -name '*.spec.ts' -print0 \
      | quet_ca_tep "$MAU_DIEU_HUONG")
    if [ -n "$HITS" ]; then
      bad "core/ điều hướng bằng route viết cứng:"; printf '%s\n' "$HITS"
    else
      ok "core/ không điều hướng bằng route viết cứng ($SO_TEP tệp đã quét)"
    fi
  fi
fi

# ================================================================ F22
section "F22 - không tệp nào vượt ngưỡng cứng tính bằng DÒNG MÃ (loại tệp VÀ ngưỡng đọc từ bảng chủ; dòng thô vượt chỉ ghi chú)"
# Bảng chủ: docs/quy-uoc/fe-architecture.md §5, cột "Ngưỡng cứng". 05-gate.md §3.1 trỏ lệnh của F22
# về đúng bảng đó — bảng là nguồn duy nhất của cả NGƯỠNG lẫn DANH SÁCH LOẠI TỆP.
#
# 🛑 Không viết cứng con số ở đây. Bản trước của section này viết cứng 400 và chỉ đo *.page.ts: đó
# là NƠI THỨ HAI giữ con số, nên đổi ngưỡng ở bảng thì cổng vẫn canh số cũ, im lặng. Liệt kê tay
# danh sách loại tệp là cùng một lỗ đổi chiều: thêm một dòng vào bảng mà cổng không thấy. Dòng bảng
# không dịch được thành lệnh dò thì ĐỎ, không bỏ qua im lặng.
#
# Cột tìm theo TÊN TIÊU ĐỀ ("Loại file", "Ngưỡng cứng"), không theo chỉ số cột — đảo cột trong bảng
# mà cổng vẫn đọc cột thứ ba là một kiểu đọc nhầm im lặng nữa.
#
# Dịch cột "Loại file" thành lệnh dò: `<thư mục>/<mẫu>` → tìm dưới $FE/src/<thư mục>; `<mẫu>` trần
# → tìm dưới $FE/src/app. Miễn trừ *.spec.ts là câu ngay dưới bảng §5.
#
# ĐẾM HAI SỐ mỗi tệp (ADR-0081): DÒNG MÃ vượt ngưỡng cứng là FAIL; DÒNG THÔ (mọi dòng, như awk FNR)
# vượt mà dòng mã không vượt chỉ in NOTE. Phân loại dòng — trống / chú thích / mã — theo ĐÚNG đoạn
# "Phân loại một dòng" ở §5 của chính bảng chủ; bộ bóc F22_DEM dưới đây là bản thi hành của đoạn đó,
# không phải một định nghĩa thứ hai. Ba quy tắc giữ nó đơn giản, cả ba đến từ §5:
#   - dấu chú thích chỉ có hiệu lực khi ĐỨNG ĐẦU DÒNG (sau khi bỏ khoảng trắng) — nên `'https://…'`
#     giữa dòng không bao giờ mở chú thích, và bộ bóc không phải hiểu chuỗi ký tự;
#   - khối mở đầu dòng kéo tới dòng ĐẦU TIÊN chứa dấu đóng; dòng mở/đóng còn ký tự khác ngoài phần
#     chú thích thì là dòng mã (`/* a */ f();`);
#   - mọi chỗ mơ hồ nghiêng về MÃ: khối mở giữa dòng sau mã không được theo dõi.
# Cú pháp chú thích theo đuôi tệp ĐỌC LÚC CHẠY từ gạch "Cú pháp theo đuôi tệp" của §5 (CU_PHAP_5 dưới
# đây) — câu chữ gạch đó là đầu vào của cổng, script không giữ bản chép. Đuôi có trong bảng ngưỡng mà
# gạch đó không khai thì FAIL, không đoán; không đọc được gạch đó thì FAIL.
# Gom nhiều tệp vào một lần gọi awk bằng FNR cho nhanh.
#
# Chốt chống xanh rỗng (05-gate.md §8.2 — ngoại lệ tập rỗng là của riêng F4): đỏ khi bảng đọc ra 0
# dòng ngưỡng, và đỏ khi một loại trong bảng không khớp tệp nào.
#
# `-type f` KHÔNG thừa: `find` khớp cả THƯ MỤC trùng mẫu tên. Một thư mục tên `x.component.ts` từng
# làm SO_TEP đếm nó như một tệp — chốt chống xanh rỗng coi như loại đó có đầu vào, awk chỉ cảnh báo ra
# stderr rồi bỏ qua, và section in OK trong khi KHÔNG tệp `*.component.ts` thật nào được đo.
# Canary: ca "chỉ có một THƯ MỤC trùng mẫu tên".
#
# Lỗ mù đã biết: `*.scss` dưới $FE/src/app đo theo ngưỡng của dòng `*.scss`, kể cả khi nằm trong một
# thư mục tên `styles/` bên trong app — dòng `styles/*.scss` chỉ phủ gốc riêng của nó.
# Canary: scripts/tests/fe-gate-f22.test.sh.
BANG=docs/quy-uoc/fe-architecture.md
F22_AWK='
/^## / { f = ($0 ~ /Ngưỡng kích thước file/) ? 1 : 0; next }
f && /^\|/ {
  if (!hdr) {
    for (i = 1; i <= NF; i++) { c = $i; gsub(/^[ \t]+|[ \t]+$/, "", c)
      if (c == "Loại file") ct = i; if (c == "Ngưỡng cứng") cn = i }
    hdr = 1; next
  }
  if (!ct || !cn || $0 !~ /^\| `/) next
  mau = $ct; sub(/^[^`]*`/, "", mau); sub(/`.*$/, "", mau)
  cung = $cn; gsub(/^[ \t]+|[ \t]+$/, "", cung)
  if (cung ~ /^\*\*[0-9]+\*\*$/) gsub(/\*/, "", cung); else cung = "?"
  if (mau != "") print mau "\t" cung
}'
# BỘ BÓC DÒNG — dùng chung cho F22 (đếm) và mọi phép dò của F39 (in dòng mã): một bộ bóc, không hai.
# Mặc định in mỗi tệp một dòng `tệp<TAB>dòng thô<TAB>dòng mã`; với `-v in_ma=1` thì in từng dòng mã
# dạng `tệp:số dòng:nguyên văn` (như grep -n) thay cho con số. Biến vào: cm (dấu chú thích dòng, rỗng
# nếu cú pháp không có), mo / dg (dấu mở / đóng khối). So bằng index() — dấu là chuỗi thường, không regex.
# MỌI lời gọi bộ bóc kèm `-v BINMODE=3`. gawk của Git Bash đọc ở chế độ văn bản và tự nuốt `\r` trước
# `\n`; gawk trên Linux thì không. Không có cờ này, phép bỏ `\r` của chính bộ bóc (quy tắc "dòng trống,
# kể cả \r" ở §5) không bao giờ chạy trên máy Windows — tệp CRLF vẫn đếm đúng, nhưng các ca CRLF của
# canary xanh nhờ tầng đọc tệp chứ không nhờ bộ bóc, và một bộ bóc thôi bỏ `\r` lọt qua canary ở đây
# trong khi đỏ trên CI. BINMODE chỉ có nghĩa với gawk trên hệ không POSIX; nơi khác nó là biến thừa.
F22_DEM='
function xuat() { if (!in_ma && tep != "") print tep "\t" tho "\t" ma }
function la_ma() { ma++; if (in_ma) print tep ":" FNR ":" $0 }
FNR == 1 { xuat(); tep = (FILENAME == "") ? "-" : FILENAME; tho = 0; ma = 0; khoi = 0 }
{
  tho++
  t = $0; sub(/^[[:space:]]+/, "", t); sub(/[[:space:]]+$/, "", t)
  if (t == "") next
  if (khoi) {
    p = index(t, dg)
    if (p) { khoi = 0; sau = substr(t, p + length(dg)); sub(/^[[:space:]]+/, "", sau); if (sau != "") la_ma() }
    next
  }
  if (cm != "" && index(t, cm) == 1) next
  if (mo != "" && index(t, mo) == 1) {
    p = index(t, dg)
    if (!p) { khoi = 1; next }
    sau = substr(t, p + length(dg)); sub(/^[[:space:]]+/, "", sau)
    if (sau != "") la_ma()
    next
  }
  la_ma()
}
END { xuat() }'

# CÚ PHÁP CHÚ THÍCH THEO ĐUÔI — đọc LÚC CHẠY từ gạch đầu dòng "Cú pháp theo đuôi tệp" trong mục §5
# của bảng chủ; câu chữ gạch đó là đầu vào của cổng, không có bản chép nào trong script. Mỗi đoạn
# "`*.đuôi` — …" cho một dòng `đuôi<TAB>dấu dòng<TAB>dấu mở khối<TAB>dấu đóng khối`: dấu dòng là cụm
# trong backtick ngay sau chữ "dòng", cặp khối là hai cụm backtick ngay sau chữ "khối" — "không có chú
# thích dòng" không có backtick theo sau nên ra rỗng. Chỉ đọc trong mục "Ngưỡng kích thước file".
CU_PHAP_5=""
if [ -f "$BANG" ]; then
  CU_PHAP_5=$(LC_ALL=C perl -ne '
    if (/^## /) { $f = /Ngưỡng kích thước file/ ? 1 : 0; next }
    next unless $f && /^\s*-\s+\*\*C\S*\s+ph\S*p\s+theo\s+\S+\s+t\S*p:\*\*(.*)$/;
    my $r = $1;
    for my $doan (split /(?=`\*\.[A-Za-z0-9]+`)/, $r) {
      next unless $doan =~ /^`\*\.([A-Za-z0-9]+)`/;
      my $duoi = $1;
      my $cm = $doan =~ /d\S*ng\s+`([^`]+)`/ ? $1 : "";
      my ($mo, $dg) = $doan =~ /kh\S*i\s+`([^`]+)`\s+\S+\s+`([^`]+)`/ ? ($1, $2) : ("", "");
      print "$duoi\t$cm\t$mo\t$dg\n";
    }' "$BANG")
fi
# cu_phap <đuôi> — đặt CM_DONG / CM_MO / CM_DG theo CU_PHAP_5; trả 1 nếu §5 không khai đuôi đó.
cu_phap() {
  local dong
  dong=$(printf '%s\n' "$CU_PHAP_5" | LC_ALL=C awk -F'\t' -v d="$1" '$1 == d { print; exit }')
  [ -n "$dong" ] || return 1
  CM_DONG=$(printf '%s' "$dong" | cut -f2); CM_MO=$(printf '%s' "$dong" | cut -f3); CM_DG=$(printf '%s' "$dong" | cut -f4)
}
if [ ! -f "$BANG" ]; then
  bad "không thấy bảng ngưỡng $BANG"
elif [ ! -d "$FE/src" ]; then
  bad "không có $FE/src để quét"
else
  # LC_ALL=C — tiêu đề mục và tên cột trong chương trình awk là chuỗi BYTE UTF-8 nguyên văn, so theo
  # byte mới đúng (cùng bẫy với F37).
  F22_BANG=$(LC_ALL=C awk -F'|' "$F22_AWK" "$BANG")
  F22_SO_LOAI=$(printf '%s' "$F22_BANG" | grep -c .)
  # Canary nội tại của bộ bóc (T6): mỗi quy tắc phân loại ở §5 một dòng, dựng tại chỗ, đọc bằng đúng
  # lời gọi cổng dùng. Bộ bóc chết (mọi dòng thành chú thích, hoặc không dòng nào) trông y hệt "không
  # tệp nào vượt".
  #   ts:   // a | /* | * b | */ | /* c */ f(); | f(); // d | x /* e | y */ | 'https://x'; | (trống) | \r
  #         -> 11 dòng thô, 5 dòng mã (dòng 5..9)
  #   html: <!-- | a | --> | <!-- b --> <p> | // c   -> 5 dòng thô, 2 dòng mã (dòng 4, 5)
  F22_CANARY_TS=$(printf '// a\n/*\n * b\n */\n/* c */ f();\nf(); // d\nx /* e\ny */\nconst u = \x27https://x\x27;\n\n  \r\n' \
    | LC_ALL=C awk -v BINMODE=3 -v cm='//' -v mo='/*' -v dg='*/' "$F22_DEM" | cut -f2,3 | tr '\t' ' ')
  F22_CANARY_HTML=$(printf '<!--\n  a\n-->\n<!-- b --> <p>\n// c\n' \
    | LC_ALL=C awk -v BINMODE=3 -v cm='' -v mo='<!--' -v dg='-->' "$F22_DEM" | cut -f2,3 | tr '\t' ' ')
  if [ "$F22_SO_LOAI" -eq 0 ]; then
    bad "không đọc được dòng ngưỡng nào từ $BANG §5 (mất mục, mất cột 'Loại file'/'Ngưỡng cứng', hay bảng rỗng) — nguồn đọc hỏng, không phải trạng thái hợp lệ"
  elif [ -z "$CU_PHAP_5" ]; then
    bad "không đọc được gạch 'Cú pháp theo đuôi tệp' ở $BANG §5 — không có cú pháp chú thích nào để phân loại dòng; cổng không đoán"
  elif [ "$F22_CANARY_TS" != "11 5" ] || [ "$F22_CANARY_HTML" != "5 2" ]; then
    bad "F22 canary bộ bóc hỏng — đoạn dựng sẵn đọc ra ts='$F22_CANARY_TS' (kỳ vọng '11 5'), html='$F22_CANARY_HTML' (kỳ vọng '5 2'); bộ bóc không còn theo §5"
  else
    F22_LOI=""; F22_TOM=""; F22_GHI=""
    while IFS=$'\t' read -r MAU MUC; do
      [ -n "$MAU" ] || continue
      case "$MAU" in
        */*) GOC="$FE/src/${MAU%/*}"; TEN="${MAU##*/}" ;;
        *)   GOC="$FE/src/app";       TEN="$MAU" ;;
      esac
      case "$MUC" in
        ''|*[!0-9]*)
          F22_LOI="${F22_LOI}$MAU: cột 'Ngưỡng cứng' không phải số dạng **N** — cổng không đoán thay bảng"$'\n'
          continue ;;
      esac
      case "$TEN" in
        '*.'?*) ;;
        *)
          F22_LOI="${F22_LOI}$MAU: không dịch được thành mẫu tên tệp (cần '*.ext' hoặc 'thư-mục/*.ext')"$'\n'
          continue ;;
      esac
      # Cú pháp chú thích của đuôi này — đọc từ §5 lúc chạy (CU_PHAP_5). §5 không khai đuôi: FAIL.
      DUOI="${TEN##*.}"
      if ! cu_phap "$DUOI"; then
        F22_LOI="${F22_LOI}$MAU: đuôi '.$DUOI' chưa có cú pháp chú thích ở $BANG §5 — không phân loại được dòng, cổng không đoán"$'\n'
        continue
      fi
      if [ ! -d "$GOC" ]; then
        F22_LOI="${F22_LOI}$MAU: không có thư mục $GOC để quét"$'\n'
        continue
      fi
      SO_TEP=$(find "$GOC" -type f -name "$TEN" -not -name '*.spec.ts' | wc -l | tr -d ' ')
      if [ "$SO_TEP" -eq 0 ]; then
        F22_LOI="${F22_LOI}$MAU: không tệp nào khớp dưới $GOC — section sẽ xanh rỗng cho loại này"$'\n'
        continue
      fi
      DEM=$(find "$GOC" -type f -name "$TEN" -not -name '*.spec.ts' -print0 \
        | LC_ALL=C xargs -0 -r awk -v BINMODE=3 -v cm="$CM_DONG" -v mo="$CM_MO" -v dg="$CM_DG" "$F22_DEM")
      VUOT=$(printf '%s\n' "$DEM" | LC_ALL=C awk -F'\t' -v n="$MUC" \
        'NF == 3 && $3 + 0 > n + 0 { print $1 ": " $3 " dòng mã (thô " $2 ")" }' | sort)
      GHI=$(printf '%s\n' "$DEM" | LC_ALL=C awk -F'\t' -v n="$MUC" \
        'NF == 3 && $3 + 0 <= n + 0 && $2 + 0 > n + 0 { print $1 ": thô " $2 " > " n ", mã " $3 }' | sort)
      [ -n "$GHI" ] && F22_GHI="${F22_GHI}$GHI"$'\n'
      if [ -n "$VUOT" ]; then
        F22_LOI="${F22_LOI}$MAU vượt ngưỡng cứng $MUC dòng mã:"$'\n'"$VUOT"$'\n'
      else
        F22_TOM="$F22_TOM $MAU<=$MUC($SO_TEP tệp)"
      fi
    done <<< "$F22_BANG"
    if [ -n "$F22_LOI" ]; then
      bad "ngưỡng cứng ở $BANG §5 (đếm dòng mã):"; printf '%s' "$F22_LOI"
    else
      ok "không tệp nào vượt ngưỡng cứng tính bằng dòng mã — $F22_SO_LOAI loại đọc từ bảng chủ:$F22_TOM"
    fi
    if [ -n "$F22_GHI" ]; then
      note "dòng thô vượt ngưỡng cứng nhưng dòng mã không vượt — không phải vi phạm (ADR-0081), phần chênh là chú thích và dòng trống:"
      printf '%s' "$F22_GHI" | sed 's/^/    /'
    fi
  fi
fi

# ================================================================ F25
section "F25 - mọi màn danh sách Core trong platform/ đọc CORE_SCREEN_EXT"
# "Màn danh sách" nhận diện bằng dấu hiệu máy đọc được: *.page.ts dưới platform/ dùng
# ListStateStore (trạng thái màn danh sách — fe-architecture.md §2.8). Mỗi tệp như vậy phải có
# `inject(CORE_SCREEN_EXT` (§2.7 luật 5). So trên nội dung đã bỏ khoảng trắng và xuống dòng để
# một lần prettier bẻ dòng không làm cổng đỏ giả. 0 màn danh sách là ĐỎ, không phải xanh rỗng:
# repo Core luôn có màn danh sách, nên 0 nghĩa là dấu hiệu nhận diện đã mục (05-gate.md §8.2).
PLATFORM="$FE/src/app/platform"
if [ ! -d "$PLATFORM" ]; then
  bad "không có $PLATFORM để quét"
else
  LIST_PAGES=$(find "$PLATFORM" -name '*.page.ts' -not -name '*.spec.ts' -exec grep -l 'ListStateStore' {} + 2>/dev/null)
  if [ -z "$LIST_PAGES" ]; then
    bad "không tìm thấy *.page.ts nào dưới $PLATFORM dùng ListStateStore — dấu hiệu nhận diện màn danh sách đã mục?"
  else
    MISSING=""
    while IFS= read -r f; do
      tr -d ' \t\r\n' < "$f" | grep -q 'inject(CORE_SCREEN_EXT' || MISSING="$MISSING$f"$'\n'
    done <<< "$LIST_PAGES"
    if [ -n "$MISSING" ]; then
      bad "màn danh sách Core không đọc CORE_SCREEN_EXT (fe-architecture.md §2.7 luật 5):"
      printf '%s' "$MISSING"
    else
      ok "mọi màn danh sách Core dưới platform/ đọc CORE_SCREEN_EXT ($(printf '%s\n' "$LIST_PAGES" | wc -l | tr -d ' ') tệp)"
    fi
  fi
fi

# ================================================================ F27
section "F27 - platform/shell dựng Sidebar/Topbar qua component dumb, không viết tay <nav>/<header>"
SHELL_HTML="$FE/src/app/platform/shell/shell.component.html"
if [ ! -f "$SHELL_HTML" ]; then
  bad "không có $SHELL_HTML để quét"
else
  # [^>]* cho phép attribute khác đứng trước class (vd [attr.aria-label]) — vẫn cùng ý nghĩa với
  # câu chữ ở RULES.md §10: <nav class="sidebar"> / <header class="topbar"> dựng tay.
  HITS=$(grep -nE '<nav[^>]*class="sidebar"|<header[^>]*class="topbar"' "$SHELL_HTML")
  if [ -n "$HITS" ]; then
    bad "shell.component.html còn dựng tay Sidebar/Topbar bằng <nav>/<header> (RULES.md §10 F27, adr/0037):"
    printf '%s\n' "$HITS"
  elif ! grep -q '<app-sidebar' "$SHELL_HTML" || ! grep -q '<app-topbar' "$SHELL_HTML"; then
    bad "shell.component.html không gọi đủ <app-sidebar>/<app-topbar> — component dumb chưa được dùng"
  else
    ok "shell.component.html dùng <app-sidebar>/<app-topbar>, không còn markup tay"
  fi
fi

# ================================================================ F29
section "F29 - prime-preset.ts ghép đủ sub-preset của component PrimeNG trực tiếp, không vượt bao đóng của gói, không Aura gộp"
# ADR-0056. Lệnh gốc và hai bảng: 05-gate.md §8.12. Ba tập:
#   có        = sub-preset mà prime-preset.ts import;
#   bắt buộc  = base + sub-preset của mọi primeng/<x> import trực tiếp trong src/ (trừ *.spec.ts);
#   trần      = base + sub-preset của mọi module trong bao đóng đọc từ primeng-<x>.mjs của gói đã cài.
# Vế "component con thật sự dựng ra" nằm giữa sàn và trần, chỉ prime-preset.spec.ts canh.
# Biến F29_NODE_MODULES cho canary trỏ vào gói giả hoặc gói thật (mặc định $FE/node_modules).
# Canary: scripts/tests/fe-gate-f29.test.sh.
BANG=docs/wiki-core/fe/trien-khai/05-gate.md
NM="${F29_NODE_MODULES:-$FE/node_modules}"
PKG="$NM/primeng/fesm2022"
AURA="$NM/@primeuix/themes/dist/aura"
PRESET="$FE/src/app/core/theme/prime-preset.ts"
DOI_TEN=$(awk '/Tên module PrimeNG khác tên sub-preset/ { f = 1; next } /^#/ { f = 0 }
               f && /^\| `/' "$BANG" 2>/dev/null | awk -F'`' '{ print $2 "=" $4 }')
KHONG_PRESET=$(awk '/Module PrimeNG không có sub-preset/ { f = 1; next } /^#/ { f = 0 }
                    f && /^\| `/' "$BANG" 2>/dev/null | cut -d'`' -f2)
ten_preset() { printf '%s\n' "$DOI_TEN" | awk -F= -v m="$1" '$1 == m { print $2; d = 1 } END { if (!d) print m }'; }
khong_preset() { printf '%s\n' "$KHONG_PRESET" | grep -qx "$1"; }
if [ -z "$DOI_TEN" ] || [ -z "$KHONG_PRESET" ]; then
  bad "không đọc được hai bảng F29 từ $BANG — tên module khác tên sub-preset / module không có sub-preset"
elif [ ! -d "$PKG" ] || [ ! -d "$AURA" ]; then
  bad "không đọc được gói đã cài ($PKG, $AURA) — chạy npm ci trong $FE; cổng không xanh khi không có gì để đọc"
elif [ ! -f "$PRESET" ]; then
  bad "không có $PRESET"
else
  TRUC_TIEP=$(grep -rhoE "from '(primeng/[a-z0-9-]+)'" "$FE/src" --include='*.ts' --exclude='*.spec.ts' \
    | sed -E "s#from 'primeng/(.*)'#\1#" | sort -u)
  CO=$(grep -oE "from '@primeuix/themes/aura/[a-z0-9-]+'" "$PRESET" | sed -E "s#.*/aura/(.*)'#\1#" | sort -u)
  GOP=$(grep -rnE "['\"]@primeuix/themes/[a-z0-9-]+['\"]" "$FE/src" --include='*.ts')
  LOI=""
  if [ -z "$TRUC_TIEP" ]; then
    LOI="${LOI}không có import primeng/ trực tiếp nào trong $FE/src — tập bắt buộc rỗng"$'\n'
  fi
  if [ -z "$CO" ]; then
    LOI="${LOI}$PRESET không import sub-preset @primeuix/themes/aura/<x> nào — tập có rỗng"$'\n'
  fi
  if [ -n "$GOP" ]; then
    LOI="${LOI}import preset gộp (ADR-0038, ADR-0056 quyết định 3):"$'\n'"$GOP"$'\n'
  fi
  BAT_BUOC=base
  for m in $TRUC_TIEP; do
    khong_preset "$m" && continue
    p=$(ten_preset "$m")
    if [ ! -d "$AURA/$p" ]; then
      LOI="${LOI}module lạ primeng/$m: không có sub-preset '$p' và không nằm trong bảng không-có-sub-preset — thêm một dòng kèm căn cứ vào 05-gate.md §8.12"$'\n'
      continue
    fi
    [ -f "$PKG/primeng-$m.mjs" ] \
      || LOI="${LOI}không đọc được $PKG/primeng-$m.mjs — cấu trúc gói đổi? (ADR-0056, cái giá thứ hai)"$'\n'
    BAT_BUOC="$BAT_BUOC $p"
  done
  TRAN=base; DA=""; HANG="$TRUC_TIEP"
  while :; do
    # Hàng đợi có thể chỉ còn khoảng trắng (module lá không import primeng/ nào) — `set -u` làm
    # `$1` chết nếu không kiểm số đối số trước.
    set -- $HANG
    [ "$#" -eq 0 ] && break
    m=$1; shift; HANG="$*"
    case " $DA " in *" $m "*) continue ;; esac
    DA="$DA $m"
    if ! khong_preset "$m"; then
      p=$(ten_preset "$m")
      [ -d "$AURA/$p" ] && TRAN="$TRAN $p"
    fi
    [ -f "$PKG/primeng-$m.mjs" ] || continue
    HANG="$HANG $(grep -ohE "from 'primeng/[a-z0-9-]+'" "$PKG/primeng-$m.mjs" \
      | sed -E "s#from 'primeng/(.*)'#\1#" | sort -u | tr '\n' ' ')"
  done
  if [ -n "$CO" ]; then
    THIEU=$(comm -23 <(printf '%s\n' $BAT_BUOC | sort -u) <(printf '%s\n' "$CO"))
    DU=$(comm -13 <(printf '%s\n' $TRAN | sort -u) <(printf '%s\n' "$CO"))
    [ -n "$THIEU" ] && LOI="${LOI}thiếu sub-preset của component import trực tiếp: $(printf '%s' "$THIEU" | tr '\n' ' ')"$'\n'
    [ -n "$DU" ] && LOI="${LOI}sub-preset nằm ngoài bao đóng phụ thuộc của gói (rác): $(printf '%s' "$DU" | tr '\n' ' ')"$'\n'
  fi
  if [ -n "$LOI" ]; then
    bad "prime-preset.ts lệch luật F29:"; printf '%s' "$LOI"
  else
    ok "prime-preset.ts đủ sàn, không vượt trần ($(printf '%s\n' "$CO" | wc -l | tr -d ' ') sub-preset; bắt buộc: $(printf '%s\n' $BAT_BUOC | sort -u | wc -l | tr -d ' '), trần: $(printf '%s\n' $TRAN | sort -u | wc -l | tr -d ' '))"
  fi
fi

# ================================================================ F36
section "F36 - LOCALE_ID và registerLocaleData chỉ nằm ở tệp cấp seam core/config/core-i18n.ts"
# ADR-0063. Hai chuỗi `LOCALE_ID` và `registerLocaleData` trong $FE/src/app/**/*.ts (TRỪ *.spec.ts)
# chỉ được khớp ở tệp seam. Quét trên VĂN BẢN, nên một chú thích nhắc tên hai chuỗi này ở tệp khác
# cũng đỏ — có chủ đích: người đọc tệp đó phải đi tới seam, không đọc một bản kể lại.
#
# ĐỎ theo HAI chiều, và chiều thứ hai mới là chiều dễ mục:
#   (a) khớp ở ngoài seam — ai đó "sửa lại cho đúng thói quen Angular" ở app.config.ts;
#   (b) một trong hai chuỗi KHÔNG khớp ở đâu cả — seam đã thôi cấp mã locale hoặc thôi đăng ký dữ
#       liệu locale. Lúc đó pipe date/number im lặng rơi về khuôn Anh-Mỹ và không gì báo; cổng chỉ
#       đếm vi phạm sẽ xanh rỗng đúng lúc luật đã biến mất (05-gate.md §8.2).
# Canary: scripts/tests/fe-gate-f36.test.sh.
SEAM_I18N="$FE/src/app/core/config/core-i18n.ts"
if [ ! -d "$FE/src/app" ]; then
  bad "không có $FE/src/app để quét"
elif [ ! -f "$SEAM_I18N" ]; then
  bad "không có $SEAM_I18N — tệp cấp seam CORE_I18N là nơi DUY NHẤT được giữ hai chuỗi của F36"
else
  SO_TEP=$(find "$FE/src/app" -name '*.ts' -not -name '*.spec.ts' | grep -c .)
  if [ "$SO_TEP" -eq 0 ]; then
    bad "không có tệp *.ts nào trong $FE/src/app để quét — cổng sẽ xanh rỗng"
  else
    LOI=""
    for CHUOI in LOCALE_ID registerLocaleData; do
      HITS=$(find "$FE/src/app" -name '*.ts' -not -name '*.spec.ts' -print0 \
        | xargs -0 -r grep -nE "\b$CHUOI\b")
      # So tiền tố bằng awk, không bằng regex: đường dẫn của $FE mang '.' và '/' — đưa nguyên vào
      # mẫu grep là một bộ lọc lỏng hơn nó trông.
      NGOAI=$(printf '%s\n' "$HITS" | awk -v s="$SEAM_I18N:" 'length($0) && index($0, s) != 1')
      TRONG=$(printf '%s\n' "$HITS" | awk -v s="$SEAM_I18N:" 'index($0, s) == 1' | grep -c .)
      if [ -n "$NGOAI" ]; then
        LOI="${LOI}$CHUOI xuất hiện NGOÀI tệp cấp seam:"$'\n'"$NGOAI"$'\n'
      fi
      if [ "$TRONG" -eq 0 ]; then
        LOI="${LOI}$CHUOI KHÔNG còn xuất hiện trong $SEAM_I18N — seam đã thôi làm việc của nó"$'\n'
      fi
    done
    if [ -n "$LOI" ]; then
      bad "lệch luật F36 (ADR-0063):"; printf '%s' "$LOI"
    else
      ok "LOCALE_ID và registerLocaleData chỉ nằm ở $SEAM_I18N ($SO_TEP tệp đã quét)"
    fi
  fi
fi

# ================================================================ F37
section "F37 - hằng số thời lượng nhóm B nêu đích danh token --dur-* VÀ khớp giá trị của nó"
# ADR-0073 quyết định 4; luật F37 ở docs/RULES.md §7.
#
# VÌ SAO CON SỐ SỐNG Ở HAI NƠI. `transitionOptions` / `showTransitionOptions` của PrimeNG nhận một
# CHUỖI tham số hoạt ảnh Angular, và chuỗi đó bị phân tích thành số — nó KHÔNG giải `var()`. Token
# đặt được *giá trị* nhưng không *đi tới* được, nên giá trị buộc phải có một bản thứ hai trong
# TypeScript. Luật không xoá được bản thứ hai; nó bắt bản thứ hai TRUY ĐƯỢC về bản gốc.
#
# HAI NỬA, và nửa thứ hai mới là nửa quan trọng:
#   (1) TRUY NGƯỢC — mỗi hằng `export const *_MS = <số>` dưới shared/ui/ phải có một chuỗi `--dur-`
#       trong khối chú thích LIỀN TRÊN nó;
#   (2) ĐỐI CHIẾU GIÁ TRỊ — đọc giá trị của chính token được nêu từ _tokens.scss rồi SO SỐ.
# Chỉ nửa (2) bắt được ca HAI NƠI TRÔI KHỎI NHAU: chú thích vẫn nói `--dur-base`, token đã đổi
# 200ms -> 240ms, hằng số vẫn 200, và nửa (1) xanh. Đó là chính xác lớp lỗi F37 sinh ra để chặn.
#
# NGOẠI LỆ CÓ TÊN: TOOLTIP_DELAY_CHUOT_MS là THAM SỐ HÀNH VI (độ trễ trước khi hộp nổi hiện), không
# phải thời lượng một chuyển tiếp, nên nó không phản chiếu token nào. Ngoại lệ khai ngay đây, mỗi
# mục phải trỏ vào một hằng CÓ THẬT và chú thích của hằng đó phải tự nói ra điều đó — một ngoại lệ
# không ai kiểm lại là một ngoại lệ mục ruỗng.
#
# Canary: scripts/tests/fe-gate-f37.test.sh, cộng ba canary NỘI TẠI dưới đây (bộ đọc hằng, bộ đọc
# token, phép so giá trị) — điều kiện PASS của mục này thuần phủ định nên một bộ đọc chết trông y
# hệt "không có vi phạm" (05-gate.md §8.2).
F37_TOKENS="$FE/src/styles/_tokens.scss"
F37_UI="$FE/src/app/shared/ui"
F37_NGOAI_LE="TOOLTIP_DELAY_CHUOT_MS"

# Bộ đọc hằng: in `tệp|dòng|TÊN|giá trị|token nêu trong chú thích|có-cụm-"tham số hành vi"|chú thích liền trên`.
# LC_ALL=C — mẫu tiếng Việt ở đây là chuỗi BYTE UTF-8 nguyên văn, so theo byte mới đúng (cùng bẫy
# locale mà .claude/check-docs.sh §27 đã ghi).
F37_AWK='
  FNR == 1 { inblk = 0; blk = ""; blkend = -1 }
  /^[[:space:]]*\/\*\*/ { inblk = 1; blk = "" }
  inblk { blk = blk " " $0 }
  inblk && /\*\// { inblk = 0; blkend = FNR; next }
  /^export const [A-Za-z0-9_]+_MS[[:space:]]*=[[:space:]]*[0-9]+[[:space:]]*;/ {
    ten = $3
    gia = $5; gsub(/[^0-9]/, "", gia)
    tok = ""
    if (match(blk, /--dur-[a-z]+/)) tok = substr(blk, RSTART, RLENGTH)
    hv = index(blk, "tham số hành vi") ? 1 : 0
    ke = (blkend == FNR - 1) ? 1 : 0
    print FILENAME "|" FNR "|" ten "|" gia "|" tok "|" hv "|" ke
  }'

if [ ! -f "$F37_TOKENS" ]; then
  bad "không có $F37_TOKENS — không đọc được giá trị token nào để đối chiếu; nửa (2) của F37 sẽ xanh rỗng"
elif [ ! -d "$F37_UI" ]; then
  bad "không có $F37_UI để quét"
else
  # --- bảng token --dur-*, đọc từ tệp chủ, KHÔNG hằng hoá trong cổng
  F37_DUR=$(LC_ALL=C awk -F'[:;]' '/^[[:space:]]*--dur-[a-z]+:/ {
      gsub(/[[:space:]]/, "", $1); gsub(/[^0-9]/, "", $2); print $1 "=" $2 }' "$F37_TOKENS")
  F37_SO_TOKEN=$(printf '%s\n' "$F37_DUR" | grep -c .)

  F37_HANG=$(find "$F37_UI" -name '*.ts' -not -name '*.spec.ts' -print0 \
    | LC_ALL=C xargs -0 -r awk "$F37_AWK")
  F37_SO_HANG=$(printf '%s\n' "$F37_HANG" | grep -c .)

  # --- canary nội tại 1: bộ đọc hằng còn sống (dò trên một đoạn dựng tại chỗ)
  F37_CANARY=$(printf '/**\n * phản chiếu token **`--dur-fast`** (`120ms`).\n */\nexport const ZZ_CANARY_MS = 120;\n' \
    | LC_ALL=C awk "$F37_AWK" | cut -d'|' -f3-7)
  # --- canary nội tại 2: hằng KHÔNG nêu token phải ra token rỗng
  F37_CANARY2=$(printf '/**\n * Khong neu token nao.\n */\nexport const ZZ_CANARY_MS = 120;\n' \
    | LC_ALL=C awk "$F37_AWK" | cut -d'|' -f5)

  F37_LOI=""
  if [ "$F37_SO_TOKEN" -eq 0 ]; then
    bad "F37 đọc được 0 token --dur-* từ $F37_TOKENS — bộ đọc token hỏng, nửa đối chiếu giá trị đang không kiểm gì"
  elif [ "$F37_SO_HANG" -eq 0 ]; then
    bad "F37 đọc được 0 hằng *_MS dưới $F37_UI — bộ đọc hằng hỏng hoặc hằng đã dời chỗ; mục này đang không kiểm gì"
  elif [ "$F37_CANARY" != "ZZ_CANARY_MS|120|--dur-fast|0|1" ]; then
    bad "F37 canary bộ đọc hỏng — không bóc được cặp (hằng, token) từ một đoạn dựng sẵn; đọc ra: '$F37_CANARY'"
  elif [ -n "$F37_CANARY2" ]; then
    bad "F37 canary nửa truy ngược hỏng — chú thích KHÔNG nêu token nào mà bộ đọc vẫn trả về '$F37_CANARY2'"
  else
    # --- ngoại lệ có tên phải trỏ vào một hằng CÓ THẬT (chống mục ruỗng)
    for NL in $F37_NGOAI_LE; do
      case "$F37_HANG" in
        *"|$NL|"*) ;;
        *) F37_LOI="${F37_LOI}ngoại lệ có tên '$NL' không trỏ vào hằng nào còn tồn tại dưới $F37_UI — gỡ mục đi"$'\n' ;;
      esac
    done

    while IFS='|' read -r TEP DONG TEN GIA TOK HV KE; do
      [ -z "$TEN" ] && continue
      if [ "$KE" -ne 1 ]; then
        F37_LOI="${F37_LOI}$TEP:$DONG — $TEN không có khối chú thích LIỀN TRÊN; F37 đọc chú thích ngay trên khai báo"$'\n'
        continue
      fi
      case " $F37_NGOAI_LE " in
        *" $TEN "*)
          # Ngoại lệ phải tự nói ra vì sao nó là ngoại lệ — nếu không, nó chỉ là một hằng quên token.
          if [ "$HV" -ne 1 ]; then
            F37_LOI="${F37_LOI}$TEP:$DONG — $TEN nằm trong ngoại lệ có tên nhưng chú thích KHÔNG nói nó là 'tham số hành vi'"$'\n'
          fi
          continue ;;
      esac
      if [ -z "$TOK" ]; then
        F37_LOI="${F37_LOI}$TEP:$DONG — $TEN không nêu token --dur-* nào trong chú thích liền trên (luật F37: nêu ĐÍCH DANH token nó phản chiếu)"$'\n'
        continue
      fi
      GIA_TOKEN=""
      case "$F37_DUR" in
        *"$TOK="*) GIA_TOKEN=$(printf '%s\n' "$F37_DUR" | awk -F= -v t="$TOK" '$1 == t { print $2 }') ;;
      esac
      if [ -z "$GIA_TOKEN" ]; then
        F37_LOI="${F37_LOI}$TEP:$DONG — $TEN nêu token '$TOK' nhưng token đó KHÔNG có trong $F37_TOKENS"$'\n'
      elif [ "$GIA_TOKEN" != "$GIA" ]; then
        F37_LOI="${F37_LOI}$TEP:$DONG — $TEN = $GIA nhưng '$TOK' = ${GIA_TOKEN}ms. Hai nơi đã trôi khỏi nhau — sửa con số trong TypeScript, đừng sửa token cho khớp"$'\n'
      fi
    done <<< "$F37_HANG"

    if [ -n "$F37_LOI" ]; then
      bad "lệch luật F37 (ADR-0073):"; printf '%s' "$F37_LOI"
    else
      ok "$F37_SO_HANG hằng *_MS truy về token và khớp giá trị ($F37_SO_TOKEN token --dur-* đã đọc)"
    fi
  fi
fi

# ================================================================ F38
section "F38 - index.html không mang tên sản phẩm (name, shortName đọc từ provideCoreBranding( của app.config.ts)"
# Luật F38 ở docs/RULES.md §7; quy-uoc/fe-routing-guard.md §2.2.
#
# index.html render TRƯỚC khi có injector nên seam CORE_BRANDING không với tới nó, mà tệp lại thuộc
# Core — dự án hạ nguồn không sửa. Chuỗi cấm KHÔNG viết cứng ở đây: nó đọc từ lời gọi
# `provideCoreBranding(` trong composition root của chính dự án đang chạy cổng, nên cổng đi theo
# dự án hạ nguồn mà không phải sửa.
#
# KHỚP NGUYÊN TỪ (`grep -w`), KHÔNG khớp chuỗi con. `shortName` là một mã tắt hai ba chữ cái; khớp
# chuỗi con thì `CS` khớp `CSP` trong chú thích của chính index.html và cổng đỏ trên mã đúng. Cái
# giá: dạng dính liền (`CoreAndSkillApp`) và dạng đổi hoa thường không bị bắt — canary nêu rõ ranh
# giới đó thành ca.
#
# Chú thích // và /* */ của app.config.ts bị bỏ TRƯỚC khi đọc lời gọi: một lời gọi cũ để lại trong
# chú thích sẽ cho cổng một bộ giá trị đã chết để dò, trong khi lời gọi thật không đọc được.
#
# Chốt chống xanh rỗng, mọi chốt là FAIL: thiếu index.html · thiếu app.config.ts · không thấy lời gọi
# `provideCoreBranding(` · không đọc được giá trị literal của `name` HOẶC của `shortName` (đọc được
# một nửa là dò một nửa) · canary nội tại của bộ đọc và của phép khớp. Canary ngoài:
# scripts/tests/fe-gate-f38.test.sh.
F38_CAU_HINH="$FE/src/app/app.config.ts"
F38_INDEX="$FE/src/index.html"

# Bộ đọc: in một dòng `#goi` cho mỗi lời gọi, rồi `khoá<TAB>giá trị` cho mỗi `name`/`shortName`
# literal trong đối số object của lời gọi đó. Nháy viết bằng \x22 \x27 \x60 để khỏi thoát trong shell.
F38_PERL='
  my $chuoi = qr/\x22(?:\\.|[^\x22\\])*\x22|\x27(?:\\.|[^\x27\\])*\x27|\x60(?:\\.|[^\x60\\])*\x60/s;
  s{($chuoi)|//[^\n]*|/\*.*?\*/}{defined $1 ? $1 : " "}gse;
  while (/\bprovideCoreBranding\s*\(\s*(\{(?:$chuoi|[^{}\x22\x27\x60])*\})?/g) {
    print "#goi\n";
    my $obj = defined $1 ? $1 : "";
    while ($obj =~ /(?<![\w\$])(name|shortName)\s*:\s*($chuoi)/g) {
      my ($khoa, $gia) = ($1, substr($2, 1, -1));
      $gia =~ s/\\(.)/$1/gs;
      print "$khoa\t$gia\n";
    }
  }'

if [ ! -f "$F38_INDEX" ]; then
  bad "không có $F38_INDEX — không có gì để dò; F38 sẽ xanh rỗng"
elif [ ! -f "$F38_CAU_HINH" ]; then
  bad "không có $F38_CAU_HINH — không đọc được tên sản phẩm của dự án; F38 sẽ xanh rỗng"
else
  F38_DOC=$(LC_ALL=C perl -0777 -ne "$F38_PERL" "$F38_CAU_HINH")
  F38_SO_GOI=$(printf '%s\n' "$F38_DOC" | grep -c '^#goi$')

  # --- canary nội tại 1: bộ đọc còn sống — dạng prettier bẻ dòng, nháy kép, đảo thứ tự khoá, và
  # một lời gọi giả nằm trong chú thích phải bị bỏ qua.
  F38_CANARY=$(printf '// provideCoreBranding({ name: \x27CHET\x27, shortName: \x27CH\x27 })\nprovideCoreBranding({\n  shortName: "ZZ",\n  /* name: \x27SAI\x27, */\n  name: \x27Zz Canary\x27,\n}),\n' \
    | LC_ALL=C perl -0777 -ne "$F38_PERL" | tr '\t\n' '=;')
  # --- canary nội tại 2: phép khớp nguyên từ — khớp khi đứng riêng, KHÔNG khớp khi là chuỗi con.
  F38_KHOP_RIENG=$(printf '<title>ZZ \302\267 x</title>\n' | LC_ALL=C grep -cwF -- 'ZZ')
  F38_KHOP_CON=$(printf 'ZZP cho script\n' | LC_ALL=C grep -cwF -- 'ZZ')

  if [ "$F38_CANARY" != "#goi;shortName=ZZ;name=Zz Canary;" ]; then
    bad "F38 canary bộ đọc hỏng — không bóc được name/shortName từ một lời gọi dựng sẵn; đọc ra: '$F38_CANARY'"
  elif [ "$F38_KHOP_RIENG" != "1" ] || [ "$F38_KHOP_CON" != "0" ]; then
    bad "F38 canary phép khớp hỏng — grep -w không phân biệt từ riêng ($F38_KHOP_RIENG, kỳ vọng 1) với chuỗi con ($F38_KHOP_CON, kỳ vọng 0)"
  elif [ "$F38_SO_GOI" -eq 0 ]; then
    bad "không thấy lời gọi provideCoreBranding( nào (ngoài chú thích) trong $F38_CAU_HINH — không có tên sản phẩm để dò; F38 sẽ xanh rỗng"
  else
    F38_LOI=""
    F38_DA_DO=""
    for F38_KHOA in name shortName; do
      # Giá trị rỗng hoặc toàn khoảng trắng không phải một giá trị: grep -F với mẫu rỗng khớp mọi dòng.
      F38_GIA=$(printf '%s\n' "$F38_DOC" | awk -F'\t' -v k="$F38_KHOA" '$1 == k && $2 ~ /[^[:space:]]/ { print $2 }')
      if [ -z "$F38_GIA" ]; then
        F38_LOI="${F38_LOI}không đọc được giá trị literal của '$F38_KHOA' trong provideCoreBranding( ở $F38_CAU_HINH — dò một nửa là xanh rỗng một nửa"$'\n'
        continue
      fi
      while IFS= read -r F38_V; do
        [ -n "$F38_V" ] || continue
        F38_DA_DO="$F38_DA_DO $F38_KHOA='$F38_V'"
        F38_HIT=$(LC_ALL=C grep -nwF -- "$F38_V" "$F38_INDEX")
        if [ -n "$F38_HIT" ]; then
          F38_LOI="${F38_LOI}$F38_INDEX mang $F38_KHOA '$F38_V' của provideCoreBranding( — tệp của Core phải trung lập với mọi dự án:"$'\n'"$(printf '%s\n' "$F38_HIT" | sed 's/^/    dòng /')"$'\n'
        fi
      done <<< "$F38_GIA"
    done
    if [ -n "$F38_LOI" ]; then
      bad "lệch luật F38 (quy-uoc/fe-routing-guard.md §2.2):"; printf '%s' "$F38_LOI"
    else
      ok "$F38_INDEX không mang tên sản phẩm (đã dò:$F38_DA_DO; đọc từ $F38_SO_GOI lời gọi provideCoreBranding)"
    fi
  fi
fi

# ================================================================ F39
section "F39 - provider hoạt ảnh chỉ khai qua provideCoreAnimations() của core, không nạp điểm vào tĩnh của bộ máy hoạt ảnh"
# Luật F39 ở docs/RULES.md §7; ADR-0080. Trên $FE/src/app/**/*.ts trừ *.spec.ts, MỌI phép dò đi trên
# DÒNG MÃ — phân loại theo §5 của quy-uoc/fe-architecture.md, qua bộ bóc dùng chung F22_DEM (chế độ
# in_ma), cú pháp của đuôi `.ts` đọc từ §5 (cu_phap). Chú thích một mình không thoả, và không vi phạm.
#   (1) tệp mang `export function provideCoreAnimations(` nằm dưới $FE/src/app/core/ — tìm theo TÊN
#       HÀM, rồi mới kiểm chỗ; dời nó về composition root là phương án B ADR-0080 đã loại;
#   (2) `provideAnimationsAsync(` chỉ khớp ở tệp đó;
#   (3) không dòng mã nào nhắc ĐIỂM VÀO TĨNH `@angular/platform-browser/animations` đứng ngay trước dấu
#       nháy đóng (`…/animations/async` được phép). Dò theo điểm vào, không theo tên hàm: bí danh, import
#       cả không gian tên, dạng module (`NoopAnimationsModule`, `BrowserAnimationsModule`) đều bị bắt;
#   (4) dòng mã của app.config.ts có `provideCoreAnimations(`;
#   (5) dòng mã của tệp ở (1) có `(prefers-reduced-motion: reduce)` và `'noop'` — chỉ dò HÌNH DẠNG;
#       hành vi thân hàm do unit test cạnh tệp đó khoá, không do F39.
# Chốt chống xanh rỗng, mỗi cái là FAIL: không tìm ra tệp mang hàm xuất · tệp đó không khớp
# `provideAnimationsAsync(` · canary nội tại của bộ bóc hỏng (và: §5 không khai cú pháp `.ts`).
#
# Các đột biến đã đi qua mọi test ngày 2026-09-24 đều có ca canary bắt buộc:
# scripts/tests/fe-gate-f39.test.sh.
F39_APP="$FE/src/app"
F39_CORE="$FE/src/app/core/"
F39_CAU_HINH="$FE/src/app/app.config.ts"
F39_XUAT='export function provideCoreAnimations('
if [ ! -d "$F39_APP" ]; then
  bad "không có $F39_APP để quét"
elif [ ! -f "$F39_CAU_HINH" ]; then
  bad "không có $F39_CAU_HINH — không kiểm được composition root gọi provideCoreAnimations("
elif ! cu_phap ts; then
  bad "docs/quy-uoc/fe-architecture.md §5 không khai cú pháp chú thích cho '.ts' — không tách được dòng mã; F39 không đoán"
else
  # Dòng mã của mọi tệp trong phạm vi, dạng `tệp:số dòng:nội dung` (như grep -n).
  F39_MA=$(find "$F39_APP" -type f -name '*.ts' -not -name '*.spec.ts' -print0 \
    | LC_ALL=C xargs -0 -r awk -v in_ma=1 -v BINMODE=3 -v cm="$CM_DONG" -v mo="$CM_MO" -v dg="$CM_DG" "$F22_DEM")
  F39_SO_TEP=$(find "$F39_APP" -type f -name '*.ts' -not -name '*.spec.ts' | grep -c .)
  # Canary nội tại của chế độ in_ma (T6): dòng chỉ là chú thích không được in, dòng mã in kèm tệp và số dòng.
  F39_CANARY=$(printf '// (zz)\nf(); // yy\n/* ww */\n' \
    | LC_ALL=C awk -v in_ma=1 -v BINMODE=3 -v cm="$CM_DONG" -v mo="$CM_MO" -v dg="$CM_DG" "$F22_DEM" | tr '\n' '|')
  # f39_khop <chuỗi cố định> — các dòng của F39_MA mà PHẦN NỘI DUNG (sau `tệp:số:`) chứa chuỗi đó.
  f39_khop() { printf '%s\n' "$F39_MA" | LC_ALL=C awk -v s="$1" 'length($0) { t = $0; sub(/^[^:]*:[0-9]+:/, "", t); if (index(t, s)) print }'; }
  # f39_tep <dòng dạng tệp:số:nội dung>... (stdin) — tên tệp, mỗi tệp một lần
  f39_tep() { LC_ALL=C awk 'length($0) { print substr($0, 1, index($0, ":") - 1) }' | sort -u; }
  F39_TEP_XUAT=$(f39_khop "$F39_XUAT" | f39_tep)
  if [ "$F39_CANARY" != "-:2:f(); // yy|" ]; then
    bad "F39 canary bộ bóc dòng mã hỏng — đoạn dựng sẵn in ra '$F39_CANARY' (kỳ vọng '-:2:f(); // yy|'); F39 đang không dò gì"
  elif [ "$F39_SO_TEP" -eq 0 ]; then
    bad "không có tệp *.ts nào trong $F39_APP để quét — cổng sẽ xanh rỗng"
  elif [ -z "$F39_TEP_XUAT" ]; then
    bad "không tìm ra tệp nào dưới $F39_APP có dòng mã '$F39_XUAT' — không có hàm Core nào chọn bộ chạy; F39 sẽ xanh rỗng"
  else
    F39_LOI=""
    while IFS= read -r F39_TEP; do
      [ -n "$F39_TEP" ] || continue
      # (1) hàm xuất nằm dưới core/
      case "$F39_TEP" in
        "$F39_CORE"*) ;;
        *) F39_LOI="${F39_LOI}(1) $F39_TEP mang '$F39_XUAT' nhưng nằm NGOÀI $F39_CORE — thân hàm phải sống ở nơi Core canh (ADR-0080, phương án B đã loại)"$'\n' ;;
      esac
      F39_MA_TEP=$(printf '%s\n' "$F39_MA" | LC_ALL=C awk -v p="$F39_TEP:" 'index($0, p) == 1')
      # chốt: tệp đó phải chọn bộ chạy
      printf '%s\n' "$F39_MA_TEP" | LC_ALL=C grep -qF -- 'provideAnimationsAsync(' \
        || F39_LOI="${F39_LOI}$F39_TEP mang '$F39_XUAT' nhưng không có dòng mã nào khớp 'provideAnimationsAsync(' — hàm xuất không chọn bộ chạy nào; F39 sẽ xanh rỗng"$'\n'
      # (5) hình dạng nhánh giảm chuyển động, trên dòng mã
      printf '%s\n' "$F39_MA_TEP" | LC_ALL=C grep -qF -- '(prefers-reduced-motion: reduce)' \
        || F39_LOI="${F39_LOI}(5) $F39_TEP thiếu '(prefers-reduced-motion: reduce)' trên DÒNG MÃ — chú thích một mình không thoả"$'\n'
      printf '%s\n' "$F39_MA_TEP" | LC_ALL=C grep -qF -- "'noop'" \
        || F39_LOI="${F39_LOI}(5) $F39_TEP thiếu \"'noop'\" trên DÒNG MÃ — chú thích một mình không thoả"$'\n'
    done <<< "$F39_TEP_XUAT"
    # (2) provideAnimationsAsync( chỉ ở tệp mang hàm xuất
    F39_NGOAI=$(f39_khop 'provideAnimationsAsync(' | LC_ALL=C awk -v ds="$F39_TEP_XUAT" '
      BEGIN { n = split(ds, d, "\n") }
      { for (i = 1; i <= n; i++) if (index($0, d[i] ":") == 1) next; print }')
    [ -n "$F39_NGOAI" ] && F39_LOI="${F39_LOI}(2) provideAnimationsAsync( xuất hiện NGOÀI tệp mang '$F39_XUAT':"$'\n'"$F39_NGOAI"$'\n'
    # (3) điểm vào tĩnh — mẫu của dòng luật; `…/animations/async` không khớp vì sau `animations` là `/`
    F39_TINH=$(printf '%s\n' "$F39_MA" | LC_ALL=C awk 'length($0) { t = $0; sub(/^[^:]*:[0-9]+:/, "", t)
      if (t ~ /@angular\/platform-browser\/animations[\x27"]/) print }')
    [ -n "$F39_TINH" ] && F39_LOI="${F39_LOI}(3) dòng mã nhắc ĐIỂM VÀO TĨNH @angular/platform-browser/animations — kéo bộ máy hoạt ảnh vào bundle khởi động của mọi người dùng (bí danh, không gian tên, dạng module đều tính):"$'\n'"$F39_TINH"$'\n'
    # (4) composition root gọi hàm của Core, trên dòng mã
    printf '%s\n' "$F39_MA" | LC_ALL=C awk -v p="$F39_CAU_HINH:" 'index($0, p) == 1' | LC_ALL=C grep -qF -- 'provideCoreAnimations(' \
      || F39_LOI="${F39_LOI}(4) $F39_CAU_HINH không có 'provideCoreAnimations(' trên DÒNG MÃ — composition root không nối nửa provider giảm chuyển động (dòng nối trong chú thích không tính)"$'\n'
    if [ -n "$F39_LOI" ]; then
      bad "lệch luật F39 (ADR-0080):"; printf '%s' "$F39_LOI"
    else
      ok "provider hoạt ảnh chỉ ở $(printf '%s' "$F39_TEP_XUAT" | tr '\n' ' ')(tệp mang hàm xuất, dưới core/; $F39_SO_TEP tệp đã quét trên dòng mã), app.config.ts gọi provideCoreAnimations"
    fi
  fi
fi

# ================================================================ Tổng kết
printf '\n'
if [ "$FAIL" -eq 0 ]; then
  printf '\033[32m✔ fe-gate.sh PASS\033[0m\n'
  exit 0
else
  printf '\033[31m✘ fe-gate.sh FAIL — xem chi tiết ở trên\033[0m\n'
  exit 1
fi
