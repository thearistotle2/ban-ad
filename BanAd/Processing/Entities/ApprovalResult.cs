namespace BanAd.Processing.Entities;

public class ApprovalResult
{
    public string AdSlotId { get; set; }
    public string AdId { get; set; }
    public bool Approved { get; set; }
    public string? RejectReason { get; set; }
}