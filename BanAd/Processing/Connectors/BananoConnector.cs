using System.Text.Json;
using BanAd.Config;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using SkiaSharp.QrCode.Image;
using SLSAK.Extensions;
using File = SLSAK.Docker.IO.File;

namespace BanAd.Processing.Connectors;

public class BananoConnector
{
    private AdSlotsMonitor AdSlots { get; }
    private RunOptions Config { get; }
    private HttpClient Client { get; }

    public BananoConnector(AdSlotsMonitor adSlots, RunOptions config)
    {
        AdSlots = adSlots;
        Config = config;

        Client = new() { BaseAddress = new Uri(config.BananoNode) };

        // Make sure tracking directory exists.
        Directory.CreateDirectory(Config.BananoTrackingLocation);

        // Fill seen hashes off disk.
        SeenHashes = Directory.GetDirectories(Config.BananoTrackingLocation)
            .Select(Path.GetFileName)
            .ToDictionary(
                address => address,
                address => Directory.GetFiles(Path.Combine(Config.BananoTrackingLocation, address)).ToList()
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
        return (await receivable).Concat(await history);
    }

    private async Task<IEnumerable<decimal>> GetNewPaymentsReceivable(string address)
    {
        var request = new
        {
            action = "receivable",
            account = address,
            count = $"{Config.BananoHistoryCount}",
            threshold = "100000000000000000000000000000" // Filter out transactions under 1 BAN.
        };
        var response = await Client.PostAsJsonAsync(null as Uri, request);
        response.EnsureSuccessStatusCode();
        var blocks = (await response.Content.ReadFromJsonAsync<ReceivablePayments>())?.Blocks;

        if (blocks?.ValueKind == JsonValueKind.Object)
        {
            var payments = blocks.Value.Deserialize<IDictionary<string, string>>();
            var unseen = await NewPayments(address, payments.Keys);

            return payments
                .Where(kvp => unseen.Contains(kvp.Key))
                .Select(kvp => FromRaw(kvp.Value));
        }

        return Enumerable.Empty<decimal>();
    }

    private async Task<IEnumerable<decimal>> GetNewPaymentsHistory(string address)
    {
        var request = new
        {
            action = "account_history",
            account = address,
            count = $"{Config.BananoHistoryCount}"
        };
        var response = await Client.PostAsJsonAsync(null as Uri, request);
        response.EnsureSuccessStatusCode();
        var payments = (await response.Content.ReadFromJsonAsync<HistoryPayments>())?.History;

        if (payments?.Any() == true)
        {
            var unseen = await NewPayments(
                address,
                payments.Select(payment => payment.Hash));

            return payments
                .Where(payment => unseen.Contains(payment.Hash))
                .Select(payment => FromRaw(payment.Amount));
        }

        return Enumerable.Empty<decimal>();
    }

    private decimal FromRaw(string raw)
    {
        var length = raw.Length;
        return decimal.Parse($"{raw[..(length - 29)]}.{raw[(length - 29)..]}");
    }

    #region " Seen Tracking "

    private readonly IDictionary<string, List<string>> SeenHashes;
    private ReaderWriterLockSlim Rwls { get; } = new();

    private async Task<IEnumerable<string>> NewPayments(string address, IEnumerable<string> hashes)
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
            public string Amount { get; set; }
            public string Hash { get; set; }
        }
    }

    #endregion

    #endregion
}