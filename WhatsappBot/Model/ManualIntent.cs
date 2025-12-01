namespace WhatsappBot.Model
{
    public class ManualIntent
    {
        public readonly Dictionary<string, string> Intents = new()
        {
            { "hi", "greeting" },
            { "hello", "greeting" },
            { "hey", "greeting" },

            { "yes", "confirm" },
            { "yup", "confirm" },
            { "ok", "confirm" },
            { "sure", "confirm" },

            { "no", "reject" },
            { "nope", "reject" },
            { "cancel", "cancel" },

            { "book", "appointment" },
            { "appointment", "appointment" },

            { "order", "order" },

            { "price", "pricing" },
            { "cost", "pricing" },

            { "help", "help" },
        };

    }
}
