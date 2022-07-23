using BanAd.Processing.Connectors;
using BanAd.Processing.Workflow;

namespace BanAd.Processing.Monitoring;

public class EmailMonitor : Monitor
{
    
    private EmailConnector Email { get; }
    
    public EmailMonitor(AdSubmissionProcessor processor, TimeSpan interval, EmailConnector email)
        : base(processor, interval)
    {
        Email = email;
    }

    protected override async Task Tick()
    {
        var results = await Email.GetPendingApprovalResults();
        foreach (var result in results)
        {
            try
            {
                if (result.Approved)
                {
                    await Processor.ProcessApproval(result.AdSlotId, result.AdId);
                }
                else
                {
                    await Processor.ProcessRejection(result.AdSlotId, result.AdId, result.RejectReason);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}