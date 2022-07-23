using BanAd.Config;
using BanAd.Processing.Entities;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace BanAd.Processing.Connectors;

public class EmailConnector
{
    private RunOptions Config { get; }

    public EmailConnector(RunOptions config)
    {
        Config = config;
    }
    
    #region " SendAdSubmitted "

    public async Task SendAdSubmitted(SubmittedAd submitted, string banano)
    {
        var message = BuildMessage();
        message.To.Add(new MailboxAddress(submitted.SubmitterEmail, submitted.SubmitterEmail));
        message.Subject = $"[{Config.SiteId}] Ad submitted for review";

        var builder = new BodyBuilder
        {
            TextBody = $@"The attached ad has been submitted for ad slot {submitted.AdSlotId}.
If accepted, the ad will link to '{submitted.AdLink}' and run for {Hours(submitted.Hours)} at a cost of {banano} BAN.
You will receive another email with the result of the submission (and an invoice, if accepted)."
        };
        await using var ad = submitted.Ad();
        await builder.Attachments.AddAsync(submitted.AdFilename, ad);
        message.Body = builder.ToMessageBody();

        await Send(message);
    }
    
    #endregion
    
    #region " SendAdForApproval "

    public async Task SendAdForApproval(SubmittedAd submitted, string adId)
    {
        var message = BuildMessage();
        message.To.Add(new MailboxAddress(Config.AdApproverEmail, Config.AdApproverEmail));
        message.Subject = $"[{Config.SiteId}] Ad Submission: {submitted.AdSlotId} {adId}";

        var builder = new BodyBuilder
        {
            HtmlBody = $@"The attached ad has been submitted for ad slot {submitted.AdSlotId}.<br/>
If accepted, the ad will link to '{submitted.AdLink}' and run for {Hours(submitted.Hours)} at a
cost of {submitted.Banano} BAN.<br/>
Please review the ad, then choose an option below:<br/>
<a href=""mailto:{Config.EmailAddress}?subject={submitted.AdSlotId}_{adId}_APPROVE&body="">ACCEPT</a><br/>
<a href=""mailto:{Config.EmailAddress}?subject={submitted.AdSlotId}_{adId}_REJECT&body="">REJECT</a><br/>
If you choose to reject, you can include a rejection message back to the submitter by writing
the message in the body of the rejection email."
        };
        await using var ad = submitted.Ad();
        await builder.Attachments.AddAsync(submitted.AdFilename, ad);
        message.Body = builder.ToMessageBody();

        await Send(message);
    }
    
    #endregion
    
    #region " SendAdRejected "

    public async Task SendAdRejected(string adSlotId, string submitterEmail, string? reason)
    {
        var message = BuildMessage();
        message.To.Add(new MailboxAddress(submitterEmail, submitterEmail));
        message.Subject = $"[{Config.SiteId}] Ad approval results";
        
        var builder = new BodyBuilder
        {
            TextBody = $@"Unfortunately, your ad for ad slot {adSlotId} has been rejected by the site owner."
        };
        if (!string.IsNullOrWhiteSpace(reason))
        {
            builder.TextBody += $@"

Please see the message below for more information:

{reason}";
        }
        message.Body = builder.ToMessageBody();

        await Send(message);
    }
    
    #endregion
    
    #region " SendAdApproved "
    
    public async Task SendAdApproved(string adSlotId, string submitterEmail)
    {
        var message = BuildMessage();
        message.To.Add(new MailboxAddress(submitterEmail, submitterEmail));
        message.Subject = $"[{Config.SiteId}] Ad approval results";
        
        var builder = new BodyBuilder
        {
            TextBody = $@""
        };
        message.Body = builder.ToMessageBody();

        await Send(message);
    }
    
    #endregion
    
    #region " SendAdPaid "
    
    public async Task SendAdPaid(string adSlotId, string submitterEmail)
    {
        var message = BuildMessage();
        message.To.Add(new MailboxAddress(submitterEmail, submitterEmail));
        message.Subject = $"[{Config.SiteId}] Ad payment received";
        
        var builder = new BodyBuilder
        {
            TextBody = $@"Your payment for ad slot {adSlotId} has been received and your ad has entered the queue.
You will receive another email when your ad goes live.

Thank you for advertising with {Config.SiteId}!"
        };
        message.Body = builder.ToMessageBody();

        await Send(message);
    }
    
    #endregion
    
    #region " SendAdLive "
    
    public async Task SendAdLive(string adSlotId, string submitterEmail)
    {
        var message = BuildMessage();
        message.To.Add(new MailboxAddress(submitterEmail, submitterEmail));
        message.Subject = $"[{Config.SiteId}] Ad live!";
        
        var builder = new BodyBuilder
        {
            TextBody = $@"Your ad for ad slot {adSlotId} is live!  Come take a look!

And, of course, thank you again for advertising with {Config.SiteId}."
        };
        message.Body = builder.ToMessageBody();

        await Send(message);
    }
    
    #endregion
    
    #region " GetPendingApprovalResults "

    public async Task<IEnumerable<ApprovalResult>> GetPendingApprovalResults()
    {
        using var client = new ImapClient();
        await client.ConnectAsync(Config.ImapServer, Config.ImapPort, SecureSocketOptions.SslOnConnect);
        await client.AuthenticateAsync(Config.EmailAddress, Config.EmailPassword);
        
        var query = SearchQuery.NotSeen.And(SearchQuery.FromContains(Config.AdApproverEmail));
    
        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadWrite);
        var ids = await inbox.SearchAsync(query);

        var results = new List<ApprovalResult>();
        foreach (var id in ids)
        {
            using var message = await inbox.GetMessageAsync(id);
            var result = Map(message);
            if (result != null)
            {
                results.Add(result);
            }

            await client.Inbox.StoreAsync(
                id,
                new StoreFlagsRequest(StoreAction.Add, MessageFlags.Seen) { Silent = true }
            );
        }
        await client.DisconnectAsync(true);
        return results;
    }
    
    #endregion
    
    #region " Helper Methods "

    private MimeMessage BuildMessage()
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(Config.EmailDisplayName, Config.EmailAddress));
        return message;
    }

    private async Task Send(MimeMessage message)
    {
        using var client = new SmtpClient();
        await client.ConnectAsync(Config.SmtpServer, Config.SmtpPort, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(Config.EmailAddress, Config.EmailPassword);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private string Hours(int hours)
    {
        return hours == 1 ? "1 hour" : $"{hours} hours";
    }

    private ApprovalResult? Map(MimeMessage message)
    {
        var parts = message.Subject?.Split('_');
        if (parts is { Length: 3 })
        {
            if (string.Equals(parts.Last(), "APPROVE", StringComparison.OrdinalIgnoreCase))
            {
                return new ApprovalResult
                {
                    Approved = true,
                    AdSlotId = parts.First(),
                    AdId = parts.Skip(1).First()
                };
            }
            else if (string.Equals(parts.Last(), "REJECT", StringComparison.OrdinalIgnoreCase))
            {
                var reason = message.TextBody;
                if (string.Equals(reason, "Empty Message", StringComparison.OrdinalIgnoreCase))
                {
                    reason = null;
                }
                return new ApprovalResult
                {
                    Approved = false,
                    AdSlotId = parts.First(),
                    AdId = parts.Skip(1).First(),
                    RejectReason = reason
                };
            }
        }

        return null;
    }
    
    #endregion
}