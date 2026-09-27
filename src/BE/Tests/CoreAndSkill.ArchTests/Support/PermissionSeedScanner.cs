using System.Text;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace CoreAndSkill.ArchTests.Support;

// Bóc các dòng `INSERT INTO <bảng> (...) VALUES (...)` ra khỏi văn bản SQL mà một migration EF Core
// phát ra, rồi so hai tập khoá — phần máy đọc được của luật B7 (docs/RULES.md §6).
//
// VÌ SAO PHẢI PHÂN TÍCH SQL CHỨ KHÔNG ĐỌC DB: nửa "kiểm lúc khởi động" của B7 do
// PermissionCatalogValidationHostedService làm, và nó chỉ nhìn thấy danh mục ĐÃ GỘP trong bộ nhớ —
// khoá trùng, sai khuôn, nhãn rỗng. Nó KHÔNG biết gì về dòng seed trong DB. Nửa còn lại phải đối
// chiếu hai nguồn đó với nhau, và phải chạy được ở CI KHÔNG có database (luật T9: chỉ test mang
// [Trait("Category", "RequiresDocker")] mới được đòi container). Văn bản SQL lấy từ chính
// `Migration.UpOperations` — tức đúng chuỗi migration sẽ chạy, không phải một bản chép tay.
//
// Bộ đọc cố ý đọc CẢ danh sách cột chứ không giả định thứ tự cột: đảo thứ tự cột trong migration là
// một thay đổi hợp lệ, và một bộ đọc neo theo vị trí sẽ im lặng so nhầm cột sau lần đảo đầu tiên.
internal static class PermissionSeedScanner
{
    // MỘT migration seed ra bảng này bằng HAI cách khác nhau, và bộ đọc nào chỉ biết một cách thì
    // mù nửa còn lại — mù theo CẢ HAI chiều, nên cả hai chiều đều nguy hiểm:
    //   • migrationBuilder.Sql("INSERT INTO ...")  → SqlOperation, mang văn bản SQL;
    //   • migrationBuilder.InsertData(...)          → InsertDataOperation, mang dữ liệu đã cấu trúc,
    //     KHÔNG có chuỗi SQL nào để phân tích — nhưng khi migration chạy thì nó vẫn là INSERT thật.
    // Bỏ sót dạng thứ hai thì: một khoá seed bằng InsertData mà C# không khai sẽ lọt (khoá mồ côi),
    // và một khoá C# khai + seed bằng InsertData sẽ bị báo đỏ SAI với thông điệp "migration KHÔNG có
    // dòng nào khớp" — sức ép lúc đó là đi nới bộ đọc, tức đi theo đúng hướng hỏng.
    //
    // Thao tác GHI DỮ LIỆU mà bộ đọc KHÔNG hiểu thì ném, không lặng lẽ trả rỗng: nằm ngoài tầm mà
    // im lặng chính là cách cổng này chết.
    //
    // UPDATE/DELETE/TRUNCATE viết bằng migrationBuilder.Sql đi qua ĐÚNG cửa này. Trước đây nhánh
    // SqlOperation chỉ gọi ParseInsertRows, nên một `migrationBuilder.Sql("DELETE FROM
    // core.permission WHERE …")` trả về tập INSERT rỗng và cả hai cổng B7 vẫn xanh — trong khi
    // migration đó xoá thật một khoá trên máy dev. Đo được: ReadRows(SqlOperation DELETE) trả rỗng
    // và KHÔNG ném. Migration seed duy nhất hiện có dùng đúng dạng `Sql(...)` chứ không dùng
    // InsertData, nên đó là đường đi TỰ NHIÊN nhất, không phải một ca hiếm.
    public static IReadOnlyList<IReadOnlyDictionary<string, string?>> ReadRows(
        MigrationOperation operation, string table, string? origin = null)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(table);

