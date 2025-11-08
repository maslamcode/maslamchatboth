using Chatbot.Service.Model.ChatbotTaskList;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Chatbot.Service.Services.ChatbotTaskList
{
    public class ChatbotTaskListService : IChatbotTaskListService
    {
        private readonly string _connectionString;
        private readonly IConfiguration _config;

        public ChatbotTaskListService(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("PostgreSqlConnection")
                ?? throw new ArgumentNullException("Connection string 'PostgreSqlConnection' not found.");
        }

        private NpgsqlConnection GetConnection() => new NpgsqlConnection(_connectionString);

        public async Task<IEnumerable<ChatbotTaskListModel>> GetAllTaskListsAsync()
        {
            using var conn = GetConnection();
            var sql = @"SELECT chatbot_task_list_id,
                               nama,
                               deskripsi,
                               task_list,
                               created_by,
                               created_date,
                               updated_by,
                               last_updated,
                               rowversion
                        FROM chatbot.chatbot_task_list
                        ORDER BY created_date DESC";

            return await conn.QueryAsync<ChatbotTaskListModel>(sql);
        }

        public async Task<ChatbotTaskListModel?> GetTaskListByIdAsync(Guid chatbotTaskListId)
        {
            using var conn = GetConnection();
            var sql = @"SELECT chatbot_task_list_id,
                               nama,
                               deskripsi,
                               task_list,
                               created_by,
                               created_date,
                               updated_by,
                               last_updated,
                               rowversion
                        FROM chatbot.chatbot_task_list
                        WHERE chatbot_task_list_id = @chatbotTaskListId";

            return await conn.QueryFirstOrDefaultAsync<ChatbotTaskListModel>(sql, new { chatbotTaskListId });
        }

    }
}
