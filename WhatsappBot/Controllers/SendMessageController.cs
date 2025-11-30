using Microsoft.AspNetCore.Mvc;

[Route("send")]
public class SendMessageController : ControllerBase
{
    private readonly WhatsAppService _whatsApp;

    public SendMessageController(WhatsAppService whatsApp)
    {
        _whatsApp = whatsApp;
    }

    [HttpGet("{phone}/{msg}")]
    public async Task<IActionResult> SendText(string phone, string msg)
    {
        await _whatsApp.SendMessage(phone, msg);
        return Ok("Sent");
    }
}
