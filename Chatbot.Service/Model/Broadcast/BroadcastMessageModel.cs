using System;

namespace Chatbot.Service.Model.Broadcast
{
    public class BroadcastMessageModel
    {
        public Guid BroadcastMessageId { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? LastUpdated { get; set; }
        public DateTime? RowVersion { get; set; }
        public bool IsRandom { get; set; }
        public string? Title { get; set; }
        public string? MessageContent { get; set; }
        public bool IsActive { get; set; }
    }
}
