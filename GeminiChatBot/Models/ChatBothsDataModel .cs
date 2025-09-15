using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeminiChatBot.Models
{
    public class ChatBothsDataModel
    {
        public Guid chat_boths_data_id { get; set; }
        public Guid created_by { get; set; }
        public DateTime created_date { get; set; }
        public Guid updated_by { get; set; }
        public DateTime last_updated { get; set; }
        public DateTime rowversion { get; set; }
        public string prompt_words { get; set; }
        public string data_link_online { get; set; }
    }

    public class ChatBothsFileModel
    {
        public Guid chat_boths_data_id { get; set; }
        public Guid created_by { get; set; }
        public DateTime created_date { get; set; }
        public Guid updated_by { get; set; }
        public DateTime last_updated { get; set; }
        public DateTime rowversion { get; set; }
        public string prompt_words { get; set; }
        public string file_name { get; set; }
    }
}
