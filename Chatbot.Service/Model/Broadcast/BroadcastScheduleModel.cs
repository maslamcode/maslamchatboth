using System;

namespace Chatbot.Service.Model.Chatbot
{
    public class BroadcastScheduleModel
    {
        public Guid BroadcastScheduleId { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? LastUpdated { get; set; }
        public DateTime? RowVersion { get; set; }

        public Guid BroadcastMessageId { get; set; }

        /// 'O' : Once, 'W' : Weekly, 'M' : Monthly
        public char ScheduleType { get; set; }

        /// Used for one-time (O) schedule type.
        public DateTime? ScheduleDateTime { get; set; }

        /// 0 = Sunday, 1 = Monday, ..., 6 = Saturday
        public short? DayOfWeek { get; set; }

        /// Time of day for recurring schedules.
        public TimeSpan? ScheduleTime { get; set; }

        public bool? IsActive { get; set; }
    }
}
