using System.Collections.Concurrent;
using BanAd.Config;
using BanAd.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BanAd.Controllers;

public class BanAdController : Controller
{
    
    private AdSlotsMonitor AdSlots { get; }
    private RunOptions Config { get; }
    private static ConcurrentDictionary<string, DateTime> TimeTracker { get; } = new ();

    public BanAdController(AdSlotsMonitor adSlots, RunOptions config)
    {
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
        return new VirtualFileResult("~/TempImages/629B5C2E.png", "image/png");
    }

    public IActionResult Out(string id)
    {
        return Redirect("https://waxp.rentals");
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