namespace BanAd.Config;

public class AdSlot
{
    public uint BanPerHour { get; set; }
    public uint Width { get; set; }
    public uint Height { get; set; }
    public string? DefaultImage { get; set; }
    public string? DefaultLink { get; set; }
    public string? BanAddress { get; set; }
}