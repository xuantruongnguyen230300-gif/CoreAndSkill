using CoreAndSkill.Core.Application.Export;
using CoreAndSkill.Core.Application.Files;
using Microsoft.AspNetCore.Mvc;

namespace CoreAndSkill.Core.Web.Http;

// Lối dựng phản hồi cho nhánh TẢI TỆP — docs/adr/0068-nhanh-tai-tep-di-qua-handlefile.md quyết định 3.
//
// Nhánh này cố ý KHÔNG có lớp IActionResult riêng: ba header bắt buộc của một phản hồi tệp
// (`Content-Disposition: attachment`, `X-Content-Type-Options: nosniff`, `Cache-Control: no-store`) giữ
// đúng MỘT nguồn trong Core.Web là ExportFileResult. Một lớp thứ hai đặt lại ba header đó chỉ dời chỗ
// hỏng từ "action ↔ lớp kết quả" sang "lớp kết quả ↔ lớp kết quả" (phương án C của ADR).
//
// Vòng đời luồng: FileDownload.Content là luồng ĐÃ MỞ mà người nhận đóng
// (docs/wiki-core/be/14-file-storage.md). Người nhận là chỗ này — `await using` đóng luồng sau khi ghi
// xong, VÀ khi client ngắt giữa chừng (CopyToAsync ném thì khối using vẫn chạy).
internal static class FileDownloadResponse
{
    public static IActionResult For(FileDownload download)
        => new ExportFileResult(
            new ExportFile(download.FileName, download.ContentType, async (body, ct) =>
            {
                await using var content = download.Content;
                await content.CopyToAsync(body, ct);
            }),
            KnownLength(download.Content));

    // Đọc TRƯỚC khi luồng bị tiêu thụ. Luồng không seekable thì không khai Content-Length — thà thiếu
    // header còn hơn khai một con số sai, vì khai sai làm hỏng chính giao thức.
    private static long? KnownLength(Stream content)
        => content.CanSeek ? content.Length - content.Position : null;
}
