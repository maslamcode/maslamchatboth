namespace Chatbot.Service.Model.ChatbotGroup
{
    public class ChatbotGroupModel
    {
        public Guid chatbot_group_id { get; set; }
        public string? group_name { get; set; }
        public string? group_id { get; set; }
        public bool is_receive_broadcast { get; set; }
        public DateTime created_date { get; set; }
        public Guid? updated_by { get; set; }
        public DateTime last_updated { get; set; }
        public DateTime rowversion { get; set; }
    }
}
