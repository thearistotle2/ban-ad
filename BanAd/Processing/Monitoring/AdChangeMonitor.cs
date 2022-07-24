using BanAd.Processing.Connectors;
using BanAd.Processing.Workflow;

namespace BanAd.Processing.Monitoring;

public class AdChangeMonitor : Monitor
{
    
    private FileConnector Files { get; }
    
    public AdChangeMonitor(AdSubmissionProcessor processor, TimeSpan interval, FileConnector files)
        : base(processor, interval)
    {
        Files = files;
    }
    
    protected override async Task Tick()
    {
        var live = Files.ProgressAdsIfNeeded();
        await Task.WhenAll(
            live.Select(
                kvp => Processor.ProcessLive(kvp.Key, kvp.Value)));
    }
}