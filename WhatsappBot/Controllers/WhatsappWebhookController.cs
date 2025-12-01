using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WhatsappBot.Model;
using static WhatsappBot.Model.appointmentsBookingModel;

[Route("webhook")]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly HttpClient _http;
    private readonly NlpService _nlp;
    private readonly string? _phoneNumberId;
    private readonly string? _whatsappToken;
    private readonly ManualIntent _manualIntent;

    // ============================================
    // 🔥 ORDER + BOOKING SESSIONS
    // ============================================
    private static Dictionary<string, OrderSession> _orderSessions = new();
    private static Dictionary<string, AppointmentSession> _bookingSessions = new();

    public WhatsAppWebhookController(IConfiguration config, NlpService nlp, ManualIntent manualIntent)
    {
        _config = config;
        _http = new HttpClient();
        _nlp = nlp;
        _phoneNumberId = _config["WhatsApp:PhoneNumberId"];
        _whatsappToken = _config["WhatsApp:AccessToken"];
        _manualIntent = manualIntent;
        if (string.IsNullOrEmpty(_phoneNumberId) || string.IsNullOrEmpty(_whatsappToken))
            throw new Exception("WhatsApp PhoneNumberId or AccessToken missing");
        
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
    [HttpPost]
    public async Task<IActionResult> ReceiveMessage([FromBody] JsonElement body)
    {
        Console.WriteLine(body.ToString());

        var value = body.GetProperty("entry")[0]
                        .GetProperty("changes")[0]
                        .GetProperty("value");

        if (!value.TryGetProperty("messages", out var messages))
            return Ok();

        var msg = messages[0];
        string sender = msg.GetProperty("from").GetString();

        string text = null;
        if (msg.TryGetProperty("text", out var textObj))
            text = textObj.GetProperty("body").GetString().ToLower();

        // ------------------------------------------------------
        // 1️⃣ IMAGE MESSAGE
        // ------------------------------------------------------
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

        // ------------------------------------------------------
        // 2️⃣ BUTTON
        // ------------------------------------------------------
        if (msg.TryGetProperty("interactive", out var interactive))
        {
            string buttonId = interactive.GetProperty("button_reply").GetProperty("id").GetString();
            await HandleMenuSelection(sender, buttonId);
            return Ok();
        }

        // ------------------------------------------------------
        // 3️⃣ TEXT MESSAGE
        // ------------------------------------------------------
        if (text != null)
        {
            // ------------------------------------------------------
            // 🔥 STEP 1: MANUAL INTENTS FIRST
            // ------------------------------------------------------
            if (_orderSessions.ContainsKey(sender))
            {
                await ContinueOrderProcess(sender, text);
                return Ok();
            }

            if (_bookingSessions.ContainsKey(sender))
            {
                await ContinueBooking(sender, text);
                return Ok();
            }

            // Manual predefined intents:
            if (text == "order")
            {
                await StartOrder(sender);
                return Ok();
            }

            if (text == "book" || text == "appointment")
            {
                await StartAppointmentBooking(sender);
                return Ok();
            }

            if (text == "menu" || text == "hi" || text == "hello")
            {
                await SendMenu(sender);
                return Ok();
            }

            if (text.Contains("price"))
            {
                await SendMessage(sender, "Our pricing starts at ₹499.");
                return Ok();
            }

            if (text.Contains("help"))
            {
                await SendMessage(sender, "Sure! How can I assist?");
                return Ok();
            }
            // ------------------------------------------------------
            // 🔥  MANUAL INTENT CHECK FIRST (fast, accurate)
            // ------------------------------------------------------
            string detectedIntent = null;

            if (text != null)
            {
                foreach (var kv in _manualIntent.Intents)
                {
                    if (text.Contains(kv.Key))
                    {
                        detectedIntent = kv.Value;
                        Console.WriteLine("🔥 Manual Intent Detected: " + detectedIntent);
                        break;
                    }
                }
            }

            // ------------------------------------------------------
            // 🔥 STEP 2: IF MANUAL INTENT FAILS → NLP (GPT-4o Mini)
            // ------------------------------------------------------
            string aiResponse = await _nlp.DetectIntent(text);
            Console.WriteLine("NLP Response: " + aiResponse);

            string intent = ExtractIntent(aiResponse);

            // Example custom routing:
            if (intent == "order")
            {
                await StartOrder(sender);
                return Ok();
            }
            else if (intent == "appointment")
            {
                await StartAppointmentBooking(sender);
                return Ok();
            }

            // ------------------------------------------------------
            // 🔥 STEP 3: DEFAULT WHEN NLP ALSO FAILS
            // ------------------------------------------------------
            await SendMessage(sender, "I didn't understand. Type *menu* to see options.");
            return Ok();
        }

        return Ok();
    }

    // ====================================================================
    // AUTO REPLY
    // ====================================================================
    private string GetAutoReply(string text)
    {
        if (text.Contains("price")) return "Our pricing starts at ₹499.";
        if (text.Contains("help")) return "Sure! How can I assist?";
        return "I didn't understand. Type *menu* to see options.";
    }

    // ====================================================================
    // ORDER WORKFLOW
    // ====================================================================
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
        var s = _orderSessions[user];

        switch (s.Step)
        {
            case 1:
                s.ProductName = input;
                s.Step = 2;
                await SendMessage(user, "How many quantity?");
                break;

            case 2:
                s.Quantity = input;
                s.Step = 3;
                await SendMessage(user, "Please provide your delivery address:");
                break;

            case 3:
                s.Address = input;
                s.Step = 4;

                await SendMessage(user,
                    $"🧾 *Order Summary*\n" +
                    $"Product: {s.ProductName}\n" +
                    $"Quantity: {s.Quantity}\n" +
                    $"Address: {s.Address}\n\n" +
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

    // ====================================================================
    // BUTTON HANDLER
    // ====================================================================
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
            case "MENU_BOOK_APPOINTMENT":
                await StartAppointmentBooking(from);
                break;

            case "MENU_MY_APPOINTMENTS":

                if (_bookingSessions.ContainsKey(from))
                {
                    var b = _bookingSessions[from];

                    await SendMessage(from,
                        $"📖 *Your Current Booking Progress:*\n" +
                        $"Name: {b.Name ?? "-"}\n" +
                        $"Service: {b.Service ?? "-"}\n" +
                        $"Date: {b.Date ?? "-"}\n" +
                        $"Time: {b.Time ?? "-"}\n\n" +
                        "Continue typing where you left.");
                }
                else
                {
                    await SendMessage(from,
                        "❗ You have no active bookings.\nSend *book* to start an appointment.");
                }

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
    // IMAGE DOWNLOAD
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
    // MENU
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
                body = new { text = "Welcome Sir👋\nChoose an option:" },
                action = new
                {
                    buttons = new[]
                    {
                        new { type="reply", reply=new { id="MENU_PRODUCTS", title="📦 Products" } },
                        new { type="reply", reply=new { id="MENU_PRICING", title="💰 Pricing" } },
                        new { type="reply", reply=new { id="MENU_ORDER", title="🛒 Order" } },
                        new { type="reply", reply=new { id="MENU_BOOK_APPOINTMENT", title="📅 Book Appointment" } },
                        new { type="reply", reply=new { id="MENU_MY_APPOINTMENTS", title="📖 My Appointments" } },

                    }
                }
            }
        };

        var req = new HttpRequestMessage(HttpMethod.Post, apiUrl);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _whatsappToken);
        req.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

        await _http.SendAsync(req);
    }

    // ====================================================================
    // APPOINTMENT BOOKING
    // ====================================================================
    private async Task StartAppointmentBooking(string user)
    {
        _bookingSessions[user] = new AppointmentSession();
        await SendMessage(user, "📅 *Appointment Booking Started!*\nPlease enter your *full name*:");
    }

    private async Task ContinueBooking(string user, string input)
    {
        var s = _bookingSessions[user];

        switch (s.Step)
        {
            case "name":
                s.Name = input;
                s.Step = "service";
                await SendMessage(user, "Select your service:\n1. Dental Checkup\n2. Eye Checkup\n3. Physiotherapy");
                break;

            case "service":
                s.Service = input switch
                {
                    "1" => "Dental Checkup",
                    "2" => "Eye Checkup",
                    "3" => "Physiotherapy",
                    _ => null
                };

                if (s.Service == null)
                {
                    await SendMessage(user, "❌ Invalid option.");
                    return;
                }

                s.Step = "date";
                await SendMessage(user, "Enter appointment date (DD-MM-YYYY):");
                break;

            case "date":
                s.Date = input;
                s.Step = "time";
                await SendMessage(user, "Choose time slot:\n1. 10 AM\n2. 11 AM\n3. 3 PM\n4. 5 PM");
                break;

            case "time":
                s.Time = input switch
                {
                    "1" => "10 AM",
                    "2" => "11 AM",
                    "3" => "3 PM",
                    "4" => "5 PM",
                    _ => null
                };

                if (s.Time == null)
                {
                    await SendMessage(user, "❌ Invalid time slot.");
                    return;
                }

                s.Step = "confirm";
                await SendMessage(user,
                    $"🔔 *Confirm Appointment*\n\n" +
                    $"Name: {s.Name}\n" +
                    $"Service: {s.Service}\n" +
                    $"Date: {s.Date}\n" +
                    $"Time: {s.Time}\n\n" +
                    "Reply YES to confirm or NO to cancel.");
                break;

            case "confirm":
                if (input.ToLower() == "yes")
                    await SendMessage(user, "🎉 *Appointment Confirmed!*");

                else
                    await SendMessage(user, "❌ Appointment Cancelled.");

                _bookingSessions.Remove(user);
                break;
        }
    }

    public string ExtractIntent(string json)
    {
        dynamic obj = JsonConvert.DeserializeObject(json);
        return obj.choices[0].message.content;
    }

}
