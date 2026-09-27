namespace CoreAndSkill.Core.Infrastructure.Tabular;

// Cầu nối giữa thứ CHỈ ghi đồng bộ được (đóng mục nén của ZipArchive gọi Write() đồng bộ, kể cả khi đóng bằng
// DisposeAsync) và luồng đích KHÔNG cho ghi đồng bộ (thân phản hồi HTTP của Kestrel mặc định).
//
// ZipArchive ghi vào bộ đệm này — ghi đồng bộ hay bất đồng bộ đều chỉ nối thêm vào bộ nhớ, không chờ gì. Người
// dùng (XlsxTabularWriter) tự "xả" bộ đệm sang luồng đích bằng ĐƯỜNG BẤT ĐỒNG BỘ ở những điểm an toàn: sau mỗi
// dòng và sau mỗi lần đóng mục. Bộ nhớ dùng bị chặn bởi lượng ZipArchive sinh ra giữa hai lần xả — vài KB, không
// tỉ lệ với số dòng — nên đây KHÔNG phải cách dựng cả tệp trong bộ nhớ.
//
// Không seekable có chủ đích: đó là điều kiện để ZipArchive chọn chế độ ghi tuần tự (mục nén kèm data
// descriptor) thay vì quay lại sửa header.
internal sealed class DrainableBufferStream : Stream
{
    private readonly MemoryStream _buffer = new();

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    // Số byte đang chờ xả.
    public long Pending => _buffer.Length;

    public override void Write(byte[] buffer, int offset, int count) => _buffer.Write(buffer, offset, count);

    public override void Write(ReadOnlySpan<byte> buffer) => _buffer.Write(buffer);

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        _buffer.Write(buffer.Span);
        return ValueTask.CompletedTask;
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        _buffer.Write(buffer, offset, count);
        return Task.CompletedTask;
    }

    // Đẩy mọi thứ đang chờ sang luồng đích rồi làm rỗng bộ đệm. Không có gì chờ thì không làm gì.
    public async Task DrainToAsync(Stream target, CancellationToken ct)
    {
        if (_buffer.Length == 0)
            return;

        await target.WriteAsync(_buffer.GetBuffer().AsMemory(0, (int)_buffer.Length), ct);
        _buffer.SetLength(0);
    }

    // Flush của bộ nén không có ý nghĩa với bộ đệm trong bộ nhớ; xả sang đích là việc của DrainToAsync.
    public override void Flush()
    {
    }

    public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _buffer.Dispose();

        base.Dispose(disposing);
    }
}
