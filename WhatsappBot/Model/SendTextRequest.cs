namespace WhatsappBot.Model
{
    public class SendTextRequest
    {
        
        public string Number { get; set; }
        public string Message { get; set; }
    }
    public class SendMediaRequest
    {
        public string Number { get; set; }
        public string Url { get; set; }
        public string Caption { get; set; }
    }
    public class SendTemplateRequest
    {
        public string Number { get; set; }
        public string TemplateName { get; set; }
        public object Parameters { get; set; }
    }
    public class MessageLog
    {
        public int Id { get; set; }
        public string To { get; set; }
        public string MessageType { get; set; } // text, media, template
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public string Status { get; set; } // success, failed
    }
}

