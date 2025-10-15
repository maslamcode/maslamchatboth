using Chatbot.Service.Model.Broadcast;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Chatbot.Service.Services.Broadcast
{
    public class BroadcastMessageService : IBroadcastMessageService
    {
        private readonly string _connectionString;
        private readonly IConfiguration _config;

        public BroadcastMessageService(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("PostgreSqlConnection")
                ?? throw new ArgumentNullException("Connection string 'PostgreSqlConnection' not found.");
        }

        private NpgsqlConnection GetConnection() => new NpgsqlConnection(_connectionString);

        public async Task<IEnumerable<BroadcastMessageModel>> GetAllAsync()
        {
            using var conn = GetConnection();

            const string sql = @"
                SELECT 
                    broadcast_message_id AS BroadcastMessageId,
                    created_by AS CreatedBy,
                    created_date AS CreatedDate,
                    updated_by AS UpdatedBy,
                    last_updated AS LastUpdated,
                    rowversion AS RowVersion,
                    is_random AS IsRandom,
                    title AS Title,
                    message_content AS MessageContent,
                    is_active AS IsActive
                FROM chatbot.broadcast_message
                ORDER BY created_date DESC;";

            return await conn.QueryAsync<BroadcastMessageModel>(sql);
        }

        public async Task<BroadcastMessageModel?> GetByIdAsync(Guid broadcastMessageId)
        {
            using var conn = GetConnection();

            const string sql = @"
                SELECT 
                    broadcast_message_id AS BroadcastMessageId,
                    created_by AS CreatedBy,
                    created_date AS CreatedDate,
                    updated_by AS UpdatedBy,
                    last_updated AS LastUpdated,
                    rowversion AS RowVersion,
                    is_random AS IsRandom,
                    title AS Title,
                    message_content AS MessageContent,
                    is_active AS IsActive
                FROM chatbot.broadcast_message
                WHERE broadcast_message_id = @BroadcastMessageId;";

            return await conn.QuerySingleOrDefaultAsync<BroadcastMessageModel>(sql, new { BroadcastMessageId = broadcastMessageId });
        }

    }
}
