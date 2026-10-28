using System.Linq.Expressions;
using DonationApi.Data;
using DonationApi.DTOs;
using DonationApi.Models;
using Microsoft.EntityFrameworkCore;

namespace DonationApi.Services;

public class DonationService : IDonationService
{
    private const string AnonymousName = "Anonymous";
    private readonly AppDbContext _db;

    public DonationService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Keyset (cursor) pagination, newest or oldest first.
    /// One index (CreatedAt DESC, Id DESC) serves the filter, the order and the cursor seek,
    /// so every page costs the same, and new donations never cause duplicates between pages.
    /// </summary>
    public async Task<CursorPage<DonationResponse>> GetPageAsync(DonationPageQuery q, CancellationToken ct = default)
    {
        IQueryable<Donation> query = _db.Donations.AsNoTracking();

        // 1) Filters (before the cursor, so totalCount and the pages describe the same set)
        if (q.From is { } from)
        {
            var fromUtc = DateUtc.ToUtc(from);
            query = query.Where(d => d.CreatedAt >= fromUtc);
        }
        if (q.To is { } to)
        {
            var toUtc = DateUtc.ToUtc(to);
            query = query.Where(d => d.CreatedAt <= toUtc);
        }
        if (q.MinAmount is { } minAmount)
        {
            query = query.Where(d => d.Amount >= minAmount);
        }

        // 2) Optional total (one COUNT with the same filters) - only when the client asks for it
        int? totalCount = q.IncludeTotal ? await query.CountAsync(ct) : null;

        // 3) Keyset: continue strictly after the last row the client saw.
        //    Id is the tie-breaker when two donations have the same CreatedAt.
        var newestFirst = q.Sort == DonationSort.Newest;

        if (DonationCursor.TryDecode(q.Cursor, out var cursor))
        {
            var lastCreatedAt = cursor.CreatedAt;
            var lastId = cursor.Id;

            query = newestFirst
                ? query.Where(d => d.CreatedAt < lastCreatedAt || (d.CreatedAt == lastCreatedAt && d.Id < lastId))
                : query.Where(d => d.CreatedAt > lastCreatedAt || (d.CreatedAt == lastCreatedAt && d.Id > lastId));
        }

        // 4) Deterministic order: CreatedAt, then Id (unique) - same direction as the index
        query = newestFirst
            ? query.OrderByDescending(d => d.CreatedAt).ThenByDescending(d => d.Id)
            : query.OrderBy(d => d.CreatedAt).ThenBy(d => d.Id);

        // 5) Read one extra row to know if another page exists; project to the DTO in SQL
        var items = await query
            .Take(q.PageSize + 1)
            .Select(ToResponseExpression)
            .ToListAsync(ct);

        var hasNextPage = items.Count > q.PageSize;
        if (hasNextPage) items.RemoveAt(items.Count - 1);

        var last = items.LastOrDefault();

        return new CursorPage<DonationResponse>
        {
            Items = items,
            PageSize = q.PageSize,
            HasNextPage = hasNextPage,
            NextCursor = hasNextPage && last is not null
                ? DonationCursor.Encode(last.CreatedAt, last.Id, q.Sort)
                : null,
            TotalCount = totalCount
        };
    }

    public async Task<DonationResponse?> GetByIdAsync(int id)
    {
        var donation = await _db.Donations
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        return donation is null ? null : ToResponse(donation);
    }

    public async Task<DonationResponse> CreateAsync(CreateDonationRequest request)
    {
        var donation = new Donation
        {
            Amount = request.Amount!.Value,
            DonorName = request.DonorName!.Trim(),
            IsAnonymous = request.IsAnonymous!.Value,
            CreatedAt = DateTime.UtcNow
        };

        _db.Donations.Add(donation);
        await _db.SaveChangesAsync();

        return ToResponse(donation);
    }

    public async Task<DonationResponse?> UpdateAsync(int id, UpdateDonationRequest request)
    {
        var donation = await _db.Donations.FindAsync(id);
        if (donation is null) return null;

        donation.Amount = request.Amount!.Value;
        donation.DonorName = request.DonorName!.Trim();
        donation.IsAnonymous = request.IsAnonymous!.Value;
        // CreatedAt is never changed on update

        await _db.SaveChangesAsync();

        return ToResponse(donation);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var donation = await _db.Donations.FindAsync(id);
        if (donation is null) return false;

        _db.Donations.Remove(donation);
        await _db.SaveChangesAsync();
        return true;
    }

    // The ONLY place an entity becomes an API response.
    // As an expression it runs inside SQL for the list (only the needed columns, masking done in the query);
    // compiled, it maps single entities. Either way an anonymous name can never leak.
    private static readonly Expression<Func<Donation, DonationResponse>> ToResponseExpression = d => new DonationResponse
    {
        Id = d.Id,
        Amount = d.Amount,
        DonorName = d.IsAnonymous ? AnonymousName : d.DonorName,
        IsAnonymous = d.IsAnonymous,
        CreatedAt = d.CreatedAt
    };

    private static readonly Func<Donation, DonationResponse> ToResponse = ToResponseExpression.Compile();
}
