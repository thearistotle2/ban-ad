using BanAd.Config;
using BanAd.Processing.Entities;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace BanAd.Processing.Connectors;

public class EmailConnector
{
    private RunOptions Config { get; }

    public EmailConnector(RunOptions config)
    {
        Config = config;
    }

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

    public async Task SendAdForApproval(SubmittedAd submitted, string adId)
    {
        var message = BuildMessage();
        message.To.Add(new MailboxAddress(Config.AdApproverEmail, Config.AdApproverEmail));
        message.Subject = $"Ad Submission: {Config.SiteId} {submitted.AdSlotId} {adId}";

        var builder = new BodyBuilder
        {
            HtmlBody = $@"The attached ad has been submitted for ad slot {submitted.AdSlotId}.<br/>
If accepted, the ad will link to '{submitted.AdLink}' and run for {Hours(submitted.Hours)} at a
cost of {submitted.Banano} BAN.<br/>
Please review the ad, then choose an option below:<br/>
<a href=""mailto:{Config.EmailAddress}?subject={submitted.AdSlotId}_{adId}_APPROVE"">ACCEPT</a><br/>
<a href=""mailto:{Config.EmailAddress}?subject={submitted.AdSlotId}_{adId}_REJECT"">REJECT</a><br/>
If you choose to reject, you can include a rejection message back to the submitter by writing
the message in the body of the rejection email."
        };
        await using var ad = submitted.Ad();
        await builder.Attachments.AddAsync(submitted.AdFilename, ad);
        message.Body = builder.ToMessageBody();

        await Send(message);
    }

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
}