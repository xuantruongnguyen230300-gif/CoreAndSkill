namespace CoreAndSkill.Core.Web.Permissions;

// Một trong BA mức khai báo phân quyền endpoint (luật S11) — khu quản trị hệ thống,
// docs/adr/0017-khu-quan-tri-he-thong.md. Kiểm cờ is_system_operator, KHÔNG đi qua ma trận quyền.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireSystemOperatorAttribute : Attribute;
