using BanAd.Processing.Workflow;

namespace BanAd.Processing.Monitoring;

public class AdChangeMonitor : Monitor
{
    
    public AdChangeMonitor(AdSubmissionProcessor processor, TimeSpan interval)
        : base(processor, interval)
    {
        
    }
    
    protected async override Task Tick()
    {
        
    }
}