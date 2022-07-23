namespace BanAd.Config;

public class RunOptions
{
    public string SiteId { get; set; }
    public string SiteBaseUrl { get; set; }
    
    public string AdSlotDeclarations { get; set; }
    public string AdsLocation { get; set; }

    public IEnumerable<string> SupportedExtensions { get; set; }
    public int MaxUploadSizeKiB { get; set; }
    
    public string BotHoneypotName { get; set; }
    public int BotMinSeconds { get; set; }
    
    public string SmtpServer { get; set; }
    public int SmtpPort { get; set; }
    public string ImapServer { get; set; }
    public int ImapPort { get; set; }
    public string EmailAddress { get; set; }
    public string EmailPassword { get; set; }
    public string EmailUsername { get; set; }
    public string EmailDisplayName { get; set; }
    public string AdApproverEmail { get; set; }
    
    public string BananoPaymentAddress { get; set; }
    public string BananoNode { get; set; }
}