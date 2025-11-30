namespace WhatsappBot.Services
{
    public interface IWhatsAppApiService
    {
        Task<string> SendTextAsync(string number, string message);
        Task<string> SendMediaAsync(string number, string mediaUrl, string caption);
        Task<string> SendTemplateAsync(string number, string templateName, object parameters);
    }
}
