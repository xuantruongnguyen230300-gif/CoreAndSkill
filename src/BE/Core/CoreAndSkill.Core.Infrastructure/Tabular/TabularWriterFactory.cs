using CoreAndSkill.Core.Application.Tabular;

namespace CoreAndSkill.Core.Infrastructure.Tabular;

internal sealed class TabularWriterFactory : ITabularWriterFactory
{
    public async Task<ITabularWriter> CreateAsync(
        TabularFormat format, Stream output, IReadOnlyList<string> headers, CancellationToken ct)
        => format switch
        {
            TabularFormat.Csv => await CsvTabularWriter.CreateAsync(output, headers, ct),
            TabularFormat.Xlsx => await XlsxTabularWriter.CreateAsync(output, headers, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };
}
