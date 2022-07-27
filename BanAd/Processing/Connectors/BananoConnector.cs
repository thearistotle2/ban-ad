using System.Text.Json;
using BanAd.Config;
using SkiaSharp;
using SkiaSharp.QrCode.Image;
using SLSAK.Extensions;
using File = SLSAK.Docker.IO.File;

namespace BanAd.Processing.Connectors;

public abstract class BananoConnector
{
    protected AdSlotsMonitor AdSlots { get; }
    protected RunOptions Config { get; }
    protected HttpClient Client { get; }

    public BananoConnector(AdSlotsMonitor adSlots, RunOptions config)
    {
        AdSlots = adSlots;
        Config = config;

        Client = new();

        // Make sure tracking directory exists.
        Directory.CreateDirectory(Config.BananoTrackingLocation);

        // Fill seen hashes off disk.
        SeenHashes = Directory.GetDirectories(Config.BananoTrackingLocation)
            .Select(Path.GetFileName)
            .ToDictionary(
                address => address,
                address =>
                    Directory.GetFiles(Path.Combine(Config.BananoTrackingLocation, address))
                    .Select(Path.GetFileName)
                    .ToList()
            );
    }

    #region " QR Code "

    public (byte[] QRCode, string QRCodeContent) BuildQRCode(decimal banano, string address)
    {
        var remainder = banano - decimal.Floor(banano);
        var rawRemainder = remainder == 0
            ? string.Empty.PadRight(29, '0')
            : remainder.ToString()[2..].PadRight(29, '0');
        var raw = $"{decimal.Floor(banano)}{rawRemainder}";
        var value = $"banano:{address}?amount={raw}";
        return (GenerateQRCode(value), value);
    }

    private byte[] GenerateQRCode(string content)
    {
        var qrCode = new QrCode(content, new Vector2Slim(150, 150), SKEncodedImageFormat.Png);
        using var stream = new MemoryStream();
        qrCode.GenerateImage(stream);
        return stream.ToArray();
    }

    #endregion

    #region " Payments "

    public async Task<IEnumerable<decimal>> GetNewPayments(string address)
    {
        var receivable = GetNewPaymentsReceivable(address);
        var history = GetNewPaymentsHistory(address);
        await Task.WhenAll(receivable, history);

        var all = (await receivable).Concat(await history);
        var unseen = await NewPayments(
            address,
            all.Select(kvp => kvp.Key));
        return all.Where(kvp => unseen.Contains(kvp.Key))
            .Select(kvp => kvp.Value);
    }

    protected abstract Task<Dictionary<string, decimal>> GetNewPaymentsReceivable(string address);
    protected abstract Task<Dictionary<string, decimal>> GetNewPaymentsHistory(string address);

    protected decimal FromRaw(string raw)
    {
        var length = raw?.Length ?? 0;
        // If the transaction is less than 1 BAN, it's not a payment for this system.
        return length < 30
            ? 0
            : decimal.Parse($"{raw[..(length - 29)]}.{raw[(length - 29)..]}");
    }

    #region " Seen Tracking "

    private readonly IDictionary<string, List<string>> SeenHashes;
    private ReaderWriterLockSlim Rwls { get; } = new();

    protected async Task<IEnumerable<string>> NewPayments(string address, IEnumerable<string> hashes)
    {
        IEnumerable<string> result = null;
        Rwls.SafeWrite(async () =>
        {
            var directory = Path.Combine(Config.BananoTrackingLocation, address);
            Directory.CreateDirectory(directory); // Make sure directory exists.

            if (!SeenHashes.ContainsKey(address))
            {
                SeenHashes[address] = new List<string>();
            }

            var seen = SeenHashes[address];
            var unseen = hashes.Except(seen).ToArray();

            // Track hashes on disk for continuity.
            var tasks = unseen.Select(hash =>
                File.WriteAllTextAsync(Path.Combine(directory, hash), null));
            SeenHashes[address].AddRange(unseen); // Track hashes for this run.
            await Task.WhenAll(tasks);
            result = unseen;
        });
        return result;
    }

