namespace WhatsappBot.Model
{
    public class appointmentsBookingModel
    {
        public class AppointmentSession
        {
            public string Step { get; set; } = "name";
            public string Name { get; set; }
            public string Service { get; set; }
            public string Date { get; set; }
            public string Time { get; set; }
        }

    }
}
