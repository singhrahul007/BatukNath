using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

public class WhatsAppService
{
    private readonly string _token;
    private readonly string _phoneNumberId;
    private readonly HttpClient _client;

    public WhatsAppService(IConfiguration config)
    {
        _token = config["WhatsApp:Token"];
        _phoneNumberId = config["WhatsApp:PhoneNumberId"];
        _client = new HttpClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
    }

    public async Task SendMessage(string to, string message)
    {
        var url = $"https://graph.facebook.com/v20.0/{_phoneNumberId}/messages";

        var payload = new
        {
            messaging_product = "whatsapp",
            to = to,
            text = new
            {
                body = message
            }
        };

        var json = JsonConvert.SerializeObject(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var result = await _client.PostAsync(url, content);
        Console.WriteLine(await result.Content.ReadAsStringAsync());
    }
}
