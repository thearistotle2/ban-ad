using System.Globalization;
using BanAd.Config;
using BanAd.Processing.Entities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        var filename = Directory
            .GetFiles(directory) // *.* will match extensionless files as well, so just skip it and do the below.
            .FirstOrDefault(filename => filename[1..].Contains("."));
        if (filename != null)
        {
            var link = File.ReadAllText(Path.Combine(directory, "link"));
            return (filename, link);
        }

        return (null, null);
    }

    #endregion
    
    #region " PaidAd "

    public string? PaidAd(string adSlotId, string adId)
    {
        var directory =
            Directory
            .GetDirectories(PaidDirectory(adSlotId), $"{adId}*")
            .SingleOrDefault();
        if (directory != null)
        {
            return Directory
                .GetFiles(directory) // *.* will match extensionless files as well, so just skip it and do the below.
                .FirstOrDefault(filename => filename[1..].Contains("."));
        }

        return null;
    }
    
    #endregion
    
    #region " FutureHours "

    public FutureHours FutureHours(string adSlotId)
    {
        int Sum(string directory) =>
            Directory.GetDirectories(directory)
                .Select(Path.GetFileName)
                .Sum(dir => int.Parse(dir.Split('_').Skip(1).First()));

        var hours = new FutureHours()
        {
            Paid = Sum(PaidDirectory(adSlotId)),
            Approved = Sum(ApprovedDirectory(adSlotId)),
            Pending = Sum(PendingDirectory(adSlotId))
        };
        
        var (current, _) = CurrentAd(adSlotId);
        if (current != null)
        {
            var expires = DateTime.Parse(
                Path.GetFileNameWithoutExtension(current),
                null,
                // Respect the Z, because the calculation below doesn't work Local to UTC.
                DateTimeStyles.RoundtripKind);
            hours.Paid += (int)Math.Round((expires - DateTime.UtcNow).TotalHours);
        }

        return hours;
    }
    
    #endregion
    
    #region " QueuedAds "

    public QueuedAds QueuedAds(string adSlotId)
    {
        var ads = new QueuedAds();

        var current = CurrentAd(adSlotId);
        if (current.Filename != null)
        {
            var expires = DateTime.Parse(
                Path.GetFileNameWithoutExtension(current.Filename),
                null,
                // Respect the Z, because the calculation below doesn't work Local to UTC.
                DateTimeStyles.RoundtripKind);
            ads.Current = (current.Link, expires);
        }

        var paid =
            Directory
                .GetDirectories(PaidDirectory(adSlotId))
                .OrderBy(dir => dir);
        if (paid.Any())
        {
            var list = new List<(string Id, string Link, int Hours)>();
            foreach (var directory in paid)
            {
                var parts = Path.GetFileName(directory).Split('_');
                var link = File.ReadAllText(Path.Combine(directory, "link"));
                list.Add((parts.First(), link, int.Parse(parts.Last())));
            }

            ads.Upcoming = list;
        }

        return ads;
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
                    var expires = DateTime.Parse(
                        Path.GetFileNameWithoutExtension(file),
                        null,
                        // Respect the Z, because the comparison below doesn't work Local to UTC.
                        DateTimeStyles.RoundtripKind);
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
                        var ad = Directory
                            .GetFiles(
                                next) // *.* will match extensionless files as well, so just skip it and do the below.
                            .Single(filename => filename[1..].Contains("."));
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

    public IEnumerable<PendingPayment> AdsPendingPayment()
    {
        var pending = new List<PendingPayment>();

        foreach (var adSlotId in AdSlots.Value.Ads.Keys)
        {
            var directories = Directory.GetDirectories(ApprovedDirectory(adSlotId));
            foreach (var directory in directories)
            {
                var parts = Path.GetFileName(directory).Split('_');
                var submitter = File.ReadAllText(Path.Combine(directory, "submitter"));
                pending.Add(
                    new PendingPayment
                    {
                        AdSlotId = adSlotId,
                        AdId = parts.First(),
                        Banano = decimal.Parse(parts.Last()),
                        SubmitterEmail = submitter
                    }
                );
            }
        }

        return pending;
    }

    #endregion

    #region " SaveAdSubmission "

    private static object SaveLocker { get; } = new();

    public (string AdId, decimal Banano) SaveAdSubmission(SubmittedAd submitted)
    {
        lock (SaveLocker)
        {
            var id = NextAdId(submitted.AdSlotId, submitted.Hours, submitted.Banano);
            var directory = Path.Combine(PendingDirectory(submitted.AdSlotId), id);
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "submitter"), submitted.SubmitterEmail);
            File.WriteAllText(Path.Combine(directory, "link"), submitted.AdLink);
            using var ad = submitted.Ad();
            using var file = File.Create(Path.Combine(directory, submitted.AdFilename));
            ad.CopyTo(file);

            var parts = id.Split('_');
            return (parts.First(), decimal.Parse(parts.Last()));
        }
    }

    private string NextAdId(string adSlotId, int hours, int banano)
    {
        int Max(string directory) =>
            int.Parse(Directory.GetDirectories(directory)
                .Select(Path.GetFileName)
                .Select(dir => dir.Split('_').First())
                .Max() ?? "0000000");
        
        var id = new[]
        {
            Max(PendingDirectory(adSlotId)),
            Max(ApprovedDirectory(adSlotId)),
            Max(PaidDirectory(adSlotId))
        }.Max() + 1;

        if (banano > 0)
        {
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
        else
        {
            return $"{id:0000000}_{hours}_{banano}";
        }
    }

    #endregion

    #region " ApproveAdSubmission "

    public (string SubmitterEmail, decimal Banano) ApproveAdSubmission(string adSlotId, string adId)
    {
        var directory = Directory.GetDirectories(
            PendingDirectory(adSlotId),
            $"{adId}_*"
        ).Single();
        var submitter = File.ReadAllText(Path.Combine(directory, "submitter"));

        var ban = decimal.Parse(directory.Split('_').Last());
        if (ban == 0)
        {
            var target = Path.GetFileName(directory);
            var paid = Path.Combine(PaidDirectory(adSlotId), target[..target.LastIndexOf('_')]);
            Directory.Move(directory, paid);
        }
        else
        {
            var approved = Path.Combine(ApprovedDirectory(adSlotId), Path.GetFileName(directory));
            Directory.Move(directory, approved);
        }

        return (submitter, ban);
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
        var target = Path.GetFileName(directory);
        var paid = Path.Combine(PaidDirectory(adSlotId), target[..target.LastIndexOf('_')]);
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