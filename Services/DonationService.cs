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

    public async Task<List<DonationResponse>> GetAllAsync()
    {
        var donations = await _db.Donations
            .AsNoTracking()
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return donations.Select(ToResponse).ToList();
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
    // Every endpoint goes through here, so an anonymous name can never leak.
    private static DonationResponse ToResponse(Donation d) => new()
    {
        Id = d.Id,
        Amount = d.Amount,
        DonorName = d.IsAnonymous ? AnonymousName : d.DonorName,
        IsAnonymous = d.IsAnonymous,
        CreatedAt = d.CreatedAt
    };
}