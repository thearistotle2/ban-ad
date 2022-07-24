using BanAd.Config;
using BanAd.Processing.Connectors;
using BanAd.Processing.Entities;
using BanAd.Processing.Workflow;

namespace BanAd.Processing.Monitoring;

public class PaymentMonitor : Monitor
{
    
    private AdSlotsMonitor AdSlots { get; }
    private FileConnector Files { get; }
    private BananoConnector Banano { get; }
    private RunOptions Config { get; }

    public PaymentMonitor(
        AdSubmissionProcessor processor,
        TimeSpan interval,
        AdSlotsMonitor adSlots,
        FileConnector files,
        BananoConnector banano,
        RunOptions config)
        : base(processor, interval)
    {
        AdSlots = adSlots;
        Files = files;
        Banano = banano;
        Config = config;
    }
    
    protected override async Task Tick()
    {
        var pending = Files.AdsPendingPayment();
        if (pending.Any())
        {
            var grouped = pending
                .GroupBy(payment =>
                    AdSlots.Value.Ads[payment.AdSlotId].BanAddress ?? Config.BananoPaymentAddress,
                    StringComparer.OrdinalIgnoreCase);
            await Task.WhenAll(
                grouped.Select(
                    group => Process(group.Key, group.ToArray())));
        }
    }

    private async Task Process(string address, IEnumerable<PendingPayment> waiting)
    {
        var newPayments = await Banano.GetNewPayments(address);
        var paid = waiting.Where(pending => newPayments.Contains(pending.Banano));
        await Task.WhenAll(
            paid.Select(
                payment => Processor.ProcessPayment(payment.AdSlotId, payment.AdId, payment.SubmitterEmail)));
    }
}