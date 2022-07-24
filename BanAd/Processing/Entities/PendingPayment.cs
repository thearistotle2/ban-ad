namespace BanAd.Processing.Entities;

public class PendingPayment
{
    public string AdSlotId { get; set; }
    public string AdId { get; set; }
    public decimal Banano { get; set; }
    public string SubmitterEmail { get; set; }
}