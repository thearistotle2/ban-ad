using BanAd.Config;

namespace BanAd.ViewModels;

public class AdvertiseViewModel
{
    public string AdSlotId { get; set; }
    public AdSlot AdSlotInfo { get; set; }
    public IEnumerable<string> SupportedExtensions { get; set; }
    public string MaxSize { get; set; }
}