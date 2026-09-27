# docs/wiki-core/be/18-trien-khai-va-van-hanh.md §1, §7 — MỘT artifact cho mọi môi trường; khác
# biệt giữa môi trường chỉ ở cấu hình đọc lúc chạy (biến môi trường), không ở bản build.
#
# CHƯA CHẠY THỬ TRONG MÔI TRƯỜNG NÀY — không có Docker daemon lúc viết (ghi trong báo cáo B3).
# Build từ GỐC REPO: docker build -f src/BE/CoreAndSkill.Api/Dockerfile -t coreandskill-api .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy trước phần khai package để tận dụng cache layer của Docker — đổi code không phải tải lại
# NuGet.
COPY src/BE/Directory.Build.props src/BE/Directory.Packages.props src/BE/CoreAndSkill.slnx ./src/BE/
COPY src/BE/CoreAndSkill.Api/CoreAndSkill.Api.csproj ./src/BE/CoreAndSkill.Api/
COPY src/BE/Core/CoreAndSkill.Core.Domain/CoreAndSkill.Core.Domain.csproj ./src/BE/Core/CoreAndSkill.Core.Domain/
COPY src/BE/Core/CoreAndSkill.Core.Application/CoreAndSkill.Core.Application.csproj ./src/BE/Core/CoreAndSkill.Core.Application/
COPY src/BE/Core/CoreAndSkill.Core.Infrastructure/CoreAndSkill.Core.Infrastructure.csproj ./src/BE/Core/CoreAndSkill.Core.Infrastructure/
COPY src/BE/Core/CoreAndSkill.Core.Web/CoreAndSkill.Core.Web.csproj ./src/BE/Core/CoreAndSkill.Core.Web/
COPY src/BE/Core/CoreAndSkill.Core.Contracts/CoreAndSkill.Core.Contracts.csproj ./src/BE/Core/CoreAndSkill.Core.Contracts/
RUN dotnet restore src/BE/CoreAndSkill.Api/CoreAndSkill.Api.csproj

COPY src/BE/ ./src/BE/
RUN dotnet publish src/BE/CoreAndSkill.Api/CoreAndSkill.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .

# Kho tệp (Core:File:RootPath — docs/wiki-core/be/14-file-storage.md §2): NGOÀI thư mục ứng dụng (/app), để
# triển khai lại không mất tệp. Thư mục phải có SẴN và thuộc tài khoản chạy tiến trình — cấu hình khởi động
# kiểm nó tồn tại, ghi được và không nằm trong thư mục ứng dụng, thiếu thì tiến trình không lên. docker-compose.yml
# gắn volume có tên vào đúng đường dẫn này; volume có tên mới rỗng thừa hưởng chủ sở hữu của thư mục trong image.
RUN mkdir -p /var/lib/coreandskill/files && chown $APP_UID /var/lib/coreandskill/files

# Không chạy bằng root — ngoài kho tệp ở trên, không cần quyền ghi vào hệ thống tệp cục bộ (log ra stdout,
# khoá bảo vệ dữ liệu nằm trong Postgres — docs/adr/0014-mot-instance-key-ring-postgres.md §2).
USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "CoreAndSkill.Api.dll"]
