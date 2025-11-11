using System;

namespace Chatbot.Service.Model.Chatbot
{
    public class BroadcastScheduleModel
    {
        public Guid broadcast_schedule_id { get; set; }
        public Guid created_by { get; set; }
        public DateTime created_date { get; set; }
        public Guid? updated_by { get; set; }
        public DateTime? last_updated { get; set; }
        public DateTime? rowversion { get; set; }
        public DateTime? last_executed_date { get; set; }

        public Guid broadcast_message_id { get; set; }

        /// 'O' : Once, 'W' : Weekly, 'M' : Monthly
        public char schedule_type { get; set; }

        /// Used for one-time (O) schedule type.
        public DateTime? schedule_datetime { get; set; }

        /// 0 = Sunday, 1 = Monday, ..., 6 = Saturday
        public short? day_of_week { get; set; }

        /// Time of day for recurring schedules.
        public TimeSpan? schedule_time { get; set; }

        public bool? is_active { get; set; }
    }
}
