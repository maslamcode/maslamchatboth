using System;

namespace Chatbot.Service.Model.Chatbot
{
    public class BroadcastTargetModel
    {
        public Guid BroadcastTargetId { get; set; }
        public Guid BroadcastMessageId { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? LastUpdated { get; set; }
        public DateTime? RowVersion { get; set; }

        /// G = Group, P = Personal
        public char TargetType { get; set; }

        /// Filled if Type = G (Group)
        public Guid? ChatbotGroupId { get; set; }

        /// Filled if Type = P (Personal)
        public string? NoWa { get; set; }
    }
}
