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
                    broadcast_target_id AS BroadcastTargetId,
                    created_by AS CreatedBy,
                    created_date AS CreatedDate,
                    updated_by AS UpdatedBy,
                    last_updated AS LastUpdated,
                    rowversion AS RowVersion,
                    target_type AS TargetType,
                    chatbot_group_id AS ChatbotGroupId,
                    no_wa AS NoWa
                FROM chatbot.broadcast_target
                ORDER BY created_date DESC;";

            return await conn.QueryAsync<BroadcastTargetModel>(sql);
        }
    }
}
