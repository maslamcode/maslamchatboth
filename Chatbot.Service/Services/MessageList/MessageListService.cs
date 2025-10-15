using Chatbot.Service.Model.MessageList;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Chatbot.Service.Services.MessageList
{
    public class MessageListService : IMessageListService
    {
        private readonly string _connectionString;
        private readonly IConfiguration _config;

        public MessageListService(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("PostgreSqlConnection")
                ?? throw new ArgumentNullException("Connection string 'PostgreSqlConnection' not found.");
        }

        private NpgsqlConnection GetConnection() => new NpgsqlConnection(_connectionString);

        public async Task<IEnumerable<MessageListModel>> GetAllAsync()
        {
            using var conn = GetConnection();

            const string sql = @"
                SELECT 
                    message_list_id AS MessageListId,
                    created_by AS CreatedBy,
                    created_date AS CreatedDate,
                    updated_by AS UpdatedBy,
                    updated_date AS UpdatedDate,
                    rowversion AS RowVersion,
                    title AS Title,
                    message_content AS MessageContent,
                    day_of_week AS DayOfWeek,
                    is_active AS IsActive,
                    sequence AS Sequence
                FROM chatbot.message_list
                ORDER BY created_date DESC;";

            return await conn.QueryAsync<MessageListModel>(sql);
        }
    }
}
