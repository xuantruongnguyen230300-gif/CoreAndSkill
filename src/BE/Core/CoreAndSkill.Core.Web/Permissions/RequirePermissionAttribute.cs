namespace CoreAndSkill.Core.Web.Permissions;

// Một trong BA mức khai báo phân quyền endpoint (luật S11) — docs/quy-uoc/be-api-controller.md §4.2,
// docs/adr/0024-ba-muc-khai-bao-phan-quyen-endpoint.md. AllowMultiple=true: nhiều khoá trên cùng
// action đòi TẤT CẢ (RequirePermissionFilter §4.2).
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute(string key) : Attribute
{
    public string Key { get; } = key;
}
