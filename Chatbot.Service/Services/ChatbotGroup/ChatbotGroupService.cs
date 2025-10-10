

using Chatbot.Service.Model.ChatbotGroup;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Chatbot.Service.Services.ChatbotGroup
{
    public class ChatbotGroupService : IChatbotGroupService
    {
        private readonly string _connectionString;
        private readonly IConfiguration _config;

        public ChatbotGroupService(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("PostgreSqlConnection")
                ?? throw new ArgumentNullException("Connection string 'PostgreSqlConnection' not found.");
        }

        private NpgsqlConnection GetConnection() => new NpgsqlConnection(_connectionString);

        public async Task<IEnumerable<ChatbotGroupModel>> GetAllGroupsAsync()
        {
            using var conn = GetConnection();
            var sql = @"SELECT chatbot_group_id, 
                               group_name, 
                               group_id, 
                               is_receive_broadcast, 
                               created_date, 
                               updated_by, 
                               last_updated, 
                               rowversion
                        FROM chatbot.chatbot_group
                        ORDER BY created_date DESC";

            return await conn.QueryAsync<ChatbotGroupModel>(sql);
        }

        public async Task<ChatbotGroupModel?> GetGroupByIdAsync(Guid chatbotGroupId)
        {
            using var conn = GetConnection();
            var sql = @"SELECT chatbot_group_id, 
                               group_name, 
                               group_id, 
                               is_receive_broadcast, 
                               created_date, 
                               updated_by, 
                               last_updated, 
                               rowversion
                        FROM chatbot.chatbot_group
                        WHERE chatbot_group_id = @chatbotGroupId";

            return await conn.QueryFirstOrDefaultAsync<ChatbotGroupModel>(sql, new { chatbotGroupId });
        }

        public async Task<int> InsertGroupAsync(ChatbotGroupModel model)
        {
            using var conn = GetConnection();

            var exists = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM chatbot.chatbot_group WHERE group_id = @group_id",
                new { model.group_id });

            if (exists > 0)
            {
                // Skip insert
                return 0;
            }

            var sql = @"
                INSERT INTO chatbot.chatbot_group 
                    (chatbot_group_id, group_name, group_id, is_receive_broadcast, created_date, updated_by, last_updated, rowversion)
                VALUES 
                    (@chatbot_group_id, @group_name, @group_id, @is_receive_broadcast, CURRENT_TIMESTAMP, @updated_by, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
            ";

            return await conn.ExecuteAsync(sql, model);
        }


        public async Task<int> UpdateGroupAsync(ChatbotGroupModel model)
        {
            using var conn = GetConnection();
            var sql = @"UPDATE chatbot.chatbot_group
                        SET group_name = @group_name,
                            group_id = @group_id,
                            is_receive_broadcast = @is_receive_broadcast,
                            updated_by = @updated_by,
                            last_updated = CURRENT_TIMESTAMP,
                            rowversion = CURRENT_TIMESTAMP
                        WHERE chatbot_group_id = @chatbot_group_id";

            return await conn.ExecuteAsync(sql, model);
        }

        public async Task<int> DeleteGroupAsync(Guid chatbotGroupId)
        {
            using var conn = GetConnection();
            var sql = @"DELETE FROM chatbot.chatbot_group 
                        WHERE chatbot_group_id = @chatbotGroupId";

            return await conn.ExecuteAsync(sql, new { chatbotGroupId });
        }
    }
}
