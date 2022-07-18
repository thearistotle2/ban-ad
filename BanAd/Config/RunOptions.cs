namespace BanAd.Config;

public class RunOptions
{
    public string SiteId { get; set; }
    
    public string AdSlotDeclarations { get; set; }
    public string AdsLocation { get; set; }
    
    public IEnumerable<string> SupportedExtensions { get; set; }
    public int MaxUploadSizeKiB { get; set; }
}