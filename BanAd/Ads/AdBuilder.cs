using System.Collections.Concurrent;
using BanAd.Config;
using BanAd.Processing.Connectors;
using BanAd.ViewModels;
using Microsoft.AspNetCore.StaticFiles;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using File = SLSAK.Docker.IO.File;

namespace BanAd.Ads;

public class AdBuilder
{
     
    private AdSlotsMonitor AdSlots { get; }
    private FileConnector Files { get; }
    private RunOptions Config { get; }
    private FileExtensionContentTypeProvider MimeTypeProvider { get; } = new();

    public AdBuilder(AdSlotsMonitor adSlots, FileConnector files, RunOptions config)
    {
        AdSlots = adSlots;
        Files = files;
        Config = config;
    }

    public Ad BuildAd(string adSlotId)
    {
        // If we have an ad, serve it.
        var (file, link) = Files.CurrentAd(adSlotId);
        if (file != null)
        {
            return BuildAdFromFile(file, link);
        }
        
        // Otherwise, serve a default ad.
        return BuildDefault(adSlotId);
    }

    public Ad? BuildAd(string adSlotId, string paidAdId)
    {
        var file = Files.PaidAd(adSlotId, paidAdId);
        if (file != null)
        {
            return BuildAdFromFile(file, null);
        }

        return null;
    }

    private Ad BuildAdFromFile(string file, string link)
    {
        return new Ad
        {
            Link = () => link,
            Image = () => File.ReadAllBytes(file),
            MimeType = () =>
            {
                MimeTypeProvider.TryGetContentType(file, out string type);
                return type ?? "image/png";
            }
        };
    }

    private Ad BuildDefault(string adSlotId)
    {
        var advertise = $"{Config.SiteBaseUrl}/banad/advertise/{adSlotId}";
        
        if (AdSlots.Value.Ads.ContainsKey(adSlotId))
        {
            var adSlot = AdSlots.Value.Ads[adSlotId];
            if (adSlot.DefaultImage != null)
            {
                return BuildAdFromFile(adSlot.DefaultImage, adSlot.DefaultLink ?? advertise);
            }
            
            return new Ad
            {
                Link = () => advertise,
                Image = () => BuildDefaultDefault((int)adSlot.Width, (int)adSlot.Height),
                MimeType = () => "image/png"
            };
        }
        
        return new Ad
        {
            Link = () => advertise,
            Image = Array.Empty<byte>,
            MimeType = () => "image/png"
        };
    }

    private static ConcurrentDictionary<(int Width, int Height), byte[]> DefaultDefaults { get; }= new();
    private byte[] BuildDefaultDefault(int width, int height)
    {
        if (DefaultDefaults.ContainsKey((width, height)))
        {
            return DefaultDefaults[(width, height)];
        }
        
        using var defaultImage = Image.Load("wwwroot/default/default.png");
        using var image = new Image<Rgba32>(width, height, Color.White);
        image.Mutate(context =>
            context.DrawImage(
                defaultImage,
                new Point(width - defaultImage.Width, height - defaultImage.Height),
                1
            )
        );
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        stream.Seek(0, SeekOrigin.Begin);
        DefaultDefaults[(width, height)] = stream.ToArray();
        
        return DefaultDefaults[(width, height)];
    }
    
}