    #endregion

    #endregion
}

internal class BananoRpcConnector : BananoConnector
{
    public BananoRpcConnector(AdSlotsMonitor adSlots, RunOptions config) : base(adSlots, config)
    {
    }

    protected override async Task<Dictionary<string, decimal>> GetNewPaymentsReceivable(string address)
    {
        var request = new
        {
            action = "receivable",
            account = address,
            count = $"{Config.BananoHistoryCount}",
            threshold = "100000000000000000000000000000" // Filter out transactions under 1 BAN.
        };
        var response = await Client.PostAsJsonAsync(Config.BananoNode, request);
        response.EnsureSuccessStatusCode();
        var blocks = (await response.Content.ReadFromJsonAsync<ReceivablePayments>())?.Blocks;

        if (blocks?.ValueKind == JsonValueKind.Object)
        {
            var payments = blocks.Value.Deserialize<IDictionary<string, string>>();
            return payments.ToDictionary(
                kvp => kvp.Key,
                kvp => FromRaw(kvp.Value)
            );
        }

        return new Dictionary<string, decimal>();
    }

    protected override async Task<Dictionary<string, decimal>> GetNewPaymentsHistory(string address)
    {
        var request = new
        {
            action = "account_history",
            account = address,
            count = $"{Config.BananoHistoryCount}"
        };
        var response = await Client.PostAsJsonAsync(Config.BananoNode, request);
        response.EnsureSuccessStatusCode();
        var payments =
            (await response.Content.ReadFromJsonAsync<HistoryPayments>())
            ?.History
            // We only care about receives.
            ?.Where(payment => string.Equals(payment.Type, "receive", StringComparison.OrdinalIgnoreCase));
        
        if (payments?.Any() == true)
        {
            return payments.ToDictionary(
                payment => payment.Hash,
                payment => FromRaw(payment.Amount)
            );
        }

        return new Dictionary<string, decimal>();
    }

    #region " Response Objects "

    private class ReceivablePayments
    {
        public JsonElement? Blocks { get; set; }
    }

    private class HistoryPayments
    {
        public IEnumerable<HistoryPayment>? History { get; set; }

        public class HistoryPayment
        {
            public string Type { get; set; }
            public string? Amount { get; set; } // string? because some block types don't contain amount.
            public string Hash { get; set; }
        }
    }

    #endregion
}

internal class BananoCreeperConnector : BananoConnector
{
    public BananoCreeperConnector(AdSlotsMonitor adSlots, RunOptions config) : base(adSlots, config)
    {
    }

    protected override async Task<Dictionary<string, decimal>> GetNewPaymentsReceivable(string address)
    {
        var request = new
        {
            address,
            size = Config.BananoHistoryCount
        };
        return await Process(Client.PostAsJsonAsync(Config.CreeperReceivableUrl, request), false);
    }

    protected override async Task<Dictionary<string, decimal>> GetNewPaymentsHistory(string address)
    {
        var request = new
        {
            address,
            size = Config.BananoHistoryCount
        };
        return await Process( Client.PostAsJsonAsync(Config.CreeperHistoryUrl, request), true);
    }

    private async Task<Dictionary<string, decimal>> Process(Task<HttpResponseMessage> request, bool filter)
    {
        var response = await request;
        response.EnsureSuccessStatusCode();
        var payments = (await response.Content.ReadFromJsonAsync<IEnumerable<Block>>());
        if (filter)
        {
            payments = payments?.Where(payment =>
                string.Equals(payment.Type, "receive", StringComparison.OrdinalIgnoreCase));
        }

        if (payments?.Any() == true)
        {
            return payments.ToDictionary(
                payment => payment.Hash,
                payment => payment.Amount ?? 0
            );
        }

        return new Dictionary<string, decimal>();
    }

    #region " Response Objects "

    private class Block
    {
        public string Hash { get; set; }
        public decimal? Amount { get; set; }
        public string? Type { get; set; } // receivable transactions won't have type.
    }

    #endregion
}