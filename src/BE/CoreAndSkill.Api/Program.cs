using CoreAndSkill.Core.Web.Commands;
using CoreAndSkill.Core.Web.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// builder.Services.AddSkillModule(builder.Configuration);   // mỗi module một dòng — TRƯỚC AddCore (§5.1)
builder.Services.AddCore(builder.Configuration, builder.Environment);

var app = builder.Build();

// Nhường dòng lệnh cho runner Core — docs/quy-uoc/be-architecture.md §3.
if (await app.RunCoreCommandAsync(args)) return;

await app.UseCoreAsync();
// app.UseSkillModule();

app.Run();

// Cho phép WebApplicationFactory<Program> trong CoreAndSkill.Core.IntegrationTests — không có logic khác.
public partial class Program;
