using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;

public class WhatsAppService
{
    private readonly string? _token;
    private readonly string? _phoneNumberId;
    private readonly HttpClient _client;

    public WhatsAppService(IConfiguration config)
    {
        _token = config["WhatsApp:Token"]; // Your Cloud API token
        _phoneNumberId = config["WhatsApp:PhoneNumberId"]; // Your Phone Number ID

        _client = new HttpClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
    }

    // ============================================================
    // ✓ SEND TEXT MESSAGE
    // ============================================================
    public async Task SendMessage(string to, string message)
    {
        var url = $"https://graph.facebook.com/v20.0/{_phoneNumberId}/messages";

        var payload = new
        {
            messaging_product = "whatsapp",
            to = to,
            type = "text",
            text = new { body = message }
        };

        var json = JsonConvert.SerializeObject(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync(url, content);

        Console.WriteLine(await response.Content.ReadAsStringAsync());
    }

    // ============================================================
    // ✓ UPLOAD MEDIA (IMAGE / VIDEO / PDF)
    // returns media_id
    // ============================================================
    public async Task<string> UploadMedia(string filePath, string mimeType)
    {
        var uploadUrl = $"https://graph.facebook.com/v20.0/{_phoneNumberId}/media";

        using var form = new MultipartFormDataContent();
        var stream = File.OpenRead(filePath);

        form.Add(new StreamContent(stream), "file", Path.GetFileName(filePath));
        form.Add(new StringContent("whatsapp"), "messaging_product");
        form.Add(new StringContent(mimeType), "type");

        var response = await _client.PostAsync(uploadUrl, form);
        var json = JObject.Parse(await response.Content.ReadAsStringAsync());

        return json["id"]?.ToString();
    }

    // ============================================================
    // ✓ SEND IMAGE BY MEDIA_ID
    // ============================================================
    public async Task SendImage(string to, string mediaId, string caption = "")
    {
        var url = $"https://graph.facebook.com/v20.0/{_phoneNumberId}/messages";

        var payload = new
        {
            messaging_product = "whatsapp",
            to = to,
            type = "image",
            image = new
            {
                id = mediaId,
                caption = caption
            }
        };

        var json = JsonConvert.SerializeObject(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync(url, content);
        Console.WriteLine(await response.Content.ReadAsStringAsync());
    }

    // ============================================================
    // ✓ GET MEDIA DOWNLOAD URL (WHATSAPP PROVIDES THE LINK)
    // ============================================================
    public async Task<string> GetMediaUrl(string mediaId)
    {
        var url = $"https://graph.facebook.com/v20.0/{mediaId}";

        var response = await _client.GetAsync(url);
        var json = JObject.Parse(await response.Content.ReadAsStringAsync());

        return json["url"]?.ToString();
    }

    // ============================================================
    // ✓ DOWNLOAD THE FILE FROM THE URL
    // ============================================================
    public async Task DownloadMediaFile(string downloadUrl, string saveToPath)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

        var res = await _client.SendAsync(req);
        var bytes = await res.Content.ReadAsByteArrayAsync();

        await File.WriteAllBytesAsync(saveToPath, bytes);
    }
}
