using System.Text.RegularExpressions;
using CoreAndSkill.Core.Application.Common.Logging;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Diagnostics;
using CoreAndSkill.Core.Application.Export;
using CoreAndSkill.Core.Application.Files;
using CoreAndSkill.Core.Application.Import;
using CoreAndSkill.Core.Application.Jobs;
using CoreAndSkill.Core.Application.Notifications;
using CoreAndSkill.Core.Application.Outbox;
using CoreAndSkill.Core.Application.Tabular;
using CoreAndSkill.Core.Application;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Domain.Files;
using CoreAndSkill.Core.Domain.Jobs;
using CoreAndSkill.Core.Domain.Outbox;
using CoreAndSkill.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Common;

// Lớp này ghi ảnh chụp outbox tĩnh (`CoreMetrics.SetOutboxSnapshot`) — xem `Tests/Shared/MeterProbe.cs`.
[Collection(OutboxSnapshotCollection.Name)]
public partial class B4SupportTests
{
    // ---- Khuôn mã lỗi (be-cqrs-handler.md §7.3): MIỀN.TÀI_NGUYÊN.LÝ_DO, duy nhất toàn hệ ----------------

    [GeneratedRegex(@"^[A-Z][A-Z0-9]*(\.[A-Z][A-Z0-9_]*){2}$")]
    private static partial Regex CodePattern();

