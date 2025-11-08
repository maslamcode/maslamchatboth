using Chatbot.Service.Model.ChatbotNumber;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Chatbot.Service.Services.ChatbotNumber
{
    public class ChatbotNumberService : IChatbotNumberService
    {
        private readonly string _connectionString;
        private readonly IConfiguration _config;

        public ChatbotNumberService(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("PostgreSqlConnection")
                ?? throw new ArgumentNullException("Connection string 'PostgreSqlConnection' not found.");
        }

        private NpgsqlConnection GetConnection() => new NpgsqlConnection(_connectionString);

        public async Task<IEnumerable<ChatbotNumberModel>> GetAllNumbersAsync()
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT chatbot_number_id,
                       created_by,
                       created_date,
                       updated_by,
                       last_updated,
                       rowversion,
                       nama,
                       deskripsi,
                       nomor,
                       chatbot_character_id,
                       id
                FROM chatbot.chatbot_number
                ORDER BY created_date DESC";

            return await conn.QueryAsync<ChatbotNumberModel>(sql);
        }

        public async Task<IEnumerable<ChatbotNumberModel>> GetAllNumbersByIdsAsync(List<Guid> ids)
        {
            if (ids == null || ids.Count == 0)
                return Enumerable.Empty<ChatbotNumberModel>();

            using var conn = GetConnection();

            var sql = @"
                SELECT chatbot_number_id,
                       created_by,
                       created_date,
                       updated_by,
                       last_updated,
                       rowversion,
                       nama,
                       deskripsi,
                       nomor,
                       chatbot_character_id,
                       id
                FROM chatbot.chatbot_number
                WHERE chatbot_number_id = ANY(@Ids)
                ORDER BY created_date DESC;";

            return await conn.QueryAsync<ChatbotNumberModel>(sql, new { Ids = ids });
        }

        public async Task<ChatbotNumberModel?> GetNumberByIdAsync(Guid chatbotNumberId)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT chatbot_number_id,
                       created_by,
                       created_date,
                       updated_by,
                       last_updated,
                       rowversion,
                       nama,
                       deskripsi,
                       nomor,
                       chatbot_character_id,
                       id
                FROM chatbot.chatbot_number
                WHERE chatbot_number_id = @chatbotNumberId";

            return await conn.QueryFirstOrDefaultAsync<ChatbotNumberModel>(sql, new { chatbotNumberId });
        }


    }
}
