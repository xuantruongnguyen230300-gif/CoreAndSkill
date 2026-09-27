namespace CoreAndSkill.Core.Contracts;

// Kiểu neo duy nhất của assembly này — cho ArchTests nạp assembly qua typeof(...), không qua
// đường dẫn tệp. Trống ở B0 (docs/kien-truc-core-module.md §2); nội dung thật (integration event,
// DTO dùng chung giữa module) tới cùng module đầu tiên.
public static class AssemblyMarker;
