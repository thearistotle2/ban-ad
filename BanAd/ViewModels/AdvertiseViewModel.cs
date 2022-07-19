using BanAd.Config;

namespace BanAd.ViewModels;

public class AdvertiseViewModel
{
    public string AdSlotId { get; set; }
    public AdSlot AdSlotInfo { get; set; }
    public IEnumerable<string> SupportedExtensions { get; set; }
    public int MaxSizeKiB { get; set; }
    public string MaxSizeDisplay { get; set; }
    public string HoneypotName { get; set; }
}