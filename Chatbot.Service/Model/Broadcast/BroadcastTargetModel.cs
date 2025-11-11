using System;

namespace Chatbot.Service.Model.Chatbot
{
    public class BroadcastTargetModel
    {
        public Guid broadcast_target_id { get; set; }
        public Guid broadcast_message_id { get; set; }
        public Guid created_by { get; set; }
        public DateTime created_date { get; set; }
        public Guid? updated_by { get; set; }
        public DateTime? last_updated { get; set; }
        public DateTime? rowversion { get; set; }

        /// G = Group, P = Personal
        public char target_type { get; set; }

        /// Filled if Type = G (Group)
        public Guid? chatbot_group_id { get; set; }

        /// Filled if Type = P (Personal)
        public string? no_wa { get; set; }
    }
}
