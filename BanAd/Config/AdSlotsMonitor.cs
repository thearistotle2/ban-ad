using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using SLSAK.Extensions;

namespace BanAd.Config;

public class AdSlotsMonitor
{

    public AdSlots Value => _rwls.SafeRead(() => _adSlots);

    private AdSlots _adSlots = new AdSlots { Ads = new Dictionary<string, AdSlot>(StringComparer.OrdinalIgnoreCase) };
    private readonly ReaderWriterLockSlim _rwls = new();
    
    private RunOptions Config { get; }
    private PhysicalFileProvider Watcher { get; } // FileSystemWatcher doesn't work in containers.

    public AdSlotsMonitor(RunOptions config)
    {
        Config = config;

        Watcher = new PhysicalFileProvider(config.AdSlotDeclarations)
        {
            UsePollingFileWatcher = true,
            UseActivePolling = true
        };
        Activated("*");
    }
    
    private void Activated(object f)
    {
        var filter = (string)f;
        Watcher.Watch(filter)
            .RegisterChangeCallback(Activated, filter);

        Dictionary<string, AdSlot> ads = new(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.GetFiles(Watcher.Root))
        {
            var filename = Path.GetFileNameWithoutExtension(file);
            var contents = File.ReadAllText(file);
            var ad = JsonConvert.DeserializeObject<AdSlot>(contents);
            ads[filename] = ad;
        }
        var adSlots = new AdSlots { Ads = ads };
        _rwls.SafeWrite(() => _adSlots = adSlots);
    }
    
}