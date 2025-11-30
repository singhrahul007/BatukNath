using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

[Route("webhook")]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly HttpClient _http;

    private readonly string _phoneNumberId;
    private readonly string _whatsappToken;

    public WhatsAppWebhookController(IConfiguration config)
    {
        _config = config;
        _http = new HttpClient();

        _phoneNumberId = _config["WhatsApp:PhoneNumberId"];
        _whatsappToken = _config["WhatsApp:AccessToken"];
    }

    // ------------------------------------------------------------
    // VERIFY WEBHOOK
    // ------------------------------------------------------------
    [HttpGet]
    public IActionResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string mode,
        [FromQuery(Name = "hub.challenge")] string challenge,
        [FromQuery(Name = "hub.verify_token")] string token)
    {
        if (mode == "subscribe" && token == _config["WhatsApp:VerifyToken"])
        {
            return Ok(challenge);
        }
        return Unauthorized();
    }

    // ------------------------------------------------------------
    // RECEIVE MESSAGE
    // ------------------------------------------------------------
    [HttpPost]
    public IActionResult ReceiveMessage([FromBody] JsonElement body)
    {
        Console.WriteLine("Incoming Message:");
        Console.WriteLine(body.ToString());

        var value = body.GetProperty("entry")[0]
                        .GetProperty("changes")[0]
                        .GetProperty("value");

        // =============== CHECK FOR BUTTON RESPONSE ===================
        if (value.TryGetProperty("messages", out var messages) &&
            messages[0].TryGetProperty("interactive", out var interactive))
        {
            string from = messages[0].GetProperty("from").GetString();
            string buttonId = interactive.GetProperty("button_reply")
                                         .GetProperty("id")
                                         .GetString();

            Console.WriteLine($"Button clicked: {buttonId}");

            HandleMenuSelection(from, buttonId);
            return Ok();
        }

        // =============== NORMAL TEXT MESSAGE ==========================
        if (!value.TryGetProperty("messages", out var msgArr))
            return Ok();

        var msg = msgArr[0];
        string sender = msg.GetProperty("from").GetString();
        string text = msg.GetProperty("text").GetProperty("body").GetString().ToLower();

        Console.WriteLine($"Text from {sender}: {text}");

        if (text == "hi" || text == "hello" || text.Contains("menu"))
        {
            _ = SendMenu(sender);
        }
        else
        {
            string reply = GetAutoReply(text);
            _ = SendMessage(sender, reply);
        }

        return Ok();
    }

    // ------------------------------------------------------------
    // AUTO REPLY LOGIC
    // ------------------------------------------------------------
    private string GetAutoReply(string text)
    {
        if (text.Contains("price")) return "Our pricing starts at ₹499.";
        if (text.Contains("help")) return "Sure! Tell me how I can help.";
        return "I didn't understand that. Type *menu* to see options.";
    }

    // ------------------------------------------------------------
    // BUTTON CLICK HANDLER
    // ------------------------------------------------------------
    private void HandleMenuSelection(string from, string buttonId)
    {
        switch (buttonId)
        {
            case "MENU_PRODUCTS":
                _ = SendMessage(from, "📦 Our products:\n- Smart Lights\n- Cameras\n- Automation Tools");
                break;

            case "MENU_PRICING":
                _ = SendMessage(from, "💰 Pricing starts at ₹499.\nTell me the product name for details.");
                break;

            case "MENU_SUPPORT":
                _ = SendMessage(from, "📞 Support Team:\nPlease describe your issue.");
                break;

            default:
                _ = SendMessage(from, "Invalid selection. Type *menu* again.");
                break;
        }
    }

    // ------------------------------------------------------------
    // SEND NORMAL TEXT
    // ------------------------------------------------------------
    private async Task SendMessage(string to, string message)
    {
        var apiUrl = $"https://graph.facebook.com/v20.0/{_phoneNumberId}/messages";

        var payload = new
        {
            messaging_product = "whatsapp",
            to = to,
            type = "text",
            text = new { body = message }
        };

        var req = new HttpRequestMessage(HttpMethod.Post, apiUrl);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _whatsappToken);
        req.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

        await _http.SendAsync(req);
    }

    // ------------------------------------------------------------
    // SEND MENU (INTERACTIVE BUTTONS)
    // ------------------------------------------------------------
    private async Task SendMenu(string to)
    {
        var apiUrl = $"https://graph.facebook.com/v20.0/{_phoneNumberId}/messages";

        var payload = new
        {
            messaging_product = "whatsapp",
            to = to,
            type = "interactive",
            interactive = new
            {
                type = "button",
                body = new
                {
                    text = "Welcome 👋\nHow can I help you today?"
                },
                action = new
                {
                    buttons = new[]
                    {
                        new {
                            type = "reply",
                            reply = new { id = "MENU_PRODUCTS", title = "📦 Products" }
                        },
                        new {
                            type = "reply",
                            reply = new { id = "MENU_PRICING", title = "💰 Pricing" }
                        },
                        new {
                            type = "reply",
                            reply = new { id = "MENU_SUPPORT", title = "📞 Support" }
                        }
                    }
                }
            }
        };

        var req = new HttpRequestMessage(HttpMethod.Post, apiUrl);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _whatsappToken);
        req.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

        await _http.SendAsync(req);
    }
}
