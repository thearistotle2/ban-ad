namespace BanAd.Config;

public static class Dependencies
{
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
            
            return new RunOptions
            {
                SiteId = env["SITE_ID"],
                AdSlotDeclarations = env["AD_SLOT_DECLARATIONS"],
                AdsLocation = env["ADS_LOCATION"],
                SupportedExtensions = extensions,
                MaxUploadSizeKiB = maxSize
            };
        });

        services.AddSingleton<AdSlotsMonitor>();
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