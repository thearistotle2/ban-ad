using System.ComponentModel.DataAnnotations;

namespace BanAd.ViewModels;

public class AdvertiseInputViewModel
{
    
    [Required]
    public string Id { get; set; }
    
    [Range(0, int.MaxValue)]
    public int? Days { get; set; }
    
    [Required, Range(1, int.MaxValue)]
    public int Hours { get; set; }
    
    // Very simple: just make sure there is exactly one @ and it is surrounded by other characters.
    [Required, MinLength(3), RegularExpression(@"^[^@]+@[^@]+$")]
    public string Email { get; set; }
    
    [Required]
    public IFormFile Ad { get; set; }
    
}