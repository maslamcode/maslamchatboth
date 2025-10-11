
namespace Chatbot.Service.Model.Chatbot
{
    public class ChatbotDataModel
    {
        public Guid chatbot_data_id { get; set; }
        public Guid created_by { get; set; }
        public DateTime created_date { get; set; }
        public Guid updated_by { get; set; }
        public DateTime last_updated { get; set; }
        public DateTime rowversion { get; set; }
        public string? prompt_words { get; set; }
        public string? data_link_online { get; set; }
    }

    public class ChatbotFileModel
    {
        public Guid chatbot_data_id { get; set; }
        public Guid created_by { get; set; }
        public DateTime created_date { get; set; }
        public Guid updated_by { get; set; }
        public DateTime last_updated { get; set; }
        public DateTime rowversion { get; set; }
        public string? prompt_words { get; set; }
        public string? file_name { get; set; }
    }
}
