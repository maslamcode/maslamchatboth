using Chatbot.Service.Model.Chatbot;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Chatbot.Service.Services.Broadcast
{
    public class BroadcastTargetService : IBroadcastTargetService
    {
        private readonly string _connectionString;
        private readonly IConfiguration _config;

        public BroadcastTargetService(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("PostgreSqlConnection")
                ?? throw new ArgumentNullException("Connection string 'PostgreSqlConnection' not found.");
        }

        private NpgsqlConnection GetConnection() => new NpgsqlConnection(_connectionString);

        public async Task<IEnumerable<BroadcastTargetModel>> GetAllAsync()
        {
            using var conn = GetConnection();

            const string sql = @"
            SELECT 
                broadcast_target_id,
                broadcast_message_id,
                created_by,
                created_date,
                updated_by,
                last_updated,
                rowversion,
                target_type,
                chatbot_group_id,
                no_wa
            FROM chatbot.broadcast_target
            ORDER BY created_date DESC;";

            return await conn.QueryAsync<BroadcastTargetModel>(sql);
        }

        public async Task<IEnumerable<BroadcastTargetModel>> GetByBroadcastMessageIdAsync(Guid broadcastMessageId)
        {
            using var conn = GetConnection();

            const string sql = @"
            SELECT 
                broadcast_target_id,
                broadcast_message_id,
                created_by,
                created_date,
                updated_by,
                last_updated,
                rowversion,
                target_type,
                chatbot_group_id,
                no_wa
            FROM chatbot.broadcast_target
            WHERE broadcast_message_id = @broadcastMessageId
            ORDER BY created_date DESC;";

            return await conn.QueryAsync<BroadcastTargetModel>(sql, new { broadcastMessageId });
        }

    }
}
