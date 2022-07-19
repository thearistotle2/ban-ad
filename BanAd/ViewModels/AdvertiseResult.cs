namespace BanAd.ViewModels;

public class AdvertiseResult
{
    public bool Success { get; set; }
    public IEnumerable<string> Errors { get; set; }
}