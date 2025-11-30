using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WhatsappBot.Model;

[Route("webhook")]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly HttpClient _http;

    private readonly string? _phoneNumberId;
    private readonly string? _whatsappToken;

    // ============================================
    // 🔥 ORDER SESSIONS STORE
    // ============================================
    private static Dictionary<string, OrderSession> _orderSessions = new();

    public WhatsAppWebhookController(IConfiguration config)
    {
        _config = config;
        _http = new HttpClient();

        _phoneNumberId = _config["WhatsApp:PhoneNumberId"];
        _whatsappToken = _config["WhatsApp:AccessToken"];
        if (string.IsNullOrEmpty(_phoneNumberId) || string.IsNullOrEmpty(_whatsappToken))
        {
            throw new Exception("WhatsApp PhoneNumberId or AccessToken missing in config");
        }
    }

    // ============================================
    // VERIFY WEBHOOK
    // ============================================
    [HttpGet]
    public IActionResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string mode,
        [FromQuery(Name = "hub.challenge")] string challenge,
        [FromQuery(Name = "hub.verify_token")] string token)
    {
        if (mode == "subscribe" && token == _config["WhatsApp:VerifyToken"])
            return Ok(challenge);

        return Unauthorized();
    }

    // ============================================
    // RECEIVE MESSAGE
    // ============================================
    [HttpPost]
    public async Task<IActionResult> ReceiveMessage([FromBody] JsonElement body)
    {
        Console.WriteLine("Incoming Message:");
        Console.WriteLine(body.ToString());

        var value = body.GetProperty("entry")[0]
                        .GetProperty("changes")[0]
                        .GetProperty("value");

        if (!value.TryGetProperty("messages", out var messages))
            return Ok();

        var msg = messages[0];
        string sender = msg.GetProperty("from").GetString();

        // ============================================================
        // IMAGE RECEIVED
        // ============================================================
        if (msg.TryGetProperty("type", out var msgTypeProp)
            && msgTypeProp.GetString() == "image")
        {
            var image = msg.GetProperty("image");
            string mediaId = image.GetProperty("id").GetString();
            string mimeType = image.GetProperty("mime_type").GetString();

            string filePath = await DownloadImage(mediaId, mimeType);

            await SendMessage(sender, $"📸 Image saved at: {filePath}");

            return Ok();
        }

        // ============================================================
        // BUTTON RESPONSE
        // ============================================================
        if (msg.TryGetProperty("interactive", out var interactive))
        {
            string buttonId = interactive.GetProperty("button_reply")
                                         .GetProperty("id").GetString();

            await HandleMenuSelection(sender, buttonId);
            return Ok();
        }

        // ============================================================
        // TEXT MESSAGE
        // ============================================================
        if (msg.TryGetProperty("text", out var textObj))
        {
            string text = textObj.GetProperty("body").GetString().ToLower();

            // 🔥 If user is inside order workflow → continue workflow
            if (_orderSessions.ContainsKey(sender))
            {
                await ContinueOrderProcess(sender, text);
                return Ok();
            }

            if (text == "menu" || text == "hi" || text == "hello")
            {
                await SendMenu(sender);
            }
            else if (text == "order")
            {
                await StartOrder(sender);
            }
            else
            {
                string reply = GetAutoReply(text);
                await SendMessage(sender, reply);
            }
        }

        return Ok();
    }

    // ================================================
    // AUTO REPLY LOGIC
    // ================================================
    private string GetAutoReply(string text)
    {
        if (text.Contains("price")) return "Our pricing starts at ₹499.";
        if (text.Contains("help")) return "Sure! How can I assist?";
        return "I didn't understand. Type *menu* to see options.";
    }

    // ================================================
    // ORDER WORKFLOW
    // ================================================
    private async Task StartOrder(string user)
    {
        _orderSessions[user] = new OrderSession
        {
            Step = 1,
            User = user
        };

        await SendMessage(user, "🛒 Order Started!\nWhat product do you want to order?");
    }

    private async Task ContinueOrderProcess(string user, string input)
    {
        var session = _orderSessions[user];

        switch (session.Step)
        {
            case 1:
                session.ProductName = input;
                session.Step = 2;
                await SendMessage(user, "How many quantity?");
                break;

            case 2:
                session.Quantity = input;
                session.Step = 3;
                await SendMessage(user, "Please provide your delivery address:");
                break;

            case 3:
                session.Address = input;
                session.Step = 4;

                await SendMessage(user,
                    $"🧾 *Order Summary*\n" +
                    $"Product: {session.ProductName}\n" +
                    $"Quantity: {session.Quantity}\n" +
                    $"Address: {session.Address}\n\n" +
                    "Type *confirm* to place the order or *cancel*.");

                break;

            case 4:
                if (input == "confirm")
                {
                    await SendMessage(user, "✅ Your order has been placed successfully!");
                }
                else
                {
                    await SendMessage(user, "❌ Order cancelled.");
                }

                _orderSessions.Remove(user);
                break;
        }
    }

    // ==============================================================

    // ================================================================
    // BUTTON CLICK HANDLER
    // ================================================================
    private async Task HandleMenuSelection(string from, string id)
    {
        switch (id)
        {
            case "MENU_PRODUCTS":
                await SendMessage(from, "📦 Products:\n- Bulbs\n- Fans\n- Cable\n- Automation");
                break;

            case "MENU_PRICING":
                await SendMessage(from, "💰 Pricing starts at ₹499.");
                break;

            case "MENU_ORDER":
                await StartOrder(from);
                break;

            default:
                await SendMessage(from, "Invalid option.");
                break;
        }
    }

    // ====================================================================
    // SEND TEXT
    // ====================================================================
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

    // ====================================================================
    // DOWNLOAD IMAGE → RETURNS PATH
    // ====================================================================
    private async Task<string> DownloadImage(string mediaId, string mimeType)
    {
        try
        {
            string metaUrl = $"https://graph.facebook.com/v20.0/{mediaId}";
            var metaReq = new HttpRequestMessage(HttpMethod.Get, metaUrl);
            metaReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _whatsappToken);

            var metaRes = await _http.SendAsync(metaReq);
            string metaJson = await metaRes.Content.ReadAsStringAsync();

            var doc = JsonDocument.Parse(metaJson);
            string mediaUrl = doc.RootElement.GetProperty("url").GetString();

            var imgReq = new HttpRequestMessage(HttpMethod.Get, mediaUrl);
            imgReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _whatsappToken);

            var imgRes = await _http.SendAsync(imgReq);
            byte[] bytes = await imgRes.Content.ReadAsByteArrayAsync();

            string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string ext = mimeType.Contains("png") ? "png" : "jpg";
            string filePath = Path.Combine(folder, $"received_{mediaId}.{ext}");

            System.IO.File.WriteAllBytes(filePath, bytes);

            return filePath;
        }
        catch
        {
            return "Error saving image";
        }
    }

    // ====================================================================
    // SEND MENU
    // ====================================================================
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
                body = new { text = "Welcome 👋\nChoose an option:" },
                action = new
                {
                    buttons = new[]
                    {
                        new { type="reply", reply=new { id="MENU_PRODUCTS", title="📦 Products" } },
                        new { type="reply", reply=new { id="MENU_PRICING", title="💰 Pricing" } },
                        new { type="reply", reply=new { id="MENU_ORDER", title="🛒 Order" } }
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

