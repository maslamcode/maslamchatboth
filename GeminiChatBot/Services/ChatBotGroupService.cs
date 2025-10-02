using Dapper;
using GeminiChatBot.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeminiChatBot.Services
{
    public class ChatBotGroupService : IChatBotGroupService
    {
        private readonly string _connectionString;
        private readonly IConfiguration _config;

        public ChatBotGroupService(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("PostgreSqlConnection")
                ?? throw new ArgumentNullException("Connection string 'PostgreSqlConnection' not found.");
        }

        private NpgsqlConnection GetConnection() => new NpgsqlConnection(_connectionString);

        public async Task<IEnumerable<ChatBotGroupModel>> GetAllGroupsAsync()
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
                        FROM public.chatbot_group
                        ORDER BY created_date DESC";

            return await conn.QueryAsync<ChatBotGroupModel>(sql);
        }

        public async Task<ChatBotGroupModel?> GetGroupByIdAsync(Guid chatbotGroupId)
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
                        FROM public.chatbot_group
                        WHERE chatbot_group_id = @chatbotGroupId";

            return await conn.QueryFirstOrDefaultAsync<ChatBotGroupModel>(sql, new { chatbotGroupId });
        }

        public async Task<int> InsertGroupAsync(ChatBotGroupModel model)
        {
            using var conn = GetConnection();

            var exists = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM public.chatbot_group WHERE group_id = @group_id",
                new { model.group_id });

            if (exists > 0)
            {
                // Skip insert
                return 0;
            }

            var sql = @"
                INSERT INTO public.chatbot_group 
                    (chatbot_group_id, group_name, group_id, is_receive_broadcast, created_date, updated_by, last_updated, rowversion)
                VALUES 
                    (@chatbot_group_id, @group_name, @group_id, @is_receive_broadcast, CURRENT_TIMESTAMP, @updated_by, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
            ";

            return await conn.ExecuteAsync(sql, model);
        }


        public async Task<int> UpdateGroupAsync(ChatBotGroupModel model)
        {
            using var conn = GetConnection();
            var sql = @"UPDATE public.chatbot_group
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
            var sql = @"DELETE FROM public.chatbot_group 
                        WHERE chatbot_group_id = @chatbotGroupId";

            return await conn.ExecuteAsync(sql, new { chatbotGroupId });
        }
    }
}
