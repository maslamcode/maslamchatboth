using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeminiChatBot.Models
{
    public class ChatbotResponseModel
    {
        public int id { get; set; }
        public int type { get; set; }
        public string tag_message { get; set; }
        public int order { get; set; }
    }
}
