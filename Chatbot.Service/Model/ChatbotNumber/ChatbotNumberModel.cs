using System;

namespace Chatbot.Service.Model.ChatbotNumber
{
    public class ChatbotNumberModel
    {
        public Guid chatbot_number_id { get; set; }
        public Guid? created_by { get; set; }
        public DateTime created_date { get; set; }
        public Guid? updated_by { get; set; }
        public DateTime last_updated { get; set; }
        public DateTime? rowversion { get; set; }
        public string nama { get; set; } = string.Empty;
        public string? deskripsi { get; set; }
        public string nomor { get; set; } = string.Empty;
        public Guid chatbot_character_id { get; set; }
        public string? id { get; set; }
    }
}
