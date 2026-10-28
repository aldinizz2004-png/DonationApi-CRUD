using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace DonationApi.DTOs;

/// <summary>Sort orders allowed for the donations list (whitelist; both use the same index).</summary>
public enum DonationSort
{
    Newest,
    Oldest
}

/// <summary>
/// Query string of GET /api/donations.
/// Example: ?pageSize=20&amp;sort=newest&amp;from=2026-01-01&amp;minAmount=50&amp;cursor=...
/// Invalid values return 400 automatically ([ApiController]).
/// </summary>
public class DonationPageQuery : IValidatableObject
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>Items per page (1-100, default 20).</summary>
    [Range(1, MaxPageSize, ErrorMessage = "pageSize must be between 1 and 100.")]
    [FromQuery(Name = "pageSize")]
    public int PageSize { get; set; } = DefaultPageSize;

    /// <summary>The nextCursor value from the previous page. Omit it for the first page.</summary>
    [MaxLength(200)]
    [FromQuery(Name = "cursor")]
    public string? Cursor { get; set; }

    /// <summary>newest (default) or oldest.</summary>
    [EnumDataType(typeof(DonationSort))]
    [FromQuery(Name = "sort")]
    public DonationSort Sort { get; set; } = DonationSort.Newest;

    /// <summary>Only donations created at or after this time (UTC).</summary>
    [FromQuery(Name = "from")]
    public DateTime? From { get; set; }

    /// <summary>Only donations created at or before this time (UTC).</summary>
    [FromQuery(Name = "to")]
    public DateTime? To { get; set; }

    /// <summary>Only donations with Amount greater than or equal to this value.</summary>
    [Range(typeof(decimal), "0.01", "1000000000", ErrorMessage = "minAmount must be greater than 0.")]
    [FromQuery(Name = "minAmount")]
    public decimal? MinAmount { get; set; }

    /// <summary>true = also return totalCount (one extra COUNT query). Default false.</summary>
    [FromQuery(Name = "includeTotal")]
    public bool IncludeTotal { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From.HasValue && To.HasValue && DateUtc.ToUtc(From.Value) > DateUtc.ToUtc(To.Value))
        {
            yield return new ValidationResult(
                "'from' must be earlier than or equal to 'to'.",
                new[] { nameof(From), nameof(To) });
        }

        if (Cursor is not null)
        {
            if (!DonationCursor.TryDecode(Cursor, out var cursor))
            {
                yield return new ValidationResult(
                    "The cursor is invalid. Use the nextCursor value returned by the previous page.",
                    new[] { nameof(Cursor) });
            }
            else if (cursor.Sort != Sort)
            {
                yield return new ValidationResult(
                    $"This cursor was created for sort '{cursor.Sort}'. Use the same sort, or start again without a cursor.",
                    new[] { nameof(Cursor) });
            }
        }
    }
}

/// <summary>Query-string dates may arrive as Local or Unspecified; the database stores UTC.</summary>
public static class DateUtc
{
    public static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)   // no zone given -> treat as UTC
    };
}
