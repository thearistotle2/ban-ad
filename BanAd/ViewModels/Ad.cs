namespace BanAd.ViewModels;

public class Ad
{
    public Func<byte[]> Image { get; set; }
    public Func<string> MimeType { get; set; }
    public Func<string> Link { get; set; }
}