using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OtpAuth.Infrastructure.Settings;
using OtpAuth.Infrastructure.Sms;
using Xunit;

namespace OtpAuth.Tests;

/// <summary>
/// ÖzTek SMS entegrasyonunun (SmsSender) doğrulaması.
/// Gerçek ağ/kimlik bilgisi gerekmez — ÖzTek sunucusu sahte (fake) HTTP handler ile taklit edilir.
/// </summary>
public class SmsSenderTests
{
    /// <summary>İsteği yakalayıp istenen cevabı dönen sahte HTTP handler.</summary>
    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly string _responseBody;
        public string? CapturedBody { get; private set; }
        public Uri? CapturedUri { get; private set; }
        public int CallCount { get; private set; }

        public FakeHandler(string responseBody) => _responseBody = responseBody;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            CallCount++;
            CapturedUri = request.RequestUri;
            // form-urlencoded gövdeyi URL-decode ederek okunur hale getir ("data=<sms>...").
            var raw = request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct);
            // WebUtility.UrlDecode '+' karakterini boşluğa çevirir (form-urlencoded kuralı).
            CapturedBody = WebUtility.UrlDecode(raw);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_responseBody) };
        }
    }

    private static (SmsSender sender, FakeHandler handler) Build(string responseBody, bool enabled = true)
    {
        var options = Options.Create(new SmsOptions
        {
            Enabled = enabled,
            BaseUrl = "http://www.ozteksms.com/panel/smsgonder1Npost.php",
            Kno = "1001000",
            Kulad = "OZTEK",
            Sifre = "sifre123",
            Gonderen = "OTPAUTH",
            Tur = "Normal"
        });

        var handler = new FakeHandler(responseBody);
        var httpClient = new HttpClient(handler);
        var sender = new SmsSender(httpClient, options, NullLogger<SmsSender>.Instance);
        return (sender, handler);
    }

    [Fact]
    public async Task SendAsync_E164_NumarayiOnHaneye_Cevirir()
    {
        var (sender, handler) = Build("1:123:Gonderildi:2:0,010");

        await sender.SendAsync("+905551112233", "Giris kodunuz: 123456");

        Assert.Contains("<numaralar>5551112233</numaralar>", handler.CapturedBody);
        Assert.DoesNotContain("+90", handler.CapturedBody);
    }

    [Theory]
    [InlineData("+905551112233", "5551112233")] // E.164
    [InlineData("05551112233", "5551112233")]    // başında 0
    [InlineData("5551112233", "5551112233")]     // 10 hane
    public async Task SendAsync_FarkliFormatlari_OnHaneyeNormalize_Eder(string input, string expected)
    {
        var (sender, handler) = Build("1:1:Gonderildi:2:0,010");

        await sender.SendAsync(input, "Kod: 111111");

        Assert.Contains($"<numaralar>{expected}</numaralar>", handler.CapturedBody);
    }

    [Fact]
    public async Task SendAsync_OztekXml_DogruKuruluyor()
    {
        var (sender, handler) = Build("1:1:Gonderildi:2:0,010");

        await sender.SendAsync("+905551112233", "Giris kodunuz: 654321");

        var body = handler.CapturedBody!;
        Assert.StartsWith("data=<sms>", body);                  // POST field "data" zorunlu
        Assert.Contains("<kno>1001000</kno>", body);
        Assert.Contains("<kulad>OZTEK</kulad>", body);
        Assert.Contains("<sifre>sifre123</sifre>", body);
        Assert.Contains("<tur>Normal</tur>", body);
        Assert.Contains("<gonderen>OTPAUTH</gonderen>", body);
        Assert.Contains("<mesaj>Giris kodunuz: 654321</mesaj>", body);
        Assert.Equal("http://www.ozteksms.com/panel/smsgonder1Npost.php", handler.CapturedUri!.ToString());
    }

    [Fact]
    public async Task SendAsync_BasariliCevap_HataFirlatmaz()
    {
        var (sender, handler) = Build("1:58952:Gonderildi:2:0,010");

        // Exception fırlatmamalı.
        await sender.SendAsync("+905551112233", "Kod: 222333");

        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task SendAsync_HataliCevap_Exception_Firlatir()
    {
        var (sender, _) = Build("2:Yeterli bakiyeniz yok");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync("+905551112233", "Kod: 444555"));

        Assert.Contains("Yeterli bakiyeniz yok", ex.Message);
    }

    [Fact]
    public async Task SendAsync_GecersizNumara_Exception_Firlatir()
    {
        var (sender, handler) = Build("1:1:Gonderildi:2:0,010");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync("123", "Kod: 000000"));

        Assert.Equal(0, handler.CallCount); // ÖzTek'e hiç gidilmemeli
    }

    [Fact]
    public async Task SendAsync_DevreDisiyken_OztekeGitmez()
    {
        var (sender, handler) = Build("1:1:Gonderildi:2:0,010", enabled: false);

        await sender.SendAsync("+905551112233", "Kod: 999888");

        Assert.Equal(0, handler.CallCount); // Enabled=false => sadece log
    }

    [Fact]
    public async Task SendAsync_OzelKarakterleri_XmlEscape_Eder()
    {
        var (sender, handler) = Build("1:1:Gonderildi:2:0,010");

        await sender.SendAsync("+905551112233", "Kod & <test>");

        Assert.Contains("Kod &amp; &lt;test&gt;", handler.CapturedBody);
    }
}
