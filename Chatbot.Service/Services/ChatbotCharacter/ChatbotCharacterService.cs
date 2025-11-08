using Chatbot.Service.Model.ChatbotCharacter;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Chatbot.Service.Services.ChatbotCharacter
{
    public class ChatbotCharacterService : IChatbotCharacterService
    {
        private readonly string _connectionString;
        private readonly IConfiguration _config;

        public ChatbotCharacterService(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("PostgreSqlConnection")
                ?? throw new ArgumentNullException("Connection string 'PostgreSqlConnection' not found.");
        }

        private NpgsqlConnection GetConnection() => new NpgsqlConnection(_connectionString);

        public async Task<IEnumerable<ChatbotCharacterModel>> GetAllCharactersAsync()
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT chatbot_character_id,
                       created_by,
                       created_date,
                       updated_by,
                       last_updated,
                       rowversion,
                       nama,
                       deskripsi,
                       persona
                FROM chatbot.chatbot_character
                ORDER BY created_date DESC";

            return await conn.QueryAsync<ChatbotCharacterModel>(sql);
        }

        public async Task<IEnumerable<ChatbotCharacterModel>> GetAllCharactersByIdsAsync(List<Guid> ids)
        {
            if (ids == null || ids.Count == 0)
                return Enumerable.Empty<ChatbotCharacterModel>();

            using var conn = GetConnection();

            var sql = @"
                SELECT chatbot_character_id,
                       created_by,
                       created_date,
                       updated_by,
                       last_updated,
                       rowversion,
                       nama,
                       deskripsi,
                       persona
                FROM chatbot.chatbot_character
                WHERE chatbot_character_id = ANY(@Ids)
                ORDER BY created_date DESC;";

            return await conn.QueryAsync<ChatbotCharacterModel>(sql, new { Ids = ids });
        }

        public async Task<ChatbotCharacterModel?> GetCharacterByIdAsync(Guid chatbotCharacterId)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT chatbot_character_id,
                       created_by,
                       created_date,
                       updated_by,
                       last_updated,
                       rowversion,
                       nama,
                       deskripsi,
                       persona
                FROM chatbot.chatbot_character
                WHERE chatbot_character_id = @chatbotCharacterId";

            return await conn.QueryFirstOrDefaultAsync<ChatbotCharacterModel>(sql, new { chatbotCharacterId });
        }

    }
}
