﻿using System.Collections.Concurrent;
using BanAd.Ads;
using BanAd.Config;
using BanAd.Processing.Workflow;
using BanAd.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;

namespace BanAd.Controllers;

[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public class BanAdController : Controller
{
    private AdBuilder AdBuilder { get; }
    private AdSubmissionProcessor Processor { get; }
    private AdSlotsMonitor AdSlots { get; }
    private RunOptions Config { get; }
    private static ConcurrentDictionary<string, DateTime> TimeTracker { get; } = new();

    public BanAdController(
        AdBuilder adBuilder,
        AdSubmissionProcessor processor,
        AdSlotsMonitor adSlots,
        RunOptions config)
    {
        AdBuilder = adBuilder;
        Processor = processor;
        AdSlots = adSlots;
        Config = config;
    }

    #region " AdSlot Management "

    public IActionResult NewId()
    {
        var id = $"{Guid.NewGuid().GetHashCode():X}";
        return Content(id);
    }
    
    public IActionResult Upcoming(string id)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region " Ad Serving "

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

    #endregion

    #region " New Ads "

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
            SiteId = Config.SiteId,
            AdSlotId = id,
            AdSlotInfo = adSlot,
            SupportedExtensions = Config.SupportedExtensions,
            MaxSizeKiB = Config.MaxUploadSizeKiB,
            MaxSizeDisplay = mb.Remainder == 0 ? $"{mb.Quotient}mb" : $"{Config.MaxUploadSizeKiB}kb",
            HoneypotName = Config.BotHoneypotName
        });
    }

    [HttpPost]
    public async Task<IActionResult> Advertise([FromForm] AdvertiseInputViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Check for bot control measures.
            if (IsLikelyBot())
            {
                // Slow the bot down a bit.
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
            else
            {
                // We have data and we don't think it's from a bot.  Let's go!
                return Json(await Processor.ProcessSubmission(model));
            }

            return Json(new AdvertiseResult { Success = true });
        }
        else
        {
            return Json(
                new AdvertiseResult
                {
                    Success = false,
                    Errors = ModelState.Values
                        .Where(val => val.ValidationState == ModelValidationState.Invalid)
                        .SelectMany(val => val.Errors.Select(err => err))
                        .Select(err => err.ErrorMessage)
                }
            );
        }
    }

    private bool IsLikelyBot()
    {
        // Did not submit the honeypot.
        if (!Request.Form.TryGetValue(Config.BotHoneypotName, out StringValues honeypot))
        {
            Console.WriteLine($@"Connection Id {Request.HttpContext.Connection.Id} determined to be a bot:
Honeypot field '{Config.BotHoneypotName}' not submitted.");
            return true;
        }

        // Submitted a valued honeypot.
        if (!string.IsNullOrWhiteSpace(honeypot))
        {   
            Console.WriteLine($@"Connection Id {Request.HttpContext.Connection.Id} determined to be a bot:
Honeypot field '{Config.BotHoneypotName}' submitted with value ('{honeypot}').");
            return true;
        }
        
        // Did not start on the page.
        if (!TimeTracker.ContainsKey(Request.HttpContext.Connection.Id))
        {
            Console.WriteLine($@"Connection Id {Request.HttpContext.Connection.Id} determined to be a bot:
Connection Id not in time tracker, so the request did not start on this page.");
            return true;
        }
        
        // Submitted the page too quickly.
        var timespan = DateTime.UtcNow - TimeTracker[Request.HttpContext.Connection.Id]; 
        if (timespan < TimeSpan.FromSeconds(Config.BotMinSeconds))
        {
            Console.WriteLine($@"Connection Id {Request.HttpContext.Connection.Id} determined to be a bot:
Page submitted after {timespan}, which is quicker than the required {Config.BotMinSeconds} seconds.");
            return true;
        }
        
        return false;
    }

    #endregion
}