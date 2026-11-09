namespace CoreAndSkill.Core.Contracts;

// Kiểu neo duy nhất của assembly này — cho ArchTests nạp assembly qua typeof(...), không qua
// đường dẫn tệp. Trống ở B0 (docs/kien-truc-core-module.md §2); nội dung thật là hợp đồng giữa Core và
// module (ví dụ integration event Core phát). Hợp đồng GIỮA HAI MODULE KHÔNG đặt ở đây: nó ở Contracts của
// module phát (ADR-0101).
public static class AssemblyMarker;
