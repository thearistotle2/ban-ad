namespace BanAd.ViewModels;

public class AdvertiseInputViewModel
{
    public string Id { get; set; }
    public int? Days { get; set; }
    public int Hours { get; set; }
    public string Email { get; set; }
    public IFormFile Ad { get; set; }
}