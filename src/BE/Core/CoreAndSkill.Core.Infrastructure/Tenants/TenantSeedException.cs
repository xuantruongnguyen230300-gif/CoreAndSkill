namespace CoreAndSkill.Core.Infrastructure.Tenants;

// Lỗi SEED của đơn vị mới: dữ liệu mặc định gộp từ các ITenantSeedSource không dựng được thành dòng — vai trò trùng, ánh
// xạ trỏ tới vai trò/khoá không có, mục menu trỏ tới cha/khoá không có. TenantProvisioningService đổi ĐÚNG loại này thành
// CORE.TENANT.SEED_FAILED (docs/contracts/tenants.md §2); mọi exception khác đi ra như lỗi hệ thống.
//
// Không kế thừa InvalidOperationException có chủ đích: một khối bắt InvalidOperationException ở đâu đó không được vô tình
// nuốt nó, và nó không được lẫn với InvalidOperationException của EF hay của một nguồn seed viết sai.
internal sealed class TenantSeedException(string message, Exception? innerException = null) : Exception(message, innerException);
