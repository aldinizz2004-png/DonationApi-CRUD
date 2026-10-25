using DonationApi.DTOs;

namespace DonationApi.Services;

public interface IDonationService
{
    Task<List<DonationResponse>> GetAllAsync();
    Task<DonationResponse?> GetByIdAsync(int id);
    Task<DonationResponse> CreateAsync(CreateDonationRequest request);
    Task<DonationResponse?> UpdateAsync(int id, UpdateDonationRequest request);
    Task<bool> DeleteAsync(int id);
}