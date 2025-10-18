using Chatbot.Service.Model.Chatbot;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Chatbot.Service.Services.Broadcast
{
    public class BroadcastScheduleService : IBroadcastScheduleService
    {
        private readonly string _connectionString;
        private readonly IConfiguration _config;

        public BroadcastScheduleService(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("PostgreSqlConnection")
                ?? throw new ArgumentNullException("Connection string 'PostgreSqlConnection' not found.");
        }

        private NpgsqlConnection GetConnection() => new NpgsqlConnection(_connectionString);

        public async Task<IEnumerable<BroadcastScheduleModel>> GetAllAsync()
        {
            using var conn = GetConnection();

            const string sql = @"
                SELECT 
                    broadcast_schedule_id AS BroadcastScheduleId,
                    created_by AS CreatedBy,
                    created_date AS CreatedDate,
                    updated_by AS UpdatedBy,
                    last_updated AS LastUpdated,
                    rowversion AS RowVersion,
                    broadcast_message_id AS BroadcastMessageId,
                    schedule_type AS ScheduleType,
                    schedule_datetime AS ScheduleDateTime,
                    day_of_week AS DayOfWeek,
                    schedule_time AS ScheduleTime,
                    is_active AS IsActive
                FROM chatbot.broadcast_schedule
                ORDER BY created_date DESC;";

            return await conn.QueryAsync<BroadcastScheduleModel>(sql);
        }

        public async Task<IEnumerable<BroadcastScheduleModel>> GetDueSchedulesAsync(DateTime now)
        {
            var objs = await GetAllAsync();

            return objs
                .Where(x => x.IsActive.HasValue && x.IsActive.Value &&
                    (
                        // One-time
                        (x.ScheduleType == 'O' && x.ScheduleDateTime <= now) ||

                        // Weekly
                        (x.ScheduleType == 'W' &&
                         x.DayOfWeek == (int)now.DayOfWeek &&
                         x.ScheduleTime <= now.TimeOfDay) ||

                        // Monthly
                        (x.ScheduleType == 'M' &&
                         x.DayOfWeek == (int)now.DayOfWeek &&
                         x.ScheduleTime <= now.TimeOfDay)
                    ))
                .ToList();
        }


    }
}