        switch (operation)
        {
            case SqlOperation sql:
                // Chỉ UpOperations đi qua đây (xem hai cổng gọi). Down() của migration seed CÓ
                // DELETE — đúng chính sách xuôi-chỉ — và nó nằm ngoài đường này một cách có chủ ý.
                AssertNoDataMutation(
                    sql.Sql, table, origin ?? "Văn bản SQL của migration (migrationBuilder.Sql)");

                return ParseInsertRows(sql.Sql, table);

            case InsertDataOperation insert when TargetsTable(insert.Schema, insert.Table, table):
                return ReadInsertData(insert, table);

            case UpdateDataOperation { Schema: var us, Table: var ut } when TargetsTable(us, ut, table):
            case DeleteDataOperation { Schema: var ds, Table: var dt } when TargetsTable(ds, dt, table):
                throw new InvalidOperationException(
                    $"Migration có thao tác '{operation.GetType().Name}' trên bảng '{table}' mà cổng B7 "
                  + "không đọc được. Cổng chỉ đối chiếu INSERT; một thao tác sửa hay xoá khoá làm phép "
                  + "so lệch mà không ai thấy. Mở rộng bộ đọc, KHÔNG bỏ qua thao tác này.");

            default:
                return [];
        }
    }

    // So TRỌN tên đã gắn schema, cùng luật với bộ đọc SQL: "core.permission" không khớp
    // "core.permission_resource". InsertData không khai schema thì coi như bảng không thuộc schema.
    private static bool TargetsTable(string? schema, string tableName, string table)
        => string.Equals(
            string.IsNullOrEmpty(schema) ? tableName : $"{schema}.{tableName}",
            table,
            StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<IReadOnlyDictionary<string, string?>> ReadInsertData(
        InsertDataOperation operation, string table)
    {
        var columns = operation.Columns;
        var values = operation.Values;

        if (values.GetLength(1) != columns.Length)
            throw new InvalidOperationException(
                $"InsertData trên bảng '{table}' có {values.GetLength(1)} giá trị mỗi dòng nhưng khai "
              + $"{columns.Length} cột — không đối chiếu được. Sửa migration, đừng nới bộ đọc.");

        var rows = new List<IReadOnlyDictionary<string, string?>>();

        for (var r = 0; r < values.GetLength(0); r++)
        {
            var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            for (var c = 0; c < columns.Length; c++)
                row[columns[c]] = values[r, c]?.ToString();

            rows.Add(row);
        }

        return rows;
    }

    // Bộ đọc chỉ hiểu INSERT. Một câu UPDATE hay DELETE trên cùng bảng làm tập khoá THẬT trong DB
    // lệch khỏi tập vừa được đối chiếu, mà phép so hai tập INSERT không nhìn thấy gì cả — cổng vẫn
    // xanh trong khi nó đã hết đúng. Phía migration, ReadRows ném khi gặp Update/DeleteDataOperation
    // VÀ khi gặp văn bản SQL mang câu mutation; đây là câu dùng chung cho cả hai phía.
    //
    // Quét HAI lớp, vì hai lớp hỏng theo hai kiểu ngược nhau:
    //   • phần MÃ (chú thích đã bị bỏ đi) — câu lệnh thật;
    //   • phần TRONG CHUỖI — `EXECUTE 'DELETE FROM core.permission'` là câu xoá thật với Postgres
    //     nhưng chỉ là dữ liệu với một bộ đọc văn bản tĩnh. Lớp này đã có từ trước một cách tình cờ
    //     (bộ tìm cũ quét văn bản thô); giữ lại tường minh để bản sửa chú thích KHÔNG thu hẹp cổng.
    // Chú thích thì KHÔNG thuộc lớp nào — nó không chạy. Xem TryFindStatement.
    //
    // MỖI DẠNG CÂU DƯỚI ĐÂY ĐÃ ĐO TRÊN ASSEMBLY ĐÃ BUILD, không suy từ việc đọc mã. Bản trước khai
    // bốn cụm từ khoá và đòi tên bảng đứng NGAY SAU cụm đó, nên sáu dạng ghi thật của Postgres đi
    // qua yên lặng: `DELETE FROM ONLY t`, `UPDATE ONLY t`, `TRUNCATE ONLY t`,
    // `TRUNCATE TABLE ONLY t`, `TRUNCATE a, t` (tên bảng nằm sau dấu phẩy) và
    // `MERGE INTO t … WHEN MATCHED THEN DELETE`. `ONLY` không phải cách viết hiếm — nó là cách viết
    // ĐÚNG khi người viết muốn chặn thừa kế bảng. Xem TryMatchTarget.
    //
    // Quét TRONG chuỗi thì phải KHOAN DUNG với chuỗi hỏng, và đây là chỗ hai lỗi kéo ngược chiều
    // nhau gặp nhau: nội dung một chuỗi đã gỡ thoát `''` → `'` có thể mang số nháy LẺ (giá trị
    // `'Ng''uoi'` cho nội dung `Ng'uoi`), và bộ đọc nghiêm ngặt ném "chuỗi không được đóng" cho một
    // tệp SQL HOÀN TOÀN HỢP LỆ — đo được: đỏ cả bốn test B7 phía script chỉ vì một dấu nháy ở bất
    // kỳ đâu trong tệp. Lối thoát tự nhiên của người sửa khi đó là đi nới chính hàm này, tức mở lại
    // đúng lỗ trên. Nên: lỗi cú pháp trong DỮ LIỆU không phải lỗi cú pháp của TỆP — quét chuỗi gặp
    // phần hỏng thì dừng và trả "không tìm thấy", còn quét ở mức TỆP vẫn ném như cũ.
    public static void AssertNoDataMutation(string sql, string table, string origin)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ArgumentException.ThrowIfNullOrWhiteSpace(table);

        // tableList: TRUNCATE nhận DANH SÁCH bảng ngăn bằng dấu phẩy, ba câu kia nhận đúng một tên.
        //
        // Tail: từ khoá BẮT BUỘC đứng sau tên bảng (và sau danh sách cột tuỳ chọn) để câu đó là câu
        // GHI. Chỉ COPY cần nó, và nó là thứ phân biệt `COPY t (c) FROM stdin` — nạp dữ liệu VÀO
        // bảng, một đường ghi không đi qua INSERT nào — với `COPY t TO stdout`, vốn chỉ đọc. Cho cả
        // hai cùng đỏ là báo đỏ SAI cho một câu xuất dữ liệu hợp lệ, tức đúng hình dạng hỏng mà
        // khối Detector_ về dấu nháy thoát vừa phải dọn; bỏ cả hai qua thì một đường ghi tầm thường
        // đi qua yên lặng.
        (string[] Keywords, bool TableList, string? Tail)[] shapes =
        [
            (["UPDATE"], false, null),
            (["DELETE", "FROM"], false, null),
            (["MERGE", "INTO"], false, null),
            (["TRUNCATE"], true, null),
            (["TRUNCATE", "TABLE"], true, null),
            (["COPY"], false, "FROM"),
        ];

        // DẠNG CHỈ KIỂM TRONG CHUỖI NHÁY ĐƠN, và ranh giới đó là cả nội dung của luật này.
        //
        // `INSERT` ở mức TỆP là thứ cổng ĐỌC ĐƯỢC — cho nó vào `shapes` là làm đỏ chính câu seed mà
        // cổng tồn tại để đối chiếu. Nhưng `EXECUTE 'INSERT INTO core.permission …'` thì không: với
        // Postgres đó là một dòng vào database thật, còn với ParseInsertRows thì nó là một chuỗi
        // đóng đúng — bước qua nguyên khối, đọc thêm 0 dòng. Hệ quả đo được: `Diff` — thứ canh
        // chiều "có dòng seed mà không nguồn nào khai" — không thấy gì, và một KHOÁ MỒ CÔI nằm
        // trong database mà không ai biết. Đó là đúng chiều hỏng của B7.
        //
        // KHÔNG áp cho thân nháy đô la, và đây không phải chỗ sót: `DO $EF$ … $EF$` là MÃ chạy được
        // chứ không phải dữ liệu, CollectInsertRows đọc thẳng vào thân đó, và seed thật của repo
        // này viết đúng ở dạng ấy. Cho nó đỏ là làm đỏ chính danh mục đang được canh. Hai lớp vì
        // vậy bù nhau đúng chỗ: thân nháy đô la thì ĐỌC ĐƯỢC nên đọc, chuỗi nháy đơn thì KHÔNG đọc
        // được nên ném.
        (string[] Keywords, bool TableList, string? Tail)[] literalOnlyShapes =
        [
            (["INSERT", "INTO"], false, null),
        ];

        var literals = StringLiterals(sql, table);

        foreach (var (keywords, tableList, tail) in shapes)
        {
            if (TryFindStatement(sql, table, 0, lenient: false, tableList, tail, out _, keywords))
                throw MutationFound(origin, keywords, tail, table, insideLiteral: false);

            foreach (var literal in literals)
            {
                if (TryFindStatement(literal.Content, table, 0, lenient: true, tableList, tail, out _, keywords))
                    throw MutationFound(origin, keywords, tail, table, insideLiteral: true);
            }
        }

        foreach (var (keywords, tableList, tail) in literalOnlyShapes)
        {
            foreach (var literal in literals.Where(l => !l.DollarQuoted))
            {
                if (TryFindStatement(literal.Content, table, 0, lenient: true, tableList, tail, out _, keywords))
                    throw MutationFound(origin, keywords, tail, table, insideLiteral: true);
            }
        }
    }

    // Thông điệp cho nhánh TRONG CHUỖI phải nói CẢ HAI khả năng. Bản trước chỉ dặn "mở rộng bộ
    // đọc", và với một chuỗi DỮ LIỆU tình cờ chứa văn bản SQL thì lời dặn đó đẩy người sửa đi đúng
    // hướng hỏng — lối thoát tự nhiên khi ấy là gỡ hẳn lớp quét trong chuỗi, tức mở lại lỗ SQL động
    // mà chính lớp này tồn tại để bịt. Xem khối RANH GIỚI ĐÃ BIẾT ở PermissionScriptParityTests.
    private static InvalidOperationException MutationFound(
        string origin, string[] shape, string? tail, string table, bool insideLiteral)
    {
        var advice = insideLiteral
            ? " — và câu đó nằm TRONG một chuỗi, tức SQL động. Cổng chỉ đối chiếu INSERT viết "
            + "tĩnh; một câu chạy qua EXECUTE ghi thật vào database trong khi phép so đọc thêm "
            + "0 dòng. Viết lại thành câu tĩnh, KHÔNG bỏ qua câu này. NẾU đây không phải SQL "
            + "động mà là DỮ LIỆU tình cờ chứa văn bản SQL — `COMMENT ON … IS '…'`, một dòng "
            + "nhật ký — thì đó là RANH GIỚI ĐÃ BIẾT của lớp quét trong chuỗi: lớp này gom mọi "
            + "chuỗi của toàn văn và không biết chuỗi đó thuộc câu lệnh nào. Đọc khối RANH GIỚI "
            + "ĐÃ BIẾT ở PermissionScriptParityTests trước khi sửa, và ĐỪNG gỡ lớp quét — gỡ nó "
            + "là mở lại đúng lỗ SQL động."
            : ". Cổng chỉ đối chiếu INSERT; một câu sửa hay xoá khoá làm phép so lệch mà không "
            + "ai thấy. Mở rộng bộ đọc, KHÔNG bỏ qua câu này.";

        return new InvalidOperationException(
            $"{origin} có câu '{string.Join(' ', shape)}{(tail is null ? string.Empty : $" … {tail}")}' "
          + $"trên bảng '{table}' mà cổng B7 không đọc được"
          + advice);
    }

    // Nội dung MỌI chuỗi của văn bản, đã gỡ thoát — và, ĐỆ QUY, nội dung mọi chuỗi nằm trong đó.
    // Chú thích bị bỏ trước khi tìm, nên một dấu nháy lẻ trong chú thích tiếng Việt không mở ra một
    // "chuỗi" giả.
    //
    // VÌ SAO ĐỆ QUY CHỨ KHÔNG MỘT TẦNG: SQL động lồng hai tầng là SQL chạy được thật.
    // `EXECUTE 'EXECUTE ''DELETE FROM core.permission'''` xoá thật một khoá, và cũng thật khi viết
    // trong thân `DO $EF$ … $EF$`. Bóc đúng một tầng thì nội dung thu được là
    // `EXECUTE 'DELETE FROM core.permission'`, và với bộ tìm câu lệnh thì câu xoá ở đó chỉ là "một
    // chuỗi đã đóng đúng" — bước qua nguyên khối, không ai thấy. Đo được: cả hai dạng đi qua yên
    // lặng trước khi sửa.
    //
    // Giữ KÈM cờ DollarQuoted chứ không chỉ nội dung: AssertNoDataMutation đối xử khác nhau với
    // hai loại: thân `$tag$…$tag$` là MÃ mà CollectInsertRows đọc thẳng được, còn chuỗi nháy đơn là
    // thứ chỉ chạy qua EXECUTE và không bộ đọc tĩnh nào đối chiếu nổi. Trộn hai loại làm một thì
    // hoặc bỏ sót INSERT động, hoặc làm đỏ chính câu seed thật.
    //
    // Phép đệ quy luôn dừng: mỗi tầng mất ít nhất MỘT ký tự bao, nên nội dung ngắn hơn hẳn văn bản
    // chứa nó. Một ký tự chứ không phải hai — nhánh KHOAN DUNG của SkipSingleQuoted trả nội dung
    // của một chuỗi KHÔNG ĐÓNG (`'abc` → `abc`), và nhánh đó chạy thật ở đây vì tầng trong luôn
    // gọi với lenient: true. Lời khai "hai ký tự" trước đây mô tả một nhánh không bao quát hết;
    // kết luận "luôn dừng" không đổi, vì phép giảm vẫn nghiêm ngặt.
    private static List<QuotedToken> StringLiterals(string sql, string table)
    {
        var literals = new List<QuotedToken>();
        Collect(sql, table, lenient: false, literals);
        return literals;

        static void Collect(string text, string table, bool lenient, List<QuotedToken> into)
        {
            foreach (var quoted in QuotedTokens(text, table, lenient))
            {
                into.Add(quoted);

                // Tầng trong luôn KHOAN DUNG: nội dung một chuỗi không phải một tệp SQL, nên nháy
                // lẻ hay '/*' chưa đóng ở đó chỉ có nghĩa "phần còn lại không phải câu lệnh". Ném
                // ở đó là báo đỏ SAI cho một tệp hợp lệ — xem AssertNoDataMutation.
                Collect(quoted.Content, table, lenient: true, into);
            }
        }
    }

    private readonly record struct QuotedToken(string Content, bool DollarQuoted);

    // Mọi chuỗi ở MỘT tầng của văn bản, đọc bằng đúng bộ tokenizer mà các bộ tìm dùng: chú thích bỏ
    // đi, định danh bước qua trọn vẹn, chuỗi bước qua nguyên khối.
    private static List<QuotedToken> QuotedTokens(string sql, string table, bool lenient)
    {
        var tokens = new List<QuotedToken>();
        var position = 0;

        while (true)
        {
            SkipTrivia(sql, ref position, lenient);

            if (position >= sql.Length)
                return tokens;

            if (TrySkipQuoted(sql, ref position, table, lenient, out var token))
            {
                tokens.Add(token);
                continue;
            }

            SkipToken(sql, ref position);
        }
    }

    // Bước qua MỘT chuỗi, ở mọi dạng bộ đọc biết, và trả nội dung đã gỡ thoát:
    //   • `'…'`         — `''` là nháy thoát;
    //   • `E'…'`        — chuỗi cho phép gạch chéo ngược, nên `\'` KHÔNG đóng chuỗi và `\\` là một
    //     gạch chéo. Bỏ sót dạng này làm một tệp SQL HỢP LỆ bị ném "chuỗi không được đóng" — cùng
    //     hình dạng đỏ-sai với ca `''` đã phải sửa trước đó, và cùng lối thoát sai: đi nới cổng;
    //   • `$tag$…$tag$` — nháy đô la, KHÔNG có ký tự thoát nào bên trong, tag phải khớp. Đây là dạng
    //     mọi script sinh từ EF đều dùng (`DO $EF$ … $EF$`), và cũng là cách viết một giá trị có dấu
    //     nháy mà không phải thoát: `$$don't panic$$`. Bộ đọc cũ không biết nó là chuỗi, nên dấu
    //     nháy bên trong mở ra một chuỗi giả chạy tới hết tệp rồi ném — đo được.
    //
    // `$` KHÔNG phải lúc nào cũng mở chuỗi: `$1` là tham số vị trí. Tag phải là định danh hợp lệ
    // (rỗng cũng được: `$$`) và phải có tag đóng KHỚP. Không khớp thì đây không phải chuỗi, `$` trả
    // lại cho bộ tokenizer và KHÔNG ném — phần còn lại vẫn nhìn thấy được, nên không có gì biến mất
    // im lặng để phải kêu.
    private static bool TrySkipQuoted(
        string sql, ref int position, string table, bool lenient, out QuotedToken token)
    {
        token = default;

        if (position >= sql.Length)
            return false;

        if (TrySkipDollarQuoted(sql, ref position, out var body))
        {
            token = new QuotedToken(body, DollarQuoted: true);
            return true;
        }

        var backslashEscapes = IsExtendedStringStart(sql, position);

        if (!backslashEscapes && sql[position] != '\'')
            return false;

        if (backslashEscapes)
            position++; // tiền tố E

        token = new QuotedToken(
            SkipSingleQuoted(sql, ref position, table, lenient, backslashEscapes), DollarQuoted: false);

        return true;
    }

    // `E'…'` — tiền tố phải là token đứng riêng, không phải chữ cái cuối của một định danh dài hơn.
    private static bool IsExtendedStringStart(string sql, int position)
        => position + 1 < sql.Length
        && sql[position] is 'E' or 'e'
        && sql[position + 1] == '\''
        && (position == 0 || !IsIdentifierChar(sql[position - 1]));

    private static bool TrySkipDollarQuoted(string sql, ref int position, out string body)
    {
        body = string.Empty;

        if (!TryReadDollarTag(sql, position, out var afterOpen, out var tag))
            return false;

        var close = sql.IndexOf(tag, afterOpen, StringComparison.Ordinal);

        if (close < 0)
            return false;

        body = sql[afterOpen..close];
        position = close + tag.Length;
        return true;
    }

    private static bool TryReadDollarTag(string sql, int position, out int afterOpen, out string tag)
    {
        afterOpen = -1;
        tag = string.Empty;

        if (position >= sql.Length || sql[position] != '$')
            return false;

        var scan = position + 1;

        while (scan < sql.Length && IsIdentifierChar(sql[scan]))
            scan++;

        if (scan >= sql.Length || sql[scan] != '$')
            return false;

        // `$1$` không phải tag: định danh Postgres không bắt đầu bằng chữ số.
        if (scan > position + 1 && char.IsDigit(sql[position + 1]))
            return false;

        tag = sql[position..(scan + 1)];
        afterOpen = scan + 1;
        return true;
    }

    // Trả về mỗi dòng VALUES dưới dạng ánh xạ tên cột → giá trị (chuỗi đã bỏ nháy; NULL → null).
    // `table` so khớp TRỌN TÊN: "core.permission" không khớp "core.permission_resource" — hai bảng
    // này chỉ khác nhau ở phần hậu tố, và khớp theo tiền tố sẽ trộn hai danh mục làm một.
    public static IReadOnlyList<IReadOnlyDictionary<string, string?>> ParseInsertRows(string sql, string table)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ArgumentException.ThrowIfNullOrWhiteSpace(table);

        var rows = new List<IReadOnlyDictionary<string, string?>>();
        CollectInsertRows(rows, sql, table, lenient: false);
        return rows;
    }

    // Thân nháy đô la là CHUỖI với bộ tokenizer, nhưng với Postgres nó có thể là MÃ: seed thật của
    // repo này nằm trong `DO $EF$ … $EF$`, và `DO` thi hành đúng thân đó. Nên hai bước, thiếu bước
    // nào cũng hỏng: bước qua nguyên khối ở tầng ngoài — để `$$don't panic$$` không mở ra một chuỗi
    // giả — rồi đọc LẠI chính thân đó như một văn bản SQL riêng. Bỏ bước hai thì cổng đọc 0 dòng
    // rồi xanh vì rỗng, đúng ca mà meta-test `…_ReadsAtLeastOneRealSeedRow` tồn tại để chặn.
    //
    // Tầng trong đọc KHOAN DUNG ở mức token: nháy lẻ hay '/*' chưa đóng trong một thân mang DỮ LIỆU
    // là chuyện bình thường. Ràng buộc KẾT CẤU thì vẫn ném ở mọi tầng — thiếu danh sách cột, thiếu
    // VALUES, lệch số cột, `ON CONFLICT … DO UPDATE` — vì chúng chỉ kích hoạt khi câu lệnh đã xác
    // nhận nhắm đúng bảng được canh, tức đã là thứ cổng này phải đối chiếu.
    private static void CollectInsertRows(
        List<IReadOnlyDictionary<string, string?>> rows, string sql, string table, bool lenient)
    {
        ReadInsertRows(rows, sql, table, lenient);

        foreach (var quoted in QuotedTokens(sql, table, lenient))
        {
            if (quoted.DollarQuoted)
                CollectInsertRows(rows, quoted.Content, table, lenient: true);
        }
    }

    private static void ReadInsertRows(
        List<IReadOnlyDictionary<string, string?>> rows, string sql, string table, bool lenient)
    {
        var cursor = 0;

        while (TryFindInsertInto(sql, table, cursor, lenient, out var afterTable))
        {
            cursor = afterTable;
            var position = afterTable;

            SkipTrivia(sql, ref position, lenient);

            // TỪ ĐÂY TRỞ XUỐNG, CÂU LỆNH ĐÃ XÁC NHẬN NHẮM ĐÚNG BẢNG ĐƯỢC CANH. Mọi dạng bộ đọc
            // không hiểu vì thế phải NÉM, không được `continue`.
            //
            // Hai nhánh này từng là `continue`, và cái giá đo được của nó có hai phần. (1) Khoá mồ
            // côi: một `INSERT INTO core.permission SELECT … FROM staging` đưa dòng vào database
            // thật trong khi cổng đọc 0 dòng thêm, nên `Diff` — thứ tự khai là mình canh chiều "có
            // dòng seed mà không nguồn nào khai" — không báo gì. (2) Nặng hơn: AssertNoUpsertUpdate
            // chỉ chạy SAU khi mệnh đề VALUES đọc xong, nên viết cùng câu đó ở dạng `SELECT`, hoặc
            // bỏ danh sách cột đi, là vô hiệu hoá nó — `docs/RULES.md` §6 khi đó hứa chặt hơn thứ
            // cổng kiểm. Cả bốn dạng đã đo là PASS im lặng trước khi sửa.
            //
            // Đỏ ở đây KHÔNG phải tác dụng phụ phải chịu đựng, nó là hành vi mong muốn: một
            // `INSERT … SELECT` vào bảng danh mục khoá CẦN một người xem, vì cổng không đối chiếu
            // nổi giá trị nó ghi vào.
            if (position >= sql.Length || sql[position] != '(')
                throw new InvalidOperationException(
                    $"SQL seed của bảng '{table}' có câu INSERT KHÔNG khai danh sách cột (dạng "
                  + "`INSERT INTO <bảng> VALUES …` hoặc `INSERT INTO <bảng> SELECT …`). Cổng B7 đối "
                  + "chiếu THEO TÊN CỘT nên không đọc được dạng này, trong khi dòng vẫn vào database "
                  + "thật. Mở rộng bộ đọc, KHÔNG bỏ qua câu này.");

            var columns = ParseIdentifierList(sql, ref position, table, lenient);

            SkipTrivia(sql, ref position, lenient);

            if (!TryMatchKeyword(sql, ref position, "VALUES"))
                throw new InvalidOperationException(
                    $"SQL seed của bảng '{table}' có câu INSERT không dùng mệnh đề VALUES (dạng "
                  + "`INSERT INTO <bảng> (…) SELECT …`). Giá trị khi đó đến từ một truy vấn mà cổng "
                  + "B7 không đối chiếu được, và mệnh đề `ON CONFLICT … DO UPDATE` đi kèm cũng không "
                  + "được kiểm. Mở rộng bộ đọc, KHÔNG bỏ qua câu này.");

            while (true)
            {
                SkipTrivia(sql, ref position, lenient);

                if (position >= sql.Length || sql[position] != '(')
                    break;

                var values = ParseValueTuple(sql, ref position, table, lenient);

                if (values.Count != columns.Count)
                    throw new InvalidOperationException(
                        $"SQL seed của bảng '{table}' có dòng {values.Count} giá trị nhưng danh sách cột khai "
                      + $"{columns.Count} cột — không đối chiếu được. Sửa migration, đừng nới bộ đọc.");

                var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < columns.Count; i++)
                    row[columns[i]] = values[i];

                rows.Add(row);

                SkipTrivia(sql, ref position, lenient);

                if (position < sql.Length && sql[position] == ',')
                {
                    position++;
                    continue;
                }

                break;
            }

            AssertNoUpsertUpdate(sql, position, table, lenient);

            cursor = position;
        }
    }

    // `INSERT … ON CONFLICT … DO UPDATE SET` là một thao tác GHI ĐÈ đội lốt INSERT, và nó là dạng
    // nguy hiểm nhất trong họ này vì nó đi qua đúng cửa mà cổng tin tưởng: bộ đọc lấy cặp
    // code + resource_key từ mệnh đề VALUES, so khớp, xanh — còn dòng THẬT trong database nhận giá
    // trị từ mệnh đề SET, có thể khác hẳn. Một `DO UPDATE SET resource_key = EXCLUDED.resource_key`
    // vì vậy làm cổng xác nhận một tập khoá không còn tồn tại, và hậu quả là 403 im lặng cho mọi
    // người — đúng thứ luật B7 tồn tại để chặn.
    //
    // KHÔNG PHẢI CA HIẾM: script seed hiện dùng `ON CONFLICT (code) WHERE is_deleted = false DO
    // NOTHING`, và đổi sang `DO UPDATE SET …` là sửa đổi tự nhiên khi muốn chạy lại script sau khi
    // sửa danh mục. DO NOTHING thì ngược lại, an toàn với phép đối chiếu: dòng trong DB hoặc đúng
    // bằng dòng VALUES vừa đọc, hoặc là dòng cũ — không mệnh đề nào đặt giá trị mới.
    //
    // Ở ĐÂY chứ không ở AssertNoDataMutation: mệnh đề này gắn vào MỘT câu INSERT, và chỉ có mặt
    // ParseInsertRows mới biết câu đó nhắm vào bảng được canh. Một phép tìm theo cụm từ khoá không
    // phân biệt được `DO UPDATE` của bảng này với `DO UPDATE` của bảng khác trong cùng tệp — mà
    // cùng tệp là chuyện thường: script seed có INSERT vào core.schema_script_history ngay dưới.
    private static void AssertNoUpsertUpdate(string sql, int from, string table, bool lenient)
    {
        var position = from;

        SkipTrivia(sql, ref position, lenient);

        if (!TryMatchKeyword(sql, ref position, "ON"))
            return;

        SkipTrivia(sql, ref position, lenient);

        if (!TryMatchKeyword(sql, ref position, "CONFLICT"))
            return;

        // Giữa CONFLICT và DO có thể là danh sách cột, biểu thức chỉ mục, mệnh đề WHERE hay tên
        // ràng buộc — đi theo token tới hết câu chứ không đoán hình dạng, và dừng ở ';' để mệnh đề
        // của câu SAU không bị gán nhầm cho câu này.
        while (position < sql.Length)
        {
            SkipTrivia(sql, ref position, lenient);

            if (position >= sql.Length || sql[position] == ';')
                return;

            if (TrySkipQuoted(sql, ref position, table, lenient, out _))
                continue;

            var tokenStart = position;

            if (TryMatchKeyword(sql, ref position, "DO"))
            {
                SkipTrivia(sql, ref position, lenient);

                if (TryMatchKeyword(sql, ref position, "UPDATE"))
                    throw new InvalidOperationException(
                        $"SQL seed của bảng '{table}' có mệnh đề 'ON CONFLICT … DO UPDATE'. Dòng "
                      + "thật trong database khi đó lấy giá trị từ mệnh đề SET, KHÔNG phải từ VALUES "
                      + "mà cổng B7 vừa đọc — cổng sẽ xác nhận một tập khoá không còn tồn tại. Dùng "
                      + "'DO NOTHING', hoặc mở rộng bộ đọc cho đọc được mệnh đề SET; KHÔNG bỏ qua.");

                position = tokenStart;
            }

            SkipToken(sql, ref position);
        }
    }

    // Đọc một cột bắt buộc của một dòng. Thiếu cột là lỗi ồn ào, KHÔNG phải chuỗi rỗng: một dòng
    // seed thiếu cột `code` mà bị đọc thành "" sẽ làm phép so lệch đi một cách khó truy.
    public static string RequiredValue(IReadOnlyDictionary<string, string?> row, string column, string table)
    {
        if (!row.TryGetValue(column, out var value))
            throw new InvalidOperationException(
                $"Dòng seed của bảng '{table}' không có cột '{column}' — cột đã bị đổi tên hoặc bỏ đi, "
              + "và cổng B7 đang so một thứ không còn tồn tại.");

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"Dòng seed của bảng '{table}' có cột '{column}' rỗng hoặc NULL.");

        return value;
    }

    // So HAI CHIỀU. Thiếu bên nào cũng là vi phạm, và hai chiều hỏng theo hai kiểu khác nhau:
    //   • khai trong C# mà thiếu dòng seed → IPermissionChecker trả false cho MỌI người (kể cả tài
    //     khoản mang cờ bypass, vì nhánh bypass cũng đọc từ chính bảng core.permission) ⇒ 403 im lặng;
    //   • có dòng seed mà không nguồn nào khai → một khoá mồ côi không mã nào dùng tới, gán được cho
    //     vai trò nhưng không bao giờ có hiệu lực.
    //
    // So theo BỘI SỐ, không theo tập. Luật B7 đòi "ĐÚNG MỘT dòng seed", mà một phép so hai HashSet
    // gộp mọi bản trùng lại làm một: hai dòng cùng code + resource_key khác id từng đi qua cổng này
    // mà không một thông điệp nào — cổng hứa một thứ chặt hơn thứ nó kiểm. Nửa (1)
    // (PermissionCatalogValidationHostedService) bắt được trùng ở phía C#; phía seed thì không ai
    // bắt. ON CONFLICT … DO NOTHING làm hậu quả vận hành nhẹ, nhưng lời hứa sai thì không.
    public static IReadOnlyList<string> Diff(
        IReadOnlyCollection<string> declared,
        IReadOnlyCollection<string> seeded,
        string declaredLabel,
        string seededLabel)
    {
        var declaredCounts = Counts(declared);
        var seededCounts = Counts(seeded);

        var messages = new List<string>();

        foreach (var item in Sorted(declaredCounts.Keys.Where(key => !seededCounts.ContainsKey(key))))
            messages.Add($"{item} — {declaredLabel} có khai, {seededLabel} KHÔNG có dòng nào khớp");

        foreach (var item in Sorted(seededCounts.Keys.Where(key => !declaredCounts.ContainsKey(key))))
            messages.Add($"{item} — {seededLabel} có dòng này, {declaredLabel} KHÔNG khai");

        var duplicated = declaredCounts.Concat(seededCounts)
            .Where(pair => pair.Value > 1)
            .Select(pair => pair.Key);

        foreach (var item in Sorted(duplicated.Distinct(StringComparer.Ordinal)))
        {
            declaredCounts.TryGetValue(item, out var d);
            seededCounts.TryGetValue(item, out var s);

            messages.Add(
                $"{item} — {declaredLabel} có {d} dòng, {seededLabel} có {s} dòng; luật B7 đòi ĐÚNG "
              + "MỘT dòng mỗi bên");
        }

        return messages;
    }

    private static Dictionary<string, int> Counts(IReadOnlyCollection<string> items)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var item in items)
            counts[item] = counts.TryGetValue(item, out var current) ? current + 1 : 1;

        return counts;
    }

    private static IEnumerable<string> Sorted(IEnumerable<string> items)
        => items.OrderBy(x => x, StringComparer.Ordinal);

    private static bool TryFindInsertInto(string sql, string table, int from, bool lenient, out int afterTable)
        => TryFindStatement(
            sql, table, from, lenient, tableList: false, tail: null, out afterTable, ["INSERT", "INTO"]);

    // Tìm `<từ khoá…> <tên bảng>` ở dạng câu lệnh thật: từ khoá không được là một phần của định danh
    // dài hơn, và tên bảng phải khớp TRỌN (xem ReadTableName).
    //
    // ĐI THEO TOKEN, KHÔNG PHẢI IndexOf TRÊN VĂN BẢN THÔ. Bản trước bắt đầu bằng
    // `sql.IndexOf(keywords[0], …)` và chỉ gọi SkipTrivia SAU khi một từ khoá đã khớp — tức bộ đọc
    // biết bỏ qua `--` và `/* */` nhưng không bao giờ dùng hiểu biết đó ở bước TÌM. Chú thích vì vậy
    // được coi là câu lệnh sống, và hỏng theo HAI chiều ngược nhau, cả hai đã đo trên chính file này:
    //   • ĐỎ SAI — một dòng chú thích tiếng Việt `-- … không bao giờ DELETE FROM core.permission …`
    //     làm AssertNoDataMutation ném, và thông điệp lại bảo người sửa đi "mở rộng bộ đọc";
    //   • XANH SAI — một câu seed bị chú thích để tạm vô hiệu vẫn được ParseInsertRows đọc ra như
    //     dòng sống, nên cổng xanh trong khi database thật không nhận khoá đó.
    // Repo này viết chú thích tiếng Việt rất dày trong cả script lẫn chuỗi SQL của migration, nên cả
    // hai chiều đều là chuyện sớm muộn chứ không phải giả định.
    //
    // Chuỗi bị bước qua NGUYÊN KHỐI, ở CẢ BA dạng (`'…'`, `E'…'`, `$tag$…$tag$` — xem TrySkipQuoted):
    // nó là dữ liệu, không phải câu lệnh — nhờ vậy một giá trị chứa `--` không mở ra một "chú thích"
    // giả, và một giá trị chứa chữ INSERT không sinh ra dòng seed ma. Riêng AssertNoDataMutation soi
    // thêm BÊN TRONG chuỗi, một lớp riêng, để phần SQL động không bị lọt (xem AssertNoDataMutation).
    //
    // Thân `$TAG$ … $TAG$` vì vậy KHÔNG còn được đọc như mã ở ngay tầng này — nhưng nó vẫn phải được
    // đọc, vì seed của script thật nằm trong `DO $EF$ … $EF$` và bỏ qua nó thì cổng đọc ra 0 dòng rồi
    // xanh vì rỗng. Việc đó do CollectInsertRows làm, bằng cách đọc lại thân đó như một văn bản riêng.
    //
    // `lenient` = đang quét NỘI DUNG MỘT CHUỖI, không phải một tệp: chuỗi chưa đóng hay chú thích
    // khối chưa đóng ở đó chỉ có nghĩa "phần còn lại không phải câu SQL", nên dừng và trả false.
    // Ném ở đó là báo ĐỎ SAI cho một tệp hợp lệ — xem AssertNoDataMutation.
    private static bool TryFindStatement(
        string sql, string table, int from, bool lenient, bool tableList, string? tail, out int afterTable,
        string[] keywords)
    {
        afterTable = -1;
        var position = from;

        while (true)
        {
            SkipTrivia(sql, ref position, lenient);

            if (position >= sql.Length)
                return false;

            if (TrySkipQuoted(sql, ref position, table, lenient, out _))
                continue;

            var tokenStart = position;

            if (TryMatchKeyword(sql, ref position, keywords[0]))
            {
                var scan = position;
                var matched = true;

                foreach (var keyword in keywords.Skip(1))
                {
                    SkipTrivia(sql, ref scan, lenient);

                    if (TryMatchKeyword(sql, ref scan, keyword))
                        continue;

                    matched = false;
                    break;
                }

                if (matched
                 && TryMatchTarget(sql, ref scan, table, tableList, lenient)
                 && TryMatchTail(sql, ref scan, tail, lenient))
                {
                    afterTable = scan;
                    return true;
                }

                // Khớp hụt: lùi về ĐẦU token rồi bước đúng một token. Bước từ chỗ đang đứng sẽ nhảy
                // qua phần còn lại của câu và bỏ sót câu lệnh kế tiếp nằm trong đó.
                position = tokenStart;
            }

            SkipToken(sql, ref position);
        }
    }

    // Đọc MỤC TIÊU của câu lệnh và nói nó có phải bảng đang canh không.
    //
    // HAI thứ bản trước bỏ sót, cả hai là cú pháp Postgres hợp lệ và cả hai đã đo là PASS im lặng:
    //   • từ khoá TUỲ CHỌN `ONLY` chen giữa cụm từ khoá và tên bảng — `DELETE FROM ONLY t`,
    //     `UPDATE ONLY t`, `TRUNCATE ONLY t`, `MERGE INTO ONLY t`. Bỏ qua nó KHÔNG nới cổng: phép
    //     khớp vẫn đòi TRỌN tên bảng, nên "core.permission_resource" vẫn không thành
    //     "core.permission";
    //   • `TRUNCATE` nhận DANH SÁCH bảng — `TRUNCATE core.app_user, core.permission` xoá sạch bảng
    //     được canh dù tên nó đứng thứ hai. CHỈ TRUNCATE đi đường này (tableList): Postgres không
    //     cho DELETE hay UPDATE nhiều bảng trong một câu, và cho chúng đọc danh sách sẽ biến phần
    //     sau `DELETE FROM a USING …` thành một phép khớp rộng hơn luật.
    //
    // Phần đuôi `RESTART IDENTITY` / `CASCADE` tự rơi ra ngoài: vòng lặp chỉ đi tiếp khi gặp dấu
    // phẩy, nên một từ khoá đứng sau danh sách chỉ làm phép khớp dừng lại.
    private static bool TryMatchTarget(
        string sql, ref int position, string table, bool tableList, bool lenient)
    {
        while (true)
        {
            SkipTrivia(sql, ref position, lenient);
            TryMatchKeyword(sql, ref position, "ONLY");
            SkipTrivia(sql, ref position, lenient);

            if (string.Equals(ReadTableName(sql, ref position), table, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!tableList)
                return false;

            SkipTrivia(sql, ref position, lenient);

            if (position >= sql.Length || sql[position] != ',')
                return false;

            position++;
        }
    }

    // Từ khoá BẮT BUỘC đứng sau tên bảng, sau một danh sách cột tuỳ chọn. Đây là thứ phân biệt
    // `COPY t (c) FROM stdin` — nạp dữ liệu vào bảng, một đường GHI không đi qua INSERT nào — với
    // `COPY t TO stdout`, vốn chỉ đọc và không làm tập khoá lệch đi. Dạng câu nào không khai tail
    // thì không có ràng buộc nào ở đây và hàm trả true ngay.
    private static bool TryMatchTail(string sql, ref int position, string? tail, bool lenient)
    {
        if (tail is null)
            return true;

        SkipTrivia(sql, ref position, lenient);

        if (position < sql.Length && sql[position] == '(' && !TrySkipParenGroup(sql, ref position))
            return false;

        SkipTrivia(sql, ref position, lenient);

        return TryMatchKeyword(sql, ref position, tail);
    }

    // Bước qua một cụm `( … )` cân bằng. Chuỗi đi qua nguyên khối để một dấu ')' nằm trong giá trị
    // không đóng cụm sớm. Không cân bằng thì trả false — đây là phép khớp hụt, không phải lỗi tệp.
    private static bool TrySkipParenGroup(string sql, ref int position)
    {
        var depth = 0;

        while (position < sql.Length)
        {
            if (TrySkipQuoted(sql, ref position, string.Empty, lenient: true, out _))
                continue;

            if (sql[position] == '(')
            {
                depth++;
                position++;
                continue;
            }

            if (sql[position] == ')')
            {
                position++;

                if (--depth == 0)
                    return true;

                continue;
            }

            position++;
        }

        return false;
    }

    // Bước qua đúng MỘT token: một định danh trọn vẹn, hoặc một ký tự đơn. Đi theo định danh trọn vẹn
    // là cách duy nhất để 'INSERTED' không bao giờ bị đọc thành 'INSERT' rồi phần thừa 'ED'.
    private static void SkipToken(string sql, ref int position)
    {
        if (position >= sql.Length)
            return;

        if (!IsIdentifierChar(sql[position]))
        {
            position++;
            return;
        }

        while (position < sql.Length && IsIdentifierChar(sql[position]))
            position++;
    }

    // Đọc trọn tên bảng, kể cả phần schema và nháy kép. Dừng ở khoảng trắng hoặc '(' — nhờ vậy
    // "core.permission_resource" đọc ra TRỌN, không bị cắt thành "core.permission".
    private static string ReadTableName(string sql, ref int position)
    {
        var start = position;

        while (position < sql.Length && (IsIdentifierChar(sql[position]) || sql[position] is '.' or '"'))
            position++;

        return sql[start..position].Replace("\"", string.Empty, StringComparison.Ordinal);
    }

    private static List<string> ParseIdentifierList(string sql, ref int position, string table, bool lenient)
    {
        var names = new List<string>();

        position++; // '('

        while (true)
        {
            SkipTrivia(sql, ref position, lenient);

            if (position >= sql.Length)
                throw new InvalidOperationException($"Danh sách cột của '{table}' không được đóng bằng ')'.");

            if (sql[position] == ')')
            {
                position++;
                return names;
            }

            var start = position;

            while (position < sql.Length && (IsIdentifierChar(sql[position]) || sql[position] == '"'))
                position++;

            if (position == start)
                throw new InvalidOperationException(
                    $"Không đọc được tên cột trong danh sách cột của '{table}' tại vị trí {position}.");

            names.Add(sql[start..position].Replace("\"", string.Empty, StringComparison.Ordinal));

            SkipTrivia(sql, ref position, lenient);

            if (position < sql.Length && sql[position] == ',')
                position++;
        }
    }

    private static List<string?> ParseValueTuple(string sql, ref int position, string table, bool lenient)
    {
        var values = new List<string?>();

        position++; // '('

        var depth = 0;
        var start = position;

        while (position < sql.Length)
        {
            // Chuỗi phải đi qua nguyên khối, ở MỌI dạng: một dấu ',' hay ')' NẰM TRONG chuỗi không
            // phải dấu ngăn cách, và bộ đọc nào quên điều này sẽ cắt sai ngay dòng đầu có dấu phẩy
            // trong nhãn — hoặc, với `$$…$$`, ngay dòng đầu có dấu nháy trong giá trị.
            if (TrySkipQuoted(sql, ref position, table, lenient, out _))
                continue;

            var current = sql[position];

            if (current == '(')
            {
                depth++;
                position++;
                continue;
            }

            if (current == ')')
            {
                if (depth > 0)
                {
                    depth--;
                    position++;
                    continue;
                }

                values.Add(Normalize(sql[start..position]));
                position++;
                return values;
            }

            if (current == ',' && depth == 0)
            {
                values.Add(Normalize(sql[start..position]));
                position++;
                start = position;
                continue;
            }

            position++;
        }

        throw new InvalidOperationException($"Dòng VALUES của '{table}' không được đóng bằng ')'.");
    }

    // Bước qua phần `'…'` của một chuỗi (vị trí đang ở nháy mở) và trả nội dung ĐÃ GỠ THOÁT.
    // `backslashEscapes` bật cho dạng `E'…'`: ở đó `\'` là một dấu nháy trong dữ liệu, KHÔNG phải
    // dấu đóng — coi nó là dấu đóng thì phần còn lại của tệp bị nuốt vào một "chuỗi" chạy tới hết
    // văn bản rồi ném, tức báo đỏ SAI cho SQL hợp lệ.
    private static string SkipSingleQuoted(
        string sql, ref int position, string table, bool lenient, bool backslashEscapes)
    {
        var content = new StringBuilder();

        position++; // nháy mở

        while (position < sql.Length)
        {
            var current = sql[position];

            if (backslashEscapes && current == '\\' && position + 1 < sql.Length)
            {
                content.Append(sql[position + 1]);
                position += 2;
                continue;
            }

            if (current != '\'')
            {
                content.Append(current);
                position++;
                continue;
            }

            // '' bên trong chuỗi là một dấu nháy thoát, không phải dấu đóng.
            if (position + 1 < sql.Length && sql[position + 1] == '\'')
            {
                content.Append('\'');
                position += 2;
                continue;
            }

            position++;
            return content.ToString();
        }

        // Khoan dung CHỈ dành cho việc quét nội dung một chuỗi (xem TryFindStatement): ở đó số nháy
        // lẻ là chuyện bình thường của dữ liệu đã gỡ thoát, không phải lỗi của tệp. Ở mức tệp thì
        // vẫn ném — một chuỗi không đóng làm mọi câu lệnh sau nó vô hình với cổng.
        if (lenient)
        {
            position = sql.Length;
            return content.ToString();
        }

        throw new InvalidOperationException($"Chuỗi trong SQL seed của '{table}' không được đóng bằng nháy đơn.");
    }

    // Giá trị nào là MỘT chuỗi trọn vẹn thì trả nội dung đã gỡ thoát — ở cả ba dạng, vì nếu chỉ
    // nhận `'…'` thì một giá trị viết `E'…'` hay `$$…$$` được đem đi so NGUYÊN VĂN cả dấu bao, và
    // phép so lệch đi một cách im lặng. Biểu thức (ghép chuỗi, lời gọi hàm) giữ nguyên văn bản.
    private static string? Normalize(string raw)
    {
        var text = raw.Trim();
        var position = 0;

        if (text.Length > 0
         && TrySkipQuoted(text, ref position, string.Empty, lenient: true, out var quoted)
         && position == text.Length)
            return quoted.Content;

        return string.Equals(text, "NULL", StringComparison.OrdinalIgnoreCase) ? null : text;
    }

    private static void SkipTrivia(string sql, ref int position, bool lenient = false)
    {
        while (position < sql.Length)
        {
            if (char.IsWhiteSpace(sql[position]))
            {
                position++;
                continue;
            }

            if (position + 1 < sql.Length && sql[position] == '-' && sql[position + 1] == '-')
            {
                while (position < sql.Length && sql[position] != '\n')
                    position++;

                continue;
            }

            if (position + 1 < sql.Length && sql[position] == '/' && sql[position + 1] == '*')
            {
                var end = sql.IndexOf("*/", position + 2, StringComparison.Ordinal);

                // Chú thích khối không đóng: bản trước nhảy thẳng tới cuối văn bản, tức MỌI câu lệnh
                // sau đó trở nên vô hình và cổng xanh vì không còn gì để đọc. Từ khi bộ tìm đi theo
                // token, phép bỏ chú thích quyết định cái gì được coi là mã — nên chỗ này phải kêu.
                if (end < 0)
                {
                    // Cùng lý do với SkipSingleQuoted: trong nội dung một chuỗi thì '/*' không đóng
                    // chỉ có nghĩa "phần còn lại không phải mã", không phải lỗi của tệp.
                    if (lenient)
                    {
                        position = sql.Length;
                        return;
                    }

                    throw new InvalidOperationException(
                        "SQL có chú thích khối '/*' không được đóng bằng '*/'. Phần còn lại của văn "
                      + "bản sẽ vô hình với cổng B7, và cổng xanh vì đọc được rỗng. Sửa SQL, đừng "
                      + "nới bộ đọc.");
                }

                position = end + 2;
                continue;
            }

            return;
        }
    }

    private static bool TryMatchKeyword(string sql, ref int position, string keyword)
    {
        if (position + keyword.Length > sql.Length)
            return false;

        if (string.Compare(sql, position, keyword, 0, keyword.Length, StringComparison.OrdinalIgnoreCase) != 0)
            return false;

        var after = position + keyword.Length;

        if (after < sql.Length && IsIdentifierChar(sql[after]))
            return false;

        position = after;
        return true;
    }

    private static bool IsIdentifierChar(char value) => char.IsLetterOrDigit(value) || value == '_';
}
