using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Files;
using CoreAndSkill.Core.Domain.Common;
using Microsoft.Extensions.Options;

namespace CoreAndSkill.Core.Infrastructure.Files;

// Hiện thực IFileStorage trên hệ thống tệp cục bộ — đủ cho v1 một instance (docs/wiki-core/be/14-file-storage.md
// §8 mục "Đĩa cục bộ không dùng chung được"; ADR-0014). Đổi sang ổ mạng hay dịch vụ đối tượng là thêm
// MỘT hiện thực khác của cùng interface — nghiệp vụ không biết tệp nằm đâu.
//
// Bố cục: `<gốc>/<purpose>/<năm>/<tháng>/<guid>.<đuôi>` (§2) — phân theo mục đích rồi theo thời gian.
// Tên tệp trên đĩa do HỆ THỐNG sinh. Tệp tạm ở `<gốc>/_tmp/` — TÁCH khỏi tệp lâu dài (§6). Khoá đi vào
// hàm này còn được kiểm theo đúng khuôn trước khi ghép đường dẫn, rồi kiểm lần nữa rằng đường dẫn cuối
// vẫn nằm trong thư mục gốc: khoá đến từ DB (đáng tin) nhưng không có lý do gì để không chặn thoát thư mục.
internal sealed partial class LocalFileStorage(IOptions<CoreFileOptions> options, TimeProvider timeProvider) : IFileStorage
{
    private const string TempDirectory = "_tmp";
    private const int BufferSize = 81920;

    // <purpose>/<yyyy>/<MM>/<32 hex>[.<đuôi>][.part]
    [GeneratedRegex(@"^[a-z][a-z0-9-]{0,49}/\d{4}/\d{2}/[0-9a-f]{32}(\.[a-z0-9]{1,5})?(\.part)?$")]
    private static partial Regex PermanentKeyPattern();

    [GeneratedRegex(@"^_tmp/[0-9a-f]{32}(\.part)?$")]
    private static partial Regex TempKeyPattern();

    [GeneratedRegex(@"^[a-z][a-z0-9-]{0,49}$")]
    private static partial Regex PurposePattern();

    [GeneratedRegex(@"^(\.[a-z0-9]{1,5})?$")]
    private static partial Regex ExtensionPattern();

    private string RootPath => Path.GetFullPath(options.Value.RootPath);

    public async Task<string> SaveAsync(Stream content, string purpose, string extension, CancellationToken ct)
    {
        if (!PurposePattern().IsMatch(purpose))
            throw new ArgumentException("Purpose sai khuôn.", nameof(purpose));

        if (!ExtensionPattern().IsMatch(extension))
            throw new ArgumentException("Phần mở rộng sai khuôn.", nameof(extension));

        var now = timeProvider.GetUtcNow();
        var key = $"{purpose}/{now:yyyy}/{now:MM}/{Guid.NewGuid():N}{extension}";

        await WriteAtomicAsync(ResolvePermanent(key), content, ct);
        return key;
    }

    public async Task<string> SaveTempAsync(Stream content, CancellationToken ct)
    {
        var key = $"{TempDirectory}/{Guid.NewGuid():N}";

        await WriteAtomicAsync(ResolveTemp(key), content, ct);
        return key;
    }

    public Task<Result<Stream>> OpenAsync(string key, CancellationToken ct)
        => Task.FromResult(Open(ResolvePermanent(key)));

    public Task<Result<Stream>> OpenTempAsync(string tempKey, CancellationToken ct)
        => Task.FromResult(Open(ResolveTemp(tempKey)));

    public Task DeleteAsync(string key, CancellationToken ct)
    {
        File.Delete(ResolvePermanent(key));
        return Task.CompletedTask;
    }

    public Task DeleteTempAsync(string tempKey, CancellationToken ct)
    {
        File.Delete(ResolveTemp(tempKey));
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct)
        => Task.FromResult(File.Exists(ResolvePermanent(key)));

    public async IAsyncEnumerable<StoredObject> ListAsync([EnumeratorCancellation] CancellationToken ct)
    {
        await Task.CompletedTask;

        var root = RootPath;
        foreach (var purposeDirectory in Directory.EnumerateDirectories(root))
        {
            if (string.Equals(Path.GetFileName(purposeDirectory), TempDirectory, StringComparison.Ordinal))
                continue;

            foreach (var path in Directory.EnumerateFiles(purposeDirectory, "*", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();

                var key = Path.GetRelativePath(root, path).Replace('\\', '/');

                // Tệp lạ (không đúng khuôn do hệ thống sinh) không phải thứ ta quản lý — bỏ qua, đừng xoá.
                if (PermanentKeyPattern().IsMatch(key))
                    yield return new StoredObject(key, File.GetLastWriteTimeUtc(path));
            }
        }
    }

    public Task<int> PurgeTempAsync(DateTimeOffset olderThan, CancellationToken ct)
    {
        var directory = Path.Combine(RootPath, TempDirectory);
        if (!Directory.Exists(directory))
            return Task.FromResult(0);

        var deleted = 0;
        foreach (var path in Directory.EnumerateFiles(directory))
        {
            ct.ThrowIfCancellationRequested();

            if (File.GetLastWriteTimeUtc(path) >= olderThan.UtcDateTime)
                continue;

            File.Delete(path);
            deleted++;
        }

        return Task.FromResult(deleted);
    }

    // Ghi ra tệp `.part` rồi đổi tên: người đọc không bao giờ thấy tệp ghi dở, và tiến trình chết giữa
    // chừng chỉ để lại một tệp `.part` mà job đối soát dọn (nó không có bản ghi DB).
    private static async Task WriteAtomicAsync(string path, Stream content, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var partial = path + ".part";
        try
        {
            await using (var output = new FileStream(
                partial, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous))
            {
                await content.CopyToAsync(output, BufferSize, ct);
                await output.FlushAsync(ct);
            }

            File.Move(partial, path);
        }
        catch
        {
            File.Delete(partial);
            throw;
        }
    }

    private static Result<Stream> Open(string path)
    {
        if (!File.Exists(path))
            return Result.Failure<Stream>(FileErrors.ContentMissing);

        return new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
    }

    private string ResolvePermanent(string key) => Resolve(key, PermanentKeyPattern());

    private string ResolveTemp(string key) => Resolve(key, TempKeyPattern());

    private string Resolve(string key, Regex pattern)
    {
        if (!pattern.IsMatch(key))
            throw new ArgumentException("Khoá tệp sai khuôn.", nameof(key));

        var root = RootPath;
        var full = Path.GetFullPath(Path.Combine(root, key));

        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!full.StartsWith(rootWithSeparator, StringComparison.Ordinal))
            throw new ArgumentException("Khoá tệp thoát khỏi thư mục gốc.", nameof(key));

        return full;
    }
}
