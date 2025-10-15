using System;

namespace Chatbot.Service.Model.MessageList
{
    public class MessageListModel
    {
        public Guid MessageListId { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public DateTime? RowVersion { get; set; }

        public string Title { get; set; } = string.Empty;
        public string MessageContent { get; set; } = string.Empty;

        /// 0 = Sunday, 1 = Monday, ..., 6 = Saturday
        public short? DayOfWeek { get; set; }

        public bool IsActive { get; set; } = true;

        public short? Sequence { get; set; }
    }
}
