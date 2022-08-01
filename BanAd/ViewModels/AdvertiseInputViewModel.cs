using System.ComponentModel.DataAnnotations;

namespace BanAd.ViewModels;

public class AdvertiseInputViewModel
{
    
    [Required]
    public string Id { get; set; }
    
    [Range(0, 9999)]
    public int? Days { get; set; }
    
    [Range(0, 9999)]
    public int? Hours { get; set; }
    
    // Very simple: just make sure there is exactly one @ and it is surrounded by other characters.
    [Required, MinLength(3), RegularExpression(@"^[^@]+@[^@]+$")]
    public string Email { get; set; }
    
    // Very simple: just make sure there is http, ://, and at least one other character.
    [Required, MinLength(8), RegularExpression(@"^[Hh][Tt][Tt][Pp][Ss]?:\/\/.+$")]
    public string Link { get; set; }
    
    [Required]
    public IFormFile Ad { get; set; }
    
}