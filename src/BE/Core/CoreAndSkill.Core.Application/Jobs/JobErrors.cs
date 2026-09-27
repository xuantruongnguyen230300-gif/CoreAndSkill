using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Jobs;

// docs/contracts/jobs.md.
public static class JobErrors
{
    // Không có việc đó, HOẶC việc không do người gọi khởi tạo — gộp hai ca (luật M7).
    public static readonly Error NotFound = new(
        "CORE.JOB.NOT_FOUND", "Không tìm thấy việc.", ErrorType.NotFound);

    // Ba mã dưới chỉ đi vào cột `error` của core.job (không ra HTTP với tư cách lỗi của request).
    public static readonly Error NoExecutor = new(
        "CORE.JOB.NO_EXECUTOR", "Không có bộ chạy nào cho loại việc '{JobType}'.", ErrorType.BusinessRule);

    // Tiến trình dừng giữa chừng — hàng đợi chạy nền nằm trong bộ nhớ nên việc dở dang không tự chạy
    // tiếp; JobRecoveryHostedService đánh dấu chúng để người dùng không chờ vô hạn.
    public static readonly Error Interrupted = new(
        "CORE.JOB.INTERRUPTED", "Việc bị gián đoạn vì tiến trình dừng giữa chừng. Khởi tạo lại việc.", ErrorType.BusinessRule);

    // Executor ném ngoại lệ — nhánh catch của JobRunner. Cố ý KHÔNG dùng mã hệ thống dùng chung của
    // CommonErrors: luật R9 cấm project này tham chiếu loại lỗi mà chỉ hạ tầng HTTP được phát, và một mã
    // riêng của khu việc nền còn tách "việc nền hỏng" khỏi "một request đã ra 500" khi đọc log
    // (docs/adr/0077-loi-ngoai-le-cua-viec-nen-mang-ma-core-job-unexpected.md).
    // KHÔNG handler nào được trả mã này: BusinessRule ánh xạ 422, sai nghĩa cho một lỗi hệ thống. Nó vô hại ở
    // đây chỉ vì JobJson.ErrorOf không ghi `type` và đường này không đi qua ResultToHttpMapper.
    public static readonly Error Unexpected = new(
        "CORE.JOB.UNEXPECTED", "Việc hỏng vì một lỗi không lường trước. Chi tiết chỉ nằm trong log máy chủ.", ErrorType.BusinessRule);
}
