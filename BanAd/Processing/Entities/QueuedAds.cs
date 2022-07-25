namespace BanAd.Processing.Entities;

public class QueuedAds
{
    public (string Link, DateTime Expires)? Current { get; set; }
    public IEnumerable<(string Id, string Link, int Hours)>? Upcoming { get; set; }
}