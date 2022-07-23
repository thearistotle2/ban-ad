using BanAd.Config;
using BanAd.Processing.Entities;
using File = SLSAK.Docker.IO.File;

namespace BanAd.Processing.Connectors;

// PENDING DIRECTORY NAME:  pending/0000001_48_96.02 -- id_hours_ban
//    -- contains: A53FAD9EC.png -- ad
//    -- contains: link -- ad link
//    -- contains: submitter -- submitter's email
// APPROVED DIRECTORY NAME:  approved/0000001_48_96.02 -- id_hours_ban
//    -- contains: A53FAD9EC.png -- ad
//    -- contains: link -- ad link
//    -- contains: submitter -- submitter's email
// PAID DIRECTORY NAME:  paid/0000001_48 -- id_hours
//    -- contains: A53FAD9EC.png -- ad
//    -- contains: link -- ad link
//    -- contains: submitter -- submitter's email
// CURRENT DIRECTORY NAME: current
//    -- contains: 2022-07-22T15:03:77Z -- ad (expiresZ)
//    -- contains: link -- ad link
//    -- contains: submitter -- submitter's email

public class FileConnector
{
    private AdSlotsMonitor AdSlots { get; }
    private RunOptions Config { get; }

    public FileConnector(AdSlotsMonitor adSlots, RunOptions config)
    {
        AdSlots = adSlots;
        Config = config;
    }

    #region " CurrentAd "

    public (string? Filename, string? Link) CurrentAd(string adSlotId)
    {
        var directory = CurrentDirectory(adSlotId);
        var filename = Directory.GetFiles(directory, "*.*").FirstOrDefault();
        if (filename != null)
        {
            var link = File.ReadAllText(Path.Combine(directory, "link"));
            return (filename, link);
        }

        return (null, null);
    }

    #endregion

    #region " ProgressAdsIfNeeded "

    private static object ProgressLocker { get; } = new();

    // Key = AdSlotId, Value = SubmitterEmail
    public IDictionary<string, string> ProgressAdsIfNeeded()
    {
        var live = new Dictionary<string, string>();

        lock (ProgressLocker)
        {
            foreach (var adSlotId in AdSlots.Value.Ads.Keys)
            {
                var (file, _) = CurrentAd(adSlotId);
                if (file != null)
                {
                    var expires = DateTime.Parse(Path.GetFileNameWithoutExtension(file));
                    if (expires < DateTime.UtcNow)
                    {
                        Directory.Delete(CurrentDirectory(adSlotId), true);
                        file = null;
                    }
                }

                if (file == null)
                {
                    var next = Directory.GetDirectories(PaidDirectory(adSlotId))
                        .MinBy(dir => dir, StringComparer.Ordinal);
                    if (next != null)
                    {
                        var hours = int.Parse(Path.GetFileName(next).Split('_').Last());

                        var submitter = File.ReadAllText(Path.Combine(next, "submitter"));
                        var ad = Directory.GetFiles(next, "*.*").Single();
                        var extension = Path.GetExtension(ad);

                        var directory = CurrentDirectory(adSlotId);

                        var filename = Path.Combine(directory, $"{DateTime.UtcNow.AddHours(hours):O}{extension}");
                        File.Move(ad, filename);
                        File.Move(
                            Path.Combine(next, "submitter"),
                            Path.Combine(directory, "submitter"));
                        File.Move(
                            Path.Combine(next, "link"),
                            Path.Combine(directory, "link"));
                        Directory.Delete(next, true);
                        live[adSlotId] = submitter;
                    }
                }
            }
        }

        return live;
    }

    #endregion

    #region " AdsPendingPayment "

    public IEnumerable<(string AdSlotId, string AdId, decimal Banano, string SubmitterEmail)> AdsPendingPayment()
    {
        var pending = new List<(string, string, decimal, string)>();

        foreach (var adSlotId in AdSlots.Value.Ads.Keys)
        {
            var directories = Directory.GetDirectories(ApprovedDirectory(adSlotId));
            foreach (var directory in directories)
            {
                var parts = Path.GetFileName(directory).Split('_');
                var submitter = File.ReadAllText(Path.Combine(directory, "submitter"));
                pending.Add((adSlotId, parts.First(), decimal.Parse(parts.Last()), submitter));
            }
        }

        return pending;
    }

    #endregion

