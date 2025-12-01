using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using WhatsappBot.Services;

public class NlpService : INlpService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _http;

    public NlpService(IConfiguration config)
    {
        _config = config;
        _http = new HttpClient();
    }

    // ============================
    // CALL OPENAI GPT-4O MINI
    // ============================
    public async Task<string> DetectIntent(string userMessage)
    {
        var apiKey = _config["OpenAI:ApiKey"];

        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            model = "gpt-4o-mini",
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "You are an intent detection AI. Return only JSON: {intent:'value', confidence:0-1}"
                },
                new
                {
                    role = "user",
                    content = userMessage
                }
            }
        };

        var response = await _http.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            new StringContent(JsonConvert.SerializeObject(payload),
            Encoding.UTF8, "application/json")
        );

        return await response.Content.ReadAsStringAsync();
    }

    // ============================
    // EXTRACT INTENT
    // ============================
    public string ExtractIntent(string json)
    {
        dynamic obj = JsonConvert.DeserializeObject(json);
        return obj.choices[0].message.content;
    }
}