    private static IEnumerable<Error> B4Errors()
    {
        var catalogs = new[]
        {
            typeof(FileErrors), typeof(FileDomainErrors), typeof(JobErrors), typeof(JobStateErrors), typeof(OutboxErrors),
            typeof(OutboxHandlingErrors), typeof(NotificationErrors), typeof(ImportErrors), typeof(ExportErrors),
        };

        return catalogs.SelectMany(type => type
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(Error))
            .Select(f => (Error)f.GetValue(null)!));
    }

    [Fact]
    public void B4ErrorCatalogs_ArePopulated()
        => B4Errors().Count().ShouldBeGreaterThan(20);

    [Fact]
    public void B4ErrorCodes_MatchTheDocumentedFormat()
        => B4Errors().Where(e => !CodePattern().IsMatch(e.Code)).Select(e => e.Code).ShouldBeEmpty();

    [Fact]
    public void B4ErrorCodes_AreUnique_AlsoAgainstTheCommonCatalog()
    {
        // Mã dùng chung đi kèm vì JobErrors nay có một mục TÊN GIỐNG — JobErrors.Unexpected
        // (CORE.JOB.UNEXPECTED, ADR-0077). Hai mã anh em trùng chuỗi thì log mất nghĩa.
        var codes = B4Errors().Select(e => e.Code).Append(CoreAndSkill.Core.Application.Common.CommonErrors.Unexpected.Code).ToList();

        codes.GroupBy(c => c).Where(g => g.Count() > 1).Select(g => g.Key).ShouldBeEmpty();
    }

    [Fact]
    public void B4Errors_AreNeverTheTwoTypesOnlyInfrastructureMayEmit()
    {
        // Luật R9: ErrorType.Unauthorized / Unexpected chỉ hạ tầng được phát.
        B4Errors().Select(e => e.Type).ShouldNotContain(ErrorType.Unauthorized);
        B4Errors().Select(e => e.Type).ShouldNotContain(ErrorType.Unexpected);
    }

    [Theory]
    [InlineData("CORE.FILE.TOO_LARGE", ErrorType.Validation)]
    [InlineData("CORE.FILE.TYPE_NOT_ALLOWED", ErrorType.Validation)]
    [InlineData("CORE.FILE.NOT_FOUND", ErrorType.NotFound)]
    [InlineData("CORE.JOB.NOT_FOUND", ErrorType.NotFound)]
    [InlineData("CORE.JOB.NO_EXECUTOR", ErrorType.BusinessRule)]
    [InlineData("CORE.JOB.INTERRUPTED", ErrorType.BusinessRule)]
    [InlineData("CORE.JOB.UNEXPECTED", ErrorType.BusinessRule)]
    [InlineData("CORE.NOTIFICATION.NOT_FOUND", ErrorType.NotFound)]
    [InlineData("CORE.EXPORT.TOO_MANY_ROWS", ErrorType.BusinessRule)]
    [InlineData("CORE.IMPORT.TOO_MANY_ROWS", ErrorType.BusinessRule)]
    [InlineData("CORE.IMPORT.COLUMNS_MISMATCH", ErrorType.Validation)]
    public void CardCodes_CarryTheTypeTheContractCardsDeclare(string code, ErrorType type)
        => B4Errors().Single(e => e.Code == code).Type.ShouldBe(type);

    // ---- OutboxEnvelope -----------------------------------------------------------------------

    private sealed record Sample(Guid Id, string Name);

    [Fact]
    public void ReadPayload_ReadsCamelCaseJson_IntoAPascalCaseRecord()
    {
        var id = Guid.NewGuid();
        var envelope = new OutboxEnvelope(Guid.NewGuid(), Guid.NewGuid(), "t.v1", $"{{\"id\":\"{id}\",\"name\":\"an\"}}", DateTimeOffset.UnixEpoch, null, null, null);

        envelope.ReadPayload<Sample>().ShouldBe(new Sample(id, "an"));
    }

    [Fact]
    public void ReadPayload_InvalidJson_IsNullNotAnException()
    {
        var envelope = new OutboxEnvelope(Guid.NewGuid(), Guid.NewGuid(), "t.v1", "{not json", DateTimeOffset.UnixEpoch, null, null, null);

        envelope.ReadPayload<Sample>().ShouldBeNull();
    }

    // ---- FailureText --------------------------------------------------------------------------

    [Fact]
    public void FailureText_KeepsTheTypeAndTheMessage_ButRedactsSecretsAndTruncates()
    {
        var text = FailureText.Describe(new InvalidOperationException("password: Abc@12345 rejected"));

        text.ShouldStartWith("InvalidOperationException: ");
        text.ShouldNotContain("Abc@12345");

        FailureText.Describe(new Exception(new string('x', 2000))).Length.ShouldBe(FailureText.MaxLength);
    }

    [Fact]
    public void FailureText_NeverIncludesTheStackTrace()
    {
        Exception thrown;
        try
        {
            throw new InvalidOperationException("boom");
        }
        catch (Exception ex)
        {
            thrown = ex;
        }

        FailureText.Describe(thrown).ShouldNotContain(" at ");
        FailureText.Describe(thrown).ShouldNotContain(nameof(FailureText_NeverIncludesTheStackTrace));
    }

    // ---- CoreMetrics: ảnh chụp outbox ----------------------------------------------------------

    [Fact]
    public void OutboxSnapshot_IsExposedThroughObservableGauges_WithoutAnyDatabaseQuery()
    {
        using var probe = new MeterProbe(CoreMetrics.OutboxPending, CoreMetrics.OutboxOldestPendingAge, CoreMetrics.OutboxDeadCount);

        CoreMetrics.SetOutboxSnapshot(pendingCount: 3, oldestPendingAgeSeconds: 42.5, deadCount: 2);
        probe.RecordObservableInstruments();

        probe.Value("core.outbox.pending").ShouldBe(3);
        probe.Value("core.outbox.pending_oldest_age").ShouldBe(42.5);
        probe.Value("core.outbox.dead_count").ShouldBe(2);
    }

    [Fact]
    public void OutboxAndFileCounters_AreObservableThroughAMeterListener()
    {
        using var probe = new MeterProbe(
            CoreMetrics.OutboxDispatched, CoreMetrics.OutboxAttemptFailed, CoreMetrics.OutboxDead,
            CoreMetrics.FileOrphansDeleted, CoreMetrics.FileContentMissing, CoreMetrics.FileUnattachedExpired);

        CoreMetrics.OutboxDispatched.Add(1);
        CoreMetrics.OutboxAttemptFailed.Add(1);
        CoreMetrics.OutboxDead.Add(1);
        CoreMetrics.FileOrphansDeleted.Add(1);
        CoreMetrics.FileContentMissing.Add(1);
        CoreMetrics.FileUnattachedExpired.Add(1);

        probe.Names.ShouldBe(
            ["core.outbox.dispatched", "core.outbox.attempt_failed", "core.outbox.dead", "core.file.orphans_deleted", "core.file.content_missing", "core.file.unattached_expired"],
            ignoreOrder: true);
    }

    // ---- Cấu hình -----------------------------------------------------------------------------

    [Fact]
    public void Options_SectionNames_MatchTheDocumentedKeys()
    {
        CoreFileOptions.SectionName.ShouldBe("Core:File");
        CoreExportOptions.SectionName.ShouldBe("Core:Export");
        CoreImportOptions.SectionName.ShouldBe("Core:Import");
        CoreJobOptions.SectionName.ShouldBe("Core:Jobs");
        CoreOutboxOptions.SectionName.ShouldBe("Core:Outbox");
        CoreNotificationOptions.SectionName.ShouldBe("Core:Notification");
    }

    [Fact]
    public void Options_Defaults_MatchTheDocuments()
    {
        new CoreExportOptions().MaxRows.ShouldBe(50_000);
        new CoreImportOptions().MaxRows.ShouldBe(50_000);
        new CoreJobOptions().MaxConcurrent.ShouldBe(2);
        new CoreOutboxOptions().MaxAttempts.ShouldBe(OutboxRetryPolicy.DefaultMaxAttempts);
        new CoreOutboxOptions().DegradedPendingAgeMinutes.ShouldBe(15);
        new CoreNotificationOptions().DefaultLanguage.ShouldBe("vi");
        new CoreFileOptions().MaxUploadMb.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void FileOptions_HaveNoDefaultRootPath_SoAMissingKeyStopsTheProcess()
        => new CoreFileOptions().RootPath.ShouldBeNull();

    [Theory]
    [InlineData(typeof(CoreFileOptions), "MaxUploadMb", 0)]
    [InlineData(typeof(CoreFileOptions), "MaxUploadMb", 2000)]
    [InlineData(typeof(CoreExportOptions), "MaxRows", 0)]
    [InlineData(typeof(CoreImportOptions), "MaxRows", 0)]
    [InlineData(typeof(CoreJobOptions), "MaxConcurrent", 0)]
    [InlineData(typeof(CoreOutboxOptions), "PollSeconds", 0)]
    [InlineData(typeof(CoreOutboxOptions), "MaxAttempts", 0)]
    [InlineData(typeof(CoreFileOptions), "UnattachedRetentionHours", 0)]
    public void Options_RejectOutOfRangeValues_AtStartup(Type optionsType, string property, int value)
    {
        var instance = Activator.CreateInstance(optionsType)!;
        // Các Options dùng `init` — đặt bằng reflection, cùng cách ConfigurationBinder làm.
        optionsType.GetProperty(property)!.SetValue(instance, value);
        if (optionsType == typeof(CoreFileOptions))
            optionsType.GetProperty(nameof(CoreFileOptions.RootPath))!.SetValue(instance, "x");

        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var valid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            instance, new System.ComponentModel.DataAnnotations.ValidationContext(instance), results, validateAllProperties: true);

        valid.ShouldBeFalse();
    }

    [Theory]
    [InlineData("vi", true)]
    [InlineData("en-US", true)]
    [InlineData("VI", false)]
    [InlineData("vietnamese", false)]
    [InlineData("", false)]
    public void NotificationOptions_DefaultLanguage_FollowsTheProfileLanguagePattern(string language, bool valid)
    {
        var options = new CoreNotificationOptions();
        typeof(CoreNotificationOptions).GetProperty(nameof(CoreNotificationOptions.DefaultLanguage))!.SetValue(options, language);

        System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            options, new System.ComponentModel.DataAnnotations.ValidationContext(options), [], validateAllProperties: true).ShouldBe(valid);
    }

    // ---- Đăng ký DI của Application ------------------------------------------------------------

    [Fact]
    public void AddCoreApplication_RegistersTheB4Pieces()
    {
        var services = new ServiceCollection();
        services.AddCoreApplication();

        bool Has<T>() => services.Any(d => d.ServiceType == typeof(T));

        Has<FilePurposeCatalog>().ShouldBeTrue();
        Has<FileAccessPolicy>().ShouldBeTrue();
        Has<IFileAttachment>().ShouldBeTrue();
        Has<JobRunner>().ShouldBeTrue();
        Has<INotificationPublisher>().ShouldBeTrue();
        Has<INotificationPreferences>().ShouldBeTrue();
        Has<INotificationTemplateRenderer>().ShouldBeTrue();

        services.Where(d => d.ServiceType == typeof(IOutboxEventHandler)).Count().ShouldBe(2);
        services.Where(d => d.ServiceType == typeof(INotificationChannel)).Count().ShouldBe(2);
        services.Where(d => d.ServiceType == typeof(IJobExecutor)).Count().ShouldBe(1);
        services.Where(d => d.ServiceType == typeof(IFileOwnerAccessChecker)).Count().ShouldBe(1);
    }

    [Fact]
    public void AddCoreApplication_TheDefaultsCanBeOverriddenByAProject_BecauseTheyUseTryAdd()
    {
        var services = new ServiceCollection();
        services.AddSingleton<INotificationPreferences, ProjectPreferences>(); // module đăng ký TRƯỚC AddCore
        services.AddCoreApplication();

        services.Where(d => d.ServiceType == typeof(INotificationPreferences)).ShouldHaveSingleItem()
            .ImplementationType.ShouldBe(typeof(ProjectPreferences));
    }

    private sealed class ProjectPreferences : INotificationPreferences
    {
        public Task<IReadOnlyCollection<Guid>> FilterEnabledAsync(
            IReadOnlyCollection<Guid> userIds, string code, NotificationChannels channel, CancellationToken ct)
            => Task.FromResult<IReadOnlyCollection<Guid>>([]);
    }

    [Fact]
    public void TabularFormatException_CarriesOnlyItsMessage()
        => new TabularFormatException("x").Message.ShouldBe("x");

    [Fact]
    public void ImportRowResult_OkAndFailedShapes()
    {
        ImportRowResult.Ok.IsSuccess.ShouldBeTrue();
        var failed = ImportRowResult.Failed(ImportErrors.RowNotSaved, "Email");

        failed.IsSuccess.ShouldBeFalse();
        failed.Field.ShouldBe("Email");
    }
}
