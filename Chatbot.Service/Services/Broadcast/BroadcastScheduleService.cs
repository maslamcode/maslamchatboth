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
                broadcast_schedule_id,
                created_by,
                created_date,
                updated_by,
                last_updated,
                rowversion,
                broadcast_message_id,
                schedule_type,
                schedule_datetime,
                day_of_week,
                schedule_time,
                is_active
            FROM chatbot.broadcast_schedule
            ORDER BY created_date DESC;";

            return await conn.QueryAsync<BroadcastScheduleModel>(sql);
        }

        public async Task<IEnumerable<BroadcastScheduleModel>> GetDueSchedulesAsync(DateTime now)
        {
            var objs = await GetAllAsync();

            return objs
                .Where(x => x.is_active.HasValue && x.is_active.Value &&
                    (
                        // One-time
                        (x.schedule_type == 'O' && x.schedule_datetime <= now) ||
                        
                        // Weekly
                        (x.schedule_type == 'W' &&
                         x.day_of_week == (int)now.DayOfWeek &&
                         x.schedule_time <= now.TimeOfDay) ||

                        // Monthly
                        (x.schedule_type == 'M' &&
                         x.day_of_week == (int)now.Day &&
                         x.schedule_time <= now.TimeOfDay)
                    ))
                .ToList();
        }


    }
}
