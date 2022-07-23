using BanAd.Processing.Workflow;

namespace BanAd.Processing.Monitoring;

public class PaymentMonitor : Monitor
{

    public PaymentMonitor(AdSubmissionProcessor processor, TimeSpan interval)
        : base(processor, interval)
    {
        
    }
    
    protected async override Task Tick()
    {
        
    }
}