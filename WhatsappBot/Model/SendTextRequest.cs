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
    public class WhatsAppMediaMessage
    {
        public string From { get; set; }
        public string MediaId { get; set; }
        public string MimeType { get; set; }
        public string Caption { get; set; }
    }
    public class OrderState
    {
        public string Step { get; set; }
        public string Product { get; set; }
        public int Quantity { get; set; }
    }
    public class OrderSession
    {
        public string Phone { get; set; }
        public int Step { get; set; } 
        public string ProductName { get; set; }
        public string Quantity { get; set; }
        public string Address { get; set; }
        public string User { get; set; }
    }


}