    #region " SaveAdSubmission "

    private static object SaveLocker { get; } = new();

    public (string AdId, string Banano) SaveAdSubmission(SubmittedAd submitted)
    {
        lock (SaveLocker)
        {
            var id = NextAdId(submitted.AdSlotId, submitted.Hours, submitted.Banano);
            var directory = Path.Combine(PendingDirectory(submitted.AdSlotId), id);
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "submitter"), submitted.SubmitterEmail);
            File.WriteAllText(Path.Combine(directory, "link"), submitted.AdLink);
            using var ad = submitted.Ad();
            using var file = System.IO.File.Create(Path.Combine(directory, submitted.AdFilename)); // TODO
            ad.CopyTo(file);

            var parts = id.Split('_');
            return (parts.First(), parts.Last());
        }
    }

    private string NextAdId(string adSlotId, int hours, int banano)
    {
        string? Max(string directory) =>
            Directory.GetDirectories(directory)
                .Select(Path.GetFileName)
                .Select(dir => dir.Split('_').First())
                .Max();

        var id = int.Parse(
            Max(PendingDirectory(adSlotId)) ?? Max(ApprovedDirectory(adSlotId)) ?? "0000000"
        ) + 1;
        var pattern = $"*_{banano}.*";
        var existingAdditionals =
            Directory.GetDirectories(PendingDirectory(adSlotId), pattern)
                .Concat(Directory.GetDirectories(ApprovedDirectory(adSlotId), pattern))
                .Select(dir => dir.Split("_").Last())
                .Select(ban => (int)(decimal.Parse(ban) * 100) % 100);
        var additionals = Enumerable.Range(0, 100).Except(existingAdditionals).ToArray();

        if (!additionals.Any())
        {
            throw new Exception(
                $"Too many ads for ad slot {adSlotId} costing {banano} BAN awaiting approval or payment."
            );
        }

        var additional = additionals.First();
        return $"{id:0000000}_{hours}_{banano}.{additional:00}";
    }

    #endregion

    #region " ApproveAdSubmission "

    public string ApproveAdSubmission(string adSlotId, string adId)
    {
        var directory = Directory.GetDirectories(
            PendingDirectory(adSlotId),
            $"{adId}_*"
        ).Single();
        var submitter = File.ReadAllText(Path.Combine(directory, "submitter"));
        var approved = Path.Combine(ApprovedDirectory(adSlotId), Path.GetFileName(directory));
        Directory.Move(directory, approved);
        return submitter;
    }

    #endregion

    #region " DeleteAdSubmission "

    public string DeleteAdSubmission(string adSlotId, string adId)
    {
        var directory = Directory.GetDirectories(
            PendingDirectory(adSlotId),
            $"{adId}_*"
        ).Single();
        var submitter = File.ReadAllText(Path.Combine(directory, "submitter"));
        Directory.Delete(directory, true);
        return submitter;
    }

    #endregion

    #region " PayAdSubmission "

    public string PayAdSubmission(string adSlotId, string adId)
    {
        var directory = Directory.GetDirectories(
            ApprovedDirectory(adSlotId),
            $"{adId}_*"
        ).Single();
        var submitter = File.ReadAllText(Path.Combine(directory, "submitter"));
        var paid = directory[0..directory.LastIndexOf('_')];
        Directory.Move(directory, paid);
        return submitter;
    }

    #endregion


    #region " Directories "

    private string PendingDirectory(string adSlotId)
    {
        var directory = Path.Join(Config.AdsLocation, adSlotId, "pending");
        Directory.CreateDirectory(directory); // Make sure the directory exists.
        return directory;
    }

    private string ApprovedDirectory(string adSlotId)
    {
        var directory = Path.Join(Config.AdsLocation, adSlotId, "approved");
        Directory.CreateDirectory(directory); // Make sure the directory exists.
        return directory;
    }

    private string PaidDirectory(string adSlotId)
    {
        var directory = Path.Join(Config.AdsLocation, adSlotId, "paid");
        Directory.CreateDirectory(directory); // Make sure the directory exists.
        return directory;
    }

    private string CurrentDirectory(string adSlotId)
    {
        var directory = Path.Join(Config.AdsLocation, adSlotId, "current");
        Directory.CreateDirectory(directory); // Make sure the directory exists.
        return directory;
    }

    #endregion
}