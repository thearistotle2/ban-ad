using System.Collections.Concurrent;
using BanAd.Ads;
using BanAd.Config;
using BanAd.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BanAd.Controllers;

[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public class BanAdController : Controller
{
    
    private AdBuilder AdBuilder { get; }
    private AdSlotsMonitor AdSlots { get; }
    private RunOptions Config { get; }
    private static ConcurrentDictionary<string, DateTime> TimeTracker { get; } = new ();

    public BanAdController(
        AdBuilder adBuilder,
        AdSlotsMonitor adSlots,
        RunOptions config)
    {
        AdBuilder = adBuilder;
        AdSlots = adSlots;
        Config = config;
    }

    public IActionResult NewId()
    {
        var id = $"{Guid.NewGuid().GetHashCode():X}";
        return Content(id);
    }

    public IActionResult Ad(string id)
    {
        var ad = AdBuilder.BuildAd(id);
        return File(ad.Image(), ad.MimeType());
    }

    public IActionResult Out(string id)
    {
        var ad = AdBuilder.BuildAd(id);
        return Redirect(ad.Link());
    }

    public IActionResult Advertise(string id)
    {
        if (!AdSlots.Value.Ads.ContainsKey(id))
        {
            return Content(string.Empty);
        }
        TimeTracker[Request.HttpContext.Connection.Id] = DateTime.UtcNow;
        
        var mb = Math.DivRem(Config.MaxUploadSizeKiB, 1024);
        var adSlot = AdSlots.Value.Ads[id];
        return View(new AdvertiseViewModel
        {
            AdSlotId = id,
            AdSlotInfo = adSlot,
            SupportedExtensions = Config.SupportedExtensions,
            MaxSizeKiB = Config.MaxUploadSizeKiB,
            MaxSizeDisplay = mb.Remainder == 0 ? $"{mb.Quotient}mb" : $"{Config.MaxUploadSizeKiB}kb",
            HoneypotName = Config.BotHoneypotName
        });
    }

}