using System.Text.Json;

namespace WhatsappBot.Services
{
   

        public class MockWhatsAppApiService : IWhatsAppApiService
        {
            public Task<string> SendTextAsync(string number, string message)
            {
                var response = new
                {
                    status = "success",
                    messageId = Guid.NewGuid().ToString(),
                    to = number,
                    message = message,
                    type = "text"
                };

                return Task.FromResult(JsonSerializer.Serialize(response));
            }

            public Task<string> SendMediaAsync(string number, string url, string caption)
            {
                var response = new
                {
                    status = "success",
                    messageId = Guid.NewGuid().ToString(),
                    to = number,
                    mediaUrl = url,
                    caption = caption,
                    type = "media"
                };

                return Task.FromResult(JsonSerializer.Serialize(response));
            }

            public Task<string> SendTemplateAsync(string number, string templateName, object parameters)
            {
                var response = new
                {
                    status = "success",
                    messageId = Guid.NewGuid().ToString(),
                    to = number,
                    template = templateName,
                    parameters,
                    type = "template"
                };

                return Task.FromResult(JsonSerializer.Serialize(response));
            }
            public async Task<bool> SendMessage(string to, string message)
            {
                // For now mock sending
                Console.WriteLine($"Sending WhatsApp Message To {to}: {message}");
                return await Task.FromResult(true);
            }
        }
    
}
