using Microsoft.AspNetCore.Mvc;
using WhatsappBot.Model;
using WhatsappBot.Services;

namespace WhatsappBot.Controllers
{
    [Route("api/whatsapp")]
    [ApiController]
    public class WhatsAppController : ControllerBase
    {
        private readonly IWhatsAppApiService _whatsAppService;

        public WhatsAppController(IWhatsAppApiService whatsAppService)
        {
            _whatsAppService = whatsAppService;
        }

        [HttpPost("send-text")]
        public async Task<IActionResult> SendText([FromBody] SendTextRequest body)
        {
            var result = await _whatsAppService.SendTextAsync(body.Number, body.Message);
            return Ok(result);
        }

        [HttpPost("send-media")]
        public async Task<IActionResult> SendMedia([FromBody] SendMediaRequest body)
        {
            var result = await _whatsAppService.SendMediaAsync(body.Number, body.Url, body.Caption);
            return Ok(result);
        }

        [HttpPost("send-template")]
        public async Task<IActionResult> SendTemplate([FromBody] SendTemplateRequest body)
        {
            var result = await _whatsAppService.SendTemplateAsync(body.Number, body.TemplateName, body.Parameters);
            return Ok(result);
        }
    }
}
