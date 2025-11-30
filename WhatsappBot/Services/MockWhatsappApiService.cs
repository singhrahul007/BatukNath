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
            public Task<string> UploadMedia(string filePath, string mimeType)
            {
                Console.WriteLine($"MOCK UPLOAD MEDIA: {filePath}");
                return Task.FromResult("mock-media-id-1234");
            }

            public Task SendImage(string to, string mediaId, string caption = "")
            {
                Console.WriteLine($"MOCK SEND IMAGE → {to}: mediaId={mediaId}, caption={caption}");
                return Task.CompletedTask;
            }

            public Task<string> GetMediaUrl(string mediaId)
            {
                Console.WriteLine($"MOCK GET MEDIA URL: {mediaId}");
                return Task.FromResult("https://dummy-url.com/image.jpg");
            }
    }
    
}
