using BanAd.Ads;
using BanAd.Processing.Connectors;
using BanAd.Processing.Monitoring;
using BanAd.Processing.Workflow;
using Monitor = BanAd.Processing.Monitoring.Monitor;

namespace BanAd.Config;

public static class Dependencies
{
    
    #region " Monitors "
    
    private static IEnumerable<Monitor> Monitors { get; set; }

    public static void StopMonitors()
    {
        Console.WriteLine("Stopping monitors; please wait.");
        var complete = Monitors.Select(monitor => monitor.Stop()).ToArray();
        while (complete.Any(mres => !mres.IsSet))
        {
            var first = complete.FirstOrDefault(mres => !mres.IsSet);
            if (first != null)
            {
                first.Wait();
            }
        }
        Console.WriteLine("Monitors stopped.");
    }
    
    #endregion

    public static void AddDependencies(IServiceCollection services)
    {
        var env = GetEnvironmentVariables();
        
        services.AddSingleton(_ =>
        {
            var extensions = env["EXTENSIONS"].Split(
                ';',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            ).Select(ext => $".{ext.TrimStart('.')}"); // Make sure the extension start with a .
            
            var maxSize = int.Parse(env["MAX_SIZE_KB"]);
            if (maxSize < 1) { maxSize = 1; }
            
            var minSeconds = int.Parse(env["BOT_MIN_SECONDS"]);
            if (minSeconds < 1) { minSeconds = 1; }

            string password = env.ContainsKey("EMAIL_PASSWORD_FILE")
                ? File.ReadAllText(env["EMAIL_PASSWORD_FILE"])
                : env["EMAIL_PASSWORD"];
            string username = env.ContainsKey("EMAIL_USERNAME")
                ? env["EMAIL_USERNAME"]
                : env["EMAIL_ADDRESS"];
            string displayName = env.ContainsKey("EMAIL_DISPLAY_NAME")
                ? env["EMAIL_DISPLAY_NAME"]
                : env["EMAIL_ADDRESS"];

            return new RunOptions
            {
                SiteId = env["SITE_ID"],
                SiteBaseUrl = env["SITE_BASE_URL"].TrimEnd('/'),
                AdSlotDeclarations = env["AD_SLOT_DECLARATIONS"],
                AdsLocation = env["ADS_LOCATION"],
                BananoTrackingLocation = env["BANANO_TRACKING_LOCATION"],
                BananoHistoryCount = int.Parse(env["BANANO_HISTORY_COUNT"]),
                SupportedExtensions = extensions,
                MaxUploadSizeKiB = maxSize,
                BotHoneypotName = env["BOT_HONEYPOT_NAME"],
                BotMinSeconds = minSeconds,
                SmtpServer = env["SMTP_SERVER"],
                SmtpPort = int.Parse(env["SMTP_PORT"]),
                ImapServer = env["IMAP_SERVER"],
                ImapPort = int.Parse(env["IMAP_PORT"]),
                EmailAddress = env["EMAIL_ADDRESS"],
                EmailPassword = password,
                EmailUsername = username,
                EmailDisplayName = displayName,
                AdApproverEmail = env["AD_APPROVER_EMAIL"],
                BananoPaymentAddress = env["BANANO_PAYMENT_ADDRESS"],
                BananoNode = env["BANANO_NODE"]
            };
        });

        services.AddSingleton<AdSlotsMonitor>();
        services.AddSingleton<AdBuilder>();
        services.AddSingleton<AdValidator>();
        services.AddSingleton<AdSubmissionProcessor>();
        services.AddSingleton<BananoConnector>();
        services.AddSingleton<EmailConnector>();
        services.AddSingleton<FileConnector>();
        
        // Monitors.
        services.AddSingleton(provider =>
            new AdChangeMonitor(
                provider.GetRequiredService<AdSubmissionProcessor>(),
                TimeSpan.FromSeconds(30),
                provider.GetRequiredService<FileConnector>()
            )
        );
        
        services.AddSingleton(provider =>
            new EmailMonitor(
                provider.GetRequiredService<AdSubmissionProcessor>(),
                TimeSpan.FromSeconds(30),
                provider.GetRequiredService<EmailConnector>()
            )
        );
        
        services.AddSingleton(provider =>
            new PaymentMonitor(
                provider.GetRequiredService<AdSubmissionProcessor>(),
                TimeSpan.FromSeconds(30),
                provider.GetRequiredService<AdSlotsMonitor>(),
                provider.GetRequiredService<FileConnector>(),
                provider.GetRequiredService<BananoConnector>(),
                provider.GetRequiredService<RunOptions>()
            )
        );

        var provider = services.BuildServiceProvider();
        Monitors = new Monitor[]
        {
            provider.GetRequiredService<AdChangeMonitor>(),
            provider.GetRequiredService<EmailMonitor>(),
            provider.GetRequiredService<PaymentMonitor>()
        };
    }
    
    private static IDictionary<string, string> GetEnvironmentVariables()
    {
        var env = Environment.GetEnvironmentVariables();
        var dic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string key in env.Keys)
        {
            dic.Add(key, (string)env[key]);
        }
        return dic;
    }
}