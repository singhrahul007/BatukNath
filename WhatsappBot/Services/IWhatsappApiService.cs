namespace WhatsappBot.Services
{
    public interface IWhatsAppApiService
    {
        Task<string> SendTextAsync(string number, string message);
        Task<string> SendMediaAsync(string number, string mediaUrl, string caption);
        Task<string> SendTemplateAsync(string number, string templateName, object parameters);
        Task<string> UploadMedia(string filePath, string mimeType);
        Task SendImage(string to, string mediaId, string caption = "");
        Task<string> GetMediaUrl(string mediaId);
    }
}
