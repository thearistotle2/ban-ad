namespace BanAd.Config;

public class AdSlot
{
    public int BanPerHour { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string? DefaultImage { get; set; }
    public string? DefaultLink { get; set; }
}