using System.ComponentModel.DataAnnotations;

namespace DonationApi.DTOs;

public class UpdateDonationRequest
{
    [Required]
    [Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "Amount must be greater than 0 and at most 1,000,000.")]
    public decimal? Amount { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string? DonorName { get; set; }

    [Required]
    public bool? IsAnonymous { get; set; }
}