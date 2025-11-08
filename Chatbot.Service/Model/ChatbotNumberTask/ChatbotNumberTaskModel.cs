using System;

namespace Chatbot.Service.Model.ChatbotNumberTask
{
    public class ChatbotNumberTaskModel
    {
        public Guid chatbot_number_task_id { get; set; }
        public Guid chatbot_number_id { get; set; }
        public Guid chatbot_task_list_id { get; set; }

        public Guid? created_by { get; set; }
        public DateTime created_date { get; set; } = DateTime.UtcNow;
        public Guid? updated_by { get; set; }
        public DateTime last_updated { get; set; } = DateTime.UtcNow;
        public DateTime? rowversion { get; set; } = DateTime.UtcNow;
    }
}
