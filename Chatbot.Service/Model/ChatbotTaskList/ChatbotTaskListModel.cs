using System;

namespace Chatbot.Service.Model.ChatbotTaskList
{
    public class ChatbotTaskListModel
    {
        public Guid chatbot_task_list_id { get; set; }

        public Guid? created_by { get; set; }
        public DateTime created_date { get; set; } = DateTime.UtcNow;

        public Guid? updated_by { get; set; }
        public DateTime last_updated { get; set; } = DateTime.UtcNow;

        public DateTime? rowversion { get; set; } = DateTime.UtcNow;

        public string nama { get; set; } = string.Empty;
        public string? deskripsi { get; set; }
        public string task_list { get; set; } = string.Empty;
    }
}
