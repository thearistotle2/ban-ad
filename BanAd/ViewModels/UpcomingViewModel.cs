using BanAd.Processing.Entities;

namespace BanAd.ViewModels;

public class UpcomingViewModel
{
    public string SiteId { get; set; }
    public string AdSlotId { get; set; }
    public string AdBaseUrl { get; set; }
    public QueuedAds Upcoming { get; set; }
}