namespace BanAd.Processing.Entities;

public class SubmittedAd
{
    public string AdSlotId { get; set; }
    public int Hours { get; set; }
    public int Banano { get; set; }
    public string SubmitterEmail { get; set; }
    public string AdLink { get; set; }
    public string AdFilename { get; set; }
    public Func<Stream> Ad { get; set; }
}