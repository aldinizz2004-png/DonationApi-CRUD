namespace DonationApi.DTOs;

public class DonationResponse
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string DonorName { get; set; } = string.Empty;   // "Anonymous" if IsAnonymous = true
    public bool IsAnonymous { get; set; }
    public DateTime CreatedAt { get; set; }
}