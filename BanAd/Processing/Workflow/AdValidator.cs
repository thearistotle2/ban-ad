using BanAd.Config;
using BanAd.ViewModels;
using SixLabors.ImageSharp;

namespace BanAd.Processing.Workflow;

public class AdValidator
{
    
    private AdSlotsMonitor AdSlots { get; }
    private RunOptions Config { get; }

    public AdValidator(AdSlotsMonitor adSlots, RunOptions config)
    {
        AdSlots = adSlots;
        Config = config;
    }

    public async Task<(bool Valid, string? Error)> Validate(AdvertiseInputViewModel model)
    {
        // All other checks are already validated by Model.IsValid in the controller.
        if (!AdSlots.Value.Ads.ContainsKey(model.Id))
        {
            return (false, "Unknown ad slot.");
        }

        if (model.Ad == null)
        {
            return (false, "No ad provided.");
        }

        if (!Config.SupportedExtensions.Contains(Path.GetExtension(model.Ad.FileName)))
        {
            return (false, "Unsupported extension.");
        }

        await using var stream = model.Ad.OpenReadStream();
        if (stream.Length > Config.MaxUploadSizeKiB * 1024)
        {
            return (false, "Ad file too large.");
        }

        var adSlot = AdSlots.Value.Ads[model.Id];
        using var image = await Image.LoadAsync(stream);
        if (image.Width != adSlot.Width || image.Height != adSlot.Height)
        {
            return (false, $"Image must be {adSlot.Width} pixels wide x {adSlot.Height} pixels high.");
        }
            
        return (true, null);
    }
    
}