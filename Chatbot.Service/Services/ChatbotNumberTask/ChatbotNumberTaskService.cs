using Chatbot.Service.Model.ChatbotNumberTask;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Chatbot.Service.Services.ChatbotNumberTask
{
    public class ChatbotNumberTaskService : IChatbotNumberTaskService
    {
        private readonly string _connectionString;
        private readonly IConfiguration _config;

        public ChatbotNumberTaskService(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("PostgreSqlConnection")
                ?? throw new ArgumentNullException("Connection string 'PostgreSqlConnection' not found.");
        }

        private NpgsqlConnection GetConnection() => new NpgsqlConnection(_connectionString);

        public async Task<IEnumerable<ChatbotNumberTaskModel>> GetAllTasksAsync()
        {
            using var conn = GetConnection();
            var sql = @"SELECT chatbot_number_task_id,
                               chatbot_number_id,
                               chatbot_task_list_id,
                               created_by,
                               created_date,
                               updated_by,
                               last_updated,
                               rowversion
                        FROM chatbot.chatbot_number_task
                        ORDER BY created_date DESC";

            return await conn.QueryAsync<ChatbotNumberTaskModel>(sql);
        }

        public async Task<IEnumerable<ChatbotNumberTaskModel>> GetAllTasksByNumberIdAsync(Guid chatbotNumberId)
        {
            using var conn = GetConnection();
            var sql = @"SELECT chatbot_number_task_id,
                               chatbot_number_id,
                               chatbot_task_list_id,
                               created_by,
                               created_date,
                               updated_by,
                               last_updated,
                               rowversion
                        FROM chatbot.chatbot_number_task
                        WHERE chatbot_number_id = @chatbotNumberId
                        ORDER BY created_date DESC";

            return await conn.QueryAsync<ChatbotNumberTaskModel>(sql, new { chatbotNumberId });
        }

        public async Task<ChatbotNumberTaskModel?> GetTaskByIdAsync(Guid chatbotNumberTaskId)
        {
            using var conn = GetConnection();
            var sql = @"SELECT chatbot_number_task_id,
                               chatbot_number_id,
                               chatbot_task_list_id,
                               created_by,
                               created_date,
                               updated_by,
                               last_updated,
                               rowversion
                        FROM chatbot.chatbot_number_task
                        WHERE chatbot_number_task_id = @chatbotNumberTaskId";

            return await conn.QueryFirstOrDefaultAsync<ChatbotNumberTaskModel>(sql, new { chatbotNumberTaskId });
        }

    }
}
