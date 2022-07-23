using BanAd.Config;
using SkiaSharp;
using SkiaSharp.QrCode;
using SkiaSharp.QrCode.Image;
using SkiaSharp.QrCode.Models;

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
    
    // make sure to check both receivable and history
    
    #endregion
}