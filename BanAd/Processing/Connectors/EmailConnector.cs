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

    public async Task SendAdSubmitted(SubmittedAd submitted)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(Config.EmailDisplayName, Config.EmailAddress));
        message.To.Add(new MailboxAddress(submitted.SubmitterEmail, submitted.SubmitterEmail));
        message.Subject = $"[{Config.SiteId}] Ad submitted for review";

        var builder = new BodyBuilder
        {
            TextBody = $@"The attached ad has been submitted for ad slot {submitted.AdSlotId}.
If approved, the ad will run for {submitted.Hours} hours at a cost of {submitted.Banano} BAN.
You will receive another email with the result of the submission (and an invoice, if approved)."
        };
        await using var ad = submitted.Ad();
        await builder.Attachments.AddAsync(submitted.AdFilename, ad);
        message.Body = builder.ToMessageBody();

        await Send(message);
    }

    private async Task Send(MimeMessage message)
    {
        using var client = new SmtpClient();
        await client.ConnectAsync(Config.SmtpServer, Config.SmtpPort, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(Config.EmailAddress, Config.EmailPassword);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}