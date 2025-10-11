
namespace Chatbot.Service.Model.Chatbot
{
    public class ChatbotResponseModel
    {
        public int id { get; set; }
        public int type { get; set; }
        public string? tag_message { get; set; }
        public int order { get; set; }
    }
}
