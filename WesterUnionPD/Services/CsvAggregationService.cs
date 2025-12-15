using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text.RegularExpressions;
using WesterUnionPD.Models;
using WesterUnionPD.Data;

namespace WesterUnionPD.Services;

/// <summary>
/// Streaming CSV aggregation (1M+ rows safe).
/// Any invalid row FAILS the job.
/// </summary>
public sealed class CsvAggregationService : ICsvAggregationService
{
    private static readonly Regex AbdRegex = new(@"^ABD\d{6}$");

    public async Task<List<BranchSummary>> AggregateAsync(
        Stream csvStream,
        UploadJob job,
        AppDbContext db,
        CancellationToken ct)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            IgnoreBlankLines = true
        };

        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, config);

        var dict = new Dictionary<string, (decimal c, decimal f)>();
        long processed = 0;

        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();
            processed++;

            var account = csv.GetField("Account")?.Trim();
            if (account == null || !AbdRegex.IsMatch(account))
                throw new InvalidDataException("Invalid ABD code.");

            decimal charges = Parse(csv.GetField("ShareOfChargesLOC"));
            decimal fx = Parse(csv.GetField("ShareOfFXLOC"));

            if (dict.TryGetValue(account, out var agg))
                dict[account] = (agg.c + charges, agg.f + fx);
            else
                dict[account] = (charges, fx);

            if (processed % 5000 == 0)
            {
                job.ProcessedRows = processed;
                await db.SaveChangesAsync(ct);
            }
        }

        job.ProcessedRows = processed;
        await db.SaveChangesAsync(ct);

        return dict.Select(x => new BranchSummary
        {
            UploadJobId = job.Id,
            AbdCode = x.Key,
            ChargesLOC = x.Value.c,
            FxLOC = x.Value.f
        }).ToList();
    }

    private static decimal Parse(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return 0;
        v = v.Replace(",", "");

        if (!decimal.TryParse(v, out var d))
            throw new InvalidDataException("Invalid numeric value.");

        return d;
    }
}
